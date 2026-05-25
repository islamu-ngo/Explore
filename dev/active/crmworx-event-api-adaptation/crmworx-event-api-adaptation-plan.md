<!-- ABOUTME: Implementation plan for adapting CRMWorx durable side-effect workflow lessons into ISLAMU Event. -->
<!-- ABOUTME: Centers PostgreSQL-owned EmailDispatchOutbox state, Basic Dispatch Mode first, and optional RabbitMQ transport. -->

# CRMWorx Event API Adaptation — Plan

Last Updated: 2026-05-24 Europe/Brussels

## 1. Executive Summary

This workstream is now scoped as **Durable Side-Effect Execution for Event Lifecycle Workflows**. The approved direction is to adapt CRMWorx's strongest reliability lesson without copying its Java/Spring architecture: PostgreSQL owns the workflow state, and transport infrastructure only moves already-durable intent.

The first implementation slice is intentionally small: **registration confirmation email**. A registration/lifecycle handler creates durable intent in PostgreSQL, the Basic Dispatch Mode worker sends through the existing SMTP abstraction, and attempts/receipts/final delivery state become queryable operational truth. RabbitMQ is not the conceptual center; it is an optional later transport profile that must prove publisher confirms, mandatory routing, returned/unroutable handling, manual acknowledgements, bounded prefetch, DLQ/parking, health, metrics, and graceful shutdown before it can be enabled.

## 2. Scope

### 2.1 In scope

- A specialized `EmailDispatchOutbox` state machine for email side effects.
- `EmailDispatchReceipt`, `EmailDispatchAttempt`, and `EmailDispatchTenantControl`/equivalent operational control state.
- **Basic Dispatch Mode**: API + PostgreSQL + SMTP provider, no RabbitMQ requirement.
- **RabbitMQ Dispatch Mode**: API + PostgreSQL + RabbitMQ + SMTP provider, sharing the same state machine and enabled only after reliability gates pass.
- Registration confirmation as the first vertical slice:

  ```text
  RegistrationConfirmed
    -> create in-app Notification if needed
    -> create EmailDispatchOutbox row
    -> dispatch pipeline
    -> receipt + attempts + final delivery state
  ```

- Self-hosting-friendly configuration, health, metrics, logs, and startup validation.
- Event-specific lifecycle automation later: approved/rejected registration, waitlist promotion, reminders, cancellation, organizer notifications.
- Custom-property guardrails for automation conditions, while keeping workflow-critical state out of EAV.
- Admin/HAL affordances later for status, replay, park, and tenant pause/resume.

### 2.2 Out of scope for the first implementation slice

- Making RabbitMQ mandatory.
- Generic CRM-style workflow engine or generic automation console.
- Priority queues, tenant circuit-breaker automation, advanced throttling, multiple provider fallback, and complex replay UI.
- Direct copying of CRMWorx Java classes, PipelinR middleware, or JPA repository boundaries.
- Replacing Event's existing tenant/auth/HAL architecture.
- Storing dispatch status, automation execution status, registration lifecycle state, delivery attempt state, pause/replay/parking state, or idempotency keys in EAV/custom properties.

## 3. Hard Architecture Rules

1. **PostgreSQL owns dispatch state.** `EmailDispatchOutbox` is the source of truth for pending, dispatching, retry, dead-letter, parked, replayed, unknown, and sent states.
2. **Handlers create durable intent only.** No handler, controller, automation executor, sequence processor, or domain service may send SMTP directly or publish RabbitMQ directly.
3. **Same state machine for every dispatch mode.** Basic Dispatch Mode and RabbitMQ Dispatch Mode share entities, transitions, retries, attempts, receipts, tenant controls, health, metrics, and logs.
4. **RabbitMQ is optional transport, not workflow.** RabbitMQ may carry pointer messages after durable intent exists; it must not become business truth.
5. **Event rules still dominate.** Domain remains dependency-free; Application owns CQRS/state orchestration; repositories return entities; validators are manually instantiated; tenant isolation remains explicit; HAL links remain the only UI action source of truth.
6. **No silent no-op production behavior.** Misconfiguration must be startup/health visible and test-visible.

## 4. Self-Hosting Dispatch Profiles

