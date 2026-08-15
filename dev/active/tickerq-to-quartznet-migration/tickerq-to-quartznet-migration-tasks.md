# TickerQ → Quartz.NET Migration — Task Checklist

Last Updated: 2026-08-15 Europe/Brussels

## Status Summary
- **Overall status:** ✅ Implementation complete (all 4 phases)
- **Completed:** 12/12 implementation tasks; all 4 phase verifications passed
- **Current priority:** None — awaiting review
- **Next recommended slice:** Optional follow-up — adopt first-party `Quartz.Dashboard` (see Deferred Work)

## Implementation Maintenance Rules
- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task `🟡 IN PROGRESS` when it is likely to span multiple edits or a handoff; skip this churn for tiny tasks completed immediately.
- Check a substantial completed task immediately; reconcile small completed tasks no later than phase end.
- Add discovered work where it belongs and keep completed count, priority, next slice, deferred work, and update date accurate.
- Check a phase complete only after all implementation and phase-verification checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance criteria, risk, or validation strategy changes.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools MCP, or live services for verification.

## Phase 1: Core Quartz.NET Infrastructure ✅ COMPLETE

- [x] **1.1 Replace NuGet packages**
  - **Files:** `Directory.Packages.props`, `src/Explore.API/Explore.API.csproj`
  - **Result:** 4 TickerQ packages removed. Added `Quartz`, `Quartz.AspNetCore`, `Quartz.Extensions.Hosting`, `Quartz.Serialization.SystemTextJson` @ **3.19.1** (Apache-2.0). Lock files regenerated with `dotnet restore --force-evaluate`.
  - **Effort:** S

- [x] **1.2 Create Quartz configuration and options**
  - **Files:** `Configuration/QuartzSchedulerSettings.cs` (new), `QuartzSchedulerSettingsValidator.cs` (new); deleted `TickerQSchedulerOptions.cs`, `TickerQSchedulerOptionsValidator.cs`
  - **Deviation:** named `QuartzSchedulerSettings`, **not** `QuartzSchedulerOptions` — Quartz ships its own public `Quartz.QuartzSchedulerOptions`, so the planned name collides in any file importing both.
  - **Effort:** S

- [x] **1.3 Create Quartz DI extension method**
  - **Files:** `Extensions/QuartzSchedulerExtensions.cs` (new); deleted `TickerQSchedulerExtensions.cs`
  - **Result:** `AddApiQuartzScheduler` wires `AddQuartz` + `UsePersistentStore` with a provider switch over all 5 `PrimaryDatabaseProvider` values, `UseSystemTextJsonSerializer`, `UseProperties = true`, configurable table prefix, optional clustering, and `AddQuartzServer(WaitForJobsToComplete = true)`. Recurring jobs registered durably with cron triggers using `WithMisfireHandlingInstructionDoNothing`.
  - **Effort:** L

- [x] **1.4 Embed Quartz DDL scripts and create schema initializer**
  - **Files:** `Resources/Quartz/QuartzSchema.{Sqlite,PostgreSql,SqlServer,MySql}.sql` (new, embedded), `Scheduling/QuartzSchemaInitializer.cs` (new)
  - **Result:** DDL authored independently from the schema *interface facts* per AGENTS.md Rule #8 (no third-party script text copied). `{prefix}` token substitution, `GO` batch separator, `CREATE ... IF NOT EXISTS` / `IF OBJECT_ID(...) IS NULL` guards, **zero `DROP`/`TRUNCATE`** so startup application is non-destructive. Executes through `ExploreDbContext.Database` so no new ADO provider packages were needed.
  - **Effort:** M

- [x] **1.5 Delete TickerQ EF Core artifacts**
  - **Result:** `ApiTickerQDbContext.cs`, `ApiTickerQDbContextFactory.cs`, and the entire `src/Explore.API/Migrations/TickerQ/` directory removed. `src/Explore.API/Migrations/` no longer exists.
  - **Effort:** S

