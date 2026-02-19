# MODULAR EXTENSIBILITY & GOVERNANCE REFACTOR - Implementation Plan

> **Strategic Plan for Transforming Explore API into a Composition-Based Container System**
>
> This plan implements UUID v7 primary keys, Cascading Settings Engine, Relational Aspect Architecture,
> PDS Hosting & Synchronization, and Module Governance.
>
> **Created**: 2026-01-30
> **Last Updated**: 2026-01-30
> **Status**: Planning Phase

---

## AUTHORITATIVE SOURCE

**Primary Plan**: [refactor-api-high-flexibility-implementation.md](../refactor-api-high-flexibility-implementation.md)

This detailed implementation plan expands on the architectural vision defined in the authoritative source above.
The original plan defines the WHAT and WHY; this document provides the HOW with detailed task breakdowns.

---

## Executive Summary

Transform the `Explore` API from a fixed-schema event platform into a **composition-based container system**. This enables support for any event type (Islamic, Tech, Medical, etc.) via "Aspects" and "Cascading Policies" without further database migrations for new domains.

### Key Deliverables

1. **UUID v7 Primary Keys** - Replace existing Guid generation with UUID v7 for temporal ordering and B-Tree performance
2. **Cascading Settings Engine** - Three-tier configuration hierarchy (System → Tenant → Event) with locking
3. **Relational Aspect Architecture** - Strip domain-specific fields into optional 1:1 "Aspect" tables
4. **PDS Hosting & Synchronization** - Dual-write sync with Outbox pattern for AT Protocol integration
5. **Module Governance** - Control module visibility per tenant with dynamic step sequencing
6. **Virtual Tenant Masking** - Support Single-Tenant deployment mode while keeping code Multi-Tenant

---

## Current State Analysis

### Existing Architecture

- **Primary Keys**: Mixed `Guid` and `int` IDs generated via `Guid.NewGuid()` (random UUID v4)
- **Event Entity**: Contains domain-specific fields (MadhabId, etc.) directly in core entity
- **Multi-Tenancy**: Implemented via `ITenantEntity` interface with `TenantId` and global query filters
- **Federation**: Domain entities exist (`Actor`, `ActorKeyStore`, `AtprotoRecord`) but HTTP endpoints not implemented
- **Settings**: Basic tenant settings without cascading resolution or locking

### Current Limitations

1. **Database Performance**: Random UUID v4 causes B-Tree index fragmentation at scale
2. **Schema Rigidity**: Adding new event types requires database migrations
3. **No Module Governance**: All fields visible to all tenants regardless of relevance
4. **No PDS Sync**: AT Protocol records not synchronized with local database
5. **Mixed Deployment**: No clean separation between single/multi-tenant modes

---

## Technical Research Summary

### UUID v7 Implementation (.NET 10 / EF Core 10)

**Key Findings:**
- .NET 9+ provides `Guid.CreateVersion7()` for native UUID v7 generation
- PostgreSQL 18+ provides `uuidv7()` function for database-side generation
- Npgsql 9.0+ generates UUID v7 client-side by default for Guid PKs
- EF Core value converter pattern for explicit v7 generation

**Recommended Approach:**
```csharp
// Application-side generation (preferred for this project)
builder.Entity<Event>()
    .Property(e => e.Id)
    .HasValueGenerator<UuidV7ValueGenerator>();

// OR PostgreSQL 18+ database-side (alternative)
builder.Entity<Event>()
    .Property(e => e.Id)
    .HasDefaultValueSql("uuidv7()");
```

**Critical Note**: The `Guid.CreateVersion7()` method uses non-big-endian byte storage which can still cause fragmentation. Consider using `UUIDNext` library or custom generator for optimal PostgreSQL performance.

### Named Query Filters (EF Core 10)

**Key Feature:** Multiple named filters per entity with selective disabling

```csharp
modelBuilder.Entity<Event>()
    .HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted)
    .HasQueryFilter(name: "TenantFilter", predicate: e => e.TenantId == tenantId);

// Selective disabling
var allEvents = await _dbContext.Events
    .IgnoreQueryFilter("SoftDelete")  // Still applies TenantFilter
    .ToListAsync();
```

### Outbox Pattern for PDS Sync

**Implementation Strategy:**
1. Local Save → Outbox Entry (same transaction)
2. Background Worker → Pick up Outbox entries
3. Push to PDS (local MST or remote via `com.atproto.repo.applyWrites`)
4. Mark as processed

---

## Implementation Phases

