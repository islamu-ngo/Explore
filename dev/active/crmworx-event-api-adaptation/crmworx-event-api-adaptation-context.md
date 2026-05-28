<!-- ABOUTME: Current-state evidence for adapting CRMWorx durable side-effect execution patterns into ISLAMU Event. -->
<!-- ABOUTME: Tracks repository facts, CTO feedback, research findings, active dependencies, and constraints implementation agents must preserve. -->

# CRMWorx Event API Adaptation — Context

Last Updated: 2026-05-28 Europe/Brussels

## 1. Purpose

This workstream turns the completed CRMWorx analysis reports into an ISLAMU Event implementation program. It is not a direct port of CRMWorx. It adapts the useful reliability patterns into Event's .NET 10, EF Core, Clean Architecture, CQRS/MediatR, HAL, tenant-isolated, Aspire-tested platform.

The CTO-approved framing is **Durable Side-Effect Execution for Event Lifecycle Workflows**: PostgreSQL state machines are the center, RabbitMQ is one optional transport, and the first vertical slice is registration confirmation email.

Primary source reports:

- `dev/active/crmworx-api-report.md` — conceptual transfer report.
- `dev/active/crmworx-api-implementation-report.md` — source-level CRMWorx implementation report covering queue-first email, RabbitMQ, outbox, sequence enrollment, automation, EAV/custom fields, and testing.

## 2. User And CTO Requirements Captured

- Create repository-grounded dev docs under `dev/active/[task-name]/`.
- Required files: plan, context, and tasks.
- Every file must include `Last Updated: YYYY-MM-DD Europe/Brussels`.
- Use Tavily MCP and Context7 MCP research. Initial Context7 attempts failed with a streamable HTTP session error, but the 2026-05-26 RabbitMQ/Aspire continuation succeeded through Context7 and Aspire docs; Tavily and librarian research also succeeded and are recorded below.
- Apply repository skills and conventions: `aspire`, `auth-patterns`, `dotnet-efcore-guidelines`, `outbox-pattern`, `cqrs-mediatr-guidelines`, `error-tracking`, `agentic-research`, and `clean-architecture-rules`.
- No backward compatibility requirement because the repository is pre-release/development. This permits replacing weak interim abstractions when that better fits the architecture, but still requires migrations, tests, docs, and clear intent records.
- RabbitMQ must not be mandatory. ISLAMU Event must function with or without RabbitMQ like it functions with optional Keycloak/Cerbos-style deployment choices.
- The approved first release path is Basic Dispatch Mode: API + PostgreSQL + SMTP provider.
- RabbitMQ Dispatch Mode is optional and must share the same `EmailDispatchOutbox` state machine.
- Choose `EmailDispatchOutbox` first, not a combined `NotificationDispatchOutbox`.
- First flow: `RegistrationConfirmed -> create in-app Notification if needed -> create EmailDispatchOutbox row -> dispatch pipeline -> receipt + attempts + final delivery state`.

## 3. Matched Repository Intents

The implementation work will touch several intent classes. Agents must classify each PR/subtask against the relevant intent before editing code.

| Intent | Why it applies | Required docs/rules/tests |
|---|---|---|
| `add-cqrs-handler` | Registration confirmation, dispatch state, tenant controls, and later lifecycle automation require Application handlers and validators. | `docs/ARCHITECTURE.md`, `docs/QUICK_REFERENCE.md`, `.claude/rules/application-layer.md`, `Event.Application.UnitTests`, `Event.Architecture.Tests` |
| `add-write-endpoint` | Later admin APIs may replay, park, pause/resume, and inspect dispatch operations. | `docs/API.md`, `docs/SECURITY-MODEL.md`, `.claude/rules/api-controllers.md`, API integration and architecture tests |
| `add-ef-migration` | `EmailDispatchOutbox`, receipts, attempts, tenant controls, and optional lifecycle automation require EF migrations. | `docs/DOMAIN.md`, `.claude/rules/efcore-migrations.md`, Persistence integration and architecture tests |
| `update-repository-query` | Dispatch workers, claim operations, tenant-safe specifications, and status reads require repository changes. | `.claude/rules/efcore-persistence.md`; never return DTOs from repositories |
| `add-hal-link` | Later pause/resume/replay/park operations must be link-gated. | `.claude/rules/api-hateoas.md`; HAL fail-closed tests |
| `openapi-contract-change` | New admin endpoints must update API contract docs. Breaking change restrictions are relaxed by user direction, but must still be documented. | `docs/API.md`, `docs/API_CHANGELOG.md`, API contract tests |

## 4. Current Event State From Repository Evidence

### 4.1 Already present

- General transactional outbox exists: `Explore.Domain/OutboxMessage.cs`, `IOutboxRepository`, `IOutboxMessageDispatcher`, `OutboxRepository`, `OutboxMessageConfiguration`, `OutboxProcessorSettings`, and `Explore.API/BackgroundServices/OutboxProcessor.cs`.
- Specialized outbox precedent exists: `PdsSyncOutbox` and `PolicyChangeOutbox` are separate from the generic `OutboxMessage` model.
- Messaging provider abstraction exists from `dev/active/rabbitmq-messaging`: `MessagingProviderEnum`, `IMessagingProvider`, `MessagingConfiguration`, `RabbitMqMessagingProvider`, `RuntimeMessagingProvider`, `MqContractOutboxMessageDispatcher`, and `EventPublishedIntegrationEvent`.
- Notification API/domain exists but is in-app focused: `Notification`, `UserNotificationPreference`, `NotificationController`, `NotificationResourceAssembler`, `NotificationLinkPolicy`.
- Email SMTP abstraction exists: `IEmailService`, `EmailMessage`, `SmtpConfiguration`, `SmtpEmailService`, `SmtpConfigResolver`, `EmailResiliencePipelines`, `SmtpHealthCheck`, unsubscribe token service.
- Existing evidence shows no application workflow currently creates `EmailMessage` or calls `IEmailService.SendAsync`; SMTP sending is isolated in `Explore.Infrastructure/Mail/SmtpEmailService.cs`.
- Custom-property/EAV infrastructure is substantial: governance policy, runtime value validator, template instantiation/sync services, projection updaters, repositories, DbSets, and tenant filters.
- Tenant/auth/HAL infrastructure is mature: `TenantResolverService`, `TenantContext`, named EF query filters, `AuthorizationBehavior`, fallback authorization, `HateoasAuthorizationEvaluator`, route names, and HAL link policies.
- Observability foundations exist: OpenTelemetry service defaults, `BusinessMetrics`, projection metrics, translation metrics, secret metrics, structured audit logging, health checks.
- Self-hosting docs already model optional infrastructure through profiles such as `storage` and `authz`; this supports an optional `rabbitmq`/dispatch profile model.

