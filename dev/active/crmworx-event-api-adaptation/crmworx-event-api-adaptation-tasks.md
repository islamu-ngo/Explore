<!-- ABOUTME: Task checklist for adapting CRMWorx durable side-effect execution patterns into ISLAMU Event. -->
<!-- ABOUTME: Tracks phase gates, acceptance evidence, validation commands, and documentation maintenance requirements. -->

# CRMWorx Event API Adaptation — Tasks

Last Updated: 2026-05-25 Europe/Brussels

## Task Status Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `[!]` Blocked or needs decision

## Phase 0 — ADRs, Dispatch Modes, Failure Matrix, Architecture Guard

- [x] Write ADR: PostgreSQL-owned `EmailDispatchOutbox` state machine.
  - Evidence: ADR or linked decision note states RabbitMQ is optional transport, not workflow truth.
  - Acceptance: implementation cannot begin until this is recorded.
  - Validation: added `docs/adr/ADR-008-email-dispatch-state-machine.md` and linked it from `docs/index.md`; ADR records Basic Dispatch Mode first, RabbitMQ as optional transport, durable-intent-only side-effect boundary, and MQContract/RabbitMQ reliability gate; architecture/docs gate passed with 175 total, 174 succeeded, 1 skipped, 0 failed.
- [x] Decide dispatch profiles.
  - Required profiles: dispatch disabled, Basic Dispatch Mode, RabbitMQ Dispatch Mode.
  - Acceptance: Basic mode does not require RabbitMQ; RabbitMQ misconfiguration only blocks RabbitMQ mode.
  - Validation: `ADR-008` records dispatch disabled, Basic Dispatch Mode, and future RabbitMQ Dispatch Mode; Basic mode is API + PostgreSQL + SMTP and does not require RabbitMQ.
- [x] Confirm first vertical slice.
  - Required slice: registration confirmation email.
  - Acceptance: exact domain trigger, handler, outbox row, SMTP/template payload, and E2E assertion are named.
  - Validation: `ADR-008`, this plan, and completed implementation evidence record registration confirmation as the first vertical slice; `CreateEventRegistrationCommandHandler` writes `EmailDispatchOutbox` durable intent in the registration transaction.
- [x] Choose specialized entity names.
  - Required direction: `EmailDispatchOutbox`, `EmailDispatchReceipt`, `EmailDispatchAttempt`, `EmailDispatchTenantControl` or equivalent.
  - Acceptance: names and lifecycle enums documented in context.
- [x] Decide whether MQContract remains the RabbitMQ abstraction.
  - Evidence: confirms, mandatory routing, returned/unroutable handling, manual ack/reject/nack, bounded prefetch, consumer identity, topology declaration, health checks, metrics, and graceful shutdown verified in code/tests or replacement decision recorded.
  - Acceptance: RabbitMQ Dispatch Mode cannot start until broker reliability semantics are proven.
  - Validation: recorded decision is **keep MQContract as the provider-choice abstraction candidate, but capability-gate it for EmailDispatch reliability**. MQContract remains valuable for self-hoster transport choice, but future EmailDispatch RabbitMQ/Kafka/etc. adapters must prove provider-specific semantics before activation. Required capability gates include publisher confirms or delivery reports, mandatory routing/returns or equivalent failed-delivery feedback, manual ack/reject/nack or explicit offset/commit control, bounded prefetch/backpressure, topology/DLQ/parking or provider-native dead-letter behavior, health, metrics, graceful shutdown, and broker-consume integration with PostgreSQL `EmailDispatchReceipt`. Basic Dispatch Mode remains complete and independent.
- [x] Write async workflow failure matrix.
  - Must cover: Basic worker crash before/after SMTP, duplicate claim, tenant pause, missing SMTP config, transient SMTP failure, permanent SMTP failure, SMTP timeout/unknown, retry exhaustion, dead-letter, parking, replay, Rabbit publisher crash before/after broker ack, unroutable publish, broker nack, duplicate consume, tenant mismatch, missing row, consumer crash before/after SMTP, already-sent replay.
  - Acceptance: matrix checked into this workstream or linked from it.
  - Validation: added `crmworx-event-api-adaptation-failure-matrix.md` covering Basic Dispatch Mode and future RabbitMQ Dispatch Mode crash windows, expected PostgreSQL state, operator signal, and required validation lanes; LSP diagnostics clean; architecture/docs gate passed with 175 total, 174 succeeded, 1 skipped, 0 failed.
