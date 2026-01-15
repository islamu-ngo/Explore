# Blazor Pagination Support - Task Checklist

**Last Updated**: 2026-01-15

## Phase 1: Client Contract Alignment ? NOT STARTED
- [ ] Regenerate/update `Explore.Blazor.Client/Clients/EventApiClient.g.cs` for pagination
- [ ] Add shared `PaginatedResult<T>` model if not generated

## Phase 2: Service Layer Updates ? NOT STARTED
- [ ] Update `Explore.Blazor.Client/Services/EventService.cs` to accept pageNumber/pageSize and return paginated result
- [ ] Update `Explore.Blazor.Client/Services/LandingPageService.cs` for paginated fetches
- [ ] Update lookup/admin services to handle paginated list results
- [ ] Add helper to fetch all pages for lookup tables when needed

## Phase 3: UI Refactors ? NOT STARTED
- [ ] Refactor `Explore.Blazor.Client/Pages/Event/EventList.razor` to server pagination
- [ ] Refactor `Explore.Blazor.Client/Pages/Event/MyEvents.razor` to server pagination
- [ ] Update admin list pages to use paginated services
- [ ] Update organization/user list pages to use paginated services
- [ ] (Optional) Create shared pagination component

## Phase 4: BFF/Server Checks ? NOT STARTED
- [ ] Validate BFF mappings for pagination assumptions and update docs if needed

## Phase 5: Validation ? NOT STARTED
- [ ] Manual UI verification for pagination across updated pages
- [ ] `dotnet build Explore.sln` and fix compile errors if any
