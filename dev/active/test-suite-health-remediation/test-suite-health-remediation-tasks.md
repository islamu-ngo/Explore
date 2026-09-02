<!-- ABOUTME: Hot execution ledger for the test-suite health remediation workstream. -->
<!-- ABOUTME: Owns granular tasks, phase verification gates, and pre-authored phase commit contracts. -->

# Test Suite Health Remediation — Task Checklist

Last Updated: 2026-09-02 Europe/Brussels

## Status Summary
- **Overall status:** Draft
- **Completed:** 0/28 implementation tasks (phase verification and commit closure tracked separately)
- **Current priority:** User review and approval of the planning triad
- **Next recommended slice:** Phase 1 — Deterministic Secrets Lane (no dependencies, smallest blast radius, unblocks an entire currently-unverifiable project)
- **Review state:**
  - I-VSD report: `islamic-value-sensitive-design/i-vsd-test-suite-health-remediation.md`
  - I-VSD reviewed input revision: `9776cda0654511f5ba07ad096d15f3a307d8ce9d8bcaad0c8256ee33b6f52a6a`
  - I-VSD status / disposition: `current` / `plan-aligned`
  - CTO review: Reviewed — Approved with required changes (see `test-suite-health-remediation-cto-review.md`); triad rewritten to eliminate commit placeholders and shard Phase 6 execution
  - User approval: Awaiting approval

## Implementation Maintenance Rules
- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task `🟡 IN PROGRESS` when it spans multiple edits or a handoff; skip that churn for tiny tasks completed immediately.
- Check a substantial completed task immediately; reconcile small completed tasks no later than phase end.
- Add discovered work under its owning phase and keep completed count, priority, next slice, deferred work, and update date accurate.
- Check a phase complete only after all implementation, phase-verification disposition, and phase-commit checkboxes pass.
- Close every verified phase immediately with its phase-owned Conventional Commit; this approved checklist is standing authorization to commit without another prompt.
- Use the planning-authored default title, description, changelog treatment, and trailers unchanged while they remain truthful.
- Do not load `conventional-commit` to reuse an approved contract; load it only for a permitted material divergence.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, or live services for verification.
- **Fail-closed disposition rule (`IVSD-M001`):** no test cohort covering security, tenant isolation, privacy, money, concurrency, or state machines may be deleted, skipped, or weakened without a passing stronger replacement. "It was failing" is never sufficient justification.
- **Environment prerequisite for container lanes:** export `TMPDIR` to a directory with free space, `DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock`, `TESTCONTAINERS_RYUK_DISABLED=true`, and `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE` per `.agents/rules/tests.md:43`. Without these the suite reports a fabricated mass regression.

---

## Phase 1: Deterministic Secrets Lane ⏳ NOT STARTED
**Phase-owned paths:** `tests/Explore.Secrets.UnitTests/`

- [ ] **1.1 Identify and bound every outbound network path in the secrets lane**
  - **Files:** `tests/Explore.Secrets.UnitTests/Bootstrap/BootstrapSecretLoaderTests.cs` (existing), secrets test support types (existing)
  - **Acceptance:** every test that configures an Infisical endpoint resolves through a bounded transport or a substituted fake, and verify by running `--treenode-filter "/*/*/BootstrapSecretLoaderTests/*"` that the class completes in seconds rather than hanging
  - **Effort:** M
  - **Dependencies:** none
- [ ] **1.2 Preserve the fail-closed no-fallback assertions while removing egress**
  - **Files:** `tests/Explore.Secrets.UnitTests/Bootstrap/BootstrapSecretLoaderTests.cs` (existing)
  - **Acceptance:** the assertions that Infisical selection without credentials and with an invalid URL never fall back to another provider still fail when the guard is removed, and verify by temporarily inverting the production guard locally that the test goes red before restoring it
  - **Effort:** S
  - **Dependencies:** 1.1

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project tests/Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet` once and record the executed test count, which must be nonzero.
- [ ] Confirm the phase-owned verification lane is green and no phase-attributable failure remains.

### Phase 1 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract
- **Default title:** `test(testing): make the secrets verification lane finish deterministically`
- **Default description:** `Replace unbounded outbound secret-provider calls in the bootstrap secret loader tests with a bounded transport so the lane executes a nonzero test count in seconds instead of hanging indefinitely. The fail-closed assertions that a selected Infisical provider never silently falls back to another source are preserved unchanged.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:**
  - `Changelog: skip`
  - `Changelog-Reason: Internal test determinism work with no product behavior change`
- **Commit paths:** `tests/Explore.Secrets.UnitTests/`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- tests/Explore.Secrets.UnitTests/`
- **Commit command:** `git commit --only -m "test(testing): make the secrets verification lane finish deterministically" -m "Replace unbounded outbound secret-provider calls in the bootstrap secret loader tests with a bounded transport so the lane executes a nonzero test count in seconds instead of hanging indefinitely. The fail-closed assertions that a selected Infisical provider never silently falls back to another source are preserved unchanged." -m "Changelog: skip" -m "Changelog-Reason: Internal test determinism work with no product behavior change" -- tests/Explore.Secrets.UnitTests/`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path is phase-owned, and execute the exact staging and commit commands.
- [ ] Only if the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts`.
- [ ] Run the post-commit verification command, confirm the file list equals `Commit paths`, and record the hash.

