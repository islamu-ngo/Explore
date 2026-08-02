<!-- ABOUTME: Execution ledger for multi-database support across configuration, providers, migrations, deployment, and evidence. -->
<!-- ABOUTME: Keeps implementation progress synchronized with the multi-database context and plan. -->

# Multi-Database Support Tasks

**Last Updated:** 2026-08-02 Europe/Brussels

**Status:** Phase 0 complete; Phase 1 candidate present, verification and acceptance pending

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

- [ ] **MDB-100** Define the closed primary provider enum.
- [ ] **MDB-101** Define structured fields for provider, host, port, database/path, username, password, TLS mode, certificate trust, and bounded server version/flavor, instantiated for distinct runtime and migrator roles on server providers.
- [ ] **MDB-102** Define validation matrices for server providers and primary SQLite.
- [ ] **MDB-103** Implement provider-native connection-string construction without manual credential concatenation.
- [ ] **MDB-104** Add credential-safe startup diagnostics and redaction tests.
- [ ] **MDB-105** Route runtime persistence registration through the shared structured binder and builder.
- [ ] **MDB-106** Route `ExploreDbContextFactory` and `DataProtectionKeyContextFactory` through the same logic.
- [ ] **MDB-107** Route MigrationService and test fixtures through the same logic.
- [ ] **MDB-108** Verify the shared structured vocabulary is consumed by OREA-802 while OREA-804 and OREA-1201 own authority runtime escape-hatch removal.
- [ ] **MDB-109** Remove hardcoded localhost database strings and the legacy `POSTGRESQL_PUBLIC_URL` mapping.
- [ ] **MDB-110** Ensure `ConnectionStrings:*` values are derived process-local outputs only.
- [ ] **MDB-111** Prove invalid combinations fail before readiness.
- [ ] **MDB-112** Record phase verification evidence.

Phase 1 handoff evidence: executor session `ses_03c96c432ffeACCq72heVpiQ23` wrote a candidate structured contract, builders, redaction, consumer routing, and tests. The Release build passed and Secrets tests passed 205/205. Keep MDB-100 through MDB-112 unchecked until the focused persistence tests, manual external-process QA, cleanup receipt, and independent review confirm the candidate. The full Persistence suite currently reports unrelated dirty-worktree FK and migration-count failures.

## Phase 2: Provider Composition and Model Portability

- [ ] **MDB-200** Add closed startup provider selection and DI registration.
- [ ] **MDB-201** Audit mappings for timestamps, UUIDv7, JSON, decimals, generated values, collations, indexes, constraints, tenant filters, and soft delete.
- [ ] **MDB-202** Replace avoidable PostgreSQL-only mappings with portable EF Core constructs.
- [ ] **MDB-203** Introduce only evidence-backed provider capability seams.
- [ ] **MDB-204** Preserve PostgreSQL-native defenses behind PostgreSQL implementations.
- [ ] **MDB-205** Enforce fixed `islamu_event` schema/object-prefix policy.
- [ ] **MDB-206** Verify repositories still return entities and read-only paths remain no-tracking.
- [ ] **MDB-207** Record architecture and phase verification evidence.

## Phase 3: PostgreSQL

- [ ] **MDB-300** Move Npgsql registration into the structured provider path.
- [ ] **MDB-301** Generate or retain PostgreSQL application migrations under the approved history policy.
- [ ] **MDB-302** Generate or retain PostgreSQL Data Protection migrations under the approved history policy.
- [ ] **MDB-303** Prove clean migration and supported upgrade migration.
- [ ] **MDB-304** Run the shared behavioral contract and PostgreSQL-native defense tests.
- [ ] **MDB-305** Prove current PostgreSQL deployment inputs can migrate to structured fields without credential exposure.
- [ ] **MDB-306** Record phase verification evidence.

## Phase 4: Primary SQLite

