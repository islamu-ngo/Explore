<!-- ABOUTME: Resume context for planning and implementing the optional Event.Standalone combined host. -->
<!-- ABOUTME: Records current evidence, fixed architecture decisions, blockers, and the next implementation slice. -->

# Event Standalone Combined Host — Context

Last Updated: 2026-08-09 Europe/Brussels

## SESSION PROGRESS (2026-08-02 Europe/Brussels)

### ✅ COMPLETED

- Classified the request under a governance fallback contract because no registered intent covers a new combined composition root.
- Inspected current API, Blazor/BFF, shared hosting, project-reference, Aspire, test, configuration, security, and documentation surfaces.
- Established the target architecture: reusable owning-host modules plus a third composition root, one `/api/*` endpoint graph, and an in-process BFF credential bridge instead of YARP self-proxy.
- Created synchronized plan, context, and task artifacts. No runtime code was changed.
- Incorporated Senior CTO feedback: renamed to `Event.Standalone`, added SQLite default persistence with provider override, added Docker packaging phases, added `UseWhen` middleware branch strategy.
- Reconfirmed the shared-worktree Release baseline after external build contention cleared: 35 projects, 0 errors; the previously documented 13 source errors are gone.
- Completed Task 1.1: `Explore.API/Program.cs` is a thin caller over public API-owned service, startup, middleware, and endpoint composition modules; focused host and startup-order proofs are independently confirmed.
- Phase 1 Release build passed with 0 errors. The full API integration project remains open because concurrent persistence work caused an invalid EF value-generator factory error and stale PostgreSQL schema failures before host requests executed.
- Completed Task 2.1: `Explore.Blazor/Program.cs` is a thin Split-profile caller over reusable service, middleware, endpoint, and startup modules; Combined omits YARP and remote API readiness while preserving BFF, auth, Razor/static-assets, SignalR, localization, and render-policy behavior.
- Completed Task 2.2: architecture tests protect the reusable-host seam and retain the existing Blazor/Client dependency boundaries.
- Diagnosed the Phase 2 Refit failure with official Refit v14.0.1 sources retrieved through Tavily. Generated clients were present, but `AddRefitClient<T>` selected the unavailable reflection resolver; the one-line `AddRefitGeneratedClient<T>` registration fix passed red-green-red-green toggle proof and the original 10-row Atproto surface.
- Independently closed the Phase 2 gate after a 20-second no-writer window: Release build succeeded with 35 projects and 0 errors; all 409 Blazor integration tests passed with no reflection-resolver signature; port 5200 and task-owned temporary artifacts were clean.
- Context7 documentation lookup was attempted as requested but could not authenticate because the configured OAuth token is invalid or expired. No Context7 claim was substituted; official Tavily/primary-source evidence is retained instead.
- Completed Task 3.1: the new `Event.Standalone` .NET 10 Web SDK project references the API and Blazor hosts, is registered under the solution's existing API hosting group, uses launch ports 5180/7180, and composes one API-owned common/startup/shutdown state with the Combined Blazor profile. Split retains its existing common registrations; Combined omits duplicate service defaults, common readiness/health, and shutdown middleware while preserving BFF/UI-specific services. Independent evidence includes a 12-project targeted Release build with 0 errors, focused profile tests 5/5, and locked-mode restore against the retained generated NuGet lock file.
- Completed Task 3.2: Split YARP and the new Combined bridge reuse one `EventBffRequestEnricher` for token, tenant, setup-secret, and support-session reconstruction. `/api/*` uses explicit cookie classification only, shared antiforgery rules, fail-closed token handling, shared privileged-header sanitation, and the existing `MultiAuth` API scheme; non-cookie external bearer/API-key traffic remains unchanged. The independent security review found and verified a repair that scopes the cookie principal only while user-bound trusted providers resolve, restores it in `finally`, and clears it before downstream API authentication. Focused evidence is green: Standalone 9/9, sanitizer 3/3, Split proxy 20/20, shared architecture 3/3, targeted build 0 errors.
- Completed Task 3.3: Unified middleware and endpoint ownership is now composed in `Event.Standalone` with `UseWhen` path predicates, explicit API/Blazor boundary isolation, no `/api` self-proxy recursion, explicit assembly registration for API application parts, and in-process API transport wiring.
- Completed Task 3.4: Added and green-passed standalone integration coverage in `CombinedApiBridgeMiddlewareTests`, `StandaloneHostGraphTests`, and `InProcessEventApiTransportTests`; verified API/tooling/UI/SignalR ownership, token fail-close semantics, header trust resets, external auth behavior, no loopback, and startup/worker execution.

