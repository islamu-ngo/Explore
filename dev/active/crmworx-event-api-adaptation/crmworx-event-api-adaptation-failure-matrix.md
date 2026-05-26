<!-- ABOUTME: Failure matrix for Basic and future RabbitMQ EmailDispatch workflow crash windows. -->
<!-- ABOUTME: Defines expected persisted state, operator signal, and validation coverage for async dispatch reliability. -->

# CRMWorx Event API Adaptation — EmailDispatch Failure Matrix

Last Updated: 2026-05-25 Europe/Brussels

## Purpose

This matrix freezes the expected behavior for the PostgreSQL-owned `EmailDispatchOutbox` state machine before adding broader E2E coverage or optional RabbitMQ transport. It is the acceptance contract for Basic Dispatch Mode first and RabbitMQ Dispatch Mode later.

The central rule is unchanged: **PostgreSQL owns business state**. SMTP and RabbitMQ are side-effect transports. If a crash, retry, duplicate, replay, or tenant-control event happens, the durable rows in PostgreSQL must explain what happened without requiring broker inspection or SMTP logs.

## State Vocabulary

| State or record | Meaning |
|---|---|
| `EmailDispatchOutbox.Pending` | Durable intent exists and has not been claimed yet. |
| `EmailDispatchOutbox.Processing` | A worker claimed the row with a lease token and is attempting delivery. |
| `EmailDispatchOutbox.Sent` | SMTP/provider success was recorded. |
| `EmailDispatchOutbox.RetryScheduled` | A retryable failure occurred and `NextAttemptAt` controls the next worker attempt. |
| `EmailDispatchOutbox.DeadLettered` | Retry budget exhausted or non-retryable failure requires operator action. |
| `EmailDispatchOutbox.Parked` | Operator or replay safety logic intentionally removed the row/message from automatic replay. |
| `EmailDispatchOutbox.Unknown` | Provider outcome is ambiguous, most commonly timeout after handoff; do not blindly retry. |
| `EmailDispatchAttempt` | One SMTP/provider attempt record with normalized outcome/failure category. |
| `EmailDispatchReceipt` | Duplicate-protection and consumer identity ledger keyed by `(TenantId, PublishEventId)`. |
| `EmailDispatchTenantControl` | Tenant dispatch pause state. |

## Basic Dispatch Mode Matrix

| Scenario | Trigger / failure window | Expected persisted state | Operator signal | Required validation |
|---|---|---|---|---|
| Registration transaction rolls back before commit | Registration handler creates intent but registration transaction fails | No registration state and no `EmailDispatchOutbox` row commit | No dispatch row visible | Persistence integration test around rollback/transaction boundary |
| Registration transaction commits | Registration succeeds | `EmailDispatchOutbox.Pending`, `AttemptCount=0`, source points to registration intent, recipient/body snapshot stored | Status read endpoint shows pending operational row without body/recipient fields | Application unit test + persistence integration test |
| Duplicate registration command | Same tenant/event/user/scope already registered | No duplicate dispatch row for same source/kind | Existing registration response; status row count remains one | Handler unit test or API integration test |
| Worker starts with dispatch disabled | `EmailDispatchProcessor:Enabled=false` | Rows remain pending; no attempts consumed | `email-dispatch` health reports `Degraded` intentionally disabled | Health-check test |
| Worker sees tenant pause before claim | `EmailDispatchTenantControl.IsPaused=true` | Row remains pending; no attempt consumed; metric outcome `tenant_paused` | Status remains pending; metric count increments | Repository/worker unit test |
| Two workers claim same row | Concurrent polling | Exactly one `TryMarkAsProcessing` succeeds; one attempt number allocated | No duplicate attempt for same `EmailDispatchOutboxId`/attempt | Persistence integration test with concurrent claims |
| Worker crashes after claim before SMTP | Process dies after `Processing` lease set, before provider call | Row must become claimable again after lease/recovery policy or be detected as stuck | Health/ops docs expose stuck processing age; no sent state | Component/integration test once lease recovery is implemented |
| SMTP success then mark sent succeeds | Provider returns success and DB update succeeds | `Sent`, `SentAt`, completed receipt, succeeded attempt | Status endpoint shows delivered timestamp; metric outcome `sent` | Worker unit/component test |
| SMTP transient failure | Provider returns retryable failure | `RetryScheduled`, `NextAttemptAt`, failed attempt, normalized failure category | Status endpoint shows retry timestamp/failure category, no raw error | Worker unit/component test |
| SMTP permanent failure | Provider returns non-retryable failure | `DeadLettered`, `DeadLetteredAt`, failed attempt, normalized failure category | Status endpoint shows dead-letter state | Worker unit/component test |
| SMTP timeout or cancellation after handoff | Provider outcome ambiguous | `Unknown`, `UnknownAt`, unknown attempt, receipt failed with `smtp_outcome_unknown` | Status endpoint shows unknown timestamp; metric outcome `unknown`; no blind retry | Worker unit/component test |
| Retry budget exhausted | Repeated retryable failures exceed min(row max, configured max) | `DeadLettered`, retry stops, attempts preserved | Status endpoint shows terminal failure | Worker unit/component test |
| Worker crashes after SMTP success before marking sent | Provider may have sent, DB still `Processing` or retryable | Current risk: at-least-once duplicate possible; future mitigation requires provider idempotency or Unknown transition on lease recovery | Failure matrix marks risk explicitly; operators see non-sent row | Documented risk + future test when lease recovery exists |
| Receipt duplicate claim | Same `(TenantId, PublishEventId)` observed twice | Second claim returns false; processing is idempotent | No duplicate completed receipt | Repository unit/integration test |
| Missing SMTP configuration | Tenant/system SMTP resolver cannot produce config | Retry/dead-letter/unknown according to provider result; never silent success | `smtp` health unhealthy/degraded; dispatch status shows normalized failure | Health + worker test |
| Invalid Basic processor options | Bad polling interval, batch size, retry delay, max attempts, or consumer ID | Startup validation fails | Startup/config validation error; no silent no-op | `EmailDispatchProcessorSettingsValidatorTests` |
| Status read request | Operator requests status | Safe DTO only; no recipient, subject, body, reply-to, provider message ID, or raw error | Authenticated status endpoint | Query handler test |
| Direct SMTP from handler introduced | Future code bypasses durable intent | Architecture test fails | CI red | `DurableSideEffectBoundaryTests` |