### Phase 1: UUID v7 Infrastructure (Foundation)

**Goal:** Replace existing UUID v4 generation with UUID v7 for all primary keys

**Duration Estimate:** 2-3 days

#### Tasks

**1.1 Create UUID v7 Value Generator**
- Location: `Explore.Infrastructure/ValueGenerators/UuidV7ValueGenerator.cs`
- Implement `ValueGenerator<Guid>` using `Guid.CreateVersion7()` or `UUIDNext`
- Add PostgreSQL byte-order handling for optimal index performance
- Acceptance: Generator produces monotonic, time-sortable UUIDs

**1.2 Update Entity Configurations**
- Location: `Explore.Persistence/Configurations/*.cs`
- Apply `HasValueGenerator<UuidV7ValueGenerator>()` to all Guid PK properties
- Remove any existing `HasDefaultValueSql("gen_random_uuid()")` configurations
- Entities to update: Event, Actor, Organization, Location, EventSession, Category, Tag, etc.
- Acceptance: All entities use UUID v7 for new records

**1.3 Create Migration for Existing Data**
- Create EF Core migration documenting the change
- No data migration needed (existing UUIDs remain valid)
- Update any indexes that might benefit from v7 ordering
- Acceptance: Migration applies cleanly, all tests pass

**1.4 Add DID Unique Index**
- Add unique index on `Actor.Did` column
- Ensure DID remains the federation identity while UUID v7 handles local FK relations
- Acceptance: `CREATE UNIQUE INDEX` on Did, FK constraints intact

---

### Phase 2: Cascading Settings Engine

**Goal:** Implement three-tier configuration hierarchy with locking capabilities

**Duration Estimate:** 2-3 days

#### Tasks

**2.1 Create Domain Entities**
- Location: `Explore.Domain/Settings/`
- Create `SystemSetting`: Key, Value (JSON string), IsLocked, AllowedValues, Description
- Create `TenantSetting`: TenantId, Key, Value, LastModifiedAt, LastModifiedBy
- Both implement auditing fields (CreatedAt, CreatedBy, etc.)
- Acceptance: Entities compile, follow domain conventions

**2.2 Create Entity Configurations**
- Location: `Explore.Persistence/Configurations/`
- `SystemSettingConfiguration.cs` with unique index on Key
- `TenantSettingConfiguration.cs` with composite unique index on (TenantId, Key)
- Apply named query filters for soft delete and tenant isolation
- Acceptance: Configurations generate correct schema

**2.3 Create Repository Interfaces**
- Location: `Explore.Application/Contracts/Persistence/`
- `ISystemSettingRepository` with `GetByKeyAsync`, `GetAllAsync`, `UpsertAsync`
- `ITenantSettingRepository` with `GetByKeyAsync(tenantId, key)`, `GetAllForTenantAsync`
- Acceptance: Interfaces follow existing repository patterns

**2.4 Implement Settings Resolver Service**
- Location: `Explore.Application/Services/`
- Interface: `ISettingsResolver`
- Method: `GetSettingAsync<T>(string key, Guid? tenantId = null)`
- Resolution Logic:
  1. If `SystemSetting.IsLocked == true` → return SystemSetting.Value
  2. If TenantSetting exists → return TenantSetting.Value
  3. Fallback → return SystemSetting.Value (default)
- Support type conversion (string → int, bool, enum, JSON objects)
- Acceptance: Resolver correctly applies cascade logic

**2.5 Create CQRS Commands/Queries**
- Commands: `UpsertSystemSettingCommand`, `UpsertTenantSettingCommand`, `LockSystemSettingCommand`
- Queries: `GetSettingQuery`, `GetAllSystemSettingsQuery`, `GetTenantSettingsQuery`
- Validators for each command
- Acceptance: Full CQRS pattern implemented

**2.6 Create Settings Controller**
- Location: `Explore.API/Controllers/SettingsController.cs`
- Endpoints:
  - `GET /api/settings/{key}` - Get resolved setting value
  - `GET /api/settings/system` - List all system settings (Admin only)
  - `PUT /api/settings/system/{key}` - Upsert system setting (Admin only)
  - `PUT /api/settings/tenant/{key}` - Upsert tenant setting (Tenant Admin)
  - `POST /api/settings/system/{key}/lock` - Lock setting (Admin only)
- Acceptance: All endpoints functional with proper authorization

---

### Phase 3: Relational Aspect Architecture

**Goal:** Strip domain-specific fields from Event and move to optional 1:1 Aspect tables

