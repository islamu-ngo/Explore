# Tri-State Tag Filter Dropdown — Task Checklist

**Last Updated**: 2026-02-23 (v3 — live badge, two-level reset, AND/OR modes, no backward compat)

---

## Phase 1: Application Layer — Multi-Tag Query + AND/OR Modes 🔴 NOT STARTED
**Effort: M** | **Dependencies: None** | **Skill: `cqrs-mediatr-guidelines`, `clean-architecture-rules`**

- [ ] **1.1** Create `TagFilterMode` enum
  - File (NEW): `Explore.Application/Specifications/Events/TagFilterMode.cs`
  - Values: `And = 0`, `Or = 1`
  - Acceptance: Compiles

- [ ] **1.2** Extend `EventSubqueryFilter` with 4 multi-tag filter types (remove old `Tag()`)
  - File: `Explore.Application/Specifications/Events/EventSubqueryFilter.cs`
  - **Remove**: `Tag(Guid tagId)` factory method + `Tag` enum value
  - **Add**: `TagsIncludedAll()`, `TagsIncludedAny()`, `TagsExcludedAll()`, `TagsExcludedAny()`
  - **Add**: 4 new enum values in `EventSubqueryFilterType`
  - Acceptance: Compiles; old Tag() gone

- [ ] **1.3** Update repository to apply 4 multi-tag subquery filters
  - File: `Explore.Persistence/Repositories/EventRepository.cs`
  - **Remove**: old `case Tag:` handler
  - **Add**: `TagsIncludedAll` (EXISTS per tag), `TagsIncludedAny` (IN list), `TagsExcludedAny` (NOT IN), `TagsExcludedAll` (NOT ALL)
  - Acceptance: Correct SQL for all 4 modes

- [ ] **1.4** Replace `TagId` with multi-tag + mode params in `GetEventListRequest`
  - File: `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs`
  - **Remove**: `Guid? TagId`
  - **Add**: `List<Guid>? IncludedTagIds`, `List<Guid>? ExcludedTagIds`, `TagFilterMode InclusionMode = And`, `TagFilterMode ExclusionMode = Or`
  - Acceptance: Compiles; old TagId gone

- [ ] **1.5** Update `GetEventListRequestHandler.BuildSpecificationAsync()` for mode-aware multi-tag
  - File: `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`
  - **Remove**: old TagId spec building
  - **Add**: mode-aware branching (And → TagsIncludedAll, Or → TagsIncludedAny, etc.)
  - Acceptance: Handler builds correct spec per mode

- [ ] **1.6** Create `TagTypeWithTagsDto`
  - File (NEW): `Explore.Application/DTOs/TagType/TagTypeWithTagsDto.cs`
  - Properties: `int Id`, `string FullName`, `string? Description`, `List<TagListDto> Tags`
  - Acceptance: Compiles

- [ ] **1.7** Create `GetTagsGroupedByTagTypeRequest` + Handler
  - File (NEW): `Explore.Application/Features/TagTypeTags/Requests/Queries/GetTagsGroupedByTagTypeRequest.cs`
  - File (NEW): `Explore.Application/Features/TagTypeTags/Handlers/Queries/GetTagsGroupedByTagTypeRequestHandler.cs`
  - Returns `List<TagTypeWithTagsDto>`
  - Acceptance: Returns grouped data

- [ ] **1.8** Add AutoMapper profile for `TagTypeWithTagsDto` if needed
  - File: `Explore.Application/Profiles/MappingProfile.cs`
  - Acceptance: Mapping works

- [ ] **1.9** Build and verify: `dotnet build --configuration Release --verbosity quiet`

---

## Phase 2: API Layer — Endpoint Updates 🔴 NOT STARTED
**Effort: S** | **Dependencies: Phase 1** | **Skill: `clean-architecture-rules`**

