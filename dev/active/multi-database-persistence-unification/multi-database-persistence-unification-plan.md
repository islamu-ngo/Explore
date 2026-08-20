<!-- ABOUTME: Senior CTO-reviewed implementation plan for hardening the multi-database persistence contract. -->
<!-- ABOUTME: Preserves provider-native migration and authority boundaries while aligning composition, tests, and operator guidance. -->

# Multi-Database Persistence Contract Hardening — Implementation Plan

Last Updated: 2026-08-20 Europe/Brussels

## Planning Metadata

- **Workstream:** `dev/active/multi-database-persistence-unification/`
- **Status:** Complete — Phase 1/2 implementation, architecture and quality remediation, isolated verification, independent architecture/quality approval, and final audit passed
- **Decision:** The prior HIGH same-target architecture finding is resolved. The test-contract remediation passed independent quality review, and the final architecture, quality, and completion audits all passed.
- **Canonical implementation intent:** `platform-privacy-erasure`
- **Primary layers:** Persistence composition, migration orchestration, architecture/integration tests, CI contract, and operator documentation
- **Breaking-change policy:** Pre-v1 breaking changes are allowed. No compatibility shim is required for a removed or rejected configuration contract.
- **External influence:** Official Microsoft EF Core and ASP.NET Core Data Protection documentation only, retrieved through Anysearch and Context7 and reduced to source-free functional constraints. No external source code or dependency change was used.

## Senior CTO Verdict

The original workstream was not implementation-ready. It combined four materially different changes:

1. collapsing migration projects;
2. expanding co-located privacy-erasure authority to every primary provider;
3. replacing provider-specific repositories and SQL with one generic implementation; and
4. adding Quartz runtime validation.

That combination increased migration, restore, concurrency, and release risk without a demonstrated operator benefit. It also contradicted the canonical `platform-privacy-erasure` contract and implemented documentation, which currently support:

- all five providers for the primary application and Data Protection stores;
- `EmbeddedSqlite` authority independently of the selected primary provider;
- `CoLocated` authority on PostgreSQL or SQLite; and
- `ExternalDatabase` authority on PostgreSQL.

The approved direction is contract hardening, not universal storage unification. Keep provider-native implementations where database semantics differ, make the supported matrix explicit and fail closed, preserve generated migration ownership, and prove that runtime, migrator, tests, CI, and operator docs agree.

## Verified Current State

| Verified fact | Source anchor | Planning consequence |
|---|---|---|
| Primary application persistence supports PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL | `PrimaryDatabaseProviderComposition.cs`; `PrimaryDatabaseProviderCompositionTests.cs` | Keep the five-provider primary matrix |
| PostgreSQL application and Data Protection migrations live in `Explore.Persistence` | `PrimaryDatabaseProviderComposition.GetMigrationsAssemblyName` | Preserve the current PostgreSQL owner |
| Each non-PostgreSQL provider has separate application and Data Protection migration assemblies and history tables | `ProviderMigrationOwnershipTests.cs`; provider `.csproj` files | Do not collapse projects without a separately approved migration-ownership change |
| Embedded authority migrations are owned by `Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite` | `EmbeddedPrivacyErasureAuthorityDbContext.cs` | Keep embedded authority isolated from primary SQLite migrations |
| Co-located PostgreSQL uses `CoLocatedPrivacyErasureAuthorityDbContext` and a PostgreSQL-specific repository | `PersistenceServicesRegistration.cs`; `CoLocatedPostgresPrivacyErasureAuthorityRepository.cs` | Preserve transaction/locking semantics |
| Co-located SQLite reuses the primary file through the embedded SQLite context/repository | `PersistenceServicesRegistration.cs`; `EmbeddedPrivacyErasureAuthorityRepository.cs` | Preserve SQLite writer and file semantics |
| SQL Server, MariaDB, and MySQL co-located authority fail closed | `PersistenceServicesRegistration.cs`; composition tests | Treat these combinations as unsupported, not unfinished |
| External authority is a distinct PostgreSQL database with function-only runtime access | `EfCorePrivacyErasureAuthorityRepository.cs`; `docs/PRIVACY_ERASURE.md` | Do not generalize the external adapter |
| The CI provider matrix already runs MigrationService twice for each primary provider | `.github/workflows/_build-test.yml`; `docs/TESTING.md` | Reuse the existing enterprise evidence lane |
| The Release build passes but emits pre-existing NU1903 and analyzer warnings | Baseline build on 2026-08-20 | Track outside this workstream; do not claim warning-clean enterprise readiness |

