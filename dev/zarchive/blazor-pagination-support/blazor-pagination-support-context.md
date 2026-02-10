# Blazor Infinite Scroll + Modular Aspect-Aware Query Specification - Context

**Last Updated**: 2026-02-10 11:30 CET

## SESSION PROGRESS (2026-02-10 — Session 6)

### COMPLETED THIS SESSION

**Phase 4: Rollout to Card Grid Pages — COMPLETE**

- MyEvents.razor — Replaced `@foreach` + MudPagination with `<Virtualize Items="@AllFilteredEvents">`, removed all pagination state/logic
- MyOrganizations.razor — Replaced `@foreach` with `<Virtualize Items="@FilteredOrganizations.ToList()">`
- MyRegistrations.razor — Replaced `@foreach` with `<Virtualize Items="@FilteredRegistrations.ToList()">`
- OrganizationReviews.razor — Replaced `@foreach` with `<Virtualize Items="@_reviews">`
- All 4 pages have `<Placeholder>` skeleton cards

**Phase 5: Admin Table Pagination Removal — COMPLETE**

- Removed `<PagerContent><MudTablePager /></PagerContent>` from: Categories, Tags, Locations, EventStatuses
- Other admin tables (Languages, Madhabs, EventTypes, EventFormats, AudienceAges, AudienceGenders) already had no pager
- Approach: small lookup datasets (<100 rows) don't need server-side virtualization — just show all rows inline

**Phase 6: Cleanup & Verification — COMPLETE**

- Verified zero `MudTablePager` or `MudPagination` in any source .razor file
- Build verified: 0 errors

### COMPLETED PREVIOUSLY (Sessions 1-5)

**Phase 3: NSwag + Blazor EventList Infinite Scroll — COMPLETE (Session 5)**
- Fixed NSwag breaking changes, user regenerated client with 35 query params
- Rewrote EventList.razor → `<Virtualize ItemsProvider="LoadEventsAsync">` with server-side paging

### COMPLETED PREVIOUSLY (Sessions 1-4)

**Phase 1: Infrastructure (Service Layer + Models) — COMPLETE**
- Created PaginatedResult<T> model in Blazor.Client/Models/
- Added 8 ToPaginatedResult() extension methods to HalResourceExtensions.cs
- Added paged methods to all 6 services (Event, Organization, Category, Tag, Location, EventRegistration)
- Build verified: Blazor.Client 0 errors

**Phase 2A-2E: Server-Side IQuerySpecification Pattern (Core) — COMPLETE**
- Created IFilterSpecification<T>, ISortSpecification<T>, IQuerySpecification<T> interfaces
- Created EventFilter (12 filters), EventSubqueryFilter (5 junction table filters), EventSort (4 sorts)
- Created EventQuerySpecification (immutable fluent builder)
- Updated GetEventListRequest (14 core filter + 2 sort params)
- Updated GetEventListRequestHandler (BuildSpecification, cache key)
- Updated IEventRepository + EventRepository (overload with spec, ApplySubqueryFilters)
- Updated EventController (16 FromQuery params)
- Build verified: Application, Persistence, API — all 0 errors

**Phase 2.5: Deep Research for Modular Aspects — COMPLETE**
- Read ALL docs/ + entity files + NSwag client + module governance
- Researched EF Core JSONB querying + pgvector patterns

**Phase 2.6: Modular Aspect-Aware Specification Pattern — COMPLETE**
- Created IslamicAspectFilter, TechAspectFilter, AspectPresenceFilter
- Updated EventSubqueryFilter (+2 JSONB), EventQuerySpecification, Handler, Repository, Controller
- Build verified: Application, Persistence, API — all 0 errors

### IN PROGRESS
- Nothing — ALL PHASES COMPLETE

### BLOCKERS
- None

### REMAINING (Optional Future Work)
- Manual UI validation across all migrated pages
- Remove old non-paged service methods (future cleanup, not blocking)
- Landing page sections: keep limited preview, ensure paged API calls (deferred)

---

## Key Architecture Decisions

### Infinite Scroll Strategy
- **Public events (large dataset)**: `<Virtualize ItemsProvider="...">` — server-side paging via IQuerySpecification
- **User-owned pages (small datasets)**: `<Virtualize Items="...">` — client-side, all data loaded at once
- **Admin tables (small lookup data)**: `MudTable` without pager — shows all rows inline, no pagination buttons
- **Lookup dropdowns**: Keep full fetch (`pageSize=100`) — small datasets for selects

### Aspect Filter Strategy (Phase 2.6)
- **Aspect filters use Event navigation properties** (`e.IslamicAspect.MadhabId`) — NOT subqueries
- EF Core generates efficient LEFT JOIN + WHERE, since repo already does `.Include(e => e.IslamicAspect)`
- This means `IslamicAspectFilter`, `TechAspectFilter`, `AspectPresenceFilter` are regular `IFilterSpecification<Event>` predicates
- They compose identically to `EventFilter` in the spec builder

### JSONB Filters are Subquery-Type
- `MetadataJson` is `string?` mapped as `jsonb` — can't use typed property access
- JSONB filters use `EF.Functions.JsonContains()` and `EF.Functions.JsonExists()` from Npgsql
- These need DbContext-level access, so they're `EventSubqueryFilter` entries applied in the repository

