# Blazor Refactoring: Performance Dependencies (D.1)

**Last Updated: 2026-02-15**

---

## Scope Split

This note separates optimization work into:

- **Blazor-only improvements**: can be delivered without API contract changes.
- **API-coupled improvements**: blocked until backend query/filter contracts evolve.

---

## Blazor-Only (Can Ship Now)

1. Keep Virtualize-driven paging in `EventList` and avoid client-side preloading of large collections.
2. Keep lookup caching (`LookupCacheService`) for static/semi-static dropdown data.
3. Keep deterministic loading/error/empty UX states to prevent perceived performance regressions.
4. Continue targeted render-path tests for high-traffic auth-sensitive pages.

These items remain in Blazor refactoring scope and do not require API epic work.

---

## API-Coupled (Blocked by Contract Shape)

### 1) Rich Filtering + Search Scalability

Current client calls already pass many optional filters via `IEventService.GetEventsPagedAsync(...)`, but scalability and precision are constrained by server query/index behavior.

Blocked improvements:

- Full-text ranking/weighted search across title/description/organization fields.
- Stable server-defined sort semantics across mixed filter combinations.
- Cursor/keyset pagination option for very large datasets (offset paging is currently used).

### 2) Organization Listing Query Efficiency

`MyOrganizations` and related pages depend on API-side shaping for scalable filtering/sorting.

Blocked improvements:

- Dedicated server-side search/sort parameters for organization-member views.
- Optional projection/lightweight list endpoints for card-list rendering.

### 3) Aggregates/Counts for Dashboard-Like Surfaces

Landing/admin-like experiences may require aggregate endpoints to avoid repeated list fetches.

Blocked improvements:

- Lightweight count endpoints for reusable cards/stats.
- Query contracts that return filtered counts without full collection fetch.

---

## Proposed API Epic Reference (Activation on Approval)

Use this as the handoff reference when API work is approved:

- **Epic Title**: `API Query Contract Expansion for Blazor Performance`
- **Epic Goal**: add server-side query/filter/sort/pagination contracts that unblock high-scale Blazor list performance.
- **Candidate deliverables**:
  1. Event query contract review + index-aligned filter semantics.
  2. Organization member list query parameters (search/sort/page).
  3. Aggregate/count endpoints for dashboard and landing scenarios.
  4. Contract tests to keep API + Blazor expectations synchronized.

Status: **Prepared reference only; activation requires product/implementation approval.**

---

## Verification Anchors

- `Explore.Blazor.Client/Pages/Event/EventList.razor.cs`
- `Explore.Blazor.Client/Pages/Organization/MyOrganizations.razor.cs`
- `Explore.Blazor.Client/Services/EventService.cs`
- `Explore.Blazor.Client/Services/OrganizationService.cs`
- `Explore.Blazor.Client/Services/LookupCacheService.cs`
