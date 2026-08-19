<!-- ABOUTME: Plan for migrating queue-driven polling workers to Quartz.NET with enterprise-grade readiness gates and operator-safe sequencing. -->
<!-- ABOUTME: Keeps BackgroundService polling at arm's length and makes recovery/deletion criteria explicit before execution. -->

# Queue-Driven & Outbox-Drain Worker Migration to Quartz.NET — Implementation Plan

Last Updated: 2026-08-19 Europe/Brussels

## 0. Planning Metadata

- **Original Request:** Add a production-safe implementation plan to migrate polling queue workers to Quartz.NET with explicit multi-node de-duplication and crash-window recovery proof, while keeping self-hosting/operator concerns first.
- **Task Directory:** `dev/active/queue-driven-worker-migration/`
- **Planning Status:** Draft — Awaiting User Approval (CTO feedback integrated)
- **Matched Intents:** `schedule-background-work` (primary), `add-cqrs-handler`, `external-infrastructure-bootstrap`
- **Relevant Skills:** `implementation-plan`, `senior-cto-feedback`, `outbox-pattern`, `clean-architecture-rules`, `dotnet-efcore-guidelines`
- **Relevant Rules:** `.agents/rules/api-scheduling.md`, `.agents/rules/application-layer.md`, `.agents/rules/efcore-persistence.md`, `.agents/rules/tests.md`
- **Primary Layers Touched:** `Explore.API`, `Explore.Application`, `Explore.Persistence`, `Explore.Infrastructure`, `tests/Event.Architecture.Tests`, `tests/Event.API.IntegrationTests`, `tests/Event.Persistence.IntegrationTests`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/OUTBOX_PATTERN.md`.
- **Complexity:** **XL (high-risk infrastructure + correctness migration)**
- **Senior-CTO Position:** Approve with required changes (plan now requires explicit sequencing gates before each migration wave).

---

## 1. Executive Summary

This workstream migrates polling queue-driven background workers from `BackgroundService` loops to Quartz-powered periodic jobs.

This is not just a scheduler swap:
- It is a correctness migration to remove multi-replica duplicate side effects and stale-lease ambiguity.
- It is an operations migration with explicit gating so self-hosters can recover from failures safely.
- It assumes a pre-v1 posture: breaking changes are accepted, and legacy polling worker paths are removed, not shimmed.

**What is changed now:**
- Introduce gate-first sequencing: prove Quartz multi-node behavior and stale-lease crash recovery before queue-worker conversion.
- Migrate outbox-related drain workers out of hosted loops in a two-wave approach:
  1) low-risk queue workers,
  2) optional/conditional migration of `OutboxProcessor` and `PdsSyncWorker` only after proof gates and operational runbook readiness.
- Remove deprecated worker classes and compose scheduling only through Quartz job registrations + service contracts.

**What is explicitly out of scope:** API contract shape, BFF rendering, and external queueing topology changes are not in scope. This work is runtime scheduling + persistence semantics only.

---

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Source |
|---|---|---|---|
| Email dispatch already runs as Quartz jobs | `email-dispatch-drain` + `email-dispatch-recovery-scan` are registered in `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`; job docs exist in `docs/OPERATIONS.md` | High | Source files + docs |
| Queue-driven polling workers still run as hosted services | `AddApiBackgroundProcessing` registers `OutboxProcessor`, `PdsSyncWorker`, `WebhookDeliveryProcessor`, `IntegrationSyncProcessor`, `IncomingWebhookProcessor`, `IncomingWebhookEffectProcessor` | High | `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` |
| Operational gate is explicit in architecture docs | `docs/OPERATIONS.md` marks general outbox and PDS as planned-only and explicitly says they are not yet migrated before proof | High | `docs/OPERATIONS.md` |
| Worker job patterns exist in current architecture | Existing tests and jobs already validate Quartz clustering (`QuartzClusteringTests.cs`) and queue-job health paths | Medium | `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs`, scheduling jobs |
| API architecture requires scheduler-neutral application layer | Scheduling is owned in API, Quartz details must remain in API layer | High | `.agents/rules/api-scheduling.md`, `docs/ARCHITECTURE.md` |

### 2.2 Existing Implementation

Current poller implementations are in:
- `src/Explore.API/BackgroundServices/OutboxProcessor.cs`
- `src/Explore.API/BackgroundServices/PdsSyncWorker.cs`
- `src/Explore.API/BackgroundServices/WebhookDeliveryProcessor.cs`
- `src/Explore.API/BackgroundServices/IntegrationSyncProcessor.cs`
- `src/Explore.API/BackgroundServices/IncomingWebhookProcessor.cs`
- `src/Explore.API/BackgroundServices/IncomingWebhookEffectProcessor.cs`

Current Quartz registration points include:
- `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` (`AddCronJob` / `AddSweepJob`)
- `src/Explore.API/Scheduling` job handlers
- `src/Explore.API/Configuration/QuartzSchedulerSettings*` for validation/settings.

### 2.3 Existing Tests And Verification Coverage

Validated:
- API integration clustering proof exists (trigger-level exactly-once under PostgreSQL clustering) in `QuartzClusteringTests`.
- Architecture baseline ratchet exists in `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`.
- Multiple outbox lifecycle tests exist (`OutboxProcessorDeadLetterTests`) but do not yet cover queue-driven worker migration under a Quartz duplicate/ crash scenario.

Missing for this workstream:
- Quartz-driven duplicate-proof for outbox/queue worker side effects, not just scheduler trigger execution.
- Stale-lease recovery proof using real worker drain state for this worker family.

### 2.4 Existing Documentation And Contracts

Current docs already cover these contracts and constraints:
- `docs/OPERATIONS.md` (job catalog, clustering guidance, `registration-finalization-drain` migration precedent).
- `docs/CONFIGURATION.md` (sweep job settings and queue worker settings patterns).
- `docs/OUTBOX_PATTERN.md` (durable intent, claim semantics, retries, dead-letter semantics).
- `docs/ARCHITECTURE.md` (layer boundaries and runtime ownership).

### 2.5 Current Pain Points / Improvement Areas

- No direct "queue-worker conversion + stale recovery" evidence currently gates worker migration.
- Legacy queue-worker classes increase the risk of duplicate processing under self-hosted multi-node scale-out.
- Operator change surface is under-documented if conversion removes worker ownership abruptly (new job identifiers, health visibility, alerts, recovery flow).

### 2.6 Unknowns After Investigation

- Max supported tenant scale for queued recovery scans on heavy PDS/outbox rows.
- Acceptable recovery-time SLOs per worker family (to set explicit deadlines in the runbook).
- Whether tenants will require a temporary dual-run phase during upgrade; if so, this must be documented as an exception.

---

## 3. Proposed Future State

At completion:
- All queue-driven background workers execute through Quartz jobs with `[DisallowConcurrentExecution]`.
- Legacy polling loops for those workers are removed (no compatibility wrappers retained).
- Lease ownership, tenant binding, and idempotent processing remain in application services, not Quartz jobs.
- Job identifiers in `ScheduledJobNames` become operator contracts (`registration`, `runbook`, log, alert references).
- Recovery paths are explicit:
  - active drain job + periodic recovery scan where required,
  - no duplicated side effects after lease expiry,
  - stale rows reconciled into bounded retry/unknown states with operator-led recovery.

Non-negotiable keep-out:
- Keep stream/event-driven workers as hosted services (`AtprotoJetstreamSubscriber`, RabbitMQ consumers).
- Keep `EmailDispatchProcessor` semantics intact as the fallback transport path where applicable.

---

## 4. Non-Negotiable Constraints

1. **Clean Architecture, Scheduling Layering:** Quartz and scheduler registration remain in API; application owns domain contracts and processing semantics.
2. **Job Contract Discipline:** Jobs are one-pass wrappers. Enablement, cadence, retry policy, DI scope, and exception containment are handled by scheduler/service boundaries.
3. **Operator Contract Stability:** `ScheduledJobNames` + trigger names are treated as operational schema; no renaming without runbook migration notes.
4. **Fail-Closed Recovery:** Lease token mismatch, claim expiry mismatch, or tenant-context mismatch must not execute remote side effects.
5. **Pointer-Only Triggers:** No business payload in `JobDataMap`; durable state lives in persistence tables.
6. **No compatibility shims:** Pre-v1 breaking-change policy accepts deletion of legacy polling workers after migration verification.
7. **Self-hosting recovery first:** A worker conversion cannot start without a proven queue-drain duplicate/recovery path on clustered PostgreSQL.
8. **HAL trust boundary maintained:** Authorization and UI action visibility continue to be HAL-driven; this migration changes backend execution only.

---

## 5. Architecture And Design Decisions

### Decision 1 — Gate-First Migration (Required)
Migration moves in waves only after green cluster + crash recovery proof for the queue-worker lane being migrated. This avoids turning correctness debt into deployment incidents.

### Decision 2 — Two-Wave Workstream
- Wave A (lower coupling): `WebhookDeliveryProcessor`, `IntegrationSyncProcessor`, `IncomingWebhookProcessor`, `IncomingWebhookEffectProcessor` first.
- Wave B (high coupling): `OutboxProcessor` and `PdsSyncWorker` only after the gate passes and their tenant/lease behavior is observed stable in staging-like integration tests.

### Decision 3 — Delete Old Paths on Completion
No shadow-mode adapters. Once Quartz conversion is validated, remove `BackgroundService` registrations and worker files for migrated types to prevent dual semantics.

### Decision 4 — Recovery Coverage Is the Exit Criterion
Every converted worker must have:
- at-least-once idempotent processing path,
- stale-lease scan/recovery path,
- explicit operator recovery evidence before production rollout.

### Decision 5 — PR Split by Risk Boundary
Plan is split so that data/persistence fixes, service contracts, scheduler integration, and worker deletion do not land in one PR.

---

## 6. Implementation Phases

### Phase 1: Quartz Cluster Safety & Crash-Recovery Gate (No worker deletion yet)
- **Goal:** Establish hard evidence that duplicate execution and stale-lease recovery are proven before modifying worker ownership.
- **Files:**
  - `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs` [MODIFY]
  - `tests/Event.API.IntegrationTests/Features/OutboxProcessorDeadLetterTests.cs` [MODIFY] (or new focused lease-recovery file if ownership is cleaner)
  - `tests/Event.API.IntegrationTests/Features/QuartzOutboxClusteringTests.cs` [NEW]
  - `tests/Event.API.IntegrationTests/Fixtures/QuartzPostgreSqlSchedulerFixture.cs` [MODIFY]
- **Acceptance criteria:**
  - One test proves one-shot trigger execution under two nodes sharing one PostgreSQL store.
  - One test simulates mid-flight lease expiry/recovery and proves no duplicate external intent is produced.
  - Recovery behavior is recorded in test evidence for enterprise operators.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 2: Application Drain Contracts & Scheduling Catalog
- **Goal:** Introduce explicit drain contracts and job name registry for every queue-driven conversion target.
- **Files:**
  - `src/Explore.Application/Contracts/Services/IGeneralOutboxDrainService.cs` [NEW]
  - `src/Explore.Application/Contracts/Services/IPdsSyncDrainService.cs` [NEW]
  - `src/Explore.Application/Contracts/Services/IWebhookDeliveryDrainService.cs` [NEW]
  - `src/Explore.Application/Contracts/Services/IIntegrationSyncDrainService.cs` [NEW]
  - `src/Explore.Application/Contracts/Services/IIncomingWebhookDrainService.cs` [NEW]
  - `src/Explore.Application/Contracts/Services/IIncomingWebhookEffectDrainService.cs` [NEW]
  - `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs` [MODIFY]
- **Acceptance criteria:**
  - Contracts expose `ProcessBatchAsync` and `RecoverStaleProcessingAsync` with result metadata that drives recovery decisions.
  - Job names for all six jobs are present in `ScheduledJobNames`.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

### Phase 3: Persistence Lease Semantics and Cross-Tenant Safety
- **Goal:** Make claim/recovery semantics uniform and tenant-safe before conversion.
- **Files:**
  - `src/Explore.Application/Contracts/Persistence/IOutboxRepository.cs` [MODIFY]
  - `src/Explore.Persistence/Repositories/OutboxRepository.cs` [MODIFY]
  - `src/Explore.Persistence/Repositories/PdsSyncOutboxRepository.cs` [MODIFY]
  - `src/Explore.Persistence/Repositories/WebhookDeliveryAttemptRepository.cs` [MODIFY]
  - `src/Explore.Persistence/Repositories/IntegrationSyncOutboxRepository.cs` [MODIFY, if present]
  - `tests/Event.Persistence.IntegrationTests/Repositories/OutboxRepositoryClaimTests.cs` [MODIFY]
- **Acceptance criteria:**
  - Atomic claim, lease-owner, and lease token checks are verified for overlapping workers.
  - Reclamation path is tenant-safe and never bypasses tenant context.
  - Stale rows transition into bounded recovery states without side-effect replay.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 4: Quartz Job Implementation (Wave A)
- **Goal:** Convert low-risk queue drains to Quartz without touching high-coupling `OutboxProcessor` and `PdsSyncWorker` yet.
- **Files:**
  - `src/Explore.API/Scheduling/WebhookDeliveryDrainJob.cs` [NEW]
  - `src/Explore.API/Scheduling/IntegrationSyncDrainJob.cs` [NEW]
  - `src/Explore.API/Scheduling/IncomingWebhookDrainJob.cs` [NEW]
  - `src/Explore.API/Scheduling/IncomingWebhookEffectDrainJob.cs` [NEW]
  - `src/Explore.Application/Services/WebhookDeliveryDrainService.cs` [NEW]
  - `src/Explore.Application/Services/IntegrationSyncDrainService.cs` [NEW]
  - `src/Explore.Application/Services/IncomingWebhookDrainService.cs` [NEW]
  - `src/Explore.Application/Services/IncomingWebhookEffectDrainService.cs` [NEW]
  - `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` [MODIFY]
- **Acceptance criteria:**
  - All four jobs use `[DisallowConcurrentExecution]`.
  - Scheduler config and cron settings are loaded through existing settings patterns.
  - Tenant context is set before service call boundaries.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 5: Wave A Decommission
- **Goal:** Remove legacy worker registrations and classes only for migrated Wave A workers.
- **Files:**
  - `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY]
  - `src/Explore.API/BackgroundServices/WebhookDeliveryProcessor.cs` [DELETE]
  - `src/Explore.API/BackgroundServices/IntegrationSyncProcessor.cs` [DELETE]
  - `src/Explore.API/BackgroundServices/IncomingWebhookProcessor.cs` [DELETE]
  - `src/Explore.API/BackgroundServices/IncomingWebhookEffectProcessor.cs` [DELETE]
  - `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs` [MODIFY]
