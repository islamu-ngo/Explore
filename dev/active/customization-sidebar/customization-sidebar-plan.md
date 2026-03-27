ABOUTME: Strategic plan for the Event List Customization Sidebar feature — split into Track A (Settings Platform) and Track B (EventList UI Refactor).
ABOUTME: Incorporates senior architect feedback: generic application core, unified API, rigorous lock semantics, SSR neutrality, observability, and cache invalidation.

# Plan: Event List Customization Sidebar (v2)

**Last Updated: 2026-03-26**

---

## Executive Summary

Build a **right-side settings drawer** for the Event List page allowing users to customize their browsing experience — inspired by MangaDex reader settings. The drawer exposes toggles for **pagination vs. infinite scroll**, **card information visibility**, and **layout preferences**.

**Platform-first approach**: The settings infrastructure is a **generic, reusable platform capability**. EventList is the **first consumer**, not the only one. The application layer handles any setting group generically; EventList-specific DTOs exist only at the API boundary.

**Two delivery tracks**:

- **Track A — Settings Platform**: Domain definitions, tenant-level locking, generic CQRS handlers, unified settings API. Independently valuable — can serve future settings UIs (appearance, notifications, etc.) without modification.
- **Track B — EventList UI Refactor**: EventCard extraction, pagination mode, customization drawer, autosave. Depends on Track A's API being available.

**Key architectural decisions**:
1. Reuse existing 5-tier hierarchical settings system (Instance > Tenant > Organization > Group > User)
2. **Generic handlers** — application layer is group-agnostic; uses `SettingRegistry` category filtering
3. **Unified settings controller** — single controller with scope-aware routes (`/api/settings/{scope}/{group}`)
4. **Rigorous lock semantics** — lower-scope values persist in storage but become non-effective during lock; unlock restores cascade
5. **No auto-merge** of anonymous→authenticated preferences in V1
6. **SSR/render-mode neutral** — feature works across static SSR, interactive server, interactive auto, and mixed modes

**Out of scope**: Waterfall/masonry layout, tag/category color persistence, organization/group scope UI, anonymous→authenticated preference migration.

---

## Non-Functional Requirements

These are **first-class requirements**, not afterthoughts. Every implementation decision must satisfy them.

### NFR-1: SSR/Render-Mode Neutrality
- Feature MUST work across: static SSR, InteractiveServer, InteractiveAuto, mixed render modes
- No browser-only API dependency during initial render (no JS interop for initial layout)
- No layout flash after hydration — initial render must match hydrated state
- Settings resolution happens server-side during SSR; client-side hydration preserves resolved values
- Fallback: If settings unavailable during SSR, use system defaults (never blank/broken UI)

### NFR-2: Accessibility
- All drawer controls keyboard-navigable (Tab, Enter, Space, Escape to close)
- Lock indicators announced to screen readers (`aria-disabled`, `aria-describedby` for lock reason)
- Focus trapped within open drawer; returns to trigger button on close
- Color contrast meets WCAG 2.1 AA for lock/disabled states

### NFR-3: URL State as Source of Truth
- Pagination state (page, pageSize) persisted in URL query parameters
- Browse mode persisted in URL when explicitly set
- Shareable URLs — opening a shared link reproduces the exact view
- Back/forward navigation works correctly with pagination state

### NFR-4: Cache Invalidation Determinism
- Every setting mutation has a defined cache invalidation path (see Cache Invalidation Strategy)
- No stale reads after writes within the same user session
- Cross-user cache staleness bounded to TTL (5 minutes max)

### NFR-5: Auditability
- Every setting change produces a `SettingChangedNotification` (existing MediatR pipeline)
- Lock/unlock operations logged with actor, scope, key, and timestamp
- Batch operations log individual key outcomes (applied vs. skipped)

### NFR-6: Feature Rollout Safety
- Feature behind a tenant-level setting (`Features.EventListCustomization.Enabled`)
- Disabled tenants see no gear icon, no drawer, no customization API access
- Enable/disable is instant (no deployment required)

---

## V1 Product Decisions

Explicit scope boundaries for the first release. These are **not** technical limitations — they are intentional product choices.

### PD-1: No Anonymous→Authenticated Preference Migration
- **Decision**: Authenticated users are server-authoritative, period
- **Behavior**: When an anonymous user logs in, their localStorage preferences are ignored. Server defaults (or previously saved preferences) apply.
- **Future**: Optional "Import browser preferences" as an explicit user action (V2+)
- **Rationale**: Auto-merge introduces UX confusion (which value wins?), conflict resolution complexity, and potential data quality issues. Server-authoritative is simpler and more predictable.

### PD-2: Pagination as Default Browse Mode
- **Decision**: `EventList.BrowseMode` defaults to `"pagination"` at system level
- **Rationale**: Better for SEO, URL sharing, accessibility, and predictable page load performance
- **Impact**: Existing infinite scroll behavior becomes opt-in

### PD-3: Organization/Group Scope UI Deferred
- **Decision**: Settings UI only exposes Instance > Tenant > User cascade
- **Implementation**: Org/Group scope resolution works in the resolver (already implemented) but has no management UI in V1
- **Future**: Org admins and group leaders get settings management pages (V2+)

### PD-4: EventList Settings Are First Consumer
- **Decision**: The settings platform built in Track A is generic, but EventList is the only consumer in V1
- **Implication**: The platform must be designed for reuse, but we don't need to prove reuse in this release

---

## Current State Analysis

### What Exists (Verified)

