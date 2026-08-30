<!-- ABOUTME: Execution ledger for complete instance and tenant configuration portability. -->
<!-- ABOUTME: Preserves completed foundation evidence and sequences import, migration, legal, recovery, and extensibility work. -->

# Configuration Manifest And Reporting-Intake Policy — Task Checklist

Last Updated: 2026-08-31 Europe/Brussels

## Status Summary

- **Overall status:** In implementation; reactivated by explicit user directive
  on 2026-08-30.
- **Implemented tasks:** 46/49 through CM-2320; CM-1930, CM-2030, and CM-2330
  remain open because their required full-project gates are not green.
- **Remaining tasks:** 3/49 verification closures in active Phases 19, 20, and
  23.
- **Current priority:** complete or explicitly waive the API, Persistence, and
  Architecture phase gates; no implementation phase is being skipped.
- **Next recommended slice:** resolve the recorded unrelated gate failures and
  the committed SQLite migration-chain conflict, then rerun the three gates.
- **Known blockers:** the API gate is red on unrelated secret-configuration
  tests; the Persistence gate was stopped after 16 minutes without completion;
  the Architecture gate is red on three unrelated findings. No waiver exists.
- **Superseded workstream:** `dev/active/tenant-configuration-manifest/`.
  Its completed runtime foundation remains in the branch, but its planning
  artifacts are replaced by this workstream.
- **Plan:** [configuration-manifest-plan.md](configuration-manifest-plan.md)
- **Context:** [configuration-manifest-context.md](configuration-manifest-context.md)
- **I-VSD:** [i-vsd-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-configuration-manifest.md)
- **I-VSD reviewed input:** `sha256:b1bb05932eef7c11ec0af43b307d4afdb4eac17ac3b8d563f095cbe16c99f26d`
- **I-VSD status/disposition:** `plan-aligned`; F025–F030 remain deferred to
  Setup Assistant.
- **CTO review:** [configuration-manifest-cto-review.md](configuration-manifest-cto-review.md),
  decision `Approve`
- **Deferred future workstream:** Avalonia, Terminal.Gui, CLI/TUI, `.env`, and
  agentic skill; no task below implements them.

## Reactivation Disposition

- The Release build completed with zero errors during Phase 18 closure.
- CM-1830 focused Application/API/HTTP/HAL/BFF/generated-client/architecture
  gates are Green and its generated artifacts converged twice.
- The complete API project did not pass: unrelated onboarding, payment, setup,
  and public-experience failures reproduced. A fresh rerun was stopped by the
  user and deferred to the final verification sweep.
- Every unchecked task in Phases 19–23 is active again and must be implemented
  in order without claiming completion before its acceptance evidence exists.
- Setup Assistant remains downstream-only: it consumes the frozen v1alpha2
  wire contract, `Event.Wire.Contracts` extraction boundary, no-secret
  portability rules, legal Markdown contract, and Phase 1 dependency seams.
  This workstream makes no Setup Assistant implementation claim.

## Continuation Verification — 2026-08-31

- Release build: passed with 0 errors.
- Application phase gate: 1,993/1,993 passed.
- Blazor phase gate: 2,601 passed, 0 failed, 1 documented skip.
- Persistence focused evidence: atomicity 2/2, recovery 2/2, encrypted
  artifact/provider-model parity 10/10. The full project remained active for
  16 minutes without producing a result and was stopped; this is not a pass or
  waiver.
- API focused configuration-import controller contract: 9/9 passed. The full
  project remains red on unrelated secret-configuration tests; no waiver was
  granted.
- Architecture: 499 passed, 3 unrelated failures, 1 documented skip. Every
  configuration-manifest-specific architecture failure was repaired, but the
  full-project gate is not green.
- Both v1alpha2 schemas passed `--check` twice. Earlier two-pass OpenAPI, API
  inventory, and NSwag digests remained stable.
- Release-fragment policy tests passed 22/22. Range preflight is blocked by an
  unrelated commit-policy violation at `8aea1bf4c133afcf50cb0d9f2126d23c68a48207`.
- The committed SQLite development migration sequence still contains the
  superseded three-migration chain. It cannot be rewritten without an explicit
  exception to the immutable-merged-migration rule; provider model parity alone
  does not prove that chain can migrate an empty SQLite database.

## Implementation Maintenance Rules

1. This file is the sole hot execution ledger.
2. Start from the first unchecked task and retrieve only its referenced plan
   phase and exact evidence paths.
3. Every behavioral slice writes failing public-contract or invariant-breaker
   tests before production code. Record the expected Red failure.
4. Mark a substantial task immediately after its acceptance criteria and
   focused selector pass.
5. Phase completion requires every task plus one Release build and at most the
   one selected project test.
6. Use focused TUnit `--treenode-filter` selectors during active work; do not run
   unrelated projects.
7. Keep Application orchestration out of Domain/Persistence and keep direct
   setting/document/payment table writes out of manifest handlers.
8. Repositories return entities; validators are manually instantiated; HAL is
   the sole UI action gate.
9. Regenerate EF migrations/snapshots, JSON Schema, OpenAPI, API inventory, and
   NSwag from source. Never hand-edit generated output.
10. Delete old tenant-manifest contracts and tests; do not add compatibility
    shims.
11. No fixed sleeps, timing-luck polling, skipped tests, weakened ratchets,
    suppressed diagnostics, browser/live-service verification, or ad-hoc
    Python/JavaScript tooling.
12. Update context on every phase, decision, blocker, failed gate, or handoff.

## Historical Foundation — Implemented, To Be Generalized

The branch already contains strict tenant-manifest ingestion, schema generation,
tenant catalog/validation, atomic tenant bootstrap, audit/outbox, startup
ownership, export/BFF/UI, reporting-intake policy, and payment-narrowing
integration. These are verified current-state foundations, not completion
evidence for the instance-wide contract.

## Phase 9: Breaking Contract And Namespace Rebase

Plan reference: Phase 9 and Sections 3.1, 4, 5.1, and 12.

### [x] CM-910 — Red: specify the sole ConfigurationManifest contract

**Owning layers:** Application contract tests, architecture tests

**Files:**

- `tests/Event.Application.UnitTests/Features/ConfigurationManifest/**`
- `tests/Event.Architecture.Tests/ConfigurationManifest/**`

**Work:**

- Author failing tests for `kind: ConfigurationManifest`,
  `spec.instance`, `spec.tenants`, required closed objects, single-/multi-tenant
  shape, strict unknown-member rejection, and deterministic serialization.
- Author Phase 9 naming ratchets limited to the Application root contract,
  `TenantConfigurationList`, feature namespace, schema tool, and schema identity.
  Environment/path, API/media/HAL/generated contract, BFF/UI, and repository-wide
  ratchets belong to Phases 12, 13, 14, and 15 respectively.
- Author schema expectations for
  `schemas/configuration-manifest-v1alpha1.schema.json` and its canonical `$id`.
- Record the Red failure caused by the missing new contract and existing old
  surface.

**Acceptance:**

- [x] Tests target the public JSON/schema/naming contracts, not private helpers.
- [x] Tests fail for the intended missing contract and obsolete-name reasons.
- [x] No implementation file is changed in this task.

**Evidence:** `ConfigurationManifestContractTests` compiled and failed 7/7:
the unified metadata/root types are absent and the tenant-only root remains.
`ConfigurationManifestSchemaArtifactTests` compiled and failed 2/2 because the
new contract/schema/generator/workflow identities are absent. No product file
changed during the Red task.

### [x] CM-920 — Green: introduce the unified Application contract

**Owning layer:** Application

**Files:**

- `src/Explore.Application/Features/ConfigurationManifest/Contracts/**`
- `src/Explore.Application/Features/ConfigurationManifest/Catalog/**`
- `src/Explore.Application/Features/ConfigurationManifest/Serialization/**`

**Work:**

- Add `ConfigurationManifestV1Alpha1`, root metadata/spec, required instance section,
  tenant list, source-generated JSON context, deterministic serializer
  contracts, and scope-tagged catalog descriptors.
- Keep instance and tenant catalog entries independently enumerable and
  explicitly admitted.
- Preserve strict case sensitivity and unmapped-member rejection.

**Acceptance:**

- [x] CM-910 contract tests pass.
- [x] Contract types remain Application-owned and immutable.
- [x] No old alias or parallel root contract is introduced.

**Evidence:** Added the strict Application-owned
`ConfigurationManifestV1Alpha1` record graph, aligned v1alpha1 metadata,
required instance and tenant sections, independently enumerable scope-tagged
catalog descriptors, and source-generated JSON metadata. Migrated Application
compiler, validator, preflight, export, apply-plan, mapper, serializer, and
their compile-time unit-test consumers without compatibility aliases.
`ConfigurationManifestContractTests` passed 7/7;
`TenantConfigurationManifestSerializationTests` passed 3/3; the
`Event.Application.UnitTests` project built with 0 errors; changed contract and
test files reported no LSP errors; and `git diff --check` was clean.

### [x] CM-930 — Cut over feature paths, intent scope, and schema identity

**Owning layers:** agent contract, Application, Infrastructure/tooling/tests

**Files:**

- `.agents/contract/intents.yaml`
- `.agents/rules/**` and `.omo/rules/**` only when matched twins require updates
- `src/Explore.Application/Features/TenantConfigurationManifest/Contracts/**`
- `src/Explore.Application/Features/TenantConfigurationManifest/Catalog/**`
- `src/Explore.Application/Features/TenantConfigurationManifest/Serialization/**`
- `eng/tenant-manifest-schema/**`
- `schemas/tenant-configuration-manifest-v1.schema.json`
- Application contract/schema architecture test namespaces/paths only

**Work:**

- Replace only Application contract/catalog/serialization and schema-tool
  path/namespace names with `ConfigurationManifest`. Compiler, validation,
  apply, export, startup, API, BFF, and UI names remain for their owning phases.
- Rename the BCL schema tool and generate the new schema path/identity.
- Delete old contract/schema files and update intent paths without broadening
  forbidden secret/bootstrap authority.
- Retain tenant-specific nested plan/result names only where they describe a
  tenant child of the unified manifest.

**Acceptance:**

- [x] Phase 9 Application/schema naming ratchets pass.
- [x] Schema `--check` reports the new artifact current.
- [x] Twin rules remain byte-equivalent when touched.
- [x] No compatibility alias exists.

