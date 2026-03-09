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

## Analytics Operational Contract

Analytics is optional infrastructure. Provider failures must never block normal product flows.

Current operational expectations:

- `NullAnalyticsProvider` is the safe baseline for disabled deployments.
- Runtime provider selection is tenant-aware and settings-driven through `AnalyticsConfigResolver`.
- Provider/network failures are logged and swallowed so command/query flows continue.
- Self-hosted operators may need first-party proxying or custom endpoints via `analytics.endpoint_url` to reduce CSP or ad-blocker loss.
- Browser relay fallback is available at `POST /api/a/t` for operators who cannot or do not want to load vendor analytics scripts in the browser.

Current provider capability tiers:

| Provider | Current operational tier | Notes |
|---|---|---|
| `none` | Disabled | First-class no-op mode |
| `plausible` | Lightweight web analytics | Pageviews and custom events only; no identify/group semantics by design |
| `posthog` | Rich product analytics | Richest current provider: identify, groups, and feature flags |
| `rybbit` | Browser-richer / validate before broad rollout | Current server code is track/pageview-focused; official docs justify browser identify but not server/group/flag parity |
| `rudderstack` | Advanced pipeline / validate before broad rollout | Full event-spec surface in code, but better understood as a CDP/router than a first-party analytics backend |

Self-hoster deployment tiers:

| Tier | Typical operator posture | Expected analytics mode | Operational focus |
|---|---|---|---|
| Tier 0 | Privacy-first / no analytics | `none` | Zero breakage, zero analytics dependency |
| Tier 1 | Lightweight self-hosting | `plausible`-style web analytics | Simple setup, low overhead, pageview/custom event tracking |
| Tier 2 | Product analytics | `posthog`, `rybbit`, or `rudderstack` | Richer events, optional identity semantics, stronger operator validation |
| Tier 3 | Accuracy-sensitive/self-hosted proxy | First-party proxied analytics | Reverse proxy, CSP, and blocker mitigation guidance |

Incident triage for analytics-related issues:

1. Check whether `analytics.enabled` is actually `true` for the affected tenant.
2. Check resolved `analytics.provider` and `analytics.endpoint_url` values.
3. Check browser blocking conditions first for client-side analytics: CSP, ad blockers, reverse proxy pathing.
4. Confirm failures are isolated to analytics logging and not leaking into user-facing requests.

Transport mode quick guide:

| Mode | Browser behavior | Typical operator use | Requirements |
|---|---|---|---|
| `direct` | Loads provider script from vendor/provider host | Fastest setup, cloud-hosted or permissive CSP | Public API/site key, provider host allowed by CSP/network |
| `proxy` | Loads provider script and ingest through a first-party reverse proxy | Self-hosted deployments that want better blocker resistance and simpler CSP | Reverse proxy for script + ingest paths, forwarded host/proto headers, stable first-party path |
| `relay` | No vendor script in browser; client posts pageview/custom events to `/api/a/t` | Strict CSP, privacy-sensitive, or heavily blocked environments | API reachable from browser, provider configured server-side, no public API key required |

Reverse proxy and CSP notes:

- Avoid blocker-friendly path names such as `/analytics`, `/tracking`, or `/stats` when fronting third-party providers; use opaque first-party paths instead.
- Preserve `Host`, `X-Forwarded-Proto`, and `X-Forwarded-For` correctly so provider proxies and relay rate limiting see the intended origin/protocol.
- Keep proxied analytics endpoints on HTTPS; mixed-content browser failures look like random analytics drops.
- `direct` and `proxy` modes need CSP allowances for the chosen script/connect sources; `relay` mode can keep CSP tighter because the browser only talks to the application origin.
- The relay endpoint has its own `AnalyticsRelay` fixed-window rate limit in addition to normal API protections.

Bootstrap and failure behavior:

- Disabled analytics or provider `none` results in a clean no-op bridge.
- `relay` mode initializes even when `analytics.api_key` is empty.
- Script load failures in `direct` or `proxy` mode degrade to a no-op adapter rather than breaking the page.
- Relay/browser failures must be treated as observational loss only; user-facing navigation and commands continue normally.

