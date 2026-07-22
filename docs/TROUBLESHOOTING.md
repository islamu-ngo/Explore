ABOUTME: Practical troubleshooting guide for this repository's API, BFF, and tenant-aware runtime.
ABOUTME: Prioritizes repeat incidents and non-obvious checks over generic .NET advice.

# Troubleshooting

> **Audience:** Operators | Contributors | Admins
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-07-04
> **Source Anchors:** `Explore.API/Program.cs`, `Explore.Blazor/`, `Explore.Infrastructure/Services/Keycloak/KeycloakBootstrapService.cs`, `Explore.Infrastructure/StorageObjectDeletionService.cs`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, `docs/BACKUP_RESTORE_UPGRADE.md`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`

Use this page when you have a symptom. For planned work, installation, backup, restore, upgrade, or rollback procedures, use the linked runbooks instead of copying procedures into this file.

## Related Runbooks

| Need | Go To |
|---|---|
| Install, Compose topology, setup secret, Keycloak, MinIO, Cerbos, reverse proxy | [SELF_HOSTING.md](SELF_HOSTING.md) |
| Back up, restore, upgrade, roll back, or validate release safety | [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) |
| Runtime health, rate limiting, request timeouts, metrics, graceful shutdown | [OPERATIONS.md](OPERATIONS.md) |
| Configuration keys and environment-variable mapping | [CONFIGURATION.md](CONFIGURATION.md) |
| Secret-provider setup and secret naming | [SECRETS.md](SECRETS.md) |
| Auth/authz trust boundaries | [SECURITY.md](SECURITY.md) |

## Quick Triage Order

1. Run the read-only doctor when diagnosing local or self-hosting setup drift:
   `dotnet run --project Explore.Diagnostic/Explore.Diagnostic.csproj -- --root .`
2. Check `https://localhost:7039/health` and `/alive`.
3. Check API startup logs for migration/seed failures.
4. Verify deployment mode and tenant resolution behavior.
5. Verify auth session (`/auth/status`) and token forwarding through BFF.
6. Check rate limiting (`429`) and request timeout (`504`) before deeper debugging.
7. If the issue followed an upgrade or restore, stop and verify [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) rollback and validation steps before changing data.

## Build And Test Failures

Run from solution root:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity normal
```

Important:
- run tests with `--project` (not solution-level `dotnet test`).
- if failures are unclear, generate TRX:
  `dotnet test --project <project.csproj> --configuration Release -- --report-trx --report-trx-filename results.trx`

## OpenAPI / NSwag Drift

Symptoms:
- Blazor compile errors after DTO changes.
- Missing/old generated client types.

Checks:
1. Regenerate the governed OpenAPI document through the same API build-time generation path used by CI:

   ```bash
   dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet
   ```

2. Rebuild `Explore.Blazor.Client`; its `GenerateApiClient` target regenerates `Clients/EventApiClient.g.cs`:

   ```bash
   dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --no-restore --verbosity quiet
   ```

3. Confirm only intentional generated artifacts changed:

   ```bash
   git diff -- schemas/openapi.json Explore.Blazor.Client/Clients/EventApiClient.g.cs
   ```

4. Commit `schemas/openapi.json` and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` only when the API-surface change is intentional. Do not hand-edit either file.

If CI reports drift on an unrelated PR, check `.github/workflows/openapi-contract.yml` job summary. The guard has an internal no-op detector; unrelated changes should pass without regeneration.

## Auth And BFF Issues

### 401 on write endpoints

Checks:
- user is authenticated in BFF (`GET /auth/status`).
- YARP is forwarding bearer token to `/api/*`.
- API token contains expected audience (`islamu-event-api` or `islamu-event-blazor`) and valid issuer.

### OIDC redirect URI errors behind proxy

Cause:
- forwarded proto/host not propagated, so app computes wrong callback URL.

Check:
- proxy sends `X-Forwarded-Proto` and `X-Forwarded-Host`.
- forwarded headers middleware is active in Blazor server pipeline.
- API forwarded-header trust is configured for the reverse proxy; see [CONFIGURATION.md](CONFIGURATION.md) and [SELF_HOSTING.md](SELF_HOSTING.md).

### OIDC `invalid_redirect_uri` in local Aspire

Checks:

1. Run `aspire describe explore-blazor --format Table` and compare its browser-facing origin with the `redirect_uri` shown by Keycloak. Isolated Aspire runs intentionally use generated ports.
2. Confirm `keycloak-init` completed after the current AppHost launch. Local initialization reconciles exact login, origin, and logout values from the allocated Blazor HTTP/HTTPS ports; no Keycloak volume reset is required.
3. If a nonblank `KEYCLOAK_BLAZOR_REDIRECT_URIS`, `KEYCLOAK_BLAZOR_WEB_ORIGINS`, or `KEYCLOAK_BLAZOR_LOGOUT_REDIRECT_URIS` override is configured, it intentionally wins. Update all three exact allow-lists together; do not introduce wildcards or the `+` origin shortcut.

### AT Protocol provider is unavailable or OAuth fails closed

Symptoms:

