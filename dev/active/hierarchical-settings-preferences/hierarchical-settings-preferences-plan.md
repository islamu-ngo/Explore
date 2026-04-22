# Hierarchical Settings Preferences - Implementation Plan

Last Updated: 2026-04-21

## Executive Summary
The foundational work for hierarchical settings preferences (Domain, Persistence, and base CQRS handlers) is complete. The system now models theme catalogs as first-class `UiTheme` entities and uses the hierarchical settings engine for behavior flags (`appearance.theme_mode`, `appearance.default_theme_id`). 

The remaining work is entirely focused on **Integration and UI**: updating the Blazor Client's `AppearanceThemeService` to consume dynamic palettes from the backend, updating the API/BFF to sync with the database, and building the Admin/User preference screens.

## Current State Analysis

### What is Completed ✅
- **Phase 0 (ADR):** Completed. The decision to use first-class entities (`UiTheme`) for the catalog, rather than JSON settings, was adopted.
- **Phase 1 (Domain & Registry):** `UiTheme` aggregate, `UiThemePalette` value objects, and `AppearanceSettingDefinitions` (ThemeMode, Language, Direction, DefaultThemeId) are implemented.
- **Phase 3 (Persistence):** `UiThemeConfiguration`, `UiThemeRepository`, and EF Core migrations are fully applied.
- **Phase 2 (Application - Partial):** 
  - `AppearanceSettingGroup` correctly resolves the appearance keys.
  - `UiTheme` catalog CRUD handlers (`GetUiThemeCatalog`, `UpdateUiTheme`) are present.
  - `UpdateCurrentUserAppearancePreferencesCommandHandler` correctly persists `ThemeMode`, `Language`, and `Direction` as sparse overrides.
- **Phase 5 (UI - Partial):** `MainLayout.razor.cs` and `SetupLayout.razor.cs` have been refactored to delegate theme logic to `AppearanceThemeService`.

### What Still Needs Implementation (Gaps) ❌
- **Application Layer (User Preferences):** `UpdateCurrentUserAppearancePreferencesCommandHandler` must be extended to support saving the `DefaultThemeId` override.
- **Application Layer (Public Settings):** `GetPublicExperienceSettingsQueryHandler` and `PublicExperienceSettingsDto` need to be updated to expose the resolved anonymous-safe `DefaultThemeId` and `ThemeMode` for hydration, replacing the legacy `BrandLogoUrl` etc.
- **Blazor Client (Runtime Theme Composition):** `AppearanceThemeService` still uses hardcoded `LightPalette` and `DarkPalette`. It must be updated to dynamically fetch the selected `UiTheme` from the backend and map its tokens to `MudTheme`.
- **API/BFF (Transport):** The `/bff/theme` endpoint is still cookie-based. It must be updated to read/write from the database-backed settings engine for authenticated users.
- **Blazor Client (Admin UI):** `InstanceBrandingSection` and `TenantBrandingSection` still use the legacy branding model. They must be rebuilt to manage the `UiTheme` catalog (CRUD operations for themes).
- **Blazor Client (User Preferences UI):** A dedicated UI for authenticated users to select their active theme, mode, and event-card click behavior is still missing.

## Implementation Phases (Updated)

### Phase 1: Domain, Registry, And Theme Model Foundations [COMPLETE]
- `UiTheme` and `UiThemePalette` created.
- `AppearanceSettingDefinitions` registered.
- User-overridable settings explicitly marked.

### Phase 2: Application Layer Resolution And Commands [PARTIALLY COMPLETE]
- `AppearanceSettingGroup` resolving correctly.
- `UiTheme` catalog CQRS handlers implemented.
- **TODO:** Update `UpdateCurrentUserAppearancePreferencesCommandHandler` to handle `DefaultThemeId`.
- **TODO:** Update `GetPublicExperienceSettingsQueryHandler` and `PublicExperienceSettingsDto` to include the resolved appearance settings for anonymous users.

### Phase 3: Persistence, Caching, And Migration [COMPLETE]
- `UiThemeRepository` and EF Configurations implemented.
- EF migrations applied.

### Phase 4: API/BFF And Authorization Surface [TODO]
- **TODO:** Replace cookie-only `/bff/theme` persistence with database-backed preference endpoints. SSR bootstrap must respect the new authority order.

### Phase 5: Blazor UI And UX [TODO]
- **TODO:** Rebuild `AppearanceThemeService` to fetch the `UiTheme` and dynamically map its `LightPalette` and `DarkPalette` into the `MudTheme`.
- **TODO:** Extend `InstanceBrandingSection` and `TenantBrandingSection` to manage the `UiTheme` catalog with validated hex inputs and preview states.
- **TODO:** Build the authenticated User Preferences UI for theme selection and event-card behavior.

### Phase 6: Testing, Documentation, And Rollout [TODO]
- **TODO:** Unit tests for updated handlers.
- **TODO:** Integration tests for the new database-backed BFF flow.
