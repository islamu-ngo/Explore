# Modular Extensibility Refactor - Implementation Plan

> **Comprehensive Architecture Refactor for ISLAMU Event Platform**
>
> Transform the Explore API from a fixed-schema event platform into a **composition-based container system** supporting any event type (Islamic, Tech, Medical, etc.) via "Aspects" and "Cascading Policies" without further database migrations for new domains.

**Last Updated**: January 2026  
**Estimated Total Duration**: 12-16 weeks  
**Risk Level**: High (Core architectural changes affecting all layers)

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State Analysis](#current-state-analysis)
3. [Target Architecture](#target-architecture)
4. [Implementation Phases](#implementation-phases)
   - [Phase 1: Foundation (UUID v7 + Named Query Filters)](#phase-1-foundation)
   - [Phase 2: Settings Engine](#phase-2-settings-engine)
   - [Phase 3: Aspect Infrastructure](#phase-3-aspect-infrastructure)
   - [Phase 4: Module Governance](#phase-4-module-governance)
   - [Phase 5: Strategy Pattern](#phase-5-strategy-pattern)
   - [Phase 6: PDS Synchronization](#phase-6-pds-synchronization)
   - [Phase 7: Virtual Tenant Masking](#phase-7-virtual-tenant-masking)
   - [Phase 8: Integration & Testing](#phase-8-integration--testing)
5. [Risk Assessment](#risk-assessment)
6. [Success Metrics](#success-metrics)
7. [Dependencies & Prerequisites](#dependencies--prerequisites)

---

## Executive Summary

### Goals

| # | Goal | Description |
|---|------|-------------|
| 1 | **UUID v7 Primary Keys** | Migrate from current Guid to UUID v7 for better database performance and sortability |
| 2 | **Cascading Settings Engine** | Three-tier configuration (System → Tenant → Event) with locking |
| 3 | **Relational Aspect Architecture** | Extract domain-specific fields into 1:1 aspect tables (Islamic, Tech, etc.) |
| 4 | **Module Governance** | Control module visibility per tenant |
| 5 | **PDS Hosting & Synchronization** | Outbox pattern for ATProto sync with hosted and external PDS support |
| 6 | **Virtual Tenant Masking** | Support single-tenant mode while keeping multi-tenant codebase |

### Key Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| UUID Generation | PostgreSQL `uuidv7()` + client-side fallback | Already using `HasDefaultValueSql("uuidv7()")` in EventConfiguration |
| Query Filters | Named filters (EF Core 10+) | Selective disabling for admin operations; current combined filters need refactor |
| Aspect Pattern | 1:1 relational tables with shared PK | Fast B-Tree joins for filtering; FK = PK pattern |
| PDS Sync | Outbox pattern with background worker | Transactional consistency; resilience to external failures |
| Settings Resolution | Service with caching | Performance; avoid repeated DB queries |

---

## Current State Analysis

### Domain Layer (`Explore.Domain`)

**Current Entities with Multi-Tenancy Support:**
- `Event` - Implements `ITenantEntity`, `IAuditableEntity`, `ISoftDeletable`
- `Actor` - Contains DID, Handle, PdsHost for ATProto federation
- `Tenant`, `TenantSettings` - Multi-tenancy foundation exists
- `AtprotoRecord`, `SyncState`, `IndexedDid` - ATProto sync infrastructure exists

**Islamic-Specific Field to Extract:**
```csharp
// Explore.Domain/Event.cs - Line 44-46
[ForeignKey("Madhab")]
public int? MadhabId { get; set; }
public Madhab? Madhab { get; set; }
```

**Current Interfaces:**
```csharp
// ITenantEntity - Multi-tenant marker
public interface ITenantEntity { Guid TenantId { get; set; } }

// IAuditableEntity - Audit trail
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

// ISoftDeletable - Soft delete support
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
```

### Persistence Layer (`Explore.Persistence`)

**Current Query Filter Pattern (Combined - Needs Refactor):**
```csharp
// ExploreDbContext.cs - Lines 57-111
// Currently uses combined tenant + soft delete filters
modelBuilder.Entity<Event>()
    .HasQueryFilter(e => (TenantContext == null || e.TenantId == TenantContext.TenantId) && !e.IsDeleted);
```

**UUID v7 Already Configured:**
```csharp
// EventConfiguration.cs - Line 17
builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
```

**Generic Repository Pattern:**
```csharp
// GenericRepository.cs - Supports soft delete detection
public async Task Delete(T entity)
{
    if (entity is ISoftDeletable)
    {
        _dbContext.Entry(entity).State = EntityState.Deleted;
        await _dbContext.SaveChangesAsync();  // SaveChangesAsync converts to soft delete
    }
    else { /* hard delete */ }
}
```

### Application Layer (`Explore.Application`)

**Current Patterns:**
- CQRS with MediatR
- Commands return `BaseCommandResponse<TKey>`
- Validators use manual instantiation (not DI)
- `ICurrentUserService` for audit field population
- `ITenantContext` for multi-tenant resolution

### Infrastructure Already in Place

| Component | Status | Location |
|-----------|--------|----------|
| Multi-tenant DbContext | Implemented | `ExploreDbContext.cs` |
| Soft delete handling | Implemented | `SaveChangesAsync` override |
| Audit field population | Implemented | `SaveChangesAsync` override |
| UUID v7 generation | Partial | PostgreSQL default, not all entities |
| ATProto record storage | Implemented | `AtprotoRecord`, `IndexedDid` entities |
| Sync state tracking | Implemented | `SyncState` entity |

---

## Target Architecture

### High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              PRESENTATION LAYER                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │  Explore.API    │  │ Explore.Blazor  │  │  Module Discovery Endpoint  │  │
│  │  (REST + HATEOAS)│  │ (BFF Pattern)   │  │  GET /api/v1/modules       │  │
│  └────────┬────────┘  └────────┬────────┘  └──────────────┬──────────────┘  │
└───────────┼────────────────────┼───────────────────────────┼────────────────┘
            │                    │                           │
┌───────────▼────────────────────▼───────────────────────────▼────────────────┐
│                            APPLICATION LAYER                                 │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐   │
│  │ Settings Resolver│  │ Strategy Resolver│  │ Aspect Validators        │   │
│  │ (3-Tier Cascade) │  │ (Runtime Logic)  │  │ (Dynamic by ModuleKey)   │   │
│  └──────────────────┘  └──────────────────┘  └──────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ CQRS Handlers (Commands/Queries) with Aspect Include/Mapping        │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
            │
┌───────────▼─────────────────────────────────────────────────────────────────┐
│                              DOMAIN LAYER                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ Event (Core)        │ EventIslamicAspect │ EventTechAspect          │    │
│  │ - No MadhabId       │ - MadhabId         │ - GithubRepoUrl          │    │
│  │ - MetadataJson      │ - PrayerOffset     │ - TechStack              │    │
│  │ - AvailableAspects  │ - GenderMode       │                          │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ SystemSetting       │ TenantSetting      │ ModuleDefinition         │    │
│  │ TenantCapability    │ PdsSyncOutbox      │                          │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
            │
┌───────────▼─────────────────────────────────────────────────────────────────┐
│                           INFRASTRUCTURE LAYER                               │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐   │
│  │ Named Query      │  │ PDS Service      │  │ Outbox Background        │   │
│  │ Filters (EF10)   │  │ (Host/Proxy)     │  │ Worker                   │   │
│  └──────────────────┘  └──────────────────┘  └──────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ Aspect Configurations (EntityTypeConfiguration for 1:1 tables)      │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow: Event with Aspects

```
┌─────────────────────────────────────────────────────────────────────────┐
│ CREATE EVENT WITH ISLAMIC ASPECT                                        │
├─────────────────────────────────────────────────────────────────────────┤
│ 1. Client POST /api/v1/events with aspect data                          │
│ 2. Controller → MediatR → CreateEventCommandHandler                     │
│ 3. Handler validates via EventAspectValidator (ModuleKey = "Islamic")   │
│ 4. Handler creates Event + EventIslamicAspect in same transaction       │
│ 5. SaveChangesAsync adds audit fields + triggers outbox                 │
│ 6. Background worker syncs to PDS (if federated)                        │
│ 7. Response includes AvailableAspects: ["Islamic"]                      │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Phases

---

## Phase 1: Foundation

**Duration**: 2 weeks  
**Complexity**: Medium  
**Risk**: Medium (Database migration required)

### Goal
Establish the foundation for all subsequent phases: UUID v7 consistency and named query filters.

### 1.1 UUID v7 Migration

**Current State**: Event entity uses `HasDefaultValueSql("uuidv7()")`, but not all entities are consistent.

**Tasks**:

#### 1.1.1 Audit All Entity Configurations
- [ ] Review all 44 entity configurations in `Explore.Persistence/Configurations/Entities/`
- [ ] Identify entities using Guid PKs without `uuidv7()` default
- [ ] Document which entities need migration

**Files to Update**:
```
Explore.Persistence/Configurations/Entities/
├── ActorConfiguration.cs
├── ActorKeyStoreConfiguration.cs
├── AtprotoRecordConfiguration.cs
├── CategoryConfiguration.cs
├── EventSessionConfiguration.cs
├── EventSessionAgendaItemConfiguration.cs
├── LocationConfiguration.cs
├── OrganizationConfiguration.cs
├── OrganizationMemberConfiguration.cs
├── OrganizationReviewConfiguration.cs
├── StorageObjectConfiguration.cs
├── TenantConfiguration.cs
├── TenantSettingsConfiguration.cs
├── TenantUserConfiguration.cs
├── UserConfiguration.cs
├── UserAuthenticationTokenConfiguration.cs
├── UserExternalLoginConfiguration.cs
└── UserRoleConfiguration.cs
```

#### 1.1.2 Add UUID v7 Default to All Guid Entities
```csharp
// Pattern to apply to all Guid PK configurations
builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
```

#### 1.1.3 Consider Client-Side Generation for Performance
```csharp
// Optional: Application layer generation for better control
// In command handler before repository.Create():
entity.Id = Guid.CreateVersion7();  // .NET 9+ native support
```

**Acceptance Criteria**:
- [ ] All Guid PK entities have `HasDefaultValueSql("uuidv7()")`
- [ ] Migration generated and tested
- [ ] Existing data remains intact (no ID changes for existing records)
- [ ] Tests pass with new ID generation

### 1.2 Named Query Filters (EF Core 10)

**Current State**: Combined filters in `ExploreDbContext.ApplyGlobalQueryFilters()`:
```csharp
// Current (combined)
modelBuilder.Entity<Event>()
    .HasQueryFilter(e => (TenantContext == null || e.TenantId == TenantContext.TenantId) && !e.IsDeleted);
```

**Target State**: Named filters for selective disabling:
```csharp
// Target (named)
modelBuilder.Entity<Event>()
    .HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);

modelBuilder.Entity<Event>()
    .HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted);
```

#### 1.2.1 Refactor Query Filters

**File**: `Explore.Persistence/ExploreDbContext.cs`

**Tasks**:
- [ ] Split combined filters into separate tenant and soft delete filters
- [ ] Use `HasQueryFilter(name: "...", predicate: ...)` syntax
- [ ] Apply to all entities with combined filters

**Entities Requiring Refactor** (from current codebase):
- Event
- EventSession
- Organization
- OrganizationMember
- Actor
- User

**Implementation Pattern**:
```csharp
private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
{
    // ===== Event Entities =====
    // Tenant filter (always active unless TenantContext is null)
    modelBuilder.Entity<Event>()
        .HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
    
    // Soft Delete filter (can be disabled with IgnoreQueryFilter("SoftDelete"))
    modelBuilder.Entity<Event>()
        .HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted);
    
    // ... repeat for other entities
}
```

#### 1.2.2 Create Admin Query Extensions

**New File**: `Explore.Persistence/Extensions/QueryableExtensions.cs`

```csharp
namespace Explore.Persistence.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Includes soft-deleted entities in the query results.
    /// Use for admin operations that need to see all records.
    /// </summary>
    public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query) 
        where T : class
    {
        return query.IgnoreQueryFilter("SoftDelete");
    }
}
```

**Acceptance Criteria**:
- [ ] All combined filters split into tenant + named soft delete
- [ ] `IgnoreQueryFilter("SoftDelete")` works for admin queries
- [ ] Tenant filter cannot be bypassed (security)
- [ ] Unit tests for filter behavior
- [ ] Integration tests for admin queries including deleted

---

## Phase 2: Settings Engine

**Duration**: 2 weeks  
**Complexity**: Medium  
**Risk**: Low (New feature, no breaking changes)

### Goal
Implement a three-tier configuration hierarchy (System → Tenant → Event) with locking capabilities.

### 2.1 Domain Entities

#### 2.1.1 Create SystemSetting Entity

**New File**: `Explore.Domain/SystemSetting.cs`

```csharp
namespace Explore.Domain;

/// <summary>
/// System-wide configuration settings that apply to all tenants.
/// When IsLocked=true, tenants cannot override this setting.
/// </summary>
public class SystemSetting
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Unique setting key (e.g., "Events.MaxAttendees", "Federation.PdsUrl")
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON-serialized value for the setting
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// When true, tenants cannot override this setting
    /// </summary>
    public bool IsLocked { get; set; }
    
    /// <summary>
    /// JSON array of allowed values (null = any value allowed)
    /// </summary>
    public string? AllowedValues { get; set; }
    
    /// <summary>
    /// Human-readable description of this setting
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Category for grouping settings in admin UI
    /// </summary>
    public string? Category { get; set; }
}
```

#### 2.1.2 Refactor TenantSettings Entity

**Modify File**: `Explore.Domain/TenantSettings.cs`

Current:
```csharp
public class TenantSettings : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; }
}
```

Target:
```csharp
namespace Explore.Domain;

/// <summary>
/// Tenant-specific setting overrides.
/// Can only override SystemSettings where IsLocked=false.
/// </summary>
public class TenantSetting : ITenantEntity
{
    public Guid Id { get; set; }
    
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    /// <summary>
    /// Setting key (must match a SystemSetting.Key)
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON-serialized override value
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
```

**Note**: This is a breaking change to the existing `TenantSettings` entity. Data migration required.

### 2.2 Persistence Layer

#### 2.2.1 Create Entity Configurations

**New File**: `Explore.Persistence/Configurations/Entities/SystemSettingConfiguration.cs`

```csharp
namespace Explore.Persistence.Configurations.Entities;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.Property(s => s.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(s => s.Key).HasMaxLength(256).IsRequired();
        builder.Property(s => s.Value).IsRequired();
        builder.Property(s => s.AllowedValues).HasMaxLength(4000);
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.Category).HasMaxLength(100);
        
        builder.HasIndex(s => s.Key).IsUnique();
        builder.HasIndex(s => s.Category);
    }
}
```

**Modify File**: `Explore.Persistence/Configurations/Entities/TenantSettingsConfiguration.cs`

```csharp
namespace Explore.Persistence.Configurations.Entities;

public class TenantSettingConfiguration : IEntityTypeConfiguration<TenantSetting>
{
    public void Configure(EntityTypeBuilder<TenantSetting> builder)
    {
        builder.Property(s => s.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(s => s.Key).HasMaxLength(256).IsRequired();
        builder.Property(s => s.Value).IsRequired();
        
        builder.HasIndex(s => new { s.TenantId, s.Key }).IsUnique();
        
        builder.HasOne(s => s.Tenant)
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 2.2.2 Add DbSets

**Modify File**: `Explore.Persistence/ExploreDbContext.cs`

```csharp
// Add to DbSets section
public DbSet<SystemSetting> SystemSettings { get; set; }
public DbSet<TenantSetting> TenantSettings { get; set; }  // Renamed from TenantSettings
```

### 2.3 Application Layer

#### 2.3.1 Create ISettingsResolver Interface

**New File**: `Explore.Application/Contracts/Infrastructure/ISettingsResolver.cs`

```csharp
namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Resolves settings using the three-tier hierarchy:
/// 1. If SystemSetting.IsLocked, return system value
/// 2. Check TenantSetting for override
/// 3. Fall back to SystemSetting.Value (default)
/// </summary>
public interface ISettingsResolver
{
    /// <summary>
    /// Gets a setting value for the current tenant.
    /// </summary>
    Task<T?> GetSettingAsync<T>(string key, CancellationToken ct = default);
    
    /// <summary>
    /// Gets a setting value for a specific tenant (admin operations).
    /// </summary>
    Task<T?> GetSettingAsync<T>(string key, Guid tenantId, CancellationToken ct = default);
    
    /// <summary>
    /// Checks if a setting can be overridden by the tenant.
    /// </summary>
    Task<bool> CanOverrideAsync(string key, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all settings for the current tenant (merged system + overrides).
    /// </summary>
    Task<Dictionary<string, object?>> GetAllSettingsAsync(CancellationToken ct = default);
}
```

#### 2.3.2 Create SettingsResolver Implementation

**New File**: `Explore.Infrastructure/Services/SettingsResolver.cs`

```csharp
namespace Explore.Infrastructure.Services;

public class SettingsResolver : ISettingsResolver
{
    private readonly ExploreDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SettingsResolver> _logger;
    
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<T?> GetSettingAsync<T>(string key, CancellationToken ct = default)
    {
        return await GetSettingAsync<T>(key, _tenantContext.TenantId, ct);
    }

    public async Task<T?> GetSettingAsync<T>(string key, Guid tenantId, CancellationToken ct = default)
    {
        var cacheKey = $"setting:{tenantId}:{key}";
        
        if (_cache.TryGetValue(cacheKey, out T? cachedValue))
            return cachedValue;

        // 1. Get system setting
        var systemSetting = await _dbContext.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);
        
        if (systemSetting == null)
            return default;

        // 2. If locked, return system value
        if (systemSetting.IsLocked)
        {
            var lockedValue = JsonSerializer.Deserialize<T>(systemSetting.Value);
            _cache.Set(cacheKey, lockedValue, CacheDuration);
            return lockedValue;
        }

        // 3. Check for tenant override
        var tenantSetting = await _dbContext.TenantSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Key == key, ct);
        
        var finalValue = tenantSetting != null
            ? JsonSerializer.Deserialize<T>(tenantSetting.Value)
            : JsonSerializer.Deserialize<T>(systemSetting.Value);
        
        _cache.Set(cacheKey, finalValue, CacheDuration);
        return finalValue;
    }

    // ... other methods
}
```

### 2.4 Seed Data

**New File**: `Explore.Persistence/Seed/SystemSettingSeedData.cs`

```csharp
namespace Explore.Persistence.Seed;

public static class SystemSettingSeedData
{
    public static readonly List<SystemSetting> DefaultSettings = new()
    {
        new SystemSetting
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
            Key = "Events.MaxAttendeesPerEvent",
            Value = "1000",
            IsLocked = false,
            Category = "Events",
            Description = "Maximum number of attendees per event"
        },
        new SystemSetting
        {
            Id = new Guid("00000000-0000-0000-0000-000000000002"),
            Key = "Federation.PdsUrl",
            Value = "\"https://pds.islamu.io\"",
            IsLocked = true,
            Category = "Federation",
            Description = "Default PDS URL for hosted users (locked)"
        },
        new SystemSetting
        {
            Id = new Guid("00000000-0000-0000-0000-000000000003"),
            Key = "Modules.EnabledByDefault",
            Value = "[\"Core\"]",
            IsLocked = false,
            Category = "Modules",
            Description = "Modules enabled for new tenants by default"
        }
    };
}
```

**Acceptance Criteria**:
- [ ] `SystemSetting` entity created with all fields
- [ ] `TenantSetting` entity refactored (renamed from `TenantSettings`)
- [ ] Migration generated and tested
- [ ] `ISettingsResolver` interface and implementation complete
- [ ] Caching implemented for performance
- [ ] Seed data for default settings
- [ ] Unit tests for resolution logic (locked vs unlocked)
- [ ] Integration tests for tenant overrides

---

## Phase 3: Aspect Infrastructure

**Duration**: 3 weeks  
**Complexity**: High  
**Risk**: High (Core entity modification, breaking changes)

### Goal
Strip domain-specific fields from the core `Event` entity and move them into optional 1:1 "Aspect" tables.

### 3.1 Domain Entities

#### 3.1.1 Create EventIslamicAspect Entity

**New File**: `Explore.Domain/EventIslamicAspect.cs`

```csharp
namespace Explore.Domain;

/// <summary>
/// Islamic-specific event metadata.
/// Uses shared primary key pattern (PK = FK to Event).
/// </summary>
public class EventIslamicAspect
{
    /// <summary>
    /// Shared primary key - same as Event.Id
    /// </summary>
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    
    /// <summary>
    /// Islamic school of thought (Madhab)
    /// </summary>
    public int? MadhabId { get; set; }
    public Madhab? Madhab { get; set; }
    
    /// <summary>
    /// Prayer time offset in minutes (e.g., "30 min after Maghrib")
    /// </summary>
    public int? PrayerTimeOffsetMinutes { get; set; }
    
    /// <summary>
    /// Which prayer time to use as reference
    /// </summary>
    public string? PrayerTimeReference { get; set; }  // "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha"
    
    /// <summary>
    /// Gender segregation mode for the event
    /// </summary>
    public string? GenderMode { get; set; }  // "MenOnly", "WomenOnly", "Mixed", "Segregated"
    
    /// <summary>
    /// Language of primary instruction (Arabic, English, etc.)
    /// </summary>
    public int? InstructionLanguageId { get; set; }
    public Language? InstructionLanguage { get; set; }
}
```

#### 3.1.2 Create EventTechAspect Entity

**New File**: `Explore.Domain/EventTechAspect.cs`

```csharp
namespace Explore.Domain;

/// <summary>
/// Tech/developer-specific event metadata.
/// Uses shared primary key pattern (PK = FK to Event).
/// </summary>
public class EventTechAspect
{
    /// <summary>
    /// Shared primary key - same as Event.Id
    /// </summary>
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    
    /// <summary>
    /// GitHub repository URL for event materials
    /// </summary>
    public string? GithubRepoUrl { get; set; }
    
    /// <summary>
    /// Tech stack covered (JSON array: ["C#", ".NET", "Blazor"])
    /// </summary>
    public string? TechStack { get; set; }
    
    /// <summary>
    /// Skill level required
    /// </summary>
    public string? SkillLevel { get; set; }  // "Beginner", "Intermediate", "Advanced"
    
    /// <summary>
    /// Is hands-on coding involved?
    /// </summary>
    public bool IsHandsOn { get; set; }
    
    /// <summary>
    /// URL to live coding environment (CodeSandbox, StackBlitz, etc.)
    /// </summary>
    public string? LiveCodingUrl { get; set; }
}
```

#### 3.1.3 Modify Event Entity

**Modify File**: `Explore.Domain/Event.cs`

```csharp
namespace Explore.Domain;

public class Event : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }
    
    // ... existing fields ...
    
    // REMOVE: MadhabId and Madhab navigation property
    // [ForeignKey("Madhab")]
    // public int? MadhabId { get; set; }
    // public Madhab? Madhab { get; set; }
    
    // ADD: Navigation properties for aspects
    public virtual EventIslamicAspect? IslamicAspect { get; set; }
    public virtual EventTechAspect? TechAspect { get; set; }
    
    // ADD: Container for dynamic/rare metadata
    /// <summary>
    /// JSON container for rare/dynamic fields that don't warrant a full aspect table.
    /// </summary>
    public string? MetadataJson { get; set; }
}
```

### 3.2 Persistence Layer

#### 3.2.1 Create Aspect Configurations

**New File**: `Explore.Persistence/Configurations/Entities/Aspects/EventIslamicAspectConfiguration.cs`

```csharp
namespace Explore.Persistence.Configurations.Entities.Aspects;

public class EventIslamicAspectConfiguration : IEntityTypeConfiguration<EventIslamicAspect>
{
    public void Configure(EntityTypeBuilder<EventIslamicAspect> builder)
    {
        builder.ToTable("EventIslamicAspects");
        
        // Shared primary key pattern
        builder.HasKey(a => a.EventId);
        
        // 1:1 relationship with Event
        builder.HasOne(a => a.Event)
            .WithOne(e => e.IslamicAspect)
            .HasForeignKey<EventIslamicAspect>(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(a => a.Madhab)
            .WithMany()
            .HasForeignKey(a => a.MadhabId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(a => a.InstructionLanguage)
            .WithMany()
            .HasForeignKey(a => a.InstructionLanguageId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(a => a.PrayerTimeReference).HasMaxLength(20);
        builder.Property(a => a.GenderMode).HasMaxLength(20);
    }
}
```

**New File**: `Explore.Persistence/Configurations/Entities/Aspects/EventTechAspectConfiguration.cs`

```csharp
namespace Explore.Persistence.Configurations.Entities.Aspects;

public class EventTechAspectConfiguration : IEntityTypeConfiguration<EventTechAspect>
{
    public void Configure(EntityTypeBuilder<EventTechAspect> builder)
    {
        builder.ToTable("EventTechAspects");
        
        // Shared primary key pattern
        builder.HasKey(a => a.EventId);
        
        // 1:1 relationship with Event
        builder.HasOne(a => a.Event)
            .WithOne(e => e.TechAspect)
            .HasForeignKey<EventTechAspect>(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(a => a.GithubRepoUrl).HasMaxLength(500);
        builder.Property(a => a.TechStack).HasMaxLength(2000);
        builder.Property(a => a.SkillLevel).HasMaxLength(20);
        builder.Property(a => a.LiveCodingUrl).HasMaxLength(500);
    }
}
```

#### 3.2.2 Update EventConfiguration

**Modify File**: `Explore.Persistence/Configurations/Entities/EventConfiguration.cs`

```csharp
// REMOVE: Madhab relationship
// builder.HasOne(e => e.Madhab)
//     .WithMany()
//     .HasForeignKey(e => e.MadhabId)
//     .OnDelete(DeleteBehavior.Restrict);

// ADD: MetadataJson column
builder.Property(e => e.MetadataJson)
    .HasColumnType("jsonb");
```

#### 3.2.3 Add DbSets

**Modify File**: `Explore.Persistence/ExploreDbContext.cs`

```csharp
// Add to DbSets section
public DbSet<EventIslamicAspect> EventIslamicAspects { get; set; }
public DbSet<EventTechAspect> EventTechAspects { get; set; }
```

### 3.3 Data Migration

#### 3.3.1 Create Migration for Aspect Tables

```sql
-- Migration: CreateAspectTables

-- 1. Create EventIslamicAspects table
CREATE TABLE "EventIslamicAspects" (
    "EventId" uuid NOT NULL,
    "MadhabId" integer NULL,
    "PrayerTimeOffsetMinutes" integer NULL,
    "PrayerTimeReference" character varying(20) NULL,
    "GenderMode" character varying(20) NULL,
    "InstructionLanguageId" integer NULL,
    CONSTRAINT "PK_EventIslamicAspects" PRIMARY KEY ("EventId"),
    CONSTRAINT "FK_EventIslamicAspects_Events_EventId" FOREIGN KEY ("EventId") 
        REFERENCES "Events" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EventIslamicAspects_Madhabs_MadhabId" FOREIGN KEY ("MadhabId") 
        REFERENCES "Madhabs" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EventIslamicAspects_Languages_InstructionLanguageId" FOREIGN KEY ("InstructionLanguageId") 
        REFERENCES "Languages" ("Id") ON DELETE RESTRICT
);

-- 2. Create EventTechAspects table
CREATE TABLE "EventTechAspects" (
    "EventId" uuid NOT NULL,
    "GithubRepoUrl" character varying(500) NULL,
    "TechStack" character varying(2000) NULL,
    "SkillLevel" character varying(20) NULL,
    "IsHandsOn" boolean NOT NULL DEFAULT false,
    "LiveCodingUrl" character varying(500) NULL,
    CONSTRAINT "PK_EventTechAspects" PRIMARY KEY ("EventId"),
    CONSTRAINT "FK_EventTechAspects_Events_EventId" FOREIGN KEY ("EventId") 
        REFERENCES "Events" ("Id") ON DELETE CASCADE
);

-- 3. Add MetadataJson column to Events
ALTER TABLE "Events" ADD "MetadataJson" jsonb NULL;

-- 4. Migrate existing MadhabId data to EventIslamicAspects
INSERT INTO "EventIslamicAspects" ("EventId", "MadhabId")
SELECT "Id", "MadhabId" FROM "Events" WHERE "MadhabId" IS NOT NULL;

-- 5. Remove MadhabId from Events
ALTER TABLE "Events" DROP CONSTRAINT IF EXISTS "FK_Events_Madhabs_MadhabId";
ALTER TABLE "Events" DROP COLUMN "MadhabId";
```

### 3.4 Application Layer Updates

#### 3.4.1 Create Aspect DTOs

**New File**: `Explore.Application/DTOs/Event/Aspects/EventIslamicAspectDto.cs`

```csharp
namespace Explore.Application.DTOs.Event.Aspects;

public class EventIslamicAspectDto
{
    public int? MadhabId { get; set; }
    public string? MadhabName { get; set; }
    public int? PrayerTimeOffsetMinutes { get; set; }
    public string? PrayerTimeReference { get; set; }
    public string? GenderMode { get; set; }
    public int? InstructionLanguageId { get; set; }
    public string? InstructionLanguageName { get; set; }
}

public class CreateEventIslamicAspectDto
{
    public int? MadhabId { get; set; }
    public int? PrayerTimeOffsetMinutes { get; set; }
    public string? PrayerTimeReference { get; set; }
    public string? GenderMode { get; set; }
    public int? InstructionLanguageId { get; set; }
}
```

**New File**: `Explore.Application/DTOs/Event/Aspects/EventTechAspectDto.cs`

```csharp
namespace Explore.Application.DTOs.Event.Aspects;

public class EventTechAspectDto
{
    public string? GithubRepoUrl { get; set; }
    public List<string>? TechStack { get; set; }
    public string? SkillLevel { get; set; }
    public bool IsHandsOn { get; set; }
    public string? LiveCodingUrl { get; set; }
}

public class CreateEventTechAspectDto
{
    public string? GithubRepoUrl { get; set; }
    public List<string>? TechStack { get; set; }
    public string? SkillLevel { get; set; }
    public bool IsHandsOn { get; set; }
    public string? LiveCodingUrl { get; set; }
}
```

#### 3.4.2 Update EventDto

**Modify File**: `Explore.Application/DTOs/Event/EventDto.cs`

```csharp
namespace Explore.Application.DTOs.Event;

public class EventDto
{
    // ... existing fields ...
    
    // REMOVE: MadhabId, MadhabName
    
    // ADD: Aspects
    public List<string> AvailableAspects { get; set; } = new();
    public EventIslamicAspectDto? IslamicAspect { get; set; }
    public EventTechAspectDto? TechAspect { get; set; }
    
    // ADD: Dynamic metadata
    public Dictionary<string, object>? Metadata { get; set; }
}
```

#### 3.4.3 Update CreateEventDto

**Modify File**: `Explore.Application/DTOs/Event/CreateEventDto.cs`

```csharp
namespace Explore.Application.DTOs.Event;

public class CreateEventDto
{
    // ... existing fields ...
    
    // REMOVE: MadhabId
    
    // ADD: Optional aspects
    public CreateEventIslamicAspectDto? IslamicAspect { get; set; }
    public CreateEventTechAspectDto? TechAspect { get; set; }
    
    // ADD: Dynamic metadata
    public Dictionary<string, object>? Metadata { get; set; }
}
```

#### 3.4.4 Update AutoMapper Profiles

**Modify File**: `Explore.Application/Profiles/MappingProfile.cs`

```csharp
// Add to constructor
CreateMap<EventIslamicAspect, EventIslamicAspectDto>()
    .ForMember(d => d.MadhabName, opt => opt.MapFrom(s => s.Madhab != null ? s.Madhab.Name : null))
    .ForMember(d => d.InstructionLanguageName, opt => opt.MapFrom(s => s.InstructionLanguage != null ? s.InstructionLanguage.Name : null));

CreateMap<CreateEventIslamicAspectDto, EventIslamicAspect>();

CreateMap<EventTechAspect, EventTechAspectDto>()
    .ForMember(d => d.TechStack, opt => opt.MapFrom(s => 
        string.IsNullOrEmpty(s.TechStack) ? null : JsonSerializer.Deserialize<List<string>>(s.TechStack)));

CreateMap<CreateEventTechAspectDto, EventTechAspect>()
    .ForMember(d => d.TechStack, opt => opt.MapFrom(s => 
        s.TechStack != null ? JsonSerializer.Serialize(s.TechStack) : null));

// Update Event mapping
CreateMap<Event, EventDto>()
    .ForMember(d => d.AvailableAspects, opt => opt.MapFrom(s => GetAvailableAspects(s)))
    .ForMember(d => d.Metadata, opt => opt.MapFrom(s => 
        string.IsNullOrEmpty(s.MetadataJson) ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(s.MetadataJson)));

// Helper method
private static List<string> GetAvailableAspects(Event e)
{
    var aspects = new List<string>();
    if (e.IslamicAspect != null) aspects.Add("Islamic");
    if (e.TechAspect != null) aspects.Add("Tech");
    return aspects;
}
```

#### 3.4.5 Update Query Handlers

**Modify File**: `Explore.Application/Features/Events/Handlers/Queries/GetEventDetailsRequestHandler.cs`

```csharp
public async Task<EventDto> Handle(GetEventDetailsRequest request, CancellationToken ct)
{
    var @event = await _eventRepository.GetByIdWithIncludes(
        request.Id,
        includes: e => e
            .Include(x => x.IslamicAspect)
                .ThenInclude(a => a.Madhab)
            .Include(x => x.IslamicAspect)
                .ThenInclude(a => a.InstructionLanguage)
            .Include(x => x.TechAspect)
            .Include(x => x.Actor)
            // ... other includes
    );
    
    if (@event == null)
        throw new NotFoundException(nameof(Event), request.Id);
    
    return _mapper.Map<EventDto>(@event);
}
```

#### 3.4.6 Update Command Handlers

**Modify File**: `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`

```csharp
public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken ct)
{
    var response = new BaseCommandResponse<Guid>();
    
    // Validation
    var validator = new CreateEventDtoValidator(_eventTypeRepository, _audienceGenderRepository, ...);
    var validationResult = await validator.ValidateAsync(request.EventDto, ct);
    
    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }
    
    // Map and create event
    var @event = _mapper.Map<Event>(request.EventDto);
    @event = await _eventRepository.Create(@event);
    
    // Create aspects if provided
    if (request.EventDto.IslamicAspect != null)
    {
        var islamicAspect = _mapper.Map<EventIslamicAspect>(request.EventDto.IslamicAspect);
        islamicAspect.EventId = @event.Id;
        await _eventIslamicAspectRepository.Create(islamicAspect);
    }
    
    if (request.EventDto.TechAspect != null)
    {
        var techAspect = _mapper.Map<EventTechAspect>(request.EventDto.TechAspect);
        techAspect.EventId = @event.Id;
        await _eventTechAspectRepository.Create(techAspect);
    }
    
    response.Success = true;
    response.Id = @event.Id;
    return response;
}
```

### 3.5 Aspect Filtering

#### 3.5.1 Create Query Specification for Aspect Filtering

**New File**: `Explore.Application/Features/Events/Specifications/EventFilterSpecification.cs`

```csharp
namespace Explore.Application.Features.Events.Specifications;

/// <summary>
/// Builds EF Core queries from dynamic filter dictionaries.
/// Maps aspect filters to the appropriate relational tables.
/// </summary>
public class EventFilterSpecification
{
    private readonly Dictionary<string, string> _filters;
    
    public EventFilterSpecification(Dictionary<string, string>? filters)
    {
        _filters = filters ?? new();
    }
    
    public IQueryable<Event> Apply(IQueryable<Event> query)
    {
        foreach (var filter in _filters)
        {
            query = ApplyFilter(query, filter.Key, filter.Value);
        }
        return query;
    }
    
    private IQueryable<Event> ApplyFilter(IQueryable<Event> query, string key, string value)
    {
        return key.ToLower() switch
        {
            // Islamic aspect filters
            "madhab" => query.Where(e => e.IslamicAspect != null && 
                e.IslamicAspect.Madhab != null && 
                e.IslamicAspect.Madhab.Name.ToLower() == value.ToLower()),
            "gendermode" => query.Where(e => e.IslamicAspect != null && 
                e.IslamicAspect.GenderMode == value),
            
            // Tech aspect filters
            "skilllevel" => query.Where(e => e.TechAspect != null && 
                e.TechAspect.SkillLevel == value),
            "ishandson" => query.Where(e => e.TechAspect != null && 
                e.TechAspect.IsHandsOn == bool.Parse(value)),
            
            // Aspect existence filters
            "hasaspect" => value.ToLower() switch
            {
                "islamic" => query.Where(e => e.IslamicAspect != null),
                "tech" => query.Where(e => e.TechAspect != null),
                _ => query
            },
            
            _ => query  // Unknown filter, ignore
        };
    }
}
```

**Acceptance Criteria**:
- [ ] `EventIslamicAspect` and `EventTechAspect` entities created
- [ ] Entity configurations with 1:1 shared PK pattern
- [ ] Migration with data migration from `MadhabId`
- [ ] `MadhabId` removed from Event entity
- [ ] DTOs updated with aspect support
- [ ] AutoMapper profiles updated
- [ ] Query handlers include aspects
- [ ] Command handlers create aspects
- [ ] Aspect filtering specification working
- [ ] All existing tests updated and passing
- [ ] New tests for aspect CRUD operations

---

## Phase 4: Module Governance

**Duration**: 2 weeks  
**Complexity**: Medium  
**Risk**: Low (New feature, no breaking changes)

### Goal
Control module visibility per tenant so a "Tech Hub" never sees "Islamic" fields.

### 4.1 Domain Entities

#### 4.1.1 Create ModuleDefinition Entity

**New File**: `Explore.Domain/Modules/ModuleDefinition.cs`

```csharp
namespace Explore.Domain.Modules;

/// <summary>
/// Defines an available module (aspect category) in the system.
/// </summary>
public class ModuleDefinition
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Unique module key (e.g., "Mod_Islamic", "Mod_Tech", "Mod_Medical")
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for the module
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this module provides
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// URL to JSON schema for the wizard form fields
    /// </summary>
    public string? WizardSchemaUrl { get; set; }
    
    /// <summary>
    /// Icon for UI display (Material Design icon name)
    /// </summary>
    public string? IconName { get; set; }
    
    /// <summary>
    /// Display order in module selection UI
    /// </summary>
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// Whether this module is globally enabled
    /// </summary>
    public bool IsActive { get; set; } = true;
}
```

#### 4.1.2 Create TenantCapability Entity

**New File**: `Explore.Domain/Modules/TenantCapability.cs`

```csharp
namespace Explore.Domain.Modules;

/// <summary>
/// Links modules to tenants, controlling which aspects are available.
/// </summary>
public class TenantCapability : ITenantEntity
{
    public Guid Id { get; set; }
    
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    public Guid ModuleId { get; set; }
    public ModuleDefinition Module { get; set; } = null!;
    
    /// <summary>
    /// Whether this module is enabled for the tenant
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// When this capability was enabled
    /// </summary>
    public DateTime EnabledAt { get; set; }
    
    /// <summary>
    /// Who enabled this capability
    /// </summary>
    public Guid? EnabledBy { get; set; }
}
```

### 4.2 Persistence Layer

#### 4.2.1 Create Entity Configurations

**New File**: `Explore.Persistence/Configurations/Entities/Modules/ModuleDefinitionConfiguration.cs`

```csharp
namespace Explore.Persistence.Configurations.Entities.Modules;

public class ModuleDefinitionConfiguration : IEntityTypeConfiguration<ModuleDefinition>
{
    public void Configure(EntityTypeBuilder<ModuleDefinition> builder)
    {
        builder.ToTable("ModuleDefinitions");
        
        builder.Property(m => m.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(m => m.Key).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(500);
        builder.Property(m => m.WizardSchemaUrl).HasMaxLength(500);
        builder.Property(m => m.IconName).HasMaxLength(50);
        
        builder.HasIndex(m => m.Key).IsUnique();
        
        // Seed default modules
        builder.HasData(
            new ModuleDefinition
            {
                Id = new Guid("10000000-0000-0000-0000-000000000001"),
                Key = "Mod_Core",
                Name = "Core Events",
                Description = "Basic event functionality",
                IconName = "Event",
                DisplayOrder = 0,
                IsActive = true
            },
            new ModuleDefinition
            {
                Id = new Guid("10000000-0000-0000-0000-000000000002"),
                Key = "Mod_Islamic",
                Name = "Islamic Events",
                Description = "Islamic-specific event features (Madhab, prayer times)",
                IconName = "Mosque",
                DisplayOrder = 1,
                IsActive = true
            },
            new ModuleDefinition
            {
                Id = new Guid("10000000-0000-0000-0000-000000000003"),
                Key = "Mod_Tech",
                Name = "Tech Events",
                Description = "Tech/developer event features (GitHub, skill levels)",
                IconName = "Code",
                DisplayOrder = 2,
                IsActive = true
            }
        );
    }
}
```

**New File**: `Explore.Persistence/Configurations/Entities/Modules/TenantCapabilityConfiguration.cs`

```csharp
namespace Explore.Persistence.Configurations.Entities.Modules;

public class TenantCapabilityConfiguration : IEntityTypeConfiguration<TenantCapability>
{
    public void Configure(EntityTypeBuilder<TenantCapability> builder)
    {
        builder.ToTable("TenantCapabilities");
        
        builder.Property(c => c.Id).HasDefaultValueSql("uuidv7()");
        
        builder.HasIndex(c => new { c.TenantId, c.ModuleId }).IsUnique();
        
        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(c => c.Module)
            .WithMany()
            .HasForeignKey(c => c.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 4.3 Application Layer

#### 4.3.1 Create IModuleService Interface

**New File**: `Explore.Application/Contracts/Infrastructure/IModuleService.cs`

```csharp
namespace Explore.Application.Contracts.Infrastructure;

public interface IModuleService
{
    /// <summary>
    /// Gets all modules available to the current tenant.
    /// </summary>
    Task<List<ModuleDefinitionDto>> GetAvailableModulesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Checks if a specific module is enabled for the current tenant.
    /// </summary>
    Task<bool> IsModuleEnabledAsync(string moduleKey, CancellationToken ct = default);
    
    /// <summary>
    /// Gets the wizard schema for a module.
    /// </summary>
    Task<string?> GetModuleWizardSchemaAsync(string moduleKey, CancellationToken ct = default);
}
```

#### 4.3.2 Create Module Discovery Endpoint

**New File**: `Explore.API/Controllers/ModuleController.cs`

```csharp
namespace Explore.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class ModuleController : ControllerBase
{
    private readonly IModuleService _moduleService;
    
    [HttpGet("available")]
    [AllowAnonymous]
    [EndpointSummary("Get available modules for current tenant")]
    [EndpointDescription("Returns modules enabled for the current tenant, used to drive dynamic UI forms.")]
    [ProducesResponseType(typeof(List<ModuleDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ModuleDefinitionDto>>> GetAvailableModules()
    {
        var modules = await _moduleService.GetAvailableModulesAsync();
        return Ok(modules);
    }
    
    [HttpGet("{moduleKey}/schema")]
    [AllowAnonymous]
    [EndpointSummary("Get wizard schema for a module")]
    [EndpointDescription("Returns JSON schema for building dynamic forms for this module.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetModuleSchema(string moduleKey)
    {
        var schema = await _moduleService.GetModuleWizardSchemaAsync(moduleKey);
        if (schema == null)
            return NotFound();
        return Ok(JsonSerializer.Deserialize<object>(schema));
    }
}
```

**Acceptance Criteria**:
- [ ] `ModuleDefinition` entity with seed data
- [ ] `TenantCapability` entity with tenant filter
- [ ] `IModuleService` interface and implementation
- [ ] `GET /api/v1/modules/available` endpoint
- [ ] `GET /api/v1/modules/{moduleKey}/schema` endpoint
- [ ] Cache module queries for performance
- [ ] Unit tests for module service
- [ ] Integration tests for discovery endpoint

---

## Phase 5: Strategy Pattern

**Duration**: 2 weeks  
**Complexity**: Medium  
**Risk**: Low (New feature, extensibility)

### Goal
Enable modular business logic (like scheduling based on prayer times) that adapts at runtime.

### 5.1 Define Strategy Interface

**New File**: `Explore.Application/Contracts/Strategies/IEventStrategy.cs`

```csharp
namespace Explore.Application.Contracts.Strategies;

/// <summary>
/// Base interface for module-specific event business logic.
/// </summary>
public interface IEventStrategy
{
    /// <summary>
    /// Module key this strategy applies to
    /// </summary>
    string ModuleKey { get; }
    
    /// <summary>
    /// Validates module-specific fields
    /// </summary>
    Task<ValidationResult> ValidateAsync(CreateEventDto dto, CancellationToken ct = default);
    
    /// <summary>
    /// Applies module-specific business logic after event creation
    /// </summary>
    Task PostCreateAsync(Event @event, CancellationToken ct = default);
    
    /// <summary>
    /// Gets module-specific HATEOAS links
    /// </summary>
    IEnumerable<LinkDto> GetLinks(Event @event);
}
```

### 5.2 Create Islamic Strategy

**New File**: `Explore.Infrastructure/Strategies/IslamicEventStrategy.cs`

```csharp
namespace Explore.Infrastructure.Strategies;

public class IslamicEventStrategy : IEventStrategy
{
    private readonly IPrayerTimeService _prayerTimeService;
    
    public string ModuleKey => "Mod_Islamic";
    
    public async Task<ValidationResult> ValidateAsync(CreateEventDto dto, CancellationToken ct = default)
    {
        var result = new ValidationResult();
        
        if (dto.IslamicAspect == null)
        {
            result.Errors.Add(new ValidationFailure("IslamicAspect", "Islamic aspect is required for Islamic events"));
            return result;
        }
        
        if (dto.IslamicAspect.PrayerTimeReference != null)
        {
            var validReferences = new[] { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" };
            if (!validReferences.Contains(dto.IslamicAspect.PrayerTimeReference))
            {
                result.Errors.Add(new ValidationFailure(
                    "IslamicAspect.PrayerTimeReference", 
                    $"Invalid prayer reference. Must be one of: {string.Join(", ", validReferences)}"));
            }
        }
        
        return result;
    }
    
    public async Task PostCreateAsync(Event @event, CancellationToken ct = default)
    {
        // Calculate actual start time based on prayer offset if configured
        if (@event.IslamicAspect?.PrayerTimeReference != null && 
            @event.IslamicAspect.PrayerTimeOffsetMinutes.HasValue)
        {
            // This could trigger a background job to update session times
            // based on prayer times for the event location
        }
    }
    
    public IEnumerable<LinkDto> GetLinks(Event @event)
    {
        yield return new LinkDto
        {
            Rel = "islamic-details",
            Href = $"/api/v1/events/{@event.Id}/islamic",
            Method = "GET"
        };
    }
}
```

### 5.3 Create Strategy Resolver

**New File**: `Explore.Infrastructure/Strategies/StrategyResolver.cs`

```csharp
namespace Explore.Infrastructure.Strategies;

public class StrategyResolver : IStrategyResolver
{
    private readonly IEnumerable<IEventStrategy> _strategies;
    private readonly IModuleService _moduleService;
    
    public StrategyResolver(
        IEnumerable<IEventStrategy> strategies,
        IModuleService moduleService)
    {
        _strategies = strategies;
        _moduleService = moduleService;
    }
    
    public async Task<IEnumerable<IEventStrategy>> GetApplicableStrategiesAsync(
        CreateEventDto dto, 
        CancellationToken ct = default)
    {
        var applicableStrategies = new List<IEventStrategy>();
        
        foreach (var strategy in _strategies)
        {
            if (await _moduleService.IsModuleEnabledAsync(strategy.ModuleKey, ct))
            {
                // Check if the DTO has data for this strategy
                if (strategy.ModuleKey == "Mod_Islamic" && dto.IslamicAspect != null)
                    applicableStrategies.Add(strategy);
                else if (strategy.ModuleKey == "Mod_Tech" && dto.TechAspect != null)
                    applicableStrategies.Add(strategy);
            }
        }
        
        return applicableStrategies;
    }
    
    public async Task<ValidationResult> ValidateWithStrategiesAsync(
        CreateEventDto dto,
        CancellationToken ct = default)
    {
        var result = new ValidationResult();
        var strategies = await GetApplicableStrategiesAsync(dto, ct);
        
        foreach (var strategy in strategies)
        {
            var strategyResult = await strategy.ValidateAsync(dto, ct);
            result.Errors.AddRange(strategyResult.Errors);
        }
        
        return result;
    }
}
```

### 5.4 Register Strategies in DI

**Modify File**: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`

```csharp
// Add strategy registration
services.AddScoped<IEventStrategy, IslamicEventStrategy>();
services.AddScoped<IEventStrategy, TechEventStrategy>();
services.AddScoped<IStrategyResolver, StrategyResolver>();
```

**Acceptance Criteria**:
- [ ] `IEventStrategy` interface defined
- [ ] `IslamicEventStrategy` implemented
- [ ] `TechEventStrategy` implemented
- [ ] `StrategyResolver` working with module service
- [ ] Strategies registered in DI
- [ ] Command handlers use strategy validation
- [ ] HATEOAS links include strategy links
- [ ] Unit tests for each strategy
- [ ] Integration test for strategy resolution

---

## Phase 6: PDS Synchronization

**Duration**: 3 weeks  
**Complexity**: High  
**Risk**: Medium (External system integration)

### Goal
Implement outbox pattern for ATProto sync with hosted and external PDS support.

### 6.1 Domain Entities

#### 6.1.1 Create PdsSyncOutbox Entity

**New File**: `Explore.Domain/Federation/PdsSyncOutbox.cs`

```csharp
namespace Explore.Domain.Federation;

/// <summary>
/// Outbox table for PDS synchronization.
/// Records are created in the same transaction as business data.
/// </summary>
public class PdsSyncOutbox
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// DID of the actor to sync for
    /// </summary>
    public string Did { get; set; } = string.Empty;
    
    /// <summary>
    /// Collection in the PDS (e.g., "io.islamu.event")
    /// </summary>
    public string Collection { get; set; } = string.Empty;
    
    /// <summary>
    /// Record key in the collection
    /// </summary>
    public string RecordKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Operation type: "create", "update", "delete"
    /// </summary>
    public string Operation { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON payload to sync (null for delete)
    /// </summary>
    public string? Payload { get; set; }
    
    /// <summary>
    /// When this outbox entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When this entry was successfully processed (null if pending)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; }
    
    /// <summary>
    /// Last error message if processing failed
    /// </summary>
    public string? LastError { get; set; }
    
    /// <summary>
    /// Status: "pending", "processing", "completed", "failed"
    /// </summary>
    public string Status { get; set; } = "pending";
}
```

### 6.2 Create IPdsService Interface

**New File**: `Explore.Application/Contracts/Infrastructure/IPdsService.cs`

```csharp
namespace Explore.Application.Contracts.Infrastructure;

public interface IPdsService
{
    /// <summary>
    /// Hosts a record on the Islamu PDS for users using our hosted identity.
    /// </summary>
    Task<PdsWriteResult> HostRecordAsync(
        string did,
        string collection,
        string recordKey,
        object record,
        CancellationToken ct = default);
    
    /// <summary>
    /// Proxies a record to an external PDS for users with their own PDS.
    /// </summary>
    Task<PdsWriteResult> ProxyRecordAsync(
        string remotePdsUrl,
        string did,
        string collection,
        string recordKey,
        object record,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a record from the PDS.
    /// </summary>
    Task<PdsWriteResult> DeleteRecordAsync(
        string did,
        string collection,
        string recordKey,
        CancellationToken ct = default);
    
    /// <summary>
    /// Resolves which PDS should be used for a given actor.
    /// </summary>
    Task<PdsResolution> ResolvePdsAsync(Guid actorId, CancellationToken ct = default);
}

public record PdsWriteResult(bool Success, string? Cid, string? Uri, string? Error);
public record PdsResolution(bool IsHosted, string PdsUrl, string Did);
```

### 6.3 Create SaveChanges Interceptor for Outbox

**New File**: `Explore.Persistence/Interceptors/PdsSyncInterceptor.cs`

```csharp
namespace Explore.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that creates outbox entries for federated entities.
/// </summary>
public class PdsSyncInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var context = eventData.Context as ExploreDbContext;
        if (context == null) return await base.SavingChangesAsync(eventData, result, ct);
        
        var outboxEntries = new List<PdsSyncOutbox>();
        
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is Event @event && entry.State is EntityState.Added or EntityState.Modified)
            {
                // Only create outbox entry if event has ATProto record
                if (@event.AtprotoRecordId.HasValue)
                {
                    outboxEntries.Add(new PdsSyncOutbox
                    {
                        Id = Guid.CreateVersion7(),
                        Did = @event.Actor?.Did ?? "",  // Need to resolve
                        Collection = "io.islamu.event",
                        RecordKey = @event.Slug ?? @event.Id.ToString(),
                        Operation = entry.State == EntityState.Added ? "create" : "update",
                        Payload = JsonSerializer.Serialize(MapToAtprotoRecord(@event)),
                        CreatedAt = DateTime.UtcNow,
                        Status = "pending"
                    });
                }
            }
        }
        
        if (outboxEntries.Any())
        {
            await context.PdsSyncOutbox.AddRangeAsync(outboxEntries, ct);
        }
        
        return await base.SavingChangesAsync(eventData, result, ct);
    }
    
    private object MapToAtprotoRecord(Event @event)
    {
        // Map Event to ATProto Lexicon format
        return new
        {
            type = "io.islamu.event",
            title = @event.Title,
            description = @event.Description,
            createdAt = @event.CreatedAt.ToString("O")
        };
    }
}
```

### 6.4 Create Background Worker

**New File**: `Explore.Infrastructure/BackgroundServices/PdsSyncWorker.cs`

```csharp
namespace Explore.Infrastructure.BackgroundServices;

public class PdsSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PdsSyncWorker> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingOutboxEntriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PDS sync outbox");
            }
            
            await Task.Delay(PollingInterval, stoppingToken);
        }
    }
    
    private async Task ProcessPendingOutboxEntriesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var pdsService = scope.ServiceProvider.GetRequiredService<IPdsService>();
        
        var pendingEntries = await dbContext.PdsSyncOutbox
            .Where(e => e.Status == "pending" && e.RetryCount < 3)
            .OrderBy(e => e.CreatedAt)
            .Take(10)
            .ToListAsync(ct);
        
        foreach (var entry in pendingEntries)
        {
            entry.Status = "processing";
            await dbContext.SaveChangesAsync(ct);
            
            try
            {
                var result = entry.Operation switch
                {
                    "create" or "update" => await pdsService.HostRecordAsync(
                        entry.Did, entry.Collection, entry.RecordKey, 
                        JsonSerializer.Deserialize<object>(entry.Payload!), ct),
                    "delete" => await pdsService.DeleteRecordAsync(
                        entry.Did, entry.Collection, entry.RecordKey, ct),
                    _ => new PdsWriteResult(false, null, null, $"Unknown operation: {entry.Operation}")
                };
                
                if (result.Success)
                {
                    entry.Status = "completed";
                    entry.ProcessedAt = DateTime.UtcNow;
                }
                else
                {
                    entry.Status = "pending";
                    entry.RetryCount++;
                    entry.LastError = result.Error;
                }
            }
            catch (Exception ex)
            {
                entry.Status = "pending";
                entry.RetryCount++;
                entry.LastError = ex.Message;
            }
            
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
```

**Acceptance Criteria**:
- [ ] `PdsSyncOutbox` entity created
- [ ] `IPdsService` interface defined
- [ ] SaveChanges interceptor creates outbox entries
- [ ] Background worker processes outbox
- [ ] Retry logic with exponential backoff
- [ ] Error logging and monitoring
- [ ] Support for hosted PDS
- [ ] Support for external PDS proxy
- [ ] Unit tests for outbox logic
- [ ] Integration tests with mock PDS

---

## Phase 7: Virtual Tenant Masking

**Duration**: 1 week  
**Complexity**: Low  
**Risk**: Low (Configuration change only)

### Goal
Support single-tenant mode while keeping multi-tenant codebase.

### 7.1 Create Deployment Mode Configuration

**New File**: `Explore.Application/Settings/DeploymentSettings.cs`

```csharp
namespace Explore.Application.Settings;

public class DeploymentSettings
{
    public const string SectionName = "Deployment";
    
    /// <summary>
    /// Deployment mode: "SingleTenant" or "MultiTenant"
    /// </summary>
    public string Mode { get; set; } = "MultiTenant";
    
    /// <summary>
    /// Default tenant ID to use in SingleTenant mode
    /// </summary>
    public Guid DefaultTenantId { get; set; } = SeedIds.DefaultTenantId;
    
    /// <summary>
    /// Whether to hide SuperAdmin UI in SingleTenant mode
    /// </summary>
    public bool HideSuperAdminInSingleTenant { get; set; } = true;
}
```

### 7.2 Modify TenantContext Middleware

**Modify File**: `Explore.Infrastructure/Services/TenantContext.cs`

```csharp
namespace Explore.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<DeploymentSettings> _deploymentSettings;
    private readonly ITenantRepository _tenantRepository;
    
    public Guid TenantId
    {
        get
        {
            // Single-tenant mode: always return default tenant
            if (_deploymentSettings.Value.Mode == "SingleTenant")
            {
                return _deploymentSettings.Value.DefaultTenantId;
            }
            
            // Multi-tenant mode: resolve from request
            return ResolveTenantFromRequest();
        }
    }
    
    private Guid ResolveTenantFromRequest()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return _deploymentSettings.Value.DefaultTenantId;
        
        // 1. Check header
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            if (Guid.TryParse(tenantIdHeader, out var headerTenantId))
                return headerTenantId;
        }
        
        // 2. Check subdomain
        var host = httpContext.Request.Host.Host;
        var subdomain = GetSubdomain(host);
        if (!string.IsNullOrEmpty(subdomain))
        {
            var tenant = _tenantRepository.GetBySlug(subdomain);
            if (tenant != null)
                return tenant.Id;
        }
        
        // 3. Fallback to default
        return _deploymentSettings.Value.DefaultTenantId;
    }
    
    private string? GetSubdomain(string host)
    {
        var parts = host.Split('.');
        return parts.Length > 2 ? parts[0] : null;
    }
}
```

### 7.3 Block SuperAdmin in SingleTenant Mode

**New File**: `Explore.API/Filters/SingleTenantGuardAttribute.cs`

```csharp
namespace Explore.API.Filters;

/// <summary>
/// Blocks access to endpoints in SingleTenant deployment mode.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class BlockInSingleTenantAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var settings = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<DeploymentSettings>>();
        
        if (settings.Value.Mode == "SingleTenant" && 
            settings.Value.HideSuperAdminInSingleTenant)
        {
            context.Result = new NotFoundResult();
        }
    }
}
```

**Usage**:
```csharp
[Route("api/v1/admin/tenants")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[BlockInSingleTenant]  // This controller is hidden in SingleTenant mode
public class TenantAdminController : ControllerBase
{
    // ...
}
```

**Acceptance Criteria**:
- [ ] `DeploymentSettings` configuration class
- [ ] `TenantContext` respects deployment mode
- [ ] `BlockInSingleTenant` attribute working
- [ ] SuperAdmin controllers blocked in single-tenant
- [ ] Configuration in appsettings.json
- [ ] Documentation for deployment modes
- [ ] Unit tests for tenant resolution
- [ ] Integration tests for both modes

---

## Phase 8: Integration & Testing

**Duration**: 2 weeks  
**Complexity**: Medium  
**Risk**: Medium (Cross-cutting changes)

### Goal
Update all handlers, DTOs, controllers, and tests to work with the new architecture.

### 8.1 Update All Event Handlers

**Tasks**:
- [ ] Update `CreateEventCommandHandler` with aspect support
- [ ] Update `UpdateEventCommandHandler` with aspect CRUD
- [ ] Update `DeleteEventCommandHandler` (cascading delete handles aspects)
- [ ] Update `GetEventDetailsRequestHandler` with aspect includes
- [ ] Update `GetEventListRequestHandler` with aspect filtering

### 8.2 Update HATEOAS

**Modify File**: `Explore.API/Hateoas/EventLinkPolicy.cs`

```csharp
public IEnumerable<LinkDto> GetLinks(EventDto @event, HttpContext context)
{
    // Base links
    yield return new LinkDto { Rel = "self", Href = $"/api/v1/events/{@event.Id}", Method = "GET" };
    yield return new LinkDto { Rel = "update", Href = $"/api/v1/events/{@event.Id}", Method = "PUT" };
    yield return new LinkDto { Rel = "delete", Href = $"/api/v1/events/{@event.Id}", Method = "DELETE" };
    
    // Aspect-specific links
    if (@event.AvailableAspects.Contains("Islamic"))
    {
        yield return new LinkDto { Rel = "islamic-aspect", Href = $"/api/v1/events/{@event.Id}/islamic", Method = "GET" };
    }
    
    if (@event.AvailableAspects.Contains("Tech"))
    {
        yield return new LinkDto { Rel = "tech-aspect", Href = $"/api/v1/events/{@event.Id}/tech", Method = "GET" };
    }
}
```

### 8.3 Create Integration Tests

**New Test File**: `Event.API.IntegrationTests/Features/Aspects/EventAspectTests.cs`

```csharp
namespace Event.API.IntegrationTests.Features.Aspects;

public class EventAspectTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateEvent_WithIslamicAspect_ReturnsAspectInResponse()
    {
        // Arrange
        var createDto = new CreateEventDto
        {
            Title = "Fiqh Class",
            EventTypeId = 1,
            IslamicAspect = new CreateEventIslamicAspectDto
            {
                MadhabId = 1,
                GenderMode = "Segregated"
            }
        };
        
        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/events", createDto);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        
        var eventResponse = await Client.GetFromJsonAsync<EventDto>($"/api/v1/events/{result.Id}");
        Assert.Contains("Islamic", eventResponse.AvailableAspects);
        Assert.NotNull(eventResponse.IslamicAspect);
        Assert.Equal(1, eventResponse.IslamicAspect.MadhabId);
    }
    
    [Fact]
    public async Task GetEvents_FilterByMadhab_ReturnsOnlyMatchingEvents()
    {
        // Arrange - create events with different madhabs
        
        // Act
        var response = await Client.GetFromJsonAsync<List<EventListDto>>(
            "/api/v1/events?filters[madhab]=Hanafi");
        
        // Assert
        Assert.All(response, e => Assert.Contains("Islamic", e.AvailableAspects));
    }
}
```

### 8.4 Update Unit Tests

- [ ] Update all Event handler unit tests
- [ ] Add unit tests for settings resolver
- [ ] Add unit tests for module service
- [ ] Add unit tests for strategy resolver
- [ ] Add unit tests for PDS sync logic

### 8.5 Documentation Updates

- [ ] Update `docs/ARCHITECTURE.md` with aspect pattern
- [ ] Update `docs/API.md` with new endpoints
- [ ] Create `docs/MODULAR_EXTENSIBILITY.md` with module guide
- [ ] Update `docs/DOMAIN.md` with new entities
- [ ] Update OpenAPI documentation

**Acceptance Criteria**:
- [ ] All existing tests passing
- [ ] New integration tests for aspects
- [ ] New integration tests for modules
- [ ] New integration tests for settings
- [ ] HATEOAS includes aspect links
- [ ] Documentation complete
- [ ] No breaking changes to public API (deprecation warnings only)
- [ ] Performance benchmarks maintained

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Data migration failure | Medium | High | Test migration on staging; have rollback plan |
| EF Core 10 named filter issues | Low | Medium | Verify EF Core 10 support; fallback to combined filters |
| PDS sync failures | Medium | Medium | Outbox pattern ensures eventual consistency; monitoring |
| Performance degradation from joins | Low | Medium | Benchmark aspect queries; add indexes |
| Breaking API changes | Medium | High | Version API; deprecation warnings; migration period |
| Module governance too restrictive | Low | Low | Allow admin overrides; clear documentation |

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Migration success | 100% data migrated | Zero data loss |
| API response time | <200ms p95 | APM monitoring |
| Test coverage | >80% | CodeCov |
| Zero downtime | 0 minutes | Deployment monitoring |
| Aspect query performance | <50ms | Query profiling |
| PDS sync latency | <30s average | Outbox metrics |

---

## Dependencies & Prerequisites

### Technical Prerequisites

1. **.NET 10.0 SDK** - Required for EF Core 10 named filters
2. **PostgreSQL 16+** - Required for `uuidv7()` function
3. **EF Core 10.0** - Named query filter support
4. **Npgsql 9.0+** - UUID v7 support

### External Dependencies

1. **ATProto Lexicon definitions** - For PDS sync
2. **Prayer time API** (optional) - For Islamic scheduling strategy

### Team Prerequisites

1. Database migration approval from stakeholders
2. API versioning strategy agreed upon
3. Staging environment for migration testing
4. Monitoring/alerting for PDS sync

---

## Appendix: File Changes Summary

### New Files to Create

```
Explore.Domain/
├── Aspects/
│   ├── EventIslamicAspect.cs
│   └── EventTechAspect.cs
├── Modules/
│   ├── ModuleDefinition.cs
│   └── TenantCapability.cs
├── Federation/
│   └── PdsSyncOutbox.cs
└── SystemSetting.cs

Explore.Application/
├── Contracts/
│   ├── Infrastructure/
│   │   ├── ISettingsResolver.cs
│   │   ├── IModuleService.cs
│   │   └── IPdsService.cs
│   └── Strategies/
│       └── IEventStrategy.cs
├── DTOs/Event/Aspects/
│   ├── EventIslamicAspectDto.cs
│   └── EventTechAspectDto.cs
├── Features/Events/Specifications/
│   └── EventFilterSpecification.cs
└── Settings/
    └── DeploymentSettings.cs

Explore.Persistence/
├── Configurations/Entities/
│   ├── Aspects/
│   │   ├── EventIslamicAspectConfiguration.cs
│   │   └── EventTechAspectConfiguration.cs
│   ├── Modules/
│   │   ├── ModuleDefinitionConfiguration.cs
│   │   └── TenantCapabilityConfiguration.cs
│   └── SystemSettingConfiguration.cs
├── Interceptors/
│   └── PdsSyncInterceptor.cs
└── Extensions/
    └── QueryableExtensions.cs

Explore.Infrastructure/
├── Services/
│   ├── SettingsResolver.cs
│   ├── ModuleService.cs
│   └── PdsService.cs
├── Strategies/
│   ├── IslamicEventStrategy.cs
│   ├── TechEventStrategy.cs
│   └── StrategyResolver.cs
└── BackgroundServices/
    └── PdsSyncWorker.cs

Explore.API/
├── Controllers/
│   └── ModuleController.cs
└── Filters/
    └── SingleTenantGuardAttribute.cs
```

### Files to Modify

```
Explore.Domain/
├── Event.cs (remove MadhabId, add aspects, add MetadataJson)
└── TenantSettings.cs (rename to TenantSetting, add Key/Value)

Explore.Persistence/
├── ExploreDbContext.cs (add DbSets, refactor query filters)
└── Configurations/Entities/
    └── EventConfiguration.cs (remove Madhab relationship)

Explore.Application/
├── DTOs/Event/
│   ├── EventDto.cs (add aspects)
│   ├── CreateEventDto.cs (add aspects)
│   └── UpdateEventDto.cs (add aspects)
├── Features/Events/Handlers/
│   ├── Commands/*.cs (aspect handling)
│   └── Queries/*.cs (aspect includes)
└── Profiles/
    └── MappingProfile.cs (aspect mappings)

Explore.Infrastructure/
└── InfrastructureServicesRegistration.cs (register new services)

Explore.API/
└── Hateoas/
    └── EventLinkPolicy.cs (aspect links)
```

---

**Document Version**: 1.0  
**Author**: Claude Code Agent  
**Status**: Ready for Review