| Layer | Component | Path | Status |
|---|---|---|---|
| Domain | `UserPreference` entity | `Explore.Domain/UserPreference.cs` | Complete |
| Domain | `TenantSetting` entity | `Explore.Domain/TenantSetting.cs` | **Missing `IsLocked`** |
| Domain | `SystemSetting` entity (has `IsLocked`) | `Explore.Domain/SystemSetting.cs` | Complete |
| Domain | `SettingDefinition` record | `Explore.Domain/Settings/SettingDefinition.cs` | Complete |
| Domain | `SettingScope` enum (5 levels) | `Explore.Domain/Settings/SettingScope.cs` | Complete |
| Domain | `SettingRegistry` (static FrozenDictionary) | `Explore.Domain/Settings/SettingRegistry.cs` | Complete |
| Domain | `GovernanceSettingKeys` constants | `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Needs new keys |
| Domain | 17 `*SettingDefinitions` classes | `Explore.Domain/Settings/Definitions/` | Pattern established |
| Application | `IHierarchicalSettingsResolver` | `Explore.Application/Contracts/Infrastructure/` | Complete (instance lock only) |
| Application | `ISettingGroup` interface | `Explore.Application/Contracts/Infrastructure/` | Complete |
| Application | 16 setting group implementations | `Explore.Application/Settings/Groups/` | Pattern established |
| Application | `SettingContext`, `SettingValueSerializer` | `Explore.Application/Settings/` | Complete |
| Application | `SettingUpsertService` | `Explore.Application/Settings/` | Complete |
| Application | `SettingChangedNotification` + `SettingAuditLogHandler` | `Explore.Application/` | Complete |
| Infrastructure | `HierarchicalSettingsResolver` | `Explore.Infrastructure/Services/` | **Instance lock only** |
| Persistence | `UserPreferenceRepository` | `Explore.Persistence/Repositories/` | Complete |
| Persistence | `TenantSettingRepository` | `Explore.Persistence/Repositories/` | Complete |
| API | `InstanceSettingsController` | `Explore.API/Controllers/` | Exists (sub-resource pattern) |
| API | `UserAppearanceController` | `Explore.API/Controllers/` | Exists (user preference precedent) |
| Blazor | EventList page | `Explore.Blazor.Client/Pages/Events/EventList.razor/.cs/.css` | 1291+1150+592 lines — monolith |
| Blazor | EventFilterBar | `Explore.Blazor.Client/Pages/Events/Components/` | Needs customization button |
| Blazor | EventCard.razor (inline, 129 lines) | `Explore.Blazor.Client/Pages/Events/Components/` | Exists but not properly extracted |

### Architectural Gaps

1. **Tenant-level locking**: `TenantSetting` has no `IsLocked` column. `HierarchicalSettingsResolver.LockAsync()` only supports `SettingScope.Instance`. Resolver's `ResolveSingleKey()` only checks `systemSetting?.IsLocked`.
2. **No user preference API**: No controller exposes CRUD for `UserPreference`. Only `InstanceSettingsController` and `UserAppearanceController` exist.
3. **No generic settings handlers**: Each feature builds its own handlers. Need group-agnostic CQRS handlers for resolve/update/reset/lock.
4. **No `SettingSource.TenantLocked`**: Enum only has `SystemLocked`, no tenant equivalent.
5. **EventList monolith**: 1150-line code-behind with inline card rendering — needs extraction before settings integration.
6. **No pagination mode**: EventList uses `Virtualize<EventListDto>` exclusively; no paginated rendering path.
7. **No metadata contract**: `ResolvedSetting` lacks `CanEdit` and `Reason` fields needed for UI lock indicators.

---

## Proposed Future State

### Setting Keys (dot-notation, `GovernanceSettingKeys.EventList.*`)

| Key | Type | Default | Description |
|---|---|---|---|
| `EventList.BrowseMode` | `string` | `"pagination"` | `"pagination"` or `"infinite-scroll"` |
| `EventList.PageSize` | `int` | `12` | Items per page (pagination mode) |
| `EventList.DefaultLayout` | `string` | `"DetailedList"` | Default layout mode |
| `EventList.Card.ShowDate` | `bool` | `true` | Show event date on card |
| `EventList.Card.ShowLocation` | `bool` | `true` | Show location on card |
| `EventList.Card.ShowOrganizer` | `bool` | `true` | Show organizer name |
| `EventList.Card.ShowDescription` | `bool` | `true` | Show description snippet |
| `EventList.Card.ShowTags` | `bool` | `true` | Show tag chips |
| `EventList.Card.ShowCategories` | `bool` | `true` | Show category chips |
| `EventList.Card.ShowCapacity` | `bool` | `false` | Show capacity/registration count |
| `EventList.Card.ShowPrice` | `bool` | `true` | Show price indicator |
| `EventList.Card.ShowStatus` | `bool` | `true` | Show event status badge |

### Effective Setting Metadata Contract

Every resolved setting returned by the API includes this metadata — reusable across all future settings UIs:

```csharp
public sealed record EffectiveSettingDto(
    string Key,
    string Value,               // JSON-serialized current effective value
    string ValueType,           // "String", "Integer", "Boolean"
    string Source,              // Human-readable: "System Default", "Tenant Override", "User Preference"
    bool IsLocked,              // Whether a higher scope has locked this key
    bool CanEdit,               // Computed: !IsLocked && user has scope permission
    string? Reason,             // Null if CanEdit=true; "Locked by tenant administrator" if not
    string? Description,        // Human-readable setting description
    IReadOnlyList<string>? AllowedValues  // Constrained values (if any)
);
```

**`CanEdit` computation**:
- `false` + Reason="Locked by instance administrator" → `SystemSetting.IsLocked = true`
- `false` + Reason="Locked by tenant administrator" → `TenantSetting.IsLocked = true`
- `false` + Reason="Insufficient permissions" → user lacks scope permission
- `true` + Reason=null → user can modify at their scope

**`Source` values** (human-readable mapping from `SettingSource` enum):

| `SettingSource` enum | `Source` string |
|---|---|
| `SystemDefault` | `"System Default"` |
| `SystemLocked` | `"Instance Policy (Locked)"` |
| `TenantOverride` | `"Tenant Override"` |
| `TenantLocked` | `"Tenant Policy (Locked)"` |
| `OrganizationOverride` | `"Organization Override"` |
| `GroupOverride` | `"Group Override"` |
| `UserPreference` | `"User Preference"` |

### Lock Semantics (Rigorous Definition)

**Lock = "this scope's value becomes authoritative; lower scopes cannot override"**

Precise behavior:

1. **When tenant admin locks `EventList.BrowseMode`**:
   - `TenantSetting.IsLocked` is set to `true` for that key
   - Existing user preferences for that key are **NOT deleted** — they stay in storage
   - Resolver returns the tenant's value with `Source = TenantLocked`, `IsLocked = true`
   - UI shows the locked value with lock icon and "Overridden by tenant policy"
   - User's `MudSwitch` / `MudToggleGroup` is `Disabled=true`

2. **When tenant admin unlocks the same key**:
   - `TenantSetting.IsLocked` is set to `false`
   - Cascade resumes: user preferences become effective again
   - User gets their previously-saved value back (it was never deleted)
   - No data migration, no "reset to defaults" — just flag flip

3. **Lock precedence**: Instance locked > Tenant locked > Unlocked (lower scopes can override)
   - If both instance AND tenant lock the same key, instance value wins
   - Unlocking at tenant does NOT override an instance lock

4. **What lock does NOT do**:
   - Does NOT delete lower-scope rows
   - Does NOT prevent lower-scope writes to storage (values are saved but non-effective)
   - Does NOT require migration on lock/unlock

### Batch Update Modes

Two explicit modes for batch setting updates — designed into the application service:

| Mode | Behavior | Use Case |
|---|---|---|
| **BestEffort** | Skip locked keys, apply rest. Return which keys were skipped with reasons. | Drawer autosave — user changes 3 settings, 1 is locked → apply 2, report 1 skipped |
| **Strict** | Reject entire request if ANY key is locked. Return all locked keys in error. | Admin operations — all-or-nothing semantics for policy enforcement |

```csharp
public enum BatchUpdateMode
{
    BestEffort,  // Skip locked, apply rest — for autosave UX
    Strict       // Reject all if any locked — for admin operations
}
```

**Batch response shape**:
```csharp
public sealed record BatchUpdateResponse(
    bool Success,                              // true if all requested applied (or BestEffort with some skipped)
    IReadOnlyList<SettingUpdateResult> Results  // Per-key outcome
);

