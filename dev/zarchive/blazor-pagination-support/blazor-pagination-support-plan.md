# Blazor Infinite Scroll & Server-Driven Pagination - Implementation Plan

**Last Updated**: 2026-02-10

## Executive Summary

The API returns paginated results for all GetAll endpoints via `PaginatedResult<T>` (with `pageNumber`, `pageSize`, `TotalCount`, `TotalPages`, `HasNextPage`). The Blazor UI still fetches all data with `pageSize=100`, performs in-memory filtering/paging, and uses either "Load More" buttons or `MudPagination` controls. This plan replaces the previous button-based pagination approach with **infinite scroll** using Blazor's built-in `<Virtualize>` component with `ItemsProvider` for card grids and `MudDataGrid VirtualizeServerData` for admin tables — delivering a seamless, scroll-to-load experience everywhere.

## Current State Analysis

### API Layer (✅ Complete)
- All controllers return `PaginatedResult<T>` wrapped in `HalCollectionResource<T>` with HATEOAS links.
- Controllers accept `pageNumber` (default 1) and `pageSize` (default 20, max 100) query params.
- `PaginatedResult<T>` model lives in `Explore.Application/Responses/PaginatedResult.cs`.

### NSwag Generated Client (✅ Complete)
- `EventApiClient.g.cs` already includes `pageNumber`/`pageSize` parameters on all `*AllAsync()` methods.
- Returns `HalCollectionResourceOf*Dto` types with `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`, `HasPrevious`, `HasNext`.
- `HalResourceExtensions.cs` provides `GetItems()` to extract typed items from HAL responses.

### Blazor Services (❌ Needs Work)
- Services return `ICollection<T>` — pagination metadata is **discarded**.
- Hardcoded `pageSize=100` in service calls (e.g., `_apiClient.GetEventsAsync(1, 100)`).
- No way for UI to get TotalCount, HasNextPage, or request specific pages.

### Blazor UI Pages (❌ Needs Work)
- **Zero usage** of `Virtualize` component in entire project.
- `EventList.razor`: "Load More" button over full in-memory list.
- `MyEvents.razor`: `MudPagination` over full in-memory list.
- `MyOrganizations.razor`: No pagination at all.
- `MyRegistrations.razor`: No pagination at all.
- Admin tables: `MudTable` + `MudTablePager` over full in-memory lists.

## Proposed Future State

**All list views** use server-driven infinite scroll:
- **Card grids** (EventList, MyEvents, MyOrganizations, MyRegistrations, OrganizationReviews): Blazor `<Virtualize ItemsProvider="...">` with skeleton placeholders.
- **Admin tables** (Tags, Categories, Locations, etc.): `MudDataGrid` with `VirtualizeServerData` for scroll-based loading.
- **Lookup tables** (EventTypes, EventFormats, Madhabs): Continue fetching all with `pageSize=100` (small datasets, used for dropdowns).
- Services expose `PaginatedResult<T>` with full metadata. UI passes `startIndex`/`count` or `pageNumber`/`pageSize` to services.

## Architecture Decisions

### Why Blazor `<Virtualize>` (not MudBlazor infinite scroll)
1. **Built-in .NET component** — no extra dependency, maintained by Microsoft.
2. **ItemsProvider pattern** — receives `ItemsProviderRequest` with `StartIndex` and `Count`, returns `ItemsProviderResult<T>` with `TotalItemCount`.
3. **OverscanCount** — pre-fetches items beyond visible viewport for smooth scrolling.
4. **Placeholder support** — native `<Placeholder>` template for skeleton loading.
5. **Works with any layout** — wraps MudGrid/cards/lists, not tied to MudTable.

### Why MudDataGrid `VirtualizeServerData` for Admin Tables
1. **Native MudBlazor solution** for tabular data with virtualization.
2. Handles sorting, filtering, and pagination in `GridStateVirtualize<T>`.
3. Consistent with MudBlazor patterns already used in admin pages.

### PaginatedResult<T> in Blazor.Client
- Mirror of the API model but in `Explore.Blazor.Client/Models/` namespace.
- Properties: `Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`, `HasPreviousPage`, `HasNextPage`.
- Services map from `HalCollectionResource` → `PaginatedResult<T>` using existing `GetItems()` + HAL metadata.

## Implementation Phases

### Phase 1: Infrastructure (Service Layer + Models)

**1.1 Create PaginatedResult<T> in Blazor.Client**
- File: `Explore.Blazor.Client/Models/PaginatedResult.cs` (new)
- Mirror API model: `Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`, `HasPreviousPage`, `HasNextPage`.
- Add static factory: `FromHalCollection(HalCollectionResourceOf*Dto response)`.
- Effort: S
- Dependencies: None

**1.2 Update IEventService + EventService for Paginated Returns**
- Files: `Explore.Blazor.Client/Services/EventService.cs`
- Add: `Task<PaginatedResult<EventListDto>> GetEventsPagedAsync(int pageNumber, int pageSize)`
- Add: `Task<PaginatedResult<EventListDto>> GetMyEventsPagedAsync(int pageNumber, int pageSize)`
- Keep old methods for backward compat during migration.
- Acceptance: New methods return pagination metadata from HAL response.
- Effort: M
- Dependencies: 1.1
- Related Skills: `blazor-bff-patterns`, `clean-architecture-rules`

