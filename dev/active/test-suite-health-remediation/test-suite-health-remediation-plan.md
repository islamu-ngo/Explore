<!-- ABOUTME: Canonical implementation plan for restoring and rationalizing the repository verification lane. -->
<!-- ABOUTME: Owns current-state evidence, behavioral contracts, architecture decisions, and phase exit criteria. -->

# Test Suite Health Remediation — Implementation Plan

Last Updated: 2026-09-02 Europe/Brussels

## 0. Planning Metadata

- **Original request:** "run all tests and report failing tests, proposed fix, are they outdated tests? relevant or not, so we can ensure no failing tests remain", followed by "write an implementation plan ... to fully work on the tests and improve, refactor and make sure we are fully perfectly clean, no technical debts and perfect experience for working and maintaining the codebase".
- **Task directory:** `dev/active/test-suite-health-remediation/`
- **Planning status:** Draft
- **Change Classification:** **Mixed, declared per phase.**
  - Phases 1–4, 6, 10 are `Non-Behavioral Delta` — test infrastructure, fixture, registry, and documentation correctness with zero externally observable product behavior change.
  - Phases 5, 7, 8, 9 are `Behavioral Delta` — they change observable server behavior (authorization classification, startup validation, public endpoint responses, BFF shell availability) and therefore carry RFC 2119 requirements and `WHEN`/`THEN` scenarios in Section 3.
- **Matched intents:**
  - `test-suite-rationalization` (primary; `.agents/contract/intents.yaml:297`) — owns Phases 1, 2, 3, 4, 6, 10.
  - `bff-auth-bug` (`.agents/contract/intents.yaml:448`) — owns Phase 9.
  - **Fallback contract** (labeled; no exact intent matched) for Phases 5, 7, 8 — production defects discovered by the verification lane. Constructed from `AGENTS.md` §5, `docs/internal/QUICK_REFERENCE.md`, `.agents/rules/auth-trust-boundaries.md`, and `.agents/rules/api-controllers.md`. A planning task considers a reusable `verification-exposed-production-defect` intent only if this category recurs.
- **Relevant skills:** `refactor-safely`, `criticality-guardrail`, `auth-patterns`, `blazor-bff-patterns`, `dotnet-efcore-guidelines`, `conventional-commit`, `i-vsd`.
- **Relevant rules:** `.agents/rules/tests.md`, `.agents/rules/work-criticality-matrix.md`, `.agents/rules/auth-trust-boundaries.md`, `.agents/rules/api-controllers.md`.
- **Primary layers touched:** Tests (all lanes), API, Application, BFF hosting, Domain-adjacent registration, agent-contract registry, internal documentation.
- **Complexity:** **L**. Evidence-based rationale: 190+ verified failures spanning 6 lanes plus one hanging lane, across 4 architectural layers, with 4 genuine production defects requiring `src/**` changes and one lane whose full baseline exceeds a 20-minute run.
- **I-VSD Document:** [islamic-value-sensitive-design/i-vsd-test-suite-health-remediation.md](../../../islamic-value-sensitive-design/i-vsd-test-suite-health-remediation.md)
- **I-VSD Reviewed Input Revision:** `9776cda0654511f5ba07ad096d15f3a307d8ce9d8bcaad0c8256ee33b6f52a6a`
- **I-VSD Status / Disposition:** `current` / `plan-aligned`
- **CTO Review:** Reviewed — Approved with required changes (see `test-suite-health-remediation-cto-review.md`); triad rewritten in-place to eliminate commit contract placeholders and shard Phase 6 execution.
- **User Approval:** Awaiting approval
- **Grill-Me Intake:** Two material branches were identified and both were resolved from repository evidence without consuming a user question.
  1. *Are the 10 `ConfigurationManifestContractTests` failures an abandoned contract (delete) or a mislocated lookup (repoint)?* — **Resolved: repoint.** `ConfigurationManifestContractMetadata` and `ConfigurationManifestV1Alpha2` exist at `src/Event.Wire.Contracts/ConfigurationPortability/ConfigurationManifestV1Alpha2.cs:10` and are consumed by live controllers and handlers. The test resolves types from `typeof(SettingUpsertService).Assembly` (Explore.Application), which never contained them.
  2. *Is the BFF shell 302 an intended setup gate or a self-hosting regression?* — **Resolved: regression.** `.agents/rules/auth-trust-boundaries.md` rule 6 mandates air-gapped fallback, and the test's own stated invariant is that static pages serve "regardless of auth configuration".
  - Explicitly deferred: nothing. No branch remains that would alter scope, architecture, or task sequencing.

## 1. Executive Summary

The repository's verification lane is not trustworthy. A full execution of all 22 test projects at `f49dea080` produced 190+ failures across 6 projects, one project that hangs indefinitely, and one project that was never being executed by routine sweeps because it lives outside `tests/`. Critically, the failures are a **mixture of three different kinds**: cascading fixture collateral that inflates the apparent damage, genuinely outdated tests that assert against a superseded reality, and four real production defects that the lane correctly caught but which nobody acted on.

This workstream restores the lane to a state where a red result means a real defect and a green result means real safety. It fixes the production defects the lane exposed, repairs or repoints the outdated tests without weakening a single safety invariant, eliminates the nondeterminism (an unbounded network wait and a shared-fixture disposal race), and then documents explicit lane ownership so every project has a declared role and a runnable command.

**Outcome:** every one of the 22 projects executes deterministically, with a documented role and nonzero test execution, and no failure remains unexplained.

**Non-goals:** changing product features; adding new capabilities; introducing E2E/browser/Playwright lanes; deleting coverage to reach green; preserving any backward-compatibility shim.

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context (Blast Radius)

