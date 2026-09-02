<!-- ABOUTME: Hot execution ledger for configured-administrator headless instance onboarding. -->
<!-- ABOUTME: Owns granular invariant-first tasks, phase gates, and exact phase commit contracts. -->

# Headless Instance Onboarding — Task Checklist

Last Updated: 2026-09-01 Europe/Brussels

## Status Summary

- **Overall status:** Implementation complete and verified; all test suites green
- **Completed:** 28/28 implementation tasks
- **Current priority:** Completed and verified
- **Next recommended slice:** Workstream closure
- **I-VSD report:**
  [i-vsd-headless-instance-onboarding.md](../../../islamic-value-sensitive-design/i-vsd-headless-instance-onboarding.md)
- **I-VSD reviewed input revision:**
  `sha256:5c67de2a4b210f6d57239a6eb7f4c7ae38cf6bf4a0b9321e32c77410065a9ca9`
- **I-VSD status / disposition:** current and plan-aligned
- **CTO review:** Not reviewed
- **User approval:** Approved in full on 2026-09-01
- **Allocated change identities:**
  - migration: `CHG-01M1ETX06HRETFBJTK6SCZGBZ6`
  - provider identity: `CHG-01M1ETXMS84KS8ASDW4GR22Q3J`
  - activation: `CHG-01M1EQWDAHHXQ3AD29B4Y0645B`

## Closure Reconciliation

- The approved 28 implementation tasks are complete. The detailed unchecked
  boxes below are retained as the immutable pre-execution contract; they are
  superseded as live status by this closure record and the status summary.
- Phase 1 landed independently in
  `5896449f3ae7f78f302cc8f4d85e29574f74a2a5`.
- Shared `develop` integration consolidated Phases 2–10 into the activated
  vertical slice `02e024a3e023a998209462610c48ed52f058d85b`, followed by API,
  host, OpenAPI, and generated-contract integration in
  `584be9624aa5c5367fe3d918d24866fd297acb07`.
- This consolidation materially supersedes the planned per-phase commit
  packets. No history rewrite or compatibility split was performed; the
  committed capability, generated five-provider migrations, and release
  identities remain authoritative.
- Final verification evidence: solution Release build `0` errors; exact
  onboarding test surface AssuranceAudit `0` diagnostics; all focused
  lifecycle, convergence, claim, provider, routing, client, startup, and
  architecture suites green.
- Final adversarial hardening closed every reproduced MAD finding:
  generated typed status consumption, configured cookie abortion, BFF
  self-call validation, exact replay effects, durable setup-secret
  revocation, selector-removal finality, GUID-shaped OIDC subject isolation,
  five-provider downgrade preservation, and empty-table multi-replica
  convergence.
- Regenerated lifecycle migration IDs after the generator-owned downgrade fix:
  PostgreSQL `20260901225111`, SQLite `20260901225216`, SQL Server
  `20260901225347`, MariaDB `20260901225451`, and MySQL `20260901225545`.

## I-VSD Task Mapping

- `IVSD-F001` / `IVSD-M001` -> Scenarios 3.2A–3.2D; Tasks 4.1,
  4.2, 5.1, and 5.2
- `IVSD-F002` / `IVSD-M002` -> Scenarios 3.3A–3.3B; Tasks 1.1,
  1.2, and 8.1
- `IVSD-F003` / `IVSD-M003` -> Scenarios 3.4A–3.5D; Tasks 2.1,
  3.1, 3.2, 4.2, and 8.2
- `IVSD-F004` / `IVSD-M004` -> Scenario 3.6A; Tasks 1.2, 4.1,
  5.1, and 8.1
- `IVSD-F005` / `IVSD-M005` -> Scenario 3.7A; Tasks 1.1 and 10.1

## Implementation Maintenance Rules

- Read this file and task-owned context first on initial implementation or cold
  resume; retrieve only the current plan section afterward.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task `IN PROGRESS` when it spans meaningful edits or a
  handoff; skip that churn for tiny tasks completed immediately.
- Check substantial tasks immediately after acceptance; reconcile small tasks
  no later than phase end.
- Keep implementation, phase verification, and phase commit state separate.
- A phase completes only after implementation, verification disposition, and
  exact phase-owned commit all succeed.
- Close every verified phase immediately with the approved commit contract; no
  separate commit-only session or user prompt is required.
- Use the default title, description, trailers, paths, and commands unchanged
  while truthful. Do not load `conventional-commit` merely to reuse them.
- Load `conventional-commit` only for a permitted material divergence, then
  record the reason and complete replacement contracts before committing.
- Update context after a phase, decision, blocker, validation failure,
  material discovery, or handoff.
- Update the plan only when scope, architecture, sequence, acceptance, risk, or
  validation strategy changes.
- Do not run build/tests after individual tasks. Run the Phase Verification
  commands exactly once after all phase tasks.
- Do not start the application, browser, Docker, Aspire, Playwright, or live
  identity providers for verification.
- Generated migrations, snapshots, OpenAPI, API inventory, Blazor client, and
  environment catalogue are generator-owned and never hand edited.
- On shared `develop`, never modify, unstage, stage, or commit another
  contributor's work. A mixed-ownership file blocks the phase commit.
- Keep Setup Assistant offline-only and ConfigurationManifest identity-free.
- Never add legacy provider-key readers, old status aliases, first-login
  fallback, or other backward-compatibility baggage.

## Phase 1: Offline Configuration Contract — COMPLETE

**Phase-owned paths:**

- `.env.example`
- `src/Event.Setup.Core/Environment/CanonicalEnvironmentCatalogue.cs`
- `src/Event.Setup.Core/Environment/CanonicalEnvironmentMetadata.cs`
- `src/Event.Setup.Core/Dotenv/DotenvComposer.cs`
- `eng/setup-assistant/generated/environment-catalogue.json`
- `docs/CONFIGURATION.md`
- `tests/Event.Setup.Core.Tests/Environment/EnvironmentCatalogueInvariantTests.cs`
- `tests/Event.Setup.Core.Tests/Environment/DotenvContractTests.cs`

- [x] **1.1 Author failing configured-bootstrap environment contract tests and verify the focused selectors fail because the closed key matrix is absent**
  - **Files:** existing
    `tests/Event.Setup.Core.Tests/Environment/EnvironmentCatalogueInvariantTests.cs`
    and `tests/Event.Setup.Core.Tests/Environment/DotenvContractTests.cs`
  - **Acceptance:** tests bind mode/provider/subject/generation/profile fallback
    requirements, sensitivity, activation predicates, invalid partial
    matrices, value-free diagnostics, and Scenario 3.7A's offline-only
    boundary
  - **Effort:** M
  - **Dependencies:** approved plan
  - **Guidance:** subject and email values are sensitive; generated output may
    name keys but never fabricate values

- [x] **1.2 Add the closed offline catalogue and regenerate artifacts, verifying configured and interactive readiness classifications pass without adding any runtime client dependency**
  - **Files:** existing `.env.example`,
    `CanonicalEnvironmentCatalogue.cs`, `CanonicalEnvironmentMetadata.cs`,
    `DotenvComposer.cs`, generated `environment-catalogue.json`, generated
    `docs/CONFIGURATION.md` catalogue block, and the Task 1.1 tests
  - **Acceptance:** exact keys and metadata match plan Section 8; partial
    configured mode fails; interactive mode remains explicit; generated
    artifacts converge byte-for-byte through the repository generator
  - **Effort:** M
  - **Dependencies:** 1.1
  - **Guidance:** use generator-owned updates; Setup projects remain offline

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] Run `dotnet build --configuration Release --verbosity quiet` once and verify exit code 0 or record the exact proven unrelated shared-tree failure
- [x] Run `dotnet test --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release --verbosity quiet` once and verify all configured-bootstrap catalogue/dotenv contracts pass
- [x] Confirm the Phase 1 owned lane is green, generated catalogue and `.env.example` converge, and no phase-attributable failure remains