- AT Protocol is omitted or reported not ready by the BFF provider endpoint.
- authorization fails before redirect, PAR, code exchange, refresh, revocation, or `getSession`.
- logs contain a bounded reason such as `provider_not_configured`, `invalid_public_url_or_callback`, `key_ring_unavailable`, `state_store_unavailable`, `session_store_unavailable`, or `secret_resolver_unavailable`.

Checks:

1. Confirm `Atproto:PublicUrl` is the exact browser-facing HTTPS origin and `Atproto:CallbackPath` matches the published client metadata. Remove credentials, non-root paths, queries, fragments, trailing-dot or Unicode host aliases, and ambiguous callback segments. Do not change the callback for an in-flight flow.
2. Confirm the instance secret provider can resolve `/atproto/ATPROTO_OAUTH_CLIENT_PRIVATE_JWKS`. Do not print the value. Validate only that the ring has bounded canonical P-256/ES256 keys, unique `kid` values, one active key, and any still-needed older keys marked retired. Session persistence and first-party token issuance separately require `/atproto/ATPROTO_SESSION_ENCRYPTION_KEYRING` and `/atproto/ATPROTO_SESSION_JWT_PRIVATE_JWKS`.
3. `state_store_unavailable` or `session_store_unavailable` means the OAuth persistence adapter service is not registered. It does not prove Redis or the backing API is reachable. Restore the registration first, then use the platform dependency health checks and a fresh login to verify the backing store; never enable an in-memory production fallback. Caller cancellation should remain a cancellation, not be reported as a provider or network failure.
4. Verify public DNS for the PDS and authorization server from the BFF and Infrastructure network namespaces. Every answer must be public; a mixed public/private response is rejected. Production rejects loopback, RFC1918, link-local, unspecified, multicast, documentation, and benchmark ranges. Development loopback requires both Development environment and the explicit option.
5. Verify authorization-server metadata is HTTP 200 JSON, bounded, has the exact canonical issuer, and advertises PAR, `private_key_jwt`, ES256 assertion and DPoP algorithms, S256 PKCE, code and refresh grants, code response, issuer response parameter, URL client metadata, and `atproto` scope. Authorization, PAR, token, and optional revocation endpoints may retain their declared query, but redirects and unsafe endpoint hosts are rejected.
6. If a previously working flow fails after about five minutes, start a new discovery/login flow. Endpoint trust is deliberately short-lived. An expired mapping rejects OAuth-shaped POSTs instead of sending them without confidential authentication.
7. For `client_id_mismatch`, callback mismatch, ambiguous-form, invalid-DPoP, or missing-nonce failures, verify the remote server is preserving the published client ID/callback and AT Protocol DPoP contract. Do not relax form, assertion, proof, or nonce validation.
8. During key rotation, keep the previous key as retired until all sessions pinned to its `kid` are expired or revoked. An unknown pinned key must not be silently replaced with the new active key.
9. Do not enable CarpaNet logging or attach raw OAuth bodies to tickets. Never capture authorization codes, refresh/access tokens, assertions, DPoP proofs/nonces, private JWK material, provider response bodies, or user PDS identifiers. Report only the bounded failure category and redacted endpoint class.
10. PDS calls must go through the Infrastructure hardened core-client factory. If `getSession` appears to bypass DNS/redirect controls, check for accidental use of CarpaNet's returned OAuth client and replace it with the application-owned core client.

Cache and PDS recovery:

- Redis or distributed-cache loss invalidates outstanding OAuth state and tenant handoff codes. The `atproto-authentication` check only confirms adapter registration, so restore the cache, confirm the platform cache readiness check, and restart the login from the handle form. Do not retry an old callback and do not enable `UseSingleNodeMemoryStore` outside a single-node Development host. Persisted encrypted OAuth sessions are separate from this transient state.
- A PDS outage can prevent login, refresh, `getSession`, and best-effort remote revocation. Local sign-out must still clear the BFF cookie. Restore public DNS/TLS/PDS availability, then start a new login if refresh reports the durable session invalid, corrupt, revoked, expired, or bound to an unavailable retired key.
- To invalidate a compromised local session, revoke/delete it through the authorized session lifecycle and clear the BFF cookie. Do not edit encrypted session bytes or reuse another user's DID/PDS binding. If remote revocation is unavailable, local removal remains authoritative and the bounded outage result is retained for operators.

Recovery is configuration- and dependency-first: restore the secret resolver or durable stores, correct canonical public URLs/DNS/metadata, retain required retired keys, and start a new flow. Do not bypass endpoint discovery, private-client assertions, DPoP, response limits, or SSRF checks.

### AT Protocol events are missing, pending, retrying, or failed

Symptoms:

- no federated events appear in public discovery;
- a local My Events card remains `AT Protocol publication pending` or `delivery retrying`;
- a card reports `AT Protocol delivery needs attention`;
- an event exists locally but has no PDS record.

Checks:

