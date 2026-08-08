<!-- ABOUTME: Implementation plan for a restore-isolated retained erasure authority with EmbeddedSqlite, CoLocated, and ExternalDatabase modes. -->
<!-- ABOUTME: Defines anti-resurrection storage, replay, migration, security, recovery, and topology transition work. -->

# Optional Retained Erasure Authority Implementation Plan

**Status:** Re-baselined after partial implementation

**Default:** Restore-isolated embedded SQLite
**Alternative:** `CoLocated` or `ExternalDatabase` explicit topologies for different operational constraints.

**Enterprise option:** Separately restored external PostgreSQL

## Outcome

Privacy erasure intent survives primary database backup rollback. The retained authority is outside the primary restore lifecycle, startup replay converges restored primary data before readiness, and any authority rollback or uncertainty fails closed.

## Preserved Completed Scope

Do not reimplement accepted authority contracts, erasure orchestration, startup gating, applier flow, transactional outbox behavior, replay semantics, or completed security/operations work unless topology replacement invalidates its evidence. The new work replaces storage ownership, not the established privacy outcome.

## Acceptance Criteria

1. Authority storage is single-sink per deployment mode:
   - `EmbeddedSqlite` (default): dedicated local authority file.
   - `CoLocated`: authority tables in the primary application database.
   - `ExternalDatabase`: authority in a separate external PostgreSQL database.
2. The embedded authority file lives on a dedicated persisted local volume outside primary backup/restore.
3. `ExternalDatabase` remains available using a separate PostgreSQL database and independently scoped credentials.
4. In `EmbeddedSqlite` and `ExternalDatabase`, the primary database contains only the replay checkpoint as authority state; `CoLocated` also stores retained authority rows in the primary application database.
5. `PrivacyErasureStartupGate` replays retained intent before readiness after a primary restore.
6. Primary checkpoint ahead of retained authority state fails readiness closed.
7. Embedded append, replay, compaction, restart, backup/restore, and concurrency behavior are proven against a real file.
8. Embedded mode rejects multi-replica, shared/network filesystem, invalid path, and unsafe-permission deployments.
9. External authority accepts structured privacy-prefixed database fields, never a raw connection string.
10. Runtime and migrator credentials are separate for external PostgreSQL and enforced by ACL tests.
12. Generated migration artifacts are reproducible and never hand-edited.
13. Retained intent is durably appended before destructive primary erasure is acknowledged; the primary outbox is not the first authority copy.

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
- Register the context only for `EmbeddedSqlite`.
- Initialize WAL once, use a bounded busy timeout and private cache, and avoid per-connection global pragma churn.
- Enforce one API writer/replica and local filesystem deployment.
- Create parent directory/file with restrictive permissions and verify them at startup.
- Adapt append, read, sequence, floor, replay, and compaction operations to portable EF/SQLite semantics without weakening monotonicity.
- Keep the primary and authority SQLite contexts, files, migrations, volumes, health checks, and backups distinct.

### Exit criteria

- Real-file tests prove monotonic append, bounded contention, restart durability, replay, compaction, and integrity checks.
- Invalid or unsafe file topology blocks readiness.

## Phase 10: CoLocated Alignment and Legacy Retained-Ledger Retirement

### Work

- Preserve the `CoLocated` path as a first-class topology while completing legacy cleanup for non-colocated modes.
- Ensure this phase does not introduce operator-side mode migration; it only finalizes topology contracts.
- Reduce authority footprint in `EmbeddedSqlite`/`ExternalDatabase` to replay checkpoint-only.
- Retain normal privacy saga, transactional outbox, receipt, and completion records required by primary transaction ownership.
- Remove or retire legacy retained-authority repositories, tables, functions, configuration, and tests only where they conflict with the finalized topology model.
- Ensure MigrationService is the only production migration/cutover owner.

### Exit criteria

- No retained authority ledger remains in the primary schema for `EmbeddedSqlite` and `ExternalDatabase`; `CoLocated` intentionally keeps the retained authority tables there.
- Existing retained intent is preserved and replayable from the authority file.
- Rollback procedure cannot resurrect erased data or silently fork authority history.

## Phase 11: Startup Gate and Rollback Detection

### Work

- Compare primary replay checkpoint with retained authority high-water/floor state before readiness.
- Replay idempotently when primary is behind.
- Fail closed when primary is ahead, authority storage is missing/corrupt/unavailable, permissions are unsafe, or replay cannot complete.
- Persist and validate monotonic counter/floor evidence sufficient to detect authority rollback or replacement.
- Preserve bounded startup behavior and actionable credential-safe diagnostics.

### Exit criteria

- Restoring primary alone triggers replay from untouched authority storage and converges before readiness.
- Restoring/replacing authority with an older copy blocks readiness.
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
- Revalidate previously completed security, compaction, observability, and performance evidence under both target topologies.
- Complete OREA-620, 710, and 720 with linked evidence.

### Exit criteria

- Operators can prove which authority instance and high-water state are active without exposing personal data or secrets.
- Every recovery drill has an expected failure/ready state and rollback procedure.
- All acceptance criteria and historical open items are closed.

## Testing Strategy

- Contract tests: topology validation, structured external settings, credential redaction, one-writer and path constraints.
- Embedded file tests: concurrent append, monotonic sequence/floor, busy timeout, restart, replay, compaction, corruption, permissions, backup/restore, and rollback detection.
- Primary integration: erasure transaction, saga/outbox/receipt behavior, checkpoint advancement, idempotent replay, and absence of shadow authority ledgers in non-colocated topologies.
- External PostgreSQL: real functions, ACLs, runtime/migrator separation, clean migrations, replay, backup/restore, and rollback detection.
- Recovery: primary-only rollback must replay; authority rollback must fail closed.
- Architecture: independent contexts, migration assemblies, provider registration, and no generated artifact edits.

## Security and Privacy

- Authority rows contain only the minimal pseudonymous material required to prevent resurrection.
- Keys, salts, credentials, and file permissions follow least privilege and rotation procedures.
- Logs expose topology, sequence ranges, counts, and outcomes, never raw identifiers, secrets, or connection strings.
- Embedded file copies are encrypted/protected according to deployment policy and handled as privacy-sensitive backups.
- External runtime credentials cannot perform schema ownership operations.

## Operational Boundaries

- Embedded mode is for one application writer using a durable local filesystem.
- Multi-replica or shared-storage deployments use external PostgreSQL.
- Primary and authority backups have separate jobs, retention, labels, restore commands, and drills.
- Authority compaction, backup, and restore preserve monotonic floor evidence.

## Definition of Done

- `EmbeddedSqlite`, `CoLocated`, and external PostgreSQL each satisfy acceptance criteria for their selected mode.
- Each mode is mutually exclusive and writes retained authority state to only one destination.
- Historical completed behavior remains intact.
- Startup replay and rollback detection are proven failure-closed.
- Recovery documentation and drills prove authority independence from every supported primary provider.
- Release evidence closes all open and revalidated OREA tasks.
