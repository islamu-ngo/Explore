ABOUTME: Non-intuitive patterns, hidden knowledge, and implementation details requiring deep analysis.
ABOUTME: Captures what you cannot guess from reading ARCHITECTURE.md alone — internal mechanics and gotchas.

# Codebase Insights

> Non-intuitive patterns, hidden knowledge, and things requiring deep analysis.
> This document captures what you cannot guess from reading ARCHITECTURE.md alone.
> Last Updated: April 2026

---

## 1. Multi-Tenancy Deep Mechanics

### How Tenant Isolation Actually Works

Multi-tenancy is a **middleware-plus-query-filter pipeline**. `ApiTenantResolutionMiddleware` determines request tenant context before data access, and EF Core global query filters enforce that tenant scope in persistence. The flow:

1. **ApiTenantResolutionMiddleware** resolves tenant identity from trusted slug/host request context before tenant-scoped data access
2. **ExploreDbContext** receives TenantContext via **property injection** (not constructor injection) because the DbContext uses pooling
3. **Named query filters** on every tenant-scoped entity automatically filter by `TenantId`
4. The filter expression is: `TenantContext == null || e.TenantId == TenantContext.TenantId` — the null check allows migrations and seeding to bypass tenant filtering

### Tenant Resolution Priority

The API-authoritative tenant resolver uses this order for normal multi-tenant requests:
1. trusted `X-Tenant-Slug` HTTP header from the BFF
2. Custom domain lookup (checks `TenantSetting` for matching domain)
3. Subdomain extraction (extracts from host, looks up tenant by subdomain)
4. Unresolved request fails closed; single-tenant mode still uses the configured default tenant

### Runtime vs Static Deployment Mode

The system checks deployment mode (SingleTenant/MultiTenant) **at runtime from the database** (`SystemSettings` table), not just from `appsettings.json`. This means a running instance can be switched between modes without redeployment. The static config is only a fallback when the DB is unreachable.

### Not All Entities Are Tenant-Scoped

- **Tenant-scoped** (implement `ITenantEntity`): Event, Organization, Actor, Location, StorageObject, Category, Tag, and their junction tables
- **Global** (no tenant filter): User, lookup tables (EventType, Language, etc.), SystemSetting, ModuleDefinition, InstanceAdministrator
- **Soft-delete only** (no tenant, has `ISoftDeletable`): User

### The TenantSetting vs TenantSettings Distinction

There are **two different entities** for tenant settings:
- `TenantSetting` — Individual key-value pairs per tenant (governance overrides, used by `SettingsResolver`)
- `TenantSettings` — A snapshot entity with explicit properties per tenant (display name, theme, etc.)

The DbSet names reflect this: `TenantSettingOverrides` for `TenantSetting`, `TenantSettings` for `TenantSettings`.

---

## 2. DbContext Pooling, Property Injection, and Partial Class Decomposition

The `ExploreDbContext` uses **pooled DbContext factory** for performance. This has a critical implication:

- The constructor **cannot** accept scoped services (TenantContext, CurrentUserService)
- Instead, these are set via **property injection** after the context is created from the pool
- The scoped registration in `PersistenceServicesRegistration.cs` creates the context from the factory, then sets the properties

This means:
- `TenantContext` and `CurrentUserService` can be `null` (during migrations, seeding, or background services)
- All query filters and audit logic must handle null gracefully
- If you add a new scoped dependency to DbContext, it must follow this property injection pattern

### Partial Class Structure

The DbContext is split into 4 partial class files for maintainability:
- `ExploreDbContext.cs` — Constructor, property injection, `OnModelCreating` (calls `ApplyGlobalQueryFilters`)
- `ExploreDbContext.DbSets.cs` — All 170 DbSet declarations organized by domain area
- `ExploreDbContext.QueryFilters.cs` — `ApplyGlobalQueryFilters()` with 48 named filter registrations
- `ExploreDbContext.SaveChanges.cs` — `SaveChangesAsync` override (audit, soft delete, concurrency)

Each partial needs only its own usings. `internal` visibility works across partials in the same assembly.

---

## 3. Soft Delete Interception in SaveChanges

When you call `Delete()` on an entity that implements `ISoftDeletable`, the `GenericRepository` sets `EntityState.Deleted`. But the entity is **never actually deleted** from the database. Here's what happens:

