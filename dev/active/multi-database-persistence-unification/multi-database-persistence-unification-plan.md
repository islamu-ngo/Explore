<!-- ABOUTME: Implementation plan for Multi-Database Persistence Unification and API-First Architecture. -->
<!-- ABOUTME: Eliminates SQL dialect lock-in, unifies migration ownership per provider, and enables multi-provider co-located privacy authority. -->

# Multi-Database Persistence Unification & API-First Architecture — Implementation Plan

Last Updated: 2026-08-19 Europe/Brussels

## 0. Planning Metadata

- **Original Request:** Unify persistence behavior across PostgreSQL, SQL Server, MySQL, MariaDB, and SQLite; remove provider lock-in and normalize repository/migration ownership for an enterprise-grade self-hostable platform.
- **Task Directory:** `dev/active/multi-database-persistence-unification/`
- **Planning Status:** Rewritten and CTO-reviewed
- **Matched Intents:**
  - `add-ef-migration`
  - `update-repository-query`
  - `update-repository-query` for high-risk raw SQL refactors
- **Relevant Skills:**
  - `dotnet-efcore-guidelines`
  - `clean-architecture-rules`
  - `implementation-plan`
  - `senior-cto-feedback`
- **Relevant Rules:**
  - `.agents/rules/efcore-persistence.md`
  - `.agents/rules/efcore-migrations.md`
  - `.agents/rules/domain.md`
  - `docs/QUICK_REFERENCE.md`
- **Primary Layers Touched:** `Persistence`, `Migration orchestration`, `Architecture Tests`, `Integration Tests`, `Host Packaging/CI Docs`
- **Complexity:** XL — includes cross-provider persistence behavior, migration ownership, and operational documentation for self-hosting.

## 1. Senior CTO Feedback (Immediate Decision)

## Executive Verdict

This workstream is strategically correct and important, but the previous version was **not implementation-ready** because it contained stale implementation assumptions and incomplete risk control across tests/docs. I approve it **only with required changes** below.

**Decision:** Approve with required changes

### Hard CTO Constraints Applied
- No compatibility shims for already-accepted breaking-change directions.
- Clearly delete old contracts when they block simplification.
- Enforce operator recovery and failure-path clarity before touching migration ownership.

## 2. Executive Summary

### What changes this workstream enforces
1. **Collapse migration ownership** so each non-PostgreSQL provider has exactly one persistence migration assembly for both application + data-protection + co-located authority ownership.
2. **Enable co-located privacy-erasure authority across all supported providers** via provider-neutral repository/persistence patterns.
3. **Eliminate high-risk provider-specific raw SQL in persistence hotspots** (adapters and lock-heavy repositories) in favor of provider-neutral patterns where correct and practical.
4. **Re-baseline dev-doc and verification contracts** so implementation agents can continue without re-investigating assumptions.

### Why this is enterprise-grade relevant
- Self-hosted operators need predictable assembly ownership per provider and deterministic migration contracts.
- Deployment failure modes must be explicit for authority migration, data-protection readiness, and rollback behavior.
- Multi-provider topology must fail closed for unsupported topologies and validate composition decisions at startup.

## 3. Source-Grounded Current-State Report

### 3.1 Evidence Log (Verified)

| Claim | Evidence | Impact on Plan |
|---|---|---|
| Solution is `Explore.slnx`, not `Event.sln` | `Explore.slnx:23-28` includes migration projects explicitly | All solution-edit tasks must target `Explore.slnx` |
| Migration assemblies are split across 10 projects | `rg --files src | rg 'Explore.Persistence.*\.csproj'` → 10 projects | Plan now uses this exact baseline |
| Co-located authority currently supports only PostgreSQL + SQLite topologies | `src/Explore.Persistence/PersistenceServicesRegistration.cs` + `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs` | Requires explicit widening plan + tests |
| `GetMigrationsAssemblyName` currently throws for non-PG co-located authority | `PrimaryDatabaseProviderComposition.cs` + integration composition assertions | Requires plan change + contract tests update |
| `ProviderMigrationOwnershipTests` currently expects separate DP assemblies for non-PG providers | `tests/Event.Architecture.Tests/ProviderMigrationOwnershipTests.cs` | Requires deliberate rewrite or replacement |
| `PrimaryDatabaseProviderCompositionTests` currently encodes old migration contract | same as above in integration project | Must be updated before refactor acceptance |
| Privacy erasure DB contexts point at legacy sqlite migration assembly |
`EmbeddedPrivacyErasureAuthorityDbContext.cs` const `Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite` | If legacy project is removed, compose/migrations must be rerouted |
| Raw SQL lock/update usage still exists in many persistence files | repository scan shows broad `ExecuteSqlRaw/Interpolated` usage |
Need tight scope boundary before declaring "eliminated raw SQL" |

