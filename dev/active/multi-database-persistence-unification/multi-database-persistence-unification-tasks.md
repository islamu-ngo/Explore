<!-- ABOUTME: Execution ledger for the Senior CTO-rebaselined multi-database persistence contract workstream. -->
<!-- ABOUTME: Tracks capability, migration ownership, deployment, documentation, and phase verification tasks. -->

# Multi-Database Persistence Contract Hardening — Task Checklist

Last Updated: 2026-08-20 Europe/Brussels

## Status Summary

- **Overall status:** Complete — Phase 1/2 implementation, architecture and quality remediation, isolated verification, final architecture/quality approval, and final audit passed
- **Completed:** 6/6 planned implementation tasks plus post-review remediation and quality test-contract hardening/verification (both phase verification gates passed; governance gates tracked separately)
- **Current priority:** None; workstream complete
- **Next recommended slice:** None; reopen only for contradictory production or migration evidence

## Implementation Maintenance Rules

- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Mark a substantial task `🟡 IN PROGRESS` only when it spans meaningful edits or a handoff.
- Check completed tasks immediately and keep the completed count, priority, next slice, and date accurate.
- Check a phase complete only after all tasks and its verification checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance, risk, or verification changes.
- Run phase verification once after all phase tasks, not after individual tasks.
- Do not start Docker, Aspire, browsers, the application, Playwright, or live services for verification.
- Never hand-edit generated migrations or snapshots.

## Phase 1 — Capability Contract and Fail-Closed Composition ✅ COMPLETE

- [x] **1.1 Lock the provider/topology matrix in tests**
  - **Files:**
    - `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs` (existing)
    - `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityCompositionValidationTests.cs` (existing)
  - **Acceptance:** All five primary providers remain supported; `CoLocated` succeeds only for PostgreSQL/SQLite; unsupported combinations fail before adapter resolution without secret leakage; every supported topology registers exactly one authority adapter.
  - **Effort:** M
  - **Dependencies:** None

- [x] **1.2 Normalize composition validation and diagnostics**
  - **Files:**
    - `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs` (existing)
    - `src/Explore.Persistence/PersistenceServicesRegistration.cs` (existing)
    - `src/Explore.Persistence/Privacy/ErasureAuthority/EmbeddedPrivacyErasureAuthorityDbContextFactory.cs` (existing, only if message drift exists)
  - **Acceptance:** Unsupported provider/topology combinations fail deterministically before I/O with bounded remediation; existing PostgreSQL, SQLite, and external PostgreSQL adapters remain unchanged and singular.
  - **Effort:** S
  - **Dependencies:** 1.1

- [x] **1.3 Pin migration ownership and history boundaries**
  - **Files:**
    - `tests/Event.Architecture.Tests/ProviderMigrationOwnershipTests.cs` (existing)
    - `tests/Event.Architecture.Tests/PrimaryDatabaseMigrationCompositionTests.cs` (existing)
    - `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityModelTests.cs` (existing)
  - **Acceptance:** Tests pin application, Data Protection, and embedded-authority owners and distinct histories; no test expects one migration assembly per database engine.
  - **Effort:** S
  - **Dependencies:** 1.1

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release -- --treenode-filter "/*/*/PrimaryDatabaseProviderCompositionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Evidence:** Isolated Release build passed. `PrimaryDatabaseProviderCompositionTests` passed 37/37 twice; `ProviderMigrationOwnershipTests` passed 13/13 twice; `PrivacyErasureAuthorityModelTests` passed 9/9 twice. Aggregate: 118 passed, 0 failed, 0 skipped. No generated migration/snapshot/designer changes.

## Phase 2 — Migrator, CI, and Operator Contract ✅ COMPLETE

