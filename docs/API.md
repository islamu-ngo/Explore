ABOUTME: Comprehensive API architecture reference covering middleware pipeline, rate limiting, caching, HATEOAS, specification pattern, and all runtime behavior.
ABOUTME: Authoritative source for Explore.API patterns — middleware order, request protection, content negotiation, filtering, and error handling.

# API Architecture

## Scope
This document describes the full API behavior in `Explore.API`: the middleware pipeline, rate limiting, request timeouts, caching strategy, HATEOAS implementation, specification pattern, error handling, content negotiation, and client-generation flow.

## Runtime Endpoints
### Development
- API: `https://localhost:7039`
- Swagger UI: `https://localhost:7039/swagger`
- Scalar: `https://localhost:7039/scalar/v1`
- OpenAPI document: `https://localhost:7039/openapi/event-api.json`

### Docker Compose
- API: `http://localhost:7039`

---

## Middleware Pipeline (Exact Order)

The middleware pipeline in `Program.cs` is ordered precisely. Changing order will break behavior:

1. **API Exception Handling** — `UseApiExceptionHandling()`. ProblemDetails-based chained `IExceptionHandler` (Validation → Global).
2. **Forwarded Headers** — `UseForwardedHeaders()`. Applies trusted `X-Forwarded-*` values before host-derived tenant resolution.
3. **Security Headers** — `UseSecurityHeaders()`. Adds defensive headers to every response.
4. **Correlation ID** — `UseCorrelationId()`. Reads `X-Correlation-ID` or `X-Request-ID`, generates UUID if absent, pushes to Serilog `LogContext`.
5. **Request Logging** — `UseRequestLogging()`. Structured Serilog logging: method, path, status, duration, userId, tenantId, correlationId.
6. **Response Compression** — `UseResponseCompression()`. Brotli + Gzip at `CompressionLevel.Fastest`. Enabled for HTTPS. Additional MIME types: `application/json`, `application/hal+json`.
7. **HTTPS Redirection** — `UseHttpsRedirection()`.
8. **HATEOAS Prefer Header** — `UseHateoas()`. RFC 7240 `Prefer` header processing (`return=minimal` strips `_links`).
9. **Routing** — `UseRouting()`.
10. **Tenant Resolution (pre-auth)** — `UseMiddleware<ApiTenantResolutionMiddleware>()`. Resolves `X-Tenant-Slug` and normalized host hints for `/api` requests; API-key requests may defer binding until after authentication.
11. **Request Timeouts** — `UseRequestTimeouts()`. Three configurable tiers (see below).
12. **Auth Conflict Guard** — `UseMiddleware<ApiAuthenticationConflictMiddleware>()`. Rejects conflicting auth inputs before standard authentication runs.
13. **Authentication** — `UseAuthentication()`. JWT Bearer via Keycloak.
14. **Tenant Resolution (post-auth)** — `UseMiddleware<ApiTenantPostAuthenticationMiddleware>()`. Finalizes API-key tenant binding, mismatch handling, and fail-closed auth behavior.
15. **Request Localization** — `UseRequestLocalization()`.
16. **Idempotency** — `UseMiddleware<IdempotencyMiddleware>()`. Implements `Idempotency-Key` header for write operations (POST/PUT/PATCH/DELETE). Caches responses by (Key, TenantId) and replays on duplicate requests within 24-hour window.
17. **Rate Limiter** — `UseRateLimiter()`. Five tiered policies (see below).
18. **Authorization** — `UseAuthorization()`.
19. **Output Cache** — `UseOutputCache()`. Five cache policies (see below).
20. **ETag** — `UseETag()`. SHA256-based weak ETags, 304 Not Modified support.

---

## API Versioning

Dual-strategy versioning — both strategies work simultaneously:

1. **Media-type strategy**: `Accept: application/json;v=0.1` or `application/hal+json;v=0.1`.
2. **URL segment strategy**: `/api/v0.1/controller`. `VersionedRouteConvention` auto-adds URL routes to all controllers.
3. Default API version is `0.1` when unspecified.
4. Version is reported in response headers via `Asp.Versioning` middleware.

## Controller Conventions

1. Controllers are thin: receive request → dispatch MediatR command/query → assemble HATEOAS response → return HTTP result.
2. Business logic belongs in handlers/services, never controllers.
3. Every endpoint has named routes (via `RouteNames` constants) for HATEOAS link generation.
4. Endpoints include `[ProducesResponseType]` and XML doc summaries for OpenAPI quality.

