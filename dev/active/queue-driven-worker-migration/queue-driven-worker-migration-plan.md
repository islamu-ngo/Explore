<!-- ABOUTME: Repository-grounded plan for moving API-hosted interval drains to Quartz.NET without moving durable processing semantics. -->
<!-- ABOUTME: Defines enterprise self-hosting gates, phased cutover, multi-node proof, and operator-safe rollback. -->

# Periodic Queue-Drain Migration to Quartz.NET — Implementation Plan

Last Updated: 2026-08-20 Europe/Brussels

## 0. Planning Metadata

- **Task Directory:** `dev/active/queue-driven-worker-migration/`
- **Planning Status:** Re-baselined — implementation may start at Phase 1; every cutover phase remains gated.
- **Senior CTO Verdict:** **Approve with required changes.** The required architecture, evidence, and rollout changes are encoded below.
- **Primary Intent:** `schedule-background-work`
- **Required Skills:** `senior-cto-feedback`, `implementation-plan`
- **Authoritative Rules:** `docs/QUICK_REFERENCE.md` rule 27 and `.agents/rules/api-scheduling.md`
- **Primary Layers:** `Explore.API`, `Explore.Application`, `Explore.Infrastructure`, tests, and operator documentation.
- **Estimated Complexity:** Large, but separable into independently shippable waves.
- **Compatibility Posture:** Pre-v1; breaking changes are allowed. No dual-mode compatibility layer will be added.

## 1. Executive Outcome

Move API-hosted **interval-driven** queue drains to Quartz.NET while leaving claim, lease, fencing, retry, tenant-context, and external-side-effect behavior in their existing services.

The durable `OutboxProcessor` remains a `BackgroundService`. It is an explicit repository exception and is not a Quartz migration candidate. The unsupported planned `general-outbox-drain` catalog entry is removed so the operator surface no longer promises a forbidden future state.

The implementation is intentionally incremental:

1. prove the existing EmailDispatch safety gate end to end;
2. make Quartz a platform scheduler independent of EmailDispatch mode;
3. migrate low-risk registration and integration drains;
4. migrate webhook drains;
5. extract and migrate the higher-risk PDS drain;
6. complete operator rehearsal and architecture ratchets.

No EF Core migration, Quartz schema change, new scheduler, message broker, or scheduler payload format is required.

## 2. Senior CTO Review Findings

### Blocker 1 — The previous plan violated the durable-outbox exception

The draft proposed deleting `OutboxProcessor` and creating `GeneralOutboxDrainJob`. That conflicts with both `docs/QUICK_REFERENCE.md` and `.agents/rules/api-scheduling.md`, which explicitly preserve `OutboxProcessor` as the durable side-effect authority.

**Required correction:** keep `OutboxProcessor` unchanged and remove the planned-only general-outbox job name, descriptor, test expectation, and operator documentation.

### Blocker 2 — The previous plan was not grounded in the current implementation

It proposed new contracts and services that already exist:

- `IWebhookDeliveryDrainService` / `WebhookDeliveryDrainService`
- `IIntegrationSyncDrainService` / `IntegrationSyncDrainService`
- `IIncomingWebhookDrainService` / `IncomingWebhookDrainService`
- `IIncomingWebhookEffectDrainService` / `IncomingWebhookEffectDrainService`
- `IWebhookProviderPublicationDrainService` / `WebhookProviderPublicationDrainService`
- `IWebhookBulkReplayService` / `WebhookBulkReplayService`

**Required correction:** reuse these one-pass boundaries and add only thin API-layer Quartz jobs.

### Blocker 3 — Quartz lifecycle is incorrectly coupled to EmailDispatch mode

`ApiHostServiceCollectionExtensions`, `ApiHostApplicationExtensions`, and `ApiHostStartupExtensions` currently compose, initialize, expose, and map Quartz only when `EmailDispatchProcessor:Mode=Quartz`. A self-hoster using hosted-service email mode therefore cannot reliably run maintenance jobs or the proposed drain jobs. Enabling Quartz for another drain would also register email Quartz jobs unconditionally, risking dual email authorities.