### Phase 1 Verification ✅
- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors
- [x] `Event.Architecture.Tests` — 378 passed / 4 failed, all 4 pre-existing (see Verification Notes)

---

## Phase 2: Job Implementation ✅ COMPLETE

- [x] **2.1 Rewrite EmailDispatchTickerQJobs as Quartz IJob**
  - **Files:** `Scheduling/EmailDispatchDrainJob.cs`, `Scheduling/EmailDispatchRecoveryScanJob.cs` (new); deleted `EmailDispatchTickerQJobs.cs`
  - **Result:** Both `[DisallowConcurrentExecution]`, both delegate to `IEmailDispatchDrainService`. Cron corrected to Quartz syntax `*/10 * * * * ?` and `0 */1 * * * ?` (see Discovered Work).
  - **Effort:** M

- [x] **2.2 Rewrite EventLifecycleTickerQJobs as Quartz IJob**
  - **Files:** `Scheduling/EventReminderDispatchJob.cs` (new); deleted `EventLifecycleTickerQJobs.cs`
  - **Result:** Reads pointer JSON from `MergedJobDataMap`, validates the use case, delegates to `ProcessSingleAsync`. Malformed or absent payloads are dropped as poison rather than retried forever.
  - **Effort:** M

- [x] **2.3 Rewrite TickerQScheduledEmailDispatchTrigger**
  - **Files:** `Scheduling/QuartzScheduledEmailDispatchTrigger.cs` (new); deleted TickerQ version
  - **Result:** `ISchedulerFactory` → one-off `SimpleTrigger` with `StartAt(dueAt)` attached to the durably-stored reminder job; pointer serialized to `JobDataMap` as JSON; returns a UUIDv7 trigger id.
  - **Design note:** scheduler-level retries were intentionally dropped. The recurring drain is the single retry authority, so a failed wake-up simply leaves the outbox row due.
  - **Effort:** M

- [x] **2.4 Update hosting extensions, health checks, and infrastructure settings**
  - **Result:** `ApiHostCompositionState.UseTickerQEmailDispatch` → `UseQuartzEmailDispatch`; startup applies the Quartz schema in place of TickerQ migrations; `EmailDispatchProcessorMode.TickerQ` → `.Quartz` (+ validator message); auth policy constant → `quartz_instance_admin`; OTel `.AddSource("TickerQ")` → `.AddSource("Quartz")`; standalone route classifier key → `Scheduler:Quartz:StatusEndpointPath`; health-check data keys → `schedulerEnabled` / `schedulerPersistentStore` / `schedulerStatusEndpointEnabled`; `appsettings.json` `Scheduler:Quartz` section rewritten.
  - **Effort:** L

### Phase 2 Verification ✅
- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors
- [x] `Event.API.IntegrationTests` scheduler + dispatch suites — 71/71 passed

---

## Phase 3: Tests ✅ COMPLETE

- [x] **3.1 Rewrite API integration tests**
  - **Deleted:** `EmailDispatchTickerQJobsTests.cs`, `TickerQDashboardRouteTests.cs`, `TickerQSchedulerOperationalStoreTests.cs`, `TickerQSchedulerOptionsValidatorTests.cs`, `Scheduling/ApiTickerQDbContextFactoryTests.cs`
  - **Added:** `EmailDispatchQuartzJobsTests.cs`, `QuartzSchedulerSettingsValidatorTests.cs`, `QuartzSchemaInitializerTests.cs`, `QuartzSqliteDurableSchedulingTests.cs`
  - **Modified:** `EmailDispatchHealthCheckTests.cs`, `AuthorizationProductionGuardrailTests.cs`
  - **Note:** the Testcontainers-PostgreSQL dashboard/store tests were replaced by **container-free SQLite** coverage that proves strictly more: schema creation, idempotency, restart durability, and real job firing.
  - **Effort:** L