---

## Rate Limiting (5 Tiers)

Configured in `RateLimitingExtensions.cs`. All settings are configurable via `appsettings.json` under `RateLimiting` section.

### Global (IP Token Bucket)
- **Policy**: `global` — applied to all endpoints by default.
- **Mechanism**: Token bucket per API key ID when present, otherwise per remote IP address.
- **Defaults**: 200 tokens, replenish 40 tokens per 10 seconds.
- **IP Resolution**: uses `HttpContext.Connection.RemoteIpAddress`; trusted forwarded-header middleware updates the effective remote/host values earlier in the pipeline.
- **Exemption**: Localhost (`127.0.0.1`, `::1`) is exempt.

### Authenticated (Sliding Window)
- **Policy**: `authenticated` — for authenticated user endpoints.
- **Mechanism**: Sliding window per API key ID when present, otherwise per `User.Identity.Name`.
- **Defaults**: 200 requests per 60-second window, 4 segments.

### Write (Fixed Window)
- **Policy**: `write` — for mutation endpoints (`POST`, `PUT`, `DELETE`).
- **Mechanism**: Fixed window per API key ID when present, otherwise per `User.Identity.Name`.
- **Defaults**: 30 requests per 60-second window.

### SetupSecret (Fixed Window)
- **Policy**: `setup_secret` — for instance bootstrap endpoints.
- **Mechanism**: Fixed window per IP address.
- **Defaults**: 5 requests per 60-second window.

### AnalyticsRelay (Fixed Window)
- **Policy**: `AnalyticsRelay` — for anonymous browser analytics relay traffic.
- **Mechanism**: Fixed window per IP address.
- **Defaults**: 120 requests per 60-second window.

### Rejection Behavior
- Returns `429 Too Many Requests` with RFC 6585 `ProblemDetails`.
- Includes `Retry-After` when available plus `X-RateLimit-Limit` and `X-RateLimit-Remaining`.

### Testing Override
In `Testing` environment, all rate limiters are replaced with `NoLimiter` (disabled).

---

## Request Timeouts (3 Tiers)

Configured in `RequestTimeoutExtensions.cs`. All settings configurable via `RequestTimeouts` section.

| Policy | Default | Use Case |
|---|---|---|
| `Default` | 30 seconds | Standard operations |
| `Lookup` | 10 seconds | Fast lookup queries |
| `Complex` | 60 seconds | Complex queries, exports |

Timeout expiry returns `504 Gateway Timeout`.

---

## Caching (3 Layers)

### Layer 1: Output Cache (HTTP Response Level)
Applied via `[OutputCache(PolicyName = "...")]` on controller endpoints.

| Policy | Duration | Vary By | Use Case |
|---|---|---|---|
| `LookupData` | 1 hour | `X-Tenant-Slug`, `Host` | Lookup tables (event types, languages, etc.) |
| `PublicData` | 1 hour | `X-Tenant-Slug`, `Host` | Anonymous lookup endpoints |
| `ListData` | 30 seconds | `X-Tenant-Slug`, `Host`, `Authorization`, query: `pageNumber`, `pageSize` | Collection listings |
| `DetailData` | 60 seconds | `X-Tenant-Slug`, `Host`, `Authorization`, route: `id` | Single-entity detail views |
| `TenantNav` | 5 minutes | `X-Tenant-Slug`, `Host` | Tenant navigation/config endpoints |

### Layer 2: HybridCache (Application Level — L1 + L2)
Injected into MediatR handlers, not controllers. Provides in-memory L1 + distributed L2 caching with stampede protection.

| Setting | Value |
|---|---|
| Default expiration | 30 minutes |
| Local cache expiration | 5 minutes |
| Max payload size | 10 MB |
| Max key length | 512 characters |

**Read-through pattern** (query handlers): `_cache.GetOrCreateAsync(key, factory, options)`.
**Invalidation** (command handlers): `_cache.RemoveAsync(key)`.

### Layer 3: ETag Middleware
- Computes SHA256-based weak ETags on `application/json` and `application/hal+json` responses.
- Returns `304 Not Modified` when client sends `If-None-Match` header matching current ETag.
- Applied globally after output cache in the pipeline.
- Uses `RecyclableMemoryStream` for efficient memory handling. Bodies larger than 256 KB skip ETag computation.

