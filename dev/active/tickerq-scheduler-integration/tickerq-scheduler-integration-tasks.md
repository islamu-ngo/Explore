<!-- ABOUTME: Tactical checklist for integrating TickerQ as scheduler and operations layer. -->
<!-- ABOUTME: Tracks review, implementation slices, acceptance criteria, validation, and docs maintenance. -->

# TickerQ Scheduler Integration — Task Checklist

Last Updated: 2026-05-28 Europe/Brussels

## Status Summary

- **Overall status:** Draft
- **Completed:** 0/32 implementation tasks
- **Current priority:** User review of planning docs.
- **Next recommended slice:** Phase 0 package/API spike, then Phase 2 EmailDispatch drain-service extraction.

## Implementation Maintenance Rules

- [ ] Before starting work, read plan/context/tasks.
- [ ] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline

- [ ] **0.1 User reviews and approves/corrects the plan**
  - **Files:** `dev/active/tickerq-scheduler-integration/*`
  - **Acceptance:** Planning status changes from Draft to User-reviewed/Approved.
  - **Validation:** User confirmation or corrections recorded in context.
  - **Effort:** S
  - **Dependencies:** None

- [ ] **0.2 Implementation agent confirms current repo state before edits**
  - **Files:** `git status`, relevant active docs
  - **Acceptance:** Agent records unrelated dirty files/blockers and does not revert user changes.
  - **Validation:** Context file updated with baseline.
  - **Effort:** S
  - **Dependencies:** 0.1

- [ ] **0.3 Compile-verify TickerQ package/API names**
  - **Files:** `Directory.Packages.props`, target project files
  - **Acceptance:** Exact package names and selected version are proven by restore/build or recorded as blocker.
  - **Validation:** `dotnet restore`; focused build.
  - **Effort:** M
  - **Dependencies:** 0.1

- [ ] **0.4 Decide whether to add a scheduler-platform intent**
  - **Files:** `.claude/contract/intents.yaml` if changed; otherwise this context
  - **Acceptance:** Decision recorded; if context files change, architecture context tests run.
  - **Validation:** `Event.Architecture.Tests` if `.claude`/contract files are edited.
  - **Effort:** S
  - **Dependencies:** 0.1

## Phase 1: TickerQ Infrastructure Baseline