```yaml
# Injected Structural Context (Pre-Flight Blast Radius)
Target: Repository verification lane (22 executable test projects)
Callers (Upstream):
  - .github/workflows/_build-test.yml (CI lane composition)
  - .github/workflows/test.yml
  - AGENTS.md §8 Verification Baseline (agent contract)
Callees (Downstream):
  - Explore.Domain.InstanceBootstrapState.RequireUuidV7 (identifier invariant)
  - Explore.API.Controllers.NotificationController.GetVapidPublicKey (public read)
  - Event.Web.BffHosting.EventBffAuthenticationExtensions (BFF auth composition)
  - Explore.Persistence.Repositories.ConfigurationDirectTransferChunkRepository (DI graph)
  - ISLAMU.Wire.Contracts.ConfigurationPortability.* (wire contract)
Impacted Flows:
  - Flow: InstanceBootstrapAndSetupSecret (Criticality: Tier 1 Security)
  - Flow: SelfHostedBffAvailability (Criticality: Tier 1 Security)
  - Flow: AuthorizationSurfaceClassification (Criticality: Tier 1 Security)
  - Flow: ConfigurationPortabilityExportImport (Criticality: Tier 3 Domain State)
Test Coverage:
  - tests/ (21 projects)
  - eng/release/tests/ISLAMU.ReleaseEngineering.Tests (1 project, previously unswept)
```

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| The repository has 22 executable test projects, not 21 | `Verified: eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj`; a `tests/**` glob misses it | High | It is listed in `intents.yaml::test-suite-rationalization.minimum_tests` but lives outside `tests/` |
| 14 projects are fully green | Executed at `f49dea080`: Domain.UnitTests 1113, Blazor.Client.Tests 2622, Setup.Core 72, SetupAssistant ×5 = 97, Standalone.Integration 49, Wire.Contracts 35, Diagnostic 21, GeneratedContracts 8, Mutation lanes 19 | High | 7,585 passing assertions |
| 72 API failures are cascade collateral, not defects | `Verified by execution:` `--treenode-filter "/*/*/TagControllerTests/*"` passes 9/9 in isolation; whole-assembly run yields `ObjectDisposedException: TestServer` in exactly 5 classes | High | Decisive isolation proof |
| 26 API failures are an outdated fixture helper | `Verified: tests/Event.API.IntegrationTests/Features/SetupSecretAuthorizationMatrixTests.cs:166` seeds `Guid.NewGuid()`; `Verified: src/Explore.Domain/InstanceBootstrapState.cs:226` `RequireUuidV7` rejects it | High | Production guard is correct; fixture is stale |
| 10 Application failures are a mislocated assembly lookup | `Verified: tests/Event.Application.UnitTests/Features/ConfigurationManifest/ConfigurationManifestContractTests.cs:17` uses `typeof(SettingUpsertService).Assembly`; `Verified: src/Event.Wire.Contracts/ConfigurationPortability/ConfigurationManifestV1Alpha2.cs:10` defines the types | High | Types are live, consumed by controllers |
| `ClaimConfiguredInstanceAdministratorCommand` is unclassified | `Verified by execution:` `AuthorizationSurfaceGuardrailTests` at `tests/Event.Architecture.Tests/AuthorizationSurfaceGuardrailTests.cs:333` | High | Tier 1 security gap |
| Intent `agent-workflow-guard` references a missing plan file | `Verified: .agents/contract/intents.yaml:1077,1097,1117` reference `dev/active/agentic-workflow-control-plane/...-plan.md`; `Not found: dev/active/agentic-workflow-control-plane/` | High | Stale registry pointer |
| Deferred startup DI graph is incomplete | `Verified by execution:` `Explore.Infrastructure.Tests::DeferredStartupGraph_ResolvesWithoutRuntimeEffectServices` — unresolvable `IDataProtectionProvider`, `IConfigurationImportSessionRepository`, `ILegalDocumentRepository`, `IHierarchicalSettingsResolver` | High | Real startup-validation defect |
| Public Web Push reads return 500 when unconfigured | `Verified by execution:` `/api/notification/web-push/config` and `/vapid-public-key` → 500; `Verified: src/Explore.API/Controllers/NotificationController.cs:197-207` | High | Operator-facing opacity |
| Persistence failures are real, not collateral | `Verified by execution:` `EventSessionLifecycleConstraintTests` fails 7/8 **in isolation**; 37 × `23503 ... violates foreign key constraint "fk_events_event_types_event_type_id"` | High | Lookup seed gap in fixture |
| Secrets lane hangs | `Verified by execution:` one output line in 30 min; `--list-tests` completes in 547 ms; `Verified: tests/Explore.Secrets.UnitTests/Bootstrap/BootstrapSecretLoaderTests.cs:99-106` points Infisical at a bogus URL | High | Unbounded egress |
| Release fragment policy test uses a stale path | `Verified by execution:` `DirectoryNotFoundException: .../docs/releases/changes` at `eng/release/tests/.../ReleaseInputPolicyTests.cs:44`; `Verified: docs/internal/releases/changes` exists | High | Docs moved under `docs/internal/` |
| Container lanes require explicit Podman wiring | `Verified: .agents/rules/tests.md:43` | High | Without it the suite reports a false mass regression |

### 2.2 Existing Implementation

- **Domain (`src/Explore.Domain`)** — enforces identifier invariants; `InstanceBootstrapState.RequireUuidV7` rejects non-UUIDv7 identifiers for `completedByUserId`. Behavior is correct and must not be relaxed.
- **Application (`src/Explore.Application`)** — hosts ConfigurationManifest handlers and validation that consume wire contracts from a separate assembly. Registers ConfigurationManifest services whose dependencies are not present in every composition path.
- **Wire contracts (`src/Event.Wire.Contracts`)** — owns `ISLAMU.Wire.Contracts.ConfigurationPortability`, including `ConfigurationManifestContractMetadata` and `ConfigurationManifestV1Alpha2`.
- **API (`src/Explore.API`)** — `NotificationController` exposes two anonymous public reads that dispatch `GetWebPushPublicConfigurationQuery` and return its result without a configured/unconfigured distinction.
- **BFF hosting (`src/Event.Web.BffHosting`)** — composes cookie + OIDC authentication and a named `ControlPlaneAccess` policy requiring an authenticated user; serves the Blazor shell.

### 2.3 Existing Tests And Verification Coverage

Baseline executed at `f49dea080` with `TMPDIR` off the full tmpfs and Podman wired per `.agents/rules/tests.md:43`:

| Project | Result | Disposition |
|---|---|---|
| Event.Domain.UnitTests | 1113 pass | Healthy |
| Explore.Blazor.Client.Tests | 2622 pass (1 skipped) | Healthy |
| Explore.Infrastructure.Tests | 1664 pass / **2 fail** | Real DI defect |
| Event.Application.UnitTests | 2010 pass / **10 fail** | Outdated lookup |
| Event.Architecture.Tests | 573 pass / **3 fail** | 1 real gap, 1 stale pointer |
| Event.API.IntegrationTests | 2361 pass / **62 fail** | 72 cascade + 26 stale fixture + real assertions |
| Explore.Blazor.IntegrationTests | 553 pass / **7 fail** | Real self-hosting regression |
| Event.Persistence.IntegrationTests | **≥108 fail**, run truncated | Real fixture seed gap |
| ISLAMU.ReleaseEngineering.Tests | 243 pass / **1 fail** | Stale path constant |
| Explore.Secrets.UnitTests | **hangs, 0 executed** | Unbounded egress |
| Setup/Standalone/Wire/Diagnostic/GeneratedContracts/Mutation (10 projects) | pass | Healthy |

**Explicit gaps:** no lane ownership documentation maps a project to its role, its required infrastructure, or its runnable command; `Explore.Secrets.UnitTests` has never been proven to execute nonzero tests in this environment; `ISLAMU.ReleaseEngineering.Tests` is absent from routine sweeps because of its location.

### 2.4 Existing Documentation And Contracts