- [x] **2.1 Prove one migration path per selected topology**
  - **Files:**
    - `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs` (existing)
    - `src/Event.MigrationService/Worker.cs` (existing, unchanged delegation boundary)
    - `tests/Event.Persistence.IntegrationTests/Migrations/ExploreDatabaseMigratorTests.cs` (existing)
    - `tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` (existing)
  - **Acceptance:** MigrationService applies application and Data Protection migrations for the selected primary provider and exactly one authority migration path; no fallback, duplicate sink, or API-owned deployed migration is introduced.
  - **Effort:** M
  - **Dependencies:** Phase 1
  - **Independent verifier PASS:** 16 aggregate tests passed. The migrator sequence is `Application` -> `ProviderAdjustments` -> `DataProtection` -> one topology authority -> `Seed`; physical histories prove one authority destination/history and exclusivity. The missing embedded-migration mutation failed, and duplicate authority completion failed 5-vs-6.
  - **Topology QA:** Embedded SQLite, CoLocated SQLite/PostgreSQL, and External PostgreSQL each prove exactly five ordered operations and one authority destination/history. Unsupported CoLocated fails before I/O with secret-safe diagnostics.
  - **Exclusions confirmed:** `src/Event.MigrationService/Worker.cs` unchanged; no source/IL scanning, fallback, shim, dual write, duplicate sink, API ownership drift, unnecessary project reference, generated artifact, or leaked verifier cleanup asset/container.
  - **Known constraint:** The legacy SQLite migration catalog cannot replay empty-to-current because an older generated migration recreates `ie_account_authority_kinds`; no generated files were edited. SQLite establishes current model/history before migrator execution; PostgreSQL remains the fresh replay proof.

- [x] **2.2 Synchronize self-hosting and recovery documentation**
  - **Files:**
    - `docs/PRIVACY_ERASURE.md` (existing, changed)
    - `docs/CONFIGURATION.md` (existing, changed)
    - `docs/SELF_HOSTING.md` (existing, changed)
    - `docs/TROUBLESHOOTING.md` (existing, changed)
  - **Acceptance:** Passed. Operator docs agree on supported providers, migration ownership, backup units, `restoreReplayProtection`, failure remediation, and no compatibility shim. Scoped documentation diff check exited 0; `docs/BACKUP_RESTORE_UPGRADE.md` required no edit.
  - **Effort:** M
  - **Dependencies:** 2.1

- [x] **2.3 Align CI and test documentation with the contract**
  - **Files:**
    - `.github/workflows/_build-test.yml` (existing; change only if current assertions are insufficient)
    - `docs/TESTING.md` (existing)
    - `dev/active/multi-database-persistence-unification/multi-database-persistence-unification-plan.md` (existing)
    - `dev/active/multi-database-persistence-unification/multi-database-persistence-unification-context.md` (existing)
    - `dev/active/multi-database-persistence-unification/multi-database-persistence-unification-tasks.md` (existing)
  - **Acceptance:** Passed. CI retains all five primary provider lanes and twice-run MigrationService evidence; tests/docs claim only embedded SQLite, co-located PostgreSQL/SQLite, and external PostgreSQL authority support. `.github/workflows/_build-test.yml` and `docs/TESTING.md` required no edit; exact green evidence is recorded for review/audit, not workstream closure.
  - **Effort:** S
  - **Dependencies:** 2.2

### Phase 2 Verification — PASSED ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
  - **Evidence:** exited 0; 0 errors; 14,154 existing warnings, including `NU1903` for `SSH.NET` 2025.1.0.
- [x] Focused isolated suites
  - **Evidence:** all exited 0 with 0 failed and 0 skipped — `ProviderMigrationOwnershipTests` 13; `PrimaryDatabaseMigrationCompositionTests` 5; `PrimaryDatabaseProviderCompositionTests` 37; unsupported `CoLocated` providers 3; singular adapter 1; authority model 9; migrator topology 2; real PostgreSQL migrator 3 (73 total).

## Post-Review Architecture Remediation ✅ IMPLEMENTATION / TEST / VERIFICATION COMPLETE