1. Resolve the effective `federation.atproto_events_enabled` value and lock metadata. The same capability governs both inbound discovery and new eligible outbound enqueue. A disabled or locked-off tenant must show neither federated cards nor new publication work.
2. For inbound discovery, decide whether `Atproto:Jetstream:AllowedDids` should be empty for all public publishers of the exact community collections or contain the intended curated DIDs. With capability enabled, both modes are healthy. Confirm the subscriber lease, last safe cursor, quarantine counts, exact wanted collections, and discovery-cache invalidation without logging DID/rkey/payload values.
3. For outbound publication, confirm the owner enabled only their own `federation.atproto_publish_my_events`, has exactly one linked DID/session for the same tenant and PDS, and the event passed the effective `platform` or `community_lexicon` readiness profile. Administrator enablement never grants personal consent.
4. Confirm the local event publication and immutable outbox row committed. A request handler never calls a PDS, and no recovery procedure may synthesize a PDS record when the local Event is absent. Projection coverage/privacy/size failure intentionally creates no outbox row; do not truncate the description or omit sessions/EAV/aspects/lookups to bypass it.
5. For `pending` or `retrying`, inspect bounded lease age, retry count, next retry time, PDS worker health, and stable failure code. Expired processing leases are reclaimable. Do not manually clear fences or change stable record keys.
6. `session_unavailable`, `session_binding_mismatch`, or `reauth_required` requires the user to reconnect the same AT Protocol account, then update the local event to request publication again.
7. `record_conflict` or `remote_record_missing` means the PDS copy changed or disappeared. Update the local event to request safe reconciliation; do not issue an unfenced direct record write.
8. Provider rate-limit, timeout, or availability failures retry automatically within the row's configured bound. Dead-lettered rows retain only stable codes; raw provider bodies, tokens, DPoP material, and session envelopes must not enter logs, support artifacts, API responses, or the UI.
9. After successful delivery, verify the outbox URI/CID, canonical ownership/presentation, and local Event `AtprotoRecordId` were settled together. A crash after remote success should reconcile the same record key, never create a duplicate.

Local data remains authoritative during every PDS outage or permanent federation failure. Do not delete or roll back a valid local event merely because delivery failed.

### Keycloak `unauthorized_client` during login

Cause:
- the Blazor BFF confidential client secret does not match the `islamu-event-blazor` client secret stored in Keycloak.

Checks:
1. Confirm `KEYCLOAK_BLAZOR_CLIENT_SECRET` is set for the Compose environment. The realm export intentionally contains no confidential client secret.
2. Check `docker compose logs keycloak-init` for successful redacted sync messages. The log must not include raw secret values.
3. Rerun `docker compose run --rm keycloak-init` after changing or rotating `KEYCLOAK_BLAZOR_CLIENT_SECRET`.
4. For disposable Compose development, generate a value with `openssl rand -hex 32`; no default-secret escape hatch exists. Local Aspire generates its own persisted secret parameter when deployment configuration is absent.
5. If the client is missing, verify `docker/keycloak/realm-export.json` imported successfully and that `KEYCLOAK_REALM` matches the imported realm name. Existing Keycloak realms are not overwritten by startup import; reset the disposable Keycloak database volume before expecting realm-export changes to apply.
6. For external Keycloak setup, rerun `/onboarding/auth-provider` bootstrap mode with the same Blazor client ID and the intended runtime client secret. The setup flow updates existing clients by `clientId`; it does not require manually editing the Keycloak UI when the bootstrap credential has client-secret update permission.

### External Keycloak bootstrap fails before contacting Keycloak

Symptoms:
- setup page reports a bad Keycloak URL or unsafe host.
- API returns a safe failure code such as `keycloak_invalid_url` or `keycloak_unsafe_host`.

Checks:
1. Use an absolute `http://` or `https://` Keycloak base URL with no embedded username/password, query string, or fragment.
2. Do not use `localhost`, loopback, link-local, unspecified, or multicast IP literals from the setup form. Use the operator-facing Keycloak DNS name instead.
3. For Compose-managed local Keycloak, prefer the Compose `keycloak-init` service instead of the external bootstrap UI path.

### External Keycloak bootstrap authentication or permission failure

Symptoms:
- setup returns `keycloak_auth_failed`, `keycloak_realm_check_failed`, `keycloak_realm_create_failed`, `keycloak_client_create_failed`, or `keycloak_client_secret_update_failed`.
- Keycloak logs show Admin API `401`, `403`, or rejected client operations.

Checks:
1. Confirm the one-time bootstrap username/password or service-account secret is valid in Keycloak.
2. Confirm the credential can read the target realm, create the realm when using create mode, list clients, create clients, and update client secrets.
3. If using patch-existing mode, verify the realm already exists. Missing realms return `keycloak_realm_not_found`; switch to create mode only if the operator intends ISLAMU to create the realm.
4. If client creation fails with a conflict, rerun bootstrap after confirming the existing client ID is correct. The adapter locates clients by `clientId` before creation and treats existing realms as safe to patch.
5. Do not paste raw Keycloak Admin API response bodies, access tokens, admin passwords, client secrets, or setup secrets into issue reports. Use the safe failure code and Keycloak status code instead.

### External Keycloak bootstrap succeeds but login still fails

Checks:
1. Confirm the Keycloak authority saved by setup is `<base-url>/realms/<realm>` and is reachable from both API and Blazor BFF.
2. Confirm reverse-proxy public origin matches the Blazor client redirect URIs and web origins in Keycloak.
3. Trigger `/bff/auth/refresh-schemes` or restart the Blazor BFF if testing outside the setup UI. The onboarding UI calls the refresh path after successful bootstrap.
4. If `KEYCLOAK_BLAZOR_CLIENT_SECRET` is also configured as deployment-managed, confirm the saved application-managed value is not being overridden by deployment config.

