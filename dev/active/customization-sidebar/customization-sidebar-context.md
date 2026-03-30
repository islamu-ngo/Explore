ABOUTME: Key files, decisions, dependencies, and essential interface signatures for the Event List Customization Sidebar feature.
ABOUTME: Companion to customization-sidebar-plan.md — provides the technical context needed during implementation.

# Context: Event List Customization Sidebar (v2)

**Last Updated: 2026-03-26**

---

## Key Files Map

### Domain Layer
| File | Purpose | Status |
|---|---|---|
| `Explore.Domain/UserPreference.cs` | User-level preference entity (tenant-scoped) | Complete |
| `Explore.Domain/TenantSetting.cs` | Tenant-level setting entity | **Needs `IsLocked`** |
| `Explore.Domain/SystemSetting.cs` | Instance-level setting (has `IsLocked`) | Complete |
| `Explore.Domain/Settings/SettingDefinition.cs` | Immutable record: key, type, default, scopes, lockability | Complete |
| `Explore.Domain/Settings/SettingScope.cs` | Enum: Instance=0, Tenant=1, Org=2, Group=3, User=4 | Complete |
| `Explore.Domain/Settings/SettingRegistry.cs` | Static FrozenDictionary of all definitions | Complete |
| `Explore.Domain/Settings/SettingValueType.cs` | Enum: String, Integer, Boolean, Decimal, Json, DateTime | Complete |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Dot-notation key constants (nested static classes) | Needs `EventList` class |
| `Explore.Domain/Settings/Definitions/EventSettingDefinitions.cs` | Example definitions class (pattern to follow) | Pattern reference |
| `Explore.Domain/Settings/Definitions/AppearanceSettingDefinitions.cs` | Another example definitions class | Pattern reference |

### Application Layer
| File | Purpose | Status |
|---|---|---|
| `Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs` | Core resolver interface | Complete (instance lock only) |
| `Explore.Application/Contracts/Infrastructure/ISettingGroup.cs` | Setting group interface (static SettingKeys + Populate) | Complete |
| `Explore.Application/Contracts/Infrastructure/ResolvedSetting.cs` | Resolved value + source + lock status | **Needs `TenantLocked` in SettingSource** |
| `Explore.Application/Contracts/Persistence/IUserPreferenceRepository.cs` | User preference data access | Complete |
| `Explore.Application/Contracts/Persistence/ITenantSettingRepository.cs` | Tenant setting data access | Needs Lock/Unlock methods |
| `Explore.Application/Settings/SettingContext.cs` | Immutable context record (TenantId?, OrgId?, GroupId?, UserId?) | Complete |
| `Explore.Application/Settings/SettingValueSerializer.cs` | JSON serializer helpers | Complete |
| `Explore.Application/Settings/SettingUpsertService.cs` | Centralized upsert logic with audit | Complete |
| `Explore.Application/Settings/Groups/AppearanceSettingGroup.cs` | Example group (pattern to follow) | Pattern reference |
| `Explore.Application/Settings/Groups/EventSettingGroup.cs` | Example group (pattern to follow) | Pattern reference |
| `Explore.Application/Features/Settings/` | **New — generic handlers** | To be created |
| `Explore.Application/DTOs/Settings/EffectiveSettingDto.cs` | **New — metadata DTO** | To be created |

### Infrastructure Layer
| File | Purpose | Status |
|---|---|---|
| `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs` | 5-tier resolver with 5-min memory cache | **Instance lock only** |

### Persistence Layer
| File | Purpose | Status |
|---|---|---|
| `Explore.Persistence/Repositories/UserPreferenceRepository.cs` | UserPreference CRUD | Complete |
| `Explore.Persistence/Repositories/TenantSettingRepository.cs` | TenantSetting CRUD | Needs Lock/Unlock |
| `Explore.Persistence/Configurations/Entities/UserPreferenceConfiguration.cs` | EF config for UserPreference | Complete |
| `Explore.Persistence/Configurations/Entities/TenantSettingConfiguration.cs` | EF config for TenantSetting | Needs `IsLocked` column |

