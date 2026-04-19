ABOUTME: Practical troubleshooting guide for this repository's API, BFF, and tenant-aware runtime.
ABOUTME: Prioritizes repeat incidents and non-obvious checks over generic .NET advice.

# Troubleshooting

## Quick Triage Order

1. Check `https://localhost:7039/health` and `/alive`.
2. Check API startup logs for migration/seed failures.
3. Verify deployment mode and tenant resolution behavior.
4. Verify auth session (`/auth/status`) and token forwarding through BFF.
5. Check rate limiting (`429`) and request timeout (`504`) before deeper debugging.

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
1. Ensure API starts in `Development` so `OpenApiExportService` refreshes `Explore.API/swagger.json`.
2. Rebuild `Explore.Blazor.Client`; its `GenerateApiClient` target regenerates `Clients/EventApiClient.g.cs`.
3. Confirm `swagger.json` timestamp changed.

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

## Setup Secret Failures

Symptoms:
- onboarding blocked at `/setup`.
- setup calls return `410`, `400`, or `502/503`.

Checks:
1. API logs: setup mode active vs completed.
2. BFF endpoints:
   - `POST /bff/setup-secret`
   - `POST /bff/setup-secret/sync`
3. ensure secret is not being injected directly by client headers; proxy strips and re-resolves trusted value.
4. auto-generated setup secrets expire after 60 minutes from API startup.

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
2. If `FailFast` is `true`, missing required secrets will crash at startup. Check logs for `RequiredSecretsValidator` errors.
3. For Infisical: verify `ClientId`, `ClientSecret`, `ProjectId`, and `Environment` are set.
4. Check health endpoint: `/health` includes `secret_provider` check — `Degraded` after 1-2 failures, `Unhealthy` after 3+.
5. If refresh is enabled, check `secrets_refresh_failures_total` Prometheus metric for recurring failures.
6. Key mapping: Infisical uses `SCREAMING_SNAKE_CASE` (e.g., `DATABASE__CONNECTIONSTRING` → `Database:ConnectionString`).

## Local URLs

- API: `https://localhost:7039`
- Swagger: `https://localhost:7039/swagger`
- Scalar: `https://localhost:7039/scalar/v1`
- Blazor (dotnet): `https://localhost:7177`
- Blazor (docker compose): `http://localhost:7002`
