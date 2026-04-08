ABOUTME: Current-state plan for the Blazor performance track after auditing the live repo.
ABOUTME: Documents verified shipped work, stale assumptions removed, and the remaining optimization backlog.

# Blazor Performance Optimization - Current Plan

## Executive Summary

This track is no longer starting from a blank slate. The repo already contains several meaningful Blazor performance improvements: both Blazor projects target `net10.0`, the client has moved to `System.Text.Json`, source-generated serialization is in place, virtualization exists on multiple list pages, `StreamRendering` exists on at least some pages, `PersistentState` is already in use, and the Blazor BFF has resilient `HttpClient` registration via `AddStandardResilienceHandler`.

The remaining work is therefore narrower than the original plan suggested. The highest-value unfinished items are client-side HTTP caching, request deduplication, client use of ETags, render-policy cleanup around the now-existing runtime render policy system, memory/lifecycle auditing, and Blazor-specific observability. A smaller but important documentation problem also remains: older completion notes for parts of rendering optimization drifted away from the current code and need to be treated as re-validation work rather than settled fact.

## Verified Current State

### Already Shipped
- **Platform baseline upgraded**
  - `Explore.Blazor/Explore.Blazor.csproj` targets `net10.0`
  - `Explore.Blazor.Client/Explore.Blazor.Client.csproj` targets `net10.0`
- **Server/BFF resilience exists**
  - `Explore.Blazor/Extensions/HttpClientExtensions.cs` uses `AddStandardResilienceHandler` for interactive, admin, and background HTTP clients
- **Client JSON stack already modernized**
  - `Explore.Blazor.Client/nswag.json` uses `SystemTextJson`
  - `Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs` exists and contains the generated serialization context
  - `Explore.Blazor.Client` no longer carries the old Newtonsoft-based baseline described by the original plan
- **Rendering optimizations already present**
  - `Virtualize` is used in `Pages/Events/EventList.razor`, `Pages/Events/MyEvents.razor`, `Pages/Organizations/MyOrganizations.razor`, `Pages/Organizations/OrganizationReviews.razor`, and `Pages/User/MyRegistrations.razor`
  - `ShouldRender()` exists in `Pages/Events/Components/EventIslamicAspectCard.razor` and `Pages/Events/Components/EventTechAspectCard.razor`
  - `@attribute [StreamRendering]` is present at least in `Pages/Organizations/OrganizationProfile.razor` and `Pages/Admin/AdminListDetails.razor`
  - `[PersistentState]` is present in multiple pages, including `Pages/Events/EventList.razor.cs`, `Pages/Events/EventDetail.razor.cs`, `Pages/Home.razor`, `Pages/HomeStart.razor`, landing pages, and organization pages
- **Lazy-loading groundwork exists**
  - `Explore.Blazor.Client/Contracts/Providers/ILazyAssemblyLoader.cs`
  - `Explore.Blazor.Client/Services/LazyAssemblyLoader.cs`

### Verified Gaps Still Open
- **No general client-side HTTP response cache**
  - `LookupCacheService` only covers lookup/reference data
- **No verified request deduplication layer**
  - no current evidence of a shared in-flight request coalescing mechanism
- **No verified client use of `If-None-Match` / response ETag reuse for API reads**
  - client grep found only upload-response ETag logging in `ImageStorageService`
- **Memory/lifecycle work is incomplete**
  - lifecycle-related patterns exist across the client, but there is no finished audit or cleanup pass proving resources are consistently disposed
  - `LookupCacheService.Dispose()` still throws `NotImplementedException`
- **Blazor-specific observability is still missing**
  - the solution has OpenTelemetry infrastructure, but not a documented Blazor-focused metric set for render/circuit behavior in this track
- **Circuit configuration remains unverified for this performance track**
  - current grep did not find explicit `CircuitOptions` tuning in `Explore.Blazor`

### Documentation Drift To Correct During Implementation
- Older notes claiming **zero virtualization**, **zero stream rendering**, **zero IDisposable/IAsyncDisposable patterns**, or **no resilience** are no longer true
- Older notes claiming rendering work is fully settled are also too confident; some completion counts need fresh validation against the current repo

## Remaining Work Plan

### Phase A: Re-validate Drifted Rendering Checklist
**Priority**: HIGH | **Why first**: the task folder currently overstates completion in a few rendering areas.

