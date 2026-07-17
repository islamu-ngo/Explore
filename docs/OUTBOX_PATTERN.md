ABOUTME: Transactional outbox pattern implementation for reliable event delivery with at-least-once semantics.
ABOUTME: Covers OutboxMessage entity, processor lifecycle, retry/dead-letter strategy, and configuration.

# Outbox Pattern

The transactional outbox ensures domain state changes and event publication are atomic. Events are persisted in the same database transaction as business data, then dispatched asynchronously by a background processor.

## Why Outbox

The dual-write problem occurs when a service must update its database and publish a message — either can fail independently. The outbox pattern eliminates this by making event creation part of the business transaction. The outbox table becomes the source of truth for pending events.

Delivery guarantee: **at-least-once**. Consumers must be idempotent.

## Domain Model

### OutboxMessage Entity

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` (UUID v7) | Auto-generated via `uuidv7()` |
| `AggregateType` | `string(200)` | Source aggregate (e.g., `Event`, `Actor`) |
| `AggregateId` | `Guid` | Source aggregate ID |
| `EventType` | `string(200)` | Event name (e.g., `EventCreated`) |
| `Payload` | `string?` (JSONB) | Serialized event data |
| `Status` | `OutboxMessageStatus` | Current lifecycle state |
| `CreatedAt` | `DateTime` | When the message was created |
| `ProcessedAt` | `DateTime?` | When processing completed |
| `RetryCount` | `int` | Number of dispatch attempts |
| `LastError` | `string?(2000)` | Last failure reason |
| `NextRetryAt` | `DateTime?` | Scheduled next attempt |
| `MaxRetries` | `int` (default 10) | Maximum retry attempts |
| `DeadLetteredAt` | `DateTime?` | When dead-lettered |

### Status Lifecycle

```
Pending → Processing → Completed
                    ↘ Failed → (retry) → Processing
                              ↘ DeadLettered