1. `GenericRepository.Delete()` marks entity as `EntityState.Deleted`
2. `ExploreDbContext.SaveChangesAsync()` intercepts the Deleted state
3. Changes it to `EntityState.Modified` instead
4. Sets `IsDeleted = true`, `DeletedAt = now`, `DeletedBy = userId`
5. Also updates `UpdatedAt`/`UpdatedBy` if entity is `IAuditableEntity`

For entities that don't implement `ISoftDeletable`, `Delete()` performs a real hard delete.

`HardDelete()` method exists for admin operations that need permanent removal regardless of `ISoftDeletable`.

---

## 4. Named Query Filters (EF Core 10+)

This project uses EF Core 10's **named query filters** feature. Each entity can have multiple independently-controllable filters:

- `QueryFilterNames.Tenant` — Tenant isolation filter
- `QueryFilterNames.SoftDelete` — Soft delete filter

You can selectively disable one without the other:
- `IgnoreQueryFilter("SoftDelete")` — Shows deleted records but still respects tenant isolation
- `IgnoreQueryFilter("Tenant")` — Cross-tenant query (admin operations) but still hides deleted records
- `IgnoreQueryFilters()` — Disables ALL filters (dangerous — cross-tenant + deleted)

Not all entities have both filters. Check `ExploreDbContext.ApplyGlobalQueryFilters()` for the exact mapping.

---

## 5. Blazor Server + WASM Hybrid Architecture

### The Two-Project Structure

- `Explore.Blazor` — Blazor Server host. Handles SSR, OIDC authentication, serves the WASM app, and acts as a BFF proxy
- `Explore.Blazor.Client` — Blazor WebAssembly. Contains all interactive UI components, pages, and services

### How Authentication Flows

1. User accesses the Blazor Server host
2. OIDC challenge redirects to Keycloak for login
3. After login, the server stores tokens in a `CircuitAccessTokenService` (per-circuit lifetime)
4. For WASM: Authentication state is serialized to the client using `AddAuthenticationStateDeserialization()`
5. WASM makes API calls through the same origin (BFF pattern)
6. `BrowserCredentialsMessageHandler` adds cookies to all WASM HttpClient requests
7. `BffUnauthorizedHandler` intercepts 401 responses and triggers server-side re-login

### The BFF Pattern

The Blazor Server host proxies API requests. WASM never calls the API directly — it goes through the BFF:
- WASM `HttpClient.BaseAddress` = same origin as Blazor Server
- Blazor Server proxies `/api/*` requests to the actual API
- Cookies (including auth tokens) travel automatically

### S3 Image Upload Exception

Image uploads bypass the BFF. They go directly to S3-compatible storage (Hetzner) using presigned URLs:
- `ImageStorageService` gets a presigned URL from the API
- `S3UploadMessageHandler` configures CORS mode for cross-origin PUT
- Direct browser-to-S3 upload avoids server bottleneck

---

## 6. Module Governance System

### What Modules Are

The system supports pluggable modules that add domain-specific behavior:
- **Core** — Always enabled. Basic event functionality
- **Islamic** — Madhab selection, prayer times, gender segregation awareness
- **Tech** — GitHub repositories, skill levels, live coding flags

### How Modules Work

1. `ModuleDefinition` table defines available modules (seeded)
2. `TenantCapability` maps which modules each tenant has enabled
3. `IModuleService` checks if a module is enabled for the current tenant
4. `EventIslamicAspect` / `EventTechAspect` — Optional 1:1 extension tables that store module-specific fields
5. `IEventStrategy` / `StrategyResolver` — Module-specific behavior dispatched at runtime via strategy pattern

### The Strategy Pattern

When creating/updating events, the `StrategyResolver` picks the right strategy based on enabled modules:
- `IslamicEventStrategy` — Handles Islamic-specific validation and field population
- `TechEventStrategy` — Handles tech-specific validation and field population

---

## 7. Lookup Table Dual-Track System

Lookup tables are managed in **two synchronized places**:

### Track 1: Domain Enums
`Explore.Domain/Enums/{EntityName}Enum.cs` — Defines the int IDs and names. Used in code for type-safe references.

