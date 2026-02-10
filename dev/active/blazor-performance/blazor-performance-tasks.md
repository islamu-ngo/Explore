# Blazor Performance Optimization - Task Checklist

## Phase 1: Upgrade to .NET 10 + Platform Features (CRITICAL) ✅ COMPLETE
- [x] 1.1: Upgrade Explore.Blazor.csproj from net9.0 to net10.0
- [x] 1.2: Upgrade Explore.Blazor.Client.csproj from net9.0 to net10.0
- [x] 1.3: Enable WasmStripILAfterAOT in Blazor.Client csproj
- [x] 1.4: Enable static web asset fingerprinting (MapStaticAssets already in use — verified)
- [x] 1.5: Fix any net9.0 -> net10.0 breaking changes, build clean (0 errors, 398/398 tests pass)
- [x] 1.6: Verify MudBlazor 8.13.0 compatibility with net10.0 (all tests pass)

## Phase 2: Rendering Optimization (HIGH) 🟡 IN PROGRESS
- [ ] 2.1: Audit 27 StateHasChanged() calls - remove unnecessary ones
- [ ] 2.2: Add @key directives to all @foreach loops with identifiable items
- [ ] 2.3: Implement ShouldRender() on expensive display-only components
- [ ] 2.4: Replace large-list @foreach with Virtualize/MudVirtualize (audit: few candidates, most paginated)
- [ ] 2.5: Add @attribute [StreamRendering] to data-heavy pages
- [ ] 2.6: Implement [PersistentState] for component state preservation

## Phase 3: Bundle Size Reduction (HIGH) 🟡 MOSTLY COMPLETE
- [x] 3.1: Configure NSwag to generate System.Text.Json serialization (nswag.json jsonLibrary=SystemTextJson)
- [x] 3.2: Migrate HalResourceExtensions from Newtonsoft.Json to System.Text.Json
- [x] 3.3: Remove Newtonsoft.Json package reference from Blazor.Client
- [x] 3.4: Regenerate NSwag client with STJ (runtime=Net90, jsonLibrary=SystemTextJson)
- [x] 3.5: Fix test files (AdminServiceTests, EventServiceTests, OrganizationServiceTests) STJ migration
- [ ] 3.6: Enable Blazor WASM lazy loading for non-critical assemblies
- [ ] 3.7: Add STJ source generator context for WASM AOT optimization

## Phase 4: Client-Side Caching & API Optimization (MEDIUM-HIGH)
- [ ] 4.1: Implement client-side HTTP response caching service
- [ ] 4.2: Add Polly resilience policies (retry + circuit breaker) to HttpClient
- [ ] 4.3: Implement request deduplication for concurrent identical requests
- [ ] 4.4: Enhance LookupCacheService with cache invalidation on writes
- [ ] 4.5: Add HTTP ETag/If-None-Match support for conditional GETs

## Phase 5: Render Mode Optimization (MEDIUM)
- [ ] 5.1: Audit all pages and categorize render mode requirements
- [ ] 5.2: Apply @rendermode per-page where beneficial
- [ ] 5.3: Configure enhanced navigation and form handling

## Phase 6: JS Interop Optimization (LOW-MEDIUM)
- [ ] 6.1: Audit all IJSRuntime calls across components
- [ ] 6.2: Batch sequential JS interop calls
- [ ] 6.3: Use .NET 10 direct JS property access where applicable

## Phase 7: Memory Management & Lifecycle (MEDIUM)
- [ ] 7.1: Audit all components for disposable resources
- [ ] 7.2: Implement IAsyncDisposable on components with disposable resources
- [ ] 7.3: Configure Blazor Server circuit options (idle timeout, max retained)
- [ ] 7.4: Implement DotNetObjectReference cleanup pattern

## Phase 8: Observability & Measurement (LOW)
- [ ] 8.1: Add Blazor-specific OpenTelemetry metrics
- [ ] 8.2: Add performance markers for key user journeys
- [ ] 8.3: Configure .NET 10 circuit metrics

---

## Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| 1. net10.0 Upgrade | 6 | ✅ Complete |
| 2. Rendering | 6 | 🟡 In Progress |
| 3. Bundle Size | 7 | 🟡 5/7 Complete |
| 4. Client Caching | 5 | Not Started |
| 5. Render Modes | 3 | Not Started |
| 6. JS Interop | 3 | Not Started |
| 7. Memory | 4 | Not Started |
| 8. Observability | 3 | Not Started |
| **Total** | **37** | **11/37** |
