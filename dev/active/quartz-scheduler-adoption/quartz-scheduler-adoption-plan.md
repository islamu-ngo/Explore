# Quartz Scheduler Adoption — Implementation Plan

Last Updated: 2026-08-16 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Write an implementation plan for the remaining Quartz.NET background-job priorities identified in `dev/report/quartznet-background-jobs-implementation-report.md`, explicitly excluding the Quartz dashboard (owned by another agent).
- **Task directory:** `dev/active/quartz-scheduler-adoption/`
- **Planning status:** Draft
- **Matched intents:** **Fallback contract — no intent matches.** `.agents/contract/intents.yaml` holds 18 intents; none covers background-job scheduling. The predecessor workstream noted "consider adding an intent if this pattern recurs"; it has now recurred three times (migration, dashboard, this adoption), so **Task 5.2 adds a `schedule-background-job` intent**.
- **Relevant skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `ip-clean-room`
- **Relevant rules:** `.agents/rules/application-layer.md`, `.agents/rules/domain.md`, `.agents/rules/tests.md`
- **Primary layers touched:** API (primary), Application (new scheduler-neutral port), Tests, Docs, Agent contract
- **Complexity:** **L** — five reviewable slices across ~18 source files and ~6 test files. No database migration, no public API contract change.

## 1. Executive Summary

The TickerQ → Quartz.NET migration delivered durable scheduling on every database provider. This workstream spends that capability on the work that most needs it, and closes the correctness gaps the migration knowingly left open.

**What will change:**

1. **Correctness first.** Enable Quartz's own schema validation, execute the PostgreSQL DDL against a real engine, and prove clustering does not double-fire before anyone scales out.
2. **One deadline abstraction.** Replace the single-purpose `IScheduledEmailDispatchTrigger` with a general, pointer-only `IScheduledDeadlineDispatcher`. Two use cases already exist and more are queued; one port beats a port per feature.
3. **Precise inventory-hold expiry.** Replace a 60-second poll with a trigger that fires *at* the deadline, backed by a reconciliation sweep. This is the highest-value adoption target and sits on the ticketing revenue path.
4. **Uniform job telemetry.** One `IJobListener` replaces per-job hand-written logging with consistent metrics and traces.
5. **Close the governance loop.** Add the missing intent so the next scheduler change has a contract to follow.

**Why it matters:** inventory holds are released up to a minute late today, which directly withholds sellable capacity. Scheduler state is unvalidated against three of five providers. And a second replica would today silently double-run every cron job.

**Non-goals:**

- **The Quartz dashboard.** Owned by `dev/active/quartz-dashboard-integration/` (another agent). Out of scope entirely.
- **The maintenance sweeps.** `MaintenanceSweepJobs.cs` migrated 8 processors and is **in flight and uncommitted** by another agent. This plan must not touch that file.
- **Queue-drain migration at large.** `OutboxProcessor`, webhook processors, `NotificationFanoutProcessor`, and `PdsSyncWorker` keep their own claim/lease logic; only `RegistrationFinalizationWorker` moves, as a bounded proof.
- **Backward compatibility.** Development mode; ports are replaced, not shimmed.
- **Execution limits / node affinity.** Require extra schema columns for features we do not use.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| Quartz 3.19.1 drives exactly 3 jobs plus 8 in-flight sweeps | `ls src/Explore.API/Scheduling/` → `EmailDispatchDrainJob`, `EmailDispatchRecoveryScanJob`, `EventReminderDispatchJob`, `MaintenanceSweepJobs` | High | Sweeps are uncommitted work by a concurrent agent |
| **`IScheduledEmailDispatchTrigger` has no active caller** | `grep -rn "ScheduleAsync(" src` returns only unrelated `ApplyScheduleAsync` methods; the only references are the DI registrations and the no-op | **High** | The one-off deadline path is wired but **dormant**. Materially changes the design in §5 Decision 2 |
| `InventoryHoldExpiryWorker` polls every 60s | `BackgroundServices/InventoryHoldExpiryWorker.cs:11` `PollingInterval = TimeSpan.FromMinutes(1)`, `BatchSize = 100` | High | Also handles `GetHoldExpiryRecoveryTargetsAsync`, so a pure deadline trigger is insufficient — see Decision 3 |
| Hold expiry instant is known at creation | `Explore.Domain/RegistrationInventoryHold.cs:52` `ExpiresAt`, set in `Create` at line 111-125 | High | Enables precise scheduling |
| Holds are created in one handler | `Explore.Application/Features/RegistrationOrders/Handlers/Commands/CreateOrderWithHoldCommandHandler.cs:271` `CreateHolds` | High | Single call site for deadline registration |
| `RegistrationFinalizationWorker` polls every 10s and delegates to one MediatR command | `BackgroundServices/RegistrationFinalizationWorker.cs` → `DrainRegistrationFinalizationEffectsCommand` | High | Thin; safe first drain migration |
| 26 `BackgroundService` classes remain | `ls src/Explore.API/BackgroundServices/*.cs \| wc -l` | High | Down from 35 after the concurrent sweep migration |
| Only SQLite DDL is executed against a real engine | `QuartzSqliteDurableSchedulingTests.cs` exists; no PostgreSQL equivalent | High | PostgreSQL is the Tier 2/3 default |
| `Testcontainers.PostgreSql` is already available | `tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj:17` | High | No new dependency needed for Task 1.2 |
| Clustering is implemented but never exercised | `QuartzSchedulerExtensions.ConfigurePersistentStore` calls `UseClustering`; `QuartzSchedulerSettingsValidator` enforces `InstanceId=AUTO`; no multi-node test exists | High | |
| `PerformSchemaValidation` is available and unused | `SchedulerBuilder.PersistentStoreOptions.PerformSchemaValidation` in the shipped `Quartz.xml`; absent from `QuartzSchedulerExtensions` | High | |
| Quartz silently degrades on missing optional columns | Assembly string: *"Column MISFIRE_ORIG_FIRE_TIME not found in triggers table. ScheduledFireTimeUtc will not be corrected…"* | High | One such defect already shipped and was fixed on 2026-08-16 |
| Listener exceptions can halt the scheduler | Quartz troubleshooting docs via Context7: unhandled listener exceptions "can disrupt the scheduling cycle" | High | Hard constraint on Task 4.1 |
| No intent covers background jobs | `grep -n "^  - id:" .agents/contract/intents.yaml` → 18 ids, none scheduler-related | High | Drives Task 5.2 |
| Architecture tests already fence Quartz out of Application/Domain | `tests/Event.Architecture.Tests/DurableSideEffectBoundaryTests.cs` `SchedulerOperationPattern` | High | Protects Decision 1 |