### 4.2 Gaps this workstream targets

- No `EmailDispatchOutbox` state machine with delivery state, receipt ledger, delivery attempts, unknown outcome, parking, replay, and tenant pause controls.
- Registration confirmation email is not wired even though UI copy promises one.
- RabbitMQ abstraction is not fully verified and must not block Basic Dispatch Mode: health, metrics, Docker/Aspire wiring, tests, and build verification remain unfinished in `dev/active/rabbitmq-messaging`.
- No direct notification-to-email fanout or durable email delivery tracking pipeline.
- No Event-specific lifecycle automation subsystem. Existing workflows are UI helpers and managed provider provisioning/template sync; there is no generic or registration-focused automation runtime.
- Outbox processor and RabbitMQ provider lack direct failure-window tests matching the CRMWorx failure matrix.
- Custom properties exist, but future work must avoid using EAV for core workflow concepts that should be explicit entities/aspect tables.

## 5. Dispatch Mode Decision Required

| Decision | Approved direction | Notes |
|---|---|---|
| Default mode | Basic Dispatch Mode | API + PostgreSQL + SMTP provider; no RabbitMQ requirement. |
| Optional mode | RabbitMQ Dispatch Mode | API + PostgreSQL + RabbitMQ + SMTP provider; same state machine, optional profile. |
| Durable source of truth | PostgreSQL `EmailDispatchOutbox` | Broker state never owns business truth. |
| First slice | Registration confirmation email | No reminders, bulk email, generic automation, or admin UI first. |
| RabbitMQ abstraction | Gate MQContract | Keep only if reliability semantics are proven; otherwise replace/wrap below Event port. |
| Admin/HAL | Later | Add only after state machine and Basic slice are proven. |

## 6. Existing Dev Workstreams That Must Be Coordinated

| Workstream | Status | Role in this plan |
|---|---|---|
| `dev/active/rabbitmq-messaging/` | Active, partially complete. Provider abstraction mostly complete; health, metrics, Docker/Aspire, tests, and build verification unfinished. | Optional RabbitMQ Dispatch Mode gate only, not first-slice dependency. |
| `dev/pause/notification-system/` | Paused. Notification entity/API/query/HAL work mostly present; migration deferred; explicitly excludes creation/dispatch. | Dependency/input for in-app notification resource model and later HAL affordances. |
| `dev/pause/email-smtp-abstraction/` | Paused. MailKit + DB settings + Polly retry mostly complete; stale task status; obsolete files noted. | Dependency/input for Basic Dispatch Mode SMTP sender abstraction, settings resolution, and health checks. |
| `dev/active/modularity-event-aspects-note.md` | Architectural note. | Reinforces vertical aspect tables over fat tables or blanket EAV. |
| `dev/active/mvp-report.md` | MVP evidence. | Confirms email confirmations are missing despite UI copy and existing SMTP/outbox primitives. |

## 7. CRMWorx Patterns To Adapt

### 7.1 Durable email dispatch state

CRMWorx's strongest pattern is a dedicated email dispatch workflow:

1. A durable `email_outbox` row records intent and business retry state.
2. A worker claims pending rows and creates/reuses a stable publish/dispatch event ID.
3. RabbitMQ, when used, receives a pointer-only event payload with no message body or secrets.
4. Publisher marks transport success only after correlated broker confirm and no return.
5. A manual-ack consumer re-enters tenant context, claims a receipt by event ID, validates tenant/outbox/event identity, dispatches SMTP, and persists success/retry/permanent/unknown outcome.
6. DLQ replay validates database truth before requeue and parks unsafe messages.

Event translation: create a specialized `EmailDispatchOutbox`. Keep business truth in PostgreSQL/EF Core. Basic mode sends directly through the SMTP abstraction from the worker. RabbitMQ mode only transports durable pointer events.

### 7.2 Sequence enrollment and automation

CRMWorx separates workflow decisions into durable state machines, but Event should not start with a generic workflow engine. Start with Event lifecycle workflows only: registration confirmation, approved/rejected registration, waitlist promotion, reminders, cancellation, and organizer notifications.

### 7.3 Custom fields and data modeling

CRMWorx uses a hybrid model, not pure EAV:

- Work-item custom fields for tenant-defined optional metadata.
- Projection rows for list/filter/export performance.
- Explicit columns or aspect tables when concepts become workflow-critical.

Event already has Layer 3 custom-property governance. The plan must preserve the boundary: custom fields are for long-tail metadata, while dispatch status, registration lifecycle, automation execution, delivery state, tenant pause/replay/parking, and idempotency must be explicit entities/aspects.

### 7.4 Testing lanes

CRMWorx validates async workflows with unit, persistence integration, RabbitMQ/SMTP container tests, E2E chain tests, migration rehearsal, architecture tests, and failure matrices. Event should copy the lane structure and adapt it to TUnit, PostgreSQL fixtures, Aspire, Playwright, and Testcontainers where appropriate.

## 8. External Research Findings

Context7 calls for EF Core and RabbitMQ .NET Client failed with a streamable HTTP session error. Tavily and librarian research supplied current guidance:

- Use .NET Generic Host and `BackgroundService` for long-running workers.
- Use a transactional outbox: write domain state and outbox state in the same PostgreSQL transaction; publish/dispatch asynchronously.
- Basic Dispatch Mode should be viable with only PostgreSQL + SMTP.
- RabbitMQ consumers should use manual acknowledgements, bounded prefetch, durable queues, and publisher confirms for reliable publish semantics.
- RabbitMQ .NET guidance warns against auto-ack, unbounded prefetch, polling with `BasicGet`, and sharing one channel across concurrent publishers.
- Consumers must be idempotent by stable message/event ID.
- Testcontainers for .NET supports PostgreSQL/RabbitMQ integration tests; Aspire testing supports closed-box distributed app E2E.
- Use options validation/`ValidateOnStart` and health checks to make selected-mode misconfiguration explicit.
- Avoid direct publish-after-`SaveChanges`, distributed transactions/2PC, auto-ack consumers, unlimited prefetch, `basic.get` polling, and broker-as-source-of-truth workflow design.

## 9. Non-Negotiable Event Constraints

- Domain remains dependency-free.
- Application references Domain abstractions only.
- Repositories return entities, never DTOs.
- Validators are manually instantiated in handlers/services.
- `Guid` for aggregates, `int` for lookups, `long` for cursors.
- GET endpoints are `[AllowAnonymous]`; write endpoints are `[Authorize]`.
- HAL links are the single source of truth for UI action affordances.
- Tenant isolation remains enforced through EF filters and explicit worker tenant rebinding.
- ProblemDetails responses include safe details, trace/correlation metadata, and no secrets.
- No new silent no-op production behavior. Misconfiguration must be health-visible and test-visible.
- No handler, controller, automation executor, sequence processor, or domain service may send SMTP directly or publish RabbitMQ directly; they may only create durable intent.