---

## Security Headers

Added by `SecurityHeadersMiddleware` to every response:

| Header | Value |
|---|---|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` |

Non-GET responses additionally receive:
- `Cache-Control: no-store`
- `Pragma: no-cache`

---

## Auth And Authorization

### JWT Bearer Configuration
- Authority: Keycloak OIDC metadata endpoint.
- Multi-client audience validation: `islamu-event-api`, `islamu-event-blazor`.
- Custom `AudienceValidator`: checks both `aud` claim and `azp` (Keycloak authorized party) claim.
- Clock skew tolerance: 5 minutes.
- Dev mode: accepts self-signed certificates.
- Minimal JWT event logging: `OnAuthenticationFailed` (Warning), `OnChallenge` (Debug). PII-leaking handlers removed.

### Endpoint Auth Pattern
- `GET`: usually `[AllowAnonymous]`
- `POST/PUT/DELETE`: `[Authorize]`
- Privileged operations: role/policy constrained
- User ID extraction fallback order: `sub` → `nameidentifier` → `sid`.

### MediatR Authorization Behavior
`AuthorizationBehavior` in the pipeline checks:
1. `IAuthorizedRequest` interface — commands/queries declare required permissions.
2. `[AuthorizeResource]` attribute — declarative resource-level authorization.
3. `ISecureRequest` — provides dynamic resource context for permission evaluation.

Denied requests throw `AuthorizationException` → mapped to `403 Forbidden` by exception handler.

---

## HAL / HATEOAS Implementation

### Architecture
The HATEOAS system uses a layered architecture:

1. **`ResourceAssemblerBase<TDto, TListDto>`** — Base class for assembling HAL responses. Handles both single-entity and collection assembly with batch authorization evaluation.
2. **`ILinkPolicy<TDto>`** / **`ICollectionLinkPolicy<TDto>`** — Per-entity link definitions. Each entity has a detail link policy and a collection link policy.
3. **`LinkDefinition`** — Rich record type with: `Rel`, `RouteName`, `RouteValues`, `Method`, `Title`, `RequiresAuth`, `RequiredRoles`, `Condition`, `PermissionResourceKind`, `PermissionAction`, and more.
4. **`HateoasAuthorizationEvaluator`** — Batch evaluates link permissions. Static checks (auth, roles, conditions) run first, then remaining links go through `IAuthorizationProvider.IsAllowedBatchAsync()` in a single call. On batch failure, permission-bound links are denied (fail-closed).
5. **`HateoasLinkGenerator`** — Resolves URLs from named routes using ASP.NET `LinkGenerator`.
6. **`RouteNames`** — 100+ named route constants ensuring type-safe link generation.

### Content Negotiation
- Default format: `application/hal+json` with `_links` and `_embedded` sections.
- `Prefer: return=minimal` (RFC 7240) strips all `_links` from responses for lightweight clients.
- `PreferHeaderMiddleware` reads the `Prefer` header and sets a flag consumed by assemblers.

### Pagination Links
Collection responses include standard pagination links:
- `self` — current page
- `first` — first page
- `prev` — previous page (omitted on first page)
- `next` — next page (omitted on last page)
- `last` — last page

### Authorization-Aware Links
Links are conditionally included via a 4-phase capability planning pipeline:
1. **Static checks**: `RequiresAuth`, `RequiredRoles`, `Condition` lambda filter links before any provider call.
2. **Permission checks**: `PermissionResourceKind` + `PermissionAction` (from `AuthorizationActions` constants) evaluated via `IAuthorizationProvider`. Link policies use `ResourceDescriptors` to extract resource metadata from DTOs.
3. **Deduplication**: Identical `AuthorizationCheck` records (same resource kind + id + action) collapse before batch evaluation.
4. **Fail-closed**: If batch authorization fails, all permission-bound links are denied. Non-permission links are unaffected.

---

## Specification Pattern (Advanced Query Composition)

The application uses a custom **Specification Pattern** for complex filtering, especially on the `Event` entity.

### Core Interfaces
- **`IQuerySpecification<T>`** — Composes `IFilterSpecification<T>` + `ISortSpecification<T>`. Immutable builder pattern.
- **`IFilterSpecification<T>`** — Individual filter producing `Expression<Func<T, bool>>`.
- **`ISortSpecification<T>`** — Sort directives with field name and direction.

### EventQuerySpecification (Fluent Builder)
`EventQuerySpecification` is an **immutable fluent builder** that composes filters via AND logic:

```
spec = spec
    .And(new EventFilter(...))
    .And(new EventSubqueryFilter(...))
    .And(new IslamicAspectFilter(...))
    .And(new EventCustomPropertyProjectionFilter(...))
    .SortByDescending(EventSort.StartUtc);