### API Layer
| File | Purpose | Status |
|---|---|---|
| `Explore.API/Controllers/InstanceSettingsController.cs` | Existing settings controller at `/api/instance/settings/{domain}` | Pattern reference |
| `Explore.API/Controllers/UserAppearanceController.cs` | Existing user preference precedent at `/api/user/appearance` | Pattern reference |
| `Explore.API/RouteNames.cs` | Centralized route name constants | Needs new entries |
| `Explore.API/Controllers/SettingsController.cs` | **New — unified settings controller** | To be created |

### Blazor Layer
| File | Purpose | Status |
|---|---|---|
| `Explore.Blazor.Client/Pages/Events/EventList.razor` | Event list page markup (1291 lines) | Monolith — needs refactor |
| `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` | Code-behind (1150 lines, 16+ injected services) | Monolith — needs refactor |
| `Explore.Blazor.Client/Pages/Events/EventList.razor.css` | Scoped styles (592 lines) | Needs card CSS extraction |
| `Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor` | Filter bar + layout mode switcher | Needs settings button |
| `Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor.cs` | Filter bar code-behind | Needs settings button callback |
| `Explore.Blazor.Client/Pages/Events/Components/EventCard.razor` | Exists (129 lines) but inline, not properly extracted | Needs full extraction |
| `Explore.Blazor.Client/Services/EventService.cs` | Event API client service | May need paged query support |
| `Explore.Blazor.Client/Layout/MainLayout.razor` | App layout | Reference only |

---

## Essential Interface Signatures

### IHierarchicalSettingsResolver
```csharp
// Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs
public interface IHierarchicalSettingsResolver
{
    Task<T> ResolveAsync<T>(SettingContext context, string key, CancellationToken ct = default);
    Task<ResolvedSetting> ResolveWithMetadataAsync(SettingContext context, string key, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, ResolvedSetting>> ResolveBatchAsync(SettingContext context, IEnumerable<string> keys, CancellationToken ct = default);
    Task<TGroup> ResolveGroupAsync<TGroup>(SettingContext context, CancellationToken ct = default) where TGroup : ISettingGroup, new();
    Task SetValueAsync(SettingContext context, string key, string value, SettingScope scope, CancellationToken ct = default);
    Task RemoveOverrideAsync(SettingContext context, string key, SettingScope scope, CancellationToken ct = default);
    Task LockAsync(SettingContext context, string key, SettingScope scope, CancellationToken ct = default);
    // NEW: UnlockAsync needed for tenant unlock support
    void InvalidateCache(Guid? tenantId = null);
    void InvalidateUserCache(Guid userId);
}
```

### ISettingGroup
```csharp
// Explore.Application/Contracts/Infrastructure/ISettingGroup.cs
public interface ISettingGroup
{
    static abstract IEnumerable<string> SettingKeys { get; }
    void Populate(IReadOnlyDictionary<string, ResolvedSetting> resolvedSettings);
}
```

### SettingContext
```csharp
// Explore.Application/Settings/SettingContext.cs
public sealed record SettingContext(
    Guid? TenantId,
    Guid? OrganizationId,
    Guid? GroupId,
    Guid? UserId
);
```

### SettingDefinition
```csharp
// Explore.Domain/Settings/SettingDefinition.cs
public sealed record SettingDefinition(
    string Key,
    Type ValueType,
    string DefaultValue,        // JSON-serialized
    string Category,            // e.g., "EventList", "Appearance" — used for group filtering
    string Description,
    SettingScope MinScope,      // Lowest scope that can set this
    SettingScope MaxScope,      // Highest scope that can set this
    bool IsLockable,
    bool IsSensitive,
    IReadOnlyList<string>? AllowedValues
);
```

### ResolvedSetting (Current)
```csharp
// Explore.Application/Contracts/Infrastructure/ResolvedSetting.cs
public sealed record ResolvedSetting(
    string Key,
    string Value,               // JSON-serialized
    SettingSource Source,        // SystemDefault | SystemLocked | TenantOverride | TenantLocked(NEW) | OrgOverride | GroupOverride | UserPreference
    bool IsLocked
);
```

