ABOUTME: Strategic plan for extending the hierarchical settings engine with first-class theme entities and selective user overrides.
ABOUTME: Rejects JSON-based theme catalogs and defines the runtime, storage, and governance boundaries for implementation.

# Hierarchical Settings Preferences - Implementation Plan

Last Updated: 2026-03-22

## Executive Summary

This feature should extend the existing five-tier settings system instead of creating a second preference subsystem. The repo already has the right primitives for scope-aware resolution, persisted user overrides, and governance semantics: `Explore.Application/Settings/SettingContext.cs`, `Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs`, `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`, `Explore.Domain/UserPreference.cs`, and the typed governance model built around `Explore.Domain/Policies/PolicySlot.cs`.

The revised architectural direction is:

1. Keep instance -> tenant -> organization -> group -> user precedence in the existing settings engine.
2. Store theme catalogs as first-class relational entities, not JSON in generic setting rows.
3. Use the settings engine only for references, defaults, mode, and selective user-overridable behavior.
4. Introduce a dedicated theme composition/runtime service so `MainLayout` and `SetupLayout` remain consumers, not policy engines.
5. Keep `UserPreference` as the sparse cross-device store for approved user overrides.
6. Define SSR/bootstrap authority order in an ADR before UI coding starts.

The user-visible outcome is:

- Instance admins can define platform defaults and lock tenant customizations.
- Tenant admins can define tenant-owned themes, enter validated hex colors, and publish multiple selectable themes.
- Users can override only approved settings, such as event-card click behavior and active theme choice, and those choices persist across devices because they are stored in the database.

## Current State Analysis

### Verified Existing Architecture

- `Explore.Application/Settings/SettingContext.cs` defines a five-tier scope chain: instance, tenant, organization, group, user.
- `Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs` already exposes `ResolveAsync`, `ResolveWithMetadataAsync`, `ResolveBatchAsync`, `ResolveGroupAsync`, `SetValueAsync`, `RemoveOverrideAsync`, and `LockAsync`.
- `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs` already resolves through `SystemSetting`, `TenantSetting`, `OrganizationSetting`, `GroupSetting`, and `UserPreference`, and only applies user preferences when `SettingDefinition.MaxScope >= SettingScope.User`.
- The current implementation only supports `LockAsync` at instance scope.
- `Explore.Domain/Settings/SettingDefinition.cs` and `Explore.Domain/Settings/SettingRegistry.cs` already provide code-defined setting metadata, allowed scope ranges, lockability, categories, and allowed values.

### Verified Related Governance Patterns The Plan Should Reuse

- `Explore.Domain/Policies/PolicySlot.cs` already models bounded override semantics with `ChildOverrideMode.Allow` and `ChildOverrideMode.Deny`.
- `Explore.Persistence/Services/PolicyResolver.cs` already resolves typed governance values and provenance through instance -> tenant -> organization precedence.
- `Explore.Domain/Policies/InstancePolicySet.cs` and `Explore.Domain/Policies/TenantPolicySet.cs` already show how the repo models bounded governance aggregates with auditing and optimistic concurrency.
- `Explore.Persistence/Configurations/Entities/InstancePolicySetConfiguration.cs` and `Explore.Persistence/Configurations/Entities/TenantPolicySetConfiguration.cs` show that the repo already uses typed owned-policy structures for small bounded governance sets.
- `Explore.Persistence/Configurations/Entities/AppSettingConfiguration.cs` and `Explore.Application/Contracts/Persistence/IAppSettingRepository.cs` show an existing optimistic-concurrency pattern that should be mirrored for admin-edited theme data.
- `Explore.Application/Notifications/PolicyChangedCacheInvalidationHandler.cs` shows an existing explicit cache-invalidation pattern the new appearance runtime should follow.

### Verified Existing Settings Relevant To This Feature

- `Explore.Domain/Constants/GovernanceSettingKeys.cs` already contains branding keys and `events.card_click_opens_detail_page`.
- `Explore.Domain/Settings/Definitions/BrandingSettingDefinitions.cs` currently supports display name, logo URL, favicon URL, and custom CSS URL, all capped at `SettingScope.Tenant`.
- `Explore.Domain/Settings/Definitions/EventSettingDefinitions.cs` already defines `events.card_click_opens_detail_page`, but it is currently capped at `SettingScope.Tenant`.
- `Explore.Application/Settings/Groups/BrandingSettingGroup.cs` and `Explore.Application/Settings/Groups/EventSettingGroup.cs` provide typed setting groups consumed by runtime code.