**Required correction:** make `Scheduler:Quartz:Enabled` the scheduler authority, and conditionally register EmailDispatch jobs only when EmailDispatch explicitly selects Quartz mode.

### Blocker 4 — The documented safety gate is not yet proven

`QuartzClusteringTests` proves one trigger is acquired once by two PostgreSQL-backed scheduler nodes. `EmailDispatchQuartzJobsTests` proves wrapper delegation. Neither proves the real crash window where a provider may accept work before local settlement.

**Required correction:** add an EmailDispatch end-to-end clustered drain test proving that an ambiguous stale lease becomes `Unknown` and is not automatically sent twice. No worker cutover starts before this is green.

### Critical 1 — The worker inventory was incomplete

The draft omitted active interval workers `WebhookBulkReplayProcessor`, `RegistrationProviderSubmissionWriteWorker`, and `RegistrationProviderSubscriptionLifecycleWorker`. It also omitted `WebhookProviderPublicationProcessor`, whose settings and runbook exist but which is not registered by the API host.

**Required correction:** include all four. Provider publication moves directly to Quartz; do not introduce a temporary hosted-service registration.

### Critical 2 — Scope had expanded into unrelated lifecycle features

`dead-letter-summary`, `waitlist-promotion-scan`, and `tenant-maintenance-scan` are separate product/operations features, not timer-wrapper migrations.

**Required correction:** remove them from this workstream. Their current planned catalog state is untouched and requires separate plans.

### Critical 3 — Mixed-version rollout can create dual authorities

A rolling deployment can run old hosted loops beside new Quartz jobs. Data claims reduce risk but cannot make every external side effect transactionally exactly once.

**Required correction:** use a coordinated stop/start upgrade and rollback, with lease-expiry waiting and no mixed-version window.

## 3. Verified Current State

| Runtime component | Current trigger | Existing one-pass boundary | Plan disposition |
|---|---|---|---|
| `OutboxProcessor` | `Task.Delay` loop | Processing is coupled to the worker | **Keep as hosted-service exception** |
| `IntegrationSyncProcessor` | `Task.Delay` loop | `IIntegrationSyncDrainService` | Migrate in Phase 3 |
| `RegistrationProviderSubmissionWriteWorker` | `PeriodicTimer` | `DrainRegistrationProviderSubmissionWriteEffectsCommand` | Migrate in Phase 3 |
| `RegistrationProviderSubscriptionLifecycleWorker` | `PeriodicTimer` | `RegistrationProviderSubscriptionLifecycleService.DrainOnceAsync` | Migrate in Phase 3 |
| `WebhookDeliveryProcessor` | `PeriodicTimer` | `IWebhookDeliveryDrainService` | Migrate in Phase 4 |
| `IncomingWebhookProcessor` | `PeriodicTimer` | `IIncomingWebhookDrainService` | Migrate in Phase 4 |
| `IncomingWebhookEffectProcessor` | `PeriodicTimer` | `IIncomingWebhookEffectDrainService` | Migrate in Phase 4 |
| `WebhookBulkReplayProcessor` | `PeriodicTimer` | `IWebhookBulkReplayService.ProcessQueuedAsync` | Migrate in Phase 4 |
| `WebhookProviderPublicationProcessor` | `PeriodicTimer`, but not host-registered | `IWebhookProviderPublicationDrainService` | Register directly as Quartz in Phase 4 |
| `PdsSyncWorker` | `Task.Delay` loop | `RunOnceAsync` is still embedded in worker | Extract and migrate in Phase 5 |

Existing evidence that must be preserved:

- PostgreSQL Quartz clustering: `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs`
- Webhook claim/fence/recovery: persistence and Infrastructure webhook tests
- Integration-sync claim/fence/retry: `IntegrationSyncDrainServiceTests`
- PDS lease reclaim and fencing: `AtprotoFederationPersistenceTests` and `AtprotoPdsDeliveryProcessorTests`
- Architecture timer-loop ratchet: `ApiLiabilityRatchetTests`
- Scheduler health/status/admin surfaces: `SchedulerHealthCheck` and Quartz scheduler endpoints

## 4. Scope Boundaries

### In Scope

