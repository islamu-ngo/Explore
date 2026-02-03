# MODULAR EXTENSIBILITY & GOVERNANCE REFACTOR - Context

> **Key Information for Resuming Work**
>
> This document tracks progress, decisions, and key files for the modular extensibility refactor.
>
> **Created**: 2026-01-30
> **Last Updated**: 2026-01-30

---

## AUTHORITATIVE SOURCE

**Primary Plan**: [refactor-api-high-flexibility-implementation.md](../refactor-api-high-flexibility-implementation.md)

This context file supports the implementation of the architectural vision defined in the authoritative source.
Always refer to the primary plan for strategic decisions and overall direction.

---

## SESSION PROGRESS (2026-01-30)

### ✅ COMPLETED

**Phase 1: Foundation (UUID v7 + Named Query Filters)**
- Created `GuidVersion7ValueGenerator` in `Explore.Persistence/ValueGenerators/`
- Created `QueryFilterNames` constants in `Explore.Persistence/QueryFilters/`
- Created `QueryFilterExtensions` extension methods
- Refactored `ExploreDbContext.ApplyGlobalQueryFilters()` to use EF Core 10+ named filters
- Split combined tenant+soft-delete filters into separate named filters

**Phase 2: Cascading Settings Engine**
- Created `SystemSetting` entity with locking capability
- Created `TenantSetting` entity for tenant overrides
- Created EF configurations for both entities
- Created `ISettingsResolver` interface with cascading resolution
- Implemented `SettingsResolver` service with caching
- Added DbSet properties and query filters to DbContext

**Phase 3: Aspect Infrastructure**
- Created `EventIslamicAspect` entity with Madhab, prayer scheduling, gender mode
- Created `EventTechAspect` entity with hackathon, skill level, tech stack
- Created EF configurations using shared primary key pattern (1:1)
- Updated `Event` entity with navigation properties and MetadataJson
- Created aspect DTOs (`EventIslamicAspectDto`, `EventTechAspectDto`)
- Updated `EventDto` with AvailableAspects and aspect properties
- Added AutoMapper mappings for aspects

### 🟡 IN PROGRESS
- Nothing currently in progress

### ⏳ NOT STARTED
- Phase 4: Module Governance (ModuleDefinition, TenantCapability)
- Phase 5: Strategy Pattern (IEventStrategy, IslamicSchedulingStrategy)
- Phase 6: PDS Synchronization (Outbox pattern, background worker)
- Phase 7: Virtual Tenant Masking (deployment modes)

### ⚠️ BLOCKERS
- None identified

## Build Status
- All core projects compile successfully
- 58 unit tests passing
- Pre-existing HATEOAS issues in Explore.API (unrelated to this refactor)

---

## Quick Resume

To continue this work:
1. Read this file for current state
2. Check `tasks.md` for next actionable items
3. Refer to `plan.md` for detailed implementation guidance
4. Start with Phase 1 (UUID v7 Infrastructure) after plan approval

---

## Key Architecture Decisions

### Decision 1: UUID v7 Generation Strategy
**Decision**: Use application-side generation with `Guid.CreateVersion7()` via EF Core ValueGenerator

**Rationale**:
- .NET 9+ provides native UUID v7 support
- Npgsql 9.0+ generates v7 by default for Guid PKs
- Application-side avoids extra database roundtrip for generated IDs
- Works with PostgreSQL <18 which doesn't have `uuidv7()` function

**Alternative Considered**: Database-side with `uuidv7()` (PostgreSQL 18+)
- Rejected because: Requires PostgreSQL 18+ upgrade

### Decision 2: Aspect Table Pattern
**Decision**: Use shared primary key (1:1 relationship) where aspect PK = Event PK

**Rationale**:
- Same ID for Event and its aspects simplifies queries
- Natural 1:1 relationship (one event, one Islamic aspect)
- Follows existing documentation in EXTENSIBILITY.md
- Efficient joins via PK lookup

**Alternative Considered**: Separate auto-generated PK with EventId FK
- Rejected because: Adds unnecessary column, complicates joins

### Decision 3: Settings Cascade Resolution
**Decision**: Three-tier cascade: System (lockable) → Tenant → Default

**Rationale**:
- Matches existing MULTI_TENANCY.md documentation
- Locked settings enforce instance-level policies
- Tenant overrides for customization
- System defaults as fallback

