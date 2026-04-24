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
| `global` | Token bucket | API key ID when present, otherwise remote IP | 200 tokens, replenish 40/10s. Localhost exempt |
| `authenticated` | Sliding window | API key ID when present, otherwise `User.Identity.Name` | 200 requests/60s, 4 segments |
| `write` | Fixed window | API key ID when present, otherwise `User.Identity.Name` | 30 requests/60s |
| `setup_secret` | Fixed window | IP | 5 requests/60s |
| `AnalyticsRelay` | Fixed window | IP | 120 requests/60s |

**Rejection**: `429 Too Many Requests` with RFC 6585 ProblemDetails, `Retry-After` when available, plus `X-RateLimit-Limit` and `X-RateLimit-Remaining` headers.

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

### Forwarded Header Trust

- `Explore.API` uses ASP.NET Core forwarded-header middleware with explicit `ForwardedHeadersTrust` configuration.
- Operators must configure trusted reverse-proxy IPs or CIDR networks before relying on `X-Forwarded-Host` for custom-domain or subdomain tenant resolution.
- Without trusted proxy configuration, forwarded host/IP headers are ignored by the API host.

### Security Headers

Added to every response by `SecurityHeadersMiddleware`:
- `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()`
- `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`
- Non-GET responses additionally receive `Cache-Control: no-store` and `Pragma: no-cache`.

### Business Metrics (OpenTelemetry)

Meter `Explore.Business` exposes counters tagged with `tenant_id` and `resource_type`:
- `events.created`, `events.published`, `registrations.created`, `organizations.created`, `authorization.decisions`

## Cerbos PDP Operations

### Storage Topology

Cerbos uses overlay storage (`.cerbos.yaml`):

| Store | Purpose | Notes |
|---|---|---|
| PostgreSQL (primary) | Dynamic policies from `PolicySyncService` | Admin API writes here |
| Disk (fallback) | Static resource policies + derived roles | `cerbos/policies/*.yaml` |

Admin API: basic auth, port 3592. Compile cache: 60s. Audit retention: 7 days.

### Policy Sync

`PolicySyncService` generates Cerbos derived role policies from `Role`/`RolePermission` tables:

| Operation | Trigger | Scope |
|---|---|---|
| `SyncRolePoliciesAsync(roleId)` | Custom role create/update/delete | Single role |
| `SyncAllPoliciesAsync()` | Admin-triggered full resync | All roles as bundle |
| `ReloadAllInstancesAsync()` | After any policy push | All PDP instances |

Push flow: read roles → build typed policy documents → push to primary endpoint → broadcast reload → invalidate admin cache.

Resilience: push and reload failures are logged but never fail the calling command.

### Admin API Configuration (`Cerbos:AdminApi`)

| Key | Type | Description |
|---|---|---|
| `Endpoints` | `List<string>` | All PDP instance URLs for reload broadcast |
| `AdminUsername` | `string` | Basic Auth username |
| `AdminPassword` | `string` | Basic Auth password |

### Monitoring

| Log Message | Severity | Meaning |
|---|---|---|
| `Starting full policy sync to Cerbos` | Info | Full resync started |
| `Synced policies for role {RoleId}` | Info | Single role sync succeeded |
| `Failed to sync policies for role {RoleId}` | Error | Push failure (policies may be stale) |
| `Reload broadcast: {Succeeded} succeeded, {Failed} failed` | Warning | Partial reload failure |
| `BYO Cerbos PDP unreachable` | Warning | Tenant's custom PDP failed |
| `Cerbos batch request: {CheckCount} checks` | Debug | Authorization batch sent |

### Incident Triage (Cerbos)

1. Check PDP health: `GET {endpoint}/health` on each Cerbos instance.
2. Verify `Cerbos:AdminApi:Endpoints` matches running instances.
3. Check PostgreSQL store connectivity in Cerbos logs.
4. For stale policies: trigger `SyncAllPoliciesAsync` or re-save the affected custom role.
5. For BYO failures: check tenant's `failure_mode` and look for `SafeMode` activation in logs.

## Setup Secret Lifecycle

Instance bootstrap uses `ISetupSecretProvider`:

- if setup mode is active and no env secret exists, API auto-generates a setup secret and logs it at startup;
- onboarding endpoints in BFF (`/bff/setup-secret*`) validate and synchronize secret state.

## External API Key Operations

External API keys are long-lived credentials for non-interactive callers. Operational guarantees differ from interactive JWT flows in three places: rate-limit partitioning, quota enforcement, and usage-metadata freshness.

### Rate-Limit Partitioning

