# Plan: Implement Navbar Customization Feature

## Executive Summary
This plan details the implementation of a **Navbar Customization** feature for the ISLAMU Event platform. This feature empowers Tenant Administrators to add, manage, and order custom external navigation links in their instance's sidebar. This reduces friction for end-users by creating a seamless visual integration between the tenant's main website and their event portal.

## Current State Analysis
- **Domain**: `Tenant` entity exists but lacks navigation configuration.
- **UI**: `NavMenu.razor` in `Explore.Blazor.Client` has a static list of links.
- **Architecture**: Strictly follows Clean Architecture with CQRS.
- **Multi-tenancy**: System is multi-tenant; data must be isolated by `TenantId`.

## Proposed Future State
- **Domain**: New `TenantNavigationLink` entity (1:N with Tenant).
- **Admin UI**: New "Navigation" tab in Tenant Settings for managing links.
- **Public UI**: `NavMenu` dynamically renders custom links above/below standard items.
- **Performance**: Links are cached to prevent DB hits on every page load.

---

## Phase 1: Domain Layer
**Goal**: Define the data structure for navigation links.

### Task 1.1: Create `TenantNavigationLink` Entity
- **File**: `Explore.Domain/TenantNavigationLink.cs`
- **Description**: Create an entity implementing `ITenantEntity`.
- **Properties**:
  - `Id` (Guid)
  - `TenantId` (Guid, FK)
  - `Label` (string, required, max 50)
  - `Url` (string, required, max 500)
  - `Icon` (string, nullable) - MudBlazor icon string
  - `Order` (int) - For display sorting
  - `OpenInNewTab` (bool)
  - `IsActive` (bool)
- **Related Skills**: `clean-architecture-rules`

### Task 1.2: Update `Tenant` Entity
- **File**: `Explore.Domain/Tenant.cs`
- **Action**: Add `ICollection<TenantNavigationLink> NavigationLinks` property.

---

## Phase 2: Infrastructure Layer
**Goal**: Persist the new entity.

### Task 2.1: Configure EF Core
- **File**: `Explore.Persistence/Configurations/TenantNavigationLinkConfiguration.cs`
- **Action**: Implement `IEntityTypeConfiguration`.
  - Configure max lengths.
  - Configure `Tenant` relationship (DeleteBehavior.Cascade).
  - Add Global Query Filter for `TenantId` (via `ITenantEntity` convention or explicit).

### Task 2.2: Create Migration
- **Action**: Add migration `AddTenantNavigationLinks`.
- **Command**: `dotnet ef migrations add AddTenantNavigationLinks -p Explore.Persistence -s Explore.API`

---

## Phase 3: Application Layer
**Goal**: Implement business logic via CQRS.

### Task 3.1: Define DTOs
- **File**: `Explore.Application/DTOs/Tenant/TenantNavigationLinkDto.cs`
- **File**: `Explore.Application/DTOs/Tenant/CreateTenantNavigationLinkDto.cs`
- **File**: `Explore.Application/DTOs/Tenant/UpdateTenantNavigationLinkOrderDto.cs`

### Task 3.2: Create `GetTenantNavLinks` Query
- **File**: `Explore.Application/Features/Tenants/Queries/GetTenantNavLinks/GetTenantNavLinksQuery.cs`
- **Logic**: Fetch links for current `TenantId` (from `ITenantContext`), ordered by `Order`.
- **Caching**: Use `[OutputCache]` in API, but Application layer should just query DB.

### Task 3.3: Create `ManageTenantNavLinks` Commands
- **Files**:
  - `CreateTenantNavLinkCommand.cs`
  - `UpdateTenantNavLinkCommand.cs`
  - `DeleteTenantNavLinkCommand.cs`
  - `ReorderTenantNavLinksCommand.cs`
- **Logic**: Standard CRUD. Ensure `TenantId` is enforced from context.

---

## Phase 4: API Layer
**Goal**: Expose logic to the frontend.

### Task 4.1: Update `TenantController`
- **File**: `Explore.API/Controllers/TenantController.cs`
- **Endpoints**:
  - `GET /api/tenant/navigation` (Anonymous/Public) - Cached 5-10m.
  - `POST /api/tenant/navigation` (Admin only)
  - `PUT /api/tenant/navigation/{id}` (Admin only)
  - `DELETE /api/tenant/navigation/{id}` (Admin only)
  - `PUT /api/tenant/navigation/reorder` (Admin only)

---

## Phase 5: Blazor Client (Service & UI)
**Goal**: Consume API and render UI.

### Task 5.1: Update NSwag Client
- **Action**: Re-generate API client (automatic on build usually, or manual trigger).

### Task 5.2: Create `TenantNavigationService`
- **File**: `Explore.Blazor.Client/Services/TenantNavigationService.cs`
- **Interface**: `ITenantNavigationService`
- **Methods**: `GetLinksAsync`, `CreateLinkAsync`, `ReorderLinksAsync`, etc.

### Task 5.3: Update `NavMenu.razor`
- **File**: `Explore.Blazor.Client/Layout/NavMenu.razor`
- **Logic**:
  - Inject `ITenantNavigationService`.
  - Fetch links in `OnInitializedAsync`.
  - Render `MudNavLink` items inside a loop.
  - Use `Target="_blank"` if `OpenInNewTab` is true.

### Task 5.4: Create Admin Page
- **File**: `Explore.Blazor.Client/Pages/Admin/TenantSettings/Navigation.razor`
- **Features**:
  - List current links.
  - "Add Link" button -> Dialog.
  - "Edit/Delete" buttons.
  - **Drag & Drop** reordering (using `MudDropContainer` or simple Up/Down buttons if Drag/Drop is complex for junior). *Recommendation: Use simple Up/Down buttons first for stability.*

---

## Risk Assessment
- **Cache Invalidation**: Public nav links are cached. When Admin updates them, the cache must be invalidated.
  - *Mitigation*: Use `[OutputCache]` tags in API and `IOutputCacheStore.EvictByTagAsync` in the Command Handler (or Controller).
- **Tenant Context**: Ensure `GetTenantNavLinks` NEVER leaks links from other tenants.
  - *Mitigation*: Rely on Global Query Filters + specific Unit Tests.

## Success Metrics
- Tenant Admin can add a link and see it in the sidebar.
- Link correctly redirects to external URL.
- No performance regression in `NavMenu` load time.

## Effort Estimates
- **Domain/Infra**: S (2h)
- **Application**: M (4h)
- **API**: S (2h)
- **Blazor Admin UI**: M (5h)
- **Blazor Public UI**: S (2h)
- **Total**: ~15 hours
