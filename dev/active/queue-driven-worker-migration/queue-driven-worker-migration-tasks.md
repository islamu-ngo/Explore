<!-- ABOUTME: Task checklist for queue-driven and outbox-drain worker migration to Quartz.NET. -->
<!-- ABOUTME: Converts execution safety gates into executable, phase-verified slices. -->

# Queue-Driven & Outbox-Drain Worker Migration to Quartz.NET — Task Checklist

Last Updated: 2026-08-19 Europe/Brussels

## Status Summary
- **Overall status:** Draft — Awaiting User Approval, Ready for Phase 1
- **Current priority:** Phase 1 (Safety Gate)
- **Completed:** 0/18 implementation tasks
- **Current next:** Phase 1.1

## Implementation Maintenance Rules

- Use this file as the active ledger; update checkboxes after meaningful work.
- Keep changes phase-scoped; avoid starting later phases before prior phase verification exits.
- Use one verification command per phase (one `dotnet build` plus one `dotnet test --project`).
- Do not start app/browser/manual runtime runs from this workstream.
- Update `queue-driven-worker-migration-context.md` whenever gate status or blockers change.

---

## Phase 1: Quartz Cluster Safety & Crash-Recovery Gate (No worker deletion yet)

- [ ] **1.1 Extend cluster trigger tests for queue worker behavior**
  - **Files:** `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs` [MODIFY]
  - **Acceptance:** test suite includes explicit evidence that clustered nodes cannot both execute the same queue-driven work window.
- [ ] **1.2 Add stale lease/crash recovery proof**
  - **Files:** `tests/Event.API.IntegrationTests/Features/OutboxProcessorDeadLetterTests.cs` [MODIFY] or `tests/Event.API.IntegrationTests/Features/QuartzOutboxClusteringTests.cs` [NEW]
  - **Acceptance:** recovery test proves stale processing ownership does not duplicate side effects.
- [ ] **1.3 Wire clustered fixture if required**
  - **Files:** `tests/Event.API.IntegrationTests/Fixtures/QuartzPostgreSqlSchedulerFixture.cs` [MODIFY]
  - **Acceptance:** fixture supports repeated node startup/shutdown and row-state assertions for two-node tests.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE 1 TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

---

## Phase 2: Contracts and Scheduling Catalog

- [ ] **2.1 Add dedicated drain contracts for converted workers**
  - **Files:** `src/Explore.Application/Contracts/Services/IGeneralOutboxDrainService.cs` [NEW], `src/Explore.Application/Contracts/Services/IPdsSyncDrainService.cs` [NEW], `src/Explore.Application/Contracts/Services/IWebhookDeliveryDrainService.cs` [NEW], `src/Explore.Application/Contracts/Services/IIntegrationSyncDrainService.cs` [NEW], `src/Explore.Application/Contracts/Services/IIncomingWebhookDrainService.cs` [NEW], `src/Explore.Application/Contracts/Services/IIncomingWebhookEffectDrainService.cs` [NEW]
  - **Acceptance:** each contract exposes batch+recovery results and can reject mismatched claim ownership.
- [ ] **2.2 Register job names and lane taxonomy**
  - **Files:** `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs` [MODIFY]
  - **Acceptance:** names are complete for all six potential queue-driven jobs and recovery scans.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE 2 TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 3: Persistence Claiming and Recovery Hardening

- [ ] **3.1 Add uniform claim/recovery semantics**
  - **Files:** `src/Explore.Application/Contracts/Persistence/IOutboxRepository.cs` [MODIFY], `src/Explore.Persistence/Repositories/OutboxRepository.cs` [MODIFY], `src/Explore.Persistence/Repositories/PdsSyncOutboxRepository.cs` [MODIFY], `src/Explore.Persistence/Repositories/WebhookDeliveryAttemptRepository.cs` [MODIFY], `src/Explore.Persistence/Repositories/IntegrationSyncOutboxRepository.cs` [MODIFY]
  - **Acceptance:** all converted workers use deterministic lease ownership checks and stale reclaim transitions.
- [ ] **3.2 Add/expand concurrency and stale recovery tests**
  - **Files:** `tests/Event.Persistence.IntegrationTests/Repositories/OutboxRepositoryClaimTests.cs` [MODIFY]
  - **Acceptance:** overlapping worker IDs cannot claim the same rows; stale rows are reclaimed only through bounded recovery rules.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE 3 TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

---

## Phase 4: Quartz Implementation (Wave A)

