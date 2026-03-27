# Blazor Performance Optimization - Implementation Plan

## Executive Summary

Comprehensive performance optimization of the Blazor frontend (`Explore.Blazor` + `Explore.Blazor.Client`) targeting rendering efficiency, bundle size reduction, memory management, and client-side caching. Both projects currently target net9.0 while the rest of the solution targets net10.0, missing significant .NET 10 Blazor performance improvements.

## Current State Analysis

### Architecture
- **Blazor Hybrid**: Server (`Explore.Blazor`) + WASM Client (`Explore.Blazor.Client`)
- **BFF Pattern**: YARP reverse proxy in Blazor Server -> API (`/api/{**catch-all}`)
- **Auth Flow**: Keycloak OIDC -> Cookie auth on server -> `CircuitAccessTokenService` -> token forwarding via YARP
- **Render Mode**: `InteractiveAuto` on Routes (server-first, then WASM after download)
- **UI Framework**: MudBlazor 8.5.0

### Critical Performance Issues (Ordered by Impact)

| # | Issue | Impact |
|---|-------|--------|
| 1 | **net9.0 targeting** | Missing .NET 10 Blazor perf: 76% smaller blazor.web.js, WasmStripILAfterAOT, [PersistentState] |
| 2 | **38,874-line NSwag client** | Massive WASM bundle bloat, single monolithic file |
| 3 | **Newtonsoft.Json in WASM** | Heavier than System.Text.Json, no AOT optimization |
| 4 | **0 Virtualize components** | 46 @foreach loops rendering all items regardless of count |
| 5 | **27 StateHasChanged() calls** | Potential unnecessary cascading re-renders |
| 6 | **0 StreamRendering** | No `@attribute [StreamRendering]` on any page |
| 7 | **No client-side HTTP caching** | Every navigation re-fetches data from API |
| 8 | **0 IDisposable/IAsyncDisposable** | Potential memory leaks across 127 .razor components |
| 9 | **No Polly resilience** | HttpClient calls have no retry/circuit-breaker |
| 10 | **No request deduplication** | Concurrent identical API requests not merged |

### Key Metrics (Pre-Optimization Baseline)
- **127 .razor files** total across both projects
- **46 @foreach loops** (candidates for virtualization)
- **27 StateHasChanged()** calls (audit needed)
- **38,874 lines** NSwag generated client
- **1 cache service** (LookupCacheService, 10-min MemoryCache TTL)
- **0 Virtualize** components
- **0 StreamRendering** attributes
- **0 IDisposable** implementations on components

---

## Implementation Phases

### Phase 1: Upgrade to .NET 10 + Enable Platform Features
**Priority**: CRITICAL | **Estimated effort**: 2-3 hours
**Why first**: Unblocks all .NET 10 Blazor features used in later phases.

- **Task 1.1**: Upgrade `Explore.Blazor.csproj` from net9.0 to net10.0
  - Acceptance: `<TargetFramework>net10.0</TargetFramework>`, builds clean
- **Task 1.2**: Upgrade `Explore.Blazor.Client.csproj` from net9.0 to net10.0
  - Acceptance: `<TargetFramework>net10.0</TargetFramework>`, builds clean
- **Task 1.3**: Enable `WasmStripILAfterAOT` in Blazor.Client csproj (strips IL after AOT, 20-50% smaller bundle)
  - Acceptance: `<WasmStripILAfterAOT>true</WasmStripILAfterAOT>` in csproj
- **Task 1.4**: Enable static web asset fingerprinting for cache-busting
  - Acceptance: Verify fingerprinting is enabled (default in .NET 10)
- **Task 1.5**: Update any net9.0-specific API calls or breaking changes
  - Acceptance: Full solution builds with 0 errors, 0 warnings from Blazor projects
- **Task 1.6**: Verify MudBlazor 8.5.0 compatibility with net10.0
  - Acceptance: MudBlazor components render correctly after upgrade

**Risk**: Breaking changes between net9.0 and net10.0. Mitigation: Build frequently, fix incrementally.

---

### Phase 2: Rendering Optimization
**Priority**: HIGH | **Estimated effort**: 4-6 hours
**Why**: Eliminates unnecessary re-renders and improves perceived performance.

- **Task 2.1**: Audit all 27 `StateHasChanged()` calls — remove unnecessary ones
  - Acceptance: Each remaining call justified with comment; unnecessary calls removed