- [x] Add or plan architecture guard for direct side effects.
  - Acceptance: no handler/controller/automation/sequence code sends SMTP or publishes RabbitMQ directly; only approved Infrastructure dispatch components may do that.
  - Validation: added `DurableSideEffectBoundaryTests` to `Event.Architecture.Tests`; guard scans all Application handler files under `/Handlers/`, blocks direct SMTP send calls through email transport references including `System.Net.Mail`, `SmtpClient`, `MailKit`, and `MimeKit`, and blocks direct RabbitMQ/MQContract broker operations while avoiding non-broker `IPolicyPackageService.PublishAsync` false positives; focused guard tests passed; full architecture suite passed 175 total, 174 succeeded, 1 skipped; Release build passed 25 projects with 0 errors.

## Phase 1 — PostgreSQL-Owned Email Dispatch State

- [x] Add Domain entities/enums for `EmailDispatchOutbox`, receipts, attempts, and tenant/global controls.
  - Acceptance: no external dependencies in Domain; IDs follow `Guid` aggregate convention; states include `Unknown`, `DeadLettered`, and `Parked` where needed.
  - Validation: architecture tests.
- [x] Add Application repository contracts.
  - Acceptance: repositories return entities, not DTOs; atomic contention methods return `bool`; cancellation tokens present.
  - Validation: Application unit/architecture tests.
- [x] Add EF configurations and migration(s).
  - Acceptance: tenant filters active, due-work indexes present, receipt/event idempotency unique constraints present, attempt-number uniqueness present, valid Down methods.
  - Validation: `Event.Persistence.IntegrationTests` and migration checks.
- [x] Add repository implementations.
  - Acceptance: `AsNoTracking` for read-only paths; no unsafe tenant-filter bypass; specification/query composition follows repo patterns.
  - Validation: persistence tests for claim, retry, idempotency, attempts, receipt uniqueness, tenant isolation.
- [x] Add operator-safe status projection/query.
  - Fields: `OutboxId`, `TenantId`, `SourceType`, `SourceId`, `DeliveryStatus`, `AttemptCount`, `NextRetryAt`, `LastFailureCategory`, `LastFailureAt`, `UnknownAt`, `PublishedAt`, `DeliveredAt`, `ParkedAt`, `CorrelationId`.
  - Acceptance: no email body, HTML, recipient content beyond what policy permits, or secrets exposed.
  - Validation: added `EmailDispatchStatusDto`, `GetEmailDispatchStatusQuery`, `GetEmailDispatchStatusQueryHandler`, repository `GetStatusRows`, and authenticated `GET /api/admin/email-dispatch/status`; focused handler tests verify tenant/limit validation and that recipient, subject, body, reply-to, provider message ID, and raw error are not exposed; LSP diagnostics clean.

## Phase 2 — Basic Dispatch Mode And Registration Confirmation

- [x] Wire registration confirmation to durable intent.
  - Acceptance: registration flow creates `EmailDispatchOutbox` row in the same durable transaction as registration state; no SMTP or RabbitMQ call in handler.
  - Validation: Application unit test and persistence integration test.
- [x] Add Basic dispatcher worker.
  - Acceptance: `BackgroundService`/`PeriodicTimer`, cancellation-aware, bounded batch size, atomic claim, global pause, tenant pause, retry scheduling, graceful shutdown.
  - Validation: unit/component tests.
- [x] Integrate existing SMTP abstraction.
  - Acceptance: worker delegates through approved email abstraction; success, transient failure, permanent failure, timeout/`Unknown`, retry exhaustion, and dead-letter are persisted.
  - Validation: Mailpit/container or SMTP abstraction tests.
- [x] Add Basic mode configuration validation and health checks.
  - Acceptance: dispatch disabled is explicitly reported; Basic mode with missing SMTP is fail-fast or unhealthy/paused per final decision; no silent no-op.
  - Validation: `EmailDispatchProcessorSettingsValidator`, `EmailDispatchHealthCheck`, LSP diagnostics, architecture tests, Release build, `Explore.Infrastructure.Tests` 288/288, and two focused `Event.API.IntegrationTests` health-check method runs.
- [x] Add first E2E path.
  - Acceptance: registration confirmation flows through PostgreSQL + Basic dispatcher + SMTP/Mailpit and records receipt/attempt/final state.
  - Validation: added Mailpit Testcontainers fixture, SMTP governance-setting wiring in `AppHostFixture`, Keycloak bearer-token API flow, and `RegistrationFlowLoginBrowseRegisterConfirmationMyRegistrations` assertions for registration intent, child registration, `EmailDispatchOutbox` `Sent`, successful attempt, completed receipt, and Mailpit delivery; targeted E2E command passed with 1 total, 1 succeeded, 0 failed; LSP diagnostics clean; architecture tests passed 177 total, 176 succeeded, 1 skipped; persistence integration tests passed 109/109; Release build passed 25 projects with 0 errors and no EmailDispatch/Mailpit-specific warning matches.

