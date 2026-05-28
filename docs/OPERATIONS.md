ABOUTME: Operational runbook for startup, health, shutdown, and runtime safeguards.
ABOUTME: Captures current behavior implemented in API, Blazor BFF, migration service, and service defaults.

# Operations

> **Audience:** Operators | Contributors | AI agents
> **Status:** Mixed
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.AppHost/AppHost.cs`, `Explore.API/Program.cs`, `Explore.ServiceDefaults/`, `docker-compose.yml`, `docs/SELF_HOSTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md`, `docs/TROUBLESHOOTING.md`

This page is the operational reference for implemented runtime behavior. Task procedures should live in dedicated runbooks and be linked from here.

## Operational Runbooks

| Task | Runbook | Use When |
|---|---|---|
| Install or update a self-hosted stack | [SELF_HOSTING.md](SELF_HOSTING.md) | You need Compose topology, ports, setup secret behavior, Keycloak, MinIO, Cerbos, or reverse-proxy boundaries. |
| Back up, restore, upgrade, or roll back | [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) | You are preparing a release, recovering an environment, or testing disaster recovery. |
| Diagnose repeated symptoms | [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | You have a concrete failure such as `401`, `429`, `504`, unhealthy readiness, setup-secret errors, or secret-provider failures. |
| Validate release readiness | [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) | A change affects migrations, configuration, secrets, security, upgrade paths, or operator docs. |

## Read-Only Doctor Diagnostics

`Explore.Diagnostic` includes a read-only doctor CLI for self-hosting and local-environment preflight checks:

```bash
dotnet run --project Explore.Diagnostic/Explore.Diagnostic.csproj -- --root .
```

The doctor prints deterministic `PASS`, `WARN`, and `FAIL` results with remediation links. It exits `0` when all checks are `PASS` or `WARN`, and exits `1` when any check is `FAIL`.

Current checks cover:

- .NET SDK version versus `global.json`;
- Docker and Docker Compose availability;
- Aspire CLI availability;
- Compose service topology and BFF `API_ENDPOINT` alignment;
- discrete PostgreSQL bootstrap variables expected by `BootstrapSecretLoader`;
- presence of operator remediation docs.

Non-negotiable safety boundary: doctor does **not** repair configuration, generate secrets, start containers, start Aspire, run migrations, seed data, call setup write endpoints, or persist setup state. Use it before running Compose/Aspire or when diagnosing a self-hosting setup, then follow the linked remediation docs for corrective action.

Sensitive values are redacted before output. Do not add checks that print raw connection strings, passwords, setup secrets, bearer tokens, cookies, authorization headers, or secret-provider responses.

## Local Startup Topology (Aspire)

`Explore.AppHost/AppHost.cs` starts services in this order:

1. RabbitMQ resource `messaging` for optional local RabbitMQ Dispatch Mode experiments
2. `Event.MigrationService`
3. `Explore.API` (waits for migration completion, Redis, and RabbitMQ in Aspire local development)
4. `Explore.Blazor` (waits for migration completion and API readiness)

The Blazor app resolves the API through Aspire service discovery (`services__explore-api__https__0` / `services__explore-api__http__0`) or `ExploreApi:BaseUrl`. Do not hardcode the Compose/API host port into AppHost documentation.

## API Startup Behavior

On startup (except `Testing` environment), API performs:

1. `db.Database.Migrate()`
2. `DatabaseSeeder.SeedAsync(...)`

If migration fails, startup fails (application does not continue).

## Health and Metrics Endpoints

Via `Explore.ServiceDefaults` + app-specific checks:

- `/health`: readiness (`ready`-tag checks only)
- `/alive`: liveness (`live`-tag checks only)
- `/metrics`: Prometheus scraping endpoint

Status-code contract:

- `Healthy` / `Degraded` -> `200`
- `Unhealthy` -> `503`

Readiness interpretation:

| Check | Host | Healthy | Degraded | Unhealthy |
|---|---|---|---|---|
| `shutdown` | API, Blazor | Process is accepting traffic | Not used | Graceful shutdown is active; remove from load balancer |
| `database` | API, Blazor | EF Core can reach PostgreSQL | Not used | Database unavailable or migration/runtime connectivity failed |
| `distributed-cache` | API, Blazor | Effective cache round-trip works | Configured Redis fell back to in-memory cache | Effective cache round-trip failed |
| `oidc-discovery` | API, Blazor | OIDC metadata valid, or OIDC is not configured | Not used | Configured OIDC metadata endpoint is unreachable or invalid |
| `smtp` | API | SMTP connection/auth succeeds | SMTP is not configured | Configured SMTP is unreachable or authentication fails |
| `email-dispatch` | API | Basic Dispatch Mode worker is enabled | Worker is intentionally disabled | Invalid worker options fail startup; RabbitMQ is not checked in Basic mode |
| `email-dispatch-rabbitmq` | API | RabbitMQ Dispatch Mode is disabled, or enabled and topology can be declared | Not used | RabbitMQ mode is enabled but the broker/topology is unreachable or invalid |
| `cerbos` | API | Local provider mode is selected, or configured Cerbos PDP passes gRPC health | Not used | Instance `authorization.provider` is `cerbos` and the PDP is missing or unreachable |
| `islamu-event-api` | Blazor | BFF can reach API readiness endpoint | Not used | API readiness endpoint is unavailable or unhealthy |
| `secret_provider` | API, Blazor | Secret backend path is healthy | Secret backend has transient failures within the configured threshold | Secret backend crossed the unhealthy threshold |

Operational rules:

- Point load balancer readiness checks at `/health` and liveness checks at `/alive`.
- Treat `Degraded` as deployable only when the affected dependency is optional for the deployment mode and the response body clearly identifies the dependency.
- Treat `Unhealthy` as non-deployable for rolling updates; fix the dependency or intentionally switch the related feature/provider off.
- Instance Cerbos readiness follows authorization fail-closed semantics: if the operator selected `authorization.provider=cerbos`, an unreachable PDP makes `/health` unhealthy rather than silently falling back to local RBAC.
- Local authorization mode skips Cerbos readiness, so self-hosted/local deployments do not need a Cerbos PDP unless explicitly selected.
- Basic Email Dispatch Mode skips RabbitMQ readiness entirely. A self-hosted deployment can send registration confirmation email with API + PostgreSQL + configured SMTP only.
- RabbitMQ Dispatch Mode is optional transport infrastructure. When `EmailDispatchRabbitMq:Enabled=false`, the `email-dispatch-rabbitmq` check is healthy without opening a broker connection. When enabled, missing broker connectivity or failed topology declaration is unhealthy because the operator explicitly selected RabbitMQ transport.

## Deployment Protection and Evidence

GitHub Actions deploys use the `staging` and `production` environments. Configure environment rules in GitHub repository settings, not in application runtime configuration:

- `production` should require reviewer approval and restrict deployments to `main` and version tags.
- `staging` should use environment-scoped secrets and can deploy automatically from `develop` unless the release process requires review.
- Store Coolify webhook URLs and bearer tokens as environment secrets. Do not print webhook URLs or tokens in workflow logs.

See [CI_CD_GOVERNANCE.md](CI_CD_GOVERNANCE.md) for the required/advisory gate matrix, branch-protection settings, and artifact retention policy.

Deploy jobs call the existing Coolify webhook contract with timeout, retry, redacted error output, transport-failure summaries, and explicit HTTP status validation. The job writes a deployment summary and uploads deployment evidence for 90 days. If environment URL variables are configured, the workflow smoke-checks `/alive` and `/health` for the deployed API and UI with a bounded retry budget before reporting success.

### Deployment Image Source of Truth

Container builds publish mutable convenience tags (`latest` for production and `develop` for staging), immutable commit-SHA tags, digest evidence, SBOM/provenance attestations, and image scan artifacts.

Production must not rely on `latest` as the source of truth once deployment promotion is complete.

Decision path:

1. Preferred: configure Coolify to deploy explicit image digests when the current Coolify application/webhook model supports `image@sha256:...`.
2. Fallback: configure Coolify to deploy immutable commit-SHA tags (`sha-*` for production and `dev-*` for staging) and record the resolved digest in deployment evidence.
3. Temporary risk: mutable tags remain convenience aliases while Coolify capability is being confirmed.

Do not remove digest/SBOM/provenance evidence even if Coolify temporarily consumes immutable tags rather than digests.

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
- Uses `HttpContext.TraceIdentifier` when both request headers are absent.
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

Meter `Explore.Business` exposes source-defined business counters. Counter names and tags are not uniform across all events; check the metric-specific tags before building dashboards.

Current counters include:

- `explore.events.created` (`tenant_id`, `event_type`)
- `explore.events.published` (`tenant_id`)
- `explore.registrations.created` (`tenant_id`)
- `explore.organizations.created` (`tenant_id`)
- `explore.authorization.decisions` (`resource`, `action`, `result`)
- `event_role_assignment.changed` (`operation`, `outcome`, `role`)
- `explore.email_dispatch.attempts` (`tenant_id`, `outcome`, `failure_category`) — Basic Dispatch Mode email outcomes; labels intentionally exclude recipient, subject, body, provider message ID, and raw error text.
- `explore.email_dispatch.rabbitmq.publishes` (`tenant_id`, `outcome`, `failure_category`) — optional RabbitMQ pointer-publish outcomes; labels intentionally exclude recipient, subject, body, provider message ID, raw broker error text, and connection strings.

### Basic Email Dispatch Operations

Registration confirmation email is handled as a durable side effect:

1. The registration command creates an `EmailDispatchOutbox` row in the same PostgreSQL transaction as the registration state.
2. `EmailDispatchProcessor` polls due rows, checks tenant pause state, atomically claims one row, and sets tenant context before resolving SMTP settings.
3. SMTP is called through `IEmailService`; handlers and controllers do not send SMTP or publish RabbitMQ directly.
4. The worker records `EmailDispatchAttempt` and `EmailDispatchReceipt` state, then marks the outbox row `Sent`, `RetryScheduled`, `DeadLettered`, or `Unknown`.

Operator signals:

| Signal | Meaning |
|---|---|
| `email-dispatch` health check | Worker enabled/disabled state and safe worker settings. |
| `explore.email_dispatch.attempts` | Outcome counter for sent, tenant paused, unknown, retry-scheduled, and dead-lettered attempts. |
| Structured worker logs | Include dispatch/outbox IDs, tenant IDs, outcomes, retry delay, and normalized failure category; do not include bodies, recipients, subjects, secrets, provider message IDs, or raw SMTP error text. |

Timeout-like SMTP outcomes are recorded as `Unknown` instead of blind retry. Dead-lettered rows remain in PostgreSQL for operator inspection and later replay tooling.

### Optional RabbitMQ Dispatch Operations

RabbitMQ Dispatch Mode is an optional transport foundation over the same PostgreSQL-owned `EmailDispatchOutbox` state machine. The current implemented slice declares RabbitMQ topology, publishes pointer-only `EmailDispatchPointer` messages with mandatory routing and publisher confirmations, exposes `email-dispatch-rabbitmq` readiness, and wires the local Aspire `messaging` resource. It does **not** replace the Basic SMTP worker and does not yet include the manual-ack consumer or DLQ replay/parking operator flow.

Operator signals:

| Signal | Meaning |
|---|---|
| `email-dispatch-rabbitmq` health check | Disabled mode is healthy and independent; enabled mode proves broker connectivity and topology declaration. |
| `explore.email_dispatch.rabbitmq.publishes` | Outcome counter for disabled, confirmed, returned, nacked, failed, and timeout publish attempts. |
| Structured RabbitMQ transport logs | Include dispatch IDs, tenant IDs, topology names, outcomes, and normalized failure categories; do not include recipient addresses, subjects, bodies, provider message IDs, raw broker errors, or AMQP connection strings. |

The RabbitMQ payload is a pointer contract only: tenant ID, stable `PublishEventId`, dispatch kind, source IDs, and optional event/registration/user IDs. Email body, subject, recipient, reply-to, SMTP settings, provider message IDs, and raw provider errors remain out of broker payloads and logs.

## Cerbos PDP Operations

### Storage And Package Topology

Static policies, schemas, and derived roles live in repo-root `cerbos/policies/`; native policy tests live in `cerbos/tests/`. The application publishes the bundled package through `IPolicyPackageService` instead of generating ad-hoc role policies at runtime.

`Cerbos:PolicyPackagePath` (environment variable `CERBOS__POLICYPACKAGEPATH`) points the API at the policy package directory. Container deployments should either bundle `cerbos/` into the API image or mount the policy folder read-only, for example `./cerbos/policies:/app/cerbos/policies:ro` with `CERBOS__POLICYPACKAGEPATH=/app/cerbos/policies`. Aspire local development sets `Cerbos__PolicyPackagePath` for `explore-api` to repo-root `cerbos/policies`; direct `dotnet run` also falls back from the default relative `cerbos/policies` path to repo-root policies when launched from a project subdirectory. If the folder is missing, download endpoints return safe `503 ProblemDetails` without host paths.

Package delivery paths:

| Path | Trigger | Notes |
|---|---|---|
| Docker Compose one-shot sync | `docker compose --profile authz run --rm cerbos-policy-sync` | Recommended self-hosting path. Starts the `authz` profile with `cerbos-db`, uses server-side `CERBOS_ADMIN_USER` / `CERBOS_ADMIN_PASSWORD`, recursively uploads policies and `_schemas`, then requests store reload. Set `CERBOS_ADMIN_PASSWORD_HASH` to the hash matching `CERBOS_ADMIN_PASSWORD` before using Admin API sync. |
| Zero-touch boot sync | API startup when complete instance Admin API config exists | Skips safely when endpoint or credentials are incomplete. |
| Setup/Admin UI sync | Operator-triggered setup or admin action | Advanced path shown only when server-side Admin API credentials are already configured; the browser does not collect Cerbos Admin API passwords. Returns safe issue codes for missing config, auth failure, unavailable/rejected package, reload failure, or unknown package status. |
| Manual ZIP fallback | Setup/Admin download endpoint | Always visible in onboarding, including Local RBAC mode. Exports the same bundled package for `cerbosctl put policy --recursive .` and `cerbosctl put schema --recursive _schemas` when Admin API sync is unavailable or intentionally disabled. |

Runtime authorization checks use the PDP gRPC endpoint. Package sync/status uses the Admin API endpoint and credentials. Do not treat a healthy Admin API as proof that runtime PDP checks are healthy, or vice versa.

### Admin API Configuration (`Cerbos:AdminApi` And BYO Admin API)

| Key | Type | Description |
|---|---|---|
| `Endpoint` / `Endpoints` | `string` / `List<string>` | Instance Admin API target(s) for package upload/status/reload. |
| `AdminUsername` | `string` | Basic Auth username; secret-bearing and redacted from reads/logs. |
| `AdminPassword` | `string` | Basic Auth password; secret-bearing and redacted from reads/logs. Docker Compose uses `CERBOS_ADMIN_PASSWORD` for `cerbosctl` and `CERBOS_ADMIN_PASSWORD_HASH` for the Cerbos server config. |
| `Cerbos:PolicyPackagePath` | `string` | API-local path to bundled or mounted `cerbos/policies`; use `CERBOS__POLICYPACKAGEPATH=/app/cerbos/policies` in containers. |
| BYO custom Admin API endpoint/credentials | tenant governance/secret settings | Optional per-tenant package target, preserved even when the tenant custom PDP endpoint is blank. |

Non-local Admin API/PDP endpoints must use safe TLS-capable URLs. Unsafe endpoint changes are rejected before provider settings are persisted. Runtime failure logs must not include raw endpoints, credentials, JWTs/tokens, response bodies, or exception objects/messages.

### Monitoring

| Signal | Meaning |
|---|---|
| Cerbos readiness health check | Follows fail-closed semantics when instance Cerbos mode is active; local mode skips PDP readiness. |
| Package status issue code | Distinguishes Admin API not configured, auth failure, Admin API unavailable/rejected package, reload failure, generic publish failure, and Cerbos package-status unknown. |
| BYO closed safe-mode log | Tenant BYO failure activated provider-instance fallback safe mode; non-instance-admin decisions deny. |
| BYO open fallback log | Tenant explicitly chose local RBAC fallback for BYO PDP failure. |
| Runtime failure type metadata | Safe diagnostic context; no raw endpoints, credentials, JWTs, response bodies, or exception messages. |

### Incident Triage (Cerbos)

1. Check the runtime PDP health and the app Cerbos readiness endpoint.
2. Verify instance `Cerbos:GrpcEndpoint` for runtime checks and `Cerbos:AdminApi:*` for package operations.
3. For BYO tenants, verify `cerbos.mode`, `cerbos.custom_endpoint`, `cerbos.failure_mode`, and optional custom Admin API endpoint/credentials.
4. If `cerbos.mode=custom_endpoint` has a blank PDP endpoint, runtime authorization still follows BYO failure mode; configure the PDP endpoint or temporarily choose local/open behavior only as an explicit operator decision.
5. For package sync failures, inspect the safe issue code before retrying. Prefer `docker compose --profile authz run --rm cerbos-policy-sync` for self-hosted Compose deployments after confirming `CERBOS_ADMIN_PASSWORD_HASH` matches `CERBOS_ADMIN_PASSWORD`, or use setup/admin manual ZIP download plus `cerbosctl put policy --recursive .` and `cerbosctl put schema --recursive _schemas` when Admin API sync is unavailable.
6. For missing HAL affordances, confirm the link was not denied by server-side authorization before debugging route generation.

## Setup Secret Lifecycle

Instance bootstrap uses `ISetupSecretProvider`:

- if setup mode is active and no env secret exists, API auto-generates a setup secret and logs it at startup;
- onboarding endpoints in BFF (`/bff/setup-secret*`) validate and synchronize secret state;
- setup status returns client-safe state labels (`Environment`, `Generated`, `Expired`, `Locked`, `Unavailable`) and operator guidance without exposing raw secrets;
- generated setup secrets expire 60 minutes after API startup, and recovery is to restart the API and use the newly logged generated secret;
- environment-provided setup secrets remain authoritative, but a timed-out setup window still requires an API restart to reopen setup mode.

The convention-first launch path is Setup Secret → Admin Auth → Site Profile → Preflight → Launch. Preflight readiness data is non-sensitive and separates launch blockers from operational warnings.

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
- Revoked keys emit `explore.external_api_keys.revoked` business metrics tagged with `tenant_id` and `owner_type`.

### Usage Reporting

- Tenant admins call `GET /api/ExternalApiKey/usage-report?from=&to=` and receive a report scoped to their tenant.
- Instance admins call the same endpoint and receive a platform-wide report — optionally narrowed via `tenantId` query parameter.
- Reports are aggregated from request counts + last-used timestamps; no raw request logs are surfaced (privacy boundary).

### Observability

External API-key business counters use the `explore.external_api_keys.*` prefix. Most lifecycle counters include `tenant_id` and `owner_type`; authentication attempts add `outcome`, and throttle events add `policy`.

- `explore.external_api_keys.created`
- `explore.external_api_keys.revoked`
- `explore.external_api_keys.policy_updated`
- `explore.external_api_keys.rotated` (metric exists for future/overlap workflows; do not infer a public rotate endpoint from this metric alone)
- `explore.external_api_keys.authentication_attempts` (+ `outcome` tag: `success` / `invalid` / `inactive` / `expired` / `tenant_mismatch` / `empty_header`)
- `explore.external_api_keys.throttled` (+ `policy` tag)

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

### Runbook: TMS Credential Replacement
1. Open the Admin UI localization provider settings for the affected instance or tenant.
2. Replace the TMS API credential through the configured settings or secret-provider path.
3. Save the settings and use the provider test action, if available, to verify the new credential.
4. If the credential is managed by an external secret provider, rotate it in that provider first and then refresh or restart the application according to the secret-provider runbook.
5. Keep the old credential active only for the minimum overlap window required by the external TMS provider.

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

## AI Agent Operational Context

AI-agent workflow rules are not runtime operations. Keep them in [../AGENTS.md](../AGENTS.md), [../AGENTS.md](../AGENTS.md), and [../dev/active/README.md](../dev/active/README.md) so operators do not have to scan agent tooling while diagnosing production behavior.


## Planned Capacity Work

Partitioning is not implemented. Treat partitioning notes as future capacity planning only; do not document partitioned-table behavior as a current operator contract. Revisit this when tenant-scoped or append-only tables approach sizes where normal indexing and query-filter pruning no longer meet SLOs.

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
Returns: `State` (Idle/Rebuilding/Failed), `LastRebuildStartedAt`, `LastRebuildCompletedAt`, `RowsProcessed`, `RowsFailed`, `LastErrorMessage`, `PendingDirtyScopeCount`, `OperationalState`, `RequiresOperatorAction`, and `RecommendedAction`.

`OperationalState` is intentionally bounded for dashboarding:

| State | Meaning | First action |
|---|---|---|
| `healthy` | Projection is idle and no dirty-scope backlog is pending | No action |
| `dirty_backlog_pending` | Inline writers skipped during rebuild contention and queued dirty scopes | Drain dirty scopes or run a tenant rebuild |
| `rebuilding` | Rebuild is currently active and not yet stale | Monitor until completion |
| `rebuild_stale` | Rebuild has been active for more than 10 minutes | Investigate PostgreSQL advisory-lock waits and worker health |
| `failed` | Last rebuild failed | Inspect `LastErrorMessage`, fix the root cause, then rebuild |

**Dirty-scope backlog:**
```
GET /api/admin/custom-property-projections/dirty-scopes?tenantId={tenantId}&projectionName=event_custom_property_projection
```
Returns: pending (un-drained) dirty-scope rows with creation timestamps and reasons.

**Projection rows for a specific event:**
```
GET /api/admin/custom-property-projections/events/{eventId}?exposureCeiling=Public
```
Use `exposureCeiling` when inspecting rows for public/export/moderation analysis. Public callers and generated-client consumers must not read raw projection rows without a ceiling.

**Projection rows for a specific event session:**
```
GET /api/admin/custom-property-projections/sessions/{eventSessionId}?exposureCeiling=Public
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
- **Quota errors:** Business quota breaches return HTTP 422 with `code: quota_exceeded`, `quotaKey`, `limit`, `scope`, and optional `actual`/`attempted` fields.
- **Admin purge:** Hard purge is separate from normal delete. It is admin-only, writes an audit summary, and is blocked when historical values, projection rows, audit references, or sync provenance exist.