- **Task 2.2**: Add `@key` directives to all `@foreach` loops with identifiable items
  - Acceptance: All @foreach loops over entities use `@key="item.Id"` or equivalent
- **Task 2.3**: Implement `ShouldRender()` override on expensive components
  - Acceptance: Components with heavy render trees override ShouldRender with parameter change detection
- **Task 2.4**: Replace large-list `@foreach` loops with `<Virtualize>` or `<MudVirtualize>`
  - Acceptance: Lists that can grow beyond ~50 items use virtualization
  - Note: Only where items have uniform height and virtualization makes UX sense
- **Task 2.5**: Add `@attribute [StreamRendering]` to data-heavy pages
  - Acceptance: Pages that load data show immediate skeleton/loading then stream content
- **Task 2.6**: Implement `[PersistentState]` for component state preservation (net10.0 feature)
  - Acceptance: Key form states survive prerendering without re-fetch

**Risk**: Virtualize requires uniform item height. Some lists may not be suitable.

---

### Phase 3: Bundle Size Reduction (NSwag + Newtonsoft)
**Priority**: HIGH | **Estimated effort**: 3-5 hours
**Why**: 38K-line NSwag client + Newtonsoft.Json is the largest WASM bundle contributor.

- **Task 3.1**: Evaluate NSwag client splitting strategy
  - Option A: Configure NSwag to generate partial classes per controller
  - Option B: Switch to Kiota/openapi-generator with tree-shaking support
  - Acceptance: Decision documented, approach selected
- **Task 3.2**: Migrate HalResourceExtensions from Newtonsoft.Json/JObject to System.Text.Json
  - File: `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`
  - Acceptance: Newtonsoft.Json removed from Blazor.Client.csproj, STJ used instead
- **Task 3.3**: Remove Newtonsoft.Json package reference from Blazor.Client
  - Acceptance: No Newtonsoft references in Blazor.Client project
- **Task 3.4**: Configure NSwag to generate System.Text.Json serialization (instead of Newtonsoft)
  - Acceptance: nswag.json updated, regenerated client uses STJ
- **Task 3.5**: Enable Blazor WASM lazy loading for non-critical assemblies
  - Acceptance: Initial download only includes critical assemblies; others load on demand

**Risk**: NSwag client regeneration may introduce breaking changes. Newtonsoft->STJ migration requires careful HAL link handling.

---

### Phase 4: Client-Side Caching & API Optimization
**Priority**: MEDIUM-HIGH | **Estimated effort**: 3-4 hours
**Why**: Every navigation currently re-fetches all data from API.

- **Task 4.1**: Implement client-side HTTP response caching service
  - Pattern: In-memory cache keyed by URL + query params with configurable TTL
  - Acceptance: Repeated navigation to same page doesn't re-fetch if data is fresh
- **Task 4.2**: Add Polly resilience policies to HttpClient (retry + circuit breaker)
  - Acceptance: Transient HTTP failures auto-retry; circuit opens on sustained failure
- **Task 4.3**: Implement request deduplication for concurrent identical requests
  - Pattern: SemaphoreSlim or ConcurrentDictionary<string, Task> to coalesce in-flight requests
  - Acceptance: 5 concurrent calls to same endpoint result in 1 actual HTTP request
- **Task 4.4**: Enhance LookupCacheService with cache invalidation on write operations
  - Acceptance: Cache entries invalidated when user performs create/update/delete
- **Task 4.5**: Add HTTP ETag/If-None-Match support for conditional GET requests
  - Acceptance: API responses with ETag are cached; 304 Not Modified avoids re-download

**Risk**: Cache invalidation complexity. Stale data risk. Mitigation: Conservative TTLs, manual invalidation on writes.

---

### Phase 5: Render Mode Optimization
**Priority**: MEDIUM | **Estimated effort**: 2-3 hours
**Why**: Not all pages need WebAssembly interactivity; some can be server-rendered or static.

- **Task 5.1**: Audit all pages and categorize render mode requirements
  - Categories: Static (no interactivity), Server (admin, low-traffic), Auto (public, high-traffic)
  - Acceptance: Spreadsheet/table of all pages with recommended render mode
- **Task 5.2**: Apply `@rendermode` per-page where beneficial
  - Acceptance: Admin pages use Server mode; public read-only pages use Static/SSR where possible
- **Task 5.3**: Configure enhanced navigation and form handling
  - Acceptance: Enhanced navigation enabled for seamless SPA-like transitions

