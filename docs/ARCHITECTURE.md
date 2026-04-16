ABOUTME: System architecture summary for the current codebase, not a theoretical template.
ABOUTME: Captures key runtime patterns and boundaries that are not obvious from one file.

# Technical Architecture

## System Profile
- Style: Clean Architecture + CQRS + BFF.
- Runtime: .NET 10 (`net10.0`, preview SDK pinned in `global.json`).
- API host: `Explore.API`.
- BFF host: `Explore.Blazor`.
- Interactive UI client: `Explore.Blazor.Client`.
- Data: PostgreSQL via EF Core.

## Layer Boundaries
1. `Explore.Domain`: entities, enums, domain rules, no infrastructure concerns.
2. `Explore.Application`: requests/handlers, DTOs, validators, contracts.
3. `Explore.Persistence` + `Explore.Infrastructure`: data + external service implementations.
4. `Explore.API` and `Explore.Blazor`: presentation and composition roots.

Dependency direction is inward: presentation -> application -> domain.

## Request Flow
1. HTTP request enters the middleware pipeline (exception handling → security headers → correlation ID → logging → compression → HATEOAS → routing → timeouts → auth → rate limiting → authorization → output cache → ETag → idempotency).
2. Controller receives request, dispatches MediatR command/query.
3. MediatR pipeline behaviors execute: `PerformanceBehavior` (>500ms warning), `AuthorizationBehavior` (resource-level permission checks via `IAuthorizedRequest` / `[AuthorizeResource]`; uses reflection caching and emits OpenTelemetry activity spans).
4. Handler orchestrates validation (manually instantiated validators), repository calls, mapping.
5. Persistence layer returns entities; handlers map to DTO/response contracts.
6. Controller delegates to `ResourceAssemblerBase` for HATEOAS HAL wrapping with authorization-aware link generation.

## BFF Model (Blazor)
1. Browser authenticates via OIDC through BFF endpoints.
2. Session/cookie state remains in server-controlled flow.
3. BFF forwards API calls to backend (token forwarding + tenant header propagation where needed).
4. `Explore.Blazor.Client` focuses on UI and typed service calls; it is not a token authority.

## Multi-Tenancy Model
1. Runtime mode is resolved from governance settings (`SingleTenant` / `MultiTenant`).
2. In `SingleTenant`, default tenant is used for all requests.
3. In `MultiTenant`, tenant is resolved from header/domain/subdomain fallback chain.
4. EF query filters enforce tenant isolation centrally in `ExploreDbContext`.

## Authorization Architecture
1. Endpoint-level auth is handled via ASP.NET attributes/policies. `[AuthorizeResource]` attribute pairs a resource kind with a domain action constant from `AuthorizationActions`.
2. Resource-level auth is handled in the `AuthorizationBehavior` MediatR pipeline. Checks route to `IAuthorizationProvider` which resolves to Cerbos PDP or local fallback.
3. `AuthorizationActions` (string constants) and `ResourceKinds` (string constants) form the canonical action/resource catalogs shared by commands, link policies, and Cerbos policies.
4. `IAuthorizableResourceDescriptor<T>` + `ResourceDescriptors` extract resource metadata (kind, id, attributes, scope) from DTOs — eliminating manual attribute dictionaries in HATEOAS link policies.
5. HATEOAS capability planning uses a 4-phase pipeline: candidate links → normalized `AuthorizationCheck` with dedup key → batch evaluate unique checks → map decisions back to links. Fail-closed on batch failure.
6. Runtime authorization provider routes checks: BYO Cerbos (per-tenant) → Instance Cerbos (if enabled) → FallbackAuthorizationService (DB-driven RBAC).
7. SafeMode is a one-way latch via `ActivateSafeMode()` — once activated by a Cerbos failure, it cannot be deactivated until restart. Logs `LogCritical` once.
8. Cerbos policies reference JSON schemas (`_schemas/`) for principal and resource attribute contracts. Schema enforcement is `warn` by default.

## API Representation
1. HAL/HATEOAS wrappers are used for discoverable responses.
2. `Prefer: return=minimal` can reduce link payload where clients do not need hypermedia.
3. OpenAPI is exposed and exported in development for client generation.
4. `HalSchemaTransformer` adjusts OpenAPI schemas to reflect HAL structure.
5. Dual API versioning: media-type (`Accept: application/json;v=0.1`) and URL segment (`/api/v0.1/controller`) supported simultaneously via `VersionedRouteConvention`.

## Specification Pattern (Query Composition)
Complex filtering uses a custom Specification Pattern:
1. `IQuerySpecification<T>` composes `IFilterSpecification<T>` + `ISortSpecification<T>` via immutable builder.
2. `EventQuerySpecification` chains filters using AND composition: direct filters (`EventFilter`), subquery filters for junction tables (`EventSubqueryFilter`), module-conditional aspect filters (`IslamicAspectFilter`, `TechAspectFilter`), presence filters (`AspectPresenceFilter`), and projection-backed custom-property filters (`EventCustomPropertyProjectionFilter`).
3. Filters are applied to `IQueryable<T>` in the repository — module-specific filters are silently ignored when modules are disabled.
4. `ToCacheKeySuffix()` generates deterministic cache keys from active filter/sort state.

