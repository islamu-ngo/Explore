<!-- ABOUTME: Resume context for the topology-aware Quartz.NET scheduler operator surfaces. -->
<!-- ABOUTME: Records the corrected architecture, what shipped, verification evidence, and remaining work. -->

# Quartz.NET Dashboard Integration — Context

Last Updated: 2026-08-16 Europe/Brussels

## SESSION PROGRESS (2026-08-16 Europe/Brussels)

### ✅ COMPLETED

All three phases are implemented and verified.

- **Phase 1** — first-party scheduler administration API at `/api/admin/scheduler` (7 operations), settings, HAL
  policies/assemblers, OpenAPI + generated client refreshed.
- **Phase 2** — `InstanceSchedulerSection` admin UI, client service adapter, HAL-gated sidebar integration.
- **Phase 3** — `Quartz.Dashboard` 3.19.1 mounted in `Event.Standalone` only, behind `DashboardEnabled`.
- Documentation: `docs/CONFIGURATION.md`, `docs/API_CHANGELOG.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`,
  `dev/report/quartznet-background-jobs-implementation-report.md` §7.

### 🔴 TWO PLANNING ASSUMPTIONS WERE WRONG — READ BEFORE RESUMING

1. **`MapQuartzHttpApi()` does not exist.** Verified against `Quartz.Dashboard.dll` 3.19.1 exported types and the
   official docs. The package has **no** API-only mode; everything is Blazor Server. Phases 1–2 were rebuilt as a
   first-party slice (plan Decision E). Do not reintroduce this method.
2. **Phase 3 was not blocked.** `src/Event.Standalone` and `tests/Event.Standalone.IntegrationTests` already exist;
   the plan's "archived workstream" note was stale.

### ⏭️ NEXT

1. Archive this workstream — implementation, documentation, and verification are complete. Full-suite failures were
   attributed against a clean `HEAD` worktree (see Validation Baseline); none belong to this work.
2. Optional follow-up: surface cluster node detail when clustering (P0.3) is proven.

### ⚠️ BLOCKERS

None.

## Quick Resume

1. Read this context and `quartz-dashboard-integration-tasks.md`.
2. Read plan §5 Decisions E–H before touching anything — they supersede the original design.
3. Do not reread the pre-correction plan sections; they are kept for provenance only.

## What Shipped — Key Files And Responsibilities