### Verified Persistence And Runtime Consumption

- `Explore.Domain/UserPreference.cs` already persists tenant-scoped per-user overrides keyed by `SettingKey`.
- `Explore.Persistence/Configurations/Entities/UserPreferenceConfiguration.cs` enforces a unique `(TenantId, UserId, SettingKey)` constraint.
- `Explore.Persistence/Repositories/UserPreferenceRepository.cs` already supports get/list/remove for user-scoped overrides.
- `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs` feeds runtime public experience settings into `PublicExperienceSettingsDto`.
- `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs` already transports `EventCardClickOpensDetailPage` to the client.
- `Explore.Blazor.Client/Services/PublicExperienceService.cs` caches those public settings client-side.
- `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` already switches behavior based on `EventCardClickOpensDetailPage`.

### Verified Current Theme Implementation

- `Explore.Blazor.Client/Layout/MainLayout.razor.cs` hardcodes one `PaletteLight`, one `PaletteDark`, typography, and layout properties into a single `MudTheme`.
- `Explore.Blazor.Client/Layout/SetupLayout.razor.cs` duplicates the same palette-building pattern.
- `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs` persists only `light` and `dark` via `/bff/theme` cookie endpoints.
- `Explore.Blazor.Client/wwwroot/js/theme.js` still reads/writes theme state through cookie/local storage helpers.
- The current runtime detects system dark mode through `MudThemeProvider.GetSystemDarkModeAsync()` and defaults to light when no cookie is present.

### Verified Admin UI Extension Points