### Module-Conditional Composition
- Handler checks `IModuleService.IsModuleEnabledAsync(tenantId, "Mod_Islamic")` ONLY when Islamic filter params are present
- Same for Tech module — lazy check avoids wasted DB calls
- If module is disabled, aspect filter params are silently ignored (not an error)
- `ITenantContext.TenantId` provides current tenant ID from request context

### pgvector Readiness (NOT YET IMPLEMENTED)
- Event entity does NOT have an Embedding column yet — this is FUTURE work
- The spec pattern is designed to accommodate `VectorFilter` as another filter type
- When ready: add `Vector? Embedding` to Event, create `VectorFilter.SimilarTo(embedding, topK)`, add to EventSubqueryFilterType
- Npgsql supports pgvector via `Pgvector.EntityFrameworkCore` — HNSW indexes, CosineDistance, L2Distance

### No EF Migration Needed for Phase 2.6
- All changes are Application-layer query logic
- No new entities, columns, or schema changes
- JSONB column and aspect tables already exist

---

## Key Entity Structures (for quick reference)

### Event Entity (Event.cs)
- Core FKs: EventTypeId, EventFormatId, MadhabId, AudienceGenderId, AudienceAgeId, EventStatusId, VisibilityTypeId, ActorId
- Core fields: Title, Description, FirstSessionDate, LastSessionDate, Price, TotalViews, IsRegistrationRequired, Slug
- Aspect navs: `IslamicAspect?` (1:1 shared PK), `TechAspect?` (1:1 shared PK)
- JSONB: `MetadataJson` (string?, HasColumnType("jsonb"))
- Note: Event has NO `ICollection<EventSession>` navigation — subqueries go through DbContext.EventSessions directly

### EventIslamicAspect (shared PK = EventId)
- MadhabId (int?), GenderMode (GenderSegregationMode enum: Mixed=0, MenOnly=1, WomenOnly=2, Segregated=3, Family=4)
- IncludesQuranRecitation (bool), ReferencePrayer (PrayerTime? enum: Fajr=1..Isha=6)
- PrayerTimeOffset (int?), PrimaryLanguageId (int?)

### EventTechAspect (shared PK = EventId)
- SkillLevel (SkillLevel enum: AllLevels=0, Beginner=1, Intermediate=2, Advanced=3)
- IsCodingCompetition (bool), HackathonTrack (string?), RequiresLaptop (bool)
- TechStackTags (string?, comma-separated), MaxTeamSize (int?), PrizePool (decimal?)
- GithubRepoUrl (string?), PrizeCurrencyCode (string?)

### Module Governance
- `IModuleService.IsModuleEnabledAsync(tenantId, moduleKey)` — cached (5 min)
- Module keys: "Mod_Islamic", "Mod_Tech"
- `ITenantContext.TenantId` — current tenant from request

---

## Complete File Inventory

### Specification Pattern Files (Application Layer)
```
Explore.Application/Specifications/IFilterSpecification.cs             — Generic filter interface
Explore.Application/Specifications/ISortSpecification.cs               — Generic sort interface
Explore.Application/Specifications/IQuerySpecification.cs              — Generic fluent builder interface
Explore.Application/Specifications/Events/EventFilter.cs               — 12 core event filters
Explore.Application/Specifications/Events/EventSubqueryFilter.cs       — 7 subquery types (5 junction + 2 JSONB)
Explore.Application/Specifications/Events/EventSort.cs                 — 4 sort options
Explore.Application/Specifications/Events/IslamicAspectFilter.cs       — 5 Islamic aspect filters
Explore.Application/Specifications/Events/TechAspectFilter.cs          — 7 Tech aspect filters
Explore.Application/Specifications/Events/AspectPresenceFilter.cs      — 3 aspect presence filters
Explore.Application/Specifications/Events/EventQuerySpecification.cs   — Immutable fluent builder composing all types
```

### CQRS Pipeline Files
```
Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs     — 31 filter + sort properties
Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs — Async spec builder + IModuleService
Explore.Application/Contracts/Persistence/IEventRepository.cs                    — Overload with EventQuerySpecification
Explore.Persistence/Repositories/EventRepository.cs                              — ApplySubqueryFilters (junction + JSONB)
Explore.API/Controllers/EventController.cs                                       — 35 FromQuery params
```

### Blazor Service Layer Files
```
Explore.Blazor.Client/Models/PaginatedResult.cs
Explore.Blazor.Client/Helpers/HalResourceExtensions.cs
Explore.Blazor.Client/Services/EventService.cs
Explore.Blazor.Client/Services/OrganizationService.cs
Explore.Blazor.Client/Services/CategoryService.cs
Explore.Blazor.Client/Services/TagService.cs
Explore.Blazor.Client/Services/LocationService.cs
Explore.Blazor.Client/Services/EventRegistrationService.cs
```

---

## API Endpoint Reference (GET /api/v1/event — 35 params)

