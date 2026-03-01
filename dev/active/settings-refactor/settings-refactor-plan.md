# Settings Architecture Refactor — Implementation Plan

**Last Updated: 2026-02-27**

---

## Executive Summary

The current settings system has accumulated significant technical debt through organic growth. Settings logic is spread across **4 different entity models** (`SystemSetting`, `TenantSetting`, `TenantSettings`, `AppSetting`), **3+ service silos** with copy-pasted helper methods, and **only supports 2 of the 5 hierarchy levels** documented in `ADMIN_HIERARCHY.md`. This plan unifies settings into a single architectural pattern: a **code-defined Setting Definition Registry** + **per-scope EAV tables** + **hierarchical cascade resolver** + **strongly-typed setting groups**.

The goal: any setting can be defined once, resolved through the full hierarchy (Instance → Tenant → Organization → Group → User), cached efficiently, audited, and accessed via strongly-typed C# classes — with zero copy-pasted deserialization code.

---

## Current State Analysis (Verified)

### Entities

| Entity | File | Purpose | Debt |
|--------|------|---------|------|
| `SystemSetting` | `Explore.Domain/SystemSetting.cs` | Instance-level EAV settings (key/value/type/lock) | Works but semantics conflated — serves as both "definition" and "instance value" |
| `TenantSetting` | `Explore.Domain/TenantSetting.cs` | Tenant-scoped key/value overrides | Works, but limited to tenant scope only |
| `TenantSettings` | `Explore.Domain/TenantSettings.cs` | **LEGACY** strongly-typed 1:1 per-tenant settings | Pre-dates EAV model. Duplicates what TenantSetting does. Confusing naming clash. |
| `AppSetting` | `Explore.Domain/AppSetting.cs` | Encrypted operational settings | Different concern (secrets), but key notation inconsistent (colon vs dot) |
| `ConfigurationChangeLog` | `Explore.Domain/ConfigurationChangeLog.cs` | Audit trail | Adequate, but not consistently called from all write paths |

### Services (Verified Exists)

| Service | File | Debt |
|---------|------|------|
| `SettingsResolver` | `Explore.Infrastructure/Services/SettingsResolver.cs` | Only 2-tier cascade (System → Tenant). No Org/Group/User. |
| `InstanceGovernanceSettingService` | `Explore.Application/Services/InstanceGovernanceSettingService.cs` | **30+ individual `GetByKey()` calls** (N+1). 600+ lines. |
| `InstanceStorageSettingService` | `Explore.Application/Services/InstanceStorageSettingService.cs` | Copy-pasted `DeserializeString/Int/Bool` + `UpsertSystemSettingAsync` |
| `InstanceSmtpSettingService` | `Explore.Application/Services/InstanceSmtpSettingService.cs` | Same copy-paste as above |
| `TenantPolicySettingService` | `Explore.Application/Services/TenantPolicySettingService.cs` | **16+ system + 11 tenant GetByKey() calls**. Manually resolves cascade per field. |
| `S3ConfigResolver` | `Explore.Infrastructure/Storage/S3ConfigResolver.cs` | Own cache layer + IConfiguration fallback |
| `SmtpConfigResolver` | `Explore.Infrastructure/Mail/SmtpConfigResolver.cs` | Own cache layer |
| `AnalyticsConfigResolver` | `Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs` | Own cache layer |
| `CerbosConfigResolver` | `Explore.Infrastructure/Services/CerbosConfigResolver.cs` | Own cache layer |

### Constants (Verified Exists)

| File | Lines | Debt |
|------|-------|------|
| `GovernanceSettingKeys` | ~219 lines | Legacy flat aliases duplicate nested class keys |
| `InfrastructureSecretSettingKeys` | ~25 lines | Separate from GovernanceSettingKeys but stored in same SystemSetting table |

### Missing (Verified via grep: no `UserSetting`, `OrganizationSetting`, `GroupSetting` entities exist)

