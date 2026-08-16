# Quartz Scheduler Adoption — Context

Last Updated: 2026-08-16 Europe/Brussels

## SESSION PROGRESS (2026-08-16 Europe/Brussels)

### ✅ COMPLETED
- Planning created from `dev/report/quartznet-background-jobs-implementation-report.md`, scoped to the remaining priorities minus the dashboard.
- Current-state report completed with evidence from repository search and the shipped `Quartz 3.19.1` assembly.
- Verified the concurrent maintenance-sweep migration landed 8 processors into `Scheduling/MaintenanceSweepJobs.cs` (uncommitted).

### 🟡 IN PROGRESS
- Awaiting user review of the implementation plan. No runtime code was changed by this planning workstream.

### ⏭️ NEXT
1. User reviews and corrects or approves the plan.
2. **Wait for the concurrent maintenance-sweep work to be committed** before starting — see Blockers.
3. First implementation agent starts with Task 1.1 (enable Quartz schema validation).
4. Refresh context after Phase 1 completion.

### ⚠️ BLOCKERS

- **Concurrent agents are editing this working tree right now.** Two other workstreams are live:
  - The **maintenance-sweep migration** (8 processors → `Scheduling/MaintenanceSweepJobs.cs`) is **uncommitted**. It has already appended to `ScheduledJobNames.cs` and `QuartzSchedulerKeys.cs` — the same two files Phases 3 and 5 append to. **Starting Phase 3 before it is committed will conflict.**
  - `dev/active/quartz-dashboard-integration/` is owned by another agent and is **out of scope** for this workstream.
- **Docker availability is unconfirmed.** Tasks 1.2 and 1.3 depend on Testcontainers PostgreSQL. Other active workstreams record Docker as unavailable in this environment. The tasks are specified to skip visibly rather than fail, but if Docker is absent the clustering claim stays unproven and should be escalated, not silently skipped.
- **Tavily MCP is unavailable** (HTTP 432, plan usage limit) — external research used Context7 and direct assembly inspection instead.

## Quick Resume

1. Read this context and `quartz-scheduler-adoption-tasks.md`.
2. Read only the current phase, constraints, or changed decisions from `quartz-scheduler-adoption-plan.md`; do not reread the full unchanged plan on every resume.
3. Start from the first unchecked high-priority task unless the user overrides it.
4. Before editing `ScheduledJobNames.cs` or `QuartzSchedulerKeys.cs`, check `git status` for concurrent edits and **append only**.
5. Keep `tasks.md` current during implementation and update context/plan only at their defined triggers.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `src/Explore.Application/Contracts/Scheduling/IScheduledDeadlineDispatcher.cs` | **New** | Application | General "wake me at T" port | Replaces the single-purpose email trigger |
| `src/Explore.Application/Contracts/Scheduling/ScheduledDeadline.cs` | **New** | Application | Pointer-only deadline envelope | `IReadOnlyDictionary<string,string>` by design |
| `src/Explore.Application/Services/NoOpScheduledDeadlineDispatcher.cs` | **New** | Application | Default when no scheduler is registered | |
| `src/Explore.Application/Contracts/Infrastructure/IScheduledEmailDispatchTrigger.cs` | Existing → **DELETE** | Application | Single-purpose port | **Verified: no caller** |
| `src/Explore.Application/Contracts/Infrastructure/ScheduledEmailDispatchPointer.cs` | Existing → **DELETE** | Application | Its payload record | |
| `src/Explore.Application/Services/NoOpScheduledEmailDispatchTrigger.cs` | Existing → **DELETE** | Application | Its no-op | |
| `src/Explore.API/Scheduling/QuartzScheduledDeadlineDispatcher.cs` | **New** | API | Quartz implementation with cancel support | Deterministic trigger keys |
| `src/Explore.API/Scheduling/QuartzScheduledEmailDispatchTrigger.cs` | Existing → **DELETE** | API | Replaced by the dispatcher | |
| `src/Explore.API/Scheduling/InventoryHoldExpiryJob.cs` | **New** | API | One order's hold expiry at its deadline | Tenant context set/cleared in `finally` |
| `src/Explore.API/Scheduling/InventoryHoldExpiryReconciliationJob.cs` | **New** | API | Safety-net sweep | Covers recovery targets and missed deadlines |
| `src/Explore.API/BackgroundServices/InventoryHoldExpiryWorker.cs` | Existing → **DELETE** | API | 60-second poll being replaced | Logic reused verbatim in the two jobs |
| `src/Explore.API/Scheduling/RegistrationFinalizationDrainJob.cs` | **New** | API | Cron drain | Timer-only migration |
| `src/Explore.API/BackgroundServices/RegistrationFinalizationWorker.cs` | Existing → **DELETE** | API | 10-second poll | |
| `src/Explore.API/Scheduling/SchedulerTelemetryJobListener.cs` | **New** | API | Uniform job telemetry | **Must be exception-contained** |
| `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` | Existing | API | Registration + schema validation | ⚠️ Concurrently edited |
| `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs` | Existing | API | Job/trigger keys | ⚠️ Concurrently edited — **append only** |
| `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs` | Existing | Application | Job-name constants | ⚠️ Concurrently edited — **append only** |
| `src/Explore.API/Scheduling/MaintenanceSweepJobs.cs` | Existing | API | 8 migrated sweeps | 🚫 **Do not modify** — other agent owns it |
| `.agents/contract/intents.yaml` | Existing | Contract | Intent registry | New `schedule-background-job` intent |

