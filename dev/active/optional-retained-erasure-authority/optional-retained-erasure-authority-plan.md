<!-- ABOUTME: Implementation plan for three mutually exclusive erasure-authority topologies. -->
<!-- ABOUTME: Expands CoLocated authority storage across every supported primary database provider. -->

# Optional Retained Erasure Authority Implementation Plan

**Status:** Re-baselined for five-provider `CoLocated` completion

**Last Updated:** 2026-08-08 (Europe/Brussels)

**Default:** Restore-isolated embedded SQLite
**Alternative:** `CoLocated` or `ExternalDatabase` explicit topologies for different operational constraints.

**Enterprise option:** Separately restored external PostgreSQL

## Outcome

Privacy erasure intent survives primary database backup rollback in `EmbeddedSqlite` and `ExternalDatabase`. `CoLocated` works with every supported primary provider (`PostgreSQL`, `SQLite`, `SqlServer`, `MariaDb`, and `MySql`) and intentionally shares the primary restore lifecycle in exchange for one atomic database backup; startup replay still runs, but this topology does not claim restore-isolated resurrection protection.

## Preserved Completed Scope

Do not reimplement accepted authority contracts, erasure orchestration, startup gating, applier flow, transactional outbox behavior, replay semantics, or completed security/operations work unless topology replacement invalidates its evidence. The new work replaces storage ownership, not the established privacy outcome.

## Acceptance Criteria

1. Authority storage is single-sink per deployment mode:
   - `EmbeddedSqlite` (default): dedicated local authority file.
   - `CoLocated`: authority tables in the selected PostgreSQL, SQLite, SQL Server, MariaDB, or MySQL primary application database.
   - `ExternalDatabase`: authority in a separate external PostgreSQL database.
2. `CoLocated` derives provider, connection, and credentials exclusively from the primary database configuration; it has no second authority target or dual-write path.
3. PostgreSQL and SQL Server co-located tables use `DATABASE_SCHEMA` (default `islamu_event`); SQLite, MariaDB, and MySQL use the fixed `ie_` prefix. No database-prefix setting exists.
4. The embedded authority file lives on a dedicated persisted local volume outside primary backup/restore.
5. `ExternalDatabase` remains available using a separate PostgreSQL database and independently scoped credentials.
6. In `EmbeddedSqlite` and `ExternalDatabase`, the primary database contains only the replay checkpoint as authority state; `CoLocated` also stores retained authority rows in the primary application database.
7. `PrivacyErasureStartupGate` replays retained intent before readiness after a primary restore.
8. Primary checkpoint ahead of retained authority state fails readiness closed.
9. Embedded append, replay, compaction, restart, backup/restore, and concurrency behavior are proven against a real file.
10. Embedded mode rejects multi-replica, shared/network filesystem, invalid path, and unsafe-permission deployments.
11. External authority accepts structured privacy-prefixed database fields, never a raw connection string.
12. Runtime and migrator credentials are separate for external PostgreSQL and enforced by ACL tests.
13. Generated provider-specific migration artifacts are reproducible and never hand-edited.
14. Retained intent is durably appended before destructive primary erasure is acknowledged; the primary outbox is not the first authority copy.

## Target Data Flow

```text
retained authority
  - embedded SQLite file (`EmbeddedSqlite`),
  - primary application database (`CoLocated`), or
  - external PostgreSQL database (`ExternalDatabase`)
        |-- durable idempotent intent append
        v
primary erasure transaction
        |-- domain erasure + saga/outbox/receipt
        |-- replay checkpoint advancement
        `-- acknowledge only after commit

process start / primary restore
        v
PrivacyErasureStartupGate
        |-- compare authority state and primary checkpoint
        |-- replay missing intent idempotently
        |-- advance primary checkpoint
        `-- allow readiness only when convergence is proven
```

Authority-first ordering avoids a loss window between primary commit and authority delivery. If primary application fails after the authority append, retry or startup replay completes it idempotently. The primary outbox continues to provide at-least-once delivery for ordinary post-commit side effects, but it is never the first durable copy of retained authority intent.

## Phase History

The preceding plan accepted Phases 1, 2, 5, and most of 6. It completed OREA-100/110/120, 200/210/220, 300, 420, 500/510/520, 600/610, and 700. Those results remain evidence. OREA-310/320 are incomplete. Provider settlement OREA-400/410, disaster recovery OREA-620, and final evidence OREA-710/720 remain open or must be rerun.

## Phase 8: Topology Settlement and Contract Rebaseline

### Work

