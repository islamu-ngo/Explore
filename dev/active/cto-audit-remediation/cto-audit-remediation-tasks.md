<!-- ABOUTME: Hot execution ledger for the CTO audit remediation workstream. -->
<!-- ABOUTME: Tracks granular tasks, verification gates, and commit contracts across 6 phases. -->

# CTO Audit Remediation — Task Checklist

Last Updated: 2026-09-02 Europe/Brussels

## Status Summary

- **Overall status:** Implementation active
- **Completed:** 2/25 implementation tasks
- **Current priority:** Phase 1, Task 1.3
- **Next recommended slice:** Phase 1 — Merge MariaDb into MySql
- **Review state:**
  - I-VSD: `i-vsd-cto-audit-remediation.md` | `current` + `plan-aligned`
  - CTO review: Not reviewed
  - User approval: Approved by the explicit implementation request on 2026-09-02

## Implementation Maintenance Rules

- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Mark tasks `🟡 IN PROGRESS` when spanning multiple edits; `[x]` when done.
- Do not run build/tests after individual tasks; verify once at phase end.
- Close every verified phase immediately with a phase-owned Conventional Commit.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Do not start the app, browser, Docker, Aspire, or live services for verification.

---

## Phase 1: Merge MariaDb Migration Projects into MySql Provider ⏳

- [x] **1.1 Update `GetMigrationsAssemblyName` to route MariaDb to MySql assembly names**
  - **Files:** `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs`, `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs`, `tests/Event.Architecture.Tests/ProviderMigrationOwnershipTests.cs`
  - **Acceptance:** `GetMigrationsAssemblyName(MariaDb, Application)` → `"Explore.Persistence.Migrations.MySql"`, `GetMigrationsAssemblyName(MariaDb, DataProtection)` → `"Explore.Persistence.DataProtection.Migrations.MySql"`
  - **Effort:** S

- [x] **1.2 Delete MariaDb migration projects and remove all references**
  - **Files:** DELETE `src/Explore.Persistence.Migrations.MariaDb/`, DELETE `src/Explore.Persistence.DataProtection.Migrations.MariaDb/`; MODIFY `Explore.slnx`, `src/Event.MigrationService/Event.MigrationService.csproj`, `src/Event.MigrationService/Dockerfile`, `src/Event.Standalone/Dockerfile`, `.github/workflows/test.yml`, `tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj`, `tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj`, the five live migration-ownership test/helper files, four directly affected `packages.lock.json` files, `.agents/contract/intents.yaml`, `.agents/benchmarks/cold-start-tasks.yaml`, and the generated dependency report when its canonical generator confirms ownership.
  - **Acceptance:** No live build, test, container, agent-contract, lockfile, or generated dependency artifact references either deleted project; MariaDb runtime/provider support remains intact; historical plan evidence is not rewritten.
  - **Effort:** M | **Depends:** 1.1

- [ ] **1.3 Update documentation**
  - **Files:** `docs/internal/CONFIGURATION.md`, `docs/internal/OPERATIONS.md`, `docs/internal/SELF_HOSTING.md`, `docs/public/documentation/readme/configuration-and-operations/backup-restore-upgrade.md`
  - **Acceptance:** Current internal and public operator guidance states that MariaDb remains a distinct runtime provider but shares MySql application and Data Protection migration assemblies; no current documentation recommends MariaDb-specific migration projects or commands; historical migration-head evidence remains unchanged.
  - **Effort:** S | **Depends:** 1.2

### Phase 1 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 1 Commit
- **Title:** `refactor(database): merge MariaDb migration assembly into MySql provider`
- **Description:** Route MariaDb to the MySql migration assembly in PrimaryDatabaseProviderComposition since both providers use Pomelo UseMySql() and produce identical migration output. Delete Explore.Persistence.Migrations.MariaDb and Explore.Persistence.DataProtection.Migrations.MariaDb projects, removing ~250K lines of duplicate migration code.
- **Trailers:** `Changelog: skip` + `Changelog-Reason: internal migration project consolidation with no runtime behavior change`
- [ ] Stage, commit, and verify with `git show --name-only --format=fuller HEAD`

---

## Phase 2: Squash All Provider Migrations into Single InitialCreate ⏳

> **Atomic execution note:** Tasks 2.1-2.4 form one generated-artifact cutover. The eight remaining migration projects contain eleven provider/context catalogs; deletion must not be verified or committed until every catalog has a generated `InitialCreate`.