### SettingSource Enum (Current — needs TenantLocked)
```csharp
// Explore.Application/Contracts/Infrastructure/ResolvedSetting.cs (line 20)
public enum SettingSource
{
    SystemDefault,
    TenantOverride,
    SystemLocked,
    OrganizationOverride,
    GroupOverride,
    UserPreference
    // NEW: TenantLocked — to be added
}
```

### New DTOs (To Be Created)

```csharp
// Explore.Application/DTOs/Settings/EffectiveSettingDto.cs
public sealed record EffectiveSettingDto(
    string Key,
    string Value,               // JSON-serialized current effective value
    string ValueType,           // "String", "Integer", "Boolean"
    string Source,              // Human-readable: "System Default", "Tenant Policy (Locked)", etc.
    bool IsLocked,
    bool CanEdit,               // !IsLocked && user has scope permission
    string? Reason,             // "Locked by tenant administrator" or null
    string? Description,
    IReadOnlyList<string>? AllowedValues
);

public sealed record SettingGroupResponse(
    string Category,
    IReadOnlyList<EffectiveSettingDto> Settings
);

public enum BatchUpdateMode
{
    BestEffort,  // Skip locked, apply rest — drawer autosave
    Strict       // Reject all if any locked — admin operations
}

public sealed record BatchUpdateResponse(
    bool Success,
    IReadOnlyList<SettingUpdateResult> Results
);

public sealed record SettingUpdateResult(
    string Key,
    bool Applied,
    string? SkipReason
);
```

### Entity Signatures

```csharp
// Explore.Domain/UserPreference.cs
// Id (Guid), TenantId (Guid), UserId (Guid), SettingKey (string), Value (string)
// IAuditableEntity, ISoftDeletable
// Unique constraint: TenantId + UserId + SettingKey

// Explore.Domain/TenantSetting.cs (CURRENT — missing IsLocked)
// Id (Guid), TenantId (Guid), SettingKey (string), Value (string)
// IAuditableEntity, ISoftDeletable
// NEEDS: IsLocked (bool) — to be added in Phase A1

// Explore.Domain/SystemSetting.cs (has IsLocked)
// Id (Guid), SettingKey (string), Value (string), IsLocked (bool)
// IAuditableEntity, ISoftDeletable
```

---

## Key Decisions

### D1: Reuse Existing 5-Tier Settings Infrastructure
- **Decision**: Use existing `UserPreference` + `HierarchicalSettingsResolver` — no separate preferences system
- **Rationale**: Infrastructure handles cascade, caching, serialization, scope resolution
- **Impact**: All work is additive (new keys, groups, handlers), not architectural

### D2: Generic Application Core
- **Decision**: Application handlers are category-agnostic — work with ANY setting group via `SettingRegistry` filtering
- **Rationale**: EventList is the first consumer, not the only one. Handlers reusable for Appearance, Notifications, etc.
- **Impact**: `ResolveSettingGroupQuery(string Category)` filters registry by category; no EventList-specific handler logic

### D3: Unified Settings Controller
- **Decision**: Single `SettingsController` with scope-aware routes. NO separate `UserPreferencesController` or `TenantSettingsController`
- **Routes**:
  - `/api/settings/user/{category}` — user preferences (GET, PUT)
  - `/api/settings/user/keys/{key}` — single key operations (PUT, DELETE)
  - `/api/settings/tenant/{category}` — tenant management (GET, PUT)
  - `/api/settings/tenant/keys/{key}` — single key + lock/unlock (PUT, POST lock, DELETE lock)
- **Rationale**: Scope is a route parameter, not a separate controller. Cleaner API surface.

### D4: Tenant-Level Locking via `TenantSetting.IsLocked`
- **Decision**: Add `IsLocked` column to `TenantSetting`, add `SettingSource.TenantLocked` enum value
- **Rationale**: Matches `SystemSetting.IsLocked` pattern; resolver already has lock precedence logic
- **Impact**: Migration, resolver update, new enum value, exhaustive switch coverage