---

## Phase 2: API Integration Fixture Isolation And Identifier Alignment ⏳ NOT STARTED
**Phase-owned paths:** `tests/Event.API.IntegrationTests/`

- [ ] **2.1 Give the five cascade-affected controller classes an owned fixture lifetime**
  - **Files:** `tests/Event.API.IntegrationTests/` shared factory/fixture types (existing); the `StorageObjectControllerTests`, `TagControllerTests`, `TenantControllerTests`, `TenantSettingsDocumentsControllerAnonymousTests`, and `UserControllerTests` classes (existing)
  - **Acceptance:** a whole-assembly run produces zero `ObjectDisposedException: TestServer` failures, and verify by running `--treenode-filter "/*/*/TagControllerTests/*"` and then the whole assembly that the isolated and in-suite results are identical
  - **Effort:** L
  - **Dependencies:** none
- [ ] **2.2 Align seeded aggregate identifiers with the UUIDv7 domain invariant**
  - **Files:** `tests/Event.API.IntegrationTests/Features/SetupSecretAuthorizationMatrixTests.cs` (existing) and any other fixture helper seeding aggregate identifiers (existing)
  - **Acceptance:** no fixture seeds a non-UUIDv7 aggregate identifier and `RequireUuidV7` is not relaxed, and verify by running `--treenode-filter "/*/*/SetupSecretAuthorizationMatrixTests/*"` that no `ArgumentException: Identifier must be an RFC 4122 UUIDv7 value` remains
  - **Effort:** S
  - **Dependencies:** none
- [ ] **2.3 Disposition every residual API failure that is neither cascade nor identifier related**
  - **Files:** `tests/Event.API.IntegrationTests/` (existing)
  - **Acceptance:** each remaining failure is recorded in this ledger as fixed, replaced by a stronger assertion, or explicitly deferred with a named reason and owning phase, and verify the recorded disposition count equals the residual failure count
  - **Effort:** M
  - **Dependencies:** 2.1, 2.2

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` once and record the pass evidence and lane duration.
- [ ] Confirm the phase-owned verification lane is green apart from failures explicitly deferred to Phase 8, and no cascade failure remains.

### Phase 2 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract
- **Default title:** `test(testing): stop a disposed server fixture from masking API test results`
- **Default description:** `Give the affected API controller test classes a fixture lifetime they own so disposing one web application factory can no longer fail dozens of unrelated tests with ObjectDisposedException, and seed aggregate identifiers as UUIDv7 so fixtures satisfy the production identifier invariant instead of contradicting it.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:**
  - `Changelog: skip`
  - `Changelog-Reason: Internal test fixture isolation with no product behavior change`
- **Commit paths:** `tests/Event.API.IntegrationTests/`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- tests/Event.API.IntegrationTests/`
- **Commit command:** `git commit --only -m "test(testing): stop a disposed server fixture from masking API test results" -m "Give the affected API controller test classes a fixture lifetime they own so disposing one web application factory can no longer fail dozens of unrelated tests with ObjectDisposedException, and seed aggregate identifiers as UUIDv7 so fixtures satisfy the production identifier invariant instead of contradicting it." -m "Changelog: skip" -m "Changelog-Reason: Internal test fixture isolation with no product behavior change" -- tests/Event.API.IntegrationTests/`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path is phase-owned, and execute the exact staging and commit commands.
- [ ] Only if the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts`.
- [ ] Run the post-commit verification command, confirm the file list equals `Commit paths`, and record the hash.

---

## Phase 3: Configuration Manifest Contract Test Rebinding ⏳ NOT STARTED
**Phase-owned paths:** `tests/Event.Application.UnitTests/Features/ConfigurationManifest/`

- [ ] **3.1 Resolve contract types from the assembly that declares them**
  - **Files:** `tests/Event.Application.UnitTests/Features/ConfigurationManifest/ConfigurationManifestContractTests.cs` (existing)
  - **Acceptance:** the assembly anchor resolves `ConfigurationManifestContractMetadata` and `ConfigurationManifestV1Alpha2` from `Event.Wire.Contracts` rather than `Explore.Application`, and verify by running `--treenode-filter "/*/*/ConfigurationManifestContractTests/*"` that all 10 previously failing assertions pass
  - **Effort:** S
  - **Dependencies:** none