### Track 2: EF Core HasData Seeding
`Explore.Persistence/Configurations/Entities/{EntityName}Configuration.cs` — Uses `HasData()` to seed the database with matching values.

### The Synchronization Rule

The enum values and HasData seed values **must always match**. If you add a new enum value, you must also add a matching `HasData()` entry. The `LookupTableSeeder` class helps ensure runtime synchronization.

### Which ID Type for What

- **Lookup tables** use `int` PK (EventType, Language, etc.)
- **Main entities** use `Guid` PK (Event, Organization, etc.)
- **Link tables** use composite keys (EventId + CategoryId, etc.)
- Never use `long` except for file sizes or pagination cursors

---

## 8. Seed Data Architecture

### Two Seeding Mechanisms

1. **EF Configuration HasData()** — Lookup tables are seeded via entity configurations. This creates migration-tracked seed data. Used for: EventType, EventStatus, EventFormat, AudienceAge, etc.

2. **Runtime DatabaseSeeder** — Called at startup via `DatabaseSeeder.cs`. Seeds main entities (Tenant, User, Organization, Actor, StorageObject, Event). Uses `SeedIds.cs` for deterministic GUIDs.

### SeedIds and UUIDv7

All seed GUIDs use **UUIDv7** format (`018e4e5c-xxxx-7xxx-8xxx-xxxxxxxxxxxx`) for:
- Time-ordered insertion (better index performance)
- Deterministic values (same GUIDs every run, prevents duplicates)
- Range-separated by entity type (different `xxxx` ranges for tenants vs users vs organizations)

**Critical rule:** Never change seed IDs after they've been used in production migrations.

---

## 9. HATEOAS Implementation

The API implements **HAL (Hypertext Application Language)** for hypermedia with a layered architecture:

### Core Components
- `ResourceAssemblerBase<TDto, TListDto>` — Base class for assembling HAL responses. Handles both single-entity and collection assembly with **batch authorization evaluation**.
- `ILinkPolicy<TDto>` / `ICollectionLinkPolicy<TDto>` — Per-entity link definitions using yield return pattern. Each entity has separate detail and collection link policies.
- `LinkDefinition` — Rich record type with: `Rel`, `RouteName`, `RouteValues`, `Method`, `Title`, `RequiresAuth`, `RequiredRoles`, `Condition` lambda, and permission metadata (`PermissionResourceKind`, `PermissionAction`, `PermissionResourceId`, `PermissionResourceAttributes`).
- `HateoasAuthorizationEvaluator` — Performs batch authorization: static checks (auth, roles, conditions) first, then remaining permission-bound links sent to `IAuthorizationProvider.IsAllowedBatchAsync()` in a single call. On failure, permission-bound links are denied (fail-closed).
- `HateoasLinkGenerator` — Resolves URLs from named routes via ASP.NET Core `LinkGenerator`.
- `RouteNames` — 100+ named route constants organized by resource type (events, organizations, actors, etc.).

### Content Negotiation
- Default format: `application/hal+json` with `_links` and `_embedded`.
- `PreferHeaderMiddleware` reads RFC 7240 `Prefer: return=minimal` header and sets a flag consumed by assemblers to strip `_links`.
- Pagination links: `self`, `first`, `prev`, `next`, `last`.

### Authorization-Aware Links
Links declare requirements via `LinkDefinition`:
- `RequiresAuth: true` — user must be authenticated.
- `RequiredRoles` — user must have specific roles.
- `Condition` — lambda evaluated at assembly time.
- `.RequirePermission()` — fluent method adding `PermissionResourceKind`, `PermissionAction`, and optional resource attributes for Cerbos/RBAC check.

HTTP method → action mapping: GET→read, POST→create, PUT/PATCH→update, DELETE→delete.

---

## 10. The Actors System (Federation/ATProto)

### What Actors Are

`Actor` is the federation identity concept from ATProto/ActivityPub:
- Every user gets a personal Actor
- Every organization gets an Actor
- Actors have DIDs (Decentralized Identifiers), key pairs, and can federate

### Actor ↔ User/Organization Relationship

- An `Actor` has an `ActorType` (Personal, Organization, Bot, Service)
- A `User` owns one or more Actors
- An `Organization` is linked to exactly one Actor
- `ActorKeyStore` stores signing keys for federated operations
- `DidCustodyType` tracks who controls the DID (self, server, external)