**Research note:** Tavily MCP was attempted for external best-practice research and returned **HTTP 432 (plan usage limit exceeded)** on every call. All external grounding therefore comes from Context7 against `/quartznet/quartznet` and from direct inspection of the shipped `Quartz 3.19.1` assembly. No claim in this plan rests on unverified recall.

### 2.2 Existing Implementation

**API layer (`src/Explore.API/`)**
- `Extensions/QuartzSchedulerExtensions.cs` — `AddApiQuartzScheduler` (provider switch over all five `PrimaryDatabaseProvider` values, `UseSystemTextJsonSerializer`, `UseProperties = true`, table prefix, optional clustering), plus status-endpoint mapping and schema application.
- `Scheduling/QuartzSchedulerKeys.cs` — stable `JobKey`s and cron constants derived from `ScheduledJobNames`.
- `Scheduling/QuartzSchemaInitializer.cs` — applies embedded provider DDL, `{prefix}` substitution, `GO` batch split, non-destructive.
- `Scheduling/QuartzScheduledEmailDispatchTrigger.cs` — implements the dormant port via a one-off `SimpleTrigger`.
- `BackgroundServices/` — 26 remaining hosted services.

**Application layer (`src/Explore.Application/`)**
- `Contracts/Scheduling/{IScheduledJobRegistry, ScheduledJobDescriptor, ScheduledJobNames}` — scheduler-neutral catalog.
- `Contracts/Infrastructure/IScheduledEmailDispatchTrigger.cs` + `ScheduledEmailDispatchPointer.cs` — single-purpose port, **no caller**.
- `Services/NoOpScheduledEmailDispatchTrigger.cs` — default registration.

### 2.3 Existing Tests And Verification Coverage

| Test file | Protects | Gap |
|---|---|---|
| `QuartzSqliteDurableSchedulingTests.cs` | SQLite schema creation, idempotency, restart durability, real firing | PostgreSQL/SQL Server/MySQL never executed |
| `QuartzSchemaInitializerTests.cs` | Per-provider table coverage, prefix substitution, non-destructiveness, `MISFIRE_ORIG_FIRE_TIME` | Structural only |
| `EmailDispatchQuartzJobsTests.cs` | Jobs delegate to Application contracts; poison payloads dropped | — |
| `QuartzSchedulerSettingsValidatorTests.cs` | Options validation incl. clustering rules | Validation only; clustering never run |
| `DurableSideEffectBoundaryTests.cs` | Quartz confined to API layer | — |
| — | **Clustering / double-fire** | **No coverage** |
| — | **Inventory-hold expiry timing** | **No coverage** |

### 2.4 Existing Documentation And Contracts

`docs/CONFIGURATION.md` (`Scheduler:Quartz` table + schema-ownership section), `docs/OPERATIONS.md` (scheduler runbook, job catalog), `docs/SELF_HOSTING.md`, `docs/DEPLOYMENT_TIERS.md`, `docs/TESTING.md`, `docs/TROUBLESHOOTING.md`, `.agents/contract/intents.yaml`. No OpenAPI or generated-client impact: the scheduler is internal and the status endpoint uses `ExcludeFromDescription()`.

### 2.5 Current Pain Points / Improvement Areas

1. **Inventory capacity is withheld for up to 60 seconds** past expiry — a revenue-path defect, not a cosmetic one (`InventoryHoldExpiryWorker.cs:11`).
2. **A dormant abstraction.** `IScheduledEmailDispatchTrigger` exists, has a Quartz implementation and a durably-registered job, and is called by nothing. It is single-purpose by name, so the next deadline feature would add a second near-identical port.
3. **Three of five providers have unexecuted DDL.** PostgreSQL is the Tier 2/3 default.
4. **Scale-out is unsafe today.** Clustering is configured but unproven; two replicas would both run `email-dispatch-drain`.
5. **Silent schema degradation is unguarded.** `PerformSchemaValidation` exists and is off.
6. **Per-job logging is copy-paste.** Each job hand-writes its own completion log; there is no uniform failure metric.

### 2.6 Unknowns After Investigation

| Unknown | Searched | Resolution task |
|---|---|---|
| Whether Testcontainers PostgreSQL is usable in this environment (Docker availability) | `Testcontainers.PostgreSql` is referenced; other workstreams record "Docker unavailable" | **Task 1.2** — gate the test on Docker availability and skip cleanly rather than fail |
| Whether hold-expiry deadlines should be per-hold or per-order | `InventoryHoldExpiryWorker` groups by `(TenantId, RegistrationOrderId)` before recovery | **Task 3.1** — per-order, matching the existing grouping |
| Whether the recovery-target path can be dropped once deadlines are precise | `GetHoldExpiryRecoveryTargetsAsync` covers orders needing lifecycle recovery independent of hold expiry | **Task 3.1** — it cannot; keep it in the reconciliation sweep |

## 3. Proposed Future State

**Scheduling ownership.** Quartz owns *when*; the Application layer owns *what*. Every deadline in the platform is registered through one scheduler-neutral port that accepts a pointer and a due time and returns a cancellable handle.

**Inventory holds.** When an order is created with holds, the handler registers a deadline at the order's earliest hold expiry. At that instant `InventoryHoldExpiryJob` runs one order's expiry and lifecycle recovery. A low-frequency `inventory-hold-expiry-reconciliation` cron sweep remains the safety net for lost triggers, pre-existing holds, and recovery targets that have no hold deadline. Precision comes from the trigger; correctness guarantees come from the sweep.