### 🟡 IN PROGRESS

- Phase 1 API integration-suite re-verification remains open until owning persistence/auth/snapshot inputs materially change.

### ⭐️ NEXT

1. Implement Phase 4: add explicit `Hosting:Topology` selection to `Explore.AppHost` (Split default, Standalone opt-in) and assert forwarding invariants.
2. Re-run Phase 1 API integration suite only when owning dependency inputs change or failures become in-scope.
3. The `multi-database-support` workstream is fully complete (MDB-001 through MDB-715). Phase 5 can consume the structured `DatabaseOptions` contract, all five provider registrations, 10 migration assemblies, and SQLite WAL/busy-timeout configuration directly with no co-development needed.

### ⚠️ BLOCKERS

- The latest full Phase 1 API run reached 2,153 passed, 11 failed, and 1 not executed. The two extraction-owned MCP source-contract failures are repaired and pass 2/2 focused; the remaining failures are attributed to stale PostgreSQL schema/table state, Cerbos authorization expectations, HATEOAS snapshots, and privacy replay authorization. This workstream must not patch or suppress those owning defects.

## Quick Resume

1. Read this file and `event-standalone-combined-host-tasks.md`.
2. Read only the current phase plus relevant constraints/decisions in `event-standalone-combined-host-plan.md`.
3. Start from the first unchecked high-priority task unless the user overrides it.
4. Keep tasks current; update context/plan only on their defined triggers.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `src/Explore.API/Program.cs` | Existing | API | Current API composition root | Extract registrations/startup/pipeline/endpoints into owning modules. |
| `src/Explore.API/Hosting/` | New | API | Reusable API host modules | Preserve exact pipeline and single-run startup gates. |
| `src/Explore.Blazor/Program.cs` | Existing | Blazor | Current BFF/UI composition root | Becomes a thin Split-profile caller. |
| `src/Explore.Blazor/Hosting/` | New | Blazor | Profile-aware BFF/UI modules | `Split` uses YARP/remote readiness; `Combined` does not. |
| `src/Event.Web.BffHosting/` | Existing | Shared BFF | Trusted server-side BFF primitives | Extract transport-neutral privileged-header enrichment for YARP and bridge reuse. |
| `src/Event.Standalone/` | New | Composition | One-process/one-port host | References API and Blazor host assemblies; owns one startup/shutdown sequence. |
| `src/Explore.AppHost/AppHost.cs` | Existing | DevOps | Aspire topology | Add explicit `Hosting:Topology`, default Split. |
| `tests/Event.Standalone.IntegrationTests/` | New | Tests | Combined-host behavioral boundary | Auth selection, XSRF, headers, endpoint/assets, singleton ownership. |
| `tests/Event.Architecture.Tests/` | Existing | Tests | Dependency/topology guardrails | Recognize Standalone without weakening Blazor boundaries. |
| `Explore.slnx` | Existing | Solution | Canonical project manifest | Add Standalone and its test project. |
| `src/Event.Standalone/Dockerfile` | New | DevOps | Multi-stage Docker image build | Single-container deployment target. |
| `docker-compose.standalone.yml` | New | DevOps | Standalone Compose file | SQLite default with volume mount; provider override examples. |


## Key Decisions

1. **Additive host:** Standalone is optional; split API plus Blazor remains default.
2. **Shared composition:** Extract modules from both programs; do not duplicate their code in Standalone.
3. **One API graph:** Keep existing `/api/*` controllers/routes/versioning and never add `/api/v1` duplicates.
4. **No self-proxy:** Browser API requests are enriched in-process before the existing API pipeline.
5. **Preserve BFF trust:** Explicit cookie-scheme classification never supplies the API principal. Valid cookie session → XSRF for unsafe methods → retrieve usable server-held token → strip/rebuild privileged headers → existing bearer validation supplies the sole controller principal. Missing/unrefreshable token fails `401/403` and never falls through.
6. **External clients unchanged:** Requests without a valid BFF session use existing bearer/API-key behavior.
7. **Explicit profile:** `BlazorHostProfile.Split` vs `Combined` controls YARP and readiness registration.
8. **One owner:** Standalone registers API workers, migration/seeding, privacy gate, setup-secret initialization, service defaults, health, and shutdown once.
9. **Explicit assemblies:** Add API MVC application parts and Blazor root/client Razor/static asset assemblies deliberately.
10. **Ports:** Standalone launch profiles reserve HTTP 5180 and HTTPS 7180.
11. **Aspire selection:** `Hosting__Topology=Standalone`; omitted value means Split; invalid values fail fast.
12. **SQLite default with provider override:** Event.Standalone defaults to SQLite (`/app/data/islamu_event.db`); operators override via `DATABASE_PROVIDER` env var or Infisical secret. Docker packaging ships a single-container image.