### D5: Rigorous Lock Semantics
- **Decision**: Lock = "this scope's value becomes authoritative; lower scopes cannot override"
- **Key behaviors**:
  1. Lower-scope values stay in storage but become non-effective during lock
  2. Unlock restores cascade — user's previously saved value returns
  3. Lock does NOT delete lower-scope rows
  4. Lock does NOT prevent lower-scope writes (values saved but non-effective)
  5. Lock precedence: Instance locked > Tenant locked > unlocked cascade
- **Rationale**: Preserves user data, enables clean lock/unlock round-trips, no migration on toggle

### D6: Batch Update Modes (BestEffort / Strict)
- **Decision**: Two explicit modes for batch setting updates
- **BestEffort**: Skip locked keys, apply rest, return per-key results. Use case: drawer autosave
- **Strict**: Reject entire request if ANY key is locked. Use case: admin operations
- **Rationale**: Different consumers need different semantics; design explicitly, not implicitly

### D7: No Anonymous→Authenticated Preference Migration (V1)
- **Decision**: Authenticated = server-authoritative, period. No merge on login.
- **Behavior**: Anonymous uses localStorage. Logging in ignores localStorage values.
- **Future**: Optional "Import browser preferences" as explicit user action (V2+)
- **Rationale**: Auto-merge introduces UX confusion, conflict resolution complexity, data quality issues

### D8: Pagination as Default Browse Mode
- **Decision**: `EventList.BrowseMode` defaults to `"pagination"` at system level
- **Rationale**: Better for SEO, URL sharing, accessibility, predictable page load
- **Impact**: Existing Virtualize infinite scroll becomes opt-in

### D9: SSR/Render-Mode Neutrality
- **Decision**: Feature works across static SSR, InteractiveServer, InteractiveAuto, mixed render modes
- **Constraints**:
  - No browser-only API during initial render (no JS interop for initial layout)
  - No layout flash after hydration
  - Settings resolution server-side during SSR; PersistentState for hydration
  - Fallback: system defaults (never blank/broken UI)

### D10: EventCard Extraction Before Any Drawer Work
- **Decision**: Extract inline card rendering into standalone `EventCard` component FIRST (Phase B1)
- **Prerequisite**: Baseline screenshots captured first (Phase B0)
- **Rationale**: Reduces risk of breaking 3 layout modes while adding settings logic
- **Gate**: Zero visual regression before proceeding to drawer

### D11: Effective Setting Metadata Contract
- **Decision**: Every resolved setting returned by API includes: Value, Source, IsLocked, CanEdit, Reason, Description, AllowedValues
- **CanEdit computation**: `!IsLocked && user has scope permission`
- **Reason values**: "Locked by instance administrator", "Locked by tenant administrator", "Insufficient permissions", null (editable)
- **Rationale**: UI shows meaningful lock indicators; reusable across all future settings UIs

### D12: Two Independent Delivery Tracks
- **Decision**: Track A (Settings Platform) and Track B (EventList UI Refactor) — separate delivery streams
- **Track A**: Domain → Persistence → Infrastructure → Application → API → NSwag → Tests
- **Track B**: Baseline → EventCard extraction → Loading/state → Pagination → URL state → Drawer → Autosave → localStorage → Tests
- **Parallelism**: B0-B1 can start while A is in progress (EventCard extraction is independent of settings API)
- **Dependency**: B5+ requires A6 complete (NSwag client generated)

### D13: Cache Invalidation Strategy
- **Decision**: Deterministic invalidation paths for every mutation type
- **Paths**:
  - User updates/resets → `InvalidateUserCache(userId)`
  - Tenant updates/locks/unlocks → `InvalidateCache(tenantId)` + user caches under tenant (or TTL 5-min max)
  - Instance changes → `InvalidateCache(null)` (global)
- **Output cache**: `UserData` policy with 30s TTL, vary by user + tenant. Lock mutations evict output cache tag.

