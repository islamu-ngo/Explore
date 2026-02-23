# Blazor Performance Optimization - Context

## SESSION PROGRESS (2026-02-11)

### COMPLETED
- Full codebase research: component analysis, render patterns, NSwag client, auth flow, caching
- Synthesized findings from 3 parallel research agents (Explore x2 + Librarian)
- Created plan, context, and tasks files
- Phase 1: net10.0 upgrade complete (6/6 tasks)
- Phase 2: Rendering Optimization complete (6/6 tasks)
  - 2.1: StateHasChanged audit — 0 unnecessary calls remain
  - 2.2: @key directives — 26/28 @foreach loops properly keyed, 2 correctly skipped (strings, chunks)
  - 2.3: ShouldRender() on EventIslamicAspectCard and EventTechAspectCard
  - 2.4: Virtualize evaluation — NONE NEEDED (all lists paginated or <50 items)
  - 2.5: StreamRendering — 20 pages now have @attribute [StreamRendering] (9 added this session)
  - 2.6: PersistentState evaluation — COMPLETE (implementation deferred; Tier 1 candidates identified)
- Phase 3: Bundle Size Reduction complete (7/7 tasks)
  - 3.1-3.5: Newtonsoft→STJ migration (nswag.json, HalResourceExtensions, EventApiClient.g.cs, 3 test files)
  - 3.6: Lazy loading infrastructure (ILazyAssemblyLoader service + Routes.razor; requires library separation)
  - 3.7: STJ source generator context (AppJsonSerializerContext with 180+ DTOs for AOT optimization)
- Test fix: AuthenticationFlowTests — added ITenantNavigationService mock
- Build: 0 errors
- Tests: 384/398 Blazor Client (14 pre-existing EventListTests MudBlazor Virtualize BUnit failures), 190/190 Application, 61/61 Domain, 24/24 Architecture, 190/190 Secrets

### IN PROGRESS
- Nothing — Phases 1-3 complete, Phases 4-8 not yet started

### BLOCKERS
- None
- Note: `dotnet test --project` broken in .NET 10 SDK + TUnit. Use `dotnet run --project` instead.

---

## Key Files

### Blazor Server (BFF Host)
- **`Explore.Blazor/Explore.Blazor.csproj`** - Targets net10.0 ✅
- **`Explore.Blazor/Program.cs`** (~336 lines) - BFF host: YARP reverse proxy, Keycloak OIDC, cookie auth, circuit config
- **`Explore.Blazor/Components/App.razor`** - Root component, sets InteractiveAuto render mode on Routes

### Blazor Client (WASM)
- **`Explore.Blazor.Client/Explore.Blazor.Client.csproj`** - Targets net10.0, WasmStripILAfterAOT=true, Newtonsoft REMOVED ✅
- **`Explore.Blazor.Client/Program.cs`** (~14 lines) - Minimal WASM client setup
- **`Explore.Blazor.Client/Clients/EventApiClient.g.cs`** - NSwag-generated API client (System.Text.Json) ✅
- **`Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`** - Migrated to System.Text.Json ✅
- **`Explore.Blazor.Client/Services/LookupCacheService.cs`** - Only caching service (MemoryCache, 10-min TTL)

### NSwag Configuration
- **`Explore.Blazor.Client/nswag.json`** - runtime=Net90, jsonLibrary=SystemTextJson ✅
- **`Explore.API/swagger.json`** - Source OpenAPI spec (checked into source control)

---

## Important Decisions

1. **net10.0 upgrade is Phase 1** because .NET 10 provides 76% smaller blazor.web.js, WasmStripILAfterAOT, [PersistentState], ParameterState<T>, and other optimizations that later phases depend on.

2. **NSwag client stays** (for now) - Replacing NSwag with Kiota/openapi-generator is a separate decision. Phase 3 focuses on configuring NSwag to output STJ instead of Newtonsoft, and evaluating splitting strategies.

3. **Virtualize only where appropriate** - Not all 46 @foreach loops need virtualization. Only lists that can grow beyond ~50 items with uniform height.

4. **Conservative caching** - Client-side cache TTLs should be short (30s-2min) with explicit invalidation on write operations to avoid stale data.

---

## Technical Constraints

- **BFF Pattern**: Blazor Server proxies all API calls via YARP. Client-side caching must work at the HttpClient level in WASM, not at the BFF level.
- **InteractiveAuto mode**: Components must work in both Server and WASM contexts. Caching/state strategies must be render-mode agnostic.
- **NSwag regeneration**: The NSwag client is regenerated from swagger.json during build. Any manual edits to EventApiClient.g.cs will be lost.
- **Method typo**: `CongfigurePersistenceServices` in codebase - DO NOT rename per CLAUDE.md.
- **No scripts**: User explicitly prohibited script-based approaches.

---

## Research Findings Summary

### Agent 1 (Explore - Component Structure)
- 127 .razor files total (both projects)
- 46 @foreach loops (virtualization candidates)
- 27 StateHasChanged() calls (audit needed)
- 0 IDisposable/IAsyncDisposable implementations
- 0 Virtualize or MudVirtualize usage
- 0 StreamRendering attributes

### Agent 2 (Explore - API Communication)
- 38,874-line NSwag generated client
- Newtonsoft.Json dependency in WASM (HalResourceExtensions.cs uses JObject)
- LookupCacheService is the only cache (10-min TTL, MemoryCache)
- No Polly resilience on any HttpClient
- No request deduplication
- Auth: CircuitAccessTokenService + TokenForwardingDelegatingHandler for YARP

### Agent 3 (Librarian - .NET 10 Best Practices)
- blazor.web.js: 76% smaller in .NET 10
- WasmStripILAfterAOT: 20-50% smaller WASM bundle
- [PersistentState]: Replaces manual PersistComponentState usage
- ParameterState<T>: Value-type wrapper preventing unnecessary re-renders
- Direct JS property access: Avoids InvokeAsync overhead for simple reads
- Circuit metrics: Built-in observability in .NET 10

---

## New Files Created This Session
- `Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs` — STJ source generator context with 180+ DTOs
- `Explore.Blazor.Client/Services/LazyAssemblyLoader.cs` — ILazyAssemblyLoader service for WASM lazy loading

## Quick Resume

To continue this task:
1. Read this context file for current state
2. Check tasks file for what's done/remaining
3. Phases 1-3 ✅ complete (19/37 tasks)
4. Next: Phase 4 (Client-Side Caching & API Optimization)
5. Build: `dotnet build --configuration Release --verbosity quiet`
6. Test: `dotnet run --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release`
   (Note: `dotnet test --project` broken in .NET 10 SDK + TUnit)
7. NSwag regen: `dotnet nswag run nswag.json` (from Explore.Blazor.Client/ dir, runtime=Net90)
8. 14 pre-existing test failures in EventListTests (MudBlazor Virtualize BUnit issue) — NOT caused by our changes
## Context Reset Session Update (2026-02-15 21:25 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority shifted to analytics implementation completion and verification.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in `blazor-performance-tasks.md`.

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority focused on admin consolidation handoff in navbar customization track.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in this task file.
