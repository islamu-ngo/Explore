# Settings Definition System Refactor — Context

Last Updated: 2026-03-18

## SESSION PROGRESS (2026-03-18)

### ✅ COMPLETED
- Full codebase exploration of settings system (14 definition files, resolver, registry, groups, tests)
- External research on .NET hierarchical settings best practices (ABP, Finbuckle, IOptions patterns)
- Created refactoring plan, context, and tasks files
- Incorporated senior architect review feedback into plan (7 guardrails added)

### 🟡 IN PROGRESS
- Nothing yet — plan finalized, ready for implementation

### ⚠️ BLOCKERS
- None

---

## Senior Architect Review: Key Takeaways

**Verdict:** Architecture is fundamentally correct. Approved with guardrails.

**What's right:**
- Code-defined definitions as source of truth
- Hierarchical resolution as primary read abstraction
- Typed groups as consumer pattern
- Killing the god service

**7 Non-negotiable guardrails added:**
1. **Concern separation** — Settings ≠ Capabilities ≠ Authorization ≠ Secrets. Never mix them.
2. **SettingDefinition is single metadata authority** — No duplicated defaults or rules anywhere else.
3. **Typed groups for reads, explicit commands for writes** — Write path needs richer semantics (partial update, reset, lock, audit reason).
4. **Reset-to-inherited is first-class** — Three states: no override (inherit), explicit value, removed/reset. API must support `DELETE` to remove overrides.
5. **Cache invalidation is core design** — Document strategy, consider multi-node future.
6. **Observability at resolver layer** — Structured logging for latency, cache hits, query counts, validation failures.
7. **Scope policy validation in write path** — Registry check, scope range, lock, AllowedValues — all validated before write.

**Refinements to original plan:**
- Line count is not the objective — clean orchestration is. 180 lines is fine if clean.
- "Every category gets a group" → every category **consumed as a coherent unit** gets a group. 1-key groups fine for consistency.
- All writes through MediatR — no controller → resolver bypass.

---

## Key Files

### Domain Layer (Keep, Improve)

**`Explore.Domain/Settings/SettingDefinition.cs`** — Sealed record, well-designed. The single metadata authority (Guardrail 2). No changes needed.

**`Explore.Domain/Settings/SettingRegistry.cs`** — FrozenDictionary-backed static registry. Populated from 14 definition classes. No changes needed.

**`Explore.Domain/Settings/SettingScope.cs`** — Enum: Instance(0), Tenant(1), Organization(2), Group(3), User(4). No changes needed.

**`Explore.Domain/Settings/Definitions/*.cs`** (14 files) — Per-category definition classes. ~80 total settings. Keep as-is (add missing routing resolver definitions in Phase 0).

**`Explore.Domain/Constants/GovernanceSettingKeys.cs`** (320 lines) — NEEDS CLEANUP. Delete flat aliases (lines 218-319). Keep only nested static classes.

**`Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs`** — Separate file for credential keys. Keep as-is (Guardrail 1 — secrets are a stricter subdomain).

### Application Layer (Major Changes)

**`Explore.Application/Contracts/Infrastructure/ISettingGroup.cs`** — Interface: `static abstract IEnumerable<string> SettingKeys`, `void Populate(IReadOnlyDictionary<string, ResolvedSetting>)`. Foundation for typed groups (read path — Guardrail 3).

**`Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs`** — 5-tier resolver interface. Key methods: `ResolveGroupAsync<TGroup>()` for reads, `SetValueAsync()` + `RemoveOverrideAsync()` for writes. This is the center of gravity.

**`Explore.Application/Settings/Groups/`** — 5 existing groups (Branding, Email, Storage, Cerbos, Analytics). Need 9 more.

**`Explore.Application/Services/InstanceGovernanceSettingService.cs`** (780 lines) — THE MAIN TARGET. God service with 40+ individual GetByKey calls. Rewrite to thin orchestration using setting groups.

**`Explore.Application/Settings/SettingUpsertService.cs`** (114 lines) — Centralized write path. Works well, keep as-is.

**`Explore.Application/Settings/SettingValueSerializer.cs`** (104 lines) — JSON deserialization with fallback. Used by setting groups. Keep.

**`Explore.Application/Contracts/Services/IInstanceGovernanceSettingService.cs`** — Interface for the god service. Will slim down as methods move to resolver.

### Infrastructure Layer (Minor Changes)

**`Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`** (470 lines) — The good resolver. Batch loading, 5-tier cascade, memory cache. Add: AllowedValues validation (Guardrail 7), structured logging (Guardrail 6).

**`Explore.Infrastructure/Services/SettingsResolver.cs`** — LEGACY 2-tier resolver. Delete in Phase 4.

### API Layer

**`Explore.API/Controllers/InstanceSettingsController.cs`** — Sub-resource endpoints. Currently calls god service. Will call through MediatR → handler → resolver (Guardrail 3).

**`Explore.API/Controllers/TenantSettingsController.cs`** — DEPRECATED. Delete in Phase 4.

### Test Files

