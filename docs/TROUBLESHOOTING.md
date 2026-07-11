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

### Keycloak `unauthorized_client` during login

Cause:
- the Blazor BFF confidential client secret does not match the `islamu-event-blazor` client secret stored in Keycloak.

Checks:
1. Confirm `KEYCLOAK_BLAZOR_CLIENT_SECRET` is set for the Compose environment. Production/self-hosted deployments should not rely on the realm export's static default.
2. Check `docker compose logs keycloak-init` for successful redacted sync messages. The log must not include raw secret values.
3. Rerun `docker compose run --rm keycloak-init` after changing or rotating `KEYCLOAK_BLAZOR_CLIENT_SECRET`.
4. If this is a disposable local stack and no secret is configured, set `KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET=true` intentionally, then rerun `keycloak-init`. Do not use that flag in production.
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
4. auto-generated setup secrets expire after 60 minutes from API startup.
5. if the setup page reports `Environment`, use the configured `SETUP_SECRET` value; if it reports `Generated`, use the API startup log secret; if it reports `Expired`, restart the API to reopen setup mode.
6. use `GET /api/System/onboarding-preflight` to inspect non-sensitive launch blockers and operational warnings before retrying completion.
7. for first-run Compose setup, confirm the operator used the service names and ports from [SELF_HOSTING.md](SELF_HOSTING.md), not older `api`/`blazor` examples.

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
| `authorization_provider_missing` | No explicit authorization provider configuration has been saved. | Save the intended authorization provider configuration; local authorization remains the default until changed. |

## Cerbos / Authorization Provider Issues

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
- If Admin API sync is unavailable, download the manual ZIP package from setup/admin UI and install it with `cerbosctl put`.
- Zero-touch boot sync skips safely when Admin API endpoint or credentials are incomplete; use setup/admin UI sync after configuration is complete.

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
2. Check `/health`: `smtp` covers configured SMTP/Mailpit connectivity, `email-dispatch` covers Basic Dispatch trigger readiness, and `email-dispatch-rabbitmq` covers optional broker topology only when RabbitMQ mode is enabled. If Mailpit is stopped in FullLocal, API `/health` should return HTTP 503 with `smtp` Unhealthy. The SMTP readiness probe is bounded to five seconds; the 2026-07-04 local proof returned in `5.014s`.
3. Inspect HAL-gated EmailDispatch admin status before replaying rows. Replay and park actions must be driven by `_links`; do not infer permissions from local roles.
4. Query `email_dispatch_outbox` by status and tenant. `Unknown` rows are inspectable crash-window outcomes; `DeadLettered` rows require operator review; `Skipped` rows are terminal preference/compliance outcomes.
5. In RabbitMQ mode, verify broker connectivity, dispatch/DLX/parking topology, and bounded logs. Broker payloads must contain only pointer fields, never recipient, subject, body, SMTP credentials, provider IDs, or raw errors.
6. Use `docs/EMAIL_NOTIFICATIONS.md` for focused Mailpit and RabbitMQ verification commands.

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
