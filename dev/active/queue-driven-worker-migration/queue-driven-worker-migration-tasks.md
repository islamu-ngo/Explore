<!-- ABOUTME: Executable task ledger for migrating API-hosted interval drains to Quartz.NET in risk-bounded waves. -->
<!-- ABOUTME: Keeps safety gates, file scope, acceptance evidence, verification, and operator cutover synchronized. -->

# Periodic Queue-Drain Migration to Quartz.NET — Task Checklist

Last Updated: 2026-08-20 Europe/Brussels

## Status Summary

- **Overall status:** Re-baselined and CTO-reviewed; ready for Phase 1 only.
- **Current priority:** Phase 1 safety gate.
- **Implementation tasks completed:** 0/16.
- **Current next:** 1.1.
- **Hard stop:** no hosted-service cutover before Phase 1 verification is green.

## Maintenance Rules

- Complete phases in dependency order; Phase 3 and later never bypass Phase 1 or 2.
- A Quartz job performs one bounded pass. Do not move claims, leases, fencing, retry, tenant context, or transport logic into API.
- Update operator docs in the same task as any job name, configuration, health, or runtime ownership change.
- Update this ledger immediately after each task and update the context whenever a gate or blocker changes.
- Run each phase verification once after all tasks in that phase; do not substitute solution-level `dotnet test`.

---

## Phase 1 — Contract Correction and EmailDispatch Safety Proof

- [ ] **1.1 Prove clustered EmailDispatch crash-window behavior end to end**
  - **Files:** `tests/Event.API.IntegrationTests/Features/EmailDispatchQuartzClusterRecoveryTests.cs` [NEW], `tests/Event.API.IntegrationTests/Fixtures/QuartzPostgreSqlSchedulerFixture.cs` [MODIFY], `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs` [MODIFY only if shared fixture support is required].
  - **Acceptance:** two PostgreSQL-backed Quartz nodes cause one real drain trigger; simulated transport acceptance followed by missing local settlement creates `Unknown`/operator reconciliation state and never a second automatic transport call.
  - **Evidence constraint:** test Quartz state carries pointers or no payload; no recipient, tenant, or message content is persisted in scheduler rows.

- [ ] **1.2 Remove the forbidden general-outbox scheduler promise**
  - **Files:** `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs`, `docs/OPERATIONS.md`, `docs/OUTBOX_PATTERN.md` [MODIFY].
  - **Acceptance:** `general-outbox-drain` is absent from catalog/status/tests/docs; `OutboxProcessor` remains registered, documented, and covered as an explicit hosted-service exception.

### Phase 1 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

---

## Phase 2 — Global Quartz Scheduler Authority

- [ ] **2.1 Decouple scheduler lifecycle from EmailDispatch mode**
  - **Files:** `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/Hosting/ApiHostApplicationExtensions.cs`, `src/Explore.API/Hosting/ApiHostStartupExtensions.cs` [MODIFY].
  - **Acceptance:** service registration, schema initialization, middleware, status/admin endpoints, and health are controlled by `Scheduler:Quartz:Enabled`; OpenAPI generation and `Testing` still suppress runtime scheduling.
  - **Breaking change:** replace the misleading `UseQuartzEmailDispatch` host-composition state with scheduler-wide state; do not add a compatibility property.

- [ ] **2.2 Make recurring registration conditional and prove the composition matrix**
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` [MODIFY], `tests/Event.API.IntegrationTests/Features/QuartzSchedulerCompositionTests.cs` [NEW].
  - **Acceptance:** EmailDispatch jobs exist only in Quartz mode; hosted-service email mode never registers them; maintenance jobs still register when Quartz is enabled; disabled scheduler and disabled-feature cases leave no dormant trigger.

- [ ] **2.3 Document scheduler authority and disabled behavior**
  - **Files:** `docs/OPERATIONS.md`, `docs/CONFIGURATION.md` [MODIFY].
  - **Acceptance:** self-hosters can identify the global enable flag, persistent-store/clustering requirements, health behavior, schema startup, and consequences of disabling scheduling.

### Phase 2 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 3 — Registration and Integration Drains

- [ ] **3.1 Add interval catalog contracts for the first wave**
  - **Files:** `src/Explore.Application/Contracts/Scheduling/ScheduledJobDescriptor.cs`, `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Acceptance:** add `ScheduledJobScheduleKind.Interval` and implemented descriptors for `registration-provider-submission-write-drain`, `registration-provider-subscription-lifecycle-drain`, and `integration-sync-drain`; names are included in bounded telemetry labels.

- [ ] **3.2 Add one-pass registration and integration Quartz jobs**
  - **Files:** `src/Explore.API/Scheduling/RegistrationProviderDrainJobs.cs`, `src/Explore.API/Scheduling/IntegrationSyncDrainJob.cs`, `tests/Event.API.IntegrationTests/Features/RegistrationAndIntegrationDrainQuartzJobsTests.cs` [NEW]; `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` [MODIFY].
  - **Acceptance:** each `[DisallowConcurrentExecution]` job calls exactly one existing command/service pass, has no payload, lets unexpected failures bubble, and logs only bounded counts using `Scheduled job {JobName} completed.`