- `.agents/contract/intents.yaml` — `test-suite-rationalization` (scope, forbidden paths, minimum tests, acceptance, PR checklist); `agent-workflow-guard` (holds the stale triad reference).
- `.agents/rules/tests.md` — container runtime contract; twin at `.omo/rules/tests.md`.
- `docs/internal/TESTING.md`, `docs/internal/OPERATIONS.md`, `docs/internal/GOVERNANCE.md`, `docs/internal/QUICK_REFERENCE.md`, `docs/internal/AGENTIC_CONTEXT_ENGINEERING.md` — intent-mandated documentation targets.
- `docs/internal/releases/changes/` — change-fragment directory; the release policy test still points at the pre-reorganization `docs/releases/changes`.
- `eng/release/policy/scope-registry.yaml` — canonical commit scopes used by every phase commit contract below.

### 2.5 Current Pain Points / Improvement Areas

1. **Dishonest signal (highest severity).** 72 of 62 reported API failures are one fixture's disposal cascade. A maintainer reading the board sees catastrophe and learns to ignore the lane. `IVSD-F007`.
2. **A lane that cannot finish.** `Explore.Secrets.UnitTests` performs unbounded outbound network calls and never completes, so its invariants are effectively unverified. `IVSD-F005`.
3. **Real production defects sitting unactioned.** Startup DI validation failure, public 500s, unclassified mutating command, and a self-hosting shell regression were all correctly caught and all left red. `IVSD-F002`, `IVSD-F003`, `IVSD-F004`.
4. **Stale assertions against a moved reality.** Docs relocated to `docs/internal/`, wire contracts moved to their own assembly, and the domain tightened to UUIDv7 — three test cohorts still assert the old world.
5. **An unswept project.** `ISLAMU.ReleaseEngineering.Tests` is required by the intent but invisible to a `tests/**` sweep.
6. **Environment fragility.** Without documented `TMPDIR` and Podman wiring, the suite reports a fabricated mass regression, which trains contributors to distrust real failures.

### 2.6 Unknowns After Investigation (Strict Deferrable Open Questions Rule)

All remaining unknowns are genuinely deferrable and none alters scope, architecture, or task sequencing.

| Unknown | What was searched | Resolving task |
|---|---|---|
| Exact total Persistence failure counts | Both lanes executed; Persistence lane failed twice past 30-minute deadline with no summary emitted; full baseline recorded in context as lower bound (≥108) | Task 6.1 produces completed counts via sharded, class-filtered baseline execution rather than monolithic run |
| Resolution mechanism of the BFF shell 302 redirect to `/setup` | Code analysis of `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs:288-295` confirmed `HandleStartupRedirectAsync` redirects `/` to `/setup` under `InteractivePending` disposition | Task 9.1 verifies whether `NoKeycloakBlazorBffWebApplicationFactory` mock or endpoint routing precedence governs `/` accessibility |
| Whether CI currently skips the Secrets lane | `.github/workflows/_build-test.yml` referenced but lane composition not enumerated | Task 10.1 reconciles lane ownership against CI |

## 3. Proposed Future State: Behavioral Contract & Scenarios

Behavioral requirements apply to Phases 5, 7, 8, and 9. Phases 1–4, 6, and 10 are `Non-Behavioral Delta`; their invariants are stated at the end of this section.

### Requirement: Self-Hosted Shell Availability Without An Identity Provider
The system SHALL serve its browser application shell successfully when no identity provider is configured, and SHALL keep authenticated areas fail-closed.

#### Scenario: Shell served without identity configuration (Happy Path)
- **GIVEN** an instance started with no identity-provider authority, realm, or client credentials configured
- **WHEN** an anonymous browser requests the application shell root
- **THEN** the response status is 200 and the shell document is returned with its security headers intact

#### Scenario: Protected area remains fail-closed (Adversarial)
- **GIVEN** the same instance with no identity provider configured
- **WHEN** an anonymous caller requests a control-plane protected resource
- **THEN** the request is refused without a session being created, and no anonymous principal is granted administrative authority

### Requirement: Legible Degradation Of Unconfigured Optional Capabilities
The system SHALL respond to public reads for an optional capability that is not configured with a successful, explicitly disabled representation, and MUST NOT return a server-error status for the absence of optional configuration.

#### Scenario: Web Push configuration absent (Boundary)
- **GIVEN** an instance with no Web Push signing configuration present
- **WHEN** an anonymous caller requests the public Web Push configuration
- **THEN** the response is successful and states that the capability is disabled, exposing no key material

#### Scenario: Capability configured (Happy Path)
- **GIVEN** an instance with valid Web Push signing configuration
- **WHEN** an anonymous caller requests the public Web Push configuration
- **THEN** the response is successful, states the capability is enabled, and exposes only browser-safe public material

### Requirement: Complete Authorization Classification Of Mutating Requests
Every mutating request in the application SHALL carry an explicit authorization classification, and an unclassified mutating request MUST fail the architecture guardrail rather than be exempted by an allowlist.

#### Scenario: Instance-administrator claim is classified (Happy Path)
- **GIVEN** the compiled application surface
- **WHEN** the authorization surface inventory is derived
- **THEN** no mutating request is reported unclassified

#### Scenario: Unauthorized actor attempts to claim instance administration (Adversarial)
- **GIVEN** an instance whose administrator has already been claimed
- **WHEN** an unauthenticated or non-entitled actor attempts to claim instance administration
- **THEN** the attempt is refused fail-closed and no administrative authority is transferred

### Requirement: Startup Composition Completeness
The system SHALL resolve every registered scoped service in its deferred startup composition, and a composition missing a dependency MUST fail validation at startup rather than at first request.

#### Scenario: Deferred startup graph validates (Happy Path)
- **GIVEN** the deferred startup service composition
- **WHEN** the container validates every registered descriptor
- **THEN** validation succeeds with no unresolvable dependency

#### Scenario: Missing dependency is caught (Adversarial)
- **GIVEN** a composition from which a required dependency has been removed
- **WHEN** the container validates the composition
- **THEN** validation fails immediately and names the unresolvable service

### Non-Behavioral Invariants (Phases 1–4, 6, 10)

These phases MUST hold the following strictly invariant:
- No production behavior, API contract, HAL affordance, persisted schema, or authorization outcome changes.
- No test cohort covering security, tenant isolation, privacy, money, concurrency, or state machines is deleted or weakened without a passing stronger replacement (`IVSD-M001`).
- Total passing assertions in the 14 currently-green projects does not decrease.
- Every one of the 22 projects executes a nonzero test count in Release configuration.

## 4. Non-Negotiable Constraints

- **Fail-closed coverage.** `intents.yaml::test-suite-rationalization.forbidden_without_approval` prohibits deleting or weakening critical coverage without a passing stronger replacement. Deletion justified only by failure is forbidden.
- **Forbidden paths for the primary intent.** `src/**`, `schemas/openapi_islamu-event.json`, `src/**/Migrations/**`, `src/**/*ModelSnapshot.cs`, `src/**/*.Designer.cs`. Phases that must touch `src/**` (5, 7, 8, 9) run under `bff-auth-bug` or the labeled fallback contract, never under `test-suite-rationalization`.
- **No generated-artifact hand edits.** Migrations, model snapshots, OpenAPI artifacts, and generated clients are never hand-edited (`AGENTS.md` §5 rule 7).
- **No hidden infrastructure in fast lanes.** A fast lane must not acquire a Docker, browser, Aspire, broker, or live-provider prerequisite.
- **No test may reach an external network.** Enforced by `IVSD-M005`.
- **Greenfield.** No compatibility shim, deprecated alias, or legacy adapter. Break and replace.
- **Secrets isolation.** No credential, token, or connection string is introduced into fixtures (`AGENTS.md` §5 rule 10).
- **No ad-hoc Python/Node scripts** (`AGENTS.md` §5 rule 9).
- **Verification discipline.** One Release build plus at most one project test per phase. No E2E, Playwright, browser, Aspire, Docker-startup, or manual runtime verification is planned.

