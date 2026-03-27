ABOUTME: Database indexes, queries, and schema conventions for outbox tables.
ABOUTME: Covers PostgreSQL-specific optimizations and query patterns.

# Outbox Database Schema

## Indexes

| Name | Columns | Purpose |
|------|---------|---------|
| IX_OutboxMessages_WorkerPoll | Status, NextRetryAt, CreatedAt | Processor batch query — filter by Pending + retry-eligible |
| IX_OutboxMessages_Aggregate | AggregateType, AggregateId | Lookup messages by source aggregate |
| IX_OutboxMessages_Dedup | AggregateType, AggregateId, EventType, CreatedAt | Duplicate detection at creation time |

## Key Queries

### Fetch Pending Batch (OutboxProcessor)

```sql
SELECT * FROM outbox_messages
WHERE status = 'Pending'
  AND (next_retry_at IS NULL OR next_retry_at <= NOW())
ORDER BY created_at
LIMIT @batchSize;
```

### Optimistic Claim (TryMarkAsProcessing)

```sql
UPDATE outbox_messages
SET status = 'Processing'
WHERE id = @id AND status = 'Pending'
RETURNING id;
```

Returns the row only if still Pending — concurrent processors safely contend without exceptions.

### Cleanup Completed

```sql
DELETE FROM outbox_messages
WHERE status = 'Completed' AND processed_at < @cutoff;
```

### Monitor Dead-Lettered

```sql
SELECT * FROM outbox_messages
WHERE status IN ('Failed', 'DeadLettered')
ORDER BY created_at DESC
LIMIT @limit;
```

## PostgreSQL-Specific

- `Payload` column uses `jsonb` type — queryable, indexable if needed.
- `Id` uses `uuidv7()` default — time-ordered UUIDs for insert performance.
- Named query filter `SoftDelete` does NOT apply to outbox tables (no `IsDeleted` field).

## Related

- `resources/entity-model.md` — field definitions
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md` — EF Core conventions