- **A.1** Re-audit remaining `StateHasChanged()` usage in `Explore.Blazor.Client`
  - Acceptance: every remaining usage is either removed or explicitly justified in a fresh audit note
- **A.2** Re-audit `@key` coverage for current `@foreach` usage
  - Acceptance: current coverage is documented from the live repo, not inherited from the February snapshot
- **A.3** Re-audit `StreamRendering` coverage and keep only defensible claims
  - Acceptance: docs reflect verified usage and remaining candidates, not stale counts

### Phase 4: Client-Side Caching & API Optimization
**Priority**: HIGHEST | **Why**: the largest remaining end-user win is reducing repeat API work on navigation and reloads.

- **4.1** Implement general client-side HTTP response caching beyond lookup data
  - Acceptance: repeat navigation to the same read-heavy pages can reuse fresh cached responses instead of re-fetching immediately
- **4.2** Add request deduplication for concurrent identical reads
  - Acceptance: overlapping requests to the same resource collapse into one in-flight fetch
- **4.3** Add client-side ETag / `If-None-Match` support for conditional GETs
  - Acceptance: API ETags can prevent unnecessary payload downloads on unchanged responses
- **4.4** Upgrade `LookupCacheService` lifecycle and invalidation behavior
  - Acceptance: cache invalidation is write-aware and disposal is correctly implemented
- **4.5** Decide whether WASM-side resilience is still needed in addition to current BFF/server resilience
  - Acceptance: explicit design note documents whether extra client resilience adds value in this architecture

### Phase 5: Render-Mode Optimization
**Priority**: MEDIUM | **Why**: render-mode work now needs to build on the current runtime policy system, not replace a nonexistent baseline.

- **5.1** Audit how per-page `@rendermode` declarations interact with `RuntimeRenderPolicyService`
  - Acceptance: clear explanation of which mechanism is authoritative per route
- **5.2** Categorize routes by actual render-mode need
  - Acceptance: public SEO, onboarding, admin, and operational routes each have an intentional render decision
- **5.3** Remove plan assumptions that all pages are effectively one-mode today
  - Acceptance: docs and implementation strategy reflect the existing mixed model

### Phase 6: JS Interop Optimization
**Priority**: LOW-MEDIUM | **Why**: likely smaller payoff, but still worth auditing after caching and render policy work.

- **6.1** Inventory current `IJSRuntime` usage and identify hot paths
- **6.2** Batch or reduce sequential JS interop where it materially affects UX
- **6.3** Evaluate .NET 10 direct JS property access only where it removes real overhead

### Phase 7: Memory Management & Lifecycle
**Priority**: MEDIUM-HIGH | **Why**: long-lived interactive sessions need a cleanup pass now that the app contains more interop and cached state than the old plan assumed.

- **7.1** Audit components/services using timers, event subscriptions, JS interop handles, or disposable synchronization primitives
- **7.2** Apply `IDisposable` / `IAsyncDisposable` where required
- **7.3** Fix known lifecycle issues, including `LookupCacheService.Dispose()`
- **7.4** Revisit Blazor Server circuit retention/timeout tuning if current defaults are not acceptable

### Phase 8: Observability & Measurement
**Priority**: MEDIUM | **Why**: this track still lacks a clean measurement layer for Blazor-specific regressions and gains.

- **8.1** Add Blazor-specific metrics for render/circuit behavior where practical
- **8.2** Add user-journey timing markers for key navigation flows
- **8.3** Document the minimal dashboard/reporting needed to tell whether the performance work helped

## Risks And Constraints

- **BFF architecture matters**: some resilience and caching choices belong on the client, some already exist on the server; the plan must not duplicate layers blindly
- **Generated client remains generated**: any change touching NSwag output must avoid manual edits to generated files
- **`WasmStripILAfterAOT` is currently disabled intentionally**: the repo documents runtime-safety concerns, so it should not be treated as unfinished by default
- **Documentation claims must stay evidence-backed**: previous stale counts created confusion; future updates should favor verified file references over hard numbers unless freshly measured

## Recommended Execution Order

1. Re-validate the drifted rendering checklist (Phase A)
2. Deliver client-side caching and deduplication (Phase 4)
3. Reconcile render-mode strategy with the runtime policy system (Phase 5)
4. Complete lifecycle and disposal work (Phase 7)
5. Audit JS interop hotspots if still needed (Phase 6)
6. Add measurement/observability so later work can be judged objectively (Phase 8)
