ABOUTME: Verified context for planning hierarchical settings, tenant theming, and selective user overrides in this repo.
ABOUTME: Captures confirmed files, architecture decisions, rejected approaches, and resume notes so implementation can start without rediscovery.

# Hierarchical Settings Preferences - Context

Last Updated: 2026-03-22

## SESSION PROGRESS (2026-03-22)

### Completed
- Verified the repo already has a five-tier settings system and persisted user overrides.
- Verified current runtime consumers for event-card click behavior and theme selection.
- Verified admin settings entry points for instance and tenant branding.
- Verified related repo patterns for typed governance (`PolicySlot<T>`), concurrency (`RowVersion`/`xmin`), and explicit cache invalidation.
- Researched external patterns for hierarchical settings, first-class theme entities, non-JSON theming, Blazor SSR bootstrap, and MudBlazor runtime theming.
- Updated the plan to reject JSON-based theme catalog storage and move to first-class theme entities plus reference-based settings.
- Captured reset-safe handoff notes, restart commands, and the exact next implementation slice before context reset.

### In Progress
- No code implementation is in progress yet; the task is still at the architecture-and-handoff stage.

### Blockers / Watch Items
- Current authenticated theme persistence is cookie-based; cross-device behavior needs a server-authoritative replacement.
- No verified appearance/theme setting definitions or typed appearance group exist yet.
- Need careful SSR bootstrap design to avoid theme flash for authenticated users.
- Need an ADR before coding starts so scope/lock/bootstrap rules do not drift mid-implementation.

## Current Implementation State

- Planning only; no feature code for hierarchical settings preferences has been implemented in this session.
- The working result of this session is documentation:
  - the plan now explicitly rejects JSON-based theme catalogs
  - the plan now requires first-class theme entities plus settings references
  - the plan now requires a dedicated theme composition/runtime service outside `MainLayout`
  - the plan now requires an ADR before code changes begin
- No migrations, entity classes, handlers, or UI components for this feature exist yet.

## Files Modified This Session And Why

- `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-plan.md`
  - Reworked the architecture to replace the JSON catalog concept with first-class theme entities and a dedicated runtime service boundary.
- `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-context.md`
  - Expanded verified evidence, rejected approaches, and reset-safe continuation guidance.
- `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-tasks.md`
  - Reordered the execution plan so ADR + storage/runtime-contract work happen before implementation.
- `dev/_journal/journal.md`
  - Added session-level insights and rejected approaches that would be expensive to rediscover.
- `dev/_journal/MAJOR_DECISIONS.md`
  - Recorded the major architectural decision to keep settings reference-based and move theme catalogs into first-class entities.
- `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-handoff-2026-03-22.md`
  - Added explicit restart instructions, current goal, and exact next editing target.

## Issues / Risks Discovered This Session

- The original JSON catalog direction would have turned theme definitions into an accidental mini-database hidden inside settings.
- `MainLayout.razor.cs` and `SetupLayout.razor.cs` currently duplicate palette-building logic, so leaving them as runtime decision points would spread theming rules across UI code.
- The current theme persistence path is cookie/bootstrap driven, which is incompatible with cross-device user preferences unless the database becomes authoritative.
- The git worktree is heavily dirty with many unrelated user changes; continuation must avoid treating the worktree as feature-isolated.

## Next Immediate Steps

1. Create the ADR for appearance architecture, storage boundary, lock semantics, and SSR/bootstrap authority order.
2. Define the first-class theme entity model and bounded palette token/value-object shape.
3. Define `AppearanceSettingDefinitions` for references/behavior only (`appearance.default_theme_id`, `appearance.theme_mode`, and promoted user-overridable behavior keys).
4. Design the dedicated runtime service boundary (`IThemeCompositionService` and/or `IAppearanceRuntimeService`) before touching `MainLayout` or admin UI.

## Commands To Run On Restart

```bash
git status --short
```

```bash
dotnet build --configuration Release --verbosity quiet
```

## Uncommitted Changes Needing Attention

- The repository already contains many unrelated modified and untracked files outside this task.
- Do not revert or clean unrelated work while continuing this track.
- The new task folder `dev/active/hierarchical-settings-preferences/` is untracked in the current worktree and should be treated as this session's planning output.

## Exact Restart Point