**1.3 Update Remaining Services for Paginated Returns**
- Files: `OrganizationService.cs`, `CategoryService.cs`, `TagService.cs`, `LocationService.cs`, `EventRegistrationService.cs`, `AdminService.cs`
- Add paged method variants that return `PaginatedResult<T>`.
- Acceptance: Each service exposes at least one paged method.
- Effort: L
- Dependencies: 1.1
- Related Skills: `blazor-bff-patterns`

### Phase 2: Reference Implementation (EventList.razor)

**2.1 Implement Infinite Scroll on EventList.razor**
- File: `Explore.Blazor.Client/Pages/Event/EventList.razor` + `.razor.cs`
- Replace `@foreach` + "Load More" with `<Virtualize ItemsProvider="LoadEvents">`.
- ItemsProvider converts `ItemsProviderRequest.StartIndex`/`Count` to `pageNumber`/`pageSize` and calls `EventService.GetEventsPagedAsync()`.
- Skeleton `<Placeholder>` template for loading state.
- Filters reset the Virtualize provider (call `virtualize.RefreshDataAsync()`).
- Remove in-memory `AllFilteredEvents` / `FilteredEvents` / `displayedCount` / `loadBatchSize`.
- Acceptance:
  - Scrolling loads new pages automatically.
  - No "Load More" button.
  - Filters trigger new server request and reset scroll position.
  - Skeleton cards shown while loading.
  - Total count displayed from API metadata.
- Effort: L
- Dependencies: 1.2
- Related Skills: `blazor-ui-conventions`

**2.2 Verify and Iterate**
- Build and test the EventList infinite scroll.
- Verify filter + scroll interaction.
- Validate skeleton UX.
- Document the pattern for rollout to other pages.
- Effort: M

### Phase 3: Rollout to All Card Grid Pages

**3.1 MyEvents.razor** → Infinite scroll (remove MudPagination)
**3.2 MyOrganizations.razor** → Infinite scroll
**3.3 MyRegistrations.razor** → Infinite scroll
**3.4 OrganizationReviews.razor** → Infinite scroll
**3.5 LandingPage sections** → Limited preview with "View All" link (keep small pageSize)

- Each follows the EventList pattern.
- Effort: M per page
- Dependencies: Phase 2

### Phase 4: Admin Table Virtualization

**4.1 Convert Admin Tables to MudDataGrid VirtualizeServerData**
- Files: `Tags.razor`, `Categories.razor`, `Locations.razor`, `Languages.razor`, `Madhabs.razor`, `EventTypes.razor`, `EventFormats.razor`, `AudienceAges.razor`, `AudienceGenders.razor`, `EventStatuses.razor`
- Replace `MudTable Items=@...` + `MudTablePager` with `MudDataGrid VirtualizeServerData=@...` + `Virtualize=true`.
- ServerDataFunc calls paged service methods.
- Acceptance: Scroll-based loading, no page buttons, sorting preserved.
- Effort: L (10+ pages but repetitive pattern)
- Dependencies: 1.3
- Related Skills: `blazor-ui-conventions`

**4.2 OrganizationMembers.razor** → MudDataGrid VirtualizeServerData
- Effort: M
- Dependencies: 1.3

### Phase 5: Cleanup & Validation

**5.1 Remove Dead Code**
- Remove old `AllFilteredEvents`, `FilteredEvents`, `displayedCount`, `loadBatchSize` patterns.
- Remove `MudPagination` / `MudTablePager` from migrated pages.
- Remove old non-paged service methods once all callers migrated.
- Effort: M

**5.2 Build Verification**
- `dotnet build --configuration Release --verbosity quiet`
- Effort: S

**5.3 Manual UI Validation**
- Test infinite scroll on each migrated page.
- Verify filter interactions.
- Test with slow network (should show skeletons).
- Effort: M

## Risk Assessment

- **Risk**: Virtualize may not work well with MudGrid card layouts.
  - Mitigation: Test in Phase 2 with EventList first. Fallback: use IntersectionObserver JS interop.
- **Risk**: Filter changes require resetting Virtualize state.
  - Mitigation: Call `virtualize.RefreshDataAsync()` on filter change; store Virtualize reference via `@ref`.
- **Risk**: Admin tables may lose sorting/search when switching to MudDataGrid.
  - Mitigation: MudDataGrid VirtualizeServerData natively supports sorting/filtering via `GridStateVirtualize<T>`.
- **Risk**: Concurrent scroll requests may cause race conditions.
  - Mitigation: Use `CancellationToken` from `ItemsProviderRequest` to cancel stale requests.

## Success Metrics

- All list views load data incrementally as user scrolls — no "Load More" or pagination buttons.
- No full-list fetches (no `pageSize=100` for display lists).
- Skeleton placeholders shown during loading for all list views.
- Total counts reflect API metadata, not local collection size.
- Build succeeds without errors.

## Effort Estimates

- Phase 1 (Infrastructure): 0.5-1 day
- Phase 2 (Reference impl): 0.5-1 day
- Phase 3 (Card grid rollout): 1-2 days
- Phase 4 (Admin tables): 1-2 days
- Phase 5 (Cleanup + validation): 0.5 day

Total Estimate: 3-6 days