- **No Organization-level settings** — `ConfigurationScopeEnum` has `Organization = 3` but no entity or resolution logic
- **No Group-level settings** — `Group` entity exists but has zero settings capability
- **No User-level preferences** — `ADMIN_HIERARCHY.md` documents "User Preference" as the 4th cascade level, but no implementation

---

## Identified Tech Debt (Ranked by Impact)

### Critical

1. **N+1 Query Pattern** — Every settings read makes 15-30+ individual DB roundtrips (one per key). In `TenantPolicySettingService.ReadEffectiveTenantSettingsAsync()`, 27 separate `GetByKey()` calls fire for a single request.

2. **Copy-Pasted Code** — `DeserializeString`, `DeserializeInt`, `DeserializeBoolean`, and `UpsertSystemSettingAsync` are duplicated across 3+ services verbatim (~100 duplicated lines).

3. **Uncoordinated Multi-Layer Caching** — 5 independent `IMemoryCache` layers (`SettingsResolver`, `S3ConfigResolver`, `SmtpConfigResolver`, `AnalyticsConfigResolver`, `CerbosConfigResolver`) with no coordination. Cache invalidation in one layer doesn't propagate to others.

### High

4. **Only 2 of 5 Hierarchy Levels Implemented** — `ADMIN_HIERARCHY.md` specifies System → Instance → Tenant → Organization → User. Only System/Tenant exist in code.

5. **Legacy `TenantSettings` Entity** — Strongly-typed entity predating the EAV model. Creates naming confusion (`TenantSettings` vs `TenantSetting`). Two repos, two configurations, two DTOs — for the same concept.

6. **Setting Definitions Scattered** — Metadata about each setting (type, default, description, allowed values, lockability) is split across: seed data (`LookupTableSeeder`), `SystemSetting` entity properties, and hardcoded in service logic.

### Medium

7. **Inconsistent Key Notation** — GovernanceSettingKeys uses dot-notation (`email.smtp_host`), AppSetting uses colon-notation (`Smtp:Host`), IConfiguration uses colon-notation. No single canonical form.

8. **No Validation Pipeline** — Settings are written without formal validation of value against type or constraints. `AllowedValues` exists on `SystemSetting` but is never checked on write.

9. **Mixed Secret/Non-Secret Storage** — SMTP credentials are stored as plain JSON in `SystemSetting` alongside governance settings, despite `AppSetting` existing for encrypted secrets.

10. **No Distributed Cache Support** — All caching uses `IMemoryCache`. Multi-server deployments get stale reads after one server's cache is invalidated.

---

## Proposed Architecture

### Design Principles

1. **Define Once, Resolve Everywhere** — Every setting is defined in a single registry with its metadata.
2. **Hierarchical Cascade with Lock Semantics** — Instance → Tenant → Organization → Group → User, with lock-at-any-level.
3. **Batch Resolution** — Load all settings for a scope chain in ≤2 queries, not N.
4. **Strongly-Typed Access** — Consumers get `IEmailSettings`, not `string settingKey`.
5. **Unified Caching** — One cache strategy, composite keys, coordinated invalidation.
6. **Audit Everything** — Every write goes through a central service that logs changes.