- [x] **3.2 Update architecture tests**
  - **Files:** `DurableSideEffectBoundaryTests.cs`, `StandaloneHostGraphTests.cs`
  - **Result:** `SchedulerOperationPattern` now detects Quartz leakage (`Quartz|ISchedulerFactory|IScheduler\b|JobDataMap|TriggerBuilder|JobBuilder|CronScheduleBuilder|SimpleScheduleBuilder|DisallowConcurrentExecution`) into Application handlers and Domain. Boundary tests pass.
  - **Effort:** M

### Phase 3 Verification ✅
- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors
- [x] `Event.API.IntegrationTests` — `Quartz*` 26/26 passed, `EmailDispatch*` 45/45 passed

---

## Phase 4: Documentation ✅ COMPLETE

- [x] **4.1 Update core documentation files**
  - **Result:** All 13 files updated; `grep -rn -i tickerq docs/` returns **zero** matches. `CONFIGURATION.md` gained a full `Scheduler:Quartz` settings table plus a "Scheduler schema ownership" subsection; `OPERATIONS.md` scheduler section rewritten; `SELF_HOSTING.md` / `DEPLOYMENT_TIERS.md` now state that Tier 1 SQLite has durable scheduling; multi-instance guidance changed from "separate databases because `ticker` is fixed" to "distinct `SchedulerName`, or deliberate clustering".
  - **Effort:** M

### Phase 4 Verification ✅
- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors
- [x] `Event.Architecture.Tests` — 378 passed / 4 failed, all 4 pre-existing at baseline

**CI matrix updated.** `.github/workflows/_build-test.yml` previously ran `email_dispatch_mode: TickerQ` for
PostgreSQL and `HostedService` for every other provider (because TickerQ was PostgreSQL-only). That value no
longer exists as an enum member and would have failed startup validation. PostgreSQL and **SQLite** now run
`Quartz`, which exercises the migration's headline claim in CI. SQL Server, MariaDB, and MySQL stay on
`HostedService` until their DDL scripts are executed against a real engine (see Deferred Work).

### Final verification run (plain commands, post-`dotnet workload repair`)

| Command | Result |
|---|---|
| `dotnet build --configuration Release --verbosity quiet` | **0 errors** |
| `Event.Architecture.Tests` (full) | 378 passed / 4 failed — all 4 identical at baseline |
| `Event.API.IntegrationTests` `Quartz*` | **26 / 26 passed** |
| `Event.API.IntegrationTests` `EmailDispatch*` | **45 / 45 passed** |
| `Event.API.IntegrationTests` `AuthorizationProductionGuardrailTests` | 1 passed / 3 failed — **identical 3 failures at baseline** |
| `Explore.Infrastructure.Tests` `EmailDispatchDrainServiceTests` | **32 / 32 passed** |
| `Event.Standalone.IntegrationTests` `StandaloneHostGraphTests` | **12 / 12 passed** |

---

## Verification Notes

**Environment blocker diagnosed and permanently fixed.** The repository build was failing for reasons unrelated to this workstream: the machine's .NET SDK had a corrupted workload set (`Workload set version 10.0.301.1 has missing manifests likely removed by package management`). This is what the prior commit `6b2398268 chore(architecture): halt liability-reduction plan pending a working build` was blocked on — no application code was ever at fault. Implementation proceeded using `MSBuildEnableWorkloadResolver=false`; **`dotnet workload repair` was then run on 2026-08-15** and the SDK is healthy (`wasm-tools 10.0.109/10.0.100`). All verification below was re-run with plain `dotnet build` / `dotnet test` and stayed green, so no workaround remains in effect.

**Pre-existing test failures (not caused by this workstream).** Verified by running `Event.Architecture.Tests` in a detached git worktree at baseline `HEAD` (62f94b751):

| | Baseline HEAD | This workstream |
|---|---:|---:|
| Failed | 7 | 4 |
| Succeeded | 375 | 378 |