### Decision 4: Named Query Filters (EF Core 10)
**Decision**: Use named filters for SoftDelete and TenantFilter separately

**Rationale**:
- EF Core 10 feature allows selective disabling
- Admin operations can disable SoftDelete while keeping TenantFilter
- Cleaner than combined filter expression

**Pattern**:
```csharp
builder.HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted);
builder.HasQueryFilter(name: "TenantFilter", predicate: e => e.TenantId == tenantId);
```

### Decision 5: PDS Sync Mechanism
**Decision**: Outbox pattern with EF Core SaveChanges interceptor

**Rationale**:
- Atomic capture of domain events in same transaction
- Background worker for reliable delivery
- Retry logic for transient failures
- Decouples sync from request/response cycle

---

## Key Files Reference

### Domain Layer

**Explore.Domain/Event.cs**
- Core event entity (to be refactored)
- Remove domain-specific fields (MadhabId)
- Add navigation properties for aspects
- Current location of all event properties

**Explore.Domain/Actor.cs**
- Federated identity entity
- Contains Did, Handle, ActorType
- Links to UserExternalLogin for auth

**Explore.Domain/ (NEW FILES)**
- `Settings/SystemSetting.cs` - Instance-level settings
- `Settings/TenantSetting.cs` - Tenant overrides
- `Aspects/EventIslamicAspect.cs` - Islamic event aspect
- `Aspects/EventTechAspect.cs` - Tech event aspect
- `Modules/ModuleDefinition.cs` - Module metadata
- `Modules/TenantCapability.cs` - Module-tenant link
- `Federation/PdsSyncOutbox.cs` - Outbox messages

### Persistence Layer

**Explore.Persistence/ExploreDbContext.cs**
- Main DbContext
- Add new DbSets for aspects, settings, modules
- Configure named query filters

**Explore.Persistence/Configurations/ (NEW FILES)**
- `Settings/SystemSettingConfiguration.cs`
- `Settings/TenantSettingConfiguration.cs`
- `Aspects/EventIslamicAspectConfiguration.cs`
- `Aspects/EventTechAspectConfiguration.cs`
- `Modules/ModuleDefinitionConfiguration.cs`
- `Modules/TenantCapabilityConfiguration.cs`
- `Federation/PdsSyncOutboxConfiguration.cs`

**Explore.Persistence/Interceptors/ (NEW)**
- `OutboxInterceptor.cs` - Captures domain events to outbox

### Application Layer

**Explore.Application/Contracts/Persistence/ (NEW FILES)**
- `ISystemSettingRepository.cs`
- `ITenantSettingRepository.cs`
- `IModuleRepository.cs`
- `IPdsSyncOutboxRepository.cs`

**Explore.Application/Services/ (NEW FILES)**
- `ISettingsResolver.cs` - Cascade resolution interface
- `SettingsResolver.cs` - Implementation
- `IModuleService.cs` - Module availability
- `ModuleService.cs` - Implementation
- `IStrategyResolver.cs` - Strategy pattern interface

**Explore.Application/DTOs/Aspects/ (NEW)**
- `EventIslamicAspectDto.cs`
- `EventTechAspectDto.cs`
- `CreateEventIslamicAspectDto.cs`
- `CreateEventTechAspectDto.cs`