### Federation Sync

- `PdsSyncOutbox` — Outbox pattern for syncing changes to ATProto PDS
- `PdsSyncWorker` — Background service that processes the outbox
- `IndexedDid` / `SyncState` / `AtprotoRecord` — Firehose indexer state

---

## 11. Hierarchical Settings Resolution Cascade

The governance settings system has a five-tier resolution implemented in `HierarchicalSettingsResolver`:

1. **User Preference** — individual user settings.
2. **GroupSetting** — group-level overrides.
3. **OrganizationSetting** — organization-level overrides.
4. **TenantSetting** — per-tenant overrides.
5. **SystemSetting** — instance-wide platform defaults.

### Mechanics

- **Batch Loading**: The resolver pre-fetches all tiers for requested keys in exactly two queries (one for system, one for scoped tiers) to avoid N+1 query patterns.
- **Lock Precedence**: If a lower tier (e.g., System) marks a setting as `IsLocked`, the resolver ignores overrides from higher tiers (Tenant, Org, etc.).
- **Serialization**: Values are stored as JSON-serialized strings in the database. The `SettingValueSerializer` handles typed deserialization.
- **Single-Tenant Bypass**: In single-tenant mode, instance-level locks are automatically bypassed to give the tenant full administrative control.

### GovernanceSettingKeys

Setting keys are string constants in `GovernanceSettingKeys.cs`. They use dot-separated hierarchical naming:
- `Deployment.Mode`
- `Events.MaxSessionsPerEvent`
- `Modules.Islamic.Enabled`
- `Domains.InstanceBaseDomain`
- `Domains.AllowTenantCustomDomain`

---

## 12. AutoMapper Profile Organization

Mappings are split into **10 domain-specific profiles** in `Explore.Application/Profiles/`:

| Profile | Covers |
|---|---|
| `TenantMappingProfile` | Tenant, TenantMember, TenantSettings, Footer |
| `EventMappingProfile` | Event, EventSeries, EventDay, EventAgendaItem, Tags, Categories, Aspects |
| `EventSessionMappingProfile` | EventSession, SessionAgendaItem, SessionSpeaker, SessionLanguage |
| `CustomPropertyMappingProfile` | All custom property definitions, templates, options, values |
| `OrganizationMappingProfile` | Organization, Group, Members, ApprovalStatus, Reviews |
| `UserMappingProfile` | User, UserAuthenticationToken, UserExternalLogin |
| `RegistrationMappingProfile` | EventRegistration, RegistrationIntent, Scope, Policy |
| `ActorFederationMappingProfile` | Actor, ActorKeyStore, StorageObject, IndexedDid, SyncState |
| `LookupMappingProfile` | All lookup tables (Location, Tag, Language, etc.) |
| `NotificationMappingProfile` | Notification, ProjectionStatus |

`AddAutoMapper(Assembly.GetExecutingAssembly())` auto-discovers all `Profile` subclasses — **no DI changes needed** when adding new profiles.

When adding a new entity/DTO pair, add the mapping to the appropriate domain profile. Watch for namespace clashes: use aliases like `using EventSeriesNS = Explore.Application.DTOs.EventSeries;` and fully-qualified `Domain.EventStatus`, `Domain.Actor` etc. where DTO names collide.

---

## 13. API Client Generation

The Blazor client uses an **NSwag-generated API client**:

- `EventApiClient.cs` — Hand-written interface (`IEventApiClient`) with additional methods
- `EventApiClient.g.cs` — Auto-generated code from `swagger.json`
- The generated client is registered in WASM `Program.cs` with the BFF HttpClient

To regenerate: update `swagger.json` from the API, then run the NSwag generator.

---

## 14. Exception Handling Flow

### API Side — Chained IExceptionHandler Pattern
Exception handling uses .NET 8+ **chained `IExceptionHandler`** (not middleware). Two handlers in chain order:

1. **`ValidationExceptionHandler`** — Catches `FluentValidation.ValidationException` and `Application.Exceptions.ValidationException`. Returns `400 Bad Request` with structured errors dictionary.
2. **`GlobalExceptionHandler`** — Catches everything else:
   - `BadRequestException` → `400`
   - `NotFoundException` → `404`
   - `AuthorizationException` → `403`
   - Unhandled → `500` (detail message hidden in production)

