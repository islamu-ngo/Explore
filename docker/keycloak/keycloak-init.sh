# ABOUTME: One-shot Keycloak realm bootstrap for synchronizing Compose-managed OIDC client secrets.
# ABOUTME: Uses kcadm.sh idempotently and keeps admin credentials, tokens, and client secrets out of logs.

#!/usr/bin/env bash

set -euo pipefail

KEYCLOAK_INTERNAL_URL="${KEYCLOAK_INTERNAL_URL:-http://keycloak:8080}"
KEYCLOAK_REALM="${KEYCLOAK_REALM:-ISLAMU}"
KEYCLOAK_ADMIN="${KEYCLOAK_ADMIN:-}"
KEYCLOAK_ADMIN_PASSWORD="${KEYCLOAK_ADMIN_PASSWORD:-}"
KEYCLOAK_BLAZOR_CLIENT_ID="${KEYCLOAK_BLAZOR_CLIENT_ID:-islamu-event-blazor}"
KEYCLOAK_CONTROL_PLANE_CLIENT_ID="${KEYCLOAK_CONTROL_PLANE_CLIENT_ID:-islamu-event-control-plane}"
KEYCLOAK_API_CLIENT_ID="${KEYCLOAK_API_CLIENT_ID:-islamu-event-api}"
KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET="${KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET:-false}"
DEFAULT_LOCAL_BLAZOR_SECRET="islamu-event-blazor-secret"
DEFAULT_LOCAL_CONTROL_PLANE_SECRET="islamu-event-control-plane-secret"
KCADM="${KCADM:-/opt/keycloak/bin/kcadm.sh}"

log() {
  printf '[keycloak-init] %s\n' "$1" >&2
}

fail() {
  log "ERROR: $1"
  exit 1
}

require_non_empty() {
  local name="$1"
  local value="$2"

  if [ -z "$value" ]; then
    fail "$name is required. Set it through environment/secret-provider configuration."
  fi
}

resolve_blazor_secret() {
  if [ -n "${KEYCLOAK_BLAZOR_CLIENT_SECRET:-}" ]; then
    printf '%s' "$KEYCLOAK_BLAZOR_CLIENT_SECRET"
    return 0
  fi

  if [ "$KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET" = "true" ]; then
    log "Using the opt-in local development default for ${KEYCLOAK_BLAZOR_CLIENT_ID}. Do not use this in production."
    printf '%s' "$DEFAULT_LOCAL_BLAZOR_SECRET"
    return 0
  fi

  fail "KEYCLOAK_BLAZOR_CLIENT_SECRET is required for the confidential Blazor BFF client. Set KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET=true only for throwaway local development."
}

resolve_control_plane_secret() {
  if [ -n "${KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET:-}" ]; then
    printf '%s' "$KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET"
    return 0
  fi

  if [ "$KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET" = "true" ]; then
    log "Using the opt-in local development default for ${KEYCLOAK_CONTROL_PLANE_CLIENT_ID}. Do not use this in production."
    printf '%s' "$DEFAULT_LOCAL_CONTROL_PLANE_SECRET"
    return 0
  fi

  return 1
}

client_uuid() {
  local client_id="$1"
  "$KCADM" get clients -r "$KEYCLOAK_REALM" -q "clientId=$client_id" --fields id --format csv --noquotes | head -n 1 | tr -d '\r'
}

set_client_secret() {
  local client_id="$1"
  local secret="$2"
  local uuid

  require_non_empty "secret for client $client_id" "$secret"

  uuid="$(client_uuid "$client_id")"
  if [ -z "$uuid" ]; then
    fail "Keycloak client '$client_id' was not found in realm '$KEYCLOAK_REALM'. Confirm realm import completed successfully."
  fi

  "$KCADM" update "clients/$uuid" -r "$KEYCLOAK_REALM" -s "secret=$secret" >/dev/null
  log "Synchronized secret for client '$client_id' (value redacted)."
}

main() {
  local blazor_secret
  local control_plane_secret

  require_non_empty "KEYCLOAK_ADMIN" "$KEYCLOAK_ADMIN"
  require_non_empty "KEYCLOAK_ADMIN_PASSWORD" "$KEYCLOAK_ADMIN_PASSWORD"

  if [ ! -x "$KCADM" ]; then
    fail "kcadm.sh was not found at '$KCADM'. Run this script inside the Keycloak image or set KCADM."
  fi

  log "Authenticating to Keycloak Admin API at ${KEYCLOAK_INTERNAL_URL} for realm '${KEYCLOAK_REALM}'."
  "$KCADM" config credentials \
    --server "$KEYCLOAK_INTERNAL_URL" \
    --realm master \
    --user "$KEYCLOAK_ADMIN" \
    --password "$KEYCLOAK_ADMIN_PASSWORD" >/dev/null

  blazor_secret="$(resolve_blazor_secret)"
  set_client_secret "$KEYCLOAK_BLAZOR_CLIENT_ID" "$blazor_secret"

  if control_plane_secret="$(resolve_control_plane_secret)"; then
    set_client_secret "$KEYCLOAK_CONTROL_PLANE_CLIENT_ID" "$control_plane_secret"
  else
    log "KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET is unset; leaving optional control-plane BFF client secret unchanged."
  fi

  if [ -n "${KEYCLOAK_API_CLIENT_SECRET:-}" ]; then
    set_client_secret "$KEYCLOAK_API_CLIENT_ID" "$KEYCLOAK_API_CLIENT_SECRET"
  else
    log "KEYCLOAK_API_CLIENT_SECRET is unset; leaving optional API resource-server client secret unchanged."
  fi

  log "Keycloak client secret synchronization completed."
}

main "$@"