- [ ] **3.2 Confirm the cohort still enforces its full contract surface**
  - **Files:** `tests/Event.Application.UnitTests/Features/ConfigurationManifest/ConfigurationManifestContractTests.cs` (existing)
  - **Acceptance:** contract identity, closed required scopes, strict unknown-member rejection, and ordinal ordering assertions are all retained with none deleted or weakened, and verify by confirming the executed test count for the class is not lower than its pre-change discovered count
  - **Effort:** S
  - **Dependencies:** 3.1

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` once and record pass evidence.
- [ ] Confirm the phase-owned verification lane is green and no phase-attributable failure remains.

### Phase 3 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract
- **Default title:** `test(testing): restore the configuration manifest wire contract guard`
- **Default description:** `Resolve the v1alpha2 configuration manifest contract types from the Event.Wire.Contracts assembly that actually declares them instead of the application assembly, so the cohort once again enforces contract identity, closed instance and tenant scopes, strict unknown-member rejection, and ordinal ordering.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:**
  - `Changelog: skip`
  - `Changelog-Reason: Internal test correction with no product behavior change`
- **Commit paths:** `tests/Event.Application.UnitTests/Features/ConfigurationManifest/`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- tests/Event.Application.UnitTests/Features/ConfigurationManifest/`
- **Commit command:** `git commit --only -m "test(testing): restore the configuration manifest wire contract guard" -m "Resolve the v1alpha2 configuration manifest contract types from the Event.Wire.Contracts assembly that actually declares them instead of the application assembly, so the cohort once again enforces contract identity, closed instance and tenant scopes, strict unknown-member rejection, and ordinal ordering." -m "Changelog: skip" -m "Changelog-Reason: Internal test correction with no product behavior change" -- tests/Event.Application.UnitTests/Features/ConfigurationManifest/`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path is phase-owned, and execute the exact staging and commit commands.
- [ ] Only if the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts`.
- [ ] Run the post-commit verification command, confirm the file list equals `Commit paths`, and record the hash.

---

## Phase 4: Agent Registry And Release Policy Path Truth ⏳ NOT STARTED
**Phase-owned paths:** `.agents/contract/intents.yaml`, `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/`

- [ ] **4.1 Remove the stale workstream triad reference from the intent registry**
  - **Files:** `.agents/contract/intents.yaml` (existing, lines 1077/1097/1117 reference `dev/active/agentic-workflow-control-plane/`)
  - **Acceptance:** the `agent-workflow-guard` intent references only workstream files that exist, and verify by running `--treenode-filter "/*/*/StrongTypingIntentArchitectureTests/*"` that `AllIntents_MustNotReferenceNonExistentActiveOrArchivedTriads` passes
  - **Effort:** S
  - **Dependencies:** none
- [ ] **4.2 Point the release input policy test at the real change-fragment directory**
  - **Files:** `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ReleaseInputPolicyTests.cs` (existing, line 44 resolves `docs/releases/changes`)
  - **Acceptance:** the test discovers fragments under `docs/internal/releases/changes` and every committed fragment passes policy validation, and verify by running `--treenode-filter "/*/*/*/RepositoryChangeFragmentsPassReleaseInputPolicy"` that it passes with a nonzero fragment count
  - **Effort:** S
  - **Dependencies:** none

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet` once and record pass evidence.
- [ ] Confirm the phase-owned verification lane is green and no phase-attributable failure remains.

### Phase 4 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract
- **Default title:** `fix(testing): point registry and release policy checks at paths that exist`
- **Default description:** `Drop the agent workflow guard intent's reference to a workstream triad that is no longer present, and resolve repository change fragments from their real location under docs/internal so the release input policy check validates the fragments the repository actually ships.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:**
  - `Changelog: skip`
  - `Changelog-Reason: Internal agent registry and release tooling path correction with no product behavior change`
- **Commit paths:** `.agents/contract/intents.yaml`, `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- .agents/contract/intents.yaml eng/release/tests/ISLAMU.ReleaseEngineering.Tests/`
- **Commit command:** `git commit --only -m "fix(testing): point registry and release policy checks at paths that exist" -m "Drop the agent workflow guard intent's reference to a workstream triad that is no longer present, and resolve repository change fragments from their real location under docs/internal so the release input policy check validates the fragments the repository actually ships." -m "Changelog: skip" -m "Changelog-Reason: Internal agent registry and release tooling path correction with no product behavior change" -- .agents/contract/intents.yaml eng/release/tests/ISLAMU.ReleaseEngineering.Tests/`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path is phase-owned, and execute the exact staging and commit commands.
- [ ] Only if the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts`.
- [ ] Run the post-commit verification command, confirm the file list equals `Commit paths`, and record the hash.

---

## Phase 5: Authorization Surface Classification Completeness ⏳ NOT STARTED
**Phase-owned paths:** `src/Explore.Application/Features/InstanceOnboarding/`, `tests/Event.Architecture.Tests/`, `docs/internal/releases/changes/CHG-01M1G5H8QN3B7D9E2F4K6M0P1R.yaml`

- [ ] **5.1 (Red Phase) Author the failing fail-closed invariant test for the instance-administrator claim**
  - **Files:** `tests/Event.Architecture.Tests/` new invariant test (new)
  - **Acceptance:** a test asserts the Section 3 adversarial scenario — an unauthenticated or non-entitled actor attempting to claim instance administration is refused with no authority transfer — and verify it fails as a red anchor before the classification exists via `--treenode-filter`
  - **Effort:** M
  - **Dependencies:** none
- [ ] **5.2 (Green Phase) Classify the mutating instance-administrator claim command**
  - **Files:** `src/Explore.Application/Features/InstanceOnboarding/Requests/Commands/ClaimConfiguredInstanceAdministratorCommand.cs` (existing) and its handler (existing)
  - **Acceptance:** the authorization surface inventory reports zero unclassified mutating requests through a real classification rather than an inventory exemption, and verify by running `--treenode-filter "/*/*/AuthorizationSurfaceGuardrailTests/*"` that both previously failing guardrail assertions pass
  - **Effort:** M
  - **Dependencies:** 5.1
- [ ] **5.3 Create the governed change fragment for the authorization gap closure**
  - **Files:** `docs/internal/releases/changes/CHG-01M1G5H8QN3B7D9E2F4K6M0P1R.yaml` (new)
  - **Acceptance:** the fragment is valid YAML matching the structure of existing fragments with `Change-Id: CHG-01M1G5H8QN3B7D9E2F4K6M0P1R`, and verify it passes `RepositoryChangeFragmentsPassReleaseInputPolicy`
  - **Effort:** S
  - **Dependencies:** 5.2

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` once and record pass evidence.
- [ ] Confirm the phase-owned verification lane is green and no phase-attributable failure remains.