- Decouple platform Quartz startup, schema initialization, middleware, and endpoints from EmailDispatch mode.
- Keep EmailDispatch Quartz jobs conditional on its explicit mode.
- Remove the misleading `general-outbox-drain` planned contract.
- Add stable names, keys, catalog descriptors, registrations, and one-pass jobs for the nine migration candidates.
- Reuse existing settings sections and intervals unless a setting moves layers for PDS.
- Delete replaced API timer wrappers and their now-unused runners.
- Strengthen architecture tests to detect both `Task.Delay` and `PeriodicTimer` loops.
- Update operator docs in the same slice as every operator-visible change.
- Prove single-node SQLite and clustered PostgreSQL behavior.

### Out of Scope

- Migrating or refactoring `OutboxProcessor`.
- Changing EmailDispatch delivery/recovery semantics.
- Implementing `dead-letter-summary`, `waitlist-promotion-scan`, or `tenant-maintenance-scan`.
- Replacing Quartz, adding a broker, or introducing a second scheduler.
- Changing repository claims, leases, fencing, or retry behavior unless a required proof exposes a real defect.
- EF Core migrations or hand-edited Quartz DDL.
- UI or public API changes.

## 5. Target Architecture

```text
Quartz trigger (no payload)
    -> API IJob (one pass, no retry/lease logic)
        -> existing scheduler-neutral drain/command/service
            -> durable repository claim + tenant/fence validation
                -> bounded external side effect
                    -> durable settlement/retry/dead-letter state
```

### Decision A — Quartz owns cadence only

Every job performs one bounded pass. It does not loop, sleep, drain until empty, create a retry policy, or catch unexpected drain failures. Unexpected failures bubble to Quartz and the existing telemetry listener; per-item failures remain handled by the drain service.

### Decision B — Preserve existing configuration keys

Existing `Enabled`, polling interval, initial delay, batch, concurrency, and lease settings remain authoritative. `AddSweepJob<TJob>` uses those values. Registration-provider intervals remain their current fixed 10-second and 30-second values; no speculative settings are added.

PDS is the only settings move: rename/move `PdsSyncWorkerOptions` to an Infrastructure-owned drain setting while preserving the `Atproto:PdsSync` configuration section.

### Decision C — Scheduler authority is global

`Scheduler:Quartz:Enabled` controls Quartz. EmailDispatch mode controls only whether the two EmailDispatch recurring jobs are registered. OpenAPI generation and the `Testing` environment continue to suppress runtime scheduling.

### Decision D — Durable state remains the correctness authority

Quartz clustering and `[DisallowConcurrentExecution]` prevent overlapping trigger execution for a job key, but they do not replace database claims or idempotency. The existing claim token/fence and tenant-context checks remain mandatory.

### Decision E — Stable, bounded operator contracts

- Add `ScheduledJobScheduleKind.Interval` for interval-triggered catalog entries.
- Add stable kebab-case names for each migrated lane.
- Use empty `JobDataMap` values for recurring drains.
- Emit bounded completion counts with the template `Scheduled job {JobName} completed.`
- Never log payloads, destination URLs, tenant IDs, secrets, or PII from the job wrapper.

### Decision F — No mixed-version cutover

The supported upgrade is coordinated: stop old replicas, wait at least the longest active lease, deploy the new version, then verify scheduler and queue health. Rollback follows the same sequence in reverse.

## 6. Implementation Phases

### Phase 1 — Correct the Contract and Prove the Safety Gate

**Goal:** establish the evidence required by `docs/OPERATIONS.md` before changing worker ownership.

**Primary files:**