**Observability.** A single `IJobListener` records execution count, duration, and failure category for every job, with all listener code exception-contained so a telemetry fault can never stall the scheduler.

**Governance.** A `schedule-background-job` intent tells the next contributor which docs, skills, rules, paths, and tests apply.

## 4. Non-Negotiable Constraints

1. **Clean Architecture inward-only dependencies.** Quartz types appear only in `Explore.API`. Enforced by `DurableSideEffectBoundaryTests`.
2. **Pointer-only scheduler payloads.** No PII, message content, secrets, or provider identifiers in `JobDataMap`. Enforced by `DurableSideEffectBoundaryTests`.
3. **Do not touch `Scheduling/MaintenanceSweepJobs.cs`, `ScheduledJobNames` sweep constants, or `RegisterMaintenanceSweeps`** — in-flight uncommitted work owned by another agent.
4. **Do not touch `dev/active/quartz-dashboard-integration/`** or add dashboard packages.
5. **Every file starts with a two-line `ABOUTME:` comment**; file-scoped namespaces.
6. **Validators are manually instantiated** (no DI) per critical rule #2.
7. **No compatibility shims.** Delete replaced ports and workers outright.
8. **Repositories return entities, never DTOs**; `Guid` (UUIDv7) for aggregate identity per critical rule #3.
9. **Listener code must be exception-contained** — unhandled listener exceptions can halt the scheduling cycle.
10. **No new EF Core migration.** Scheduler tables stay raw ADO.

## 5. Architecture And Design Decisions

### Decision 1: Keep Quartz strictly behind an Application-owned port
- **Why:** the Application layer must be able to express "wake me at T" without knowing a scheduler exists, so the scheduler stays swappable and unit tests need no Quartz.
- **Alternatives considered:** injecting `ISchedulerFactory` into handlers (breaks the boundary and the architecture test); MediatR notifications with a delay (no durability).
- **Consequences:** one indirection; deadline registration is testable with a substitute.
- **Files/layers:** `Explore.Application/Contracts/Scheduling/`, `Explore.API/Scheduling/`.

### Decision 2: Replace `IScheduledEmailDispatchTrigger` with a general `IScheduledDeadlineDispatcher`
- **Why:** the existing port is single-purpose, and — critically — **has no caller**, so generalizing it costs nothing in migration risk. Two concrete use cases exist now (event reminder, inventory hold) and three more are queued (`waitlist-promotion-scan`, registration finalization deadlines, `tenant-maintenance-scan`). A port per feature would multiply near-identical code.
- **Alternatives considered:** keep the email port and add a second inventory port (duplicated serialization, cancellation, and key-generation logic three times over); a generic `IScheduler<T>` (leaks scheduler semantics into Application).
- **Consequences:** `IScheduledEmailDispatchTrigger`, `ScheduledEmailDispatchPointer`, `NoOpScheduledEmailDispatchTrigger`, and `QuartzScheduledEmailDispatchTrigger` are deleted and replaced. `EventReminderDispatchJob` reads the new envelope shape.
- **Files/layers:** Application contracts, API scheduling, DI registration.

### Decision 3: Precise deadline trigger **plus** reconciliation sweep, not either alone
- **Why:** a one-off trigger gives precision but is not a correctness guarantee — triggers can be lost, holds may pre-date the feature, and `GetHoldExpiryRecoveryTargetsAsync` returns orders needing recovery that have no hold deadline at all. This mirrors the proven email-dispatch design where the recurring drain is the retry authority.
- **Alternatives considered:** deadline trigger only (silently strands recovery targets); keep polling only (the current 60-second latency defect).
- **Consequences:** two jobs instead of one; the sweep runs at a much lower frequency than today's 1-minute poll.
- **Files/layers:** `Explore.API/Scheduling/`, `Explore.Application/Features/RegistrationOrders/`.

### Decision 4: Deadlines are keyed per order, and cancellation is explicit
- **Why:** the existing worker already groups by `(TenantId, RegistrationOrderId)`; one trigger per order avoids trigger-table churn from multi-line orders. A completed or cancelled order must unschedule its deadline so the store does not accumulate dead triggers.
- **Alternatives considered:** per-hold triggers (N triggers per order, N unschedules); never cancelling (unbounded trigger growth, and a fired job that finds nothing to do).
- **Consequences:** the port needs a `CancelAsync`; the deadline key must be deterministic from the order identity.
- **Files/layers:** Application contracts, order lifecycle handlers.

### Decision 5: One `IJobListener` for telemetry, fully exception-contained
- **Why:** 11+ jobs each hand-writing completion logs is duplication that drifts. A listener is Quartz's supported cross-cutting hook and yields uniform metrics.
- **Alternatives considered:** a job base class (inheritance coupling, and `[DisallowConcurrentExecution]` is per-class anyway); a decorator over `IJob` (Quartz constructs jobs through its own factory, so decoration is awkward).
- **Consequences:** every `try`/`catch` inside the listener is mandatory — an unhandled listener exception can disrupt the scheduling cycle.
- **Files/layers:** `Explore.API/Scheduling/`, `Explore.Application/Telemetry/BusinessMetrics`.

### Decision 6: Enable `PerformSchemaValidation` and fail fast
- **Why:** the platform already shipped one silent-degradation defect (`MISFIRE_ORIG_FIRE_TIME`). Startup validation converts that class of defect from invisible to loud.
- **Alternatives considered:** rely on integration tests (only covers providers we test); leave off (status quo, already proven insufficient).
- **Consequences:** a provider whose DDL drifts fails startup instead of degrading. That is the desired trade.
- **Files/layers:** `QuartzSchedulerExtensions`, `QuartzSchedulerSettings`.

## 6. Implementation Phases

### Phase 1: Scheduler Correctness Gates