All responses use **RFC 7807 ProblemDetails** with extensions:
- `traceId` — from `Activity.Current` or `HttpContext.TraceIdentifier`
- `timestamp` — UTC ISO 8601

### Command Response Pattern
Commands don't throw for validation failures. They return `BaseCommandResponse<TKey>`:
- `Success = true/false`
- `Errors` list with validation messages
- Controllers return `Ok(response)` or `BadRequest(response)` based on `Success`

### Client Side
`ServiceResult<T>` wraps API responses in the Blazor client, providing a consistent error handling pattern.

---

## 15. ExploreControllerBase and Identity Resolution

All controllers that access user identity inherit from `ExploreControllerBase` (not `ControllerBase` directly). The base class provides:
- `protected IUserContext UserContext` — Lazy-resolved via `HttpContext.RequestServices.GetRequiredService<IUserContext>()`
- `protected Guid? CurrentUserId` — Nullable user ID from claims (fallback order: `internal_user_id` → `sub` → `nameidentifier` → `sid`)
- `protected Guid RequiredUserId` — Throws `UnauthorizedAccessException` if no authenticated user

**Why lazy resolution?** Avoids the "service not available during construction" problem with pooled DbContext scenarios. No constructor injection needed — controllers just change their base class.

Controllers that don't need identity (most GET-only lookup controllers) still inherit `ControllerBase` directly. The architecture test `ControllersAccessingIdentity_ShouldInherit_ExploreControllerBase` enforces this.

---

## 15.1. Include Chain Extensions (Persistence)

Repeated EF Core Include chains are extracted into `IQueryable<T>` extension methods in `Explore.Persistence/Extensions/`:
- `EventQueryExtensions.IncludeStandardDetails()` — 15 includes (EventType, Actor chain, FeaturedImage, etc.)
- `EventSessionQueryExtensions.IncludeStandardDetails()` — 5 includes (Event, Location, Room, etc.)
- `NotificationQueryExtensions.IncludeStandardDetails()` — 6 includes (Type, EntityType, Scope, Actor chains)

**Key design rule:** Extensions contain ONLY the `.Include()` chain — NOT `AsNoTracking()` or `AsSplitQuery()`. Callers control query strategy. This preserves flexibility for tracked vs untracked scenarios.

---

## 15.2. EventActorResolver (Application Service)

Event creation requires resolving which Actor (org/group/personal) the event is published under. This cross-cutting logic was extracted from command handlers into `IEventActorResolver`:
- Checks organization membership + `EventCreate` permission
- Checks group membership + `EventCreate` permission
- Falls back to personal actor with publishing policy check (`events.user_submission_enabled`)
- Returns `EventActorResult` (success with ActorId, or failure with error details)

Registered as scoped in `ApplicationServicesRegistration.cs`.

---

## 15.3. BlockInSingleTenant Filter

The `BlockInSingleTenantAttribute` action filter prevents certain endpoints from being called in single-tenant mode. This is used for multi-tenant management endpoints that don't make sense in single-tenant deployments.

---

## 16. DI Registration Locations

Services are registered in multiple places — knowing where to add yours is critical:

| What | Where | Method |
|---|---|---|
| Repositories | `Explore.Persistence/PersistenceServicesRegistration.cs` | `ConfigurePersistenceServices()` |
| Application services | `Explore.Application/ApplicationServicesRegistration.cs` | `ConfigureApplicationServices()` |
| Infrastructure services | `Explore.Infrastructure/InfrastructureServicesRegistration.cs` | `ConfigureInfrastructureServices()` |
| API authentication | `Explore.API/Extensions/AuthenticationExtensions.cs` | `AddApiAuthentication()` |
| API caching | `Explore.API/Extensions/CachingExtensions.cs` | `AddApiCaching()` |
| API CORS | `Explore.API/Extensions/CorsExtensions.cs` | `AddApiCors()` |
| API rate limiting | `Explore.API/Extensions/RateLimitingExtensions.cs` | `AddApiRateLimiting()` |
| MediatR handlers | Auto-registered via `AddMediatR()` | Assembly scanning |
| AutoMapper profiles | Auto-registered via `AddAutoMapper()` | Assembly scanning (10 profile files) |
| Blazor WASM services | `Explore.Blazor.Client/Program.cs` | Direct registration |
| Blazor Server services | `Explore.Blazor/Program.cs` | Direct registration |
| Secrets | `Explore.Secrets/Extensions/ServiceCollectionExtensions.cs` | `AddSecrets()` |