- `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs` [MODIFY only if shared fixture support is needed]
- `tests/Event.API.IntegrationTests/Features/EmailDispatchQuartzClusterRecoveryTests.cs` [NEW]
- `tests/Event.API.IntegrationTests/Fixtures/QuartzPostgreSqlSchedulerFixture.cs` [MODIFY]
- `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs` [MODIFY]
- `src/Explore.Application/Services/ScheduledJobRegistry.cs` [MODIFY]
- `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY]
- `docs/OPERATIONS.md`, `docs/OUTBOX_PATTERN.md` [MODIFY]

**Acceptance:**

- Two Quartz nodes share PostgreSQL and execute one EmailDispatch drain trigger once.
- A simulated crash after transport acceptance but before settlement produces `Unknown`, not an automatic second transport call.
- Recovery remains operator-controlled and contains no recipient/payload data in Quartz state.
- `general-outbox-drain` is absent from names, catalog, planned-job output, tests, and docs.
- `OutboxProcessor` remains registered and unchanged.

**Gate:** Phases 2–5 cannot start until this phase is green.

### Phase 2 — Make Quartz a Platform Scheduler

**Goal:** remove the accidental EmailDispatch ownership of scheduler lifecycle without enabling dual email dispatch.

**Primary files:**

- `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY]
- `src/Explore.API/Hosting/ApiHostApplicationExtensions.cs` [MODIFY]
- `src/Explore.API/Hosting/ApiHostStartupExtensions.cs` [MODIFY]
- `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` [MODIFY]
- `tests/Event.API.IntegrationTests/Features/QuartzSchedulerCompositionTests.cs` [NEW]
- `docs/OPERATIONS.md`, `docs/CONFIGURATION.md` [MODIFY]

**Acceptance:**

- Scheduler composition, schema application, middleware, status endpoint, admin endpoint, and health are controlled by `Scheduler:Quartz:Enabled`, not EmailDispatch mode.
- EmailDispatch Quartz jobs are present only when EmailDispatch is enabled in Quartz mode.
- Hosted-service EmailDispatch mode never registers EmailDispatch Quartz jobs.
- Disabled scheduler behavior stays explicit: jobs do not run and scheduler readiness is degraded.
- Existing maintenance jobs continue to register under an enabled scheduler even when EmailDispatch uses hosted-service mode.

### Phase 3 — Registration and Integration Drain Cutover

**Goal:** migrate the simplest existing one-pass boundaries first.

**Jobs:**

- `registration-provider-submission-write-drain`
- `registration-provider-subscription-lifecycle-drain`
- `integration-sync-drain`

**Primary files:**

