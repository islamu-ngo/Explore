---
name: outbox-pattern
description: Transactional outbox pattern for reliable at-least-once messaging in Clean Architecture.
type: domain
enforcement: enforce
priority: high
---

ABOUTME: Transactional outbox pattern skill for reliable at-least-once event delivery.
ABOUTME: Covers OutboxMessage entity, OutboxProcessor, retry/dead-letter, and specialized variants.

# Outbox Pattern

## Keywords

outbox, outbox message, outbox processor, at-least-once, dead letter, event dispatch, transactional messaging, retry, background service

## File Patterns

- `**/OutboxMessage*.cs`
- `**/OutboxProcessor*.cs`
- `**/IOutboxRepository.cs`
- `**/IOutboxMessageDispatcher.cs`
- `**/PolicyChangeOutbox*.cs`
- `**/PdsSyncOutbox*.cs`

## Non-Inferable Rules

1. OutboxMessage uses UUID v7 (`Guid` in C#, `uuidv7()` in PostgreSQL) — not auto-increment.
2. `TryMarkAsProcessing` uses optimistic concurrency — returns `bool`, no exceptions on contention.
3. Retry delay formula: `InitialRetryDelaySeconds × 2^retryCount`, capped at `MaxRetryDelaySeconds`.
4. After `MaxRetries` exhausted, status transitions to `DeadLettered` — messages stay in DB indefinitely for monitoring.
5. `LoggingOutboxMessageDispatcher` is the default no-op — real dispatchers must be registered to replace it.
6. Three specialized outbox variants exist: general `OutboxMessage`, `PolicyChangeOutbox` (settings), `PdsSyncOutbox` (AT Protocol federation). Each has its own entity and repository.
7. OutboxProcessor polls on a `PeriodicTimer` — not event-driven. Default interval: 5 seconds.
8. Dispatchers must be idempotent — at-least-once delivery means duplicates are possible.

## Activation

Load this skill when:
- Creating or modifying outbox message entities or processors
- Implementing event dispatchers
- Working on retry/dead-letter logic
- Touching background services that process queued messages

## Resources

- `resources/entity-model.md` — OutboxMessage fields, status lifecycle, EF configuration
- `resources/processor-configuration.md` — OutboxProcessor settings, retry strategy, dead-letter
- `resources/dispatcher-patterns.md` — IOutboxMessageDispatcher, specialized variants
- `resources/database-schema.md` — Indexes, queries, database conventions

## Related

- `docs/OUTBOX_PATTERN.md`
- `docs/ARCHITECTURE.md` (Background Services section)
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md`
- `.claude/skills/clean-architecture-rules/SKILL.md`
