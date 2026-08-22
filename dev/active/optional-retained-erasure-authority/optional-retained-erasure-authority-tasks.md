<!-- ABOUTME: Execution ledger for retained privacy-erasure authority lifecycle and recovery work. -->
<!-- ABOUTME: Tracks six current tasks while preserving delivered evidence and cancelling stale five-provider expansion. -->

# Optional Retained Erasure Authority — Task Checklist

Last Updated: 2026-08-20 Europe/Brussels

## Status Summary

- **Overall status:** Re-baselined; awaiting user approval
- **Completed:** 0/6 current implementation tasks (historical evidence tracked separately)
- **Current priority:** OREA-1400 after dependency resolution
- **Next recommended slice:** Resolve master legal-hold policy and coordinate multi-database Phase 1, then add typed authority high-water/floor state
- **Known blockers:** legal-hold pseudonymization policy; overlapping provider-composition ownership

## Implementation Maintenance Rules

- Read the full workstream once at implementation start; on resume, read context/tasks first and only the relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark substantial work `🟡 IN PROGRESS`; check it immediately when acceptance criteria are met.
- Keep completed count, priority, next slice, dependencies, risks, and date accurate.
- Check a phase complete only after all task and phase-verification checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequence, acceptance, risk, or verification changes.
- Run build/tests once at phase end, not after individual tasks.
- Do not start Docker, Aspire, the application, browsers, Playwright, or live services for local verification.
- Never hand-edit generated migrations or snapshots.
- Do not resume OREA-1010–1018.

## Phase 14 — Authority State and Replay Safety ⏳ NOT STARTED

- [ ] **OREA-1400 Add typed authority state and maintenance boundaries**
  - **Files:**
    - `src/Explore.Domain/PrivacyErasureAuthorityState.cs` (new)
    - `src/Explore.Domain/PrivacyErasureCounter.cs` (existing)
    - `src/Explore.Application/Contracts/PrivacyErasure/IPrivacyErasureAuthority.cs` (existing)
    - `src/Explore.Application/Contracts/PrivacyErasure/IPrivacyErasureAuthorityMaintenance.cs` (new)
    - `src/Explore.Application/Configuration/PrivacyErasureOptions.cs` (existing)
    - `tests/Event.Domain.UnitTests/PrivacyErasureContractTests.cs` (existing)
    - `tests/Event.Application.UnitTests/Configuration/PrivacyErasureModelCompositionTests.cs` (existing)
  - **Acceptance:** High-water/floor and dry-run/apply contracts are typed, reject invalid/early maintenance before I/O, contain no sensitive values, and consume the master legal-hold policy without adding normal repository delete APIs.
  - **Effort:** M
  - **Dependencies:** Multi-database Phase 1; master privacy-erasure task 18 policy

- [ ] **OREA-1401 Make replay and readiness floor-aware**
  - **Files:**
    - `src/Explore.Application/Services/RetainedAuthorityPrivacyErasureWorkflow.cs` (existing)
    - `src/Explore.API/BackgroundServices/PrivacyErasureStartupGate.cs` (existing)
    - `src/Explore.API/HealthChecks/PrivacyErasureReadinessHealthCheck.cs` (existing)
    - `tests/Event.Application.UnitTests/Services/GlobalLocationPrivacyReplayCacheGateTests.cs` (existing)
    - `tests/Event.API.IntegrationTests/Privacy/PrivacyErasureStartupGateTests.cs` (existing)
    - `tests/Event.API.IntegrationTests/Privacy/PrivacyErasureReadinessHealthCheckTests.cs` (existing)
  - **Acceptance:** Checkpoint-ahead, below-floor, gap, rollback, and unavailable states block startup with bounded reason codes; in-range replay remains ordered/idempotent; `CoLocated` never reports restore isolation.
  - **Effort:** M
  - **Dependencies:** OREA-1400

### Phase 14 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 15 — Topology-Specific Retention and Recovery ⏳ NOT STARTED