---

## 17. Aspire Integration

The project uses **.NET Aspire** for local development orchestration:

- `Explore.AppHost` — Defines the service topology (API + Blazor + PostgreSQL + dependencies)
- `Explore.ServiceDefaults` — Shared configuration (OpenTelemetry, health checks, resilience)
- Database connection is configured via Aspire's `AddNpgsqlDbContext` in the API project, while `PersistenceServicesRegistration` uses `AddPooledDbContextFactory` for production

The Aspire integration is intentionally **not** in the Persistence project to avoid an ASP.NET Core dependency in the persistence layer.

---

## 18. Migration Service

`Event.MigrationService` is a **worker service** that applies EF Core migrations at startup:
- Runs as a separate process in the Aspire topology
- Applies pending migrations to the database
- Calls `DatabaseSeeder` to ensure seed data exists
- Exits after completion

This decouples migration execution from the API startup, allowing the API to assume the database is ready.

---

## 19. Image Storage Flow

Images are stored in **S3-compatible object storage** (Hetzner):

1. Client requests a presigned upload URL from the API (`StorageObjectController`)
2. API generates presigned URL via `ObjectStorageService` and creates a `StorageObject` record
3. Client uploads directly to S3 using the presigned URL (bypasses BFF)
4. `S3Image.razor` component renders images using the S3 URL
5. `StorageObject` entity tracks: bucket, key, content type, size, and owner (via `OwnerType`)

---

## 20. Organization Review System

Organization reviews use a slightly different CQRS structure:

- `OrganizationReviews/Commands/` and `OrganizationReviews/Queries/` (flattened, no `Handlers/Requests` split)
- This is the only feature that uses this alternative structure — all others use the standard nested pattern

---

## 21. ABOUTME File Comments

Every file should start with a two-line comment:
```
// ABOUTME: Brief description of what this file does.
// ABOUTME: Second line with additional context.
```

Not all files have this yet, but it's the convention. Add it when modifying a file.

---

## 22. Client-Side Lookup Caching

`LookupCacheService` caches lookup table data (EventTypes, AudienceAges, Languages, etc.) in the WASM client to avoid repeated API calls. When adding a new lookup table:

1. Add the API call to the relevant service
2. Add caching in `LookupCacheService`
3. Use the cached version in page components

---

## 23. Route Guards (Blazouter)

The Blazor client uses **Blazouter** library for client-side routing with guards:

- `AuthenticatedRouteGuard` — Requires authentication
- `AdminRouteGuard` — Requires admin role

Guards are registered in `Program.cs` and applied via Blazouter's routing configuration.

---

## 25. Specification Pattern Implementation

The application uses a custom **Specification Pattern** to handle complex filtering logic, particularly for the `Event` entity which has modular aspects.

### Core Components
- **`IQuerySpecification<T>`**: Interface defining the contract for query specifications.
- **`EventQuerySpecification`**: The main specification for event retrieval. It orchestrates sub-filters.
- **`IFilterSpecification<T>`**: Interface for individual filter components.

### Modular Filtering Strategy
Instead of a massive `Where()` clause in the repository, filters are broken down into small, reusable classes:

1.  **`EventFilter`**: Handles core fields (Date, Location, Category).
2.  **`AspectPresenceFilter`**: Handles "HasIslamicAspect" / "HasTechAspect" flags.
3.  **`IslamicAspectFilter`**: Handles Islamic-specific fields (Madhab, GenderMode).
4.  **`TechAspectFilter`**: Handles Tech-specific fields (SkillLevel, Stack).

The `EventQuerySpecification` applies these filters sequentially to the `IQueryable<Event>`. This allows module-specific filters to be applied only when relevant.

---

## 26. Hybrid Caching Strategy (L1 + L2)

We use **.NET 9+ HybridCache** for application-level caching, which provides L1 (In-Memory) + L2 (Redis) caching with built-in stampede protection.