## Target Support Contract

### Primary persistence and Data Protection

| Provider | Application | Data Protection | Namespace |
|---|---:|---:|---|
| PostgreSQL | Supported | Supported | configured schema |
| SQLite | Supported | Supported | fixed `ie_` prefix |
| SQL Server | Supported | Supported | configured schema |
| MariaDB | Supported | Supported | fixed `ie_` prefix |
| MySQL | Supported | Supported | fixed `ie_` prefix |

### Privacy-erasure authority

| Topology | Supported provider/placement | Restore contract |
|---|---|---|
| `EmbeddedSqlite` | Dedicated SQLite authority file with any supported primary provider | Restore-isolated only when its file/volume is protected from primary restore |
| `CoLocated` | Primary PostgreSQL or primary SQLite | Restored atomically with the primary; `restoreReplayProtection=false` |
| `ExternalDatabase` | Separate PostgreSQL database | Restore-isolated only when restored independently |
| `CoLocated` on SQL Server, MariaDB, or MySQL | Unsupported | Startup/configuration fails before adapter use and without leaking secrets |

This matrix is the release contract for this workstream. Expanding it requires an explicit update to the canonical intent, privacy-erasure docs, threat/recovery model, migration ownership, and real-engine test evidence.

## Architecture Decisions

### 1. Keep migration ownership separated by DbContext and provider

Separate generated snapshots and histories are operational boundaries, not accidental project sprawl. The plan keeps:

- `Explore.Persistence` for PostgreSQL application, Data Protection, and current co-located PostgreSQL authority migrations;
- `Explore.Persistence.Migrations.{Provider}` for non-PostgreSQL application migrations;
- `Explore.Persistence.DataProtection.Migrations.{Provider}` for non-PostgreSQL Data Protection migrations; and
- `Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite` for embedded/co-located SQLite authority migrations.

Generated migrations and snapshots remain untouched unless a source model change requires regeneration through `dotnet ef`.

### 2. Keep topology-specific authority adapters

There will be no universal `CoLocatedPrivacyErasureAuthorityRepository`. PostgreSQL row locking, SQLite single-writer serialization, and external PostgreSQL function ACLs are materially different safety contracts. A shared interface already exists at the Application boundary: `IPrivacyErasureAuthority`.

### 3. Standardize capability and diagnostics, not database semantics

`PrimaryDatabaseProviderComposition` and `PersistenceServicesRegistration` remain closed switches. Implementation should remove wording drift and make unsupported topology/provider combinations fail before repository use. Do not add a plugin system, provider factory hierarchy, or new dependency.

### 4. Preserve authority-first and tenant-isolation boundaries

This workstream does not change the privacy-erasure workflow, replay checkpoint, specialized outboxes, receipt authorization, or tenant predicates. Authority facts are instance-level recovery data accessed only through the dedicated privacy-erasure adapter; normal repositories must not bypass tenant filters.

### 5. Keep provider-specific SQL when it owns provider-specific behavior

Raw SQL is not a defect by itself. Keep parameterized, bounded SQL where it implements PostgreSQL locks/functions, SQLite conflict semantics, provider DDL, or migration invariants. Replace SQL only when a concrete correctness or portability failure is proven and a provider-neutral EF operation preserves identical transaction semantics.

### 6. Enforce external-authority physical exclusivity at every composition boundary