- `Explore.Blazor.Client/Pages/Admin/Instance/InstanceSettings.razor` exists.
- `Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor` exists and hosts the tenant settings layout.
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceBrandingSection.razor` already supports instance branding fields plus tenant lock toggles.
- `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantBrandingSection.razor` already supports tenant branding fields with lock-aware disabled states.

### Confirmed Gaps

- No verified `ThemeSettingDefinitions`, `ThemeSettingGroup`, `AppearanceSettingGroup`, or first-class theme catalog model exists today.
- No verified DB-backed end-user preference UI exists for theme selection or event-card click behavior.
- The current theme persistence path is browser-based, not cross-device.
- The current branding model supports logo/CSS URLs but not a relational multi-theme catalog or validated hex palette input.
- No dedicated theme composition service exists; both layouts currently hardcode palettes and build `MudTheme` locally.

## Proposed Future State

### Architectural Direction

Use one settings engine for admin defaults and approved personal overrides, but do not store the theme catalog itself in settings.

- Admin defaults remain registry-driven settings resolved through `IHierarchicalSettingsResolver`.
- User overrides remain `UserPreference` rows, but only for keys whose `SettingDefinition.MaxScope` is explicitly raised to `SettingScope.User`.
- Theme catalog data lives in first-class entities with auditing, active/default state, and optimistic concurrency.
- The settings engine stores only references and behavior flags, such as the effective default theme reference, theme mode, and event-card behavior.
- `MainLayout` and `SetupLayout` consume a runtime service such as `IThemeCompositionService` or `IMudThemeFactory`; they do not own precedence, fallback, or palette mapping rules.

### Policy Semantics

The policy seam should be explicit before implementation starts.

- `SettingDefinition.MaxScope` is the technical gate for whether user overrides are possible.
- Instance lock remains the only hard lock for MVP.
- This feature should not introduce tenant-level booleans such as `appearance.allow_user_theme_override` unless product requirements explicitly demand tenant suppression of user personalization.
- For MVP, the intended truth is: if a key is capped at `SettingScope.User` and not instance-locked, the user may override it.

### Precedence Model

Effective value resolution remains:

1. Instance default or lock
2. Tenant override
3. Organization override
4. Group override
5. User override

With these rules:

- Instance locks continue to block child overrides.
- Only a curated subset of keys may resolve at user scope.
- Tenant theme catalog data is shared downward; users may choose from allowed themes but not mutate the catalog.
- User preference rows are sparse; a row exists only when the user deviates from inherited defaults.
- For this feature, do not expand locking beyond instance scope unless a concrete tenant-to-user lock requirement emerges during implementation.

### Recommended Setting Model

Add a new appearance/theme category, but use settings only for references and behavior flags. Do not store the theme catalog inside settings.

Recommended keys to introduce:

- `appearance.default_theme_id` - effective selected theme reference inherited by users; max scope user.
- `appearance.theme_mode` - `system|light|dark`; max scope user.

Recommended existing key change:

- Promote `events.card_click_opens_detail_page` from tenant-only to user-overridable by changing its `MaxScope` to `SettingScope.User`.

### Theme Catalog Model

Preferred MVP model: a first-class `UiTheme` aggregate with optimistic concurrency and a bounded palette model.

- `UiTheme`
  - `Id`
  - ownership strategy decided in ADR (`TenantId` nullable or separate platform/tenant tables)
  - stable `ThemeKey`
  - `DisplayName`
  - `IsActive`
  - `IsDefault`
  - `SortOrder`
  - audit fields
  - `RowVersion`
- `UiThemePaletteLight` and `UiThemePaletteDark` as bounded owned value objects mapped to explicit columns, not `ToJson(...)`
- a fixed token vocabulary only, based on the MudBlazor palette tokens the app actually uses

This lets the UI expose multiple named themes while the runtime still builds a single `MudTheme` from the effective selected theme.

### Why The Plan Rejects JSON For Theme Catalogs

The repo already uses typed JSON columns for small bounded policy sets such as `InstancePolicySet` and `TenantPolicySet`, but a tenant-managed theme catalog has different lifecycle pressure:

- more edits
- stronger concurrency needs
- admin UX around list management and defaults
- deletion/fallback semantics
- future filtering/reporting/support needs

Because of that, a theme catalog should not be stored as arbitrary JSON inside a generic setting row or policy blob. The relational model keeps validation, references, concurrency, and migrations tractable.

### Theme Composition Boundary

Introduce a dedicated service boundary before UI work starts.

- `IThemeCompositionService` or `IMudThemeFactory` maps bounded theme entities + mode into a `MudTheme`.
- `IAppearanceRuntimeService` coordinates effective theme lookup, provenance, and bootstrap state for anonymous/authenticated users.
- `MainLayout` and `SetupLayout` should only request effective runtime theme data and render it.

### DTO And Transport Boundary

Do not overload `PublicExperienceSettingsDto` with authenticated preference state.

- Keep a public/anonymous-safe runtime DTO for tenant-level appearance defaults and basic catalog projection needed for first paint.
- Add a dedicated authenticated `UserAppearancePreferencesDto` for user overrides, provenance, and reset behavior.
- Share resolution in the application layer rather than collapsing all transport needs into one DTO shape.

### Runtime Behavior

- Anonymous visitors receive tenant-resolved defaults through the public experience path.
- Authenticated users receive the same tenant defaults plus user-preference overlays for allowed keys.
- If `appearance.theme_mode = system`, the app uses the browser system mode but still applies the effective selected tenant/user theme palette for that mode.
- Cookies may remain only as SSR/bootstrap hints until database-backed preference hydration completes, but the database becomes the authoritative source.

### SSR Bootstrap Authority Order

Define this before UI implementation:

1. Anonymous request: tenant default theme reference + tenant runtime appearance projection
2. Authenticated SSR: server-known user preference wins over tenant default when available
3. Client hydration: browser-reported system preference is applied only when the effective mode is `system`
4. Cookie/bootstrap hint never overrides authoritative database state for authenticated users

### Cache Design

Add explicit cache-key planning up front.

- tenant public appearance/runtime cache: keyed by `tenantId`
- authenticated effective appearance cache: keyed by `tenantId:userId`
- theme catalog caches: keyed by owner scope plus theme/version identifiers
- invalidate on:
  - theme create/update/delete/disable
  - default theme change
  - user theme preference set/reset
  - user event-card behavior preference set/reset
  - instance lock/default changes affecting appearance keys

Prefer versioned or scope-specific cache keys following the direction already used in `Explore.Application/Notifications/PolicyChangedCacheInvalidationHandler.cs`.

### UX Principles

- Tenant admins manage branding defaults and theme catalog in tenant settings.
- Users get a separate personal preferences page/surface for allowed overrides only.
- Locked fields stay visible but disabled with clear provenance.
- Hex fields validate format and accessibility constraints before save.
- Theme previews should be visible before save to reduce support burden.
- Reset actions explicitly remove sparse user overrides and reveal the inherited effective value plus source.

## Implementation Phases

### Phase 0: ADR And Runtime Contract

#### Task 0.1: Write ADR for appearance architecture before coding
- **Files**: new ADR alongside this task set, following the pattern used by `dev/active/external-api-access/phase0-auth-tenant-request-flow-adr.md`
- **Acceptance Criteria**:
  - [ ] ADR states that theme catalogs are first-class entities, not generic-setting JSON.
  - [ ] ADR states lock semantics for MVP.
  - [ ] ADR states bootstrap authority order for anonymous, authenticated, and `system` mode flows.
  - [ ] ADR states exit criteria for future model changes if theme complexity grows.
- **Dependencies**: none
- **Effort**: S
- **Related Skills**: `clean-architecture-rules`

### Phase 1: Domain, Registry, And Theme Model Foundations

#### Task 1.1: Add appearance/theme governance keys and definitions
- **Files**: `Explore.Domain/Constants/GovernanceSettingKeys.cs`, new `Explore.Domain/Settings/Definitions/AppearanceSettingDefinitions.cs`, `Explore.Domain/Settings/SettingRegistry.cs`
- **Acceptance Criteria**:
  - [ ] Appearance/theme keys are defined in canonical dot-notation.
  - [ ] New definitions use `SettingDefinition` with correct category, allowed values, and scope limits.
  - [ ] `SettingRegistry` registers the new definition class.
  - [ ] No theme catalog payload is stored in a generic setting value.
- **Dependencies**: Task 0.1
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`