Client vs server event responsibilities:

- Browser-side analytics is for pageviews and low-risk interaction telemetry only.
- Server-side analytics is for business events that must come from authoritative handlers or workflows.
- Do not emit the same business action from both client and server unless the event is deliberately modeled as correlated-but-distinct.
- Treat browser pageviews as navigation context and server events as domain facts.

Provider-capability caution:

- Do not promise semantic parity across providers just because the interface shape is shared.
- `PostHog` is the only currently validated provider in this repo that supports rich product-analytics behavior plus feature flags.
- `Plausible` should be positioned as the privacy-friendly lightweight tier.
- `Rybbit` should stay in a validated-with-caution tier until official docs justify server-side parity beyond browser events.
- `RudderStack` should be documented as an advanced event pipeline option, especially for operators who want to forward data onward, not as a direct substitute for PostHog dashboards/flags.

Analytics rollout and incident runbook:

1. **Enable / disable**
   - Use `analytics.enabled = false` or provider `none` for a clean global or tenant-level kill switch.
   - For urgent incident response, short-circuit the proxy/relay path to `204` rather than redeploying clients.
2. **Provider switch**
   - Validate the target provider in a staging tenant first.
   - Keep event names and property semantics stable across the switch; do not promise unsupported provider parity.
   - Prefer a short dual-run window only when operators explicitly need migration confidence.
3. **Proxy / relay verification**
   - Confirm the reverse proxy preserves `Host`, `X-Forwarded-Proto`, and `X-Forwarded-For`.
   - Verify CSP permits the required `script-src` / `connect-src` entries for `direct` or `proxy` mode.
   - Verify `relay` mode can reach `/api/a/t` from the browser without a public API key.
4. **Blocked or missing data**
   - Check CSP reports, browser network failures, and ad-blocker interference before suspecting provider code.
   - If traffic is heavily blocked, move to `proxy` or `relay` mode before expanding provider-specific debugging.
5. **Deferred reliability work**
   - Buffered/outbox analytics delivery is intentionally deferred to a follow-up milestone.
   - Current guidance is best-effort delivery for browser analytics and handler-driven best-effort server events; introduce an outbox only if operators need stronger guarantees for business-critical analytics.

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

## Partitioning Strategy — Planned

**Status:** Not yet implemented. Strategy documented for post-v1.0 when table sizes warrant it.

**Candidate tables (high-growth, multi-tenant):**

| Table | Growth Pattern | Partitioning Strategy |
|---|---|---|
| `events` | Per tenant, unbounded | Hash by `tenant_id` (16-64 partitions) |
| `event_sessions` | Per event, high fan-out | Hash by `tenant_id` |
| `event_registrations` | Per session × users | Hash by `tenant_id` |
| `pds_sync_outbox` | Transactional, time-ordered | Range by `created_at` (monthly) |
| `audit_logs` | Append-only, all writes | Range by `created_at` (monthly) |
| `notifications` | Per user, high volume | Hash by `tenant_id` |
| `configuration_change_logs` | Append-only audit | Range by `timestamp` (monthly) |

**PostgreSQL declarative partitioning approach:**
1. **Tenant-scoped tables** → Hash partition by `tenant_id`. This distributes tenants across partitions and allows partition pruning when `tenant_id` is in the WHERE clause (it always is due to query filters).
2. **Time-series tables** (outbox, audit, change logs) → Range partition by creation timestamp with monthly boundaries. Old partitions can be detached and archived without affecting active data.
3. **Hybrid** (notifications) → Could use composite partitioning (hash by tenant, then range by date) if volume justifies complexity.

**Prerequisites before implementing:**
- Table sizes must exceed ~10M rows to justify partitioning overhead.
- All queries on partitioned tables must include the partition key in WHERE clause (tenant_id or created_at). Current EF query filters already ensure this for tenant_id.
- Unique indexes must include the partition key. Current unique constraints already include tenant_id where applicable.
- Foreign keys referencing partitioned tables have limitations in PostgreSQL — plan migration carefully.

**Estimated trigger point:** When any single table exceeds 50M rows or query latency degrades despite proper indexing.