### Usage Pattern
HybridCache is injected into **MediatR Handlers**, not Controllers.

**Read-Through (Query Handlers):**
```csharp
return await _cache.GetOrCreateAsync(
    key: $"event:{request.Id}",
    factory: async cancel => await _repo.GetById(request.Id),
    options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) }
);
```

**Invalidation (Command Handlers):**
```csharp
// In Create/Update/Delete handlers
await _cache.RemoveAsync($"event:{request.Id}");
```

**Key Distinction:**
- **OutputCache**: Caches the *HTTP response* (Controllers). Good for anonymous public lists.
- **HybridCache**: Caches the *Domain Entity/DTO* (Handlers). Good for shared data, authenticated views, and internal logic.

---

## 27. Validation Architecture

### The "Manual Instantiation" Rule
Validators are **never** injected via DI. They are manually instantiated in the Handler.

**Why?**
Our validators often require database access (e.g., "Does this CategoryId exist?", "Is this User an Admin?"). Injecting Repositories into Validators via DI can cause lifetime issues and circular dependencies if not careful.

**The Pattern:**
```csharp
public class CreateEventCommandHandler : IRequestHandler<...>
{
    public CreateEventCommandHandler(
        IEventRepository eventRepo,
        IOrganizationRepository orgRepo) // Inject repos into Handler
    { ... }

    public async Task<Response> Handle(...)
    {
        // Pass repos to Validator constructor manually
        var validator = new CreateEventDtoValidator(_eventRepo, _orgRepo);
        var result = await validator.ValidateAsync(request.Dto);
        // ...
    }
}
```

This ensures the validator uses the same repository instances (and DbContext) as the handler transaction.

---

## 28. API Middleware Pipeline Order

The middleware order in `Program.cs` is **critical** — changing it will break behavior. The exact order:

1. `UseApiExceptionHandling()` — Must be first to catch all downstream errors.
2. `UseForwardedHeaders()` — applies trusted `X-Forwarded-*` values before host-based tenant resolution.
3. `UseSecurityHeaders()` — Adds defensive headers before any response.
4. `UseCorrelationId()` — Generates/reads correlation ID for log context.
5. `UseRequestLogging()` — Structured logging with correlation ID, user ID, tenant ID.
6. `UseResponseCompression()` — Brotli + Gzip before response leaves.
7. `UseHttpsRedirection()` — Redirects HTTP to HTTPS.
8. `UseHateoas()` — RFC 7240 Prefer header processing.
9. `UseRouting()` — Endpoint routing.
10. `UseMiddleware<ApiTenantResolutionMiddleware>()` — resolves tenant hint from `X-Tenant-Slug` or normalized host before auth.
11. `UseRequestTimeouts()` — 3-tier timeout enforcement (Default 30s, Lookup 10s, Complex 60s).
12. `UseMiddleware<ApiAuthenticationConflictMiddleware>()` — rejects conflicting auth inputs.
13. `UseAuthentication()` — JWT Bearer auth.
14. `UseMiddleware<ApiTenantPostAuthenticationMiddleware>()` — finalizes API-key tenant binding and mismatch handling.
15. `UseRequestLocalization()` — request culture selection.
16. `UseMiddleware<IdempotencyMiddleware>()` — idempotent replay for write requests.
17. `UseRateLimiter()` — 5-tier rate limiting.
18. `UseAuthorization()` — ASP.NET authorization.
19. `UseOutputCache()` — HTTP response caching.
20. `UseETag()` — SHA256 weak ETags, 304 Not Modified support.

**Why order matters:** forwarded headers must run before host-based tenant resolution; API-key tenant selection is intentionally split across pre-auth and post-auth middleware; rate limiting runs after authentication so it can use API key IDs or `User.Identity.Name`; output cache comes after authorization so cached responses respect auth boundaries; ETag is last because it needs the final response body.

---

## 29. Correlation ID Propagation

`CorrelationIdMiddleware` provides distributed tracing context:

1. Reads `X-Correlation-ID` or `X-Request-ID` from incoming request headers.
2. If neither exists, generates a new UUID.
3. Pushes the value into Serilog `LogContext` as `CorrelationId` property.
4. All subsequent log entries in the request pipeline include this correlation ID.
5. Enables tracing a single request across API → BFF → background services.