## Constraints And Rules To Remember

- The fallback contract is bounded to composition, tests, solution/orchestration wiring, and topology docs.
- Blazor/Client must not reference Domain/Application/Persistence/Infrastructure or call MediatR directly.
- Browser tokens remain server-side; privileged inbound browser headers are untrusted.
- API tenant/auth/rate-limit/authorization/idempotency/output-cache order and BFF auth/antiforgery order are security contracts.
- Controllers, HAL, ProblemDetails, generated clients, and API versioning remain unchanged.
- Preserve the route-aware runtime render policy: fallback `InteractiveServer`, tenant-selectable `InteractiveAuto`/`InteractiveWebAssembly`, forced `InteractiveServer` onboarding, and no component dependency on `HttpContext`.
- SQLite is the default provider for Event.Standalone; provider override uses the completed `multi-database-support` workstream's `DatabaseOptions` contract (closed provider enum, structured fields, provider-native builders, dual-role credentials, `RelationalModelNamespace` schema/prefix policy, 10 migration assemblies). The privacy-erasure authority SQLite file remains separate from the primary application SQLite file, enforced by the MDB authority-topology boundary.
- New files require two `ABOUTME:` lines.
- Tests use project-scoped Release commands only.

## Validation Baseline

- Planning-only validation: `git diff --check -- dev/active/event-standalone-combined-host`.
- Current repository baseline is green: `dotnet build --configuration Release --verbosity quiet` passed with 0 errors on 2026-08-02 after shared build contention cleared.
- Each implementation phase runs exactly one Release build and at most one selected non-browser test project after all phase tasks.
- Phase 1: `Event.API.IntegrationTests` for API host extraction.
- Phase 2: `Explore.Blazor.IntegrationTests` for profile-aware BFF/UI extraction.
- Phase 3: `Event.Standalone.IntegrationTests` for the unified host.
- Phase 4: `Event.Architecture.Tests` for final solution/composition/AppHost topology invariants.
- Phase 5: `Event.Standalone.IntegrationTests` for SQLite default and provider override.
- Phase 6: Manual Docker build verification.

## Current Known Risks / Unknowns

- **Shared worktree contention:** concurrent builds can lock common Release outputs; serialize verification commands.
- **Task 3.3:** combining globally ordered middleware is the highest architectural/security risk.
- **Task 3.3/3.4:** referenced Web SDK static asset/root component discovery must be proven by integration tests without copying assets.
- **Task 3.2/3.4:** cookie classification must never become the API principal; missing tokens fail closed and all API policies evaluate the revalidated bearer principal.
- **Task 4.1:** configuration forwarded to two current resources must be inventoried and forwarded once without conflicting names or duplicate services.
- **Phase 5:** The `multi-database-support` workstream is fully implemented (MDB-001 through MDB-715). The structured `DatabaseOptions` contract, all five provider registrations, 10 migration assemblies, and SQLite-specific WAL/busy-timeout configuration are available for direct consumption. No co-development is needed.
- **Phase 6:** The functional framework-dependent image is `362,893,616` uncompressed bytes. Safe pruning retains only `linux-x64`, removes symbols and `BlazorDebugProxy`, and preserves localization, Blazor static assets, and SQLite native libraries, but cannot satisfy the `<250 MB` contract.
- **Phase 6 publish graph:** Framework-dependent trimming is unsupported. A self-contained trim probe reaches a transitive `browser-wasm` application-host conflict because global server publish properties flow into `Explore.Blazor.Client`; resolving it requires an explicit scope/acceptance decision and full runtime regression proof.

## Handoff Notes