- [ ] **2.1 Delete all existing migration files across all providers**
  - **Files:** All generated migration/designer/snapshot `*.cs` files across the eight remaining migration projects and the PostgreSQL co-located context sub-directories
  - **Effort:** S (mechanical deletion)

- [ ] **2.2 Regenerate single InitialCreate for PostgreSQL ExploreDbContext**
  - **Files:** NEW `src/Explore.Persistence/Migrations/` (3 files)
  - **Effort:** M | **Depends:** 2.1

- [ ] **2.3 Regenerate InitialCreate for non-PostgreSQL ExploreDbContext providers (MySql, SqlServer, Sqlite)**
  - **Files:** NEW migration files in each provider project (3 files each)
  - **Effort:** M | **Depends:** 2.1

- [ ] **2.4 Regenerate InitialCreate for all DataProtection and PrivacyErasureAuthority contexts**
  - **Files:** NEW migration files across all provider/context combinations
  - **Effort:** M | **Depends:** 2.1

- [ ] **2.5 Add `.gitattributes` entries for migration generated files**
  - **Files:** `.gitattributes`, `docs/internal/OPERATIONS.md`
  - **Acceptance:** Generated migration paths are marked `linguist-generated`; repository commands consistently generate `InitialCreate` for all eleven provider/context catalogs.
  - **Effort:** S

### Phase 2 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 2 Commit
- **Title:** `refactor(database): squash accumulated migrations into single InitialCreate per provider`
- **Description:** Delete all accumulated development migrations and regenerate a single InitialCreate for each provider and context combination. Safe because the platform is pre-release with zero production databases. Add linguist-generated markers to .gitattributes.
- **Trailers:** `Changelog: skip` + `Changelog-Reason: internal migration history reset in pre-release greenfield`
- [ ] Stage, commit, and verify

---

## Phase 3: Split NSwag Monolithic Client into Per-Tag Clients ⏳

- [ ] **3.1 Update Roslyn transformer to handle multiple generated client interfaces**
  - **Files:** `eng/tools/Explore.GeneratedContracts/GeneratedContractPolicy.cs`
  - **Acceptance:** `DiscoverProtocolInputTypes` finds ALL generated client interfaces instead of `Single()` on `IEventApiClient`
  - **Effort:** M

- [ ] **3.2 Change NSwag config and regenerate the API client**
  - **Files:** `src/Explore.Blazor.Client/nswag.json`, `Clients/EventApiClient.g.cs` (regenerated)
  - **Acceptance:** `operationGenerationMode: "MultipleClientsFromFirstTagAndOperationId"`, `className: "{controller}Client"`. All 894 endpoints covered.
  - **Effort:** M | **Depends:** 3.1

- [ ] **3.3 Create DI registration helpers for multi-client setup**
  - **Files:** `Program.cs`, `HttpClientExtensions.cs`
  - **Effort:** M | **Depends:** 3.2

- [ ] **3.4 Update partial class hooks for multi-client**
  - **Files:** `Clients/EventApiClient.cs` → per-client partials or DelegatingHandler
  - **Effort:** M | **Depends:** 3.2

- [ ] **3.5 Update all ~85 service files to inject per-tag client interfaces**
  - **Files:** `src/Explore.Blazor.Client/Services/**/*.cs`
  - **Effort:** L (mechanical, high volume) | **Depends:** 3.2, 3.3

- [ ] **3.6 Update all ~17 BFF endpoint files**
  - **Files:** `src/Explore.Blazor/Extensions/Bff*.cs`
  - **Effort:** M | **Depends:** 3.2, 3.3

- [ ] **3.7 Update service contract interface comments**
  - **Files:** `src/Explore.Blazor.Client/Contracts/Services/**/*.cs`
  - **Effort:** S | **Depends:** 3.5

- [ ] **3.8 Update architecture and contract tests**
  - **Files:** ~9 test files across `Event.Architecture.Tests`, `Explore.Blazor.Client.Tests`, `Explore.GeneratedContracts.Tests`, `Event.Standalone.IntegrationTests`
  - **Effort:** M | **Depends:** 3.2, 3.5, 3.6

### Phase 3 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 3 Commit
- **Title:** `refactor(architecture): split monolithic NSwag client into per-tag clients`
- **Description:** Change NSwag operationGenerationMode from SingleClientFromOperationId to MultipleClientsFromFirstTagAndOperationId, generating ~161 per-tag client classes. Update Roslyn transformer, DI registration, ~85 service files, ~17 BFF endpoints, and architecture tests.
- **Trailers:** `Changelog: skip` + `Changelog-Reason: internal API client architecture improvement`
- [ ] Stage, commit, and verify

---

