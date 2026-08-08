<!-- ABOUTME: Progress ledger for three mutually exclusive privacy-erasure authority topologies. -->
<!-- ABOUTME: Tracks five-provider CoLocated completion while retaining accepted historical work. -->

# Optional Retained Erasure Authority Tasks

**Status:** Historical semantics accepted; five-provider `CoLocated` completion in progress

**Last Updated:** 2026-08-08 (Europe/Brussels)

**Rule:** Do not uncheck completed behavior merely because its storage implementation is being replaced.

## Status Summary

- **Overall status:** Re-baselined; implementation paused for session handoff.
- **Completed rebaseline:** OREA-1009.
- **Current priority:** OREA-1010.
- **Next recommended slice:** Provider-neutral co-located authority model and
  context for all five primary providers; do not start migration regeneration
  until that model is settled.
- **Known worktree condition:** Uncommitted implementation, migration, test,
  intent, and documentation changes overlap Phase 10. Preserve and reconcile
  them before editing.
- **Handoff validation:** Planning-only `git diff --check` passed; no runtime
  build or test command was run for the handoff.

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
- [ ] **OREA-720** Capture final release evidence for all three modes.

## Phase 8: Rebaseline Contracts and Inventory

- [ ] **OREA-799** Restore a green Release build before any runtime authority edit and record unrelated fixes outside this workstream.
- [x] **OREA-800** Settle three deployable authority modes (`CoLocated`, `EmbeddedSqlite`, `ExternalDatabase`) as mutually exclusive with one active authority storage destination.
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

- [x] **OREA-900** Add a dedicated SQLite authority DbContext and clean generated migration assembly with fixed `ie_` names.
- [x] **OREA-901** Register the SQLite authority context for `EmbeddedSqlite` and co-located primary SQLite, while registering dedicated storage only for `EmbeddedSqlite`.
- [x] **OREA-902** Create/validate the dedicated directory and file with restrictive permissions.
- [x] **OREA-903** Initialize WAL once with bounded busy timeout and private cache.
- [ ] **OREA-904** Implement monotonic append/read/high-water/floor behavior using SQLite-safe transactions.
- [ ] **OREA-905** Implement replay and compaction without weakening retained-floor semantics.
- [ ] **OREA-906** Enforce one API writer/replica and reject network/shared filesystem claims.
- [ ] **OREA-907** Keep primary and authority SQLite files, volumes, health checks, and backups separate for `EmbeddedSqlite`; deliberately share only the physical file for `CoLocated` while keeping contexts and migration histories separate.
- [ ] **OREA-908** Add real-file append concurrency, restart, replay, compaction, permissions, integrity, and backup/restore tests.
- [ ] **OREA-909** Complete and evidence OREA-400.
- [ ] **OREA-910** Record phase build/test evidence.

## Phase 10: Co-Located Provider Alignment and Single-Sink Enforcement