- **Goal:** make schema and clustering defects loud and proven before adding new jobs.
- **Depends on:** nothing
- **Related skills/rules:** `clean-architecture-rules`, `.agents/rules/tests.md`
- **Acceptance criteria:** schema validation is enabled and configurable; PostgreSQL DDL is executed against a real engine or cleanly skipped when Docker is absent; a two-scheduler test proves a single fire per trigger.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** if schema validation rejects a provider, fix the DDL script rather than disabling validation; the setting exists as an operator escape hatch, not a developer one.

#### Task 1.1: Enable and expose Quartz schema validation
- **Type:** modify
- **Layer:** API
- **Files:**
  - `src/Explore.API/Configuration/QuartzSchedulerSettings.cs` (existing)
  - `src/Explore.API/Configuration/QuartzSchedulerSettingsValidator.cs` (existing)
  - `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` (existing)
  - `src/Explore.API/appsettings.json` (existing)
  - `docs/CONFIGURATION.md` (existing)
- **Description:** Add `ValidateSchemaOnStartup` (default `true`) to settings and set `store.PerformSchemaValidation` from it inside `ConfigurePersistentStore`. Validation is meaningless without a persistent store, so the validator must reject `ValidateSchemaOnStartup = true` combined with `UsePersistentStore = false`. Document the key in the `Scheduler:Quartz` table alongside a sentence explaining that it exists to catch silently-degrading optional columns.
- **Acceptance Criteria:**
  - [ ] `PerformSchemaValidation` reflects `ValidateSchemaOnStartup`
  - [ ] Validator rejects validation-without-persistent-store with a named failure
  - [ ] `QuartzSchedulerSettingsValidatorTests` covers the new rule
  - [ ] `docs/CONFIGURATION.md` documents the key and its purpose
- **Dependencies:** none
- **Effort:** S
- **Required Skills/Rules:** `cqrs-mediatr-guidelines` (manual validator convention)

#### Task 1.2: Execute the PostgreSQL scheduler DDL against a real engine
- **Type:** create
- **Layer:** Tests
- **Files:**
  - `tests/Event.API.IntegrationTests/Features/QuartzPostgreSqlSchemaTests.cs` (new)
- **Description:** Mirror `QuartzSqliteDurableSchedulingTests` against a `PostgreSqlContainer`: apply `QuartzSchemaInitializer.BuildStatements(PostgreSql, "QRTZ_")`, assert every required table exists in `information_schema.tables`, re-apply to prove idempotency, then run a real scheduler over `UsePostgres` and verify a scheduled job persists and fires. **Docker may be unavailable in some environments**, so detect it and skip with a recorded reason rather than failing — a red suite that means "no Docker" trains contributors to ignore red.
- **Acceptance Criteria:**
  - [ ] All 11 `QRTZ_` tables verified present after application
  - [ ] Second application succeeds unchanged
  - [ ] A scheduled trigger fires under the PostgreSQL delegate
  - [ ] The test skips cleanly and visibly when Docker is unavailable
- **Dependencies:** none
- **Effort:** M
- **Required Skills/Rules:** `.agents/rules/tests.md`

#### Task 1.3: Prove clustering does not double-fire
- **Type:** create
- **Layer:** Tests
- **Files:**
  - `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs` (new)
- **Description:** Start two scheduler instances sharing one store with the same `SchedulerName`, clustering enabled and `InstanceId = AUTO`, and schedule one trigger for a job that records executions in a shared static counter. Assert exactly one execution. Use SQLite when it can support the clustered lock handler, otherwise the PostgreSQL container from Task 1.2; record which store was used in the test name so the coverage claim is unambiguous.
- **Acceptance Criteria:**
  - [ ] Two clustered schedulers over one store produce exactly one execution for one trigger
  - [ ] Both schedulers register rows in `QRTZ_SCHEDULER_STATE`
  - [ ] The chosen store is explicit in the test name and ABOUTME
  - [ ] Skips cleanly if it depends on Docker and Docker is unavailable
- **Dependencies:** 1.2 (reuses the container fixture if PostgreSQL is chosen)
- **Effort:** M
- **Required Skills/Rules:** `.agents/rules/tests.md`

### Phase 2: Generalized Deadline Port

- **Goal:** one scheduler-neutral abstraction for "wake me at T with this pointer", replacing the dormant single-purpose port.
- **Depends on:** Phase 1
- **Related skills/rules:** `clean-architecture-rules`, `.agents/rules/application-layer.md`
- **Acceptance criteria:** Application expresses deadlines without any Quartz reference; the email reminder path uses the new port; the old port and its no-op are deleted.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** if the architecture boundary test fails, the leak is in the new contract's shape — remove the offending type rather than relaxing the test pattern.

#### Task 2.1: Introduce the deadline contract and delete the single-purpose port
- **Type:** create / delete
- **Layer:** Application
- **Files:**
  - `src/Explore.Application/Contracts/Scheduling/IScheduledDeadlineDispatcher.cs` (new)
  - `src/Explore.Application/Contracts/Scheduling/ScheduledDeadline.cs` (new)
  - `src/Explore.Application/Services/NoOpScheduledDeadlineDispatcher.cs` (new)
  - `src/Explore.Application/ApplicationServicesRegistration.cs` (existing) — swap the default registration
  - `src/Explore.Application/Contracts/Infrastructure/IScheduledEmailDispatchTrigger.cs` (existing) — delete
  - `src/Explore.Application/Contracts/Infrastructure/ScheduledEmailDispatchPointer.cs` (existing) — delete
  - `src/Explore.Application/Services/NoOpScheduledEmailDispatchTrigger.cs` (existing) — delete
- **Description:** Define `ScheduledDeadline(string JobName, string DeadlineKey, DateTimeOffset DueAt, IReadOnlyDictionary<string,string> Pointer)` — a closed, string-only pointer map so no domain object can be serialized into scheduler state by accident. Define `IScheduledDeadlineDispatcher` with `ScheduleAsync(ScheduledDeadline, CancellationToken)` returning a result record carrying success plus a failure category, and `CancelAsync(string jobName, string deadlineKey, CancellationToken)` returning whether anything was removed. The no-op returns `not_scheduled("scheduler_disabled")` and `false`, preserving today's behavior when no scheduler is registered. Delete the email-specific port, its pointer record, and its no-op; there is no caller to migrate.
- **Acceptance Criteria:**
  - [ ] `ScheduledDeadline.Pointer` is `IReadOnlyDictionary<string,string>` — no domain types
  - [ ] Contract has zero Quartz references
  - [ ] Old port, pointer, and no-op files are deleted
  - [ ] Default DI registration points at the new no-op