#### Task 1.2: Create first-class theme entities and bounded palette value objects
- **Files**: new domain entities/value objects in `Explore.Domain/`
- **Acceptance Criteria**:
  - [ ] A bounded `UiTheme` aggregate exists with auditing and concurrency support.
  - [ ] Palette/token value objects are mapped through explicit properties or owned types, not JSON columns.
  - [ ] The theme model supports active/inactive status, default selection, and deterministic ordering.
  - [ ] The ADR records whether ownership is modeled with nullable `TenantId` or separate platform/tenant theme tables.
- **Dependencies**: Task 0.1
- **Effort**: L
- **Related Skills**: `clean-architecture-rules`, `dotnet-efcore-guidelines`

#### Task 1.3: Mark selective user-overridable settings explicitly
- **Files**: `Explore.Domain/Settings/Definitions/EventSettingDefinitions.cs`, new `AppearanceSettingDefinitions.cs`
- **Acceptance Criteria**:
  - [ ] `events.card_click_opens_detail_page` is promoted to `SettingScope.User`.
  - [ ] Theme-selection keys that users may personalize are capped at `SettingScope.User`.
  - [ ] Theme catalog/admin keys remain tenant-only or broader.
  - [ ] The policy rule is explicit: no tenant suppression flag for MVP.
- **Dependencies**: Task 1.1
- **Effort**: S
- **Related Skills**: `clean-architecture-rules`

#### Task 1.4: Introduce typed theme contracts and validation rules
- **Files**: new domain/application contracts near existing settings definitions, entities, and groups
- **Acceptance Criteria**:
  - [ ] Theme contracts have stable IDs and bounded token structure.
  - [ ] Validation rules cover required fields, valid hex values, uniqueness of theme IDs/keys, valid default references, and disabled-theme restrictions.
  - [ ] Invalid theme data fails validation before persistence.
  - [ ] No free-form CSS is required for core theme configuration.
- **Dependencies**: Tasks 1.1 and 1.2
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`, `blazor-ui-conventions`

### Phase 2: Application Layer Resolution And Commands

#### Task 2.1: Add typed appearance setting group(s) for references and behavior only
- **Files**: new `Explore.Application/Settings/Groups/AppearanceSettingGroup.cs` or equivalent, existing public/tenant DTOs as needed
- **Acceptance Criteria**:
  - [ ] A typed group resolves theme references, default theme ID, and theme mode from hierarchical settings.
  - [ ] Population logic mirrors existing `BrandingSettingGroup` and `EventSettingGroup` patterns.
  - [ ] Group defaults match definition defaults, not ad hoc UI defaults.
- **Dependencies**: Phase 1 complete
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`