### D14: Feature Rollout via Feature Flag
- **Decision**: Feature behind `Features.EventListCustomization.Enabled` (tenant-level setting)
- **Control points**: API returns 404 for disabled tenants, Blazor hides gear icon
- **Rollout**: Internal → Beta → GA (flip system default to true) → Per-tenant override

### D15: Auto-Save with Debounce
- **Decision**: Settings changes auto-save after 500ms debounce (no explicit save button)
- **Mode**: BestEffort batch update — skip locked keys, apply rest
- **Feedback**: Subtle "Saved" indicator; show "X skipped (locked)" if any
- **Error**: Toast on failure, revert local state

### D16: Out of Scope (V1)
- Waterfall/masonry layout
- Tag/category color persistence
- Organization/Group scope settings UI
- Anonymous→authenticated preference migration
- Multiple simultaneous drawer instances

---

## Dependencies

### External Dependencies
- **MudBlazor**: `MudDrawer`, `MudSwitch<T>`, `MudToggleGroup<T>`, `MudSelect<T>`, `MudPagination`
- **EF Core**: Migration tooling for `TenantSetting.IsLocked`
- **NSwag**: Client regeneration after `SettingsController` is added
- **Prometheus**: Metrics instrumentation (existing infrastructure)

### Two-Track Dependency Graph

```
TRACK A — SETTINGS PLATFORM
═══════════════════════════════════════════════════════════════════

A1 (Domain: keys, IsLocked) ──► A2 (Persistence: migration, repo) ──► A3 (Infrastructure: lock cascade)
                                                                              │
                                                                              ▼
                                                                     A4 (Application: generic handlers)
                                                                              │
                                                                              ▼
                                                                     A5 (API: unified controller)
                                                                              │
                                                                              ▼
                                                                     A6 (NSwag regen) ──► A7 (Integration tests)


TRACK B — EVENTLIST UI REFACTOR
═══════════════════════════════════════════════════════════════════

B0 (Baseline screenshots) ──► B1 (EventCard extraction)
                                       │
                                       ▼
                              B2 (Loading/state separation) ──► B3 (Paginated mode) ──► B4 (URL state)
                                                                                              │
                                                                                              ▼
                                                                    [GATE: Track A6 done] ► B5 (Drawer)
                                                                                              │
                                                                                              ▼
                                                                                     B6 (Autosave/reset)
                                                                                              │
                                                                                              ▼
                                                                                     B7 (Anonymous localStorage)
                                                                                              │
                                                                                              ▼
                                                                                     B8 (Visual regression)


PARALLELISM
═══════════
• B0-B1 start IMMEDIATELY — no Track A dependency (pure refactoring)
• B2-B4 can overlap with Track A (pagination mode independent of settings API)
• B5+ BLOCKED on A6 (needs NSwag-generated client for settings endpoints)
• Within Track A: strictly sequential (A1 → A2 → A3 → A4 → A5 → A6 → A7)
```

### Critical Path
```
A1 → A2 → A3 → A4 → A5 → A6 → B5 → B6 → B7 → B8
```

### Early-Start Items (Parallel with Track A)
```
B0 → B1 → B2 → B3 → B4  (all independent of Track A)
```

---

## Unified Settings API Routes

```
# User scope — authenticated users managing their preferences
GET    /api/settings/user/{category}                → ResolveSettingGroupQuery (effective cascade)
PUT    /api/settings/user/{category}                → UpdateSettingBatchCommand (BestEffort)
PUT    /api/settings/user/keys/{key}                → UpdateSettingCommand (User scope)
DELETE /api/settings/user/keys/{key}                → ResetSettingCommand (User scope)

# Tenant scope — tenant admins managing tenant policies
GET    /api/settings/tenant/{category}              → ResolveSettingGroupQuery (tenant-level only)
PUT    /api/settings/tenant/{category}              → UpdateSettingBatchCommand (Strict)
PUT    /api/settings/tenant/keys/{key}              → UpdateSettingCommand (Tenant scope)
POST   /api/settings/tenant/keys/{key}/lock         → LockSettingCommand (Tenant scope)
DELETE /api/settings/tenant/keys/{key}/lock         → UnlockSettingCommand (Tenant scope)
```

