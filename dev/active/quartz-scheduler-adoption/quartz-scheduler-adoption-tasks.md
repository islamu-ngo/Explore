# Quartz Scheduler Adoption — Task Checklist

Last Updated: 2026-08-16 Europe/Brussels

## Status Summary
- **Overall status:** Draft — awaiting user review
- **Completed:** 0/12 implementation tasks (phase verification tracked separately)
- **Current priority:** Task 1.1 — Enable and expose Quartz schema validation
- **Next recommended slice:** Phase 1 (scheduler correctness gates)
- **Start gate:** prefer starting after the concurrent maintenance-sweep work is committed — it edits two files this workstream appends to

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
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools MCP, or live services for verification. (Testcontainers in Tasks 1.2/1.3 is a database fixture, not application startup.)
- 🚫 Never modify `src/Explore.API/Scheduling/MaintenanceSweepJobs.cs` or `dev/active/quartz-dashboard-integration/` — other agents own them.
- ⚠️ `ScheduledJobNames.cs` and `QuartzSchedulerKeys.cs` are concurrently edited — **append only**.

## Phase 1: Scheduler Correctness Gates ⏳ NOT STARTED

- [ ] **1.1 Enable and expose Quartz schema validation**
  - **Files:** `Configuration/QuartzSchedulerSettings.cs`, `Configuration/QuartzSchedulerSettingsValidator.cs`, `Extensions/QuartzSchedulerExtensions.cs`, `appsettings.json`, `docs/CONFIGURATION.md` (all existing)
  - **Acceptance:** `PerformSchemaValidation` follows a new `ValidateSchemaOnStartup` setting (default `true`); validator rejects validation without a persistent store; validator tests cover the rule; key documented
  - **Effort:** S
  - **Dependencies:** none

- [ ] **1.2 Execute the PostgreSQL scheduler DDL against a real engine**
  - **Files:** `tests/Event.API.IntegrationTests/Features/QuartzPostgreSqlSchemaTests.cs` (new)
  - **Acceptance:** all 11 `QRTZ_` tables verified in `information_schema.tables`; re-application idempotent; a trigger fires under the PostgreSQL delegate; skips visibly when Docker is unavailable
  - **Effort:** M
  - **Dependencies:** none

- [ ] **1.3 Prove clustering does not double-fire**
  - **Files:** `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs` (new)
  - **Acceptance:** two clustered schedulers over one store produce exactly one execution for one trigger; both appear in `QRTZ_SCHEDULER_STATE`; chosen store named in the test; skips visibly if Docker-dependent and unavailable
  - **Effort:** M
  - **Dependencies:** 1.2

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

---

## Phase 2: Generalized Deadline Port ⏳ NOT STARTED

- [ ] **2.1 Introduce the deadline contract and delete the single-purpose port**
  - **Files:** `Contracts/Scheduling/IScheduledDeadlineDispatcher.cs` (new), `Contracts/Scheduling/ScheduledDeadline.cs` (new), `Services/NoOpScheduledDeadlineDispatcher.cs` (new), `ApplicationServicesRegistration.cs` (existing); delete `Contracts/Infrastructure/IScheduledEmailDispatchTrigger.cs`, `Contracts/Infrastructure/ScheduledEmailDispatchPointer.cs`, `Services/NoOpScheduledEmailDispatchTrigger.cs`
  - **Acceptance:** pointer is `IReadOnlyDictionary<string,string>`; zero Quartz references; three old files deleted; default DI points at the new no-op
  - **Effort:** M
  - **Dependencies:** none

- [ ] **2.2 Implement the Quartz-backed dispatcher**
  - **Files:** `Scheduling/QuartzScheduledDeadlineDispatcher.cs` (new), `Scheduling/QuartzSchedulerKeys.cs` (existing, append), `Extensions/QuartzSchedulerExtensions.cs` (existing); delete `Scheduling/QuartzScheduledEmailDispatchTrigger.cs`
  - **Acceptance:** deterministic trigger key from job name + deadline key; re-schedule does not duplicate; `CancelAsync` removes and reports `false` when absent; only string values in `JobDataMap`; old implementation deleted
  - **Effort:** M
  - **Dependencies:** 2.1

- [ ] **2.3 Re-point the event reminder job at the deadline envelope**
  - **Files:** `Scheduling/EventReminderDispatchJob.cs` (existing), `tests/Event.API.IntegrationTests/Features/EmailDispatchQuartzJobsTests.cs` (existing)
  - **Acceptance:** job reads discrete string keys; missing/invalid values remain a logged no-op; use-case validation unchanged; existing tests pass against the new shape
  - **Effort:** S
  - **Dependencies:** 2.1, 2.2

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 3: Precise Inventory-Hold Expiry ⏳ NOT STARTED

- [ ] **3.1 Add the expiry job and its reconciliation sweep**
  - **Files:** `Scheduling/InventoryHoldExpiryJob.cs` (new), `Scheduling/InventoryHoldExpiryReconciliationJob.cs` (new), `Contracts/Scheduling/ScheduledJobNames.cs` (existing, **append only**), `Services/ScheduledJobRegistry.cs` (existing), `Scheduling/QuartzSchedulerKeys.cs` (existing, **append only**), `Extensions/QuartzSchedulerExtensions.cs` (existing)
  - **Acceptance:** expiry job handles one order from pointer data; tenant context set and cleared in `finally`; reconciliation covers expired holds **and** recovery targets; both registered as `Implemented`; cron is Quartz-valid (`?` rule)
  - **Effort:** L
  - **Dependencies:** 2.2