- Settle a mutually exclusive topology enum/configuration with `EmbeddedSqlite`, `CoLocated`, and `ExternalDatabase`.
- Define embedded file path, local-volume, one-writer, permissions, WAL, busy-timeout, private-cache, and integrity requirements.
- Define external privacy-prefixed structured database fields using the shared multi-database input vocabulary.
- Permit only PostgreSQL for external authority initially.
- Complete OREA-310/320 contract and boundary work under the new topology.
- Define the authority-first handoff and stable idempotency contract before changing storage implementations.
- Inventory all primary authority ledger tables, functions, repositories, migrations, and tests by mode:
  - retained in `EmbeddedSqlite`,
  - retained in primary DB for `CoLocated`, and
  - reduced/removed for `ExternalDatabase`.

### Exit criteria

- Configuration matrix has no raw connection-string path.
- Topology and ownership boundaries are explicit in code-facing contracts.
- A reviewed mode inventory maps every legacy artifact and each mode's intended destination.

## Phase 9: Embedded SQLite Authority

### Work

- Add a dedicated authority SQLite DbContext, entities/configuration as needed, and generated migration assembly.
- Use the dedicated SQLite authority context only for `EmbeddedSqlite`.
- Initialize WAL once, use a bounded busy timeout and private cache, and avoid per-connection global pragma churn.
- Enforce one API writer/replica and local filesystem deployment.
- Create parent directory/file with restrictive permissions and verify them at startup.
- Adapt append, read, sequence, floor, replay, and compaction operations to portable EF/SQLite semantics without weakening monotonicity.
- Keep primary and authority SQLite contexts, files, migrations, volumes, health
  checks, and backups distinct in `EmbeddedSqlite`.

### Exit criteria

- Real-file tests prove monotonic append, bounded contention, restart durability, replay, compaction, and integrity checks.
- Invalid or unsafe file topology blocks readiness.

## Phase 10: CoLocated Provider Alignment and Single-Sink Enforcement

### Work

- Preserve `CoLocated` as one topology with five provider implementations, not
  five modes and not an additional copy beside embedded or external storage.
- Replace the PostgreSQL-specific and co-located SQLite composition branches
  with one provider-neutral co-located DbContext and repository selected from
  the primary provider configuration.
- Reuse `RelationalModelNamespace`: PostgreSQL and SQL Server use
  `DATABASE_SCHEMA` (default `islamu_event`); SQLite, MariaDB, and MySQL use the
  fixed `ie_` prefix. Do not add a configurable prefix.
- Reuse `RelationalNamedLock` inside the authority append transaction to
  serialize the singleton counter across PostgreSQL advisory locks, SQL Server
  `sp_getapplock`, MySQL/MariaDB `GET_LOCK`, and the SQLite process lock. Keep
  MySQL/MariaDB transaction-completion lock release through the existing
  interceptor.
- Make authority entity mappings provider-portable while retaining required
  keys, unique indexes, value conversions, and valid provider-specific check
  constraints; apply the existing MySQL identifier-shortening policy.
- Give each provider a dedicated generated co-located migration lane and
  migration-history namespace. Keep embedded SQLite migrations separate from
  co-located SQLite; regenerate all unapplied development artifacts with
  `dotnet ef` rather than editing them.
- Route runtime DI and `Event.MigrationService` through the same closed
  five-provider composition switch, and migrate the selected co-located
  authority context exactly once.
- Ensure this phase does not introduce operator-side mode migration; it only finalizes topology contracts.
- Reduce authority footprint in `EmbeddedSqlite`/`ExternalDatabase` to replay checkpoint-only.
- Retain normal privacy saga, transactional outbox, receipt, and completion records required by primary transaction ownership.
- Remove or retire legacy retained-authority repositories, tables, functions, configuration, and tests only where they conflict with the finalized topology model.
- Ensure MigrationService is the only production migration/cutover owner.

### Exit criteria

- PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL each complete clean and
  idempotent co-located migration, append, read, replay, and restart scenarios
  against their real engine.
- Concurrent appends produce unique, strictly monotonic authority sequences;
  same-intent/same-payload retries reuse the row and mismatched payloads fail.
- Schema-capable providers use the configured schema, schema-less providers use
  `ie_`, and all five use a distinct authority migration-history namespace.
- No retained authority ledger remains in the primary schema for `EmbeddedSqlite` and `ExternalDatabase`; `CoLocated` intentionally keeps the retained authority tables there.
- Each topology replays only from its selected authority sink; no cross-mode
  migration compatibility is added during pre-v1 development.
- Restore documentation identifies `CoLocated` as part of the primary atomic
  backup and does not claim restore-isolated resurrection protection.

## Phase 11: Startup Gate and Rollback Detection

### Work