### Handoff — 2026-08-05 Europe/Brussels

- **Current state:** Task 3.3 and Task 3.4 are complete; phase-3 verification is green (`dotnet build`, `Event.Standalone.IntegrationTests`).
- **Next action:** Implement explicit Aspire topology selection and phase-4 invariants.
- **Blockers:** External/ownership failures remain in `Event.API.IntegrationTests` due persistence/auth/snapshot state.
- **Modified files:** `src/Event.Standalone/Program.cs`; `src/Event.Standalone/Middleware/CombinedApiBridgeMiddleware.cs`; `src/Event.Web.BffHosting/Security/*`; `src/Explore.API/Hosting/ApiHostApplicationExtensions.cs`; `src/Explore.Blazor/{Extensions,Hosting,Services}/...`; `tests/Event.Standalone.IntegrationTests/**`; and phase 3 workstream docs.
- **Validation:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- **Documentation impact:** Runtime docs remain open for Phase 4–6 updates.
- **Risks:** Combined middleware/auth order and static web asset discovery are now guarded by phase 3 tests; monitor when topology logic is added.
- **Notes for next contributor/agent:** Do not patch unrelated owning failures from other workstreams; keep Split as default and Standalone explicit.

### Handoff — 2026-08-09 Europe/Brussels

- **Current state:** The `multi-database-support` workstream is fully complete (MDB-001 through MDB-715). The standalone host plan, context, and tasks have been updated to reflect the completed MDB contract. Phase 5's dependency on MDB is resolved — no co-development needed.
- **What changed:** Updated plan Decision H, Phase 5, Task 5.1, Section 12, Section 17, and constraint #10 with precise references to the completed MDB implementation (closed provider enum, `DatabaseOptions`/`DatabaseOptionsValidator`, provider-native builders, dual-role credentials, `RelationalModelNamespace` schema/prefix policy, 10 migration assemblies, SQLite WAL/busy-timeout, authority-topology separation). Removed conditional "must be landed or co-developed" language throughout.
- **Next action:** Continue with Phase 4 (Aspire topology) or Phase 6 (Docker packaging) depending on priority.
- **Risks:** Phase 4 architecture test verification remains open. Docker packaging (Phase 6 tasks 6.1, 6.2) remains incomplete.

### Handoff — 2026-08-09 Phase 6 Runtime Investigation

- **Current state:** Tasks 6.1 and 6.2 remain unchecked. The Dockerfile now restores with current lock evaluation, resolves combined-host publish output collisions in favor of `Event.Standalone/appsettings.json`, omits debug output, retains only `linux-x64` native assets, and removes `BlazorDebugProxy`.
- **Observed runtime:** Rootless Podman produced a functional `linux/amd64` image. Metadata is UID 1654, port 8080, and `dotnet Event.Standalone.dll`; `libe_sqlite3.so` is present and the non-root process creates SQLite WAL files in the mounted `/app/data` volume. Production web startup correctly stops before health when the migration-service-owned privacy schema is absent.
- **Blocking acceptance:** The uncompressed image is `362,893,616` bytes versus the plan's `<250 MB` ceiling. Docker's native image-size convention is uncompressed local size; compressed transfer size is not a valid reinterpretation of the current wording.
- **Investigated alternatives:** Plain chiseled saves only `39,048,620` bytes and drops the `-extra` globalization posture; arbitrary DLL/static/native deletion violates runtime behavior. Framework-dependent trimming fails `NETSDK1102`. Self-contained trimming requires MSBuild property isolation between the `linux-x64` server graph and transitive `browser-wasm` client before it can even reach ILLink/runtime validation.
- **Separate blocker:** The exact Phase 4 architecture suite remains red at 358 passed, 4 failed, 1 skipped; all four failures are owned by `registration-data-collection`, and no Standalone invariant is failing.
- **Decision required:** Either authorize the cross-project, container-specific trim-safety work and its broader verification scope, or re-baseline Task 6.1's uncompressed image ceiling to a measured value (recommended floor: 375 MB). After that, run migration-first Compose health and volume-persistence QA.
- **Evidence:** `.omo/evidence/event-standalone-combined-host/task-6-1-20260809/`, `.omo/evidence/event-standalone-combined-host/task-6-1-publish-graph-debug-20260809/`, and `.omo/evidence/event-standalone-combined-host/phase4-architecture-regate-20260809/`.