- No production code file is partially edited for this feature.
- The next intentional file to create is the ADR file for this task at line 1, likely under `dev/active/hierarchical-settings-preferences/`.
- The next implementation-facing design target after the ADR is the new theme entity model in `Explore.Domain/`.

## Key Verified Files

### Core Settings Engine

- `Explore.Application/Settings/SettingContext.cs`
  - Immutable scope context record.
  - Current chain: instance -> tenant -> organization -> group -> user.

- `Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs`
  - Existing core contract for batch resolution, metadata resolution, override writes, and locking.

- `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`
  - Applies system, tenant, organization, group, and user precedence.
  - Applies user preferences only when `SettingDefinition.MaxScope >= SettingScope.User`.
  - Currently treats instance lock as the effective hard stop.
  - `LockAsync` is implemented only for instance scope today.

- `Explore.Domain/Settings/SettingDefinition.cs`
  - Existing metadata model for `Key`, `ValueType`, `DefaultValue`, `Category`, `MinScope`, `MaxScope`, `IsLockable`, `AllowedValues`.

- `Explore.Domain/Settings/SettingRegistry.cs`
  - Registry of code-defined setting definitions.

### Related Governance Patterns

- `Explore.Domain/Policies/PolicySlot.cs`
  - Existing typed override/deny model for bounded governance fields.

- `Explore.Persistence/Services/PolicyResolver.cs`
  - Existing deterministic policy precedence service returning value, source scope, and lock provenance.

- `Explore.Domain/Policies/InstancePolicySet.cs`
- `Explore.Domain/Policies/TenantPolicySet.cs`
  - Existing audited, row-versioned governance aggregates.

- `Explore.Persistence/Configurations/Entities/InstancePolicySetConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/TenantPolicySetConfiguration.cs`
  - Existing typed-policy persistence pattern. Useful as a cautionary reference for bounded structures, but not the chosen storage model for theme catalogs.

- `Explore.Persistence/Configurations/Entities/AppSettingConfiguration.cs`
  - Existing optimistic concurrency pattern that should inform theme admin editing.

- `Explore.Application/Notifications/PolicyChangedCacheInvalidationHandler.cs`
  - Existing explicit cache-key/invalidation direction for scope-aware governance changes.

### Relevant Existing Keys / Groups

- `Explore.Domain/Constants/GovernanceSettingKeys.cs`
  - Contains `GovernanceSettingKeys.Branding.*`.
  - Contains `GovernanceSettingKeys.Events.CardClickOpensDetailPage`.

- `Explore.Domain/Settings/Definitions/BrandingSettingDefinitions.cs`
  - Current branding definitions are tenant-overridable but asset-oriented only.

- `Explore.Domain/Settings/Definitions/EventSettingDefinitions.cs`
  - Existing event behavior setting includes `events.card_click_opens_detail_page`.

- `Explore.Application/Settings/Groups/BrandingSettingGroup.cs`
  - Typed branding group used for runtime resolution.

- `Explore.Application/Settings/Groups/EventSettingGroup.cs`
  - Typed event group already surfaces `CardClickOpensDetailPage`.

### Persistence

- `Explore.Domain/UserPreference.cs`
  - Existing persisted sparse user override entity.
  - Tenant-scoped and keyed by `SettingKey`.

- `Explore.Persistence/Configurations/Entities/UserPreferenceConfiguration.cs`
  - Unique index: `(TenantId, UserId, SettingKey)`.

- `Explore.Persistence/Repositories/UserPreferenceRepository.cs`
  - Existing read/remove operations for user preference overrides.

### Runtime Consumers

- `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`
  - Builds `PublicExperienceSettingsDto` for the public/anonymous runtime.

- `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs`
  - Already exposes `EventCardClickOpensDetailPage` to the client.

- `Explore.Blazor.Client/Services/PublicExperienceService.cs`
  - Loads/caches public experience settings in the client.

- `Explore.Blazor.Client/Pages/Events/EventList.razor.cs`
  - Reads resolved public setting and toggles page-vs-drawer click behavior.

### Current Theme Implementation

- `Explore.Blazor.Client/Layout/MainLayout.razor.cs`
  - Hardcoded `PaletteLight`, `PaletteDark`, typography, and layout properties.
  - Uses `MudThemeProvider.GetSystemDarkModeAsync()`.
  - Persists `light`/`dark` preference via `/bff/theme`.