## 10. Open Decisions For Implementation

These must be settled in Phase 0 before code changes:

1. Final ADR for `EmailDispatchOutbox` as durable state and Basic/RabbitMQ profiles.
2. Whether invalid Basic SMTP config fails fast or starts dispatch paused/unhealthy.
3. Whether MQContract proves required RabbitMQ reliability semantics or must be replaced/wrapped.
4. Exact state enum names and transition methods for pending, dispatching/leased, sent, retry, dead-letter, parked, unknown, and replay.
5. Exact first registration confirmation trigger and template/message payload reference strategy.
6. Which operational controls ship in release 1: global pause, tenant pause, retry scheduling, dead-letter, parking, replay endpoint, health, metrics/logs, duplicate consume protection.
7. Which custom-property definitions may drive later automation conditions and what projection/index requirements they need.

## 11. Documentation Maintenance Contract

Implementation agents must update this context file whenever they discover new repository evidence, alter phase scope, complete or cancel a dependency, choose an open decision, or encounter a verification failure. Updates must preserve the `Last Updated` line in Europe/Brussels date format.

## 12. Implementation Evidence — Basic Dispatch Mode Slice

The first implementation pass has started the approved Basic Dispatch Mode path without making RabbitMQ mandatory.

### 12.1 Added durable state model

- Added Domain entities for `EmailDispatchOutbox`, `EmailDispatchAttempt`, `EmailDispatchReceipt`, and `EmailDispatchTenantControl`.
- Added state enums for `Pending`, `Processing`, `Sent`, `RetryScheduled`, `DeadLettered`, `Parked`, and `Unknown` email dispatch outcomes.
- Added attempt and receipt state so SMTP success, retryable failure, timeout/unknown, and terminal failure can be recorded outside the registration handler.
- Added tenant pause control as explicit PostgreSQL state. The Basic dispatcher checks tenant pause before claiming an outbox row, so pause does not burn a send attempt.

### 12.2 Added persistence contracts and EF state

- Added `IEmailDispatchOutboxRepository` with entity-returning methods, `bool` claim/receipt methods for contention, and cancellation-token-aware state transitions.
- Added EF configurations for outbox, attempts, receipts, and tenant controls.
- Added DbSets and tenant filters for email dispatch entities.
- Generated EF migrations for the email dispatch outbox schema and tenant-control schema.

### 12.3 Registration confirmation now creates durable intent

- `CreateEventRegistrationCommandHandler` now builds a registration-confirmation `EmailDispatchOutbox` row with tenant, event, user, registration intent, recipient snapshot, subject/body snapshot, and correlation ID.
- `EventRegistrationIntentRepository.CreateWithChildrenAndCapacityAsync` accepts the optional email dispatch row and inserts it inside the existing serializable registration/capacity transaction.
- The handler still does **not** call SMTP, RabbitMQ, or any transport adapter. It only creates durable intent.

### 12.4 Added Basic dispatcher worker

- Added `EmailDispatchProcessor` as a `BackgroundService`.
- The worker polls due `EmailDispatchOutbox` rows, checks tenant pause, claims a row via optimistic update, sets `ITenantContextAccessor` before SMTP config resolution, calls the existing `IEmailService`, and records attempts/receipts/final state.
- Timeout-like provider errors transition to `Unknown` instead of blind retry.
- Retryable failures schedule retry through PostgreSQL state; exhausted attempts transition to dead-letter state.

### 12.5 Verification evidence so far

- `dotnet ef migrations add AddEmailDispatchOutbox --project Explore.Persistence/Explore.Persistence.csproj --startup-project Explore.API/Explore.API.csproj --context ExploreDbContext` completed successfully.
- `dotnet ef migrations add AddEmailDispatchTenantControls --project Explore.Persistence/Explore.Persistence.csproj --startup-project Explore.API/Explore.API.csproj --context ExploreDbContext` completed successfully.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed: 1,022/1,022 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 172 succeeded, 1 skipped.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed: 108/108 tests.
- Follow-up verification after the tenant-control migration:
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed again: 1,022/1,022 tests.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed again: 172 succeeded, 1 skipped.
  - `dotnet build --configuration Release --verbosity quiet` passed: 25 projects, 0 errors. The warning log contains the repository's existing warning baseline; no warnings matched `EmailDispatch`, `CreateEventRegistrationCommandHandler`, or `AddEmailDispatch`.
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed again: 108/108 tests.
- Independent verifier result: PASS, with no blocking findings. The verifier confirmed the registration handler creates durable intent only, PostgreSQL owns the dispatch state, SMTP is isolated to `EmailDispatchProcessor`, RabbitMQ is not required for Basic Dispatch Mode, tenant context is rebound before SMTP resolution, repositories return entities, migrations are reversible, and the green verification set is sufficient for this first slice.

### 12.6 Basic dispatch configuration and health visibility

- Added `EmailDispatchProcessorSettingsValidator` to validate Basic Dispatch Mode worker options on startup with `ValidateOnStart`.
- Validation covers polling interval, batch size, max attempts, retry delay bounds, and non-empty consumer identity.
- Added `EmailDispatchHealthCheck` to make Basic Dispatch Mode status visible to operators.
- The health check reports `Degraded` when dispatch is intentionally disabled and `Healthy` when the Basic processor is enabled.
- Health data exposes only operational configuration (`enabled`, polling interval, batch size, max attempts, consumer ID) and does not expose email bodies, recipients, or secrets.
- Wired the health check as `email-dispatch` with `ready`, `email`, `dispatch`, and `infrastructure` tags.
- LSP diagnostics were clean for `EmailDispatchProcessorSettingsValidator`, `EmailDispatchHealthCheck`, `InfrastructureServicesRegistration`, `Program`, and this workstream directory.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed after the hardening changes: 173 total, 172 succeeded, 1 skipped, 0 failed.
- `dotnet build --configuration Release --verbosity quiet` passed after the hardening changes: 25 projects, 0 errors. Focused warning grep found no `EmailDispatch`, `email_dispatch`, `CreateEventRegistrationCommandHandler`, or `AddEmailDispatch` warnings in the build warning log.
- Added focused tests for the Basic dispatch validator and health check:
  - `Explore.Infrastructure.Tests/Infrastructure/EmailDispatchProcessorSettingsValidatorTests.cs` verifies valid defaults and rejects invalid polling interval, batch size, retry window, and consumer ID.
  - `Event.API.IntegrationTests/Features/EmailDispatchHealthCheckTests.cs` verifies enabled dispatch is `Healthy`, disabled dispatch is `Degraded`, and health data does not expose body, recipient, or secret fields.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed: 288 total, 288 succeeded, 0 failed.
