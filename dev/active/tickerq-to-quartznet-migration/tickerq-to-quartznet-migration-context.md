# TickerQ → Quartz.NET Migration — Context

Last Updated: 2026-08-15 Europe/Brussels

## SESSION PROGRESS (2026-08-15 Europe/Brussels)

### ✅ COMPLETED — ALL 4 PHASES IMPLEMENTED
- Phase 1: packages swapped to Quartz 3.19.1; `QuartzSchedulerSettings` + validator; `QuartzSchedulerExtensions`; embedded multi-provider DDL + `QuartzSchemaInitializer`; all TickerQ EF artifacts deleted.
- Phase 2: three `IJob` classes, `QuartzScheduledEmailDispatchTrigger`, and every hosting/health/auth/OTel/standalone/Infrastructure touchpoint rewired.
- Phase 3: TickerQ tests deleted; Quartz test suite added, including a real SQLite durability round-trip.
- Phase 4: all 13 documentation files updated.
- Zero `TickerQ` references remain in `src/`, `tests/`, or `docs/`. Release build green (0 errors).

### 🟡 IN PROGRESS
- Nothing. Awaiting user review.

### ⏭️ NEXT
1. User review of the implementation.
2. Decide on the deferred first-party `Quartz.Dashboard` adoption (see `tasks.md` → Deferred Work).
3. Optionally add a Testcontainers PostgreSQL round-trip for the non-SQLite DDL scripts.

### ⚠️ BLOCKERS
- **None.** The earlier repository build failure was an environment problem, not code: the .NET SDK had a corrupted workload set (`Workload set version 10.0.301.1 has missing manifests likely removed by package management`), which is what commit `6b2398268` was blocked on. **`dotnet workload repair` was run on 2026-08-15 and the SDK is healthy** (`wasm-tools 10.0.109/10.0.100` installed). A plain `dotnet build --configuration Release` now succeeds with 0 errors; no `MSBuildEnableWorkloadResolver` workaround is needed.

## Quick Resume
1. Read this context and `tickerq-to-quartznet-migration-tasks.md`.
2. Read only the current phase, constraints, or changed decisions from `tickerq-to-quartznet-migration-plan.md`; do not reread the full unchanged plan on every resume.
3. Implementation is complete; remaining items are in `tasks.md` → Remaining / Deferred Work.
4. Build and test with the standard commands; the SDK workload set was repaired on 2026-08-15 and needs no workaround.

## Key Files And Responsibilities (as delivered)

| Path | State | Layer | Purpose |
|---|---|---|---|
| `Directory.Packages.props` / `src/Explore.API/Explore.API.csproj` | Modified | Build | Quartz 3.19.1 (`Quartz`, `.AspNetCore`, `.Extensions.Hosting`, `.Serialization.SystemTextJson`); TickerQ removed |
| `src/Explore.API/Configuration/QuartzSchedulerSettings.cs` | New | API | Scheduler settings — **named `Settings` to avoid colliding with `Quartz.QuartzSchedulerOptions`** |
| `src/Explore.API/Configuration/QuartzSchedulerSettingsValidator.cs` | New | API | Startup validation incl. DDL-safe table prefix and clustering rules |
| `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` | New | API | `AddApiQuartzScheduler`, `UseApiQuartzScheduler`, `MapApiQuartzSchedulerEndpoints`, `ApplyQuartzSchedulerSchemaAsync`, `IsQuartzSchedulerEnabled` |
| `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs` | New | API | Stable `JobKey`/`TriggerKey`/cron/data-key constants derived from `ScheduledJobNames` |
| `src/Explore.API/Scheduling/EmailDispatchDrainJob.cs` | New | API | `[DisallowConcurrentExecution]` cron drain |
| `src/Explore.API/Scheduling/EmailDispatchRecoveryScanJob.cs` | New | API | `[DisallowConcurrentExecution]` recovery scan |
| `src/Explore.API/Scheduling/EventReminderDispatchJob.cs` | New | API | Durable job; pointer-only JSON read from `MergedJobDataMap` |
| `src/Explore.API/Scheduling/QuartzScheduledEmailDispatchTrigger.cs` | New | API | One-off `SimpleTrigger` via `ISchedulerFactory` |
| `src/Explore.API/Scheduling/QuartzSchemaInitializer.cs` | New | API | Applies embedded idempotent DDL through `ExploreDbContext.Database` |
| `src/Explore.API/Scheduling/QuartzSchedulerStatusEndpoint.cs` | New | API | Read-only operator status surface (replaces the TickerQ dashboard) |
| `src/Explore.API/Resources/Quartz/QuartzSchema.*.sql` | New | API | Embedded DDL for SQLite, PostgreSQL, SQL Server, MySQL/MariaDB |
| `src/Explore.Infrastructure/EmailDispatchProcessorSettings.cs` | Modified | Infra | `EmailDispatchProcessorMode.Quartz` replaces `.TickerQ` |
| `src/Explore.ServiceDefaults/Extensions.cs` | Modified | Defaults | OTel `.AddSource("Quartz")` |
| `src/Event.Standalone/Hosting/StandaloneHostApplicationExtensions.cs` | Modified | Standalone | Route classifier reads `Scheduler:Quartz:StatusEndpointPath` |
| `src/Explore.Application/Contracts/**` | Comments + cron text only | App | Contracts unchanged; ABOUTME wording made scheduler-neutral; registry cron corrected to Quartz syntax |

## Key Decisions

