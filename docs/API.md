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
2. **Security Headers** — `UseSecurityHeaders()`. Adds defensive headers to every response.
3. **Correlation ID** — `UseCorrelationId()`. Reads `X-Correlation-ID` or `X-Request-ID`, generates UUID if absent, pushes to Serilog `LogContext`.
4. **Request Logging** — `UseRequestLogging()`. Structured Serilog logging: method, path, status, duration, userId, tenantId, correlationId.
5. **Response Compression** — `UseResponseCompression()`. Brotli + Gzip at `CompressionLevel.Fastest`. Enabled for HTTPS. Additional MIME types: `application/json`, `application/hal+json`.
6. **HTTPS Redirection** — `UseHttpsRedirection()`.
7. **HATEOAS Prefer Header** — `UseHateoas()`. RFC 7240 `Prefer` header processing (`return=minimal` strips `_links`).
8. **Routing** — `UseRouting()`.
9. **Request Timeouts** — `UseRequestTimeouts()`. Three configurable tiers (see below).
10. **Authentication** — `UseAuthentication()`. JWT Bearer via Keycloak.
11. **Rate Limiter** — `UseRateLimiter()`. Four tiered policies (see below).
12. **Authorization** — `UseAuthorization()`.
13. **Output Cache** — `UseOutputCache()`. Three cache policies (see below).
14. **ETag** — `UseETag()`. SHA256-based weak ETags, 304 Not Modified support.

---

## API Versioning

1. Media-type strategy: `Accept: application/json;v=0.1` or `application/hal+json;v=0.1`.
2. Default API version is `0.1` when unspecified.
3. Clean URLs — no `/v1/` path segments.
4. Version is reported in response headers via `Asp.Versioning` middleware.

## Controller Conventions

1. Controllers are thin: receive request → dispatch MediatR command/query → assemble HATEOAS response → return HTTP result.
2. Business logic belongs in handlers/services, never controllers.
3. Every endpoint has named routes (via `RouteNames` constants) for HATEOAS link generation.
4. Endpoints include `[ProducesResponseType]` and XML doc summaries for OpenAPI quality.

---

## Rate Limiting (4 Tiers)

Configured in `RateLimitingExtensions.cs`. All settings are configurable via `appsettings.json` under `RateLimiting` section.

### Global (IP Token Bucket)
- **Policy**: `global` — applied to all endpoints by default.
- **Mechanism**: Token bucket per IP address.
- **Defaults**: 200 tokens, replenish 40 tokens per 10 seconds.
- **IP Resolution**: `X-Forwarded-For` header aware (picks first address). Falls back to `RemoteIpAddress`.
- **Exemption**: Localhost (`127.0.0.1`, `::1`) is exempt.

### Authenticated (Sliding Window)
- **Policy**: `authenticated` — for authenticated user endpoints.
- **Mechanism**: Sliding window per user ID.
- **Defaults**: 200 requests per 60-second window, 4 segments.

### Write (Fixed Window)
- **Policy**: `write` — for mutation endpoints (`POST`, `PUT`, `DELETE`).
- **Mechanism**: Fixed window per user ID.
- **Defaults**: 30 requests per 60-second window.

### SetupSecret (Fixed Window)
- **Policy**: `setup_secret` — for instance bootstrap endpoints.
- **Mechanism**: Fixed window per IP address.
- **Defaults**: 5 requests per 60-second window.

### Rejection Behavior
- Returns `429 Too Many Requests` with RFC 6585 `ProblemDetails`.
- Includes `Retry-After` header and `X-RateLimit-*` headers (limit, remaining, reset).

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
| `LookupData` | 1 hour | — | Lookup tables (event types, languages, etc.) |
| `ListData` | 30 seconds | page number, page size, all query params | Collection listings |
| `DetailData` | 60 seconds | entity ID | Single-entity detail views |

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
- Multi-client audience validation: `explore-api`, `explore-blazor-server`, `account`.
- Custom `AudienceValidator`: checks both `aud` claim and `azp` (Keycloak authorized party) claim.
- Clock skew tolerance: 5 minutes.
- Dev mode: accepts self-signed certificates.
- Detailed JWT event logging: `OnAuthenticationFailed`, `OnTokenValidated`, `OnChallenge`, `OnMessageReceived`.

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
Links are conditionally included based on:
1. **Static checks**: `RequiresAuth`, `RequiredRoles`, `Condition` lambda.
2. **Permission checks**: `PermissionResourceKind` + `PermissionAction` evaluated via `IAuthorizationProvider`.
3. HTTP method → permission action mapping: GET→read, POST→create, PUT/PATCH→update, DELETE→delete.

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
    .WithFilter(new EventFilter(...))           // Direct expression filter
    .WithSubqueryFilter(new EventSubqueryFilter(...))  // Junction table filter
    .WithAspectFilter(new IslamicAspectFilter(...))    // Module-conditional filter
    .WithSort(new EventSort(EventSortField.Date, SortDirection.Desc));
```

### Filter Types

| Filter Class | What It Handles | Mechanism |
|---|---|---|
| `EventFilter` | Core fields (search, date, status, type, format) | Direct `Expression<Func<Event, bool>>` |
| `EventSubqueryFilter` | Junction tables (categories, tags, locations, languages, registration modes) + JSONB metadata | Subquery with `Any()` / `All()` |
| `IslamicAspectFilter` | Islamic module fields (madhab, gender mode) | Module-conditional — silently ignored when module disabled |
| `TechAspectFilter` | Tech module fields (skill level, stack) | Module-conditional — silently ignored when module disabled |
| `AspectPresenceFilter` | HasIslamicAspect / HasTechAspect flags | Navigation property null check |

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
- `traceId` — from `Activity.Current` or `HttpContext.TraceIdentifier`
- `timestamp` — UTC ISO 8601

---

## CORS Policies

Five policies configured in `Program.cs`:

| Policy | Origins | Methods | Credentials | Use Case |
|---|---|---|---|---|
| `InternalAppPolicy` | Configurable | All | Yes | Internal app communication |
| `ExternalAppPolicy` | Configurable | Specific set | No | External API consumers |
| `InternalWebsitePolicy` | `iloveibadah.app` only | All | Yes | Internal website |
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
| `AuthorizationBehavior` | Checks `IAuthorizedRequest` / `[AuthorizeResource]` attribute; throws `AuthorizationException` on deny |

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

---

## Background Services

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
- `SIGINT`: immediate shutdown.
- `Kestrel.KeepAliveTimeout`: 30 seconds.
- `Host.ShutdownTimeout`: 30 seconds.

---

## Response Compression

- Algorithms: Brotli + Gzip at `CompressionLevel.Fastest`.
- Enabled for HTTPS.
- Additional MIME types: `application/json`, `application/hal+json`.

---

## Multi-Tenancy In API

1. Tenant context is resolved per request.
2. Resolution behavior:
   - `SingleTenant`: default tenant
   - `MultiTenant`: `X-Tenant-Id` → custom domain → subdomain → default tenant
3. EF query filters enforce tenant scoping in persistence.

---

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
5. Federation:
   - `/api/atproto/*` — AT Protocol record management
   - `/api/indexeddid/*` — DID indexing

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
- `docs/CONTRIBUTING.md` — development workflow
