# Blazor Infinite Scroll + Modular Aspect-Aware Query Specification - Task Checklist

**Last Updated**: 2026-02-10 12:30 CET

## Phase 1: Infrastructure (Service Layer + Models) ✅ COMPLETE

### 1.1 Shared Pagination Model
- [x] Create `Explore.Blazor.Client/Models/PaginatedResult.cs`
- [x] Add ToPaginatedResult() extension methods for HAL collection types

### 1.2 EventService Paged Methods
- [x] `GetEventsPagedAsync`, `GetMyEventsPagedAsync`, `GetSessionsPagedAsync`
- [x] Keep old methods for backward compat during migration

### 1.3 Other Services Paged Methods
- [x] Organization, Category, Tag, Location, EventRegistration — all paged methods added
- [ ] `AdminService` → Paged methods for lookup table admin views (deferred to Phase 4)

## Phase 2A-2E: Server-Side IQuerySpecification Pattern (Core) ✅ COMPLETE

- [x] Create IFilterSpecification<T>, ISortSpecification<T>, IQuerySpecification<T> interfaces
- [x] Create EventFilter (12 core filters), EventSubqueryFilter (5 junction table filters), EventSort (4 sorts)
- [x] Create EventQuerySpecification (immutable fluent builder with cache key generation)
- [x] Update GetEventListRequest (+14 filter params, +2 sort params)
- [x] Update GetEventListRequestHandler (BuildSpecification, ResolveSortField, cache key)
- [x] Update IEventRepository + EventRepository (spec overload, ApplySubqueryFilters with EXISTS pattern)
- [x] Update EventController (+16 FromQuery params)
- [x] Build verify: Application 0 errors, Persistence 0 errors, API 0 errors

## Phase 2.5: Deep Research for Modular Aspects ✅ COMPLETE

- [x] Read ALL docs/ files (10 files)
- [x] Read Event.cs, EventIslamicAspect.cs, EventTechAspect.cs (entity structures + enums)
- [x] Read EventConfiguration.cs (JSONB MetadataJson config, aspect shared PK config)
- [x] Read IModuleService.cs, ModuleService.cs, ITenantContext.cs (module governance)
- [x] Read NSwag EventApiClient.g.cs (current method signatures — pageNumber/pageSize only)
- [x] Research EF Core JSONB querying (JsonContains, JsonExists, GIN indexes)
- [x] Research pgvector patterns (Vector, CosineDistance, HNSW indexes)

## Phase 2.6: Modular Aspect-Aware Specification Pattern ✅ COMPLETE

- [x] Create `IslamicAspectFilter.cs` — 5 filters (Madhab, GenderMode, QuranRecitation, ReferencePrayer, PrimaryLanguage)
- [x] Create `TechAspectFilter.cs` — 7 filters (SkillLevel, CodingCompetition, Hackathon, HackathonTrack, RequiresLaptop, TechStack, HasPrizePool)
- [x] Create `AspectPresenceFilter.cs` — 3 filters (HasIslamicAspect, HasTechAspect, HasBothAspects)
- [x] Update `EventSubqueryFilter.cs` — +2 JSONB filter types (JsonContains, JsonKeyExists) + enum entries
- [x] Update `EventQuerySpecification.cs` — +3 typed And() overloads for aspect filters + updated XML docs
- [x] Update `GetEventListRequest.cs` — +15 properties (5 Islamic, 6 Tech, 2 JSONB, 2 aspect presence)
- [x] Update `GetEventListRequestHandler.cs` — +IModuleService, +ITenantContext, async BuildSpecificationAsync with module-conditional composition
- [x] Update `EventRepository.cs` — +2 JSONB cases in ApplySubqueryFilters (EF.Functions.JsonContains, JsonExists)
- [x] Update `EventController.cs` — +15 FromQuery params (organized by section), updated endpoint description
- [x] Build verify: Application 0 errors, Persistence 0 errors, API 0 errors
- [x] No EF migration needed (all Application-layer query logic)

## Phase 3: NSwag + Blazor EventList Infinite Scroll ✅ COMPLETE

### 3.0 NSwag Breaking Change Fixes
- [x] Fixed `TenantNavigationService.cs` — added `using Explore.Blazor.Client.Clients;`
- [x] Fixed `NavMenu.razor` line 52 — `bool?` to `bool` (used `== true`)
- [x] Fixed `Navigation.razor` lines 205, 234 — `Guid?` to `Guid` (used `.Value`)
- [x] Build verified: 0 errors

### 3.1 NSwag Client Regeneration
- [x] User manually regenerated NSwag client
- [x] Verified `GetEventsAsync` signature includes all 35 query params (31 filters + pagination + sort + cancellation)

### 3.2 Blazor EventService Filter Support
- [x] Added new `GetEventsPagedAsync` overload to `IEventService` with 14 filter/sort params
- [x] Implemented overload in `EventService` — passes all params to `_apiClient.GetEventsAsync()`
- [x] CancellationToken forwarded from Virtualize's `ItemsProviderRequest`

### 3.3 EventList.razor Infinite Scroll (REFERENCE IMPLEMENTATION)
- [x] Replaced `@foreach` + "Load More" with `<Virtualize @ref="_virtualize" ItemsProvider="LoadEventsAsync" OverscanCount="4">`
- [x] Created `LoadEventsAsync(ItemsProviderRequest)` — converts StartIndex/Count → pageNumber/pageSize + all current filter state
- [x] Added `<Placeholder>` template with skeleton cards
- [x] Wired ALL 8 filter change handlers to `await _virtualize?.RefreshDataAsync()!;`
- [x] Removed ALL in-memory filtering: allEvents, ComputeFilteredEvents, displayedCount, loadBatchSize, _cachedFilteredEvents, _filtersDirty
- [x] Removed client-side category/tag fetching (isLoadingCategory, isLoadingTag, eventsByCategory, eventsByTag)
- [x] Removed allSessions, sessionLanguages (no longer needed for client-side location/language/regMode filtering)
- [x] Server-side date range conversion (selectedDate → dateFrom/dateTo)
- [x] Display total count from API metadata (`_totalCount events found`)
- [x] Empty state handled outside Virtualize grid (`!isLoading && _totalCount == 0`)
- [x] `LoadDataAsync()` now only loads lookup data (eventTypes, formats, categories, tags, madhabs, locations, registrationModes, languages)