- [ ] **2.1** Update Events controller — replace `tagId` with multi-tag + mode params
  - File: `Explore.API/Controllers/EventController.cs`
  - **Remove**: `[FromQuery] Guid? tagId`
  - **Add**: `[FromQuery] List<Guid>? includedTagIds`, `excludedTagIds`, `string? inclusionMode`, `string? exclusionMode`
  - Parse mode strings → `TagFilterMode` enum
  - Update endpoint metadata
  - Acceptance: API accepts multi-tag + modes; old tagId removed

- [ ] **2.2** Add grouped tags endpoint to TagType controller
  - File: `Explore.API/Controllers/TagTypeController.cs`
  - `[HttpGet("with-tags")]` `[AllowAnonymous]` → `GetTagsGroupedByTagTypeRequest`
  - Acceptance: `GET /api/tagtypes/with-tags` returns grouped tags

- [ ] **2.3** Build and verify

---

## Phase 3: Blazor Client — Service Layer 🔴 NOT STARTED
**Effort: S** | **Dependencies: Phase 2** | **Skill: `blazor-bff-patterns`**

- [ ] **3.1** Update `EventService` — replace `tagId` with multi-tag + mode params
  - File: `Explore.Blazor.Client/Services/EventService.cs`
  - **Remove**: `Guid? tagId` parameter
  - **Add**: `List<Guid>? includedTagIds`, `excludedTagIds`, `TagFilterMode? inclusionMode`, `exclusionMode`
  - Acceptance: Service passes tags + modes correctly

- [ ] **3.2** Add `GetTagsGroupedByTagTypeAsync()` to TagService
  - File: `Explore.Blazor.Client/Services/TagService.cs`
  - Returns `List<TagTypeWithTagsDto>`
  - Acceptance: Returns grouped tag data