| Profile | Runtime shape | Intended audience | Required health behavior |
|---|---|---|---|
| Dispatch disabled | API + PostgreSQL, no dispatch worker | Installations intentionally not sending email | Health reports dispatch intentionally disabled, not silently healthy as if sending. |
| Basic Dispatch Mode | API + PostgreSQL + SMTP provider | Default self-hosting profile and first release path | Missing/invalid SMTP config is fail-fast or unhealthy/paused according to final configuration decision. RabbitMQ is irrelevant. |
| RabbitMQ Dispatch Mode | API + PostgreSQL + RabbitMQ + SMTP provider | Operators that want broker-backed transport and can run RabbitMQ | Missing broker, unbound queue, disabled confirms/returns, missing DLQ/parking, or missing consumer is unhealthy. Basic mode must still work when RabbitMQ mode is not selected. |

The local development stack must support a lightweight Basic profile and a production-rehearsal profile with PostgreSQL + API + RabbitMQ + Mailpit + worker + health + metrics through Aspire and/or Compose profiles.

## 5. Current-State Report

### 5.1 Strengths already in Event

- General outbox and specialized outbox precedent exist.
- MailKit SMTP abstraction, tenant/system config cascade, Polly resilience, unsubscribe token support, and SMTP health checks exist.
- Notification entity/API/HAL resource model exists.
- RabbitMQ provider abstraction exists but is unfinished and unverified.
- Custom-property governance and projections exist.
- Tenant isolation, auth behavior, HAL fail-closed evaluation, ProblemDetails, rate limiting, and OpenTelemetry foundations exist.
- Test structure is mature: unit, architecture, persistence integration, API integration, component, E2E, Playwright/Aspire lanes.

### 5.2 Material gaps

- Registration confirmation email is not wired even though UI copy promises one.
- No durable `EmailDispatchOutbox` state machine exists.
- No receipt/attempt ledger exists for email delivery outcomes.
- No `UNKNOWN`/ambiguous SMTP outcome state exists.
- RabbitMQ provider has no completed health/metrics/Aspire/test evidence and must not block Basic Dispatch Mode.
- No queue-first optional broker consumer with manual ack, idempotent receipt claim, DLQ replay, and parking exists.
- No failure matrix exists for async notification/email dispatch.

## 6. Future-State Architecture

### 6.1 Basic Dispatch Mode runtime flow

```text
Registration lifecycle handler
  -> changes aggregate state
  -> same EF transaction writes EmailDispatchOutbox row
  -> Basic dispatcher BackgroundService claims due row
  -> SMTP abstraction sends email
  -> dispatcher records EmailDispatchAttempt
  -> dispatcher records receipt/final delivery state
  -> health, metrics, logs, and later HAL/admin status expose operational truth
```

### 6.2 RabbitMQ Dispatch Mode runtime flow

```text
Registration lifecycle handler
  -> same durable EmailDispatchOutbox row
  -> publisher worker claims due row and creates/reuses PublishEventId
  -> RabbitMQ pointer-only message after confirms/no-return reliability gate
  -> consumer manual-acks only after Application consume service persists outcome
  -> consume service rebinds tenant, claims receipt, validates outbox/event identity
  -> SMTP abstraction sends email or records retry/permanent/unknown outcome
  -> attempts, receipts, metrics, logs, DLQ/parking, and later HAL admin actions expose operational truth
```

### 6.3 Clean Architecture ownership

| Layer | Ownership |
|---|---|
| Domain | `EmailDispatchOutbox`, `EmailDispatchReceipt`, `EmailDispatchAttempt`, `EmailDispatchTenantControl`/control aggregate, enums/value objects; no RabbitMQ, SMTP, EF, MediatR, or ASP.NET dependencies. |
| Application | Commands/queries/handlers, manual validators, repository contracts, state-machine services, Basic dispatch orchestration, Rabbit consume orchestration, ports for SMTP/transport adapters. |
| Persistence | EF configurations, migrations, repositories returning entities, atomic claim/update methods, tenant/soft-delete filters, indexes, retry/due-work queries. |
| Infrastructure | Existing SMTP adapter usage, optional RabbitMQ publisher/consumer adapters, provider-specific health checks, metrics adapters, Testcontainers helpers. |
| API | Controllers, HAL assemblers/link policies, ProblemDetails, background service hosting, health endpoints, DI composition. |
| AppHost/ServiceDefaults | Aspire PostgreSQL/Mailpit/RabbitMQ/OTEL wiring, local E2E orchestration, dashboards. |

## 7. Matched Intents And Rule Stack

Implementation PRs must classify against these intents before editing:

- `add-cqrs-handler`
- `add-write-endpoint`
- `add-ef-migration`
- `update-repository-query`
- `add-hal-link`
- `openapi-contract-change`

