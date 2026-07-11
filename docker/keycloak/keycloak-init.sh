# ABOUTME: One-shot Keycloak realm bootstrap for synchronizing local realm auth settings and OIDC clients.
# ABOUTME: Uses kcadm.sh idempotently and keeps admin credentials, tokens, and client secrets out of logs.

#!/usr/bin/env bash

set -euo pipefail

KEYCLOAK_INTERNAL_URL="${KEYCLOAK_INTERNAL_URL:-http://keycloak:8080}"
KEYCLOAK_REALM="${KEYCLOAK_REALM:-ISLAMU}"
KEYCLOAK_ADMIN="${KEYCLOAK_ADMIN:-}"
KEYCLOAK_ADMIN_PASSWORD="${KEYCLOAK_ADMIN_PASSWORD:-}"
KEYCLOAK_BLAZOR_CLIENT_ID="${KEYCLOAK_BLAZOR_CLIENT_ID:-islamu-event-blazor}"
KEYCLOAK_API_CLIENT_ID="${KEYCLOAK_API_CLIENT_ID:-islamu-event-api}"
KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET="${KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET:-false}"
KEYCLOAK_SMTP_HOST="${KEYCLOAK_SMTP_HOST:-}"
KEYCLOAK_SMTP_PORT="${KEYCLOAK_SMTP_PORT:-}"
KEYCLOAK_SMTP_FROM="${KEYCLOAK_SMTP_FROM:-}"
KEYCLOAK_SMTP_FROM_DISPLAY_NAME="${KEYCLOAK_SMTP_FROM_DISPLAY_NAME:-}"
KEYCLOAK_SMTP_AUTH="${KEYCLOAK_SMTP_AUTH:-false}"
KEYCLOAK_SMTP_SSL="${KEYCLOAK_SMTP_SSL:-false}"
KEYCLOAK_SMTP_STARTTLS="${KEYCLOAK_SMTP_STARTTLS:-false}"
KEYCLOAK_SMTP_REPLY_TO="${KEYCLOAK_SMTP_REPLY_TO:-}"
KEYCLOAK_SMTP_REPLY_TO_DISPLAY_NAME="${KEYCLOAK_SMTP_REPLY_TO_DISPLAY_NAME:-}"
KEYCLOAK_SMTP_ENVELOPE_FROM="${KEYCLOAK_SMTP_ENVELOPE_FROM:-}"
KEYCLOAK_SMTP_USER="${KEYCLOAK_SMTP_USER:-}"
KEYCLOAK_SMTP_PASSWORD="${KEYCLOAK_SMTP_PASSWORD:-}"
DEFAULT_LOCAL_BLAZOR_SECRET="islamu-event-blazor-secret"
KCADM="${KCADM:-/opt/keycloak/bin/kcadm.sh}"
BLAZOR_REDIRECT_URIS='["http://localhost:7002/*","https://localhost:7177/*","https://100.64.0.2:7177/*","http://localhost/*","https://localhost/*","http://127.0.0.1/*","https://100.64.0.2/*","http://100.64.0.2/*","http://admin.localhost:7002/*","http://admin.localhost/*","https://admin.localhost/*","http://host.docker.internal/*"]'
BLAZOR_LOGOUT_REDIRECT_URIS='http://localhost:7002/*##https://localhost:7177/*##http://localhost/*##https://localhost/*##https://100.64.0.2:7177/*##https://100.64.0.2/*##http://100.64.0.2:7177/*##http://100.64.0.2/*##http://admin.localhost:7002/*##http://admin.localhost/*##https://admin.localhost/*'

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

