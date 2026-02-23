# Context: Navbar Customization

## Key Files
- `Explore.Domain/Tenant.cs` - Core entity.
- `Explore.Blazor.Client/Layout/NavMenu.razor` - The component to modify.
- `Explore.API/Controllers/TenantController.cs` - API endpoint location.
- `Explore.Persistence/ExploreDbContext.cs` - DB Context.

## Architecture Decisions
- **Entity Name**: `TenantNavigationLink` to allow future expansion (e.g., specific icons, permission roles).
- **Storage**: Relational table (not JSON) to allow easy querying/ordering.
- **UI Lib**: MudBlazor `MudNavLink` for rendering.
- **Admin UI**: MudBlazor `MudTable` or `MudList` with Up/Down reordering controls.

## Dependencies
- **Authentication**: Admin features require `TenantAdmin` role.
- **Multi-tenancy**: Must strictly use `ITenantContext` to resolve `TenantId`.

## References
- [MudBlazor NavMenu Docs](https://mudblazor.com/components/navmenu)
- [MudBlazor Icons](https://mudblazor.com/features/icons)
## Context Reset Session Update (2026-02-15 21:25 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority shifted to analytics implementation completion and verification.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in `navbar-customization-tasks.md`.

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: Admin consolidation work is planned and fully analyzed, but no product code edits are committed yet in this session.
- Key decisions made this session:
  - Consolidate admin UX to two panel pages only: tenant administration (`/admin/tenant/settings`) and instance administration (`/admin/instance/settings`).
  - Remove standalone admin dashboard route (`/admin`) and standalone lookup CRUD/read pages once functionality is embedded into panel sections.
  - Add role-based dropdown entries in `NavMenu` for tenant and instance administrators only.
  - Add SMTP section to instance admin panel with test connection action.
- Files analyzed this session and why:
  - `Explore.Blazor.Client/Layout/NavMenu.razor` and `Explore.Blazor.Client/Layout/NavMenu.razor.cs`: current role-based admin menu behavior.
  - `Explore.Blazor.Client/Pages/Admin/AdminList.razor`: current organization approval + admin cards source to migrate.
  - `Explore.Blazor.Client/Components/Admin/Tenant/TenantAdminSettingsLayout.razor`: tenant panel host where Organizations/Lookup sections will be added.
  - `Explore.Blazor.Client/Components/Admin/Instance/InstanceAdminSettingsLayout.razor`: instance panel host where SMTP section will be added.
  - `Explore.Blazor.Client/Pages/Admin/LookupTables.razor` and `.razor.cs`: lookup tab content to embed in tenant panel.
  - `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`: available client service contract to extend with SMTP methods.
  - `Explore.API/Controllers/InstanceOnboardingController.cs`: API shape for adding SMTP get/update/test endpoints.
  - `Explore.Domain/Constants/GovernanceSettingKeys.cs`: canonical SMTP setting keys already defined.
- Blockers/issues discovered:
  - No blocker for data keys/models; SMTP keys already exist.
  - Need to add/extend application and API DTO/contracts for SMTP in the same pattern as storage settings.
- Integration points discovered:
  - Admin claims are resolved via `AdminClaimTypes` and consumed in `NavMenu` helper methods (`IsInstanceAdmin`, `IsTenantAdmin`, `HasAnyAdminAuthority`).
  - Existing storage test pattern (`test-storage`) is the template for SMTP test endpoint.
- Exact unfinished work at handoff:
  - Start creating `TenantOrganizationsSection.razor` and `TenantLookupTablesSection.razor` under `Explore.Blazor.Client/Components/Admin/Tenant/`.
  - Then wire both sections into `TenantAdminSettingsLayout.razor` nav state and content rendering.
  - Then create `InstanceSmtpSection.razor`, wire into `InstanceAdminSettingsLayout.razor`, extend onboarding service/api/DTOs, and update `NavMenu` labels/links.
- Next immediate steps:
  1. Implement tenant section components and wire layout.
  2. Implement SMTP section + API/service contract extension.
  3. Remove `/admin` and standalone lookup pages/routes.
  4. Run diagnostics/build/tests.