**Duration Estimate:** 4-5 days

#### Tasks

**3.1 Refactor Event Core Entity**
- Location: `Explore.Domain/Event.cs`
- Remove: `MadhabId` and any other Islamic-specific fields
- Add: `string? MetadataJson` for rare/small dynamic fields
- Add Navigation Properties:
  ```csharp
  public virtual EventIslamicAspect? IslamicAspect { get; set; }
  public virtual EventTechAspect? TechAspect { get; set; }
  ```
- Keep: Universal properties (Title, StartDate, EndDate, OrganizationId, etc.)
- Acceptance: Event entity contains only universal properties

**3.2 Create Aspect Entities**
- Location: `Explore.Domain/Aspects/`
- `EventIslamicAspect`:
  - PK: `Guid EventId` (shared key with Event)
  - Properties: MadhabId, PrayerTimeOffset, GenderMode, LanguageMode
  - FK to Event (1:1 relationship)
- `EventTechAspect`:
  - PK: `Guid EventId` (shared key with Event)
  - Properties: GithubRepoUrl, TechStack (JSON), HackathonTrack, SkillLevel
  - FK to Event (1:1 relationship)
- Include auditing fields on all aspects
- Acceptance: Aspect entities use shared PK pattern

**3.3 Create Aspect Entity Configurations**
- Location: `Explore.Persistence/Configurations/Aspects/`
- `EventIslamicAspectConfiguration.cs`:
  - Shared primary key with Event
  - `HasOne(a => a.Event).WithOne(e => e.IslamicAspect).HasForeignKey<EventIslamicAspect>(a => a.EventId)`
- `EventTechAspectConfiguration.cs` (same pattern)
- Apply named query filters (SoftDelete, TenantFilter via Event navigation)
- Acceptance: 1:1 relationship configured correctly

**3.4 Create EF Core Migration**
- Create migration for:
  - New `EventIslamicAspects` table
  - New `EventTechAspects` table
  - Remove `MadhabId` from `Events` table (if exists)
- Data migration script to move existing data to aspect tables
- Acceptance: Migration applies cleanly, data preserved

**3.5 Create Aspect DTOs**
- Location: `Explore.Application/DTOs/Aspects/`
- `EventIslamicAspectDto`, `CreateEventIslamicAspectDto`, `UpdateEventIslamicAspectDto`
- `EventTechAspectDto`, `CreateEventTechAspectDto`, `UpdateEventTechAspectDto`
- Acceptance: DTOs follow existing patterns

**3.6 Update Event DTOs**
- Update `EventDto` to include:
  ```csharp
  public List<string> AvailableAspects { get; set; } = new();
  public EventIslamicAspectDto? IslamicAspect { get; set; }
  public EventTechAspectDto? TechAspect { get; set; }
  ```
- Update `CreateEventDto` to accept optional aspect creation
- Acceptance: Polymorphic response structure

**3.7 Update Event Repository**
- Add `GetEventWithAspectsAsync(Guid id)` method
- Use `.Include(e => e.IslamicAspect).Include(e => e.TechAspect)`
- Acceptance: Eager loading of aspects works

**3.8 Update Event Handlers**
- Modify `GetEventDetailsRequestHandler`:
  - Use `GetEventWithAspectsAsync`
  - Build `AvailableAspects` list based on non-null aspects
  - Map aspects to DTOs
- Modify `CreateEventCommandHandler`:
  - Accept optional aspect DTOs
  - Create aspect records in same transaction
- Acceptance: Full CRUD for events with aspects

**3.9 Update AutoMapper Profiles**
- Add mappings for aspect entities ↔ DTOs
- Configure conditional mapping for polymorphic responses
- Acceptance: All mappings work correctly

**3.10 Create Aspect-Specific Validators**
- `EventIslamicAspectValidator`: Validate MadhabId exists, GenderMode valid enum
- `EventTechAspectValidator`: Validate URL format, skill level enum
- Integrate with main event validation when aspects provided
- Acceptance: Validation errors for invalid aspects

---

### Phase 4: Module Governance & Discovery

**Goal:** Control module visibility per tenant with dynamic API discovery

**Duration Estimate:** 2-3 days

#### Tasks

**4.1 Create Module Definition Entities**
- Location: `Explore.Domain/Modules/`
- `ModuleDefinition`:
  - Key (e.g., "Mod_Islamic", "Mod_Tech")
  - Name, Description
  - WizardSchemaUrl (for dynamic UI forms)
  - IsEnabled (instance-level)