- `Explore.Blazor.Client/Layout/SetupLayout.razor.cs`
  - Duplicates theme construction and cookie persistence patterns.

- `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs`
  - Current theme endpoint writes cookie only.

- `Explore.Blazor.Client/wwwroot/js/theme.js`
  - Cookie/local-storage helper still exists.

### Admin UI Extension Points

- `Explore.Blazor.Client/Pages/Admin/Instance/InstanceSettings.razor`
- `Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor`
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceBrandingSection.razor`
- `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantBrandingSection.razor`

These are the best verified entry points for theme/branding admin UX.

## Confirmed Missing Pieces

Searches found no verified:

- `ThemeSettingDefinitions`
- `ThemeSettingGroup`
- `AppearanceSettingGroup`
- first-class theme catalog entities
- dedicated theme composition/runtime service
- DB-backed user preferences UI for theme selection
- theme catalog/admin color editor UI

These should be planned as new work, not assumed existing infrastructure.

## Core Decisions For Implementation

1. Use the existing settings engine for tenant defaults and approved user overrides.
2. Do not create a second long-term theme preference system beside `UserPreference`.
3. Promote only curated settings to `SettingScope.User`.
4. Treat tenant-managed theme catalogs as first-class admin-owned entities, not generic-setting JSON.
5. Keep cookies as optional SSR/bootstrap hints only if needed after database-backed preference loading exists.
6. Keep `MainLayout` and `SetupLayout` thin by introducing a dedicated theme composition/runtime service.
7. For MVP, use `MaxScope` plus instance locks as the policy model; do not add tenant-level suppression flags unless product explicitly requires them.

## Essential Interface Signatures

### Existing Resolver Contract

```csharp
Task<T?> ResolveAsync<T>(string key, SettingContext context, CancellationToken ct = default);
Task<ResolvedSetting?> ResolveWithMetadataAsync(string key, SettingContext context, CancellationToken ct = default);
Task<IReadOnlyList<ResolvedSetting>> ResolveBatchAsync(IEnumerable<string> keys, SettingContext context, CancellationToken ct = default);
Task<TGroup> ResolveGroupAsync<TGroup>(SettingContext context, CancellationToken ct = default) where TGroup : ISettingGroup, new();
Task SetValueAsync(string key, string value, SettingScope scope, Guid scopeId, Guid actorId, CancellationToken ct = default);
Task RemoveOverrideAsync(string key, SettingScope scope, Guid scopeId, Guid actorId, CancellationToken ct = default);
Task LockAsync(string key, SettingScope scope, Guid scopeId, Guid actorId, CancellationToken ct = default);
```

### Existing User Preference Persistence Shape

```csharp
public class UserPreference
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public required string SettingKey { get; set; }
    public required string Value { get; set; }
}
```

### Recommended New Setting Concepts

```csharp
appearance.default_theme_id      // inherited default, user-overridable
appearance.theme_mode            // system|light|dark, user-overridable
events.card_click_opens_detail_page // promote existing key to SettingScope.User
```

## Rejected Approach

- Do not store the tenant theme catalog as JSON in a generic setting such as `appearance.available_themes`.
- Do not let `MainLayout.razor.cs` remain the place where theme precedence and palette mapping logic live.
- Do not overload `PublicExperienceSettingsDto` with authenticated user preference state.

## External Research Notes

- Hierarchical config should stay sparse: only store overrides, inherit everything else.
- Lockable config works best when the same key is resolved through one precedence chain and locked at the parent.
- MudBlazor supports runtime `MudTheme` construction via `MudThemeProvider`, so dynamic palette composition is feasible without abandoning the current layout architecture.
- Cross-device preference persistence requires server-side storage as source of truth; browser storage alone is insufficient.
- MudBlazor docs support runtime `MudTheme` composition, which fits a dedicated `IThemeCompositionService` boundary.
- Enterprise theming research strongly favors first-class entities with optimistic concurrency and FK-based user selections over JSON blobs in settings tables.

## Quick Resume

1. Read `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-plan.md`.
2. Read `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-handoff-2026-03-22.md`.
3. Start with the ADR for storage, lock semantics, and SSR bootstrap authority order.
4. Then define theme entities plus appearance setting references.
5. Reuse current admin settings surfaces rather than inventing a separate settings UI shell.