#### Task 2.2: Add theme catalog repository and CQRS flows
- **Files**: new repository interfaces plus feature slices for theme catalog CRUD
- **Acceptance Criteria**:
  - [ ] Admins can create, update, activate/deactivate, and select defaults for themes through MediatR.
  - [ ] Theme edits use optimistic concurrency tokens.
  - [ ] Handlers operate on entities, not raw JSON payload strings.
  - [ ] Deleting/disabling a theme has explicit fallback behavior for affected users.
- **Dependencies**: Tasks 1.2 and 1.4
- **Effort**: L
- **Related Skills**: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`

#### Task 2.3: Add admin command/query flows for instance and tenant appearance defaults/locks
- **Files**: existing admin/onboarding feature slices plus new CQRS request/handler files
- **Acceptance Criteria**:
  - [ ] Tenant admin flows can load/save appearance defaults through MediatR.
  - [ ] Instance admin flows can load/save defaults and locks through MediatR.
  - [ ] Validators are manually instantiated per repo convention.
  - [ ] Commands return `BaseCommandResponse<Guid>` where applicable.
- **Dependencies**: Tasks 2.1 and 2.2
- **Effort**: L
- **Related Skills**: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

#### Task 2.4: Add authenticated user preference commands for allowed overrides
- **Files**: new user-preferences feature slice or extension of existing profile/preferences slice if one exists
- **Acceptance Criteria**:
  - [ ] Authenticated users can get and set only allowed override keys.
  - [ ] Removing an override deletes the sparse `UserPreference` row and falls back to inherited value.
  - [ ] Attempts to override locked or non-user-scoped settings are rejected with clear errors.
  - [ ] Cross-device persistence is driven by database state, not browser-only state.
- **Dependencies**: Tasks 1.3 and 2.1
- **Effort**: L
- **Related Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`

#### Task 2.5: Add runtime appearance services outside the layout layer
- **Files**: new application/client services plus related interfaces
- **Acceptance Criteria**:
  - [ ] `IThemeCompositionService` or `IMudThemeFactory` maps effective theme entities into `MudTheme`.
  - [ ] `IAppearanceRuntimeService` resolves anonymous vs authenticated runtime appearance state and provenance.
  - [ ] Layouts consume these services instead of containing resolution/business rules.
- **Dependencies**: Tasks 2.1, 2.2, and 2.4
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`, `blazor-ui-conventions`

#### Task 2.6: Split public and authenticated appearance transport cleanly
- **Files**: `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`, related DTOs/services
- **Acceptance Criteria**:
  - [ ] Public settings payload includes only anonymous-safe tenant appearance defaults and minimal catalog projection needed for first render.
  - [ ] Authenticated runtime path uses a dedicated user appearance/preferences DTO.
  - [ ] Shared resolution lives below the transport layer.
  - [ ] Event-card click behavior uses the same resolved setting path for tenant default and user override.
- **Dependencies**: Tasks 2.1, 2.4, and 2.5
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`

### Phase 3: Persistence, Caching, And Migration

#### Task 3.1: Persist theme catalog as first-class relational data
- **Files**: new repositories/configurations in `Explore.Persistence/`
- **Acceptance Criteria**:
  - [ ] The theme catalog is not stored in generic settings or `ToJson(...)` policy blobs.
  - [ ] Palette values are mapped through explicit columns / owned types with a fixed token vocabulary.
  - [ ] Theme rows include optimistic concurrency support.
  - [ ] Existing repository patterns remain entity-based.
- **Dependencies**: Phase 2 contracts finalized
- **Effort**: L
- **Related Skills**: `dotnet-efcore-guidelines`, `clean-architecture-rules`

#### Task 3.2: Reuse `UserPreference` for sparse personal overrides and extend repositories only where needed
- **Files**: existing repositories/configurations in `Explore.Persistence/`
- **Acceptance Criteria**:
  - [ ] No new long-term per-user preference store is introduced if `UserPreference` remains sufficient.
  - [ ] Existing repository patterns remain entity-based.
  - [ ] Reference-based user overrides are consistent and testable.
  - [ ] Caching invalidation in `HierarchicalSettingsResolver` still works for tenant and user changes.
