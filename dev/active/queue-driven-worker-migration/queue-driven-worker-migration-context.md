<!-- ABOUTME: Context and resume state for queue-driven and outbox-drain worker migration to Quartz.NET. -->
<!-- ABOUTME: Keeps the resume-critical state, risks, and execution gate between plans and tasks. -->

# Queue-Driven & Outbox-Drain Worker Migration to Quartz.NET — Context

Last Updated: 2026-08-19 Europe/Brussels

## SESSION PROGRESS (2026-08-19 Europe/Brussels)

### ✅ COMPLETED
- Read and synchronized plan with Senior CTO enterprise-grade feedback.
- Added explicit production gate: Quartz multi-node duplicate and stale-lease recovery proof required before any worker migration.
- Added two-wave migration approach to isolate lower-risk queue workers from higher-coupling `OutboxProcessor` / `PdsSyncWorker`.

### 🟡 IN PROGRESS
- Context is to be resumed by implementation at **Phase 1** (safety gate) once you want to start execution.

### ⏭️ NEXT
1. Run Phase 1 evidence work:
   - extend `QuartzClusteringTests` and add crash-recovery proof.
2. Proceed only after Phase 1 green.
3. Begin implementation of Phase 2 contracts and job registry expansion.

### ⚠️ BLOCKERS
- Phase 1 safety gate not complete (no green cluster + stale-lease recovery proof for queue-worker families yet).

---

## Quick Resume

1. Read this context, then `queue-driven-worker-migration-tasks.md`.
2. Read only the current implementation phase in `queue-driven-worker-migration-plan.md`.
3. Start from the first unchecked task in the active phase.
4. Do not execute worker deletion until Phase 1 and preceding phase(s) are green.

---

## Key Files And Responsibilities

### Plan and Control
- `dev/active/queue-driven-worker-migration/queue-driven-worker-migration-plan.md` — execution strategy and phased architecture.
- `dev/active/queue-driven-worker-migration/queue-driven-worker-migration-tasks.md` — operational slice ledger.

### Execution Targets
- `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` — polling service registration and final cutover point.
- `src/Explore.API/Scheduling/*.cs` — new Quartz jobs and scheduler wiring.
- `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` — job registration + settings.
- `src/Explore.Application/Contracts/Services/*.cs` — new drain contracts.
- `src/Explore.Persistence/Repositories/*` — lease claim and recovery query work.
- `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs` — multi-node proof lane.
- `tests/Event.Persistence.IntegrationTests/Repositories/OutboxRepositoryClaimTests.cs` — claim/recovery invariants.
- `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs` — worker roster ratchet.

### Documentation
- `docs/OPERATIONS.md`
- `docs/CONFIGURATION.md`
- `docs/OUTBOX_PATTERN.md`
- `docs/ARCHITECTURE.md`

---

## Key Decisions

1. **Gate-first migration**: no worker migration until Phase 1 proves duplicate prevention and recovery.
2. **No compatibility shims**: pre-v1 policy allows deleting old polling workers after verification.
3. **Two-wave migration**: Wave A lowers risk before touching `OutboxProcessor` and `PdsSyncWorker`.
4. **Tenant-safe recovery is mandatory**: every reclaimed lease path must preserve tenant context.
5. **Docs are deployment assets**: operator runbooks must be updated at each ownership change.

## Constraints And Rules To Remember

- Keep Quartz types in API only.
- No custom payload in `JobDataMap`.
- `[DisallowConcurrentExecution]` required on converted queue jobs.
- `ApiLiabilityRatchetTests` must stay synchronized with active and deleted workers.
- Self-hosting docs must include recovery behavior, health checks, and rollback path.

---

## Validation Baseline

| Check | Command | Baseline Status |
|---|---|---|
| Build | `dotnet build --configuration Release --verbosity quiet` | Baseline green expectation before each phase |
| Architecture Tests | `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` | Run at phase boundaries and after worker deletions |
| Persistence Tests | `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` | Run before/after lease contract edits |
| API Integration Tests | `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` | Run on scheduling and recovery proof phases |

---

## Current Known Risks / Unknowns

- Recovery SLOs for 3+ node deployments may require per-worker scan tuning.
- Wave B (`OutboxProcessor`/`PdsSyncWorker`) may require additional tenant/fence evidence before deletion.
- Long-running tenants with very high queue depth may need backpressure tuning before full rollouts.

---

## Handoff Notes

- **Current State:** planning artifacts updated, implementation not started.
- **Critical handoff condition:** phase implementation can start immediately at Phase 1 safety gate work.
- **Go/no-go checkpoint:** do not delete any worker class unless Phase 1 is green and the current phase’s verification commands pass.
