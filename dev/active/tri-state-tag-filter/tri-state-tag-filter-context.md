# Tri-State Tag Filter Dropdown — Context

**Last Updated**: 2026-02-23 (v3)

---

## SESSION PROGRESS (2026-02-23)

### ✅ COMPLETED
- Research & analysis of current codebase
- MudBlazor component research (MudPopover, MudChipSet, MudChip patterns)
- Plan v1 → v2 → v3
- Phase 1: Application Layer (TagFilterMode, EventSubqueryFilter 4 modes, GetEventListRequest, Handler, Repository)
- Phase 2: API Layer (EventController multi-tag params, TagTypeController with-tags endpoint)
- Phase 3: Blazor Service Layer (EventService, TagService.GetTagsGroupedByTagTypeAsync)
- Phase 4: TriStateTagFilterDropdown UI Component (razor + code-behind + CSS)
- Phase 5: Integration (EventList wired up, EventFilterBar cleaned)
- All 836 tests passing (287 + 32 + 517)

### ⏳ REMAINING
- Phase 6: Testing (bUnit tests for TriStateTagFilterDropdown)
- Visual testing in browser

### ⚠️ BLOCKERS
- None

---

## Key Files

### Domain Layer (No Changes Needed)
- **`Explore.Domain/Tag.cs`** — Tag entity (Guid Id, FullName, MasterCode, TenantId)
- **`Explore.Domain/TagType.cs`** — TagType entity (int Id, FullName, MasterCode, Description)
- **`Explore.Domain/TagTypeTags.cs`** — Junction: Tag ↔ TagType (many-to-many, tenant-scoped)
- **`Explore.Domain/EventTags.cs`** — Junction: Event ↔ Tag (tenant-scoped, auditable)

### Application Layer (Modify — REMOVE old single-tag, ADD multi-tag + modes)
- **`Explore.Application/Specifications/Events/EventSubqueryFilter.cs`** — Remove `Tag()`, add 4 new: `TagsIncludedAll()`, `TagsIncludedAny()`, `TagsExcludedAll()`, `TagsExcludedAny()`
- **`Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs`** — Remove `TagId`, add `IncludedTagIds`, `ExcludedTagIds`, `InclusionMode`, `ExclusionMode`
- **`Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`** — Remove old TagId spec, add mode-aware multi-tag spec building

### Application Layer (New Files)
- **`Explore.Application/Specifications/Events/TagFilterMode.cs`** (NEW) — Enum: `And = 0`, `Or = 1`
- **`Explore.Application/DTOs/TagType/TagTypeWithTagsDto.cs`** (NEW) — DTO with grouped tags
- **`Explore.Application/Features/TagTypeTags/Requests/Queries/GetTagsGroupedByTagTypeRequest.cs`** (NEW)
- **`Explore.Application/Features/TagTypeTags/Handlers/Queries/GetTagsGroupedByTagTypeRequestHandler.cs`** (NEW)

### API Layer (Modify — REMOVE old tagId, ADD multi-tag + modes)
- **`Explore.API/Controllers/EventController.cs`** — Remove `tagId` param, add `includedTagIds`, `excludedTagIds`, `inclusionMode`, `exclusionMode`
- **`Explore.API/Controllers/TagTypeController.cs`** — Add `GET /api/tagtypes/with-tags` grouped endpoint

### Blazor Client Layer (Modify)
- **`Explore.Blazor.Client/Services/EventService.cs`** — Remove `tagId`, add multi-tag + mode params
- **`Explore.Blazor.Client/Services/TagService.cs`** — Add `GetTagsGroupedByTagTypeAsync()`
- **`Explore.Blazor.Client/Components/Event/EventFilterBar.razor`** — Remove auto-query callbacks; replace tag MudSelect with TriStateTagFilterDropdown
- **`Explore.Blazor.Client/Components/Event/EventFilterBar.razor.cs`** — Remove `OnFilterChanged`, `OnFilterChange()`, `SelectedTagId`; add multi-tag state + modes
- **`Explore.Blazor.Client/Pages/Event/EventList.razor`** — Add Search button; remove auto-query wiring; load grouped tags

