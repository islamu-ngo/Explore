ABOUTME: Current context snapshot for the Blazor performance track after a documentation-only repo audit.
ABOUTME: Captures verified shipped features, stale assumptions removed, and the remaining work focus.

# Blazor Performance Optimization - Context

## SESSION PROGRESS (2026-04-07)

### Completed This Session
- Performed a documentation-only audit of the live repo and the `dev/active/blazor-performance` task folder
- Confirmed the old plan was materially stale and removed outdated baseline assumptions from the active docs
- Verified that several optimizations already exist in code:
  - both Blazor projects target `net10.0`
  - BFF/API-facing `HttpClient` registrations already use resilience handlers in `Explore.Blazor/Extensions/HttpClientExtensions.cs`
  - `System.Text.Json` generation/config is already in place (`nswag.json`, `AppJsonSerializerContext.cs`)
  - virtualization exists on multiple list pages
  - `ShouldRender()` exists on event aspect cards
  - `StreamRendering` exists on at least some pages
  - `[PersistentState]` is already in use in multiple pages
  - lazy assembly loading infrastructure exists
- Confirmed key remaining gaps:
  - no general client-side HTTP response cache beyond lookup data
  - no verified request deduplication layer
  - no verified client read-path ETag usage
  - lifecycle/disposal work is incomplete
  - Blazor-specific observability remains unplanned in code for this track

### Not Done This Session
- No application code changes
- No implementation of caching, render-mode, lifecycle, or observability improvements
- No full test sweep; only the earlier build verification was used as repo-state evidence for this documentation refresh

### Current Status
- The track is **not** a blank-slate optimization effort anymore
- The track is **not** ready to jump straight into the old Phase 4 wording without first acknowledging the repo’s newer baseline
- The immediate next step is to resume from the refreshed task list, starting with re-validation of the rendering items whose earlier completion notes drifted

---

## Verified Repo Evidence

### Core Files
- `Explore.Blazor/Explore.Blazor.csproj` - `net10.0`
- `Explore.Blazor.Client/Explore.Blazor.Client.csproj` - `net10.0`, `WasmStripILAfterAOT` currently `false`
- `Explore.Blazor/Extensions/HttpClientExtensions.cs` - interactive/admin/background resilience handlers
- `Explore.Blazor.Client/nswag.json` - `SystemTextJson` generation
- `Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs` - source-generated serialization context
- `Explore.Blazor.Client/Services/LookupCacheService.cs` - lookup-only cache, `Dispose()` still unimplemented
- `Explore.Blazor.Client/Services/RuntimeRenderPolicyService.cs` - runtime route-group render policy decisions
- `Explore.Blazor.Client/Routes.razor` - Blazouter route definitions
- `Explore.Blazor/Components/App.razor` - runtime render-mode resolution path

### Verified Optimization Evidence
- `Virtualize` currently appears in:
  - `Pages/Events/EventList.razor`
  - `Pages/Events/MyEvents.razor`
  - `Pages/Organizations/MyOrganizations.razor`
  - `Pages/Organizations/OrganizationReviews.razor`
  - `Pages/User/MyRegistrations.razor`
- `ShouldRender()` currently appears in:
  - `Pages/Events/Components/EventIslamicAspectCard.razor`
  - `Pages/Events/Components/EventTechAspectCard.razor`
- `StreamRendering` verified at least in:
  - `Pages/Organizations/OrganizationProfile.razor`
  - `Pages/Admin/AdminListDetails.razor`
- `[PersistentState]` verified in multiple pages, including:
  - `Pages/Events/EventList.razor.cs`
  - `Pages/Events/EventDetail.razor.cs`
  - `Pages/Home.razor`
  - `Pages/HomeStart.razor`
  - `Pages/Landing/LandingPageForUsers.razor.cs`
  - `Pages/Landing/LandingPageForNonUsers.razor.cs`
  - organization pages

---

## Important Corrections To Older Context

- The track should no longer describe the Blazor projects as `net9.0`
- The track should no longer claim there is zero virtualization, zero stream rendering, zero resilience, or zero persistent state usage
- The track should no longer treat `WasmStripILAfterAOT=false` as automatically unfinished work; the repo documents that setting as an intentional safety tradeoff
- The track should no longer present old completion counts for rendering tasks as settled fact without re-auditing current code

---

## Remaining Work Focus

### Highest-Value Remaining Work
1. Re-validate rendering checklist items whose prior completion notes drifted (`StateHasChanged`, `@key`, `StreamRendering`)
2. Add general client-side response caching and request deduplication
3. Add client-side conditional GET / ETag support where the API already supports it
4. Reconcile the render-mode plan with the existing runtime policy service and per-page `@rendermode` usage
5. Audit lifecycle/disposal responsibilities and fix known gaps
6. Add Blazor-specific observability for meaningful before/after measurement

### Lower-Risk / Later Work
- JS interop micro-optimization after the larger caching/render/lifecycle wins are settled

---

## Constraints

- This repo uses a BFF architecture; client and server optimizations should not be duplicated thoughtlessly
- NSwag output remains generated code; do not plan around manual edits there
- Build/test claims should be refreshed from live runs when implementation resumes; this documentation refresh intentionally avoided inventing new test numbers it did not execute
- No scripts were introduced during this documentation refresh

---

## Quick Resume

To continue this track:
1. Read this context file and `blazor-performance-tasks.md`
2. Start with the re-validation items for rendering drift
3. Then move to client-side caching/deduplication
4. Re-run build/tests at the point implementation resumes