Mandatory rules/skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `outbox-pattern`, `auth-patterns`, `error-tracking`, `aspire`, `agentic-research`, `.claude/rules/api-controllers.md`, `.claude/rules/application-layer.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/efcore-migrations.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/tests.md`.

## 8. Phase Plan

### Phase 0 — ADRs, Hard Gates, And Failure Matrix

**Goal:** Freeze the state-machine contract before implementation.

**Tasks:**

1. Write ADR: `EmailDispatchOutbox` is the durable source of truth; RabbitMQ is optional transport.
2. Decide dispatch-mode configuration shape: disabled, Basic, RabbitMQ.
3. Confirm first vertical slice: registration confirmation email only.
4. Decide MQContract gate: keep only if it proves confirms, mandatory routing, returned/unroutable messages, manual ack/reject/nack, bounded prefetch, consumer identity, topology declaration, health checks, metrics, and graceful shutdown.
5. Define states/transitions for `Pending`, `Dispatching`/leased, `Sent`, `RetryScheduled`, `DeadLettered`, `Parked`, and `Unknown`.
6. Write failure matrix before schema/code work.
7. Add or plan an architecture guard proving no direct SMTP/RabbitMQ usage outside approved dispatch infrastructure.

**Acceptance:** implementation is blocked until ADR, first slice, mode decision, state lifecycle, MQContract decision, and failure matrix are recorded.

**Validation:** current build status recorded; architecture tests still pass or blockers are explicit.

### Phase 1 — PostgreSQL-Owned Email Dispatch State

**Goal:** Add durable dispatch state independent of RabbitMQ.

**Tasks:**

1. Add Domain entities/enums for `EmailDispatchOutbox`, `EmailDispatchReceipt`, `EmailDispatchAttempt`, and tenant/global control state.
2. Add Application repository contracts with entity-returning methods and atomic claim methods returning `bool` on contention.
3. Add EF configurations and migrations with tenant filters, due-work indexes, valid Down methods, unique receipt/event constraints, and unique attempt number per outbox.
4. Add repository implementations using `AsNoTracking` for reads and tenant-safe filters.

**Acceptance:** schema supports pending/leased/sent/retry/dead-letter/parked/unknown states, idempotency, receipt protection, attempt history, and operator-safe status fields without exposing body content.

**Validation:** `Event.Persistence.IntegrationTests`, architecture tests, migration rehearsal where applicable.

### Phase 2 — Basic Dispatch Mode And Registration Confirmation

**Goal:** Prove the first vertical slice with API + PostgreSQL + SMTP only.

**Tasks:**

1. Wire registration confirmation to create durable email intent in the same EF transaction as registration state change.
2. Add Basic dispatcher `BackgroundService` that claims due outbox rows, honors global/tenant pause, sends through existing SMTP abstraction, and records attempts/receipts/final state.
3. Classify SMTP outcomes: success, transient retry, permanent failure/dead-letter, timeout/ambiguous `Unknown`.
4. Add startup/options validation and health checks for Basic mode.
5. Add Mailpit/Testcontainers or existing SMTP abstraction integration tests for registration confirmation.

**Acceptance:** registration confirmation completes without RabbitMQ; no handler sends SMTP directly; duplicate/retry windows do not create uncontrolled duplicate emails.

**Validation:** Application unit tests, Persistence integration tests, SMTP/Mailpit tests, API/E2E registration confirmation test.

### Phase 3 — Operations, Observability, And Configuration Validation

**Goal:** Make Basic mode operator-safe before adding broker complexity.

**Tasks:**

1. Add business metrics for pending, claimed, sent, retry, dead-lettered, parked, unknown, oldest pending age, and attempt outcomes.
2. Add structured logs with `OutboxId`, `TenantId`, `SourceType`, `SourceId`, `DeliveryStatus`, `AttemptCount`, `NextRetryAt`, `LastFailureCategory`, `CorrelationId`, and no body/secret/PII content.
3. Add status query/read model for operators without exposing email body.
4. Add health behavior for disabled, Basic, and RabbitMQ modes.
5. Update configuration, operations, self-hosting, outbox, and architecture docs during implementation.

**Acceptance:** operators can answer what happened to a registration confirmation without reading RabbitMQ internals.

**Validation:** health check tests, metrics/logging assertions where practical, docs checks.

### Phase 4 — Optional RabbitMQ Dispatch Mode

**Goal:** Add broker transport without changing the PostgreSQL state machine.

**Tasks:**

