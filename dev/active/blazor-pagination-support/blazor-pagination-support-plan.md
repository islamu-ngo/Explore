# Blazor Pagination Support - Implementation Plan

**Last Updated**: 2026-01-15

## Executive Summary

The API now returns paginated results for GetAll endpoints, while Blazor (Explore.Blazor and Explore.Blazor.Client) still expects full lists and performs in-memory paging and filtering. This plan updates the generated API client, service layer, and list-based UI components to consume paginated responses consistently, with server-driven pagination and updated UX for totals and page navigation.

## Current State Analysis

- API controllers (example: `Explore.API/Controllers/EventController.cs`) return `PaginatedResult<T>` with `pageNumber` and `pageSize` query parameters.
- Blazor client services return `ICollection<T>` from `*AllAsync()` endpoints and load entire lists (example: `Explore.Blazor.Client/Services/EventService.cs`).
- UI components apply local pagination and filtering on full lists (example: `Explore.Blazor.Client/Pages/Event/EventList.razor`, `Explore.Blazor.Client/Pages/Event/MyEvents.razor`).
- NSwag-generated client (`Explore.Blazor.Client/Clients/EventApiClient.g.cs`) is outdated and does not model paginated responses.

## Proposed Future State

- API client supports paginated endpoints and exposes `PaginatedResult` models (generated or wrapped).
- Service interfaces accept pagination parameters and return paginated results.
- List pages (events, orgs, registrations, admin tables) request pages explicitly and display counts based on API metadata.
- Lookup tables continue to load fully using `pageSize = 100` or multi-page fetch helpers.

## Implementation Phases (Clean Architecture Alignment)

### Phase 1: Contract and Client Alignment (API Client Layer)

**1.1 Regenerate or Update API Client for Pagination**
- Files: `Explore.Blazor.Client/Clients/EventApiClient.g.cs` (generated)
- Update client to include query parameters and paginated response types for all GetAll endpoints.
- Acceptance Criteria:
  - `EventAllAsync` and other `*AllAsync` methods include `pageNumber` and `pageSize` parameters (optional or overloads).
  - Paginated response models are generated (e.g., `PaginatedResultOfEventListDto`) or a custom wrapper exists.
- Effort: M
- Dependencies: API Swagger schema reflects paginated responses
- Related Skills: `blazor-bff-patterns`

**1.2 Define a Shared Pagination Model (if needed)**
- Files: `Explore.Blazor.Client/Models/PaginatedResult.cs` (new, if not generated)
- Acceptance Criteria:
  - Single generic model with `Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`, `HasPreviousPage`, `HasNextPage`.
- Effort: S
- Dependencies: 1.1
- Related Skills: `clean-architecture-rules`

### Phase 2: Blazor Client Service Updates (Application Layer in UI)

**2.1 Update Core Services to Return Paginated Results**
- Files: `Explore.Blazor.Client/Services/EventService.cs`, `Explore.Blazor.Client/Services/EventService.Operations.cs`, `Explore.Blazor.Client/Services/LandingPageService.cs`
- Change `GetAllEventsAsync` and `GetMyEventsAsync` to accept `pageNumber` and `pageSize` and return paginated results.
- Acceptance Criteria:
  - Service methods return paginated results without breaking error handling.
  - Landing page queries use pageSize limits and read totals from metadata.
- Effort: M
- Dependencies: 1.1, 1.2
- Related Skills: `cqrs-mediatr-guidelines`

**2.2 Update Lookup and Admin Services for Pagination**
- Files: `Explore.Blazor.Client/Services/CategoryService.cs`, `Explore.Blazor.Client/Services/TagService.cs`, `Explore.Blazor.Client/Services/LocationService.cs`, `Explore.Blazor.Client/Services/AdminService.cs`, `Explore.Blazor.Client/Services/EventRegistrationService.cs`, `Explore.Blazor.Client/Services/OrganizationService.cs`, `Explore.Blazor.Client/Services/OrganizationMemberService.cs`, `Explore.Blazor.Client/Services/OrganizationReviewService.cs`
- Acceptance Criteria:
  - Add paged methods or update existing list methods to accept pagination parameters.
  - Lookup helpers fetch all pages when needed (pageSize=100, loop while `HasNextPage`).
- Effort: L
- Dependencies: 1.1, 1.2
- Related Skills: `blazor-bff-patterns`

### Phase 3: UI Component Refactors (Presentation Layer)

**3.1 Event List Page (Public Explore)**
- File: `Explore.Blazor.Client/Pages/Event/EventList.razor`
- Replace local `AllFilteredEvents` paging with server-driven pagination.
- Update filter behaviors to request new pages; re-evaluate category/tag filter endpoints for pagination support.
- Acceptance Criteria:
  - Page navigation updates the API request `pageNumber`.
  - Results count uses API `TotalCount` and `TotalPages`.
  - Filters reset pagination to page 1 and request a new page.
- Effort: L
- Dependencies: 2.1
- Related Skills: `blazor-ui-conventions`