- **Dependencies:** none
- **Effort:** M
- **Required Skills/Rules:** `clean-architecture-rules`, `.agents/rules/application-layer.md`

#### Task 2.2: Implement the Quartz-backed dispatcher
- **Type:** create / delete
- **Layer:** API
- **Files:**
  - `src/Explore.API/Scheduling/QuartzScheduledDeadlineDispatcher.cs` (new)
  - `src/Explore.API/Scheduling/QuartzScheduledEmailDispatchTrigger.cs` (existing) — delete
  - `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` (existing) — swap the registration
  - `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs` (existing) — add a deadline trigger-key helper
- **Description:** Implement the port over `ISchedulerFactory`. Build a deterministic `TriggerKey` from `(jobName, deadlineKey)` so `CancelAsync` can find it and a re-schedule replaces rather than duplicates — pass `replace: true` semantics by unscheduling first or using a stable key. Serialize the pointer map into `JobDataMap` as individual string entries, consistent with `UseProperties = true`. Keep the existing failure-category behavior: catch non-cancellation exceptions, log the exception *type* only, and return `scheduler_unavailable`. Do **not** add scheduler-level retries — the owning domain's reconciliation sweep is the retry authority.
- **Acceptance Criteria:**
  - [ ] Deterministic trigger key from job name + deadline key
  - [ ] Re-scheduling the same deadline key does not create a duplicate trigger
  - [ ] `CancelAsync` removes an existing trigger and reports `false` when none existed
  - [ ] Only string values enter `JobDataMap`
  - [ ] Old trigger implementation deleted
- **Dependencies:** 2.1
- **Effort:** M
- **Required Skills/Rules:** `clean-architecture-rules`

#### Task 2.3: Re-point the event reminder job at the deadline envelope
- **Type:** modify
- **Layer:** API
- **Files:**
  - `src/Explore.API/Scheduling/EventReminderDispatchJob.cs` (existing)
  - `tests/Event.API.IntegrationTests/Features/EmailDispatchQuartzJobsTests.cs` (existing)
- **Description:** Read `tenantId`, `publishEventId`, and `useCase` as individual `MergedJobDataMap` string entries instead of a serialized `ScheduledEmailDispatchPointer`. Preserve today's defensive behavior exactly: absent or unparsable values are a logged no-op, not an exception, and an unsupported use case is skipped. Update the existing job tests to build the new data-map shape.
- **Acceptance Criteria:**
  - [ ] Job reads discrete string keys; no dependency on the deleted pointer record
  - [ ] Missing/invalid values remain a logged no-op
  - [ ] Use-case validation unchanged
  - [ ] Existing job tests pass against the new shape
- **Dependencies:** 2.1, 2.2
- **Effort:** S
- **Required Skills/Rules:** `clean-architecture-rules`

### Phase 3: Precise Inventory-Hold Expiry

- **Goal:** release held inventory at the deadline instead of up to 60 seconds late, without weakening recovery guarantees.
- **Depends on:** Phase 2
- **Related skills/rules:** `clean-architecture-rules`, `.agents/rules/application-layer.md`, `.agents/rules/domain.md`
- **Acceptance criteria:** holds expire on a deadline trigger; a reconciliation sweep still covers recovery targets and orphans; the polling worker is deleted.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** if deadline registration proves unreliable under load, raise the reconciliation sweep frequency — correctness never depended on the trigger.

#### Task 3.1: Add the expiry job and its reconciliation sweep
- **Type:** create
- **Layer:** API
- **Files:**
  - `src/Explore.API/Scheduling/InventoryHoldExpiryJob.cs` (new)
  - `src/Explore.API/Scheduling/InventoryHoldExpiryReconciliationJob.cs` (new)
  - `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs` (existing) — add two names
  - `src/Explore.Application/Services/ScheduledJobRegistry.cs` (existing) — add two descriptors
  - `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs` (existing) — add two keys
  - `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` (existing) — register both
- **Description:** `InventoryHoldExpiryJob` is durable, `[DisallowConcurrentExecution]`, and processes **one order** read from the data map (`tenantId`, `registrationOrderId`). It reuses the exact logic the worker holds today: set tenant context, expire each due hold via `TryExpireDueHoldAsync`, then call `IRegistrationOrderLifecycleService.RecoverExpiredHoldAsync`, always clearing tenant context in a `finally`. `InventoryHoldExpiryReconciliationJob` runs on a low-frequency cron and performs the current batch behavior — expired active holds plus `GetHoldExpiryRecoveryTargetsAsync` — catching anything the deadline path missed. **Append only** to `ScheduledJobNames`; the sweep constants added by the concurrent agent must not be reordered or edited.
- **Acceptance Criteria:**
  - [ ] Expiry job handles exactly one order from pointer data
  - [ ] Tenant context is set and cleared in a `finally`
  - [ ] Reconciliation job covers both expired holds and recovery targets
  - [ ] Both jobs appear in `IScheduledJobRegistry` as `Implemented`
  - [ ] Reconciliation cron is Quartz-valid (`?` day rule)
- **Dependencies:** 2.2
- **Effort:** L
- **Required Skills/Rules:** `clean-architecture-rules`

#### Task 3.2: Register and cancel hold deadlines from the order lifecycle
- **Type:** modify
- **Layer:** Application
- **Files:**
  - `src/Explore.Application/Features/RegistrationOrders/Handlers/Commands/CreateOrderWithHoldCommandHandler.cs` (existing)
  - `src/Explore.Application/Services/Registration/RegistrationOrderLifecycleService.cs` (existing)