- [ ] **3.2 Register and cancel hold deadlines from the order lifecycle**
  - **Files:** `Features/RegistrationOrders/Handlers/Commands/CreateOrderWithHoldCommandHandler.cs` (existing), `Services/Registration/RegistrationOrderLifecycleService.cs` (existing)
  - **Acceptance:** deadline registered after persistence at the earliest hold expiry; dispatcher failure never fails order creation; terminal transitions cancel the deadline; pointer holds only string identifiers; no Quartz reference in Application
  - **Effort:** M
  - **Dependencies:** 3.1

- [ ] **3.3 Delete the polling worker and cover the new behavior**
  - **Files:** delete `BackgroundServices/InventoryHoldExpiryWorker.cs`; `Hosting/ApiHostServiceCollectionExtensions.cs` (existing), `tests/Event.API.IntegrationTests/Features/InventoryHoldExpiryJobTests.cs` (new), `docs/OPERATIONS.md`, `docs/CONFIGURATION.md` (existing)
  - **Acceptance:** worker deleted and unregistered; expiry job tested for happy path and already-finalized no-op; reconciliation tested as safety net for a missed deadline; job catalog and config docs updated
  - **Effort:** M
  - **Dependencies:** 3.1, 3.2

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

---

## Phase 4: Uniform Job Observability ⏳ NOT STARTED

- [ ] **4.1 Add an exception-contained telemetry job listener**
  - **Files:** `Scheduling/SchedulerTelemetryJobListener.cs` (new), `Extensions/QuartzSchedulerExtensions.cs` (existing), `Telemetry/BusinessMetrics.cs` (existing), `tests/Event.API.IntegrationTests/Features/SchedulerTelemetryJobListenerTests.cs` (new)
  - **Acceptance:** duration and outcome recorded for every job; labels carry no tenant identity or payload values; every listener method exception-contained, proven by a throwing-sink test; `MaintenanceSweepJobs.cs` untouched
  - **Effort:** M
  - **Dependencies:** 3.3

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

---

## Phase 5: Bounded Drain Migration And Contract Governance ⏳ NOT STARTED

- [ ] **5.1 Move registration finalization onto a cron job**
  - **Files:** `Scheduling/RegistrationFinalizationDrainJob.cs` (new); delete `BackgroundServices/RegistrationFinalizationWorker.cs`; `Hosting/ApiHostServiceCollectionExtensions.cs`, `Contracts/Scheduling/ScheduledJobNames.cs` (**append only**), `Services/ScheduledJobRegistry.cs`, `Extensions/QuartzSchedulerExtensions.cs`, `Scheduling/QuartzSchedulerKeys.cs` (**append only**), `docs/OPERATIONS.md` (all existing)
  - **Acceptance:** identical `DrainRegistrationFinalizationEffectsCommand` sent from a scoped `ISender`; `[DisallowConcurrentExecution]` preserves sequential behavior; worker deleted and unregistered; job in registry and operations catalog
  - **Effort:** M
  - **Dependencies:** 4.1

- [ ] **5.2 Add the `schedule-background-job` intent**
  - **Files:** `.agents/contract/intents.yaml` (existing), `docs/OPERATIONS.md` (existing)
  - **Acceptance:** intent parses and matches the existing schema; triggers include "add scheduled job", "background job", "cron job", "scheduler change"; forbidden list encodes the four scheduler invariants; operations doc references it
  - **Effort:** S
  - **Dependencies:** 5.1

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Remaining / Deferred Work

- **Quartz dashboard** — owned by `dev/active/quartz-dashboard-integration/` (another agent). Explicitly out of scope; do not add dashboard packages here.
- **Maintenance sweeps** — 8 processors already migrated into `Scheduling/MaintenanceSweepJobs.cs` by a concurrent agent. Out of scope. Its per-job "completed" log lines become redundant once Task 4.1 lands; leave that cleanup to its owner.
- **Remaining queue drains** — `OutboxProcessor`, `IntegrationSyncProcessor`, the five webhook processors, `NotificationFanoutProcessor`, `WebPushDispatchProcessor`, `PdsSyncWorker`, and the two registration-provider workers. Deferred until Task 5.1 proves the timer-only migration pattern. Trigger: Phase 5 green. These implement their own claim/lease/fairness logic — migrate the timer only, never the claim logic.
- **Startup gates and stream consumers** — `AiProviderSettingsBootstrapWorker`, `CerbosPolicyBootSyncWorker`, `PrivacyErasureStartupGate`, `JwtAuthorityWarmupHostedService`, `LookupDataCacheInitializer`, `AtprotoJetstreamSubscriber`, the three `EmailDispatchRabbitMq*` services, `AiAssistantRunWorker`. **Permanently out of scope** — a scheduler is the wrong tool for once-before-traffic work, persistent connections, and in-memory latency queues.
- **`RecurrenceTrigger` (RFC 5545 RRULE) for recurring events** — the product domain already speaks RRULE. Worth a spike before hand-rolling recurrence expansion. Trigger: recurring-events feature work.
- **Calendars for quiet hours / blackout windows** — natural fit for notification policy. Trigger: notification-policy work.
- **`TimeProvider` injection for deterministic schedule tests** — removes sleep-based timing. Trigger: flaky scheduler timing tests.
- **Execution limits and node affinity** — require `EXECUTION_GROUP`, `PREFERRED_NODE`, and `PREFERRED_NODE_AUTO` columns. Add the columns only together with the feature, never speculatively.
- **5 unimplemented catalog jobs** — `general-outbox-drain`, `pds-sync-drain`, `dead-letter-summary`, `waitlist-promotion-scan`, `tenant-maintenance-scan` remain `Planned` in `ScheduledJobNames`. `waitlist-promotion-scan` is a natural second consumer of the deadline port from Phase 2.