- [x] **R.1 Enforce external-authority physical exclusivity before migration I/O**
  - **Review trigger:** The first bounded architecture review returned **ARCHITECTURE FAIL** with one HIGH finding: `MigrationService`/the external migrator could target the same physical PostgreSQL database because only runtime DI enforced distinct targets; runtime tests omitted migrator pre-I/O coverage.
  - **TDD red evidence:** `MigrateAndSeedAsync_ExternalAuthoritySameTarget_FailsBeforeMigrationIo` ran alone in a disposable detached worktree: 1 executed, 1 failed, exit 2, because no `OptionsValidationException` was thrown.
  - **Files:** `src/Explore.Secrets/Database/PrivacyErasureAuthorityDatabaseConfiguration.cs`; `src/Explore.Persistence/PersistenceServicesRegistration.cs`; `src/Event.MigrationService/Program.cs`; `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs`; focused configuration/composition/migrator tests.
  - **Acceptance:** `PrivacyErasureAuthorityDatabaseConfiguration.EnsureDistinctPhysicalDatabase` centralizes structured PostgreSQL host/loopback/port/database identity and bounded secret-safe failure; runtime persistence, `Event.MigrationService` `Program`, and `ExploreDatabaseMigrator` invoke it. Migrator preflight occurs before Application, ProviderAdjustments, DataProtection, authority, Seed, or migration-completion log I/O. No generated migration edit, fallback, shim, or dual write.
  - **Green evidence:** Release build exited 0 with 0 errors and 14,154 warnings after removing the change-caused `CS8604` without suppression. Relevant selectors passed 77/77 with 0 failed/skipped: same-target 1; `ExploreDatabaseMigratorTests` 4; topology 2; composition validation 11; authority database configuration 4; provider ownership 13; primary migration composition 5; primary provider composition 37. Same-target histories/log operations stayed empty; supported topology five-stage behavior and physical exclusivity remained exact.
  - **Final architecture approval:** **PASS**; the prior HIGH same-target finding is resolved. Residual DNS/CNAME alias behavior and disclosed legacy SQLite migration-catalog and `SSH.NET` risks remain non-blocking.
  - **Operational impact:** No operator documentation, CI, testing, or backup edit required because their existing distinct-external-target and fail-before-I/O contract is now correctly implemented.

## Post-Review Quality Remediation ✅ TEST HARDENING / VERIFICATION COMPLETE

- [x] **Q.1 Correct the two MEDIUM `ExploreDatabaseMigratorTests` test-contract findings**
  - **Review trigger:** The first replacement quality review returned **QUALITY FAIL**: `exception!` nullable suppression, and incomplete diagnostic secrecy coverage with no length bound or exclusions for host, database, username, and the full connection string.
  - **Files:** `tests/Event.Persistence.IntegrationTests/Migrations/ExploreDatabaseMigratorTests.cs` (existing; test-only fix).
  - **Acceptance:** A null exception and all structured target values convert explicitly to setup failures; no suppression remains. An explicitly typed `string[]` avoids CS9176 and asserts diagnostic length <=512, the required remediation fragment, and exclusions for host, database, username, password, and the complete connection string. Empty operation log and all three physical histories remain asserted.
  - **Trusted evidence:** An initial one-shot caught CS9176 and was then fixed. A selector that ran zero tests and a verifier that accidentally included unrelated payment hunks were discarded as verifier-construction failures, not green evidence. The established exact 12-path reconstruction passed without retry: full Release build exit 0, 0 errors, exactly 14,154 accepted warnings, no assertion-hardening diagnostic; proven `ExploreDatabaseMigratorTests` class selector 4 passed/0 failed/0 skipped, including same-target; scoped diff check 0. Worktree, metadata, containers, reports, logs, patches, and temporary artifacts were cleaned; shared worktree untouched.

## Pending Review And Audit Gates

- [x] **Final independent quality approval** — decisive review returned `QUALITY PASS` with no findings after independently confirming the corrected test contract, trusted verification, operational-documentation scope, cleanup, and warning classification.
- [x] **Final audit** — reconciled every requested implementation, documentation, isolated verification, mutation, manual QA, evidence, approval, exclusion, and direct-delivery requirement; the three workstream artifacts now agree that the workstream is complete.

## Remaining / Deferred Work

- **Five-provider `CoLocated` authority:** Not planned; OREA-1010–1018 are cancelled. Reconsider only through a new product decision plus canonical intent, recovery, security, migration, and real-engine evidence updates.
- **Migration-project consolidation:** Rejected for this workstream; reconsider only with measured project/packaging cost and a deployed migration-history transition plan.
- **Raw-SQL cleanup:** Split by repository and only after a failing provider scenario proves a defect.
- **Quartz DDL validation:** Split to a scheduling/runtime workstream covering `QuartzMultiDatabaseSchemaTests.cs`, `QuartzSchema.{SqlServer,MySql}.sql`, and `QuartzSchemaInitializer.cs`.
- **Dependency warning remediation:** Route the `SSH.NET` NU1903 advisory and build-warning reduction to their owning security/dependency workstreams.
