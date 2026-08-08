<!-- ABOUTME: Execution ledger for multi-database support across configuration, providers, migrations, deployment, and evidence. -->
<!-- ABOUTME: Keeps implementation progress synchronized with the multi-database context and plan. -->

# Multi-Database Support Tasks

**Last Updated:** 2026-08-08 Europe/Brussels

**Status:** Implementation complete; post-review hardening, manual QA, and release evidence reconciled

**Rule:** Check substantial tasks immediately. Reconcile every remaining checkbox by phase end.

## Phase 0: Baseline and Inventory

- [x] **MDB-001** Restore a green Release build and record unrelated fixes outside this workstream.
- [x] **MDB-002** Inventory PostgreSQL packages, registrations, factories, migrations, SQL, functions, JSON mappings, locks, indexes, health checks, deployment settings, and tests.
- [x] **MDB-003** Classify every inventory item as portable, capability-specific, PostgreSQL enhancement, or unsupported.
- [x] **MDB-004** Record application and Data Protection migration histories for every deployed environment.
- [x] **MDB-005** Obtain explicit approval before any disposable pre-v1 migration-history reset; otherwise plan generated forward migrations.
- [x] **MDB-006** Record the phase verification evidence in this ledger.

Phase 0 evidence: Release build exits 0 after isolated Refit 14 and Microsoft.OpenApi 2.7.5 compatibility repairs; five inventory lanes recorded current runtime, design-time, migration, provider-specific, test, and deployment surfaces. The product is pre-v1 and the user explicitly approved removing obsolete development-only compatibility and migration history.

## Phase 1: Structured Configuration

- [x] **MDB-100** Define the closed primary provider enum.
- [x] **MDB-101** Define structured fields for provider, host, port, database/path, schema, username, password, TLS mode, certificate trust, and bounded server version/flavor, instantiated for distinct runtime and migrator roles on server providers.
- [x] **MDB-102** Define validation matrices for server providers and primary SQLite.
- [x] **MDB-103** Implement provider-native connection-string construction without manual credential concatenation.
- [x] **MDB-104** Add credential-safe startup diagnostics and redaction tests.
- [x] **MDB-105** Route runtime persistence registration through the shared structured binder and builder.
- [x] **MDB-106** Route `ExploreDbContextFactory` and `DataProtectionKeyContextFactory` through the same logic.
- [x] **MDB-107** Route MigrationService and test fixtures through the same logic.
- [x] **MDB-108** Verify the shared structured vocabulary is consumed by OREA-802 while OREA-804 and OREA-1201 own authority runtime escape-hatch removal.
- [x] **MDB-109** Remove hardcoded localhost database strings and the legacy `POSTGRESQL_PUBLIC_URL` mapping.
- [x] **MDB-110** Ensure `ConnectionStrings:*` values are derived process-local outputs only.
- [x] **MDB-111** Prove invalid combinations fail before readiness.
- [x] **MDB-112** Record phase verification evidence.

Phase 1 evidence: configuration, bootstrap, API consumer, diagnostic, factory, composition, redaction, and external-process checks are recorded under `.omo/evidence/mdb-phase1-*`, `.omo/evidence/mdb-api-fixture-config/`, and `.omo/evidence/MDB-702/`.

## Phase 2: Provider Composition and Model Portability

- [x] **MDB-200** Add closed startup provider selection and DI registration.
- [x] **MDB-201** Audit mappings for timestamps, UUIDv7, JSON, decimals, generated values, collations, indexes, constraints, tenant filters, and soft delete.
- [x] **MDB-202** Replace avoidable PostgreSQL-only mappings with portable EF Core constructs.
- [x] **MDB-203** Introduce only evidence-backed provider capability seams.
- [x] **MDB-204** Preserve PostgreSQL-native defenses behind PostgreSQL implementations.
- [x] **MDB-205** Enforce configurable PostgreSQL/SQL Server schema with default `islamu_event`, and fixed `ie_` prefix for schema-less providers.
- [x] **MDB-206** Verify repositories still return entities and read-only paths remain no-tracking.
- [x] **MDB-207** Record architecture and phase verification evidence.

Phase 2 evidence: five-provider composition/model checks, repository invariants, portable model normalization, runtime PostgreSQL-token audits, and final domain/projection portability checks are recorded in `.omo/evidence/mdb-composition-tests/`, `.omo/evidence/mdb-phase2-model-20260802/`, `.omo/evidence/mdb-portability-locks-recheck/`, and the provider migration verification ledger.