**3.2 My Events Page**
- File: `Explore.Blazor.Client/Pages/Event/MyEvents.razor`
- Replace local pagination with paginated `GetMyEventsAsync` calls.
- Acceptance Criteria:
  - My events list reflects API pagination and totals.
  - Page controls are enabled/disabled based on API metadata.
- Effort: M
- Dependencies: 2.1
- Related Skills: `blazor-ui-conventions`

**3.3 Admin and Management Lists**
- Files: `Explore.Blazor.Client/Pages/Admin/AdminList.razor`, `Explore.Blazor.Client/Pages/Admin/Tags.razor`, `Explore.Blazor.Client/Pages/Admin/Categories.razor`, `Explore.Blazor.Client/Pages/Admin/Locations.razor`, `Explore.Blazor.Client/Pages/Organization/OrganizationMembers.razor`, `Explore.Blazor.Client/Pages/User/MyRegistrations.razor`
- Update list pages to use paginated service calls and show totals.
- Acceptance Criteria:
  - Each list page requests pages explicitly and displays pagination controls.
  - All counts reflect API `TotalCount`.
- Effort: L
- Dependencies: 2.2
- Related Skills: `blazor-ui-conventions`

**3.4 Shared Pagination Component (Optional)**
- Files: `Explore.Blazor.Client/Components/Common/PaginationControls.razor` (new)
- Acceptance Criteria:
  - Shared component supports MudPagination and emits page changes.
  - Used by multiple pages for consistent UI.
- Effort: S
- Dependencies: 3.1
- Related Skills: `blazor-ui-conventions`

### Phase 4: BFF/Server Integration Check (Explore.Blazor)

**4.1 Validate BFF Proxy Expectations**
- Files: `Explore.Blazor/Extensions/BFF_REFACTORING_README.md`, any BFF endpoint mappings
- Acceptance Criteria:
  - No hardcoded assumptions about non-paginated responses in server-side proxying.
  - Documentation updated if BFF endpoints now expect pagination parameters.
- Effort: S
- Dependencies: 1.1
- Related Skills: `blazor-bff-patterns`

### Phase 5: Testing and Validation

**5.1 Manual UI Validation**
- Verify Event List, My Events, Admin lists, and registrations paging.
- Acceptance Criteria:
  - Pagination controls switch pages without errors.
  - Totals display correctly for all updated pages.
- Effort: M
- Dependencies: 3.1, 3.2, 3.3

**5.2 Build Verification**
- Run `dotnet build Explore.sln` and resolve compile issues if any.
- Acceptance Criteria:
  - Build succeeds without errors.
- Effort: S
- Dependencies: All prior phases

## Detailed Tasks with Acceptance Criteria

1. Update NSwag client to include paginated responses and page parameters.
   - Acceptance: Generated methods return paginated models and include `pageNumber`, `pageSize`.
   - Effort: M

2. Update EventService and LandingPageService to use paginated responses.
   - Acceptance: Event list and landing page data use API pagination metadata.
   - Effort: M

3. Update lookup/admin services to support pagination or multi-page fetch.
   - Acceptance: All list-fetch methods handle paginated responses.
   - Effort: L

4. Refactor `Explore.Blazor.Client/Pages/Event/EventList.razor` and `Explore.Blazor.Client/Pages/Event/MyEvents.razor` to server pagination.
   - Acceptance: UI uses API totals and fetches pages on demand.
   - Effort: L

5. Refactor admin and management list pages to server pagination.
   - Acceptance: Each page loads via paged service calls with UI pagination.
   - Effort: L

6. Validate UI manually and fix any edge cases.
   - Acceptance: Pagination works across all updated screens.
   - Effort: M

## Risk Assessment and Mitigation

- Risk: API does not support pagination for some filter endpoints (category/tag/session lists).
  - Mitigation: Confirm API endpoints; if missing, keep limited fetch (pageSize=100) or add API changes in follow-up.
- Risk: NSwag client regen changes DTO names and breaks references.
  - Mitigation: Isolate changes to generated file and update service interfaces accordingly.
- Risk: UX regressions in filtering when data no longer loaded locally.
  - Mitigation: Reset page on filter change and validate counts with API metadata.

## Success Metrics

- All Blazor list pages load with paginated API responses and no full-table fetches.
- UI pagination shows accurate totals and page counts.
- `Explore.Blazor.Client/Pages/Event/EventList.razor` and `Explore.Blazor.Client/Pages/Event/MyEvents.razor` no longer rely on in-memory pagination over full datasets.

## Required Resources and Dependencies

- Updated API Swagger with paginated response schemas.
- NSwag client generation workflow (see prior NSwag refactor notes if needed).
- MudBlazor pagination component usage.

## Effort Estimates

- Phase 1: 0.5-1 day
- Phase 2: 1-2 days
- Phase 3: 2-3 days
- Phase 4: 0.5 day
- Phase 5: 0.5-1 day

Total Estimate: 4-7 days
