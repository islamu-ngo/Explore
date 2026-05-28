<!-- ABOUTME: Strategic implementation plan for adopting TickerQ as ISLAMU Event's scheduler and operations layer. -->
<!-- ABOUTME: Keeps PostgreSQL outbox state authoritative while introducing scheduler persistence, dashboard, and lifecycle jobs. -->

# TickerQ Scheduler Integration — Implementation Plan

Last Updated: 2026-05-28 Europe/Brussels

## 0. Planning Metadata

- **Request:** Create repository-grounded dev docs for adopting TickerQ as a background job scheduler, using the CTO feedback that TickerQ may schedule and coordinate work but must not become business truth, replace `EmailDispatchOutbox`, or replace RabbitMQ consumer semantics.
- **Task directory:** `dev/active/tickerq-scheduler-integration/`
- **Planning status:** Draft
- **Matched intents:** Multi-intent infrastructure/application/persistence/API planning. Closest intents are `add-cqrs-handler`, `update-repository-query`, `add-ef-migration`, `add-write-endpoint`, `add-hal-link`, and `openapi-contract-change`. No existing intent directly covers "add scheduler infrastructure", so this plan uses a fallback contract and adds a task to consider a future intent for background scheduler platform work.
- **Relevant skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `outbox-pattern`, `error-tracking`, `aspire`, `auth-patterns` for dashboard/API security.
- **Relevant rules:** `.claude/rules/application-layer.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/efcore-migrations.md`, `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/tests.md`.
- **Primary layers touched:** Application, Persistence, Infrastructure, API, AppHost/DevOps, Docs, Tests. Domain should stay mostly untouched unless a new explicit lifecycle scheduling entity is later approved.
- **Estimated complexity:** XL. This cuts across scheduler persistence, hosted service replacement, EmailDispatch reliability, dashboard auth, OpenTelemetry, health checks, migrations, self-hosting docs, and multi-node testing.

### 0.1 Contribution Contract Mapping