**Decision:** `PrivacyErasureAuthorityDatabaseConfiguration.EnsureDistinctPhysicalDatabase` is the single preflight for `ExternalDatabase` topology. It compares structured PostgreSQL host identity (including loopback normalization), port, and database identity and raises a bounded, secret-safe `OptionsValidationException` for a same-target configuration.

**Why:** The first bounded architecture review returned **ARCHITECTURE FAIL** with one HIGH finding: `MigrationService` and the external migrator could target the same physical PostgreSQL database because only runtime DI enforced distinct targets. The runtime test did not cover `MigrationService`/migrator pre-I/O behavior.

**Consequences:** Runtime persistence, `Event.MigrationService` `Program` composition, and `ExploreDatabaseMigrator` invoke the shared guard. The migrator guard executes before Application, ProviderAdjustments, DataProtection, authority, Seed, or migration-completion log I/O. No generated migration edit, fallback, compatibility shim, dual write, or duplicate sink is introduced.

**Files/layers affected:** `src/Explore.Secrets/Database/PrivacyErasureAuthorityDatabaseConfiguration.cs`; runtime persistence composition; `src/Event.MigrationService/Program.cs`; `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs`; focused configuration, composition, and migrator tests.

## Scope

### In scope

- exact provider/topology support assertions;
- secret-safe, actionable fail-fast composition errors;
- exact migration assembly/history ownership assertions;
- MigrationService selection of application, Data Protection, and one authority sink;
- operator documentation for topology choice, migration ownership, backup/restore, and unsupported combinations;
- CI/documentation alignment with the existing five-provider primary matrix.

### Out of scope

- five-provider co-located privacy authority;
- migration-project deletion or assembly merging;
- blanket raw-SQL removal;
- a generic provider plugin/factory layer;
- privacy workflow, API, HAL, BFF, receipt, outbox, or authorization redesign;
- generated migration edits without a model change;
- Quartz DDL/runtime validation.

## Implementation Phases

## Phase 1 — Capability Contract and Fail-Closed Composition

**Goal:** Make source, DI composition, and focused tests state the same supported matrix.

### Task 1.1 — Lock the provider/topology matrix in tests

- **Files:**
  - `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs` (existing)
  - `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityCompositionValidationTests.cs` (existing)
- **Work:**
  - Assert all five primary application/Data Protection providers.
  - Assert `CoLocated` succeeds only for PostgreSQL and SQLite.
  - Assert SQL Server, MariaDB, and MySQL `CoLocated` settings fail before adapter resolution.
  - Assert exactly one authority adapter is registered for every supported topology.
- **Acceptance:** Tests encode the target matrix and verify that failure text contains no credentials or connection details.
- **Effort:** M
- **Dependencies:** None

### Task 1.2 — Normalize composition validation and diagnostics

- **Files:**
  - `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs` (existing)
  - `src/Explore.Persistence/PersistenceServicesRegistration.cs` (existing)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/EmbeddedPrivacyErasureAuthorityDbContextFactory.cs` (existing, only if needed to remove message drift)
- **Work:**
  - Keep the existing closed switches and provider-specific adapters.
  - Reject unsupported combinations during composition with the topology, supported providers, and operator remediation.
  - Do not expose structured database values, credentials, or generated connection strings.
- **Acceptance:** Every unsupported combination fails deterministically before authority I/O; supported topology registrations remain singular and unchanged.
- **Effort:** S
- **Dependencies:** 1.1

### Task 1.3 — Pin migration ownership and history boundaries

- **Files:**
  - `tests/Event.Architecture.Tests/ProviderMigrationOwnershipTests.cs` (existing)
  - `tests/Event.Architecture.Tests/PrimaryDatabaseMigrationCompositionTests.cs` (existing)
  - `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityModelTests.cs` (existing)
- **Work:**
  - Preserve exact application/Data Protection owners and distinct history tables.
  - Add the embedded SQLite authority migration owner/history to the contract.
  - Confirm no test expects one assembly per database engine.
- **Acceptance:** An accidental project merge, history collision, or authority migration reroute fails a deterministic test.
- **Effort:** S
- **Dependencies:** 1.1

### Phase 1 Verification — run once after all Phase 1 tasks

1. `dotnet build --configuration Release --verbosity quiet`
2. `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release -- --treenode-filter "/*/*/PrimaryDatabaseProviderCompositionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`

