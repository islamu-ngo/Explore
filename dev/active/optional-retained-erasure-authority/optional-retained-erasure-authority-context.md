<!-- ABOUTME: Resumable context for three mutually exclusive privacy-erasure authority topologies. -->
<!-- ABOUTME: Tracks five-provider CoLocated completion without changing embedded or external storage boundaries. -->

# Optional Retained Erasure Authority Context

**Status:** Five-provider `CoLocated` rebaseline; prior erasure semantics retained

**Last Updated:** 2026-08-08 (Europe/Brussels)

**Default topology:** `EmbeddedSqlite`

**Enterprise topology:** `ExternalDatabase` using PostgreSQL

**Alternative topology:** `CoLocated` on any supported primary provider

**Related workstream:** [Multi-Database Support](../multi-database-support/multi-database-support-plan.md)

## SESSION PROGRESS (2026-08-08 Europe/Brussels)

### COMPLETED

- Re-baselined `CoLocated` as one topology supporting PostgreSQL, SQLite, SQL
  Server, MariaDB, and MySQL.
- Preserved `EmbeddedSqlite`, `CoLocated`, and `ExternalDatabase` as the only
  three mutually exclusive modes.
- Settled `DATABASE_SCHEMA` for PostgreSQL/SQL Server and fixed `ie_` names for
  SQLite/MariaDB/MySQL, with no configurable prefix.
- Added OREA-1010 through OREA-1018 for the remaining five-provider work.

### IN PROGRESS

- None. Implementation is paused at the session handoff boundary.

### NEXT

1. Read this context and the Phase 10 task ledger; open only the Phase 10 plan
   section and referenced provider-composition files.
2. Start OREA-1010 by converging the co-located authority model/context across
   all five providers without changing the embedded or external contracts.
3. Continue with OREA-1011 only after the provider-neutral model boundary is
   settled and its focused tests are updated.

### BLOCKERS

- No unresolved architecture decision blocks OREA-1010.
- The worktree contains uncommitted runtime, generated migration, test, intent,
  and operator-documentation changes. Preserve and reconcile them; do not
  delete, regenerate, or overwrite those artifacts until ownership and current
  acceptance are verified.

## Objective

Prevent erased personal data from reappearing after a primary database restore when a restore-isolated topology is selected. `EmbeddedSqlite` and `ExternalDatabase` retain monotonic erasure intent outside the primary restore lifecycle; `CoLocated` supports PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL and deliberately trades restore isolation for atomic one-database backup and restore. Before readiness, `PrivacyErasureStartupGate` replays the selected authority state and fails closed when that topology's safety contract cannot be established.

The default authority is a dedicated SQLite file such as `/app/data/privacy_erasure_authority.db` on its own durable volume. Restoring the primary database must not restore, replace, or roll back that file. Enterprises may instead use a separately backed-up and restored PostgreSQL authority database.

## Historical Implementation Evidence

The preceding workstream delivered and accepted substantial behavior that remains valid:

- Authority contracts, monotonic sequencing/floor semantics, and replay rules.
- Privacy erasure orchestration and application flow.
- `PrivacyErasureStartupGate` replay before readiness.
- Erasure applier behavior and the primary transactional outbox used for ordinary post-commit convergence; the topology rebaseline must not use that outbox as the first durable copy of retained authority intent.
- Startup, replay, compaction, security, and much of the operational evidence.
- External PostgreSQL authority foundations, functions, ACL direction, and migration ownership through MigrationService.

Completed task IDs retained as historical evidence: OREA-100, 110, 120, 200, 210, 220, 300, 420, 500, 510, 520, 600, 610, and 700. OREA-310 and 320 remain partially open. OREA-400/410, 620, 710, and 720 remain open or require revalidation.

- `CoLocated` is now an explicit operator choice and is treated as a first-class storage topology, not as an incidental fallback.

## Verified Current State

- Current authority topologies are `CoLocated`, `EmbeddedSqlite`, and `ExternalDatabase`.
- Co-located authority currently works only in the primary PostgreSQL schema or
  primary SQLite file. Runtime DI and MigrationService reject SQL Server,
  MariaDB, and MySQL; completing those providers is the active rebaseline.
- External authority uses a dedicated PostgreSQL context plus PostgreSQL functions and ACLs.
- External authority migrations are owned by `Event.MigrationService` only when that topology is selected.
- `PrivacyErasureStartupGate` always replays before readiness.
- `PrivacyErasureApplier` writes the normal erasure outbox in the same primary transaction as erasure. Current post-commit authority delivery is insufficient for restore safety because the primary could be lost before dispatch; the rebaseline must replace that durability ordering.
- The primary database currently duplicates more authority intent than the re-baselined design permits.
- Runtime SQLite authority is implemented for both the dedicated embedded file
  and the co-located primary SQLite file.

## Settled Topology

### Embedded SQLite, default

