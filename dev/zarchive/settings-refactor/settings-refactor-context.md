# Settings Architecture Refactor — Context & Key Decisions

**Last Updated: 2026-02-27**

---

## SESSION PROGRESS

- [x] Full codebase analysis of all settings-related code
- [x] External research via Tavily on enterprise settings patterns
- [x] 14 tech debt items identified and ranked
- [x] Approach C (Hybrid) architecture designed
- [x] `settings-refactor-plan.md` written
- [x] `settings-refactor-context.md` written
- [ ] `settings-refactor-tasks.md` written
- [ ] Implementation started

---

## Quick Resume

**What we're doing:** Refactoring the settings system from a 2-tier (System → Tenant) cascade with N+1 queries and copy-pasted code into a 5-tier (Instance → Tenant → Org → Group → User) hierarchical system with batch loading, a code-defined setting registry, strongly-typed setting groups, and unified caching.

**Key architectural choice:** Approach C — Hybrid (code-defined definitions + per-scope EAV tables + unified resolver). NOT a single-table approach (too risky) and NOT extending the current model (too messy).

**Next action:** Start Phase 1 (Setting Definition Registry + shared utilities).

---

## Key Files — Current Implementation

### Domain Entities
| File | Purpose | Refactor Status |
|------|---------|-----------------|
| `Explore.Domain/SystemSetting.cs` | Instance-level EAV settings | **Keep** — serves as instance-scope store |
| `Explore.Domain/TenantSetting.cs` | Tenant-scoped overrides | **Keep** — extend pattern to other scopes |
| `Explore.Domain/TenantSettings.cs` | **LEGACY** strongly-typed 1:1 tenant settings | **Remove** in Phase 6 |
| `Explore.Domain/AppSetting.cs` | Encrypted operational secrets (AES-256-GCM) | **Unchanged** — separate concern |
| `Explore.Domain/ConfigurationChangeLog.cs` | Audit trail | **Enhance** — ensure all writes go through it |
| `Explore.Domain/Enums/ConfigurationScopeEnum.cs` | System=0, Instance=1, Tenant=2, Organization=3 | **Extend** — add Group=4, User=5 |

### Constants
| File | Purpose | Refactor Status |
|------|---------|-----------------|
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | ~219 lines of setting key constants | **Replace** with SettingRegistry definitions |
| `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs` | Secret key constants | **Replace** same as above |

### Services — Application Layer
| File | Purpose | Refactor Status |
|------|---------|-----------------|
| `Explore.Application/Services/InstanceGovernanceSettingService.cs` | 30+ individual GetByKey() calls | **Refactor** — use batch resolve |
| `Explore.Application/Services/InstanceStorageSettingService.cs` | Copy-pasted helpers + upsert | **Refactor** — extract shared code |
| `Explore.Application/Services/InstanceSmtpSettingService.cs` | Copy-pasted helpers + upsert | **Refactor** — extract shared code |
| `Explore.Application/Services/TenantPolicySettingService.cs` | 27 individual reads per call | **Refactor** — use batch resolve |
| `Explore.Application/Services/ConfigurationChangeLogService.cs` | Audit logging | **Keep** — wire into central write path |

### Services — Infrastructure Layer
| File | Purpose | Refactor Status |
|------|---------|-----------------|
| `Explore.Infrastructure/Services/SettingsResolver.cs` | 2-tier cascade + IMemoryCache | **Adapter** — delegates to new resolver |
| `Explore.Infrastructure/Storage/S3ConfigResolver.cs` | S3 config + own cache layer | **Simplify** — use StorageSettingGroup |
| `Explore.Infrastructure/Mail/SmtpConfigResolver.cs` | SMTP config + own cache layer | **Simplify** — use EmailSettingGroup |
| `Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs` | Analytics config + own cache | **Simplify** — use AnalyticsSettingGroup |
| `Explore.Infrastructure/Services/CerbosConfigResolver.cs` | Cerbos config + own cache | **Simplify** — use CerbosSettingGroup |

### Contracts
| File | Purpose | Refactor Status |
|------|---------|-----------------|
| `Explore.Application/Contracts/Infrastructure/ISettingsResolver.cs` | Current 2-tier resolver contract | **Keep** for backward compat, add new interface |

### Persistence
| File | Purpose | Refactor Status |
|------|---------|-----------------|
| `Explore.Persistence/Configurations/Entities/SystemSettingConfiguration.cs` | EF config for SystemSetting | **Keep** |
| `Explore.Persistence/Configurations/Entities/TenantSettingConfiguration.cs` | EF config for TenantSetting | **Keep** |
| `Explore.Persistence/Configurations/Entities/TenantSettingsConfiguration.cs` | Legacy entity config | **Remove** Phase 6 |
| `Explore.Persistence/Seed/LookupTableSeeder.cs` | Seeds ~50 SystemSetting rows | **Update** — align with registry definitions |

---

## Important Architectural Decisions

