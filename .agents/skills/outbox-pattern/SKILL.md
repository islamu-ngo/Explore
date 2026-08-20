---
name: outbox-pattern
description: "Load for reliable post-commit messaging, domain/integration event delivery, transactional outbox tables, dispatch workers, retries, idempotent consumers, duplicate delivery, or dual-write failure; not for synchronous in-process events with no external side effect."
type: pattern
enforcement: block
priority: high
---

ABOUTME: Transactional outbox pattern skill for reliable at-least-once event delivery.
ABOUTME: Covers OutboxMessage entity, OutboxProcessor, retry/dead-letter, and specialized variants.

# Outbox Pattern

## Non-Inferable Rules

1. OutboxMessage uses UUID v7 (`Guid` in C#, `uuidv7()` in PostgreSQL) — not auto-increment.
2. `TryMarkAsProcessing` uses optimistic concurrency — returns `bool`, no exceptions on contention.
3. Retry delay formula: `InitialRetryDelaySeconds × 2^retryCount`, capped at `MaxRetryDelaySeconds`.
4. After `MaxRetries` exhausted, status transitions to `DeadLettered` — messages stay in DB indefinitely for monitoring.
5. `LoggingOutboxMessageDispatcher` is the default no-op — real dispatchers must be registered to replace it.
6. Three specialized outbox variants exist: general `OutboxMessage`, `PolicyChangeOutbox` (settings), `PdsSyncOutbox` (AT Protocol federation). Each has its own entity and repository.
7. OutboxProcessor polls on a `PeriodicTimer` — not event-driven. Default interval: 5 seconds.
8. Dispatchers must be idempotent — at-least-once delivery means duplicates are possible.

## Resources

- `resources/entity-model.md` — OutboxMessage fields, status lifecycle, EF configuration
- `resources/processor-configuration.md` — OutboxProcessor settings, retry strategy, dead-letter
- `resources/dispatcher-patterns.md` — IOutboxMessageDispatcher, specialized variants
- `resources/database-schema.md` — Indexes, queries, database conventions

## Related

- `docs/OUTBOX_PATTERN.md`
- `docs/ARCHITECTURE.md` (Background Services section)
- `.agents/skills/dotnet-efcore-guidelines/SKILL.md`
- `.agents/skills/clean-architecture-rules/SKILL.md`