### Post-onboarding Keycloak doctor or sync reports drift

Symptoms:
- the admin auth-provider panel reports missing `offline_access`, missing API audience mapper, missing redirect/web origin entries, or blocked sync operations.
- sync apply is unavailable or returns a blocked plan.

Checks:
1. Run the read-only realm doctor first. Basic mode should verify saved runtime config and OIDC discovery without admin credentials.
2. For drift-aware inspection, enter a temporary Keycloak admin or service-account credential with permission to read realm clients, scopes, roles, protocol mappers, and client settings. ISLAMU uses it only for the active request.
3. Review the sync preview before applying. The plan must be additive; it should not propose deleting a realm, user, group, unrelated client, redirect origin, or operator-managed customization.
4. Before sync apply, confirm a current Keycloak database backup. Apply blocks without backup confirmation and temporary admin credentials.
5. For client-secret rotation, check whether the secret is application-managed or deployment-managed. Deployment-managed secrets must be rotated in environment variables, Infisical, or the owning secret provider rather than overwritten from the app UI.
6. Do not paste temporary admin credentials, Keycloak access tokens, client secrets, raw Admin API response bodies, or screenshots containing secrets into support tickets. Use the safe finding/operation codes from the doctor or sync plan.

## Setup Secret Failures

Symptoms:
- onboarding blocked at `/setup`.
- setup calls return `410`, `400`, or `502/503`.

Checks:
1. API logs: setup mode active vs completed.
2. BFF endpoints:
    - `POST /bff/setup-secret`
    - `POST /bff/setup-secret/sync`
3. ensure browser-supplied `X-Setup-Secret` headers are stripped and ignored. The BFF should forward only resolver output from BFF-owned setup handshake/session state, protected setup cookie, or explicit local/development/bootstrap configuration fallback.
4. setup-secret windows expire after 60 minutes from API startup.
5. if the setup page reports `Environment`, use the configured `SETUP_SECRET` value; if it reports `Generated` or `Expired`, configure `SETUP_SECRET` and restart the API. Raw values are never shown in startup output.
6. use `GET /api/System/onboarding-preflight` to inspect non-sensitive launch blockers and operational warnings before retrying completion.
7. for first-run Compose setup, confirm the operator used the service names and ports from [SELF_HOSTING.md](SELF_HOSTING.md), not older `api`/`blazor` examples.

### Onboarding Recovery Matrix

Refresh before every recovery action. The setup UI is a projection of server status, provider verification, and preflight state; browser history or a cached task list is not recovery evidence.

| Scenario | Detection | Safe action | Operator, rotation, or backup requirement |
|---|---|---|---|
| Invalid or expired setup secret | Validation fails, the source is reported as `Expired`, or the API returns the setup-secret failure response before authentication. | Re-enter the configured secret, or configure `SETUP_SECRET` and restart the API. Do not retry blindly, search output for a raw value, or move the value into browser storage. | Rotate a configured/deployment-managed secret at its owning source if disclosure is suspected. No data restore is required. |
| Provider verification interrupted | The task overview still reports provider verification incomplete/unknown after a redirect, timeout, or lost response. | Refresh authoritative state, then rerun the same verification. Existing provider resources are located and repaired additively. | Rotate temporary provider admin credentials when their handling is uncertain. Take a provider/database backup before any apply operation that changes shared provider state. |
| Authorization provider unavailable | Provider status or preflight reports the configured PDP/Admin API unavailable. | For application-managed configuration, restore Cerbos or explicitly save Local. For deployment-managed `cerbos`, fix `AUTHORIZATION_PROVIDER`/Cerbos deployment values and retry; the browser cannot override deployment intent. | Operator intervention is required. Cerbos remains fail-closed. Do not add or use an inventory endpoint or arbitrary policy-decision test as a recovery shortcut. |
| Blocking preflight check | Preflight classifies an item as blocking. | Fix the named dependency and refresh. Do not bypass completion. Ordinary warnings remain non-blocking; a serious warning may require explicit acknowledgement. | Follow the linked subsystem runbook. Restore from backup only when the blocker identifies corrupted or missing persisted state. |
| Completion submitted repeatedly | The server reports completed/already completed, the response was lost after launch, or retry reaches the completion guard. | Treat the completed state as authoritative and follow the mode-specific handoff. Completion is idempotent; do not reset resources to make it run again. | No secret rotation or restore is needed unless a separate compromise/failure is detected. |
| Refresh after partial success | The refreshed overview shows some tasks complete and a remaining provider/preflight failure. | Preserve successful resources, repair the failed dependency, and retry only the incomplete server-guarded operation. | Back up affected provider/database state before a repair apply. Destructive delete/reimport requires explicit approval. |
| Setup locked after completion | `/setup` reports completed/locked or setup-secret calls are rejected after launch. | Use authenticated instance administration; in multi-tenant mode use `/admin/instance`, and use a tenant-scoped flow only for tenant onboarding. | Never unlock setup by editing database state. A genuine failed-launch recovery requires operator intervention and a verified backup/restore plan. |

## Tenant Resolution Problems

Symptoms:
- wrong tenant branding/data.
- tenant-scoped data appears empty.

