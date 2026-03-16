# Tri-State Tag Filter Dropdown — Implementation Plan

**Last Updated**: 2026-03-10 (v5 — All Phases Complete)

---

## Executive Summary

Replace the current single-select `MudSelect<Guid?>` tag dropdown in `EventFilterBar` with an advanced **Tri-State Tag Filter Dropdown** component. Tags are grouped by `TagType` (e.g., "Genre", "Format", "Theme") and each tag cycles through three states on click: **Neutral** (no filter) → **Include** (must have) → **Exclude** (must not have). The component uses a `MudPopover` with a search bar, two-level reset, and scrollable tag sections. Users can configure **inclusion mode** (AND/OR) and **exclusion mode** (AND/OR) independently.

**Status**: Implementation complete. Integration verified. Testing complete and passing.

**Key constraints**:
- **No auto-query**: All filter state changes are local. API query fires ONLY on explicit "Search" button click in EventList.
- **No backward compatibility**: The existing `Guid? TagId` single-tag filter is removed entirely.
- **Live badge feedback**: The trigger button shows a live "+N -M" count badge updating instantly as tags are toggled.

---

## Final State Analysis (As of 2026-03-10)

### Implementation Status

| Component | File | Status |
|-----------|------|--------|
| `TagFilterMode` enum | `Explore.Application/Specifications/Events/TagFilterMode.cs` | ✅ Done |
| `EventSubqueryFilter` | `Explore.Application/Specifications/Events/EventSubqueryFilter.cs` | ✅ Multi-tag modes added |
| `GetEventListRequest` | `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs` | ✅ Updated to multi-tag |
| `TagTypeController` | `Explore.API/Controllers/TagTypeController.cs` | ✅ Grouped tags endpoint added |
| `TriStateTagFilterDropdown` | `Explore.Blazor.Client/Pages/Events/Components/TriStateTagFilterDropdown.razor` | ✅ UI implemented |
| `EventFilterBar` | `Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor` | ✅ Wired to new component |
| `EventList` | `Explore.Blazor.Client/Pages/Events/EventList.razor` | ✅ Search button refactor complete |
| `EventRepository` | `Explore.Persistence/Repositories/EventRepository.cs` | ✅ Refactored for robust SQL generation |

---

## Implementation Phases (Status)

### ✅ Phase 1: Application Layer — Multi-Tag Query Support
- Enabled specification + handler to filter by multiple included/excluded tags with AND/OR modes.
- Created `TagFilterMode` enum.
- Extended `EventSubqueryFilter` with 4 multi-tag types.
- Updated repository for subquery application. **Refactored ExcludedAll filters to use Count-based logic for EF provider compatibility.**
- Grouped tag fetching (GetTagsGroupedByTagTypeRequest) implemented.

### ✅ Phase 2: API Layer — Endpoint Updates
- `EventController` updated to accept multi-tag parameters.
- `TagTypeController` now provides `GET /api/tagtypes/with-tags`.

### ✅ Phase 3: Blazor Client — Service Layer
- `EventService` and `TagService` updated.
- NSwag client regenerated.

### ✅ Phase 4: Blazor Client — TriStateTagFilterDropdown Component
- Full UI component with live badge, search, two-level reset, and mode toggles.
- Scoped CSS for layout and state transitions.

### ✅ Phase 5: Integration — Wire Into EventFilterBar + Explicit Search
- `EventFilterBar` refactored to pure state container (no auto-query on interaction).
- Explicit **Search** button added to `EventList` page.
- End-to-end flow verified: query only fires on Search button click.

### ✅ Phase 6: Testing
- **Specification Tests**: Verified all 4 factory methods.
- **Handler Tests**: Verified mode-aware specification building.
- **Component Tests**: bUnit tests for state cycling, badge updates, and reset functionality.
- **API Integration Tests**: Verified end-to-end multi-tag filtering with AND/OR combinations using isolated test host.
- **Infrastructure Fixes**: Resolved EF InMemory issues for complex subqueries and tenant resolution in test host.