- [ ] **3.3 Cut over ownership and remove first-wave timer wrappers**
  - **Files:** `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY]; `RegistrationProviderSubmissionWriteWorker.cs`, `RegistrationProviderSubscriptionLifecycleWorker.cs`, `IntegrationSyncProcessor.cs`, `IntegrationSyncHostedDrainRunner.cs` [DELETE]; `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md` [MODIFY].
  - **Acceptance:** one host cannot register old and new authorities together; 10-second/30-second/configured integration cadence is preserved; existing claim owner/fence/tenant semantics remain below the jobs.

### Phase 3 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 4 — Webhook Drains

- [ ] **4.1 Add webhook job contracts and one-pass jobs**
  - **Files:** `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs` [MODIFY]; `src/Explore.API/Scheduling/WebhookDrainJobs.cs`, `tests/Event.API.IntegrationTests/Features/WebhookDrainQuartzJobsTests.cs` [NEW].
  - **Acceptance:** implemented interval jobs exist for delivery, incoming intake, incoming effects, bulk replay, and provider publication; every job is payload-free and non-overlapping.

- [ ] **4.2 Register enabled webhook jobs and delete timer wrappers**
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY]; `WebhookDeliveryProcessor.cs`, `IncomingWebhookProcessor.cs`, `IncomingWebhookEffectProcessor.cs`, `WebhookBulkReplayProcessor.cs`, `WebhookProviderPublicationProcessor.cs` [DELETE].
  - **Acceptance:** existing settings control registration and cadence; delivery performs stale recovery before processing; provider publication performs publication before reconciliation; configured provider publication now actually runs when enabled.

- [ ] **4.3 Prove webhook safety and converge health/runbooks**
  - **Files:** existing Webhook Infrastructure/Persistence tests [MODIFY only for scheduler-boundary coverage], `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/WEBHOOK_OPERATIONS_RUNBOOK.md` [MODIFY].
  - **Acceptance:** concurrent claims, stale recovery, delivery fencing, bulk replay audit, and fresh tenant/machine-principal scopes remain green; health text and runbooks use Quartz job names and describe pause/backlog recovery.

### Phase 4 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 5 — PDS Drain Extraction and Cutover

- [ ] **5.1 Extract PDS orchestration below the scheduler boundary**
  - **Files:** `src/Explore.Application/Contracts/Services/IPdsSyncDrainService.cs`, `src/Explore.Infrastructure/Services/Federation/PdsSyncDrainService.cs`, `src/Explore.Infrastructure/Services/Federation/PdsSyncDrainSettings.cs` [NEW]; `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY].
  - **Acceptance:** move the current `RunOnceAsync` orchestration, stable process lease owner, scoped per-claim processing, and bounded parallelism without changing the `Atproto:PdsSync` configuration section or `AtprotoPdsDeliveryProcessor` behavior.

- [ ] **5.2 Add the PDS Quartz job and remove the worker**
  - **Files:** `src/Explore.API/Scheduling/PdsSyncDrainJob.cs` [NEW]; `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY]; `src/Explore.API/BackgroundServices/PdsSyncWorker.cs`, `src/Explore.API/BackgroundServices/PdsSyncWorkerOptions.cs` [DELETE].
  - **Acceptance:** `pds-sync-drain` is implemented, one-pass, payload-free, and non-overlapping; no PDS polling worker registration remains.

- [ ] **5.3 Prove PDS claim/fence recovery and document cutover**
  - **Files:** `tests/Explore.Infrastructure.Tests/Infrastructure/Federation/PdsSyncDrainServiceTests.cs`, `tests/Event.API.IntegrationTests/Features/PdsSyncDrainQuartzJobTests.cs` [NEW]; existing `AtprotoPdsDeliveryProcessorTests` and `AtprotoFederationPersistenceTests` [MODIFY only if a real uncovered defect is found]; `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/OUTBOX_PATTERN.md`, `docs/ARCHITECTURE.md` [MODIFY].
  - **Acceptance:** concurrent passes claim each row once; expired claims get new token/fence; stale settlement is refused; operator docs include longest-lease wait, backlog signals, and recovery/rollback.

### Phase 5 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 6 — Final Ratchet and Operator Release Gate

- [ ] **6.1 Close timer-loop and operator-contract gaps**
  - **Files:** `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, scheduler catalog tests, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/ARCHITECTURE.md`, `docs/OUTBOX_PATTERN.md` [MODIFY].
  - **Acceptance:** the ratchet detects `Task.Delay` and `PeriodicTimer`; only documented exceptions remain; enabled jobs, names, settings, health, logs, metrics, and docs agree exactly.

- [ ] **6.2 Execute and record self-hosted release rehearsal**
  - **Evidence:** single-node SQLite; two-node PostgreSQL persistent Quartz cluster; scheduler disabled; job pause/resume; node termination during a claimed batch; coordinated rollback after lease expiry.
  - **Acceptance:** no mixed-version runtime, no duplicate committed side effect, durable backlog recovers, readiness/status identifies disabled or failed scheduling, and no tenant/payload/secret data appears in Quartz or telemetry.

### Phase 6 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] All five test-project commands from Phases 3–5 are green without changed inputs being skipped.
- [ ] Operator rehearsal evidence is attached to the workstream context or release record.

## Deferred / Separate Workstreams

- `dead-letter-summary`, `waitlist-promotion-scan`, and `tenant-maintenance-scan` feature implementation.
- Infrastructure-hosted queue consumers outside `Explore.API/BackgroundServices` unless a separate inventory proves they are interval scheduling violations.
- Throughput/load-test tuning beyond existing configurable batch, interval, concurrency, and lease controls.
- The pre-existing package vulnerability and compiler/analyzer warnings reported by the baseline build; they are not changed by this planning task.
