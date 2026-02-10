# Blazor Performance Optimization - Context

## SESSION PROGRESS (2026-02-10)

### COMPLETED
- Full codebase research: component analysis, render patterns, NSwag client, auth flow, caching
- Synthesized findings from 3 parallel research agents (Explore x2 + Librarian)
- Created plan, context, and tasks files
- Phase 1: net10.0 upgrade complete (6/6 tasks)
- Phase 3 (partial): Newtonsoft→STJ migration complete
  - nswag.json: jsonLibrary=SystemTextJson, runtime=Net90 (NSwag 14.4 doesn't support Net100)
  - HalResourceExtensions.cs: migrated from Newtonsoft to System.Text.Json
  - Removed Newtonsoft.Json package from Blazor.Client.csproj
  - Regenerated EventApiClient.g.cs (0 Newtonsoft references)
  - Fixed 3 test files using Newtonsoft (AdminServiceTests, EventServiceTests, OrganizationServiceTests)
  - Build: 0 errors, 398/398 tests pass

### IN PROGRESS
- Phase 2: Rendering Optimization (3 parallel agents launched)
  - bg_37f96fc6: StateHasChanged audit + @key directives (session: ses_3b814edcfffey5pbhT0wqNXODT)
  - bg_c6e8b3c0: StreamRendering + PersistentState (session: ses_3b814047affemsxSpWT0eIdVjf)
  - bg_b11f17ce: ShouldRender candidates exploration (session: ses_3b812e22bffedbFaZ7WLxaR4KA)

### BLOCKERS
- None

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

## Quick Resume

To continue this task:
1. Read this context file for current state
2. Check tasks file for what's done/remaining
3. Phase 1 ✅ complete, Phase 3 mostly ✅ complete (5/7)
4. Phase 2 in progress (rendering optimization — StateHasChanged, @key, StreamRendering)
5. Next after Phase 2: finish Phase 3 remaining (lazy loading, STJ source gen), then Phase 4+
6. Build: `dotnet build --configuration Release --verbosity quiet`
7. Test: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
8. NSwag regen: `dotnet nswag run nswag.json` (from Explore.Blazor.Client/ dir, runtime=Net90)