- [ ] **4.1 Implement Quartz jobs and services for wave-A workers**
  - **Files:** `src/Explore.API/Scheduling/WebhookDeliveryDrainJob.cs` [NEW], `src/Explore.API/Scheduling/IntegrationSyncDrainJob.cs` [NEW], `src/Explore.API/Scheduling/IncomingWebhookDrainJob.cs` [NEW], `src/Explore.API/Scheduling/IncomingWebhookEffectDrainJob.cs` [NEW], `src/Explore.Application/Services/WebhookDeliveryDrainService.cs` [NEW], `src/Explore.Application/Services/IntegrationSyncDrainService.cs` [NEW], `src/Explore.Application/Services/IncomingWebhookDrainService.cs` [NEW], `src/Explore.Application/Services/IncomingWebhookEffectDrainService.cs` [NEW]
  - **Acceptance:** jobs are single-pass, [DisallowConcurrentExecution], no scheduler payload content.
- [ ] **4.2 Register jobs and wiring in Quartz extensions**
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` [MODIFY], `src/Explore.API/Configuration/QuartzSchedulerSettings*.cs` [MODIFY]
  - **Acceptance:** wave-A cron cadence and enablement read from settings with consistent disabled-registration behavior.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE 4 TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

---

## Phase 5: Wave A Decommission

- [ ] **5.1 Remove wave-A hosted background services from startup**
  - **Files:** `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY]
  - **Acceptance:** `WebhookDeliveryProcessor`, `IntegrationSyncProcessor`, `IncomingWebhookProcessor`, `IncomingWebhookEffectProcessor` are no longer added as hosted services.
- [ ] **5.2 Delete wave-A worker implementations**
  - **Files:** `src/Explore.API/BackgroundServices/WebhookDeliveryProcessor.cs` [DELETE], `src/Explore.API/BackgroundServices/IntegrationSyncProcessor.cs` [DELETE], `src/Explore.API/BackgroundServices/IncomingWebhookProcessor.cs` [DELETE], `src/Explore.API/BackgroundServices/IncomingWebhookEffectProcessor.cs` [DELETE]
  - **Acceptance:** no startup path can create two active scheduler paths for these workers.
- [ ] **5.3 Ratchet update for moved workers**
  - **Files:** `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs` [MODIFY]
  - **Acceptance:** ratchet allows only intended active timer workers.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE 5 TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 6: Wave B Decommission (Conditional)

- **Precondition:** Phase 1 green + Phase 2/3/4/5 verification green + explicit migration/go/no-go accepted.
- [ ] **6.1 Implement Quartz jobs for `OutboxProcessor` and `PdsSyncWorker`**
  - **Files:** `src/Explore.API/Scheduling/GeneralOutboxDrainJob.cs` [NEW], `src/Explore.API/Scheduling/GeneralOutboxRecoveryScanJob.cs` [NEW], `src/Explore.API/Scheduling/PdsSyncDrainJob.cs` [NEW], `src/Explore.Application/Services/GeneralOutboxDrainService.cs` [NEW], `src/Explore.Application/Services/PdsSyncDrainService.cs` [NEW]
  - **Acceptance:** wave-B jobs pass idempotency/fencing and recovery checks under test harness.
- [ ] **6.2 Register wave-B jobs and remove polling workers**
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` [MODIFY], `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY], `src/Explore.API/BackgroundServices/OutboxProcessor.cs` [DELETE], `src/Explore.API/BackgroundServices/PdsSyncWorker.cs` [DELETE]
  - **Acceptance:** job-only ownership for both workers; no legacy poller registration paths remain.
- [ ] **6.3 Ratchet updates and final legacy-free verification**
  - **Files:** `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs` [MODIFY]
  - **Acceptance:** ratchet reflects final removed workers and allowed active timer loops.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE 6 TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 7: Docs, Config, and Runbook Convergence

- [ ] **7.1 Update runtime docs and config migration notes**
  - **Files:** `docs/OPERATIONS.md` [MODIFY], `docs/CONFIGURATION.md` [MODIFY], `docs/OUTBOX_PATTERN.md` [MODIFY], `docs/ARCHITECTURE.md` [MODIFY]
  - **Acceptance:** every worker migration has operator guidance, cron/cfg references, and recovery playbook.
- [ ] **7.2 Remove stale worker references and archive expected behavior**
  - **Files:** docs and any migration notes listed in tasks
  - **Acceptance:** no stale references to deleted polling paths in canonical docs.

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE 7 TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Remaining / Deferred Work

- Optional load-test calibration after Wave B completion if queue volume exceeds baseline assumptions.
- Optional add-on recovery observability dashboards for tenant-isolated stale-lease age metrics if operators require tighter runbook SLAs.
