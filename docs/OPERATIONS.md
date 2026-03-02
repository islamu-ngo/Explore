ABOUTME: Operational runbook for startup, health, shutdown, and runtime safeguards.
ABOUTME: Captures current behavior implemented in API, Blazor BFF, migration service, and service defaults.

# Operations

## Local Startup Topology (Aspire)

`Explore.AppHost/AppHost.cs` starts services in this order:

1. `Event.MigrationService`
2. `Explore.API` (waits for migration completion)
3. `Explore.Blazor` (waits for migration completion and API readiness)

The Blazor app receives `ExploreAPI__BaseUrl=https://localhost:7039/` from AppHost for local orchestration.

## API Startup Behavior

On startup (except `Testing` environment), API performs:

1. `db.Database.Migrate()`
2. `DatabaseSeeder.SeedAsync(...)`

If migration fails, startup fails (application does not continue).

## Health and Metrics Endpoints

Via `Explore.ServiceDefaults` + app-specific checks:

- `/health`: readiness (all registered checks)
- `/alive`: liveness (`live`-tag checks)
- `/metrics`: Prometheus scraping endpoint

Status-code contract:

- `Healthy` / `Degraded` -> `200`
- `Unhealthy` -> `503`

## Graceful Shutdown Contract

API and Blazor include shutdown-aware checks for rolling deployments.

API specifics:

- grace window: 25 seconds
- during shutdown, health checks become unhealthy so load balancers stop routing traffic

## Request Protection Defaults

API runtime protections include:

### Rate Limiting (`RateLimiting` config section)

| Policy | Mechanism | Partition Key | Defaults |
|---|---|---|---|
| `global` | Token bucket | IP (X-Forwarded-For aware) | 200 tokens, replenish 40/10s. Localhost exempt |
| `authenticated` | Sliding window | User ID | 200 requests/60s, 4 segments |
| `write` | Fixed window | User ID | 30 requests/60s |
| `setup_secret` | Fixed window | IP | 5 requests/60s |

**Rejection**: `429 Too Many Requests` with RFC 6585 ProblemDetails, `Retry-After` header, and `X-RateLimit-*` headers (limit, remaining, reset).

**Testing override**: All rate limiters replaced with `NoLimiter` in `Testing` environment.

**Config keys** (all under `RateLimiting` section):
- `Global:TokenLimit`, `Global:ReplenishmentPeriodSeconds`, `Global:TokensPerPeriod`
- `Authenticated:PermitLimit`, `Authenticated:WindowSeconds`, `Authenticated:SegmentsPerWindow`
- `Write:PermitLimit`, `Write:WindowSeconds`
- `SetupSecret:PermitLimit`, `SetupSecret:WindowSeconds`

### Request Timeouts (`RequestTimeouts` config section)

| Policy | Default | Applied To |
|---|---|---|
| `Default` | 30 seconds | Standard operations |
| `Lookup` | 10 seconds | Lookup/fast queries |
| `Complex` | 60 seconds | Complex queries, exports |

Timeout expiry: `504 Gateway Timeout`.

### Response Compression

- Brotli + Gzip at `CompressionLevel.Fastest`, enabled for HTTPS.
- Additional MIME types: `application/json`, `application/hal+json`.

### ETag / Conditional Requests

- SHA256-based weak ETags on JSON/HAL responses.
- Client sends `If-None-Match` → API returns `304 Not Modified` when content unchanged.
- Saves bandwidth for repeat requests.

### Correlation ID

- `CorrelationIdMiddleware` reads `X-Correlation-ID` or `X-Request-ID` from incoming request.
- Generates new UUID if absent.
- Pushes to Serilog `LogContext` as `CorrelationId` property for structured log correlation.

### Security Headers

Added to every response by `SecurityHeadersMiddleware`:
- `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()`
- `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`
- Non-GET responses additionally receive `Cache-Control: no-store` and `Pragma: no-cache`.

### Business Metrics (OpenTelemetry)

Meter `Explore.Business` exposes counters tagged with `tenant_id` and `resource_type`:
- `events.created`, `events.published`, `registrations.created`, `organizations.created`, `authorization.decisions`

## Setup Secret Lifecycle

Instance bootstrap uses `ISetupSecretProvider`:

- if setup mode is active and no env secret exists, API auto-generates a setup secret and logs it at startup;
- onboarding endpoints in BFF (`/bff/setup-secret*`) validate and synchronize secret state.

## Single-Tenant Endpoint Exposure

`BlockInSingleTenantAttribute` behavior:

- in single-tenant mode with hiding enabled, guarded endpoints return `404` (hidden from discovery).

`RequireMultiTenantAttribute` behavior:

- returns `403` with a clear error payload when feature requires multi-tenant mode.

## Incident Triage Quick Checks

1. Check `/health` and `/alive`.
2. Check API logs for migration/seeding failures.
3. Check rate-limit/timeouts if clients receive `429` or `504`.
4. Check tenant resolution and deployment mode (`deployment.mode`) if tenant-scoped behavior is wrong.
5. Check setup-secret mode if onboarding is blocked.

## Related

- [CONFIGURATION.md](CONFIGURATION.md)
- [SECURITY.md](SECURITY.md)
- [MULTI_TENANCY.md](MULTI_TENANCY.md)