### Component Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    CONSUMERS                                 │
│  Handlers │ ConfigResolvers │ Controllers │ Blazor Pages     │
└──────────────┬──────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────┐
│           STRONGLY-TYPED SETTING GROUPS                      │
│  EmailSettings │ StorageSettings │ BrandingSettings │ ...    │
│  (Auto-resolved via ISettingGroupResolver<T>)                │
└──────────────┬──────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────┐
│           HIERARCHICAL SETTINGS RESOLVER                     │
│  IHierarchicalSettingsResolver                               │
│  - ResolveAsync(key, context)                                │
│  - ResolveGroupAsync<T>(context)                             │
│  - ResolveBatchAsync(keys[], context)                        │
│  - Lock checking at each scope level                         │
└──────────────┬──────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────┐   ┌──────────────────────────────┐
│   SETTING DEFINITION    │   │     UNIFIED CACHE LAYER      │
│   REGISTRY              │   │  ISettingsCacheManager        │
│  (Code-defined, static) │   │  - Composite keys             │
│  Key, Type, Default,    │   │  - Scope-aware invalidation   │
│  AllowedScopes,         │   │  - IMemoryCache +             │
│  Validation, Category   │   │    IDistributedCache ready    │
└─────────────────────────┘   └──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────┐
│           PER-SCOPE VALUE TABLES (EAV)                       │
│  SystemSetting (Instance) ──────── existing, evolved         │
│  TenantSetting ─────────────────── existing, kept            │
│  OrganizationSetting ───────────── NEW                       │
│  GroupSetting ──────────────────── NEW                       │
│  UserPreference ────────────────── NEW                       │
│                                                              │
│  ConfigurationChangeLog ────────── existing, enhanced        │
└─────────────────────────────────────────────────────────────┘
```

### Setting Definition Registry (Domain Layer)

Instead of a DB table (which creates chicken-and-egg bootstrapping issues), setting definitions are **code-defined** in `Explore.Domain`:

```csharp
// Explore.Domain/Settings/SettingDefinition.cs
public sealed record SettingDefinition(
    string Key,
    SettingValueType ValueType,
    string DefaultValue,
    string Category,
    string Description,
    SettingScope MinScope = SettingScope.Instance,  
    SettingScope MaxScope = SettingScope.User,
    bool IsLockable = true,
    bool IsSensitive = false,
    string[]? AllowedValues = null,
    Func<string, bool>? ValidationRule = null);

// Explore.Domain/Settings/SettingScope.cs
public enum SettingScope
{
    Instance = 0,
    Tenant = 1,
    Organization = 2,
    Group = 3,
    User = 4
}
```

Setting definitions are organized by category in static registry classes:

```csharp
// Explore.Domain/Settings/Definitions/EmailSettingDefinitions.cs
public static class EmailSettingDefinitions
{
    public static readonly SettingDefinition SmtpHost = new(
        Key: "email.smtp_host",
        ValueType: SettingValueType.String,
        DefaultValue: "",
        Category: "Email",
        Description: "SMTP server hostname",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Tenant);
    
    // ... all email settings
    
    public static IEnumerable<SettingDefinition> All => [SmtpHost, SmtpPort, ...];
}

// Explore.Domain/Settings/SettingRegistry.cs  
public static class SettingRegistry
{
    private static readonly Dictionary<string, SettingDefinition> _definitions = new();
    
    static SettingRegistry()
    {
        Register(EmailSettingDefinitions.All);
        Register(StorageSettingDefinitions.All);
        Register(BrandingSettingDefinitions.All);
        // ... etc
    }
    
    public static SettingDefinition? Get(string key) => _definitions.GetValueOrDefault(key);
    public static IReadOnlyCollection<SettingDefinition> GetByCategory(string category) => ...;
    public static IReadOnlyCollection<SettingDefinition> All => _definitions.Values;
}
```

### Hierarchical Resolver (Application Layer Contract, Infrastructure Implementation)

```csharp
// Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs
public interface IHierarchicalSettingsResolver
{
    Task<T?> ResolveAsync<T>(string key, SettingContext context, CancellationToken ct = default);
    Task<ResolvedSetting?> ResolveWithMetadataAsync(string key, SettingContext context, CancellationToken ct = default);
    Task<IReadOnlyList<ResolvedSetting>> ResolveBatchAsync(IEnumerable<string> keys, SettingContext context, CancellationToken ct = default);
    Task<TGroup> ResolveGroupAsync<TGroup>(SettingContext context, CancellationToken ct = default) where TGroup : ISettingGroup, new();
    Task SetValueAsync(string key, string value, SettingScope scope, Guid scopeId, Guid actorId, CancellationToken ct = default);
    Task RemoveOverrideAsync(string key, SettingScope scope, Guid scopeId, Guid actorId, CancellationToken ct = default);
    Task LockAsync(string key, SettingScope scope, Guid scopeId, Guid actorId, CancellationToken ct = default);
    void InvalidateCache(SettingScope? scope = null, Guid? scopeId = null);
}