- [ ] **1.1 Add TickerQ packages through central package management**
  - **Files:** `Directory.Packages.props`; `Explore.API/Explore.API.csproj`; likely `Explore.Persistence/Explore.Persistence.csproj` or selected scheduler host project
  - **Acceptance:** TickerQ package versions are centrally pinned; no inline versions; restore succeeds.
  - **Validation:** `dotnet restore`; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`
  - **Effort:** M
  - **Dependencies:** 0.3

- [ ] **1.2 Add scheduler options and validator**
  - **Files:** new scheduler options/validator in Infrastructure; `InfrastructureServicesRegistration.cs`; `docs/CONFIGURATION.md`
  - **Acceptance:** Dashboard cannot be enabled insecurely in production; schema/path/node options validate on start.
  - **Validation:** `Explore.Infrastructure.Tests`
  - **Effort:** M
  - **Dependencies:** 1.1

- [ ] **1.3 Add TickerQ EF Core operational store**
  - **Files:** new scheduler DbContext/migration or equivalent TickerQ store configuration
  - **Acceptance:** TickerQ tables are in separate schema/context unless compile spike proves a better local pattern.
  - **Validation:** build; migration apply/integration test where feasible.
  - **Effort:** L
  - **Dependencies:** 1.1, 1.2

- [ ] **1.4 Wire TickerQ services in API composition root**
  - **Files:** `Explore.API/Program.cs`; service registration files
  - **Acceptance:** Scheduler can be enabled without altering current EmailDispatch behavior.
  - **Validation:** focused API startup test/build.
  - **Effort:** M
  - **Dependencies:** 1.3

- [ ] **1.5 Secure dashboard mapping**
  - **Files:** `Explore.API/Program.cs`; API integration tests; docs
  - **Acceptance:** Dashboard disabled/protected by default; enabled dashboard requires host auth/instance-admin policy.
  - **Validation:** focused `Event.API.IntegrationTests`
  - **Effort:** L
  - **Dependencies:** 1.2, 1.4

## Phase 2: Extract EmailDispatch Drain Service

- [ ] **2.1 Create `IEmailDispatchDrainService` contract**
  - **Files:** new `Explore.Application/Contracts/Services/IEmailDispatchDrainService.cs`
  - **Acceptance:** Contract has no TickerQ, SMTP concrete, RabbitMQ concrete, EF, or API dependency.
  - **Validation:** `Event.Architecture.Tests`
  - **Effort:** S
  - **Dependencies:** 0.1

- [ ] **2.2 Implement `EmailDispatchDrainService`**
  - **Files:** new Infrastructure/Application service implementation according to chosen ownership; existing DI registration
  - **Acceptance:** Preserves tenant pause, claim, tenant context rebind, SMTP send, attempt/receipt/final state, metrics, sanitized logs.
  - **Validation:** `Explore.Infrastructure.Tests`; existing EmailDispatch unit tests.
  - **Effort:** L
  - **Dependencies:** 2.1

- [ ] **2.3 Thin `EmailDispatchProcessor` to timer wrapper**
  - **Files:** `Explore.API/BackgroundServices/EmailDispatchProcessor.cs`
  - **Acceptance:** Hosted service delegates to drain service and no longer owns per-row business orchestration.
  - **Validation:** build; focused worker tests if present/added.
  - **Effort:** M
  - **Dependencies:** 2.2

- [ ] **2.4 Add drain-service behavior tests**
  - **Files:** `Explore.Infrastructure.Tests` or `Event.Application.UnitTests` depending on final service placement
  - **Acceptance:** Tests cover success, retryable failure, permanent/dead-letter, unknown, tenant paused, duplicate claim.
  - **Validation:** relevant test project.
  - **Effort:** L
  - **Dependencies:** 2.2

## Phase 3: TickerQ Email Dispatch Cron Job

- [ ] **3.1 Create stable scheduler job names registry**
  - **Files:** new `ScheduledJobNames` or equivalent
  - **Acceptance:** `email-dispatch-drain` and future names are constants and documented.
  - **Validation:** build; optional naming test.
  - **Effort:** S
  - **Dependencies:** 2.1

- [ ] **3.2 Implement TickerQ `email-dispatch-drain` function**
  - **Files:** new TickerQ job class in API/Infrastructure scheduler area
  - **Acceptance:** Function delegates to `IEmailDispatchDrainService`; no SMTP/RabbitMQ/domain state logic is embedded in the function.
  - **Validation:** unit test with substitute drain service.
  - **Effort:** M
  - **Dependencies:** 1.4, 2.2, 3.1

- [ ] **3.3 Seed/register recurring cron idempotently**
  - **Files:** scheduler registration/seeding code; configuration docs
  - **Acceptance:** Single cron schedule exists across restarts; uses second-level cadence only if TickerQ API and tests confirm it.
  - **Validation:** integration/startup test.
  - **Effort:** M
  - **Dependencies:** 3.2

- [ ] **3.4 Prove expected SMTP failures do not rely on TickerQ retries**
  - **Files:** drain service tests; TickerQ job tests
  - **Acceptance:** SMTP business failures are persisted in `EmailDispatchOutbox` and job completes normally; unexpected DB/infrastructure exceptions bubble to TickerQ.
  - **Validation:** unit/integration tests.
  - **Effort:** M
  - **Dependencies:** 3.2

## Phase 4: Dispatch Mode Switch And Rollout

- [ ] **4.1 Add explicit dispatch scheduler mode**
  - **Files:** `EmailDispatchProcessorSettings.cs`; validator; `Program.cs`; docs
  - **Acceptance:** `HostedService`, `TickerQ`, and `Disabled` modes or equivalent are explicit and validated.
  - **Validation:** options tests; health tests.
  - **Effort:** M
  - **Dependencies:** 3.3

- [ ] **4.2 Update `email-dispatch` health for selected mode**
  - **Files:** `EmailDispatchHealthCheck`; scheduler health checks
  - **Acceptance:** Disabled is visible, TickerQ mode reports scheduler readiness, Basic mode ignores RabbitMQ unless RabbitMQ mode is selected.
  - **Validation:** focused API health tests.
  - **Effort:** M
  - **Dependencies:** 4.1

- [ ] **4.3 Promote TickerQ as Basic Dispatch default**
  - **Files:** API registration/config/docs
  - **Acceptance:** Basic Dispatch Mode is PostgreSQL + TickerQ drain + SMTP; hosted worker remains only as temporary rollback if retained.
  - **Validation:** build; app startup test; EmailDispatch tests.
  - **Effort:** M
  - **Dependencies:** 4.2

## Phase 5: Enterprise Hardening

- [ ] **5.1 Extend side-effect architecture guard for TickerQ**
  - **Files:** `Event.Architecture.Tests/DurableSideEffectBoundaryTests.cs` or new scheduler boundary test
  - **Acceptance:** Handlers/controllers/domain cannot directly schedule TickerQ side-effect jobs.
  - **Validation:** `Event.Architecture.Tests`
  - **Effort:** M
  - **Dependencies:** 3.2

- [ ] **5.2 Add scheduler payload safety tests**
  - **Files:** new tests in architecture or infrastructure suite
  - **Acceptance:** Payloads are pointer-only and exclude PII/secrets/raw errors.
  - **Validation:** `Explore.Infrastructure.Tests`; `Event.Architecture.Tests`
  - **Effort:** M
  - **Dependencies:** 3.2

- [ ] **5.3 Wire TickerQ OpenTelemetry with bounded tags**
  - **Files:** `Explore.ServiceDefaults` or API OTEL setup; docs
  - **Acceptance:** Traces/logs expose job execution without forbidden high-cardinality/sensitive tags.
  - **Validation:** observability tests where practical; docs review.
  - **Effort:** M
  - **Dependencies:** 1.1

- [ ] **5.4 Add multi-node duplicate execution proof**
  - **Files:** persistence/API/Aspire tests
  - **Acceptance:** Two scheduler nodes do not double-send one `EmailDispatchOutbox` row; repository claim/receipt remains final guard.
  - **Validation:** PostgreSQL integration or Aspire distributed test.
  - **Effort:** XL
  - **Dependencies:** 4.3

- [ ] **5.5 Add crash-window/recovery proof**
  - **Files:** tests and operations docs
  - **Acceptance:** Killed node leaves work in documented recoverable or known-risk state; operators can diagnose.
  - **Validation:** integration/manual smoke plus docs.
  - **Effort:** L
  - **Dependencies:** 4.3

## Phase 6: Documentation And Operations

- [ ] **6.1 Update configuration docs**
  - **Files:** `docs/CONFIGURATION.md`
  - **Acceptance:** Scheduler, TickerQ, dashboard, dispatch mode, schema, and cron settings documented with safe defaults.
  - **Validation:** docs review; architecture docs tests if applicable.
  - **Effort:** M
  - **Dependencies:** 4.1

- [ ] **6.2 Update operations docs**
  - **Files:** `docs/OPERATIONS.md`, `docs/TROUBLESHOOTING.md`
  - **Acceptance:** Health, dashboard, job names, failure modes, and diagnostic steps documented.
  - **Validation:** docs review.
  - **Effort:** M
  - **Dependencies:** 4.2

- [ ] **6.3 Update architecture/outbox/self-hosting docs**
  - **Files:** `docs/ARCHITECTURE.md`, `docs/OUTBOX_PATTERN.md`, `docs/SELF_HOSTING.md`
  - **Acceptance:** Docs clearly state TickerQ schedules work while PostgreSQL owns outbox truth and RabbitMQ remains optional transport.
  - **Validation:** docs review.
  - **Effort:** M
  - **Dependencies:** 4.3

- [ ] **6.4 Update CRMWorx active docs after runtime behavior changes**
  - **Files:** `dev/active/crmworx-event-api-adaptation/*`
  - **Acceptance:** EmailDispatch workstream reflects TickerQ trigger if implemented.
  - **Validation:** docs review.
  - **Effort:** S
  - **Dependencies:** 4.3

## Phase 7: Future Scheduler Platform Work

- [ ] **7.1 Define `IScheduledJobRegistry`**
  - **Files:** new Application/Infrastructure contract
  - **Acceptance:** Stable job names, schedules, and ownership are discoverable.
  - **Validation:** unit tests.
  - **Effort:** M
  - **Dependencies:** 5.x

- [ ] **7.2 Define `IEventLifecycleScheduler`**
  - **Files:** new Application contract and implementation
  - **Acceptance:** Future reminders/waitlist jobs schedule pointer-only work and persist domain intent first.
  - **Validation:** unit/architecture tests.
  - **Effort:** L
  - **Dependencies:** 5.x

- [ ] **7.3 Plan PDS/general outbox scheduler migration**
  - **Files:** new or updated dev docs
  - **Acceptance:** No broad migration starts until EmailDispatch TickerQ mode is stable.
  - **Validation:** user-approved plan.
  - **Effort:** M
  - **Dependencies:** 4.3

## Verification Checklist

- [ ] LSP diagnostics clean for modified source files where supported.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes or unrelated blockers are documented.
- [ ] `Event.Application.UnitTests` passes.
- [ ] `Explore.Infrastructure.Tests` passes.
- [ ] `Event.Architecture.Tests` passes.
- [ ] `Event.Persistence.IntegrationTests` passes when Docker/Testcontainers are available or blockers are recorded.
- [ ] Focused `Event.API.IntegrationTests` for dashboard and health pass.
- [ ] Optional Aspire/Mailpit E2E registration confirmation proof passes before declaring production-ready default.
- [ ] Docs updated where behavior/config/operations/API changed.
- [ ] Dev docs refreshed with final state and remaining work.

## Remaining / Deferred Work

- RabbitMQ manual-ack consumer and DLQ replay remain outside this TickerQ first slice.
- Public/tenant UI for scheduler internals is rejected; product UI remains domain/HAL based.
- Broader lifecycle automation uses TickerQ only after EmailDispatch drain is proven.