**Evidence:** Moved the explicit tenant allowlists under the canonical
`ConfigurationManifest` catalog with scope-tagged `TenantSettings` and
`TenantDocuments`; migrated direct Application, Infrastructure, tooling, and
test consumers to the unified contract/catalog/serialization namespaces.
Renamed the schema project and generator to
`ISLAMU.ConfigurationManifest.SchemaGenerator`, updated solution/project/CI
references, generated
`schemas/configuration-manifest-v1alpha1.schema.json`, and removed the old
schema/tool project identities. The generator emits the closed required
instance and tenant sections with the aligned v1alpha1 `$id`, API version, and
kind. `ConfigurationManifestSchemaGenerationTests` passed 7/7,
`ConfigurationManifestSchemaArtifactTests` passed 2/2, and
`ConfigurationManifestCatalogTests` passed 5/5. Schema `--check`,
`git diff --check`, C# LSP diagnostics, old-namespace AST sweeps, and the old
tool-tree sweep were clean. No twin rule file was touched.

### Phase 9 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

**Build evidence:** `dotnet build Explore.slnx --configuration Release
--verbosity quiet` completed in 1 minute 33 seconds with 0 errors. The 5,625
analyzer warnings are pre-existing repository-wide debt and were not suppressed
or changed by this phase.

**Post-correction build evidence:** After the CQRS, repository naming, and
selective query-filter corrections, the same full Release solution build
completed in 1 minute 21 seconds with 0 errors. The 10,460 reported analyzer
warnings remain unrelated repository-wide debt and were neither suppressed nor
changed.

**Architecture evidence:** The initial full run found 11 failures. Corrected the
five branch-created violations by moving the apply command and handler into
project-standard CQRS namespaces, renaming non-CQRS mutation/ingestion inputs,
renaming persistence implementations with the required `Repository` suffix,
and replacing direct all-filter bypass with a named tenant-only bypass.
Focused `CqrsPatternTests` passed 5/5 and `NamingConventionTests` passed 11/11;
AST inspection confirmed the coordinated repository contains no direct
`IgnoreQueryFilters` call. The full rerun improved to 472 passed, 6 failed, and
1 skipped. All six remaining failures concern unchanged repository baseline
artifacts: agent-context route/size/inventory governance, a missing shared
workflow-transition artifact, and the privacy-provider adapter inventory.
Scoped `git status` confirmed the implicated `AGENTS.md`,
`.agents/CONTEXT_ENGINEERING.md`,
`.agents/skills/implementation-plan/SKILL.md`, Stripe privacy adapter, and
workflow-artifact paths were not changed by this workstream. No failure was
suppressed or weakened.

## Phase 10: Instance Configuration Authority And Canonical Mutation

Plan reference: Phase 10 and Sections 3.2, 4, 5.2, and 9.

### [x] CM-1010 — Red: classify and reject unsafe instance settings

**Owning layers:** Domain/Application tests

**Files:**

- `tests/Event.Application.UnitTests/Features/ConfigurationManifest/*Catalog*Tests.cs`
- `tests/Event.Application.UnitTests/Features/ConfigurationManifest/*Validator*Tests.cs`
- verified `SettingRegistry` definitions and mutation-boundary tests

**Work:**

- Inventory every candidate instance setting with owner, sensitivity,
  persistence, lock, runtime effect, exportability, and canonical mutation path.
- Author failing tests proving automatic registry exposure is impossible.
- Reject secrets, secret references, connection strings, topology, provider
  credentials/accounts, operator identity, official status, payment operational
  state, PII, and settings without transaction-aware ownership.
- Reject tenant entries that attempt any instance catalog key.

**Acceptance:**

- [x] Every admitted or rejected instance key has a recorded reason.
- [x] Wrong-scope and sensitive-field tests fail before any repository call.
- [x] Logs/results reflect stable codes and never rejected values.

**Evidence:** Added
`ConfigurationManifestInstanceAuthorityTests` with the exact 25 approved
v1alpha1 instance keys, closed-world classification of every unapproved
`SettingRegistry` entry, canonical scope/sensitivity checks, sensitive-value
non-reflection, tenant-to-instance wrong-scope coding, and compiler
defense-in-depth. Extended the canonical test builder to supply instance
settings/documents. The focused suite compiled and ran 6 tests: 2 exhaustive
inventory/safety assertions passed, while 4 failed for the intended missing
production behaviors—no `InstanceSettings` catalog, ignored sensitive instance
values, absent wrong-scope reason code, and compiler acceptance of invalid
instance input. These pure validator/compiler seams execute before any
repository or external effect. Both changed test files had no LSP errors and
`git diff --check` was clean.

### [x] CM-1020 — Green: add the explicit instance setting catalog and boundary

**Owning layers:** Application

**Files:**

- `src/Explore.Application/Features/ConfigurationManifest/Catalog/**`
- `src/Explore.Application/Settings/SettingUpsertService*`
- coordinated mutation boundaries for approved guarded settings

**Work:**

- Implement the explicit instance setting catalog.
- Add current-transaction instance mutation APIs that preserve Domain
  validation, canonical locks, fresh reads, audit facts, and deferred effects.
- Keep guarded publication/location/payment policy writes routed through their
  existing canonical boundaries.

**Acceptance:**

- [x] CM-1010 tests pass.
- [x] The manifest handler has no direct settings repository write.
- [x] Instance and tenant scope are represented by types, not caller strings.

**Evidence:** Added the exact closed 25-key `InstanceSettings` catalog with
registry-identity, instance-scope, sensitivity, and coordinated-publication
startup assertions. Instance validation now runs before tenant validation,
rejects sensitive/unknown/wrong-scope keys with stable safe paths and reason
codes, bounds display names, and admits only HTTPS branding asset URLs.
`SettingUpsertService` exposes a typed caller-owned transaction API with exact
UTC audit facts and deferred effects; `PublicationPolicyMutationBoundary`
exposes a non-lock-reacquiring instance transaction seam. The canonical
instance dispatcher routes its four guarded event keys through that boundary
before ordinary scalar writes and returns all effects deferred. A typed tenant
setting boundary replaced the manifest handler's direct
`ITenantSettingRepository` dependency and rejects guarded/catalog-invalid
writes before persistence. Both typed boundaries are registered by the
manifest Application graph.

Focused verification passed: instance authority 9/9, instance/tenant mutation
boundaries and registration 9/9, publication boundary 18/18, handler 12/12,
catalog 5/5, validator 18/18, compiler 4/4, publication safety 8/8, setting
mutation architecture 18/18, naming 11/11, and CQRS 5/5. All changed C# files
had no LSP errors and `git diff --check` was clean.

### [x] CM-1030 — Red: specify the sole v1alpha1 instance document authority

**Owning layers:** Domain/Application tests, Tier 0 invariant breakers

**Files:**

- `tests/Event.Domain.UnitTests/**PaidEventPolicy*`
- `tests/Event.Application.UnitTests/Features/ConfigurationManifest/*PaidEventPolicy*Tests.cs`

**Work:**

- Author failing tests for approved instance-policy fields, sovereign
  exclusions, stale active revision, concurrent revision, and tenant narrowing
  against the proposed instance policy from the same manifest.
- Prove `instance.paid_event_policy` is the only admitted v1alpha1 instance
  document key and is backed by the existing paid-policy aggregate rather than a
  generic instance document table.
- Prove every other instance document key and arbitrary JSON document fails
  closed.
- Prove callers cannot inject an unrelated `instancePolicyVersion`.
- Preserve exclusion of sale control, review, handoff, reconciliation,
  acceptance, liability, disputes, negative balances, and refund execution.

**Acceptance:**

- [x] Tests fail on the missing unified binding behavior.
- [x] Broadening and sovereign fields fail before persistence.
- [x] Failure/audit/telemetry contains no payment value, PII, or provider state.

**Evidence:** Added
`ConfigurationManifestPaidEventPolicyAuthorityTests` with a closed sole-key
instance-document catalog specification, canonical aggregate-backed storage,
exact portable policy/risk-field allowlists, caller-selected revision
rejection, nine sovereign operational exclusions, safe diagnostic
non-reflection, same-manifest instance/tenant narrowing, broadening rejection,
an apply-plan active-revision fence, and a caller-owned concurrent-revision
boundary contract. The focused suite compiled and ran 19 tests: the existing
portable risk-ceiling shape passed, while 18 failed for the intended missing
unified behavior—no instance document catalog/validation, the public
`instancePolicyVersion` field remains, sovereign instance fields are ignored,
same-manifest narrowing is not bound, and neither the apply plan nor interface
exposes the expected active-revision fence. These contract, validator, and
reflection seams execute before persistence, and value non-reflection is
asserted before each sovereign-field Red failure.

Existing lower-level authority remained green: Domain paid-policy narrowing
18/18, paid-policy mutation boundary 3/3, and existing manifest validator
18/18. The new test file had no LSP errors and `git diff --check` was clean.

### [x] CM-1040 — Green: bind canonical paid-policy revisions in the apply plan

**Owning layers:** Application

**Files:**

- `src/Explore.Application/Features/PaidEventPolicies/PaidEventPolicyMutationBoundary.cs`
- `src/Explore.Application/Features/ConfigurationManifest/**PaidEventPolicy*`

**Work:**

- Add a transaction-aware instance-policy mutation entry point where required.
- Compile the active or manifest-created instance policy revision as internal
  apply-plan authority.
- Bind tenant narrowing to that revision and validate through
  `PaidEventPolicyRules`.

**Acceptance:**

- [x] CM-1030 tests pass.
- [x] The external contract cannot target another instance revision.
- [x] Normal CQRS and manifest paths share Domain rules, locks, and effects.
- [x] No generic `InstanceSettingsDocument` entity/repository/migration exists.

**Evidence:** Added the closed `InstanceDocuments` catalog with only
`instance.paid_event_policy`, removed caller-selected instance policy revision
authority from the public v1alpha1 payload, and kept storage in the existing
immutable `PaidEventPolicyVersion` aggregate. The compiler emits typed internal
root authority, initial preflight binds the active revision, lock-time
preflight rejects drift, and the handler applies the instance revision before
tenant policy revisions against the resulting effective revision.
`IPaidEventPolicyMutationBoundary` now exposes a caller-owned transaction seam
that starts no nested transaction and reacquires no lock; the normal CQRS path
and manifest path therefore share the same Domain constructors, narrowing
rules, repository fencing, lock identity, and deferred effects.

The generated schema admits only the approved instance document and omits
`instancePolicyVersion`; no generic instance document entity, repository,
migration, or table was introduced. Adversarial tests also fixed five fail-open
or state-integrity defects: rejected tenant broadening no longer retires a
tracked active policy, null instance and tenant documents fail closed,
`ValidateOnly` leaves pending outbox effects untouched, and null sovereign
export metadata returns a stable contract error.