## Phase 3 — Operations, Observability, And Self-Hosting Docs

- [x] Add business metrics and structured logs.
  - Acceptance: low-cardinality tags only; IDs/correlation included; no message body, recipient content, or secrets.
  - Validation: `BusinessMetrics` emits `explore.email_dispatch.attempts` with bounded `tenant_id`, `outcome`, and `failure_category` tags; `BusinessMetricsEmailDispatchTests` verify no body, recipient, subject, secret, provider-message, or raw error tags; worker warning logs use normalized failure categories instead of raw provider error fields; LSP diagnostics clean; Application unit tests passed 1,024/1,024; Architecture tests passed 172/173 with 1 skipped; Release build passed 25 projects with 0 errors.
- [x] Add health/readiness behavior for all dispatch modes.
  - Acceptance: Basic mode ignores RabbitMQ health; RabbitMQ mode is unhealthy on missing broker/queue/confirms/DLQ/parking/consumer.
  - Validation: Basic `email-dispatch` health check wired and verified through LSP diagnostics, architecture tests, Release build, and focused API health-check tests. RabbitMQ mode health remains pending until optional RabbitMQ Dispatch Mode starts.
- [x] Update self-hosting and operations docs.
  - Required docs during implementation: `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/ARCHITECTURE.md`, `docs/OUTBOX_PATTERN.md`.
  - Acceptance: Basic mode is documented as valid without RabbitMQ; RabbitMQ profile is optional.
  - Validation: updated all five canonical docs; LSP diagnostics clean for each; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 173 total, 172 succeeded, 1 skipped, 0 failed.
- [x] Add operator status read path if needed before admin writes.
  - Acceptance: operators can inspect status fields without body/secrets and without HAL action commitments.
  - Validation: authenticated read-only `EmailDispatchAdminController.GetStatus` exposes sanitized status DTOs only; Application unit tests passed 1,027/1,027, Infrastructure tests passed 288/288, focused API health checks passed, Architecture tests passed 172/173 with 1 skipped; Release build passed 25 projects with 0 errors and focused warning scan found no EmailDispatch-specific warnings.

## Phase 4 — Optional RabbitMQ Dispatch Mode

- [ ] Implement/wrap RabbitMQ publisher after MQContract gate.
  - Acceptance: pointer-only payload, stable `PublishEventId`, mandatory routing, correlated confirms, return detection, timeout, redacted logs, metrics.
  - Validation: RabbitMQ Testcontainers tests.
- [ ] Add RabbitMQ topology/health/Aspire wiring.
  - Acceptance: exchange, standard queue, DLQ, parking queue; priority queue deferred unless consumed/tested; Rabbit profile optional.
  - Validation: Aspire startup/profile or AppHost resource test; health check tests.
- [ ] Add RabbitMQ manual-ack consumer.
  - Acceptance: ACK only after persisted terminal/deferred outcome; reject poison to DLQ; nack transient unexpected failures according to retry policy; bounded prefetch; consumer identity recorded.
  - Validation: RabbitMQ integration tests.
- [ ] Add DLQ replay and parking.
  - Acceptance: replay validates DB row, tenant, event ID, and not already sent; unsafe messages park; DLQ replay without parking queue is unhealthy.
  - Validation: unit/RabbitMQ tests.
- [ ] Prove Basic mode remains independent.
  - Acceptance: RabbitMQ disabled/broken does not prevent Basic Dispatch Mode startup or dispatch.
  - Validation: mode-isolation integration test.

## Phase 5 — Event-Specific Lifecycle Automation

- [ ] Define Event automation use cases.
  - Candidate cases: registration approved/rejected, waitlist promotion, event reminder, cancellation follow-up, organizer notification.
  - Acceptance: no generic workflow engine or generic automation console scope creep.
- [ ] Model automation rule/revision and receipt deduplication only if configurable rules are needed.
  - Acceptance: rules pin to published revisions; event receipts enforce idempotency by tenant/event/dedup key.
  - Validation: Application and persistence tests.
- [ ] Model execution runtime only for delayed or multi-step workflows.
  - Acceptance: current step, next run, retry count, lock token, lock expiry, progression key, failure state; atomic claim method.
  - Validation: claim/concurrency/retry tests.
- [ ] Ensure automation side effects write dispatch outbox rows.
  - Acceptance: no RabbitMQ/SMTP direct send from automation handlers/executors.
  - Validation: unit tests assert persisted intent or approved port interactions.

## Phase 6 — Admin Operations And HAL Affordances