### Phase 5 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract
- **Default title:** `fix(access): close the unclassified instance administrator claim gap`
- **Default description:** `Give the instance administrator claim command an explicit authorization classification so the compiled authorization surface reports no unclassified mutating request, and anchor the fail-closed behavior with an invariant test proving an unauthorized actor cannot acquire instance administrative authority.`
- **Changelog treatment:** Change fragment `CHG-01M1G5H8QN3B7D9E2F4K6M0P1R`
- **Required trailers:**
  - `Change-Id: CHG-01M1G5H8QN3B7D9E2F4K6M0P1R`
- **Commit paths:** `src/Explore.Application/Features/InstanceOnboarding/`, `tests/Event.Architecture.Tests/`, `docs/internal/releases/changes/CHG-01M1G5H8QN3B7D9E2F4K6M0P1R.yaml`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Application/Features/InstanceOnboarding/ tests/Event.Architecture.Tests/ docs/internal/releases/changes/CHG-01M1G5H8QN3B7D9E2F4K6M0P1R.yaml`
- **Commit command:** `git commit --only -m "fix(access): close the unclassified instance administrator claim gap" -m "Give the instance administrator claim command an explicit authorization classification so the compiled authorization surface reports no unclassified mutating request, and anchor the fail-closed behavior with an invariant test proving an unauthorized actor cannot acquire instance administrative authority." -m "Change-Id: CHG-01M1G5H8QN3B7D9E2F4K6M0P1R" -- src/Explore.Application/Features/InstanceOnboarding/ tests/Event.Architecture.Tests/ docs/internal/releases/changes/CHG-01M1G5H8QN3B7D9E2F4K6M0P1R.yaml`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path is phase-owned, and execute the exact staging and commit commands.
- [ ] Only if the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts`.
- [ ] Run the post-commit verification command, confirm the file list equals `Commit paths`, and record the hash.

---

## Phase 6: Persistence Fixture Seed And Constraint Integrity ⏳ NOT STARTED
**Phase-owned paths:** `tests/Event.Persistence.IntegrationTests/`

- [ ] **6.1 Measure and disposition the persistence baseline via sharded passes before changing code**
  - **Files:** `tests/Event.Persistence.IntegrationTests/` (existing, read-only for this task)
  - **Acceptance:** execute targeted class-filtered passes per root-cause group (Group 1: FK lookup seeds e.g. `--treenode-filter "/*/*/EventSessionLifecycleConstraintTests/*"`; Group 2: EF provider warnings/fixture lifetime e.g. `--treenode-filter "/*/*/ExploreDbContextModelProviderTests/*"`; Group 3: provider migrations) avoiding the 30-minute monolithic runner timeout deadlock; each failure is grouped in this ledger and dispositioned as fixture-seed, fixture-lifetime, provider-specific, or genuine defect; verify the dispositioned count equals the observed failure count before any fix begins
  - **Effort:** M
  - **Dependencies:** none
- [ ] **6.2 Provision seeded lookup data in the persistence fixture**
  - **Files:** `tests/Event.Persistence.IntegrationTests/` fixture and schema-provisioning types (existing)
  - **Acceptance:** aggregates referencing lookup tables satisfy their foreign keys without any constraint being dropped or relaxed, and verify by running `--treenode-filter "/*/*/EventSessionLifecycleConstraintTests/*"` that the class passes 8/8 where it previously failed 7/8 in isolation
  - **Effort:** L
  - **Dependencies:** 6.1
- [ ] **6.3 Resolve the fixture-lifetime pressure that escalates EF provider warnings to failures**
  - **Files:** `tests/Event.Persistence.IntegrationTests/` fixture and context-factory types (existing)
  - **Acceptance:** a whole-assembly run no longer throws `ManyServiceProvidersCreatedWarning`, and verify the whole-assembly result for a sampled class equals its isolated result
  - **Effort:** L
  - **Dependencies:** 6.1