## Phase 2 — Migrator, CI, and Operator Contract

**Goal:** Make deployment and recovery behavior match the source-level capability contract.

### Task 2.1 — Prove one migration path per selected topology

- **Files:**
  - `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs` (existing)
  - `src/Event.MigrationService/Worker.cs` (existing, verified unchanged delegation boundary)
  - `tests/Event.Persistence.IntegrationTests/Migrations/ExploreDatabaseMigratorTests.cs` (existing)
  - `tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` (existing; references the existing SQLite Data Protection migration project for executable tests)
- **Work:**
  - Emit structured completion events from the real migrator only after application, provider-adjustment, Data Protection, selected authority, and seed stages succeed.
  - Execute production-faithful EF scenarios for embedded SQLite, co-located SQLite, co-located PostgreSQL, and external PostgreSQL.
  - Assert exact ordered operations plus physical migration histories and destination exclusivity so skipped, duplicate, reordered, and cross-topology authority writes fail the suite.
  - Preserve the API/runtime rule that deployed schema migration belongs to MigrationService.
- **Acceptance:** Tests prove no skipped authority migration, duplicate sink, cross-topology write, or provider fallback.
- **Effort:** M
- **Dependencies:** Phase 1

**Completion evidence (2026-08-20):** Independent verification passed all 16 aggregate verifier tests. The migrator order is `Application` -> `ProviderAdjustments` -> `DataProtection` -> exactly one topology-selected authority -> `Seed`; physical histories prove authority-destination exclusivity. The embedded-authority-migration deletion mutation failed, and the duplicate authority-completion mutation failed 5-vs-6. `Worker` remained unchanged, and verifier cleanup completed.

### Task 2.2 — Synchronize self-hosting and recovery documentation

- **Files:**
  - `docs/PRIVACY_ERASURE.md` (existing, changed)
  - `docs/CONFIGURATION.md` (existing, changed)
  - `docs/SELF_HOSTING.md` (existing, changed)
  - `docs/TROUBLESHOOTING.md` (existing, changed)
- **Work:**
  - Published the exact support matrix, topology-selection guidance, migration owners, backup/restore boundaries, `restoreReplayProtection`, and fail-closed recovery behavior in the four bounded operator documents.
  - Stated that pre-v1 unsupported combinations require configuration correction, not a compatibility shim.
- **Acceptance:** Passed. Scoped documentation diff check exited 0. `docs/BACKUP_RESTORE_UPGRADE.md` required no edit because its existing backup-unit and restore guarantees already matched the contract.
- **Effort:** M
- **Dependencies:** 2.1

### Task 2.3 — Align CI and test documentation with the contract

- **Files:**
  - `.github/workflows/_build-test.yml` (existing; change only if current assertions are insufficient)
  - `docs/TESTING.md` (existing)
  - `dev/active/multi-database-persistence-unification/multi-database-persistence-unification-plan.md` (existing)
  - `dev/active/multi-database-persistence-unification/multi-database-persistence-unification-context.md` (existing)
  - `dev/active/multi-database-persistence-unification/multi-database-persistence-unification-tasks.md` (existing)
- **Work:**
  - Confirmed the five-provider primary matrix and twice-run MigrationService evidence remain aligned.
  - Confirmed authority coverage is embedded SQLite, co-located PostgreSQL/SQLite, and external PostgreSQL, not five-provider co-location.
  - Recorded exact green evidence; it did not close the workstream at that point. Final architecture approval and quality-failure test hardening are now complete; only final independent quality approval and final audit remain pending.
- **Acceptance:** Passed. `.github/workflows/_build-test.yml` and `docs/TESTING.md` required no edit; they make no broader provider claim than the implementation.
- **Effort:** S
- **Dependencies:** 2.2

