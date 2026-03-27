# Tri-State Tag Filter Dropdown — Implementation Plan

**Last Updated**: 2026-02-23 (v3 — live badge, two-level reset, AND/OR modes, no backward compat)

---

## Executive Summary

Replace the current single-select `MudSelect<Guid?>` tag dropdown in `EventFilterBar` with an advanced **Tri-State Tag Filter Dropdown** component. Tags are grouped by `TagType` (e.g., "Genre", "Format", "Theme") and each tag cycles through three states on click: **Neutral** (no filter) → **Include** (must have) → **Exclude** (must not have). The component uses a `MudPopover` with a search bar, two-level reset, and scrollable tag sections. Users can configure **inclusion mode** (AND/OR) and **exclusion mode** (AND/OR) independently.

**Key constraints**:
- **No auto-query**: All filter state changes are local. API query fires ONLY on explicit "Search" button click in EventList.
- **No backward compatibility**: The existing `Guid? TagId` single-tag filter is removed entirely (not released yet, still in development).
- **Live badge feedback**: The trigger button shows a live "+N -M" count badge updating instantly as tags are toggled, providing immediate visual feedback even before Search is clicked.

### Architecture Impact

- **Domain**: No changes (TagType, Tag, TagTypeTags already exist)
- **Application**: Replace `TagId` with `IncludedTagIds`/`ExcludedTagIds` + `InclusionMode`/`ExclusionMode` in `GetEventListRequest`; extend `EventSubqueryFilter` with 4 filter types (TagsIncludedAll, TagsIncludedAny, TagsExcludedAll, TagsExcludedAny); add `GetTagsGroupedByTagTypeRequest` query
- **API**: Replace `tagId` param with `includedTagIds`, `excludedTagIds`, `inclusionMode`, `exclusionMode`; add grouped tags endpoint
- **Blazor Client**: New `TriStateTagFilterDropdown` component with live badge, two-level reset, AND/OR mode toggles; update `EventFilterBar`; update `EventService`

---

## Current State Analysis

### What Exists

| Component | File | Status |
|-----------|------|--------|
| `Tag` entity | `Explore.Domain/Tag.cs` | Guid PK, FullName, MasterCode, TenantId |
| `TagType` entity | `Explore.Domain/TagType.cs` | int PK, FullName, MasterCode, Description |
| `TagTypeTags` junction | `Explore.Domain/TagTypeTags.cs` | Links Tag ↔ TagType (many-to-many) |
| `EventTags` junction | `Explore.Domain/EventTags.cs` | Links Event ↔ Tag |
| `EventSubqueryFilter.Tag()` | `Explore.Application/Specifications/Events/EventSubqueryFilter.cs` | Single tag filter — **REPLACE** |
| `GetEventListRequest.TagId` | `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs` | Single `Guid?` — **REMOVE** |
| `GetTagsByTagTypeRequest` | `Explore.Application/Features/TagTypeTags/Requests/Queries/GetTagsByTagTypeRequest.cs` | Returns tags for ONE TagType |
| `EventFilterBar` tag select | `Explore.Blazor.Client/Components/Event/EventFilterBar.razor` (line 74-82) | Single `MudSelect<Guid?>` — **REPLACE** |
| `TagService` | `Explore.Blazor.Client/Services/TagService.cs` | CRUD, no grouped fetch |
| `TagTypeController` | `Explore.API/Controllers/TagTypeController.cs` | Exists, needs grouped endpoint |

### What's Missing

1. **Multi-tag include/exclude with AND/OR modes** at API + specification level
2. **Grouped tag fetching** (all tags grouped by their TagTypes in one call)
3. **Tri-state UI component** with live badge, two-level reset, mode toggles
4. **EventService** multi-tag parameter support

---

## Proposed Future State

### Interaction Model: Explicit Search (No Auto-Query)

**CRITICAL DESIGN DECISION**: The filter UI does NOT trigger API queries on every user interaction. Instead:

1. User interacts with filter controls (toggles tags, selects dropdowns, types search) — **all state changes are local only**
2. The trigger button badge updates **instantly** ("+3 -1") as visual feedback that state is changing
3. User clicks an explicit **"Search" button** on the EventList page
4. Only then does the system collect the current filter state and send ONE query to the API

### AND/OR Mode Logic

Users can independently configure how included and excluded tags are combined:

| Mode | Inclusion (AND) | Inclusion (OR) |
|------|----------------|----------------|
| **Meaning** | Event must have **ALL** included tags | Event must have **at least one** included tag |
| **SQL** | `EXISTS(tag1) AND EXISTS(tag2) AND ...` | `EXISTS(tag IN (tag1, tag2, ...))` |
| **Use case** | "Must be both Workshop AND Beginner" | "Either Workshop OR Lecture" |

| Mode | Exclusion (AND) | Exclusion (OR) |
|------|----------------|----------------|
| **Meaning** | Exclude only if event has **ALL** excluded tags simultaneously | Exclude if event has **ANY** excluded tag |
| **SQL** | `NOT (EXISTS(tag1) AND EXISTS(tag2))` | `NOT EXISTS(tag IN (tag1, tag2, ...))` |
| **Use case** | "Exclude only if both Horror AND Gore" | "Exclude anything with Horror or Gore" |

**Defaults**: Inclusion = AND, Exclusion = OR (most intuitive for typical filtering)

### Data Flow (on Search button click)

```
User clicks "Search" button in EventList
  ↓ EventList reads current state from EventFilterBar
EventFilterBar.IncludedTagIds / ExcludedTagIds / InclusionMode / ExclusionMode
  ↓ EventList calls
EventService.GetEventsPagedAsync(..., includedTagIds, excludedTagIds, inclusionMode, exclusionMode)
  ↓ HTTP GET /api/events?includedTagIds=...&excludedTagIds=...&inclusionMode=And&exclusionMode=Or
GetEventListRequest { IncludedTagIds, ExcludedTagIds, InclusionMode, ExclusionMode }
  ↓ handler builds spec based on modes
EventQuerySpecification
  .And(EventSubqueryFilter.TagsIncludedAll(ids))   // if AND mode
  // OR
  .And(EventSubqueryFilter.TagsIncludedAny(ids))   // if OR mode
  ↓ repository applies
PostgreSQL: EXISTS / NOT EXISTS subqueries on EventTags
```

### UI Component Structure

**EventList page layout** (Search button lives here, NOT inside filter components):
```
┌─────────────────────────────────────────────────────────┐
│ <EventFilterBar>                                        │
│   [Date ▼] [Category ▼] [Location ▼] [Format ▼] ...   │
│   [Filter Tags +3 -1 ▼]  ← live badge, updates on click│
│   Islamic / Tech expansion panels...                    │
│   Active filter chips...                                │
├─────────────────────────────────────────────────────────┤
│  [ 🔍 Search ]  ← MudButton in EventList, triggers API │
├─────────────────────────────────────────────────────────┤
│ Event cards grid...                                     │
└─────────────────────────────────────────────────────────┘
```

**TriStateTagFilterDropdown popover** (local state only, no API calls):
```
[Filter Tags +3 -1 ▼] ← MudButton with live "+N -M" badge
┌─────────────────────────────────────────────────────────┐
│ [🔍 Search tags...              ] [Clear Visible][Reset]│
│  Click: include (green) → exclude (red) → clear        │
│─────────────────────────────────────────────────────────│
│ Options                                                 │
│  Inclusion mode: [AND | OR]   Exclusion mode: [AND | OR]│
│─────────────────────────────────────────────────────────│
│ Genre                                                   │
│ ─────────────────────────────────────────               │
│ [✓ Fiction] [Non-Fiction] [✗ Horror] [Drama] [Poetry]  │
│                                                         │
│ Format                                                  │
│ ─────────────────────────────────────────               │
│ [✓ Workshop] [✓ Lecture] [Panel] [Seminar]             │
│                                                         │
│ Theme                                                   │
│ ─────────────────────────────────────────               │
│ [Education] [Technology] [Culture] [Health]             │
└─────────────────────────────────────────────────────────┘

Tag States (visual only — NO API call on state change):
  [Tag]       → Neutral: Default surface color, outlined variant
  [✓ Tag]     → Include: Color.Success, filled variant, check icon
  [✗ Tag]     → Exclude: Color.Error, filled variant, close icon

Two-Level Reset:
  [Reset]         → Global: clears ALL tags back to neutral
  [Clear Visible] → Contextual: clears only tags visible in current search results
```