- **Description:** After holds are persisted, register one deadline per order at the earliest `ExpiresAt`, keyed by the order id, with a pointer carrying only `tenantId` and `registrationOrderId` as strings. Deadline registration is an **optimization, not a correctness dependency**: a failure must be logged and swallowed, never failing the order-creation transaction, because the reconciliation sweep still covers the order. Cancel the deadline when an order reaches a terminal state so dead triggers do not accumulate.
- **Acceptance Criteria:**
  - [ ] Deadline registered after successful persistence, at the earliest hold expiry
  - [ ] A dispatcher failure never fails order creation
  - [ ] Terminal order transitions cancel the deadline
  - [ ] Pointer contains only string identifiers
  - [ ] No Quartz reference enters Application
- **Dependencies:** 3.1
- **Effort:** M
- **Required Skills/Rules:** `.agents/rules/application-layer.md`, `cqrs-mediatr-guidelines`

#### Task 3.3: Delete the polling worker and cover the new behavior
- **Type:** delete / modify
- **Layer:** API / Tests
- **Files:**
  - `src/Explore.API/BackgroundServices/InventoryHoldExpiryWorker.cs` (existing) — delete
  - `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` (existing) — remove its registration
  - `tests/Event.API.IntegrationTests/Features/InventoryHoldExpiryJobTests.cs` (new)
  - `docs/OPERATIONS.md`, `docs/CONFIGURATION.md` (existing)
- **Description:** Delete the worker outright — no flag, no dormant class. Add tests proving the expiry job expires due holds for its order and triggers lifecycle recovery, that it is a safe no-op when the order has already been finalized, and that the reconciliation job still picks up an order whose deadline never fired. Add both jobs to the `docs/OPERATIONS.md` job catalog and document the reconciliation cadence in `docs/CONFIGURATION.md`.
- **Acceptance Criteria:**
  - [ ] Worker file deleted and unregistered
  - [ ] Expiry job tested for the happy path and the already-finalized no-op
  - [ ] Reconciliation tested as the safety net for a missed deadline
  - [ ] Job catalog and configuration docs updated
- **Dependencies:** 3.1, 3.2
- **Effort:** M
- **Required Skills/Rules:** `.agents/rules/tests.md`

### Phase 4: Uniform Job Observability

- **Goal:** one place that knows how every scheduled job succeeded or failed.
- **Depends on:** Phase 3
- **Related skills/rules:** `clean-architecture-rules`
- **Acceptance criteria:** every job emits consistent duration and outcome telemetry; a listener fault cannot disturb scheduling.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** the listener is additive; unregistering it restores prior behavior with no data loss.

#### Task 4.1: Add an exception-contained telemetry job listener
- **Type:** create / modify
- **Layer:** API
- **Files:**
  - `src/Explore.API/Scheduling/SchedulerTelemetryJobListener.cs` (new)
  - `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` (existing) — `AddJobListener`
  - `src/Explore.Application/Telemetry/BusinessMetrics.cs` (existing) — add scheduler counters/histogram
  - `tests/Event.API.IntegrationTests/Features/SchedulerTelemetryJobListenerTests.cs` (new)
- **Description:** Implement `IJobListener` recording execution count, duration, and outcome, labelled by job name and group only — never tenant or payload values. **Every method body must be wrapped in `try`/`catch`**: Quartz documents that an unhandled listener exception can disrupt the scheduling cycle, so a telemetry defect must degrade to silence, not to a stalled scheduler. Record the exception *type* on failure, consistent with the existing dispatcher logging convention. Once the listener owns completion reporting, the per-job "completed" log lines become redundant — leave `MaintenanceSweepJobs.cs` untouched regardless, and note the cleanup as deferred work for its owner.
- **Acceptance Criteria:**
  - [ ] Listener records duration and outcome for every job
  - [ ] Labels carry no tenant identity or payload values
  - [ ] Every listener method is exception-contained, proven by a test with a throwing metrics sink
  - [ ] `MaintenanceSweepJobs.cs` is not modified
- **Dependencies:** 3.3
- **Effort:** M
- **Required Skills/Rules:** `clean-architecture-rules`

### Phase 5: Bounded Drain Migration And Contract Governance

- **Goal:** prove the drain-migration pattern on the smallest safe candidate, and give future scheduler work a contract.
- **Depends on:** Phase 4
- **Related skills/rules:** `clean-architecture-rules`, `create-agent-context-skill`
- **Acceptance criteria:** the finalization drain runs on cron with identical claim semantics; a `schedule-background-job` intent exists.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** if drain throughput regresses, tighten the cron interval; the underlying command is unchanged.

#### Task 5.1: Move registration finalization onto a cron job
- **Type:** create / delete
- **Layer:** API
- **Files:**
  - `src/Explore.API/Scheduling/RegistrationFinalizationDrainJob.cs` (new)
  - `src/Explore.API/BackgroundServices/RegistrationFinalizationWorker.cs` (existing) — delete
  - `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` (existing)
  - `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `Services/ScheduledJobRegistry.cs` (existing)
  - `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `Scheduling/QuartzSchedulerKeys.cs` (existing)
  - `docs/OPERATIONS.md` (existing)
- **Description:** The job sends the same `DrainRegistrationFinalizationEffectsCommand` from a scoped `ISender`, preserving the fenced-drain semantics exactly — **migrate the timer only**. Use `[DisallowConcurrentExecution]` to keep the old sequential guarantee and a 10-second cron to match today's cadence. Pass a consumer id that identifies the scheduler rather than the deleted worker.
- **Acceptance Criteria:**
  - [ ] Job dispatches the identical command with a scoped sender
  - [ ] `[DisallowConcurrentExecution]` preserves sequential behavior
  - [ ] Worker deleted and unregistered
  - [ ] Job appears in the registry and the `docs/OPERATIONS.md` catalog
- **Dependencies:** 4.1
- **Effort:** M
- **Required Skills/Rules:** `clean-architecture-rules`

