<!-- ABOUTME: Task checklist and execution ledger for Multi-Database Persistence Unification and API-First Architecture. -->
<!-- ABOUTME: Hot progress ledger tracking implementation tasks and phase-end verification. -->

# Multi-Database Persistence Unification & API-First Architecture — Task Checklist

Last Updated: 2026-08-19 Europe/Brussels

## Status Summary
- **Overall status:** User-reviewed
- **Completed:** 0/11 implementation tasks (phase verification tracked separately)
- **Current priority:** Phase 1 (API-Side Invariant & Privacy Authority Unification)
- **Next recommended slice:** Task 1.1 (`ImmutableEntityInterceptor.cs`)

## Implementation Maintenance Rules
- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Check a substantial completed task immediately; reconcile small completed tasks no later than phase end.
- Check a phase complete only after all implementation and phase-verification checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance criteria, risk, or validation strategy changes.
- Do not run build/tests after individual tasks; verify once at phase end.

---

## Phase 1: API-Side Invariant & Privacy Authority Unification ⏳ NOT STARTED

- [ ] **1.1 Create `ImmutableEntityInterceptor` for API-Side Fact Immutability**
  - **Files:** `src/Explore.Persistence/Interceptors/ImmutableEntityInterceptor.cs` (new)
  - **Acceptance:** Interceptor blocks updates and deletes for immutable privacy facts in memory before SQL execution.
  - **Effort:** S
  - **Dependencies:** None

- [ ] **1.2 Seed Counter in `PrivacyErasureCounterConfiguration`**
  - **Files:** `src/Explore.Persistence/Privacy/ErasureAuthority/Configurations/PrivacyErasureCounterConfiguration.cs` (existing)
  - **Acceptance:** Singleton counter row is configured with standard EF Core `builder.HasData()`.
  - **Effort:** S
  - **Dependencies:** Task 1.1

- [ ] **1.3 Create Unified `CoLocatedPrivacyErasureAuthorityRepository`**
  - **Files:** `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/CoLocatedPrivacyErasureAuthorityRepository.cs` (new)
  - **Acceptance:** Uses `RelationalNamedLock.AcquireTransactionAsync` and pure LINQ with zero raw SQL strings. Works on all 5 DB providers.
  - **Effort:** M
  - **Dependencies:** Task 1.2

- [ ] **1.4 Retire Legacy Provider-Specific Authority Repositories and Register Unified Repository**
  - **Files:**
    - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/CoLocatedPostgresPrivacyErasureAuthorityRepository.cs` (delete)
    - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EmbeddedPrivacyErasureAuthorityRepository.cs` (delete)
    - `src/Explore.Persistence/PersistenceServicesRegistration.cs` (modify)
  - **Acceptance:** `CoLocated` topology in `PersistenceServicesRegistration` supports all 5 providers without throwing provider exceptions.
  - **Effort:** S
  - **Dependencies:** Task 1.3

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 2: Repository Raw SQL Elimination & Named Lock Standardization ⏳ NOT STARTED

- [ ] **2.1 Convert Raw Updates in `EventAgendaItemRepository` & `RegistrationInventoryRepository` to LINQ**
  - **Files:**
    - `src/Explore.Persistence/Repositories/EventAgendaItemRepository.cs` (existing)
    - `src/Explore.Persistence/Repositories/RegistrationInventoryRepository.cs` (existing)
  - **Acceptance:** Raw SQL strings eliminated; updates execute as single-roundtrip LINQ `ExecuteUpdateAsync` batch operations.
  - **Effort:** M
  - **Dependencies:** Phase 1