- Focused TUnit runs for the two new API health-check tests passed with `--treenode-filter` and `--minimum-expected-tests 1`:
  - `CheckHealthAsync_WhenDispatchEnabled_ReturnsHealthyWithSafeData`: 1 total, 1 succeeded, 0 failed.
  - `CheckHealthAsync_WhenDispatchDisabled_ReturnsDegraded`: 1 total, 1 succeeded, 0 failed.
- A full `Event.API.IntegrationTests` project run timed out before completion and is not counted as evidence for this slice; focused TUnit method-level runs were used instead.

### 12.7 Basic dispatch metrics and safe logging

- Added low-cardinality Basic Dispatch Mode metrics through the existing `BusinessMetrics` OpenTelemetry meter (`Explore.Business`).
- Added counter `explore.email_dispatch.attempts` with bounded tags only: `tenant_id`, `outcome`, and `failure_category`.
- `EmailDispatchProcessor` records outcomes for sent, tenant paused, unknown, retry scheduled, and dead-lettered dispatch attempts without making RabbitMQ mandatory.
- Tightened worker warning logs so SMTP provider error text is not emitted as a structured log field for retry/unknown outcomes; logs now use dispatch ID, tenant/source IDs where already present, outcome, retry delay, and normalized failure category.
- Added `Event.Application.UnitTests/Telemetry/BusinessMetricsEmailDispatchTests.cs` to verify the metric emits expected tags and does not emit body, recipient, subject, secret, provider-message, or raw error labels.
- LSP diagnostics were clean for `BusinessMetrics`, `EmailDispatchProcessor`, and the new metrics tests.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed after fixing the metrics test harness for TUnit parallel execution: 1,024 total, 1,024 succeeded, 0 failed.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` remained green for this slice: 173 total, 172 succeeded, 1 skipped, 0 failed.
- `dotnet build --configuration Release --verbosity quiet` passed after the metrics/logging updates: 25 projects, 0 errors. Warnings remain the repository baseline.

### 12.8 Canonical Basic Dispatch Mode documentation

- Updated `docs/CONFIGURATION.md` with the implemented `EmailDispatchProcessor:*` settings, startup validation rules, default values, and the boundary that SMTP credentials still come from the existing `email.*` governance/secret keys.
- Updated `docs/OPERATIONS.md` with the `email-dispatch` readiness check, Basic Dispatch operational flow, `explore.email_dispatch.attempts` metric tags, and safe structured logging expectations.
- Updated `docs/SELF_HOSTING.md` to state that Basic Dispatch Mode is implemented with API + PostgreSQL + SMTP and requires no RabbitMQ profile. RabbitMQ Dispatch Mode is documented as optional future infrastructure only.
- Updated `docs/ARCHITECTURE.md` to record the hard architecture rule: handlers, controllers, automation executors, sequence processors, and domain services create durable intent only; approved background workers own SMTP/broker side effects.
- Updated `docs/OUTBOX_PATTERN.md` to list `EmailDispatchOutbox` as a specialized outbox variant and reinforce that RabbitMQ, when added later, is transport only over the PostgreSQL state machine.
- LSP diagnostics were clean for all five updated canonical docs.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed after the canonical-doc updates: 173 total, 172 succeeded, 1 skipped, 0 failed.

### 12.9 Operator-safe dispatch status read path

- Added `EmailDispatchStatusDto` as the first operator-safe Basic Dispatch Mode read model.
- The DTO exposes lifecycle fields only: outbox ID, tenant ID, source type/id, delivery status, attempt count, next retry timestamp, normalized failure category/timestamps, unknown/delivered/parked timestamps, and correlation ID.
- The DTO intentionally excludes recipient email, subject, plain/html body, reply-to, provider message ID, and raw provider error text.
- Added `GetEmailDispatchStatusQuery` and `GetEmailDispatchStatusQueryHandler` under Application. The handler validates tenant ID and bounded limit, calls the entity-returning repository, and maps to the safe DTO in Application.
- Extended `IEmailDispatchOutboxRepository` and `EmailDispatchOutboxRepository` with `GetStatusRows(Guid tenantId, int limit, CancellationToken)`. The implementation uses `AsNoTracking`, tenant filtering, and entity ordering; it does not return DTOs from Persistence.
- Added `EmailDispatchAdminController` with authenticated `GET /api/admin/email-dispatch/status` using route name `GetEmailDispatchStatus`, MediatR dispatch, authenticated rate limiting, and lookup timeout. This endpoint is read-only and does not introduce HAL management actions yet.
- Added `GetEmailDispatchStatusQueryHandlerTests` to verify validation and safe projection behavior. The test constructs an outbox entity containing recipient/body/subject/reply-to/provider/raw-error fields and verifies the exposed DTO surface omits those fields.
- LSP diagnostics were clean for the status DTO, query, handler, repository contract/implementation, admin controller, route names, JSON context, and new tests.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed after adding the status read path and analyzer cleanup: 1,027 total, 1,027 succeeded, 0 failed.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed after analyzer cleanup: 288 total, 288 succeeded, 0 failed.
- Focused API health-check tests were rerun with updated analyzer-friendly names and passed with `--treenode-filter` / `--minimum-expected-tests 1`.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed for the status-read slice: 173 total, 172 succeeded, 1 skipped, 0 failed.
- `dotnet build --configuration Release --verbosity quiet` passed after the status-read slice and analyzer cleanup: 25 projects, 0 errors, 5,118 warnings. Focused warning scan for `EmailDispatch`, `email_dispatch`, `GetEmailDispatchStatus`, `EmailDispatchAdmin`, `EmailDispatchStatusDto`, `EmailDispatchHealth`, and `BusinessMetricsEmailDispatch` returned no matches.

### 12.10 Durable side-effect architecture guard

- Added `Event.Architecture.Tests/DurableSideEffectBoundaryTests.cs` to enforce the CTO-approved durable-intent boundary.
- The guard scans all Application feature handler files under `/Handlers/`, not only files named `*Handler.cs`, so plural/container handler files are covered.
- Application handlers fail the guard if they reference direct email transport contracts and call send methods, or if they reference broker transport contracts and call publish/subscribe methods.
- API controllers fail the guard if they perform direct SMTP sends or RabbitMQ/MQContract broker operations.
- The guard intentionally avoids a false positive on `IPolicyPackageService.PublishAsync`, because policy package sync is not RabbitMQ/MQContract transport.
- The guard also allows safe SMTP configuration checks such as `IEmailService.TestConnectionAsync`; the forbidden operation is direct send from handlers/controllers.
- LSP diagnostics were clean for `DurableSideEffectBoundaryTests.cs`.
- Focused TUnit runs passed for both guard tests:
  - `ApplicationHandlersShouldNotSendEmailOrPublishBrokerMessagesDirectly`
  - `ApiControllersShouldNotSendEmailOrPublishBrokerMessagesDirectly`
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed after the guard was tightened: 175 total, 174 succeeded, 1 skipped, 0 failed.
- `dotnet build --configuration Release --verbosity quiet` passed after the guard: 25 projects, 0 errors. Focused warning scan found only the repository's existing MailKit NU1902 advisory lines and no guard-specific warnings.
- A read-only verifier retry failed with an infrastructure `UnknownError`, so it is not counted as evidence; deterministic LSP, focused guard tests, full architecture tests, and Release build are the recorded verification for this slice.

### 12.11 Async workflow failure matrix

- Added `crmworx-event-api-adaptation-failure-matrix.md` to freeze the expected behavior for Basic Dispatch Mode and future RabbitMQ Dispatch Mode failure windows.
- The matrix keeps PostgreSQL state as the source of truth and names expected outcomes for rollback, commit, duplicate registration, dispatch disabled, tenant pause, duplicate claims, worker crash windows, SMTP success/failure/timeout, retry exhaustion, missing SMTP configuration, status reads, and direct-side-effect violations.
- The RabbitMQ section is intentionally future-facing and records required behavior for optional broker mode without making RabbitMQ part of the Basic Dispatch Mode acceptance path.
- Each row names expected persisted state, operator signal, and the validation lane required before claiming that scenario complete.
- LSP diagnostics were clean for `dev/active/crmworx-event-api-adaptation` after adding the matrix.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed after the matrix update: 175 total, 174 succeeded, 1 skipped, 0 failed.

### 12.12 Email dispatch architecture decision record

- Added `docs/adr/ADR-008-email-dispatch-state-machine.md` to record the accepted decision for a PostgreSQL-owned `EmailDispatchOutbox` state machine.
- The ADR documents three dispatch profiles: dispatch disabled, Basic Dispatch Mode, and future RabbitMQ Dispatch Mode.
- The ADR records the hard side-effect boundary: handlers, controllers, automation executors, sequence processors, and domain services create durable intent only; background dispatch components own SMTP and future broker side effects.
- The ADR keeps RabbitMQ optional and requires pointer payloads, stable publish identity, mandatory routing, confirms, returned-message handling, manual ack/reject/nack, bounded prefetch, DLQ/parking, health, metrics, graceful shutdown, and receipt idempotency before RabbitMQ Dispatch Mode can start.
- Linked the ADR from `docs/index.md`.
- LSP diagnostics were clean for `docs/adr/ADR-008-email-dispatch-state-machine.md`, `docs/index.md`, and this workstream directory after the ADR update.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed after the ADR/index update: 175 total, 174 succeeded, 1 skipped, 0 failed.
- A follow-up architecture/docs gate after recording dispatch profiles and first-slice task evidence also passed: 175 total, 174 succeeded, 1 skipped, 0 failed.

### 12.13 Registration confirmation E2E through Basic Dispatch Mode

- Added `MailpitContainerFixture` to the Aspire/Playwright E2E project using the generic Testcontainers container image `axllent/mailpit:v1.30.0`.
- Extended `AppHostFixture` to start Mailpit, configure the API's SMTP settings in PostgreSQL governance settings, and lower Basic dispatch polling delay for E2E runs.
- Added a Keycloak ROPC token helper to `BffKeycloakFixture` so the registration-confirmation dispatch proof can call the real API directly with a bearer token instead of relying on the Blazor/YARP BFF path.
- Extended `RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations` to prove the complete Basic Dispatch Mode path:
  1. Seed a tenant, event, and registration session.
  2. Obtain a Keycloak test-user bearer token.
  3. Call the API directly with `X-Tenant-Slug` and `Authorization` headers.
  4. Create an event registration.
  5. Assert registration intent and child registration rows exist.
  6. Wait for `EmailDispatchOutbox` to reach `Sent`.
  7. Assert a successful `EmailDispatchAttempt` and completed `EmailDispatchReceipt` exist.
  8. Assert Mailpit received the registration confirmation email for `user@test.islamu.org`.
- Kept the E2E proof on Basic Dispatch Mode only. RabbitMQ is not started, configured, or required.
- Fixed E2E seed/runtime blockers discovered while exercising the full path:
  - Development seed cleanup no longer uses unsupported EF Core TPT `ExecuteDelete` against the `Event` hierarchy.
  - Ramadan conference seed sessions now reference an existing event day.
  - Islamic session aspect seed normalization now stores relative prayer start metadata only when both prayer and offset are present.
  - Removed the obsolete database default for `event_session_islamic_aspects.start_time_type` via migration `20260525160808_RemoveEventSessionIslamicAspectStartTypeDefault`.
  - Worker/status repository operations use explicit `IgnoreQueryFilters()` plus explicit tenant/id predicates where background processing must see all due dispatch rows outside an HTTP tenant scope.
- Targeted E2E verification passed:
  - Command: `timeout 12m dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations" --minimum-expected-tests 1`
  - Result: 1 total, 1 succeeded, 0 failed, 0 skipped, duration 1m 33s 263ms.