| Path | Layer | Purpose |
|---|---|---|
| `Explore.Application/Contracts/Scheduling/ISchedulerOperations.cs` | Application | Scheduler-neutral read/control seam; mirrors the existing `IScheduledJobRegistry` boundary |
| `Explore.Application/Contracts/Scheduling/ISchedulerAdminPolicy.cs` | Application | One shared answer on enabled/read-only, consumed by HAL policy **and** command handlers |
| `Explore.Application/Contracts/Scheduling/SchedulerRuntimeSnapshot.cs` | Application | Snapshot models; `Unavailable` distinguishes "scheduling off" from "no jobs" |
| `Explore.Application/Contracts/Scheduling/SchedulerAdminStates.cs` | Application | Normalized wire tokens (`running`/`standby`/`paused`/`on-demand`/…) |
| `Explore.Application/Features/Scheduling/SchedulerAdminProjection.cs` | Application | Shared projection so overview counts and job rows cannot disagree |
| `Explore.Application/Features/Scheduling/Handlers/Commands/SchedulerAdminCommandHandlerBase.cs` | Application | Central read-only gate + outcome→failure-code mapping for all five actions |
| `Explore.API/Scheduling/QuartzSchedulerOperations.cs` | API | **Only** Quartz-aware implementation; keeps the library out of Application/Domain |
| `Explore.API/Scheduling/UnavailableSchedulerOperations.cs` | API | Null object so a scheduler-less host answers instead of failing DI |
| `Explore.API/Controllers/SchedulerAdminController.cs` | API | Thin: MediatR dispatch + HAL assembly + ProblemDetails mapping |
| `Explore.API/Hateoas/Policies/SchedulerAdminLinkPolicy.cs` | API | Withholds control links when unavailable/read-only; per-job affordances by state |
| `Explore.Blazor.Client/Services/Scheduling/SchedulerAdminApiAdapter.cs` | Blazor | Transport only; no local mapping, so `_links` survive to components |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceSchedulerSection.razor` | Blazor | Status tiles, job table, HAL-gated controls |
| `Event.Standalone/Hosting/StandaloneSchedulerDashboardExtensions.cs` | Standalone | Conditional self-contained `MapQuartzDashboard()` |

## Key Decisions (superseding the original plan)

1. **First-party administration API**, not a third-party one — the package has no HTTP API (plan Decision E).
2. **Jobs are a HAL collection** so each row carries its own affordances (Decision F).
3. **Self-contained dashboard mapping** because this app uses Blazouter's explicit route table, not the standard
   attribute `Router` (Decision G). Verified empirically in a probe app.
4. **Availability is discovered from the served resource**, not from local admin claims (Decision H).
5. **Endpoints are in OpenAPI** (the original plan said exclude — correct for third-party, wrong for first-party).
6. **No `HttpApiAuthorizationPolicy` setting** — the controller uses the repo's resource-based authorization, so a
   policy-name string would be dead config.
7. Disabled by default; read-only by default for both surfaces.

## Constraints And Rules To Remember

- `Explore.Blazor.Client` must never reference Quartz — it is compiled into the **WebAssembly bundle**.
- `Quartz.Dashboard` is referenced **only** by `Event.Standalone`.
- `DurableSideEffectBoundaryTests` forbids `Quartz|ISchedulerFactory|IScheduler\b|JobBuilder|TriggerBuilder|JobDataMap`
  in `Explore.Application/Features/**`, `Explore.API/Controllers/**`, and `Explore.Domain/**`. `ISchedulerOperations`
  passes because `IScheduler\b` requires a word boundary.
- Adding an API endpoint auto-refreshes `schemas/openapi_islamu-event.json` (API Release build) and
  `docs/API_CONTRACT_INVENTORY.md` (architecture test); never hand-edit either.
- Regenerate the client with
  `dotnet msbuild src/Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1`.
- New HAL wrappers must be registered in `Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs` or the generated client
  loses the DTO fields.

## Validation Baseline — Evidence

| Check | Result |
|---|---|
| `dotnet build --configuration Release` (full solution) | ✅ Build succeeded |
| `Event.Architecture.Tests` (full) | ✅ 394 total, 393 passed, 1 pre-existing skip, **0 failed** |
| `Event.Application.UnitTests` — scheduler scope | ✅ 14/14 passed |
| `Event.API.IntegrationTests` — `QuartzSchedulerSettingsValidatorTests` | ✅ 14/14 passed |
| `Event.API.IntegrationTests` — `SchedulerAdminHateoasTests` | ✅ 9/9 passed |
| `Event.Standalone.IntegrationTests` — `StandaloneSchedulerSurfaceTests` | ✅ 4/4 passed |
| OpenAPI contract | ✅ 7 operations + HAL wrapper schemas present |
| `Event.API.IntegrationTests` (full) | ⚠️ 2310 total, 2280 passed, 30 failed — **26 identical on clean HEAD**, 4 traced to another workstream (below) |
| `Explore.Blazor.Client.Tests` (full) | ✅ 2423 total, 2422 passed, 1 pre-existing skip, **0 failed** |
| `Event.Standalone.IntegrationTests` (full) | ✅ 47 total, 1 failed — `DefaultStandaloneSqliteConfigurationUsesPersistedWalDatabaseWithThirtySecondTimeout` **also fails on clean HEAD** |

### The full-suite failures were attributed, not assumed

A clean `git worktree` at `HEAD` (no working-tree changes) was built and run for comparison.

- **Clean HEAD baseline:** 2291 total, 28 failed.
- **This tree:** 2310 total, 30 failed (the +19 are this workstream's new tests, all passing).
- **26 failures are identical in both** → pre-existing, unrelated to this work. They cluster around Cerbos PDP,
  Keycloak authority, event HAL affordances, storage, and tenant settings — consistent with those services not
  running locally. Run-to-run counts also vary (an earlier identical run reported 66), so several are flaky under
  parallel execution.
- **4 failures appear only in this tree**: `GuestPostOpenApiMetadata_RequiresIdempotencyKey`,
  `GuestRoutes_UseCapabilityScopedPublicTransactionalContracts`,
  `NativeSubmissionRoutes_ExposeAuthenticatedAndGuestTransactionalContracts`, and
  `ExistingWebhookManagementGets_AreControllerAnonymousAndHandlerAuthorized`. These are **not** from this
  workstream. They reflect over `RegistrationOrderController` / `GuestRegistrationOrderController` /
  webhook controller members, and the working tree contains a mid-flight refactor of exactly those controllers
  (`InstanceSettingsController.cs` and eight `BackgroundServices/*` files are deleted; `EventController`,
  `EventTicketingController`, and others are modified). `RegistrationOrderControllerTests` passes 19/19 on clean
  HEAD and fails 3/19 here, with no scheduler code in its path.

### Regressions this workstream did cause — found and fixed

- 19 `InstanceAdminSettingsLayout_*` bUnit tests failed after `ISchedulerAdminService` was injected into the
  settings layout, because the test fixture did not register it. Fixed by registering the substitute in
  `InstanceAdminSettingsLayoutTests`, and two tests were added for the discovery behaviour itself
  (`WhenSchedulerAdminApiIsServed_ExposesSchedulerNavigation` / `WhenSchedulerAdminApiIsAbsent_HidesSchedulerNavigation`).
  That suite is now 2423/2423 green.

## Current Known Risks / Unknowns

- Dashboard **rendering** inside `Event.Standalone` is not covered by integration tests: the dashboard is excluded in
  the `Testing` environment, exactly as the scheduler is. Composition, route ownership, and circuit uniqueness are
  asserted; rendering was proven only in an out-of-tree probe.

## Handoff Notes

### Handoff — 2026-08-16 Europe/Brussels

- **Current state:** Phases 1–3 implemented, documented, and verified at the scopes listed above.
- **Next action:** attribute or clear the 66 `Event.API.IntegrationTests` failures.
- **Blockers:** none.
- **Unrelated dirty files the next contributor must not fold into this work:** the `api-application-liability-reduction`
  workstream's edits across `src/Explore.API/Controllers/*`, `src/Explore.Application/Features/RegistrationForms/*`,
  `src/Explore.Blazor.Client/Pages/Studio/RegistrationForms/*`, `src/Explore.Infrastructure/Identity/UserContext.cs`,
  `src/Explore.Persistence/**`, plus doc edits in `docs/API.md`, `docs/CODEBASE_INSIGHTS.md`,
  `docs/CODEBASE_STRUCTURE.md`, `docs/EMAIL_NOTIFICATIONS.md`, `docs/SECURITY-MODEL.md`.
  `schemas/openapi_islamu-event.json` and `docs/API_CONTRACT_INVENTORY.md` are generated and contain both
  workstreams' changes.
- **Documentation impact:** complete (see Completed above).
- **Notes for the next contributor/agent:** do not try to reintroduce `MapQuartzHttpApi()` — it does not exist. Do
  not switch the standalone dashboard to the coexistence overload — Blazouter's router cannot resolve its pages.