### 3.2 Current Implementation Snapshot
- `Explore.Persistence` still contains PostgreSQL-only co-located raw SQL lock/insert patterns.
- `PersistenceServicesRegistration` and composition switch still treat non-PG co-located authority as unsupported.
- Migration ownership contract is partially split by architecture and includes legacy project assumptions.
- Dockerfiles and host project references still include legacy migration assemblies.

### 3.3 Existing Verification Evidence
- `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs`
- `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityCompositionValidationTests.cs`
- `tests/Event.Architecture.Tests/ProviderMigrationOwnershipTests.cs`
- `tests/Event.Architecture.Tests/PrimaryDatabaseMigrationCompositionTests.cs`
- Host/build references in `Explore.slnx`, `Dockerfiles`, and project refs.

### 3.4 Current Pain Areas
- Plan and code are currently out-of-sync on co-located topology, migration ownership, and project references.
- Raw SQL elimination scope is underspecified (some SQL is still intentional in domain-specific queries and PostgreSQL-only constraint support).
- Migration ownership and packaging changes are missing in deployment artifacts.

### 3.5 Unknowns After Investigation
- Whether co-located authority should use application schema name semantics for every provider or follow provider-specific defaults consistently; this must be decided and fixed once in the composition layer.
- Whether to keep any PostgreSQL-only authority function/trigger code in legacy external authority path after consolidating co-located support.

## 4. Proposed Future State

1. **Migration ownership model:**
   - `Explore.Persistence` keeps all PostgreSQL application/data-protection/co-located authority ownership.
   - SQLite/SQL Server/MySQL/MariaDB ownership moves to exactly their provider migration assembly:
     - `Explore.Persistence.Migrations.Sqlite`
     - `Explore.Persistence.Migrations.SqlServer`
     - `Explore.Persistence.Migrations.MySql`
     - `Explore.Persistence.Migrations.MariaDb`
   - Remove these legacy projects:
     - `Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite`
     - `Explore.Persistence.DataProtection.Migrations.Sqlite`
     - `Explore.Persistence.DataProtection.Migrations.SqlServer`
     - `Explore.Persistence.DataProtection.Migrations.MySql`
     - `Explore.Persistence.DataProtection.Migrations.MariaDb`

2. **Privacy erasure authority model:**
   - Single repository strategy for co-located authority writes/replays across all providers.
   - No DB-side function/trigger enforcement for immutable rules in co-located mode where possible; enforce in repository/application layer with provider-agnostic locking.

3. **Raw SQL boundary and lock strategy:**
   - Replace only raw SQL that is now maintainability/risk-heavy and migration-blocking.
   - Keep PG-only SQL where semantically required and isolated to clearly identified boundaries.

4. **Deployment correctness:**
   - Align Dockerfiles, csproj references, and solution manifest with the unified migration assembly model.
   - Update operator docs for new migration ownership before any release.

## 5. Non-Negotiable Constraints