---

## 30. CORS Policy Architecture

Five CORS policies handle different trust levels:

| Policy | Use Case | Key Behavior |
|---|---|---|
| `InternalAppPolicy` | BFF ↔ API | Configurable origins, all methods, credentials allowed |
| `ExternalAppPolicy` | External API consumers | Configurable origins, specific methods, no credentials |
| `InternalWebsitePolicy` | `iloveibadah.app` | Single-origin, all methods, credentials |
| `ExternalWebsitePolicy` | External read-only | Configurable origins, GET/OPTIONS only |
| `DevPolicy` | Development only | All origins, all methods, credentials |

---

## 31. Business Metrics (OpenTelemetry)

`BusinessMetrics` class exposes counters under the `Explore.Business` meter. All counters are tagged with `tenant_id` and `resource_type` dimensions.

| Counter | Tracks |
|---|---|
| `events.created` | Event creation |
| `events.published` | Event publication |
| `registrations.created` | Event registrations |
| `organizations.created` | Organization creation |
| `authorization.decisions` | Authorization check outcomes |

These integrate with Prometheus scraping via the `/metrics` endpoint.

---

## 32. Graceful Shutdown Mechanics

API implements production-grade graceful shutdown:

1. **SIGTERM** triggers a 25-second grace period.
2. Health checks immediately return `503` (unhealthy) during shutdown for load balancer draining.
3. **SIGINT** triggers immediate shutdown (development).
4. `Kestrel.KeepAliveTimeout` and `Host.ShutdownTimeout` both set to 30 seconds.
5. In-flight requests are given time to complete before process exit.

---

## 33. SetupSecret Filter Pattern

The `SetupSecretRequiredAttribute` uses `TypeFilterAttribute` for DI-aware action filtering:

1. It wraps `SetupSecretRequiredFilter` which receives `ISetupSecretProvider` via DI.
2. If setup mode is inactive → returns `410 Gone` (setup already completed).
3. If `X-Setup-Secret` header is missing or invalid → returns `403 Forbidden`.
4. This pattern allows action filter attributes to use DI services without constructor injection.

---

## 34. API Versioning Strategy

API versioning uses **media-type strategy** (not URL segments):

- Version parameter: `v` in the `Accept` header.
- Example: `Accept: application/json;v=0.1` or `application/hal+json;v=0.1`.
- Default version: `0.1` when unspecified.
- Clean URLs — no `/v1/` or `/v2/` path segments.
- Reported in response headers via `Asp.Versioning` middleware.

---

## 35. Analytics Abstraction Is Already Runtime-Selectable

The analytics system is not a future concept anymore; it already follows the same runtime-provider pattern as localization.

### Actual shape

- `IAnalyticsProvider` is the lowest-common-denominator contract.
- `RuntimeAnalyticsProvider` routes to the active provider at runtime.
- `AnalyticsConfigResolver` resolves provider config from governance settings with tenant-aware cascade and short-lived cache.
- `NullAnalyticsProvider` is a first-class safe fallback, not an error path.

### Current capability reality

- `PostHog` is the richest provider today: track, pageview, identify, group identify, feature flags.
- `Plausible` is intentionally lightweight by product design, not by incomplete integration: track/pageview only; identify/group calls safely no-op.
- `Rybbit` currently behaves as a browser-richer but server-thinner provider: the repo implements track/pageview only on the server, while official docs show browser-side identify but no documented server-side parity, groups, or feature flags.
- `RudderStack` is implemented across track/pageview/identify/group, but its product category is different: it behaves more like a CDP/router than a first-party analytics backend, so rollout guarantees should be framed carefully.
- Feature flags are effectively `PostHog`-only in the current abstraction. Other providers degrade to safe false/null defaults.

### Non-intuitive rule

Do not assume every provider supports the same semantics. The abstraction is designed around safe degradation, not fake parity.

### Canonical governance keys

- `analytics.provider`
- `analytics.enabled`
- `analytics.api_key`
- `analytics.endpoint_url`
- `analytics.personal_api_key`

`analytics.endpoint_url` is canonical. `analytics.endpoint` and `analytics.site_id` are legacy drift, not valid runtime contract keys.
