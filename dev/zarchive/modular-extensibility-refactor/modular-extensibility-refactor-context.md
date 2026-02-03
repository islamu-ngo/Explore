# Modular Extensibility & Governance Refactor - Context

> **Purpose:** Key information for resuming work on the modular extensibility refactor
>
> **Last Updated:** 2026-01-30

---

## PROJECT OVERVIEW

Transforming the Explore API from a fixed-schema event platform into a composition-based container system supporting any event type (Islamic, Tech, Medical, etc.) via "Aspects" and "Cascading Policies".

---

## SESSION PROGRESS (2026-01-30)

### COMPLETED

#### Phase 6: PDS Synchronization (Outbox Pattern) - COMPLETED
- Created `PdsSyncOutbox.cs` entity with Status, Operation, RetryCount fields
- Created `PdsSyncOperation` and `PdsSyncStatus` enums
- Created `PdsSyncOutboxConfiguration.cs` with optimized indexes for worker polling
- Added `DbSet<PdsSyncOutbox>` to ExploreDbContext
- Created `IPdsService` interface with Create/Update/Delete record operations
- Created `PdsOperationResult` class with success/retry logic
- Created `PdsService` implementation with AT Protocol HTTP API calls
- Created `PdsSyncSettings` configuration class
- Created `IPdsSyncOutboxRepository` interface
- Created `PdsSyncOutboxRepository` with optimistic locking for worker
- Created `PdsSyncWorker` background service with exponential backoff
- Registered all services in DI container

#### Phase 7: Virtual Tenant Masking - COMPLETED
- Created `DeploymentSettings` configuration class
- Created `DeploymentMode` enum (SingleTenant, MultiTenant)
- Updated `TenantContext` to respect deployment mode
- Added subdomain resolution for multi-tenant mode
- Created `BlockInSingleTenantAttribute` authorization filter
- Created `RequireMultiTenantAttribute` authorization filter
- Registered DeploymentSettings in DI container

### Pre-existing Build Issues (Not Part of This Refactor)
- HATEOAS implementation (Phase 5) has incomplete code:
  - Missing `GetModuleEnabledSetting` method in ISettingsResolver
  - `GetCurrentEndpointRenderPolicy` signature mismatch
  - These are in files: HateoasLinkBuilderFactory.cs, HypermediaAssembler.cs, PolicyAwareLinkBuilder.cs

#### Phase 1: Foundation
- Created `GuidVersion7ValueGenerator.cs` for application-side UUID v7 generation
- Created `QueryFilterNames.cs` constants for named query filters
- Created `QueryFilterExtensions.cs` extension methods
- Refactored `ExploreDbContext.ApplyGlobalQueryFilters()` to use EF Core 10 named filters

#### Phase 2: Settings Engine
- Created `SystemSetting.cs` entity with locking capability
- Created `TenantSetting.cs` entity for tenant overrides
- Created `ISettingsResolver` interface and `SettingsResolver` implementation
- Created repository interfaces and implementations
- Added seed IDs to `SeedIds.cs` (500-504 range)

#### Phase 3: Aspect Infrastructure
- Created `EventIslamicAspect.cs` with PrayerTime and GenderSegregationMode enums
- Created `EventTechAspect.cs` with SkillLevel enum
- Created EF configurations using shared primary key pattern
- Updated `Event.cs` with navigation properties and MetadataJson
- Created aspect DTOs and updated MappingProfile
- Configured `MetadataJson` as PostgreSQL `jsonb` column type

#### Phase 4: Module Governance
- Created `ModuleDefinition.cs` entity
- Created `TenantCapability.cs` entity (implements ITenantEntity)
- Created EF configurations with seed data (Core, Islamic, Tech modules)
- Added module seed IDs to `SeedIds.cs` (600-611 range)
- Created `IModuleDefinitionRepository` and `ITenantCapabilityRepository`
- Created repository implementations
- Created `IModuleService` interface and `ModuleService` implementation
- Created `ModuleController` with discovery endpoints
- All 88 tests passing

### IN PROGRESS
- None

### NOT STARTED
- All phases completed!

#### Phase 5: Strategy Pattern (COMPLETED)
- Created `IEventStrategy` interface with validation, post-create/update, and HATEOAS link methods
- Created `IStrategyResolver` interface for orchestrating strategy selection
- Implemented `IslamicEventStrategy` with prayer time and gender mode validation
- Implemented `TechEventStrategy` with GitHub URL and skill level validation
- Implemented `StrategyResolver` with module-aware strategy selection
- Registered all strategies in DI container
- Unit tests passing (24/24)

### BLOCKERS
- None identified

---

## KEY DECISIONS MADE

### 1. UUID v7 Strategy
Use PostgreSQL's `uuidv7()` for database-side generation (PG 18+) or application-side with `Guid.CreateVersion7()` and value converter.

```csharp
// PostgreSQL 18+
builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

// Application-side
builder.Property(e => e.Id).HasConversion(new GuidVersion7Converter());

internal class GuidVersion7Converter(ConverterMappingHints? mappingHints = null)
    : ValueConverter<Guid, Guid>(guid => EnsureVersion7(guid), guid => guid, mappingHints)
{
    private static Guid EnsureVersion7(Guid guid) 
        => guid == Guid.Empty ? Guid.CreateVersion7() : guid;
}
```

### 2. Named Query Filters (EF Core 10+)
Use `HasQueryFilter(name: "SoftDelete", predicate)` for selective disabling.

```csharp
modelBuilder.Entity<Event>()
    .HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted)
    .HasQueryFilter(name: "TenantFilter", predicate: e => e.TenantId == tenantId);

// Selective disable
await context.Events.IgnoreQueryFilter("SoftDelete").ToListAsync();
```