- Dedicated context and generated migration assembly.
- Persisted file, defaulting to `/app/data/privacy_erasure_authority.db`.
- Dedicated durable local volume excluded from primary database backup and restore.
- One API writer/replica only; no network filesystem or shared-file multi-replica deployment.
- WAL initialized once, bounded busy timeout, private cache, restrictive file and directory permissions.
- Authority backup/restore, integrity checking, retention, and rollback detection are independent of the primary database.
- File-backed tests cover append concurrency, restart, replay, compaction, backup/restore, corruption response, and rollback detection.

### External PostgreSQL, enterprise

- Dedicated server database with independent backup and restore.
- PostgreSQL remains the initial provider because existing authority functions, ownership, and ACL contracts are PostgreSQL-specific.
- Runtime append/read credentials and migrator/owner credentials remain separate.
- Operators bind the same privacy-prefixed structured shape separately for runtime and migrator roles: provider, host, port, database, username, password, TLS mode/trust, and bounded settings. No raw connection string or free-form fragment is accepted.

### CoLocated

- Authority ledger and replay checkpoint both live in the selected primary
  PostgreSQL, SQLite, SQL Server, MariaDB, or MySQL database.
- PostgreSQL and SQL Server use `DATABASE_SCHEMA`, defaulting to
  `islamu_event`; SQLite, MariaDB, and MySQL use the fixed `ie_` prefix.
- Provider, connection, credentials, lifecycle, and backup ownership come from
  the primary database. No co-located authority connection or prefix setting is
  accepted.
- The same anti-resurrection and checkpoint invariants apply; mutating `ExternalDatabase`/`EmbeddedSqlite` state is not permitted while this mode is active.

## Primary Database Boundary

Authority-specific primary state is limited to the replay checkpoint in `EmbeddedSqlite` and `ExternalDatabase`. In `CoLocated`, the primary database additionally owns the retained authority ledger. Exactly one topology destination is active: embedded file, external authority database, or primary database.

Normal application records remain in the primary database when transaction ownership requires them, including erasure saga state, transactional outbox messages, dispatch receipts, and domain-side completion evidence. They must not become a second retained authority ledger.

## Authority-First Durability

The retained authority must hold an idempotent erasure intent before destructive primary erasure is acknowledged as successful. The flow uses a stable intent identifier:

1. Append the intent durably to the selected retained authority.
2. Apply primary erasure idempotently and advance the replay checkpoint.
3. Report success only after primary convergence is committed.

If authority append succeeds and primary erasure fails, retry or startup replay completes the primary erasure. This conservative ordering may retain an intent for work not yet applied, but it cannot acknowledge an erasure whose only durable intent can disappear with a primary restore. The primary outbox may continue to drive unrelated post-commit side effects; it is never the first durable authority copy.

## Restore and Replay Invariants

1. Authority sequence/counter/floor values are monotonic.
2. Retained intent is durable before primary erasure is acknowledged.
3. Primary restore alone leaves authority storage untouched only for
   `EmbeddedSqlite` and `ExternalDatabase`; `CoLocated` shares the primary
   restore lifecycle.
4. On startup, the gate compares the primary replay checkpoint with retained authority state.
5. If primary is behind, replay reapplies retained erasure intent idempotently and advances the checkpoint.
6. If primary checkpoint is ahead of authority state, readiness fails closed because authority rollback or replacement is possible.
7. Authority corruption, unavailable storage, invalid permissions, or failed replay blocks readiness.
8. Compaction cannot remove evidence needed to prevent resurrection at or below the retained floor.
9. Authority restore is an explicit independent operation for the
   restore-isolated topologies and must pass rollback/floor checks before the
   service resumes; `CoLocated` is restored atomically with primary data.

## Configuration Contract

Embedded mode accepts a topology and a file path plus bounded SQLite operational settings owned by code. Server fields are invalid in embedded mode. `CoLocated` accepts no separate authority target and derives its provider, connection, credentials, and namespace from the primary database configuration. `DATABASE_SCHEMA` applies to PostgreSQL and SQL Server; `ie_` is fixed for SQLite, MariaDB, and MySQL.

External mode uses the same provider-neutral structured database model defined by the multi-database workstream, under privacy-erasure-specific keys. Only `PostgreSql` is valid initially. Runtime and migrator credential sets are distinct. Internally derived connection strings may be passed to provider APIs but are never operator inputs or logged values.

## Migration Ownership

- MigrationService is the only production migration owner.
- Embedded SQLite authority migrations target only the dedicated authority file.
- Co-located authority migrations are generated per primary provider and target
  the same physical primary database through a distinct authority migration
  history. PostgreSQL and SQL Server place both history and tables in the
  configured schema; SQLite, MariaDB, and MySQL use fixed `ie_` names.
- External PostgreSQL authority migrations target only the external authority database.
- Application migrations retain only the replay checkpoint for authority state;
  selected co-located authority migrations separately own the retained ledger
  in the same physical primary database.