## 5. Architecture And Design Decisions

### D1 — Repoint the ConfigurationManifest contract tests rather than delete them
- **Decision:** Resolve contract types from the assembly that actually declares them (`Event.Wire.Contracts`) instead of `Explore.Application`.
- **Why:** The types exist and are consumed by live controllers, handlers, validators, and OpenAPI transformers. The tests encode a real wire-contract invariant; only their assembly anchor is wrong.
- **Alternatives considered:** Delete the cohort (rejected — destroys a live wire-contract guarantee and violates `IVSD-M001`); move the tests into `Event.Wire.Contracts.UnitTests` (rejected for now — the assertions also cover Application-side serialization behavior; revisit only if the cohort proves misplaced).
- **Consequences:** The cohort becomes a durable guard on the v1alpha2 contract identity and closed scopes.
- **Files/layers affected:** `tests/Event.Application.UnitTests/Features/ConfigurationManifest/` (test layer only).

### D2 — Fix fixture lifetime rather than serialize the API assembly
- **Decision:** Give the five affected API controller test classes a fixture whose lifetime they own, instead of marking the assembly non-parallel.
- **Why:** Serializing the whole assembly would hide the defect and make the slowest lane slower. Isolation proof (`TagControllerTests` 9/9) shows the tests themselves are sound.
- **Alternatives considered:** `[NotInParallel]` on the whole assembly (rejected — masks the bug, costs runtime); a shared static singleton factory never disposed (rejected — leaks a server across the run).
- **Consequences:** Cascade disappears; a future disposal bug fails locally in one class instead of poisoning 72 tests.
- **Files/layers affected:** `tests/Event.API.IntegrationTests/` fixtures and the five affected classes.

### D3 — Align fixtures to the domain invariant, never relax the guard
- **Decision:** Fixtures generate UUIDv7 identifiers.
- **Why:** `AGENTS.md` §5 rule 3 makes UUIDv7 the aggregate identifier standard; the production guard is the correct behavior and the fixture is the deviation.
- **Alternatives considered:** Relax `RequireUuidV7` for test paths (rejected — introduces a test-only production branch and destroys the invariant).
- **Consequences:** 26 API failures resolve; the identifier invariant gains real coverage.
- **Files/layers affected:** `tests/Event.API.IntegrationTests/` fixture helpers.

### D4 — Restore the anonymous shell contract in production, keep the test assertion intact
- **Decision:** Make the BFF serve its shell anonymously when no identity provider is configured; do not weaken `StaticPages_AreAccessible`.
- **Why:** `.agents/rules/auth-trust-boundaries.md` rule 6 (Air-Gapped Fallback) and the platform's self-hosting promise. A shell that 302s without an IdP makes an air-gapped deployment unusable.
- **Alternatives considered:** Accept the redirect and relax the test (rejected — encodes a self-hosting regression as intent, contradicts `IVSD-M002`); configure a dummy IdP in the fixture (rejected — hides the operator-facing defect).
- **Consequences:** Self-hosting availability restored; protected areas remain fail-closed via the named policy.
- **Files/layers affected:** `src/Event.Web.BffHosting/`, `tests/Explore.Blazor.IntegrationTests/`.

### D5 — Model unconfigured optional capability as a successful disabled state
- **Decision:** Public Web Push reads return a successful disabled representation when signing material is absent.
- **Why:** A 500 for absent optional configuration is an operator-hostile failure mode and pollutes the public-endpoint authorization matrix. `IVSD-M003`.
- **Alternatives considered:** 404 (rejected — implies the route does not exist, breaking client discovery); 503 (rejected — implies transient outage, inviting retry storms).
- **Consequences:** Clients can branch on an explicit disabled flag; the public GET matrix becomes meaningful again.
- **Files/layers affected:** `src/Explore.Application` query handler, `src/Explore.API/Controllers/NotificationController.cs`.

### D6 — Classify the mutating command, never allowlist it
- **Decision:** Give `ClaimConfiguredInstanceAdministratorCommand` a real authorization classification.
- **Why:** It transfers instance-administrator authority — Tier 1. `intents.yaml` forbids resolving guardrail failures by allowlist, and `IVSD-M004` requires fail-closed classification.
- **Alternatives considered:** Add to the Phase 0 inventory exemption (rejected — converts a security gap into permanent debt).
- **Consequences:** The authorization surface becomes complete; an adversarial claim attempt gains an invariant test.
- **Files/layers affected:** `src/Explore.Application/Features/InstanceOnboarding/`, `tests/Event.Architecture.Tests/`.

### D7 — Remove network egress from the secrets unit lane
- **Decision:** Bound the transport or substitute a fake so no unit test performs outbound network I/O.
- **Why:** A unit lane that depends on network timeouts is nondeterministic by construction and currently hangs forever. This also violates the repository's test-determinism expectation.
- **Alternatives considered:** Add a long timeout (rejected — still nondeterministic, still slow, still egress); mark the lane skipped (rejected — converts absent evidence into apparent safety).
- **Consequences:** The lane completes in seconds and its fail-closed assertions become real evidence.
- **Files/layers affected:** `tests/Explore.Secrets.UnitTests/`.

### D8 — Seed lookup data in the persistence fixture rather than relax the constraint
- **Decision:** The persistence fixture provisions seeded lookup rows (notably event types) so aggregates can satisfy their foreign keys.
- **Why:** 37 failures are `fk_events_event_types_event_type_id` violations. The constraint is correct; the fixture builds a schema without its seed data.
- **Alternatives considered:** Drop the FK in test schemas (rejected — removes the exact integrity guarantee under test); per-test ad-hoc inserts (rejected — duplicated setup across dozens of classes).
- **Consequences:** A large block of persistence failures resolves at the fixture seam rather than test-by-test.
- **Files/layers affected:** `tests/Event.Persistence.IntegrationTests/` fixtures.

No decision in this workstream constitutes a major technology selection or competing architectural pattern, so no `robin-neutral` steelmanning pass was required.

## 6. Implementation Phases