#### Task 5.2: Add the `schedule-background-job` intent
- **Type:** create
- **Layer:** DevOps / Agent contract
- **Files:**
  - `.agents/contract/intents.yaml` (existing)
  - `docs/OPERATIONS.md` (existing)
- **Description:** Add an intent answering the eight contract questions for adding or changing a scheduled job: `must_read_docs` (`OPERATIONS.md`, `CONFIGURATION.md`, `TESTING.md`, `QUICK_REFERENCE.md`), `load_skills` (`clean-architecture-rules`, `cqrs-mediatr-guidelines`), `load_rules` (`application-layer.md`, `tests.md`), `paths_in_scope` (`src/Explore.API/Scheduling/**`, `src/Explore.Application/Contracts/Scheduling/**`, `src/Explore.API/Resources/Quartz/**`), `minimum_tests` (`Event.API.IntegrationTests`, `Event.Architecture.Tests`), and `forbidden_without_approval` — no Quartz types outside `Explore.API`, no payload/PII in `JobDataMap`, no EF migration for scheduler tables, no destructive scheduler DDL. Encode the two traps that already caused defects: the Quartz cron `?` rule, and optional columns that degrade silently.
- **Acceptance Criteria:**
  - [ ] Intent parses and follows the existing entry schema
  - [ ] Triggers include "add scheduled job", "background job", "cron job", "scheduler change"
  - [ ] Forbidden list encodes the four scheduler invariants
  - [ ] `docs/OPERATIONS.md` references the intent from the scheduler section
- **Dependencies:** 5.1
- **Effort:** S
- **Required Skills/Rules:** `create-agent-context-skill`

## 7. Testing Strategy

| Phase | Test project | Rationale |
|---|---|---|
| 1 | `Event.API.IntegrationTests` | The only project wired for real scheduler harnesses (SQLite file store, Testcontainers PostgreSQL) |
| 2 | `Event.Architecture.Tests` | The dominant risk of a new Application port is a boundary leak, which only this project detects |
| 3 | `Event.API.IntegrationTests` | Repeat with concrete reason: hold-expiry behavior needs a live scheduler and database, available only here |
| 4 | `Event.API.IntegrationTests` | Repeat with concrete reason: listener behavior is observable only through a running scheduler |
| 5 | `Event.Architecture.Tests` | Validates the intent registry and re-checks the scheduler boundary after the drain migration |

Contract-required distribution: `Event.Architecture.Tests` (Phases 2, 5) and `Event.API.IntegrationTests` (Phases 1, 3, 4). No browser, Playwright, Chrome DevTools MCP, Aspire, or live-service verification is planned. Testcontainers usage in Phase 1 is a database fixture, not application startup, and degrades to a visible skip when Docker is absent.

## 8. Documentation, Configuration, And Operations Impact

- **`docs/CONFIGURATION.md`** — add `ValidateSchemaOnStartup` and the reconciliation cadence to the `Scheduler:Quartz` table.
- **`docs/OPERATIONS.md`** — extend the scheduled-job catalog with `inventory-hold-expiry`, `inventory-hold-expiry-reconciliation`, and `registration-finalization-drain`; reference the new intent.
- **`.agents/contract/intents.yaml`** — new `schedule-background-job` intent.
- **`src/Explore.API/appsettings.json`** — new settings keys.
- **Not applicable:** OpenAPI/NSwag (scheduler is internal; status endpoint is excluded from description), EF migrations (scheduler tables are raw ADO), docker-compose/.env (no scheduler service or env var), Aspire.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- **Trust boundary unchanged.** The scheduler runs in-process under the API's existing boundary; no new inbound surface is added.
- **Privacy.** `ScheduledDeadline.Pointer` is constrained to `IReadOnlyDictionary<string,string>` precisely so a domain object carrying PII cannot be serialized into scheduler tables by accident. Telemetry labels are restricted to job name and group.
- **Tenant isolation.** The expiry job sets tenant context from its pointer and clears it in a `finally`, matching the worker it replaces. Tenant identity in the pointer is a durable identifier, not caller-supplied authority — the job re-reads state under that tenant rather than trusting any payload.
- **Auditability.** Failure logging records exception *types*, never messages, preserving the existing convention that raw provider errors stay out of logs.
- **Abuse.** Deadline registration is driven by order creation, which is already rate-limited and authorized; one trigger per order bounds growth, and terminal-state cancellation prevents accumulation.
- **No new secrets.** The scheduler reuses the application connection string.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Explanation |
|---|---|---|
| Multi-tenancy | **Applicable** | The expiry job is tenant-scoped; tenant context must be set from the durable pointer and cleared in a `finally` (Task 3.1) |
| Federation | Not Applicable | No AT Protocol or PDS surface is touched |
| Localization | Not Applicable | No user-facing strings; job names are stable operational identifiers, deliberately untranslated |
| Accessibility | Not Applicable | No UI is produced; the dashboard is out of scope |
| Product | **Applicable** | Held inventory is released at its deadline instead of up to 60 seconds late, returning sellable capacity sooner |

## 11. Observability And Operations

- **Metrics:** scheduler execution counter and duration histogram labelled by job name and group (Task 4.1).
- **Traces:** the existing `.AddSource("Quartz")` OTel registration already captures job spans.
- **Logs:** bounded — job name, group, outcome, and exception type. No payloads, tenant identifiers, or provider errors.
- **Health:** unchanged; `EmailDispatchHealthCheck` already reports scheduler enablement and persistent-store posture.
- **Operator visibility:** new jobs appear in the read-only status endpoint and the `docs/OPERATIONS.md` catalog.
- **Failure modes:** a missed deadline degrades to sweep latency, not lost work; schema drift fails startup loudly (Task 1.1); a telemetry fault degrades to silence.

## 12. Migration And Compatibility Plan

