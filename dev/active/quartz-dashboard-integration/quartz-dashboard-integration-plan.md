<!-- ABOUTME: Implementation plan for topology-aware Quartz.NET Dashboard integration across standalone and split hosts. -->
<!-- ABOUTME: Embeds first-party Quartz.Dashboard in Event.Standalone and builds custom Blazor admin pages consuming MapQuartzHttpApi for split mode. -->

# Quartz.NET Dashboard Integration — Implementation Plan

Last Updated: 2026-08-16 Europe/Brussels

> **⚠️ PLAN CORRECTED DURING IMPLEMENTATION (2026-08-16).** The original plan's central mechanism,
> `MapQuartzHttpApi()`, **does not exist** in `Quartz.Dashboard` 3.19.1. Sections 5 (Decisions E/F/G),
> 6 (Phases), and 13 (Risks) record what replaced it and why. Sections written before that discovery are kept
> for provenance but are superseded where they conflict.

## 0. Planning Metadata

- **Original request:** Integrate the first-party `Quartz.Dashboard` Blazor UI into Event.Standalone (conditionally enabled), expose `MapQuartzHttpApi` in the split-mode API host, and build custom Blazor admin pages that consume the HTTP API for split-mode operators.
- **Task directory:** `dev/active/quartz-dashboard-integration/`
- **Planning status:** Implemented (Phases 1–3 complete)
- **Matched intents:** `add-get-endpoint` (HTTP API surface), `blazor-ui-page` (admin pages), `add-infrastructure` (dashboard package adoption)
- **Relevant skills:** `blazor-ui-conventions`, `blazor-css-isolation`, `blazor-bff-patterns`, `auth-patterns`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `design-system`
- **Relevant rules:** `.agents/rules/api-controllers.md`, `.agents/rules/blazor-components.md`
- **Primary layers touched:** API (endpoint), Blazor.Client (pages/services), Blazor (host composition), Event.Standalone (host composition)
- **Complexity:** L. Three integration surfaces (dashboard embed, HTTP API, custom UI pages) across two host topologies, but each builds on existing patterns and proven Quartz infrastructure.

## 1. Executive Summary

Quartz.NET's first-party `Quartz.Dashboard` package (Apache-2.0, Blazor Server + SignalR) will be conditionally mounted in `Event.Standalone` where both Blazor and `IScheduler` coexist in-process. For the split-mode topology where the API host lacks Blazor infrastructure, `MapQuartzHttpApi()` will expose a REST API at `/api/admin/scheduler` behind instance-admin authorization. Custom Blazor admin pages in `Explore.Blazor.Client` will consume that HTTP API through the existing BFF proxy, giving split-mode operators a native scheduler management UI without requiring the full `Quartz.Dashboard` Blazor app in the API host.

### Explicit Non-Goals