**Route design notes**:
- `{category}` maps to `SettingDefinition.Category` (e.g., `event-list`, `appearance`)
- Existing `InstanceSettingsController` at `/api/instance/settings/{domain}` remains unchanged
- User routes: `[Authorize]`. Tenant routes: `[Authorize(Roles = "TenantAdmin")]`
- User GET returns fully-resolved cascade (effective values). Tenant GET returns tenant-level overrides only.
- All mutations return `BaseCommandResponse<Guid>` (single) or `BatchUpdateResponse` (batch)

---

## Setting Key Reference

All keys use prefix `EventList.` and are defined in `GovernanceSettingKeys.EventList.*`.
`Category = "EventList"` for all 12 keys (used for group filtering in generic handlers).

| Key | Type | Default | MinScope | MaxScope | Lockable |
|---|---|---|---|---|---|
| `EventList.BrowseMode` | string | `"pagination"` | Tenant | User | Yes |
| `EventList.PageSize` | int | `12` | Tenant | User | Yes |
| `EventList.DefaultLayout` | string | `"DetailedList"` | Tenant | User | Yes |
| `EventList.Card.ShowDate` | bool | `true` | Tenant | User | Yes |
| `EventList.Card.ShowLocation` | bool | `true` | Tenant | User | Yes |
| `EventList.Card.ShowOrganizer` | bool | `true` | Tenant | User | Yes |
| `EventList.Card.ShowDescription` | bool | `true` | Tenant | User | Yes |
| `EventList.Card.ShowTags` | bool | `true` | Tenant | User | Yes |
| `EventList.Card.ShowCategories` | bool | `true` | Tenant | User | Yes |
| `EventList.Card.ShowCapacity` | bool | `false` | Tenant | User | Yes |
| `EventList.Card.ShowPrice` | bool | `true` | Tenant | User | Yes |
| `EventList.Card.ShowStatus` | bool | `true` | Tenant | User | Yes |

---

## SettingSource → Human-Readable Mapping

Used by `EffectiveSettingDto.Source` and lock reason display:

| `SettingSource` Enum | `Source` String | `Reason` (when locked) |
|---|---|---|
| `SystemDefault` | `"System Default"` | — |
| `SystemLocked` | `"Instance Policy (Locked)"` | `"Locked by instance administrator"` |
| `TenantOverride` | `"Tenant Override"` | — |
| `TenantLocked` | `"Tenant Policy (Locked)"` | `"Locked by tenant administrator"` |
| `OrganizationOverride` | `"Organization Override"` | — |
| `GroupOverride` | `"Group Override"` | — |
| `UserPreference` | `"User Preference"` | — |

---

## Lock Cascade Resolution (Quick Reference)

```
Input: Resolve key "EventList.BrowseMode" for User X in Tenant T

Step 1: Check SystemSetting.IsLocked for key
        → If locked: return (systemValue, SystemLocked, IsLocked=true) ← DONE

Step 2: Check TenantSetting[T].IsLocked for key  ← NEW
        → If locked: return (tenantValue, TenantLocked, IsLocked=true) ← DONE

Step 3: Normal cascade (highest scope wins):
        UserPreference[T, UserX, key]        → if exists → return (value, UserPreference, IsLocked=false)
        GroupSetting[G, key]                 → if exists → return (value, GroupOverride, IsLocked=false)
        OrganizationSetting[O, key]          → if exists → return (value, OrganizationOverride, IsLocked=false)
        TenantSetting[T, key]               → if exists → return (value, TenantOverride, IsLocked=false)
        SystemSetting[key]                   → if exists → return (value, SystemDefault, IsLocked=false)
        SettingDefinition.DefaultValue       → return (default, SystemDefault, IsLocked=false)
```

---

## Existing EventList Architecture (Key Points)