- **Dependencies**: Phase 2 contracts finalized
- **Effort**: M
- **Related Skills**: `dotnet-efcore-guidelines`, `clean-architecture-rules`

#### Task 3.3: Add EF migration for theme entities, references, and concurrency support
- **Files**: `Explore.Persistence/Migrations/*`, possibly snapshot updates
- **Acceptance Criteria**:
  - [ ] Migration creates theme entities/owned columns with concurrency tokens.
  - [ ] Existing `UserPreference` uniqueness and tenant isolation remain intact.
  - [ ] Seed/bootstrap impact is documented for tenants with no appearance settings yet.
- **Dependencies**: Tasks 3.1 and 3.2
- **Effort**: M
- **Related Skills**: `dotnet-efcore-guidelines`

#### Task 3.4: Define explicit cache keys and invalidation flows
- **Files**: settings/runtime services and notifications
- **Acceptance Criteria**:
  - [ ] Tenant public appearance cache keys are explicit.
  - [ ] Authenticated effective appearance cache keys are explicit.
  - [ ] Theme default/theme catalog/user override mutations invalidate the correct scopes.
  - [ ] The cache plan is documented in code comments and dev docs.
- **Dependencies**: Tasks 2.5 and 3.1
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`

### Phase 4: API/BFF And Authorization Surface

#### Task 4.1: Replace cookie-only theme persistence with database-backed preference endpoints
- **Files**: `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs` or new API/BFF endpoints, related application handlers
- **Acceptance Criteria**:
  - [ ] Theme preference updates flow through authenticated server endpoints backed by the settings engine.
  - [ ] SSR bootstrap remains supported for first render.
  - [ ] Anonymous flows still use tenant defaults safely.
  - [ ] API/BFF mutations honor antiforgery/authentication patterns already used by the Blazor host.
  - [ ] Errors return ProblemDetails-aligned responses consistent with existing exception handling.
- **Dependencies**: Task 2.4
- **Effort**: M
- **Related Skills**: `blazor-bff-patterns`, `auth-patterns`

#### Task 4.2: Enforce admin vs user authorization boundaries
- **Files**: relevant controllers/endpoints/handlers and Cerbos policies if new resources/actions are introduced
- **Acceptance Criteria**:
  - [ ] Tenant appearance catalog updates require tenant admin authorization.
  - [ ] User preference mutations require authenticated self access only.
  - [ ] Any new Cerbos action/resource is documented and tested if existing policies do not already cover it.
  - [ ] GET/read endpoints remain aligned with repo auth rules.
  - [ ] If controller-based API endpoints are added instead of BFF-only endpoints, they preserve repo conventions for HAL/read behavior and applicable rate limiting.
- **Dependencies**: Task 4.1
- **Effort**: M
- **Related Skills**: `auth-patterns`, `clean-architecture-rules`

#### Task 4.3: Formalize SSR bootstrap contract before UI wiring
- **Files**: relevant API/BFF handlers, runtime services, and ADR updates
- **Acceptance Criteria**:
  - [ ] Bootstrap authority order is implemented exactly as documented.
  - [ ] Anonymous, authenticated, and `system` mode paths are testable.
  - [ ] Cookie/bootstrap hints are clearly demoted to non-authoritative status.
- **Dependencies**: Tasks 2.5, 2.6, and 4.1
- **Effort**: M
- **Related Skills**: `blazor-bff-patterns`, `auth-patterns`

### Phase 5: Blazor UI And UX

#### Task 5.1: Extend instance and tenant admin settings UI for theme management
- **Files**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceBrandingSection.razor`, `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantBrandingSection.razor`, related models/services/components
- **Acceptance Criteria**:
  - [ ] Instance admins can define platform defaults and lock theme-related tenant overrides.
  - [ ] Tenant admins can create/edit multiple themes with labeled palette fields and hex validation.
  - [ ] The admin UI clearly separates branding assets from structured theme configuration.
  - [ ] Theme preview and lock state are visible in the form.
- **Dependencies**: Phases 2 and 4 ready
- **Effort**: XL
- **Related Skills**: `blazor-ui-conventions`, `blazor-css-isolation`