1. Implement or wrap RabbitMQ publisher under an Event-specific transport port after MQContract gate passes.
2. Publish pointer-only payloads with stable `PublishEventId`, mandatory routing, confirms, returned-message detection, timeout handling, metrics, and redacted logs.
3. Add RabbitMQ consumer using manual ack/reject/nack, bounded prefetch, consumer identity, tenant rebinding, receipt idempotency, and persisted outcome-before-ack.
4. Add durable exchange/queue/DLQ/parking topology and health checks. Do not add priority queues unless consumed and tested.
5. Add guarded replay that validates database truth before requeue.

**Acceptance:** RabbitMQ outage or MQContract mismatch does not block Basic Dispatch Mode; no message is marked transport-successful before confirm/no-return; duplicate deliveries ACK without re-sending.

**Validation:** RabbitMQ Testcontainers tests, failure-matrix tests, health tests, Aspire profile smoke/E2E.

### Phase 5 — Event-Specific Lifecycle Automation

**Goal:** Add explicit Event lifecycle automation only after dispatch is durable.

**Tasks:**

1. Model fixed Event lifecycle triggers: registration approved/rejected, waitlist promotion, reminders, cancellation, organizer notifications.
2. Add rule/revision/receipt/execution state only where configurability or delayed/multi-step workflows require it.
3. Ensure automation writes durable `EmailDispatchOutbox` rows and never sends SMTP or RabbitMQ directly.
4. Permit custom-property conditions only when governed, type-validated, automation-allowed, projection-backed if filtered/searched, and tenant-owned.

**Acceptance:** no generic workflow engine scope creep; PostgreSQL owns workflow decisions; RabbitMQ only transports side-effect intents.

**Validation:** Application unit tests, Persistence claim/retry tests, E2E chain from lifecycle trigger to dispatch outcome.

### Phase 6 — Admin Operations And HAL Affordances

**Goal:** Expose management only after state and first slice are proven.

**Tasks:**

1. Add admin status endpoints later: `GET /admin/email-dispatch`, `GET /admin/email-dispatch/{id}`.
2. Add replay, park, global pause/resume, and tenant pause/resume write actions when transition rules are stable.
3. Gate every action through authorization and HAL links; UI must not infer actions from roles/claims/local state.
4. Add ProblemDetails for invalid transitions and misconfiguration.

**Acceptance:** HAL is the sole UI action source; admin endpoints do not expose message body/secrets.

**Validation:** API integration tests, HAL affordance tests, auth tests, architecture tests.

### Phase 7 — Custom Fields Guardrails, Documentation, And Final Hardening

**Goal:** Preserve flexible metadata while keeping core workflow state explicit.

**Tasks:**

1. Document that custom properties cannot store dispatch status, automation execution status, registration lifecycle state, delivery attempt state, tenant pause/replay/parking state, or idempotency keys.
2. Add projection/index strategy for custom fields that can drive automation conditions.
3. Run required project-specific tests and build.
4. Update dev docs and public docs changed by implementation.
5. Log durable findings for non-obvious behavior.

**Acceptance:** workflow-critical concepts are explicit entities/aspects; custom fields remain governed optional metadata.

**Validation:** custom-property tests, architecture tests, API contract tests, full required verification set.

## 9. Security And Multi-Tenancy Plan

- Internal workers must explicitly bind `TenantContext` before repository operations.
- Email body and rendered content stay in PostgreSQL/provider payload handling; RabbitMQ pointer payloads contain no message body, HTML, secrets, or unnecessary PII.
- Every dispatch/consume/replay path revalidates tenant/outbox/event identity from the database.
- Admin replay/pause/resume endpoints are write endpoints and require `[Authorize]`, resource descriptors, and HAL link policies.
- Authorization/provider outage behavior must fail closed; operational ProblemDetails should distinguish auth outage from permission deny.
- Logs and metrics must use IDs and bounded labels; no email body, recipient content, or secrets.

## 10. Observability Plan

Recommended meter/log dimensions:

- `stage`: intent_created, claimed, sent, retry_scheduled, dead_lettered, parked, unknown, replayed, rabbit_published, rabbit_consumed.
- `outcome`: succeeded, transient_failure, permanent_failure, unknown, duplicate, poison, tenant_mismatch, paused, config_invalid, unroutable, broker_unavailable.
- `source_type`: registration_confirmation, waitlist, reminder, cancellation, organizer_notification, manual.
- `mode`: disabled, basic, rabbitmq.