- [ ] **2.2 Standardize Advisory Locks to `RelationalNamedLock`**
  - **Files:**
    - `src/Explore.Persistence/Repositories/RegistrationFinalizationRepository.cs` (existing)
    - `src/Explore.Persistence/Repositories/IncomingWebhookEffectOutboxRepository.cs` (existing)
    - `src/Explore.Persistence/Repositories/IncomingWebhookMessageRepository.cs` (existing)
    - `src/Explore.Persistence/Repositories/RegistrationProviderSubscriptionStateRepository.cs` (existing)
    - `src/Explore.Persistence/Repositories/RegistrationProviderSubmissionWriteEffectRepository.cs` (existing)
    - `src/Explore.Persistence/Repositories/PdsSyncOutboxRepository.cs` (existing)
    - `src/Explore.Persistence/Repositories/WebhookLocalTargetRepository.cs` (existing)
    - `src/Explore.Persistence/Repositories/AtprotoJetstreamRepository.cs` (existing)
  - **Acceptance:** Zero raw SQL advisory lock strings across all repositories; transparently supports all 5 database providers.
  - **Effort:** M
  - **Dependencies:** Task 2.1

- [ ] **2.3 Convert `NotificationFanoutOccurrenceRepository` to Database-Agnostic LINQ**
  - **Files:** `src/Explore.Persistence/Repositories/NotificationFanoutOccurrenceRepository.cs` (existing)
  - **Acceptance:** Raw SQL with `GREATEST` replaced by provider-neutral LINQ update.
  - **Effort:** S
  - **Dependencies:** Task 2.2

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 3: Project Consolidation & Provider Composition (Option 2: 1 Assembly Per Database) ⏳ NOT STARTED

- [ ] **3.1 Update `PrimaryDatabaseProviderComposition.cs` and `ExploreDatabaseMigrator.cs`**
  - **Files:**
    - `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs` (existing)
    - `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs` (existing)
  - **Acceptance:** `GetMigrationsAssemblyName` routes all targets to `Explore.Persistence.Migrations.{Provider}` per provider.
  - **Effort:** M
  - **Dependencies:** Phase 2

- [ ] **3.2 Delete 5 Redundant Projects and Remove from Solution**
  - **Files:**
    - `src/Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite/` (delete)
    - `src/Explore.Persistence.DataProtection.Migrations.Sqlite/` (delete)
    - `src/Explore.Persistence.DataProtection.Migrations.SqlServer/` (delete)
    - `src/Explore.Persistence.DataProtection.Migrations.MySql/` (delete)
    - `src/Explore.Persistence.DataProtection.Migrations.MariaDb/` (delete)
    - `Event.sln` (modify)
  - **Acceptance:** 5 projects deleted; solution contains only 4 provider migration projects plus core `Explore.Persistence`.
  - **Effort:** S
  - **Dependencies:** Task 3.1

- [ ] **3.3 Update Host and Service `.csproj` References**
  - **Files:**
    - `src/Event.MigrationService/Event.MigrationService.csproj` (existing)
    - `src/Event.Standalone/Event.Standalone.csproj` (existing)
    - `src/Explore.API/Explore.API.csproj` (existing)
  - **Acceptance:** Host and service projects reference only the 4 remaining provider migration assemblies.
  - **Effort:** S
  - **Dependencies:** Task 3.2

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 4: Schema Migrations & Final Multi-Database Verification ⏳ NOT STARTED

- [ ] **4.1 Update Architecture Tests for Consolidated Assemblies**
  - **Files:** `tests/Event.Architecture.Tests/PrimaryDatabaseMigrationCompositionTests.cs` (existing)
  - **Acceptance:** `PrimaryDatabaseMigrationCompositionTests` asserts that all migration targets resolve to the unified per-provider assembly.
  - **Effort:** S
  - **Dependencies:** Phase 3

- [ ] **4.2 Full Solution Release Build and Persistence Test Verification**
  - **Files:** `tests/Event.Persistence.IntegrationTests/` (existing)
  - **Acceptance:** Full solution release build succeeds with zero errors; architecture and integration tests pass cleanly.
  - **Effort:** M
  - **Dependencies:** Task 4.1

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Remaining / Deferred Work
- None. Full migration consolidation and raw SQL elimination covered across Phases 1–4.