// Explore.Application/Models/SettingContext.cs
public sealed record SettingContext(
    Guid? TenantId = null,
    Guid? OrganizationId = null,
    Guid? GroupId = null,
    Guid? UserId = null);
```

### Strongly-Typed Setting Groups (Application Layer)

```csharp
// Explore.Application/Settings/ISettingGroup.cs
public interface ISettingGroup
{
    static abstract IEnumerable<string> SettingKeys { get; }
    void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings);
}

// Explore.Application/Settings/Groups/EmailSettingGroup.cs
public class EmailSettingGroup : ISettingGroup
{
    public string SmtpHost { get; private set; } = "";
    public int SmtpPort { get; private set; } = 587;
    public string SmtpSecurity { get; private set; } = "StartTls";
    // ... all email properties
    
    public static IEnumerable<string> SettingKeys => EmailSettingDefinitions.All.Select(d => d.Key);
    
    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings) { ... }
}
```

### New Scope Entities (Domain Layer)

```csharp
// Same shape as TenantSetting but with OrganizationId
public class OrganizationSetting : IAuditableEntity { ... }
public class GroupSetting : IAuditableEntity { ... }
public class UserPreference : IAuditableEntity { ... }
```

---

## Implementation Phases

### Phase 1: Foundation — Setting Definition Registry & Shared Utilities
**Layer**: Domain + Application  
**Effort**: L  
**Risk**: Low (additive, no breaking changes)

#### Task 1.1: Create Setting Definition Types
- **Files**: `Explore.Domain/Settings/SettingDefinition.cs`, `SettingScope.cs`
- **Acceptance**: Record type compiles, `SettingScope` enum has 5 levels
- **Effort**: S
- **Skill**: `clean-architecture-rules`

#### Task 1.2: Create Setting Definition Registry
- **File**: `Explore.Domain/Settings/SettingRegistry.cs`
- **Acceptance**: Static dictionary, `Get(key)` returns definition, `All` returns all
- **Effort**: S

#### Task 1.3: Create Per-Category Definition Classes
- **Files**: `Explore.Domain/Settings/Definitions/EmailSettingDefinitions.cs`, `StorageSettingDefinitions.cs`, `BrandingSettingDefinitions.cs`, `DeploymentSettingDefinitions.cs`, `EventSettingDefinitions.cs`, `OrganizationSettingDefinitions.cs`, `ModuleSettingDefinitions.cs`, `DomainSettingDefinitions.cs`, `AnalyticsSettingDefinitions.cs`, `SecuritySettingDefinitions.cs`, `RoutingSettingDefinitions.cs`, `TenantSettingDefinitions.cs`
- **Acceptance**: Every key in `GovernanceSettingKeys` + `InfrastructureSecretSettingKeys` has a corresponding definition. Unit test validates no orphan keys.
- **Effort**: M

#### Task 1.4: Extract Shared SettingValueSerializer
- **File**: `Explore.Application/Settings/SettingValueSerializer.cs`
- **Acceptance**: Single static class with `Deserialize<T>(string?, T default)`, `Serialize(object)`. Replace all copy-pasted `DeserializeString/Int/Bool` methods.
- **Effort**: S

#### Task 1.5: Extract Shared SettingUpsertService
- **File**: `Explore.Application/Settings/SettingUpsertService.cs`  
- **Acceptance**: Single service handles upsert for any scope level. Replaces 3 copy-pasted `UpsertSystemSettingAsync` methods.
- **Effort**: S

#### Task 1.6: Unit Tests for Registry & Serializer
- **Files**: `Event.Application.UnitTests/Settings/SettingRegistryTests.cs`, `SettingValueSerializerTests.cs`
- **Acceptance**: All definitions valid, serializer handles edge cases (null, empty, malformed JSON)
- **Effort**: S

---

### Phase 2: Hierarchical Resolver — Replace SettingsResolver
**Layer**: Application (contract) + Infrastructure (impl)  
**Effort**: L  
**Risk**: Medium (replaces core resolution engine)

#### Task 2.1: Define IHierarchicalSettingsResolver Contract
- **File**: `Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs`
- **Acceptance**: Interface defines Resolve, ResolveBatch, ResolveGroup, Set, Remove, Lock, InvalidateCache
- **Effort**: S

#### Task 2.2: Define SettingContext Value Object
- **File**: `Explore.Application/Models/SettingContext.cs`
- **Acceptance**: Immutable record with TenantId, OrganizationId, GroupId, UserId
- **Effort**: S

#### Task 2.3: Implement HierarchicalSettingsResolver
- **File**: `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`
- **Acceptance**: 
  - Batch-loads settings for all scopes in ≤2 queries (system + scoped)
  - Cascades: Instance → Tenant → Org → Group → User with lock-at-each-level
  - Validates writes against SettingDefinition (scope range, allowed values)
  - Calls ConfigurationChangeLogService on every write
- **Effort**: L
- **Skill**: `clean-architecture-rules`, `dotnet-efcore-guidelines`

#### Task 2.4: Implement Unified SettingsCacheManager
- **File**: `Explore.Infrastructure/Services/SettingsCacheManager.cs`
- **Acceptance**: 
  - Composite cache keys: `Settings:{scope}:{scopeId}`
  - Scope-aware invalidation (invalidating tenant also invalidates child org/group/user)
  - Uses IMemoryCache now, interface supports IDistributedCache later
- **Effort**: M

#### Task 2.5: Adapter — Keep ISettingsResolver Working (Backward Compat)
- **File**: `Explore.Infrastructure/Services/SettingsResolver.cs`
- **Acceptance**: Existing `ISettingsResolver` delegates to `IHierarchicalSettingsResolver`. Zero breakage to existing consumers.
- **Effort**: S

#### Task 2.6: Unit Tests for Hierarchical Resolver
- **Files**: `Event.Application.UnitTests/Settings/HierarchicalSettingsResolverTests.cs`
- **Acceptance**: Tests cover: basic cascade, lock semantics at each level, batch loading, cache invalidation, setting outside allowed scope rejected
- **Effort**: M

---

### Phase 3: New Scope Entities — Org, Group, User
**Layer**: Domain + Persistence  
**Effort**: M  
**Risk**: Low (additive DB schema)

#### Task 3.1: Create OrganizationSetting Entity
- **File**: `Explore.Domain/OrganizationSetting.cs`
- **Acceptance**: Same shape as TenantSetting but with OrganizationId FK. Implements ITenantEntity (org settings are scoped within a tenant), IAuditableEntity.
- **Effort**: S

#### Task 3.2: Create GroupSetting Entity
- **File**: `Explore.Domain/GroupSetting.cs`
- **Acceptance**: Same shape with GroupId FK. Implements ITenantEntity, IAuditableEntity.
- **Effort**: S

#### Task 3.3: Create UserPreference Entity
- **File**: `Explore.Domain/UserPreference.cs`
- **Acceptance**: UserId + SettingKey + Value. NOT tenant-scoped (user prefs follow the user). Implements IAuditableEntity.
- **Effort**: S

#### Task 3.4: EF Configurations + Repositories
- **Files**: `Explore.Persistence/Configurations/Entities/OrganizationSettingConfiguration.cs`, `GroupSettingConfiguration.cs`, `UserPreferenceConfiguration.cs`
- **Files**: `Explore.Persistence/Repositories/OrganizationSettingRepository.cs`, `GroupSettingRepository.cs`, `UserPreferenceRepository.cs`
- **Acceptance**: Unique index on (ScopeId + SettingKey). Query filters applied. Repos registered in DI.
- **Effort**: M
- **Skill**: `dotnet-efcore-guidelines`

#### Task 3.5: EF Migration
- **Acceptance**: `dotnet ef migrations add AddHierarchicalSettings` succeeds. Migration creates 3 new tables.
- **Effort**: S

#### Task 3.6: Wire New Repos into Resolver
- **Acceptance**: HierarchicalSettingsResolver queries all 5 scope tables. Batch loading tested with seeded data.
- **Effort**: S

---

### Phase 4: Strongly-Typed Setting Groups
**Layer**: Application  
**Effort**: M  
**Risk**: Low

#### Task 4.1: Define ISettingGroup Interface
- **File**: `Explore.Application/Settings/ISettingGroup.cs`
- **Acceptance**: Interface with `SettingKeys` static property and `Populate()` method
- **Effort**: S

#### Task 4.2: Create EmailSettingGroup
- **File**: `Explore.Application/Settings/Groups/EmailSettingGroup.cs`
- **Acceptance**: Strongly-typed properties for all email settings, auto-populated from resolver
- **Effort**: S

#### Task 4.3: Create StorageSettingGroup
- **File**: `Explore.Application/Settings/Groups/StorageSettingGroup.cs`
- **Effort**: S

#### Task 4.4: Create BrandingSettingGroup, AnalyticsSettingGroup, CerbosSettingGroup, EventPolicySettingGroup, DeploymentSettingGroup, RoutingSettingGroup
- **Effort**: M (one per existing *ConfigResolver)

#### Task 4.5: Refactor SmtpConfigResolver to Use EmailSettingGroup
- **File**: `Explore.Infrastructure/Mail/SmtpConfigResolver.cs`
- **Acceptance**: Resolver uses `IHierarchicalSettingsResolver.ResolveGroupAsync<EmailSettingGroup>()`. Own cache layer removed. Single DB roundtrip. All existing tests pass.
- **Effort**: S

#### Task 4.6: Refactor S3ConfigResolver to Use StorageSettingGroup
- **Effort**: S

#### Task 4.7: Refactor AnalyticsConfigResolver to Use AnalyticsSettingGroup
- **Effort**: S

#### Task 4.8: Refactor CerbosConfigResolver to Use CerbosSettingGroup
- **Effort**: S

#### Task 4.9: Unit Tests for All Setting Groups
- **Effort**: M

---

### Phase 5: Migrate Consumers — Kill N+1 and Copy-Paste
**Layer**: Application  
**Effort**: L  
**Risk**: Medium (touches widely-used services)

#### Task 5.1: Refactor InstanceGovernanceSettingService
- **File**: `Explore.Application/Services/InstanceGovernanceSettingService.cs`
- **Acceptance**: Uses `IHierarchicalSettingsResolver.ResolveBatchAsync()` instead of 30+ `GetByKey()` calls. All existing unit tests pass.
- **Effort**: M

#### Task 5.2: Refactor InstanceStorageSettingService
- **Acceptance**: Uses shared `SettingUpsertService` and `SettingValueSerializer`. Delete 60+ lines of duplicated helpers.
- **Effort**: S

#### Task 5.3: Refactor InstanceSmtpSettingService
- **Acceptance**: Same as 5.2
- **Effort**: S

#### Task 5.4: Refactor TenantPolicySettingService
- **Acceptance**: Uses batch resolution. ~27 individual queries → 2 batch queries. All existing tests pass.
- **Effort**: M

#### Task 5.5: Refactor GetPublicExperienceSettingsQueryHandler
- **Effort**: S

#### Task 5.6: Integration Tests Validation
- **Acceptance**: All existing integration tests pass. New integration test verifies hierarchical cascade end-to-end.
- **Effort**: M

---

### Phase 6: Deprecate Legacy TenantSettings Entity
**Layer**: Domain + Persistence + Application + API  
**Effort**: M  
**Risk**: Medium (entity referenced across layers)

#### Task 6.1: Migrate TenantSettings Data to TenantSetting
- **Acceptance**: DB migration script converts `TenantSettings` column values into `TenantSetting` EAV rows per tenant.
- **Effort**: M

#### Task 6.2: Update TenantSettingsController to Use Resolver
- **Acceptance**: Controller endpoints return resolved settings from hierarchical resolver instead of querying legacy entity.
- **Effort**: M

#### Task 6.3: Remove TenantSettings Entity, Repository, Configuration
- **Files**: Delete `Explore.Domain/TenantSettings.cs`, `TenantSettingsRepository.cs`, `TenantSettingsConfiguration.cs`
- **Acceptance**: Build succeeds. No references remain.
- **Effort**: S

#### Task 6.4: Update DTOs and Hateoas Policies
- **Acceptance**: `TenantSettingsDto` / `TenantSettingsListDto` still work but pull from resolved settings
- **Effort**: S

#### Task 6.5: DB Migration — Drop Legacy Table
- **Effort**: S

---

### Phase 7: Cleanup & Documentation
**Layer**: Cross-cutting  
**Effort**: S

#### Task 7.1: Deprecate Legacy GovernanceSettingKeys Flat Aliases
- **Acceptance**: All consumers use `SettingRegistry` or definition classes directly. Flat aliases marked `[Obsolete]`.
- **Effort**: S

#### Task 7.2: Update docs/CONFIGURATION.md
- **Acceptance**: Documents new 5-tier cascade, setting groups, registry pattern
- **Effort**: S

#### Task 7.3: Update docs/MULTI_TENANCY.md
- **Acceptance**: References new org/group/user scope entities
- **Effort**: S

#### Task 7.4: Update docs/ADMIN_HIERARCHY.md
- **Acceptance**: References actual implementation instead of theoretical design
- **Effort**: S

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking existing settings resolution | Medium | High | Phase 2.5 adapter keeps `ISettingsResolver` working during migration |
| N+1 → batch loading query performance | Low | Medium | Profile with EF Core logging. Add composite indexes. |
| Migration data loss (TenantSettings → TenantSetting) | Low | High | Reversible migration with rollback script. Test on staging first. |
| Cache coordination bugs | Medium | Medium | Extensive unit tests on invalidation cascades |
| Scope of Phase 6 (legacy removal) is larger than expected | Medium | Low | Phase 6 is optional — system works without removing legacy entity |

---

## Dependencies Between Phases

```
Phase 1 (Foundation) ← no deps
    ↓