Avoid tags containing tenant names, emails, subjects, template names, or high-cardinality exception messages.

## 11. Migration Strategy

- Because the project is pre-release, prefer clean corrective migrations over compatibility shims.
- Do not edit merged/applied migrations; add new focused PascalCase migrations.
- Add valid Down methods and snapshot updates.
- Use `Guid`/UUIDv7 for aggregate IDs.
- Preserve tenant and soft-delete filters.
- Add unique constraints for idempotency and receipt identity.
- Add due-work indexes for dispatch/retry queries.
- Keep seed/enum lookup data synchronized across enum, configuration, migration, and SQL.

## 12. Testing Strategy

| Lane | Required evidence |
|---|---|
| Domain/Application unit | State transitions, validators, idempotency, failure classification, retry math, first registration-confirmation intent creation. |
| Architecture | Layer boundaries, repository-return entity rule, no direct SMTP/RabbitMQ outside approved Infrastructure dispatch components, API controller conventions, HAL policy location. |
| Persistence integration | EF mappings, migrations, tenant filters, claim concurrency, unique constraints, attempts/receipts. |
| Basic SMTP/Mailpit | Registration confirmation success, transient failure, permanent failure, timeout/unknown, duplicate prevention. |
| RabbitMQ container | Optional mode only: publisher confirms/returns, manual ack/reject/nack, DLQ, parking, bounded prefetch. |
| API integration | Later admin status/replay/pause endpoints, ProblemDetails, auth, rate limits, HAL affordance gating. |
| Aspire E2E | Basic profile first; RabbitMQ profile later with PostgreSQL + RabbitMQ + Mailpit + API flow. |

Never run solution-level `dotnet test`; follow `docs/TESTING.md` and project-specific commands.

## 13. Risks And Mitigations

| Risk | Mitigation |
|---|---|
| RabbitMQ becomes mandatory or conceptual center | Basic Dispatch Mode is default and first release target; RabbitMQ is an optional transport profile only. |
| Broker becomes source of truth | PostgreSQL `EmailDispatchOutbox` state machine owns business truth; broker carries pointer-only messages after durable intent exists. |
| MQContract cannot prove reliability semantics | Treat MQContract as a gate; replace/wrap below Event transport port if it lacks required semantics. |
| First slice grows into generic automation | Registration confirmation only until Basic dispatch is proven. |
| Admin/HAL contract hardens too early | Defer admin/HAL operations until state machine and first slice are verified. |
| Silent no-op dispatch | Startup validation and health checks; no blank endpoint success paths. |
| Duplicate email on crash window | Stable idempotency keys, receipt claim unique constraint, attempt ledger, and terminal duplicate handling. |
| Tenant data leak in workers | Mandatory tenant rebind and database identity validation before side effects. |
| Overbroad EAV | Workflow-critical state remains explicit; custom properties are governed optional metadata only. |
| Insufficient tests | Failure matrix is a Phase 0 deliverable and blocks implementation. |

## 14. Implementation-Agent Documentation Contract

Before each implementation PR/commit-sized unit, agents must:

1. Read `AGENTS.md`, `docs/QUICK_REFERENCE.md`, the matched intent in `.claude/contract/intents.yaml`, relevant docs/rules/skills, and these three dev docs.
2. Update `crmworx-event-api-adaptation-context.md` with new facts or decisions.
3. Update `crmworx-event-api-adaptation-tasks.md` before and after each task.
4. Preserve `Last Updated: YYYY-MM-DD Europe/Brussels` in all touched dev docs.
5. Record verification evidence next to the completed task.
6. If implementation diverges from this plan, record the reason and the new evidence source.

## 15. Success Criteria

This workstream is complete when:

- Event can persist a registration-confirmation `EmailDispatchOutbox` intent in the same transaction as registration state.
- Basic Dispatch Mode can send through SMTP, record receipt/attempt/final state, and surface health/metrics/logs without RabbitMQ.
- RabbitMQ Dispatch Mode, if enabled, uses the same state machine and passes confirms/returns/manual-ack/DLQ/parking/failure-window tests.
- Operators can inspect dispatch state, failures, unknown outcomes, and retry/dead-letter state without reading broker internals or message bodies.
- HAL/admin affordances are added only after state transitions are stable and are gated solely by API-provided links.
- Custom fields can participate in automation only through governed, tenant-safe, projection-aware paths and never store workflow-critical state.
- All relevant architecture, unit, persistence, API, SMTP, optional broker, and E2E tests pass.