Focused verification passed: authority matrix 22/22, validator 19/19,
paid-policy mutation boundary 6/6, manifest handler 13/13, compiler 5/5,
preflight 7/7, export 8/8, contract 7/7, catalog 5/5, serialization 3/3,
Domain paid-policy rules 18/18, schema generation 7/7, setting-mutation
architecture 18/18, CQRS 5/5, naming 11/11, and PostgreSQL policy
concurrency/rollback 5/5. The Application test project built with 0 errors.
Changed files have no LSP errors and `git diff --check` is clean. The final
weighted MAD review passed: transaction 60%, contract/sovereign authority 20%,
and hands-on QA 20%, with every reproduced finding fixed test-first.

### [x] CM-1050 — Preserve reporting-intake and correction safeguards

**Owning layers:** Application/API regression tests

**Files:**

- publication-policy and reporting-intake tests under
  `tests/Event.Application.UnitTests`
- report endpoint/options/HAL tests under `tests/Event.API.IntegrationTests`
- ConfigurationManifest catalog/validator tests

**Work:**

- Pin the existing green behavior before the rename: keep
  `event_reporting.intake_enabled` tenant-owned, reject it from the instance
  catalog, and route tenant changes through the canonical publication-policy
  mutation boundary.
- Prove direct report POST, report options, and HAL agree when intake is enabled
  or disabled.
- Prove the independent correction/legal/copyright route remains available and
  local-first reporting remains independent of optional external providers.
- Keep these regressions green through Phases 11–15 and map their operator copy
  into CM-1520.

**Acceptance:**

- [x] Instance configuration cannot disable reporting intake globally.
- [x] Manifest tenant changes cannot bypass publication-safety validation.
- [x] POST/options/HAL and correction-route regression selectors pass.
- [x] No external reporting provider becomes required.

**Evidence:** Added explicit regressions proving
`event_reporting.intake_enabled` is valid only in tenant scope and reaches the
canonical `PublicationPolicyMutationBoundary`, never the scalar-setting
boundary. General report POST remains governed by that tenant switch, while
correction, unsafe-link, and legal/copyright submissions use distinct
authenticated routes with server-owned `EventReportSubmissionChannel` values.
They reuse the same local `EventReport` aggregate, encrypted evidence, case,
receipt, quota, serializable unit-of-work, and transactional outbox; optional
provider dispatch remains asynchronous and cannot gate local acceptance.

The security hardening rejects body-spoofed reserved remedy subcategories,
validates null commands safely, masks privacy-erasure fences before and after
intake resolution, collapses non-public event identities to the not-found
contract, and replays public eligibility inside the serializable transaction.
Duplicate identity now includes canonical subcategory, so one remedy cannot
suppress another, and duplicate/quota reads execute atomically with writes.

Focused verification passed: manifest instance authority 10/10, manifest
handler 14/14, publication boundary 18/18, report submission handler 30/30,
report options 8/8, HAL accountability 6/6, report controller 11/11, real
authenticated/unauthenticated HTTP remedy routes and generated hrefs 6/6,
reporting-intake OpenAPI security 2/2, and the real PostgreSQL duplicate query
1/1. Application, API integration, and Persistence integration projects built
with 0 errors. Changed files have no LSP errors and `git diff --check` is
clean. The final weighted MAD review passed across privacy/security,
transaction/outbox, and API/HAL/real-surface QA lanes.

### Phase 10 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

**Evidence:** The full Release solution build completed with 0 errors. The
canonical `dotnet test --project` wrapper stalled before spawning a test host,
so the built TUnit/Microsoft.Testing.Platform executable was run directly from
the manifest worktree. It discovered and passed 4,457/4,457 Application tests
in 5.255 seconds with `--minimum-expected-tests 1`; this proves the Phase 10
snapshot rather than treating a zero-discovery invocation as green.

## Phase 11: Unified Compiler, Atomic Apply, Audit, And Concurrency

Plan reference: Phase 11 and Sections 3.3–3.4 and 5.3.

### [x] CM-1110 — Red: specify complete-state compilation and bootstrap lifecycle

**Owning layers:** Application tests

**Files:**

- `tests/Event.Application.UnitTests/Features/ConfigurationManifest/*Compiler*Tests.cs`
- `tests/Event.Application.UnitTests/Features/ConfigurationManifest/*Preflight*Tests.cs`
- `tests/Event.Application.UnitTests/Features/ConfigurationManifest/*Handler*Tests.cs`

**Work:**

- Author failing tests for instance-before-tenant proposed-state validation,
  deterministic lock plans, first bootstrap, same-digest rerun, changed instance
  section, added tenant under unchanged instance section, existing tenant skip,
  same-section rerun after Day 2 instance changes, current-authority tenant
  validation, exact revision capture, and complete preflight before writes.
- Prove one invalid tenant prevents every instance and tenant write.
- Prove omitted fields never imply deletion/reset.

**Acceptance:**

- [x] Tests fail on missing instance plan/bootstrap marker behavior.
- [x] Tests observe public results and repository state, not helper calls.
- [x] No production compiler/apply code changes in this task.

**Evidence:** Expanded compiler, preflight, and handler tests across the full
bootstrap matrix. Existing green coverage now explicitly proves
instance-before-tenant proposed paid-policy validation, binding to the fresh
active revision, current-authority tenant narrowing, existing-tenant wholesale
skip, complete initial and lock-time preflight, one-invalid-tenant zero-write
behavior, exact revision use, and omission-as-no-reset semantics.

The intentional Red selectors compile with 0 errors and isolate five missing
behaviors. Compiler tests are 6/7 with the sole failure proving that approved
instance settings have no typed instance plan or lock identity. Preflight tests
are 8/8, proving the reusable authority rules are already sound. Handler tests
are 16/20: first bootstrap persists no instance-section digest/generation;
same-section reruns reapply historical instance policy; adding a tenant under
an unchanged section also reapplies it; and changed instance sections succeed
instead of returning
`configuration_manifest_instance_already_bootstrapped`. No production
compiler, preflight, handler, Domain, or Persistence code changed in CM-1110.

### [x] CM-1120 — Green: compile the scope-aware apply plan

**Owning layer:** Application

**Files:**

- `src/Explore.Application/Features/ConfigurationManifest/Compilation/**`
- `src/Explore.Application/Features/ConfigurationManifest/Validation/**`
- `src/Explore.Application/Features/ConfigurationManifest/Application/**Preflight*`

**Work:**

- Compile the complete proposed instance state.
- Validate all tenant settings/documents/policies against that state.
- On a same-section rerun after Day 2 mutation, skip historical instance values
  and validate new tenants against freshly read current effective instance state
  and active policy revisions.
- Produce typed instance and tenant plans, normalized instance-section digest,
  scope-qualified changed-key facts, and deterministic lock keys.
- Replay every authority, revision, uniqueness, lock, and bootstrap marker check
  after locks are held.

**Acceptance:**

- [x] CM-1110 compiler/preflight tests pass.
- [x] No write can occur before complete preflight succeeds.
- [x] Instance and tenant plan types cannot be interchanged.

**Evidence:** The compiler now emits a distinct typed instance plan containing
guarded and ordinary settings, paid-policy authority, and instance-scoped
changed-key facts beside—but never interchangeable with—typed tenant plans.
It computes a 64-character canonical SHA-256 instance-section digest by
ordinally ordering maps and recursively canonicalizing JSON objects; tenant
changes and dictionary insertion order do not alter that identity. The
deterministic lock plan now starts with the canonical instance-manifest
identity and includes every instance setting and paid-policy resource before
the existing tenant/resource identities.

Preflight now composes proposed instance publication settings before tenant
overrides, validates the proposed base and all current tenant states, and keeps
paid-policy narrowing bound to the proposed instance revision. A typed
bootstrap state classifies first bootstrap, unchanged-section reruns, malformed
persisted state, and changed-section rejection. Same-section reruns scrub all
historical instance mutations and changed-key facts, then reread current Day-2
publication settings and active paid-policy revision for new-tenant
validation. The pure classifier is replay-safe for both initial and lock-time
preflight; CM-1140 will supply and persist its marker inside the atomic
orchestrator.

Focused verification passed: compiler 8/8, preflight 12/12, instance authority
10/10, instance mutation boundary 9/9, and paid-policy authority 22/22. Handler
coverage remains intentionally 16/20, with exactly the four CM-1140
operation/marker persistence and orchestration failures. Application source and
test projects build with 0 errors; changed files have no LSP errors and
`git diff --check` is clean.

### [x] CM-1130 — Red: specify real atomicity and competing-writer behavior

**Owning layer:** Persistence integration tests

**Files:**

- `tests/Event.Persistence.IntegrationTests/ConfigurationManifest/**`

**Work:**

- Author failing PostgreSQL tests for instance setting, instance paid-policy,
  tenant create, tenant setting, branding, and tenant
  paid-policy competitors.
- Subscribe to exact transaction/lock events before triggering competitors; no
  sleeps or timing polling.
- Prove only valid serial orders, stale revision rejection, cancellation, full
  rollback, privacy-minimized failure audit, and durable effect recovery.

**Acceptance:**

- [x] Tests fail because the unified transaction/lock behavior is missing.
- [x] Every actual shared authority has a real PostgreSQL collision proof.
- [x] No test asserts a collision for fields excluded from the contract.
- [x] Tests prove the executable hierarchy acquires the instance-manifest,
  sorted instance-resource, and sorted tenant-resource session leases before
  opening the serializable transaction.

### [x] CM-1140 — Green: apply and audit the whole manifest atomically

**Owning layers:** Domain audit/bootstrap state, Application, Persistence

**Files:**

- `src/Explore.Domain/ConfigurationManifest*`
- `src/Explore.Application/Features/ConfigurationManifest/Application/**`
- `src/Explore.Application/Contracts/Persistence/IConfigurationManifest*`
- `src/Explore.Persistence/Configurations/Entities/ConfigurationManifest*`
- `src/Explore.Persistence/Repositories/ConfigurationManifest*`
- provider migration projects and model snapshots

**Work:**

- Add instance bootstrap marker/generation and scope-qualified operation/results.
- Acquire the instance-manifest lease, sorted instance-resource leases, and
  sorted tenant/resource leases; begin one serializable transaction while they
  remain held, replay current-state preflight, then apply/audit/outbox and
  commit.
- Route instance and tenant writes through their canonical in-transaction
  boundaries without nested transactions or inverse lock reacquisition.
- Enqueue value-free outbox effects atomically and dispatch after commit.
- Persist safe failure evidence only after rollback when possible.
- Delete/replace the current unapplied development audit migration and generate
  the renamed bootstrap/audit model for every supported provider from source.

**Acceptance:**