- [ ] **OREA-1500 Implement embedded and co-located SQLite maintenance**
  - **Files:**
    - `src/Explore.Persistence/Privacy/ErasureAuthority/EmbeddedPrivacyErasureAuthorityDbContext.cs` (existing)
    - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EmbeddedPrivacyErasureAuthorityRepository.cs` (existing)
    - `src/Explore.Persistence/Privacy/ErasureAuthority/Configurations/EmbeddedPrivacyErasureCounterConfiguration.cs` (existing)
    - `src/Explore.Persistence/Privacy/ErasureAuthority/Configurations/EmbeddedPrivacyErasureIntentConfiguration.cs` (existing)
    - `src/Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite/Migrations/` (generated output)
    - `tests/Event.Persistence.IntegrationTests/Privacy/EmbeddedPrivacyErasureRecoveryTests.cs` (existing)
  - **Acceptance:** Horizon/hold eligibility, compaction, and floor advancement are atomic; held evidence is pseudonymized per policy; below-floor restore fails closed; supported replay remains idempotent; SQLite migrations are generated, not patched.
  - **Effort:** L
  - **Dependencies:** Phase 14

- [ ] **OREA-1501 Implement co-located and external PostgreSQL maintenance**
  - **Files:**
    - `src/Explore.Persistence/Privacy/ErasureAuthority/CoLocatedPrivacyErasureAuthorityDbContext.cs` (existing)
    - `src/Explore.Persistence/Privacy/ErasureAuthority/PrivacyErasureAuthorityDbContext.cs` (existing)
    - `src/Explore.Persistence/Privacy/ErasureAuthority/PrivacyErasureAuthorityDatabaseContract.cs` (existing)
    - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/CoLocatedPostgresPrivacyErasureAuthorityRepository.cs` (existing)
    - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EfCorePrivacyErasureAuthorityRepository.cs` (existing)
    - `src/Explore.Persistence/Migrations/CoLocatedPrivacyErasureAuthority/` (generated output)
    - `src/Explore.Persistence/Migrations/PrivacyErasureAuthority/` (generated output)
    - `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs` (existing)
    - `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs` (existing)
  - **Acceptance:** External maintenance remains function-only and table/migration access stays denied; co-located work remains in the primary boundary; append/maintenance concurrency cannot skip facts or over-advance floor; generated migrations/functions/grants are reproducible.
  - **Effort:** L
  - **Dependencies:** OREA-1400, OREA-1401

### Phase 15 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release -- --treenode-filter "/*/*/EmbeddedPrivacyErasureRecoveryTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`

## Phase 16 — Operator Contract and Release Closure ⏳ NOT STARTED

- [ ] **OREA-1600 Converge operator diagnostics, deployment, and recovery guidance**
  - **Files:**
    - `src/Explore.API/HealthChecks/PrivacyErasureReadinessHealthCheck.cs` (existing)
    - `src/Explore.AppHost/AppHost.cs` (existing)
    - `docker-compose.yml` (existing)
    - `.env.example` (existing)
    - `docs/PRIVACY_ERASURE.md` (existing)
    - `docs/CONFIGURATION.md` (existing)
    - `docs/SECRETS.md` (existing)
    - `docs/SELF_HOSTING.md` (existing)
    - `docs/BACKUP_RESTORE_UPGRADE.md` (existing)
    - `docs/TROUBLESHOOTING.md` (existing)
    - `docs/TESTING.md` (existing)
  - **Acceptance:** All surfaces use the approved provider matrix; embedded/external independence remains conditional; co-located atomic restore and false protection flag are explicit; diagnostics are bounded and credential/PII-free; Blazor receives no authority credentials.
  - **Effort:** M
  - **Dependencies:** Phase 15; multi-database Phase 2

- [ ] **OREA-1601 Changelog contribution and final commit composition**
  - **Files:** `docs/releases/changes/CHG-2026-0002.yaml` (new)
  - **Acceptance:** Tier 2 fragment passes release policy with `Scope: privacy`, all impact dispositions, and `Change-Id: CHG-2026-0002`; one outcome-led commit is prepared after green verification and executed only with explicit authorization; breaking syntax/footer is added only if operator action is required.
  - **Effort:** S
  - **Dependencies:** OREA-1600 and all prior phase verification

### Phase 16 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Historical Delivered Evidence

- [x] OREA-100/110/120: retained facts, monotonic sequencing, replay invariants.
- [x] OREA-200/210/220: authority-first orchestration, local transaction, outbox/receipt behavior.
- [x] OREA-300/420: authority persistence foundations and MigrationService ownership.
- [x] OREA-500/510/520: startup gate, replay, and failure-closed behavior.
- [x] OREA-600/610/700: privacy/security, bounded observability, initial operator guidance.
- [x] OREA-900–903: embedded SQLite context/migration, registration, permissions, WAL/integrity.
- [x] OREA-1000–1006: PostgreSQL/SQLite co-located composition, namespace, singular adapter, checkpoint boundary, migrator ownership.

## Superseded / Cancelled Work

- [x] **OREA-1009 superseded:** historical five-provider planning decision; not runtime evidence.
- [x] **OREA-D12/D13/D14 superseded:** five-provider co-location, all-provider namespace, and universal repository decisions.
- [x] **OREA-1010–1018 cancelled:** do not build provider-neutral co-located contexts/repositories or SQL Server/MariaDB/MySQL authority lanes.

## Remaining / Transferred Work

- Full User-PII inventory, provider settlement, receipt/status, and central fence remain in the master platform privacy-erasure plan.
- Provider capability, composition diagnostics, migration ownership tests, CI matrix, and broad support docs remain in `multi-database-persistence-unification`.
- The pre-existing `SSH.NET` advisory and build-warning reduction remain separate dependency/quality work.