### Metrics

Use these meters for custom-property projection and lifecycle dashboards:

| Meter | Metric | Safe dimensions |
|---|---|---|
| `Explore.Projections` | `explore.projections.rebuild_total` | `tenant_id`, `projection_type`, `lock_acquired` |
| `Explore.Projections` | `explore.projections.rebuild_failures_total` | `tenant_id`, `projection_type`, `lock_acquired` |
| `Explore.Projections` | `explore.projections.rebuild_duration_seconds` | `tenant_id`, `projection_type`, `lock_acquired` |
| `Explore.Projections` | `explore.projections.drain_total` | `tenant_id`, `projection_type` |
| `Explore.Projections` | `explore.projections.drained_scopes_total` | `tenant_id`, `projection_type` |
| `Explore.Projections` | `explore.projections.dirty_scope_skips_total` | `tenant_id`, `projection_type`, `operation`, `reason` |
| `Explore.Projections` | `explore.projections.quota_exceeded_total` | `tenant_id`, `projection_type`, `quota_key`, `scope` |
| `Explore.Business` | `explore.custom_properties.purge_decisions` | `tenant_id`, `scope`, `outcome`, `blocker_category` |

Do not add raw custom-property `Namespace`, `Key`, display names, event IDs, session IDs, or purge reasons as metric dimensions. Those values are high-cardinality and may expose tenant-specific semantics. Use admin API responses for targeted inspection instead.

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