## Phase 4: Controller Base Standardization & Concurrency Stamp Migration ⏳

### Sub-phase 4A: Standardize EventControllerBase and Migrate Duplicators

- [ ] **4.1 Rename `ExploreControllerBase.cs` to `EventControllerBase.cs` and update existing derived controllers**
  - **Files:** [RENAME/MODIFY] `src/Explore.API/Controllers/ExploreControllerBase.cs` → `src/Explore.API/Controllers/EventControllerBase.cs`, [MODIFY] ~55 existing derived controllers and domain-family bases (`InstanceSettingsControllerBase`, `ConfigurationImportSessionsControllerBase`, etc.) to inherit `EventControllerBase`
  - **Acceptance:** Class and file renamed to `EventControllerBase`. Solution builds cleanly with existing controllers inheriting `EventControllerBase`.
  - **Effort:** M (mechanical rename & namespace check)

- [ ] **4.2 Change 17 controllers from `ControllerBase` to `EventControllerBase` and delete duplicate `TryParseConcurrencyStamp`**
  - **Files:** `CategoryController.cs`, `CustomPropertyDefinitionController.cs`, `EventAgendaItemController.cs`, `EventCustomPropertyController.cs`, `EventDayController.cs`, `EventParticipationController.cs`, `EventSeriesController.cs`, `EventSessionController.cs`, `EventSessionCustomPropertyController.cs`, `EventSessionGroupController.cs`, `EventSessionLanguageController.cs`, `EventSessionSpeakerController.cs`, `EventSessionTemplateController.cs`, `EventTemplateController.cs`, `LocationController.cs`, `LocationRoomController.cs`, `RegistrationFormsController.cs`
  - **Acceptance:** Zero remaining private `TryParseConcurrencyStamp` methods. `grep -rn 'private static bool TryParseConcurrencyStamp' src/Explore.API/Controllers` returns 0 results. All 17 controllers inherit `EventControllerBase`.
  - **Effort:** M (mechanical) | **Depends:** 4.1

- [ ] **4.3 Update architecture tests to enforce `EventControllerBase`**
  - **Files:** `tests/Event.Architecture.Tests/CodeHygieneTests.cs`
  - **Acceptance:** `ControllersAccessingIdentity_ShouldInherit_ExploreControllerBase` updated to check `EventControllerBase`. Architecture tests pass.
  - **Effort:** S | **Depends:** 4.1, 4.2

*(Note: Generic `CrudControllerBase` and `LookupControllerBase` have been formally dropped from scope to preserve concrete controller transparency, OpenAPI metadata fidelity, and independent evolvability for upcoming backlog features.)*

### Phase 4 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Verify zero duplicate `TryParseConcurrencyStamp`: `grep -rn 'private static bool TryParseConcurrencyStamp' src/Explore.API/Controllers` (must return 0)
- [ ] Verify OpenAPI spec endpoint count unchanged: `grep -c '"operationId"' schemas/openapi_islamu-event.json`

### Phase 4 Commit
- **Title:** `refactor(api): standardize EventControllerBase and eliminate duplicate concurrency parsing`
- **Description:** Rename ExploreControllerBase to EventControllerBase to align with canonical ISLAMU Event architecture. Migrate 17 controllers from ControllerBase to EventControllerBase and remove duplicate private TryParseConcurrencyStamp methods. Reject generic CRUD and lookup bases to preserve concrete controller authoring, OpenAPI metadata fidelity, and route customization. Zero behavioral change to API endpoints.
- **Trailers:** `Changelog: skip` + `Changelog-Reason: internal controller base standardization`
- [ ] Stage, commit, and verify

---

## Phase 5: Compiler Warning Ratchet ⏳

- [ ] **5.1 Enable `TreatWarningsAsErrors: true` and capture all warning categories**
  - **Files:** `Directory.Build.props`
  - **Acceptance:** `TreatWarningsAsErrors` set to `true`. Initial build captures all failing warning codes.
  - **Effort:** S

- [ ] **5.2 Add temporary `WarningsNotAsErrors` for all failing categories**
  - **Files:** `Directory.Build.props`
  - **Acceptance:** Build passes with `TreatWarningsAsErrors: true` and temporary suppressions. Document all suppressed codes.
  - **Effort:** S | **Depends:** 5.1

- [ ] **5.3 Fix nullable warnings (CS8xxx) and remove suppression**
  - **Files:** Hundreds of source files across the solution
  - **Acceptance:** All CS8xxx nullable warnings resolved. `nullable` removed from `WarningsNotAsErrors`.
  - **Effort:** XL | **Depends:** 5.2