Phase 2 (Resolver) ← depends on Phase 1
    ↓
Phase 3 (New Entities) ← depends on Phase 2 for wiring
    ↓
Phase 4 (Setting Groups) ← depends on Phase 2
    ↓
Phase 5 (Consumer Migration) ← depends on Phase 2 + Phase 4
    ↓
Phase 6 (Legacy Removal) ← depends on Phase 5
    ↓
Phase 7 (Cleanup) ← depends on all above
```

Phases 3 and 4 can run in parallel after Phase 2 completes.

---

## Success Metrics

1. **Zero copy-pasted deserialization helpers** — One `SettingValueSerializer` used everywhere
2. **≤2 DB queries per settings resolution** — Down from 15-30+
3. **All 5 hierarchy levels functional** — Instance, Tenant, Org, Group, User
4. **100% audit coverage** — Every write logged via `ConfigurationChangeLogService`
5. **All existing tests pass** — Zero regressions
6. **Single cache invalidation path** — `SettingsCacheManager` coordinates all layers

---

## Potential Risks & Unknowns

The **highest-risk area** is Phase 2.3 (HierarchicalSettingsResolver implementation) — building the batch-loading cascade that replaces the current per-key resolution. The challenge is correctly implementing lock semantics across 5 scope levels without performance regression. The current 2-tier resolver is simple (load all system, load all tenant, LINQ merge); a 5-tier resolver needs to load from 5 tables and merge with lock precedence at each level. If the batch query returns too many rows for large tenants with many orgs, we may need per-scope pagination or lazy loading of lower scopes.

The **second risk** is Phase 6 — removing `TenantSettings` may have hidden references in Blazor client generated code (`EventApiClient.g.cs` already has `TenantSettingsDto`). The generated client code may need regeneration, which cascades to Blazor UI components.