- Hosting `Quartz.Dashboard` in the split-mode API host (requires Blazor infrastructure the API doesn't have).
- A separate dashboard container/sidecar.
- Modifying existing Quartz job definitions, triggers, or the persistent store schema.
- Clustering enablement (tracked separately as P0.3 in the Quartz implementation report).

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| Quartz.NET 3.19.1 is the active scheduler | `Explore.API.csproj` package reference, `QuartzSchedulerExtensions.cs` | High | Apache-2.0 |
| `Quartz.Dashboard` is first-party, Apache-2.0, Blazor Server | NuGet package metadata, Quartz documentation | High | Requires .NET 8+, Quartz 3.16+ |
| Dashboard requires in-process `IScheduler` access via DI | Quartz documentation, web research | High | No remote connection mode |
| Dashboard has `MapRazorComponents` coexistence overload | Quartz docs: `MapQuartzDashboard(blazorBuilder)` shares existing SignalR hub | High | Exists, but **unusable here** — it requires the host `Router` to resolve dashboard pages via `AdditionalAssemblies`, and this app routes through Blazouter's explicit table. Superseded by Decision G. |
| ~~`MapQuartzHttpApi()` is a separate REST API surface~~ **FALSE** | Reflection over `Quartz.Dashboard.dll` 3.19.1 exported types + official package docs | High | **No such method exists.** The package's entire public surface is `AddQuartzDashboard`, `MapQuartzDashboard` (2 overloads), `QuartzDashboardOptions`, `IDashboardAuthorizationFilter`. There is no API-only hosting mode. |
| Scheduler is confined to `Explore.API` | Zero Quartz references in `Explore.Blazor` or `Explore.Blazor.Client` | High | Clean Architecture boundary maintained |
| Blazor `MapRazorComponents<App>()` is inside `BlazorHostApplicationExtensions.MapBlazorHostEndpoints()` | `src/Explore.Blazor/Hosting/BlazorHostApplicationExtensions.cs` L128–146 | High | Returns `RazorComponentsEndpointConventionBuilder` |
| Existing admin pages use `IAdminService` → `IEventApiClient` → BFF proxy pattern | `AdminService.cs`, `InstanceAdminSettings.razor` | High | Established pattern to follow |
| Read-only JSON status endpoint exists at `/admin/scheduler` | `QuartzSchedulerStatusEndpoint.cs`, `QuartzSchedulerSettings.cs` | High | Behind `quartz_instance_admin` policy, disabled by default |
| `BlazorHostProfile` enum controls split vs combined composition | `Explore.Blazor/Program.cs`, `BlazorHostApplicationExtensions.cs` | High | Used for conditional YARP/dashboard registration |
| `Event.Standalone` plan (archived in `dev/zarchive/`) defines `UseWhen` middleware branching | Decision G in archived standalone plan | High | Dashboard routes need explicit branch assignment |

### 2.2 Existing Implementation

**API layer:**
- `QuartzSchedulerExtensions.AddApiQuartzScheduler()` — registers Quartz with DI, persistent store, jobs.
- `QuartzSchedulerExtensions.UseApiQuartzScheduler()` — authorization middleware for status endpoint.
- `QuartzSchedulerExtensions.MapApiQuartzSchedulerEndpoints()` — maps the read-only JSON status GET endpoint.
- `QuartzSchedulerStatusEndpoint.HandleAsync()` — returns scheduler metadata, job/trigger state, planned jobs.
- `QuartzSchedulerSettings` — settings under `Scheduler:Quartz` section, includes `StatusEndpointEnabled`, `StatusEndpointPath`, `StatusEndpointAuthorizationPolicy`.

**Blazor layer:**
- Zero Quartz awareness. Admin pages use `IAdminService` → `IEventApiClient` → BFF proxy → API.
- `InstanceAdminSettings.razor` uses `InstanceAdminSettingsLayout` with sidebar navigation pattern.
- Instance admin pages are `@attribute [Authorize]` with `@rendermode InteractiveServer`.

**Standalone layer:**
- Not yet implemented (archived workstream). When it exists, it will compose both API and Blazor in one process.

### 2.3 Existing Tests And Verification Coverage

- `Event.API.IntegrationTests/Features/QuartzSqliteDurableSchedulingTests.cs` — end-to-end scheduler durability.
- `Event.Architecture.Tests` — layer boundary enforcement (Quartz confined to API).
- No dashboard-specific tests exist (dashboard not adopted yet).

### 2.4 Existing Documentation And Contracts

- `dev/report/quartznet-background-jobs-implementation-report.md` — §7 documents the dashboard adoption decision as deferred.
- `QuartzSchedulerSettings` documents all current configuration keys.
- No OpenAPI contract for the status endpoint (explicitly excluded via `ExcludeFromDescription()`).

### 2.5 Current Pain Points / Improvement Areas

- The JSON status endpoint is read-only and requires direct API access with a tool like `curl`. No UI exists.
- Split-mode operators have no visual way to inspect job state, trigger next-fire-times, or identify stuck jobs.
- Instance admin pages have no scheduler section despite managing other infrastructure settings.

### 2.6 Unknowns After Investigation

| Unknown | What was searched | Resolving task |
|---|---|---|
| Exact `Quartz.Dashboard` package dependency tree and size impact | NuGet metadata; need `dotnet add` to verify | 1.1 |
| Whether `AddQuartzDashboard` conflicts with existing Quartz service registrations | Documentation says it augments; need integration test | 1.2 |
| Whether `MapQuartzHttpApi()` response shape matches our DTOs | Need to inspect actual HTTP responses | 2.1 |
| Whether custom `DashboardPath` works in standalone (non-coexistence) mode | Docs say path is fixed in coexistence; standalone might allow custom | 1.1 |

## 3. Proposed Future State

### Topology Matrix

| Topology | Scheduler UI | How It Works |
|---|---|---|
| **Event.Standalone** | Full interactive `Quartz.Dashboard` at `/quartz` | `AddQuartzDashboard()` + `MapQuartzDashboard(blazorBuilder)` conditionally registered when `Quartz__Dashboard__Enabled=true`. Shares existing Blazor/SignalR infrastructure. |
| **Split mode (API)** | `MapQuartzHttpApi()` REST endpoints at `/api/admin/scheduler/*` | Pure HTTP API behind `InstanceAdmin` policy. No Blazor dependencies. Replaces the current hand-rolled JSON status endpoint. |
| **Split mode (Blazor)** | Custom admin pages under Instance Admin Settings | `ISchedulerAdminService` → `IEventApiClient` → BFF proxy → API HTTP endpoints. Follows existing `InstanceAdminSettings` sidebar pattern. |

### Data Flow (Split Mode)

```
Browser → Blazor (InteractiveServer)
  → ISchedulerAdminService.GetSchedulerStatusAsync()
    → IEventApiClient.GetAsync("/api/admin/scheduler/status")
      → BFF YARP proxy → API host
        → MapQuartzHttpApi() → IScheduler (DI)
          → Response JSON
```

### Data Flow (Standalone)

```
Browser → Event.Standalone (Blazor circuit)
  → Quartz.Dashboard Razor components
    → IScheduler (DI, in-process)
      → Live scheduler state via SignalR
```

## 4. Non-Negotiable Constraints

1. `Quartz.Dashboard` is a Blazor Server app; it can only run where `MapRazorComponents` and `IScheduler` coexist in the same process.
2. The API host in split mode must NOT gain Blazor/SignalR dependencies.
3. Dashboard is disabled by default; operators opt in via environment variable.
4. All dashboard/HTTP API surfaces require `InstanceAdmin` authorization.
5. Clean Architecture: `Explore.Blazor.Client` must not reference Quartz.NET packages. It consumes only HTTP DTOs.
6. The existing `QuartzSchedulerStatusEndpoint` (`/admin/scheduler`) must be preserved as the lightweight fallback for operators who disable both the dashboard and the HTTP API.
7. HAL links gate UI affordances for scheduler admin actions.
8. Every file starts with two ABOUTME comments.

## 5. Architecture And Design Decisions

### Decision A: Topology-aware conditional registration

- **Decision:** Use `Quartz__Dashboard__Enabled` (default `false`) to conditionally register `AddQuartzDashboard()` + `MapQuartzDashboard()` in Event.Standalone, and `Quartz__HttpApi__Enabled` (default `false`) to conditionally register `MapQuartzHttpApi()` in the API host.
- **Why:** Both surfaces are operator tools that add attack surface when enabled. Disabled by default follows the project's "smallest deployment" philosophy.
- **Alternatives considered:** Always-on dashboard; dashboard behind feature flag in code only; compile-time conditional.
- **Consequences:** Two new settings in `QuartzSchedulerSettings`. Simple env-var opt-in.
- **Files/layers affected:** `QuartzSchedulerSettings.cs`, `QuartzSchedulerExtensions.cs`, `Event.Standalone/Program.cs`.

### Decision B: Custom Blazor pages over embedded dashboard for split mode

- **Decision:** Build custom Blazor admin pages in `Explore.Blazor.Client` that consume `MapQuartzHttpApi()` via the BFF proxy, rather than embedding `Quartz.Dashboard` in the API host.
- **Why:** The API host has no Blazor/SignalR infrastructure. Adding it for a dashboard would bloat the API binary and create a second SignalR hub. Custom pages follow the established `InstanceAdminSettings` pattern and maintain Clean Architecture boundaries.
- **Alternatives considered:** Embed `Quartz.Dashboard` in API host (rejected: adds Blazor stack); separate dashboard container (rejected: adds infrastructure); no split-mode UI (rejected: user explicitly requested it).
- **Consequences:** Custom pages are simpler (read-only + basic actions) than the full dashboard but integrate naturally with existing admin navigation. The full interactive dashboard is a standalone-only benefit.
- **Files/layers affected:** `Explore.Blazor.Client/Pages/Admin/Instance/`, `Explore.Blazor.Client/Services/`.

### Decision C: Extend existing QuartzSchedulerSettings rather than new settings class

- **Decision:** Add `DashboardEnabled`, `DashboardReadOnly`, `HttpApiEnabled`, and `HttpApiAuthorizationPolicy` to the existing `QuartzSchedulerSettings` class under `Scheduler:Quartz`.
- **Why:** All scheduler operator configuration belongs in one validated settings section. No proliferation.
- **Alternatives considered:** Separate `QuartzDashboardSettings` class; separate configuration section.
- **Consequences:** Settings section grows but stays cohesive.
- **Files/layers affected:** `QuartzSchedulerSettings.cs`.

### Decision D: Dashboard routes excluded from API bridge in standalone

- **Decision:** In Event.Standalone's `UseWhen` middleware branching (Decision G of the standalone plan), `/quartz` routes are excluded from the API bridge and routed directly through Blazor's authentication pipeline.
- **Why:** `Quartz.Dashboard` uses Blazor circuits, not REST. Running it through the API bridge would break SignalR negotiation.
- **Alternatives considered:** Include in API branch (broken: Blazor circuits need cookie auth, not bearer); new dedicated branch.
- **Consequences:** `/quartz` traffic uses cookie auth directly, consistent with other Blazor admin pages.
- **Files/layers affected:** `Event.Standalone/Program.cs` (when standalone is implemented).

### Decision E: `MapQuartzHttpApi()` does not exist — build a first-party administration API instead

- **Discovered:** 2026-08-16, during Phase 1 implementation.
- **Evidence:** Reflection over the exported types of `Quartz.Dashboard.dll` 3.19.1 (downloaded from nuget.org),
  cross-checked against the official package documentation page. The complete public surface is
  `QuartzDashboardServiceCollectionExtensions.AddQuartzDashboard(IServiceCollection, Action<QuartzDashboardOptions>)`,
  `QuartzDashboardEndpointRouteBuilderExtensions.MapQuartzDashboard(IEndpointRouteBuilder)` and its
  `RazorComponentsEndpointConventionBuilder` overload, `QuartzDashboardOptions`
  (`DashboardPath`, `ApiPath`, `ReadOnly`, `AuthorizationPolicy`, `AuthorizationFilter`), and
  `IDashboardAuthorizationFilter`. No `MapQuartzHttpApi`. No API-only hosting mode.
- **Decision:** Build a first-party scheduler administration API rather than consume a third-party one.
  `ISchedulerOperations` + `ISchedulerAdminPolicy` are Application contracts; `QuartzSchedulerOperations` in
  `Explore.API/Scheduling/` is the sole Quartz-aware implementation; MediatR queries/commands sit behind a thin
  `SchedulerAdminController` at `/api/admin/scheduler`.
- **Why, beyond necessity:** three properties the original design could not have had.
  1. **HAL gating becomes possible.** Constraint #7 ("HAL links gate UI affordances") was unsatisfiable against
     `MapQuartzHttpApi` — a third-party endpoint emits no HAL links, so Phase 2's gated buttons could never have
     worked. A first-party resource emits them.
  2. **No coupling to an unstable private contract.** `QuartzDashboardOptions.ApiPath` does route the dashboard's
     own internal transport (`IQuartzApiClient`/`InProcessQuartzApiClient`), but it is undocumented and the package
     README warns its API surface may change between releases.
  3. **The API host stays lean.** Because the package is never referenced by `Explore.API`, the API host gains no
     Blazor, Razor, or SignalR dependency at all — satisfying constraint #2 by construction rather than by care.
- **Alternatives considered:** consume `ApiPath` (rejected: undocumented, unstable, and still unmapped without the
  Blazor stack); host the dashboard in the API (rejected: violates constraint #2); ship no split-mode UI
  (rejected: the user asked for one).
- **Consequences:** ~15 new files across Application and API. The endpoints are **included** in OpenAPI — the
  opposite of the original plan's `ExcludeFromDescription()`, which was right for a third-party surface and wrong
  for a first-party contract the generated client must consume.

### Decision F: Jobs are a HAL collection, not an inline array

- **Decision:** `GET /api/admin/scheduler` returns scheduler state and summary counts; `GET /api/admin/scheduler/jobs`
  returns a `HalCollectionResource<SchedulerAdminJobDto>` whose embedded items each carry their own `_links`.
- **Why:** a HAL resource has one link map. An inline job array would have to share the parent's, making it
  impossible to express "this job may be resumed but that one may not". The repo's `ICollectionLinkPolicy<T>.GetItemLinks`
  already exists for exactly this, and the assembler batches per-item authorization into one deduplicated call.
- **Consequences:** the UI makes two reads. In exchange, `trigger`/`pause`/`resume` are decided per row on the
  server: `trigger` for every job, `pause` only for one with active triggers, `resume` only for a paused one, and
  an on-demand durable job with no trigger of its own gets `trigger` alone.

### Decision G: The standalone dashboard uses the self-contained mapping, not the coexistence overload

- **Decision:** `Event.Standalone` calls `MapQuartzDashboard()` (no argument), not `MapQuartzDashboard(blazorBuilder)`.
- **Why:** the coexistence overload expects the host's `Router` to resolve the dashboard's attribute-routed pages
  through `AdditionalAssemblies`. This application does not use the standard router — it uses **Blazouter**, whose
  `Router` resolves only components present in an explicit `RouteConfig` table and has no attribute-route fallback.
  The overload would therefore render the app's not-found page at every dashboard path. Listing the dashboard's
  page types in that table would require referencing `Quartz.Dashboard` from `Explore.Blazor.Client`, which is also
  compiled into the **WebAssembly bundle** — putting a scheduler library in the browser payload and breaking
  constraint #5.
- **Verification:** proven empirically in a scratchpad probe app, not assumed. With `MapRazorComponents<App>()`
  already mapped and a router that does not know the dashboard, `MapQuartzDashboard()` mapped cleanly, the host
  started, and `GET /quartz` and `/quartz/jobs` both returned 200 with real dashboard markup; `_content/Quartz.Dashboard/`
  assets resolved once `MapStaticAssets()` was present (the real host already calls it).
- **Consequences:** the dashboard is fully independent of the app's router and shell. It brings its own root
  component and its own circuit. The `Quartz.Dashboard` package reference lives only in `Event.Standalone`.

### Decision H: Availability is discovered from the served resource, not from claims

- **Decision:** the settings sidebar renders the scheduler item only when `GET /api/admin/scheduler` actually
  returned a resource; the API answers `404` when `AdminApiEnabled=false`.
- **Why:** whether the surface exists is a *host* fact (`AdminApiEnabled`) combined with a *caller* fact
  (permission). Only the server knows both. A local `IsInstanceAdmin` check would show the section on hosts that
  do not serve it.
- **Consequences:** one extra read on the settings page; a host without the surface produces one 404 and no
  section.

### Decision I: Every state the operator surface displays must have a remedy

- **Discovered:** 2026-08-16, during post-implementation review of the deferred-work list.
- **Problem:** the job table renders an `error` chip when any trigger is in Quartz's ERROR state, and a `running`
  chip when a job is executing — but the API offered no action for either. An operator could see a stuck trigger
  and a long-running job and do nothing about either from this surface. That is the same advertise/observe gap the
  read-only design exists to prevent, inverted: the UI reports a condition it cannot act on.
- **Decision:** treat *recovery* actions as in-scope for the first-party API, and only *authoring* actions as
  deferred. Phase 4 adds `reset-error` (clears ERROR triggers back to their normal state) and `interrupt`
  (requests cancellation of a running execution).
- **Why this line and not the plan's original one:** the original deferral bundled recovery with authoring under
  "advanced management". They are different. Recovery acts on state the platform already surfaces and that only an
  operator can clear. Authoring — create/delete jobs, reschedule triggers, calendars — competes with how this
  platform defines work: jobs are code-defined in `RegisterRecurringJobs`/`RegisterMaintenanceSweeps` and their
  cadence is settings-defined and re-registered from configuration on every boot. A UI reschedule would contend
  with startup registration and could be silently reverted, so cadence changes belong in the existing settings
  sections, not here.
- **Consequences:** the split-mode/standalone capability gap narrows to authoring only. Interruption is
  **cooperative** — Quartz signals the executing job's `CancellationToken`; a job that does not observe it keeps
  running — so the API reports whether an execution was actually signalled rather than implying a hard kill.

### Decision J: An operator surface must be monitorable, accountable, and honest about cluster scope

- **Discovered:** 2026-08-16, reviewing the scheduler surface against how the rest of this platform treats
  operational subsystems.
- **Problems found:**
  1. **Not monitorable.** Every other background subsystem publishes a readiness check — `email-dispatch`,
     `idempotency-cleanup`, `storage-reconciliation`, `ai-retention-cleanup`. The scheduler that *runs all of them*
     publishes none. An operator who pauses the scheduler, or whose triggers fall into the error state, sees a
     fully healthy `/health` while no background work happens at all. For self-hosted deployments where `/health`
     is the whole monitoring story, that is the difference between a five-minute and a five-hour incident.
  2. **Not accountable.** Pausing the scheduler, interrupting a run, and firing a job off-schedule are privileged
     production actions that currently leave no record of who did them. §11 of this plan promised audit logging and
     it was never implemented. The repository already treats comparable actions as audit-worthy
     (`WebhookAuditEvent`, `SupportAccessAuditEvent`, `TenantLifecycleLog`).
  3. **Dishonest under clustering.** `IScheduler.GetCurrentlyExecutingJobs()` and `IScheduler.Interrupt(JobKey)`
     are **node-local** in Quartz. With `ClusteringEnabled=true` the job table's "running" chip reflects only the
     node that served the request, and interrupting a job executing on another node silently does nothing. The
     surface would be quietly wrong exactly in the multi-node deployments enterprises run.
- **Decision:** add Phase 5 covering a readiness check, an audit trail, admin-action metrics, explicit
  cluster-scope reporting, and a confirmation guard on the instance-wide pause.
- **On audit storage:** control actions emit structured audit records through a dedicated sink abstraction whose
  default implementation writes to the logging pipeline with principal, action, target, outcome, and correlation
  id. Self-hosted operators already ship these structured JSON logs to their SIEM. A durable audit *table* is
  deliberately not added here: without a read API and retention policy it would be write-only compliance theatre,
  and the sink seam lets one be added later without touching a single call site. This is recorded as a conscious
  trade-off, not an oversight.
- **On the confirmation guard:** pausing the scheduler stops **all** background work — email dispatch, retention
  sweeps, storage reconciliation — from one button. The platform already requires typed confirmation for
  comparably broad actions (tenant purge). Per-job actions are narrow and need no guard.

## 6. Implementation Phases

### Phase 1: API HTTP API Surface And Settings Extension

- **Goal:** Expose `MapQuartzHttpApi()` in the API host behind instance-admin authorization, and extend settings for all new configuration keys.
- **Depends on:** Nothing; can start immediately.
- **Relevant files:** `src/Explore.API/Configuration/QuartzSchedulerSettings.cs` (existing), `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` (existing), `src/Explore.API/Explore.API.csproj` (existing).
- **Related skills/rules:** `auth-patterns`, `clean-architecture-rules`, `.agents/rules/api-controllers.md`.
- **Acceptance criteria:** `MapQuartzHttpApi()` is conditionally registered when `Quartz__HttpApi__Enabled=true`; endpoints require `InstanceAdmin` authorization; existing status endpoint is preserved; settings validate on start; `docs/CONFIGURATION.md` documents all new keys.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revert settings additions and endpoint mapping; existing status endpoint is untouched.

#### Task 1.1: Extend QuartzSchedulerSettings with dashboard and HTTP API configuration

- **Type:** modify
- **Layer:** API
- **Files:** `src/Explore.API/Configuration/QuartzSchedulerSettings.cs` (existing)
- **Description:** Add `DashboardEnabled` (bool, default `false`), `DashboardReadOnly` (bool, default `true`), `DashboardAuthorizationPolicy` (string, default `quartz_instance_admin`), `HttpApiEnabled` (bool, default `false`), and `HttpApiAuthorizationPolicy` (string, default `quartz_instance_admin`). Update the settings validator to validate new fields.
- **Acceptance Criteria:**
  - [ ] New settings properties exist with documented defaults.
  - [ ] Settings validator rejects invalid combinations (e.g., dashboard enabled but Quartz disabled).
  - [ ] `docs/CONFIGURATION.md` documents all new keys under `Scheduler:Quartz`.
- **Dependencies:** None.
- **Effort:** S
- **Required Skills/Rules:** None specific.

#### Task 1.2: Register MapQuartzHttpApi conditionally in API host

- **Type:** modify
- **Layer:** API
- **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` (existing), `src/Explore.API/Explore.API.csproj` (existing).
- **Description:** When `HttpApiEnabled` is `true` and Quartz is enabled, call `app.MapQuartzHttpApi().RequireAuthorization(settings.HttpApiAuthorizationPolicy)`. The `Quartz.Dashboard` NuGet package provides `MapQuartzHttpApi()` — add the package reference. Ensure the HTTP API endpoints are excluded from OpenAPI generation (`ExcludeFromDescription()`).
- **Acceptance Criteria:**
  - [ ] `Quartz.Dashboard` package reference added to `Explore.API.csproj`.
  - [ ] `MapQuartzHttpApi()` is conditionally called when `HttpApiEnabled=true`.
  - [ ] Endpoints require the configured authorization policy.
  - [ ] Endpoints are excluded from OpenAPI generation.
  - [ ] Existing status endpoint continues to work independently.
- **Dependencies:** 1.1.
- **Effort:** M
- **Required Skills/Rules:** `auth-patterns`.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

---

### Phase 2: Custom Blazor Admin Pages For Split Mode

- **Goal:** Build scheduler admin pages in `Explore.Blazor.Client` that consume `MapQuartzHttpApi()` via the BFF proxy, integrated into the Instance Admin Settings sidebar.
- **Depends on:** Phase 1 complete.
- **Relevant files:** `src/Explore.Blazor.Client/Services/` (existing), `src/Explore.Blazor.Client/Pages/Admin/Instance/` (existing), `src/Explore.Blazor.Client/Pages/Admin/Instance/Components/` (existing).
- **Related skills/rules:** `blazor-ui-conventions`, `blazor-css-isolation`, `blazor-bff-patterns`, `design-system`.
- **Acceptance criteria:** Scheduler admin section appears in Instance Admin Settings sidebar (gated by HAL link presence); pages display scheduler status, job list, trigger states, next/previous fire times; basic actions (pause/resume scheduler, trigger job, pause/resume job) work via HTTP API; pages follow existing admin page patterns (InteractiveServer, BEM CSS, MudBlazor v9).
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove new pages and service; existing Instance Admin Settings is untouched.

#### Task 2.1: Create ISchedulerAdminService and DTOs

- **Type:** create
- **Layer:** Blazor
- **Files:** `src/Explore.Blazor.Client/Services/SchedulerAdminService.cs` (new).
- **Description:** Create `ISchedulerAdminService` with methods mapping to `MapQuartzHttpApi()` responses: `GetSchedulerStatusAsync()`, `GetJobsAsync()`, `GetJobDetailAsync(string group, string name)`, `GetTriggersForJobAsync(string group, string name)`, `PauseSchedulerAsync()`, `ResumeSchedulerAsync()`, `TriggerJobAsync(string group, string name)`, `PauseJobAsync(string group, string name)`, `ResumeJobAsync(string group, string name)`. Implement using `IEventApiClient` with paths like `/api/admin/scheduler/...`. Define response DTOs matching the Quartz HTTP API response shapes. Register in DI.
- **Acceptance Criteria:**
  - [ ] `ISchedulerAdminService` interface and implementation exist.
  - [ ] DTOs match `MapQuartzHttpApi()` response shapes (verified by inspecting actual responses in Task 1.2).
  - [ ] Service registered in `ServiceCollectionExtensions.cs`.
  - [ ] No Quartz NuGet references in `Explore.Blazor.Client.csproj`.
- **Dependencies:** 1.2.
- **Effort:** M
- **Required Skills/Rules:** `blazor-bff-patterns`, `clean-architecture-rules`.

#### Task 2.2: Create scheduler admin pages and sidebar integration

- **Type:** create
- **Layer:** Blazor
- **Files:** `src/Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceSchedulerSection.razor` (new), `src/Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceSchedulerSection.razor.cs` (new), `src/Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceSchedulerSection.razor.css` (new).
- **Description:** Create a scheduler admin section component following the `InstanceAdminSettingsLayout` sidebar pattern. Display: scheduler status (running/standby/shutdown), clustered status, job count. Job list table with columns: name, group, owner, trigger state, next fire time, previous fire time. Action buttons: pause/resume scheduler, trigger individual jobs, pause/resume individual jobs. All actions gated by HAL link presence. Use MudBlazor v9 components (`MudTable`, `MudChip`, `MudButton`, `MudAlert`). BEM CSS isolation.
- **Acceptance Criteria:**
  - [ ] Scheduler section appears in Instance Admin Settings sidebar when HAL link `scheduler-admin` is present.
  - [ ] Scheduler status (running/standby) displays with appropriate visual indicator.
  - [ ] Job list table shows all scheduled jobs with trigger state and fire times.
  - [ ] Pause/resume/trigger actions work and refresh the display.
  - [ ] Actions are gated by HAL link presence (not local role checks).
  - [ ] CSS follows BEM methodology with scoped isolation.
  - [ ] Component uses `@rendermode InteractiveServer`.
- **Dependencies:** 2.1.
- **Effort:** L
- **Required Skills/Rules:** `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`.

#### Task 2.3: Add HAL link for scheduler admin and API endpoint for admin discovery

- **Type:** modify
- **Layer:** API
- **Files:** Relevant HAL link policy files in `src/Explore.API/` (existing), `src/Explore.Blazor.Client/Components/Shell/AppWorkspaceRail.razor` or equivalent navigation component (existing).
- **Description:** Add a `scheduler-admin` HAL link to instance-admin responses when `HttpApiEnabled=true` and the user has the `InstanceAdmin` policy. The Blazor sidebar uses HAL link presence to conditionally render the scheduler navigation item.
- **Acceptance Criteria:**
  - [ ] `scheduler-admin` HAL link appears in instance-admin responses when scheduler HTTP API is enabled.
  - [ ] HAL link is absent when the HTTP API is disabled or user lacks authorization.
  - [ ] Sidebar navigation item renders only when HAL link is present.
- **Dependencies:** 1.2.
- **Effort:** M
- **Required Skills/Rules:** `auth-patterns`, `blazor-ui-conventions`.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

### Phase 3: Event.Standalone Dashboard Embedding

- **Goal:** Conditionally mount the full `Quartz.Dashboard` in Event.Standalone when the standalone host workstream is implemented.
- **Depends on:** Phase 1 complete. Event.Standalone workstream must be implemented first.
- **Relevant files:** `src/Event.Standalone/Program.cs` (future), `src/Explore.Blazor/Hosting/BlazorHostApplicationExtensions.cs` (existing).
- **Related skills/rules:** `blazor-ui-conventions`, `auth-patterns`.
- **Acceptance criteria:** `Quartz__Dashboard__Enabled=true` mounts the full interactive dashboard at `/quartz` in Event.Standalone; dashboard uses the existing Blazor/SignalR infrastructure via the `MapQuartzDashboard(blazorBuilder)` overload; dashboard requires instance-admin authorization; read-only mode is configurable; dashboard routes are excluded from the API bridge middleware.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove conditional dashboard registration; standalone host continues without dashboard.

#### Task 3.1: Register Quartz.Dashboard services conditionally in standalone composition

- **Type:** modify
- **Layer:** Standalone / Blazor hosting
- **Files:** `src/Event.Standalone/Program.cs` (future, depends on standalone workstream), `src/Explore.Blazor/Hosting/BlazorHostApplicationExtensions.cs` (existing).
- **Description:** When `DashboardEnabled=true` and the host profile is `Combined` (standalone), call `builder.Services.AddQuartzDashboard(options => { options.AuthorizationPolicy = settings.DashboardAuthorizationPolicy; options.ReadOnly = settings.DashboardReadOnly; })`. In the endpoint mapping, pass the `RazorComponentsEndpointConventionBuilder` from `MapRazorComponents<App>()` to `MapQuartzDashboard(blazorBuilder)` to share the existing SignalR hub. Also call `MapQuartzHttpApi().RequireAuthorization(...)` for the REST surface.
- **Acceptance Criteria:**
  - [ ] `Quartz__Dashboard__Enabled=true` mounts dashboard at `/quartz` in standalone mode.
  - [ ] Dashboard shares existing SignalR hub (no second `/_blazor` endpoint).
  - [ ] Dashboard requires configured authorization policy.
  - [ ] Dashboard is read-only when `DashboardReadOnly=true`.
  - [ ] Dashboard is NOT registered in split-mode Blazor host.
  - [ ] `/quartz` routes are excluded from the API bridge `UseWhen` branch.
- **Dependencies:** 1.1, 1.2, Event.Standalone workstream.
- **Effort:** M
- **Required Skills/Rules:** `blazor-ui-conventions`, `auth-patterns`.

#### Task 3.2: Add standalone dashboard integration tests

- **Type:** create
- **Layer:** Tests
- **Files:** `tests/Event.Standalone.IntegrationTests/` — new test file (depends on standalone workstream).
- **Description:** Tests proving: dashboard is not registered when `DashboardEnabled=false`; dashboard is accessible at `/quartz` when enabled; dashboard requires authorization; dashboard does not conflict with existing Blazor routes; SignalR hub is shared (single `/_blazor` endpoint).
- **Acceptance Criteria:**
  - [ ] Test proves dashboard disabled by default.
  - [ ] Test proves dashboard accessible when enabled.
  - [ ] Test proves authorization enforcement.
  - [ ] Test proves no SignalR endpoint duplication.
- **Dependencies:** 3.1.
- **Effort:** M
- **Required Skills/Rules:** Testing conventions.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --configuration Release --verbosity quiet`

---

### Phase 4: Scheduler Recovery Actions

- **Goal:** give every job state the operator surface displays a corresponding remedy, closing the
  see-but-cannot-act gap identified in Decision I.
- **Depends on:** Phase 1 (contracts, controller, HAL policies) and Phase 2 (admin UI).
- **Relevant files:** `Explore.Application/Contracts/Scheduling/ISchedulerOperations.cs`,
  `SchedulerOperationResult.cs`, `Explore.Application/Responses/FailureCodes.cs`,
  `Explore.Application/Features/Scheduling/**`, `Explore.API/Scheduling/QuartzSchedulerOperations.cs`,
  `UnavailableSchedulerOperations.cs`, `Explore.API/Controllers/SchedulerAdminController.cs`,
  `Explore.API/Hateoas/Policies/SchedulerAdminLinkPolicy.cs`, `RouteNames.cs`, `LinkRelations.cs`,
  `Explore.Blazor.Client/**` (contract, adapter, section).
- **Acceptance criteria:** a job whose triggers are in ERROR advertises and accepts `reset-error`; an executing job
  advertises and accepts `interrupt`; neither link appears otherwise, nor on a read-only or disabled host; an
  action that no longer applies (job finished, trigger already recovered) is reported distinctly rather than as a
  false success; OpenAPI and the generated client are refreshed.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `Event.Application.UnitTests` and `Event.API.IntegrationTests` scheduler scope, `Event.Architecture.Tests`
- **Rollback / failure handling:** the two actions are additive; removing their links and routes restores Phase 1–3
  behaviour exactly.

#### Task 4.1: Add recovery operations to the scheduler contract and Quartz adapter

- **Type:** modify
- **Layer:** Application + API
- **Description:** add `ResetJobErrorStateAsync` and `InterruptJobAsync` to `ISchedulerOperations`. Implement in
  `QuartzSchedulerOperations` over `IScheduler.ResetTriggerFromErrorState(TriggerKey)` and
  `IScheduler.Interrupt(JobKey)`. Reset applies to every trigger of the job currently in ERROR, matching the
  job-level granularity the table presents. Add a `NotApplicable` outcome for "no errored trigger" / "nothing was
  executing" so a no-op is never reported as success, and a matching failure code.
- **Effort:** M

#### Task 4.2: Expose the actions through commands, controller, and HAL

- **Type:** modify
- **Layer:** Application + API
- **Description:** add `ResetSchedulerJobErrorStateCommand` and `InterruptSchedulerJobCommand` on the existing
  handler base so read-only enforcement and outcome mapping stay centralized; add the two controller actions and
  route names; emit `reset-error` only for a job in `error` state and `interrupt` only for an executing job, both
  behind the existing enabled/read-only gate.
- **Effort:** M

#### Task 4.3: Surface the actions in the admin UI

- **Type:** modify
- **Layer:** Blazor
- **Description:** extend `ISchedulerAdminService` and its adapter, add the two link relations, and render the
  buttons in `InstanceSchedulerSection` gated on the per-row links. Reuse the existing reload-after-action flow.
- **Effort:** S

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- `dotnet build --configuration Release --verbosity quiet`
- `Event.Application.UnitTests`, `Event.API.IntegrationTests` (scheduler scope), `Event.Architecture.Tests`

---

### Phase 5: Operational Accountability, Monitoring, And Cluster Correctness

- **Goal:** bring the scheduler operator surface up to the standard the rest of this platform's operational
  subsystems already meet (Decision J).
- **Depends on:** Phases 1–4.
- **Acceptance criteria:** `/health` reports scheduler posture and degrades when scheduling is disabled, paused, or
  has jobs in the error state; every control action emits an audit record and a metric; the surface states when its
  executing-job view is node-local; the instance-wide pause requires typed confirmation.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `Event.Application.UnitTests`, `Event.API.IntegrationTests` (scheduler scope), `Event.Architecture.Tests`,
    `Explore.Blazor.Client.Tests`

#### Task 5.1: Add the `scheduler` readiness health check

- **Layer:** API
- **Description:** add `SchedulerHealthCheck` registered as `scheduler` with `ready` tags, following the
  `IdempotencyCleanupHealthCheck` shape. Degraded when scheduling is intentionally disabled or the scheduler is in
  standby; unhealthy when it is shut down while enabled, or when any job's triggers are in the error state.
  Bounded, non-sensitive data only: state, job/error/executing counts, clustered flag.
- **Effort:** M

#### Task 5.2: Audit every scheduler control action

- **Layer:** Application + API
- **Description:** introduce `ISchedulerAdminAuditSink` and a logging implementation. Record principal reference,
  action, target job, outcome (including refusals), and correlation id. Emit from the controller boundary where the
  principal and correlation id are available, for successes and refusals alike — a denied privileged action is the
  one most worth recording.
- **Effort:** M

#### Task 5.3: Publish scheduler admin-action metrics

- **Layer:** Application
- **Description:** add a `Counter<long>` to `BusinessMetrics` for scheduler admin actions, labelled by action and
  outcome from a closed vocabulary, carrying no tenant or principal identity, consistent with existing metric
  conventions.
- **Effort:** S

#### Task 5.4: Report cluster scope honestly

- **Layer:** Application + API + Blazor
- **Description:** Quartz's executing-jobs read and interrupt are node-local. Surface `ExecutingViewIsNodeLocal`
  (true when clustered) and the node's `InstanceId` on the overview, label the running/interrupt affordances in the
  UI accordingly, and document the limitation. Do not hide the action — an operator on the right node needs it —
  but never imply cluster-wide reach.
- **Effort:** M

#### Task 5.5: Require typed confirmation for the instance-wide pause

- **Layer:** Application + API + Blazor
- **Description:** `PauseSchedulerCommand` takes a confirmation value that must match the configured scheduler
  name, mirroring the tenant-purge confirmation pattern. Mismatch returns a typed validation problem. Resume and
  all per-job actions stay unguarded.
- **Effort:** M

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- `dotnet build --configuration Release --verbosity quiet`
- `Event.Application.UnitTests`, `Event.API.IntegrationTests` (scheduler scope), `Event.Architecture.Tests`,
  `Explore.Blazor.Client.Tests`

## 7. Testing Strategy

- **Phase 1:** `Event.API.IntegrationTests` — validates HTTP API registration, authorization, and coexistence with existing status endpoint.
- **Phase 2:** `Event.Architecture.Tests` — validates Clean Architecture boundaries (no Quartz references in Blazor.Client).
- **Phase 3:** `Event.Standalone.IntegrationTests` — validates dashboard embedding, SignalR coexistence, and authorization (blocked on standalone workstream).

## 8. Documentation, Configuration, And Operations Impact

- **`docs/CONFIGURATION.md`:** Add `Scheduler:Quartz:DashboardEnabled`, `Scheduler:Quartz:DashboardReadOnly`, `Scheduler:Quartz:DashboardAuthorizationPolicy`, `Scheduler:Quartz:HttpApiEnabled`, `Scheduler:Quartz:HttpApiAuthorizationPolicy` with defaults and examples.
- **`docs/SELF_HOSTING.md`:** Add section on scheduler monitoring: standalone gets `/quartz` dashboard; split mode gets admin UI pages.
- **`dev/report/quartznet-background-jobs-implementation-report.md`:** Update §7 to reflect that the dashboard has been adopted with the topology-aware strategy.
- **`docs/OPERATIONS.md`:** Add scheduler dashboard to the operator tooling section.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- **Authorization:** All surfaces require `InstanceAdmin` policy. The dashboard, HTTP API, and custom pages are all gated.
- **Trust boundary:** In standalone, dashboard uses cookie auth (Blazor circuit). In split mode, HTTP API uses bearer auth through the BFF proxy.
- **Read-only default:** `DashboardReadOnly=true` prevents accidental job manipulation in production.
- **No tenant data exposure:** Quartz scheduler state contains only job/trigger metadata. `UseProperties = true` ensures no business payloads enter scheduler tables (enforced by `DurableSideEffectBoundaryTests`).
- **CSP:** `Quartz.Dashboard` serves static assets from `_content/Quartz.Dashboard/`; CSP must allow this path when dashboard is enabled.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy:** Not applicable. The scheduler is instance-level infrastructure, not tenant-scoped. Instance admin sees all jobs across tenants.
- **Federation:** Not applicable. Scheduler is local to the instance.
- **Localization:** Not applicable for Phase 1–2 (English-only admin tooling). Dashboard has no localization support.
- **Accessibility:** Custom Blazor pages must follow MudBlazor a11y patterns (ARIA labels, keyboard navigation).
- **Product:** Scheduler monitoring is an operator tool, not a user-facing feature.

## 11. Observability And Operations

- The existing Quartz OTel source (`.AddSource("Quartz")` in ServiceDefaults) already provides traces and metrics.
- The HTTP API and custom pages add no new observability requirements.
- Dashboard access should be logged at `Information` level for audit (who viewed/modified scheduler state).

## 12. Migration And Compatibility Plan

- **Database/schema/data:** No changes. Dashboard reads existing `QRTZ_` tables. No new schema.
- **Breaking changes:** None. All features are opt-in and disabled by default.
- **Deployment order:** No ordering requirements. API can deploy with HTTP API before Blazor pages are available.

## 13. Risk Register — Outcomes

| Original risk | Outcome |
|---|---|
| `Quartz.Dashboard` adds unexpected transitive dependencies to API | **Avoided entirely.** The package is never referenced by `Explore.API`. In `Event.Standalone` it adds one new transitive package (`Microsoft.AspNetCore.SignalR.Client`); `Quartz.AspNetCore` and `Quartz.Serialization.SystemTextJson` were already present. Package size 160 KB. |
| `MapQuartzHttpApi()` response shape changes between versions | **Eliminated.** The method does not exist; the contract is now first-party and owned by this repo. |
| SignalR hub conflict in standalone | **Not observed, and asserted.** `StandaloneSchedulerSurfaceTests` proves exactly one `_blazor` endpoint. The self-contained mapping gives the dashboard its own circuit rather than sharing the app's. |
| Custom `DashboardPath` unsupported in coexistence mode | **Not applicable.** The coexistence overload is not used (Decision G), so `DashboardPath` is configurable. |
| CSP blocks `_content/Quartz.Dashboard/` assets | **Non-issue.** Assets are same-origin and already permitted by the existing `default-src 'self'` policy. No CSP change was made. |

### Risks discovered during implementation

| Risk | Mitigation |
|---|---|
| Plans that name third-party APIs can specify methods that do not exist | Public API surface was verified against the shipped assembly *and* the vendor docs before implementation, not assumed from a plan. Worth repeating for any future third-party adoption. |
| A UI router replacement (Blazouter) silently breaks third-party Blazor packages that assume the standard `Router` | Documented in Decision G. Any future embedded Blazor package must either be self-contained or have its pages added to the Blazouter table. |

## 14. Success Metrics And Definition Of Done

- Split-mode operators who enable `Quartz__HttpApi__Enabled=true` can view scheduler status, job list, and trigger states from the Instance Admin Settings UI.
- Standalone operators who enable `Quartz__Dashboard__Enabled=true` see the full interactive Quartz.Dashboard at `/quartz`.
- Both surfaces require `InstanceAdmin` authorization and are disabled by default.
- Clean Architecture boundaries are preserved: `Explore.Blazor.Client` has zero Quartz NuGet references.
- All new configuration keys are documented in `docs/CONFIGURATION.md`.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. At the first implementation start, read plan, context, and tasks once; on a cold resume, read context and tasks first, then only the plan sections needed for the current phase or changed decision.
2. During an uninterrupted session, do not reread unchanged plan/context/tasks after every task; keep the current task in working context and reopen only the exact section needed.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `tasks.md` as the hot execution ledger: check a substantial task immediately after its implementation acceptance criteria are met, and reconcile smaller completed tasks together no later than phase end.
5. Keep implementation-task and phase-verification checkboxes separate; a task may be checked when its implementation is complete, but the phase is complete only after its build and selected test checkboxes pass.
6. Update the task status summary, completed count, current priority, next recommended slice, discovered tasks, deferred work, and `Last Updated` whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer; do not rewrite it for trivial edits.
8. Update the plan only when scope, architecture, phase order, acceptance criteria, risks, or validation strategy changes; do not churn it for ordinary progress.
9. Record failed validation with the known cause and next recovery action in tasks/context without marking the phase complete.
10. Before pausing, compaction, transfer, or PR creation, reconcile the affected tasks, add a concise dated handoff, and identify unrelated dirty files that the next contributor must avoid.
11. Run phase verification only after all phase tasks, with one Release build and at most one selected project test; do not repeat successful commands or start the application/browser.
12. Never report completion when repository reality and the task ledger disagree.

Require every implementation summary to teach: what changed and why; architecture/design patterns, libraries, infrastructure, protocols, and project abstractions used; important files, classes, handlers, services, and components with their responsibilities; data/control flow; relevant repository conventions and reliability/security practices; verification performed, remaining work, next work, and dev-doc update status.

## 16. Progress Reporting Contract

Require this response shape after each implementation slice:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```

## 17. Potential Risks & Unknowns

The most likely failure point is **Phase 3 (standalone dashboard embedding)**, which depends on the `Event.Standalone` workstream completing first. That workstream is archived and has not started implementation. Phases 1 and 2 are independent and deliver value immediately for split-mode operators. The SignalR coexistence overload (`MapQuartzDashboard(blazorBuilder)`) is documented but has limited community validation — it must be proven in an integration test before shipping. The `MapQuartzHttpApi()` response shape is undocumented in Quartz's public docs, so Task 2.1 requires inspecting actual responses before building DTOs.