- [ ] **5.4 Fix security analyzer warnings (CA2xxx) and remove suppression**
  - **Files:** Source files flagged by security analyzers
  - **Acceptance:** All CA2xxx warnings resolved or individually documented with `[SuppressMessage]` + justification.
  - **Effort:** L | **Depends:** 5.2

- [ ] **5.5 Fix remaining analyzer warnings (CA1xxx, IDE, etc.) and remove all suppressions**
  - **Files:** Remaining source files with design/style warnings
  - **Acceptance:** Zero `WarningsNotAsErrors` suppressions remain (or a documented minimal allowlist). Build passes with zero warnings.
  - **Effort:** L | **Depends:** 5.3, 5.4

### Phase 5 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet` (zero warnings)

### Phase 5 Commit(s)
> Phase 5 may be split into multiple commits per warning category. Each commit:
- **Title pattern:** `refactor(build): fix {category} warnings and remove suppression`
- **Trailers:** `Changelog: skip` + `Changelog-Reason: internal compiler warning remediation`
- [ ] Stage, commit, and verify per category batch

---

## Phase 6: Tenant Isolation Hardening — PostgreSQL RLS ⏳

> [!WARNING]
> **Tier 1 Security Work.** The implementing agent must follow the `criticality-guardrail` skill. Write invariant-breaker tests FIRST (fail without RLS, pass after).

- [ ] **6.1 Inventory all tenant-scoped entity tables requiring RLS**
  - **Files:** Analysis of `ExploreDbContext.QueryFilters.cs` to enumerate all entities with `QueryFilterNames.Tenant`
  - **Acceptance:** Complete list of tables needing `ENABLE ROW LEVEL SECURITY` and `CREATE POLICY`
  - **Effort:** S

- [ ] **6.2 Write invariant-breaker tests (FAIL first, PASS after RLS)**
  - **Files:** [NEW] Test class in `Event.Persistence.IntegrationTests`
  - **Acceptance:** Tests prove: (a) `IgnoreQueryFilters()` without RLS returns other tenant's data, (b) Raw SQL without WHERE returns other tenant's data, (c) After RLS, both scenarios return ONLY current tenant's data
  - **Effort:** L
  - **Depends:** 6.1

- [ ] **6.3 Create EF Core migration adding RLS policies for all tenant tables**
  - **Files:** [NEW] Migration via `dotnet ef migrations add AddTenantRowLevelSecurity`
  - **Acceptance:** PostgreSQL-conditional migration adds `ENABLE ROW LEVEL SECURITY` + `CREATE POLICY tenant_isolation ... USING (tenant_id = current_setting('app.current_tenant_id')::uuid)` for every tenant-scoped table. Non-PostgreSQL providers are unaffected (migration is provider-conditional).
  - **Effort:** L
  - **Depends:** 6.2

- [ ] **6.4 Verify runtime PostgreSQL role has `NOBYPASSRLS`**
  - **Files:** Verify `PrimaryDatabaseProviderComposition` and database contract files
  - **Acceptance:** Runtime role cannot bypass RLS. Only migration role may bypass.
  - **Effort:** S
  - **Depends:** 6.3

- [ ] **6.5 Update tenant isolation documentation**
  - **Files:** `docs/internal/QUICK_REFERENCE.md`, `docs/CONFIGURATION.md`
  - **Acceptance:** Documents RLS as defense-in-depth layer alongside EF Core query filters
  - **Effort:** S
  - **Depends:** 6.4

### Phase 6 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] Invariant-breaker tests pass (RLS enforcement verified)

### Phase 6 Commit
- **Title:** `feat(access): add PostgreSQL row-level security for tenant isolation`
- **Description:** Add RLS policies on all tenant-scoped entity tables using the existing PostgresTenantSessionInterceptor that sets app.current_tenant_id. Defense-in-depth layer that prevents cross-tenant data access even when EF Core query filters are bypassed via IgnoreQueryFilters() or raw SQL. Includes invariant-breaker tests.
- **Trailers:** None (this is a feat, not skipped from changelog)
- [ ] Stage, commit, and verify

---

## Remaining / Deferred Work

- **Cross-replica cache invalidation** — deferred; requires distributed cache architecture decision (Redis pub/sub vs. database change notification). Trigger: CTO review or scaling discussion.
- **Evaluate Kiota for long-term API client generation** — deferred per user instruction.
- **Consider reducing providers from 5 to 2 (PostgreSQL + SQLite)** — deferred per user scope.
- **Privacy erasure authority end-to-end tests** — can be added as a follow-up to Phase 6.