**`Event.Domain.UnitTests/Settings/SettingRegistryTests.cs`** (239 lines) — Comprehensive registry validation including alignment tests. Update after alias removal.

**`Event.Architecture.Tests/GovernanceSettingKeysTests.cs`** (62 lines) — Tests flat alias alignment. Remove flat alias tests, add new architecture tests.

**`Event.Application.UnitTests/Settings/`** — HierarchicalSettingsResolverTests, AnalyticsSettingGroupTests, SettingValueSerializerTests. Add tests for new groups.

---

## Critical Bugs Found (Pre-existing)

### BUG 1: StorageSettingGroup uses wrong key namespace (CRITICAL)
- `StorageSettingDefinitions.cs` defines keys as `s3.*` (e.g., `s3.endpoint`, `s3.bucket_name`)
- `StorageSettingGroup.cs` expects `storage.*` keys (e.g., `storage.endpoint`, `storage.bucket_name`)
- **Result:** `ResolveGroupAsync<StorageSettingGroup>()` will silently return defaults — keys never match

### BUG 2: CerbosSettingGroup keys don't match CerbosSettingDefinitions (CRITICAL)
- `CerbosSettingDefinitions.cs` has 7 keys (mode, custom_endpoint, failure_mode, etc.)
- `CerbosSettingGroup.cs` expects completely different 7 keys (endpoint, port, use_tls, etc.)
- **Result:** Batch resolution fails silently

### BUG 3: SettingGroups hardcode key strings instead of using GovernanceSettingKeys
- `EmailSettingGroup`, `StorageSettingGroup`, `BrandingSettingGroup`, `CerbosSettingGroup` all use hardcoded `"email.*"` strings
- Only `AnalyticsSettingGroup` correctly references `GovernanceSettingKeys.Analytics.*`
- **Result:** DRY violation, keys can drift out of sync (as proven by bugs 1 & 2)

### BUG 4: Routing resolver keys missing definitions
- `GovernanceSettingKeys.Routing` defines `resolver_header_enabled`, `resolver_subdomain_enabled`, etc.
- No corresponding entries in `RoutingSettingDefinitions.cs`
- **Result:** These keys won't be found in `SettingRegistry`

---

## Key Decisions

1. **Keep SettingDefinition and SettingRegistry unchanged** — They're well-designed and industry-aligned (architect confirmed)
2. **Delete flat aliases, no backward compatibility** — Project is in dev mode, no external consumers
3. **ISettingGroup for reads, explicit commands for writes** (Guardrail 3) — Every consumed category gets a typed group for reads; writes use richer DTOs
4. **ResolveGroupAsync<T>() replaces individual GetByKey chains** — Batch loading = ≤2 DB queries
5. **Module capability logic goes to dedicated service** (Guardrail 1) — Capabilities ≠ Settings
6. **ISettingsResolver (legacy) gets deleted** — Fully replaced by IHierarchicalSettingsResolver
7. **Reset-to-inherited is a first-class operation** (Guardrail 4) — API endpoint for removing overrides
8. **All writes through MediatR** — No controller → resolver bypass
9. **SettingDefinition owns all metadata** (Guardrail 2) — No duplicated defaults or rules elsewhere
10. **Cache invalidation and multi-node are documented concerns** (Guardrail 5)

---

## Interface Signatures (Critical for Implementation)

### ISettingGroup (existing — read path)
```csharp
public interface ISettingGroup
{
    static abstract IEnumerable<string> SettingKeys { get; }
    void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings);
}
```

### IHierarchicalSettingsResolver key methods (existing)
```csharp
// Read
Task<TGroup> ResolveGroupAsync<TGroup>(SettingContext context)
    where TGroup : ISettingGroup, new();

// Write
Task SetValueAsync(string key, string value, SettingScope scope, Guid? scopeId, Guid? actorId);

// Reset to inherited (Guardrail 4)
Task RemoveOverrideAsync(string key, SettingScope scope, Guid? scopeId, Guid? actorId);

// Lock
Task LockAsync(string key, SettingScope scope, Guid? scopeId, Guid? actorId);
```

### Pattern for new setting groups
```csharp
public class EventSettingGroup : ISettingGroup
{
    public bool UserSubmissionEnabled { get; private set; } = true;
    public bool OrganizationSubmissionEnabled { get; private set; } = true;
    // ... more properties with defaults from SettingDefinition

    public static IEnumerable<string> SettingKeys =>
    [
        GovernanceSettingKeys.Events.UserSubmissionEnabled,
        GovernanceSettingKeys.Events.OrganizationSubmissionEnabled,
        // ... all keys using GovernanceSettingKeys constants
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue(GovernanceSettingKeys.Events.UserSubmissionEnabled, out var v))
            UserSubmissionEnabled = SettingValueSerializer.DeserializeBool(v.Value, true);
        // ... map each
    }
}
```

---

## Quick Resume

To continue this task:
1. Read this file + tasks file
2. Check which phase is next
3. Read the plan for phase details and guardrails
4. Follow TDD: write tests first, then implement
5. Build + run all tests after each phase
6. **Before any write-path changes:** verify guardrails 3, 4, 7 are respected