### Phase 2 Verification — PASSED ONCE AFTER ALL PHASE 2 TASKS

1. `dotnet build --configuration Release --verbosity quiet` — exited 0 with 0 errors and 14,154 existing warnings, including `NU1903` for `SSH.NET` 2025.1.0.
2. Focused isolated suites exited 0 with 0 failed and 0 skipped: `ProviderMigrationOwnershipTests` 13; `PrimaryDatabaseMigrationCompositionTests` 5; `PrimaryDatabaseProviderCompositionTests` 37; unsupported `CoLocated` providers 3; singular adapter 1; authority model 9; migrator topology 2; real PostgreSQL migrator 3.

The existing real-engine provider matrix remains a merge/release gate in CI. This plan does not ask implementation agents to start Docker, Aspire, the application, browsers, or live services as phase-end verification.

### Post-Review Architecture Remediation — COMPLETE (2026-08-20)

The initial bounded architecture review returned **ARCHITECTURE FAIL** with one HIGH finding: runtime DI alone kept `ExternalDatabase` authority distinct, so `MigrationService` and the external migrator could still target the same physical PostgreSQL database. The missing coverage was migrator pre-I/O behavior.

TDD red evidence: `MigrateAndSeedAsync_ExternalAuthoritySameTarget_FailsBeforeMigrationIo` ran alone in a disposable detached worktree: 1 executed, 1 failed, exit 2, because no `OptionsValidationException` was thrown.

The completed remediation centralizes PostgreSQL host/loopback/port/database identity and bounded secret-safe failure in `PrivacyErasureAuthorityDatabaseConfiguration.EnsureDistinctPhysicalDatabase`. Runtime persistence, `Event.MigrationService` `Program` composition, and `ExploreDatabaseMigrator` invoke it. The migrator preflight runs before Application, ProviderAdjustments, DataProtection, authority, Seed, or migration-completion log I/O. There were no generated migration edits, fallback, shim, or dual write.

Green remediation verification: `dotnet build --configuration Release --verbosity quiet` exited 0 with 0 errors and 14,154 warnings after removing the change-caused `CS8604` without suppression. Relevant selectors passed 77/77 with 0 failed/skipped: same-target 1; `ExploreDatabaseMigratorTests` 4; topology 2; composition validation 11; authority database configuration 4; provider ownership 13; primary migration composition 5; primary provider composition 37. Same-target histories and log operations remained empty; supported topology five-stage behavior and physical exclusivity remained exact.

Final recheck: the full Release build again exited 0 with 0 errors and 14,154 warnings; the single regression passed 1/1 with 0 failed/skipped; scoped diff check exited 0; containers, worktrees, and artifacts were cleaned.

### Final Architecture Approval and Quality-Failure Remediation — COMPLETE (2026-08-20)

The fresh final architecture review **passed**: the prior HIGH same-target finding is resolved. Residual DNS/CNAME alias behavior and the disclosed legacy SQLite migration-catalog and `SSH.NET` advisory risks remain non-blocking and unchanged.

The first replacement quality review returned **QUALITY FAIL** with two MEDIUM test-contract findings in `ExploreDatabaseMigratorTests.cs`: nullable suppression through `exception!`, and incomplete diagnostic-secrecy coverage (no length bound and no exclusions for host, database, username, or the complete connection string). The test-only fix converts a null exception and every structured target value explicitly into setup failures; uses an explicitly typed `string[]` (avoiding CS9176) to assert a diagnostic length of at most 512, the required remediation fragment, and exclusions for host, database, username, password, and the complete connection string. It retains the empty operation log and all three physical histories, and contains no suppression.

Verification record: the initial one-shot caught CS9176 and was then fixed. One verifier selected zero tests and another accidentally included unrelated payment hunks; both are discarded as verifier-construction failures, not green evidence. The trusted verifier used the established exact 12-path reconstruction and passed without retry: full Release build exit 0, 0 errors, exactly 14,154 accepted warnings, and no assertion-hardening diagnostic; the proven `ExploreDatabaseMigratorTests` class selector ran 4 passed, 0 failed, 0 skipped, including the same-target regression; scoped diff check exited 0. Worktree, metadata, containers, reports, logs, patches, and temporary artifacts were cleaned; the shared worktree was untouched.