- [x] CM-1110 and CM-1130 tests pass.
- [x] No partial instance, tenant, success-audit, or effect state survives.
- [x] Same instance-section digest is idempotent; changed digest fails stably.
- [x] A same-section rerun after Day 2 changes never reapplies historical
  instance values and binds new tenants to current authority revisions.
- [x] Audit/logs contain no configuration values, secrets, PII, or provider data.
- [x] All provider models report no pending changes; no migration/snapshot was
  hand-edited.

CM-1130 first compiled and failed only at the missing lock hierarchy and
instance-state seams. CM-1140 now acquires the manifest/instance/tenant lease
groups before entering a serializable retryable unit of work, replays marker
and authority state inside the fresh transaction while the leases remain held,
routes all writes through caller-owned-transaction boundaries, records
scope-qualified value-free audit facts, and commits the outbox with state.
Focused evidence is handler 22/22, instance mutation 9/9,
publication policy 19/19, Domain audit 5/5, PostgreSQL lock/state 2/2,
provider parity 5/5, audit persistence 5/5, atomic rollback 7/7, and competing
writer coverage 5/5. EF tooling generated the renamed audit/bootstrap
migration and snapshots for PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL
from source after removing the unapplied development heads.

### Phase 11 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] **Explicitly abandoned:** `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`.
  The complete project exceeded the bounded phase-exit window and produced 147
  unrelated failures: 81 `InvalidOperationException` failures dominated by EF
  `ManyServiceProvidersCreatedWarning`, 49 unrelated `DbUpdateException`
  failures, 11 assertions, 3 PostgreSQL failures, 2 SQLite failures, and 1
  timeout. No ConfigurationManifest-specific failure appeared. The owning real
  PostgreSQL/provider selectors remained green: hierarchy/state 2/2, audit
  persistence 5/5, atomic rollback 7/7, competing writers 5/5, provider parity
  5/5, paid-policy ceiling 1/1, and cross-provider named locks 12/12.

  A fresh final-audit rerun with `--no-restore` again reached the 1,200-second
  deadline after reproducing broad non-manifest failures: EF
  `ManyServiceProvidersCreatedWarning` exceptions, stale migration-chain
  assertions, SQLite schema collisions, missing unrelated federation indexes,
  and privacy-fixture initialization failures. No ConfigurationManifest-owned
  failure appeared, but the required full-project gate remains unsatisfied and
  has not been waived.

## Phase 12: Startup, Deployment, And Recovery Rename

Plan reference: Phase 12 and Sections 3.4, 8, and 11.

### [x] CM-1210 — Red: specify canonical startup naming and topology ownership

**Owning layers:** Infrastructure/host tests

**Files:**

- `tests/Explore.Infrastructure.Tests/ConfigurationManifest/**`
- `tests/Event.Standalone.IntegrationTests/**ConfigurationManifest*`
- migration-service composition tests

**Work:**

- Author failing tests for `CONFIGURATION_MANIFEST_PATH`,
  `CONFIGURATION_MANIFEST_MODE`, canonical convention path, old-key rejection,
  exact one-owner topology, post-migration/pre-traffic ordering, invalid-file
  exit, cancellation, rerun, and safe diagnostics.
- Add deployment-contract tests for read-only mounts, non-root paths, and absence
  of tenant-manifest names.

**Acceptance:**

- [x] Tests fail on old names and missing unified startup service.
- [x] Tests do not start Docker, Aspire, browsers, or external services.
- [x] No fixed sleeps or polling are used.

Focused Red evidence compiles with 0 errors and runs 4 tests: the existing
post-migration/pre-traffic ordering proof passes, while exactly 3 tests fail on
the missing canonical Infrastructure surface, old environment/path constants,
and legacy deployment artifacts. The tests only inspect assemblies and static
artifacts; they start no services and contain no waits or polling.

### [x] CM-1220 — Green: cut over Infrastructure, hosts, and deployment

**Owning layers:** Infrastructure, hosts, Compose/Aspire/images

**Files:**

- `src/Explore.Infrastructure/ConfigurationManifest/**`
- `src/Event.Standalone/**`
- `src/Event.MigrationService/**`
- development migration-owner composition
- `src/Explore.AppHost/**`
- `docker-compose.yml`
- Dockerfiles and `.env.example`

**Work:**

- Rename options, reader, runner, DI, startup sequence, stable codes, and log
  categories.
- Wire only the canonical keys/path and delete old environment/configuration
  compatibility.
- Preserve one startup owner and durable post-commit effect recovery.

**Acceptance:**

- [x] CM-1210 tests pass.
- [x] Split API replicas cannot become bootstrap owners.
- [x] Explicit invalid files prevent traffic/one-shot success without leaking path contents.

The Infrastructure namespace, concrete reader/scanner/options, DI extension,
startup runner/exception, and post-migration sequence now use the canonical
ConfigurationManifest identity. Application-facing stable failure and tenant
result codes use the `configuration_manifest_*` prefix. MigrationService owns
split bootstrap, Standalone owns combined bootstrap, and both run the shared
sequence after migration but before successful completion/traffic. Compose
retains a read-only migration-owner mount and API completion dependency; Aspire
assigns exactly one owner by topology. The focused static and behavioral suite
passes 37/37, including missing/invalid explicit-source path redaction,
migration failure short-circuiting, one-shot stop ordering, DI composition, and
canonical deployment artifact assertions.

### [x] CM-1230 — Update operator configuration and recovery contracts

**Owning layer:** documentation/configuration

**Files:**

- `.env.example`
- `docs/CONFIGURATION.md`
- `docs/SECRETS.md`
- `docs/SELF_HOSTING.md`
- `docs/OPERATIONS.md`
- `docs/TROUBLESHOOTING.md`

**Work:**

- Replace old names, file paths, examples, and schema references.
- Document single-/multi-tenant shape, `Off`/`ValidateOnly`/`Bootstrap`,
  instance-section digest semantics, tenant skip/add behavior, read-only mount,
  non-root permissions, reset/cutover, and failure recovery.
- Add the plan's operator state/action matrix for missing/invalid/lost source,
  Day 2 divergence, changed digest, existing/conflicting tenant, unavailable
  failure audit, database restore, export overflow, and disablement.
- State that `.env` selects the mounted path but never contains manifest
  business values or mounts the file itself.

**Acceptance:**

- [x] No operator doc advertises old keys or continuous reconciliation.
- [x] Secrets remain Infisical/`.env` owned and absent from manifest examples.
- [x] Recovery instructions identify exact safe operator actions.

The operator contract now documents one instance-wide file for single- and
multi-tenant deployments, canonical mode/path/mount names, strict no-secret
boundaries, read-only non-root ownership, first-bootstrap and same-section
digest semantics, whole-tenant skips, and the no-reconciliation rule. The
state/action matrix covers explicit/convention source loss, invalid input, lost
source, Day 2 divergence, changed digest, tenant conflict, unavailable failure
audit, database restore, export overflow, and disablement. Pre-commit failures
are corrected and retried atomically; recorded operations retain audit evidence
and recover post-commit effects from the durable outbox. Canonical schema
`--check`, Compose config validation, link-target checks, legacy-term scans, and
`git diff --check` pass.

### Phase 12 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

The Phase 12 Release build is green with 0 warnings and 0 errors after
canonicalizing the Standalone integration fixture's startup probe/interface.
The complete Infrastructure phase-exit project passes 1554/1554.

## Phase 13: Whole-Instance Export, API, HAL, And Generated Contract

Plan reference: Phase 13 and Sections 3.5, 5.4, and 9.

### [x] CM-1310 — Red: specify whole-instance export authority and contract

**Owning layers:** Application/API tests

**Files:**

- `tests/Event.Application.UnitTests/Features/ConfigurationManifest/*Export*Tests.cs`
- `tests/Event.API.IntegrationTests/Features/ConfigurationManifest/**`

**Work:**

- Author failing tests for one instance export in single-/multi-tenant mode,
  deterministic ordering, Overrides/Portable semantics, typed document output,
  sovereign omission metadata, instance-admin authorization, wrong-instance,
  tenant-admin denial, no-store response, the shared 256-tenant ceiling, 4 MiB
  aggregate bound, no partial overflow bytes, canonical media type, filename,
  and stable ProblemDetails.
- Pin
  `GET /api/control-plane/configuration-manifest/export?view=Overrides|Portable`,
  operation ID `ExportConfigurationManifest`, trusted current-instance
  resolution, `InstanceSettings`/`View` authorization plus export facts,
  `application/vnd.islamu.configuration-manifest.v1alpha1+json`,
  401/403/provider-unavailable behavior, and absence of caller-supplied instance
  identity.
- Prove the dedicated all-tenant entity query is available only to the
  instance-authorized export path and remains constrained to active tenants.
- Author HAL and contract tests proving tenant export relations/routes are gone.
- Add secret/PII/provider/operational-state output scans.

**Acceptance:**

- [x] Tests fail on the tenant-only export and missing whole-instance authority.
- [x] Authorization tests cover Cerbos and local fallback parity.
- [x] Tests inspect bytes and public HTTP/HAL contracts, not private serializers.

Two compile-clean Red classes specify the canonical Application and API
contracts. The Application selector fails 5/5 only on the absent whole-instance
query, serializer/contract/result, and dedicated active-tenant entity read. The
API selector fails 7/7 only on the absent canonical route/controller, current
404s, remaining tenant aliases, missing shared export fact, and unreachable
provider/overflow ProblemDetails contract. Coverage includes single-/multi-
tenant ordering, Overrides/Portable metadata, typed documents, sovereign
omission, instance authority, trusted instance resolution, tenant/wrong-
instance denial, no-store buffered bytes, the 4 MiB preflight, canonical media
type/filename/operation ID, and Cerbos/local parity. LSP and diff integrity are
clean; no production or legacy tests changed.

### [x] CM-1320 — Green: implement unified export CQRS, API, and HAL

**Owning layers:** Application, API/HAL

**Files:**

- `src/Explore.Application/Features/ConfigurationManifest/Application/*Export*`
- `src/Explore.API/Controllers/*ConfigurationManifest*`
- `src/Explore.API/Hateoas/**ConfigurationManifest*`
- route names, failure policies, authorization providers

**Work:**

- Replace tenant export query with one instance-authorized query and canonical
  deterministic serializer.
- Emit instance and all authorized tenant configuration through one file.
- Add the exact canonical Control Plane endpoint, trusted current-instance
  resolution, `IAuthorizedRequest` facts, Cerbos/local parity, fail-closed
  provider behavior, named all-tenant repository query, HAL relations, rate
  limit, timeout, no-store caching, bounded incremental serialization, media
  type, attachment, and ProblemDetails.

**Acceptance:**

- [x] CM-1310 new endpoint/authorization/serialization subsets pass; obsolete
  route/HAL absence assertions intentionally remain Red until CM-1330.