- [x] Add admin/status endpoints after state machine is stable.
  - Candidate endpoints: `GET /admin/email-dispatch`, `GET /admin/email-dispatch/{id}`.
  - Acceptance: controllers dispatch MediatR only; route names used; writes `[Authorize]`; ProblemDetails metadata; no body/secrets exposed.
  - Validation: operator-safe status read path was implemented earlier via authenticated `EmailDispatchAdminController.GetStatus`, `GetEmailDispatchStatusQuery`, and safe `EmailDispatchStatusDto`; Application unit tests verify recipient/body/subject/reply-to/provider-message/raw-error fields are not exposed.
- [x] Add tenant pause/resume write actions after Basic Dispatch transition rules are stable.
  - Acceptance: action availability comes only from server-owned routes/HAL links; UI must not infer from roles/claims/local state.
  - Validation: tenant pause/resume controls implemented as the first admin write action slice. `PUT api/admin/email-dispatch/tenants/{tenantId}/pause` and `DELETE api/admin/email-dispatch/tenants/{tenantId}/pause` use `RouteNames.PauseEmailDispatchTenant` and `RouteNames.ResumeEmailDispatchTenant`, authenticated write rate limiting, MediatR command dispatch, manual command validation, durable `EmailDispatchTenantControl` upsert, `ProblemDetails` metadata for `400 Bad Request`, and safe `BaseCommandResponse<Guid>` responses. Application Unit passed 1032/1032; Persistence Integration passed 110/110; final Architecture passed 176/177 with 1 skipped; final Release build passed 25 projects/0 errors; focused warning scan found no EmailDispatch admin-control warnings.
- [ ] Add replay/park write actions after replay and parking transition rules are implemented.
  - Acceptance: replay/park availability comes only from HAL links and validates current outbox state; unsafe messages remain parked/dead-lettered; UI must not infer from roles/claims/local state.
  - Validation: HAL affordance tests, authorization tests, and transition tests for replay/park.
- [ ] Add ProblemDetails for invalid transitions and misconfiguration.
  - Acceptance: safe errors with trace/correlation details, no secrets.

## Phase 7 — Custom Fields/Data Modeling Guardrails

- [ ] Define which custom-property values can drive automation conditions.
  - Acceptance: only governed definitions that are type-validated, automation-allowed, projection-backed if filtered/searched, and tenant-owned are eligible.
- [ ] Add projection support where needed.
  - Acceptance: searchable/filterable automation fields have explicit projection/index strategy.
  - Validation: persistence projection tests.
- [ ] Keep workflow-critical state explicit.
  - Acceptance: no dispatch status, automation execution status, registration lifecycle state, delivery attempt state, tenant pause/replay/parking, or idempotency keys stored as generic EAV.
  - Validation: architecture/code review checklist.

## Phase 8 — Final Hardening

- [x] Run required project-specific tests.
  - Minimum likely set: architecture tests, application unit tests for touched handlers, persistence integration tests, API integration tests, Basic SMTP/Mailpit tests, optional RabbitMQ integration tests, relevant Aspire/E2E tests.
- [x] Run build.
  - Command: `dotnet build --configuration Release --verbosity quiet`.
- [x] Update docs.
  - Required as implementation progresses: this workstream docs, `docs/OPERATIONS.md`, `docs/API.md`/`docs/API_CHANGELOG.md` if APIs changed, `docs/CONFIGURATION.md` for config keys, `docs/SELF_HOSTING.md` for optional profile, `schemas/islamu-event.md` for schema changes, relevant testing docs if lanes added.
  - Validation: workstream context/tasks record completed Basic Dispatch Mode implementation and E2E evidence; canonical configuration, operations, self-hosting, architecture, outbox, ADR, and failure-matrix docs were updated earlier in this workstream.
- [x] Log durable findings if non-obvious implementation behavior is discovered.
  - Validation: appended durable journal findings for cross-tenant background repositories requiring explicit `IgnoreQueryFilters()` plus tenant/id predicates, E2E seed relational invariants blocking feature proof, and Mailpit body assertions needing the detail API endpoint; `dev/_journal/journal.md` diagnostics clean.

## Implementation-Agent Checklist

Every implementation agent must do the following before claiming task completion:

- [ ] Load/read matched skills, docs, rules, and intent.
- [ ] Update this file's task statuses and evidence.
- [ ] Update `crmworx-event-api-adaptation-context.md` with decisions or discoveries.
- [ ] Preserve `Last Updated: YYYY-MM-DD Europe/Brussels`.
- [ ] Run `lsp_diagnostics` on modified files when supported.
- [ ] Run relevant tests and build commands; record exact commands/results.
- [ ] Never send SMTP or publish RabbitMQ directly from handlers/controllers/automation/sequence processors.
- [ ] Do not commit unless explicitly requested.