```

| Status | Value | Meaning |
|--------|-------|---------|
| `Pending` | 1 | Awaiting first dispatch attempt |
| `Processing` | 2 | Currently being dispatched (optimistic lock held) |
| `Completed` | 3 | Successfully dispatched |
| `Failed` | 4 | Dispatch failed, may be retried |
| `DeadLettered` | 5 | Exceeded max retries, requires manual intervention |

## Repository Interface

`IOutboxRepository` provides:

| Method | Description |
|--------|-------------|
| `Create` | Insert new outbox message |
| `GetPendingBatch(batchSize, ct)` | Fetch pending messages ordered by creation |
| `TryMarkAsProcessing(id, ct)` | Optimistic concurrency lock — returns `bool` |
| `MarkAsCompleted(id, ct)` | Set status to Completed with timestamp |
| `MarkAsFailed(id, error, isRetryable, retryDelay, maxRetries, ct)` | Record failure and schedule retry |
| `GetFailedEntries(limit, ct)` | Retrieve Failed and DeadLettered for monitoring |
| `DeleteCompletedOlderThan(cutoff, ct)` | Cleanup old completed messages |

`TryMarkAsProcessing` uses optimistic concurrency — only one processor instance can claim a message. This enables safe multi-instance deployment without distributed locks.

## Dispatcher Interface

`IOutboxMessageDispatcher` defines a single method:

```csharp
Task DispatchAsync(OutboxMessage message, CancellationToken ct);
```

The current general dispatcher implementation is `CompositeOutboxMessageDispatcher`. It routes internal application messages such as `EventPublishedNotificationFanoutRequested`, moderation fanout messages, and report provider sync messages to their Infrastructure dispatchers. Unknown or retired event types fail closed by throwing so the processor can retry or dead-letter the row.

Dispatchers must be **idempotent** — the same message may be dispatched more than once if the processor crashes after dispatch but before marking completion.

## OutboxProcessor (Background Service)

The processor runs as a hosted `BackgroundService` with a configurable polling loop:

1. Poll for pending messages in batches
2. Attempt `TryMarkAsProcessing` on each (optimistic lock)
3. Call `IOutboxMessageDispatcher.DispatchAsync`
4. On success: `MarkAsCompleted`
5. On failure: `MarkAsFailed` with retry scheduling

### Retry Strategy

Exponential backoff with a cap:

```
delay = InitialRetryDelaySeconds × 2^retryCount
capped at MaxRetryDelaySeconds
```

After `MaxRetryCount` exhausted → status becomes `DeadLettered` with `DeadLetteredAt` timestamp. Dead-lettered messages remain in the database indefinitely for manual review.

## Configuration

Section: `OutboxProcessor`

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Enable/disable the processor |
| `PollingIntervalSeconds` | `5` | Seconds between poll cycles |
| `BatchSize` | `100` | Messages per batch |
| `MaxRetryCount` | `5` | Retries before dead-letter |
| `InitialRetryDelaySeconds` | `1` | Base delay for first retry |
| `MaxRetryDelaySeconds` | `3600` | Maximum delay cap (1 hour) |
| `VerboseLogging` | `false` | Detailed per-message logging |

## Database Indexes

| Index | Columns | Purpose |
|-------|---------|---------|
| WorkerPoll | `Status`, `NextRetryAt`, `CreatedAt` | Efficient pending batch queries |
| Aggregate | `AggregateType`, `AggregateId` | Lookup by source entity |
| Dedup | `AggregateType`, `AggregateId`, `EventType`, `CreatedAt` | Deduplication support |

## DI Registration

- `Infrastructure` registers `OutboxProcessorSettings` and `CompositeOutboxMessageDispatcher`
- `Persistence` registers `OutboxRepository`
- `API` registers `OutboxProcessor` as a hosted service
- `DbContext` has `DbSet<OutboxMessage>`

## Specialized Variants

The outbox pattern is also used for domain-specific event flows:

| Variant | Purpose | Additional Fields |
|---------|---------|-------------------|
| `PolicyChangeOutbox` | Settings/policy change propagation | `SettingScope` |
| `PdsSyncOutbox` | AT Protocol federation sync | `Did`, `Collection`, `RecordKey`, `PdsHost` |
| `EmailDispatchOutbox` | Basic Dispatch Mode email delivery state plus optional RabbitMQ pointer-publish metadata | `TenantId`, `PublishEventId`, `Kind`, `SourceType`, `SourceId`, recipient/subject/body snapshots, `AttemptCount`, `NextAttemptAt`, `SentAt`, `UnknownAt`, `DeadLetteredAt`, `ParkedAt`, `CorrelationId`, `RabbitMqLastPublishedAt`, `RabbitMqLastPublishAttemptAt`, `RabbitMqPublishAttemptCount`, `RabbitMqLastPublishFailureCategory` |
| `IntegrationSyncOutbox` | External integration synchronization intent for Listmonk and future providers | `TenantId`, `Provider`, `ResourceType`, `ResourceId`, payload snapshot, `AttemptCount`, `NextAttemptAt`, `LastAttemptAt`, `SucceededAt`, `DeadLetteredAt`, failure details |

`EmailDispatchOutbox` is a specialized durable-intent table, not a TickerQ job table or RabbitMQ queue mirror. Registration confirmation creates an outbox row in the registration transaction. The default TickerQ `email-dispatch-drain` job triggers the shared drain service, which claims due rows, rebinds tenant context, calls the approved SMTP abstraction, records attempts/receipts, and advances delivery state. `EmailDispatchProcessor` remains a hosted-service fallback trigger over that same service. Optional RabbitMQ Dispatch Mode shares this PostgreSQL state machine: `EmailDispatchRabbitMqPointerPublisherService` publishes pointer-only messages after durable rows exist, and the manual-ack consumer calls the same drain service before settling broker deliveries.

Approved planned lifecycle-email changes keep this single-ledger boundary. Recipient intent, channel delivery, and SMTP work become explicit rows created atomically in an Application-owned PostgreSQL transaction. Fanout mutations create one immutable occurrence plus one PII-free general-outbox pointer; a leased worker materializes recipients with an audience cutoff, stable cursor, idempotent conflict target, supersession, and fair tenant scheduling. The conditional provider-handoff transition is the suppression fence; post-handoff uncertainty settles as `Unknown` and requires reconciliation before replay.

The Coop callback path uses a specialized `IncomingWebhookEffectOutbox`, not the general email ledger. It stores tenant/provider/inbox identity, nonblank signed `ProviderDecisionId`, payload SHA-256, effect kind, and bounded processing state. Composite inbox FK retention and exact unique constraints keep callback bytes alive until terminal effect settlement. Claims carry a renewable lease, generation, monotonically increasing fence, and opaque token; every renewal and settlement must match the active claim. The worker loads and revalidates the retained callback, invokes the existing decision command outside the intake transaction, then atomically creates/reuses the applied-effect receipt and completes the pointer. Retryable failures reschedule; poison input dead-letters; authenticated redrive increments the processing generation and is allowed only while the callback remains replayable.

Event publication uses the general `OutboxMessage` table for internal actor-subscription notification fanout. The `EventPublishedNotificationFanoutRequested` row drives fanout; the retired external `EventPublished` MQContract route is no longer produced. The fanout service records resumable progress in `NotificationFanoutRun` and uses deterministic `Notification.DeduplicationKey` values so outbox retries do not create duplicate inbox rows.

Event moderation uses the same general `OutboxMessage` table for attendee fanout. Light moderation writes `EventLightModeratedNotificationFanoutRequested`, which may include event context because the event content is preserved. Heavy redaction writes `EventHeavyRedactedNotificationFanoutRequested`, which omits event id, title, slug, URL, image URI, object key, and original content from the payload; the fanout service resolves the event id from the safe moderation record only for recipient lookup. Heavy attendee notifications are generic, linkless in-app rows.

Event reporting uses the general `OutboxMessage` table for provider synchronization. Report intake writes `EventReportProviderSyncRequested` with safe report/case metadata only. `CompositeOutboxMessageDispatcher` routes that message to `ReportProviderSyncDispatcher`, which derives a stable idempotency key from the outbox message id, calls the runtime moderation provider after commit, and persists local `EventReportExternalLink` / `EventReportSignal` outcomes. Retryable provider failures throw after recording bounded local failure metadata so the existing outbox retry/dead-letter policy remains authoritative.

Listmonk registration sync uses `IntegrationSyncOutbox`. Registration handlers enqueue durable Listmonk subscriber intent only after consent/settings qualify; they do not call Listmonk directly. `IntegrationSyncDrainService` claims due rows after commit, rebinds tenant context, calls the generated Listmonk client through Infrastructure services, and advances attempt/success/dead-letter state.

Non-negotiable boundary: handlers, controllers, automation executors, sequence processors, and domain services may create durable outbox intent only. They must not send SMTP, publish RabbitMQ, or schedule TickerQ jobs directly.

## Monitoring

- `GetFailedEntries` returns both `Failed` and `DeadLettered` messages for dashboards
- Verbose logging reveals per-message dispatch details
- Notification fanout metrics are emitted on the `Explore.Business` meter as `explore.notifications.fanout_runs` and `explore.notifications.fanout_subscribers` with bounded tags only
- Health check integration available via outbox processor status

## Related

- [ARCHITECTURE.md](ARCHITECTURE.md) — background services and system design
- [DOMAIN.md](DOMAIN.md) — OutboxMessage entity definition
- [ADR-002](adr/ADR-002-outbox-pattern.md) — architectural decision record
- [OPERATIONS.md](OPERATIONS.md) — monitoring and observability