The decisive final independent quality review returned **QUALITY PASS** with no findings. It confirmed the corrected null contracts, bounded and complete secret-safety assertions, physical pre-I/O evidence, trusted 12-path verification, clean scope, cleanup, and unchanged architecture/documentation conclusions. The final audit reconciled every requested implementation, documentation, verification, evidence, review, exclusion, and delivery-mode requirement.

### Current Resume Point

1. The workstream is complete. Reopen it only if contradictory production or migration evidence appears.
2. Preserve the isolated implementation inventory: `src/Event.MigrationService/Program.cs`; `src/Explore.Secrets/Database/PrivacyErasureAuthorityDatabaseConfiguration.cs`; `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs`; `src/Explore.Persistence/PersistenceServicesRegistration.cs`; `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs`; `tests/Event.Architecture.Tests/ProviderMigrationOwnershipTests.cs`; `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs`; `tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj`; `tests/Event.Persistence.IntegrationTests/Migrations/ExploreDatabaseMigratorTests.cs`; `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityCompositionValidationTests.cs`; `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityModelTests.cs`; and `tests/Event.Persistence.IntegrationTests/packages.lock.json` only for its SQLite Data Protection project entry.
3. Retain exclusions: no generated migration/designer/snapshot edits; no fallback, shim, dual write, duplicate sink, or API ownership drift; no operator documentation, CI, testing, or backup edit was required because their distinct-external-target and fail-before-I/O contract is now correctly implemented; containers, worktrees, and artifacts were cleaned.

## Security, Privacy, and Multi-Tenancy

- **Authentication/authorization:** No HTTP contract changes. Existing receipt authorization and server-side policy remain authoritative.
- **HAL/BFF:** Not applicable; no client affordance or token flow changes.
- **Tenant isolation:** No normal repository may disable the Tenant filter. Instance-level authority access remains confined to the dedicated privacy-erasure adapter with exact subject predicates.
- **Secrets:** Configuration failures must use bounded field/provider names only. Never log credentials, DSNs, identifiers, provider payloads, or exception text containing them.
- **Data lifecycle:** `EmbeddedSqlite` and independently restored external PostgreSQL can protect against stale primary restores; `CoLocated` cannot make that claim.
- **Outbox:** Existing transactional and specialized outbox behavior remains unchanged. Provider work stays post-commit, idempotent, fenced, retryable, and observable without PII.

## Documentation and Operational Impact

| Artifact | Required outcome |
|---|---|
| `docs/PRIVACY_ERASURE.md` | canonical topology/provider matrix (changed) |
| `docs/CONFIGURATION.md` | exact accepted values and fail-fast combinations (changed) |
| `docs/SELF_HOSTING.md` | topology selection and deployment implications (changed) |
| `docs/BACKUP_RESTORE_UPGRADE.md` | no edit required; existing backup unit and restore guarantees remain correct |
| `docs/TROUBLESHOOTING.md` | secret-safe diagnosis and remediation (changed) |
| `docs/TESTING.md` | no edit required; existing provider and authority evidence lanes remain exact |
| `.github/workflows/_build-test.yml` | no edit required; existing five-provider/twice-run evidence remains aligned |
| Operator docs, CI, testing, and backup material after remediation | no edit required; their existing distinct-external-target and fail-before-I/O contract is now correctly implemented |
| Release notes/checklist | required only if implementation changes a shipped configuration or migration contract |

## Risk Register

