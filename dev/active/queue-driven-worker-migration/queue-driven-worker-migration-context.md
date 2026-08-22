<!-- ABOUTME: Resume context for the phased migration of API-hosted interval drains to Quartz.NET. -->
<!-- ABOUTME: Records verified repository state, CTO decisions, execution gates, and current handoff status. -->

# Periodic Queue-Drain Migration to Quartz.NET — Context

Last Updated: 2026-08-20 Europe/Brussels

## Session Progress

### Completed

- Applied the `senior-cto-feedback` and `implementation-plan` workflows to the current repository, not the prior plan's assumptions.
- Re-baselined all three workstream artifacts.
- Verified a green Release build before planning edits: 39 projects, 0 errors. Existing warnings remain outside this planning-only change.
- Confirmed that `OutboxProcessor` is an explicit hosted-service exception and removed its proposed migration from the plan.
- Confirmed that the Webhook and Integration drain contracts/services already exist; the plan now reuses them.
- Found the scheduler lifecycle is incorrectly gated by EmailDispatch Quartz mode across service composition, schema startup, middleware, and endpoint mapping.
- Expanded the worker inventory to include omitted interval drains and the configured-but-unregistered provider-publication processor.
- Removed unrelated lifecycle/maintenance feature implementation from this workstream.

### Current State

- **Implementation has not started.**
- **Current task:** Phase 1.1, EmailDispatch clustered crash-window proof.
- **Current gate:** no worker ownership change until Phase 1 is green.
- **Plan verdict:** Approve with required changes; all required changes are now explicit tasks.

## Binding Architecture Decisions

1. `OutboxProcessor` stays a `BackgroundService`; no `GeneralOutboxDrainJob` or general-outbox drain service will be created.
2. Quartz owns cadence only. Jobs execute one bounded pass and contain no claim, retry, lease, fence, tenant, or transport logic.
3. `Scheduler:Quartz:Enabled` becomes the global scheduler authority. EmailDispatch mode controls only EmailDispatch job registration.
4. Existing feature settings and cadence keys remain in use. PDS settings move layers but retain the `Atproto:PdsSync` section.
5. Existing drain services/commands are reused. Only PDS needs a new scheduler-neutral drain boundary.
6. Every migrated job uses `[DisallowConcurrentExecution]`, an empty `JobDataMap`, and a stable operator-visible name.
7. Unexpected drain failures bubble to Quartz; claim-level failures remain handled by existing drain services.
8. Cutover and rollback are coordinated stop/start operations. Mixed old/new replicas are unsupported.

## Verified Worker Roster

### Keep as Hosted Services

- `OutboxProcessor` — durable side-effect authority and explicit scheduling exception.
- `ManagedControlPlaneRegistrationWorker` — retry-until-success bootstrap.
- Startup and event/stream workers that do not run an interval loop.

### Phase 3: Registration and Integration

- `RegistrationProviderSubmissionWriteWorker`
- `RegistrationProviderSubscriptionLifecycleWorker`
- `IntegrationSyncProcessor`

### Phase 4: Webhooks

- `WebhookDeliveryProcessor`
- `IncomingWebhookProcessor`
- `IncomingWebhookEffectProcessor`
- `WebhookBulkReplayProcessor`
- `WebhookProviderPublicationProcessor` — settings/runbook exist, but the worker is not currently registered; migrate directly to Quartz.

### Phase 5: PDS

- `PdsSyncWorker` — extract `RunOnceAsync` orchestration to an Infrastructure drain service before deleting the worker.

## Gate Evidence

### Already Present

- Generic two-node PostgreSQL Quartz trigger acquisition proof in `QuartzClusteringTests`.
- Existing per-lane claim, fence, stale-lease, retry, and tenant-isolation tests for Webhook, Integration, Registration Provider, and PDS persistence/services.
- Scheduler status, admin, telemetry listener, and readiness surfaces.

### Still Required

- Real EmailDispatch clustered drain proof, including the transport-accepted/local-settlement crash window.
- Composition matrix proving global Quartz lifecycle is independent of EmailDispatch mode and cannot create dual email dispatch.
- Job delegation and enabled/disabled registration tests for each migration wave.
- Final single-node SQLite and two-node PostgreSQL operator rehearsals.

## Resume-Critical Files

### Scheduler Lifecycle

- `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`
- `src/Explore.API/Hosting/ApiHostApplicationExtensions.cs`
- `src/Explore.API/Hosting/ApiHostStartupExtensions.cs`
- `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`
- `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`

### Catalog and Guardrails

- `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`
- `src/Explore.Application/Contracts/Scheduling/ScheduledJobDescriptor.cs`
- `src/Explore.Application/Services/ScheduledJobRegistry.cs`
- `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs`
- `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`

### Safety Proof

- `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs`
- `tests/Event.API.IntegrationTests/Fixtures/QuartzPostgreSqlSchedulerFixture.cs`
- `tests/Event.API.IntegrationTests/Features/EmailDispatchQuartzJobsTests.cs`
- `tests/Explore.Infrastructure.Tests/Infrastructure/EmailDispatchDrainServiceTests.cs`

### Operator Contract

- `docs/OPERATIONS.md`
- `docs/CONFIGURATION.md`
- `docs/ARCHITECTURE.md`
- `docs/OUTBOX_PATTERN.md`
- `docs/WEBHOOK_OPERATIONS_RUNBOOK.md`

## Current Blockers and Risks

- Phase 1 crash-window proof is absent; this is the only blocker to beginning cutover phases.
- Quartz currently does not start unless EmailDispatch selects Quartz mode; Phase 2 must fix this before any new job registration.
- A rolling mixed-version deployment can create dual scheduling authorities even with database claims.
- `ApiLiabilityRatchetTests` detects `Task.Delay` but not `PeriodicTimer`; broaden it only as migrated wrappers are removed so the baseline stays honest.
- PDS orchestration currently lives inside the API worker and must move below the Quartz wrapper without changing delivery semantics.

## Verification Baseline

| Check | Status |
|---|---|
| Release build before planning edits | Green: 39 projects, 0 errors |
| Runtime implementation | Not started |
| Phase 1 gate | Not started |
| Planning artifact consistency | Pending final diff verification in this session |

## Next Action

Implement task **1.1** from `queue-driven-worker-migration-tasks.md`. Do not edit hosted-service registrations or delete workers until Phase 1 verification passes.
