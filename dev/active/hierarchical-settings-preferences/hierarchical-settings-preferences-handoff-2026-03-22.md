ABOUTME: Reset-safe handoff note for the hierarchical settings preferences planning track.
ABOUTME: Captures exact restart guidance, touched files, unresolved risks, and the next editing target.

# Handoff: Hierarchical Settings Preferences

Last Updated: 2026-03-22 Europe/Brussels

## Goal Of Current Changes

Prepare an implementation-ready architecture for hierarchical settings preferences where:
- instance admins can lock tenant behavior
- tenant admins can manage default themes and branding behavior
- users can override only approved settings across devices
- the theme catalog is modeled as first-class relational data rather than JSON in settings

## Exact Files Last Updated In This Session

- `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-plan.md:1`
  - Rewritten to reject JSON catalog storage and require first-class theme entities plus runtime services.
- `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-context.md:1`
  - Expanded with reset-safe state, touched files, blockers, and restart commands.
- `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-tasks.md:1`
  - Reordered to start with ADR + storage/runtime-contract work.
- `dev/_journal/journal.md:1`
  - Appended session insights and rejected approaches.
- `dev/_journal/MAJOR_DECISIONS.md:1`
  - Appended the major architectural decision for this track.

## Current Implementation State

- No feature code has been implemented yet for this track.
- This session only produced and refined planning/handoff documentation.
- The plan is now stable on four critical points:
  1. no second preference subsystem
  2. no JSON theme catalog in settings
  3. dedicated theme composition/runtime service outside layout code
  4. ADR required before code changes begin

## Most Important Decisions From This Session

- Keep `IHierarchicalSettingsResolver` + `UserPreference` as the single precedence path for defaults and approved user overrides.
- Do not store `appearance.available_themes` or equivalent as JSON in generic settings.
- Model theme catalogs as first-class entities with concurrency and audit support.
- Use settings only for references and behavior keys, such as `appearance.default_theme_id`, `appearance.theme_mode`, and user-overridable event-card behavior.
- Keep MVP policy simple: `MaxScope` + instance lock only; do not add tenant-level override suppression flags unless product explicitly demands them.

## Hard-To-Rediscover Integration Points

- Existing precedence engine: `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`
- Existing sparse user override store: `Explore.Domain/UserPreference.cs`
- Existing typed governance/override model: `Explore.Domain/Policies/PolicySlot.cs`
- Existing policy precedence service: `Explore.Persistence/Services/PolicyResolver.cs`
- Existing cache invalidation pattern: `Explore.Application/Notifications/PolicyChangedCacheInvalidationHandler.cs`
- Current theme duplication to replace later:
  - `Explore.Blazor.Client/Layout/MainLayout.razor.cs`
  - `Explore.Blazor.Client/Layout/SetupLayout.razor.cs`

## Unfinished Work

- The ADR file has not been created yet.
- No theme entity/value-object schema has been drafted in code.
- No `AppearanceSettingDefinitions` file exists yet.
- No runtime theme service contract exists yet.
- No tests or migration planning have started beyond documentation.

## Next File To Edit

- Create a new ADR file under `dev/active/hierarchical-settings-preferences/` and start at line 1.
- After that, the next code-facing design work should begin in new domain files under `Explore.Domain/` for theme entities and bounded palette models.

## Uncommitted Changes Needing Attention

- The repository is already heavily dirty with many unrelated user changes.
- Do not assume the worktree reflects only this task.
- Treat `dev/active/hierarchical-settings-preferences/` as this session's authored output; everything else in `git status` must be inspected before touching.

## Commands To Run On Restart

```bash
git status --short
```

```bash
dotnet build --configuration Release --verbosity quiet
```

## Verification Commands For This Track

```bash
git diff -- "dev/active/hierarchical-settings-preferences"
```

```bash
dotnet build --configuration Release --verbosity quiet
```

## Main Risk If Work Resumes Without Reading This Note

The easiest mistake is to reintroduce the earlier JSON-settings approach or let `MainLayout` absorb runtime theming logic. Both were explicitly rejected in this session and should not re-enter implementation through convenience or drift.