### Core Params (always available)
pageNumber, pageSize, searchTerm, categoryId, tagId, formatId, madhabId, locationId, registrationModeId, languageId, dateFrom, dateTo, eventTypeId, audienceGenderId, audienceAgeId, eventStatusId

### Islamic Aspect Params (silently ignored when Mod_Islamic disabled)
genderModeId, includesQuranRecitation, referencePrayerId, islamicPrimaryLanguageId, hasIslamicAspect

### Tech Aspect Params (silently ignored when Mod_Tech disabled)
skillLevelId, isCodingCompetition, isHackathon, requiresLaptop, techStackTag, hasTechAspect

### JSONB Metadata Params (always available)
metadataJsonContains, metadataJsonKeyExists

### Sort Params
sortBy (date|title|views|createdAt), sortDescending

---

## NSwag Client State ✅ REGENERATED

The NSwag client (`EventApiClient.g.cs`) has been regenerated and includes all 35 query params:
```csharp
GetEventsAsync(int? pageNumber, int? pageSize, string? searchTerm, Guid? categoryId, Guid? tagId,
    int? formatId, int? madhabId, Guid? locationId, int? registrationModeId, int? languageId,
    DateTimeOffset? dateFrom, DateTimeOffset? dateTo, int? eventTypeId, int? audienceGenderId,
    int? audienceAgeId, int? eventStatusId, int? genderModeId, bool? includesQuranRecitation,
    int? referencePrayerId, int? islamicPrimaryLanguageId, bool? hasIslamicAspect, int? skillLevelId,
    bool? isCodingCompetition, bool? isHackathon, bool? requiresLaptop, string? techStackTag,
    bool? hasTechAspect, string? metadataJsonContains, string? metadataJsonKeyExists,
    string? sortBy, bool? sortDescending, CancellationToken)
```

---

## Fluent API Usage (Complete Reference)

```csharp
var spec = new EventQuerySpecification()
    .And(EventFilter.SearchTerm("workshop"))
    .And(EventFilter.Format(formatId))
    .And(EventSubqueryFilter.Category(categoryId))
    .And(EventSubqueryFilter.JsonContains("{\"customField\": \"value\"}"))
    .And(AspectPresenceFilter.HasIslamicAspect())
    .And(IslamicAspectFilter.GenderMode(GenderSegregationMode.WomenOnly))
    .And(TechAspectFilter.SkillLevel(SkillLevel.Advanced))
    .SortByDescending(EventSort.Date);
// Applied in repository: spec.Apply(query) + ApplySubqueryFilters(query, spec)
```

---

## Virtualize ItemsProvider Pattern (Reference for Phase 3)

```razor
<Virtualize @ref="_virtualize" ItemsProvider="LoadEventsAsync" OverscanCount="4">
    <ItemContent Context="evt">
        <MudItem xs="12" sm="6" md="4" lg="3">@* Event card *@</MudItem>
    </ItemContent>
    <Placeholder>
        <MudItem xs="12" sm="6" md="4" lg="3">@* Skeleton card *@</MudItem>
    </Placeholder>
</Virtualize>
```

```csharp
private Virtualize<EventListDto>? _virtualize;
private async ValueTask<ItemsProviderResult<EventListDto>> LoadEventsAsync(ItemsProviderRequest request)
{
    var pageSize = Math.Max(request.Count, 20);
    var pageNumber = (request.StartIndex / pageSize) + 1;
    var result = await EventService.GetEventsPagedAsync(pageNumber, pageSize);
    return new ItemsProviderResult<EventListDto>(result.Items, result.TotalCount);
}
private async Task OnFilterChanged()
{
    if (_virtualize is not null) await _virtualize.RefreshDataAsync();
}
```

---

## Build State
- `Explore.Application` — 0 errors, 50 warnings (pre-existing)
- `Explore.Persistence` — 0 errors, 156 warnings (pre-existing, mostly migration files)
- `Explore.API` — 0 errors, 179 warnings (pre-existing)
- `Explore.Blazor.Client` — 0 errors, 1 pre-existing warning (deprecated NuGet package)
- No EF migration needed

---

## Quick Resume for Next Session

**🎉 ALL PHASES COMPLETE** — This task is done.

1. **Phase 1**: Infrastructure (PaginatedResult, ToPaginatedResult, paged service methods) ✅
2. **Phase 2**: Server-side IQuerySpecification (core + modular aspects + JSONB + pgvector-ready) ✅
3. **Phase 3**: NSwag + EventList infinite scroll (`<Virtualize ItemsProvider>`, server-side paging) ✅
4. **Phase 4**: Card grid pages (`<Virtualize Items>`: MyEvents, MyOrganizations, MyRegistrations, OrganizationReviews) ✅
5. **Phase 5**: Admin tables (removed MudTablePager from Categories, Tags, Locations, EventStatuses) ✅
6. **Phase 6**: Cleanup & verification (zero pagination controls remaining, build 0 errors) ✅

**Result**: Zero `MudPagination` or `MudTablePager` controls remain in any source .razor file.
All pages use either server-paged Virtualize (public events) or client-side Virtualize/full-render (user pages, admin tables).

**Optional remaining**: Manual UI validation, remove old non-paged service methods, landing page sections.