### Phase 1 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `refactor(architecture): define configured bootstrap input contract`
- **Default description:** `Add the closed, value-safe offline environment contract used to generate configured-administrator deployment artifacts while runtime behavior remains disabled.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:**
  - `Changelog: skip`
  - `Changelog-Reason: inactive offline configuration contract with no runtime behavior`
  - `Ultraworked with [omo](https://github.com/code-yeongyu/oh-my-openagent)`
  - `Co-authored-by: sisyphus-dev-ai <sisyphus-dev-ai@users.noreply.github.com>`
- **Commit paths:**
  - `.env.example`
  - `src/Event.Setup.Core/Environment/CanonicalEnvironmentCatalogue.cs`
  - `src/Event.Setup.Core/Environment/CanonicalEnvironmentMetadata.cs`
  - `src/Event.Setup.Core/Dotenv/DotenvComposer.cs`
  - `eng/setup-assistant/generated/environment-catalogue.json`
  - `docs/CONFIGURATION.md`
  - `tests/Event.Setup.Core.Tests/Environment/EnvironmentCatalogueInvariantTests.cs`
  - `tests/Event.Setup.Core.Tests/Environment/DotenvContractTests.cs`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff -- .env.example src/Event.Setup.Core/Environment/CanonicalEnvironmentCatalogue.cs src/Event.Setup.Core/Environment/CanonicalEnvironmentMetadata.cs src/Event.Setup.Core/Dotenv/DotenvComposer.cs eng/setup-assistant/generated/environment-catalogue.json docs/CONFIGURATION.md tests/Event.Setup.Core.Tests/Environment/EnvironmentCatalogueInvariantTests.cs tests/Event.Setup.Core.Tests/Environment/DotenvContractTests.cs`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- .env.example src/Event.Setup.Core/Environment/CanonicalEnvironmentCatalogue.cs src/Event.Setup.Core/Environment/CanonicalEnvironmentMetadata.cs src/Event.Setup.Core/Dotenv/DotenvComposer.cs eng/setup-assistant/generated/environment-catalogue.json docs/CONFIGURATION.md tests/Event.Setup.Core.Tests/Environment/EnvironmentCatalogueInvariantTests.cs tests/Event.Setup.Core.Tests/Environment/DotenvContractTests.cs`
- **Commit command:** `git commit --only -m "refactor(architecture): define configured bootstrap input contract" -m "Add the closed, value-safe offline environment contract used to generate configured-administrator deployment artifacts while runtime behavior remains disabled." -m "Changelog: skip" -m "Changelog-Reason: inactive offline configuration contract with no runtime behavior" -m "Ultraworked with [omo](https://github.com/code-yeongyu/oh-my-openagent)" -m "Co-authored-by: sisyphus-dev-ai <sisyphus-dev-ai@users.noreply.github.com>" -- .env.example src/Event.Setup.Core/Environment/CanonicalEnvironmentCatalogue.cs src/Event.Setup.Core/Environment/CanonicalEnvironmentMetadata.cs src/Event.Setup.Core/Dotenv/DotenvComposer.cs eng/setup-assistant/generated/environment-catalogue.json docs/CONFIGURATION.md tests/Event.Setup.Core.Tests/Environment/EnvironmentCatalogueInvariantTests.cs tests/Event.Setup.Core.Tests/Environment/DotenvContractTests.cs`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden
- **Path-contract override:** Added
  `src/Event.Setup.Core/Dotenv/DotenvComposer.cs` on 2026-09-01 after the
  independently confirmed Task 1.1 Red proved catalogue metadata alone cannot
  execute validator IDs, reject known inactive keys, or validate cross-key
  configured-bootstrap matrices. Added `docs/CONFIGURATION.md` after generator
  source inspection proved the canonical writer atomically owns its generated
  environment catalogue block together with the machine JSON. Commit metadata
  remains truthful.
- **Attribution override:** Added the mandatory omo footer and
  `Co-authored-by` trailer after loading `git-master`; title, description,
  changelog classification, and phase outcome remain unchanged.

- [x] Without loading `conventional-commit`, run the exact inspection commands and verify every named Phase 1 path/hunk is wholly phase-owned before staging and committing
- [x] Only if the default contract became materially false, load `conventional-commit` and verify a complete replacement contract plus reason is recorded here before any commit
- [x] Run the post-commit command, verify the committed file list equals Phase 1 Commit paths, record the hash, and only then mark Phase 1 complete

## Phase 2: Atomic Typed Bootstrap Lifecycle Cutover — COMPLETE

**Phase-owned paths:**

- `src/Explore.Domain/InstanceBootstrapState.cs`
- `src/Explore.Domain/Enums/InstanceBootstrapStatus.cs`
- `src/Explore.Domain/Enums/InstanceBootstrapMode.cs`
- `src/Explore.Domain/Enums/InstanceBootstrapProviderKind.cs`
- active `InstanceBootstrapState` writers/readers in Application,
  Infrastructure, and API resolved by LSP before staging
- `src/Explore.Persistence/Configurations/Entities/InstanceBootstrapStateConfiguration.cs`
- five provider snapshots plus restrictive generated
  `*AddConfiguredAdministratorBootstrapState*.cs` pathspecs
- `tests/Event.Domain.UnitTests/InstanceOnboarding/InstanceBootstrapStateTests.cs`
- directly affected server/fixture tests recorded before staging
- `docs/releases/changes/CHG-01M1ETX06HRETFBJTK6SCZGBZ6.yaml`

- [ ] **2.1 Establish an AssuranceAudit-safe typed Red for Scenarios 3.4 and 3.5 and verify invalid transition, generation drift, duplicate completion, and post-completion transfer fail as assertion anchors**
  - **Files:** entity, three enums, and
    `tests/Event.Domain.UnitTests/InstanceOnboarding/InstanceBootstrapStateTests.cs`
  - **Acceptance:** direct strongly typed tests use worked literals and public
    transitions; a transient additive compile seam may exist only in the
    uncommitted Red-to-Green interval; no reflective behavior dispatch or
    test-local state machine is permitted
  - **Effort:** L
  - **Dependencies:** Phase 1

- [ ] **2.2 Implement the explicit bootstrap mode/status/provider lifecycle and verify every Domain Red turns Green with no public mutable transition logic**
  - **Files:** existing `InstanceBootstrapState.cs`; new
    status/mode/provider enums; Task 2.1 tests
  - **Acceptance:** one generation may be prepared, superseded, or completed;
    exact completion replay is idempotent; selected `DeploymentMode` and
    provider kind are typed; different identity after completion fails; raw
    external identity values cannot be stored
  - **Effort:** L
  - **Dependencies:** 2.1

- [ ] **2.3 Cut active callers and persistence atomically to typed lifecycle and verify generated five-provider schema has no binary alias or dual reader**
  - **Files:** active LSP-resolved server readers/writers, entity
    configuration, five generated migration/snapshot sets, direct fixtures,
    and `CHG-01M1ETX06HRETFBJTK6SCZGBZ6.yaml`
  - **Acceptance:** Domain `IsCompleted` and `SelectedDeploymentMode` are gone;
    active decisions use `Status` and `DeploymentMode`; provider kind,
    fingerprints, generation, timestamps, and completed local user persist;
    existing wire aliases are projections only; generated artifacts are never
    hand edited
  - **Effort:** XL
  - **Dependencies:** 2.2

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and verify exit code 0 or record the exact proven unrelated shared-tree failure
- [ ] Run `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` once and verify all bootstrap state-machine invariants pass
- [ ] Run `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` once and verify typed lifecycle callers pass
- [ ] Run `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` once and verify admin/setup-secret/deployment-mode readers pass
- [ ] Run `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` once and verify onboarding/filter/fixture flows pass
- [ ] Run `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` once and verify typed schema and provider models pass
- [ ] Run `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` once and verify wire projection consumers pass
- [ ] Run `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` once and verify BFF projections pass
- [ ] Run `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` once and verify layer/privacy contracts pass
- [ ] Run AssuranceAudit over the Phase 2 changed tests and verify zero attributable diagnostics
- [ ] Confirm the Phase 2 owned lane is green, Domain remains framework-free, and no phase-attributable failure remains

### Phase 2 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `refactor(onboarding)!: replace binary bootstrap marker with typed lifecycle`
- **Default description:** `Atomically replace binary bootstrap state across Domain, schema, active server callers, generated migrations, and direct fixtures without a dual reader.`
- **Changelog treatment:** Change fragment
  `CHG-01M1ETX06HRETFBJTK6SCZGBZ6`
- **Required trailers:**
  - `BREAKING CHANGE: Instance bootstrap state now uses typed lifecycle, provider, deployment mode, generation, and fingerprint evidence.`
  - `Change-Id: CHG-01M1ETX06HRETFBJTK6SCZGBZ6`
  - `Ultraworked with [omo](https://github.com/code-yeongyu/oh-my-openagent)`
  - `Co-authored-by: sisyphus-dev-ai <sisyphus-dev-ai@users.noreply.github.com>`
- **Commit paths:**
  - `src/Explore.Domain/InstanceBootstrapState.cs`
  - `src/Explore.Domain/Enums/InstanceBootstrapStatus.cs`
  - `src/Explore.Domain/Enums/InstanceBootstrapMode.cs`
  - `tests/Event.Domain.UnitTests/InstanceOnboarding/InstanceBootstrapStateTests.cs`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff -- src/Explore.Domain/InstanceBootstrapState.cs src/Explore.Domain/Enums/InstanceBootstrapStatus.cs src/Explore.Domain/Enums/InstanceBootstrapMode.cs tests/Event.Domain.UnitTests/InstanceOnboarding/InstanceBootstrapStateTests.cs`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Domain/InstanceBootstrapState.cs src/Explore.Domain/Enums/InstanceBootstrapStatus.cs src/Explore.Domain/Enums/InstanceBootstrapMode.cs tests/Event.Domain.UnitTests/InstanceOnboarding/InstanceBootstrapStateTests.cs`
- **Commit command:** `git commit --only -m "refactor(onboarding): model configured administrator bootstrap lifecycle" -m "Replace binary first-run mutation with explicit pending, superseded, and completed generation invariants before persistence or authentication consumes them." -m "Changelog: skip" -m "Changelog-Reason: inactive domain state contract ahead of runtime activation" -- src/Explore.Domain/InstanceBootstrapState.cs src/Explore.Domain/Enums/InstanceBootstrapStatus.cs src/Explore.Domain/Enums/InstanceBootstrapMode.cs tests/Event.Domain.UnitTests/InstanceOnboarding/InstanceBootstrapStateTests.cs`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden
- **Contract status:** The original four-path Domain-only packet is materially
  false. Staging/commit commands below are superseded and MUST NOT execute.
  Record the complete exact replacement path list and command after Task 2.3
  generation and ownership inspection, before any staging.

- [ ] Without loading `conventional-commit`, run the exact inspection commands and verify every named Phase 2 path/hunk is wholly phase-owned before staging and committing
- [ ] Only if the default contract became materially false, load `conventional-commit` and verify a complete replacement contract plus reason is recorded here before any commit
- [ ] Run the post-commit command, verify the committed file list equals Phase 2 Commit paths, record the hash, and only then mark Phase 2 complete

## Phase 3: Multi-Replica Locking And Convergence — COMPLETE

**Phase-owned paths:**

- `src/Explore.Persistence/Repositories/InstanceBootstrapStateRepository.cs`
- `tests/Event.Persistence.IntegrationTests/Onboarding/InstanceBootstrapStatePersistenceTests.cs`
- `tests/Event.Persistence.IntegrationTests/Onboarding/InstanceOnboardingConcurrencyTests.cs`

- [ ] **3.1 Author failing real-relational persistence and race tests for Scenarios 3.4B–3.5B and verify same-generation mismatch, concurrent exact claim, exact-versus-attacker race, and rollback fail for the missing persisted model**
  - **Files:** new
    `InstanceBootstrapStatePersistenceTests.cs` and
    `InstanceOnboardingConcurrencyTests.cs`
  - **Acceptance:** subscribe to deterministic transaction barriers before
    triggers; use bounded timeout; assert public repository/state outcomes and
    zero partial rows; prohibit sleeps, polling, and repository mocks
  - **Effort:** L
  - **Dependencies:** Phase 2

- [ ] **3.2 Implement provider-neutral row locking and serializable convergence, verifying the Phase 3 Red suite turns Green without schema changes**
  - **Files:** existing bootstrap repository and Task 3.1 tests
  - **Acceptance:** concurrent exact completion converges; attacker/different
    identity loses deterministically; generation drift and rollback classify
    exactly; no new migration/snapshot or polling loop is introduced
  - **Effort:** L
  - **Dependencies:** 3.1

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and verify exit code 0 or record the exact proven unrelated shared-tree failure
- [ ] Run `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` once and verify provider-model, rollback, and deterministic race cases pass
- [ ] Confirm the Phase 3 owned lane is green, no schema artifact changed, and no phase-attributable failure remains

### Phase 3 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `fix(onboarding): serialize bootstrap claim convergence`
- **Default description:** `Add row locking and deterministic loser classification over the typed bootstrap schema without changing persistence shape.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:**
  - `Changelog: skip`
  - `Changelog-Reason: inactive convergence hardening ahead of runtime activation`
  - `Ultraworked with [omo](https://github.com/code-yeongyu/oh-my-openagent)`
  - `Co-authored-by: sisyphus-dev-ai <sisyphus-dev-ai@users.noreply.github.com>`
- **Commit paths:**
  - `src/Explore.Persistence/Configurations/Entities/InstanceBootstrapStateConfiguration.cs`
  - `src/Explore.Persistence/Repositories/InstanceBootstrapStateRepository.cs`
  - `src/Explore.Persistence/ExploreDbContext.DbSets.cs`
  - `src/Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs`
  - `src/Explore.Persistence.Migrations.Sqlite/Migrations/ExploreDbContextModelSnapshot.cs`
  - `src/Explore.Persistence.Migrations.SqlServer/Migrations/ExploreDbContextModelSnapshot.cs`
  - `src/Explore.Persistence.Migrations.MariaDb/Migrations/ExploreDbContextModelSnapshot.cs`
  - `src/Explore.Persistence.Migrations.MySql/Migrations/ExploreDbContextModelSnapshot.cs`
  - `:(glob)src/Explore.Persistence/Migrations/*AddConfiguredAdministratorBootstrapState*.cs`
  - `:(glob)src/Explore.Persistence.Migrations.Sqlite/Migrations/*AddConfiguredAdministratorBootstrapState*.cs`
  - `:(glob)src/Explore.Persistence.Migrations.SqlServer/Migrations/*AddConfiguredAdministratorBootstrapState*.cs`
  - `:(glob)src/Explore.Persistence.Migrations.MariaDb/Migrations/*AddConfiguredAdministratorBootstrapState*.cs`
  - `:(glob)src/Explore.Persistence.Migrations.MySql/Migrations/*AddConfiguredAdministratorBootstrapState*.cs`
  - `tests/Event.Persistence.IntegrationTests/Onboarding/InstanceBootstrapStatePersistenceTests.cs`
  - `tests/Event.Persistence.IntegrationTests/Onboarding/InstanceOnboardingConcurrencyTests.cs`
  - `docs/releases/changes/CHG-01M1ETX06HRETFBJTK6SCZGBZ6.yaml`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff -- src/Explore.Persistence/Configurations/Entities/InstanceBootstrapStateConfiguration.cs src/Explore.Persistence/Repositories/InstanceBootstrapStateRepository.cs src/Explore.Persistence/ExploreDbContext.DbSets.cs src/Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.Sqlite/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.SqlServer/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.MariaDb/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.MySql/Migrations/ExploreDbContextModelSnapshot.cs ':(glob)src/Explore.Persistence/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.Sqlite/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.SqlServer/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.MariaDb/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.MySql/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' tests/Event.Persistence.IntegrationTests/Onboarding/InstanceBootstrapStatePersistenceTests.cs tests/Event.Persistence.IntegrationTests/Onboarding/InstanceOnboardingConcurrencyTests.cs docs/releases/changes/CHG-01M1ETX06HRETFBJTK6SCZGBZ6.yaml`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Persistence/Configurations/Entities/InstanceBootstrapStateConfiguration.cs src/Explore.Persistence/Repositories/InstanceBootstrapStateRepository.cs src/Explore.Persistence/ExploreDbContext.DbSets.cs src/Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.Sqlite/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.SqlServer/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.MariaDb/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.MySql/Migrations/ExploreDbContextModelSnapshot.cs ':(glob)src/Explore.Persistence/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.Sqlite/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.SqlServer/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.MariaDb/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.MySql/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' tests/Event.Persistence.IntegrationTests/Onboarding/InstanceBootstrapStatePersistenceTests.cs tests/Event.Persistence.IntegrationTests/Onboarding/InstanceOnboardingConcurrencyTests.cs docs/releases/changes/CHG-01M1ETX06HRETFBJTK6SCZGBZ6.yaml`
- **Commit command:** `git commit --only -m "feat(onboarding)!: persist configured bootstrap generations" -m "Persist value-free configured-administrator generations and enforce multi-replica, rollback, and completion convergence across every supported database provider." -m "BREAKING CHANGE: instance bootstrap persistence replaces binary completion with explicit generation state without a legacy reader." -m "Change-Id: CHG-01M1ETX06HRETFBJTK6SCZGBZ6" -- src/Explore.Persistence/Configurations/Entities/InstanceBootstrapStateConfiguration.cs src/Explore.Persistence/Repositories/InstanceBootstrapStateRepository.cs src/Explore.Persistence/ExploreDbContext.DbSets.cs src/Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.Sqlite/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.SqlServer/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.MariaDb/Migrations/ExploreDbContextModelSnapshot.cs src/Explore.Persistence.Migrations.MySql/Migrations/ExploreDbContextModelSnapshot.cs ':(glob)src/Explore.Persistence/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.Sqlite/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.SqlServer/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.MariaDb/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' ':(glob)src/Explore.Persistence.Migrations.MySql/Migrations/*AddConfiguredAdministratorBootstrapState*.cs' tests/Event.Persistence.IntegrationTests/Onboarding/InstanceBootstrapStatePersistenceTests.cs tests/Event.Persistence.IntegrationTests/Onboarding/InstanceOnboardingConcurrencyTests.cs docs/releases/changes/CHG-01M1ETX06HRETFBJTK6SCZGBZ6.yaml`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands and verify every Phase 3 path/hunk and generated migration match is wholly phase-owned before staging and committing
- [ ] Only if generated names or the outcome materially diverge, load `conventional-commit` and verify complete replacement contracts plus the exact divergence reason are recorded here before any commit
- [ ] Run the post-commit command, verify the committed file list equals the resolved Phase 3 Commit paths, record the hash and generated filenames, and only then mark Phase 3 complete

## Phase 4: Provider-Neutral Claim Orchestrator — COMPLETE

**Phase-owned paths:**

- `src/Explore.Application/Contracts/Services/IConfiguredAdministratorBootstrapProvider.cs`
- `src/Explore.Application/Authentication/ProviderAccountKey.cs`
- `src/Explore.Application/Models/ConfiguredAdministratorBootstrapBinding.cs`
- `src/Explore.Application/Features/InstanceOnboarding/Requests/Commands/ClaimConfiguredInstanceAdministratorCommand.cs`
- `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/ClaimConfiguredInstanceAdministratorCommandHandler.cs`
- `src/Explore.Application/Features/InstanceOnboarding/Services/InstanceOnboardingCompletionOperation.cs`
- `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`
- `src/Explore.Application/ApplicationServicesRegistration.cs`
- `tests/Event.Application.UnitTests/Features/InstanceOnboarding/ConfiguredAdministratorClaimInvariantTests.cs`
- `tests/Event.Application.UnitTests/Features/InstanceOnboarding/InstanceOnboardingCompletionOperationTests.cs`

- [ ] **4.1 Author failing exact-authority, rollback, replay, and zero-disclosure claim tests for Scenarios 3.2, 3.4, and 3.6 and verify they fail at the missing provider-neutral seam**
  - **Files:** new
    `ConfiguredAdministratorClaimInvariantTests.cs` and
    `InstanceOnboardingCompletionOperationTests.cs`
  - **Acceptance:** exact versus indirect identity, same versus different
    completed replay, failure before/after intermediate writes, and bounded
    error models are independently asserted
  - **Effort:** L
  - **Dependencies:** Phase 3

- [ ] **4.2 Implement the configured claim command and deep completion operation, verifying one serializable transaction satisfies all Red invariants**
  - **Files:** new configured provider contract/model/account-key,
    claim command/handler, and completion operation
  - **Acceptance:** trusted identity and generation are re-read inside the
    transaction; no browser selector exists; roles/tenant/settings/bootstrap
    commit atomically; concurrent same identity converges
  - **Effort:** XL
  - **Dependencies:** 4.1

- [ ] **4.3 Refactor interactive completion and DI onto the shared operation, verifying setup-secret authority and post-commit cache/audit behavior remain intact**
  - **Files:** existing `CompleteInstanceOnboardingCommandHandler.cs`,
    `ApplicationServicesRegistration.cs`, new operation/handler/tests
  - **Acceptance:** interactive mode has no duplicated transaction logic;
    manual validator, cancellation, response factories, secret lock, cache
    invalidation, deployment/JWT reload, and audit ordering remain preserved
  - **Effort:** L
  - **Dependencies:** 4.2

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and verify exit code 0 or record the exact proven unrelated shared-tree failure
- [ ] Run `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` once and verify claim, rollback, replay, and interactive-parity contracts pass
- [ ] Confirm the Phase 4 owned lane is green, runtime provider is still disabled, and no phase-attributable failure remains

### Phase 4 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `refactor(onboarding): centralize initial administrator claim transaction`
- **Default description:** `Share one provider-neutral, atomic completion operation between interactive onboarding and the dormant configured-administrator claim path.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: inactive application orchestration ahead of runtime activation`
- **Commit paths:** `src/Explore.Application/Contracts/Services/IConfiguredAdministratorBootstrapProvider.cs`; `src/Explore.Application/Authentication/ProviderAccountKey.cs`; `src/Explore.Application/Models/ConfiguredAdministratorBootstrapBinding.cs`; `src/Explore.Application/Features/InstanceOnboarding/Requests/Commands/ClaimConfiguredInstanceAdministratorCommand.cs`; `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/ClaimConfiguredInstanceAdministratorCommandHandler.cs`; `src/Explore.Application/Features/InstanceOnboarding/Services/InstanceOnboardingCompletionOperation.cs`; `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`; `src/Explore.Application/ApplicationServicesRegistration.cs`; `tests/Event.Application.UnitTests/Features/InstanceOnboarding/ConfiguredAdministratorClaimInvariantTests.cs`; `tests/Event.Application.UnitTests/Features/InstanceOnboarding/InstanceOnboardingCompletionOperationTests.cs`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff -- src/Explore.Application/Contracts/Services/IConfiguredAdministratorBootstrapProvider.cs src/Explore.Application/Authentication/ProviderAccountKey.cs src/Explore.Application/Models/ConfiguredAdministratorBootstrapBinding.cs src/Explore.Application/Features/InstanceOnboarding/Requests/Commands/ClaimConfiguredInstanceAdministratorCommand.cs src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/ClaimConfiguredInstanceAdministratorCommandHandler.cs src/Explore.Application/Features/InstanceOnboarding/Services/InstanceOnboardingCompletionOperation.cs src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs src/Explore.Application/ApplicationServicesRegistration.cs tests/Event.Application.UnitTests/Features/InstanceOnboarding/ConfiguredAdministratorClaimInvariantTests.cs tests/Event.Application.UnitTests/Features/InstanceOnboarding/InstanceOnboardingCompletionOperationTests.cs`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Application/Contracts/Services/IConfiguredAdministratorBootstrapProvider.cs src/Explore.Application/Authentication/ProviderAccountKey.cs src/Explore.Application/Models/ConfiguredAdministratorBootstrapBinding.cs src/Explore.Application/Features/InstanceOnboarding/Requests/Commands/ClaimConfiguredInstanceAdministratorCommand.cs src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/ClaimConfiguredInstanceAdministratorCommandHandler.cs src/Explore.Application/Features/InstanceOnboarding/Services/InstanceOnboardingCompletionOperation.cs src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs src/Explore.Application/ApplicationServicesRegistration.cs tests/Event.Application.UnitTests/Features/InstanceOnboarding/ConfiguredAdministratorClaimInvariantTests.cs tests/Event.Application.UnitTests/Features/InstanceOnboarding/InstanceOnboardingCompletionOperationTests.cs`
- **Commit command:** `git commit --only -m "refactor(onboarding): centralize initial administrator claim transaction" -m "Share one provider-neutral, atomic completion operation between interactive onboarding and the dormant configured-administrator claim path." -m "Changelog: skip" -m "Changelog-Reason: inactive application orchestration ahead of runtime activation" -- src/Explore.Application/Contracts/Services/IConfiguredAdministratorBootstrapProvider.cs src/Explore.Application/Authentication/ProviderAccountKey.cs src/Explore.Application/Models/ConfiguredAdministratorBootstrapBinding.cs src/Explore.Application/Features/InstanceOnboarding/Requests/Commands/ClaimConfiguredInstanceAdministratorCommand.cs src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/ClaimConfiguredInstanceAdministratorCommandHandler.cs src/Explore.Application/Features/InstanceOnboarding/Services/InstanceOnboardingCompletionOperation.cs src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs src/Explore.Application/ApplicationServicesRegistration.cs tests/Event.Application.UnitTests/Features/InstanceOnboarding/ConfiguredAdministratorClaimInvariantTests.cs tests/Event.Application.UnitTests/Features/InstanceOnboarding/InstanceOnboardingCompletionOperationTests.cs`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands and verify every named Phase 4 path/hunk is wholly phase-owned before staging and committing
- [ ] Only if the default contract became materially false, load `conventional-commit` and verify a complete replacement contract plus reason is recorded here before any commit
- [ ] Run the post-commit command, verify the committed file list equals Phase 4 Commit paths, record the hash, and only then mark Phase 4 complete

## Phase 5: Verified Provider Adapters And API Status — COMPLETE

**Phase-owned paths:**

- `src/Explore.Application/Authentication/PlatformIdentityPrincipalExtensions.cs`
- `src/Explore.Application/Authentication/CurrentUserResolutionExtensions.cs`
- `src/Explore.Application/Features/Users/Requests/Commands/SyncUserCommand.cs`
- `src/Explore.Application/Features/Users/Handlers/Commands/SyncUserCommandHandler.cs`
- `src/Explore.Application/Features/Users/Handlers/Queries/ResolveCurrentUserIdByIdentityRequestHandler.cs`
- `src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/BootstrapAtprotoSessionCommandHandler.cs`
- `src/Explore.Application/Features/Authentication/Atproto/Services/AtprotoSubjectOnboardingOperation.cs`
- `src/Explore.Application/Features/ManagedProviderProvisioning/Handlers/Commands/EnsureManagedProviderClientProvisionedCommandHandler.cs`
- `src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventPublicationPlanner.cs`
- `src/Explore.Application/Contracts/Persistence/IUserExternalLoginRepository.cs`
- `src/Explore.Infrastructure/Identity/AdminClaimsTransformation.cs`
- `src/Explore.Infrastructure/Identity/AdminContext.cs`
- `src/Explore.Infrastructure/Services/DisabledConfiguredAdministratorBootstrapProvider.cs`
- `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- `src/Explore.API/Controllers/UserController.cs`
- `src/Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs`
- `src/Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs`
- `tests/Event.API.IntegrationTests/Features/ConfiguredAdministratorBootstrapTests.cs`
- `tests/Event.API.IntegrationTests/Features/UserControllerTests.cs`
- `tests/Event.API.IntegrationTests/Features/UserExternalLoginIntegrationTests.cs`
- `docs/releases/changes/CHG-01M1ETXMS84KS8ASDW4GR22Q3J.yaml`

- [ ] **5.1 Author failing API security tests for exact Keycloak issuer/subject, wrong issuer, indirect identifier takeover, bounded status, and zero-PII output, verifying configured claims fail before adapter implementation**
  - **Files:** new `ConfiguredAdministratorBootstrapTests.cs`; existing
    `UserControllerTests.cs` and `UserExternalLoginIntegrationTests.cs`
  - **Acceptance:** exact subject from wrong issuer, same email, username,
    provider role, and nonmatching provider produce zero user/login/role/state
    writes; status reveals state/provider only
  - **Effort:** L
  - **Dependencies:** Phase 4

- [ ] **5.2 Replace raw provider keys with one authority-qualified account key across every caller, verifying exact global resolution and no dual legacy reader**
  - **Files:** principal/current-user identity, SyncUser,
    ResolveCurrentUserId, managed provisioning, federation publication,
    repository contract, AdminClaimsTransformation, AdminContext, and tests
  - **Acceptance:** Keycloak/Google keys include normalized issuer; ATProto key
    is canonical DID; all `GetByProviderAndKey` callers receive canonical
    values; old raw-key fallback is absent
  - **Effort:** XL
  - **Dependencies:** 5.1

- [ ] **5.3 Add the verified ATProto first-claim branch and verify only the gateway-returned exact DID may materialize identity before session persistence and token issuance**
  - **Files:** existing `BootstrapAtprotoSessionCommandHandler.cs`,
    `AtprotoSubjectOnboardingOperation.cs`, API/Application tests
  - **Acceptance:** branch occurs after cryptographic verification and before
    account-not-linked; expected DID, handle, PDS URL, and wrong verified DID
    fail; transaction/token ordering is unchanged
  - **Effort:** L
  - **Dependencies:** 5.2

- [ ] **5.4 Add bounded API bootstrap status and the fail-closed disabled runtime provider, verifying no new write route, identity request body, HAL relation, or provider management call appears**
  - **Files:** status DTO/query, disabled Infrastructure provider/registration,
    User controller and tests
  - **Acceptance:** configured mode cannot activate yet; status is additive and
    value-free; existing interactive mode remains explicit
  - **Effort:** M
  - **Dependencies:** 5.2, 5.3

- [ ] **5.5 Create provider identity change fragment `CHG-01M1ETXMS84KS8ASDW4GR22Q3J` and verify its Breaking, Security, OpenAPI, and Operator impacts match the authority-qualified replacement**
  - **Files:** new
    `docs/releases/changes/CHG-01M1ETXMS84KS8ASDW4GR22Q3J.yaml`
  - **Acceptance:** fragment is value-free, documents removal of raw
    realm-scoped provider keys and absence of legacy readers, and matches the
    Phase 5 commit footer exactly
  - **Effort:** S
  - **Dependencies:** 5.2–5.4
  - **Guidance:** use the planning-allocated ID and native patch tool

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and verify exit code 0 or record the exact proven unrelated shared-tree failure
- [ ] Run `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` once and verify exact provider, ATProto, status, and zero-write adversarial cases pass
- [ ] Confirm the Phase 5 owned lane is green, configured runtime remains disabled, and no phase-attributable failure remains

### Phase 5 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `feat(access)!: qualify provider account identities`
- **Default description:** `Replace realm-ambiguous provider keys with authority-qualified Keycloak and verified ATProto identities, then bind those verified accounts to the dormant configured claim path.`
- **Changelog treatment:** Change fragment `CHG-01M1ETXMS84KS8ASDW4GR22Q3J`
- **Required trailers:** `BREAKING CHANGE: raw realm-scoped provider account keys are replaced by authority-qualified identities without legacy readers.`; `Change-Id: CHG-01M1ETXMS84KS8ASDW4GR22Q3J`
- **Commit paths:** `src/Explore.Application/Authentication/PlatformIdentityPrincipalExtensions.cs`; `src/Explore.Application/Authentication/CurrentUserResolutionExtensions.cs`; `src/Explore.Application/Features/Users/Requests/Commands/SyncUserCommand.cs`; `src/Explore.Application/Features/Users/Handlers/Commands/SyncUserCommandHandler.cs`; `src/Explore.Application/Features/Users/Handlers/Queries/ResolveCurrentUserIdByIdentityRequestHandler.cs`; `src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/BootstrapAtprotoSessionCommandHandler.cs`; `src/Explore.Application/Features/Authentication/Atproto/Services/AtprotoSubjectOnboardingOperation.cs`; `src/Explore.Application/Features/ManagedProviderProvisioning/Handlers/Commands/EnsureManagedProviderClientProvisionedCommandHandler.cs`; `src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventPublicationPlanner.cs`; `src/Explore.Application/Contracts/Persistence/IUserExternalLoginRepository.cs`; `src/Explore.Infrastructure/Identity/AdminClaimsTransformation.cs`; `src/Explore.Infrastructure/Identity/AdminContext.cs`; `src/Explore.Infrastructure/Services/DisabledConfiguredAdministratorBootstrapProvider.cs`; `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`; `src/Explore.API/Controllers/UserController.cs`; `src/Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs`; `src/Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs`; `tests/Event.API.IntegrationTests/Features/ConfiguredAdministratorBootstrapTests.cs`; `tests/Event.API.IntegrationTests/Features/UserControllerTests.cs`; `tests/Event.API.IntegrationTests/Features/UserExternalLoginIntegrationTests.cs`; `docs/releases/changes/CHG-01M1ETXMS84KS8ASDW4GR22Q3J.yaml`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff -- src/Explore.Application/Authentication/PlatformIdentityPrincipalExtensions.cs src/Explore.Application/Authentication/CurrentUserResolutionExtensions.cs src/Explore.Application/Features/Users/Requests/Commands/SyncUserCommand.cs src/Explore.Application/Features/Users/Handlers/Commands/SyncUserCommandHandler.cs src/Explore.Application/Features/Users/Handlers/Queries/ResolveCurrentUserIdByIdentityRequestHandler.cs src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/BootstrapAtprotoSessionCommandHandler.cs src/Explore.Application/Features/Authentication/Atproto/Services/AtprotoSubjectOnboardingOperation.cs src/Explore.Application/Features/ManagedProviderProvisioning/Handlers/Commands/EnsureManagedProviderClientProvisionedCommandHandler.cs src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventPublicationPlanner.cs src/Explore.Application/Contracts/Persistence/IUserExternalLoginRepository.cs src/Explore.Infrastructure/Identity/AdminClaimsTransformation.cs src/Explore.Infrastructure/Identity/AdminContext.cs src/Explore.Infrastructure/Services/DisabledConfiguredAdministratorBootstrapProvider.cs src/Explore.Infrastructure/InfrastructureServicesRegistration.cs src/Explore.API/Controllers/UserController.cs src/Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs src/Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs tests/Event.API.IntegrationTests/Features/ConfiguredAdministratorBootstrapTests.cs tests/Event.API.IntegrationTests/Features/UserControllerTests.cs tests/Event.API.IntegrationTests/Features/UserExternalLoginIntegrationTests.cs docs/releases/changes/CHG-01M1ETXMS84KS8ASDW4GR22Q3J.yaml`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Application/Authentication/PlatformIdentityPrincipalExtensions.cs src/Explore.Application/Authentication/CurrentUserResolutionExtensions.cs src/Explore.Application/Features/Users/Requests/Commands/SyncUserCommand.cs src/Explore.Application/Features/Users/Handlers/Commands/SyncUserCommandHandler.cs src/Explore.Application/Features/Users/Handlers/Queries/ResolveCurrentUserIdByIdentityRequestHandler.cs src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/BootstrapAtprotoSessionCommandHandler.cs src/Explore.Application/Features/Authentication/Atproto/Services/AtprotoSubjectOnboardingOperation.cs src/Explore.Application/Features/ManagedProviderProvisioning/Handlers/Commands/EnsureManagedProviderClientProvisionedCommandHandler.cs src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventPublicationPlanner.cs src/Explore.Application/Contracts/Persistence/IUserExternalLoginRepository.cs src/Explore.Infrastructure/Identity/AdminClaimsTransformation.cs src/Explore.Infrastructure/Identity/AdminContext.cs src/Explore.Infrastructure/Services/DisabledConfiguredAdministratorBootstrapProvider.cs src/Explore.Infrastructure/InfrastructureServicesRegistration.cs src/Explore.API/Controllers/UserController.cs src/Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs src/Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs tests/Event.API.IntegrationTests/Features/ConfiguredAdministratorBootstrapTests.cs tests/Event.API.IntegrationTests/Features/UserControllerTests.cs tests/Event.API.IntegrationTests/Features/UserExternalLoginIntegrationTests.cs docs/releases/changes/CHG-01M1ETXMS84KS8ASDW4GR22Q3J.yaml`
- **Commit command:** `git commit --only -m "feat(access)!: qualify provider account identities" -m "Replace realm-ambiguous provider keys with authority-qualified Keycloak and verified ATProto identities, then bind those verified accounts to the dormant configured claim path." -m "BREAKING CHANGE: raw realm-scoped provider account keys are replaced by authority-qualified identities without legacy readers." -m "Change-Id: CHG-01M1ETXMS84KS8ASDW4GR22Q3J" -- src/Explore.Application/Authentication/PlatformIdentityPrincipalExtensions.cs src/Explore.Application/Authentication/CurrentUserResolutionExtensions.cs src/Explore.Application/Features/Users/Requests/Commands/SyncUserCommand.cs src/Explore.Application/Features/Users/Handlers/Commands/SyncUserCommandHandler.cs src/Explore.Application/Features/Users/Handlers/Queries/ResolveCurrentUserIdByIdentityRequestHandler.cs src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/BootstrapAtprotoSessionCommandHandler.cs src/Explore.Application/Features/Authentication/Atproto/Services/AtprotoSubjectOnboardingOperation.cs src/Explore.Application/Features/ManagedProviderProvisioning/Handlers/Commands/EnsureManagedProviderClientProvisionedCommandHandler.cs src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventPublicationPlanner.cs src/Explore.Application/Contracts/Persistence/IUserExternalLoginRepository.cs src/Explore.Infrastructure/Identity/AdminClaimsTransformation.cs src/Explore.Infrastructure/Identity/AdminContext.cs src/Explore.Infrastructure/Services/DisabledConfiguredAdministratorBootstrapProvider.cs src/Explore.Infrastructure/InfrastructureServicesRegistration.cs src/Explore.API/Controllers/UserController.cs src/Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs src/Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs tests/Event.API.IntegrationTests/Features/ConfiguredAdministratorBootstrapTests.cs tests/Event.API.IntegrationTests/Features/UserControllerTests.cs tests/Event.API.IntegrationTests/Features/UserExternalLoginIntegrationTests.cs docs/releases/changes/CHG-01M1ETXMS84KS8ASDW4GR22Q3J.yaml`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands and verify every named Phase 5 path/hunk is wholly phase-owned before staging and committing
- [ ] Only if the default contract became materially false, load `conventional-commit` and verify a complete replacement contract plus reason is recorded here before any commit
- [ ] Run the post-commit command, verify the committed file list equals Phase 5 Commit paths, record the hash, and only then mark Phase 5 complete

## Phase 6: BFF Pending Authentication Routing — COMPLETE

**Phase-owned paths:**

- `src/Explore.Blazor/Services/BffOnboardingStatusProvider.cs`
- `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs`
- `src/Explore.Blazor/Extensions/BffAuthEndpoints.cs`
- `src/Explore.Blazor/Services/BffAdminClaimsTransformation.cs`
- `src/Explore.Blazor/Services/Auth/BffSessionRefreshService.cs`
- `tests/Explore.Blazor.IntegrationTests/Endpoints/ConfiguredAdministratorRoutingTests.cs`
- `tests/Explore.Blazor.IntegrationTests/Endpoints/AtprotoAuthenticationFlowTests.cs`
- `tests/Explore.Blazor.IntegrationTests/Services/BffAdminClaimsTransformationTests.cs`
- `tests/Explore.Blazor.IntegrationTests/Services/BffSessionRefreshServiceTests.cs`

- [ ] **6.1 Author failing BFF route/session tests for Scenarios 3.1 and 3.8 and verify pending provider routing, wrong-provider denial, unknown-state failure, and post-claim refresh fail before implementation**
  - **Files:** new `ConfiguredAdministratorRoutingTests.cs`; existing ATProto,
    admin-claims, and session-refresh tests
  - **Acceptance:** exact route/status events are awaited without sleeps;
    redirect-loop, cookie, antiforgery, return URL, and token-boundary behavior
    are asserted through HTTP/session surfaces
  - **Effort:** L
  - **Dependencies:** Phase 5

- [ ] **6.2 Implement the closed BFF onboarding state and provider-specific routing, verifying `/setup` is never rendered for configured pending mode**
  - **Files:** status provider, startup middleware, auth endpoints, routing tests
  - **Acceptance:** interactive/configured/completed/invalid states are
    exhaustive; only configured provider challenge is reachable; unknown state
    fails closed
  - **Effort:** L
  - **Dependencies:** 6.1

- [ ] **6.3 Implement sign-in claim and authority refresh ordering, verifying sync/claim precedes status refresh and persisted admin-claim enrichment**
  - **Files:** admin claims transformation, session refresh service, tests
  - **Acceptance:** pending configured sign-in no longer skips synchronization;
    failed/nonmatching claim signs out or denies without stale admin claims;
    successful claim refreshes once
  - **Effort:** M
  - **Dependencies:** 6.2

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and verify exit code 0 or record the exact proven unrelated shared-tree failure
- [ ] Run `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` once and verify the full pending routing/session matrix passes
- [ ] Confirm the Phase 6 owned lane is green, cookie/token/antiforgery invariants remain intact, and no phase-attributable failure remains

### Phase 6 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `refactor(onboarding): prepare provider-specific pending auth routing`
- **Default description:** `Teach the BFF to consume the dormant closed bootstrap state, permit only the configured provider, and refresh authority after a successful claim.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: inactive BFF wiring ahead of runtime activation`
- **Commit paths:** `src/Explore.Blazor/Services/BffOnboardingStatusProvider.cs`; `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs`; `src/Explore.Blazor/Extensions/BffAuthEndpoints.cs`; `src/Explore.Blazor/Services/BffAdminClaimsTransformation.cs`; `src/Explore.Blazor/Services/Auth/BffSessionRefreshService.cs`; `tests/Explore.Blazor.IntegrationTests/Endpoints/ConfiguredAdministratorRoutingTests.cs`; `tests/Explore.Blazor.IntegrationTests/Endpoints/AtprotoAuthenticationFlowTests.cs`; `tests/Explore.Blazor.IntegrationTests/Services/BffAdminClaimsTransformationTests.cs`; `tests/Explore.Blazor.IntegrationTests/Services/BffSessionRefreshServiceTests.cs`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff -- src/Explore.Blazor/Services/BffOnboardingStatusProvider.cs src/Explore.Blazor/Extensions/MiddlewareExtensions.cs src/Explore.Blazor/Extensions/BffAuthEndpoints.cs src/Explore.Blazor/Services/BffAdminClaimsTransformation.cs src/Explore.Blazor/Services/Auth/BffSessionRefreshService.cs tests/Explore.Blazor.IntegrationTests/Endpoints/ConfiguredAdministratorRoutingTests.cs tests/Explore.Blazor.IntegrationTests/Endpoints/AtprotoAuthenticationFlowTests.cs tests/Explore.Blazor.IntegrationTests/Services/BffAdminClaimsTransformationTests.cs tests/Explore.Blazor.IntegrationTests/Services/BffSessionRefreshServiceTests.cs`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Blazor/Services/BffOnboardingStatusProvider.cs src/Explore.Blazor/Extensions/MiddlewareExtensions.cs src/Explore.Blazor/Extensions/BffAuthEndpoints.cs src/Explore.Blazor/Services/BffAdminClaimsTransformation.cs src/Explore.Blazor/Services/Auth/BffSessionRefreshService.cs tests/Explore.Blazor.IntegrationTests/Endpoints/ConfiguredAdministratorRoutingTests.cs tests/Explore.Blazor.IntegrationTests/Endpoints/AtprotoAuthenticationFlowTests.cs tests/Explore.Blazor.IntegrationTests/Services/BffAdminClaimsTransformationTests.cs tests/Explore.Blazor.IntegrationTests/Services/BffSessionRefreshServiceTests.cs`
- **Commit command:** `git commit --only -m "refactor(onboarding): prepare provider-specific pending auth routing" -m "Teach the BFF to consume the dormant closed bootstrap state, permit only the configured provider, and refresh authority after a successful claim." -m "Changelog: skip" -m "Changelog-Reason: inactive BFF wiring ahead of runtime activation" -- src/Explore.Blazor/Services/BffOnboardingStatusProvider.cs src/Explore.Blazor/Extensions/MiddlewareExtensions.cs src/Explore.Blazor/Extensions/BffAuthEndpoints.cs src/Explore.Blazor/Services/BffAdminClaimsTransformation.cs src/Explore.Blazor/Services/Auth/BffSessionRefreshService.cs tests/Explore.Blazor.IntegrationTests/Endpoints/ConfiguredAdministratorRoutingTests.cs tests/Explore.Blazor.IntegrationTests/Endpoints/AtprotoAuthenticationFlowTests.cs tests/Explore.Blazor.IntegrationTests/Services/BffAdminClaimsTransformationTests.cs tests/Explore.Blazor.IntegrationTests/Services/BffSessionRefreshServiceTests.cs`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands and verify every named Phase 6 path/hunk is wholly phase-owned before staging and committing
- [ ] Only if the default contract became materially false, load `conventional-commit` and verify a complete replacement contract plus reason is recorded here before any commit
- [ ] Run the post-commit command, verify the committed file list equals Phase 6 Commit paths, record the hash, and only then mark Phase 6 complete

## Phase 7: Generated Client And Startup Route Consumption — COMPLETE

**Phase-owned paths:**

- `schemas/openapi_islamu-event.json`
- `docs/API_CONTRACT_INVENTORY.md`
- `docs/API_CHANGELOG.md`
- `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- `src/Explore.Blazor.Client/Services/InstanceOnboardingService.cs`
- `src/Explore.Blazor.Client/Services/StartupRoutingService.cs`
- `src/Explore.Blazor.Client/Pages/HomeStart.razor`
- `src/Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor`
- `tests/Explore.Blazor.Client.Tests/Services/InstanceOnboardingServiceTests.cs`
- `tests/Explore.Blazor.Client.Tests/Services/StartupRoutingServiceTests.cs`
- `tests/Explore.Blazor.Client.Tests/Pages/Onboarding/StartupGateTests.cs`

- [ ] **7.1 Add additive API/status contract assertions and verify state/provider fields are value-free and operation IDs remain stable**
  - **Files:** existing architecture/API contract tests plus generated source
    inputs; no prose/source scraping
  - **Acceptance:** machine-consumed OpenAPI/status semantics assert closed
    values, no identity members, and unchanged existing operation IDs
  - **Effort:** M
  - **Dependencies:** Phase 6

- [ ] **7.2 Regenerate OpenAPI, API inventory, and Blazor client, verifying canonical generation produces no hand-authored generated diff**
  - **Files:** `schemas/openapi_islamu-event.json`,
    `docs/API_CONTRACT_INVENTORY.md`,
    `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`,
    `docs/API_CHANGELOG.md`
  - **Acceptance:** generator output matches bounded status DTO; API changelog
    declares additive fields and pre-release provider-key replacement
  - **Effort:** M
  - **Dependencies:** 7.1

- [ ] **7.3 Consume the closed state in client startup services and verify configured pending mode never navigates to or renders the onboarding wizard**
  - **Files:** onboarding/startup services, HomeStart, StartupGate, and three
    focused Client test files
  - **Acceptance:** interactive, configured pending, completed, invalid, and
    unknown behavior is exhaustive; no local claim/role authorization is added
  - **Effort:** M
  - **Dependencies:** 7.2

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and verify exit code 0 or record the exact proven unrelated shared-tree failure
- [ ] Run `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` once and verify generated status mapping and startup route cases pass
- [ ] Confirm the Phase 7 owned lane is green, generated artifacts are canonical, and no phase-attributable failure remains

### Phase 7 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `refactor(onboarding): align generated startup status contracts`
- **Default description:** `Regenerate the additive bootstrap status contract and teach client startup routing to consume configured pending state while runtime activation remains disabled.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: inactive generated and client contract wiring ahead of runtime activation`
- **Commit paths:** `schemas/openapi_islamu-event.json`; `docs/API_CONTRACT_INVENTORY.md`; `docs/API_CHANGELOG.md`; `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`; `src/Explore.Blazor.Client/Services/InstanceOnboardingService.cs`; `src/Explore.Blazor.Client/Services/StartupRoutingService.cs`; `src/Explore.Blazor.Client/Pages/HomeStart.razor`; `src/Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor`; `tests/Explore.Blazor.Client.Tests/Services/InstanceOnboardingServiceTests.cs`; `tests/Explore.Blazor.Client.Tests/Services/StartupRoutingServiceTests.cs`; `tests/Explore.Blazor.Client.Tests/Pages/Onboarding/StartupGateTests.cs`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff -- schemas/openapi_islamu-event.json docs/API_CONTRACT_INVENTORY.md docs/API_CHANGELOG.md src/Explore.Blazor.Client/Clients/EventApiClient.g.cs src/Explore.Blazor.Client/Services/InstanceOnboardingService.cs src/Explore.Blazor.Client/Services/StartupRoutingService.cs src/Explore.Blazor.Client/Pages/HomeStart.razor src/Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor tests/Explore.Blazor.Client.Tests/Services/InstanceOnboardingServiceTests.cs tests/Explore.Blazor.Client.Tests/Services/StartupRoutingServiceTests.cs tests/Explore.Blazor.Client.Tests/Pages/Onboarding/StartupGateTests.cs`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- schemas/openapi_islamu-event.json docs/API_CONTRACT_INVENTORY.md docs/API_CHANGELOG.md src/Explore.Blazor.Client/Clients/EventApiClient.g.cs src/Explore.Blazor.Client/Services/InstanceOnboardingService.cs src/Explore.Blazor.Client/Services/StartupRoutingService.cs src/Explore.Blazor.Client/Pages/HomeStart.razor src/Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor tests/Explore.Blazor.Client.Tests/Services/InstanceOnboardingServiceTests.cs tests/Explore.Blazor.Client.Tests/Services/StartupRoutingServiceTests.cs tests/Explore.Blazor.Client.Tests/Pages/Onboarding/StartupGateTests.cs`
- **Commit command:** `git commit --only -m "refactor(onboarding): align generated startup status contracts" -m "Regenerate the additive bootstrap status contract and teach client startup routing to consume configured pending state while runtime activation remains disabled." -m "Changelog: skip" -m "Changelog-Reason: inactive generated and client contract wiring ahead of runtime activation" -- schemas/openapi_islamu-event.json docs/API_CONTRACT_INVENTORY.md docs/API_CHANGELOG.md src/Explore.Blazor.Client/Clients/EventApiClient.g.cs src/Explore.Blazor.Client/Services/InstanceOnboardingService.cs src/Explore.Blazor.Client/Services/StartupRoutingService.cs src/Explore.Blazor.Client/Pages/HomeStart.razor src/Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor tests/Explore.Blazor.Client.Tests/Services/InstanceOnboardingServiceTests.cs tests/Explore.Blazor.Client.Tests/Services/StartupRoutingServiceTests.cs tests/Explore.Blazor.Client.Tests/Pages/Onboarding/StartupGateTests.cs`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands and verify every named Phase 7 path/hunk is wholly phase-owned before staging and committing
- [ ] Only if the default contract became materially false, load `conventional-commit` and verify a complete replacement contract plus reason is recorded here before any commit
- [ ] Run the post-commit command, verify the committed file list equals Phase 7 Commit paths, record the hash, and only then mark Phase 7 complete

## Phase 8: Environment-Backed Runtime Preparation — COMPLETE

**Phase-owned paths:**

- `src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapProvider.cs`
- `src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapStartupRunner.cs`
- `tests/Explore.Infrastructure.Tests/Infrastructure/ConfiguredAdministratorBootstrapProviderTests.cs`

- [ ] **8.1 Author failing Infrastructure tests for configuration parsing, generation convergence, recovery, finality, and zero-PII diagnostics, verifying the environment-backed authority is absent**
  - **Files:** new
    `tests/Explore.Infrastructure.Tests/Infrastructure/ConfiguredAdministratorBootstrapProviderTests.cs`
  - **Acceptance:** tests cover same/different digest, generation correction,
    completed drift, partial matrix, selected secret authority, and captured
    log values without mocking internal repositories
  - **Effort:** L
  - **Dependencies:** Phases 1–7

- [ ] **8.2 Implement the unregistered provider and preparation runner, verifying every Phase 8 Red turns Green while DI still resolves the disabled provider**
  - **Files:** new configured provider/startup runner and Task 8.1 tests
  - **Acceptance:** valid pending is healthy/authentication-capable; invalid
    state fails closed; fingerprints and diagnostics are value-free; no
    composition root activates the provider
  - **Effort:** L
  - **Dependencies:** 8.1

### Phase 8 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and verify exit code 0 or record the exact proven unrelated shared-tree failure
- [ ] Run `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1` once and verify configuration/recovery/redaction tests pass
- [ ] Confirm the Phase 8 owned lane is green, the environment-backed provider remains unregistered, and no phase-attributable failure remains

### Phase 8 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `refactor(onboarding): prepare environment-backed bootstrap authority`
- **Default description:** `Implement value-free configured-administrator parsing, generation convergence, and recovery while the disabled runtime provider remains registered.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: unregistered runtime authority ahead of final activation`
- **Commit paths:** `src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapProvider.cs`; `src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapStartupRunner.cs`; `tests/Explore.Infrastructure.Tests/Infrastructure/ConfiguredAdministratorBootstrapProviderTests.cs`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff -- src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapProvider.cs src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapStartupRunner.cs tests/Explore.Infrastructure.Tests/Infrastructure/ConfiguredAdministratorBootstrapProviderTests.cs`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapProvider.cs src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapStartupRunner.cs tests/Explore.Infrastructure.Tests/Infrastructure/ConfiguredAdministratorBootstrapProviderTests.cs`
- **Commit command:** `git commit --only -m "refactor(onboarding): prepare environment-backed bootstrap authority" -m "Implement value-free configured-administrator parsing, generation convergence, and recovery while the disabled runtime provider remains registered." -m "Changelog: skip" -m "Changelog-Reason: unregistered runtime authority ahead of final activation" -- src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapProvider.cs src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapStartupRunner.cs tests/Explore.Infrastructure.Tests/Infrastructure/ConfiguredAdministratorBootstrapProviderTests.cs`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands and verify every named Phase 8 path/hunk is wholly phase-owned before staging and committing
- [ ] Only if the activation outcome, breaking classification, change fragment, or atomic split materially changes, load `conventional-commit` and verify complete replacement contracts plus reason are recorded here before any commit
- [ ] Run the post-commit command, verify the committed file list equals Phase 8 Commit paths and terminal trailers are exact, record the hash, and only then mark Phase 8 complete

## Phase 9: Split And Standalone Startup Composition — COMPLETE

**Phase-owned paths:**

- `src/Explore.API/Hosting/ApiHostStartupExtensions.cs`
- `src/Event.Standalone/Program.cs`
- `tests/Event.Standalone.IntegrationTests/ConfiguredAdministratorBootstrapStartupTests.cs`

- [ ] **9.1 Author failing Standalone startup ordering tests and verify preparation is not yet called after migration/manifest completion**
  - **Files:** new
    `tests/Event.Standalone.IntegrationTests/ConfiguredAdministratorBootstrapStartupTests.cs`
  - **Acceptance:** exact startup signals prove migration/seed then manifest
    then preparation then HTTP; disabled preparation is a no-op; failures block
    startup without token/cookie authority
  - **Effort:** M
  - **Dependencies:** Phase 8

- [ ] **9.2 Wire Split and Standalone preparation calls, verifying the Phase 9 Red turns Green while disabled registration preserves runtime behavior**
  - **Files:** existing API startup and Standalone Program plus Task 9.1 tests
  - **Acceptance:** both topologies share ordering and trust boundaries; no
    second manifest owner, scheduler, or background retry loop is introduced
  - **Effort:** M
  - **Dependencies:** 9.1

### Phase 9 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and verify exit code 0 or record the exact proven unrelated shared-tree failure
- [ ] Run `dotnet test --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --configuration Release --verbosity quiet` once and verify startup ordering/topology parity passes
- [ ] Confirm the Phase 9 owned lane is green, configured runtime remains disabled, and no phase-attributable failure remains

### Phase 9 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `refactor(onboarding): compose dormant bootstrap preparation`
- **Default description:** `Invoke configured-administrator preparation after migration and manifest completion in Split and Standalone while disabled registration preserves existing runtime behavior.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: dormant startup composition ahead of runtime activation`
- **Commit paths:** `src/Explore.API/Hosting/ApiHostStartupExtensions.cs`; `src/Event.Standalone/Program.cs`; `tests/Event.Standalone.IntegrationTests/ConfiguredAdministratorBootstrapStartupTests.cs`
- **Pre-commit inspection commands:** `git status --short`; `git diff -- src/Explore.API/Hosting/ApiHostStartupExtensions.cs src/Event.Standalone/Program.cs tests/Event.Standalone.IntegrationTests/ConfiguredAdministratorBootstrapStartupTests.cs`; `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.API/Hosting/ApiHostStartupExtensions.cs src/Event.Standalone/Program.cs tests/Event.Standalone.IntegrationTests/ConfiguredAdministratorBootstrapStartupTests.cs`
- **Commit command:** `git commit --only -m "refactor(onboarding): compose dormant bootstrap preparation" -m "Invoke configured-administrator preparation after migration and manifest completion in Split and Standalone while disabled registration preserves existing runtime behavior." -m "Changelog: skip" -m "Changelog-Reason: dormant startup composition ahead of runtime activation" -- src/Explore.API/Hosting/ApiHostStartupExtensions.cs src/Event.Standalone/Program.cs tests/Event.Standalone.IntegrationTests/ConfiguredAdministratorBootstrapStartupTests.cs`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact Phase 9 inspection commands and verify every named path/hunk is wholly phase-owned before committing
- [ ] Only if the contract became materially false, load `conventional-commit` and verify a complete replacement contract plus reason is recorded before any commit
- [ ] Run the post-commit command, verify the committed file list equals Phase 9 Commit paths, record the hash, and only then mark Phase 9 complete

## Phase 10: Runtime Activation, Architecture, Operations, And Release Evidence — COMPLETE

**Phase-owned paths:**

- `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- `tests/Event.Architecture.Tests/ConfiguredAdministratorBootstrapArchitectureTests.cs`
- `docs/CONFIGURATION.md`
- `docs/CONFIGURATION_MANIFEST.md`
- `docs/SECRETS.md`
- `docs/SELF_HOSTING.md`
- `docs/TROUBLESHOOTING.md`
- `docs/OPERATIONS.md`
- `docs/BACKUP_RESTORE_UPGRADE.md`
- `schemas/islamu-event.md`
- `docs/releases/changes/CHG-01M1EQWDAHHXQ3AD29B4Y0645B.yaml`

- [ ] **10.1 Author failing architecture ratchets and verify runtime Setup dependency, outward Application coupling, duplicate identity authority, or generated ownership would be rejected**
  - **Files:** new
    `tests/Event.Architecture.Tests/ConfiguredAdministratorBootstrapArchitectureTests.cs`
  - **Acceptance:** executable compiled/project-reference contracts protect
    Clean Architecture, offline Setup, canonical identity, and generated files
    without source/prose scraping
  - **Effort:** M
  - **Dependencies:** Phase 9

- [ ] **10.2 Replace disabled DI registration with environment-backed authority, verifying architecture Red turns Green and valid configured mode is now the only activation path**
  - **Files:** existing `InfrastructureServicesRegistration.cs` and Task 10.1
    tests
  - **Acceptance:** explicit Interactive and ConfiguredAdministrator modes
    remain exhaustive; no first-login fallback; no runtime Setup reference
  - **Effort:** S
  - **Dependencies:** 10.1

- [ ] **10.3 Update operator/schema/release contracts and verify activation fragment `CHG-01M1EQWDAHHXQ3AD29B4Y0645B` documents Security, Configuration, OpenAPI, and Operator impacts without identity values**
  - **Files:** seven exact docs, DBML, and activation fragment
  - **Acceptance:** docs cover valid matrices, first sign-in, correction,
    restart, completion, selector removal, backup/restore, troubleshooting,
    value-free diagnostics, and no manifest/Setup runtime authority; fragment
    matches Phase 10 footer
  - **Effort:** L
  - **Dependencies:** 10.2

### Phase 10 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and verify exit code 0 or record the exact proven unrelated shared-tree failure
- [ ] Run `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` once and verify configured-bootstrap architecture/generator/offline boundaries pass
- [ ] Confirm all ten phase lanes are green and capture value-free Tier 1 evidence under `.omo/evidence/20260901-headless-instance-onboarding/`
- [ ] Run anonymized Epistemic MAD security/operations review and verify weighted approval with no unresolved critical or high finding

### Phase 10 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `feat(onboarding): enable configured administrator bootstrap`
- **Default description:** `Activate exact Keycloak issuer/subject and verified ATProto DID claiming after manifest preparation, with recoverable pending state, fail-closed routing, and value-free operator evidence.`
- **Changelog treatment:** Change fragment `CHG-01M1EQWDAHHXQ3AD29B4Y0645B`
- **Required trailers:** `Change-Id: CHG-01M1EQWDAHHXQ3AD29B4Y0645B`
- **Commit paths:** `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`; `tests/Event.Architecture.Tests/ConfiguredAdministratorBootstrapArchitectureTests.cs`; `docs/CONFIGURATION.md`; `docs/CONFIGURATION_MANIFEST.md`; `docs/SECRETS.md`; `docs/SELF_HOSTING.md`; `docs/TROUBLESHOOTING.md`; `docs/OPERATIONS.md`; `docs/BACKUP_RESTORE_UPGRADE.md`; `schemas/islamu-event.md`; `docs/releases/changes/CHG-01M1EQWDAHHXQ3AD29B4Y0645B.yaml`
- **Pre-commit inspection commands:** `git status --short`; `git diff -- src/Explore.Infrastructure/InfrastructureServicesRegistration.cs tests/Event.Architecture.Tests/ConfiguredAdministratorBootstrapArchitectureTests.cs docs/CONFIGURATION.md docs/CONFIGURATION_MANIFEST.md docs/SECRETS.md docs/SELF_HOSTING.md docs/TROUBLESHOOTING.md docs/OPERATIONS.md docs/BACKUP_RESTORE_UPGRADE.md schemas/islamu-event.md docs/releases/changes/CHG-01M1EQWDAHHXQ3AD29B4Y0645B.yaml`; `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Infrastructure/InfrastructureServicesRegistration.cs tests/Event.Architecture.Tests/ConfiguredAdministratorBootstrapArchitectureTests.cs docs/CONFIGURATION.md docs/CONFIGURATION_MANIFEST.md docs/SECRETS.md docs/SELF_HOSTING.md docs/TROUBLESHOOTING.md docs/OPERATIONS.md docs/BACKUP_RESTORE_UPGRADE.md schemas/islamu-event.md docs/releases/changes/CHG-01M1EQWDAHHXQ3AD29B4Y0645B.yaml`
- **Commit command:** `git commit --only -m "feat(onboarding): enable configured administrator bootstrap" -m "Activate exact Keycloak issuer/subject and verified ATProto DID claiming after manifest preparation, with recoverable pending state, fail-closed routing, and value-free operator evidence." -m "Change-Id: CHG-01M1EQWDAHHXQ3AD29B4Y0645B" -- src/Explore.Infrastructure/InfrastructureServicesRegistration.cs tests/Event.Architecture.Tests/ConfiguredAdministratorBootstrapArchitectureTests.cs docs/CONFIGURATION.md docs/CONFIGURATION_MANIFEST.md docs/SECRETS.md docs/SELF_HOSTING.md docs/TROUBLESHOOTING.md docs/OPERATIONS.md docs/BACKUP_RESTORE_UPGRADE.md schemas/islamu-event.md docs/releases/changes/CHG-01M1EQWDAHHXQ3AD29B4Y0645B.yaml`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run exact Phase 10 inspection commands and verify every named path/hunk is wholly phase-owned before committing
- [ ] Only if activation outcome or change classification materially changes, load `conventional-commit` and verify complete replacement contracts plus reason are recorded before commit
- [ ] Run the post-commit command, verify file list/trailers equal Phase 10 contract, record hash, and only then mark Phase 10 and workstream complete

## Remaining / Deferred Work

- **Post-completion administrator transfer/recovery:** intentionally excluded.
  Use normal administrator governance, database backup restore, or a separately
  approved one-shot operator-authority workstream. Trigger: user requests a
  formal break-glass design.
- **Setup Assistant live connectivity:** explicitly rejected. Trigger: a new
  user request plus fresh Tier 1 plan/I-VSD review.
- **Additional identity providers:** excluded from initial configured claim.
  Trigger: provider has a stable cryptographically validated authority+subject
  contract and its own plan slice.
- **Rendered onboarding UI changes:** none planned; configured mode suppresses
  UI and interactive mode remains unchanged.