- [ ] **3.3** Regenerate NSwag client if needed
  - File: `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
  - Acceptance: Client includes new endpoints/params; old tagId gone

- [ ] **3.4** Build and verify

---

## Phase 4: Blazor Client — TriStateTagFilterDropdown Component 🔴 NOT STARTED
**Effort: L** | **Dependencies: None (parallel with 1-3)** | **Skill: `blazor-ui-conventions`, `blazor-css-isolation`**

- [ ] **4.1** Create `TagFilterState` enum
  - File (NEW): `Explore.Blazor.Client/Models/TagFilterState.cs`
  - `Neutral = 0`, `Include = 1`, `Exclude = 2`
  - Acceptance: Compiles

- [ ] **4.2** Create `TagFilterChangedEventArgs`
  - File (NEW): `Explore.Blazor.Client/Models/TagFilterChangedEventArgs.cs`
  - `IncludedTagIds`, `ExcludedTagIds`, `InclusionMode`, `ExclusionMode`
  - Acceptance: Compiles

- [ ] **4.3** Create `TriStateTagFilterDropdown.razor.cs` (code-behind)
  - File (NEW): `Explore.Blazor.Client/Components/Event/TriStateTagFilterDropdown.razor.cs`
  - Parameters: `TagGroups`, `OnFilterChanged`
  - State: `_tagStates` dict, `_searchTerm`, `_isOpen`, `_inclusionMode`, `_exclusionMode`
  - Methods:
    - `ToggleTagState(Guid)` — cycles Neutral→Include→Exclude→Neutral
    - `ResetAll()` — **Global**: clears ALL tags to neutral
    - `ClearVisible()` — **Contextual**: clears only visible/searched tags
    - `GetFilteredGroups()` — filters by search term
    - `GetIncludeCount()` / `GetExcludeCount()` — for live badge
    - `OnInclusionModeChanged()` / `OnExclusionModeChanged()`
  - Acceptance: State management correct; badge counts update instantly

- [ ] **4.4** Create `TriStateTagFilterDropdown.razor` (template)
  - File (NEW): `Explore.Blazor.Client/Components/Event/TriStateTagFilterDropdown.razor`
  - **Trigger**: MudButton with live badge text `"Filter Tags +N -M"` (green/red)
  - **Popover**: MudPopover with:
    - Search bar + "Clear Visible" button + "Reset All" button
    - Info text: "Click: include → exclude → clear"
    - Options section: Inclusion mode [AND|OR], Exclusion mode [AND|OR] (MudToggleGroup)
    - Tag groups: MudText headers + MudDivider + flex-wrap MudChips
    - Each MudChip: color/variant/icon changes per state
  - MudOverlay for outside-click-to-close
  - Scrollable: `overflow-y: auto; max-height: 60vh`
  - Acceptance: Renders; tags cycle; badge updates live; both resets work; mode toggles work

- [ ] **4.5** Create `TriStateTagFilterDropdown.razor.css` (scoped styles)
  - File (NEW): `Explore.Blazor.Client/Components/Event/TriStateTagFilterDropdown.razor.css`
  - Popover sizing (min 400px, max 600px), tag spacing, transitions
  - Badge colors (green/red for +/-), `::deep` if needed
  - Theme-aware via CSS variables
  - Acceptance: Scoped; responsive; light+dark mode

- [ ] **4.6** Build and visual test

---

## Phase 5: Integration — Wire Into EventFilterBar + Explicit Search 🔴 NOT STARTED
**Effort: M** | **Dependencies: Phases 1-4**

**CRITICAL**: Refactors EventFilterBar from auto-query to pure state container. No API call on any filter interaction — only on Search button click.

- [ ] **5.1** Refactor EventFilterBar to pure state container (no auto-query)
  - File: `Explore.Blazor.Client/Components/Event/EventFilterBar.razor`
  - **Remove**: `SelectedValuesChanged="OnFilterChange"` from ALL MudSelect
  - **Remove**: `CheckedChanged="OnFilterChange"` from ALL MudCheckBox
  - **Remove**: `TextChanged="OnFilterChange"` from ALL text fields
  - Keep `@bind-Value` for local binding
  - File: `Explore.Blazor.Client/Components/Event/EventFilterBar.razor.cs`
  - **Remove**: `OnFilterChange()` method
  - **Remove**: `[Parameter] EventCallback OnFilterChanged`
  - **Remove**: `Guid? SelectedTagId`
  - Keep `ClearAllFilters()` (local only)
  - Keep `GetActiveFilterCount()`
  - Acceptance: NO filter interaction triggers API call

- [ ] **5.2** Replace tag MudSelect with TriStateTagFilterDropdown in EventFilterBar
  - File: `Explore.Blazor.Client/Components/Event/EventFilterBar.razor` (remove lines 74-82)
  - Add `<TriStateTagFilterDropdown TagGroups="@TagGroups" OnFilterChanged="OnTagFilterChanged" />`
  - File: `Explore.Blazor.Client/Components/Event/EventFilterBar.razor.cs`
  - Add `[Parameter] public List<TagTypeWithTagsDto> TagGroups`
  - Add `List<Guid> IncludedTagIds`, `ExcludedTagIds`, `TagFilterMode InclusionMode`, `ExclusionMode`
  - Add `OnTagFilterChanged(TagFilterChangedEventArgs)` — updates local state only
  - Update `ClearAllFilters()` to reset tag lists + modes
  - Update `GetActiveFilterCount()` to count included + excluded
  - Acceptance: Component renders; badge live; state local only

- [ ] **5.3** Add explicit "Search" button to EventList page
  - File: `Explore.Blazor.Client/Pages/Event/EventList.razor`
  - Add `MudButton` (Color.Primary, Variant.Filled, StartIcon=Search) below EventFilterBar
  - On click → `LoadEventsAsync()` reads state from EventFilterBar ref
  - Remove old `OnFilterChanged` wiring
  - Initial page load → one automatic query with defaults
  - Acceptance: ONLY Search button triggers API calls

- [ ] **5.4** Update EventList to load grouped tags and pass all params
  - File: `Explore.Blazor.Client/Pages/Event/EventList.razor`
  - Load `TagGroups` via `ITagService.GetTagsGroupedByTagTypeAsync()` in parallel
  - Pass `TagGroups` to `<EventFilterBar>`
  - In `LoadEventsAsync()`: read all tag params from filter bar
  - Pass `IncludedTagIds`, `ExcludedTagIds`, `InclusionMode`, `ExclusionMode` to EventService
  - Acceptance: End-to-end works; query only on Search click

- [ ] **5.5** Build and manual test

---

## Phase 6: Testing 🔴 NOT STARTED
**Effort: M** | **Dependencies: Phase 5** | **TDD where possible**

- [ ] **6.1** Unit tests: EventSubqueryFilter 4 multi-tag factories
  - File (NEW): `Event.Application.UnitTests/Specifications/EventSubqueryFilterTests.cs`
  - Test all 4 factory methods create correct filter types
  - Test empty list edge case
  - Acceptance: Tests pass

- [ ] **6.2** Unit tests: GetEventListRequestHandler mode-aware spec building
  - File: `Event.Application.UnitTests/Features/Events/` (new or extend)
  - Test: IncludedTagIds + And → TagsIncludedAll
  - Test: IncludedTagIds + Or → TagsIncludedAny
  - Test: ExcludedTagIds + Or → TagsExcludedAny
  - Test: ExcludedTagIds + And → TagsExcludedAll
  - Test: empty lists → no tag filters
  - Acceptance: Tests pass

- [ ] **6.3** bUnit tests: TriStateTagFilterDropdown
  - File (NEW): `Explore.Blazor.Client.Tests/Components/Event/TriStateTagFilterDropdownTests.cs`
  - Test: renders all tag groups and tags
  - Test: click cycles Neutral → Include → Exclude → Neutral
  - Test: live badge shows correct "+N -M"
  - Test: Global Reset clears ALL tags (including hidden by search)
  - Test: Contextual Clear clears only visible/searched tags
  - Test: Search filters tags within groups
  - Test: AND/OR toggles emit correct mode in event args
  - Test: OnFilterChanged emits correct include/exclude lists + modes
  - Acceptance: All bUnit tests pass

- [ ] **6.4** Integration test: multi-tag API with AND/OR modes
  - File: `Event.API.IntegrationTests/` (extend existing)
  - Test: includedTagIds + AND → events with ALL tags
  - Test: includedTagIds + OR → events with ANY tag
  - Test: excludedTagIds + OR → events without ANY tag
  - Test: excludedTagIds + AND → excludes only if event has ALL excluded tags
  - Test: combined include + exclude
  - Acceptance: Tests pass

- [ ] **6.5** Run full test suite
  ```bash
  dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
  dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
  dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
  dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
  dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
  ```
  - Acceptance: ALL tests pass (existing + new)

---

## Summary

| Phase | Tasks | Effort | Status |
|-------|-------|--------|--------|
| 1. Application Layer (multi-tag + AND/OR) | 9 | M | 🔴 Not Started |
| 2. API Layer | 3 | S | 🔴 Not Started |
| 3. Client Service Layer | 4 | S | 🔴 Not Started |
| 4. UI Component (badge, resets, modes) | 6 | L | 🔴 Not Started |
| 5. Integration + Search Button | 5 | M | 🔴 Not Started |
| 6. Testing | 5 | M | 🔴 Not Started |
| **Total** | **32** | **XL** | |

**Recommended execution order:**
- Phase 1 → 2 → 3 (sequential backend pipeline)
- Phase 4 (parallel with 1-3, pure UI work)
- Phase 5 (after all phases complete)
- Phase 6 (after Phase 5, but TDD tests for Phase 1 can be written first)

---

## Session Checkpoint (2026-02-27 Europe/Brussels)

- [x] Reviewed task continuity status for context reset handoff.
- [ ] Resume implementation work from this task latest documented in-progress section.
- [ ] Re-validate with build/tests once implementation resumes.

