ABOUTME: Decision record for adopting the transactional outbox pattern for reliable messaging.
ABOUTME: Covers dual-write problem, at-least-once delivery, and dead-letter handling.

# ADR-002: Transactional Outbox Pattern

- **Status:** Accepted
- **Date:** 2026-01
- **Deciders:** Core team

## Context

The platform needs to reliably propagate domain events across subsystems (ATProto federation sync, policy change notifications, future integrations). Writing to the database and publishing to a message broker in the same operation creates a dual-write problem — either write can fail independently, leaving the system in an inconsistent state.

## Decision

Adopt a poll-based transactional outbox pattern where domain events are written to an `outbox_messages` table within the same database transaction as the aggregate change. A background worker (`OutboxProcessor`) polls for pending messages and dispatches them.

### Delivery Guarantees

- **At-least-once delivery** — messages may be dispatched more than once; consumers must be idempotent.
- **Optimistic concurrency** — `TryMarkAsProcessing` prevents duplicate processing across scaled instances.
- **Exponential backoff** — failed messages retry with `InitialRetryDelaySeconds × 2^retryCount`, capped at `MaxRetryDelaySeconds`.
- **Dead-letter** — after `MaxRetries` exhausted, messages move to `DeadLettered` status and remain in the database for manual inspection.

### Specialized Variants

The general `OutboxMessage` entity is supplemented by domain-specific outbox tables:

- `pds_sync_outbox` — ATProto PDS federation sync (Did, Collection, RecordKey, PdsHost).
- `policy_change_outbox` — governance policy propagation (SettingScope, SettingKey).

Each variant follows the same status lifecycle (Pending → Processing → Completed/Failed → DeadLettered) but carries domain-specific payload columns instead of generic JSONB.

### Current Limitation

The default `IOutboxMessageDispatcher` implementation is `LoggingOutboxMessageDispatcher` — a no-op that logs warnings. Real dispatchers will be registered as integrations are built.

## Consequences

1. Domain events are never lost if the database transaction succeeds.
2. Consumers must handle duplicate delivery (idempotency requirement).
3. The outbox table grows and requires periodic cleanup of completed messages.
4. Polling introduces latency (configurable via `PollingIntervalSeconds`, default 5s).
5. Dead-lettered messages require manual intervention or monitoring dashboards.
6. No external message broker dependency — the database is the single source of truth.

## Related

- [OUTBOX_PATTERN.md](../OUTBOX_PATTERN.md) — full implementation reference
- [ARCHITECTURE.md](../ARCHITECTURE.md) — system architecture overview
- [ADR-001](ADR-001-authorization-provider-architecture.md) — authorization architecture