- `TenantCapability`:
  - TenantId, ModuleKey
  - IsEnabled (tenant-level)
- Acceptance: Module governance entities created

**4.2 Create Module Repository and Service**
- `IModuleRepository`: CRUD for module definitions
- `IModuleService`:
  - `GetAvailableModulesAsync(Guid tenantId)` - Returns modules enabled at both instance AND tenant level
  - `IsModuleEnabledAsync(Guid tenantId, string moduleKey)`
- Acceptance: Module availability respects 3-tier hierarchy

**4.3 Create Module Discovery Endpoint**
- Location: `Explore.API/Controllers/ModulesController.cs`
- `GET /api/modules/available` - Returns modules enabled for current tenant
- Response includes:
  - Module key, name, description
  - Wizard schema URL for dynamic forms
  - Aspect keys associated with module
- Acceptance: Endpoint returns correct modules per tenant

**4.4 Integrate Module Check with Event Creation**
- Validate that aspects provided match enabled modules
- Reject Islamic aspect if "Mod_Islamic" not enabled for tenant
- Acceptance: Module governance enforced on write operations

**4.5 Seed Default Modules**
- Create data seeder for:
  - `Mod_Islamic`: Islamic event features
  - `Mod_Tech`: Tech event features
  - `Mod_Educational`: Educational features (future)
- Acceptance: Modules seeded on database initialization

---

### Phase 5: Request-Scoped Strategy Resolver

**Goal:** Enable modular business logic that adapts at runtime without restart

**Duration Estimate:** 2-3 days

#### Tasks

**5.1 Define Strategy Interfaces**
- Location: `Explore.Application/Contracts/Strategies/`
- `IEventStrategy`: Base interface for event-related strategies
- `ISchedulingStrategy`: For prayer-time-based scheduling
- `IValidationStrategy`: For module-specific validation rules
- Acceptance: Strategy interfaces defined

**5.2 Implement Islamic Scheduling Strategy**
- Location: `Explore.Infrastructure/Strategies/`
- `IslamicSchedulingStrategy`:
  - Calculates event timings based on prayer times
  - Uses prayer time API/library
  - Resolves "30 minutes after Maghrib" to actual time
- Acceptance: Prayer-based scheduling works

**5.3 Create Strategy Resolver**
- Location: `Explore.Application/Services/`
- `IStrategyResolver`:
  - `GetSchedulingStrategy(Guid tenantId)` → returns appropriate strategy
  - Uses TenantContext to identify enabled modules
- DI registration for strategy implementations
- Acceptance: Correct strategy resolved per tenant

**5.4 Integrate with MediatR Handlers**
- Inject `IStrategyResolver` into relevant handlers
- Apply strategies during event creation/update
- Acceptance: Strategies applied transparently

---

### Phase 6: PDS Hosting & Synchronization

**Goal:** Implement Outbox pattern for reliable AT Protocol record synchronization

**Duration Estimate:** 4-5 days

#### Tasks

**6.1 Create Outbox Entity**
- Location: `Explore.Domain/Federation/`
- `PdsSyncOutbox`:
  - Id (UUID v7)
  - ActorDid (string)
  - RecordType (string, e.g., "app.islamu.event")
  - RecordUri (string)
  - Payload (JSON)
  - OccurredAt (DateTime)
  - ProcessedAt (DateTime?)
  - Error (string?)
  - RetryCount (int)
- Acceptance: Outbox entity created

**6.2 Create Outbox Entity Configuration**
- Index on ProcessedAt (NULL filter for unprocessed)
- Index on RetryCount for retry logic
- Acceptance: Efficient querying of unprocessed entries

**6.3 Create PDS Adapter Service**
- Location: `Explore.Infrastructure/Services/Federation/`
- `IPdsService`:
  - `HostRecordAsync(string did, object record)` - For Islamu-hosted users
  - `ProxyRecordAsync(string remotePds, object record)` - For external PDS users
- Implementation uses AT Protocol APIs
- Acceptance: PDS communication works

**6.4 Create Domain Event for Record Creation**
- `EventCreatedDomainEvent`: Raised when event saved
- Contains all data needed for AT Protocol record
- Acceptance: Domain event pattern implemented

**6.5 Implement Outbox Interceptor**
- Location: `Explore.Persistence/Interceptors/`
- `OutboxInterceptor : SaveChangesInterceptor`
- Before SaveChanges:
  - Collect domain events from entities
  - Create OutboxMessage for each event
  - Add to same transaction
