# Codebase Insights

> Non-intuitive patterns, hidden knowledge, and things requiring deep analysis.
> This document captures what you cannot guess from reading ARCHITECTURE.md alone.
> Last Updated: February 2026

---

## 1. Multi-Tenancy Deep Mechanics

### How Tenant Isolation Actually Works

Multi-tenancy is **not middleware-based** — it uses EF Core global query filters injected via the DbContext. The flow:

1. **TenantContext** (API service) resolves the tenant from the HTTP request
2. **ExploreDbContext** receives TenantContext via **property injection** (not constructor injection) because the DbContext uses pooling
3. **Named query filters** on every tenant-scoped entity automatically filter by `TenantId`
4. The filter expression is: `TenantContext == null || e.TenantId == TenantContext.TenantId` — the null check allows migrations and seeding to bypass tenant filtering

### Tenant Resolution Priority

TenantContext resolves the tenant in this order:
1. `X-Tenant-Id` HTTP header (explicit selection)
2. Custom domain lookup (checks `TenantSetting` for matching domain)
3. Subdomain extraction (extracts from host, looks up tenant by subdomain or slug)
4. Default tenant (from configuration or hardcoded fallback)

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

## 2. DbContext Pooling and Property Injection

The `ExploreDbContext` uses **pooled DbContext factory** for performance. This has a critical implication:

- The constructor **cannot** accept scoped services (TenantContext, CurrentUserService)
- Instead, these are set via **property injection** after the context is created from the pool
- The scoped registration in `PersistenceServicesRegistration.cs` creates the context from the factory, then sets the properties

This means:
- `TenantContext` and `CurrentUserService` can be `null` (during migrations, seeding, or background services)
- All query filters and audit logic must handle null gracefully
- If you add a new scoped dependency to DbContext, it must follow this property injection pattern

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

The API implements **HAL (Hypertext Application Language)** for hypermedia:

- `HalResource<T>` wraps response DTOs with `_links` section
- `HalCollectionResource<T>` wraps lists with pagination links
- `ResourceAssemblerBase<T>` generates links per entity type
- `HateoasLinkGenerator` resolves URLs from route names
- OpenAPI schema is transformed by `HalSchemaTransformer` to show HAL structure

The `PreferHeaderMiddleware` handles content negotiation: clients can request HAL format via `Prefer: return=representation` header.

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

## 11. Settings Resolution Chain

The governance settings system has a three-tier resolution:

1. **SystemSetting** (instance-wide defaults) — e.g., "require event approval = true"
2. **TenantSetting** (per-tenant overrides) — e.g., Tenant A overrides to "false"
3. **PlatformDefaults** (code-level fallback) — if neither DB value exists

`SettingsResolver` implements this cascade. Settings values are stored as **JSON-serialized strings** in the database. The `TenantContext.cs` file shows the deserialization pattern: `JsonSerializer.Deserialize<T>(rawValue)` with fallback to raw parsing.

### GovernanceSettingKeys

Setting keys are string constants in `GovernanceSettingKeys.cs`. They use dot-separated hierarchical naming:
- `Deployment.Mode`
- `Events.MaxSessionsPerEvent`
- `Modules.Islamic.Enabled`
- `Domains.InstanceBaseDomain`
- `Domains.AllowTenantCustomDomain`

---

## 12. AutoMapper Profile Organization

All mappings live in a **single file**: `Explore.Application/Profiles/MappingProfile.cs`. This file maps every entity to every DTO. When adding a new entity/DTO pair, the mapping goes here — not in a separate profile file.

---

## 13. API Client Generation

The Blazor client uses an **NSwag-generated API client**:

- `EventApiClient.cs` — Hand-written interface (`IEventApiClient`) with additional methods
- `EventApiClient.g.cs` — Auto-generated code from `swagger.json`
- The generated client is registered in WASM `Program.cs` with the BFF HttpClient

To regenerate: update `swagger.json` from the API, then run the NSwag generator.

---

## 14. Exception Handling Flow

### API Side
`ExceptionMiddleware` catches all unhandled exceptions and maps them to HTTP responses:
- `NotFoundException` → 404
- `BadRequestException` → 400
- `ValidationException` → 400 with error details
- Unhandled → 500

### Command Response Pattern
Commands don't throw for validation failures. They return `BaseCommandResponse<TKey>`:
- `Success = true/false`
- `Errors` list with validation messages
- Controllers return `Ok(response)` or `BadRequest(response)` based on `Success`

### Client Side
`ServiceResult<T>` wraps API responses in the Blazor client, providing a consistent error handling pattern.

---

## 15. BlockInSingleTenant Filter

The `BlockInSingleTenantAttribute` action filter prevents certain endpoints from being called in single-tenant mode. This is used for multi-tenant management endpoints that don't make sense in single-tenant deployments.

---

## 16. DI Registration Locations

Services are registered in multiple places — knowing where to add yours is critical:

| What | Where | Method |
|---|---|---|
| Repositories | `Explore.Persistence/PersistenceServicesRegistration.cs` | `CongfigurePersistenceServices()` |
| Infrastructure services | `Explore.Infrastructure/InfrastructureServicesRegistration.cs` | `ConfigureInfrastructureServices()` |
| API-layer services | `Explore.API/Program.cs` | Direct registration |
| MediatR handlers | Auto-registered via `AddMediatR()` in `Program.cs` | Assembly scanning |
| AutoMapper profiles | Auto-registered via `AddAutoMapper()` | Assembly scanning |
| Blazor WASM services | `Explore.Blazor.Client/Program.cs` | Direct registration |
| Blazor Server services | `Explore.Blazor/Program.cs` | Direct registration |
| Secrets | `Explore.Secrets/Extensions/ServiceCollectionExtensions.cs` | `AddSecrets()` |

**Note:** There is a typo in `CongfigurePersistenceServices` (missing `i` in "Configure"). This is existing and intentional — do not rename it without updating all call sites.

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

## 24. Key Things That Surprise Newcomers

1. **Main projects use "Explore.\*" prefix** — This is historical naming. Test projects already use the "Event.\*" prefix. Eventually all projects will be renamed to "Event.\*".

2. **`CongfigurePersistenceServices`** — The method name has a typo. It's intentional (changing it would be a breaking refactor).

3. **`TenantSetting` vs `TenantSettings`** — Two different entities with similar names, different purposes.

4. **Navigation properties are read-only for writes** — Never do `parent.Children.Add()`. Always use the repository's `Create()` method for the child entity. This ensures tenant isolation.

5. **Validators are manually instantiated** — Never inject validators via DI. Always `new` them in the handler with repository dependencies as constructor parameters.

6. **Database columns are snake_case** — Even though C# properties are PascalCase. `UseSnakeCaseNamingConvention()` handles this automatically.

7. **Not all entities have soft delete** — Only entities implementing `ISoftDeletable`. Lookup tables and junction tables typically don't.

8. **SystemSetting values are JSON-serialized** — Even simple strings are stored as `"\"value\""` (JSON string). Use `JsonSerializer.Deserialize<T>()` to read them.

9. **The Blazor Server host is also the BFF** — It doesn't just serve the WASM app. It proxies all API requests and handles authentication.

10. **MediatR handlers are auto-discovered** — You don't register them manually. They're found by assembly scanning via `AddMediatR()`. Just create the handler class in the right namespace.