### Phase 1: Deterministic Secrets Lane
- **Goal:** `Explore.Secrets.UnitTests` completes deterministically with nonzero executed tests and zero network egress.
- **Depends on:** none
- **Relevant files:** `tests/Explore.Secrets.UnitTests/Bootstrap/BootstrapSecretLoaderTests.cs` (existing), secrets test support types (existing)
- **Phase-owned paths:** `tests/Explore.Secrets.UnitTests/**`
- **Related skills/rules:** `refactor-safely`, `.agents/rules/tests.md`
- **Change Classification:** Non-Behavioral Delta
- **Acceptance criteria:**
  - The lane terminates well within its runner budget and reports a nonzero executed count.
  - No test performs outbound network I/O.
  - The fail-closed assertion that Infisical selection never silently falls back is preserved, not weakened.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Make the secrets verification lane finish deterministically without external network calls.
- **Rollback / failure handling:** Revert the phase commit; the lane returns to its previously hanging state with no product impact.

### Phase 2: API Integration Fixture Isolation And Identifier Alignment
- **Goal:** Eliminate the shared-fixture disposal cascade and align seed identifiers with the UUIDv7 domain invariant.
- **Depends on:** none
- **Relevant files:** `tests/Event.API.IntegrationTests/Features/SetupSecretAuthorizationMatrixTests.cs` (existing), the five cascade-affected controller test classes (existing), shared factory/fixture types (existing)
- **Phase-owned paths:** `tests/Event.API.IntegrationTests/**`
- **Related skills/rules:** `refactor-safely`, `criticality-guardrail`, `.agents/rules/tests.md`
- **Change Classification:** Non-Behavioral Delta
- **Acceptance criteria:**
  - No `ObjectDisposedException: TestServer` failure remains in a whole-assembly run.
  - No fixture seeds a non-UUIDv7 aggregate identifier.
  - Every previously cascading class still passes when run in isolation, and the whole-assembly count equals the sum of isolated runs.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Stop one disposed server fixture from masking dozens of unrelated API test results.
- **Rollback / failure handling:** Revert the phase commit; residual API failures remain attributable to later phases and are recorded in context.

### Phase 3: Configuration Manifest Contract Test Rebinding
- **Goal:** The wire-contract cohort asserts against the assembly that declares the contract.
- **Depends on:** none
- **Relevant files:** `tests/Event.Application.UnitTests/Features/ConfigurationManifest/ConfigurationManifestContractTests.cs` (existing)
- **Phase-owned paths:** `tests/Event.Application.UnitTests/Features/ConfigurationManifest/**`
- **Related skills/rules:** `refactor-safely`
- **Change Classification:** Non-Behavioral Delta
- **Acceptance criteria:**
  - All 10 previously failing assertions pass against the real contract types.
  - The cohort still enforces contract identity, closed required scopes, strict unknown-member rejection, and ordering.
  - No assertion is deleted or weakened to achieve the pass.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Restore the configuration-manifest wire-contract guard against the assembly that owns it.
- **Rollback / failure handling:** Revert the phase commit; the cohort returns to failing without affecting product code.

### Phase 4: Agent Registry And Release Policy Path Truth
- **Goal:** The intent registry and release-policy test reference paths that exist.
- **Depends on:** none
- **Relevant files:** `.agents/contract/intents.yaml` (existing), `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ReleaseInputPolicyTests.cs` (existing)
- **Phase-owned paths:** `.agents/contract/intents.yaml`, `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/**`
- **Related skills/rules:** `.agents/rules/tests.md`
- **Change Classification:** Non-Behavioral Delta
- **Acceptance criteria:**
  - The `agent-workflow-guard` intent no longer references a nonexistent workstream triad.
  - The release-input policy test discovers change fragments at their real location under `docs/internal/`.
  - Every committed change fragment passes policy validation.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Point the intent registry and release policy checks at paths that actually exist.
- **Rollback / failure handling:** Revert the phase commit; both checks return to failing with no runtime impact.

### Phase 5: Authorization Surface Classification Completeness
- **Goal:** Every mutating request carries an explicit authorization classification, with the instance-administrator claim anchored fail-closed.
- **Depends on:** none
- **Relevant files:** `src/Explore.Application/Features/InstanceOnboarding/Requests/Commands/ClaimConfiguredInstanceAdministratorCommand.cs` (existing), its handler (existing), `tests/Event.Architecture.Tests/AuthorizationSurfaceGuardrailTests.cs` (existing), new invariant test (new)
- **Phase-owned paths:** `src/Explore.Application/Features/InstanceOnboarding/**`, `tests/Event.Architecture.Tests/**`
- **Related skills/rules:** `auth-patterns`, `criticality-guardrail`, `.agents/rules/auth-trust-boundaries.md`
- **Change Classification:** Behavioral Delta — implements Section 3 *Complete Authorization Classification Of Mutating Requests*
- **Acceptance criteria:**
  - The authorization surface inventory reports zero unclassified mutating requests.
  - An unauthorized actor attempting to claim instance administration is refused fail-closed with no authority transfer.
  - The resolution is a real classification, not an inventory exemption.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Close the unclassified instance-administrator claim gap in the authorization surface.
- **Rollback / failure handling:** Revert the phase commit. Because this is a Tier 1 security slice, a failed rollback requires re-running the guardrail before any further phase proceeds.

### Phase 6: Persistence Fixture Seed And Constraint Integrity
- **Goal:** Persistence integration tests satisfy real referential constraints through seeded lookup data and stable fixture lifetimes, measured via sharded class-filtered passes to prevent 30-minute runner timeouts.
- **Depends on:** none
- **Relevant files:** `tests/Event.Persistence.IntegrationTests/` fixtures (existing), `tests/Event.Persistence.IntegrationTests/Repositories/EventSessionLifecycleConstraintTests.cs` (existing), `tests/Event.Persistence.IntegrationTests/Database/ExploreDbContextModelProviderTests.cs` (existing)
- **Phase-owned paths:** `tests/Event.Persistence.IntegrationTests/**`
- **Related skills/rules:** `dotnet-efcore-guidelines`, `refactor-safely`, `.agents/rules/tests.md`
- **Change Classification:** Non-Behavioral Delta
- **Acceptance criteria:**
  - Task 6.1 executes baseline measurement via sharded class-filtered runs per root-cause group (FK seed errors, fixture-lifetime/`ManyServiceProvidersCreatedWarning` leaks, provider-specific migration tests) rather than stalling on a monolithic 30-minute unfinishable run.
  - No `fk_events_event_types_event_type_id` violation remains.
  - No foreign key, check constraint, or exclusion constraint is dropped or relaxed in a test schema.
  - Fixture lifetime no longer creates excessive internal service providers or leaks connections across classes.
  - Every remaining failure in the lane is individually dispositioned in `tasks.md` as fixed, replaced, or explicitly deferred with a reason.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Give persistence integration fixtures the seeded lookup data and stable lifetime their real constraints require.
- **Rollback / failure handling:** Revert the phase commit; the lane returns to its recorded baseline failure count.

### Phase 7: Startup Composition Completeness
- **Goal:** The deferred startup composition resolves every registered descriptor without runtime missing-dependency errors.
- **Depends on:** none
- **Relevant files:**
  - `src/Explore.Persistence/ConfigurationManifestPersistenceServicesRegistration.cs` (existing)
  - `src/Explore.Infrastructure/ConfigurationManifest/ConfigurationManifestStartupServicesRegistration.cs` (existing)
  - `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` (existing)
  - `tests/Explore.Infrastructure.Tests/Infrastructure/ConfigurationManifest/ConfigurationManifestStartupCompositionTests.cs` (existing)