## Phase 3: PostgreSQL

- [x] **MDB-300** Move Npgsql registration into the structured provider path.
- [x] **MDB-301** Generate or retain PostgreSQL application migrations under the approved history policy.
- [x] **MDB-302** Generate or retain PostgreSQL Data Protection migrations under the approved history policy.
- [x] **MDB-303** Prove clean migration and supported upgrade migration.
- [x] **MDB-304** Run the shared behavioral contract and PostgreSQL-native defense tests.
- [x] **MDB-305** Prove current PostgreSQL deployment inputs can migrate to structured fields without credential exposure.
- [x] **MDB-306** Record phase verification evidence.

Phase 3 evidence: default/custom-schema migrations, second-run idempotence, catalog inspection, PostgreSQL-native constraints, runtime smoke, shared behavior, email/fanout/ATProto/group repository suites, and search-path ownership are recorded in `.omo/evidence/mdb-provider-migrations/` and `.omo/evidence/mdb-provider-locks/`.

## Phase 4: Primary SQLite

- [x] **MDB-400** Add primary SQLite registration and a dedicated application migration assembly.
- [x] **MDB-401** Add a dedicated SQLite Data Protection migration assembly.
- [x] **MDB-402** Require a persisted local primary file and reject unsupported host/network/multi-replica configurations.
- [x] **MDB-403** Configure bounded busy timeout and WAL behavior for the documented envelope.
- [x] **MDB-404** Resolve schema, concurrency, SQL, JSON, and locking differences minimally.
- [x] **MDB-405** Keep primary and authority SQLite files, contexts, migrations, volumes, and recovery paths distinct.
- [x] **MDB-406** Prove clean migration, restart persistence, transactions, tenant isolation, outbox, and Data Protection on a real file.
- [x] **MDB-407** Record phase verification evidence.

Phase 4 evidence: file-backed migration/runtime behavior, WAL/path safeguards, quota/idempotency/email/hierarchy/projection contention, Data Protection restart persistence, and authority-isolated recovery are recorded in `.omo/evidence/mdb-provider-migrations/`, `.omo/evidence/mdb-402-405/`, `.omo/evidence/mdb-recovery/`, and focused portability ledgers.

## Phase 5: SQL Server

- [x] **MDB-500** Add SQL Server registration and application migrations.
- [x] **MDB-501** Add SQL Server Data Protection migrations.
- [x] **MDB-502** Map TLS and certificate trust settings explicitly.
- [x] **MDB-503** Resolve datetime, UUID, JSON, index, identifier, and transaction differences.
- [x] **MDB-504** Prove clean/upgrade migrations and the shared behavioral contract on a real SQL Server.
- [x] **MDB-505** Record phase verification evidence.

Phase 5 evidence: real SQL Server clean migration, second-run idempotence, configured-schema catalog inspection, runtime smoke, and shared behavior are recorded in `.omo/evidence/mdb-provider-migrations/`.

## Phase 6: MariaDB and MySQL

- [x] **MDB-600** Register `Microting.EntityFrameworkCore.MySql` 10.0.10.
- [x] **MDB-601** Validate required server flavor/version inputs.
- [x] **MDB-602** Generate MariaDB application and Data Protection migrations.
- [x] **MDB-603** Generate MySQL application and Data Protection migrations, combining ownership only if generated evidence proves it safe.
- [x] **MDB-604** Resolve charset/collation, identifier, datetime, JSON, generated-value, and locking differences.
- [x] **MDB-605** Prove clean/upgrade migrations and shared behavior independently on supported MariaDB and MySQL engines.
- [x] **MDB-606** Record phase verification evidence.

Phase 6 evidence: independent real MariaDB and MySQL clean migrations, second-run idempotence, catalog inspection, runtime smoke, shared behavior, index-width/hash uniqueness checks, and fixed-prefix assertions are recorded in `.omo/evidence/mdb-provider-migrations/`.

## Phase 7: Deployment, CI, Recovery, and Release