### 3. Aspect Pattern (1:1 Shared PK)
Aspect table PK is also FK to parent entity.

```csharp
public class EventIslamicAspect
{
    public Guid Id { get; set; }  // PK and FK to Event.Id
    public int? MadhabId { get; set; }
    public int? PrayerTimeOffset { get; set; }
    // ... Islamic-specific fields
}

// Configuration
builder.HasKey(e => e.Id);
builder.HasOne<Event>()
       .WithOne(e => e.IslamicAspect)
       .HasForeignKey<EventIslamicAspect>(a => a.Id);
```

### 4. Outbox Pattern
EF Core SaveChangesInterceptor captures domain events, background worker publishes to PDS.

```csharp
public class PdsSyncOutbox
{
    public Guid Id { get; set; }
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string OperationType { get; set; } // Create, Update, Delete
    public string Payload { get; set; } // JSON
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}
```

### 5. Strategy Pattern
Request-scoped strategy selection based on TenantContext for modular business logic.

---

## KEY FILES TO UNDERSTAND

### Domain Layer (Explore.Domain)

| File | Purpose |
|------|---------|
| `Explore.Domain/Event.cs` | Core event entity with MadhabId (to be extracted to aspect) |
| `Explore.Domain/Actor.cs` | ATProto identity (DID, Handle, PdsHost) |
| `Explore.Domain/Madhab.cs` | Islamic jurisprudence lookup table |
| `Explore.Domain/Tenant.cs` | Multi-tenant entity |
| `Explore.Domain/TenantSettings.cs` | Tenant configuration |
| `Explore.Domain/Interfaces/ITenantEntity.cs` | Tenant isolation interface |
| `Explore.Domain/Interfaces/IAuditableEntity.cs` | Audit fields interface |
| `Explore.Domain/Interfaces/ISoftDeletable.cs` | Soft delete interface |

### Persistence Layer (Explore.Persistence)

| File | Purpose |
|------|---------|
| `Explore.Persistence/ExploreDbContext.cs` | Main DbContext with global query filters |
| `Explore.Persistence/Configurations/` | EntityTypeConfiguration classes |

### Application Layer (Explore.Application)

| File | Purpose |
|------|---------|
| `Explore.Application/Features/Events/` | Event CQRS handlers |
| `Explore.Application/DTOs/Event/` | Event DTOs and validators |

### Documentation

| File | Purpose |
|------|---------|
| `docs/ARCHITECTURE.md` | System architecture overview |
| `docs/EXTENSIBILITY.md` | Aspect architecture documentation |
| `docs/MULTI_TENANCY.md` | Multi-tenant patterns |
| `docs/MODULAR_EVENTS.md` | Modular event system |
| `docs/FEDERATION.md` | ATProto/ActivityPub federation |

---

## CURRENT ARCHITECTURE SNAPSHOT

### Domain Layer
- Event.cs - Main event entity with MadhabId (to be extracted)
- Actor.cs - ATProto identity (DID, Handle, PdsHost)
- Tenant.cs, TenantSettings.cs - Multi-tenancy foundation
- Interfaces: ITenantEntity, IAuditableEntity, ISoftDeletable

### Application Layer
- CQRS via MediatR
- Features organized by entity (Features/Events/, Features/Organizations/, etc.)
- DTOs with validators (manual instantiation pattern)

### Infrastructure Layer
- ExploreDbContext with global query filters
- EntityTypeConfiguration classes
- Repository implementations

### API Layer
- Thin controllers delegating to MediatR
- HATEOAS support
- Keycloak + ATProto OAuth authentication

---

## CONSTRAINTS & GUIDELINES

1. **Repositories return entities, never DTOs** - Map in handlers
2. **Validators use manual instantiation** - Not DI
3. **Navigation properties readonly for writes** - Use repository for explicit writes
4. **Use `int` for lookup IDs, `Guid` for main entities**
5. **No default values in entities** - Set in handlers
6. **Commands return BaseCommandResponse<T>**
7. **GET = AllowAnonymous, Write = Authorize**
8. **File-scoped namespaces** - `namespace Explore.Domain;`
9. **Named query filters for soft delete** - EF Core 10+
10. **Auditing fields required** - CreatedAt, CreatedBy, UpdatedAt, UpdatedBy

---

## IMPLEMENTATION PHASES SUMMARY

| Phase | Focus | Status |
|-------|-------|--------|
| Phase 1 | Foundation (UUID v7, Named Query Filters) | COMPLETED |
| Phase 2 | Settings Engine | COMPLETED |
| Phase 3 | Aspect Infrastructure | COMPLETED |
| Phase 4 | Module Governance | COMPLETED |
| Phase 5 | Strategy Pattern | COMPLETED |
| Phase 6 | PDS Synchronization (Outbox Pattern) | COMPLETED |
| Phase 7 | Virtual Tenant Masking | COMPLETED |

---

## QUICK RESUME

To continue this refactor:
1. Read this context file for decisions and key files
2. Read `modular-extensibility-refactor-plan.md` for detailed phases and acceptance criteria
3. Read `modular-extensibility-refactor-tasks.md` for checklist of specific tasks (when created)
4. Start with Phase 1 (Foundation) - UUID v7 and named query filters

---

## RELATED DOCUMENTATION

- [Implementation Plan](./modular-extensibility-refactor-plan.md)
- [Tasks Checklist](./modular-extensibility-refactor-tasks.md) (to be created)
- [Architecture](../../docs/ARCHITECTURE.md)
- [Extensibility](../../docs/EXTENSIBILITY.md)
- [Multi-Tenancy](../../docs/MULTI_TENANCY.md)