- `src/Explore.API/Scheduling/RegistrationProviderDrainJobs.cs` [NEW]
- `src/Explore.API/Scheduling/IntegrationSyncDrainJob.cs` [NEW]
- `src/Explore.Application/Contracts/Scheduling/ScheduledJobDescriptor.cs` [MODIFY]
- `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs` [MODIFY]
- `src/Explore.Application/Services/ScheduledJobRegistry.cs` [MODIFY]
- `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs` [MODIFY]
- `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` [MODIFY]
- `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY]
- three replaced worker files and `IntegrationSyncHostedDrainRunner.cs` [DELETE]
- `tests/Event.API.IntegrationTests/Features/RegistrationAndIntegrationDrainQuartzJobsTests.cs` [NEW]
- `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs` [MODIFY]
- operator/config docs [MODIFY]

**Acceptance:**

- Each job invokes exactly one existing command/service pass and uses `[DisallowConcurrentExecution]`.
- Existing claim owner, tenant bypass justification, fencing, retry, and settlement behavior remains below the API job.
- Job failures reach Quartz; individual item failures remain bounded by the existing service.
- Old hosted registrations and wrappers are absent in the same change.
- Existing intervals and feature enablement semantics are preserved.

### Phase 4 — Webhook Drain Cutover

**Goal:** move all API-hosted webhook interval loops to Quartz while preserving existing delivery and reconciliation services.

**Jobs:**

- `webhook-delivery-drain`
- `incoming-webhook-drain`
- `incoming-webhook-effect-drain`
- `webhook-bulk-replay-drain`
- `webhook-provider-publication-drain`

**Primary files:**

- `src/Explore.API/Scheduling/WebhookDrainJobs.cs` [NEW]
- `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs` [MODIFY]
- `src/Explore.Application/Services/ScheduledJobRegistry.cs` [MODIFY]
- `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs` [MODIFY]
- `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` [MODIFY]
- `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` [MODIFY]
- five replaced processor files [DELETE]
- `tests/Event.API.IntegrationTests/Features/WebhookDrainQuartzJobsTests.cs` [NEW]
- existing Infrastructure/Persistence webhook tests [MODIFY only if scheduler-boundary coverage exposes a gap]
- `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs` [MODIFY]
- `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/WEBHOOK_OPERATIONS_RUNBOOK.md` [MODIFY]

**Acceptance:**

- Delivery still runs stale-claim recovery before its normal batch.
- Provider publication runs both publication and reconciliation batches in the existing order.
- Provider publication is actually scheduled when enabled; no temporary hosted registration is introduced.
- Incoming webhook claims still execute with fresh tenant and machine-principal scopes.
- Bulk replay remains bounded and audited.
- Existing webhook health checks retain backlog/stale-lease meaning and use scheduler terminology where operator-facing.

### Phase 5 — PDS Drain Extraction and Cutover

**Goal:** move only the timer from `PdsSyncWorker`; keep all PDS claims, fences, parallelism, and external I/O below the Quartz wrapper.

**Primary files:**

- `src/Explore.Application/Contracts/Services/IPdsSyncDrainService.cs` [NEW]
- `src/Explore.Infrastructure/Services/Federation/PdsSyncDrainService.cs` [NEW]
- `src/Explore.Infrastructure/Services/Federation/PdsSyncDrainSettings.cs` [NEW, replaces API-owned worker options while retaining `Atproto:PdsSync`]
- `src/Explore.API/Scheduling/PdsSyncDrainJob.cs` [NEW]
- `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs` [MODIFY]
- `src/Explore.Application/Services/ScheduledJobRegistry.cs` [MODIFY]
- `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs` [MODIFY]
- `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` [MODIFY]
- `src/Explore.API/BackgroundServices/PdsSyncWorker.cs` and `PdsSyncWorkerOptions.cs` [DELETE]
- `tests/Explore.Infrastructure.Tests/Infrastructure/Federation/PdsSyncDrainServiceTests.cs` [NEW]
- `tests/Event.API.IntegrationTests/Features/PdsSyncDrainQuartzJobTests.cs` [NEW]
- existing PDS persistence tests [MODIFY only if a failing proof exposes a gap]
- architecture and operator docs [MODIFY]

**Acceptance:**

- The drain service owns a stable process-level lease owner, batch size, and bounded parallelism.
- `AtprotoPdsDeliveryProcessor` and `IPdsSyncOutboxRepository` semantics are unchanged unless a failing proof requires a targeted fix.
- Two concurrent drain attempts cannot claim the same row.
- Expired claims are reclaimed with a new token/fence; stale completion cannot settle the row.
- The API job is one pass, payload-free, and non-overlapping.

### Phase 6 — Architecture Ratchet and Operator Release Gate

**Goal:** prove the final system is supportable by self-hosters and cannot regress to timer loops.

**Primary files:**

- `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs` [MODIFY]
- `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/ARCHITECTURE.md`, `docs/OUTBOX_PATTERN.md` [MODIFY]
- deployment examples that expose scheduler settings [MODIFY only if currently present]

**Acceptance:**

- Timer-loop detection covers both `Task.Delay` and `PeriodicTimer`.
- Only documented exceptions remain in the timer-loop baseline.
- Job catalog/status/admin output includes every enabled migrated job and no removed general-outbox promise.
- Single-node SQLite and two-node PostgreSQL rehearsals pass.
- Pause/resume, scheduler-disabled, node-crash, backlog recovery, and coordinated rollback procedures are documented and observed.

## 7. Phase Dependencies

| Phase | Depends on | May ship independently? |
|---|---|---|
| 1 | Baseline build | Yes; evidence/docs only |
| 2 | Phase 1 green | Yes; scheduler ownership correction |
| 3 | Phase 2 green | Yes; registration/integration wave |
| 4 | Phase 2 green | Yes; after Phase 3 is preferred for smaller operational change |
| 5 | Phases 1–4 green | Yes; highest-risk final worker wave |
| 6 | All cutover phases | Final release gate |

## 8. Verification Strategy

### Build and static architecture

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

### Scheduler/API behavior

```bash
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

### Durable queue semantics