---

## Implementation Phases

### Phase 1: Application Layer — Multi-Tag Query Support (Effort: M)

**Goal**: Enable the specification + handler to filter by multiple included/excluded tags with AND/OR modes.

#### Task 1.1: Create `TagFilterMode` enum in Application layer
- **File (new)**: `Explore.Application/Specifications/Events/TagFilterMode.cs`
- Values: `And = 0`, `Or = 1`
- **Acceptance**: Enum compiles

#### Task 1.2: Extend `EventSubqueryFilter` with 4 multi-tag filter types
- **File**: `Explore.Application/Specifications/Events/EventSubqueryFilter.cs`
- Remove existing `Tag(Guid tagId)` single-tag factory method (no backward compat)
- Remove `Tag` from `EventSubqueryFilterType` enum
- Add four new factory methods:
  - `TagsIncludedAll(List<Guid> tagIds)` — event must have ALL included tags
  - `TagsIncludedAny(List<Guid> tagIds)` — event must have at least one included tag
  - `TagsExcludedAll(List<Guid> tagIds)` — exclude only if event has ALL excluded tags
  - `TagsExcludedAny(List<Guid> tagIds)` — exclude if event has ANY excluded tag
- Add corresponding enum values: `TagsIncludedAll`, `TagsIncludedAny`, `TagsExcludedAll`, `TagsExcludedAny`
- **Acceptance**: New factory methods compile; old `Tag()` removed

#### Task 1.3: Update repository to apply multi-tag subquery filters
- **File**: `Explore.Persistence/Repositories/EventRepository.cs` (where subquery filters are applied)
- Remove old `case Tag:` handler
- Add four new cases:
  - `TagsIncludedAll`: correlated EXISTS per tag (AND — one subquery per tag ID)
  - `TagsIncludedAny`: single `WHERE EventTags.TagId IN (...)` (OR)
  - `TagsExcludedAll`: `NOT (EXISTS(tag1) AND EXISTS(tag2) AND ...)`
  - `TagsExcludedAny`: `NOT EXISTS(TagId IN (...))`
- **Acceptance**: Correct SQL generated for all 4 modes

#### Task 1.4: Replace `TagId` with multi-tag params in `GetEventListRequest`
- **File**: `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs`
- **Remove** `Guid? TagId`
- **Add**:
  - `List<Guid>? IncludedTagIds { get; set; }`
  - `List<Guid>? ExcludedTagIds { get; set; }`
  - `TagFilterMode InclusionMode { get; set; } = TagFilterMode.And;`
  - `TagFilterMode ExclusionMode { get; set; } = TagFilterMode.Or;`
- **Acceptance**: Request compiles; old TagId fully removed

#### Task 1.5: Update `GetEventListRequestHandler.BuildSpecificationAsync()` for multi-tag
- **File**: `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`
- Remove old `TagId` spec building
- Add mode-aware spec building:
  - If `IncludedTagIds` has items → check `InclusionMode` → call `TagsIncludedAll()` or `TagsIncludedAny()`
  - If `ExcludedTagIds` has items → check `ExclusionMode` → call `TagsExcludedAll()` or `TagsExcludedAny()`
- **Acceptance**: Handler builds correct spec based on mode

#### Task 1.6: Create `TagTypeWithTagsDto`
- **File (new)**: `Explore.Application/DTOs/TagType/TagTypeWithTagsDto.cs`
- Properties: `int Id`, `string FullName`, `string? Description`, `List<TagListDto> Tags`
- **Acceptance**: DTO compiles

