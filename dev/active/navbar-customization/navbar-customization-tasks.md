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