- **Phase-owned paths:** `src/Explore.Persistence/ConfigurationManifestPersistenceServicesRegistration.cs`, `src/Explore.Infrastructure/ConfigurationManifest/ConfigurationManifestStartupServicesRegistration.cs`, `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `tests/Explore.Infrastructure.Tests/**`
- **Related skills/rules:** `clean-architecture-rules`, `refactor-safely`
- **Change Classification:** Behavioral Delta — implements Section 3 *Startup Composition Completeness*
- **Acceptance criteria:**
  - Container validation of the deferred startup graph succeeds with no unresolvable dependency (`IDataProtectionProvider`, `IConfigurationImportSessionRepository`, `ILegalDocumentRepository`, `IHierarchicalSettingsResolver` resolved cleanly).
  - A deliberately removed dependency still fails validation, proving the guard is live.
  - Layer ownership is respected; no Application type takes an infrastructure dependency to satisfy the graph.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Make the deferred startup composition resolve every registered configuration-portability service.
- **Rollback / failure handling:** Revert the phase commit; startup validation returns to failing, which is detectable before first request.

### Phase 8: Legible Degradation For Unconfigured Public Capabilities
- **Goal:** Public Web Push reads degrade to an explicit disabled state instead of a server error.
- **Depends on:** Phase 2 (a clean API lane is required to read this phase's result honestly)
- **Relevant files:**
  - `src/Explore.Application/Features/Notifications/Handlers/Queries/GetWebPushPublicConfigurationQueryHandler.cs` (existing)
  - `src/Explore.Application/Features/Notifications/Requests/Queries/GetWebPushPublicConfigurationQuery.cs` (existing)
  - `src/Explore.API/Controllers/NotificationController.cs` (existing)
  - `tests/Event.API.IntegrationTests/Features/EndpointAuthorizationMatrixTests.cs` (existing)
- **Phase-owned paths:** `src/Explore.Application/Features/Notifications/**`, `src/Explore.API/Controllers/NotificationController.cs`, `tests/Event.API.IntegrationTests/Features/**`
- **Related skills/rules:** `.agents/rules/api-controllers.md`, `auth-patterns`
- **Change Classification:** Behavioral Delta — implements Section 3 *Legible Degradation Of Unconfigured Optional Capabilities*
- **Acceptance criteria:**
  - Both public Web Push reads return success with an explicit disabled state when signing material is absent.
  - No key material is exposed in either configured or unconfigured responses.
  - The public GET matrix reports no 500 for any anonymous read.
  - Greenfield Clean Architecture: response contract is cleanly typed; no obsolete backward-compatibility fields or deprecated aliases retained.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - *Reason for reusing this project:* it is the only lane that exercises the public endpoint matrix over real HTTP, and Phase 2 changed its fixture lifetime, so this phase's assertion cannot be observed anywhere else.
- **Phase-close commit outcome:** Return an explicit disabled state instead of a server error when Web Push is unconfigured.
- **Rollback / failure handling:** Revert the phase commit; endpoints return to 500 for unconfigured instances, which remains detectable by the matrix test.

### Phase 9: Self-Hosted Shell Availability Without An Identity Provider
- **Goal:** The BFF serves its application shell anonymously with no identity provider configured, while protected areas remain fail-closed.
- **Depends on:** none
- **Relevant files:**
  - `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs` (existing, lines 288-295 `HandleStartupRedirectAsync`)
  - `src/Event.Web.BffHosting/Authentication/EventBffAuthenticationExtensions.cs` (existing)
  - `tests/Explore.Blazor.IntegrationTests/Endpoints/BffNoKeycloakResilienceTests.cs` (existing)
- **Phase-owned paths:** `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs`, `src/Event.Web.BffHosting/**`, `tests/Explore.Blazor.IntegrationTests/**`
- **Related skills/rules:** `blazor-bff-patterns`, `auth-patterns`, `.agents/rules/auth-trust-boundaries.md`
- **Change Classification:** Behavioral Delta — implements Section 3 *Self-Hosted Shell Availability Without An Identity Provider*
- **Acceptance criteria:**
  - The shell root returns 200 with no identity provider configured, retaining its security headers.
  - The redirect to `/setup` from `HandleStartupRedirectAsync` correctly evaluates whether onboarding is completed or mocked, allowing static shell accessibility.
  - A control-plane protected resource still refuses an anonymous caller and grants no administrative authority.
  - The remaining Blazor integration failures (ATProto client metadata, JWKS, handoff, token circuit) are each dispositioned as fixed or explicitly deferred with a reason.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Serve the self-hosted application shell when no identity provider is configured.
- **Rollback / failure handling:** Revert the phase commit. Because this is a Tier 1 security slice, verify after rollback that protected areas remain fail-closed before proceeding.

### Phase 10: Lane Ownership, Runbook, And Environment Contract
- **Goal:** Every one of the 22 projects has a documented role, lane, infrastructure requirement, and runnable command, and the environment prerequisites that previously fabricated mass failures are documented.
- **Depends on:** Phases 1–9 (documented lane status must describe the remediated reality)
- **Relevant files:** `docs/internal/TESTING.md` (existing), `docs/internal/OPERATIONS.md` (existing), `docs/internal/QUICK_REFERENCE.md` (existing), `docs/internal/GOVERNANCE.md` (existing), `docs/internal/AGENTIC_CONTEXT_ENGINEERING.md` (existing), `.agents/rules/tests.md` and its twin `.omo/rules/tests.md` (existing)
- **Phase-owned paths:** `docs/internal/TESTING.md`, `docs/internal/OPERATIONS.md`, `docs/internal/QUICK_REFERENCE.md`, `docs/internal/GOVERNANCE.md`, `docs/internal/AGENTIC_CONTEXT_ENGINEERING.md`, `.agents/rules/tests.md`, `.omo/rules/tests.md`
- **Related skills/rules:** `skill-authoring`, `.agents/rules/tests.md`
- **Change Classification:** Non-Behavioral Delta
- **Acceptance criteria:**
  - All 22 projects are enumerated with role, lane (fast vs container-backed), infrastructure prerequisite, and exact command — including the project under `eng/release/tests/`.
  - The container-runtime and temporary-directory prerequisites are documented as a single authoritative contract, with the twin rule files identical.
  - No documented lane requires a browser, Aspire, or live provider to run.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - *Reason for reusing this project:* it is the only lane that executes agent-context and documentation-contract assertions, which are exactly what this phase changes.
- **Phase-close commit outcome:** Give every test project a documented role, lane, and runnable command.
- **Rollback / failure handling:** Revert the phase commit; documentation returns to its prior state with no code impact.

## 7. Testing Strategy

1. **Invariant anchors.** Security and authorization invariants are anchored in `Event.Architecture.Tests` (classification completeness) and `Event.API.IntegrationTests` (fail-closed HTTP behavior). Self-hosting availability is anchored in `Explore.Blazor.IntegrationTests`. Composition validity is anchored in `Explore.Infrastructure.Tests`. Referential integrity is anchored in `Event.Persistence.IntegrationTests` against a real database. Wire-contract identity is anchored in `Event.Application.UnitTests`.
2. **High-leverage adversarial scenarios.** Each Behavioral Delta phase carries an adversarial scenario from Section 3: unauthorized instance-administration claim (Phase 5), removed-dependency composition failure (Phase 7), key-material non-exposure (Phase 8), and anonymous access to a protected control-plane resource (Phase 9). Phases 1–4, 6, and 10 assert structural invariants and add no mock-mirroring, call-count, framework-cancellation, or raw-source-text assertions.
3. **Phase verification lane.** One project per phase: Secrets (1), API Integration (2), Application Unit (3), ReleaseEngineering (4), Architecture (5), Persistence Integration (6), Infrastructure (7), API Integration (8, reason recorded), Blazor Integration (9), Architecture (10, reason recorded). Intent-mandated projects not selected as a phase lane — `Event.Domain.UnitTests`, `Explore.Blazor.Client.Tests`, `Event.Wire.Contracts.UnitTests`, `Explore.GeneratedContracts.Tests`, `Explore.Diagnostic.UnitTests`, `Event.Standalone.IntegrationTests` — are recorded as contract requirements verified by their existing green baseline; they are not given artificial phases. No E2E, Playwright, browser, visual-QA, Aspire, or live-service verification is planned.

## 8. Documentation, Configuration, And Operations Impact

- `docs/internal/TESTING.md` — lane inventory, roles, prerequisites, and commands (Phase 10).
- `docs/internal/OPERATIONS.md` — container runtime and temporary-directory prerequisites (Phase 10).
- `docs/internal/QUICK_REFERENCE.md`, `docs/internal/GOVERNANCE.md`, `docs/internal/AGENTIC_CONTEXT_ENGINEERING.md` — intent-mandated updates reflecting the rationalized lane contract (Phase 10).
- `.agents/rules/tests.md` and `.omo/rules/tests.md` — twin update; both must remain identical (Phase 10).
- `.agents/contract/intents.yaml` — stale workstream reference removal (Phase 4).
- No schema, OpenAPI artifact, generated client, migration, Aspire manifest, or deployment file changes. Phase 8 changes a response body shape for an unconfigured optional capability; because the repository is pre-release with zero external adopters, this is a clean break with no compatibility shim.

### 8.1 Release, Changelog, And Phase Commit Strategy (Procedural Contribution)

- **Tier 3 — Explicit skip** applies to Phases 1, 2, 3, 4, 6, 7, and 10: internal test architecture, fixture, registry, composition, and documentation work with no public capability change. Each carries `Changelog: skip` plus a non-empty `Changelog-Reason`.
- **Tier 1 — Standard fix** applies to Phases 8 and 9: operator-visible behavior improvements published in release notes under the `notifications` and `self-hosting` capability scopes.
- **Tier 2 — Change fragment** applies to **Phase 5** only. Closing an authorization-classification gap on instance-administrator authority is governed security work, so that phase creates an append-only fragment under `docs/internal/releases/changes/` and its commit carries the matching `Change-Id` footer.

Exact per-phase titles, descriptions, trailers, commit paths, inspection commands, staging commands, path-limited commit commands, and post-commit verification commands are pre-authored in `test-suite-health-remediation-tasks.md`.

## 9. Islamic Value-Sensitive Design (I-VSD) & Moral Boundaries

- **Report:** [islamic-value-sensitive-design/i-vsd-test-suite-health-remediation.md](../../../islamic-value-sensitive-design/i-vsd-test-suite-health-remediation.md)
- **Reviewed input revision:** `f49dea080`
- **Status / disposition:** `draft` / `ready-for-planning`

| I-VSD ID | Finding / mitigation status | Scenario and task mapping | Disposition |
|---|---|---|---|
| `IVSD-F001` / `IVSD-M001` | Open | Section 3 Non-Behavioral Invariants; enforced as an acceptance criterion in every phase and as the disposition rule in Tasks 6.1 and 9.4 | Implement |
| `IVSD-F002` / `IVSD-M002` | Open | Scenario *Shell served without identity configuration* + *Protected area remains fail-closed*; Tasks 9.1–9.3 | Implement |
| `IVSD-F003` / `IVSD-M003` | Open | Scenario *Web Push configuration absent* + *Capability configured*; Tasks 8.1–8.2 | Implement |
| `IVSD-F004` / `IVSD-M004` | Open | Scenario *Instance-administrator claim is classified* + *Unauthorized actor attempts to claim instance administration*; Tasks 5.1–5.3 | Implement |
| `IVSD-F005` / `IVSD-M005` | Open | Non-Behavioral invariant "no test performs outbound network I/O"; Tasks 1.1–1.2 | Implement |
| `IVSD-F006` / `IVSD-M006` | Open | Non-Behavioral invariant on identifier alignment; Task 2.2 | Implement |
| `IVSD-F007` / `IVSD-M007` | Open | Non-Behavioral invariant on honest signal; Task 2.1 and the disposition ledger in `tasks.md` | Implement |

No `IVSD-*` ID is marked non-applicable, and no escalation gate is currently triggered. An escalation to the Project Steward becomes mandatory if any task would delete a security, tenant-isolation, or privacy cohort without a passing stronger replacement.

## 10. Security, Authorization, Privacy, And Abuse Considerations

- **Trust boundaries:** Phase 9 touches the BFF browser boundary and Phase 5 touches instance-administrator authority — both Tier 1. Anonymous shell availability must not create an anonymous authenticated principal or a session.
- **Server-side authorization:** Phase 5 classifies a mutating command; authority remains enforced server-side, never by HAL affordance or client role inspection.
- **Tenant isolation:** unchanged. No query filter, tenant resolution path, or isolation guard is modified.
- **Privacy:** no PII enters logs or fixtures. Phase 8 must expose only browser-safe public material and never private signing keys.
- **Secrets:** Phase 1 must remove network egress without introducing any literal credential into a fixture.
- **Abuse:** unchanged. Rate limiting and idempotency are untouched.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Rationale |
|---|---|---|
| Multi-tenancy | Not Applicable | No tenant resolution, query filter, or isolation boundary changes. |
| Federation | Not Applicable | No federation contract or protocol behavior changes. |
| Localization | Not Applicable | No user-facing copy is added or changed. |
| Accessibility | Not Applicable | No UI markup or component behavior changes; Phase 9 restores availability of an existing shell without altering it. |
| Product | Applicable | Phases 8 and 9 restore intended operator-facing availability and legibility for self-hosted deployments. |

## 12. Observability And Operations

- Phase 7 moves a composition failure to startup, where it is loudest and cheapest to diagnose.
- Phase 8 converts an opaque 500 into an explicit disabled state, removing a misleading error-rate signal for operators who simply have not configured Web Push.
- Phase 10 documents the environment prerequisites whose absence previously produced a fabricated mass regression — the single highest-value operational improvement in this workstream.
- No new metric, trace, log sink, or health endpoint is introduced.

## 13. Migration And Compatibility Plan

- No database migration, schema change, model snapshot, or seed change to production data.
- Phase 6 changes only test-fixture seed provisioning.
- Phase 8 changes a public response body for an unconfigured optional capability. Per `AGENTS.md` §5 rule 11 the repository is pre-release with zero external adopters, so this is a clean break: **no compatibility shim, no aliased response shape, no deprecation window.**
- Deployment order is irrelevant; no phase requires coordinated rollout.
- Rollback is per-phase commit revert, described in each phase above.

## 14. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|
| Persistence lane hides further distinct root causes behind the FK cascade | High | High | Task 6.1 records the completed baseline and dispositions every residual failure individually before any fix | Post-fix count far exceeds the 37 FK violations | Task 6.1 |
| Making the BFF shell anonymous accidentally exposes a protected area | Low | Critical | Adversarial scenario and Tier 1 rollback gate in Phase 9; protected-resource assertion is mandatory, not optional | Protected control-plane resource returns 200 anonymously | Task 9.3 |
| The remaining 6 Blazor failures have a different root cause than the shell 302 | Medium | Medium | Phase 9 acceptance requires each to be individually dispositioned rather than assumed | Shell fix lands but Blazor lane stays red | Task 9.4 |
| Authorization classification is "resolved" by an inventory exemption | Medium | High | D6 and `IVSD-M004` forbid it; acceptance criterion states it explicitly | Guardrail passes while the command has no real policy | Task 5.2 |
| API lane remains slow enough to hit runner deadlines | Medium | Medium | Phase 2 removes the cascade, reducing wasted work; lane budget documented in Phase 10 | Lane exceeds its documented budget | Task 10.1 |
| Fixing the fixture disposal introduces per-class server startup cost | Medium | Low | Prefer a shared-but-owned lifetime over per-test construction; measure the lane duration at Phase 2 verification | Lane duration regresses materially | Task 2.1 |

## 15. Success Metrics And Definition Of Done

- All 22 test projects execute a nonzero test count in Release configuration and report `Passed!`, or carry an explicitly dispositioned and user-accepted deferral recorded in `tasks.md`.
- Zero `ObjectDisposedException` fixture-cascade failures remain.
- `Explore.Secrets.UnitTests` completes deterministically with no outbound network I/O.
- The four production defects (authorization classification, startup composition, public capability degradation, self-hosted shell availability) are fixed with adversarial scenario coverage.
- Total passing assertions in the 14 currently-green projects has not decreased.
- Every test project has a documented role, lane, prerequisite, and command.
- Per phase, the automated gate is exactly one Release build plus one selected project test, followed immediately by the phase-owned Conventional Commit. No browser, runtime, manual-QA, or operator-smoke gate is added.

## 16. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. At first implementation start or cold resume, read `test-suite-health-remediation-context.md` and the current task in `test-suite-health-remediation-tasks.md` first, then retrieve only the plan heading needed for the current phase or a changed decision. Never preload all three artifacts.
2. Keep a `path + heading/symbol + revision` ledger. During an uninterrupted session, do not reread unchanged plan/context/tasks; reopen only an invalidated exact section.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `tasks.md` as the hot execution ledger: check a substantial task immediately after its implementation acceptance criteria are met; reconcile smaller tasks no later than phase end.
5. Keep implementation-task, phase-verification, and phase-commit checkboxes separate. A phase is complete only after verification is resolved and its phase-owned commit succeeds.
6. Update the task status summary, completed count, current priority, next recommended slice, discovered tasks, deferred work, and `Last Updated` whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer.
8. Update this plan only when scope, architecture, phase order, acceptance criteria, risks, or validation strategy changes.
9. Record failed validation with known cause and next recovery action. A phase-attributable failure blocks the commit; a proven unrelated shared-tree failure must name external path/evidence and must never be reported as green.
10. Before every phase commit, reconcile the phase-owned path list against the dirty tree and existing index. Never modify, unstage, stage, or commit another contributor's work.
11. Run phase verification only after all phase tasks: one Release build and at most one selected project test. Do not start the application or a browser.
12. Immediately after the verification disposition, use the approved self-sufficient commit contract directly without loading `conventional-commit`.
13. Load `conventional-commit` only for a permitted material divergence, then record the reason and a complete actual contract for every resulting commit.
14. Apply plan/context update triggers when a divergence changes their owned decisions or state.
15. Stage exact phase-owned paths and verify the resulting commit file list before recording its hash.
16. Before pausing, compaction, transfer, or PR creation, reconcile affected tasks, add a dated handoff, and identify unrelated dirty files the next contributor must avoid — currently the untracked GitBook screenshots at repository root and modified files under `islamic-value-sensitive-design/`.
17. Never report completion when repository reality, the commit file list, and the task ledger disagree.

Every implementation summary must teach: what changed and why; architecture/design patterns, libraries, infrastructure, protocols, and project abstractions used; important files, classes, handlers, services, and components with their responsibilities; data/control flow; relevant repository conventions and reliability/security practices; verification performed, remaining work, next work, and dev-doc update status.

## 17. Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```

For completed implementation work, `Docs updated` must confirm `tasks.md` was reconciled, and must report context and plan separately as updated or unchanged because no trigger occurred.

## 18. Potential Risks & Unknowns

The part most likely to fail or expand is **Phase 6 (Persistence)**. Its baseline is a lower bound (≥108 failures) because `Event.Persistence.IntegrationTests` failed to complete twice, on two independent 30-minute attempts, emitting no run summary either time on a 16-core workstation with rootless Podman. This proves that whole-assembly execution exceeds 30 minutes wall-clock and will hit CI deadlines. The distinct-cause histogram (82 `InvalidOperationException`, 74 `DbUpdateException`, 44 `AssertionException`, plus MySQL/SQLite/SQL Server provider exceptions) confirms at least three root causes behind the 37 FK violations identified. In particular, `ManyServiceProvidersCreatedWarning` escalating to a thrown exception indicates resource pressure and internal provider churn from whole-assembly execution, making failures load-dependent. Task 6.1 mitigates this risk directly by producing its baseline via sharded, class-filtered passes per root-cause group rather than attempting an unfinishable monolithic run.

The second area of focus is **Phase 9**. Architectural review confirmed that the BFF shell returns 302 targeting `/setup` generated by `HandleStartupRedirectAsync` in `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs:288-295` when onboarding status evaluates to `InteractivePending`. The test fixture `NoKeycloakBlazorBffWebApplicationFactory` attempts to mock `IBffOnboardingStatusProvider` as `Completed`, but the route still redirected. Phase 9 investigation (Task 9.1) pins down whether DI replacement order or route base resolution causes the redirect, ensuring static shell availability is cleanly restored without weakening control-plane fail-closed boundaries or conflicting with `dev/active/setup-assistant-security-and-portability`.