- Repositories return entities, never DTOs. Mappers remain in handlers.
- No BFF/UI authority truth; permissions remain server-side.
- `I``Option Validation must fail closed for unsupported topology/provider combinations.
- Zero manual edits to generated migration/snapshot code.
- `PrimaryDatabaseProviderComposition` is the single switch for provider composition, including migration assembly and history table policy.
- For this workstream, removing broken legacy compatibility paths is allowed and required.

## 6. Architecture and Design Decisions

### Decision 1 — **One-provider migration assembly per non-PG provider**
**Decision:** Consolidate application and data-protection migrations into existing provider assemblies and remove dedicated DP/erasure sqlite projects.  
**Reason:** Reduces project count, removes duplicated project maintenance, and makes operator topology easier to reason about.  
**Trade-off:** One-time migration re-homing and broader test/document updates.

### Decision 2 — **Raise co-located support scope to all primary providers**
**Decision:** Do not keep PostgreSQL/SQLite-only co-located support.
**Reason:** Enterprise self-hosters need predictable behavior across providers.
**Constraint:** Locking strategy must remain safe and transparent per provider.

### Decision 3 — **Keep PostgreSQL-only authority constructs only for external database path unless actively replaced**
**Decision:** Do not expand PostgreSQL function/trigger elimination outside the co-located path without explicit migration proof.
**Reason:** Avoid unnecessary behavior drift while de-risking core external authority contracts.

### Decision 4 — **Plan for PR splits, not one giant PR**
**Decision:** Split by architecture boundary and rollback isolation: foundation, lock/raw SQL, composition/migrations, artifacts/docs.

## 7. Implementation Strategy and PR Split

### PR A — Foundation & Contract Hardening (Recommended first)
- Define final composition contract in tests.
- Update all tests that currently encode legacy project layout and co-located restrictions.

### PR B — Co-located Authority + Invariant Unification
- Implement one co-located repository strategy and invariant enforcement.
- Remove old repository topology classes that are no longer valid.

### PR C — Persistence Standardization
- Replace non-PG lock/raw SQL hotspots with provider-neutral mechanisms.
- Keep isolated PG-only SQL boundaries where justified.

### PR D — Migration Ownership + Deployment Surface
- Remove deprecated migration projects and references.
- Update runtime host packaging and docs.
- Regenerate migrations where assembly ownership has changed.

## 8. Implementation Phases

### Phase 1 — Contract Baseline & Compatibility Breakpoint
- Goal: Reconcile all assumptions before code deletion.
- Files:
  - `tests/Event.Architecture.Tests/ProviderMigrationOwnershipTests.cs`
  - `tests/Event.Architecture.Tests/PrimaryDatabaseMigrationCompositionTests.cs`
  - `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs`
  - `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityCompositionValidationTests.cs`
  - `dev/active/multi-database-persistence-unification-plan.md`
  - `dev/active/multi-database-persistence-unification-context.md`
  - `dev/active/multi-database-persistence-unification-tasks.md`
- Acceptance:
  - Composition tests explicitly encode the target ownership contract for all providers.
  - No legacy exception path assumptions remain in tests that are now being removed.

### Phase 2 — Co-Located Authority Unification + Invariant Enforcement
- Goal: unify co-located erasure behavior and remove lock-in while preserving tenant/session safety.
- Files:
  - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/CoLocatedPostgresPrivacyErasureAuthorityRepository.cs` (delete)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EmbeddedPrivacyErasureAuthorityRepository.cs` (retain for `EmbeddedSqlite`, rewire only if design changes)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/CoLocatedPrivacyErasureAuthorityRepository.cs` (new)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/EmbeddedPrivacyErasureAuthorityDbContext.cs` (if migration assembly target changes)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/EmbeddedPrivacyErasureAuthorityDbContextFactory.cs`
  - `src/Explore.Persistence/PersistenceServicesRegistration.cs`
  - `src/Explore.Persistence/Schema/PostgresModelConstraintApplier.cs` (no-op unless still needed)
- Acceptance:
  - Co-located mode works for all supported providers.
  - Invariant checks are enforced in persistence/application boundary, not PostgreSQL-only stored logic.

### Phase 3 — Provider-Scoped Locking and Raw SQL Escape
- Goal: remove high-risk raw SQL usage in repositories where provider-neutral alternatives exist.
- Files:
  - `src/Explore.Persistence/Database/RelationalNamedLock.cs` (use/extend if required)
  - `src/Explore.Persistence/Repositories/EventAgendaItemRepository.cs`
  - `src/Explore.Persistence/Repositories/RegistrationInventoryRepository.cs`
  - `src/Explore.Persistence/Repositories/RegistrationFinalizationRepository.cs`
  - `src/Explore.Persistence/Repositories/IncomingWebhookEffectOutboxRepository.cs`
  - `src/Explore.Persistence/Repositories/IncomingWebhookMessageRepository.cs`
  - `src/Explore.Persistence/Repositories/RegistrationProviderSubscriptionStateRepository.cs`
  - `src/Explore.Persistence/Repositories/RegistrationProviderSubmissionWriteEffectRepository.cs`
  - `src/Explore.Persistence/Repositories/PdsSyncOutboxRepository.cs`
  - `src/Explore.Persistence/Repositories/WebhookLocalTargetRepository.cs`
  - `src/Explore.Persistence/Repositories/AtprotoJetstreamRepository.cs`
  - `src/Explore.Persistence/Repositories/NotificationFanoutOccurrenceRepository.cs`
- Acceptance:
  - Zero `pg_advisory_xact_lock`-style raw lock strings.
  - `ExecuteUpdateAsync/ExecuteDeleteAsync` used where write-set updates are simple and provider-agnostic.
  - Raw SQL that remains is intentionally PG-only and documented.

### Phase 4 — Composition Wiring + Migration Ownership Rehome
- Goal: route all migration ownership to target assemblies and remove obsolete assemblies.
- Files:
  - `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs`
  - `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs`
  - `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityCompositionValidationTests.cs`
  - `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs`
  - `tests/Event.Architecture.Tests/PrimaryDatabaseMigrationCompositionTests.cs`
  - `tests/Event.Architecture.Tests/ProviderMigrationOwnershipTests.cs`
  - `src/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` (or host references)
  - `Explore.slnx`
  - `src/Event.MigrationService/Event.MigrationService.csproj`
  - `src/Event.Standalone/Event.Standalone.csproj`
  - `src/Event.Standalone/Dockerfile`
  - `src/Event.MigrationService/Dockerfile`
