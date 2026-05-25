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

The default implementation (`LoggingOutboxMessageDispatcher`) is a no-op that logs warnings. Replace it by registering a real dispatcher in DI to enable actual event delivery.

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

- `Infrastructure` registers `OutboxProcessorSettings` + `LoggingOutboxMessageDispatcher`
- `Persistence` registers `OutboxRepository`
- `API` registers `OutboxProcessor` as a hosted service
- `DbContext` has `DbSet<OutboxMessage>`

## Specialized Variants

The outbox pattern is also used for domain-specific event flows:

| Variant | Purpose | Additional Fields |
|---------|---------|-------------------|
| `PolicyChangeOutbox` | Settings/policy change propagation | `SettingScope` |
| `PdsSyncOutbox` | AT Protocol federation sync | `Did`, `Collection`, `RecordKey`, `PdsHost` |
| `EmailDispatchOutbox` | Basic Dispatch Mode email delivery state | `TenantId`, `PublishEventId`, `Kind`, `SourceType`, `SourceId`, recipient/subject/body snapshots, `AttemptCount`, `NextAttemptAt`, `SentAt`, `UnknownAt`, `DeadLetteredAt`, `ParkedAt`, `CorrelationId` |

`EmailDispatchOutbox` is a specialized durable-intent table, not a RabbitMQ queue mirror. Registration confirmation creates an outbox row in the registration transaction. `EmailDispatchProcessor` later claims due rows, rebinds tenant context, calls the approved SMTP abstraction, records attempts/receipts, and advances delivery state. RabbitMQ Dispatch Mode, when added, must share this PostgreSQL state machine and use pointer-only transport messages.

Non-negotiable boundary: handlers, controllers, automation executors, sequence processors, and domain services may create durable outbox intent only. They must not send SMTP or publish RabbitMQ directly.

## Monitoring

- `GetFailedEntries` returns both `Failed` and `DeadLettered` messages for dashboards
- Verbose logging reveals per-message dispatch details
- Health check integration available via outbox processor status

## Related

- [ARCHITECTURE.md](ARCHITECTURE.md) — background services and system design
- [DOMAIN.md](DOMAIN.md) — OutboxMessage entity definition
- [ADR-002](adr/ADR-002-outbox-pattern.md) — architectural decision record
- [OPERATIONS.md](OPERATIONS.md) — monitoring and observability