- [x] **OREA-1000** Deliver the historical PostgreSQL/SQLite `CoLocated` slice as a first-class primary-database authority sink.
- [x] **OREA-1001** Add the historical dedicated co-located PostgreSQL context, generated migration, and direct primary-credential repository.
- [x] **OREA-1002** Add the historical co-located SQLite path with fixed `ie_` tables and a distinct migration history.
- [x] **OREA-1003** Apply configurable `DATABASE_SCHEMA` to PostgreSQL and reject any configurable database prefix in the delivered slice.
- [x] **OREA-1004** Register exactly one authority adapter and no external/embedded authority credential or storage surface in `CoLocated`.
- [x] **OREA-1005** Keep the primary application model checkpoint-only outside the selected co-located authority context.
- [x] **OREA-1006** Keep MigrationService as the only production migration owner for the selected topology.
- [ ] **OREA-1007** Prove co-located atomic backup/restore behavior and report `restoreReplayProtection=false` without claiming restore isolation.
- [ ] **OREA-1008** Record phase build/test evidence.
- [x] **OREA-1009** Rebaseline `CoLocated` acceptance to PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL while preserving exactly three mutually exclusive topologies.
- [ ] **OREA-1010** Replace provider-specific co-located contexts/configurations with one provider-neutral authority model that applies `DATABASE_SCHEMA` to PostgreSQL/SQL Server, fixed `ie_` names to SQLite/MariaDB/MySQL, and the existing MySQL identifier policy.
- [ ] **OREA-1011** Replace PostgreSQL SQL and co-located SQLite special cases with one EF repository using the existing transaction-scoped `RelationalNamedLock`, including MySQL/MariaDB lock release.
- [ ] **OREA-1012** Generate dedicated co-located migration lanes for all five providers, keep embedded SQLite migrations separate, and regenerate every unapplied development artifact with `dotnet ef` rather than hand-editing it.
- [ ] **OREA-1013** Make runtime DI and MigrationService select the same five-provider co-located context/repository and remove the three provider rejection branches.
- [ ] **OREA-1014** Add fast composition, namespace, model, migration-ownership, topology-exclusivity, and architecture tests for all five providers.
- [ ] **OREA-1015** Run the real-engine co-located matrix: migrate twice, append/retry/mismatch/concurrency/read ordering, replay, restart, namespace, and exactly-one-sink checks on all five providers.
- [ ] **OREA-1016** Prove provider-native atomic co-located backup/restore for all five providers and report `restoreReplayProtection=false`.
- [ ] **OREA-1017** Converge intent acceptance, configuration schema, `.env.example`, operator docs, troubleshooting, testing, backup/restore, and self-hosting guidance on five-provider co-located support and fixed `ie_` behavior.
- [ ] **OREA-1018** Record Phase 10 build, required project-test, real-engine, migration-generation, and documentation evidence.

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
- [ ] **OREA-1308** Revalidate security, observability, compaction, and performance evidence under all three topologies.
- [ ] **OREA-1309** Complete and evidence OREA-620.
- [ ] **OREA-1310** Complete and evidence OREA-710 and OREA-720.
- [ ] **OREA-1311** Run final Release build, architecture tests, file-backed authority tests, external PostgreSQL tests, and documentation-link checks.

## Settled Decisions

- [x] **OREA-D01** `EmbeddedSqlite` and `ExternalDatabase` authority must be outside the primary restore lifecycle; `CoLocated` intentionally shares it.
- [x] **OREA-D02** Default topology is a dedicated embedded SQLite file, with `CoLocated` and `ExternalDatabase` as explicit alternate modes.
- [x] **OREA-D03** Enterprise topology is a separately restored PostgreSQL database.
- [x] **OREA-D04** Primary authority state is limited to the replay checkpoint except in `CoLocated`, where the primary database is the selected authority sink.
- [x] **OREA-D05** Normal saga/outbox/receipt records remain in primary according to transaction ownership.
- [x] **OREA-D06** Startup replay remains mandatory before readiness.
- [x] **OREA-D07** Primary checkpoint ahead of authority fails closed.
- [x] **OREA-D08** External operator configuration uses structured fields, never raw connection strings.
- [x] **OREA-D09** Embedded mode is one writer on a durable local filesystem.
- [x] **OREA-D10** Retained authority append precedes acknowledged destructive primary erasure; the primary outbox is not the first durable authority copy.
- [x] **OREA-D11** Pre-v1 provider/topology changes use reset-only development guidance; no automatic cross-mode migration or backward-compatibility shim is required.
- [x] **OREA-D12** `CoLocated` supports every primary provider: PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL.
- [x] **OREA-D13** PostgreSQL and SQL Server use `DATABASE_SCHEMA` with default `islamu_event`; SQLite, MariaDB, and MySQL use fixed `ie_`, with no configurable prefix.
- [x] **OREA-D14** Co-located monotonic allocation reuses one provider-neutral repository and the existing `RelationalNamedLock` instead of adding provider SQL repositories.
- [x] **OREA-D15** `ExternalDatabase` remains PostgreSQL-only and `EmbeddedSqlite` remains a dedicated SQLite file; expanding `CoLocated` does not expand those provider contracts.
