<!-- ABOUTME: Progress ledger for preserving privacy erasure intent outside the primary database restore lifecycle. -->
<!-- ABOUTME: Retains accepted historical work and tracks explicit authority topology settlement through release evidence. -->

# Optional Retained Erasure Authority Tasks

**Status:** Historical semantics accepted; topology settlement in progress

**Rule:** Do not uncheck completed behavior merely because its storage implementation is being replaced.

## Historical Progress Ledger

- [x] **OREA-100** Define retained authority contracts and minimal authority data.
- [x] **OREA-110** Define monotonic sequence/high-water/floor semantics.
- [x] **OREA-120** Define idempotent replay and anti-resurrection invariants.
- [x] **OREA-200** Implement privacy erasure orchestration.
- [x] **OREA-210** Implement application erasure flow and transaction boundaries.
- [x] **OREA-220** Implement transactional outbox/receipt convergence behavior.
- [x] **OREA-300** Implement authority append/read foundations.
- [ ] **OREA-310** Complete durable authority provider contract under `EmbeddedSqlite` and external PostgreSQL.
- [ ] **OREA-320** Complete replay/checkpoint boundary tests after primary ledger removal.
- [ ] **OREA-400** Settle embedded SQLite provider registration, migrations, and operational envelope.
- [ ] **OREA-410** Settle external PostgreSQL structured configuration, roles, functions, and migrations.
- [x] **OREA-420** Establish MigrationService as external authority migration owner.
- [x] **OREA-500** Implement `PrivacyErasureStartupGate` before readiness.
- [x] **OREA-510** Implement replay and checkpoint advancement.
- [x] **OREA-520** Implement failure-closed startup behavior for unsafe authority state.
- [x] **OREA-600** Establish authority security and least-data principles.
- [x] **OREA-610** Establish observability and credential/identifier redaction.
- [ ] **OREA-620** Complete independent disaster-recovery drills for target topologies.
- [x] **OREA-700** Complete initial operational documentation and accepted evidence.
- [ ] **OREA-710** Revalidate completeness after topology replacement.
- [ ] **OREA-720** Capture final release evidence for embedded and external modes.

## Phase 8: Rebaseline Contracts and Inventory

- [ ] **OREA-799** Restore a green Release build before any runtime authority edit and record unrelated fixes outside this workstream.
- [ ] **OREA-800** Settle three deployable authority modes (`CoLocated`, `EmbeddedSqlite`, `ExternalDatabase`) as mutually exclusive with one active authority storage destination.
- [ ] **OREA-801** Define embedded file path, volume, one-writer, local-filesystem, permissions, WAL, private-cache, busy-timeout, and integrity constraints.
- [ ] **OREA-802** Define privacy-prefixed external structured fields aligned with MDB-101.
- [ ] **OREA-803** Restrict external authority provider to PostgreSQL initially.
- [ ] **OREA-804** Remove raw authority connection-string and free-form fragment contracts.
- [ ] **OREA-805** Inventory co-located tables, functions, repositories, migrations, configuration, and tests.
- [ ] **OREA-806** Classify primary records as replay checkpoint, normal saga/outbox/receipt state, or duplicated authority ledger.
- [ ] **OREA-807** Complete and evidence OREA-310 under the settled topologies.
- [ ] **OREA-807A** Define and test a stable idempotent handoff that durably appends authority intent before primary erasure can be acknowledged.
- [ ] **OREA-808** Record phase build/test evidence from a green baseline.

## Phase 9: Embedded SQLite Authority

- [ ] **OREA-900** Add a dedicated SQLite authority DbContext and generated migration assembly.
- [ ] **OREA-901** Register it only for `EmbeddedSqlite`.
- [ ] **OREA-902** Create/validate the dedicated directory and file with restrictive permissions.
- [ ] **OREA-903** Initialize WAL once with bounded busy timeout and private cache.
- [ ] **OREA-904** Implement monotonic append/read/high-water/floor behavior using SQLite-safe transactions.
- [ ] **OREA-905** Implement replay and compaction without weakening retained-floor semantics.
- [ ] **OREA-906** Enforce one API writer/replica and reject network/shared filesystem claims.
- [ ] **OREA-907** Keep primary and authority SQLite files, contexts, migrations, volumes, health checks, and backups separate.
- [ ] **OREA-908** Add real-file append concurrency, restart, replay, compaction, permissions, integrity, and backup/restore tests.
- [ ] **OREA-909** Complete and evidence OREA-400.
- [ ] **OREA-910** Record phase build/test evidence.

## Phase 10: Co-Located Cutover and Primary Ledger Removal