### Decision 1: Code-Defined Definitions (NOT Database Table)
**Why:** Avoids chicken-and-egg bootstrapping (system needs settings to start, but settings are in DB that hasn't been seeded yet). Also means setting definitions are version-controlled, code-reviewed, and compile-time validated.

**Tradeoff:** Settings cannot be added at runtime via admin UI. New settings require a deployment. This is **intentional** — setting definitions are part of the application contract, not data.

### Decision 2: Separate Tables Per Scope (NOT Single SettingValue Table)
**Why:** Per-scope tables enable:
- Clear FK relationships to parent entities (TenantSetting → Tenant, OrgSetting → Organization)
- Scope-specific indexes without composite key complexity
- Simpler tenant data isolation queries
- Easier understanding for developers (each table represents one concept)

**Tradeoff:** 5 tables instead of 1. More EF configurations. But each table is tiny and simple.

### Decision 3: Keep Legacy ISettingsResolver as Adapter
**Why:** Dozens of existing consumers use `ISettingsResolver`. A hard cut-over is too risky. The adapter pattern lets us migrate consumers gradually while the new resolver is the true implementation.

**When to remove adapter:** After Phase 5 completes and all consumers are migrated. Target: never remove the interface, just make the implementation a thin pass-through.

### Decision 4: UserPreference is NOT Tenant-Scoped
**Why:** A user's preferences (theme, language, notification settings) follow the user across tenants. If a user belongs to multiple tenants, their preference for "dark mode" should apply everywhere.

**Exception:** If a tenant locks a setting (e.g., forces light mode for branding), the lock takes precedence.

### Decision 5: Setting Groups vs .NET IOptions<T> Pattern
**Considered:** Using .NET `IOptions<T>` / `IOptionsSnapshot<T>` / `IOptionsMonitor<T>` pattern to bind settings.

**Decision:** Use our own `ISettingGroup` pattern instead. Reason: `IOptions` is designed for static configuration (appsettings.json). Our settings are hierarchical, per-request (different tenant = different values), and loaded from DB. The `IOptions` pattern doesn't support per-scope resolution natively. Our setting groups are resolved per-request with a `SettingContext`.

### Decision 6: Dot Notation as Canonical Key Format
**Standard:** All setting keys use dot notation: `email.smtp_host`, `storage.s3_endpoint`.

**Migration:** `AppSetting` colon-notation keys (`Smtp:Host`) remain as-is since `AppSetting` is a separate concern (encrypted secrets). No key format migration needed — the two systems are already separate.

---

## Key Interface Signatures (Core Domain)

```csharp
// === DOMAIN ===

// Explore.Domain/Settings/SettingScope.cs
public enum SettingScope { Instance = 0, Tenant = 1, Organization = 2, Group = 3, User = 4 }

// Explore.Domain/Settings/SettingValueType.cs  
// Same as existing but with added validation capability
public enum SettingValueType { String = 0, Integer = 1, Boolean = 2, Json = 3, Decimal = 4, DateTime = 5 }

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
    string[]? AllowedValues = null);

// Explore.Domain/Settings/SettingRegistry.cs
public static class SettingRegistry
{
    public static SettingDefinition? Get(string key);
    public static IReadOnlyCollection<SettingDefinition> GetByCategory(string category);
    public static IReadOnlyCollection<string> AllCategories { get; }
    public static IReadOnlyCollection<SettingDefinition> All { get; }
}

// === NEW ENTITIES (same EAV pattern) ===

// Explore.Domain/OrganizationSetting.cs
public class OrganizationSetting : BaseEntity, ITenantEntity, IAuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }
    public string SettingKey { get; set; }
    public string? Value { get; set; }
    public bool IsLocked { get; set; }
    // + standard audit fields
}

// Explore.Domain/GroupSetting.cs
public class GroupSetting : BaseEntity, ITenantEntity, IAuditableEntity { ... }

// Explore.Domain/UserPreference.cs (NOT ITenantEntity — follows the user)
public class UserPreference : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public string SettingKey { get; set; }
    public string? Value { get; set; }
    // + audit fields, NO IsLocked (users can't lock settings)
}

// === APPLICATION ===

// Explore.Application/Models/SettingContext.cs
public sealed record SettingContext(
    Guid? TenantId = null,
    Guid? OrganizationId = null,
    Guid? GroupId = null,
    Guid? UserId = null);

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

// Explore.Application/Settings/ISettingGroup.cs
public interface ISettingGroup
{
    static abstract IEnumerable<string> SettingKeys { get; }
    void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings);
}

// Explore.Application/Settings/SettingValueSerializer.cs
public static class SettingValueSerializer
{
    public static T Deserialize<T>(string? value, T defaultValue);
    public static string Serialize<T>(T value);
}

// Explore.Application/Settings/SettingUpsertService.cs
public class SettingUpsertService
{
    public Task UpsertAsync(string key, string value, SettingScope scope, Guid scopeId, Guid actorId, CancellationToken ct);
}
```

---

## Dependencies (External / Unchanged)

- **PostgreSQL** — No new extension needed. Just new tables.
- **EF Core** — Standard migrations. No breaking package updates.
- **IMemoryCache** — Stays as primary cache. IDistributedCache prep is for future.
- **Keycloak / Cerbos** — Settings CRUD authorization via existing Cerbos policies (admin-only for instance/tenant; delegated for org/group/user prefs).
- **MediatR** — Setting read/write commands follow existing CQRS patterns.
