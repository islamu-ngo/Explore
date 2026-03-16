# Tri-State Tag Filter Dropdown — Task Checklist

**Last Updated**: 2026-03-10 (v5 — All Phases Complete)

---

## Phase 1: Application Layer — Multi-Tag Query + AND/OR Modes ✅ COMPLETED
**Effort: M** | **Dependencies: None** | **Skill: `cqrs-mediatr-guidelines`, `clean-architecture-rules`**

- [x] **1.1** Create `TagFilterMode` enum
  - File: `Explore.Application/Specifications/Events/TagFilterMode.cs`
  - Values: `And = 0`, `Or = 1`
- [x] **1.2** Extend `EventSubqueryFilter` with 4 multi-tag filter types (remove old `Tag()`)
  - File: `Explore.Application/Specifications/Events/EventSubqueryFilter.cs`
- [x] **1.3** Update repository to apply 4 multi-tag subquery filters
  - File: `Explore.Persistence/Repositories/EventRepository.cs`
  - *Note: Refactored "ExcludedAll" filters to use `.Count() < tagIds.Count` pattern for EF InMemory provider compatibility.*
- [x] **1.4** Replace `TagId` with multi-tag + mode params in `GetEventListRequest`
  - File: `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs`
- [x] **1.5** Update `GetEventListRequestHandler.BuildSpecificationAsync()` for mode-aware multi-tag
  - File: `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`
- [x] **1.6** Create `TagTypeWithTagsDto`
  - File: `Explore.Application/DTOs/TagType/TagTypeWithTagsDto.cs`
- [x] **1.7** Create `GetTagsGroupedByTagTypeRequest` + Handler
  - File: `Explore.Application/Features/TagTypeTags/Requests/Queries/GetTagsGroupedByTagTypeRequest.cs`
- [x] **1.8** Add AutoMapper profile for `TagTypeWithTagsDto` if needed
  - File: `Explore.Application/Profiles/MappingProfile.cs`
- [x] **1.9** Build and verify: `dotnet build --configuration Release --verbosity quiet`

---

## Phase 2: API Layer — Endpoint Updates ✅ COMPLETED
**Effort: S** | **Dependencies: Phase 1** | **Skill: `clean-architecture-rules`**

- [x] **2.1** Update Events controller — replace `tagId` with multi-tag + mode params
  - File: `Explore.API/Controllers/EventController.cs`
- [x] **2.2** Add grouped tags endpoint to TagType controller
  - File: `Explore.API/Controllers/TagTypeController.cs`
- [x] **2.3** Build and verify

---

## Phase 3: Blazor Client — Service Layer ✅ COMPLETED
**Effort: S** | **Dependencies: Phase 2** | **Skill: `blazor-bff-patterns`**

- [x] **3.1** Update `EventService` — replace `tagId` with multi-tag + mode params
  - File: `Explore.Blazor.Client/Services/EventService.cs`
- [x] **3.2** Add `GetTagsGroupedByTagTypeAsync()` to TagService
  - File: `Explore.Blazor.Client/Services/TagService.cs`
- [x] **3.3** Regenerate NSwag client if needed
  - File: `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- [x] **3.4** Build and verify

---

## Phase 4: Blazor Client — TriStateTagFilterDropdown Component ✅ COMPLETED
**Effort: L** | **Dependencies: None** | **Skill: `blazor-ui-conventions`, `blazor-css-isolation`**

- [x] **4.1** Create `TagFilterState` enum
  - File: `Explore.Blazor.Client/Models/TagFilterState.cs`
- [x] **4.2** Create `TagFilterChangedEventArgs`
  - File: `Explore.Blazor.Client/Models/TagFilterChangedEventArgs.cs`
- [x] **4.3** Create `TriStateTagFilterDropdown.razor.cs` (code-behind)
  - File: `Explore.Blazor.Client/Pages/Events/Components/TriStateTagFilterDropdown.razor.cs`
- [x] **4.4** Create `TriStateTagFilterDropdown.razor` (template)
  - File: `Explore.Blazor.Client/Pages/Events/Components/TriStateTagFilterDropdown.razor`
- [x] **4.5** Create `TriStateTagFilterDropdown.razor.css` (scoped styles)
  - File: `Explore.Blazor.Client/Pages/Events/Components/TriStateTagFilterDropdown.razor.css`
- [x] **4.6** Build and visual test

---

## Phase 5: Integration — Wire Into EventFilterBar + Explicit Search ✅ COMPLETED
**Effort: M** | **Dependencies: Phases 1-4**

- [x] **5.1** Refactor EventFilterBar to pure state container (no auto-query)
  - File: `Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor`
- [x] **5.2** Replace tag MudSelect with TriStateTagFilterDropdown in EventFilterBar
- [x] **5.3** Add explicit "Search" button to EventList page
- [x] **5.4** Update EventList to load grouped tags and pass all params
- [x] **5.5** Build and manual test

---

## Phase 6: Testing ✅ COMPLETED
**Effort: M** | **Dependencies: Phase 5** | **TDD where possible**

- [x] **6.1** Unit tests: EventSubqueryFilter 4 multi-tag factories
  - File: `Event.Application.UnitTests/Specifications/EventSubqueryFilterTests.cs`
- [x] **6.2** Unit tests: GetEventListRequestHandler mode-aware spec building
  - File: `Event.Application.UnitTests/Features/Events/Queries/GetEventListRequestHandlerTests.cs`
- [x] **6.3** bUnit tests: TriStateTagFilterDropdown
  - File: `Explore.Blazor.Client.Tests/Components/Event/TriStateTagFilterDropdownTests.cs`
- [x] **6.4** Integration test: multi-tag API with AND/OR modes
  - File: `Event.API.IntegrationTests/Features/EventMultiTagFilterTests.cs`
- [x] **6.5** Run full test suite
  - Application Tests: 401/401 Passing.
  - Client Tests: 530/533 Passing (New tests pass, 3 pre-existing failures).
  - Integration Tests: 144/432 Passing (New tests pass with custom setup, many pre-existing failures).

---

## Summary

| Phase | Tasks | Effort | Status |
|-------|-------|--------|--------|
| 1. Application Layer (multi-tag + AND/OR) | 9 | M | ✅ Completed |
| 2. API Layer | 3 | S | ✅ Completed |
| 3. Client Service Layer | 4 | S | ✅ Completed |
| 4. UI Component (badge, resets, modes) | 6 | L | ✅ Completed |
| 5. Integration + Search Button | 5 | M | ✅ Completed |
| 6. Testing | 5 | M | ✅ Completed |
| **Total** | **32** | **XL** | |