- **Acceptance criteria:**
  - `AddApiBackgroundProcessing` no longer registers the deleted workers.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 6: Conditional Wave B — `OutboxProcessor` and `PdsSyncWorker`
- **Goal:** Migrate remaining two workers only when Phase 1 gate and Wave A evidence are green.
- **Trigger condition:** Phase 1 tests green + Phase 4+5 review sign-off + upgrade runbook ready.
- **Files:**
  - `src/Explore.Application/Services/GeneralOutboxDrainService.cs` [NEW]
  - `src/Explore.Application/Services/PdsSyncDrainService.cs` [NEW]
  - `src/Explore.API/Scheduling/GeneralOutboxDrainJob.cs` [NEW]
  - `src/Explore.API/Scheduling/PdsSyncDrainJob.cs` [NEW]
  - `src/Explore.API/Scheduling/GeneralOutboxRecoveryScanJob.cs` [NEW]
  - `src/Explore.API/Scheduling/QuartzSchedulerExtensions.cs` [MODIFY]
  - `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY]
  - `src/Explore.API/BackgroundServices/OutboxProcessor.cs` [DELETE]
  - `src/Explore.API/BackgroundServices/PdsSyncWorker.cs` [DELETE]
  - `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs` [MODIFY]
- **Acceptance criteria:**
  - No `BackgroundService` polling remains for these workers.
  - Recovery scan runs within bounded interval and does not produce duplicate committed side effects after lease expiry.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 7: Documentation, Config, and Runbook Convergence
- **Goal:** Make operator behavior reproducible and observable.
- **Files:**
  - `docs/OPERATIONS.md` [MODIFY]
  - `docs/CONFIGURATION.md` [MODIFY]
  - `docs/OUTBOX_PATTERN.md` [MODIFY]
  - `docs/ARCHITECTURE.md` [MODIFY]
- **Acceptance criteria:**
  - Every moved worker appears in documented job catalog with exact cron, tenant scope, and recovery contract.
  - Migration upgrade note covers: stop service loop, run build, run gate test, restart topology, verify no duplicate processing.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## 7. Testing Strategy

| Phase | Risk |
|---|---|
| 1 | Cluster de-duplication and stale recovery correctness for worker lanes |
| 2 | Contract drift and compile-time slicing between API/Application |
| 3 | Lease correctness, tenant-safe persistence, stale reclaim |
| 4 | Scheduler registration and job-scoped execution boundaries |
| 5 | Runtime ownership transition from worker loops to Quartz |
| 6 | High-risk outbox/PDS conversion correctness |
| 7 | Documentation and operator runbook completeness |

Test project ownership is enforced by risk lane:
- API clustering/recovery proofs: `Event.API.IntegrationTests`
- Lease/persistence claims: `Event.Persistence.IntegrationTests`
- Contract and architecture invariants: `Event.Architecture.Tests`

---

## 8. Documentation, Configuration, And Operations Impact

- `docs/OPERATIONS.md`: job catalog updates, runbook checkpoints, expected self-hosting operator actions, alerting signals.
- `docs/CONFIGURATION.md`: required keys for each job family and deprecation of polling settings once workers are deleted.
- `docs/OUTBOX_PATTERN.md`: explicit statement that queue-driven workers remain outbox-authored in processing semantics and scheduler is orchestration only.
- `docs/ARCHITECTURE.md`: update background services matrix to show Quartz ownership for converted workers.

---

## 9. Security, Authorization, Privacy, And Abuse Considerations

- No authorization logic moves to workers; enforcement remains in existing service/handler boundaries.
- Multi-tenant filters and tenant context resets are required in every lease claim/recovery path.
- Logs and metrics must remain tenant-identity-safe and omit secrets/payloads/PII.
- Recovery outcomes that imply operator action (`Unknown`, `dead-letter`, `parked`) remain explicit and auditable.

---

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- Tenant isolation is preserved by contract-level tenant binding and repository query boundaries.
- Federation/ATProto flows stay in existing ownership boundaries; only orchestration timing changes.
- This is backend scheduling infrastructure, so no direct localization/accessibility surface changes.

---

## 11. Observability And Operations

Required metrics remain in telemetry layer:
- `explore.scheduler.job_executions` with bounded labels (`job_name`, `job_group`, `outcome`).
- `explore.scheduler.job_duration`.
- Job and recovery errors map to structured logs without payload/PII, enabling self-hosted incident forensics.

Recovery dashboards must show:
- stale claim counts by worker family,
- queue length/age by worker family,
- last duplicate-safe execution timestamp.

---

## 12. Migration And Compatibility Plan

### Compatibility Position
This migration **intentionally removes** legacy polling loop workers:
- `OutboxProcessor`
- `PdsSyncWorker`
- `WebhookDeliveryProcessor`
- `IntegrationSyncProcessor`
- `IncomingWebhookProcessor`
- `IncomingWebhookEffectProcessor`

Reason:
- This is pre-v1 development; compatibility mode adds confusion and duplicate work paths.
- Quartz ownership plus explicit recovery contract is cleaner and auditable.

Impact:
- Legacy worker settings and startup behavior are removed for moved workers.
- No shimmed dual-path execution is retained.

Migration steps for self-hosters:
- Deploy with feature-complete DB schema and Quartz PostgreSQL store.
- Run Phase 1 integration proof tests.
- Switch queue-worker workers only after phase gates pass.
- Use docs runbook to recover from `Unknown`/stale states before traffic return.

---

## 13. Risk Register

### 1. [Blocker] — Queue-worker conversion starts without proven duplicate/recovery behavior
**Why:** Multi-replica deployments can produce irreversible side effects.
**Signal:** New Phase 1 proofs are missing or failing.
**Fix:** Do not execute worker migration phases until this gate is green.

### 2. [Critical] — Tenant context not reconstructed inside Quartz job execution path
**Why:** Cross-tenant data leakage or wrong-tenant processing.
**Signal:** Recovery/retry path works only in single tenant.
**Fix:** Require tenant-aware claim and service entrypoint in every drain service.

### 3. [Critical] — `JobDataMap` carries payload data
**Why:** Stale trigger rows can recreate sensitive payloads after restart and increase blast radius.
**Fix:** Keep triggers pointer-only; always read state from DB.

### 4. [Major] — Recovery timings are left undocumented
**Why:** Self-hosters cannot predict when rows become safe to replay.
**Fix:** Add deterministic recovery SLA text in OPERATIONS and runbooks.

### 5. [Moderate] — Docs lag behind actual worker roster
**Why:** Alerts/operators continue searching for removed workers.
**Fix:** Update docs + API ratchet entries in same PR as conversion.

---

## 14. Success Metrics And Definition Of Done

- Zero duplicate job executions observed for converted workers under 2-node PostgreSQL cluster tests.
- Zero stale lease duplicates after induced worker termination/crash-window scenarios.
- `ApiLiabilityRatchetTests` exactly reflects migrated workers only.
- `docs/OPERATIONS.md` and `docs/CONFIGURATION.md` include explicit operator recovery steps.
- End-to-end build green and phase verification tests green for the migration slice executed.

---

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. Re-read this plan first, then `queue-driven-worker-migration-context.md` and `queue-driven-worker-migration-tasks.md` at task start.
2. Read only the current phase from the plan for implementation.
3. Update `tasks.md` immediately after substantial task completion.
4. Run phase-end verification once per phase.
5. Update `context.md` immediately on phase gate/blocker changes.

---

## 16. Progress Reporting Contract

After each slice, report:
- **Implemented:** concrete file-level deltas and tests run.
- **Verified:** evidence artifacts and exit criteria passed.
- **Remaining:** next phase gate status and blockers.
- **Docs:** doc changes required before proceeding to next phase.

---

## 17. Potential Risks & Unknowns

- PostgreSQL contention under high queue load may require batch-size and scan-interval tuning.
- Some workloads could need temporary pause windows during tenant maintenance if recovery windows are conservative.
- Self-hosted operator maturity varies; runbooks and health checks should be stronger than defaults because scheduler semantics are not obvious to most operators.