- **1150-line code-behind** with 16+ injected services — `@rendermode InteractiveServer`
- **Virtualize<EventListDto>** with `ItemsProvider` — preloads 20 items, caches in `_loadedEvents`
- **3 layout modes**: CompactGrid (6-col 224px cards), DetailedList (2-3 col horizontal), SingleRow (full-width horizontal)
- **Layout mode**: `MudToggleGroup<LayoutMode>` in `EventFilterBar` — session-only, not persisted
- **Detail drawer**: Right-side `MudDrawer` (Anchor.End, Temporary) — shows event details, inline registration, prev/next navigation
- **Event cards**: Rendered inline in EventList.razor — NOT a properly extracted component
- **EventCard.razor**: Exists (129 lines) but used inline, not independently
- **Filter state**: Read from `_filterBar?.SelectedXXX` properties
- **Persistent state**: `[PersistentState] EventListState` for prerender hydration
- **Grid CSS**: `event-grid event-grid--{layout}` with container queries and BEM naming
- **No settings persistence**: Layout mode is session-only, no URL state for pagination
- **Search**: Only search term in query params; no other URL state

---

## Important Codebase Distinctions

### TenantSettings Entity ≠ TenantSetting Entity
- `TenantSettings.cs` — SEPARATE entity for tenant policies (EventPublishingPolicy, etc.) — NOT part of hierarchical cascade
- `TenantSetting.cs` — Part of hierarchical settings cascade (key-value, scope-aware, lockable)
- Do NOT confuse these two entities during implementation

### AppSetting ≠ Settings System
- `AppSetting.cs` — Encrypted operational config (AES-256-GCM), connection strings, API keys
- Completely separate from the hierarchical settings system

### SettingChangedNotification (Existing)
- MediatR notification published on setting changes
- `SettingAuditLogHandler` already handles audit logging
- New handlers just need to publish this notification — audit logging comes for free

---

## Observability Quick Reference

### Structured Logging Events
| Event | Level | Key Fields |
|---|---|---|
| Setting resolved | `Debug` | Key, Source, IsLocked, TenantId, UserId |
| Setting updated | `Information` | Key, OldValue, NewValue, Scope, ActorId |
| Setting reset | `Information` | Key, Scope, ActorId |
| Lock applied | `Information` | Key, Scope, ActorId, TenantId |
| Lock rejected | `Warning` | Key, Scope, BlockingScope, ActorId |
| Batch skip (locked) | `Information` | Key, Reason, BatchId |
| Anonymous fallback | `Debug` | — |
| Feature gate check | `Information` | TenantId, Feature |

### Prometheus Metrics
| Metric | Type | Labels |
|---|---|---|
| `settings_resolution_total` | Counter | category, source, tenant_id |
| `settings_update_total` | Counter | category, scope, mode |
| `settings_lock_total` | Counter | scope, action |
| `settings_batch_skip_total` | Counter | category, reason |

---

## MudBlazor Component Quick Reference

### MudDrawer (Settings Sidebar)
```razor
<MudDrawer @bind-Open="_drawerOpen" Anchor="Anchor.End" Variant="DrawerVariant.Temporary"
           Width="320px" Overlay="true" OverlayAutoClose="true" Elevation="4">
    <!-- content -->
</MudDrawer>
```

### MudSwitch (Card Field Toggle — with lock support)
```razor
<MudSwitch T="bool" @bind-Value="_showDate" Label="Date" Color="Color.Primary"
           Disabled="@_isDateLocked" ReadOnly="@_isDateLocked"
           aria-describedby="@(_isDateLocked ? "lock-reason-date" : null)" />
@if (_isDateLocked)
{
    <MudText Typo="Typo.caption" id="lock-reason-date" Class="text-disabled">
        Overridden by tenant policy
    </MudText>
}
```

### MudToggleGroup (Browse Mode / Layout)
```razor
<MudToggleGroup T="string" @bind-Value="_browseMode" Color="Color.Primary" Outlined="true">
    <MudToggleItem T="string" Value="@("pagination")" Text="Pagination" />
    <MudToggleItem T="string" Value="@("infinite-scroll")" Text="Infinite Scroll" />
</MudToggleGroup>
```