## Key Decisions

1. **Quartz stays behind an Application-owned port** — the Application layer never references a scheduler; enforced by `DurableSideEffectBoundaryTests`.
2. **One general deadline port replaces the single-purpose email trigger** — justified because the existing port has **no caller**, so generalizing costs nothing, and five use cases are known.
3. **Precise trigger + reconciliation sweep, never either alone** — the trigger provides latency, the sweep provides the correctness guarantee. Mirrors the proven email-dispatch design.
4. **Deadlines are keyed per order and explicitly cancelled** on terminal transitions, to bound trigger growth.
5. **One exception-contained `IJobListener` for telemetry** — Quartz documents that unhandled listener exceptions can disrupt scheduling.
6. **`PerformSchemaValidation` on by default** — the platform already shipped one silent-degradation defect (`MISFIRE_ORIG_FIRE_TIME`).
7. **Deadline registration must never fail order creation** — it is an optimization; correctness rests on the sweep.
8. **No backward compatibility** — deleted ports and workers, no shims, no dormant flagged classes.

## Constraints And Rules To Remember

- **Matched intents:** none exist (fallback contract). Task 5.2 adds `schedule-background-job`.
- Quartz types only in `Explore.API`; Application and Domain stay scheduler-free.
- Pointer-only `JobDataMap` — no PII, payloads, secrets, or provider identifiers.
- Two-line `ABOUTME:` header on every file; file-scoped namespaces.
- Validators manually instantiated (no DI).
- Repositories return entities, never DTOs; UUIDv7 for aggregate identity.
- Listener methods must be exception-contained.
- No EF migration for scheduler tables; scheduler DDL stays non-destructive.
- 🚫 Do not touch `MaintenanceSweepJobs.cs` or `dev/active/quartz-dashboard-integration/`.

## Validation Baseline

For every implementation phase: one Release build and at most one fastest relevant non-browser project test, both run once after the phase tasks. No SDK workaround is required — the workload set was repaired on 2026-08-15.

| Phase | Build | Test |
|---|---|---|
| 1 | `dotnet build --configuration Release --verbosity quiet` | `Event.API.IntegrationTests` |
| 2 | same | `Event.Architecture.Tests` |
| 3 | same | `Event.API.IntegrationTests` |
| 4 | same | `Event.API.IntegrationTests` |
| 5 | same | `Event.Architecture.Tests` |

**Known pre-existing failures (not caused by this workstream):** `Event.Architecture.Tests` fails 4 at baseline — `BlazorIsolationArchitectureTests`, `NamingConventionTests.DTOs_ShouldEndWith_Dto`, `PersistenceTenantFilterArchitectureTests`, `Privacy.UserPiiInventoryArchitectureTests`. `AuthorizationProductionGuardrailTests` fails 3 in `Event.API.IntegrationTests`. Both sets were confirmed identical on a clean worktree at `62f94b751`. Do not attempt to fix them here; another workstream is actively working on them.

## Current Known Risks / Unknowns

1. **Merge conflict with the concurrent sweep work** in `ScheduledJobNames.cs` / `QuartzSchedulerKeys.cs` — Tasks 3.1, 5.1. Append only; prefer starting after it commits.
2. **Docker availability** for Testcontainers PostgreSQL — Tasks 1.2, 1.3. Skip visibly; escalate rather than silently drop the clustering proof.
3. **Whether clustered SQLite can support the clustered lock handler** — Task 1.3. If not, the proof becomes Docker-dependent.
4. **Full enumeration of terminal order transitions** for deadline cancellation — Task 3.2. The creation call site is verified; completion/cancellation paths are not fully enumerated.

## Handoff Notes

### Handoff — 2026-08-16 Europe/Brussels

- **Current state:** Planning complete and internally cross-checked. No runtime code changed by this workstream.
- **Next action:** User approves or corrects the plan; then Phase 1 Task 1.1, ideally after the concurrent sweep work is committed.
- **Blockers:** Concurrent agents editing the working tree; unconfirmed Docker availability. See ⚠️ BLOCKERS.
- **Modified files:** only `dev/active/quartz-scheduler-adoption/` (3 planning files created).
- **Validation:** none required — planning artifacts only.
- **Documentation impact:** `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and `.agents/contract/intents.yaml` are updated inside their owning implementation tasks, not as separate documentation tasks.
- **Risks:** see Risk Register in the plan; the sharpest is Task 3.2 reaching into a transactional order-creation handler on the revenue path.
- **Notes for next contributor/agent:** This working tree currently contains uncommitted work from at least two other agents, including an in-flight refactor of `Explore.API/Controllers/` and the registration-data-collection workstream. Before running a build, confirm whether a failure belongs to you. Read §17 of the plan before starting Phase 3.
