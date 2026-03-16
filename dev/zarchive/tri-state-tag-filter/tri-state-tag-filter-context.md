# Tri-State Tag Filter Dropdown — Context

**Last Updated**: 2026-03-10 (v4 — Implementation Complete)

---

## SESSION PROGRESS (2026-03-10)

### ✅ COMPLETED
- Research & analysis of current codebase
- MudBlazor component research (MudPopover, MudChipSet, MudChip patterns)
- Plan v1 → v2 → v3
- Phase 1: Application Layer (TagFilterMode, EventSubqueryFilter 4 modes, GetEventListRequest, Handler, Repository)
- Phase 2: API Layer (EventController multi-tag params, TagTypeController with-tags endpoint)
- Phase 3: Blazor Service Layer (EventService, TagService.GetTagsGroupedByTagTypeAsync)
- Phase 4: TriStateTagFilterDropdown UI Component (razor + code-behind + CSS)
- Phase 5: Integration (EventList wired up with Search button, EventFilterBar refactored to pure state)
- All 836 tests passing (287 + 32 + 517)

### ⏳ REMAINING
- Phase 6: Testing (bUnit tests for TriStateTagFilterDropdown, Unit/Integration tests for multi-tag modes)
- Visual testing in browser

### ⚠️ BLOCKERS
- None

---

## Key Files

### Domain Layer (No Changes Needed)
- **`Explore.Domain/Tag.cs`** — Tag entity
- **`Explore.Domain/TagType.cs`** — TagType entity
- **`Explore.Domain/TagTypeTags.cs`** — Junction: Tag ↔ TagType
- **`Explore.Domain/EventTags.cs`** — Junction: Event ↔ Tag

### Application Layer (Modified)
- **`Explore.Application/Specifications/Events/TagFilterMode.cs`** — Enum: `And = 0`, `Or = 1`
- **`Explore.Application/Specifications/Events/EventSubqueryFilter.cs`** — Multi-tag modes implemented
- **`Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs`** — Multi-tag parameters
- **`Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`** — Mode-aware spec building
- **`Explore.Application/DTOs/TagType/TagTypeWithTagsDto.cs`** — Grouped tags DTO
- **`Explore.Application/Features/TagTypeTags/Handlers/Queries/GetTagsGroupedByTagTypeRequestHandler.cs`** — Grouped tags logic

### API Layer (Modified)
- **`Explore.API/Controllers/EventController.cs`** — Multi-tag parameter support
- **`Explore.API/Controllers/TagTypeController.cs`** — `GET /api/tagtypes/with-tags` grouped endpoint

### Blazor Client Layer (Modified)
- **`Explore.Blazor.Client/Services/EventService.cs`** — Multi-tag parameter support
- **`Explore.Blazor.Client/Services/TagService.cs`** — `GetTagsGroupedByTagTypeAsync()`
- **`Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor`** — Refactored to pure state; integrates `TriStateTagFilterDropdown`
- **`Explore.Blazor.Client/Pages/Events/EventList.razor`** — Explicit Search button; loads grouped tags

### Blazor Client Layer (New Component)
- **`Explore.Blazor.Client/Pages/Events/Components/TriStateTagFilterDropdown.razor`**
- **`Explore.Blazor.Client/Pages/Events/Components/TriStateTagFilterDropdown.razor.cs`**
- **`Explore.Blazor.Client/Pages/Events/Components/TriStateTagFilterDropdown.razor.css`**

### Repository Layer (Modified)
- **`Explore.Persistence/Repositories/EventRepository.cs`** — Multi-tag subquery filters (TagsIncludedAll, TagsIncludedAny, TagsExcludedAll, TagsExcludedAny)

---

## Important Decisions

1. **Explicit Search button — NO auto-query on filter interaction**: API is called ONLY when user clicks "Search".
2. **Live "+N -M" badge on trigger button**: Updates instantly on every tag toggle.
3. **Two-level reset**: Global Reset and Contextual Clear (visible matching tags only).
4. **AND/OR modes**: Inclusion and exclusion modes are independent.
5. **MudPopover over MudMenu**: Provides more control over search and layout.

---

## Quick Resume

To continue:
1. Start with **Phase 6** (Testing)
2. Run bUnit tests for UI component
3. Run Unit tests for Specification factories and Handler logic
4. Run Integration tests for multi-tag API endpoints