- [ ] **6.4 Disposition every residual persistence failure**
  - **Files:** `tests/Event.Persistence.IntegrationTests/` (existing)
  - **Acceptance:** each remaining failure is recorded as fixed, replaced by a stronger assertion, or explicitly deferred with a named reason and owner, and verify the recorded disposition count equals the residual failure count
  - **Effort:** M
  - **Dependencies:** 6.2, 6.3

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` once and record pass evidence and lane duration.
- [ ] Confirm the phase-owned verification lane is green apart from explicitly dispositioned deferrals, and no constraint was relaxed to achieve it.

### Phase 6 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract
- **Default title:** `test(testing): give persistence fixtures the seed data their constraints require`
- **Default description:** `Provision seeded lookup rows and a stable context lifetime in the persistence integration fixture so aggregates satisfy real foreign key constraints and whole-assembly execution no longer escalates Entity Framework provider warnings into failures. No foreign key, check constraint, or exclusion constraint is relaxed.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:**
  - `Changelog: skip`
  - `Changelog-Reason: Internal persistence test fixture hardening with no product behavior change`
- **Commit paths:** `tests/Event.Persistence.IntegrationTests/`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- tests/Event.Persistence.IntegrationTests/`
- **Commit command:** `git commit --only -m "test(testing): give persistence fixtures the seed data their constraints require" -m "Provision seeded lookup rows and a stable context lifetime in the persistence integration fixture so aggregates satisfy real foreign key constraints and whole-assembly execution no longer escalates Entity Framework provider warnings into failures. No foreign key, check constraint, or exclusion constraint is relaxed." -m "Changelog: skip" -m "Changelog-Reason: Internal persistence test fixture hardening with no product behavior change" -- tests/Event.Persistence.IntegrationTests/`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path is phase-owned, and execute the exact staging and commit commands.
- [ ] Only if the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts`.
- [ ] Run the post-commit verification command, confirm the file list equals `Commit paths`, and record the hash.

---

## Phase 7: Startup Composition Completeness ⏳ NOT STARTED
**Phase-owned paths:** `src/Explore.Persistence/ConfigurationManifestPersistenceServicesRegistration.cs`, `src/Explore.Infrastructure/ConfigurationManifest/ConfigurationManifestStartupServicesRegistration.cs`, `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `tests/Explore.Infrastructure.Tests/`

- [ ] **7.1 Trace and wire missing dependency descriptors in the deferred startup graph**
  - **Files:** `src/Explore.Persistence/ConfigurationManifestPersistenceServicesRegistration.cs` (existing), `src/Explore.Infrastructure/ConfigurationManifest/ConfigurationManifestStartupServicesRegistration.cs` (existing), `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` (existing)
  - **Acceptance:** ensure registration of each unresolvable service — `IDataProtectionProvider`, `IConfigurationImportSessionRepository`, `ILegalDocumentRepository`, and `IHierarchicalSettingsResolver` — satisfies Clean Architecture layer rules without application taking infrastructure dependencies
  - **Effort:** M
  - **Dependencies:** none
- [ ] **7.2 (Green Phase) Complete the deferred startup composition**
  - **Files:** `src/Explore.Persistence/ConfigurationManifestPersistenceServicesRegistration.cs` (existing), `src/Explore.Infrastructure/ConfigurationManifest/ConfigurationManifestStartupServicesRegistration.cs` (existing), `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` (existing)
  - **Acceptance:** container validation of the deferred startup graph succeeds with no unresolvable dependency and no Application type takes an infrastructure dependency to achieve it, and verify by running `--treenode-filter "/*/*/*/DeferredStartupGraph_ResolvesWithoutRuntimeEffectServices"` that it passes
  - **Effort:** M
  - **Dependencies:** 7.1
- [ ] **7.3 Disposition the second infrastructure failure**
  - **Files:** `tests/Explore.Infrastructure.Tests/` (existing)
  - **Acceptance:** `ReadStreamAsync_NullRequiredStructure_FailsContractWithoutNullReference` is fixed or explicitly dispositioned with a named reason, and verify by running its targeted filter that the recorded disposition matches the observed result
  - **Effort:** M
  - **Dependencies:** none

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` once and record pass evidence.
- [ ] Confirm the phase-owned verification lane is green and no phase-attributable failure remains.