- Final diagnostics and regression lanes after the E2E fixes:
  - LSP diagnostics were clean for the E2E fixtures/flows, seed files, Islamic aspect configuration, email dispatch repository, and migration.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 177 total, 176 succeeded, 1 skipped, 0 failed.
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed: 109 total, 109 succeeded, 0 failed.
  - `dotnet build --configuration Release --verbosity quiet` passed: 25 projects, 0 errors, 14 baseline warnings.
  - Focused warning scan of `/tmp/emaildispatch-final-build.log` found no warnings matching `EmailDispatch`, `email_dispatch`, `Mailpit`, `RegistrationFlowLoginBrowseRegisterConfirmation`, `BffKeycloak`, `DatabaseSeeder`, `SeedData`, or `EventSessionIslamicAspect`.

### 12.14 MQContract/provider abstraction capability gate

- Recorded the Phase 0 provider abstraction decision as **keep MQContract as the self-hoster provider-choice abstraction candidate, but capability-gate it for EmailDispatch reliability**.
- Internal repository inspection covered the current `dev/active/rabbitmq-messaging` workstream and messaging implementation files, including `IMessagingProvider`, `RabbitMqMessagingProvider`, `RuntimeMessagingProvider`, `MqContractOutboxMessageDispatcher`, `MessagingConfigResolver`, `MessagingConfiguration`, messaging governance keys, DI wiring, current health checks, and the Basic EmailDispatch outbox/receipt repository.
- Current MQContract wiring proves optional transport selection and a RabbitMQ publish/subscribe prototype, but it does **not** yet prove the provider-specific reliability semantics required by ADR-008 for EmailDispatch transport modes.
- The EF Core analogy is valid only when treated as a capability-aware provider model: EF Core gives a common abstraction while providers still expose provider-specific migration, SQL, concurrency, JSON/index, and transaction behavior. Messaging needs the same shape: a shared transport shell plus provider-specific capability surfaces.
- RabbitMQ capability requirements confirmed by official docs and Context7: publisher confirms, mandatory publishing/returned messages, manual ack/reject/nack, bounded prefetch/QoS, consumer tags/client-provided names, topology/DLQ checks, health/readiness, host-driven shutdown, and idempotent consumer storage.
- Kafka capability requirements confirmed by official Confluent/Kafka docs and Context7: delivery reports, idempotent producer/transactions where needed, consumer groups, partition assignment/rebalancing, manual offset store/commit behavior, graceful `Close()`, and committed offsets as the provider-native consume cursor.
- `RuntimeMessagingProvider` currently catches publish/subscribe failures and falls back to `NullMessagingProvider`, which can remain acceptable for optional best-effort transport plumbing but cannot be the failure boundary for an outbox dispatcher that must propagate failures so PostgreSQL state can retry.
- Decision: do not remove MQContract. Keep it for self-hoster provider choice and generic messaging. Future EmailDispatch broker transports must sit behind an Event-specific dispatch transport port or adapter that proves the provider capability matrix. If MQContract can expose those capabilities for a provider, use it inside the adapter; if it cannot, wrap/extend it or bypass it for that provider only. Basic Dispatch Mode remains API + PostgreSQL + SMTP and must continue to work without any broker.

