ABOUTME: Decision record for deferring PostgreSQL partitioning until scale evidence requires it.
ABOUTME: Defines activation gates, candidate order, migration requirements, and rollback posture.

# ADR-009: PostgreSQL Partitioning Deferral

- **Status:** Accepted
- **Date:** 2026-05
- **Deciders:** Core team

## Context

ISLAMU Event has high-growth operational table families: audit logs, configuration and tenant lifecycle logs, notification inbox state, transactional outboxes, email dispatch state, contact-share exports, idempotency replay cache, custom-property projection coordination, external quota accounting, federation mirrors, and storage metadata.

Phase 6 classified those tables by lifecycle class and added the first low-risk cleanup implementation for expired `idempotency_records`. The platform still needs a durable position on PostgreSQL partitioning because partitioning can improve retention and time-range query operations at scale, but it also changes migrations, insert routing, backup/restore, retention procedures, and operator runbooks.

This decision is based on repository architecture and current PostgreSQL/EF Core guidance:

- PostgreSQL declarative partitioning is most useful when partition keys match query and retention predicates, and operators must create/attach/detach partitions intentionally.
- EF Core does not model PostgreSQL partition lifecycle as normal entity configuration. Provider-specific partition DDL belongs in explicit SQL migrations and runbooks.
- Deployment tiers keep Tier 1 and Tier 2 self-hosting simple; high-scale database maintenance should not become the default operational floor.

## Decision

Defer production PostgreSQL partitioning.

Partitioning is not current runtime behavior. The main schema must not add partitioned tables, partition-maintenance workers, or generated partition migrations until an operator need or load-test result crosses documented activation gates.

Partitioning is a Tier 3 capacity feature. It is allowed later, but only as a deliberate implementation package with migration SQL, preflight checks, runbooks, tests, and rollback posture.

## Activation Gates

Partitioning may be reconsidered when at least one candidate table family crosses a measurable gate:

| Gate | Default trigger | Evidence required |
|---|---:|---|
| Total table size | Candidate table exceeds the operations matrix threshold, for example `audit_logs` over 100M rows or email dispatch history over 25M rows | Database statistics, index bloat report, and table growth trend |
| Tenant concentration | One tenant exceeds a per-tenant threshold, for example `audit_logs` over 10M rows | Tenant-scoped count query and operator impact assessment |
| Query SLO pressure | Normal indexes and query-filter pruning miss production SLOs for time-range or worker scans | Query plans with timing before and after index tuning |
| Retention pressure | Deleting or archiving old rows creates unacceptable locks, vacuum debt, or maintenance windows | Retention dry-run timings and maintenance logs |
| Backup/restore pressure | Backup, restore, or export windows exceed operator objectives because of one append-heavy table family | Backup/restore timing evidence and recovery objective |

## Candidate Order

1. `audit_logs`: first candidate only after legal-hold and export-before-purge posture exists. Use monthly `Timestamp` range partitions because the table is append-only and naturally queried by time.
2. Completed outbox ledgers: consider only after completed/resolved cleanup exists. Pending, processing, retry, failed, and dead-letter rows must remain visible to operators and hot worker indexes.
3. `event_contact_share_exports`: consider after PII-aware purge/export policy exists. Export items must follow parent export lifecycle.
4. `notifications`: consider after read/archive retention rules exist and compliance notification categories are protected.
5. `email_dispatch_outbox` plus attempts/receipts: defer until parent-aware redaction/retention exists. Independent child partitioning is not allowed because attempts and receipts must follow parent evidence semantics.

## Required Implementation Package

Before partitioning becomes current behavior for any table family, the implementation must include:

1. A decision record or ADR update naming the table family, partition key, interval, retention policy, and rollback plan.
2. Explicit PostgreSQL SQL migrations through `migrationBuilder.Sql(...)` or an approved migration extension.
3. A preflight script that checks existing rows fit proposed partition bounds and reports routing failures.
4. A partition creation/attachment runbook. New partitions must exist before writes reach their date range.
5. A detach/archive/drop runbook. Detach before destructive drop when evidence value is uncertain.
6. Integration tests proving insert routing, partition-bound rejection, expected query predicates, and rollback/finalize behavior where feasible.
7. Backup/restore documentation covering parent and child partition tables.

## Consequences

1. Tier 1 and Tier 2 self-hosters keep a simpler PostgreSQL operating model.
2. Partitioning notes in docs are capacity guidance, not current behavior.
3. Future partitioning work must be evidence-led and table-specific.
4. The first production partitioning candidate is likely `audit_logs`, not email dispatch or notification state.
5. No migration files are added for partitioning until the team explicitly chooses an implementation package.
6. Cleanup jobs and legal-hold rules must continue to operate by lifecycle class, not by partition name alone.

## Rollback Posture

- Prefer `DETACH PARTITION` over immediate drop for evidence-bearing tables.
- Do not implement destructive `Down()` behavior that silently loses retained evidence.
- If partitioning is disabled or rolled back, operators must have a tested path to keep accepting new writes without data loss.
- Retention cleanup and legal-hold checks must remain valid during and after rollback.

## Related

- [OPERATIONS.md](../OPERATIONS.md) — lifecycle matrix and partitioning activation runbook.
- [BACKUP_RESTORE_UPGRADE.md](../BACKUP_RESTORE_UPGRADE.md) — backup, restore, upgrade, and rollback expectations.
- [DEPLOYMENT_TIERS.md](../DEPLOYMENT_TIERS.md) — infrastructure maturity tiers.
- [ADR-002](ADR-002-outbox-pattern.md) — transactional outbox pattern.
- [ADR-008](ADR-008-email-dispatch-state-machine.md) — email dispatch state machine and optional dispatch profiles.
