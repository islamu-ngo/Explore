ABOUTME: Active checklist for the Blazor performance track based on the audited repo state.
ABOUTME: Distinguishes verified completed work from re-validation items and the remaining implementation backlog.

# Blazor Performance Optimization - Task Checklist

## Phase 1: Platform Baseline (`net10.0`) ✅ VERIFIED COMPLETE
- [x] 1.1: Verify `Explore.Blazor.csproj` targets `net10.0`
- [x] 1.2: Verify `Explore.Blazor.Client.csproj` targets `net10.0`
- [x] 1.3: Verify `WasmStripILAfterAOT` intent is documented (`false` today for runtime-safety reasons, not an unreviewed omission)
- [x] 1.4: Verify static asset fingerprinting/platform upgrades are no longer blocked on framework version
- [x] 1.5: Confirm the old `net9.0` baseline is removed from active planning
- [x] 1.6: Keep MudBlazor compatibility as a normal verification concern when future implementation resumes

## Phase 2: Rendering Optimization ⚠️ PARTIALLY VERIFIED / RE-AUDIT NEEDED
- [ ] 2.1: Re-audit remaining `StateHasChanged()` usage and document which calls are still justified
- [ ] 2.2: Re-audit `@key` coverage for the current `@foreach` inventory
- [x] 2.3: Verify `ShouldRender()` exists on expensive event aspect components
- [x] 2.4: Verify virtualization is already in use where the repo currently applies it
- [ ] 2.5: Re-audit `StreamRendering` coverage and refresh claims with current evidence
- [x] 2.6: Verify `[PersistentState]` is already implemented in multiple pages

## Phase 3: Bundle Size / Serialization Baseline ✅ VERIFIED COMPLETE
- [x] 3.1: Verify NSwag is configured for `System.Text.Json`
- [x] 3.2: Verify the client no longer depends on the old Newtonsoft-based baseline from the original plan
- [x] 3.3: Verify the generated client and serialization pipeline align with STJ
- [x] 3.4: Verify source-generated JSON context exists for the client
- [x] 3.5: Verify lazy-loading infrastructure files exist
- [x] 3.6: Keep assembly-splitting/lazy-load activation as future work, not missing infrastructure
- [x] 3.7: Keep generated-client size concerns as an optimization topic, not a proof that Phase 3 never happened

## Phase 4: Client-Side Caching & API Optimization (NEXT HIGH-VALUE WORK)
- [ ] 4.1: Implement a general client-side HTTP response cache beyond lookup/reference data
- [ ] 4.2: Implement request deduplication for concurrent identical reads
- [ ] 4.3: Add client-side conditional GET support using API ETags / `If-None-Match`
- [ ] 4.4: Upgrade `LookupCacheService` invalidation and disposal behavior
- [ ] 4.5: Decide whether extra WASM-side resilience is still needed given the current BFF/server resilience layer

## Phase 5: Render-Mode Optimization
- [ ] 5.1: Audit current per-page `@rendermode` usage versus `RuntimeRenderPolicyService`
- [ ] 5.2: Document intended render-mode strategy by route group (public SEO, operational, admin, onboarding)
- [ ] 5.3: Implement any remaining render-mode cleanup after the audit confirms the desired authority model

## Phase 6: JS Interop Optimization
- [ ] 6.1: Inventory `IJSRuntime` usage and identify meaningful hotspots
- [ ] 6.2: Batch or reduce sequential JS interop where it materially improves UX
- [ ] 6.3: Evaluate .NET 10 direct property access only where it replaces a real hot-path cost

## Phase 7: Memory Management & Lifecycle
- [ ] 7.1: Audit components/services for timers, subscriptions, JS handles, and disposable primitives
- [ ] 7.2: Apply `IDisposable` / `IAsyncDisposable` where required
- [ ] 7.3: Fix known lifecycle gaps, including `LookupCacheService.Dispose()`
- [ ] 7.4: Review whether Blazor Server circuit configuration needs explicit tuning for this app

## Phase 8: Observability & Measurement
- [ ] 8.1: Add Blazor-specific render/circuit metrics where practical
- [ ] 8.2: Add navigation/performance markers for key user journeys
- [ ] 8.3: Define the minimal reporting/dashboard needed to evaluate performance changes

---

## Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| 1. Platform Baseline | 6 | ✅ Verified Complete |
| 2. Rendering | 6 | ⚠️ 3 verified, 3 need re-audit |
| 3. Bundle Size / Serialization | 7 | ✅ Verified Complete |
| 4. Client Caching | 5 | ⏳ Not Started |
| 5. Render Modes | 3 | ⏳ Not Started |
| 6. JS Interop | 3 | ⏳ Not Started |
| 7. Memory / Lifecycle | 4 | ⏳ Not Started |
| 8. Observability | 3 | ⏳ Not Started |
| **Total** | **37** | **16 verified complete / 21 remaining** |

## Current Resume Order
1. Re-audit Phase 2 drift items (`StateHasChanged`, `@key`, `StreamRendering`)
2. Implement Phase 4 caching/deduplication/ETag work
3. Reconcile Phase 5 render-mode behavior with existing runtime policy infrastructure
4. Tackle lifecycle and observability follow-up
