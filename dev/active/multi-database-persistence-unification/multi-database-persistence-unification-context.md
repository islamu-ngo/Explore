<!-- ABOUTME: Resumable context for the Senior CTO-rebaselined multi-database persistence contract workstream. -->
<!-- ABOUTME: Records verified support boundaries, current status, validation evidence, risks, and the next implementation slice. -->

# Multi-Database Persistence Contract Hardening — Context

Last Updated: 2026-08-20 Europe/Brussels

## SESSION PROGRESS (2026-08-20 Europe/Brussels)

### ✅ COMPLETED

- User approved the re-baselined plan and the canonical `platform-privacy-erasure` intent was added, corrected, schema-checked, and independently confirmed.
- Completed official Microsoft EF Core and ASP.NET Core Data Protection research through Anysearch and Context7 without external source code or dependency changes.
- Completed Phase 1 Tasks 1.1–1.3: provider/topology matrix tests, one shared secret-safe fail-closed diagnostic, and explicit migration assembly/history ownership contracts.
- Independently verified Phase 1 in a disposable detached worktree: Release build passed; focused suites passed 118 tests across two runs; no generated migration/snapshot/designer changes.
- Replaced Task 2.1's rejected source-scanning tests with executable EF migration scenarios and structured completion events from the real migrator.
- Independently verified Task 2.1: verifier PASS with 16 aggregate tests. The exact order is `Application` -> `ProviderAdjustments` -> `DataProtection` -> one topology authority -> `Seed`; physical histories prove exclusivity, removing the embedded migration failed, and duplicate authority completion failed 5-vs-6.
- Completed Task 2.2 with scoped changes only to `docs/PRIVACY_ERASURE.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, and `docs/TROUBLESHOOTING.md`; the scoped documentation diff check exited 0.
- Completed Task 2.3 confirmation: CI and test documentation remain correct without edits. `docs/BACKUP_RESTORE_UPGRADE.md` also required no edit.
- Passed final isolated Phase 2 Release build and focused verification. `Worker` is unchanged; no generated migration/designer/snapshot changes were made; verifier artifacts and containers were cleaned.
- Completed post-review architecture remediation after the first bounded review returned **ARCHITECTURE FAIL** with one HIGH finding: only runtime DI enforced distinct `ExternalDatabase` PostgreSQL targets, leaving `MigrationService` and the external migrator able to target the same physical database.
- Fresh final architecture review **passed** with the prior HIGH same-target finding resolved. Residual DNS/CNAME alias behavior and disclosed legacy SQLite migration-catalog and `SSH.NET` risks remain non-blocking.
- The first replacement quality review returned **QUALITY FAIL** with two MEDIUM `ExploreDatabaseMigratorTests.cs` test-contract findings: `exception!` nullable suppression and incomplete diagnostic secrecy coverage. The test-only remediation and trusted verification are complete.
- Decisive final independent quality review returned **QUALITY PASS** with no findings. The final audit reconciled every requested implementation, documentation, verification, evidence, review, exclusion, and direct-delivery requirement.

### ✅ GOVERNANCE GATES COMPLETE

- Phase 1/2 implementation, architecture and quality remediation, isolated verification, final architecture approval, final quality approval, and final audit passed.
- The workstream is complete. Reopen it only for contradictory production or migration evidence.

### ⏭️ NEXT

1. No remaining workstream action.
2. Route the disclosed DNS/CNAME identity limitation, legacy SQLite migration catalog, `SSH.NET` advisory, and warning debt through their separately owned workstreams.

### ⚠️ BLOCKERS

- The shared worktree contains extensive unrelated concurrent edits and generated migrations. Do not revert, absorb, or repair them; isolate workstream verification in disposable detached worktrees.
- Shared builds may fail because of unrelated payment/event-lifecycle changes. Such failures do not replace isolated workstream verification.
- SQL Server/MariaDB/MySQL `CoLocated` authority is deliberately outside scope. Adding it requires a new explicit product decision and canonical `platform-privacy-erasure` contract change.

## Quick Resume

1. Read this context and the pending review entries in `multi-database-persistence-unification-tasks.md`; retrieve only the relevant plan section for the review being performed.
2. Review the isolated inventory and evidence below. The shared worktree was untouched by verification; do not revert, absorb, or repair its unrelated changes.
3. Do not implement migration-project consolidation, a universal authority repository, blanket raw-SQL removal, or Quartz runtime work in this workstream.
4. Do not declare the workstream complete until final independent quality approval and final audit pass; architecture approval is complete.

## CTO Decision

**Approve with required changes.** The approved implementation hardens the existing provider contract:

- five primary application/Data Protection providers;
- `EmbeddedSqlite` authority with any primary provider;
- `CoLocated` authority on PostgreSQL/SQLite;
- `ExternalDatabase` authority on PostgreSQL.

Provider-native authority repositories and separate generated migration owners remain intentional.

## Key Files and Responsibilities

| Path | Existing/New | Layer | Responsibility |
|---|---|---|---|
| `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs` | Existing | Persistence | Closed provider switch, migration assembly, history, and namespace policy |
| `src/Explore.Persistence/PersistenceServicesRegistration.cs` | Existing | Persistence composition | Selects exactly one authority adapter and fails unsupported combinations |
| `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/CoLocatedPostgresPrivacyErasureAuthorityRepository.cs` | Existing | Persistence | PostgreSQL co-located row-lock/counter semantics |
| `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EmbeddedPrivacyErasureAuthorityRepository.cs` | Existing | Persistence | Dedicated or co-located SQLite single-writer semantics |
| `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EfCorePrivacyErasureAuthorityRepository.cs` | Existing | Persistence | External PostgreSQL function-only runtime boundary |
| `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs` | Existing | Migration orchestration | Applies application, Data Protection, and selected authority migrations |
| `src/Event.MigrationService/Worker.cs` | Existing | Deployment host | Production migration owner |
| `src/Explore.Secrets/Database/PrivacyErasureAuthorityDatabaseConfiguration.cs` | Existing | Secrets/configuration | Centralized structured PostgreSQL physical-target identity and bounded secret-safe distinct-target validation |
| `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs` | Existing | Tests | Primary provider and migration composition contract |
| `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityCompositionValidationTests.cs` | Existing | Tests | Topology registration and failure contract |
| `tests/Event.Architecture.Tests/ProviderMigrationOwnershipTests.cs` | Existing | Tests | Exact assembly/history ownership |
| `.github/workflows/_build-test.yml` | Existing | CI | Five-provider migration/idempotency matrix |

## Key Decisions

1. Keep separate application, Data Protection, and embedded-authority migration projects.
2. Keep topology-specific authority adapters behind `IPrivacyErasureAuthority`.
3. Make unsupported provider/topology combinations fail closed with bounded, secret-safe messages.
4. Keep raw SQL where it owns provider-specific lock, function, conflict, or DDL semantics.
5. Do not change privacy workflow, outboxes, authorization, HAL, BFF, or tenant-filter policy.
6. Treat Quartz multi-database DDL validation as a separate scheduling/runtime workstream.
7. Use `PrivacyErasureAuthorityDatabaseConfiguration.EnsureDistinctPhysicalDatabase` from runtime persistence, `Event.MigrationService` `Program`, and `ExploreDatabaseMigrator` for external-authority physical exclusivity; the migrator call occurs before Application, ProviderAdjustments, DataProtection, authority, Seed, or migration-completion log I/O.

## Constraints and Rules to Remember

- Generated migrations and snapshots are never hand-edited.
- Repositories return entities; handlers map DTOs.
- Normal runtime repositories keep Tenant and SoftDelete filters active.
- Authority access remains isolated to the dedicated privacy-erasure adapter.
- No new provider plugin, factory hierarchy, message broker, or dependency.
- Breaking configuration changes may fail fast; do not add compatibility shims.
- Never log secrets, DSNs, personal identifiers, provider payloads, or unbounded exception text.
- Phase verification is one Release build and at most one focused non-browser project test.
- Do not start Docker, Aspire, browsers, the app, or live services for phase verification.

## Final Isolated Verification Evidence

- `dotnet build --configuration Release --verbosity quiet`
  - **Result:** exited 0 on 2026-08-20.
  - **Errors:** 0.
  - **Warnings:** 14,154 existing warnings, including `NU1903` for `SSH.NET` 2025.1.0. Dependency remediation and warning reduction remain outside this workstream.
- Focused isolated suites all exited 0 with 0 failed and 0 skipped:
  - `ProviderMigrationOwnershipTests`: 13 passed.
  - `PrimaryDatabaseMigrationCompositionTests`: 5 passed.
  - `PrimaryDatabaseProviderCompositionTests`: 37 passed.
  - Unsupported `CoLocated` providers: 3 passed.
  - Singular adapter: 1 passed.
  - Authority model: 9 passed.
  - Migrator topology: 2 passed.
  - Real PostgreSQL migrator: 3 passed.
- **Independent Task 2.1 verifier:** PASS, 16 aggregate tests passed. It verified `Application` -> `ProviderAdjustments` -> `DataProtection` -> one topology authority -> `Seed`, physical histories and authority exclusivity, failure of the missing embedded-migration mutation, and failure of duplicate authority completion 5-vs-6.
- **Topology QA:** Embedded SQLite, CoLocated SQLite/PostgreSQL, and External PostgreSQL each execute exactly five ordered operations with one authority destination/history. Unsupported CoLocated fails before I/O and is secret-safe.
- **Documentation evidence:** Only `PRIVACY_ERASURE`, `CONFIGURATION`, `SELF_HOSTING`, and `TROUBLESHOOTING` changed; their scoped diff check exited 0. CI, `TESTING`, and `BACKUP_RESTORE_UPGRADE` required no edit.

## Post-Review Architecture Remediation Evidence

- **Initial review:** **ARCHITECTURE FAIL** with one HIGH finding. `MigrationService`/the external migrator could target the same physical PostgreSQL database because only runtime DI enforced distinct targets; runtime coverage omitted `MigrationService`/migrator pre-I/O behavior.
- **TDD red:** New `MigrateAndSeedAsync_ExternalAuthoritySameTarget_FailsBeforeMigrationIo` ran alone in a disposable detached worktree: 1 executed, 1 failed, exit 2, because no `OptionsValidationException` was thrown.
- **Root fix:** `PrivacyErasureAuthorityDatabaseConfiguration.EnsureDistinctPhysicalDatabase` centralizes structured PostgreSQL host/loopback/port/database identity and bounded secret-safe failure. Runtime persistence, `Event.MigrationService` `Program` composition, and `ExploreDatabaseMigrator` invoke it. The preflight is before Application, ProviderAdjustments, DataProtection, authority, Seed, and migration-completion log I/O. No generated migration edits, fallback, shim, or dual write.
- **Green remediation:** `dotnet build --configuration Release --verbosity quiet` exited 0 with 0 errors and 14,154 warnings after removing the change-caused `CS8604` without suppression. Relevant selectors passed 77/77 with 0 failed/skipped: same-target 1; `ExploreDatabaseMigratorTests` 4; topology 2; composition validation 11; authority database configuration 4; provider ownership 13; primary migration composition 5; primary provider composition 37. Same-target histories/log operations were empty; supported topology five-stage behavior and physical exclusivity remained exact.
- **Final recheck:** Full Release build again exited 0 with 0 errors and 14,154 warnings; single regression 1 passed/0 failed/skipped; scoped diff check exited 0; containers, worktrees, and artifacts were cleaned.
- **Final architecture approval:** **PASS**. The prior HIGH same-target finding is resolved. Residual DNS/CNAME alias behavior and the disclosed legacy SQLite migration-catalog and `SSH.NET` risks are non-blocking.
- **First replacement quality review:** **QUALITY FAIL** with two MEDIUM test-contract findings: nullable suppression via `exception!`, and incomplete diagnostic secrecy coverage lacking a <=512 bound and exclusions for host, database, username, and the complete connection string.
- **Test-only quality remediation:** `ExploreDatabaseMigratorTests.cs` explicitly converts a null exception and every structured target value into setup failures; its explicitly typed `string[]` avoids CS9176 and asserts diagnostic length <=512, the required remediation fragment, exclusions for host/database/username/password/complete connection string, plus the retained empty operation log and all three physical histories. No suppression remains.
- **Trusted final remediation verifier:** An initial one-shot caught CS9176 and was fixed. A zero-test selector and a verifier contaminated by unrelated payment hunks were discarded as verifier-construction failures, not green evidence. The established exact 12-path reconstruction passed without retry: full Release build exit 0, 0 errors, exactly 14,154 accepted warnings, no assertion-hardening diagnostic; proven `ExploreDatabaseMigratorTests` class selector 4 passed/0 failed/0 skipped including same-target; scoped diff check 0. Worktree, metadata, containers, reports, logs, patches, and temporary artifacts were cleaned; shared worktree untouched.
- **Final independent quality approval:** **QUALITY PASS** with no findings. The review confirmed the corrected null contracts, complete bounded diagnostic secrecy assertions, retained physical pre-I/O proof, trusted verifier evidence, cleanup, and unchanged architecture/documentation scope.
- **Approval state:** Complete. Final architecture and quality approvals passed, and the final audit reconciled all workstream requirements and evidence.
- **No additional operational surface:** No operator documentation, CI, testing, or backup edit was required because their existing distinct-external-target and fail-before-I/O contract is now correctly implemented.

## Isolated Implementation Inventory

- `src/Event.MigrationService/Program.cs`
- `src/Explore.Secrets/Database/PrivacyErasureAuthorityDatabaseConfiguration.cs`
- `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs`
- `src/Explore.Persistence/PersistenceServicesRegistration.cs`
- `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs`
- `tests/Event.Architecture.Tests/ProviderMigrationOwnershipTests.cs`
- `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs`
- `tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj`
- `tests/Event.Persistence.IntegrationTests/Migrations/ExploreDatabaseMigratorTests.cs`
- `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityCompositionValidationTests.cs`
- `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityModelTests.cs`
- `tests/Event.Persistence.IntegrationTests/packages.lock.json` only for its SQLite Data Protection project entry.

## Verified Exclusions

- `src/Event.MigrationService/Worker.cs` is unchanged.
- No generated migration, designer, or snapshot was edited.
- No fallback, compatibility shim, dual write, duplicate sink, or API migration-ownership drift was introduced.
- The shared worktree was untouched by isolated verification; containers, worktrees, and artifacts were cleaned.
- No operator documentation, CI, testing, or backup material was edited for remediation because its existing distinct-external-target and fail-before-I/O contract is now correctly implemented.

## Current Known Risks / Unknowns

- Future scope drift could reintroduce unsupported co-located providers; both active workstreams now pin PostgreSQL/SQLite and cancel the prior five-provider tasks.
- The current warning volume can obscure new warnings during implementation.
- The `SSH.NET` advisory must be resolved before claiming enterprise release readiness.
- DNS/CNAME alias behavior remains a non-blocking residual architecture-review risk.
- No evidence currently justifies migration-project consolidation or blanket SQL replacement.
- The legacy SQLite application migration catalog cannot replay from an empty database because an older generated migration recreates `ie_account_authority_kinds`. Do not hand-edit generated artifacts; the current Task 2.1 fixture establishes current SQLite model/history, while PostgreSQL supplies fresh replay evidence.
- A previous source-scanning Task 2.1 suite passed even when the embedded-authority migration call was deleted. That implementation was rejected and removed; future verification must retain mutation probes.

## Deferred Work

- Five-provider co-located authority expansion.
- Migration-project consolidation.
- Repository-specific raw-SQL changes without a failing provider scenario.
- Quartz DDL runtime verification from the prior local planning change.

## Handoff Notes

### Handoff — 2026-08-20 Europe/Brussels

- **Current state:** Complete. Phase 1/2 implementation, architecture remediation, final architecture approval, quality-failure test hardening, final quality approval, and the completion audit all passed. The prior HIGH same-target architecture finding is resolved; residual DNS/CNAME alias and disclosed legacy SQLite/`SSH.NET` risks are non-blocking.
- **Evidence:** The first replacement quality review was **QUALITY FAIL** on `exception!` suppression and incomplete diagnostic secrecy. The test-only fix removed suppression, added explicit null-to-setup-failure conversion, typed `string[]`, a <=512 diagnostic bound, the required remediation fragment, exclusions for host/database/username/password/full connection string, and retained empty log/three-history assertions. The trusted exact-12-path verifier passed without retry: build exit 0, 0 errors, exactly 14,154 accepted warnings; proven class selector 4 passed/0 failed/0 skipped including same-target; scoped diff check 0. The decisive independent review returned **QUALITY PASS**, and the final audit reconciled all requested work.
- **Next action:** None. Reopen only for contradictory production or migration evidence.
- **Isolation and exclusions:** The shared worktree was untouched. No generated migration/designer/snapshot edits, fallback, shim, dual write, duplicate sink, API ownership drift, or leaked verifier worktree/metadata/container/report/log/patch/temp artifact remains.
- **Documentation:** Only `docs/PRIVACY_ERASURE.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, and `docs/TROUBLESHOOTING.md` changed; scoped diff check exited 0. `.github/workflows/_build-test.yml`, `docs/TESTING.md`, and `docs/BACKUP_RESTORE_UPGRADE.md` needed no edit.
