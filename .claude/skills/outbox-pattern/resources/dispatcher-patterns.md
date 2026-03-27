ABOUTME: IOutboxMessageDispatcher interface and specialized outbox variant patterns.
ABOUTME: Covers dispatcher contract, idempotency requirement, and variant implementations.

# Dispatcher Patterns

## IOutboxMessageDispatcher Interface

```csharp
public interface IOutboxMessageDispatcher
{
    Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken);
}
```

**Contract:**
- Must be idempotent — at-least-once delivery means the same message may be dispatched multiple times.
- Must throw on transient failures (processor will retry).
- Must throw on permanent failures (processor will exhaust retries → dead-letter).
- Should distinguish transient vs permanent via exception type or custom logic.

## Default Implementation

`LoggingOutboxMessageDispatcher` is the default no-op dispatcher. It logs a warning and returns. This is intentional — real dispatchers must be registered to replace it.

```csharp
// Infrastructure registers the default:
services.AddSingleton<IOutboxMessageDispatcher, LoggingOutboxMessageDispatcher>();

// To replace, register your dispatcher AFTER Infrastructure DI:
services.AddScoped<IOutboxMessageDispatcher, MyRealDispatcher>();
```

## Specialized Outbox Variants

The project has three independent outbox tables, each with its own entity, repository, and processor:

| Variant | Entity | Purpose | Key Fields |
|---------|--------|---------|------------|
| General | `OutboxMessage` | Domain events | AggregateType, AggregateId, EventType, Payload |
| Policy Change | `PolicyChangeOutbox` | Settings/policy sync | SettingScope, related policy fields |
| PDS Sync | `PdsSyncOutbox` | AT Protocol federation | Did, Collection, RecordKey, PdsHost |

Each variant follows the same pattern (status lifecycle, retry, dead-letter) but has domain-specific fields. Do not mix variants — each has dedicated repository methods.

## Idempotency Patterns

Since at-least-once delivery means duplicates:

1. **Dedup by message ID**: Consumer tracks processed OutboxMessage IDs.
2. **Idempotent operations**: Use upserts or conditional writes.
3. **Dedup index**: `IX_OutboxMessages_Dedup` on `(AggregateType, AggregateId, EventType, CreatedAt)` helps detect duplicate creation.

## Related

- `resources/entity-model.md` — OutboxMessage fields
- `resources/processor-configuration.md` — retry settings