- Compare primary replay checkpoint with retained authority high-water/floor state before readiness.
- Replay idempotently when primary is behind.
- Fail closed when primary is ahead, authority storage is missing/corrupt/unavailable, permissions are unsafe, or replay cannot complete.
- Persist and validate monotonic counter/floor evidence sufficient to detect authority rollback or replacement.
- Preserve bounded startup behavior and actionable credential-safe diagnostics.

### Exit criteria

- In `EmbeddedSqlite` and `ExternalDatabase`, restoring primary alone triggers
  replay from untouched authority storage and converges before readiness.
- In the restore-isolated topologies, restoring/replacing authority with an
  older copy blocks readiness.
- In `CoLocated`, primary and authority restore atomically and startup replay
  validates their shared checkpoint/ledger state without claiming restore
  isolation.
- Repeated startup replay is idempotent.

## Phase 12: External PostgreSQL Completion

### Work

- Bind privacy-prefixed structured provider, host, port, database, username, password, TLS, and trust fields.
- Build Npgsql connection strings internally; remove raw `--connection` and environment escape hatches.
- Preserve separate runtime and migrator credentials.
- Complete functions, ownership, ACL, migration ownership, append/replay, and startup behavior.
- Prove independent external authority backup/restore and rollback detection.

### Exit criteria

- Runtime role cannot migrate or bypass append/read contracts.
- Migrator role is used only by MigrationService.
- External authority and primary restore operations remain independent.

## Phase 13: Operations, Recovery, and Release Evidence

### Work

- Add separate embedded authority volume examples to Aspire, Compose, and deployment manifests.
- Define independent authority backup, integrity check, retention, restore, and disaster-recovery procedures.
- Document one-writer/local-filesystem limits and external topology selection criteria.
- Run a primary-only restore drill, authority rollback drill, authority corruption/unavailability drill, interrupted cutover drill, and external independent-restore drill.
- Revalidate previously completed security, compaction, observability, and performance evidence under all three target topologies.
- Complete OREA-620, 710, and 720 with linked evidence.

### Exit criteria

- Operators can prove which authority instance and high-water state are active without exposing personal data or secrets.
- Every recovery drill has an expected failure/ready state and rollback procedure.
- All acceptance criteria and historical open items are closed.

## Testing Strategy

- Contract tests: topology validation, structured external settings, credential redaction, one-writer and path constraints.
- Embedded file tests: concurrent append, monotonic sequence/floor, busy timeout, restart, replay, compaction, corruption, permissions, backup/restore, and rollback detection.
- Co-located provider matrix: run MigrationService twice and exercise append,
  exact-payload idempotency, mismatch rejection, concurrent monotonic allocation,
  ordered `ReadAfter`, replay, restart, namespace placement, and single-sink
  composition against PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL.
- Primary integration: erasure transaction, saga/outbox/receipt behavior, checkpoint advancement, idempotent replay, and absence of shadow authority ledgers in non-colocated topologies.
- External PostgreSQL: real functions, ACLs, runtime/migrator separation, clean migrations, replay, backup/restore, and rollback detection.
- Recovery: restore-isolated primary rollback must replay, authority rollback
  must fail closed, and co-located restore must preserve one atomic database
  boundary.
- Architecture: topology boundaries, provider-specific migration ownership,
  exactly one co-located adapter, and no hand-edited generated artifacts.

## Security and Privacy

- Authority rows contain only the minimal pseudonymous material required to prevent resurrection.
- Keys, salts, credentials, and file permissions follow least privilege and rotation procedures.
- Logs expose topology, sequence ranges, counts, and outcomes, never raw identifiers, secrets, or connection strings.
- Embedded file copies are encrypted/protected according to deployment policy and handled as privacy-sensitive backups.
- External runtime credentials cannot perform schema ownership operations.

## Operational Boundaries

- Embedded mode is for one application writer using a durable local filesystem.
- Multi-replica or shared-storage deployments use external PostgreSQL.
- `CoLocated` inherits the primary provider's deployment and connection limits;
  it adds no authority database, file, credentials, or backup job.
- Embedded and external authority backups have separate jobs, retention, labels, restore commands, and drills; `CoLocated` is included in the primary backup and restore.
- Authority compaction, backup, and restore preserve monotonic floor evidence.

## Definition of Done

- `EmbeddedSqlite`, five-provider `CoLocated`, and external PostgreSQL each satisfy acceptance criteria for their selected mode.
- `CoLocated` is proven on PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL with
  the required schema/prefix and migration-history namespace.
- Each mode is mutually exclusive and writes retained authority state to only one destination.
- Historical completed behavior remains intact.
- Startup replay is proven in every topology; rollback detection is proven for the restore-isolated topologies.
- Recovery documentation and drills prove the declared restore boundary of each topology.
- Release evidence closes all open and revalidated OREA tasks.