json_escape() {
  local value="${1//\\/\\\\}"
  value="${value//\"/\\\"}"
  printf '%s' "$value"
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

enable_realm_registration() {
  "$KCADM" update "realms/$KEYCLOAK_REALM" -s registrationAllowed=true >/dev/null
  log "Enabled self-registration for realm '$KEYCLOAK_REALM'."
}

sync_realm_smtp_settings() {
  local smtp_server

  if [ -z "$KEYCLOAK_SMTP_HOST$KEYCLOAK_SMTP_PORT$KEYCLOAK_SMTP_FROM" ]; then
    log "KEYCLOAK_SMTP_* is unset; leaving realm SMTP settings unchanged."
    return 0
  fi

  require_non_empty "KEYCLOAK_SMTP_HOST" "$KEYCLOAK_SMTP_HOST"
  require_non_empty "KEYCLOAK_SMTP_PORT" "$KEYCLOAK_SMTP_PORT"
  require_non_empty "KEYCLOAK_SMTP_FROM" "$KEYCLOAK_SMTP_FROM"

  smtp_server="{\"host\":\"$(json_escape "$KEYCLOAK_SMTP_HOST")\",\"port\":\"$(json_escape "$KEYCLOAK_SMTP_PORT")\",\"from\":\"$(json_escape "$KEYCLOAK_SMTP_FROM")\",\"fromDisplayName\":\"$(json_escape "$KEYCLOAK_SMTP_FROM_DISPLAY_NAME")\",\"auth\":\"$(json_escape "$KEYCLOAK_SMTP_AUTH")\",\"ssl\":\"$(json_escape "$KEYCLOAK_SMTP_SSL")\",\"starttls\":\"$(json_escape "$KEYCLOAK_SMTP_STARTTLS")\""

  if [ -n "$KEYCLOAK_SMTP_REPLY_TO" ]; then
    smtp_server="$smtp_server,\"replyTo\":\"$(json_escape "$KEYCLOAK_SMTP_REPLY_TO")\""
  fi
  if [ -n "$KEYCLOAK_SMTP_REPLY_TO_DISPLAY_NAME" ]; then
    smtp_server="$smtp_server,\"replyToDisplayName\":\"$(json_escape "$KEYCLOAK_SMTP_REPLY_TO_DISPLAY_NAME")\""
  fi
  if [ -n "$KEYCLOAK_SMTP_ENVELOPE_FROM" ]; then
    smtp_server="$smtp_server,\"envelopeFrom\":\"$(json_escape "$KEYCLOAK_SMTP_ENVELOPE_FROM")\""
  fi
  if [ -n "$KEYCLOAK_SMTP_USER" ]; then
    smtp_server="$smtp_server,\"user\":\"$(json_escape "$KEYCLOAK_SMTP_USER")\""
  fi
  if [ -n "$KEYCLOAK_SMTP_PASSWORD" ]; then
    smtp_server="$smtp_server,\"password\":\"$(json_escape "$KEYCLOAK_SMTP_PASSWORD")\""
  fi

  smtp_server="$smtp_server}"

  "$KCADM" update "realms/$KEYCLOAK_REALM" -s "smtpServer=$smtp_server" >/dev/null
  log "Synchronized SMTP settings for realm '$KEYCLOAK_REALM'."
}

sync_blazor_client_settings() {
  local uuid
  local attributes

  uuid="$(client_uuid "$KEYCLOAK_BLAZOR_CLIENT_ID")"
  if [ -z "$uuid" ]; then
    fail "Keycloak client '$KEYCLOAK_BLAZOR_CLIENT_ID' was not found in realm '$KEYCLOAK_REALM'. Confirm realm import completed successfully."
  fi

  attributes="{\"pkce.code.challenge.method\":\"S256\",\"post.logout.redirect.uris\":\"$BLAZOR_LOGOUT_REDIRECT_URIS\",\"backchannel.logout.session.required\":\"true\",\"backchannel.logout.revoke.offline.tokens\":\"false\",\"use.refresh.tokens\":\"true\",\"oauth2.device.authorization.grant.enabled\":\"false\",\"oidc.ciba.grant.enabled\":\"false\"}"

  "$KCADM" update "clients/$uuid" -r "$KEYCLOAK_REALM" \
    -s "redirectUris=$BLAZOR_REDIRECT_URIS" \
    -s 'webOrigins=["+"]' \
    -s "attributes=$attributes" >/dev/null

  log "Synchronized redirect URIs for client '$KEYCLOAK_BLAZOR_CLIENT_ID'."
}

main() {
  local blazor_secret

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

  enable_realm_registration
  sync_realm_smtp_settings
  sync_blazor_client_settings

  blazor_secret="$(resolve_blazor_secret)"
  set_client_secret "$KEYCLOAK_BLAZOR_CLIENT_ID" "$blazor_secret"

  if [ -n "${KEYCLOAK_API_CLIENT_SECRET:-}" ]; then
    set_client_secret "$KEYCLOAK_API_CLIENT_ID" "$KEYCLOAK_API_CLIENT_SECRET"
  else
    log "KEYCLOAK_API_CLIENT_SECRET is unset; leaving optional API resource-server client secret unchanged."
  fi

  log "Keycloak client secret synchronization completed."
}

main "$@"