- [x] Tenant-only callers cannot infer or export other tenant configuration.
- [x] Export overflow fails before response bytes with
  `configuration_manifest_export_too_large`.
- [x] Controllers contain no business logic.

CM-1320 now exposes one instance-authorized MediatR query and canonical Control
Plane download endpoint. The handler reads instance settings plus every active
tenant through a named entity-returning filter bypass, composes typed
paid-policy and branding documents, emits deterministic Overrides or Portable
bytes, and fails before returning a byte array above 4 MiB. Cerbos and local
authorization consume the same explicit export fact; Control Plane HAL exposes
only permission-gated whole-instance links. Focused evidence is Application
contract 5/5, handler/overflow 3/3, API 6/7 with only the planned legacy-route
assertion Red, HAL 1/1, and local-provider parity 1/1. LSP and diff integrity
are clean; the local workstation lacks the Cerbos CLI, so repository policy
compilation remains part of the later phase gate.

### [x] CM-1330 — Remove tenant-shaped export authorization surfaces

**Owning layers:** Application/API/BFF contract cleanup

**Files:**

- former tenant export query/controller/HAL/BFF route capabilities
- authorization policies/facts and tests

**Work:**

- Delete tenant-self manifest endpoints, HAL relations, capabilities, generated
  source inputs, and support code superseded by whole-instance export.
- Preserve ordinary tenant Day 2 settings APIs and permissions.

**Acceptance:**

- [x] No partial tenant file is represented as a deployable manifest.
- [x] Tenant administrators retain existing settings administration but cannot
  receive instance export.
- [x] Old route/operation/HAL relation requests use canonical not-found
  behavior; no redirect or alias exists.
- [x] The complete CM-1310 suite now passes, including obsolete-route/HAL
  absence assertions.

CM-1330 deletes the tenant-scoped export CQRS types, both tenant HTTP routes,
tenant-only media contract, HAL candidates, OpenAPI source registrations, BFF
forwarder/service, client capability/service, and tenant settings export
component. Shared reporting-intake, effective-configuration, and tenant Day 2
settings surfaces remain intact. The complete canonical/obsolete route selector
passes 7/7; target-tenant HAL passes 11/11; reporting-intake HAL/API passes
11/11. Runtime reference and diff scans are clean outside the generated
OpenAPI/NSwag artifacts intentionally handed to CM-1340.

### [x] CM-1340 — Regenerate OpenAPI, inventory, and NSwag

**Owning layer:** generated contracts/tooling

**Files:**

- API schema/operation transformer and catalog sources
- checked-in OpenAPI document
- API contract inventory
- `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`

**Work:**

- Stabilize canonical operation IDs and binary response schema.
- Regenerate all artifacts from source twice and compare checksums.
- Delete old generated tenant-manifest methods/types and stale contract tests.

**Acceptance:**

- [x] OpenAPI, inventory, and NSwag expose only canonical names.
- [x] Second generation produces byte-identical artifacts.
- [x] Generated files were not hand-edited.
- [x] CM-1330 removal is complete before generation.

CM-1340 registers the canonical export view as a string enum and shapes the
single 200 response as the exact manifest media type with OpenAPI
`string`/`binary`, following ASP.NET Core file-download metadata. The documented
API build, inventory generator, and NSwag target produced only
`ExportConfigurationManifest` and `ConfigurationManifestExportView`; the
generated method returns `FileResponse`. Runtime OpenAPI and generated-client
contract tests pass 1/1 and 3/3. A second complete generation run produced
identical SHA-256 hashes for OpenAPI, inventory, and client artifacts.

### Phase 13 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 14: BFF And Administration Cutover

Plan reference: Phase 14 and Sections 3.5 and 10.

### [x] CM-1410 — Red: specify BFF and instance-admin UI behavior

**Owning layers:** Blazor BFF/client tests

**Files:**

- `tests/Explore.Blazor.IntegrationTests/**ConfigurationManifest*`
- `tests/Explore.Blazor.Client.Tests/**ConfigurationManifest*`

**Work:**

- Author failing tests for the fixed same-origin BFF route, bounded binary
  download, exact content type/filename validation, downstream failure mapping,
  token secrecy, HAL capability revalidation, instance-admin placement,
  tenant-page removal, focus recovery, localization, RTL, and live
  announcements.
- Prove no browser role/claim check or raw API export URL authorizes the action.

**Acceptance:**

- [x] Tests fail on the old tenant export service/components.
- [x] BFF tests subscribe to response/state signals without sleeps.
- [x] Client tests assert HAL presence/absence and rendered behavior.

### [x] CM-1420 — Green: implement canonical BFF download and instance UI

**Owning layers:** Blazor BFF/client

**Files:**

- `src/Explore.Blazor/Services/**ConfigurationManifest*`
- BFF endpoint mapping
- `src/Explore.Blazor.Client/Pages/Admin/**ConfigurationManifest*`
- service interfaces/models, localization, scoped CSS

**Work:**

- Replace tenant-manifest BFF methods/routes with the whole-instance download.
- Add a focused instance/Control Plane administration component using generated
  client adapters and HAL-only capabilities.
- Preserve bounded buffering, safe failures, no-store behavior, token secrecy,
  keyboard/focus semantics, localization, RTL, and project tokens.

**Acceptance:**

- [x] CM-1410 BFF/client tests pass.
- [x] Tenant settings navigation no longer renders manifest export.
- [x] The browser cannot choose instance/tenant authority outside HAL.
- [x] Phase 14 BFF/UI naming ratchets contain no old service, route,
  capability, component, or localization identity.

### [x] CM-1430 — Reconcile accessibility and administration guidance

**Owning layers:** Blazor client/docs

**Files:**

- instance admin composition/navigation
- localization resources
- scoped CSS
- `docs/BLAZOR.md` and relevant configuration/self-hosting links

**Work:**

- Ensure semantics, descriptions, warning copy, focus, announcements, target
  size, reflow, contrast, and RTL meet WCAG 2.2 AA repository rules.
- Explain whole-instance authority, secret omission, flattened values, and
  non-backup semantics before download.

**Acceptance:**

- [x] No raw authority code is rendered as user-facing prose.
- [x] Missing HAL affordances remove all action entry points.
- [x] Focus moves safely when a revalidated capability disappears.

### Phase 14 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 15: Generated Artifacts, Documentation, Cutover, And Review

Plan reference: Phase 15 and Sections 7–14 and 17.

### [x] CM-1510 — Complete breaking cutover and generated-artifact drift gates

**Owning layers:** all generated/configuration contract surfaces

**Files:**

- JSON Schema, OpenAPI, API inventory, NSwag
- EF provider migrations/snapshots
- CI/release/schema packaging
- tracked reference and naming ratchets

**Work:**

- Delete every obsolete tenant-manifest artifact and obsolete test.
- Verify the Phase 11 generated audit/bootstrap migration and every provider
  model remain current. Regenerate only if a later approved source-model change
  actually invalidated them; never regenerate for documentation or naming
  cosmetics.
- Regenerate schema/API/client artifacts twice and verify stable bytes.
- Verify package/container/release references use only the new schema/file.
- Run the repository-wide zero-old-runtime-name ratchet after all phase-local
  removals.

**Acceptance:**

- [x] No tracked runtime/doc/config/generated reference exposes the old name.
- [x] Provider models have no pending changes.
- [x] Schema/OpenAPI/client/inventory checksums are stable.
- [x] No generated file was hand-edited.

### [x] CM-1520 — Update canonical docs, release evidence, and I-VSD

**Owning layer:** documentation/governance

**Files:**

- `docs/CONFIGURATION.md`
- `docs/SECRETS.md`
- `docs/SELF_HOSTING.md`
- `docs/OPERATIONS.md`
- `docs/SECURITY-MODEL.md`
- `docs/PAYMENTS.md`
- `docs/TROUBLESHOOTING.md`
- `docs/API_CHANGELOG.md`
- `docs/releases/changes/CHG-*.yaml`
- `islamic-value-sensitive-design/i-vsd-configuration-manifest.md`

**Work:**

- Teach the final contract, scope/authority exclusions, ordering, transaction,
  rerun/recovery, export, reset, and bootstrap-not-reconcile semantics.
- Teach that reporting intake remains tenant-owned, local-first, guarded by the
  effective publication policy, consistent across POST/options/HAL, and
  accompanied by an independent correction/legal/copyright route.
- Record the intentional breaking rename and removal of old keys/routes/schema.
- Reconcile I-VSD provider responsibility, tenant autonomy, instance power,
  payment authority, privacy, portability, and evidence limits.

**Acceptance:**

- [x] Canonical docs and examples agree with generated contracts.
- [x] No text claims refund execution, secret backup, desired-state control, or
  moral/legal certification.
- [x] Release fragment validates and names operator cutover actions.

### [x] CM-1530 — Run criticality evidence, MAD review, and final reconciliation

**Owning layer:** verification/review evidence

**Files:**

- `.omo/evidence/<date>-configuration-manifest/**`
- plan/context/tasks reconciliation

**Work:**

- Capture focused tests, real PostgreSQL invariant breakers, tenant spoofing,
  zero-PII telemetry scans, mutation score, blast radius, and summary evidence.
- Run independent security/privacy, database/concurrency, instance/tenant
  authority, payment/I-VSD, and self-hosting/operator review lanes.
- Anonymize findings, disposition every issue, and apply weighted post-hoc MAD
  voting.
- Reconcile all plan/task/context/docs/generated evidence.

**Acceptance:**

- [x] Tier 1 and applicable Tier 0 evidence artifacts exist and are readable.
  Evidence: `.omo/evidence/2026-08-27-configuration-manifest/mad-review.md` plus
  the rendered LTR/RTL visual captures beside it.
- [x] Mutation score exceeds the repository threshold for modified critical
  handlers. Stryker 4.16.0 tested exactly
  `ApplyConfigurationManifestCommandHandler` and
  `ExportConfigurationManifestQueryHandler`: 176 killed, 5 survived, 0
  timeouts, 0 errors, and a 94.12% final score against the required `>85%`
  threshold. Report:
  `.omo/evidence/2026-08-27-configuration-manifest/mutation-critical-handlers-second/reports/mutation-report.json`.
- [x] No review blocker or critical finding survives. All eleven admitted
  findings were reproduced by a failing test and fixed; the weighted post-hoc
  vote is unanimous `pass`.
- [x] Every checklist item and phase gate is complete or explicitly abandoned
  with an approved reason.