- [x] **MDB-700** Update Aspire AppHost to collect and project structured database fields.
- [x] **MDB-701** Update Compose, `.env.example`, deployment templates, and secret-store mappings.
- [x] **MDB-702** Update health checks and readiness diagnostics without credential leakage.
- [x] **MDB-703** Add PostgreSQL, file-backed SQLite, SQL Server, MariaDB, and MySQL CI lanes.
- [x] **MDB-704** Run clean real-engine migrations and a minimal runtime path in every lane.
- [x] **MDB-705** Add embedded-authority default and external-PostgreSQL authority enterprise topology coverage.
- [x] **MDB-706** Prove primary backup/restore does not replace the embedded authority file and startup replay converges restored primary state.
- [x] **MDB-707** Document provider support, migration ownership, TLS, credentials, backup/restore, upgrades, rollback, and SQLite limitations.
- [x] **MDB-708** Remove stale PostgreSQL-only and raw-connection-string instructions after replacements are verified.
- [x] **MDB-709** Run final Release build, architecture tests, provider lanes, and documentation-link checks.
- [x] **MDB-710** Capture release evidence and close every acceptance criterion (excluding unrelated concurrent blockers listed below).
- [x] **MDB-711** Remove avoidable EF internal-provider creation from the server-lock command contract and prove the focused portability tests remain green.
- [x] **MDB-712** Add real-engine projection-lock contention/release coverage to the shared provider behavior contract and exercise it against migrated file-backed SQLite.
- [x] **MDB-713** Reconcile authority-topology ownership, canonical gate results, manual QA, independent review, and residual-failure attribution across plan, tasks, and context.

Phase 7 implementation evidence: structured Aspire/Compose/deployment inputs, readiness redaction, five-provider CI matrix, exact-image MySQL health, embedded-authority recovery, and operator documentation are recorded in `.omo/evidence/mdb-authority-deployment/`, `.omo/evidence/MDB-702/`, `.omo/evidence/mdb-ci-matrix/`, `.omo/evidence/mdb-recovery/`, and `.omo/evidence/mdb-docs/`. The 2026-08-08 closeout added a green Release build, all nine canonical project-test outcomes, a production MigrationService SQLite idempotence drill, catalog/history/file-isolation inspection, a focused 10/10 portability run, and a migrated real-SQLite shared behavior contract including lock contention/release.

Post-change lock behavior was executed locally on file-backed SQLite. The same shared contract compiles for all provider lanes, but SQL Server, MariaDB, and MySQL need their next CI/provider-lane run to produce post-change server-engine evidence.

Independent re-review against `84bd22af28d48e412513cc2c233cd0ac34cb5b0b` returned **PASS**: the authority ownership blocker is resolved, the native-connection command harness is appropriate, and the two-context real-provider lock contract covers acquire/contention/release/reacquire. Its only closeout residual is the missing post-change server-engine run noted above.

## Decision and Evidence Log

- [x] **MDB-D01** Provider scope settled: PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL.
- [x] **MDB-D02** Operator input settled as structured fields; raw connection strings and fragments are forbidden.
- [x] **MDB-D03** Microting 10.0.10 selected for MariaDB/MySQL; Pomelo references are stale.
- [x] **MDB-D04** Namespace settled as operator-configurable schema (default `islamu_event`) / fixed non-configurable `ie_` prefix for schema-less providers.
- [x] **MDB-D05** Privacy authority lifecycle remains independent of the selected primary provider.
- [x] **MDB-D06** Superseded `dev/next/multi-db-support/` workstream reconciled and removed.
- [x] **MDB-D07** Migration-history reset decision recorded with deployment evidence; pre-v1 compatibility was explicitly discarded and only freshly generated histories are supported.
- [x] **MDB-D08** Final provider support matrix and release evidence linked.

## Residual Failures (Out of Scope)

- `dotnet build --configuration Release --verbosity quiet` is green with 0 errors.
- Canonical project gates outside this workstream still have failures that were not weakened or hidden:
  - Architecture: 357 passed, 5 failed, 1 skipped; failures concern later standalone transport mutability/namespaces and unrelated OpenAPI/DTO rules.
  - API: 2,165 passed, 10 failed, 1 skipped; failures concern later scheduler schema, volatile snapshots, policy/ACL, response-contract, and missing-table work.
  - Persistence at current `HEAD`: 794 passed, 169 failed, 3 skipped. EF Core 10.0.10 throws `ManyServiceProvidersCreatedWarning` after 20 cached option configurations; this single integration assembly intentionally combines five primary providers, application/Data Protection migration shapes, and authority contexts. MDB command-contract tests no longer add needless EF configurations. Fixing the remaining project-wide process/sharding policy is separate test-infrastructure work and must not suppress the production warning.