Resolution order in API:
1. trusted `X-Tenant-Slug` header from the BFF
2. custom domain
3. subdomain
4. unresolved multi-tenant request returns `404`

Checks:
- trusted forwarded-header processing and normalized request host resolution.
- `deployment.mode` value (`SingleTenant` vs `MultiTenant`).
- for API-key callers, check whether the request hit `Tenant mismatch` (`404`) or `API key authentication failed` (`401`) in `ApiTenantPostAuthenticationMiddleware`.

## Missing HAL Links

If `_links` are missing:
- confirm request did not include `Prefer: return=minimal`.
- link pruning can be authorization-driven (user lacks action permission).
- permission-bound links are evaluated server-side through the HATEOAS authorization pipeline. If Cerbos/local authorization denies or batch evaluation fails, those links are omitted fail-closed.
- admin/sync affordances are also filtered server-side before link materialization. Do not re-enable hidden actions in Blazor by checking client-side roles or claims.
- for sync/history/status navigation links that are auth-only, verify the caller is authenticated and the endpoint is not returning a minimal representation.

## Control-Plane Operations Warnings

The instance control plane exposes warning codes with operator remediation text. Use the code first, then follow the linked status card or backing worker logs.

| Code | What it means | Operator action |
|---|---|---|
| `general_outbox_due_backlog_capped` | The general outbox due backlog reached the bounded reporting cap. | Verify the outbox worker is running and inspect due rows before raising the reporting cap. |
| `general_outbox_failures_present` | General outbox rows are failed or dead-lettered. | Fix the downstream handler or payload issue, then replay from an operator-approved path. |
| `email_provider_missing` | SMTP is not configured for platform email delivery. | Configure SMTP in instance settings before relying on platform email delivery. |
| `email_dispatch_dead_letters` | Email dispatch has dead-lettered rows. | Review dead-lettered dispatch rows, fix provider/configuration failures, and replay only after confirming recipients and payloads. |
| `email_dispatch_stale_processing` | Email dispatch rows are stuck in processing. | Check the worker lease/heartbeat and restart the worker before replaying stuck rows. |
| `email_dispatch_due_backlog` | Email dispatch due backlog exceeds the configured threshold. | Scale or restart dispatch processing and check SMTP provider throttling before increasing thresholds. |
| `moderation_reporting_provider_sync_failures` | Moderation reporting provider-sync links have failed rows at or above the configured threshold. | Review failed reporting provider sync records and provider configuration from an operator-approved path. Do not paste provider payloads, endpoint URLs, API keys, webhook secrets, raw provider errors, report evidence, or callback signatures into tickets or logs. |
| `moderation_reporting_provider_sync_stuck` | Moderation reporting provider-sync links have pending rows older than `Reporting:Health:StuckProviderSyncMinutes`. | Verify the report provider sync dispatcher is running, then check provider connectivity and tenant/instance routing locks without exposing provider identifiers or payload material. |
| `storage_provider_unavailable` | The configured storage provider reports unavailable. | Verify storage provider settings, credentials, bucket/root reachability, and health checks before upload-heavy operations. |
| `deployment_mode_not_multi_tenant` | Control-plane overview was reached outside multi-tenant mode. | Switch to a configured multi-tenant deployment; do not toggle mode casually from runtime settings. |
| `public_host_missing` | No public origin or instance base domain is configured. | Set `PublicBaseUrl` or the instance base domain before creating tenant DNS records. |
| `authentication_provider_missing` | No authentication provider is configured. | Complete authentication-provider setup and verify OIDC discovery before tenant onboarding. |
| `authorization_provider_missing` | Manual provider intent is unsaved, or explicit Cerbos reconciliation has not completed. | With blank intent, save Local or configure Cerbos. With deployment-managed Cerbos, repair the PDP/Admin API configuration and retry background reconciliation. |

## Cerbos / Authorization Provider Issues

### Deployment Provider Selection

- `AUTHORIZATION_PROVIDER` accepts blank, `local`, or `cerbos`. Invalid explicit values fail startup options validation.
- Blank means manual onboarding. Cerbos endpoint or credential variables are bootstrap prerequisites and do not select the provider; Local RBAC remains the default until the operator opens the advanced Cerbos disclosure and saves it.
- `local` is deployment-owned, skips the provider-choice page, and performs no Cerbos call.
- `cerbos` is deployment-owned, skips the automatic choice page while background work is pending or ready, and selects Cerbos at runtime immediately. The API worker retries PDP verification before publishing the package to the instance Admin API. Until both succeed, configured status remains false and authorization fails closed. After the retry bound is exhausted, use the instance authorization task to view safe failure guidance and retry after fixing deployment settings.

### Instance Cerbos PDP Unavailable

- When the instance provider is `cerbos`, runtime authorization fails closed. It does **not** fall back to local RBAC.
- Check the PDP gRPC endpoint and app readiness health. Switch to local mode only as an explicit operator action.

### BYO Tenant PDP Failure

- `failure_mode=closed` activates provider-instance safe mode and denies non-instance-admin checks.
- `failure_mode=open` uses local RBAC fallback only for that tenant BYO failure path.
- BYO config resolver failures also activate provider-instance safe mode.

### Blank Custom PDP Endpoint