- Acceptance:
  - Non-PG provider migrations map to exactly one provider assembly.
  - No references remain to removed migration projects.
  - Migrate command path and migration history names are explicit and consistent.

### Phase 5 — Migration Regeneration, Runbook, and Final Verification
- Goal: produce final migration state and close operator docs.
- Files:
  - `src/Explore.Persistence.Migrations.Sqlite/*`
  - `src/Explore.Persistence.Migrations.SqlServer/*`
  - `src/Explore.Persistence.Migrations.MySql/*`
  - `src/Explore.Persistence.Migrations.MariaDb/*`
  - `docs/OPERATIONS.md`
  - `docs/CONFIGURATION.md`
  - `docs/SELF_HOSTING.md`
- Acceptance:
  - Legacy migration assemblies are deleted.
  - Required migrations are regenerated in correct owning assemblies.
  - Docs reflect exact config + recovery behavior before release.

## 9. Verification Strategy

### Phase 1
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 2
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 3
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --filter FullyQualifiedName~Repository --configuration Release --verbosity quiet`

### Phase 4
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 5
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --filter "FullyQualifiedName~PrimaryDatabaseProviderCompositionTests|FullyQualifiedName~Migration|FullyQualifiedName~PrivacyErasure" --configuration Release --verbosity quiet`

## 10. Documentation, Configuration, and Operations Impact

- Update migration ownership matrix in `docs/OPERATIONS.md` and `docs/SELF_HOSTING.md`.
- Update provider override matrix in `docs/CONFIGURATION.md` (co-located topology support scope and migration behavior).
- Add migration rollback/preflight guidance for removing legacy assemblies:
  - verify no un-applied migration contract mismatch before upgrade;
  - enforce `dotnet ef migrations remove` only on unapplied development migrations;
  - run `event-migrationservice` twice post-upgrade before API cutover.

## 11. Security, Authorization, and Multi-Tenancy

- Keep authority topologies server-side validated and fail-closed.
- Preserve tenant filtering in all read/write repository paths.
- Keep HAL affordance model untouched (no client-side permission checks).
- Ensure topology extension does not alter tenant trust boundaries (single-tenant/co-located/shared DB semantics remain explicit).

## 12. Migration and Compatibility Plan

- Breaking changes are acceptable (pre-v1 rule), but not silent.
- Compatibility artifacts to delete:
  - `Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite`
  - `Explore.Persistence.DataProtection.Migrations.SqlServer`
  - `Explore.Persistence.DataProtection.Migrations.MySql`
  - `Explore.Persistence.DataProtection.Migrations.Sqlite`
  - `Explore.Persistence.DataProtection.Migrations.MariaDb`
- Before implementation:
  - Archive and communicate old migration assembly expectations in docs and release note.
- During migration:
  - Run migration ownership tests and integration migration composition checks.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Existing tests encode outdated contracts and block clean rollout | High | High | Rewrite tests in Phase 1 before behavior changes |
| Cross-provider co-located semantics differ due locking/transaction semantics | Medium | High | Add integration smoke assertions for each provider and use lock abstractions |
| Migration history fragmentation after assembly merge | Medium | High | Keep history table names deterministic and verify with tests |
| Raw SQL replacement causes translation regressions | Medium | Medium | Restrict replacements to audited repos and verify with provider-specific integration tests |
| Package/docs drift after deleting projects | Medium | Medium | Final doc verification phase with explicit doc checklist |

## 14. Success Metrics

1. `dotnet build --configuration Release --verbosity quiet` passes at phase end.
2. Primary provider composition tests assert correct assembly ownership for all providers and targets.
3. Co-located authority works for all supported providers with identical payload semantics.
4. Deleted migration assemblies are not referenced from csproj, solution, Dockerfiles, or tests.
5. Operator docs include migration and recovery instructions for all provider profiles.

## 15. Implementation-Agent Contract — KEEP DEV DOCS CURRENT

1. `tasks.md` is the hot execution ledger and must update after substantial completion.
2. Every phase boundary must include one build and one targeted test command exactly.
3. Any contract-changing evidence in architecture/integration tests must be updated before code touches in the next phase.
4. If composition assertions change, update context and plan before implementation handoff.

## 16. Progress Reporting Contract

After each phase:
- Completed: what was changed
- Verified: exact command output and test names
- Remaining: blocked and open risks
- Next: immediate follow-up slice
- Docs updated: list of docs touched and why

## 17. Potential Risks & Unknowns

- Whether embedded authority schema evolution should remain in sqlite-specific migration assembly or move to provider assembly first requires one explicit design decision before PR D.
- Whether co-located authority should reuse identical table naming conventions across providers (including schema/prefix behavior for SQLite and MySQL-family) must be captured in one migration design note before migrations are regenerated.
