# Hierarchical Settings Preferences - Context

Last Updated: 2026-04-21

## SESSION PROGRESS

### Completed
- Verified the repo already has a five-tier settings system and persisted user overrides.
- Defined the architecture (ADR) rejecting JSON-based theme catalogs in favor of first-class `UiTheme` entities.
- Implemented `UiTheme` domain entities, `UiThemePalette` value objects, and EF Core configurations/migrations.
- Created `AppearanceSettingDefinitions` (`appearance.default_theme_id`, `appearance.theme_mode`, etc.) and `AppearanceSettingGroup`.
- Implemented `UiTheme` catalog CQRS handlers and the initial `UpdateCurrentUserAppearancePreferencesCommandHandler` (for ThemeMode, Language, Direction).
- Refactored `MainLayout` and `SetupLayout` to use `AppearanceThemeService` instead of building palettes directly.

### In Progress / Blockers
- **Runtime Composition Blocked:** `AppearanceThemeService` in the Blazor Client still uses hardcoded palettes instead of the database-backed `UiTheme`.
- **BFF Transport Blocked:** The `/bff/theme` endpoint is still strictly cookie-based and does not sync with the new `UserPreference` storage.
- **Admin UI Missing:** The Admin UI sections (`TenantBrandingSection`, `InstanceBrandingSection`) have not been updated to use the new catalog.
- **User Pref Handler Incomplete:** `UpdateCurrentUserAppearancePreferencesCommandHandler` is missing the logic for `DefaultThemeId`.

## Current Implementation State
- **Domain & Persistence:** 100% Complete.
- **Application & Settings Precedence:** 70% Complete.
- **API & BFF:** 0% Complete.
- **Client UI & Runtime:** 10% Complete.

## Files Modified This Session And Why

- `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-plan.md`
  - Updated to reflect current implementation state and remaining gaps.
- `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-context.md`
  - Updated progress and current implementation status.
- `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-tasks.md`
  - Updated checkboxes to show Phase 0, 1, and 3 as complete.
- `dev/active/hierarchical-settings-preferences/implementation-report.md`
  - New file providing a detailed breakdown of findings and required next steps.

## Issues / Risks Discovered This Session

- `AppearanceThemeService` is a shell that still has hardcoded palettes, which prevents the new `UiTheme` catalog from actually affecting the UI.
- `PublicExperienceSettingsDto` is out of sync with the new appearance model, which will cause issues for anonymous SSR.

## Next Immediate Steps

1. Update `UpdateCurrentUserAppearancePreferencesCommandHandler` to handle `DefaultThemeId`.
2. Update `PublicExperienceSettingsDto` and its query handler.
3. Overhaul `AppearanceThemeService` to use `UiTheme` data.
4. Update Admin UI branding sections.