### 12.15 Tenant pause/resume admin controls for Basic Dispatch Mode

- Implemented the first Basic Dispatch Mode admin write controls for durable tenant pause/resume.
- Added `SetEmailDispatchTenantPauseStateCommand`, `SetEmailDispatchTenantPauseStateCommandValidator`, and `SetEmailDispatchTenantPauseStateCommandHandler` in Application.
- The handler manually instantiates its validator, returns `BaseCommandResponse<Guid>`, and mutates only the durable `EmailDispatchTenantControl` row through `IEmailDispatchOutboxRepository`.
- Extended `IEmailDispatchOutboxRepository` and `EmailDispatchOutboxRepository` with `SetTenantPauseState(...)`.
- The repository upserts one `email_dispatch_tenant_controls` row per tenant, truncates pause reason to the configured 500-character bound, records `PausedAt`/`PausedBy` only while paused, and clears pause metadata on resume.
- Extended `EmailDispatchAdminController` with authenticated write routes:
  - `PUT api/admin/email-dispatch/tenants/{tenantId}/pause` named `RouteNames.PauseEmailDispatchTenant`.
  - `DELETE api/admin/email-dispatch/tenants/{tenantId}/pause` named `RouteNames.ResumeEmailDispatchTenant`.
- The controller remains thin: it dispatches MediatR commands, uses `RouteNames`, applies authenticated write rate limiting and request timeout policies, and returns safe `BaseCommandResponse<Guid>` payloads without email recipient/body/subject/provider details.
- A read-only verifier found one metadata issue: the controller originally declared bare `400` response metadata. Fixed all EmailDispatch admin actions to declare `ProblemDetails` for `400 Bad Request`.
- Added focused tests:
  - `SetEmailDispatchTenantPauseStateCommandHandlerTests` verifies validation failures do not call persistence, pause writes durable paused state, and resume clears durable paused state.
  - `EmailDispatchTenantControlRepositoryTests` verifies PostgreSQL-backed upsert behavior, one durable control row per tenant, `IsTenantPaused` transitions, and pause metadata clearing.
- Verification after this slice:
  - LSP diagnostics were clean for the new command/validator/handler, repository contract, repository implementation, controller, and tests.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed: 1032 total, 1032 succeeded, 0 failed.
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed: 110 total, 110 succeeded, 0 failed.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed before and after the `ProblemDetails` metadata fix; final run passed: 177 total, 176 succeeded, 1 skipped, 0 failed.
  - `dotnet build --configuration Release --verbosity quiet` passed after the metadata fix: 25 projects, 0 errors, 3701 baseline warnings.
   - Focused warning scan of `/tmp/emaildispatch-admin-controls-final-build.log` found no warnings matching `EmailDispatch`, `email_dispatch`, `SetEmailDispatchTenantPause`, `PauseEmailDispatch`, `ResumeEmailDispatch`, `EmailDispatchTenantControl`, or `ProblemDetails`.

### 12.16 RabbitMQ publisher/topology/health foundation

- Implemented the first Phase 4 RabbitMQ Dispatch Mode slice as an optional transport foundation, not as a replacement for Basic Dispatch Mode.
- Added Application-owned transport contracts under `Explore.Application/Contracts/Infrastructure/`:
  - `EmailDispatchPointer` is the pointer-only broker payload and maps from `EmailDispatchOutbox` without recipient, subject, plain-text body, HTML body, reply-to, provider message IDs, raw provider errors, or SMTP credentials.
  - `IEmailDispatchTransport`, `EmailDispatchPublishResult`, `EmailDispatchPublishOutcome`, and `EmailDispatchTransportHealth` define the broker boundary without referencing RabbitMQ or MQContract from handlers/controllers/domain.
- Added Infrastructure-owned RabbitMQ implementation:
  - `EmailDispatchRabbitMqSettings` and validator bind `EmailDispatchRabbitMq:*` with disabled-by-default optional semantics.
  - `RabbitMqEmailDispatchTransport` uses direct `RabbitMQ.Client` 7.2.1, resolves direct/connection-string/Aspire `MESSAGING_URI` configuration, declares durable direct exchange, dispatch queue, DLX, DLQ, and parking queue, and publishes pointer messages with `mandatory:true`, publisher confirmations/tracking, return/nack/timeout handling, redacted logs, and low-cardinality metrics.
  - `EmailDispatchRabbitMqHealthCheck` reports healthy when disabled and unhealthy when enabled broker/topology declaration fails.