**Risk**: Render mode changes affect component state and event handling. Test each page after change.

---

### Phase 6: JS Interop Optimization
**Priority**: LOW-MEDIUM | **Estimated effort**: 1-2 hours
**Why**: JS interop calls are expensive in Blazor; batching reduces overhead.

- **Task 6.1**: Audit all IJSRuntime calls across components
  - Acceptance: List of all JS interop usage with frequency assessment
- **Task 6.2**: Batch JS interop calls where multiple calls happen in sequence
  - Acceptance: Sequential JS calls consolidated into single batched call
- **Task 6.3**: Use .NET 10 direct JS property access where applicable
  - Acceptance: Simple property reads use direct access instead of InvokeAsync

**Risk**: Low. JS interop optimization is incremental.

---

### Phase 7: Memory Management & Lifecycle
**Priority**: MEDIUM | **Estimated effort**: 3-4 hours
**Why**: 0 IDisposable implementations across 127 components = potential memory leaks.

- **Task 7.1**: Audit all components for disposable resources (event handlers, timers, JS interop refs)
  - Acceptance: Every component with subscriptions/timers/JS refs identified
- **Task 7.2**: Implement `IAsyncDisposable` on components with disposable resources
  - Acceptance: All identified components properly dispose resources
- **Task 7.3**: Configure Blazor Server circuit options (idle timeout, max retained circuits)
  - Acceptance: Circuit options configured in Program.cs with appropriate values
- **Task 7.4**: Implement `@implements IAsyncDisposable` pattern with DotNetObjectReference cleanup
  - Acceptance: All DotNetObjectReference instances disposed in DisposeAsync

**Risk**: Missing a component with leaks. Mitigation: Systematic audit with grep.

---

### Phase 8: Observability & Measurement
**Priority**: LOW | **Estimated effort**: 1-2 hours
**Why**: Can't improve what you can't measure.

- **Task 8.1**: Add Blazor-specific OpenTelemetry metrics (circuit count, render duration)
  - Acceptance: Metrics exportable to existing observability stack
- **Task 8.2**: Add performance markers for key user journeys (page load, search, event detail)
  - Acceptance: Navigation Timing API data captured for critical paths
- **Task 8.3**: Configure .NET 10 circuit metrics (if available)
  - Acceptance: Circuit lifecycle metrics captured

**Risk**: None. Observability-only, no functional changes.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| net10.0 upgrade breaks MudBlazor | Low | High | Check MudBlazor 8.5.0 net10.0 compat before upgrading |
| NSwag regeneration breaks client | Medium | High | Diff generated file before/after; test all API calls |
| Newtonsoft -> STJ breaks HAL parsing | Medium | Medium | Write tests for HAL extension methods before migrating |
| Virtualize breaks existing layouts | Low | Low | Only apply where item height is uniform |
| Cache staleness | Medium | Medium | Conservative TTLs (30s-2min), manual invalidation on writes |

## Success Metrics

| Metric | Before | Target |
|--------|--------|--------|
| WASM bundle size (published) | TBD (measure) | -30% or more |
| blazor.web.js size | ~148KB | ~35KB (76% reduction from .NET 10) |
| Initial page load (LCP) | TBD (measure) | < 2.5s |
| Unnecessary re-renders | ~27 StateHasChanged calls | < 10 justified calls |
| Virtualized lists | 0 | All lists > 50 items |
| Memory leaks (circuit) | Unknown | 0 (all resources disposed) |
| Client cache hit ratio | 0% | > 60% for repeated navigation |

## Timeline Estimate

| Phase | Effort | Dependencies |
|-------|--------|-------------|
| Phase 1: net10.0 Upgrade | 2-3h | None |
| Phase 2: Rendering | 4-6h | Phase 1 |
| Phase 3: Bundle Size | 3-5h | Phase 1 |
| Phase 4: Client Caching | 3-4h | None (can parallel with Phase 2-3) |
| Phase 5: Render Modes | 2-3h | Phase 1 |
| Phase 6: JS Interop | 1-2h | Phase 1 |
| Phase 7: Memory | 3-4h | None |
| Phase 8: Observability | 1-2h | Phase 1 |
| **Total** | **19-29h** | |

Phases 2, 3, 4, 5 can partially overlap. Critical path: Phase 1 -> Phase 2+3 (parallel) -> Phase 4-8.