- Acceptance: Events captured in outbox atomically

**6.6 Create Background Worker**
- Location: `Explore.API/BackgroundServices/`
- `OutboxProcessorService : BackgroundService`
- Polling loop:
  1. Query unprocessed outbox entries
  2. For each entry, call appropriate PDS service
  3. Mark as processed (or increment retry count on failure)
- Use Quartz or simple Timer for scheduling
- Acceptance: Outbox processed reliably

**6.7 Handle DID Status State Machine**
- Pending → Active (after DID created)
- Failed → retry → Active
- Block write operations until DID is Active
- Acceptance: DID lifecycle managed correctly

---

### Phase 7: Virtual Tenant Masking (Deployment Modes)

**Goal:** Support Single-Tenant deployment while keeping codebase Multi-Tenant

**Duration Estimate:** 1-2 days

#### Tasks

**7.1 Add Deployment Mode Configuration**
- `appsettings.json`:
  ```json
  {
    "DeploymentMode": "MultiTenant" // or "SingleTenant"
  }
  ```
- Create `DeploymentMode` enum
- Acceptance: Configuration parsed correctly

**7.2 Modify Tenant Context Middleware**
- Location: `Explore.API/Middleware/`
- If `DeploymentMode == SingleTenant`:
  - Hardcode `TenantId` to `SeedIds.DefaultTenantId`
  - Skip subdomain/header resolution
- If `DeploymentMode == MultiTenant`:
  - Existing resolution logic (subdomain or `X-Tenant-Id` header)
- Acceptance: Tenant resolved correctly per mode

**7.3 Block SuperAdmin Controllers in Single-Tenant**
- Create `[RequiresMultiTenant]` attribute
- Apply to SuperAdmin endpoints
- Return 404 in SingleTenant mode
- Acceptance: SuperAdmin UI simplified for single-tenant

**7.4 Update Seed Data Logic**
- In SingleTenant mode:
  - Create single default tenant
  - Skip multi-tenant example data
- Acceptance: Appropriate seed data per mode

---

### Phase 8: HATEOAS & API Updates

**Goal:** Update API responses with aspect-aware links and improved discovery

**Duration Estimate:** 2-3 days

#### Tasks

**8.1 Update Event Link Policy**
- Add aspect-specific detail links:
  - `self` → `/api/events/{id}`
  - `islamic-details` → `/api/events/{id}/islamic` (if aspect exists)
  - `tech-details` → `/api/events/{id}/tech` (if aspect exists)
- Acceptance: Links generated based on available aspects

**8.2 Create Aspect Detail Endpoints**
- `GET /api/events/{id}/islamic` - Get Islamic aspect only
- `PUT /api/events/{id}/islamic` - Update Islamic aspect
- `DELETE /api/events/{id}/islamic` - Remove Islamic aspect
- Same for Tech aspect
- Acceptance: Dedicated aspect endpoints work

**8.3 Update OpenAPI Documentation**
- Add endpoint summaries and descriptions
- Document polymorphic response schemas
- Add examples for aspect payloads
- Acceptance: Scalar docs show all endpoints

**8.4 Add Query Filtering by Aspects**
- `GET /api/events?aspect=Islamic` - Filter by aspect presence
- `GET /api/events?madhab=Hanafi` - Filter by aspect property
- Use Query Specification pattern for dynamic filters
- Acceptance: Filtering works efficiently

---

## Risk Assessment

### High Risk

| Risk | Mitigation |
|------|------------|
| UUID v7 migration affects existing foreign keys | UUID v7 is compatible with existing Guid columns; no migration needed |
| Aspect table proliferation | Start with 2 aspects (Islamic, Tech); add more only when needed |
| PDS sync failures | Outbox pattern with retry logic ensures eventual consistency |

### Medium Risk

| Risk | Mitigation |
|------|------------|
| Breaking changes to Event API | Version API; maintain backwards compatibility in v1 |
| Performance of cascading settings | Cache settings with short TTL; invalidate on change |
| Module governance complexity | Start simple; iterate based on tenant feedback |

### Low Risk

| Risk | Mitigation |
|------|------------|
| Single-tenant mode untested | Add integration tests for both deployment modes |
| Strategy pattern overhead | Lazy resolution; strategies are lightweight |

---

## Success Metrics

1. **UUID v7 Adoption**: 100% of new records use UUID v7
2. **Cascading Settings**: Settings resolve in <10ms
3. **Aspect Architecture**: Event API response time unchanged despite polymorphism
4. **PDS Sync**: >99% of records synced within 5 minutes
5. **Module Governance**: Tenants only see relevant modules
6. **Deployment Modes**: Both modes pass all tests