- Added API/AppHost/observability wiring:
  - `Explore.API/Program.cs` registers the `email-dispatch-rabbitmq` readiness check.
  - `Explore.AppHost/AppHost.cs` adds optional local RabbitMQ resource `messaging` with management plugin and wires it to `explore-api` through Aspire connection injection.
  - `BusinessMetrics` now records `explore.email_dispatch.rabbitmq.publishes` with `tenant_id`, `outcome`, and `failure_category` only.
- Updated operator docs in `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and `docs/SELF_HOSTING.md` to distinguish Basic mode from optional RabbitMQ transport and to document safe payload/configuration behavior.
- Verification evidence for this slice:
  - LSP diagnostics clean for new Application contracts, RabbitMQ settings/validator/transport/health check, and new Infrastructure tests.
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed: 295 total, 295 succeeded, 0 failed.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed: 177 total, 176 succeeded, 1 skipped.
  - Full `Event.API.IntegrationTests` run found 3 failures in `AuthorizationProductionGuardrailTests`; triage showed startup failure in existing Development/InMemory seeding because EF InMemory does not support `ExecuteDeleteAsync`, unrelated to the RabbitMQ slice.
- Still pending for later RabbitMQ Dispatch Mode completion: manual-ack consumer, bounded prefetch, persisted outcome-before-ACK, DLQ replay/parking writes, RabbitMQ Testcontainers confirm/return tests, and explicit Basic/Rabbit mode isolation integration proof.

### 12.17 Replay/park durable transition foundation

- Implemented the first Phase 6 replay/park slice as a durable state-transition foundation only; no API controller routes, HAL links, or UI affordances were added in this slice.
- Added Application command surface:
  - `ParkEmailDispatchCommand` and `ReplayEmailDispatchCommand` under `Explore.Application/Features/EmailDispatch/Requests/Commands/`.
  - `ParkEmailDispatchCommandValidator` and `ReplayEmailDispatchCommandValidator` manually enforce tenant/outbox identifiers and the operator park reason bound.
  - `ParkEmailDispatchCommandHandler` and `ReplayEmailDispatchCommandHandler` manually instantiate validators, return `BaseCommandResponse<Guid>`, use structured `EmailDispatchFailureCodes`, and mutate only the durable repository boundary.
- Added repository transition surface:
  - `IEmailDispatchOutboxRepository.GetByTenantAndId(...)` reads the exact tenant/outbox row for state gating without exposing recipient/body/subject data to API callers.
  - `TryParkForOperator(...)` moves eligible rows to `EmailDispatchStatus.Parked`, records `ParkedAt`, clears retry/processing fields, stores a truncated safe operator reason in failure metadata, and refuses already sent or already parked rows.
  - `TryReplayForOperator(...)` resets deferred rows (`DeadLettered`, `Parked`, `Unknown`, `RetryScheduled`) back to `Pending`, clears dead-letter/park/unknown/failure metadata, and refuses sent/processing rows.
- The handlers deliberately do not send SMTP, publish RabbitMQ, inspect HAL, or own controller-level ProblemDetails. This preserves Clean Architecture: Application owns orchestration and state validation, Persistence owns EF conditional updates, API/HAL will be a later affordance slice.
- Added focused tests:
  - `ParkEmailDispatchCommandHandlerTests` covers validation, not-found failure code, invalid sent transition, and successful repository-mediated park.
  - `ReplayEmailDispatchCommandHandlerTests` covers validation, invalid sent transition, idempotent pending replay success, and successful repository-mediated replay from dead-lettered state.
  - `EmailDispatchOutboxTransitionRepositoryTests` covers PostgreSQL-backed park/replay conditional transitions, but local execution currently requires Docker/Testcontainers availability.
- Verification evidence for this slice so far:
  - LSP diagnostics clean for the new Application commands/validators/handlers, repository contract/implementation, and new tests.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed: 1047 total, 1047 succeeded, 0 failed.
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` could not execute the repository assertions in this environment because Testcontainers could not connect to Docker (`unix:///var/run/docker.sock` or Docker Desktop socket). The failure was suite-wide and not specific to the new transition tests.
- Still pending for the full admin operations feature: authenticated controller actions, route names, API ProblemDetails shape for invalid transitions, HAL link policies/authorization metadata, API integration tests, UI affordance gating, and operator docs for replay/park actions.

### 12.18 Replay/park authenticated admin API endpoints

- Exposed the durable replay/park transition foundation through authenticated admin write endpoints in `EmailDispatchAdminController`.
- Added stable route names in `Explore.API/Hateoas/RouteNames.cs`:
  - `RouteNames.ParkEmailDispatch` for `PUT api/admin/email-dispatch/tenants/{tenantId}/outbox/{outboxId}/park`.
  - `RouteNames.ReplayEmailDispatch` for `POST api/admin/email-dispatch/tenants/{tenantId}/outbox/{outboxId}/replay`.
- Both endpoints preserve the controller pattern used by status and tenant pause/resume:
  - `[Authorize]` inherited from the controller.
  - authenticated endpoint classification.
  - write rate limiting through `RateLimitingExtensions.WritePolicy`.
  - complex request timeout policy.
  - MediatR dispatch only; no SMTP, RabbitMQ, EF DbContext, or HAL evaluation in the controller.
  - `ChangedBy = CurrentUserId` on the replay/park commands for audit attribution.
- Added controller-owned RFC 7807 mapping for command failure codes:
  - `email_dispatch_not_found` returns `404 application/problem+json` with title `Email dispatch row not found`.
  - `email_dispatch_invalid_transition` and `email_dispatch_concurrent_transition` return `409 application/problem+json` with title `Email dispatch state transition conflict`.
  - fallback validation failures return `400 ValidationProblemDetails`.
  - all problem responses include safe `traceId`, `timestamp`, optional `correlationId`, and `code` metadata when available.
  - The controller returns these payloads through an explicit `ContentResult` with `ContentType = application/problem+json` because the controller-level HAL JSON `Produces` metadata otherwise causes MVC to serialize `ObjectResult` problem payloads as `application/json`.
- Added focused API integration tests in `Event.API.IntegrationTests/Features/EmailDispatchAdminControllerTests.cs` for unauthenticated park denial, authenticated park command dispatch, replay conflict ProblemDetails shape, and route-name/write-rate-limit metadata.
- HAL affordances remain intentionally deferred. The repository search found that EmailDispatch status currently returns a flat DTO list without a resource assembler/link policy. Future HAL work should first introduce row-level EmailDispatch admin resources and then emit `replay` links only for `DeadLettered`, `Parked`, `Unknown`, and `RetryScheduled`, and `park` links only for non-`Sent`, non-`Parked` rows.
- Verification note: LSP diagnostics are clean for the controller, route names, and new API test file. Focused TUnit/Microsoft.Testing.Platform filters currently match zero API integration tests in this project, so full API integration execution is used to prove runtime behavior; known unrelated guardrail failures from Development/InMemory seeding may still appear. `Event.Architecture.Tests` passed after this API slice: 179 total, 178 succeeded, 1 skipped.