- `cerbos.mode=custom_endpoint` with a blank PDP endpoint remains BYO mode. It preserves the tenant failure mode and any explicit BYO Admin API config instead of falling back to the instance PDP.
- Configure the PDP endpoint, change the tenant mode, or use manual ZIP/Admin API package operations while runtime authorization follows the configured failure mode.

### Admin API Package Sync Failure

- Use the safe issue code first: Admin API not configured, auth failure, unavailable/rejected package, reload failure, or package-status unknown.
- Verify endpoint TLS/safety and credentials without logging or copying raw secrets into support artifacts.
- For blank/application-managed provider intent, if Admin API sync is unavailable, download the manual ZIP package from setup/admin UI and install it with `cerbosctl put`.
- With explicit `AUTHORIZATION_PROVIDER=cerbos`, missing or rejected Admin API configuration makes reconciliation fail safely and leaves readiness blocked. Fix the server-side values and use the locked remediation retry; credentials are never entered or returned on that page.
- Troubleshooting scope is endpoint verification, package download/sync, and the configured local fallback only. There is no supported Cerbos resource-inventory or arbitrary policy-decision test API.

## 429 / 504 Responses

`429`:
- triggered by API rate limiting policies (`Global`, `Authenticated`, `Write`, `PublicIngestion`, `SetupSecret`, `AnalyticsRelay`, `AiAssistant`).
- inspect `Retry-After`, `X-RateLimit-Limit`, and `X-RateLimit-Remaining` headers and caller behavior.

`504`:
- request timeout policy exceeded (`Default`, `Lookup`, `Complex`).
- verify endpoint timeout category and long-running query behavior.

## Analytics And Tracking Issues

Symptoms:
- pageviews are missing in dashboards.
- relay requests return `204`, `4xx`, or appear absent.
- analytics appears disabled for one tenant but not another.

Checks:
1. Confirm resolved `analytics.enabled`, `analytics.provider`, `analytics.transport_mode`, and `analytics.endpoint_url` for the affected tenant.
2. If using `direct` or `proxy`, inspect browser network failures and CSP violations before assuming provider-side breakage.
3. If using `relay`, confirm the browser can reach `POST /api/a/t` and that the endpoint is not being rate-limited.
4. Remember that analytics failures should degrade to no-op behavior; if user-facing routes break, the issue is larger than analytics transport.
5. If the operator wants an emergency stop, switch to provider `none` or disable analytics rather than removing scripts manually.

## Outbox Processor Issues

**Symptoms:** Messages stuck in `Pending` status, events not being dispatched.

Checks:
1. Verify `OutboxProcessor:Enabled` is `true` in configuration.
2. Check logs for `OutboxProcessor` — look for polling activity and dispatch errors.
3. Query `outbox_messages` table: `SELECT status, COUNT(*) FROM outbox_messages GROUP BY status`.
4. If messages are `DeadLettered`, check `last_error` column for root cause. Dead-lettered messages stay in DB indefinitely for manual review.
5. Verify `IOutboxMessageDispatcher` registration — the general outbox path should resolve `CompositeOutboxMessageDispatcher`, which routes internal notification fanout, moderation fanout, and report provider sync messages. Retired external broker event types should fail closed instead of being marked complete.
6. Check `MaxRetryCount` setting — messages retry with exponential backoff before dead-lettering.

## Email Dispatch Issues

**Symptoms:** Registration confirmation email does not arrive, `email-dispatch` readiness is degraded/unhealthy, or RabbitMQ dispatch/DLQ counts grow.