### 3.4 Verify & Document
- [x] Build passes: `dotnet build Explore.Blazor.Client` — 0 errors, 1 pre-existing warning
- [ ] Manual test: scroll loads new batches seamlessly
- [ ] Manual test: filter resets and reloads correctly
- [ ] Manual test: skeleton placeholders visible during load
- [x] Pattern documented for rollout (Virtualize + ItemsProvider + RefreshDataAsync)

## Phase 4: Rollout to Card Grid Pages ✅ COMPLETE

### 4.1 MyEvents.razor + MyEvents.razor.cs
- [x] Added `@using Microsoft.AspNetCore.Components.Web.Virtualization`
- [x] Replaced `@foreach (var evt in FilteredEvents)` + MudPagination with `<Virtualize Items="@AllFilteredEvents" Context="evt" OverscanCount="4">`
- [x] Added `<Placeholder>` skeleton cards inside `<Virtualize>`
- [x] Removed MudPagination, "Showing X-Y of Z" text — replaced with simple count
- [x] Removed pagination fields: `_currentPage`, `ItemsPerPage`, `_cachedFilteredEvents`, `_filtersDirty`, `_cachedUniqueEventTypes`, `_eventTypesDirty`
- [x] Removed pagination methods: `InvalidateFilterCache`, `InvalidateAllCaches`, `ComputeFilteredEvents`, `FilteredEvents` (paged), `TotalPages`, `OnPageChanged`, `ToggleOrganizationFilter`, `ToggleCategoryFilter`
- [x] Simplified `AllFilteredEvents` and `UniqueEventTypes` to direct expression-bodied properties
- [x] Simplified filter handlers (OnSearch, OnCategoryChanged, OnOrganizationChanged) — no more `_currentPage = 1;` or cache invalidation
- [x] Build verified: 0 errors

### 4.2 MyOrganizations.razor
- [x] Added `@using Microsoft.AspNetCore.Components.Web.Virtualization`
- [x] Replaced `@foreach (var org in FilteredOrganizations)` with `<Virtualize Items="@FilteredOrganizations.ToList()" Context="org" OverscanCount="4">`
- [x] Added `<Placeholder>` skeleton cards
- [x] Simplified count text (removed "Showing X of Y")
- [x] No code-behind changes needed (FilteredOrganizations already clean)
- [x] Build verified: 0 errors

### 4.3 MyRegistrations.razor
- [x] Added `@using Microsoft.AspNetCore.Components.Web.Virtualization`
- [x] Replaced `@foreach (var reg in FilteredRegistrations)` with `<Virtualize Items="@FilteredRegistrations.ToList()" Context="reg" OverscanCount="4">`
- [x] Added `<Placeholder>` skeleton cards
- [x] No code-behind changes needed
- [x] Build verified: 0 errors

### 4.4 OrganizationReviews.razor
- [x] Added `@using Microsoft.AspNetCore.Components.Web.Virtualization`
- [x] Moved empty state check outside `<MudGrid>` for cleaner structure
- [x] Replaced `@foreach (var review in _reviews)` with `<Virtualize Items="@_reviews" Context="review" OverscanCount="4">`
- [x] Added `<Placeholder>` skeleton cards with avatar + text placeholders
- [x] No code-behind changes needed (inline @code block, data already loaded)
- [x] Build verified: 0 errors

### 4.5 General
- [x] All 4 pages use `<Virtualize Items="...">` (client-side) — appropriate for small user-owned datasets
- [x] All pages have `<Placeholder>` templates with skeleton loading patterns
- [x] Final build: `dotnet build Explore.Blazor.Client` — 0 errors, 273 pre-existing warnings
- [ ] Landing page sections → Keep limited preview, ensure paged API calls (deferred)

## Phase 5: Admin Table Pagination Removal ✅ COMPLETE

**Approach changed**: Admin lookup tables are small datasets (<100 rows). MudDataGrid VirtualizeServerData would be overkill. Instead, removed `MudTablePager` so MudTable shows all rows inline — no pagination buttons.

- [x] `Categories.razor` → Removed `<PagerContent><MudTablePager /></PagerContent>`
- [x] `Tags.razor` → Removed `<PagerContent><MudTablePager /></PagerContent>`
- [x] `Locations.razor` → Removed `<PagerContent><MudTablePager /></PagerContent>`
- [x] `EventStatuses.razor` → Removed `<PagerContent><MudTablePager /></PagerContent>`
- [x] Other admin tables (Languages, Madhabs, EventTypes, EventFormats, AudienceAges, AudienceGenders) already had no pager — no changes needed
- [x] Verified: zero `MudTablePager` or `MudPagination` remaining in any source .razor file
- [x] Build verified: 0 errors

## Phase 6: Cleanup & Validation ✅ COMPLETE

- [x] Verified no `MudTablePager` or `MudPagination` exists in any source page
- [x] Dead pagination code removed from MyEvents.razor.cs (Phase 4.1)
- [x] `dotnet build Explore.Blazor.Client` passes: 0 errors
- [x] Dev docs updated with final session progress
- [ ] Manual UI validation across all migrated pages (user testing)
- [ ] Remove old non-paged service methods (future cleanup, not blocking)
