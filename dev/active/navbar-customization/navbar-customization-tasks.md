# Tasks: Navbar Customization

Last Updated: 2026-02-10

## Phase 1: Domain & Infrastructure
- [ ] **1.1 Create Entity**
  - Define `TenantNavigationLink` in `Explore.Domain`.
  - Add `ICollection` to `Tenant` entity.
- [ ] **1.2 Configure Persistence**
  - Add `TenantNavigationLinkConfiguration` in `Explore.Persistence`.
  - Register in `ExploreDbContext`.
- [ ] **1.3 Database Migration**
  - Create and apply migration `AddTenantNavigationLinks`.

## Phase 2: Application Logic (CQRS)
- [ ] **2.1 Define DTOs**
  - `TenantNavigationLinkDto`, `Create...Dto`, `Update...Dto`.
- [ ] **2.2 Implement Queries**
  - `GetTenantNavLinksQuery` handler.
- [ ] **2.3 Implement Commands**
  - `CreateTenantNavLinkCommand` handler.
  - `UpdateTenantNavLinkCommand` handler.
  - `DeleteTenantNavLinkCommand` handler.
  - `ReorderTenantNavLinksCommand` handler.

## Phase 3: API Endpoints
- [ ] **3.1 Controller Implementation**
  - Add `Navigation` endpoints to `TenantController`.
  - Apply `[Authorize]` for write ops.
  - Apply `[OutputCache]` for read ops.

## Phase 4: Frontend Service
- [ ] **4.1 API Client**
  - Refresh NSwag client.
- [ ] **4.2 Service Layer**
  - Implement `TenantNavigationService` in Blazor client.

## Phase 5: UI Implementation
- [ ] **5.1 Public NavMenu**
  - Update `NavMenu.razor` to load and render links.
- [ ] **5.2 Admin Management Page**
  - Create `Pages/Admin/TenantSettings/Navigation.razor`.
  - Implement List/Add/Edit/Delete/Reorder UI.
## Context Reset Session Update (2026-02-15 21:26 Europe/Brussels)

- Status update: No task-state changes in this session for this track.
- Priority update: Keep existing ordering; analytics work was handled in a separate track.
- Next step: Resume from current in-progress or highest-priority unchecked item.

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Status update: Shifted active focus in this track to admin consolidation requested by user; implementation not started yet, planning and codebase analysis completed.
- Priority update: Admin consolidation tasks are now highest priority for this track.

## Phase 6: Admin Consolidation Into Panel Layouts 🟡 IN PROGRESS
- [x] **6.1 Tenant panel: move organization approval into section component**
  - Create `Explore.Blazor.Client/Components/Admin/Tenant/TenantOrganizationsSection.razor`
  - Reuse organization approval table/actions currently in `Explore.Blazor.Client/Pages/Admin/AdminList.razor`
- [x] **6.2 Tenant panel: move lookup tables into section component**
  - Create `Explore.Blazor.Client/Components/Admin/Tenant/TenantLookupTablesSection.razor`
  - Reuse lookup tab loading/presentation currently in `Explore.Blazor.Client/Pages/Admin/LookupTables.razor` and `.razor.cs`
- [x] **6.3 Wire tenant sections into tenant layout navigation**
  - Update `Explore.Blazor.Client/Components/Admin/Tenant/TenantAdminSettingsLayout.razor`
  - Add left-panel entries for Organizations and Lookup Tables
- [x] **6.4 Instance panel: add SMTP section with test connection**
  - Create `Explore.Blazor.Client/Components/Admin/Instance/InstanceSmtpSection.razor`
  - Follow same UX pattern as `InstanceStorageSection.razor`
- [x] **6.5 Extend instance onboarding API/client contracts for SMTP settings**
  - Update `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`
  - Update `Explore.API/Controllers/InstanceOnboardingController.cs`
  - Add/update application DTOs/handlers for SMTP get/update/test endpoints using `GovernanceSettingKeys.Email*`
- [x] **6.6 Wire SMTP section into instance layout navigation**
  - Update `Explore.Blazor.Client/Components/Admin/Instance/InstanceAdminSettingsLayout.razor`
  - Add left-panel SMTP entry and section render branch
- [x] **6.7 Update admin dropdown menu links/labels**
  - Update `Explore.Blazor.Client/Layout/NavMenu.razor`
  - Remove `/admin` dashboard entry; use tenant/instance administration links by role
- [x] **6.8 Remove legacy standalone admin pages and routes**
  - Remove `Explore.Blazor.Client/Pages/Admin/AdminList.razor` usage/route
  - Remove standalone lookup pages replaced by panel sections
  - Ensure no dead links remain in UI routing
- [x] **6.9 Verification and hardening**
  - Run diagnostics on changed files
  - Run build and targeted tests for admin navigation and onboarding settings

## Context Reset Session Update (2026-02-23 18:47 Europe/Brussels)

- Status update: Phase 6 implementation and verification completed in this session.
- Completed verification:
  - `dotnet build` ✅
  - `dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --no-build` ✅ (522 passed)
  - `dotnet test --project "Event.Application.UnitTests/Event.Application.UnitTests.csproj"` ✅ (278 passed)
- New tasks discovered:
  - [ ] Manual browser smoke test for tenant/instance admin UI sections (organizations, lookup tables, SMTP).
  - [ ] Optional: clean up pre-existing analyzer warnings (outside current feature scope).
- Priority update: Keep manual UI smoke test as next immediate action for this track.

---

## Session Checkpoint (2026-02-27 Europe/Brussels)

- [x] Reviewed task continuity status for context reset handoff.
- [ ] Resume implementation work from this task latest documented in-progress section.
- [ ] Re-validate with build/tests once implementation resumes.