#### Task 5.2: Add authenticated user preferences UI
- **Files**: new user settings/preferences page/components in `Explore.Blazor.Client/Pages/` plus related services
- **Acceptance Criteria**:
  - [ ] Users can choose theme mode and active theme from tenant-available choices.
  - [ ] Users can choose whether event-card clicks open the detail page or right-side drawer.
  - [ ] A clear reset action removes the user override and reverts to inherited tenant behavior.
  - [ ] Preferences persist across sessions and devices after re-authentication.
- **Dependencies**: Tasks 2.4 and 4.1
- **Effort**: L
- **Related Skills**: `blazor-ui-conventions`, `blazor-css-isolation`

#### Task 5.3: Rework runtime theme consumption in `MainLayout` and `SetupLayout`
- **Files**: `Explore.Blazor.Client/Layout/MainLayout.razor.cs`, `Explore.Blazor.Client/Layout/SetupLayout.razor.cs`, related services/models
- **Acceptance Criteria**:
  - [ ] Layouts request the effective runtime theme from the dedicated composition/runtime service.
  - [ ] System light/dark preference still works when user mode is `system`.
  - [ ] Anonymous first paint uses tenant defaults without major flicker.
  - [ ] Browser storage is no longer the source of truth for authenticated users.
- **Dependencies**: Tasks 2.5, 2.6, 4.3, and 5.2
- **Effort**: L
- **Related Skills**: `blazor-ui-conventions`, `blazor-css-isolation`

### Phase 6: Testing, Documentation, And Rollout

#### Task 6.1: Add unit tests for settings resolution and validation
- **Files**: `Event.Application.UnitTests/`, `Event.Domain.UnitTests/`
- **Acceptance Criteria**:
  - [ ] Resolver tests cover tenant defaults, user overrides, and instance locks.
  - [ ] Validation tests cover malformed hex values, duplicate theme IDs, invalid default references, disabled-theme restrictions, and illegal user overrides.
  - [ ] Event-card click behavior tests cover tenant fallback and user override.
- **Dependencies**: Phases 1-5 functional
- **Effort**: M
- **Related Skills**: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`

#### Task 6.2: Add integration tests for persistence and runtime behavior
- **Files**: `Event.API.IntegrationTests/`, `Event.Persistence.IntegrationTests/`, `Explore.Blazor.Client.Tests/`
- **Acceptance Criteria**:
  - [ ] API/BFF tests verify preference save/read/reset flows.
  - [ ] Persistence tests confirm sparse `UserPreference` storage and tenant isolation.
  - [ ] Concurrency tests cover stale admin edits to themes/defaults.
  - [ ] Fallback tests cover removed/disabled themes and stale selected theme references.
  - [ ] Bootstrap tests cover anonymous defaults, authenticated overrides, and `system` mode hydration.
  - [ ] Blazor/client tests verify theme selection and event-card behavior rendering expectations.
- **Dependencies**: Task 6.1
- **Effort**: L
- **Related Skills**: `auth-patterns`, `blazor-ui-conventions`

#### Task 6.3: Update operational and developer documentation
- **Files**: relevant docs under `docs/` plus dev docs in `dev/active/`
- **Acceptance Criteria**:
  - [ ] New settings categories and precedence semantics are documented.
  - [ ] Rollout/rollback notes cover migration and bootstrap behavior.
  - [ ] Any support runbooks for theme validation or reset behavior are updated.
- **Dependencies**: Phase 6 tests stable
- **Effort**: S/M
- **Related Skills**: `clean-architecture-rules`

## Detailed Implementation Notes

### Clean Architecture Layering

- Domain owns theme entities, palette value objects, setting keys, definitions, and value constraints.
- Application owns typed setting groups, validators, commands, queries, DTOs, orchestration, and runtime composition contracts.
- Persistence owns repositories, EF configurations, migrations, and cache invalidation hooks.
- API/BFF owns authenticated transport and SSR/bootstrap orchestration.
- Blazor owns rendering, previews, and user/admin forms.

### Preferred Data Strategy

- Reuse `UserPreference` for sparse personal overrides.
- Store theme catalog data in first-class entities, not in generic settings.
- Store only references/defaults/behavior flags in the hierarchical settings engine.
- Avoid a separate theme-preference entity unless performance or query shape proves `UserPreference` insufficient.

### Concurrency And Audit Expectations

- Theme catalog entities should use optimistic concurrency tokens following repo patterns such as `RowVersion` / `xmin`.
- Admin edits must preserve `CreatedAt`, `CreatedBy`, `UpdatedAt`, and `UpdatedBy`.
- Support logs and test cases should cover who changed the default theme, theme palette, and any lock/default behavior.

### Hex And Accessibility Validation

- Accept canonical `#RRGGBB` input only for MVP.
- Normalize casing before persistence.
- Validate required palette token completeness.
- Add contrast checks for core foreground/background combinations used by text, app bar, drawer, and surface colors.
- Validate that the configured default theme points to an active theme.
- Validate that disabled themes cannot be selected as defaults or user preferences.