**CM-1530 evidence:** The first correctly scoped mutation pass established a
real 74.87% baseline (130 killed, 18 survived, 10 timed out). Public-behavior
tests were then added for response/error contracts, value-free no-op effects,
audit facts, boundary indexes, corrupt bootstrap evidence, collaborator
failure semantics, exact export view/filename, tenant-count limits, policy
provenance/narrowing, closed-catalog reads, version coherence, duplicates, and
cancellation. The hardened Apply and Export classes passed 35/35 and 23/23
before the second mutation run, which passed at 94.12%. Final LTR/RTL captures
embed production MudTheme variables plus both parent and nested scoped styles;
two independent visual reviewers returned `PASS` with no product or evidence
blocker.

### Phase 15 Verification

- [x] `dotnet build --configuration Release --verbosity quiet` — succeeded, 0 errors.
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` —
  483 total, 478 passed, 1 skipped, 4 failed. Every residual failure reproduces
  on mainline `develop` or is a branch-behind-`develop` artifact/inventory skew;
  none originates in a ConfigurationManifest file. See §7 of the evidence record.

## Phase 16: V1Alpha2 Artifact And Section Contracts

Plan reference: Phase 16 and Sections 3.6, 4, 5.1, and 9.1.

### [x] CM-1610 — Red: specify v1alpha2 artifact authority and section coverage

**Owning layers:** Application/architecture tests

**Work:**

- Author failing contract tests for distinct `ConfigurationManifest` and
  `TenantConfigurationPackage` kinds, closed roots, target-authority exclusion,
  named apply modes, legal-content limits, and clean v1alpha1 removal.
- Author failing section-registry tests requiring scope, authority,
  portability class, dependencies, references, export, preview/diff, apply,
  verify, rollback, and documentation for every section.
- Pin CM-S1, CM-S4, CM-S6, and wrong-scope/secret/PII/application-data
  invariant breakers before production changes.

**Acceptance:**

- [x] Contract selectors fail only because v1alpha2/package/registry behavior is
      missing, and verify artifact metadata cannot select target authority.
- [x] Tests assert public JSON/schema/coverage behavior rather than private
      collaborator calls.
- [x] No production or generated file changes in this Red task.

**Evidence:** Added `ConfigurationManifestV1Alpha2ContractTests` and
`ConfigurationPortabilityRegistryTests` at public reflection/contract seams.
The first selector compiled and failed 6/6 for the missing tenant-package
metadata, v1alpha2 roots, named apply-mode enum, content-limit contract, and
still-present v1alpha1 types. The registry selector failed 5/5 solely because
`ConfigurationPortabilityRegistry` does not yet exist. The fresh baseline
Release build completed with 0 errors and the complete
`Event.Application.UnitTests` project passed 1,937/1,937 before these Red tests.
No product or generated file changed in CM-1610.

### [x] CM-1620 — Green: implement artifact contracts and portability registry

**Owning layer:** Application

**Work:**

- Implement immutable v1alpha2 record graphs, deterministic serializers,
  explicit instance/tenant catalogs, section descriptors, portability classes,
  coverage/omission/fidelity contracts, and typed apply modes.
- Classify settings, typed documents, footer, navigation, templates, lookups,
  custom-property definitions, localization, registration policy, modules, and
  extension sections by actual owner and safety.
- Keep secrets, PII, application data, operational state, provider bindings,
  and deployment topology explicitly nonportable.

**Acceptance:**

- [x] `ConfigurationManifestV1Alpha2ContractTests` and
      `ConfigurationPortabilityRegistryTests` pass via focused selectors.
- [x] Every admitted section has one canonical owner and every omitted category
      appears in machine-readable coverage.
- [x] No registry discovery or generic JSON automatically grants portability.

**Evidence:** Added strict v1alpha2 whole-instance and tenant-package record
graphs, distinct metadata/media identities, explicit apply modes, bounded legal
content constants, and a 21-entry frozen portability registry. The registry
names every portable/mapped section and gives secrets, PII, application data,
operational state, provider bindings, and deployment topology explicit
nonportable descriptors with no mutation capabilities. The artifact selector
passed 6/6 and the registry selector passed 5/5. Temporary compile-only aliases
keep existing internal consumers buildable inside Phase 16; CM-1630 removes
them and every v1alpha1 source/schema identity before the phase gate.

### [x] CM-1630 — Generate schemas and complete the clean contract cutover

**Owning layers:** schema tooling/generated contracts/architecture

**Work:**

- Generate v1alpha2 manifest and tenant-package schemas from source.
- Remove v1alpha1 schema/media/generated-contract identities without aliases,
  converters, redirects, or dual reads.
- Update intent/rule paths only where the new artifacts require canonical
  scope; preserve twin rules and secret restrictions.

**Acceptance:**

- [x] Both schema `--check` commands pass and second generation produces
      byte-identical artifacts.
- [x] Architecture ratchets verify no v1alpha1 compatibility surface remains.
- [x] Phase 16 closes only after one Release build and the complete
      `Event.Application.UnitTests` project pass.

**Evidence:** Migrated Application, Infrastructure, test-support, BFF, OpenAPI,
NSwag, container packaging, workflow, and intent references to v1alpha2 names;
removed both compile-only alias files and the v1alpha1 schema. The generator now
owns deterministic manifest and tenant-package outputs through explicit
`manifest` and `tenant-package` CLI selectors. Both schema checks passed twice,
schema generation passed 9/9, artifact cutover passed 2/2, and runtime/generated
source inspection found zero v1alpha1 identities. Manual CLI QA observed help
exit 0 and invalid-artifact usage exit 64. The Release solution build completed
with 0 errors; the complete Application project passed 1,948/1,948.

### Phase 16 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 17: Typed Legal Documents And Safe Content

Plan reference: Phase 17 and Sections 3.7, 5, 9.1, and 13.

### [x] CM-1710 — Red: specify legal authority, lifecycle, and content invariants

**Owning layers:** Domain/Application tests

**Work:**

- Author failing tests for closed instance/tenant legal kinds, role ownership,
  locale/size/link/placeholder bounds, draft-to-published lifecycle, immutable
  history, and acceptance separation.
- Author adversarial constrained-Markdown tests for HTML, scripts, remote
  resources, unsafe links, tracking, oversized localized content, and
  unresolved required identity.
- Pin template provenance, non-certifying status, and target-review behavior.

**Acceptance:**

- [x] `LegalDocumentPortabilityInvariantTests` fail for the missing aggregate
      and verify CM-S7/CM-S8 without persistence mocks.
- [x] Unsafe content and acceptance-history input fail before any public-state
      mutation.
- [x] No external template prose or implementation structure is imported.

**Evidence:** Added 13 compiled reflection-bound Domain Invariant-Breakers.
They pin a closed 29-kind instance/tenant catalog, one role owner per kind,
scope/tenant isolation, Draft→ReviewRequired→Approved→Scheduled→Published/
Retired transitions, append-only publication evidence, target-review import,
acceptance-fact separation, 32-locale/256-KiB/128-link/64-placeholder bounds,
unsafe raw HTML/script/remote-image/tracking-link rejection, and non-certifying
project/FOSS template provenance. All 13 fail solely because the legal Domain
contracts are absent. Test content is repository-native sentinel prose, not an
external legal template.

### [x] CM-1720 — Green: implement legal aggregates and persistence

**Owning layers:** Domain/Application/Persistence

**Work:**

- Implement typed legal document/value objects, role/kind catalogue,
  localization metadata, lifecycle, publication versions, target drafts,
  acceptance-impact metadata, and clean template provenance.
- Add canonical mutation boundaries and repositories returning entities.
- Generate all provider migrations from source; do not hand-edit snapshots.

**Acceptance:**

- [x] Domain legal lifecycle/authority selectors pass and verify publication
      history remains append-only.
- [x] Import/export cannot carry acceptance facts or source target authority.
- [x] All provider models report current after generated migrations.

**Evidence:** Implemented the closed 29-kind role catalogue, bounded
network-free localized source policy, non-certifying provenance, target-scoped
aggregate, immutable versions, append-only publication/retirement evidence,
entity-returning repository, four EF configurations, and explicit target
coordinates on every repository read. Added acceptance-free legal source to
both v1alpha2 artifacts and generated schemas. Domain invariants pass 13/13,
portable contract tests pass 7/7, and legal persistence passes 9/9 including
cross-tenant denial, SQLite aggregate round-trip, and pending-model checks for
PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL. All five generated
`AddPortableLegalDocuments` migrations were inspected and contain legal-table
operations only; no unrelated shared model change was absorbed.

### [x] CM-1730 — Share safe rendering and public legal composition

**Owning layers:** Application/API/Blazor legal surfaces

**Work:**

- Use one deterministic constrained-Markdown validation/rendering contract for
  preview, import/export, and public pages.
- Replace static Terms/Privacy authority with role-labeled last-published
  instance/tenant documents while preserving safe fallback behavior only where
  explicitly approved.
- Add template/readiness, source-origin link, identity-placeholder, locale, and
  accessibility diagnostics without network fetches.

**Acceptance:**

- [x] `LegalDocumentRenderingContractTests` verify identical safe output across
      editorless preview, API, and public rendering.
- [x] Unsafe content never becomes public and last published content remains
      available after a failed draft/import.
- [x] Phase 17 closes only after one Release build and the complete
      `Event.Domain.UnitTests` project pass.

**Evidence:** Added one dependency-free Domain parser/renderer for the bounded
Markdown grammar; source creation, preview, persisted API composition, and
public pages all use it. It encodes identity substitutions, rejects raw/fenced
content, images, unsafe/tracking/private-network links, malformed headings and
placeholders, emits value-safe locale/template/import/accessibility
diagnostics, and never performs I/O. Application selects the latest active
immutable public publication, exact/base/English/deterministic locale, and
target-owned instance or tenant identity. The anonymous generated
`GetPublicLegalDocument` operation returns role-labeled publication facts with
locale-aware caching. `/terms` and `/privacy` now render that contract through
one accessible component and show only a neutral unavailable state when no
reviewed publication exists.

Focused evidence is Markdown 4/4, rendering 5/5, API 2/2, persisted composition
and provider parity 10/10, public pages 3/3, accessibility conventions 8/8,
record contracts 11/11, schema generation 9/9, generated transformation 6/6,
and generated serialization 12/12. OpenAPI, API inventory, and NSwag output are
byte-stable on a subsequent complete generation pass. The Release solution
build passes with 0 warnings/0 errors and the complete Domain project passes
1,043/1,043.

### Phase 17 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 18: Import Sessions, Preview, Diff, And Mapping

Plan reference: Phase 18 and CM-R2/CM-R3/CM-R5.

### [x] CM-1810 — Red: specify bounded side-effect-free import sessions

**Owning layers:** Application/API tests

**Work:**

- Author failing tests for bounded uploads, protected temporary storage,
  digest-bound sessions, authority scope, expiry, replay, cancellation,
  retention, rate limits, value-safe errors, and zero preview side effects.
- Pin changed/unchanged/skipped/mapped/blocking/warning/omitted/external-setup
  preview categories and stale-preview behavior.

**Acceptance:**

- [x] `ConfigurationImportSessionContractTests` fail only on missing session
      behavior and verify CM-S2/CM-S3.
- [x] Preview tests prove no setting/document/tenant/success-audit/outbox/provider
      mutation occurs.
- [x] Secret/PII/value scans cover ProblemDetails, logs, metrics, and traces.

**Evidence:** Added 12 compiled Application Invariant-Breakers for bounded
upload/session lifetime, trusted route target separation, opaque protected
artifact handles, expiry/cancellation/replay/consume state, digest-only session
metadata, the eight required preview categories, preview freshness binding,
parameterless pure composition with no mutation dependency, stable failure
codes, immutable collection ownership, and one value-safe observability
contract shared by logs/metrics/traces/support evidence. Added three API
boundary contracts for the canonical 4 MiB ceiling, dedicated rate/timeout
policies, and code/status/retry-only ProblemDetails.

The Application selector discovers 12 and fails 12 only on absent
`Explore.Application.Features.ConfigurationManifest.Importing` contracts. The
API selector discovers 3 and fails 3 only on absent
`Explore.API.ConfigurationImport` contracts. Both projects compile with zero
test-contract errors; no production source, dependency, schema, or generated
artifact changed in CM-1810. Evidence:
`.omo/evidence/20260830-configuration-manifest-import/`.

### [x] CM-1820 — Green: implement preview, semantic diff, and mapping

**Owning layers:** Application/Persistence

**Work:**

- Implement the import-session state machine, bounded protected storage
  metadata, parser, section dependency graph, semantic diff, stable-reference
  mapping, coverage, approval requirements, and expiry cleanup.
- Bind preview to artifact digest, trusted target, target revisions, selected
  sections, mappings, apply mode, and required approvals.

**Acceptance:**

- [x] Focused session/preview selectors pass and stale target changes invalidate
      apply readiness.
- [x] Mapping uses stable identities, never localized names or source database
      IDs as authority.
- [x] Expiry/cancellation removes temporary bytes and retains only permitted
      value-minimized evidence.

**Evidence:** Implemented a target-scoped optimistic session state machine with
fixed-time token-digest checks, expiry/cancellation/one-time consumption,
artifact/target/revision/selection/mapping/apply-mode/approval freshness
binding, and generic value-safe failures. The parser enforces strict v1alpha2
JSON, duplicate/unknown-member rejection, the canonical 4 MiB ceiling, and
exact-byte SHA-256 identity. A parameterless pure composer derives
portability/coverage/dependencies from the closed registry, snapshots
collections, classifies every required outcome, requires stable ASCII machine
mapping identities, and blocks unknown sections or missing approvals without
calling repositories, providers, audits, outboxes, or mutation boundaries.

Persistence stores artifact bytes only after purpose-bound ASP.NET Core Data
Protection encryption, rechecks digest/length after decrypting, binds every
session query to trusted target authority, uses optimistic revision fencing,
and deletes protected bytes transactionally on cancellation or expiry while
retaining digest/status evidence. Five generated
`AddConfigurationImportSessions` migrations contain import tables only and all
provider models are current.

Focused results: contract 12/12, behavior 10/10, parser 3/3, persistence and
provider parity 10/10, Clean Architecture 15/15, and record contracts 11/11.
The Release solution build exits 0; existing unrelated analyzer debt remains.

### [x] CM-1830 — Expose import-session API, HAL, and generated contracts

**Owning layers:** API/HAL/OpenAPI/BFF contract

**Work:**

- Add separate instance and tenant upload/preview/refresh/cancel routes with
  exact authorization facts, antiforgery, rate/size/timeout policies,
  ProblemDetails, no-store behavior, and HAL affordances.
- Regenerate OpenAPI, API inventory, and NSwag twice from source.

**Acceptance:**

- [x] `ConfigurationImportSessionControllerTests` verify 401/403/404/409/413,
      provider-unavailable, expiry, and wrong-scope behavior.
- [x] Generated contracts expose only canonical v1alpha2 operations and produce
      stable bytes on the second run.
- [ ] Phase 18 closes only after one Release build and the complete
      `Event.API.IntegrationTests` project pass.

**Evidence:** Added separate instance and tenant upload, preview, refresh, and
cancel controllers over Application-owned commands. Uploads are streamed into
the canonical 4 MiB bound, use dedicated per-actor rate and timeout policies,
require write authorization, return private no-store responses, and keep the
opaque capability exclusively in `X-Configuration-Import-Token`. Preview
derives current target revisions server-side from the source artifact's
portable/override view, canonicalizes object order and numeric lexical forms,
and includes tenant display-name authority in semantic section digests.

HAL exposes instance and tenant creation affordances only through the existing
authorization evaluator; the BFF enforces antiforgery and forwards only the
header capability. ProblemDetails collapses target/token mismatch to the safe
not-found shape and preserves explicit expiry, stale, size, and provider
availability statuses. Real HTTP tests prove unauthenticated and unavailable
authorization responses are no-store and capability-free.

OpenAPI now publishes binary vendor request bodies, named string enum
components, HAL result schemas, canonical operation IDs, and required token
headers. OpenAPI, API inventory, and NSwag converged byte-for-byte on the
second generation pass. Focused Application parser/snapshot/session tests,
API boundary/controller/OpenAPI/HTTP/HAL tests, the BFF antiforgery suite, the
generated-client contract suite, and Clean Architecture/API/record gates all
pass. Phase closure still awaits the two verification commands below.

### Phase 18 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 19: Atomic Apply, Receipts, Snapshots, And Forward Rollback

Plan reference: Phase 19 and CM-R4.

### [x] CM-1910 — Red: specify selected-section atomicity and rollback races

**Owning layer:** Persistence integration tests

**Work:**

- Author real PostgreSQL invariant breakers for stale preview, changed mapping,
  expired/missing approval, instance/tenant/policy/legal competitors, snapshot
  failure, one-invalid-section rollback, cancellation, and forward rollback
  racing Day 2 writers.
- Subscribe to exact lock/transaction events before competitors; use no sleeps
  or timing polling.

**Acceptance:**

- [x] `ConfigurationImportAtomicityTests` fail only on missing expanded apply
      behavior and verify CM-S5 against observable database state.
- [x] No test asserts framework call counts or excluded/nonportable fields.
- [x] Failure evidence remains value-minimized after transaction rollback.

### [x] CM-1920 — Green: implement atomic selected apply and forward rollback

**Owning layers:** Domain/Application/Persistence

**Work:**

- Replay target authority, digest, revisions, selections, mappings, mode, and
  approvals under ordered leases before a fresh serializable transaction.
- Create a protected pre-import portable snapshot, route every selected write
  through canonical transaction-aware boundaries, persist receipt/outbox, and
  verify resulting fidelity.
- Implement rollback as a new authorized preview/apply operation.

**Acceptance:**

- [x] Atomicity/concurrency selectors prove valid serial outcomes and no partial
      section, receipt, audit-success, or effect state.
- [x] Rollback preserves append-only history and never bypasses current target
      authority.
- [x] Provider migrations/models are generated and current.

### [ ] CM-1930 — Complete operation history, recovery, and phase gate

**Owning layers:** Application/Persistence/operations

**Work:**

- Add value-safe operation history, receipt download, snapshot retention,
  post-commit retry, fidelity verification, cancellation-before-commit, and
  forward-rollback relationships.
- Repair the unrelated Persistence test baseline or obtain an explicit waiver;
  focused selectors do not substitute for the phase gate.

**Acceptance:**

- [x] `ConfigurationImportRecoveryTests` verify failed/prepared/applied/effect-
      pending/rolled-back states and safe retries.
- [x] Retention/cleanup never deletes evidence required for an authorized
      rollback without an explicit expired/not-available result.
- [ ] Phase 19 closes only after one Release build and the complete
      `Event.Persistence.IntegrationTests` project pass or documented waiver.

### Phase 19 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 20: Tenant Portability And Cross-Instance Migration

Plan reference: Phase 20 and CM-R1/CM-R5.

### [x] CM-2010 — Red: specify tenant package authority and isolation

**Owning layers:** Application/API security tests

**Work:**

- Author failing tests for tenant-only export/preview/import/clone/history/
  rollback, trusted route-selected target, source-provenance-only metadata,
  delegated tenant creation, instance locks/ceilings, and other-tenant denial.
- Scan package/preview/receipt bytes for instance, other-tenant, secret, PII,
  provider, and operational state.

**Acceptance:**

- [x] `TenantConfigurationPackageAuthorityTests` fail for missing behavior and
      verify CM-S1.
- [x] Tenant callers cannot infer whole-instance or another tenant’s values,
      existence, locks, or operation history.
- [x] Package metadata never decides target identity or authorization.

### [x] CM-2020 — Implement tenant export, import, clone, history, and rollback

**Owning layers:** Application/API/HAL/BFF

**Work:**

- Implement deterministic tenant package export, import-session reuse,
  existing-target and delegated-clone modes, instance-ceiling validation,
  receipts/history/rollback, and capability-specific HAL.
- Preserve whole-instance export/import as independent instance authority.

**Acceptance:**

- [x] Tenant package handler/controller/HAL selectors pass for Cerbos/local
      parity and cross-tenant denial.
- [x] Clone fails closed without delegated create authority and never copies
      source tenant/database identity as target authority.
- [x] Existing Day 2 settings APIs remain available and independently
      authorized.

### [ ] CM-2030 — Prove migration fidelity and source independence

**Owning layers:** API/Persistence migration tests

**Work:**

- Exercise source-to-target packages across supported tenancy modes/providers,
  mappings, omitted sections, external setup, legal target drafts, retry,
  rollback, and source-origin independence.
- Produce machine-readable fidelity and target-setup reports.

**Acceptance:**

- [x] `TenantConfigurationMigrationFidelityTests` verify equivalent portable
      state and truthful named omissions.
- [x] No migration deletes or mutates source state automatically.
- [ ] Phase 20 closes only after one Release build and the complete
      `Event.API.IntegrationTests` project pass.

### Phase 20 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 21: Blazor Instance And Tenant Administration

Plan reference: Phase 21 and Section 3.8.

### [x] CM-2110 — Implement whole-instance import administration

**Owning layers:** Blazor BFF/client

**Work:**

- Add HAL-gated upload, preview, section selection, diff, mapping, approvals,
  apply, progress, history, receipt, retry, and rollback under the existing
  instance/Control Plane administration surface.
- Keep tokens and privileged API destinations server-side.

**Acceptance:**

- [x] `ConfigurationManifestImportAdministrationTests` verify rendered
      instance behavior, capability loss, stale preview, and operation results.
- [x] Missing HAL removes every instance import/apply/rollback entry point.
- [x] Raw JSON is optional and no local role/claim check grants authority.

### [x] CM-2120 — Implement tenant portability administration

**Owning layers:** Blazor BFF/client

**Work:**

- Add tenant package export/import/clone/history/rollback under the current
  tenant administration surface with source/target role labels and instance
  lock/ceiling explanations.

**Acceptance:**

- [x] `TenantConfigurationPortabilityAdministrationTests` verify tenant-scoped
      actions, target identity, mappings, omissions, and denied capabilities.
- [x] No tenant UI action exposes whole-instance or another-tenant data.
- [x] Source and target legal/operator responsibilities remain explicit.

### [x] CM-2130 — Complete accessibility, localization, and usability contracts

**Owning layers:** Blazor client/localization/scoped CSS

**Work:**

- Implement keyboard-complete wizard behavior, focus restoration, screen-reader
  summaries, non-color states, narrow reflow, RTL/logical layout, localized
  consequences/recovery, reduced motion, and plain/expert modes.
- Test dense diffs, mapping, approval, progress, capability loss, and rollback.

**Acceptance:**

- [x] `ConfigurationPortabilityAccessibilityTests` pass for semantic and
      interaction contracts without raw prose/CSS pinning.
- [x] No secret/value appears in accessible names, announcements, or errors.
- [x] Phase 21 closes only after one Release build and the complete
      `Explore.Blazor.Client.Tests` project pass.

### Phase 21 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 22: Extensibility, Managed Ownership, And Direct Transfer

Plan reference: Phase 22 and CM-R3/CM-R5.

### [x] CM-2210 — Implement extension, signature, and managed-ownership contracts

**Owning layers:** Application/security/governance

**Work:**

- Complete declarative extension descriptors and compatibility handling.
- Add signed pack provenance/trust policy and explicit managed field ownership,
  drift, takeover, relinquishment, previewed deletion, and conflict rules.
- Reject scripts, SQL, migrations, plugins, unknown licenses/provenance, and
  undeclared ownership.

**Acceptance:**

- [x] `ConfigurationExtensionAndOwnershipTests` verify non-executable packs,
      issuer trust, drift-only mode, takeover consent, and unmanaged-field
      preservation.
- [x] Managed deletion appears explicitly in preview and cannot cross declared
      ownership.
- [x] Unknown/missing required extensions fail without silent omission.

### [x] CM-2220 — Red/Green direct-transfer security and recovery

**Owning layers:** Application/API/security/privacy

**Work:**

- Write invariant breakers, then implement opt-in mutually approved transfer
  sessions with destination proof, SSRF defenses, nonce/digest binding, replay
  protection, bounded/resumable transport, expiry, cancellation, and no source
  deletion.

**Acceptance:**

- [x] `ConfigurationDirectTransferSecurityTests` first fail on missing
      safeguards, then pass for SSRF, replay, wrong target, expiry, resume, and
      duplicate commit scenarios.
- [x] Transfer never carries secrets/PII/application data and never bypasses
      target preview/approval/apply.
- [x] Interrupted transfer leaves both instances authoritative and unchanged.

### [x] CM-2230 — Complete GitOps, collaboration, and operational controls

**Owning layers:** Application/API/operations

**Work:**

- Add dry-run/drift reports, approval separation, scheduled apply windows,
  immutable receipts, effect/dead-letter visibility, safe support bundles,
  migration readiness, and retention/health metrics.
- Keep continuous overwrite disabled unless explicit managed ownership exists.

**Acceptance:**

- [x] `ConfigurationManagedOperationsTests` verify uploader/reviewer/applier
      separation, drift without overwrite, scheduled stale fencing, and
      value-free support/observability.
- [x] No metric/log label contains paths, values, PII, or unbounded identities.
- [x] Phase 22 closes only after one Release build and the complete
      `Event.Application.UnitTests` project pass.

### Phase 22 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 23: Generated Contracts, Operations, Evidence, And Release

Plan reference: Phase 23 and Sections 7–18.

### [ ] CM-2310 — Regenerate contracts and reconcile coverage/I-VSD

**Owning layers:** generated artifacts/governance

**Work:**

- Regenerate JSON Schemas, OpenAPI, API inventory, NSwag, and provider models
  twice and compare bytes.
- Generate section coverage/docs and reconcile every IVSD-F001–F024 mapping.
- Refresh the I-VSD report to `plan-aligned` when path scope permits.

**Acceptance:**

- [ ] All generated checks pass twice with stable digests and no hand edits.
- [ ] Coverage names every supported, mapped, environment-bound, secret,
      application-data, operational, and unsupported section.
- [ ] I-VSD/plan/context/tasks agree on revision/status/disposition and
      F025–F030 remain explicitly deferred.

### [x] CM-2320 — Update operator, developer, legal, and migration documentation

**Owning layer:** documentation/operations

**Work:**

- Update configuration, self-hosting, operations, security, privacy, legal,
  accessibility, troubleshooting, API changelog, and contributor guides for
  v1alpha2, imports, tenant migration, legal review, retention, rollback,
  managed ownership, transfer, and support evidence.

**Acceptance:**

- [x] Markdown/link/schema examples agree with generated contracts and verify
      configuration is not application-data migration, secrets, or backup.
- [x] Recovery instructions cover stale/expired preview, failed apply, pending
      effects, rollback, source retention, and unavailable snapshot.
- [x] Documentation contains no Setup Assistant implementation claim.

### [ ] CM-2330 — Criticality review, change fragment, and final commit composition

**Owning layer:** review/release evidence

**Work:**

- Run Tier 1/Tier 0 invariant, real-concurrency, zero-PII, security/privacy,
  accessibility, migration-fidelity, and anonymized MAD review gates.
- Create and validate the final append-only Tier 2 change fragment.
- Reconcile triad/evidence and compose the final conventional commit subject
  and trailers; do not create a commit unless the user explicitly authorizes it.

**Acceptance:**

- [ ] No unwaived phase gate, critical finding, generated drift, I-VSD mismatch,
      or triad inconsistency remains.
- [ ] Release fragment validates and records v1alpha2 breaking/operator actions.
- [ ] Phase 23 closes only after one Release build and the complete
      `Event.Architecture.Tests` project pass.
- [ ] Definition of Done is proven before any Setup Assistant implementation
      plan begins.

### Phase 23 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Focused TUnit Selector Map

Use the owning project with:

```bash
dotnet run --project <owning-project>.csproj --no-build -- \
  --treenode-filter "/*/*/*<TestClass>/*"