## Future RabbitMQ Dispatch Mode Matrix

RabbitMQ Dispatch Mode is not part of the first Basic implementation. These cases are recorded now so optional broker work does not change the PostgreSQL state-machine contract.

| Scenario | Trigger / failure window | Expected persisted state | Operator signal | Required validation |
|---|---|---|---|---|
| RabbitMQ mode disabled | Basic mode selected | Basic dispatch still works; RabbitMQ health ignored | Self-hosters can run API + PostgreSQL + SMTP only | Mode-isolation integration test |
| RabbitMQ config invalid while Basic selected | Broker missing/misconfigured | Basic mode remains healthy except broker-specific checks are not active | No RabbitMQ startup block | Config/profile test |
| RabbitMQ mode selected with broker unavailable | Broker connection fails | No intent loss; rows remain durable in PostgreSQL | RabbitMQ dispatch health unhealthy | Health test |
| Publisher crash before broker publish | Publisher claimed row but crashes before send | Row remains retryable/recoverable in PostgreSQL; no published marker | Status shows pending/retry/processing according to claim policy | RabbitMQ publisher integration test |
| Publisher crash after broker ack before DB mark | Broker accepted pointer event but DB not updated | Consumer receipt/idempotency prevents uncontrolled duplicate SMTP; outbox remains durable truth | Status may show unmarked publish; duplicate receipt visible | Failure-window RabbitMQ test |
| Broker nack | Publisher receives negative confirm | Publish retry scheduled; no delivery attempt yet | Metric/log outcome publish failed | RabbitMQ publisher confirm test |
| Unroutable publish / returned message | Mandatory routing return | Publish retry/dead-letter; no delivery attempt | Health/topology warning; normalized failure category | RabbitMQ returned-message test |
| Consumer receives duplicate pointer message | Broker redelivery or replay | Receipt claim deduplicates by `(TenantId, PublishEventId)`; no second SMTP if terminal/unknown | Receipt ledger shows duplicate/no-op | Consumer idempotency test |
| Consumer sees tenant mismatch | Pointer tenant differs from outbox tenant | Reject/park/DLQ; no SMTP | Safe failure category; no cross-tenant send | Consumer poison test |
| Consumer sees missing outbox row | Pointer event references absent row | Reject/park/DLQ; no SMTP | Parked/DLQ signal | Consumer poison test |
| Consumer crash before SMTP | Message redelivered; no terminal state | Manual ack not sent; idempotent receipt handles retry | Broker redelivery + DB state | RabbitMQ consumer test |
| Consumer crash after SMTP before persisted outcome | Potential duplicate send window | Same at-least-once risk as Basic mode; receipt/attempt ledger must reveal ambiguity | Unknown/deferred state where possible | RabbitMQ failure-window test |
| Already-sent row replayed from DLQ | Operator replay or broker redelivery after sent | Replay is ACK/no-op or parked; no SMTP resend | Status remains sent; replay logs safe reason | DLQ replay test |
| DLQ replay without parking queue | Misconfigured replay topology | RabbitMQ mode unhealthy; replay disabled | Health check unhealthy | Health/topology test |

## Validation Priority

1. Keep unit tests around state transition logic fast and deterministic.
2. Use PostgreSQL-backed persistence tests for claim/idempotency/unique constraints and transaction boundaries.
3. Use SMTP abstraction or Mailpit/Testcontainers tests for provider outcome classification.
4. Use Aspire/Playwright only for journeys that require browser or full distributed app behavior.
5. Add RabbitMQ Testcontainers only when RabbitMQ Dispatch Mode starts; RabbitMQ is not required for Basic Dispatch Mode evidence.

## Current Coverage Snapshot

| Area | Current evidence | Remaining work |
|---|---|---|
| Durable intent from registration | Application unit tests and persistence integration tests passed after `EmailDispatchOutbox` implementation. | Add first full registration-confirmation journey through SMTP/Mailpit or approved SMTP test double. |
| Config/health | Validator and `email-dispatch` health tests passed. | Add selected-mode profile tests when RabbitMQ mode exists. |
| Metrics/logging | `explore.email_dispatch.attempts` tested for safe tags; logs use normalized categories for retry/unknown warnings. | Add stuck/oldest-pending metrics if required by operations dashboard. |
| Safe operator read | Query handler tests prove DTO excludes sensitive fields. | Add admin detail/replay/HAL tests when write actions are introduced. |
| Direct side-effect boundary | Architecture guard prevents direct SMTP/RabbitMQ side effects in Application handlers and API controllers. | Extend when automation/sequence processors are introduced. |
| RabbitMQ mode | Not implemented by design. | MQContract gate plus RabbitMQ publisher/consumer/DLQ/parking tests. |