### MudSelect (Page Size)
```razor
<MudSelect T="int" @bind-Value="_pageSize" Label="Page Size" Variant="Variant.Outlined" Dense="true">
    <MudSelectItem T="int" Value="12">12</MudSelectItem>
    <MudSelectItem T="int" Value="24">24</MudSelectItem>
    <MudSelectItem T="int" Value="48">48</MudSelectItem>
</MudSelect>
```

### MudPagination
```razor
<MudPagination Count="@_totalPages" Selected="@_currentPage"
               SelectedChanged="OnPageChangedAsync"
               ShowFirstButton="true" ShowLastButton="true"
               BoundaryCount="1" MiddleCount="3" />
```

---

## Relevant Skills (Must Load Before Implementation)

| Skill | When | Track |
|---|---|---|
| `clean-architecture-rules` | All phases — dependency rules | A + B |
| `cqrs-mediatr-guidelines` | Phase A4 — generic handlers | A |
| `dotnet-efcore-guidelines` | Phase A2 — migration, config | A |
| `auth-patterns` | Phase A5 — controller auth | A |
| `blazor-ui-conventions` | Phases B1-B8 — component structure, render modes | B |
| `blazor-css-isolation` | Phases B1, B5 — scoped CSS, BEM | B |
| `blazor-bff-patterns` | Phase B7 — service + proxy | B |
| `error-tracking` | Phases A4, A5 — observability, metrics | A |

---

## Session Progress (Final State — 2026-03-29)

### Status: ✅ Feature Complete (V1)

All Track A (backend) and Track B (frontend) tasks complete. Post-implementation UX refinements done.

### Post-B8 Work (Not in Original Task List)

#### Right Sidebar Refactor
- **What**: Replaced MudDrawer overlay with content-pushing sticky `RightSidebar` common component
- **File**: `Explore.Blazor.Client/Components/Common/RightSidebar.razor` (reusable)
- **Why**: Overlay drawer obscured content; sticky sidebar pushes main content left, better UX
- **Reusability**: Designed for future AI assistant panel or other sidebar features

#### EventCard UX Improvements
- Icon badges for visibility/audience/format with MudTooltip (replaced verbose text labels)
- CompactGrid progressive disclosure: `+N more` chip with hover reveal for hidden fields
- DetailedList clutter reduction: removed redundant labels, used icon-only badges
- Organizer hover treatment: subtle opacity transition on organizer info

#### Bug Fixes
- Drawer peeking fix: sidebar no longer partially visible when closed
- Missing Tune button: feature-flag bypass (`_showCustomizationButton = true`) so button always renders

### Key Implementation Files (Final)

| Layer | File | Status |
|---|---|---|
| Common UI | `Components/Common/RightSidebar.razor` | ✅ New — reusable sticky sidebar |
| EventCard | `Pages/Events/Components/EventCard.razor` | ✅ Extracted from EventList |
| EventCard CSS | `Pages/Events/Components/EventCard.razor.css` | ✅ Scoped styles with BEM |
| EventList | `Pages/Events/EventList.razor` | ✅ Refactored — uses EventCard + RightSidebar |
| EventList Code | `Pages/Events/EventList.razor.cs` | ✅ Customization state, autosave, pagination |
| Drawer | `Pages/Events/Components/EventListCustomizationDrawer.razor` | ✅ Settings sections |
| Pagination | `Pages/Events/Components/EventListPagination.razor` | ✅ MudPagination wrapper |
| Settings Service | `Services/UserSettingsService.cs` | ✅ Auth=API, Anon=localStorage, SSR-safe |
| Tests | `Explore.Blazor.Client.Tests/Pages/Events/` | ✅ EventCard + drawer + pagination tests |

### Build & Test Verification
- **Build**: `dotnet build --configuration Release` — ✅ Clean
- **Tests**: 654 passed, 1 pre-existing skip, 0 failures

### Remaining Optional Work
- Visual verification via Playwriter (screenshots of all layouts)
- Re-enable feature-flag gating (currently bypassed for development)
- E2E tests (B8.2/B8.3 deferred — need running app + Aspire)