### Blazor Client Layer (New Files)
- **`Explore.Blazor.Client/Components/Event/TriStateTagFilterDropdown.razor`** (NEW)
- **`Explore.Blazor.Client/Components/Event/TriStateTagFilterDropdown.razor.cs`** (NEW)
- **`Explore.Blazor.Client/Components/Event/TriStateTagFilterDropdown.razor.css`** (NEW)
- **`Explore.Blazor.Client/Models/TagFilterState.cs`** (NEW) — Enum: Neutral/Include/Exclude
- **`Explore.Blazor.Client/Models/TagFilterChangedEventArgs.cs`** (NEW) — Lists + modes

### Repository Layer (Modify)
- **`Explore.Persistence/Repositories/EventRepository.cs`** (line ~218-253) — `ApplySubqueryFilters` switch expression. Remove `EventSubqueryFilterType.Tag` arm (line ~224-226); add 4 new multi-tag arms

### Old TagId References to Remove (Verified Locations)
- `EventSubqueryFilter.cs` line ~42: `Tag(Guid tagId)` factory + `Tag` enum value
- `GetEventListRequest.cs` line ~36: `Guid? TagId` property
- `GetEventListRequestHandler.cs` line ~118-119: `if (request.TagId.HasValue)` block
- `EventRepository.cs` line ~224-226: `EventSubqueryFilterType.Tag =>` switch arm
- `EventController.cs` line ~70: `[FromQuery] Guid? tagId` param; line ~110: `TagId = tagId` mapping
- `EventFilterBar.razor` line ~74-82: `MudSelect<Guid?>` for Tags
- `EventFilterBar.razor.cs` line ~36: `Guid? SelectedTagId` property
- `EventApiClient.g.cs`: auto-regenerated, no manual change needed

---

## Important Decisions

1. **No backward compatibility**: `Guid? TagId` is removed entirely from `GetEventListRequest`, `EventController`, `EventService`, and `EventFilterBar`. The feature isn't released — there's no need to keep it.

2. **Explicit Search button — NO auto-query on filter interaction**: All filter controls update local state only. API is called ONLY when user clicks "Search". The existing `EventFilterBar` auto-query pattern (`SelectedValuesChanged="OnFilterChange"`) is removed from ALL controls in Phase 5.1.

3. **Live "+N -M" badge on trigger button**: Updates instantly on every tag toggle. Green "+N" for includes, red "-M" for excludes. Provides immediate visual feedback that state is changing even though no API call happens.

4. **Two-level reset**:
   - **Global Reset**: Clears ALL tags back to neutral, regardless of search filter or visibility
   - **Contextual Clear**: Clears only tags currently visible in the search results (non-matching tags retain their state)

5. **AND/OR modes for inclusion and exclusion independently**:
   - **Inclusion AND** (default): Event must have ALL included tags
   - **Inclusion OR**: Event must have at least one included tag
   - **Exclusion OR** (default): Exclude if event has ANY excluded tag
   - **Exclusion AND**: Exclude only if event has ALL excluded tags simultaneously

6. **MudPopover over MudMenu**: MudMenu is for simple menu items. MudPopover allows arbitrary content (search bar, grouped sections, custom chips, mode toggles).

7. **Tag styling via MudChip properties**: Use `Color`, `Variant`, and `Icon` props which automatically respect MudBlazor theming. Only use `.razor.css` for layout.

---

## Core Interface Signatures

### TagFilterMode Enum (Application layer)
```csharp
namespace Explore.Application.Specifications.Events;

public enum TagFilterMode { And = 0, Or = 1 }
```