No current intent exactly matches "add scheduler infrastructure." The fallback contract is: obey `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, loaded skills/rules, and the stricter union of tests/docs from the closest matching intents below. Task 0.4 decides whether a new scheduler-platform intent should be added.

| Intent | Why it applies | Must-read docs / skills / rules | Paths in scope | Minimum tests | Docs to update | Unique acceptance / forbidden |
|---|---|---|---|---|---|---|
| `add-cqrs-handler` — Add Command/Query handler | Drain service and future scheduler commands may add Application orchestration. | `docs/ARCHITECTURE.md`, `docs/QUICK_REFERENCE.md`; `cqrs-mediatr-guidelines`; `.claude/rules/application-layer.md` | `Explore.Application/Features/**/*.cs`, Application contracts/services | `Event.Application.UnitTests`, `Event.Architecture.Tests` | none required by intent | Respect pipeline behavior; no cross-feature coupling. |
| `update-repository-query` — Modify repository query/specification | EmailDispatch claim/status/recovery paths may need repository additions. | `docs/QUICK_REFERENCE.md`; `dotnet-efcore-guidelines`; `.claude/rules/efcore-persistence.md` | `Explore.Persistence/Repositories/**/*.cs` | `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests` | none required by intent | Repositories return entities; no unsafe tenant-filter bypass. |
| `add-ef-migration` — Add/modify EF migration | TickerQ EF operational store likely needs migration/schema work. | `docs/QUICK_REFERENCE.md`, `docs/DOMAIN.md`; `dotnet-efcore-guidelines`; `.claude/rules/efcore-migrations.md` | `Explore.Persistence/Migrations/**/*.cs`, relevant persistence model files | `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests` | `schemas/islamu-event.md` if domain schema changes; scheduler schema docs if separate | Migration reversible; no destructive `Down()` without approval. |
| `add-write-endpoint` — Add authenticated write endpoint | Only if scheduler/operator APIs are added beyond TickerQ dashboard. | `docs/API.md`, `docs/QUICK_REFERENCE.md`, `docs/SECURITY-MODEL.md`; `cqrs-mediatr-guidelines`, `auth-patterns`; `.claude/rules/api-controllers.md` | `Explore.API/Controllers/**/*.cs`, Application commands | `Event.API.IntegrationTests`, `Event.Architecture.Tests` | `docs/API_CHANGELOG.md` | Writes authorized, rate-limited, idempotency considered; never remove `[Authorize]`. |
| `add-hal-link` — Modify HAL affordance | Product UI actions must remain HAL-gated if scheduler status/actions are exposed. | `docs/API.md`, `docs/QUICK_REFERENCE.md`; `.claude/rules/api-hateoas.md` | `Explore.API/Hateoas/**/*.cs`, `Explore.Blazor.Client/**/*.razor` | `Event.API.IntegrationTests`, `Explore.Blazor.Client.Tests` | none required by intent | UI gates by `_links`, never local role checks. |
| `openapi-contract-change` — Public API contract change | Any scheduler admin API changes affect OpenAPI/generated client. | `docs/API.md`, `docs/QUICK_REFERENCE.md`; `.claude/rules/api-controllers.md` | `Explore.API/Controllers/**/*.cs`, `docs/API_CHANGELOG.md` | `Event.API.IntegrationTests`, `Event.Architecture.Tests` | `docs/API_CHANGELOG.md` | Operation IDs stable; no breaking change without explicit approval, though pre-v1 allows breaking changes when documented. |

## 1. Executive Summary

Adopt TickerQ as the scheduler and operations layer for ISLAMU Event background work. TickerQ will trigger and coordinate jobs; it will not own domain workflow state. PostgreSQL-owned tables such as `EmailDispatchOutbox`, `EmailDispatchAttempt`, `EmailDispatchReceipt`, and tenant pause controls remain the durable business source of truth.

The first safe implementation slice is to replace the hand-rolled `EmailDispatchProcessor` polling loop with a TickerQ recurring cron job named `email-dispatch-drain`. The job calls an Application-owned orchestration service, `IEmailDispatchDrainService.ProcessBatchAsync(ct)`, which preserves the current PostgreSQL claim/send/attempt/receipt/final-state semantics.

Out of scope for the first slice:
- replacing `EmailDispatchOutbox`;
- making TickerQ a generic workflow engine;
- replacing RabbitMQ manual-ack consumer semantics;
- building public or tenant-facing UI around the TickerQ dashboard;
- storing email bodies, recipients, subjects, secrets, raw errors, or provider message IDs in TickerQ job payloads.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| No TickerQ integration currently exists. | Verified by `rg -n "TickerQ\|ticker"` returning no source matches outside this planning work. | High | Also no TickerQ package versions in `Directory.Packages.props`. |
| Central package management is enabled. | `Directory.Packages.props` has `ManagePackageVersionsCentrally=true`. | High | TickerQ package versions must be added centrally first. |
| API currently registers `EmailDispatchProcessor` as a hosted service. | `Explore.API/Program.cs` registers `builder.Services.AddHostedService<EmailDispatchProcessor>()`. | High | This is the first target for scheduler mode switching. |
| `EmailDispatchProcessor` owns Basic Dispatch Mode polling, tenant rebinding, SMTP send, attempts, receipts, and state transitions. | `Explore.API/BackgroundServices/EmailDispatchProcessor.cs`. | High | Logic should be extracted, not reimplemented inside TickerQ function methods. |
| Email dispatch state is PostgreSQL-owned and specialized. | `docs/adr/ADR-008-email-dispatch-state-machine.md`, `docs/OUTBOX_PATTERN.md`, `Explore.Domain/EmailDispatchOutbox.cs`, `Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs`. | High | TickerQ job status cannot replace this model. |
| `IEmailDispatchOutboxRepository` exposes entity-returning and atomic transition methods. | `Explore.Application/Contracts/Persistence/IEmailDispatchOutboxRepository.cs`; `Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs`. | High | TickerQ jobs should call Application services that use this repository contract. |
| RabbitMQ Dispatch Mode is optional and incomplete. | `dev/active/crmworx-event-api-adaptation/*`, `Explore.Application/Contracts/Infrastructure/IEmailDispatchTransport.cs`, `Explore.Infrastructure/Messaging/RabbitMqEmailDispatchTransport.cs`. | High | Publisher/topology/health foundation exists; manual-ack consumer and DLQ replay remain pending. |
| Direct side-effect boundary is enforced. | `Event.Architecture.Tests/DurableSideEffectBoundaryTests.cs`. | High | Must extend to prevent TickerQ from becoming a hidden side-effect path in handlers/controllers/domain. |
| EmailDispatch admin status uses HAL affordances. | `Explore.API/Controllers/EmailDispatchAdminController.cs`, `Explore.API/Hateoas/Policies/EmailDispatchStatusLinkPolicy.cs`. | High | Operator product UI must continue reading EmailDispatch state, not TickerQ job state. |
| Existing worktree has unrelated dirty work and known validation blockers. | `git status --short`; `dev/active/enterprise-data-model-hardening/enterprise-data-model-hardening-context.md`. | High | Implementation agents must scope diffs and not revert unrelated changes. |
| TickerQ supports EF Core persistence, job lifecycle state, retries, locking, dashboard auth, and OpenTelemetry. | Context7 `/arcenox-co/tickerq`; docs: `https://tickerq.net/docs/entity-framework`, `https://tickerq.net/docs/dashboard`, `https://tickerq.net/docs/opentelemetry`. | Medium | Package/API names must be verified during implementation against installed version. |

### 2.2 Existing Implementation

**Domain**
- `Explore.Domain/EmailDispatchOutbox.cs`, `EmailDispatchAttempt`, `EmailDispatchReceipt`, and `EmailDispatchTenantControl` are the durable email state model.
- Domain must not reference TickerQ, EF Core, SMTP, RabbitMQ, ASP.NET Core, or MediatR.

**Application**
- `CreateEventRegistrationCommandHandler` creates registration-confirmation `EmailDispatchOutbox` intent in the registration transaction.
- `IEmailDispatchOutboxRepository` returns entities and exposes status, tenant pause, claim, attempt, receipt, park, replay, sent, failed, and unknown methods.
- `IEmailDispatchTransport` and `EmailDispatchPointer` define optional broker boundaries and pointer-only payloads.

**Persistence**
- `EmailDispatchOutboxRepository` uses EF Core and `ExecuteUpdateAsync` for optimistic state transitions.
- `ExploreDbContext` has `DbSet` entries and tenant filters for email dispatch entities.
- TickerQ tables do not exist yet.

**Infrastructure**
- `IEmailService` and SMTP implementation already exist.
- `RabbitMqEmailDispatchTransport` is an optional pointer publisher/topology/health implementation using `RabbitMQ.Client`.
- TickerQ packages and services are not registered.

**API**
- `EmailDispatchProcessor` is a hosted `BackgroundService`.
- `Program.cs` registers the worker and existing health checks.
- EmailDispatch admin APIs expose domain status and replay/park/pause/resume actions. These must remain the product/operator interface.

**AppHost/Operations**
- Aspire currently wires optional RabbitMQ resource `messaging`.
- Self-hosting docs state Basic Dispatch Mode requires API + PostgreSQL + SMTP only.

### 2.3 Existing Tests And Verification Coverage

- `Event.Architecture.Tests/DurableSideEffectBoundaryTests.cs` prevents direct SMTP/RabbitMQ usage in Application handlers and API controllers.
- `Event.Application.UnitTests/Features/EventRegistrations/Commands/CreateEventRegistrationCommandHandlerTests.cs` covers durable intent creation.
- `Event.Application.UnitTests/Features/EmailDispatch/*` covers status, pause, replay, and park handlers.
- `Event.Persistence.IntegrationTests/Repositories/EmailDispatchTenantControlRepositoryTests.cs` and `EmailDispatchOutboxTransitionRepositoryTests.cs` cover repository transitions, though the current enterprise-data-model context records unrelated persistence failures in two transition tests.
- `Explore.Infrastructure.Tests/Infrastructure/EmailDispatchPointerTests.cs` covers pointer-only broker payloads.
- `Event.API.IntegrationTests/Features/EmailDispatchAdminControllerTests.cs` and `Features/Hateoas/EmailDispatchAdminHateoasTests.cs` cover admin endpoint/HAL behavior.
- No tests exist for TickerQ because no integration exists yet.

### 2.4 Existing Documentation And Contracts

- `docs/adr/ADR-008-email-dispatch-state-machine.md` is the controlling architectural decision for EmailDispatch.
- `docs/OUTBOX_PATTERN.md`, `docs/ARCHITECTURE.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, and `docs/SELF_HOSTING.md` describe current dispatch behavior.
- `docs/API_CONTRACT_INVENTORY.md`, `docs/API.md`, and `docs/API_CHANGELOG.md` include EmailDispatch admin API surface.
- `dev/active/crmworx-event-api-adaptation/` is the active source of truth for durable side-effect execution.
- `dev/active/rabbitmq-messaging/` is related but older/partly stale; its generic MQContract plan must not override the newer EmailDispatch-specific RabbitMQ capability gate.

### 2.5 Current Pain Points / Improvement Areas

1. `EmailDispatchProcessor` mixes scheduler loop mechanics with dispatch orchestration. Extracting `IEmailDispatchDrainService` will improve testability and allow TickerQ and hosted-service modes to call the same logic.
2. The current polling loop is API-local and has limited scheduler observability. TickerQ can add persisted job state, dashboard inspection, occurrence history, cancellation, retry visibility, and multi-node coordination.
3. There is no scheduler platform boundary today. Without explicit contracts, future reminders/waitlist/maintenance jobs could become ad hoc hosted services.
4. Dashboard security is a high-risk area. TickerQ dashboard defaults can be public when no auth is configured; production must require instance-admin auth or be disabled.
5. TickerQ job status could be confused with EmailDispatch business status. Architecture docs and tests must make this boundary explicit.
6. Scheduler payload privacy is not yet governed. TickerQ payloads must be pointer-only, following the RabbitMQ `EmailDispatchPointer` pattern.
7. Multi-node scheduler behavior is unproven in this repo. TickerQ has EF Core locking, but EmailDispatch still needs repository-level idempotency proof under two API instances.

### 2.6 Unknowns After Investigation

- Exact TickerQ NuGet package names and APIs for .NET 10 must be verified during implementation against version `10.2.0` or the selected current version. Context7 confirms package families and APIs, but compile-time proof is required.
- Whether TickerQ EF Core should use a separate `TickerQDbContext` or integrate with `ExploreDbContext` needs a spike. Preferred direction is a separate scheduler DbContext/schema to keep tenant-domain model snapshots clean.
- Dashboard path and authorization policy name need final naming. Preferred: disabled by default; when enabled, mount under an instance-admin-only path such as `/ops/tickerq`.
- TickerQ source generators may impose project/package placement constraints. First implementation slice must compile before broad use.
- Existing unrelated dirty work and known failing tests may block full-suite verification; implementation agents must record exact blockers rather than attributing them to TickerQ.

## 3. Proposed Future State

Target boundary:

```text
Application / Domain
  -> creates durable intent

PostgreSQL EmailDispatchOutbox
  -> owns business delivery state

TickerQ
  -> schedules / triggers / coordinates execution

Infrastructure/API job host
  -> invokes Application services and approved side-effect adapters

SMTP / RabbitMQ
  -> transports only

Operator API / HAL
  -> exposes safe domain status and actions
```

First runtime flow:

```text
TickerQ cron job: email-dispatch-drain
  -> IEmailDispatchDrainService.ProcessBatchAsync(ct)
  -> IEmailDispatchOutboxRepository.GetPendingBatch(...)
  -> TryMarkAsProcessing(...)
  -> tenantAccessor.SetTenant(...)
  -> IEmailService.SendAsync(...)
  -> RecordAttempt / MarkReceipt* / MarkAsSent|Failed|Unknown
```

Future scheduler examples:
- `general-outbox-drain`
- `pds-sync-drain`
- `email-dispatch-recovery-scan`
- `dead-letter-summary`
- `event-reminder-dispatch`
- `waitlist-promotion-scan`
- `tenant-maintenance-scan`

Stable job names become operational contracts and must be documented.

## 4. Non-Negotiable Constraints

- TickerQ schedules work; PostgreSQL owns business truth.
- `EmailDispatchOutbox` remains authoritative for sent/retry/dead-letter/parked/unknown/paused/replayable state.
- TickerQ retries are infrastructure retries, not email business retries.
- Application handlers/controllers/domain services must not send SMTP, publish RabbitMQ, or enqueue/schedule TickerQ jobs as a substitute for durable intent.
- Scheduler payloads must be pointer-only. No email body, recipient, subject, SMTP credentials, provider IDs, raw exceptions, tenant secrets, access tokens, or policy package content.
- Repositories return entities, never DTOs.
- Validators remain manually instantiated.
- Tenant isolation must be explicit in background execution; worker paths must rebind tenant context before tenant-specific config resolution.
- HAL links remain the only UI source for operator actions like replay/park.
- New files must start with two `ABOUTME:` lines.
- Pre-v1 breaking changes are allowed, but operational rollback switches are still acceptable when they reduce rollout risk.

## 5. Architecture And Design Decisions

### Decision 1: TickerQ Is Scheduler, Not Workflow Engine
- **Why:** Prevents duplicate truth between TickerQ job lifecycle and domain outbox lifecycle.
- **Alternatives considered:** Replace `EmailDispatchOutbox` with TickerQ status. Rejected because TickerQ statuses are generic and cannot represent `Unknown`, tenant pause, receipts, provider ambiguity, or HAL-gated replay.
- **Consequences:** Job functions call Application services; they do not encode domain transitions themselves.
- **Files/layers affected:** Application service contracts, API job functions, docs, architecture tests.

### Decision 2: First Slice Is EmailDispatch Drain
- **Why:** Existing worker is valuable but hand-rolled; it provides a safe equivalence test for TickerQ adoption.
- **Alternatives considered:** Add TickerQ to all background services first. Rejected due to blast radius.
- **Consequences:** Extract `EmailDispatchProcessor` logic before adding TickerQ cron function.
- **Files/layers affected:** `Explore.API/BackgroundServices/EmailDispatchProcessor.cs`, new Application service contract/implementation, tests.

### Decision 3: Prefer SQL Operational Store In Separate Schema
- **Why:** PostgreSQL is already a baseline dependency; separate schema avoids mixing scheduler internals with domain entities.
- **Alternatives considered:** Redis store first or embed TickerQ entities in `ExploreDbContext`. Rejected for first slice because Redis is not the durable baseline and `ExploreDbContext` is already complex.
- **Consequences:** Add scheduler DbContext/migrations and docs for schema ownership.
- **Files/layers affected:** `Explore.Persistence` or a dedicated scheduler infrastructure location, migrations, configuration docs.

### Decision 4: Dashboard Is Operator-Only And Disabled/Protected By Default
- **Why:** TickerQ dashboard can manage jobs and expose exception details.
- **Alternatives considered:** Expose dashboard as admin UI. Rejected because tenant admins must use HAL/domain admin screens.
- **Consequences:** Add `Scheduler:Dashboard:*` settings, host authentication, instance-admin policy, and tests proving production safety.
- **Files/layers affected:** API composition, auth policy, configuration docs, integration tests.

### Decision 5: RabbitMQ Consumer Semantics Stay Broker-Native
- **Why:** RabbitMQ still needs manual ack/nack/reject, bounded prefetch, confirms, DLQ, poison parking, and persisted outcome-before-ACK.
- **Alternatives considered:** Use TickerQ as a RabbitMQ substitute. Rejected.
- **Consequences:** TickerQ may schedule publisher drains/recovery scans, but manual-ack consumer remains a separate RabbitMQ worker.
- **Files/layers affected:** Future RabbitMQ slices, EmailDispatch transport docs/tests.

## 6. Implementation Phases

### Phase 0: Plan Review And Technical Spike
- **Goal:** Confirm the plan and compile-time TickerQ package/API shape.
- **Depends on:** User review.
- **Relevant files:** `Directory.Packages.props` existing; `Explore.API/Explore.API.csproj` existing; `Explore.Persistence/Explore.Persistence.csproj` existing.
- **Acceptance criteria:** User approves scope; implementation spike confirms exact package/API names and whether separate scheduler DbContext is viable.
- **Verification:** `dotnet restore`; focused compile after package references.
- **Rollback / failure handling:** Remove package references and keep existing hosted service untouched.

#### Task 0.1: Add Scheduler Platform Intent Proposal
- **Type:** docs/investigate
- **Layer:** Docs
- **Files:** `.claude/contract/intents.yaml` existing, or dev-doc note only if user does not want context-system changes yet
- **Description:** Decide whether recurring scheduler platform work deserves its own intent.
- **Acceptance Criteria:** A decision is recorded before implementation claims done.
- **Dependencies:** User review.
- **Effort:** S
- **Validation:** `Event.Architecture.Tests` if context files are changed.

### Phase 1: TickerQ Infrastructure Baseline
- **Goal:** Add TickerQ packages, persistence, configuration, health, and secure-by-default dashboard wiring without changing EmailDispatch behavior.
- **Depends on:** Phase 0.
- **Relevant files:** `Directory.Packages.props`, `Explore.API/Explore.API.csproj`, `Explore.Persistence/Explore.Persistence.csproj`, `Explore.API/Program.cs`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`.
- **Acceptance criteria:** TickerQ can start with SQL persistence; dashboard is disabled or protected; existing `EmailDispatchProcessor` still runs.
- **Verification:** Build, `Explore.Infrastructure.Tests`, `Event.Architecture.Tests`, dashboard security integration test.

#### Task 1.1: Add TickerQ Packages Centrally
- **Type:** modify
- **Layer:** DevOps/API/Persistence
- **Files:** `Directory.Packages.props` existing; project `.csproj` files existing
- **Description:** Add selected TickerQ packages, expected candidates `TickerQ`, `TickerQ.EntityFrameworkCore`, `TickerQ.Dashboard`, and `TickerQ.Instrumentation.OpenTelemetry` after NuGet/API verification.
- **Acceptance Criteria:** Restore succeeds; package versions are centrally pinned; no package added ad hoc to a project file with an inline version.
- **Dependencies:** 0.1
- **Effort:** M
- **Required Skills/Rules:** clean architecture, tests
- **Validation:** `dotnet restore`; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`

#### Task 1.2: Add Scheduler Configuration Options
- **Type:** create/modify
- **Layer:** Infrastructure/API
- **Files:** new `Explore.Infrastructure/Scheduler/SchedulerOptions.cs`; new validator; existing `Explore.Infrastructure/InfrastructureServicesRegistration.cs`; existing `docs/CONFIGURATION.md`
- **Description:** Define `Scheduler:*` and `Scheduler:TickerQ:*` options for enabled state, dashboard enabled state, dashboard path, dashboard auth policy, schema, node identity, and default cron intervals.
- **Acceptance Criteria:** Options validate on startup; production dashboard cannot be enabled without host auth/policy; invalid path/schema fails fast.
- **Dependencies:** 1.1
- **Effort:** M
- **Validation:** `Explore.Infrastructure.Tests` options validator tests.

#### Task 1.3: Add TickerQ Operational Store
- **Type:** create/modify
- **Layer:** Persistence/API
- **Files:** new scheduler DbContext/migration files; existing `Explore.Persistence/PersistenceServicesRegistration.cs`; existing `Explore.API/Program.cs`
- **Description:** Use TickerQ EF Core operational store with PostgreSQL, preferably in schema `ticker` or `scheduler`. Keep scheduler persistence separate from `ExploreDbContext` domain model unless spike proves integration is safer.
- **Acceptance Criteria:** TickerQ tables are created by migration path; domain model snapshot is not polluted with scheduler internals if separate DbContext is chosen.
- **Dependencies:** 1.1, 1.2
- **Effort:** L
- **Validation:** `dotnet build`; migration apply in persistence/API integration lane.

#### Task 1.4: Wire Secure Dashboard
- **Type:** create/modify/test
- **Layer:** API/Security
- **Files:** `Explore.API/Program.cs` existing; new scheduler dashboard tests; docs
- **Description:** Map dashboard only when enabled. Require host authentication with an instance-admin/operator policy. Do not expose it as tenant UI.
- **Acceptance Criteria:** Default production config does not expose unauthenticated dashboard; enabled dashboard requires expected policy; path is configurable.
- **Dependencies:** 1.2, 1.3
- **Effort:** L
- **Validation:** API integration tests for disabled/protected dashboard behavior.

### Phase 2: Extract EmailDispatch Drain Service
- **Goal:** Move current worker orchestration out of `EmailDispatchProcessor` into an Application-owned service contract with an implementation composed in API/Infrastructure.
- **Depends on:** None strictly; can start before TickerQ registration.
- **Relevant files:** `Explore.API/BackgroundServices/EmailDispatchProcessor.cs`; new `Explore.Application/Contracts/Services/IEmailDispatchDrainService.cs`; possible new `Explore.Infrastructure/EmailDispatch/EmailDispatchDrainService.cs`.
- **Acceptance criteria:** Hosted service and later TickerQ job both call the same service; behavior remains equivalent.
- **Verification:** Existing EmailDispatch unit/integration tests plus new focused drain service tests.

#### Task 2.1: Define Drain Service Boundary
- **Type:** create
- **Layer:** Application
- **Files:** new `Explore.Application/Contracts/Services/IEmailDispatchDrainService.cs`; optional request/result model
- **Description:** Add a stable orchestration boundary with batch result data such as processed count, sent count, retry count, unknown count, paused count, and failure count.
- **Acceptance Criteria:** Contract has no TickerQ, SMTP concrete, RabbitMQ, EF, or API dependency.
- **Dependencies:** none
- **Effort:** S
- **Validation:** Architecture tests.

#### Task 2.2: Move Processor Logic Behind The Service
- **Type:** create/modify
- **Layer:** Infrastructure/API
- **Files:** new implementation file; existing `EmailDispatchProcessor.cs`
- **Description:** Move batch and single-row processing from `EmailDispatchProcessor` into the service while keeping the hosted worker as a thin timer wrapper during transition.
- **Acceptance Criteria:** `EmailDispatchProcessor` does timer/scope/logging only; service preserves tenant pause, claim, tenant context rebind, SMTP send, attempt/receipt/state transitions, metrics, and sanitized logging.
- **Dependencies:** 2.1
- **Effort:** L
- **Validation:** `Event.Application.UnitTests`, `Explore.Infrastructure.Tests`, focused EmailDispatch tests.

### Phase 3: TickerQ Email Dispatch Cron Job
- **Goal:** Add `email-dispatch-drain` as the first recurring TickerQ job.
- **Depends on:** Phase 1 and 2.
- **Relevant files:** new `Explore.API/Scheduler/EmailDispatchTickerJobs.cs`; `Explore.API/Program.cs`; configuration docs.
- **Acceptance criteria:** TickerQ cron invokes the drain service; overlapping runs are skipped or prevented; expected SMTP failures persist in `EmailDispatchOutbox` and do not rely on TickerQ retries.
- **Verification:** Unit test job invokes service; integration test duplicate runs do not double-send.

#### Task 3.1: Create Stable Job Names Registry
- **Type:** create
- **Layer:** Application/Infrastructure
- **Files:** new `Explore.Application/Scheduler/ScheduledJobNames.cs` or similar
- **Description:** Define stable names like `email-dispatch-drain`, `general-outbox-drain`, `pds-sync-drain`, `email-dispatch-recovery-scan`, `event-reminder-dispatch`.
- **Acceptance Criteria:** Names are constants, documented, and used by scheduler registrations/tests.
- **Dependencies:** 2.1
- **Effort:** S
- **Validation:** Architecture/naming tests if added.

#### Task 3.2: Implement TickerQ Function
- **Type:** create
- **Layer:** API/Infrastructure
- **Files:** new TickerQ job class
- **Description:** Define a TickerQ function for `email-dispatch-drain` that resolves `IEmailDispatchDrainService` and calls `ProcessBatchAsync(ct)`. Avoid business logic in the function.
- **Acceptance Criteria:** Function has no SMTP/RabbitMQ/domain transition logic beyond invoking the approved service; TickerQ exceptions are reserved for unexpected infrastructure failures.
- **Dependencies:** 1.3, 2.2, 3.1
- **Effort:** M
- **Validation:** Unit test with substitute drain service.

#### Task 3.3: Seed/Configure Recurring Cron
- **Type:** create/modify
- **Layer:** API/Operations
- **Files:** scheduler registration/seeding code; docs
- **Description:** Register a recurring cron schedule using a 6-part expression for second-level cadence, defaulting to the current five-second behavior unless final TickerQ API constraints require a safer interval.
- **Acceptance Criteria:** Job schedule is idempotently registered; no duplicate cron definitions across restarts; disabled scheduler mode does not register or run the job.
- **Dependencies:** 3.2
- **Effort:** M
- **Validation:** Integration test or startup test proving one schedule exists.

### Phase 4: Dispatch Mode Switch And Rollout
- **Goal:** Make TickerQ the default scheduler for Basic Dispatch Mode after equivalence is proven.
- **Depends on:** Phase 3.
- **Relevant files:** `EmailDispatchProcessorSettings.cs`, `EmailDispatchHealthCheck`, `Program.cs`, docs.
- **Acceptance criteria:** `EmailDispatchProcessor:Mode=HostedService|TickerQ|Disabled` or equivalent selected-mode config works; Basic mode remains PostgreSQL + scheduler + SMTP.
- **Verification:** Mode-isolation tests; health checks.

#### Task 4.1: Add Mode Configuration
- **Type:** modify
- **Layer:** Infrastructure/API
- **Files:** `Explore.Infrastructure/EmailDispatchProcessorSettings.cs`; validator; `Explore.API/Program.cs`
- **Description:** Replace boolean-only worker behavior with explicit selected scheduler mode. Pre-v1 breaking config is acceptable, but preserve a rollback mode until TickerQ is proven.
- **Acceptance Criteria:** HostedService, TickerQ, and Disabled are explicit; invalid mode fails startup; disabled remains health-visible.
- **Dependencies:** 3.3
- **Effort:** M
- **Validation:** options tests and health tests.

#### Task 4.2: Promote TickerQ Mode
- **Type:** modify/docs
- **Layer:** API/Docs
- **Files:** `Program.cs`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, `docs/OUTBOX_PATTERN.md`
- **Description:** Make TickerQ the default trigger for Basic Dispatch Mode and keep old hosted timer as temporary rollback.
- **Acceptance Criteria:** Docs state `EmailDispatchOutbox` remains business truth; TickerQ is trigger/operations layer only.
- **Dependencies:** 4.1
- **Effort:** M
- **Validation:** build, docs review, API health tests.

### Phase 5: Enterprise Hardening
- **Goal:** Prove security, privacy, observability, and multi-node safety.
- **Depends on:** Phase 4.
- **Relevant files:** tests, docs, ServiceDefaults/OpenTelemetry wiring.
- **Acceptance criteria:** Dashboard is secure, payloads are pointer-only, OpenTelemetry tags are bounded, duplicate execution does not double-send, killed node leaves recoverable work.
- **Verification:** Architecture tests, API integration tests, persistence integration tests, optional Aspire multi-instance smoke.

#### Task 5.1: Add Architecture Guardrails For Scheduler Boundary
- **Type:** modify/test
- **Layer:** Architecture Tests
- **Files:** `Event.Architecture.Tests/DurableSideEffectBoundaryTests.cs` existing or new scheduler boundary tests
- **Description:** Prevent handlers/controllers/domain from using TickerQ scheduling APIs directly for business side effects. Approved locations are scheduler infrastructure and application scheduling ports only.
- **Acceptance Criteria:** Direct TickerQ manager/function use in handlers/controllers fails tests unless explicitly approved.
- **Dependencies:** 3.2
- **Effort:** M
- **Validation:** `Event.Architecture.Tests`.

#### Task 5.2: Add Scheduler Payload Safety Tests
- **Type:** test
- **Layer:** Unit/Architecture
- **Files:** new tests in `Explore.Infrastructure.Tests` or `Event.Architecture.Tests`
- **Description:** Assert job payload/request models are pointer-only and exclude PII/secrets/raw errors.
- **Acceptance Criteria:** Tests cover EmailDispatch job payload and future lifecycle scheduling payload pattern.
- **Dependencies:** 3.2
- **Effort:** M
- **Validation:** `Explore.Infrastructure.Tests`, `Event.Architecture.Tests`.

#### Task 5.3: Wire OpenTelemetry Carefully
- **Type:** modify/test/docs
- **Layer:** ServiceDefaults/API/Operations
- **Files:** `Explore.ServiceDefaults`, API scheduler registration, docs
- **Description:** Add TickerQ ActivitySource and instrumentation. Ensure metrics/log tags do not include forbidden high-cardinality or sensitive values.
- **Acceptance Criteria:** Allowed tags include job name/type, outcome, failure category, node ID; forbidden tags are excluded.
- **Dependencies:** 1.1
- **Effort:** M
- **Validation:** observability tests where practical; docs.

#### Task 5.4: Multi-Node And Crash-Window Verification
- **Type:** test/investigate
- **Layer:** Integration/E2E
- **Files:** persistence/API/Aspire tests
- **Description:** Run two API/scheduler nodes against one PostgreSQL store and verify only one node drains each dispatch row. Kill one node during processing and verify recoverability/known risk state.
- **Acceptance Criteria:** Duplicate job occurrences do not double-send; repository claim and receipt idempotency remain the last line of defense.
- **Dependencies:** 4.2
- **Effort:** XL
- **Validation:** PostgreSQL integration or Aspire distributed app test.

### Phase 6: Future Lifecycle Scheduler Primitives
- **Goal:** Add reusable scheduling ports for reminders, waitlist scans, maintenance, and recovery after EmailDispatch TickerQ mode is stable.
- **Depends on:** Phase 5.
- **Relevant files:** new Application contracts like `IEventLifecycleScheduler`, `IScheduledJobRegistry`, `ISchedulerHealthReporter`.
- **Acceptance criteria:** All future scheduled jobs use pointer payloads and persist domain state in domain-owned tables before side effects.
- **Verification:** Unit/architecture tests per new job family.

## 7. Testing Strategy

- **Application unit:** drain service boundary, expected SMTP/provider outcomes persist domain state, expected failures do not rely on TickerQ retries.
- **Infrastructure unit:** TickerQ options validator, dashboard settings validator, payload safety, job function calls drain service.
- **Architecture:** no direct SMTP/RabbitMQ/TickerQ side effects from handlers/controllers/domain; TickerQ APIs constrained to approved scheduler infrastructure.
- **Persistence integration:** TickerQ schema migration, EmailDispatch repository idempotency remains green, two-node duplicate claim safety where feasible.
- **API integration:** dashboard disabled/protected behavior, health checks for selected scheduler mode, EmailDispatch admin status remains domain/HAL based.
- **E2E/Aspire:** optional final smoke for registration confirmation through PostgreSQL + TickerQ cron + SMTP/Mailpit.
- **Commands:** run projects individually per `docs/TESTING.md`, not solution-level `dotnet test`.

## 8. Documentation, Configuration, And Operations Impact

Update:
- `docs/CONFIGURATION.md` for `Scheduler:*`, `Scheduler:TickerQ:*`, `EmailDispatchProcessor:Mode`, dashboard settings, schema, cron interval.
- `docs/OPERATIONS.md` for scheduler health, dashboard usage, job names, failure modes.
- `docs/SELF_HOSTING.md` for Basic Dispatch Mode becoming PostgreSQL + TickerQ drain + SMTP, while still not requiring RabbitMQ.
- `docs/ARCHITECTURE.md` and `docs/OUTBOX_PATTERN.md` for the scheduler vs outbox truth boundary.
- `docs/API.md`/`docs/API_CHANGELOG.md` only if new operator API endpoints are added.
- `dev/active/crmworx-event-api-adaptation/` if EmailDispatch runtime behavior changes.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Dashboard is disabled by default or host-auth protected with instance-admin/operator policy.
- Dashboard is never tenant-admin tooling and must not be linked as normal UI affordance.
- Dashboard access should be audited if enabled.
- Job payloads are pointer-only.
- Logs/traces/metrics must not include recipient, subject, body, raw exception, SMTP response, connection string, provider message ID, tokens, or secrets.
- TickerQ job creation/update operations must not become a way to bypass HAL-gated EmailDispatch replay/park controls.
- Tenant context must be rebound before tenant SMTP settings or governance settings are resolved.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy:** Applicable. Scheduler jobs run outside request tenant resolution and must bind tenant context explicitly when processing tenant work.
- **Federation:** Applicable later. `PdsSyncWorker` may eventually become a TickerQ-triggered drain, but not in the first slice.
- **Localization:** Not directly applicable to scheduler internals; applicable to any future operator-facing product UI, not TickerQ dashboard internals.
- **Accessibility:** Not applicable to TickerQ dashboard unless embedded/customized; product admin UI must remain accessible if scheduler status is surfaced there.
- **Product:** Applicable. Product UI must expose EmailDispatch domain status and HAL actions, not scheduler internals.

## 11. Observability And Operations

- Add scheduler health check separate from EmailDispatch health.
- Keep EmailDispatch metrics as domain outcomes: sent, retry scheduled, dead-lettered, parked, unknown, tenant paused.
- Add TickerQ job execution tracing with bounded tags: job name, job type, outcome, failure category, node ID.
- Document that TickerQ failed jobs indicate scheduler/infrastructure failure; EmailDispatch status indicates business delivery state.
- Add troubleshooting entries for scheduler disabled, dashboard locked down, job schedule missing, job failed, stale processing rows, and duplicate occurrence skipped.

## 12. Migration And Compatibility Plan

- Pre-v1: breaking configuration changes are allowed.
- Add TickerQ operational tables through an explicit migration path. Prefer a separate schema such as `ticker` or `scheduler`.
- Do not migrate EmailDispatch business state into TickerQ.
- No data backfill is required for first slice beyond idempotently seeding the cron job.
- Preserve a temporary hosted-service fallback mode for operational rollback, not for long-term backward compatibility.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| TickerQ becomes hidden workflow engine | Medium | High | Architecture tests, docs, Application service boundary | Domain/admin status disagrees with TickerQ status | 5.1 |
| Dashboard exposed publicly | Medium | High | Disabled/protected by default, integration tests | Unauthenticated dashboard route returns 200 | 1.4 |
| Job payload leaks PII/secrets | Medium | High | Pointer-only payload tests and review checklist | Payload contains body/recipient/secret/raw error | 5.2 |
| Duplicate cron execution double-sends | Medium | High | TickerQ skip/locking plus repository claim/receipt idempotency | Duplicate `EmailDispatchAttempt` or duplicate Mailpit message | 5.4 |
| Package/API mismatch with .NET 10 | Medium | Medium | Spike package compile before broad implementation | Restore/build failure | 0.1/1.1 |
| Existing dirty worktree blocks validation | High | Medium | Scope tests, record unrelated blockers, do not revert | Failures in unrelated files/workstreams | All |
| Operators confuse TickerQ dashboard with product status | Medium | Medium | Docs and UI boundary; keep HAL status authoritative | Support incidents inspect only scheduler job | 8/11 |

## 14. Success Metrics And Definition Of Done

Functional success:
- Registration confirmation email can flow through PostgreSQL `EmailDispatchOutbox` + TickerQ recurring drain + SMTP.
- EmailDispatch status, attempts, receipts, unknown/dead-letter/park/replay semantics remain unchanged.
- RabbitMQ remains optional and is not replaced by TickerQ.

Quality gates:
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` where Docker/Testcontainers are available
- focused `Event.API.IntegrationTests` for dashboard/health/API behavior
- optional Aspire/Mailpit E2E for final registration-confirmation proof

Docs gates:
- Plan/context/tasks are updated after every slice.
- Configuration, operations, self-hosting, architecture, and outbox docs reflect shipped behavior.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Future agents implementing this plan MUST follow this contract:

1. Before starting any implementation slice, read this plan, `tickerq-scheduler-integration-context.md`, and `tickerq-scheduler-integration-tasks.md`.
2. Start from the highest-priority incomplete task unless user instruction overrides it.
3. After completing each meaningful task or discovering new scope, update:
   - this plan if architecture/scope/phases/risks changed;
   - `tickerq-scheduler-integration-context.md` with current state, decisions, files changed, blockers, validation, and next step;
   - `tickerq-scheduler-integration-tasks.md` by checking completed items and adding discovered tasks.
4. Do not report "done" unless docs reflect the actual current state.
5. Every implementation summary to the user must include what changed, which patterns/libraries/infrastructure were used, important files/classes, data/control flow, conventions followed, verification, remaining work, and next step.
6. If validation fails, update context/tasks with the failure, root cause if known, and next recovery action.
7. Before pausing, context reset, handoff, or PR creation, refresh all three dev docs and add/refresh a handoff section.

## 16. Progress Reporting Contract

When an implementation agent finishes a slice, its final response should use:

- **Implemented:** medium-sized developer teaching summary naming TickerQ role, PostgreSQL outbox state, Application services, Infrastructure/API job host, and important files.
- **Verified:** exact commands and results.
- **Remaining:** concrete unchecked tasks and blockers.
- **Next:** next recommended task.
- **Docs updated:** plan/context/tasks updated yes/no with reason.

## 17. Potential Risks & Unknowns

The hardest part is not making TickerQ run; it is preventing scheduler state from becoming a second workflow engine. The first implementation must therefore make `IEmailDispatchDrainService` the execution boundary: TickerQ triggers it, the hosted fallback can trigger it, and all durable business semantics stay in `EmailDispatchOutbox`. The other high-risk area is dashboard security because an unauthenticated scheduler dashboard would expose operational control and potentially exception details. Both risks need tests before TickerQ becomes the default.