**Explore.Application/Features/Events/**
- Update handlers to include aspects
- Add aspect-specific CQRS commands/queries

### Infrastructure Layer

**Explore.Infrastructure/ValueGenerators/ (NEW)**
- `UuidV7ValueGenerator.cs` - UUID v7 generation

**Explore.Infrastructure/Strategies/ (NEW)**
- `IslamicSchedulingStrategy.cs` - Prayer-based scheduling
- `DefaultSchedulingStrategy.cs` - Standard scheduling

**Explore.Infrastructure/Services/Federation/ (NEW)**
- `IPdsService.cs` - PDS communication interface
- `PdsService.cs` - Implementation

### API Layer

**Explore.API/Controllers/**
- `SettingsController.cs` - Settings endpoints (NEW)
- `ModulesController.cs` - Module discovery (NEW)
- `EventController.cs` - Update for aspects

**Explore.API/BackgroundServices/ (NEW)**
- `OutboxProcessorService.cs` - Background worker

**Explore.API/Middleware/**
- Update TenantContext for deployment modes

---

## Research Notes

### UUID v7 in .NET

**Native Support (.NET 9+)**:
```csharp
Guid.CreateVersion7()  // Produces UUID v7
```

**Npgsql 9.0+ Default Behavior**:
- Client-side Guid generation for PKs uses UUID v7 automatically
- No configuration needed for basic usage

**PostgreSQL 18+ Native**:
```sql
SELECT uuidv7();  -- Native function
```

**For PostgreSQL <18**:
- Use `pg_uuidv7` extension OR
- Generate client-side (recommended for this project)

**Potential Issue**: `Guid.CreateVersion7()` stores bytes in non-big-endian order which may still cause some index fragmentation. For maximum performance, consider `UUIDNext` library with PostgreSQL-optimized byte ordering.

### EF Core 10 Named Query Filters

**Define Multiple Filters**:
```csharp
modelBuilder.Entity<Blog>()
    .HasQueryFilter(name: "SoftDelete", predicate: b => !b.IsDeleted)
    .HasQueryFilter(name: "TenantFilter", predicate: b => b.TenantId == tenantId);
```

**Disable Specific Filter**:
```csharp
var allBlogs = await _context.Blogs
    .IgnoreQueryFilter("SoftDelete")  // TenantFilter still applies
    .ToListAsync();
```

**Disable All Filters**:
```csharp
var allBlogs = await _context.Blogs
    .IgnoreQueryFilters()  // No filters
    .ToListAsync();
```

### Outbox Pattern Implementation

**Key Components**:
1. **OutboxMessage Entity** - Stores pending messages
2. **SaveChanges Interceptor** - Captures domain events atomically
3. **Background Worker** - Processes outbox entries
4. **Retry Logic** - Handles transient failures

**Best Practices**:
- Index on `ProcessedAt IS NULL` for efficient querying
- Limit batch size in processor
- Use idempotency keys for external calls
- Log failures with context for debugging

---

## Current Codebase Constraints

### From GOVERNANCE.md

1. Repositories return entities, NEVER DTOs
2. Validators use manual instantiation (not DI)
3. Navigation properties readonly for writes
4. Use `int` for IDs except main entities (`Guid`) and size/cursor (`long`)
5. No default values in entities
6. Keep all using statements
7. Commands return `BaseCommandResponse<T>`
8. GET = AllowAnonymous, Write = Authorize, Admin = Roles
9. UserId extraction: sub → nameidentifier → sid fallback
10. File-scoped namespaces
11. Entities include auditing fields
12. Use named query filters for soft delete (EF Core 10+)

### From ARCHITECTURE.md

- Clean Architecture: Domain ← Application ← Infrastructure / Presentation
- CQRS via MediatR
- BFF pattern for Blazor security
- PostgreSQL + PostGIS
- Keycloak for authentication

### From MULTI_TENANCY.md

- Two-tier admin model (Instance vs Tenant)
- `ITenantEntity` interface with `TenantId`
- Global query filters for tenant isolation
- Virtual tenant strategy for single-tenant mode

---

## Dependencies to Install

### NuGet Packages (Evaluate)

| Package | Purpose | Decision |
|---------|---------|----------|
| UUIDNext | Optimal PostgreSQL UUID v7 | Evaluate if `Guid.CreateVersion7()` insufficient |
| Quartz.NET | Background job scheduling | For outbox processor |

### No New Packages Required For

- UUID v7 generation (native in .NET 9+)
- Named query filters (built into EF Core 10)
- Outbox pattern (custom implementation)
- Strategy pattern (native C# patterns)

---

## Testing Considerations

### Test Projects

- `Event.Application.UnitTests` - Unit tests for handlers
- `Event.API.IntegrationTests` - Integration tests for API

### Key Test Scenarios

1. **UUID v7**: Verify monotonicity, time ordering
2. **Settings Cascade**: Lock → Tenant → Default priority
3. **Aspects**: CRUD operations, polymorphic responses
4. **Module Governance**: Visibility per tenant
5. **Outbox**: Message capture, processing, retry
6. **Deployment Modes**: Both modes function correctly

---

## Links to Referenced Implementation Plan

- **Phases 1-8**: See `plan.md` for detailed task breakdowns
- **Task Checklist**: See `tasks.md` for progress tracking

---

**End of Context**
