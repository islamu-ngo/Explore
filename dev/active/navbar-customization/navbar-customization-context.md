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

## Context Reset Session Update (2026-02-23 18:47 Europe/Brussels)

- Current implementation state: Admin consolidation and SMTP integration are implemented and verified in this session.
- Key decisions made this session:
  - Kept admin experience panel-driven (MudList sidebar + section content) for both tenant and instance administration.
  - Embedded lookup management into tenant administration instead of keeping standalone admin lookup pages.
  - Added SMTP management as instance-level governance capability with dedicated get/update/test API endpoints.
  - Removed `/admin` dashboard navigation and aligned dropdown labels to "Instance Administration" and "Tenant Administration".
- Files modified and why:
  - `Explore.Blazor.Client/Components/Admin/Tenant/TenantOrganizationsSection.razor`: moved organization approval workflow into tenant panel section.
  - `Explore.Blazor.Client/Components/Admin/Tenant/TenantLookupTablesSection.razor`: consolidated editable + reference lookup tables into tenant panel section.
  - `Explore.Blazor.Client/Components/Admin/Tenant/TenantAdminSettingsLayout.razor`: added Organizations/Lookup sidebar entries and section rendering.
  - `Explore.Blazor.Client/Components/Admin/Instance/InstanceSmtpSection.razor`: added SMTP settings UI + connection test action.
  - `Explore.Blazor.Client/Components/Admin/Instance/InstanceAdminSettingsLayout.razor`: wired SMTP into instance panel load/save/navigation.
  - `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`: added SMTP client models + get/update/test methods.
  - `Explore.API/Controllers/InstanceOnboardingController.cs`: added `smtp-settings` and `test-smtp` endpoints.
  - `Explore.Application/*` new SMTP DTO/service/CQRS files + DI registration to persist/read SMTP settings.
  - `Explore.Blazor.Client/Layout/NavMenu.razor` and `Explore.Blazor.Client/Routes.razor`: removed legacy admin links/routes and updated administration labels.
  - Deleted legacy standalone admin pages under `Explore.Blazor.Client/Pages/Admin/` now replaced by panel sections.
- Complex problems solved:
  - Route compilation break after deleting pages fixed by removing stale route component registrations in `Routes.razor`.
  - Dialog parameter type mismatch in `TenantLookupTablesSection` fixed by passing `CategoryListDto` to `EditCategoryDialog`.
- Integration points discovered:
  - SMTP persistence and runtime test rely on governance setting keys in `GovernanceSettingKeys.Email*` and existing `IEmailService.TestConnectionAsync`.
  - Admin role visibility remains claim-driven through existing `NavMenu.razor.cs` helpers.
- Testing/verification performed:
  - `dotnet build` passes (warnings only, no new errors).
  - `dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --no-build` passes (522/522).
  - `dotnet test --project "Event.Application.UnitTests/Event.Application.UnitTests.csproj"` passes (278/278).
- Blockers/issues discovered:
  - Razor LSP is unavailable in this environment; build/test used as authoritative verification for Razor changes.
  - Existing repository warnings (nullability/analyzers/Mud analyzer warnings) remain pre-existing and not addressed in this task.
- Next immediate steps:
  1. Manual UI smoke pass for tenant/instance admin panel flows in browser.
  2. Optional cleanup pass for pre-existing analyzer warnings unrelated to this feature.
  3. Prepare commit when requested by user.

---

## SESSION CHECKPOINT (2026-02-27 Europe/Brussels)

### Status This Session
- No implementation changes were made in this task during this session.
- Task remains in its previously documented state.

### Continuation Notes
- Re-open this context file and matching *-tasks.md before resuming work.
- Re-run project build/tests relevant to that task branch before new edits.