```

### Filter Types

| Filter Class | What It Handles | Mechanism |
|---|---|---|
| `EventFilter` | Core fields (search, date, status, type, format) | Direct `Expression<Func<Event, bool>>` |
| `EventSubqueryFilter` | Junction tables (categories, tags, locations, languages, registration modes) + JSONB metadata | Subquery with `Any()` / `All()` |
| `IslamicAspectFilter` | Islamic module fields (madhab, gender mode) | Module-conditional — silently ignored when module disabled |
| `TechAspectFilter` | Tech module fields (skill level, stack) | Module-conditional — silently ignored when module disabled |
| `AspectPresenceFilter` | HasIslamicAspect / HasTechAspect flags | Navigation property null check |
| `EventCustomPropertyProjectionFilter` | Projection-backed custom property discovery/filtering | Projection query composed alongside typed filters |

### Tag/Category Tri-State Filtering
Tags and categories support tri-state AND/OR filtering:
- **Include AND**: all specified tags must be present.
- **Include OR**: any specified tag matches.
- **Exclude AND**: exclude only if ALL specified tags present.
- **Exclude OR**: exclude if ANY specified tag present.

Implemented as separate `EventSubqueryFilterType` enum values.

### JSONB Metadata Filtering
Event metadata stored as JSONB supports two filter types:
- **`JsonContains`** — PostgreSQL `@>` operator for value matching.
- **`JsonKeyExists`** — PostgreSQL `?` operator for key existence check.

### Cache Key Generation
`EventQuerySpecification.ToCacheKeySuffix()` deterministically serializes all active filters and sorts into a cache key suffix for HybridCache integration.

---

## Pagination

Standard pagination via `PaginatedResult<T>`:

| Parameter | Default | Max | Description |
|---|---|---|---|
| `pageNumber` | 1 | — | Current page (1-based) |
| `pageSize` | 20 | 100 | Items per page |

`PaginatedResult.NormalizeParameters()` clamps values to valid ranges. Response includes `TotalCount`, `PageNumber`, `PageSize`, `TotalPages`, `HasPrevious`, `HasNext`.

---

## Response Contracts

1. Create/update flows return `BaseCommandResponse<Guid>` with `Success`, `Message`, `Errors`, `Id`.
2. Many delete flows return `bool` and map to `204 NoContent` or `404 NotFound`.
3. Query flows return DTOs or `PaginatedResult<TDto>` wrappers.
4. All responses wrapped in HAL format by default.

---

## Error Handling

### Chained IExceptionHandler Pattern
Exception handling uses .NET 8+ `IExceptionHandler` chain (not middleware):

1. **`ValidationExceptionHandler`** — Catches `FluentValidation.ValidationException` and `Application.Exceptions.ValidationException`. Returns `400 Bad Request` with structured errors dictionary.
2. **`GlobalExceptionHandler`** — Catches everything else:
   - `BadRequestException` → `400`
   - `NotFoundException` → `404`
   - `AuthorizationException` → `403`
   - Unhandled → `500` (detail hidden in production)

All responses use **RFC 7807 ProblemDetails** with extensions:
- `traceId` — from `HttpContext.TraceIdentifier`
- `timestamp` — UTC ISO 8601
- `correlationId` — from `X-Correlation-ID` / `X-Request-ID` header or generated UUID

The `type` field uses IANA RFC 9110 standard URIs (e.g., `https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5` for 404) instead of httpstatuses.com.

Current implementation detail: `ExceptionHandlingExtensions` writes `traceId` from `HttpContext.TraceIdentifier`.

.NET 10 note: handled exceptions can suppress diagnostics by default once an `IExceptionHandler` returns `true`. `UseApiExceptionHandling()` currently calls plain `app.UseExceptionHandler()` with no `SuppressDiagnosticsCallback` override, so treat handled-exception logging/metrics behavior as an explicit runtime decision.

---

## CORS Policies

Five policies configured in `Program.cs`:

| Policy | Origins | Methods | Credentials | Use Case |
|---|---|---|---|---|
| `InternalAppPolicy` | Configurable | All | Yes | Internal app communication |
| `ExternalAppPolicy` | Configurable | Specific set | No | External API consumers |
| `InternalWebsitePolicy` | Configurable (loaded from `CorsSettings:AllowedOrigins`) | All | Yes | Internal website |
| `ExternalWebsitePolicy` | Configurable | `GET`, `OPTIONS` only | No | External read-only |
| `DevPolicy` | All origins | All | Yes | Development only |

---

## Action Filters

### `BlockInSingleTenantAttribute`
Returns `404 Not Found` in single-tenant mode with hiding enabled. Conceals multi-tenant endpoints from discovery.

### `RequireMultiTenantAttribute`
Returns `403 Forbidden` with error payload when endpoint requires multi-tenant mode.

### `SetupSecretRequiredAttribute`
Gates onboarding endpoints behind the setup secret:
- If setup mode is inactive: returns `410 Gone`.
- If `X-Setup-Secret` header is missing/invalid: returns `403 Forbidden`.
- Uses `TypeFilterAttribute` pattern for DI-aware filtering with `ISetupSecretProvider`.

---

## MediatR Pipeline Behaviors

| Behavior | Purpose |
|---|---|
| `PerformanceBehavior` | Logs requests taking >500ms as warnings |
| `AuthorizationBehavior` | Checks `IAuthorizedRequest` / `[AuthorizeResource]` attribute; throws `AuthorizationException` on deny. Reflection results cached via `ConcurrentDictionary`. Emits OpenTelemetry activity spans on `Explore.Authorization` source with `resource.kind`, `resource.action`, and `request.type` tags. |

---

## Business Metrics (OpenTelemetry)

Meter name: `Explore.Business`. All counters tagged with `tenant_id` and `resource_type` dimensions.

| Counter | Description |
|---|---|
| `events.created` | Events created |
| `events.published` | Events published |
| `registrations.created` | Event registrations |
| `organizations.created` | Organizations created |
| `authorization.decisions` | Authorization check outcomes |

Authorization decisions are also traced via `ActivitySource` named `Explore.Authorization` with `resource.kind`, `resource.action`, and `request.type` tags.

---

## Background Services

### OutboxProcessor
- Polls `outbox_messages` table for pending events at configurable interval (default 5s).
- Processes in batches (default 100) with optimistic locking (`TryMarkAsProcessing`).
- Dispatches via `IOutboxMessageDispatcher` (currently `LoggingOutboxMessageDispatcher` no-op).
- Exponential backoff retry: `InitialRetryDelaySeconds × 2^retryCount`, capped at `MaxRetryDelaySeconds`.
- Dead-letters messages after `MaxRetryCount` exhausted.
- Configuration section: `OutboxProcessor` (Enabled, PollingIntervalSeconds, BatchSize, MaxRetryCount, InitialRetryDelaySeconds, MaxRetryDelaySeconds, VerboseLogging).

### PdsSyncWorker
- Polls `PdsSyncOutbox` table for pending AT Protocol sync entries.
- Processes batches with configurable polling interval and batch size.
- Exponential backoff on failures with configurable max retry count.
- Optimistic locking (`TryMarkAsProcessing`) for multi-worker safety.
- Availability-gated: skips processing when PDS service is unavailable.

---

## Graceful Shutdown

- Grace period: 25 seconds on `SIGTERM`.
- Health checks return `503` during shutdown for load balancer draining.
- Uses cooperative cancellation via `app.Lifetime.StopApplication()`. `Console.CancelKeyPress` sets `isShuttingDown` flag and triggers graceful stop.
- `Kestrel.KeepAliveTimeout`: 30 seconds.
- `Host.ShutdownTimeout`: 30 seconds.

---

## Idempotency

Write operations support the `Idempotency-Key` HTTP header for safe retries:
- Client sends `Idempotency-Key: <UUID>` on POST/PUT/PATCH/DELETE requests.
- Server caches the response by `(Key, TenantId)` in PostgreSQL.
- Duplicate requests within 24 hours replay the cached response with original status code.
- Keys expire after 24 hours via background cleanup.
- Entity: `IdempotencyRecord` with `Key`, `TenantId`, `StatusCode`, `ResponseBody`, `CreatedAt`, `ExpiresAt`.

---

## Response Compression

- Algorithms: Brotli + Gzip at `CompressionLevel.Fastest`.
- Enabled for HTTPS.
- Additional MIME types: `application/json`, `application/hal+json`.

---

## Multi-Tenancy In API

1. Tenant context is resolved per request.
2. Resolution behavior:
   - `SingleTenant`: default tenant is bound immediately.
   - `MultiTenant`: `ApiTenantResolutionMiddleware` resolves trusted `X-Tenant-Slug` first, then normalized `Request.Host.Host` after forwarded-header processing; unresolved non-API-key requests fail closed with `404`.
   - API-key requests may carry a requested tenant hint through pre-auth middleware and are finalized by `ApiTenantPostAuthenticationMiddleware`, which can return `404 Tenant mismatch` or `401 API key authentication failed`.
3. EF query filters enforce tenant scoping in persistence.
4. **Hierarchical Settings**: Governance settings follow a 5-tier resolution cascade: User → Group → Organization → Tenant → Instance. Resolution is performed in batch via `HierarchicalSettingsResolver` with support for instance-level locks and single-tenant bypass.

## Key Endpoint Groups

## Key Endpoint Groups

1. Core events:
   - `GET /api/event` — list with full specification pattern filtering
   - `GET /api/event/{id}` — detail with HATEOAS links
   - `POST /api/event` — create
   - `POST /api/event/with-sessions` — create with sessions in one request
2. Aspect endpoints:
   - `.../aspects/islamic` (`GET/PUT/DELETE`)
   - `.../aspects/tech` (`GET/PUT/DELETE`)
3. Module governance:
   - `/api/module/*` (`available`, `enabled`, `enable`, `disable`, `schema`)
4. Public experience:
    - `GET /api/publicexperience/settings`
    - `POST /api/a/t` — anonymous-safe analytics relay for relay transport mode
5. Federation:
   - `/api/atproto/*` — AT Protocol record management
   - `/api/indexeddid/*` — DID indexing
6. Notifications (all `[Authorize]`):
   - `GET /api/notification` — paginated list with `?isRead=` and `?type=` filters
   - `GET /api/notification/{id}` — detail
   - `GET /api/notification/unread-count` — unread count (partial index optimized)
   - `PATCH /api/notification/{id}/read` — mark single as read (idempotent)
   - `POST /api/notification/read-all` — bulk mark all as read (YouTube-style, timestamp cutoff)
   - `DELETE /api/notification/{id}` — soft delete
7. Footer management:
   - `GET /api/footer/config` — public footer config (AllowAnonymous)
   - `GET /api/footer/link-groups` — list link groups (Authorize)
   - `GET /api/footer/link-groups/{id}` — link group detail (Authorize)
   - `POST /api/footer/link-groups` — create link group
   - `PUT /api/footer/link-groups/{id}` — update link group
   - `DELETE /api/footer/link-groups/{id}` — delete link group
   - `POST /api/footer/link-groups/reorder` — reorder link groups
   - `POST /api/footer/link-groups/{groupId}/links` — create link in group
   - `PUT /api/footer/links/{id}` — update link
   - `DELETE /api/footer/links/{id}` — delete link
   - `PUT /api/footer/settings` — update footer settings
8. Actor appearance:
   - Actor entities include appearance fields (BackgroundColor, BackgroundEffect, BannerColor, BannerPictureId, BackgroundImageId) managed via actor update endpoints.

---

## OpenAPI Export And Client Generation

1. In Development, API startup exports OpenAPI to `Explore.API/swagger.json`.
2. `HalSchemaTransformer` transforms OpenAPI schemas to show HAL structure.
3. Blazor client build uses this file as NSwag input and regenerates `Clients/EventApiClient.g.cs` before compile.
4. DTO changes should follow API-first regeneration workflow (see `docs/CONTRIBUTING.md`).

---

## Related Docs
- `docs/SECURITY.md` — auth, JWT, CORS, security headers
- `docs/ARCHITECTURE.md` — Clean Architecture layers, request flow
- `docs/OPERATIONS.md` — rate limiting config, timeouts, shutdown
- `docs/CODEBASE_INSIGHTS.md` — non-obvious patterns
- `docs/MULTI_TENANCY.md` — tenant resolution and isolation
- `docs/OUTBOX_PATTERN.md` — outbox pattern implementation details
- `docs/FOOTER_MANAGEMENT.md` — footer management system
- `docs/CONTRIBUTING.md` — development workflow
