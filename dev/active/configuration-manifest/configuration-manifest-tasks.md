<!-- ABOUTME: Execution ledger for the instance-wide ConfigurationManifest rebase. -->
<!-- ABOUTME: Sequences strict Red/Green contract, authority, atomicity, export, and cutover work. -->

# Configuration Manifest And Reporting-Intake Policy — Task Checklist

Last Updated: 2026-08-27 Europe/Brussels

## Status Summary

- **Overall status:** Phases 9–15 product work complete; objective closure is
  blocked by the unsatisfied full-Persistence phase gate and unavailable
  Context7 MCP research channel.
- **Completed new tasks:** 25/25.
- **Current priority:** Obtain an explicit gate decision or restore the required
  external verification prerequisites.
- **Next recommended slice:** Re-run the complete Persistence project only
  after its unrelated shared baseline is repaired, and use Context7 only when
  that MCP is registered; otherwise obtain an explicit waiver before closing
  the objective.
- **Known blockers:** the complete Persistence project still times out with
  broad unrelated baseline failures, and Context7 MCP is not registered.
  Focused manifest/provider evidence and official-source web research are green,
  but neither substitutes for the literal original requirements without
  approval.
- **Superseded workstream:** `dev/active/tenant-configuration-manifest/`.
  Its completed runtime foundation remains in the branch, but its planning
  artifacts are replaced by this workstream.
- **Plan:** [configuration-manifest-plan.md](configuration-manifest-plan.md)
- **Context:** [configuration-manifest-context.md](configuration-manifest-context.md)
- **I-VSD:** [i-vsd-configuration-manifest.md](../../../islamic-value-sensitive-design/i-vsd-configuration-manifest.md)

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

## Remaining / Deferred Work

- managed `Reconcile` mode, field ownership, drift/diff, takeover, deletion,
  pruning, and conflict policy;
- YAML;
- remote URL ingestion;
- manifest directory/multi-file composition;
- secret references or secret-provider identifiers;
- tenant-shaped partial manifest exports;
- operational payment sale-control/review/handoff/reconciliation/refund
  execution;
- file-size changes beyond the governed limit without measured evidence.

## Synchronization Rules

- Plan, context, and tasks must agree on the `ConfigurationManifest` name,
  current phase, next task, blockers, decisions, and deferred scope.
- A checked task requires acceptance evidence in that task or the latest context
  handoff.
- A phase is complete only when all tasks and both verification checkboxes pass.
- Update the plan only for strategy/scope/phase/acceptance changes.
- Update context immediately for decisions, failures, blockers, baseline changes,
  or handoff.
- Before pause/PR, reconcile all artifacts and name unrelated baseline failures
  without weakening gates.