#### Task 1.7: Create `GetTagsGroupedByTagTypeRequest` + Handler
- **File (new)**: `Explore.Application/Features/TagTypeTags/Requests/Queries/GetTagsGroupedByTagTypeRequest.cs`
- **File (new)**: `Explore.Application/Features/TagTypeTags/Handlers/Queries/GetTagsGroupedByTagTypeRequestHandler.cs`
- Handler fetches all TagTypes, then for each loads tags via `ITagTypeTagsRepository`
- Returns `List<TagTypeWithTagsDto>`
- **Acceptance**: Returns grouped data; compiles

#### Task 1.8: Add AutoMapper profile for `TagTypeWithTagsDto` if needed
- **File**: `Explore.Application/Profiles/MappingProfile.cs`
- **Acceptance**: Mapping works

#### Task 1.9: Build and verify: `dotnet build --configuration Release --verbosity quiet`

---

### Phase 2: API Layer — Endpoint Updates (Effort: S)

#### Task 2.1: Update Events controller — replace `tagId` with multi-tag params
- **File**: `Explore.API/Controllers/EventController.cs`
- **Remove** `[FromQuery] Guid? tagId`
- **Add** `[FromQuery] List<Guid>? includedTagIds`, `[FromQuery] List<Guid>? excludedTagIds`
- **Add** `[FromQuery] string? inclusionMode` (default "And"), `[FromQuery] string? exclusionMode` (default "Or")
- Parse mode strings to `TagFilterMode` enum
- Map to `GetEventListRequest`
- Update endpoint metadata attributes
- **Acceptance**: API accepts multi-tag params with modes; old `tagId` removed

#### Task 2.2: Add grouped tags endpoint to TagType controller
- **File**: `Explore.API/Controllers/TagTypeController.cs`
- `[HttpGet("with-tags")]` `[AllowAnonymous]` → dispatches `GetTagsGroupedByTagTypeRequest`
- **Acceptance**: `GET /api/tagtypes/with-tags` returns `List<TagTypeWithTagsDto>`

#### Task 2.3: Build and verify

---

### Phase 3: Blazor Client — Service Layer (Effort: S)

#### Task 3.1: Update `EventService` — replace `tagId` with multi-tag params
- **File**: `Explore.Blazor.Client/Services/EventService.cs`
- **Remove** `Guid? tagId` parameter
- **Add** `List<Guid>? includedTagIds`, `List<Guid>? excludedTagIds`, `TagFilterMode? inclusionMode`, `TagFilterMode? exclusionMode`
- Map to NSwag client call
- **Acceptance**: Service passes tag lists and modes correctly

#### Task 3.2: Add `GetTagsGroupedByTagTypeAsync()` to TagService
- **File**: `Explore.Blazor.Client/Services/TagService.cs`
- New method calling NSwag client for `/api/tagtypes/with-tags`
- Returns `List<TagTypeWithTagsDto>`
- **Acceptance**: Returns grouped tag data

