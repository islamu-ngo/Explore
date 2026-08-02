<!-- ABOUTME: Resumable context for a retained privacy-erasure authority that survives primary database restore. -->
<!-- ABOUTME: Preserves completed erasure work while re-baselining authority storage to embedded SQLite or external PostgreSQL. -->

# Optional Retained Erasure Authority Context

**Status:** Re-baselined topology; prior erasure semantics retained

**Default topology:** `EmbeddedSqlite`

**Enterprise topology:** `ExternalDatabase` using PostgreSQL

**Related workstream:** [Multi-Database Support](../multi-database-support/multi-database-support-plan.md)

## Objective

Prevent erased personal data from reappearing after a primary database restore. A retained authority records monotonic erasure intent outside the primary restore lifecycle. Before readiness, `PrivacyErasureStartupGate` replays authority state into the primary database and fails closed if safety cannot be established.

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

The old `CoLocated` PostgreSQL topology is no longer the target. Its code and tests are evidence of implemented semantics, not the desired storage boundary.

## Verified Current State

- Current authority topologies are `CoLocated` and `ExternalDatabase`.
- Co-located authority writes append state in the primary PostgreSQL database but not in the erasure transaction itself.
- External authority uses a dedicated PostgreSQL context plus PostgreSQL functions and ACLs.
- External authority migrations are owned by `Event.MigrationService` only when that topology is selected.
- `PrivacyErasureStartupGate` always replays before readiness.
- `PrivacyErasureApplier` writes the normal erasure outbox in the same primary transaction as erasure. Current post-commit authority delivery is insufficient for restore safety because the primary could be lost before dispatch; the rebaseline must replace that durability ordering.
- The primary database currently duplicates more authority intent than the re-baselined design permits.
- There is no runtime SQLite authority implementation today.

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

## Primary Database Boundary

Authority-specific primary state is limited to the replay checkpoint needed to prove convergence. The retained authority ledger itself exists only in the embedded file or external authority database.

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
3. Primary restore alone leaves authority storage untouched.
4. On startup, the gate compares the primary replay checkpoint with retained authority state.
5. If primary is behind, replay reapplies retained erasure intent idempotently and advances the checkpoint.
6. If primary checkpoint is ahead of authority state, readiness fails closed because authority rollback or replacement is possible.
7. Authority corruption, unavailable storage, invalid permissions, or failed replay blocks readiness.
8. Compaction cannot remove evidence needed to prevent resurrection at or below the retained floor.
9. Authority restore is an explicit independent operation and must pass rollback/floor checks before service resumes.

## Configuration Contract

Embedded mode accepts a topology and a file path plus bounded SQLite operational settings owned by code. Server fields are invalid in embedded mode.

External mode uses the same provider-neutral structured database model defined by the multi-database workstream, under privacy-erasure-specific keys. Only `PostgreSql` is valid initially. Runtime and migrator credential sets are distinct. Internally derived connection strings may be passed to provider APIs but are never operator inputs or logged values.

## Migration Ownership

- MigrationService is the only production migration owner.
- Embedded SQLite authority migrations target only the authority file.
- External PostgreSQL authority migrations target only the external authority database.
- The primary provider's migrations retain only the replay checkpoint for authority state.
- Generated migrations and model snapshots are never hand-edited.
- Existing deployed co-located data needs an explicit generated transition/export strategy; do not silently discard retained intent.

## Open Work

- Finish durable provider contract settlement and replay boundary tests from OREA-310/320.
- Replace `CoLocated` with `EmbeddedSqlite` in topology, DI, contexts, migrations, configuration, health/readiness, and operations.
- Migrate or export existing co-located retained intent into the embedded file before removing the primary ledger.
- Complete external PostgreSQL credential/migration settlement using structured fields.
- Prove independent disaster recovery and failure-closed rollback detection.
- Re-run completeness and release evidence after topology replacement.

## Main Risks

| Risk | Required control |
|---|---|
| Primary restore also restores authority file | Dedicated volume and separate runbook |
| SQLite file rollback or replacement | Counter/floor comparison and failure-closed readiness |
| Multi-replica file contention | One writer/replica validation; local filesystem only |
| Primary retains a shadow ledger | Schema and flow review; only replay checkpoint is authority state |
| Existing co-located intent is lost | Explicit export/cutover and rollback procedure |
| External credentials gain schema ownership | Separate runtime and migrator roles plus ACL tests |
| Outbox mistaken for authority | Restrict it to ordinary post-commit side effects and prove authority-first durability |

## Handoff Guidance

Resume from the new OREA-800 rebaseline phase in `optional-retained-erasure-authority-tasks.md`. Keep historical completed boxes checked. Reopen only tasks whose evidence depended on `CoLocated`. Coordinate configuration naming and SQLite package/version choices with the multi-database workstream, but do not merge the two DbContexts, files, migration assemblies, or restore lifecycles.

## Verification Constraint

This rebaseline began with a pre-existing red Release build containing 13 unrelated Infrastructure and Blazor Client compile errors. Runtime implementation must begin from a green baseline. At each implementation phase end, use the bounded build/test cadence from the implementation-plan workflow.