### TagFilterState Enum (Blazor client)
```csharp
public enum TagFilterState { Neutral = 0, Include = 1, Exclude = 2 }
```

### TagFilterChangedEventArgs
```csharp
public class TagFilterChangedEventArgs
{
    public List<Guid> IncludedTagIds { get; set; } = [];
    public List<Guid> ExcludedTagIds { get; set; } = [];
    public TagFilterMode InclusionMode { get; set; } = TagFilterMode.And;
    public TagFilterMode ExclusionMode { get; set; } = TagFilterMode.Or;
}
```

### TagTypeWithTagsDto
```csharp
public class TagTypeWithTagsDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<TagListDto> Tags { get; set; } = [];
}
```

### EventSubqueryFilter extensions (replaces old Tag())
```csharp
// New factory methods (old Tag() removed)
public static EventSubqueryFilter TagsIncludedAll(List<Guid> tagIds) =>
    new(EventSubqueryFilterType.TagsIncludedAll, tagIds);
public static EventSubqueryFilter TagsIncludedAny(List<Guid> tagIds) =>
    new(EventSubqueryFilterType.TagsIncludedAny, tagIds);
public static EventSubqueryFilter TagsExcludedAll(List<Guid> tagIds) =>
    new(EventSubqueryFilterType.TagsExcludedAll, tagIds);
public static EventSubqueryFilter TagsExcludedAny(List<Guid> tagIds) =>
    new(EventSubqueryFilterType.TagsExcludedAny, tagIds);
```

### Repository subquery application logic
```csharp
// TagsIncludedAll: event must have ALL tags (one EXISTS per tag)
case EventSubqueryFilterType.TagsIncludedAll:
    var allInclude = (List<Guid>)filter.Value;
    foreach (var tagId in allInclude)
        query = query.Where(e => _dbContext.EventTags
            .Any(et => et.EventId == e.Id && et.TagId == tagId));
    break;

// TagsIncludedAny: event must have at least one tag
case EventSubqueryFilterType.TagsIncludedAny:
    var anyInclude = (List<Guid>)filter.Value;
    query = query.Where(e => _dbContext.EventTags
        .Any(et => et.EventId == e.Id && anyInclude.Contains(et.TagId)));
    break;

// TagsExcludedAny: exclude events that have ANY of these tags
case EventSubqueryFilterType.TagsExcludedAny:
    var anyExclude = (List<Guid>)filter.Value;
    query = query.Where(e => !_dbContext.EventTags
        .Any(et => et.EventId == e.Id && anyExclude.Contains(et.TagId)));
    break;

// TagsExcludedAll: exclude only if event has ALL of these tags
case EventSubqueryFilterType.TagsExcludedAll:
    var allExclude = (List<Guid>)filter.Value;
    query = query.Where(e => !allExclude.All(tid =>
        _dbContext.EventTags.Any(et => et.EventId == e.Id && et.TagId == tid)));
    break;
```

### Handler mode-aware spec building
```csharp
// In BuildSpecificationAsync():
if (request.IncludedTagIds is { Count: > 0 })
{
    spec = request.InclusionMode == TagFilterMode.And
        ? spec.And(EventSubqueryFilter.TagsIncludedAll(request.IncludedTagIds))
        : spec.And(EventSubqueryFilter.TagsIncludedAny(request.IncludedTagIds));
}

if (request.ExcludedTagIds is { Count: > 0 })
{
    spec = request.ExclusionMode == TagFilterMode.Or
        ? spec.And(EventSubqueryFilter.TagsExcludedAny(request.ExcludedTagIds))
        : spec.And(EventSubqueryFilter.TagsExcludedAll(request.ExcludedTagIds));
}
```

---

## Quick Resume

To continue:
1. Read this context file and the tasks file
2. Start with **Phase 1** (Application layer — remove old TagId, add multi-tag with AND/OR)
3. Phase 4 (UI component) can be built in parallel
4. Run tests at each phase boundary