### Phase 7 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract
- **Default title:** `fix(architecture): resolve every registered configuration portability service at startup`
- **Default description:** `Register the dependencies required by the configuration portability chunk store, import session manager, section applier, and effect delivery services so deferred startup container validation succeeds instead of failing on unresolvable descriptors, moving the failure from first request to startup where it is cheapest to diagnose.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:**
  - `Changelog: skip`
  - `Changelog-Reason: Internal composition-root registration correction with no public capability change`
- **Commit paths:** `src/Explore.Persistence/ConfigurationManifestPersistenceServicesRegistration.cs`, `src/Explore.Infrastructure/ConfigurationManifest/ConfigurationManifestStartupServicesRegistration.cs`, `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `tests/Explore.Infrastructure.Tests/`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Persistence/ConfigurationManifestPersistenceServicesRegistration.cs src/Explore.Infrastructure/ConfigurationManifest/ConfigurationManifestStartupServicesRegistration.cs src/Explore.Infrastructure/InfrastructureServicesRegistration.cs tests/Explore.Infrastructure.Tests/`
- **Commit command:** `git commit --only -m "fix(architecture): resolve every registered configuration portability service at startup" -m "Register the dependencies required by the configuration portability chunk store, import session manager, section applier, and effect delivery services so deferred startup container validation succeeds instead of failing on unresolvable descriptors, moving the failure from first request to startup where it is cheapest to diagnose." -m "Changelog: skip" -m "Changelog-Reason: Internal composition-root registration correction with no public capability change" -- src/Explore.Persistence/ConfigurationManifestPersistenceServicesRegistration.cs src/Explore.Infrastructure/ConfigurationManifest/ConfigurationManifestStartupServicesRegistration.cs src/Explore.Infrastructure/InfrastructureServicesRegistration.cs tests/Explore.Infrastructure.Tests/`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path is phase-owned, and execute the exact staging and commit commands.
- [ ] Only if the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts`.
- [ ] Run the post-commit verification command, confirm the file list equals `Commit paths`, and record the hash.

---

## Phase 8: Legible Degradation For Unconfigured Public Capabilities ⏳ NOT STARTED
**Phase-owned paths:** `src/Explore.Application/Features/Notifications/Handlers/Queries/GetWebPushPublicConfigurationQueryHandler.cs`, `src/Explore.Application/Features/Notifications/Requests/Queries/GetWebPushPublicConfigurationQuery.cs`, `src/Explore.API/Controllers/NotificationController.cs`, `tests/Event.API.IntegrationTests/Features/`

- [ ] **8.1 (Red Phase) Author the unconfigured-capability contract scenarios**
  - **Files:** `tests/Event.API.IntegrationTests/Features/` new or extended contract test (new/existing)
  - **Acceptance:** tests assert both Section 3 scenarios — a successful explicitly disabled representation when signing material is absent, and a successful enabled representation exposing only browser-safe public material — and verify both fail as red anchors against current behavior
  - **Effort:** M
  - **Dependencies:** Phase 2 complete
- [ ] **8.2 (Green Phase) Return an explicit disabled state instead of a server error**
  - **Files:** `src/Explore.Application/Features/Notifications/Handlers/Queries/GetWebPushPublicConfigurationQueryHandler.cs` (existing), `src/Explore.Application/Features/Notifications/Requests/Queries/GetWebPushPublicConfigurationQuery.cs` (existing), `src/Explore.API/Controllers/NotificationController.cs` (existing)
  - **Acceptance:** both public reads return success with an explicit disabled state when unconfigured and expose no private key material in either state, and verify by running `--treenode-filter "/*/*/EndpointAuthorizationMatrixTests/*"` that `Public_Get_Endpoints_ReturnOk` reports no 500; ensure clean-architecture model without legacy compatibility fields
  - **Effort:** M
  - **Dependencies:** 8.1

### Phase 8 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` once and record pass evidence. *(Reused lane; reason recorded in plan Section 6 Phase 8.)*
- [ ] Confirm the phase-owned verification lane is green and no phase-attributable failure remains.

### Phase 8 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract
- **Default title:** `fix(notifications): report web push as disabled instead of failing when unconfigured`
- **Default description:** `Return a successful, explicitly disabled public web push representation when no signing configuration is present, so self-hosted operators who have not enabled web push receive a legible capability state instead of an opaque server error, and browser clients can branch on an explicit flag. No private key material is exposed in either the enabled or disabled state.`
- **Changelog treatment:** Public fix aggregated into release notes under the `notifications` scope
- **Required trailers:** None
- **Commit paths:** `src/Explore.Application/Features/Notifications/Handlers/Queries/GetWebPushPublicConfigurationQueryHandler.cs`, `src/Explore.Application/Features/Notifications/Requests/Queries/GetWebPushPublicConfigurationQuery.cs`, `src/Explore.API/Controllers/NotificationController.cs`, `tests/Event.API.IntegrationTests/Features/`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Application/Features/Notifications/Handlers/Queries/GetWebPushPublicConfigurationQueryHandler.cs src/Explore.Application/Features/Notifications/Requests/Queries/GetWebPushPublicConfigurationQuery.cs src/Explore.API/Controllers/NotificationController.cs tests/Event.API.IntegrationTests/Features/`
- **Commit command:** `git commit --only -m "fix(notifications): report web push as disabled instead of failing when unconfigured" -m "Return a successful, explicitly disabled public web push representation when no signing configuration is present, so self-hosted operators who have not enabled web push receive a legible capability state instead of an opaque server error, and browser clients can branch on an explicit flag. No private key material is exposed in either the enabled or disabled state." -- src/Explore.Application/Features/Notifications/Handlers/Queries/GetWebPushPublicConfigurationQueryHandler.cs src/Explore.Application/Features/Notifications/Requests/Queries/GetWebPushPublicConfigurationQuery.cs src/Explore.API/Controllers/NotificationController.cs tests/Event.API.IntegrationTests/Features/`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path is phase-owned, and execute the exact staging and commit commands.
- [ ] Only if the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts`.
- [ ] Run the post-commit verification command, confirm the file list equals `Commit paths`, and record the hash.

