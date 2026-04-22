# Hierarchical Settings Preferences - Task Checklist

Last Updated: 2026-04-21

## Phase 0: ADR And Runtime Contract
- [x] Write the appearance architecture ADR before coding

## Phase 1: Domain, Registry, And Theme Model Foundations
- [x] Add appearance/theme keys to `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- [x] Create first-class theme entities and bounded palette value objects
- [x] Create `Explore.Domain/Settings/Definitions/AppearanceSettingDefinitions.cs`
- [x] Register appearance settings in `Explore.Domain/Settings/SettingRegistry.cs`
- [x] Promote selective existing keys to user scope

## Phase 2: Application Resolution And Validation
- [x] Add typed appearance setting group(s) under `Explore.Application/Settings/Groups/`
- [x] Add theme catalog validators with hex, uniqueness, and default-integrity checks
- [x] Add theme catalog CRUD CQRS flows with concurrency handling
- [x] Add admin CQRS flows for instance and tenant appearance defaults/locks
- [x] Add authenticated user preference CQRS flows for approved overrides (ThemeMode, Language, Direction)
- [ ] **UPDATE:** Add `DefaultThemeId` override support to `UpdateCurrentUserAppearancePreferencesCommandHandler`
- [ ] **UPDATE:** Update `GetPublicExperienceSettingsQueryHandler` and `PublicExperienceSettingsDto` to include resolved appearance settings

## Phase 3: Persistence And Transport
- [x] Persist the theme catalog as first-class relational data
- [x] Reuse `UserPreference` for sparse personal overrides
- [x] Add EF migration for theme entities, references, and concurrency support

## Phase 4: API/BFF And Authorization Surface
- [ ] Replace cookie-only theme persistence path with authenticated DB-backed endpoint flow
- [ ] Define cache keys and invalidation for tenant/user appearance runtime
- [ ] Formalize SSR bootstrap authority order before UI wiring

## Phase 5: Admin And User Interface
- [ ] Rework `Explore.Blazor.Client/Services/AppearanceThemeService.cs` to consume dynamic `UiTheme` data instead of hardcoded palettes
- [ ] Extend `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceBrandingSection.razor` to manage `UiTheme` defaults/locks
- [ ] Extend `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantBrandingSection.razor` to manage `UiTheme` catalog
- [ ] Add user preferences UI for theme and event-card behavior

## Phase 6: Tests And Documentation
- [ ] Add unit tests for updated handlers
- [ ] Add integration tests for save/read/reset flows and tenant isolation
- [ ] Add Blazor/client tests for user preference UX and event-card behavior
- [ ] Update docs and dev docs with final architecture decisions