Checks:
1. For local development, open Mailpit at the Aspire-discovered UI endpoint or Compose default `http://localhost:8025`. Non-isolated Aspire normally uses SMTP `localhost:1025`; isolated Aspire assigns dynamic ports, so run `aspire describe mailpit --apphost Explore.AppHost/Explore.AppHost.csproj --format Json` and verify API `email.smtp_port` matches the current Mailpit SMTP endpoint. Compose uses `mailpit:1025` from API containers.
2. Check `/health`: `smtp` covers configured SMTP/Mailpit connectivity, `email-dispatch` covers Basic Dispatch trigger readiness, `email-dispatch-retention-cleanup` covers the retention worker posture, and `email-dispatch-rabbitmq` covers optional broker topology only when RabbitMQ mode is enabled. If Mailpit is stopped in FullLocal, API `/health` should return HTTP 503 with `smtp` Unhealthy. The SMTP readiness probe is bounded to five seconds; the 2026-07-04 local proof returned in `5.014s`.
3. Inspect HAL-gated EmailDispatch admin status before changing rows. Use only emitted `_links`. `Unknown` never exposes generic replay: reconcile it as `Delivered` or `NotDelivered` only with provider evidence, or use `resolve-without-replay` when the outcome cannot be proven and the work must be abandoned. Redacted and `Processing` rows expose no mutation affordances.
4. Query `email_dispatch_outbox` by status and tenant. `Unknown` rows are inspectable crash-window outcomes; `DeadLettered` rows require operator review; `Skipped` rows are terminal preference/compliance outcomes.
   - For planned lifecycle rows, compare linked `NotificationDelivery` and immutable occurrence/policy versions. Do not replay `ContentRedactedAt`, consent-withdrawn, superseded, tenant-deleted, or post-handoff `Unknown` work until the authorized reconciliation surface says it is replayable.
   - If one tenant dominates the backlog, inspect fair-selection, per-tenant concurrency/rate limits, and high/low watermarks before increasing global throughput. Optional reminders should defer before required cancellation/moderation work.
   - Rows with `last_failure_category = 'smtp_rate_deferred'` did not call SMTP and did not consume an attempt. Compare `email_dispatch_processor_states.smtp_refill_at` with the tenant row in `email_dispatch_tenant_controls`; the later exhausted refill boundary controls `next_attempt_at`. Do not manually create attempt/receipt evidence for these rows.
   - An unfenced stale lease becomes `RetryScheduled`; a stale row with the current `provider_handoff_started` attempt or a processing receipt becomes `Unknown`. If those classifications look wrong, inspect the exact outbox lease/attempt graph before using a HAL replay action.
   - If `globalPaused=true`, inspect `GET /api/admin/email-dispatch/control`. Resume only after the incident is contained. A temporary global rate override is visible as a boolean in public health and as its sanitized value in the authenticated control resource.
   - For a compromised tenant sender, pause that tenant immediately with `PUT /api/admin/email-dispatch/tenants/{tenantId}/pause?reason=...`; use the tenant suppression/redaction lifecycle when tenant deletion or compromise requires queued content removal. Do not pause every tenant unless the provider/instance itself is unsafe.
   - Distinguish tenant SMTP failure from instance failure: one tenant failing while others send points to tenant override/governance or tenant pause; all tenants failing plus `smtp` health failure points to instance credentials, DNS/TLS, provider outage, or global pause/rate control.
   - Before changing retention, set `EmailDispatchRetention:DryRun=true`, restart the worker, compare only aggregate eligible counts/cutoff, then restore mutating mode. Dry-run must not be used as proof of message delivery.
5. In RabbitMQ mode, verify broker connectivity, dispatch/DLX/parking topology, and bounded logs. Broker payloads must contain only pointer fields, never recipient, subject, body, SMTP credentials, provider IDs, or raw errors.
6. Use `docs/EMAIL_NOTIFICATIONS.md` for focused Mailpit and RabbitMQ verification commands.

**Coop callback retained but no moderation decision occurs:**

1. Check `/health/webhooks/coop-effects` for disabled processing, due backlog, stale leases, or a PostgreSQL readiness failure.
2. Use the authenticated tenant-scoped `GET /api/admin/incoming-webhook-effects/status` surface. Do not query or copy raw callback bytes into tickets or logs.
3. `Pending`/`Failed` waits for `NextAttemptAt`; `Processing` with an expired lease is recovered by the next claim; `DeadLettered` requires the permanent input/configuration problem to be corrected before operator redrive.
4. Confirm the retained inbox message still has payload bytes and its replay window has not expired. Cleanup deliberately makes redrive unavailable after that boundary.
5. Use only the HAL `redrive` action and the row's current processing generation. Generation conflict means another operator/worker already changed the row; reload status instead of retrying stale input.
6. Do not directly invoke `ProcessCoopDecisionCallbackCommand` or mutate pointer/receipt rows. The durable worker is the authority for fenced execution and command-success settlement.

Duplicate callbacks, duplicate pointers, dispatcher replay, and expired-worker replay should settle idempotently. If a completed report case appears reopened, treat that as a moderation state-machine incident: retain the safe effect/audit identifiers, stop redrive, and investigate the decision command's stale/out-of-order guard without exporting provider payloads or raw errors.

## Aspire Detached Lifecycle Issues

**Symptoms:** `aspire start --format Json --isolated` or `aspire run --detach --format Json --isolated` returns AppHost/CLI PIDs and a dashboard URL, but an immediate `aspire ps --format Json` returns `[]`, `aspire describe` reports no running AppHost, and the returned AppHost PID no longer exists.

Checks:
1. Confirm the foreground proof path works first: `aspire run --apphost Explore.AppHost/Explore.AppHost.csproj --isolated`. Use that path for launch runtime evidence.
2. Inspect the detached child log printed in the JSON output. The 2026-07-04 local `13.4.6` reproduction reached resource readiness and logged `Notifying AppHost startup readiness`, then the process disappeared before `aspire ps` could inspect it.
3. Verify `Explore.Blazor` builds if AppHost startup fails during the embedded admin-shell compilation; control-plane host routing lives under `Explore.Blazor/Components/ControlPlane/`.
4. Treat repeated empty `aspire ps` after detached startup as an Aspire CLI/tooling lifecycle issue, not as proof that the API or Blazor cannot run. Foreground FullLocal startup, health, and public smoke are the current trusted readiness evidence.
5. Re-test detached mode only after updating the Aspire CLI or when explicitly investigating CLI lifecycle behavior.

## Footer Settings Issues

**Symptoms:** Footer not rendering, template changes not visible, governance locks not applying.

Checks:
1. Verify `footer.enabled` setting is `true` for the tenant.
2. Check `footer.template` value matches a known template: `standard-3-col`, `standard-2-col`, `minimal`, `community`.
3. In multi-tenant mode, check instance governance locks (`footer.lock_tenant_template`, `footer.lock_tenant_link_groups`, etc.) — locked settings prevent tenant overrides.
4. If social links not showing, verify `footer.show_social_links` is `true` and `footer.social_links` JSON array is valid.
5. After changing footer settings via API, the public config endpoint (`GET /api/footer/config`) may be output-cached — wait or clear cache.