- Generated migrations and model snapshots are never hand-edited.
- Pre-v1 topology changes use reset-only development guidance; no deployed-data
  compatibility or automatic cross-mode export is maintained.
- In `CoLocated` mode, co-located retention artifacts are intentionally retained and remain the active authority source.

## Open Work

- Finish durable provider contract settlement and replay boundary tests from OREA-310/320.
- Replace PostgreSQL-specific/co-located-SQLite composition with one
  provider-neutral co-located context and repository for all five providers.
- Generate and wire co-located SQL Server, MariaDB, and MySQL migrations; align
  PostgreSQL and SQLite with the same composition contract.
- Prove the full real-engine co-located behavior matrix, including idempotent
  double migration, transactional monotonic allocation, namespace placement,
  restart, replay, and exactly one active authority sink.
- Enforce mode-boundary exclusivity in topology, DI, contexts, migrations, configuration, health/readiness, and operations (`CoLocated` writes only primary; others write only non-primary authority stores).
- Complete recovery evidence for each topology without introducing cross-mode
  migration compatibility.
- Complete external PostgreSQL credential/migration settlement using structured fields.
- Prove independent disaster recovery and failure-closed rollback detection.
- Re-run completeness and release evidence after topology replacement.

## Main Risks

| Risk | Required control |
|---|---|
| Primary restore also restores authority file | Dedicated volume and separate runbook |
| SQLite file rollback or replacement | Counter/floor comparison and failure-closed readiness |
| Multi-replica file contention | One writer/replica validation; local filesystem only |
| Non-colocated primary retains a shadow ledger | Schema and flow review; only replay checkpoint is authority state outside `CoLocated` |
| Provider SQL diverges | Provider-portable EF operations plus real-engine contract tests |
| Counter allocation races | Existing provider-specific `RelationalNamedLock` inside one transaction |
| MySQL/MariaDB session lock leaks | Existing transaction-completion lock-release interceptor and failure tests |
| Migration histories collide | Dedicated authority migration assembly/history per provider |
| External credentials gain schema ownership | Separate runtime and migrator roles plus ACL tests |
| Outbox mistaken for authority | Restrict it to ordinary post-commit side effects and prove authority-first durability |

## Handoff Guidance

Resume from OREA-1010 in `optional-retained-erasure-authority-tasks.md`. Preserve OREA-1000 through OREA-1006 as evidence of the delivered PostgreSQL/SQLite slice; they do not prove five-provider completion. Reuse `PrimaryDatabaseProviderComposition`, `RelationalModelNamespace`, `RelationalNamedLock`, and `MySqlModelIdentifierPolicy` rather than adding provider repositories. Keep the embedded and external implementations separate. No backward-compatible topology migration is required before v1.

## Handoff Notes

### Handoff — 2026-08-08 Europe/Brussels

- **Current state:** Planning is re-baselined and OREA-1009 is complete.
  Runtime currently supports co-located PostgreSQL/SQLite; SQL Server, MariaDB,
  and MySQL remain planned work.
- **Next action:** Implement OREA-1010, then keep `tasks.md` current before
  moving to the provider-neutral repository in OREA-1011.
- **Blockers:** None in the design. The uncommitted worktree must be reconciled
  before regenerating EF migrations or modifying overlapping files.
- **Modified planning files:** `optional-retained-erasure-authority-plan.md`,
  `optional-retained-erasure-authority-context.md`, and
  `optional-retained-erasure-authority-tasks.md`.
- **Existing dirty implementation surfaces to preserve:**
  `PersistenceServicesRegistration`, `PrimaryDatabaseProviderComposition`,
  MigrationService registration/worker, privacy-erasure contexts,
  configurations, repositories, generated SQLite/PostgreSQL migrations,
  architecture/persistence tests, configuration/self-hosting docs, and the
  privacy-erasure intent contract. Unrelated dirty event-host and API snapshot
  files must also be left untouched.
- **Validation:** Planning-only `git diff --check --
  dev/active/optional-retained-erasure-authority` passed. No runtime build,
  provider migration, or test suite was run for this handoff.
- **Documentation impact:** Five-provider support is planned, not yet a runtime
  or operator-documentation completion claim.
- **Risks:** Provider-specific EF mappings and generated migrations can drift;
  MySQL/MariaDB named-lock release and concurrent monotonic allocation require
  real-engine evidence.
- **Notes for next contributor/agent:** Reuse `RelationalModelNamespace`,
  `RelationalNamedLock`, and `MySqlModelIdentifierPolicy`; keep `ie_` fixed and
  keep exactly one authority sink active. Never hand-edit generated migrations.

## Verification Constraint

This planning-only rebaseline did not run runtime verification. Runtime implementation must begin with the canonical green Release build, then use the intent's project-scoped test matrix and real provider engines. Provider model creation alone is not completion evidence.