---

## Dependencies

### External Libraries

- **UUIDNext** (optional): For optimal PostgreSQL UUID v7 generation
- **Quartz.NET**: For background job scheduling (Outbox processor)
- **No new EF Core packages**: Using EF Core 10 built-in features

### Internal Dependencies

- Existing Multi-Tenancy infrastructure
- Existing CQRS/MediatR patterns
- Existing AutoMapper configuration
- Existing Repository pattern

---

## Testing Strategy

### Unit Tests

- UUID v7 generator produces valid v7 UUIDs
- Settings resolver applies cascade logic correctly
- Strategy resolver returns correct strategy per tenant
- Module availability respects 3-tier hierarchy

### Integration Tests

- Aspect CRUD operations
- Settings persistence and retrieval
- Outbox processing end-to-end
- Module governance enforcement

### Manual Testing

- Verify Scalar API docs show new endpoints
- Test both deployment modes
- Verify HATEOAS links correct per aspect

---

## Related Documentation

- [ARCHITECTURE.md](../../docs/ARCHITECTURE.md) - System architecture
- [EXTENSIBILITY.md](../../docs/EXTENSIBILITY.md) - Aspect pattern details
- [MULTI_TENANCY.md](../../docs/MULTI_TENANCY.md) - Tenant isolation
- [GOVERNANCE.md](../../docs/GOVERNANCE.md) - Coding conventions
- [FEDERATION.md](../../docs/FEDERATION.md) - AT Protocol integration

---

## Appendix: Key Code Patterns

### UUID v7 Value Generator

```csharp
// Explore.Infrastructure/ValueGenerators/UuidV7ValueGenerator.cs
public class UuidV7ValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry)
    {
        return Guid.CreateVersion7();
    }
}
```

### Settings Resolver

```csharp
// Explore.Application/Services/SettingsResolver.cs
public async Task<T?> GetSettingAsync<T>(string key, Guid? tenantId)
{
    var systemSetting = await _systemSettingRepo.GetByKeyAsync(key);

    if (systemSetting == null)
        return default;

    // Locked settings cannot be overridden
    if (systemSetting.IsLocked)
        return Deserialize<T>(systemSetting.Value);

    // Check for tenant override
    if (tenantId.HasValue)
    {
        var tenantSetting = await _tenantSettingRepo.GetByKeyAsync(tenantId.Value, key);
        if (tenantSetting != null)
            return Deserialize<T>(tenantSetting.Value);
    }

    // Fall back to system default
    return Deserialize<T>(systemSetting.Value);
}
```

### Aspect Entity Configuration

```csharp
// Explore.Persistence/Configurations/Aspects/EventIslamicAspectConfiguration.cs
public void Configure(EntityTypeBuilder<EventIslamicAspect> builder)
{
    builder.ToTable("event_islamic_aspects");

    // Shared primary key pattern
    builder.HasKey(a => a.EventId);

    builder.HasOne(a => a.Event)
           .WithOne(e => e.IslamicAspect)
           .HasForeignKey<EventIslamicAspect>(a => a.EventId)
           .OnDelete(DeleteBehavior.Cascade);

    builder.Property(a => a.MadhabId).IsRequired(false);
    builder.Property(a => a.GenderMode).HasConversion<string>();

    // Named query filters (EF Core 10)
    builder.HasQueryFilter(name: "SoftDelete", predicate: a => !a.IsDeleted);
}
```

### Outbox Interceptor

```csharp
// Explore.Persistence/Interceptors/OutboxInterceptor.cs
public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken ct = default)
{
    var context = eventData.Context;
    if (context == null) return result;

    var entities = context.ChangeTracker.Entries<IHasDomainEvents>()
        .Where(e => e.Entity.DomainEvents.Any())
        .ToList();

    foreach (var entry in entities)
    {
        foreach (var domainEvent in entry.Entity.DomainEvents)
        {
            var outboxMessage = new PdsSyncOutbox
            {
                Id = Guid.CreateVersion7(),
                RecordType = domainEvent.GetType().Name,
                Payload = JsonSerializer.Serialize(domainEvent),
                OccurredAt = DateTime.UtcNow
            };
            context.Set<PdsSyncOutbox>().Add(outboxMessage);
        }
        entry.Entity.ClearDomainEvents();
    }

    return result;
}
```

---

**End of Plan**