- When an API-key principal is present on `HttpContext.User`, the `global` and `authenticated` rate-limit policies partition on `api-key:{keyId}` instead of remote IP or user ID.
- Partitioning guarantees that one key's burst does not starve other keys sharing the same egress IP.
- Per-key limits use the same token-bucket configuration as `authenticated` (200 requests / 60s sliding). Per-key write limits match `write` (30 requests / 60s fixed).
- Anonymous and JWT callers retain their existing partition keys (IP and user ID respectively); no behavior change for those paths.

### Clustered Deployment Semantics

Three persistence tiers behave differently in multi-node deployments:

| Tier | Storage | Cluster Behavior | Mitigation |
|---|---|---|---|
| Rate limits | In-process `PartitionedRateLimiter` | **Node-local** — N nodes give N× the advertised limit | Deploy a Redis-backed limiter or an ingress-tier limit when strict global enforcement is required |
| Quota credits | PostgreSQL `ExternalApiKeyQuota` table | **Cluster-safe** — atomic `INSERT ... ON CONFLICT` + `UPDATE ... WHERE credits_used + amount <= limit` with row-level lock | No action required |
| Usage metadata | In-memory write-through to `LastUsedAt` / `LastUsedIp` | **Eventually consistent** — 5-minute in-memory throttle per key; races between nodes are acceptable | No action required; metadata is informational, not security-critical |

### Revocation Semantics

- Revocation sets `ExternalApiKeyStatusId` to `Revoked`. The change takes effect on the **next** authentication attempt — in-flight requests already past the auth handler complete normally.
- Cache invalidation for the key row is immediate (HybridCache `RemoveAsync`); no stale reads beyond the auth handler's first lookup.
- Revoked keys emit `external_api_key.revoked` business metrics tagged with `tenant_id` and `owner_type`.

### Usage Reporting

- Tenant admins call `GET /api/ExternalApiKey/usage-report?from=&to=` and receive a report scoped to their tenant.
- Instance admins call the same endpoint and receive a platform-wide report — optionally narrowed via `tenantId` query parameter.
- Reports are aggregated from request counts + last-used timestamps; no raw request logs are surfaced (privacy boundary).

### Observability

Six business metric counters (all tagged with `tenant_id`, `owner_type`):

- `external_api_key.created`
- `external_api_key.revoked`
- `external_api_key.policy_updated`
- `external_api_key.rotated`
- `external_api_key.authentication_attempts` (+ `outcome` tag: `success` / `invalid` / `inactive` / `expired` / `tenant_mismatch` / `empty_header`)
- `external_api_key.throttled`

Structured logs on the auth handler include `key_id` and outcome only — never the secret segment.

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

### Cookie Consent & Privacy Governance

The platform provides a consent framework; each instance operator is the data controller responsible for their own legal compliance.

Global kill switch:

- `analytics.global_disable_client_tracking = true` immediately disables all browser-side analytics across all tenants.
- Use this for urgent legal/privacy incidents without editing individual tenant settings.
- Server-side analytics continues normally; this only affects browser SDK initialization.

Provider consent matrix:

| Provider | Cookieless Mode | Banner Required | Decline Behavior |
|---|---|---|---|
| `none` | N/A | No | N/A |
| `plausible` | Always cookieless | No | N/A |
| `rybbit` | Always cookieless | No | N/A |
| `posthog` (`cookieless_mode=always`) | Always cookieless | No | N/A |
| `posthog` (`cookieless_mode=on_reject`) | Cookieless after decline | Yes | Configurable: `disable` or `cookieless` (default: `cookieless`) |
| `posthog` (`cookieless_mode=off`) | Consent-managed | Yes | Disable (no cookieless fallback) |
| `rudderstack` | Full consent (v1) | Yes | Disable (full consent required) |

PostHog privacy defaults for self-hosters:

- `posthog_session_replay`: disabled by default. Requires explicit consent if enabled.
- `posthog_autocapture`: disabled by default. When enabled, captures click/input interactions.
- `posthog_heatmaps`: disabled by default. Collects pointer position data.
- `posthog_person_profiles`: `identified_only` by default. Set to `never` for fully anonymous analytics.
- Admin UI shows contextual warnings when enabling features that expand the consent surface.

Consent cookie operational notes:

- Consent cookies are tenant-scoped (`explore_cc_{stableShortKey}` where `stableShortKey` is the first 8 hex chars of the tenant's immutable GUID) — never shared across tenants. Cookie scope is per effective public host (`SameSite=Lax`, `Secure`, `path=/`).
- Default lifetime is 180 days (configurable via `analytics.consent_cookie_lifetime_days`).
- Cookie values are minimal (`accepted`/`declined` only) — not identity artifacts.
- The consent cookie is classified as strictly necessary and does not require its own consent.
- A persistent "Cookie Settings" link in the footer allows users to withdraw consent at any time.

Admin settings management:

- `GET /api/InstanceOnboarding/analytics-governance` returns current governance settings plus computed advisory fields.
- `PUT /api/InstanceOnboarding/analytics-governance` updates all 10 consent/privacy governance keys.
- Auto-computation is advisory: the resolver suggests recommended settings but does not silently overwrite operator choices.
- The resolver returns `ResolveReasons` (e.g., `GlobalKillSwitch`, `ProviderInherentlyCookieless`, `PosthogCookielessOnReject`, `CookieBannerEnabledByOperator`) for diagnostics and admin UX. These are surfaced in the admin settings panel but never exposed in public-facing DTOs.
- Save-time validation rejects illegal combinations (e.g., `Cookieless` decline behavior for providers that don't support cookieless mode, out-of-range cookie lifetime) and returns warnings for suboptimal but allowed configurations (e.g., PostHog features enabled on a non-PostHog provider, session replay with always-cookieless mode).

## Single-Tenant Endpoint Exposure

`BlockInSingleTenantAttribute` behavior:

- in single-tenant mode with hiding enabled, guarded endpoints return `404` (hidden from discovery).

`RequireMultiTenantAttribute` behavior:

- returns `403` with a clear error payload when feature requires multi-tenant mode.

## Localization Operational Runbooks

### Runbook: TMS Provider Is Down
1. Check Grafana `islamu_tms_fallback_activated_total` — if > 0 in 5m, confirm TMS outage.
2. Check container logs for `[LOCALIZATION] TMS ExportTranslations failed` entries.
3. **Immediate mitigation**: Flip `localization.force_offline_mode` to `true` via Admin UI → Localization → Kill-switches → "Save & Apply Kill-switches Now".
4. Investigate TMS provider status (Tolgee dashboard, Weblate status page).
5. When TMS is restored: disable force-offline, verify live translations resume.

### Runbook: Bundle File Lost / Corrupted
1. Navigate to Admin UI → Localization → Offline Bundle Export.
2. Click "Export {LANG}" for each affected language.
3. Verify file appears in `App_Data/Localization/Bundles/{lang}.json`.
4. If export fails, check the health banner — writable path may not be available.

### Runbook: Writable Path Health Banner Red
1. Check deployment topology: single-instance vs multi-replica.
2. Verify `App_Data/Localization/Bundles/` directory exists and has write permissions.
3. For multi-replica without shared storage: this is expected — see `dev/backlog/distributed-bundle-file-writer.md`.
4. For single-instance: check filesystem permissions, disk space.
5. Escalate to SRE if distributed bundle writer is needed.

### Runbook: API Key Rotation
1. Navigate to Admin UI → Localization → TMS Provider section.
2. Click "Rotate" next to the API key status chip.
3. Paste the new API key in the write-only field.
4. Click "Save" to persist.
5. Click "Test Connection" to verify the new key works.

### Localization Metrics & Alerts

| Metric | Alert Threshold | Description |
|--------|----------------|-------------|
| `islamu_tms_fallback_activated_total` | > 0 in 5m | TMS provider failed; page on-call |
| `islamu_translation_fetch_duration_seconds` | p99 > 5s | TMS latency degradation |
| `islamu_translation_fetch_total{result="error"}` | > 10 in 5m | Repeated fetch failures |

## Incident Triage Quick Checks

1. Check `/health` and `/alive`.
2. Check API logs for migration/seeding failures.
3. Check rate-limit/timeouts if clients receive `429` or `504`.
4. Check tenant resolution and deployment mode (`deployment.mode`) if tenant-scoped behavior is wrong.
5. Check setup-secret mode if onboarding is blocked.
6. Check `islamu_tms_fallback_activated_total` if localization is degraded — flip force-offline if needed.

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

---

## Custom Property Projections (Milestone D)

### What is it?

Custom property projections are denormalized, read-optimized rows derived from Layer 3 EAV runtime values. They keep discovery/search/filter query paths out of the raw normalized EAV joins.

**Source of truth:** `event_custom_property_definitions` + `event_custom_property_values` (and session equivalents).

**Projection tables:** `event_custom_property_projections`, `event_session_custom_property_projections`.

**Coordination tables:** `custom_property_projection_status` (rebuild tracking), `custom_property_projection_dirty_scope` (skip-on-contention backlog).

### What can go wrong?

| Symptom | Likely Cause | Recovery |
|---|---|---|
| Search/filter results are stale | Projection rows not updated after value write | Rebuild projection for the tenant |
| Projection rows missing for an event | Event created while rebuild was in progress; inline write skipped | Drain dirty scopes, or rebuild single event |
| Rebuild hangs | Advisory lock contention or long-running transaction | Check `custom_property_projection_status` for `Rebuilding` state; if stuck >10min, investigate PostgreSQL advisory lock waits |
| Dirty-scope backlog growing | Frequent rebuilds causing inline writers to skip | Drain dirty scopes; consider reducing rebuild frequency |
| Governance report shows stale data | Counts based on runtime definitions, not projection | No action needed; governance report reads from definitions, not projections |

### How to inspect

**Projection status:**
```
GET /api/admin/custom-property-projections/status?tenantId={tenantId}
```
Returns: `State` (Idle/Rebuilding/Failed), `LastRebuildStartedAt`, `LastRebuildCompletedAt`, `RowsProcessed`, `RowsFailed`, `LastErrorMessage`.

**Dirty-scope backlog:**
```
GET /api/admin/custom-property-projections/dirty-scopes?tenantId={tenantId}&projectionName=event_custom_property_projection
```
Returns: pending (un-drained) dirty-scope rows with creation timestamps and reasons.

**Projection rows for a specific event:**
```
GET /api/admin/custom-property-projections/events/{eventId}?exposureCeiling=Public
```

**Governance report (Rule 12):**
```
GET /api/admin/custom-property-definitions/governance-report?tenantId={tenantId}&scope=Event
```
Returns: all active Layer 3 definitions with flags, instance counts, and `PromotionRecommendation`.

### How to recover

**Full tenant rebuild (event projections):**
```
POST /api/admin/custom-property-projections/rebuild
Body: { "tenantId": "{tenantId}" }
```
Acquires advisory lock, rebuilds all projection rows, drains pending dirty scopes on completion. If lock is not acquired (another rebuild running), returns immediately with `lockAcquired: false`.

**Single event rebuild:**
```
POST /api/admin/custom-property-projections/rebuild-single-event
Body: { "eventId": "{eventId}" }
```
Refreshes all projection rows for one event inside a transaction.

**Drain dirty scopes without rebuild:**
```
POST /api/admin/custom-property-projections/drain-dirty-scopes
Body: { "tenantId": "{tenantId}", "projectionName": "event_custom_property_projection" }
```
Processes pending dirty-scope rows without triggering a full rebuild. Idempotent — returns `drainedCount: 0` if no pending rows.

**Session equivalents:** Replace `/rebuild` with `/sessions/rebuild`, `/rebuild-single-event` with `/sessions/rebuild-single`, etc.

### Concurrency model

- **Advisory locks:** Rebuild acquires a PostgreSQL advisory lock keyed on `fnv1a(projectionName), fnv1a(tenantId)`. Only one rebuild runs per projection per tenant.
- **Skip-on-contention:** Inline writers (triggered by value/definition changes) attempt the same lock. If contended (rebuild in progress), they upsert a `custom_property_projection_dirty_scope` row instead of blocking.
- **Drain-on-completion:** The rebuild worker drains all pending dirty scopes after completing its scan, so the skip window is bounded.
- **ConcurrencyStamp:** All mutable EAV entities carry an EF Core `ConcurrencyStamp` (`Guid`). `DbUpdateConcurrencyException` is translated to HTTP 409 with `code: concurrent_update`.

### Hard limits

| Setting Key | Default | Platform Max |
|---|---|---|
| `custom_properties.max_definitions_per_tenant_per_entity_scope` | 500 | 5000 |
| `custom_properties.max_definitions_per_event` | 100 | 1000 |
| `custom_properties.max_definitions_per_event_session` | 50 | 500 |
| `custom_properties.max_options_per_definition` | 200 | 2000 |
| `custom_properties.max_multi_value_rows_per_value` | 20 | 200 |
| `custom_properties.projection_rebuild_batch_size` | 500 | 5000 |
| `custom_properties.max_dirty_scope_pending_per_tenant` | 10000 | 100000 |

### Governance report (Rule 12)

The governance report surfaces Layer 3 custom property definitions that may be candidates for promotion to Layer 2 (typed schema) or Layer 1 (universal core), using the Atlassian 4-question framework:

| Recommendation | Trigger |
|---|---|
| `None` | No search/filter/moderation/analytics flags set |
| `ConsiderProjectionFirst` | `IsSearchable` or `IsFilterable` is true |
| `ConsiderLayer2Promotion` | `IsModerationRelevant` or `IsAnalyticsRelevant` is true |
| `ConsiderLayer1Promotion` | `IsModerationRelevant` AND (`IsSearchable` or `IsFilterable`) AND used by ≥30% of tenant's events |

Review quarterly. Promotion is an operational decision, not an automated action.