---

## Phase 9: Self-Hosted Shell Availability Without An Identity Provider ⏳ NOT STARTED
**Phase-owned paths:** `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs`, `src/Event.Web.BffHosting/`, `tests/Explore.Blazor.IntegrationTests/`

- [ ] **9.1 Trace the setup redirect and isolate static shell accessibility**
  - **Files:** `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs` (existing, lines 288-295), `tests/Explore.Blazor.IntegrationTests/Endpoints/BffNoKeycloakResilienceTests.cs` (existing)
  - **Acceptance:** confirm that `HandleStartupRedirectAsync` in `MiddlewareExtensions.cs` redirects `/` to `/setup` when onboarding is `InteractivePending`; verify why the mock `IBffOnboardingStatusProvider` in `NoKeycloakBlazorBffWebApplicationFactory` does not avert the redirect or adjust route resolution so static root `/` is served as 200 without Keycloak
  - **Effort:** S
  - **Dependencies:** none
- [ ] **9.2 (Green Phase) Serve the application shell anonymously without an identity provider**
  - **Files:** `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs` (existing), `src/Event.Web.BffHosting/` authorization/endpoint composition (existing)
  - **Acceptance:** the shell root returns 200 with no identity provider configured and retains its content security policy header, and verify by running `--treenode-filter "/*/*/BffNoKeycloakResilienceTests/*"` that `StaticPages_AreAccessible` and `StaticPages_CarryContentSecurityPolicyHeader` both pass
  - **Effort:** M
  - **Dependencies:** 9.1
- [ ] **9.3 (Invariant Anchor) Prove protected areas remain fail-closed**
  - **Files:** `tests/Explore.Blazor.IntegrationTests/Endpoints/` (existing/new)
  - **Acceptance:** a test asserts the Section 3 adversarial scenario — an anonymous caller requesting a control-plane protected resource is refused with no session created and no administrative authority granted — and verify it passes after 9.2 and fails if the shell change is over-applied to protected routes
  - **Effort:** M
  - **Dependencies:** 9.2
- [ ] **9.4 Disposition the remaining Blazor integration failures**
  - **Files:** `tests/Explore.Blazor.IntegrationTests/` (existing)
  - **Acceptance:** the ATProto client metadata, JWKS, handoff cookie, token circuit, and white-label manifest failures are each fixed or explicitly deferred with a named reason and owner, and verify the recorded disposition count equals the residual failure count
  - **Effort:** L
  - **Dependencies:** 9.2

### Phase 9 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` once and record pass evidence.
- [ ] Confirm the phase-owned verification lane is green apart from explicitly dispositioned deferrals, and that the fail-closed assertion in 9.3 passes.

### Phase 9 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract
- **Default title:** `fix(self-hosting): serve the application shell without an identity provider`
- **Default description:** `Allow the browser application shell to be served anonymously when no identity provider is configured, restoring air-gapped and self-hosted availability, while keeping control-plane protected resources fail-closed so no anonymous caller gains administrative authority or a session.`
- **Changelog treatment:** Public fix aggregated into release notes under the `self-hosting` scope
- **Required trailers:** None
- **Commit paths:** `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs`, `src/Event.Web.BffHosting/`, `tests/Explore.Blazor.IntegrationTests/`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- src/Explore.Blazor/Extensions/MiddlewareExtensions.cs src/Event.Web.BffHosting/ tests/Explore.Blazor.IntegrationTests/`
- **Commit command:** `git commit --only -m "fix(self-hosting): serve the application shell without an identity provider" -m "Allow the browser application shell to be served anonymously when no identity provider is configured, restoring air-gapped and self-hosted availability, while keeping control-plane protected resources fail-closed so no anonymous caller gains administrative authority or a session." -- src/Explore.Blazor/Extensions/MiddlewareExtensions.cs src/Event.Web.BffHosting/ tests/Explore.Blazor.IntegrationTests/`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path is phase-owned, and execute the exact staging and commit commands.
- [ ] Only if the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts`.
- [ ] Run the post-commit verification command, confirm the file list equals `Commit paths`, and record the hash.

---

## Phase 10: Lane Ownership, Runbook, And Environment Contract ⏳ NOT STARTED
**Phase-owned paths:** `docs/internal/TESTING.md`, `docs/internal/OPERATIONS.md`, `docs/internal/QUICK_REFERENCE.md`, `docs/internal/GOVERNANCE.md`, `docs/internal/AGENTIC_CONTEXT_ENGINEERING.md`, `.agents/rules/tests.md`, `.omo/rules/tests.md`