## Event Data Layers
1. Layer 1 stores universal semantics directly on `Event`, `EventSession`, and other first-class related entities.
2. Layer 2 stores sector-standard semantics in typed 1:1 schema such as `EventIslamicAspect`, `EventTechAspect`, and `EventSessionIslamicAspect`.
3. Layer 3 stores tenant-specific long-tail extensions through governed custom-property entities and event/session template/runtime rows.
4. `Event` remains the parent program/container aggregate; `EventSession` remains the scheduled child aggregate.
5. Layer 3 must not redefine or replace Layer 2 semantics; reserved namespaces and collision rules are part of the custom-properties architecture.
6. `EventCustomPropertyProjection`, `EventSessionCustomPropertyProjection`, and aggregate event-with-sessions read views are derived query models only; source of truth remains typed schema plus event-local and session-local custom-property rows.

## Caching Architecture (3 Layers)
1. **Output Cache** (HTTP response level): `LookupData` (1h), `ListData` (30s, varies by `Authorization` header), `DetailData` (60s, varies by `Authorization` header), `PublicData` (1h, no auth variance). Applied via `[OutputCache]` on endpoints.
2. **HybridCache** (application level, L1 in-memory + L2 distributed): 30min default expiration, 5min local, 10MB max payload. Used in MediatR handlers with read-through and explicit invalidation patterns.
3. **ETag Middleware** (conditional requests): RecyclableMemoryStream-based, SHA256 weak ETags on JSON/HAL responses, returns `304 Not Modified`. Skips bodies larger than 256KB.

## MediatR Pipeline Behaviors
1. `PerformanceBehavior` — logs any request taking >500ms as a warning.
2. `AuthorizationBehavior` — checks `IAuthorizedRequest` interface or `[AuthorizeResource]` attribute. Optionally enhanced by `ISecureRequest` for dynamic resource context. Throws `AuthorizationException` on deny. Uses `ConcurrentDictionary` reflection caching for attribute lookups and emits activity spans via the `Explore.Authorization` ActivitySource for distributed tracing.

## Idempotency
Write operations support `Idempotency-Key` header for safe retries. `IdempotencyMiddleware` caches responses by `(Key, TenantId)` in PostgreSQL and replays them within 24 hours. Entity: `IdempotencyRecord` in Domain layer, persisted via `IIdempotencyRepository`.

## Federation Status
Implemented today:
- ATProto-related entities and API resources (e.g., indexed DID and ATProto records).
- Outbox-based PDS sync background worker.

Not fully implemented today:
- Complete ActivityPub gateway endpoint surface.
- Full federation protocol exposure expected by third-party federated servers.

## Outbox Pattern

The system uses a transactional outbox for reliable asynchronous event delivery:

1. Domain changes write an `OutboxMessage` to the same database transaction as the business entity change.
2. `OutboxProcessor` (BackgroundService) polls for pending messages, dispatches via `IOutboxMessageDispatcher`, and manages retry/dead-letter lifecycle.
3. Delivery guarantee is **at-least-once** — consumers must be idempotent.
4. Retry uses exponential backoff: `InitialRetryDelaySeconds × 2^retryCount`, capped at `MaxRetryDelaySeconds`.
5. After `MaxRetryCount` exhausted, messages are dead-lettered and remain in the database for manual inspection.
6. Optimistic concurrency via `TryMarkAsProcessing` prevents duplicate processing across workers.

Specialized outbox variants exist for specific subsystems:
- `PdsSyncOutbox` — AT Protocol federation sync (DID, Collection, RecordKey, PdsHost).
- `PolicyChangeOutbox` — authorization policy change propagation (SettingScope).

See [OUTBOX_PATTERN.md](OUTBOX_PATTERN.md) for full entity model, configuration, and monitoring details.

## Background Services

| Service | Purpose | Polling |
|---|---|---|
| `OutboxProcessor` | General outbox message dispatch with retry/dead-letter | Configurable (default 5s) |
| `PdsSyncWorker` | AT Protocol PDS synchronization from `PdsSyncOutbox` | Configurable with exponential backoff |

Both services use optimistic locking for multi-worker safety and are availability-gated (skip processing when dependent services are unavailable).

## Local Runtime Endpoints
- API dev: `https://localhost:7039`
- Blazor dev: `https://localhost:7177`
- Docker API: `http://localhost:7039`
- Docker Blazor: `http://localhost:7002`

## Related
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md)
- [OUTBOX_PATTERN.md](OUTBOX_PATTERN.md)
- [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md)
- [FOOTER_MANAGEMENT.md](FOOTER_MANAGEMENT.md)
- [SECRETS.md](SECRETS.md)