public sealed record SettingUpdateResult(
    string Key,
    bool Applied,
    string? SkipReason  // null if applied; "Locked by tenant administrator" if skipped
);
```

### Unified Settings API Routes

Single `SettingsController` with scope-aware routes. **No separate `UserPreferencesController` or `TenantSettingsController`.**

```
# User scope (authenticated users managing their preferences)
GET    /api/settings/user/{category}              → Resolve effective settings for category
PUT    /api/settings/user/{category}              → Batch update user preferences (BestEffort)
PUT    /api/settings/user/keys/{key}              → Update single user preference
DELETE /api/settings/user/keys/{key}              → Reset single user preference (fall back to parent)

# Tenant scope (tenant admins managing tenant policies)
GET    /api/settings/tenant/{category}            → Get tenant-level settings with lock status
PUT    /api/settings/tenant/{category}            → Batch update tenant settings (Strict)
PUT    /api/settings/tenant/keys/{key}            → Update single tenant setting
POST   /api/settings/tenant/keys/{key}/lock       → Lock setting at tenant level
DELETE /api/settings/tenant/keys/{key}/lock       → Unlock setting at tenant level
```

**Notes**:
- `{category}` maps to `SettingDefinition.Category` (e.g., `event-list`, `appearance`)
- Existing `InstanceSettingsController` at `/api/instance/settings/{domain}` remains unchanged — future migration to `/api/settings/instance/{category}` is optional
- User routes require `[Authorize]`; tenant routes require `[Authorize(Roles = "TenantAdmin")]`
- GET on user scope returns the fully-resolved cascade (effective values); GET on tenant scope returns tenant-level overrides only

### Hierarchy Resolution Example

```
Instance Admin sets: EventList.BrowseMode = "pagination" (NOT locked)
Tenant Admin sets:  EventList.BrowseMode = "infinite-scroll", IsLocked = true
User tries:         EventList.BrowseMode = "pagination"
Result:             "infinite-scroll" (Source = "Tenant Policy (Locked)", CanEdit = false)

Tenant Admin unlocks EventList.BrowseMode:
Result:             "pagination" (Source = "User Preference", CanEdit = true)
                    — user's stored value restored without data migration