- **No database migration.** No EF change; the scheduler schema is unchanged by this workstream.
- **Deployment order:** single step. On first start after deploy, existing holds have no registered deadline and are covered by the reconciliation sweep until they expire — this is why the sweep is not optional.
- **Breaking changes:** `IScheduledEmailDispatchTrigger` and `ScheduledEmailDispatchPointer` are deleted. Safe because the port has no caller (§2.1). New `Scheduler:Quartz` keys are additive with defaults.
- **Rollback:** deleting the new jobs and restoring the two workers from git restores prior behavior; no data shape changes.
- **Orphaned triggers:** deadlines for orders that reached a terminal state before Task 3.2 shipped are cancelled on their next terminal transition or fire once and find nothing to do — both are safe.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|
| Merge conflict with the concurrent maintenance-sweep work | **High** | Medium | Append-only edits to `ScheduledJobNames`/`QuartzSchedulerKeys`; never open `MaintenanceSweepJobs.cs`; start after that work is committed | Git conflict in scheduler files | All phases |
| Docker unavailable, so Phase 1 proves nothing | Medium | Medium | Skip visibly with a recorded reason rather than failing; keep structural assertions as the floor | Test reports "skipped: Docker unavailable" | 1.2, 1.3 |
| Deadline registration inside order creation adds latency or failure surface | Medium | High | Register after persistence; swallow and log dispatcher failures; correctness rests on the sweep | Order-creation latency or error rate rises | 3.2 |
| Clustered SQLite cannot support the clustered lock handler | Medium | Low | Fall back to the PostgreSQL container and name the store in the test | Lock-handler exception on SQLite | 1.3 |
| Listener exception stalls the scheduling cycle | Low | **High** | Mandatory `try`/`catch` in every listener method, proven by a throwing-sink test | Jobs stop firing after a telemetry change | 4.1 |
| Trigger accumulation from uncancelled deadlines | Medium | Medium | Deterministic keys plus terminal-state cancellation; sweep is idempotent | `QRTZ_TRIGGERS` row count grows without bound | 3.2 |
| Schema validation fails startup on an untested provider | Medium | Medium | Phase 1 executes PostgreSQL first; the setting is an operator escape hatch | Startup failure naming a table/column | 1.1, 1.2 |

## 14. Success Metrics And Definition Of Done

- [ ] Held inventory is released at its deadline rather than on a 60-second poll
- [ ] `InventoryHoldExpiryWorker` and `RegistrationFinalizationWorker` are deleted, not disabled
- [ ] Application declares deadlines through one port with zero Quartz references
- [ ] PostgreSQL scheduler DDL is executed against a real engine, or skipped with a visible recorded reason
- [ ] Two clustered schedulers produce exactly one execution for one trigger
- [ ] `PerformSchemaValidation` is enabled by default
- [ ] Every scheduled job emits uniform duration and outcome telemetry
- [ ] A `schedule-background-job` intent exists and encodes the four scheduler invariants
- [ ] Each phase passes one Release build and its single selected test project

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Require future implementation agents to:

1. At the first implementation start, read plan, context, and tasks once; on a cold resume, read context and tasks first, then only the plan sections needed for the current phase or changed decision.
2. During an uninterrupted session, do not reread unchanged plan/context/tasks after every task; keep the current task in working context and reopen only the exact section needed.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `tasks.md` as the hot execution ledger: check a substantial task immediately after its implementation acceptance criteria are met, and reconcile smaller completed tasks together no later than phase end.
5. Keep implementation-task and phase-verification checkboxes separate; a task may be checked when its implementation is complete, but the phase is complete only after its build and selected test checkboxes pass.
6. Update the task status summary, completed count, current priority, next recommended slice, discovered tasks, deferred work, and `Last Updated` whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer; do not rewrite it for trivial edits.
8. Update the plan only when scope, architecture, phase order, acceptance criteria, risks, or validation strategy changes; do not churn it for ordinary progress.
9. Record failed validation with the known cause and next recovery action in tasks/context without marking the phase complete.
10. Before pausing, compaction, transfer, or PR creation, reconcile the affected tasks, add a concise dated handoff, and identify unrelated dirty files that the next contributor must avoid — this repository currently has concurrent agents editing the working tree.
11. Run phase verification only after all phase tasks, with one Release build and at most one selected project test; do not repeat successful commands or start the application/browser.
12. Never report completion when repository reality and the task ledger disagree.

Require every implementation summary to teach:

- what changed and why;
- architecture/design patterns, libraries, infrastructure, protocols, and project abstractions used;
- important files, classes, handlers, services, and components with their responsibilities;
- data/control flow;
- relevant repository conventions and reliability/security practices;
- verification performed, remaining work, next work, and dev-doc update status.

## 16. Progress Reporting Contract

Require this response shape after each implementation slice:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```

For completed implementation work, `Docs updated` must confirm that `tasks.md` was reconciled. Report context and plan separately as updated or unchanged because no trigger occurred.

## 17. Potential Risks & Unknowns

The part most likely to require iteration is **Task 3.2 — registering deadlines from inside order creation**. Everything else in this plan is additive or confined to the API layer; this one task reaches into a transactional command handler on the ticketing revenue path. Three things can go wrong. First, placement: registering before the transaction commits can schedule a deadline for an order that never exists, while registering after commit risks the process dying in between — which is precisely why the reconciliation sweep is mandatory rather than a nicety, and why a dispatcher failure must never fail the order. Second, cancellation coverage: `RegistrationOrderLifecycleService` has multiple terminal paths, and the investigation confirmed the *creation* call site but did not enumerate every completion and cancellation path; an implementer must find them all or accept trigger accumulation that only the sweep cleans up. Third, the concurrent-agent hazard is real and immediate — `MaintenanceSweepJobs.cs`, `ScheduledJobNames.cs`, and `QuartzSchedulerKeys.cs` are being edited right now in this working tree, so starting Phase 3 before that work is committed will produce conflicts in exactly the files this plan appends to.

A secondary unknown is whether **clustered SQLite** can satisfy Task 1.3 at all. Quartz's clustered lock handler depends on `SELECT … FOR UPDATE` semantics that SQLite does not provide in the same form; if it cannot, the clustering proof becomes Docker-dependent, and in an environment where Docker is unavailable the platform's most consequential unproven claim stays unproven. That would be worth escalating rather than quietly skipping.
