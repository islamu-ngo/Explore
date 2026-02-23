# Major Decisions

Last Updated: 2026-02-23

## 2026-02-23 18:12 Europe/Brussels - Admin Consolidation Handoff Scope

- Decision: Consolidate admin UX into two panel pages only:
  - `Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor`
  - `Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor`
- Why: User explicitly requested eliminating split between `/admin` and separate admin pages, and matching existing settings-style panel navigation.
- Implication: Legacy `/admin` dashboard and standalone lookup/admin pages are target candidates for removal after migration.

## 2026-02-23 18:12 Europe/Brussels - SMTP Configuration Placement

- Decision: Add SMTP configuration under instance admin panel as a dedicated sidebar section, with a test connection action.
- Why: SMTP credentials are platform-level concern requested for platform/instance administrators.
- Implementation anchor points:
  - UI pattern: `Explore.Blazor.Client/Components/Admin/Instance/InstanceStorageSection.razor`
  - API pattern: `Explore.API/Controllers/InstanceOnboardingController.cs` storage settings/test endpoints
  - Setting keys: `Explore.Domain/Constants/GovernanceSettingKeys.cs` (`EmailSmtp*`, `EmailFrom*`)

## 2026-02-23 18:12 Europe/Brussels - Dev Docs Continuity Protocol

- Decision: Before context reset, update every active context/tasks file with a timestamped checkpoint entry, and add deep handoff detail to the currently active track only.
- Why: Ensures broad continuity for all active tracks while preserving high-signal detail where active implementation is ongoing.