```

### UI Layout (Right Sidebar)

```
+------------------------------------------+--------+
| Event List Page                          | [gear] |
| [Search Bar                            ] |        |
| [Filters] [Sort] [Layout: |||  :::  =] [settings]|
|                                          |        |
| +--------------------------------------+ | Drawer |
| | Event Cards (paginated OR infinite)  | | ====== |
| |                                      | | Browse |
| |                                      | | Mode:  |
| |                                      | | [Pag|Inf]
| |                                      | |        |
| |                                      | | Card   |
| |                                      | | Fields:|
| |                                      | | [x]Date|
| |                                      | | [x]Loc |
| +--------------------------------------+ | [x]Org |
| [< 1 2 3 ... 10 >] (pagination mode)    | ...    |
+------------------------------------------+--------+
```

The settings button (gear icon) sits right of the `MudToggleGroup<LayoutMode>` in EventFilterBar. Opens `MudDrawer` with `Anchor.End`, `Variant.Temporary`, `Width="320px"`. Coexists with the existing detail drawer — only one open at a time.

---

## Track A: Settings Platform

**Goal**: Build generic, reusable settings infrastructure. EventList is the first consumer, not the design driver.

### Phase A1: Domain — Setting Definitions & TenantSetting.IsLocked

**Goal**: Define EventList setting keys and add tenant-level locking capability.

#### A1.1: Add `EventList` Keys to `GovernanceSettingKeys`
- **File**: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- **Action**: Add nested `EventList` static class with 12 dot-notation constants
- **Pattern**: Follow existing nested class pattern (e.g., `Events`, `Appearance`)
- **Effort**: S

#### A1.2: Create `EventListSettingDefinitions`
- **File**: `Explore.Domain/Settings/Definitions/EventListSettingDefinitions.cs`
- **Action**: Define `static IReadOnlyList<SettingDefinition> All` with 12 definitions
- **Details**: Each definition: `Category = "EventList"`, `MinScope = SettingScope.Tenant`, `MaxScope = SettingScope.User`, `IsLockable = true`
- **Pattern**: Follow `EventSettingDefinitions.cs` or `AppearanceSettingDefinitions.cs`
- **Effort**: S

#### A1.3: Register Definitions in `SettingRegistry`
- **File**: `Explore.Domain/Settings/SettingRegistry.cs`
- **Action**: Ensure `EventListSettingDefinitions.All` is collected by the registry
- **Verify**: Registry auto-collects via reflection or requires explicit addition
- **Effort**: S

#### A1.4: Add `IsLocked` to `TenantSetting` Entity
- **File**: `Explore.Domain/TenantSetting.cs`
- **Action**: Add `public bool IsLocked { get; set; }` (no default in entity — set in EF config)
- **Effort**: S

**Phase A1 Acceptance**:
- [ ] All 12 keys in `GovernanceSettingKeys.EventList`
- [ ] 12 `SettingDefinition` records in `EventListSettingDefinitions.All`
- [ ] Registry contains all 12 keys (unit test)
- [ ] `TenantSetting.IsLocked` property exists
- [ ] Build succeeds

---

### Phase A2: Persistence — Migration & Configuration

**Goal**: Schema migration for `TenantSetting.IsLocked` and repository support for lock operations.

#### A2.1: Update `TenantSettingConfiguration`
- **File**: `Explore.Persistence/Configurations/Entities/TenantSettingConfiguration.cs`
- **Action**: `builder.Property(e => e.IsLocked).HasDefaultValue(false).HasColumnName("is_locked");`
- **Effort**: S

#### A2.2: Create EF Core Migration
- **Command**: `dotnet ef migrations add AddTenantSettingIsLocked --project Explore.Persistence --startup-project Explore.API`
- **Verify**: Adds `is_locked boolean NOT NULL DEFAULT false` to `tenant_settings`
- **Effort**: S

#### A2.3: Add Lock/Unlock to `TenantSettingRepository`
- **Contract**: `Explore.Application/Contracts/Persistence/ITenantSettingRepository.cs`
- **Impl**: `Explore.Persistence/Repositories/TenantSettingRepository.cs`
- **Methods**: `LockAsync(Guid tenantId, string key)`, `UnlockAsync(Guid tenantId, string key)`
- **Effort**: S

**Phase A2 Acceptance**:
- [ ] Migration applies cleanly
- [ ] `is_locked` column in `tenant_settings` with `DEFAULT false`
- [ ] Lock/unlock repository methods work (integration test)

---

### Phase A3: Infrastructure — Tenant Lock Resolution

**Goal**: Extend `HierarchicalSettingsResolver` to enforce tenant-level locks in the cascade.

#### A3.1: Add `SettingSource.TenantLocked` Enum Value
- **File**: `Explore.Application/Contracts/Infrastructure/ResolvedSetting.cs` (where `SettingSource` is defined)
- **Action**: Add `TenantLocked` value
- **Update**: All `switch` expressions on `SettingSource` for exhaustive coverage
- **Effort**: S

#### A3.2: Update `ResolveSingleKey` Lock Cascade
- **File**: `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`
- **Action**:
  1. After checking `systemSetting?.IsLocked`, also check `tenantSetting?.IsLocked`
  2. If tenant locked: return tenant value with `Source = SettingSource.TenantLocked`, `IsLocked = true`
  3. Prevent org/group/user overrides when tenant is locked
  4. Lock precedence: Instance locked > Tenant locked > unlocked cascade
- **Critical**: Lower-scope values remain in storage — lock only affects resolution, not storage
- **Effort**: M

#### A3.3: Extend `LockAsync` for Tenant Scope
- **File**: `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`
- **Action**: Handle `SettingScope.Tenant` in `LockAsync` — set `TenantSetting.IsLocked = true` via repository
- **Add**: `UnlockAsync(SettingContext, string key, SettingScope scope)` method to interface and implementation
- **Effort**: S

#### A3.4: Cache Invalidation for Lock Operations
- **Action**: When a tenant locks/unlocks a key:
  1. Invalidate tenant cache: `InvalidateCache(tenantId)`
  2. Invalidate all user caches under that tenant (or let TTL handle it — 5 min max staleness)
- **Effort**: S

#### A3.5: Unit Tests for Lock Cascade
- **Tests**:
  - Tenant-locked setting returns tenant value, ignores user preference
  - Instance-locked setting returns instance value, ignores tenant and user
  - Unlocked setting allows full cascade (user > group > org > tenant > instance)
  - Lock + unlock round-trip: user value restored after unlock
  - Concurrent instance + tenant lock: instance wins
- **Effort**: M

**Phase A3 Acceptance**:
- [ ] Tenant-locked settings prevent user/org/group overrides
- [ ] Instance lock takes priority over tenant lock
- [ ] Lock/unlock does not delete lower-scope rows
- [ ] Unlock restores cascade (user gets their stored value back)
- [ ] All tests pass, no regression

---

### Phase A4: Application — Generic Settings Handlers

**Goal**: Build group-agnostic CQRS handlers. These work with ANY setting category — not just EventList.

#### A4.1: Create `EventListSettingGroup`
- **File**: `Explore.Application/Settings/Groups/EventListSettingGroup.cs`
- **Action**: Implement `ISettingGroup` with 12 typed properties
- **Note**: This is a typed wrapper for Blazor convenience. The generic handlers use `ResolveBatchAsync` directly.
- **Pattern**: Follow `AppearanceSettingGroup`
- **Effort**: S

#### A4.2: Create `EffectiveSettingDto` and Response DTOs
- **File**: `Explore.Application/DTOs/Settings/EffectiveSettingDto.cs`
- **DTOs**:
  - `EffectiveSettingDto` — Key, Value, ValueType, Source, IsLocked, CanEdit, Reason, Description, AllowedValues
  - `SettingGroupResponse` — Category, IReadOnlyList<EffectiveSettingDto>
  - `BatchUpdateResponse` — Success, IReadOnlyList<SettingUpdateResult>
  - `SettingUpdateResult` — Key, Applied, SkipReason
  - `BatchUpdateMode` enum — BestEffort, Strict
- **Effort**: S

#### A4.3: Create Generic Query — `ResolveSettingGroupQuery`
- **File**: `Explore.Application/Features/Settings/Handlers/Queries/ResolveSettingGroupQueryHandler.cs`
- **Request**: `ResolveSettingGroupQuery(string Category)`
- **Handler logic**:
  1. Filter `SettingRegistry` by `Category` to get all keys
  2. Call `ResolveBatchAsync(context, keys)` with user's `SettingContext`
  3. For each `ResolvedSetting`, compute `CanEdit` and `Reason` based on lock state and user permissions
  4. Return `SettingGroupResponse` with `EffectiveSettingDto[]`
- **Generic**: Works for "EventList", "Appearance", or any future category
- **Effort**: M

#### A4.4: Create Generic Command — `UpdateSettingCommand` (Single Key)
- **File**: `Explore.Application/Features/Settings/Handlers/Commands/UpdateSettingCommandHandler.cs`
- **Request**: `UpdateSettingCommand(string Key, string Value, SettingScope Scope)`
- **Handler logic**:
  1. Validate key exists in `SettingRegistry`
  2. Validate value parses to expected type (from `SettingDefinition.ValueType`)
  3. Validate value is in `AllowedValues` (if constrained)
  4. Check setting not locked at higher scope
  5. Call `SetValueAsync(context, key, value, scope)`
  6. Publish `SettingChangedNotification`
- **Returns**: `BaseCommandResponse<Guid>`
- **Effort**: M

#### A4.5: Create Generic Command — `UpdateSettingBatchCommand`
- **File**: `Explore.Application/Features/Settings/Handlers/Commands/UpdateSettingBatchCommandHandler.cs`
- **Request**: `UpdateSettingBatchCommand(string Category, Dictionary<string, string> Values, SettingScope Scope, BatchUpdateMode Mode)`
- **Handler logic**:
  1. Validate all keys belong to the specified category
  2. For each key: check lock status
  3. **BestEffort**: Skip locked keys, apply rest, return per-key results
  4. **Strict**: If any locked, reject entire batch with all locked keys listed
  5. Apply unlocked values via `SetValueAsync` or `SettingUpsertService`
  6. Publish `SettingChangedNotification` for each applied key
- **Returns**: `BatchUpdateResponse`
- **Observability**: Log each skipped key with reason at `Information` level
- **Effort**: M

#### A4.6: Create Generic Command — `ResetSettingCommand`
- **File**: `Explore.Application/Features/Settings/Handlers/Commands/ResetSettingCommandHandler.cs`
- **Request**: `ResetSettingCommand(string Key, SettingScope Scope)`
- **Handler logic**:
  1. Validate key exists in `SettingRegistry`
  2. Call `RemoveOverrideAsync(context, key, scope)` — falls back to parent scope
  3. Publish `SettingChangedNotification`
- **Returns**: `BaseCommandResponse<Guid>`
- **Effort**: S

#### A4.7: Create Admin Commands — `LockSettingCommand` / `UnlockSettingCommand`
- **Files**: `Explore.Application/Features/Settings/Handlers/Commands/LockSettingCommandHandler.cs`, `UnlockSettingCommandHandler.cs`
- **Requests**: `LockSettingCommand(string Key, SettingScope Scope)`, `UnlockSettingCommand(string Key, SettingScope Scope)`
- **Handler logic**:
  1. Validate key exists and `IsLockable = true`
  2. Validate scope is `Tenant` or `Instance` (users can't lock)
  3. Call `LockAsync` / new `UnlockAsync` on resolver
  4. Invalidate caches (tenant + affected users)
  5. Publish `SettingChangedNotification` with lock metadata
- **Observability**: Log lock/unlock at `Information` level with actor, key, scope
- **Returns**: `BaseCommandResponse<Guid>`
- **Effort**: S

#### A4.8: Unit Tests for All Handlers
- **Project**: `Event.Application.UnitTests`
- **Tests**:
  - ResolveSettingGroupQuery returns `EffectiveSettingDto[]` with correct Source, CanEdit, Reason
  - ResolveSettingGroupQuery returns all keys for the specified category
  - UpdateSettingCommand rejects locked keys with descriptive error
  - UpdateSettingCommand rejects invalid keys, invalid values, out-of-range values
  - UpdateSettingBatchCommand BestEffort: skips locked, applies rest, returns per-key results
  - UpdateSettingBatchCommand Strict: rejects entire batch if any locked
  - ResetSettingCommand removes override, cascade resumes
  - LockSettingCommand sets IsLocked, UnlockSettingCommand clears it
  - Validators manually instantiated (not DI)
- **Effort**: M

**Phase A4 Acceptance**:
- [ ] Generic handlers work for any setting category (not hardcoded to EventList)
- [ ] `EffectiveSettingDto` includes CanEdit and Reason fields
- [ ] BestEffort and Strict batch modes work correctly
- [ ] Lock/unlock handlers manage cache invalidation
- [ ] All unit tests pass
- [ ] Validators manually instantiated

---

### Phase A5: API — Unified Settings Controller

**Goal**: Single controller with scope-aware routes for all setting operations.

#### A5.1: Create `SettingsController`
- **File**: `Explore.API/Controllers/SettingsController.cs`
- **Routes** (see Unified Settings API Routes section above):
  - `GET /api/settings/user/{category}` — `[Authorize]`
  - `PUT /api/settings/user/{category}` — `[Authorize]`
  - `PUT /api/settings/user/keys/{key}` — `[Authorize]`
  - `DELETE /api/settings/user/keys/{key}` — `[Authorize]`
  - `GET /api/settings/tenant/{category}` — `[Authorize(Roles = "TenantAdmin")]`
  - `PUT /api/settings/tenant/{category}` — `[Authorize(Roles = "TenantAdmin")]`
  - `PUT /api/settings/tenant/keys/{key}` — `[Authorize(Roles = "TenantAdmin")]`
  - `POST /api/settings/tenant/keys/{key}/lock` — `[Authorize(Roles = "TenantAdmin")]`
  - `DELETE /api/settings/tenant/keys/{key}/lock` — `[Authorize(Roles = "TenantAdmin")]`
- **Mapping**: Controller maps route parameters to generic handler commands
  - `{category}` route param → `ResolveSettingGroupQuery.Category`
  - `{key}` route param → `UpdateSettingCommand.Key`
  - User routes → `SettingScope.User`, Tenant routes → `SettingScope.Tenant`
- **HATEOAS**: Follow existing conventions. Include `_links` for related operations (e.g., lock link on tenant GET). Don't over-engineer — these are internal preference APIs.
- **Caching**: Output cache with `UserData` policy (short TTL, vary by user + tenant)
- **Rate limiting**: `authenticated` policy for user routes, `write` policy for mutations
- **Effort**: L

#### A5.2: Add Route Names to `RouteNames.cs`
- **File**: `Explore.API/RouteNames.cs`
- **Action**: Add constants for all new routes
- **Effort**: S

#### A5.3: HATEOAS Link Policies
- **Files**: New link policy classes for settings responses
- **Action**: Follow existing `DetailLinkPolicy` / `CollectionLinkPolicy` pattern
- **Pragmatism**: Keep minimal — self link, category link, lock/unlock links where applicable
- **Effort**: S

#### A5.4: Feature Gate Middleware
- **Action**: Settings API for a category should return 404 if the feature is disabled for the tenant
- **Check**: `Features.EventListCustomization.Enabled` tenant setting
- **Implementation**: Check in controller action or via a filter attribute
- **Effort**: S

**Phase A5 Acceptance**:
- [ ] All routes return correct HTTP status codes
- [ ] User routes require `[Authorize]`, tenant routes require `TenantAdmin`
- [ ] Route params correctly map to generic handler commands
- [ ] HATEOAS links present (pragmatic, not over-engineered)
- [ ] Feature-gated: disabled tenants get 404

---

### Phase A6: NSwag Client Regeneration

#### A6.1: Regenerate NSwag Client
- **Action**: Run NSwag generation after new controller is added
- **Verify**: Generated client includes `EffectiveSettingDto`, `SettingGroupResponse`, `BatchUpdateResponse`, and all endpoint methods
- **Effort**: S

---

### Phase A7: Track A Integration Tests

#### A7.1: Persistence Integration Tests
- **Project**: `Event.Persistence.IntegrationTests`
- **Tests**: UserPreference CRUD, TenantSetting lock/unlock, migration applies
- **Effort**: M

#### A7.2: API Integration Tests
- **Project**: `Event.API.IntegrationTests`
- **Tests**:
  - GET user/event-list returns defaults for new user (CanEdit=true, Source="System Default")
  - PUT user/event-list updates preference, GET reflects change (Source="User Preference")
  - PUT user/event-list with locked key: BestEffort skips locked, applies rest
  - DELETE user/keys/{key} resets to parent value
  - GET tenant/event-list returns tenant-level view with lock status
  - POST tenant/keys/{key}/lock sets lock, user GET shows CanEdit=false
  - DELETE tenant/keys/{key}/lock removes lock, user GET shows CanEdit=true
  - Non-admin gets 403 on tenant routes
  - Disabled feature returns 404
- **Effort**: M

#### A7.3: Architecture Tests
- **Project**: `Event.Architecture.Tests`
- **Tests**: New classes follow Clean Architecture dependency rules
- **Effort**: S

**Phase A7 Acceptance**:
- [ ] All integration tests pass
- [ ] Build succeeds in Release

---

## Track B: EventList UI Refactor

**Goal**: Refactor EventList page for customization support. Depends on Track A API being available.

**Prerequisite**: Track A phases A1–A6 complete (API endpoints available, NSwag client generated).

### Phase B0: Baseline Regression Guard

**Goal**: Establish baseline before touching EventList. Any regression is immediately detectable.

#### B0.1: Baseline Screenshots
- **Action**: Use Playwriter to capture screenshots of EventList in all 3 layout modes (CompactGrid, DetailedList, SingleRow) at desktop and mobile breakpoints
- **Store**: `dev/active/customization-sidebar/baselines/`
- **Effort**: S

#### B0.2: Baseline Test Coverage
- **Action**: Ensure existing Blazor component tests for EventList pass before any changes
- **Run**: `dotnet test --project Explore.Blazor.Client.Tests`
- **Document**: Any pre-existing failures
- **Effort**: S

**Phase B0 Acceptance**:
- [ ] Baseline screenshots captured for all 3 layouts × 2 breakpoints
- [ ] All pre-existing tests documented (pass/fail)

---

### Phase B1: EventCard Component Extraction

**Goal**: Extract inline card rendering into a standalone component. **This is the highest-risk task — do it first, in isolation.**

#### B1.1: Create `EventCard` Component
- **Files**:
  - `Explore.Blazor.Client/Pages/Events/Components/EventCard.razor`
  - `Explore.Blazor.Client/Pages/Events/Components/EventCard.razor.cs`
  - `Explore.Blazor.Client/Pages/Events/Components/EventCard.razor.css`
- **Parameters**:
  - `EventListDto Event` — the event data
  - `LayoutMode Layout` — current layout mode
  - `EventCallback<EventListDto> OnClick` — card click handler
- **Note**: Do NOT add settings-based field visibility yet. Pure extraction only.
- **Effort**: L

#### B1.2: Migrate Card CSS
- **Action**: Move card-specific CSS from `EventList.razor.css` to `EventCard.razor.css`
- **Risk**: Container queries (`@container`) reference parent grid — verify they still work after extraction
- **Effort**: M

#### B1.3: Update EventList to Use EventCard Component
- **Action**: Replace inline card markup in EventList.razor with `<EventCard>` component
- **Verify**: All 3 layout modes render identically to baseline screenshots
- **Effort**: M

#### B1.4: Visual Regression Check
- **Action**: Compare Playwriter screenshots against B0.1 baselines for all 3 layouts
- **Gate**: Zero visual regression before proceeding
- **Effort**: S

**Phase B1 Acceptance**:
- [ ] `EventCard` is a standalone component with clean separation
- [ ] All 3 layout modes render identically to baseline
- [ ] Container queries work correctly after DOM restructure
- [ ] Card click → detail drawer still works
- [ ] Build succeeds

---

### Phase B2: Loading & State Separation

**Goal**: Separate EventList loading/state management from rendering. Prepare for dual-mode (pagination + infinite scroll).

#### B2.1: Extract Loading State
- **Action**: Create clear state boundaries in EventList.razor.cs:
  - `_isLoading`, `_events` (for paged), `_browseMode`, `_currentPage`, `_totalPages`, `_pageSize`
  - Separate `LoadPagedEventsAsync()` from existing `LoadEventsAsync()` (Virtualize provider)
- **Effort**: M

#### B2.2: Extract Event Data Service Calls
- **Action**: Ensure `IEventService` supports both:
  - Virtualize `ItemsProvider` (existing — returns items for a range)
  - Paged query (new — returns `PaginatedResult<EventListDto>` with total count)
- **Verify**: API already supports `?page=N&pageSize=N` parameters
- **Effort**: M

**Phase B2 Acceptance**:
- [ ] Clear separation between loading logic and rendering
- [ ] Both loading paths available (virtualize + paged)
- [ ] No functional regression

---

### Phase B3: Paginated Rendering Mode

**Goal**: Add paginated rendering alongside existing Virtualize infinite scroll.

#### B3.1: Create `EventListPagination` Component
- **Files**: `Explore.Blazor.Client/Pages/Events/Components/EventListPagination.razor/.cs/.css`
- **Features**: Wraps `MudPagination`, page size selector, keyboard accessible, `EventCallback<int> OnPageChanged`
- **Effort**: M

#### B3.2: Dual-Mode Rendering in EventList
- **Action**:
  ```razor
  @if (_isVirtualized)
  {
      <Virtualize @ref="_virtualize" ItemsProvider="LoadEventsAsync" ...>
          <EventCard Event="context" Layout="_layout" OnClick="OnCardClicked" />
      </Virtualize>
  }
  else
  {
      @foreach (var evt in _pagedEvents)
      {
          <EventCard Event="evt" Layout="_layout" OnClick="OnCardClicked" />
      }
      <EventListPagination CurrentPage="_currentPage" TotalPages="_totalPages"
                           PageSize="_pageSize" OnPageChanged="OnPageChangedAsync" />
  }
  ```
- **Mode source**: Determined by resolved setting (server-side during SSR) or URL override
- **SSR safety**: Default to pagination (server-renderable) during SSR; Virtualize requires interactivity
- **Effort**: L

**Phase B3 Acceptance**:
- [ ] Toggle between infinite scroll and pagination works
- [ ] Pagination renders correctly in all 3 layouts
- [ ] Filters/sorts work in both modes
- [ ] SSR renders pagination by default (no blank page during prerender)

---

### Phase B4: URL State Management

**Goal**: Pagination state in URL for shareability and back/forward navigation.

#### B4.1: Add Query Parameters
- **File**: `Explore.Blazor.Client/Pages/Events/EventList.razor.cs`
- **Parameters**:
  - `[SupplyParameterFromQuery(Name = "page")] int? Page`
  - `[SupplyParameterFromQuery(Name = "pageSize")] int? PageSize`
- **Effort**: S

#### B4.2: URL Synchronization
- **Action**:
  - Pagination mode: URL includes `?page=3&pageSize=12`
  - Infinite scroll mode: URL removes page/pageSize params
  - Page navigation: `NavigationManager.NavigateTo(uri, replace: false)` for back/forward support
  - Mode switch: `NavigationManager.NavigateTo(uri, replace: true)` to avoid history spam
- **Effort**: M

**Phase B4 Acceptance**:
- [ ] Pagination URL is shareable (opening link shows correct page)
- [ ] Back/forward navigation works
- [ ] Mode switch doesn't pollute browser history

---

### Phase B5: Customization Drawer

**Goal**: Build the right-sidebar settings drawer.

#### B5.1: Create `EventListCustomizationDrawer` Component
- **Files**: `Explore.Blazor.Client/Pages/Events/Components/EventListCustomizationDrawer.razor/.cs/.css`
- **Structure**:
  ```
  MudDrawer (Anchor.End, Variant.Temporary, Width="320px", Overlay, Elevation=4)
    MudStack (Vertical)
      Header: "Customize View" + close button
      Divider
      Section: "Browse Mode"
        MudToggleGroup<string> — "Pagination" | "Infinite Scroll"
        (if pagination) MudSelect<int> — Page Size (12, 24, 48)
      Divider
      Section: "Default Layout"
        MudToggleGroup<LayoutMode> — CompactGrid | DetailedList | SingleRow
      Divider
      Section: "Card Information"
        MudSwitch<bool> per card field — with lock icon if locked
        Locked switches: Disabled=true, aria-describedby="lock reason"
        Tooltip: "Overridden by tenant policy"
      Divider
      Footer: "Reset to Defaults" button
  ```
- **Parameters**:
  - `bool Open` / `EventCallback<bool> OpenChanged` — two-way binding
  - `IReadOnlyList<EffectiveSettingDto> Settings` — resolved from API
  - `EventCallback<Dictionary<string, string>> OnSettingsChanged` — notifies parent
- **Effort**: L

#### B5.2: Add Settings Button to EventFilterBar
- **File**: `Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor`
- **Action**: `MudIconButton` (Tune icon) right of layout switcher
- **Gated**: Only visible if `Features.EventListCustomization.Enabled`
- **Effort**: S

#### B5.3: Wire Drawer into EventList
- **Action**:
  - Add `_customizationDrawerOpen` state
  - Load settings via settings API on init (resolved for current user)
  - Render `<EventListCustomizationDrawer>` with two-way `Open` binding
  - On settings changed: update local state, re-render affected sections
  - Mutual exclusion: close detail drawer when customization opens
- **Effort**: M

#### B5.4: Add Card Field Visibility to EventCard
- **Action**: Add `IReadOnlyList<EffectiveSettingDto>? FieldSettings` parameter to `EventCard`
- **Behavior**: Each card field conditionally rendered based on corresponding setting's `Value`
- **Default**: If `FieldSettings` is null, show all fields (backward-compatible)
- **Effort**: M

#### B5.5: Drawer CSS (BEM + Scoped)
- **Classes**: `.customization-drawer`, `__header`, `__section`, `__section-title`, `__toggle`, `__toggle--locked`, `__footer`
- **Responsive**: Full-width on mobile
- **Accessibility**: Focus trap, escape to close, keyboard navigation
- **Effort**: M

**Phase B5 Acceptance**:
- [ ] Drawer opens from settings button, closes on overlay/close/Escape
- [ ] All settings rendered with correct control type
- [ ] Locked settings show lock icon, disabled, with reason tooltip
- [ ] Card field visibility respects settings
- [ ] Mutual exclusion with detail drawer
- [ ] Responsive on mobile
- [ ] Keyboard accessible

---

### Phase B6: Autosave & Reset

**Goal**: Auto-save settings changes and reset-to-defaults functionality.

#### B6.1: Implement Debounced Autosave
- **Action**: On any setting change in drawer, debounce 500ms, then call PUT `/api/settings/user/event-list` with BestEffort mode
- **Feedback**: Show subtle "Saved" indicator; show "X skipped (locked)" if any
- **Error handling**: Show error toast on failure, revert local state
- **Effort**: M

#### B6.2: Implement Reset to Defaults
- **Action**: "Reset to Defaults" button in drawer footer
- **Behavior**: Calls DELETE `/api/settings/user/keys/{key}` for each user-overridden key
- **Confirmation**: `MudDialog` confirmation before reset
- **Effort**: S

**Phase B6 Acceptance**:
- [ ] Changes auto-save after 500ms debounce
- [ ] Locked keys skipped silently in autosave (BestEffort)
- [ ] Reset removes all user overrides with confirmation
- [ ] Error states handled gracefully

---

### Phase B7: Anonymous localStorage (V1 Simple)

**Goal**: Anonymous users get localStorage-based preferences. No merge with server.

#### B7.1: Create Settings Service with Auth Branching
- **File**: `Explore.Blazor.Client/Services/UserSettingsService.cs`
- **Behavior**:
  - Authenticated: Calls BFF-proxied settings API
  - Anonymous: Reads/writes localStorage via JS interop
- **SSR safety**: During SSR (no JS interop), return system defaults. Hydration picks up localStorage values.
- **Caching**: In-memory cache within scoped service lifetime; invalidate on update
- **Effort**: M

#### B7.2: Register Service
- **File**: `Explore.Blazor.Client/Program.cs`
- **Effort**: S

#### B7.3: BFF Proxy Route
- **Action**: Verify `/api/settings/**` routes through YARP proxy
- **Effort**: S

**Phase B7 Acceptance**:
- [ ] Authenticated users' preferences persist to server
- [ ] Anonymous users' preferences persist to localStorage
- [ ] No merge on login (server-authoritative)
- [ ] SSR renders with defaults (no hydration flash)

---

### Phase B8: Visual Regression Coverage

**Goal**: Comprehensive test coverage and visual regression verification.

#### B8.1: Blazor Component Tests
- **Project**: `Explore.Blazor.Client.Tests`
- **Tests**: EventCard renders all fields, EventCard hides fields based on settings, Drawer opens/closes, locked settings disabled, pagination component
- **Effort**: M

#### B8.2: End-to-End Visual Tests
- **Action**: Playwriter screenshots for:
  - Drawer open with all settings
  - Drawer with locked settings (lock icons visible)
  - Pagination mode in all 3 layouts
  - Infinite scroll mode
  - Card with fields hidden
  - Mobile responsive drawer
- **Effort**: M

#### B8.3: Regression Against B0 Baselines
- **Action**: Compare final screenshots with B0.1 baselines for areas NOT affected by the feature
- **Gate**: Non-feature areas must be pixel-identical
- **Effort**: S

**Phase B8 Acceptance**:
- [ ] All Blazor component tests pass
- [ ] Visual screenshots match expected behavior
- [ ] No regression in non-feature areas

---

## Observability Requirements

All observability items are **implementation requirements**, not aspirational.

### Structured Logging

| Event | Level | Fields |
|---|---|---|
| Setting resolved | `Debug` | Key, Source, IsLocked, TenantId, UserId |
| Setting updated | `Information` | Key, OldValue, NewValue, Scope, ActorId |
| Setting reset | `Information` | Key, Scope, ActorId |
| Lock applied | `Information` | Key, Scope, ActorId, TenantId |
| Lock rejected (already locked at higher scope) | `Warning` | Key, Scope, BlockingScope, ActorId |
| Batch update — key skipped (locked) | `Information` | Key, Reason, BatchId |
| Batch update — key applied | `Debug` | Key, Value, BatchId |
| Anonymous fallback (no auth, using localStorage) | `Debug` | — |
| Render-mode delay (SSR → interactive hydration) | `Debug` | DelayMs, RenderMode |
| Feature gate check (disabled tenant) | `Information` | TenantId, Feature |

### Metrics (Prometheus)

| Metric | Type | Labels |
|---|---|---|
| `settings_resolution_total` | Counter | `category`, `source`, `tenant_id` |
| `settings_update_total` | Counter | `category`, `scope`, `mode` (single/batch) |
| `settings_lock_total` | Counter | `scope`, `action` (lock/unlock) |
| `settings_batch_skip_total` | Counter | `category`, `reason` |

---

## Cache Invalidation Strategy

Deterministic invalidation paths for every mutation:

| Mutation | Invalidation |
|---|---|
| User updates preference | Invalidate user's cache: `InvalidateUserCache(userId)` |
| User resets preference | Invalidate user's cache: `InvalidateUserCache(userId)` |
| Tenant updates setting | Invalidate tenant cache: `InvalidateCache(tenantId)` |
| Tenant locks a key | Invalidate tenant cache + all user caches under tenant (or wait for TTL — 5 min max) |
| Tenant unlocks a key | Same as lock — tenant cache + user caches |
| Instance updates setting | Invalidate all: `InvalidateCache(null)` (global) |
| Instance locks a key | Invalidate all: `InvalidateCache(null)` (global) |

**Output cache**: Settings API responses use `UserData` output cache policy with short TTL (30s), vary by user + tenant. Lock/unlock mutations explicitly evict the output cache tag for the affected category.

**ETag**: Settings responses include weak ETag. Client can use `If-None-Match` for conditional requests.

---

## Rollout Strategy

### Feature Flag
- **Setting key**: `Features.EventListCustomization.Enabled` (boolean, system-level default `false`)
- **Scope**: Tenant-level — each tenant can independently enable/disable
- **Control points**:
  - API: Settings controller returns 404 for disabled tenants (event-list category)
  - Blazor: Gear icon hidden when feature disabled
  - No drawer, no customization API access when disabled

### Rollout Plan
1. **Internal testing**: Enable for internal/dev tenant only
2. **Beta**: Enable for select beta tenants
3. **GA**: Flip system-level default to `true`
4. **Override**: Individual tenants can still disable post-GA

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| EventCard extraction breaks existing layouts | High | High | Do extraction first (B1), compare against baselines (B0), zero-regression gate |
| Tenant lock cascade introduces resolution bugs | Medium | High | Extensive unit tests (A3.5), test all 5 scope levels, lock + unlock round-trips |
| SSR/hydration mismatch causes layout flash | Medium | Medium | Default to system settings during SSR, PersistentState for hydration, test all render modes |
| Virtualize ↔ Pagination switch state confusion | Medium | Medium | Reset to page 1 on mode switch, clear Virtualize cache, separate state paths |
| Generic handlers over-abstract simple operations | Low | Medium | Start with EventList as concrete validation; ensure handler tests use real category |
| Drawer z-index conflicts with detail drawer | Low | Low | Mutual exclusion (close one when other opens) |
| Large EventList refactoring risks merge conflicts | Medium | Medium | Complete EventCard extraction (B1) as separate PR before other UI work |
| Cache staleness after lock operations | Medium | Medium | Deterministic invalidation (see strategy); bounded staleness (5 min max TTL) |

---

## Success Metrics

1. **Platform Reusability**: Settings handlers work for `event-list` AND `appearance` categories without code changes
2. **Functional**: All 12 settings resolve correctly through 5-tier hierarchy with lock enforcement
3. **Lock Correctness**: Lock → stored values preserved → unlock → values restored (no data loss)
4. **Metadata**: Every setting response includes Value, Source, IsLocked, CanEdit, Reason
5. **UX**: Drawer opens <100ms, settings persist across sessions, locked settings clearly indicated
6. **SSR**: No layout flash, no blank page during prerender, defaults visible before hydration
7. **Performance**: Pagination page load <200ms, mode switch seamless, batch resolve ≤2 DB queries
8. **Quality**: Zero test regressions, all new code covered by tests
9. **Architecture**: Clean Architecture boundaries respected, no layer violations
10. **Observability**: Lock rejections, batch skips, and anonymous fallback visible in logs

---

## Effort Summary

### Track A: Settings Platform

| Phase | Description | Effort |
|---|---|---|
| A1 | Domain — Setting Definitions & IsLocked | S |
| A2 | Persistence — Migration & Config | S |
| A3 | Infrastructure — Tenant Lock Resolution | M |
| A4 | Application — Generic Handlers | M-L |
| A5 | API — Unified Settings Controller | L |
| A6 | NSwag Regeneration | S |
| A7 | Track A Integration Tests | M |
| **Track A Total** | | **~2 weeks** |

### Track B: EventList UI Refactor

| Phase | Description | Effort |
|---|---|---|
| B0 | Baseline Regression Guard | S |
| B1 | EventCard Extraction | L |
| B2 | Loading & State Separation | M |
| B3 | Paginated Rendering Mode | L |
| B4 | URL State Management | M |
| B5 | Customization Drawer | L |
| B6 | Autosave & Reset | M |
| B7 | Anonymous localStorage | M |
| B8 | Visual Regression Coverage | M |
| **Track B Total** | | **~3 weeks** |

### Overall

- **Track A + B sequential**: ~5 weeks solo developer
- **Parallelizable**: Track B0-B1 can start while Track A is in progress (EventCard extraction is independent). Track B2+ requires Track A API.
- **Critical path**: A1 → A2 → A3 → A4 → A5 → A6 → B5 → B6 → B7 → B8