```bash
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

Minimum behavioral matrix:

| Scenario | Required evidence |
|---|---|
| Scheduler disabled | No Quartz drain trigger; scheduler readiness degraded; docs explain backlog consequence |
| Feature disabled | Its job/trigger is absent, not dormant |
| Email hosted mode | No EmailDispatch Quartz job; no dual email authority |
| Single-node SQLite | Durable scheduler starts and each enabled job performs one bounded pass |
| Two-node PostgreSQL | One trigger acquisition per job key; database claims remain exclusive |
| Long-running pass | Next pass does not overlap due to `[DisallowConcurrentExecution]` |
| Node crash after claim | Lease/fence recovery follows existing lane policy |
| Ambiguous external result | No automatic duplicate where lane policy requires `Unknown`/reconciliation |
| Tenant concurrency | Every claim executes under its persisted tenant context; no tenant data enters Quartz |

## 9. Enterprise Self-Hosting Operations Contract

- **Topology:** multi-replica deployments require persistent Quartz storage and clustering with unique `AUTO` instance IDs.
- **Readiness:** scheduler disabled/standby/shutdown/error state must remain visible through existing health checks.
- **Observability:** stable job names, bounded outcome labels, previous/next fire times, execution count/duration, backlog, and stale-lease signals; no payload or tenant-cardinality labels.
- **Capacity:** each trigger processes one bounded batch. Operators tune existing interval, batch, concurrency, and lease settings; no unbounded catch-up loop is added.
- **Misfires:** missed recurring passes collapse into the next normal pass because durable queues already hold backlog.
- **Pause semantics:** pausing a job stops claims but does not mutate durable queue state.
- **Upgrade:** all old replicas stop before any new replica starts. Wait at least the longest configured lease before enabling the new release.
- **Rollback:** stop all new replicas, wait for active leases, deploy the prior release, verify old hosted workers resume. No schema rollback is needed.

## 10. Risks and Mitigations

| Severity | Risk | Mitigation / gate |
|---|---|---|
| Blocker | EmailDispatch crash window can duplicate an external side effect | Phase 1 real clustered recovery proof |
| Blocker | Scheduler remains tied to Email mode | Phase 2 composition matrix |
| Critical | Mixed old/new replicas run two scheduling authorities | Coordinated stop/start only |
| Critical | PDS orchestration moves into API job | Extract Infrastructure drain service first |
| Critical | Cross-tenant drain bypass leaks tenant context | Preserve existing claim executors and tenant-bypass reasons; run tenant-isolation tests |
| High | Disabled scheduler silently accumulates backlog | Degraded scheduler health, backlog checks, and explicit runbook warning |
| High | Job catches failures and appears successful | Let unexpected drain exceptions bubble to Quartz telemetry |
| Moderate | High backlog monopolizes scheduler threads | One bounded batch per trigger; tune existing settings |
| Moderate | Architecture ratchet misses `PeriodicTimer` | Expand detection after target workers are removed |

## 11. Definition of Done

- Phase 1 safety evidence is green and retained in CI-capable tests.
- Quartz lifecycle is independent of EmailDispatch mode.
- The nine targeted interval workers are replaced by one-pass Quartz jobs.
- `OutboxProcessor` remains the explicit hosted-service exception.
- No active migration job contains retry, lease, fence, tenant, or transport logic.
- No old and new authority can be registered together in one host configuration.
- Stable names, catalog descriptors, health/status behavior, metrics, and runbooks match implementation.
- Architecture ratchet rejects new `Task.Delay` and `PeriodicTimer` scheduling loops outside documented exceptions.
- Release build and required test projects are green.
- Single-node and clustered operator rehearsals are recorded, including rollback.

## 12. Implementation Agent Contract

1. Read this plan, then the context and task ledger before each phase.
2. Do not start a cutover phase until its dependencies and gates are green.
3. Keep durable semantics below the API job; a job only triggers one pass.
4. Update operator docs in the same change as names, configuration, health, or worker ownership.
5. Update `queue-driven-worker-migration-tasks.md` and `queue-driven-worker-migration-context.md` after each completed task or changed gate.
6. Stop and re-baseline if a proof requires repository/schema changes not listed here.