```

| Task pair | Planned focused test class |
|---|---|
| CM-910/920 | `ConfigurationManifestContractTests` |
| CM-930 | `ConfigurationManifestSchemaArtifactTests` |
| CM-1010/1020 | `ConfigurationManifestInstanceCatalogTests` |
| CM-1030/1040 | `ConfigurationManifestPaidEventPolicyTests` |
| CM-1050 | `ConfigurationManifestReportingSafetyRegressionTests` |
| CM-1110/1120 | `ConfigurationManifestCompilerTests` and `ConfigurationManifestPreflightTests` |
| CM-1130/1140 | `ConfigurationManifestConcurrencyTests` |
| CM-1210/1220 | `ConfigurationManifestStartupTests` |
| CM-1230 | Markdown/link/whitespace checks only |
| CM-1310/1320/1330 | `ConfigurationManifestExportControllerTests` |
| CM-1340 | `ConfigurationManifestOpenApiContractTests` |
| CM-1410/1420 | `ConfigurationManifestExportServiceTests` and `ConfigurationManifestExportSectionTests` in their owning projects |
| CM-1430 | `ConfigurationManifestAdministrationAccessibilityTests` |
| CM-1510 | `ConfigurationManifestNamingAndArtifactTests` |
| CM-1520 | Markdown/link/schema/release-fragment checks only |
| CM-1530 | Criticality selectors named by the evidence ledger; no additional full project before the phase gate |
| CM-1610/1620/1630 | `ConfigurationManifestV1Alpha2ContractTests` and `ConfigurationPortabilityRegistryTests` |
| CM-1710/1720/1730 | `LegalDocumentPortabilityInvariantTests` and `LegalDocumentRenderingContractTests` |
| CM-1810/1820/1830 | `ConfigurationImportSessionContractTests` and `ConfigurationImportSessionControllerTests` |
| CM-1910/1920/1930 | `ConfigurationImportAtomicityTests` and `ConfigurationImportRecoveryTests` |
| CM-2010/2020/2030 | `TenantConfigurationPackageAuthorityTests` and `TenantConfigurationMigrationFidelityTests` |
| CM-2110 | `ConfigurationManifestImportAdministrationTests` |
| CM-2120 | `TenantConfigurationPortabilityAdministrationTests` |
| CM-2130 | `ConfigurationPortabilityAccessibilityTests` |
| CM-2210 | `ConfigurationExtensionAndOwnershipTests` |
| CM-2220 | `ConfigurationDirectTransferSecurityTests` |
| CM-2230 | `ConfigurationManagedOperationsTests` |
| CM-2310/2320/2330 | Generated-contract, documentation, criticality, and architecture gates named by the Phase 23 evidence ledger |

## Remaining / Deferred Work

- YAML;
- manifest directory/multi-file composition;
- secret references or secret-provider identifiers;
- Avalonia web/desktop Setup Assistant;
- Terminal.Gui and CLI/TUI commands;
- `.env` generation and secret-entry UI;
- agentic skill and any embedded AI;
- operational payment sale-control/review/handoff/reconciliation/refund
  execution;
- events, users, registrations, orders, tickets, payments, uploaded files, and
  other application-data migration;
- file-size changes beyond the governed limit without measured evidence.

## Synchronization Rules

- Plan, context, and tasks must agree on the `ConfigurationManifest` name,
  current phase, next task, blockers, decisions, and deferred scope.
- They must also agree that `TenantConfigurationPackage` is distinct from a
  tenant manifest and that Setup Assistant/Avalonia/TUI/CLI/skill is deferred.
- A checked task requires acceptance evidence in that task or the latest context
  handoff.
- A phase is complete only when all tasks and both verification checkboxes pass.
- Update the plan only for strategy/scope/phase/acceptance changes.
- Update context immediately for decisions, failures, blockers, baseline changes,
  or handoff.
- Before pause/PR, reconcile all artifacts and name unrelated baseline failures
  without weakening gates.