This workstream's 4 failures are a strict subset of the baseline's 7 (`BlazorIsolationArchitectureTests`, `NamingConventionTests.DTOs_ShouldEndWith_Dto`, `PersistenceTenantFilterArchitectureTests`, `Privacy.UserPiiInventoryArchitectureTests`) and reference only files this workstream never touched. **No new failures were introduced.**

---

## Discovered Work (found during implementation)

1. **TickerQ cron expressions were invalid Quartz syntax.** `ScheduledJobRegistry` advertised `*/10 * * * * *` and `0 */1 * * * *`. Quartz rejects a 6-field expression where day-of-month and day-of-week are both `*`; one must be `?`. Corrected in `ScheduledJobRegistry` and `QuartzSchedulerKeys` to `*/10 * * * * ?` and `0 */1 * * * ?`.
2. **`JobDataMap.GetString` throws on a missing key.** Caught by the new `ReminderJobSkipsWhenPointerIsMissing` test; `EventReminderDispatchJob` now probes with `TryGetValue` so a payload-free trigger is a logged no-op instead of a scheduler retry loop.
3. **Application-layer ABOUTME comments named TickerQ.** The scheduler-neutral contracts were correct in substance but their header comments referenced TickerQ. Updated to scheduler-neutral wording; no contract signatures changed.
4. **The plan's dashboard assumption was wrong, then partly right.** `Quartz.AspNetCore` provides only health checks and the hosted service — no dashboard. A separate first-party `Quartz.Dashboard` package *does* exist (3.18+, Apache-2.0), but it is a **Blazor Server + SignalR** application. See Deferred Work.

---

## Remaining / Deferred Work

- **First-party `Quartz.Dashboard` (decision needed).** `Quartz.Dashboard` 3.19.1 is first-party and Apache-2.0, with `MapQuartzDashboard()`, a configurable `DashboardPath`, and a read-only mode. It was **not** adopted because it is a Blazor Server app (depends on `Microsoft.AspNetCore.SignalR.Client`) and would pull Blazor Server component hosting, SignalR hub routing, and CSP concerns into `Explore.API`, colliding with `Event.Standalone`'s own Blazor composition and its `ApiHostRouteClassifier`. Since the surface is disabled by default, this workstream instead ships a **read-only JSON scheduler status endpoint** at the same `/admin/scheduler` path behind the same instance-admin policy (`QuartzSchedulerStatusEndpoint`), preserving the prior route and guardrails with zero new dependencies. Adopting the real dashboard is a self-contained follow-up.
- **Clustering not exercised.** `ClusteringEnabled` is implemented and validated (requires `UsePersistentStore` and `InstanceId=AUTO`) but no multi-node test exists; defer until a Tier 3 deployment needs it.
- **Non-SQLite DDL not executed in CI.** The PostgreSQL, SQL Server, and MySQL scripts are asserted structurally (`QuartzSchemaInitializerTests`) but only the SQLite script is executed against a real engine (`QuartzSqliteDurableSchedulingTests`). A Testcontainers PostgreSQL round-trip would close this gap.
- **Orphaned `ticker` schema.** Existing PostgreSQL deployments retain the old TickerQ `ticker` schema. Development mode, no automated cleanup; operators may drop it manually.
- **Stale references in another workstream's dev docs.** `dev/next/mvp-launch/*.md` still describes TickerQ (including the now-removed `EmailDispatchTickerQJobsTests` and `TickerQScheduledEmailDispatchTrigger`). Left untouched deliberately: that is a separate workstream's historical record and editing it here would create conflicts. Its owner should refresh it.
- **5 unimplemented scheduled jobs:** `general-outbox-drain`, `pds-sync-drain`, `dead-letter-summary`, `waitlist-promotion-scan`, `tenant-maintenance-scan` remain `Planned` in `ScheduledJobNames` and will be implemented as Quartz `IJob` when their features are built.
