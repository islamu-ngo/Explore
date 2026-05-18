ABOUTME: Practical troubleshooting guide for this repository's API, BFF, and tenant-aware runtime.
ABOUTME: Prioritizes repeat incidents and non-obvious checks over generic .NET advice.

# Troubleshooting

> **Audience:** Operators | Contributors | Admins
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.API/Program.cs`, `Explore.Blazor/`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, `docs/BACKUP_RESTORE_UPGRADE.md`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`

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
   git diff -- Explore.API/swagger.json Explore.Blazor.Client/Clients/EventApiClient.g.cs
   ```

4. Commit `Explore.API/swagger.json` and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` only when the API-surface change is intentional. Do not hand-edit either file.

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
- triggered by API rate limiting policies (`Global`, `Authenticated`, `Write`, `SetupSecret`, `AnalyticsRelay`).
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
5. Verify `IOutboxMessageDispatcher` registration — default `LoggingOutboxMessageDispatcher` is a no-op that logs warnings.
6. Check `MaxRetryCount` setting — messages retry with exponential backoff before dead-lettering.

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
4. Verify `S3Settings:*` values point to the restored bucket or compatible object store.
5. If migrations already ran, do not manually edit migration history tables; decide rollback vs corrective migration using the rollback matrix in [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md).

## Local URLs

- API: `https://localhost:7039`
- Swagger: `https://localhost:7039/swagger`
- Scalar: mapped by `MapScalarApiReference()` in Development/Testing API runs
- Blazor (dotnet): `https://localhost:7177`
- Blazor (docker compose): `http://localhost:7002`
