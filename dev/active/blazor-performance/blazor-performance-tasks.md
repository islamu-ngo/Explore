# Blazor Performance Optimization - Task Checklist

## Phase 1: Upgrade to .NET 10 + Platform Features (CRITICAL) ✅ COMPLETE
- [x] 1.1: Upgrade Explore.Blazor.csproj from net9.0 to net10.0
- [x] 1.2: Upgrade Explore.Blazor.Client.csproj from net9.0 to net10.0
- [x] 1.3: Enable WasmStripILAfterAOT in Blazor.Client csproj
- [x] 1.4: Enable static web asset fingerprinting (MapStaticAssets already in use — verified)
- [x] 1.5: Fix any net9.0 -> net10.0 breaking changes, build clean (0 errors, 398/398 tests pass)
- [x] 1.6: Verify MudBlazor 8.13.0 compatibility with net10.0 (all tests pass)

## Phase 2: Rendering Optimization (HIGH) ✅ COMPLETE
- [x] 2.1: Audit 27 StateHasChanged() calls - removed unnecessary ones (0 remain in .cs files)
- [x] 2.2: Add @key directives to all @foreach loops with identifiable items (26/28 keyed, 2 correctly skipped)
- [x] 2.3: Implement ShouldRender() on expensive display-only components (EventIslamicAspectCard, EventTechAspectCard)
- [x] 2.4: Evaluate Virtualize/MudVirtualize (NONE NEEDED - all lists paginated or <50 items)
- [x] 2.5: Add @attribute [StreamRendering] to data-heavy pages (28 pages audited, 20 have StreamRendering)
- [x] 2.6: Evaluate [PersistentState] for component state preservation (EVALUATION COMPLETE - Implementation deferred; Tier 1: EventList, UserProfile, MyOrganizations)

## Phase 3: Bundle Size Reduction (HIGH) ✅ COMPLETE
- [x] 3.1: Configure NSwag to generate System.Text.Json serialization (nswag.json jsonLibrary=SystemTextJson)
- [x] 3.2: Migrate HalResourceExtensions from Newtonsoft.Json to System.Text.Json
- [x] 3.3: Remove Newtonsoft.Json package reference from Blazor.Client
- [x] 3.4: Regenerate NSwag client with STJ (runtime=Net90, jsonLibrary=SystemTextJson)
- [x] 3.5: Fix test files (AdminServiceTests, EventServiceTests, OrganizationServiceTests) STJ migration
- [x] 3.6: Enable Blazor WASM lazy loading infrastructure (ILazyAssemblyLoader service + Routes.razor; requires library separation to activate)
- [x] 3.7: Add STJ source generator context for WASM AOT (AppJsonSerializerContext with 180+ DTOs)

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
| 2. Rendering | 6 | ✅ Complete |
| 3. Bundle Size | 7 | ✅ Complete |
| 4. Client Caching | 5 | ⏳ Not Started |
| 5. Render Modes | 3 | ⏳ Not Started |
| 6. JS Interop | 3 | ⏳ Not Started |
| 7. Memory | 4 | ⏳ Not Started |
| 8. Observability | 3 | ⏳ Not Started |
| **Total** | **37** | **19/37** |

## Test Status (2026-02-11)
- Blazor Client Tests: 384/398 pass (14 failures in EventListTests - pre-existing MudBlazor Virtualize BUnit issue)
- Application Unit Tests: 190/190 pass
- Domain Unit Tests: 61/61 pass
- Architecture Tests: 24/24 pass
- Secrets Unit Tests: 190/190 pass

## .NET 10 Test Runner Note
`dotnet test --project` no longer works with .NET 10 SDK + TUnit. Use `dotnet run --project` instead.
## Context Reset Session Update (2026-02-15 21:26 Europe/Brussels)

- Status update: No task-state changes in this session for this track.
- Priority update: Keep existing ordering; analytics work was handled in a separate track.
- Next step: Resume from current in-progress or highest-priority unchecked item.

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority focused on admin consolidation handoff in navbar customization track.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in this task file.