- [ ] **10.1 Document every test project's role, lane, prerequisite, and command**
  - **Files:** `docs/internal/TESTING.md` (existing)
  - **Acceptance:** all 22 projects are enumerated — including `eng/release/tests/ISLAMU.ReleaseEngineering.Tests`, which a `tests/**` sweep misses — each with role, fast/container-backed lane, infrastructure prerequisite, and exact command, and verify the enumerated count equals the discovered project count
  - **Effort:** M
  - **Dependencies:** Phases 1-9 complete
- [ ] **10.2 Document the environment prerequisites as one authoritative contract**
  - **Files:** `docs/internal/OPERATIONS.md` (existing), `.agents/rules/tests.md` (existing), `.omo/rules/tests.md` (existing)
  - **Acceptance:** the container-runtime and writable temporary-directory prerequisites are documented once and referenced elsewhere, with the twin rule files byte-identical, and verify with `diff .agents/rules/tests.md .omo/rules/tests.md` reporting no differences
  - **Effort:** S
  - **Dependencies:** none
- [ ] **10.3 Reconcile the intent-mandated context documents with the rationalized lane**
  - **Files:** `docs/internal/QUICK_REFERENCE.md` (existing), `docs/internal/GOVERNANCE.md` (existing), `docs/internal/AGENTIC_CONTEXT_ENGINEERING.md` (existing)
  - **Acceptance:** each document's testing guidance matches the documented lane inventory with no contradictory command or stale project list, and verify no referenced test project or path is absent from the repository
  - **Effort:** M
  - **Dependencies:** 10.1

### Phase 10 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` once and record pass evidence. *(Reused lane; reason recorded in plan Section 6 Phase 10.)*
- [ ] Confirm the phase-owned verification lane is green and no phase-attributable failure remains.

### Phase 10 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract
- **Default title:** `docs(documentation): give every test project a documented role and command`
- **Default description:** `Enumerate all twenty-two executable test projects with their role, lane, infrastructure prerequisite, and exact command, including the release engineering project outside the tests directory that routine sweeps missed, and record the container runtime and temporary directory prerequisites whose absence previously produced a fabricated mass regression.`
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:**
  - `Changelog: skip`
  - `Changelog-Reason: Internal contributor documentation with no product behavior change`
- **Commit paths:** `docs/internal/TESTING.md`, `docs/internal/OPERATIONS.md`, `docs/internal/QUICK_REFERENCE.md`, `docs/internal/GOVERNANCE.md`, `docs/internal/AGENTIC_CONTEXT_ENGINEERING.md`, `.agents/rules/tests.md`, `.omo/rules/tests.md`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- docs/internal/TESTING.md docs/internal/OPERATIONS.md docs/internal/QUICK_REFERENCE.md docs/internal/GOVERNANCE.md docs/internal/AGENTIC_CONTEXT_ENGINEERING.md .agents/rules/tests.md .omo/rules/tests.md`
- **Commit command:** `git commit --only -m "docs(documentation): give every test project a documented role and command" -m "Enumerate all twenty-two executable test projects with their role, lane, infrastructure prerequisite, and exact command, including the release engineering project outside the tests directory that routine sweeps missed, and record the container runtime and temporary directory prerequisites whose absence previously produced a fabricated mass regression." -m "Changelog: skip" -m "Changelog-Reason: Internal contributor documentation with no product behavior change" -- docs/internal/TESTING.md docs/internal/OPERATIONS.md docs/internal/QUICK_REFERENCE.md docs/internal/GOVERNANCE.md docs/internal/AGENTIC_CONTEXT_ENGINEERING.md .agents/rules/tests.md .omo/rules/tests.md`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path is phase-owned, and execute the exact staging and commit commands.
- [ ] Only if the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts`.
- [ ] Run the post-commit verification command, confirm the file list equals `Commit paths`, and record the hash.

---

## Failure Disposition Ledger

Populated during implementation by Tasks 2.3, 6.1, 6.4, 7.3, and 9.4. Every entry records the failing test, its root-cause group, and one disposition: `fixed`, `replaced-by-stronger`, `intentionally-removed-behavior`, or `deferred`. A `deferred` entry requires a named reason and owner. Security, tenant-isolation, privacy, money, concurrency, and state-machine cohorts may not carry a `deferred` disposition without Project Steward escalation per `IVSD-M001`.

| Test | Root-cause group | Disposition | Evidence / reason | Owner task |
|---|---|---|---|---|
| _(populated during implementation)_ | | | | |

## Remaining / Deferred Work
- **Reusable intent for verification-exposed production defects** — deferred. Reason: Phases 5, 7, and 8 currently run under a labeled fallback contract because no intent matches "a production defect discovered by the verification lane". Trigger: if this category recurs in a future workstream, propose a reusable intent entry. Owner: whoever encounters the second occurrence.
- **Coordination with `dev/active/setup-assistant-security-and-portability`** — deferred until Phase 9 begins. Reason: that workstream was updated 2026-09-02 and touches setup/bootstrap gating, which Phase 9 may also touch depending on the Task 9.1 finding. Trigger: Task 9.1 identifying a setup-gate rather than a login redirect. Owner: Phase 9 implementer.