- [ ] **OREA-1000** Design the generated migration/export path from deployed co-located authority state to embedded SQLite.
- [ ] **OREA-1001** Make cutover idempotent, observable, restartable, and rollback-safe.
- [ ] **OREA-1002** Prove every retained intent row transfers with sequence/floor integrity.
- [ ] **OREA-1003** Reduce primary authority-specific state to the replay checkpoint.
- [ ] **OREA-1004** Retain normal saga, transactional outbox, receipt, and completion records required by primary transaction ownership.
- [ ] **OREA-1005** Remove/retire co-located authority tables, functions, repositories, configuration, and tests only after cutover evidence.
- [ ] **OREA-1006** Keep MigrationService as the only production migration/cutover owner.
- [ ] **OREA-1007** Prove rollback cannot fork authority history or resurrect erased data.
- [ ] **OREA-1008** Record phase build/test evidence.

## Phase 11: Startup Replay and Rollback Detection

- [ ] **OREA-1100** Compare primary replay checkpoint with retained high-water/floor before readiness.
- [ ] **OREA-1101** Replay missing intent idempotently when primary is behind.
- [ ] **OREA-1101A** Prove authority append succeeds before primary erasure acknowledgement and that primary failure after append converges safely by retry/replay.
- [ ] **OREA-1102** Fail readiness when primary checkpoint is ahead of authority state.
- [ ] **OREA-1103** Fail readiness for missing, corrupt, unavailable, unsafe-permission, or unreplayable authority storage.
- [ ] **OREA-1104** Add counter/floor evidence sufficient to detect authority rollback or replacement.
- [ ] **OREA-1105** Prove primary-only restore converges from untouched authority storage.
- [ ] **OREA-1106** Prove older authority restore/replacement fails closed.
- [ ] **OREA-1107** Complete and evidence OREA-320.
- [ ] **OREA-1108** Record phase build/test evidence.

## Phase 12: External PostgreSQL Completion

- [ ] **OREA-1200** Bind privacy-prefixed structured PostgreSQL settings and build Npgsql strings internally.
- [ ] **OREA-1201** Remove external raw `--connection` and environment escape hatches.
- [ ] **OREA-1202** Preserve separate runtime and migrator credentials.
- [ ] **OREA-1203** Complete PostgreSQL functions, ownership, ACLs, append/read, migration, replay, and startup behavior.
- [ ] **OREA-1204** Prove the runtime role cannot migrate or bypass authority contracts.
- [ ] **OREA-1205** Prove independent external backup/restore and rollback detection.
- [ ] **OREA-1206** Complete and evidence OREA-410.
- [ ] **OREA-1207** Record phase build/test evidence.

## Phase 13: Deployment, Recovery, and Release

- [ ] **OREA-1300** Add a dedicated embedded authority volume to Aspire, Compose, and deployment manifests.
- [ ] **OREA-1301** Ensure primary backup/restore jobs exclude the authority file/volume.
- [ ] **OREA-1302** Document embedded one-writer/local-filesystem limits and external topology selection.
- [ ] **OREA-1303** Document separate authority backup, integrity, retention, restore, and key/credential procedures.
- [ ] **OREA-1304** Run primary-only restore and replay drill.
- [ ] **OREA-1305** Run authority rollback/replacement failure-closed drill.
- [ ] **OREA-1306** Run corruption, permission, unavailable-storage, and interrupted-cutover drills.
- [ ] **OREA-1307** Run independent external PostgreSQL restore drill.
- [ ] **OREA-1308** Revalidate security, observability, compaction, and performance evidence under both topologies.
- [ ] **OREA-1309** Complete and evidence OREA-620.
- [ ] **OREA-1310** Complete and evidence OREA-710 and OREA-720.
- [ ] **OREA-1311** Run final Release build, architecture tests, file-backed authority tests, external PostgreSQL tests, and documentation-link checks.

## Settled Decisions

- [x] **OREA-D01** Retained authority must be outside the primary restore lifecycle.
- [x] **OREA-D02** Default topology is a dedicated embedded SQLite file, with `CoLocated` and `ExternalDatabase` as explicit alternate modes.
- [x] **OREA-D03** Enterprise topology is a separately restored PostgreSQL database.
- [x] **OREA-D04** Primary authority state is limited to the replay checkpoint.
- [x] **OREA-D05** Normal saga/outbox/receipt records remain in primary according to transaction ownership.
- [x] **OREA-D06** Startup replay remains mandatory before readiness.
- [x] **OREA-D07** Primary checkpoint ahead of authority fails closed.
- [x] **OREA-D08** External operator configuration uses structured fields, never raw connection strings.
- [x] **OREA-D09** Embedded mode is one writer on a durable local filesystem.
- [x] **OREA-D10** Retained authority append precedes acknowledged destructive primary erasure; the primary outbox is not the first durable authority copy.
- [ ] **OREA-D11** Existing deployment cutover and rollback procedure approved with evidence.
