ABOUTME: OutboxMessage entity definition, status lifecycle, and EF Core configuration.
ABOUTME: Reference for field types, constraints, and status transitions.

# OutboxMessage Entity Model

## Fields

| Field | Type | Constraint | Notes |
|-------|------|-----------|-------|
| Id | Guid | PK, UUID v7 | Generated via `uuidv7()` in PostgreSQL |
| AggregateType | string | MaxLength 200, Required | e.g. "Event", "Actor", "TenantPolicy" |
| AggregateId | Guid | Required | The aggregate this message belongs to |
| EventType | string | MaxLength 200, Required | e.g. "EventCreated", "PolicyChanged" |
| Payload | string? | JSONB | Nullable — not all events carry data |
| Status | OutboxMessageStatus | Required | Enum: Pending, Processing, Completed, Failed, DeadLettered |
| CreatedAt | DateTime | Required | Set on creation |
| ProcessedAt | DateTime? | Nullable | Set when Completed |
| RetryCount | int | Default 0 | Incremented on each failure |
| LastError | string? | MaxLength 2000 | Last failure message |
| NextRetryAt | DateTime? | Nullable | Calculated from backoff formula |
| MaxRetries | int | Default 10 | Per-message retry cap |
| DeadLetteredAt | DateTime? | Nullable | Set when dead-lettered |

## Status Lifecycle

```
Pending → Processing → Completed
                    ↘ Failed → (retry) → Processing
                              ↘ DeadLettered (after MaxRetries)
```

- **Pending**: Created, awaiting pickup by OutboxProcessor.
- **Processing**: Claimed by processor via `TryMarkAsProcessing` (optimistic lock).
- **Completed**: Dispatcher succeeded. `ProcessedAt` set.
- **Failed**: Dispatcher threw. `RetryCount` incremented, `NextRetryAt` calculated.
- **DeadLettered**: `RetryCount >= MaxRetries`. `DeadLetteredAt` set. Stays in DB.

## EF Core Configuration

```csharp
builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
builder.Property(e => e.Payload).HasColumnType("jsonb");
builder.Property(e => e.LastError).HasMaxLength(2000);
builder.Property(e => e.MaxRetries).HasDefaultValue(10);
```

## Related

- `resources/processor-configuration.md` — retry strategy
- `resources/database-schema.md` — indexes