| Severity | Risk | Mitigation / owner |
|---|---|---|
| Critical | A generic authority repository weakens PostgreSQL/SQLite concurrency or restore semantics | Prohibited by this plan; retain topology-specific adapters |
| Critical | Migration assembly/history consolidation makes existing databases unrecoverable or ambiguous | Preserve current owners and histories; no generated migration edits |
| Medium | Future planning could reintroduce unsupported co-located providers | The optional-retained-authority workstream now cancels OREA-1010–1018 and pins the same PostgreSQL/SQLite matrix |
| High | Runtime, MigrationService, CI, and docs advertise different matrices | Tasks 1.1–2.3 pin one contract |
| High | Release build reports a known high-severity `SSH.NET` package advisory | Route to dependency/security ownership; no dependency change is authorized here |
| Medium | Blanket raw-SQL removal changes transaction or lock behavior | Defer; require a concrete failing provider scenario before replacement |
| Medium | Build warning volume hides actionable warnings | Track as repository quality debt outside this workstream |
| High | The legacy SQLite application migration catalog cannot replay from an empty database because an older generated migration recreates `ie_account_authority_kinds` | Do not hand-edit generated artifacts; the Task 2.1 SQLite fixture establishes the current model and canonical history, while PostgreSQL supplies fresh replay evidence. Route catalog regeneration through separately approved migration work. |
| High | Concurrent unrelated dirty changes make shared-worktree build results unreliable | Verify workstream diffs in disposable detached worktrees and never revert or absorb unrelated payment, event-lifecycle, or generated-migration changes. |

## Deferred and Split Work

- **Five-provider co-located authority:** Not planned. The optional-retained-authority workstream cancels OREA-1010–1018; reconsider only through a new explicit product decision and canonical intent/recovery/security update.
- **Migration-project consolidation:** Rejected for this workstream. Reconsider only with measured build/packaging cost and a deployed-database migration-history plan.
- **Raw-SQL cleanup:** Split into repository-specific fixes driven by a failing provider test.
- **Quartz DDL validation:** Preserve as a separate scheduling/runtime concern involving `QuartzMultiDatabaseSchemaTests.cs`, `QuartzSchema.{SqlServer,MySql}.sql`, and `QuartzSchemaInitializer.cs`; it does not belong to persistence migration ownership.

## Definition of Done

1. **Passed:** Source and tests enforce the target support matrix.
2. **Passed:** Unsupported topology/provider combinations fail before I/O with secret-safe remediation.
3. **Passed:** Existing application, Data Protection, and authority migration owners and histories remain explicit and collision-free.
4. **Passed:** MigrationService selects exactly the required contexts for the configured topology.
5. **Passed:** Scoped operator docs describe truthful backup/restore guarantees and do not imply universal co-located support.
6. **Passed:** Each phase ended with its single Release build and selected focused test.
7. **Passed:** No migration/designer/snapshot was edited, no fallback/shim/dual write/duplicate sink/API ownership drift was introduced, no new dependency was added, and no unrelated repository SQL was refactored.
8. **Passed:** Post-review remediation rejects same-target external PostgreSQL authority before migrator I/O and completion logging; final architecture review passed with the prior HIGH finding resolved. DNS/CNAME alias behavior and disclosed legacy SQLite/`SSH.NET` risks remain non-blocking.
9. **Passed:** The replacement quality FAIL's two MEDIUM test-contract findings were corrected in `ExploreDatabaseMigratorTests.cs` without suppression; the trusted exact-12-path verifier passed the full Release build and all 4 class-selected tests without retry.
10. **Passed:** Final independent quality review returned `QUALITY PASS` with no findings, and the final audit reconciled the complete request, all evidence, exclusions, direct-delivery mode, and the terminal workstream artifacts.

## Implementation-Agent Contract

1. Start with Task 1.1 and use failing contract assertions before changing composition behavior.
2. Keep `tasks.md` as the hot ledger; update context after a phase, blocker, material discovery, or handoff.
3. Update this plan only when scope, architecture, sequencing, acceptance, risk, or verification changes.
4. Run phase verification once after all phase tasks, not after every edit.
5. If implementation requires SQL Server/MariaDB/MySQL co-located authority, stop: that is a product-contract expansion outside this plan.