### SSR / Blazor Considerations

- Tenant default theme data must be available on first render to avoid layout flash.
- User overrides require an authenticated bootstrap path that works with interactive SSR and hybrid Blazor.
- Keep cookie usage only as bootstrap/cache support if needed; do not let it drift from database truth.

## Risk Assessment And Mitigation

| Risk | Why It Matters | Mitigation |
|---|---|---|
| Theme catalog drifts into a pseudo-database hidden inside settings | Hard to validate, version, query, and migrate | Use first-class theme entities with bounded palette columns and concurrency tokens |
| A second preference subsystem emerges beside hierarchical settings | Causes drift between cookies, DB, and runtime behavior | Make `IHierarchicalSettingsResolver` + `UserPreference` the only source of truth |
| SSR first paint flickers between tenant default and user override | Poor UX and hard-to-reproduce bugs | Add authenticated bootstrap path and short-lived SSR hint only where necessary |
| Too many settings are made user-overridable | Governance model becomes inconsistent | Promote only curated keys to `SettingScope.User` and document them explicitly |
| Hex choices break accessibility | Tenant branding can make the UI unreadable | Add validation + preview + contrast checks before save |
| Admin forms become overloaded | Tenant admins struggle to manage theme catalog safely | Use dedicated theme sections/components with previews and constrained inputs |
| Event behavior preference forks implementation paths | Drawer/detail routing logic may drift | Keep one runtime flag and resolve it through the same settings engine everywhere |
| A removed theme leaves stale references behind | Users see broken or confusing defaults | Fallback to tenant default, clear invalid references, and test the migration path |

## Success Metrics

- Tenant admins can create at least two named themes and set a tenant default without editing CSS.
- Users can persist theme choice and event-card behavior across sign-in sessions and devices.
- Instance locks prevent tenant or user mutation of locked settings.
- Anonymous traffic renders tenant default branding/theme correctly on first load.
- Stale admin edits produce deterministic concurrency failures instead of silent overwrites.
- New tests cover precedence, locking, persistence, and UI behavior with no regression in existing settings flows.

## Required Resources And Dependencies

- Existing settings engine and repositories in `Explore.Application/`, `Explore.Infrastructure/`, and `Explore.Persistence/`
- MudBlazor runtime theme composition via `MudThemeProvider` and a new dedicated runtime service
- Admin settings surfaces in `Explore.Blazor.Client/Pages/Admin/Instance/` and `Explore.Blazor.Client/Pages/Admin/Tenant/`
- Authenticated preference transport through Blazor host / BFF patterns
- EF Core migration support if schema changes are introduced

## Effort Estimates

| Area | Effort |
|---|---|
| ADR + bootstrap contract | S/M |
| Domain/settings definitions + theme entities | L |
| Application commands/queries/resolution | L |
| Persistence/migration/caching updates | L |
| API/BFF preference flow | M |
| Tenant + user UI | XL |
| Test suite and docs | L |
| Overall | XL |

## Delivery Sequence Recommendation

1. ADR and bootstrap contract
2. Domain/settings definitions, theme entities, and validation
3. Application user/admin flows plus dedicated theme composition/runtime services
4. Persistence/cache updates
5. API/BFF transport for authenticated preferences
6. Tenant admin UI
7. User preferences UI
8. Runtime theme consumption in layouts
9. Tests and docs

## Potential Risks & Unknowns

The most likely complexity is still SSR theme hydration in the hybrid Blazor setup, but the second biggest risk is choosing the wrong persistence boundary for theme catalogs. If the team lets theme definitions live in generic settings or UI-local state, concurrency, fallback, and cache invalidation will become brittle quickly. The plan now assumes first-class theme entities plus reference-based settings; if implementation drifts from that, complexity will spike fast.