### 12.19 EmailDispatch status HAL affordances

- Implemented the backend HAL affordance slice that section 12.18 intentionally deferred.
- `EmailDispatchAdminController.GetStatus` now returns `HalCollectionResource<EmailDispatchStatusDto>` instead of a raw `BaseCommandResponse<IReadOnlyList<EmailDispatchStatusDto>>` on success.
- The status endpoint still dispatches only `GetEmailDispatchStatusQuery` through MediatR; it does not inspect roles, send SMTP, publish RabbitMQ, or access EF directly.
- Status query failures now return explicit `400 application/problem+json` validation details with `traceId`, `timestamp`, optional `correlationId`, and safe `emailDispatch` errors.
- Added `EmailDispatchStatusLinkPolicy`:
  - `_links.replay` is emitted only for durable statuses `DeadLettered`, `Parked`, `Unknown`, and `RetryScheduled`.
  - `_links.park` is emitted only for rows that are not `Sent` and not already `Parked`.
  - Links use stable route names `RouteNames.ReplayEmailDispatch` and `RouteNames.ParkEmailDispatch`, route values `{ tenantId, outboxId }`, methods `POST` and `PUT`, and `RequiresAuth = true`.
  - Permission metadata is attached through `RequirePermission(AuthorizationActions.EmailDispatches.Update, ResourceDescriptors.EmailDispatchStatus, dto)`, so link materialization can fail closed through the existing HATEOAS authorization evaluator.
- Added `EmailDispatchStatusResourceAssembler` and registered the detail policy, collection policy, and resource assembler in `HateoasAssemblerRegistration`.
- Added EmailDispatch HAL serialization/OpenAPI coverage in `ExploreJsonContext` and `HalOpenApiSchemaCatalog` for `HalResource<EmailDispatchStatusDto>`, `HalCollectionResource<EmailDispatchStatusDto>`, and embedded collection items.
- Added direct HAL policy tests in `EmailDispatchAdminHateoasTests` for deferred, sent, parked, and processing rows, plus controller route metadata coverage for the HAL status return type.
- Verification evidence:
  - LSP diagnostics clean for the controller, HAL policy, assembler, DI registration, JSON context, OpenAPI schema catalog, and new/updated tests.
  - Full API integration run after the HAL fix: 1093 total, 66 failed, 1022 succeeded, 5 skipped. The previous EmailDispatch-specific `GetStatusRoute_ReturnsHalCollectionResource` failure is gone; remaining failures are unrelated baseline startup/migration/API cases such as `PendingModelChangesWarning`.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed: 179 total, 178 succeeded, 1 skipped.
  - `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet && dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passed: 7 projects, 0 errors, 12 existing warnings.
  - Repo-wide `dotnet build --configuration Release --verbosity quiet` is currently blocked by unrelated existing Blazor/test/package analyzer errors and warnings-as-errors; no EmailDispatch/HAL errors were present in the build output.
- Permission-bound metadata now has a canonical authorization catalog and policy surface:
  - `ResourceKinds.EmailDispatch = "islamuevent_email_dispatch"`.
  - `AuthorizationActions.EmailDispatches.View` and `AuthorizationActions.EmailDispatches.Update` reuse the shared `view`/`update` verbs.
  - `ResourceDescriptors.EmailDispatchStatus` extracts safe attributes only: `tenantId`, `outboxId`, `sourceType`, `sourceId`, and `deliveryStatus`; it deliberately excludes recipient, subject, body, provider message, and raw error data.
  - `ResourceDescriptorRegistry` maps `EmailDispatchStatusDto` to `ResourceKinds.EmailDispatch` for generic authorization parity.
  - `MachineScopeMapping` permits EmailDispatch operations only for `admin:tenant` or `admin:instance` machine scopes, matching the tenant-scoped operator nature of replay/park.
  - `FallbackAuthorizationService` handles `islamuevent_email_dispatch` through tenant-scoped access evaluation, preserving local/fallback fail-closed behavior.
  - Added Cerbos resource policy and schema: `cerbos/policies/islamuevent_email_dispatch.yaml` and `cerbos/policies/_schemas/islamuevent_email_dispatch.json`; instance admins can do everything, tenant admins can `view`/`update` within their tenant.
- Verification evidence for permission metadata slice:
  - LSP diagnostics clean for Application authorization catalogs, fallback authorization service, EmailDispatch HAL policy, updated HAL tests, and machine-scope tests.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed: 1062 total, 1062 succeeded.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed after formatting the `RequirePermission(AuthorizationActions...)` calls to satisfy the architecture regex: 179 total, 178 succeeded, 1 skipped.
  - `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet && dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passed: 8 projects, 0 errors, 14 existing warnings.
- Remaining for full Phase 6 completion: Blazor UI affordance rendering/tests that consume `_links.replay` and `_links.park`, and misconfiguration-specific ProblemDetails.

### 12.20 Basic/RabbitMQ dispatch-mode isolation proof

Completed the no-broker proof slice for Phase 4 Basic Dispatch Mode independence.

- Added focused coverage in `Explore.Infrastructure.Tests/Infrastructure/EmailDispatchRabbitMqHealthCheckTests.cs` using the real `RabbitMqEmailDispatchTransport` instead of a stub transport.
- The new health test configures `EmailDispatchRabbitMqSettings.Enabled = false` with deliberately invalid RabbitMQ connection data and verifies `CheckHealthAsync` returns a healthy disabled state with the description `Basic Dispatch Mode remains independent`.
- The new publish test uses the same disabled/invalid broker configuration and verifies `PublishDispatchPointerAsync` returns `EmailDispatchPublishOutcome.Disabled` without attempting to resolve or open the invalid AMQP connection. `EmailDispatchPublishResult.Disabled()` is intentionally considered successful because a disabled optional broker is a successful no-op for Basic Dispatch Mode.
- This proves the optional RabbitMQ adapter short-circuits before broker access when disabled; Basic `EmailDispatchProcessor` continues to remain separate and does not depend on `IEmailDispatchTransport`.
- Verification evidence:
  - LSP diagnostics clean for `EmailDispatchRabbitMqHealthCheckTests.cs`.
  - `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed: 297 total, 297 succeeded.
- Remaining Phase 4 work: implement the RabbitMQ manual-ack consumer, bounded prefetch, persisted outcome-before-ACK, DLQ replay/parking operations, and RabbitMQ-backed integration tests when Docker/Testcontainers are available.