- [ ] **MDB-400** Add primary SQLite registration and a dedicated application migration assembly.
- [ ] **MDB-401** Add a dedicated SQLite Data Protection migration assembly.
- [ ] **MDB-402** Require a persisted local primary file and reject unsupported host/network/multi-replica configurations.
- [ ] **MDB-403** Configure bounded busy timeout and WAL behavior for the documented envelope.
- [ ] **MDB-404** Resolve schema, concurrency, SQL, JSON, and locking differences minimally.
- [ ] **MDB-405** Keep primary and authority SQLite files, contexts, migrations, volumes, and recovery paths distinct.
- [ ] **MDB-406** Prove clean migration, restart persistence, transactions, tenant isolation, outbox, and Data Protection on a real file.
- [ ] **MDB-407** Record phase verification evidence.

## Phase 5: SQL Server

- [ ] **MDB-500** Add SQL Server registration and application migrations.
- [ ] **MDB-501** Add SQL Server Data Protection migrations.
- [ ] **MDB-502** Map TLS and certificate trust settings explicitly.
- [ ] **MDB-503** Resolve datetime, UUID, JSON, index, identifier, and transaction differences.
- [ ] **MDB-504** Prove clean/upgrade migrations and the shared behavioral contract on a real SQL Server.
- [ ] **MDB-505** Record phase verification evidence.

## Phase 6: MariaDB and MySQL

- [ ] **MDB-600** Register `Microting.EntityFrameworkCore.MySql` 10.0.10.
- [ ] **MDB-601** Validate required server flavor/version inputs.
- [ ] **MDB-602** Generate MariaDB application and Data Protection migrations.
- [ ] **MDB-603** Generate MySQL application and Data Protection migrations, combining ownership only if generated evidence proves it safe.
- [ ] **MDB-604** Resolve charset/collation, identifier, datetime, JSON, generated-value, and locking differences.
- [ ] **MDB-605** Prove clean/upgrade migrations and shared behavior independently on supported MariaDB and MySQL engines.
- [ ] **MDB-606** Record phase verification evidence.

## Phase 7: Deployment, CI, Recovery, and Release

- [ ] **MDB-700** Update Aspire AppHost to collect and project structured database fields.
- [ ] **MDB-701** Update Compose, `.env.example`, deployment templates, and secret-store mappings.
- [ ] **MDB-702** Update health checks and readiness diagnostics without credential leakage.
- [ ] **MDB-703** Add PostgreSQL, file-backed SQLite, SQL Server, MariaDB, and MySQL CI lanes.
- [ ] **MDB-704** Run clean real-engine migrations and a minimal runtime path in every lane.
- [ ] **MDB-705** Add embedded-authority default and external-PostgreSQL authority enterprise topology coverage.
- [ ] **MDB-706** Prove primary backup/restore does not replace the embedded authority file and startup replay converges restored primary state.
- [ ] **MDB-707** Document provider support, migration ownership, TLS, credentials, backup/restore, upgrades, rollback, and SQLite limitations.
- [ ] **MDB-708** Remove stale PostgreSQL-only and raw-connection-string instructions after replacements are verified.
- [ ] **MDB-709** Run final Release build, architecture tests, provider lanes, and documentation-link checks.
- [ ] **MDB-710** Capture release evidence and close every acceptance criterion.

## Decision and Evidence Log

- [x] **MDB-D01** Provider scope settled: PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL.
- [x] **MDB-D02** Operator input settled as structured fields; raw connection strings and fragments are forbidden.
- [x] **MDB-D03** Microting 10.0.10 selected for MariaDB/MySQL; Pomelo references are stale.
- [x] **MDB-D04** Namespace settled as fixed `islamu_event` schema / `islamu_event_` prefix.
- [x] **MDB-D05** Privacy authority lifecycle remains independent of the selected primary provider.
- [x] **MDB-D06** Superseded `dev/next/multi-db-support/` workstream reconciled and removed.
- [ ] **MDB-D07** Migration-history reset decision recorded with deployment evidence.
- [ ] **MDB-D08** Final provider support matrix and release evidence linked.