1. **Quartz.NET chosen** — Apache 2.0, 15+ years maturity, first-party SQLite/PostgreSQL/SQL Server/MySQL support.
2. **AdoJobStore** — durable scheduling without an EF Core `DbContext`.
3. **Co-located tables** — `QRTZ_` prefix in the application database, no separate schema.
4. **`UseSystemTextJsonSerializer` + `UseProperties = true`** — aligns with project `System.Text.Json` usage and keeps scheduler rows free of binary-serialized application types.
5. **No backward compatibility** — development mode, clean swap.
6. **(New) `QuartzSchedulerSettings`, not `QuartzSchedulerOptions`** — the planned name collides with the public `Quartz.QuartzSchedulerOptions` type.
7. **(New) DDL authored independently from schema interface facts** — AGENTS.md Rule #8 forbids ingesting third-party SQL. Table and column names are interoperability facts extracted from the assembly's own SQL templates; the scripts' structure, guards, ordering, and comments are project-native. Notably the scripts are **non-destructive** — no `DROP`/`TRUNCATE` — unlike upstream initialization scripts, so they are safe to apply on every startup.
8. **(New) Schema applied at API startup, not by `Event.MigrationService`** — mirrors the TickerQ path being replaced and avoids duplicating the embedded resources into a second project. Controlled by `Scheduler:Quartz:ApplySchemaOnStartup`.
9. **(New) Scheduler-level retries dropped for one-off reminders** — the recurring `email-dispatch-drain` is the single retry authority. A failed wake-up leaves the outbox row due for the next pass, which removes a second, competing retry policy.
10. **(New) Read-only status endpoint instead of a dashboard UI** — see `tasks.md` → Deferred Work for the `Quartz.Dashboard` trade-off.

## Constraints And Rules To Remember
- Quartz.NET is referenced ONLY in `Explore.API` (Clean Architecture) — enforced by `DurableSideEffectBoundaryTests`.
- Application-layer contracts (`IScheduledJobRegistry`, `ScheduledJobDescriptor`, `ScheduledJobNames`, `IScheduledEmailDispatchTrigger`) keep their signatures unchanged.
- Every file starts with a two-line ABOUTME comment.
- Validators are manually instantiated (no DI).
- The scheduler status endpoint must stay behind the instance-admin authorization policy.
- `Scheduler:Quartz:TablePrefix` is inlined into DDL, so it must remain `[A-Za-z0-9_]+`; both the validator and the initializer enforce this.

## Validation Baseline

Standard commands; no SDK workaround required (workload set repaired 2026-08-15).

| Phase | Build | Test | Result |
|---|---|---|---|
| 1 | `dotnet build --configuration Release --verbosity quiet` | `Event.Architecture.Tests` | ✅ 0 build errors; 378 passed / 4 pre-existing failures |
| 2 | same | `Event.API.IntegrationTests` | ✅ scheduler + dispatch suites green |
| 3 | same | `Event.API.IntegrationTests` | ✅ `Quartz*` 26/26, `EmailDispatch*` 45/45 |
| 4 | same | `Event.Architecture.Tests` | ✅ no new failures vs. baseline |

Baseline comparison was performed in a detached git worktree at `HEAD` (62f94b751): baseline fails 7, this workstream fails 4, and the 4 are a strict subset touching files this workstream never modified.

## Current Known Risks / Unknowns

1. **Non-SQLite DDL is structurally asserted but not executed in CI.** SQLite is proven end-to-end (`QuartzSqliteDurableSchedulingTests`); PostgreSQL, SQL Server, and MySQL scripts are only asserted for completeness and non-destructiveness. A Testcontainers PostgreSQL round-trip would close this.
2. **Clustering is implemented and validated but untested** against real multi-node contention.
3. **Resolved:** cron format (corrected to `?` day-of-week form), `JobDataMap` round-trip (proven by tests), and per-provider `IF NOT EXISTS` guards (implemented and asserted).

## Handoff Notes

### Handoff — 2026-08-15 Europe/Brussels (implementation complete)
- **Current state:** All 4 phases implemented and verified. Build green; scheduler/dispatch tests green; zero TickerQ references in `src/`, `tests/`, `docs/`.
- **Next action:** User review; then decide the `Quartz.Dashboard` question in `tasks.md` → Deferred Work.
- **Blockers:** None. The SDK workload set was repaired on 2026-08-15; all verification below was re-run with plain `dotnet build` / `dotnet test` and stayed green.
- **Modified files:** `Directory.Packages.props`; `src/Explore.API/**` (config, extensions, hosting, health, scheduling, resources, appsettings); `src/Explore.Infrastructure/EmailDispatchProcessorSettings*.cs`; `src/Explore.ServiceDefaults/Extensions.cs`; `src/Event.Standalone/Hosting/StandaloneHostApplicationExtensions.cs`; `src/Explore.Application/Contracts/**` and `Services/ScheduledJobRegistry.cs` (comments + cron text); `tests/**` (Quartz suites, architecture guardrails); 13 `docs/**` files; all `packages.lock.json` regenerated.
- **Unrelated dirty files the next contributor must avoid:** `dev/report/quartznet-background-jobs-selection-report.md` (pre-existing untracked report for this workstream). The 4 pre-existing architecture-test failures belong to other active workstreams (`registration-data-collection`, Blazor client DTOs, privacy PII inventory) — do not attempt to fix them here.
- **Validation:** See Validation Baseline above.
- **Documentation impact:** Completed in Phase 4; no outstanding doc debt.