#### Task 3.3: Regenerate NSwag client if needed
- **File**: `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- **Acceptance**: Client includes new endpoints/params; old `tagId` param gone

#### Task 3.4: Build and verify

---

### Phase 4: Blazor Client — TriStateTagFilterDropdown Component (Effort: L)

This is the core UI component. Can be built in parallel with Phases 1-3.

#### Task 4.1: Create `TagFilterState` enum
- **File (new)**: `Explore.Blazor.Client/Models/TagFilterState.cs`
- `Neutral = 0`, `Include = 1`, `Exclude = 2`
- **Acceptance**: Compiles

#### Task 4.2: Create `TagFilterChangedEventArgs`
- **File (new)**: `Explore.Blazor.Client/Models/TagFilterChangedEventArgs.cs`
- Properties:
  - `List<Guid> IncludedTagIds`
  - `List<Guid> ExcludedTagIds`
  - `TagFilterMode InclusionMode`
  - `TagFilterMode ExclusionMode`
- **Acceptance**: Compiles

#### Task 4.3: Create `TriStateTagFilterDropdown.razor.cs` (code-behind)
- **File (new)**: `Explore.Blazor.Client/Components/Event/TriStateTagFilterDropdown.razor.cs`
- **Parameters**:
  - `[Parameter] List<TagTypeWithTagsDto> TagGroups` — data source
  - `[Parameter] EventCallback<TagFilterChangedEventArgs> OnFilterChanged` — emits include/exclude lists + modes (updates local parent state only)
- **State**:
  - `Dictionary<Guid, TagFilterState> _tagStates` — tracks state of every tag
  - `string _searchTerm` — for filtering visible tags in popover
  - `bool _isOpen` — popover visibility
  - `TagFilterMode _inclusionMode = TagFilterMode.And` — AND/OR for included tags
  - `TagFilterMode _exclusionMode = TagFilterMode.Or` — AND/OR for excluded tags
- **Methods**:
  - `ToggleTagState(Guid tagId)` — cycles Neutral → Include → Exclude → Neutral; emits `OnFilterChanged`
  - `ResetAll()` — **Global Reset**: sets ALL tags to Neutral regardless of search/visibility
  - `ClearVisible()` — **Contextual Clear**: sets only currently visible/filtered tags to Neutral
  - `GetFilteredGroups()` — returns tag groups filtered by `_searchTerm` (hides non-matching tags within groups, hides empty groups)
  - `GetIncludedTagIds()` / `GetExcludedTagIds()` — extracts lists from state dict
  - `GetIncludeCount()` / `GetExcludeCount()` — counts for the live badge
  - `OnInclusionModeChanged()` / `OnExclusionModeChanged()` — emits `OnFilterChanged` with new mode
- **Acceptance**: All methods compile; state management works correctly; badge counts update instantly

#### Task 4.4: Create `TriStateTagFilterDropdown.razor` (template)
- **File (new)**: `Explore.Blazor.Client/Components/Event/TriStateTagFilterDropdown.razor`
- Structure:
  1. **Trigger Button**: `MudButton` with dynamic text:
     - No active tags: `"Filter Tags"`
     - Active tags: `"Filter Tags +3 -1"` (green "+N" for includes, red "-M" for excludes)
     - Updates **instantly** on every tag toggle (no API call, just local state)
  2. **MudPopover** anchored to button, `AnchorOrigin.BottomLeft`, `TransformOrigin.TopLeft`
  3. Inside popover:
     - **Top row**: `MudTextField` search + `MudButton` "Clear Visible" + `MudButton` "Reset All"
     - **Info line**: `MudText Typo.caption`: "Click: include → exclude → clear"
     - **Options section**:
       - `MudText Typo.subtitle2` "Options" header
       - `MudDivider`
       - Two `MudToggleGroup<TagFilterMode>` side by side:
         - "Inclusion mode:" [AND] [OR]
         - "Exclusion mode:" [AND] [OR]
     - **Tag sections**: For each TagType group (from `GetFilteredGroups()`):
       - `MudText Typo.subtitle2` header (e.g., "Genre")
       - `MudDivider`
       - `div.d-flex.flex-wrap.gap-2` containing tags as `MudChip` elements
     - Each `MudChip`:
       - `OnClick` → `ToggleTagState(tag.Id)`
       - Neutral: `Color.Default`, `Variant.Outlined`, no icon
       - Include: `Color.Success`, `Variant.Filled`, `Icon=Icons.Material.Filled.Check`
       - Exclude: `Color.Error`, `Variant.Filled`, `Icon=Icons.Material.Filled.Close`
  4. `MudOverlay` behind popover to close on outside click
- Use `overflow-y: auto; max-height: 60vh` on popover content for scroll
- **Acceptance**: Component renders; tags cycle through states; badge updates live; both resets work; AND/OR toggles work

#### Task 4.5: Create `TriStateTagFilterDropdown.razor.css` (scoped styles)
- **File (new)**: `Explore.Blazor.Client/Components/Event/TriStateTagFilterDropdown.razor.css`
- Styles for:
  - Popover min-width (400px) and max-width (600px)
  - Tag group spacing and margins
  - Smooth transitions on tag state change (opacity/color)
  - Badge styling for "+N -M" counts (green/red colors)
  - Use `::deep` for MudChip inner styling if needed
  - Theme-aware colors (leverage CSS variables from MudBlazor theme)
- **Acceptance**: Styles scoped; no global leaks; theme-aware; responsive

#### Task 4.6: Build and visual test

---

### Phase 5: Integration — Wire Into EventFilterBar + Explicit Search (Effort: M)

**CRITICAL**: This phase also refactors EventFilterBar away from auto-querying. Currently, every `MudSelect` fires `SelectedValuesChanged="OnFilterChange"` which immediately invokes `OnFilterChanged` on the parent, triggering an API call. The new pattern:
- Filter controls update **local state only** (no callback per interaction)
- A **"Search" button** in EventList collects the current filter state and triggers the query
- This applies to ALL filters (dropdowns, checkboxes, tri-state tags) — not just the new tag component

#### Task 5.1: Refactor EventFilterBar to be a pure state container (no auto-query)
- **File**: `Explore.Blazor.Client/Components/Event/EventFilterBar.razor`
- **Remove** `SelectedValuesChanged="OnFilterChange"` from ALL MudSelect elements
- **Remove** `CheckedChanged="OnFilterChange"` from all MudCheckBox elements
- **Remove** `TextChanged="OnFilterChange"` from text fields
- Keep `@bind-Value` for local two-way binding
- **File**: `Explore.Blazor.Client/Components/Event/EventFilterBar.razor.cs`
- **Remove** `OnFilterChange()` method
- **Remove** `[Parameter] EventCallback OnFilterChanged` parameter
- **Remove** `Guid? SelectedTagId` (replaced by multi-tag)
- Keep `ClearAllFilters()` (resets local state only, no API call)
- Keep `GetActiveFilterCount()` (reads local state)
- **Acceptance**: Changing any filter does NOT trigger an API call; state is purely local

#### Task 5.2: Replace tag MudSelect with TriStateTagFilterDropdown in EventFilterBar
- **File**: `Explore.Blazor.Client/Components/Event/EventFilterBar.razor` (remove lines 74-82)
- Add `<TriStateTagFilterDropdown TagGroups="@TagGroups" OnFilterChanged="OnTagFilterChanged" />`
- **File**: `Explore.Blazor.Client/Components/Event/EventFilterBar.razor.cs`
- Add `[Parameter] public List<TagTypeWithTagsDto> TagGroups { get; set; }`
- Add `List<Guid> IncludedTagIds` and `List<Guid> ExcludedTagIds` properties
- Add `TagFilterMode InclusionMode` and `TagFilterMode ExclusionMode` properties
- Add `OnTagFilterChanged(TagFilterChangedEventArgs args)` — updates local lists only
- Update `ClearAllFilters()` to reset tag lists and modes
- Update `GetActiveFilterCount()` to count included + excluded tags
- **Acceptance**: Tag component integrated; badge updates live; state is local only

#### Task 5.3: Add "Search" button to EventList page
- **File**: `Explore.Blazor.Client/Pages/Event/EventList.razor`
- Add `MudButton` (Color.Primary, Variant.Filled, StartIcon=Search) below EventFilterBar
- On click → calls `LoadEventsAsync()` reading all state from EventFilterBar ref
- Remove old `OnFilterChanged` wiring
- Initial page load still triggers one automatic query with default filters
- **Acceptance**: ONLY the Search button triggers API calls; filter interactions are silent

#### Task 5.4: Update EventList to load grouped tag data and pass multi-tag params
- **File**: `Explore.Blazor.Client/Pages/Event/EventList.razor`
- Load `TagGroups` via `ITagService.GetTagsGroupedByTagTypeAsync()` in parallel
- Pass `TagGroups` to `<EventFilterBar>`
- In `LoadEventsAsync()`: read `IncludedTagIds`, `ExcludedTagIds`, `InclusionMode`, `ExclusionMode` from filter bar
- Pass all to `EventService.GetEventsPagedAsync()`
- **Acceptance**: Full end-to-end flow works; query only fires on Search click

#### Task 5.5: Build and manual test

---

### Phase 6: Testing (Effort: M)

#### Task 6.1: Unit tests for multi-tag specification filters
- **File (new)**: `Event.Application.UnitTests/Specifications/EventSubqueryFilterTests.cs`
- Test all 4 factory methods create correct filter types
- Test empty list edge case
- **Acceptance**: All tests pass

#### Task 6.2: Unit tests for `GetEventListRequestHandler` multi-tag spec building
- **File**: `Event.Application.UnitTests/Features/Events/` (new or extend)
- Test: IncludedTagIds + InclusionMode.And → TagsIncludedAll
- Test: IncludedTagIds + InclusionMode.Or → TagsIncludedAny
- Test: ExcludedTagIds + ExclusionMode.And → TagsExcludedAll
- Test: ExcludedTagIds + ExclusionMode.Or → TagsExcludedAny
- Test: empty lists → no tag filters added
- **Acceptance**: All tests pass

#### Task 6.3: bUnit tests for `TriStateTagFilterDropdown`
- **File (new)**: `Explore.Blazor.Client.Tests/Components/Event/TriStateTagFilterDropdownTests.cs`
- Test: renders all tag groups and tags
- Test: click cycles Neutral → Include → Exclude → Neutral
- Test: badge shows correct "+N -M" counts
- Test: Global Reset clears ALL tags
- Test: Contextual Clear clears only visible/searched tags
- Test: Search filters visible tags within groups
- Test: AND/OR mode toggles emit correct mode
- Test: OnFilterChanged emits correct include/exclude lists + modes
- **Acceptance**: All bUnit tests pass

#### Task 6.4: Integration test for multi-tag API endpoint
- **File**: `Event.API.IntegrationTests/` (extend existing)
- Test: includedTagIds with AND mode → events with ALL tags
- Test: includedTagIds with OR mode → events with ANY tag
- Test: excludedTagIds with OR mode → events without ANY tag
- Test: excludedTagIds with AND mode → events without ALL tags simultaneously
- Test: combined include + exclude
- **Acceptance**: All tests pass

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **TagsIncludedAll** may be slow with many tags | Medium | High | Use correlated EXISTS subqueries (one per tag) rather than LINQ `.All()` which may generate poor SQL. Benchmark with 5+ included tags. |
| **NSwag client** may not serialize `List<Guid>` query params correctly | Medium | Medium | Test API binding; may need repeated param format (`?includedTagIds=a&includedTagIds=b`). |
| **AND/OR SQL complexity** for ExclusionMode.And | Medium | Medium | `NOT (EXISTS(tag1) AND EXISTS(tag2))` is logically equivalent to `NOT EXISTS(tag1) OR NOT EXISTS(tag2)` — test which generates better SQL. |
| **Popover positioning** on small screens | Medium | Medium | Use `MudPopover` with `OverflowBehavior.FlipOnOpen` and `max-height: 60vh`. Test on mobile. |
| **Tags without TagType** may be orphaned | Low | Low | Add "Uncategorized" fallback group in UI for tags not in any TagType |
| **Contextual Clear with hidden tags** | Low | Low | Clear only tags where `_searchTerm` matches name; non-matching tags retain their state. Edge case: user clears, then removes search → previously hidden tags still set. This is correct behavior. |

---

## Success Metrics

1. Users can include/exclude multiple tags simultaneously with AND/OR modes
2. Tag groups render organized by TagType with search functionality
3. Live "+N -M" badge provides instant feedback without API calls
4. Global Reset clears all tags; Contextual Clear clears only visible tags
5. AND/OR mode toggles correctly change query semantics
6. Performance: grouped tag endpoint responds < 200ms
7. All existing tests pass; new tests cover all 4 filter modes + UI interactions
8. Component respects MudBlazor theming (works in both light/dark mode)
9. Mobile-responsive: popover scrollable, tags wrap correctly

---

## Effort Summary

| Phase | Description | Effort | Dependencies |
|-------|-------------|--------|-------------|
| 1 | Application Layer — Multi-Tag Query + AND/OR | M | None |
| 2 | API Layer — Endpoint Updates | S | Phase 1 |
| 3 | Blazor Client — Service Layer | S | Phase 2 |
| 4 | Blazor Client — UI Component (badge, resets, modes) | L | None (parallel with 1-3) |
| 5 | Integration + Search Button Refactor | M | Phases 1-4 |
| 6 | Testing | M | Phase 5 |
| **Total** | | **XL** | |

**Recommended execution**: Phase 1 → 2 → 3 (sequential backend). Phase 4 (parallel with 1-3, pure UI). Phase 5 (after all). Phase 6 (after 5).

---

## Old `TagId` References to Remove (Full Trace)

Every reference to the old single-tag `TagId` pattern that must be removed:

| File | Line(s) | What to Remove/Replace |
|------|---------|----------------------|
| `Explore.Application/Specifications/Events/EventSubqueryFilter.cs` | ~42-43 | `Tag(Guid tagId)` factory + `Tag` enum value |
| `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs` | ~36 | `Guid? TagId` property |
| `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs` | ~118-119 | `if (request.TagId.HasValue)` spec building block |
| `Explore.Persistence/Repositories/EventRepository.cs` | ~224-226 | `EventSubqueryFilterType.Tag =>` switch arm |
| `Explore.API/Controllers/EventController.cs` | ~70, ~110 | `[FromQuery] Guid? tagId` param + `TagId = tagId` mapping |
| `Explore.Blazor.Client/Clients/EventApiClient.g.cs` | ~373, ~5199 | NSwag-generated — will auto-update on regen |
| `Explore.Blazor.Client/Components/Event/EventFilterBar.razor` | ~74-82 | `MudSelect<Guid?>` for Tags |
| `Explore.Blazor.Client/Components/Event/EventFilterBar.razor.cs` | ~36 | `Guid? SelectedTagId` property |

All of these are removed — NOT deprecated, NOT kept for backward compat. Clean break.

---

## Potential Risks & Unknowns (Self-Critique)

**Most likely failure point: Task 1.3 — Repository SQL generation for `TagsExcludedAll`**

The `TagsExcludedAll` mode ("exclude only if event has ALL excluded tags simultaneously") is the most complex SQL to generate correctly through EF Core LINQ. The expression `!allExclude.All(tid => _dbContext.EventTags.Any(...))` may not translate cleanly to SQL — EF Core historically struggles with `List<T>.All()` inside `Where()` clauses. The fallback is to construct it as `query.Where(e => !(EXISTS(tag1) AND EXISTS(tag2)))` using a manual loop of `.Where()` calls combined differently than the AND-inclusion pattern. This needs careful SQL inspection during Phase 1.3.

**Second risk: NSwag `List<Guid>` serialization format**

ASP.NET Core supports two formats for collection query params: repeated (`?includedTagIds=a&includedTagIds=b`) and comma-separated (`?includedTagIds=a,b`). NSwag generates client code based on the swagger spec — if the spec declares it as `array`, NSwag may serialize as repeated params but the server may expect comma-separated or vice versa. This will surface immediately when testing Phase 3 against Phase 2, so it's catchable early, but it could require a custom `[ModelBinder]` or a swagger annotation to align formats.

**Third risk: Contextual Clear edge case with MudBlazor search**

The `ClearVisible()` method needs to know which tags are "currently visible" based on `_searchTerm`. If the search implementation uses `string.Contains()` case-insensitively, tags with unicode or accented characters (common in Islamic/Arabic context) may behave unexpectedly. Should use `StringComparison.OrdinalIgnoreCase` or culture-aware comparison.

**Low risk but worth noting: MudToggleGroup for AND/OR**

`MudToggleGroup<TagFilterMode>` might not exist as a direct component in MudBlazor. May need to use `MudButtonGroup` with manual toggle state, or two `MudChip` elements acting as radio buttons. Check MudBlazor docs during Phase 4 implementation — if `MudToggleGroup` doesn't exist or doesn't fit, use a `MudSelect<TagFilterMode>` dropdown with two options as fallback.