## Secret Provider Issues

**Symptoms:** Application fails to start, secrets not loaded, connection strings missing.

Checks:
1. Check `SecretProvider:Provider` config value — `None` uses env vars, `Infisical` uses Infisical API.
2. Provider configuration is validated at startup. Check logs for `SecretProviderOptionsValidator` errors before changing secret values.
3. For Infisical: verify `ClientId`, `ClientSecret`, `ProjectId`, and `Environment` are set.
4. Check health endpoint: `/health` includes the `secret_provider` check — `Degraded` after 1-2 failures, `Unhealthy` after 3+.
5. If refresh is enabled, check `secrets_refresh_failures_total` Prometheus metric for recurring failures.
6. Key mapping: Infisical/domain secret names use `SCREAMING_SNAKE_CASE`, while .NET environment overrides use double-underscore keys such as `S3Settings__Endpoint`. PostgreSQL bootstrap values are discrete `POSTGRESQL_*` values, not a single URL-form connection string; see [SECRETS.md](SECRETS.md).

## Storage Readiness Or Upload Failures

Symptoms:
- API `/health` reports the `storage` check as unhealthy.
- Upload sessions fail before bytes are accepted.
- Metadata-backed downloads return missing-object behavior.

Checks:
1. Confirm the selected provider in instance storage settings. Local-first deployments should not require the Compose `storage` profile or S3 credentials.
2. For local-first storage, verify the API process can read/write `Storage:Local:RootPath`. Compose defaults to `/app/storage-data/local` mounted on the `local_storage_data` volume.
3. For optional S3-compatible mode, verify `S3Settings:*` values or persisted `s3.*` settings point to the intended endpoint/bucket and that secrets are present.
4. Use the instance storage provider test action or `/health` response failure code; do not expose host filesystem paths, bucket names, object keys, access keys, or raw provider errors in tickets.
5. If metadata exists but downloads fail, run reconciliation in dry-run mode and compare the reported missing-object/orphan counts before changing lifecycle state.

## Storage Reconciliation Drift

Symptoms:
- `storage-reconciliation` health is degraded or operators see recurring dry-run drift counts.
- Local files exist on disk without matching metadata.
- Metadata points to missing backing objects.

Checks:
1. `StorageReconciliation:DryRun` defaults to `true`; dry-run reports drift without mutation.
2. Before enabling mutations, confirm backups include application PostgreSQL plus `local_storage_data` or the selected S3-compatible object store from the same release manifest.
3. To quarantine missing metadata/object mismatches, set `StorageReconciliation:DryRun=false` and only the needed quarantine flag.
4. To physically delete delete-eligible objects, also set `StorageReconciliation:DeleteQuarantinedObjects=true`; provider delete is idempotent, but metadata is soft-deleted afterward.
5. If mutations ran against the wrong environment, stop write traffic, turn dry-run back on, restore database and object-storage backups together, then rerun dry-run reconciliation.

## Heavy Moderation Image Deletion Pending

Symptoms:
- Heavy event moderation returns a pending image-deletion retry failure after the event was redacted.
- Storage delete metrics show failed provider deletes for event-owned `delete_requested` rows.

Checks:
1. Treat the event redaction as committed. Public event reads and image foreign keys should already be unavailable; do not restore event content to retry image deletion.
2. Check `/health` storage readiness and provider configuration for the selected local or S3-compatible provider.
3. Re-run the heavy-redaction command for the same event after provider readiness is fixed. The command is idempotent for already-heavy-redacted events and retries remaining event-owned `delete_requested` image rows.
4. If command retry is not possible, use storage reconciliation only after verifying backups and intentionally enabling destructive mutation flags. Reconciliation can delete eligible `delete_requested` metadata through the provider abstraction.
5. Support tickets may include bounded failure category, provider name, tenant id, event id, and storage object id. Do not include object keys, filenames, filesystem paths, S3 endpoints, bucket names, credentials, raw provider responses, or raw exception text.

## Upgrade Or Restore Regressions

Symptoms:
- API starts but data is missing or from the wrong environment.
- Keycloak login works but users or clients are missing.
- Object downloads fail after a restore.
- Migrations ran during startup and rollback is being considered.

Checks:
1. Stop write traffic before repeated restore attempts.
2. Compare the release manifest, database dump timestamp, object storage snapshot, and secret/config snapshot from [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md).
3. Verify application PostgreSQL and Keycloak PostgreSQL were restored from the intended snapshots.
4. Verify `Storage:Local:RootPath` points to the restored local storage data, or that `S3Settings:*` values point to the restored bucket or compatible object store when S3-compatible mode is selected.
5. If migrations already ran, do not manually edit migration history tables; decide rollback vs corrective migration using the rollback matrix in [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md).

## Local URLs

- API: `https://localhost:7039`
- Swagger: `https://localhost:7039/swagger`
- Scalar: mapped by `MapScalarApiReference()` in Development/Testing API runs
- Blazor (dotnet): `https://localhost:7177`
- Blazor (docker compose): `http://localhost:7002`
