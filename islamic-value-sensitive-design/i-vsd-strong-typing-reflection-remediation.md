<!-- ABOUTME: I-VSD planning review for strong typing, test truthfulness, and trust-boundary consolidation. -->
<!-- ABOUTME: Maps maintainability and assurance decisions to provider responsibilities without issuing religious rulings. -->

# Strong Typing And Reflection Debt Remediation — I-VSD Planning Review

Last Updated: 2026-08-30

## Review Metadata

- Mode: planning
- Subject: Strong typing, executable assurance, and reflection-debt remediation
- Workstream: `strong-typing-reflection-remediation`
- Report kind: provider-responsibility planning review
- Report status: current
- Disposition: plan-aligned
- Evidence cutoff: 2026-08-30
- Reviewed input revision: `sha256:1a2fa2e4cfaca23086cb49648c0111b5be9c68e85ab5abdddee08e20b1f9b157`
- Supersedes: none

## Scope

This review covers provider-controlled engineering decisions in a repository-wide refactor of:

- tests that dispatch through runtime type/member names instead of public typed seams;
- tests that inspect raw implementation source instead of observable behavior;
- repeated identity, role, header, route, and configuration literals across trust boundaries;
- Domain primitive candidates whose type design can either clarify or overcomplicate authority and invariants;
- contribution-contract and verification rules needed to keep the migration truthful and safe.

The review is limited to software-provider responsibility. It does not assess a religious ruling, certify the software, or prove stakeholder or deployed operational outcomes.

## Claim Boundary

The current evidence supports:

- design validation through I-VSD principles and domains;
- implementation-traceability evidence from repository code, tests, contracts, and governance;
- a planning recommendation for preserving critical invariants during structural change.

It does not provide stakeholder interviews, production incident evidence, deployed security audit results, or qualified scholarly validation.

## Findings

### IVSD-F001 — Source-shaped tests can create false assurance

- **Lifecycle:** accepted
- **Severity:** high
- **Claim type:** design and implementation traceability
- **Principles:** Truthfulness / Sidq; Trust / Amanah; Excellence / Ihsan
- **Domain:** Technical; Evaluation
- **Stakeholders:** users relying on correct behavior, operators, maintainers, contributors, reviewers
- **Provider-controlled decision:** whether tests observe public behavior or merely pin source tokens/runtime names
- **Evidence:** `FairReturnWaitlistConcurrencyTests` source scraping; `AdmissionContractRuntime`; evidence packet sections “Reflection Runtime Debt” and “Source-Scraping Debt”
- **Validation level:** implementation traceability
- **Risk:** a green test can overstate confidence while a refactor silently changes behavior, or can fail when behavior remains correct
- **Mitigation:** `IVSD-M001`
- **Owner / next validation:** typed/behavioral test migration phases; transient invariant-disposition evidence
- **Escalation boundary:** none

### IVSD-F002 — Duplicate identity derivation can weaken fail-closed trust boundaries

- **Lifecycle:** accepted
- **Severity:** critical
- **Claim type:** security design
- **Principles:** Trust / Amanah; Non-Harm / La Darar; Rights of People
- **Domain:** Technical; Governance
- **Stakeholders:** authenticated users, tenant members, instance operators, support operators
- **Provider-controlled decision:** which code is authorized to translate claims into platform identity, provider subject, and session identity
- **Evidence:** canonical `PlatformIdentityPrincipalExtensions`; bypassing claim readers in API, BFF, Infrastructure, HATEOAS, rate limiting, idempotency, diagnostics, and logging
- **Validation level:** implementation traceability
- **Risk:** divergent fallback order or semantic conflation may misidentify a caller, alter rate partitions, expose diagnostics, or weaken tenant/resource checks
- **Mitigation:** `IVSD-M002`
- **Owner / next validation:** identity consolidation and Tier 1 adversarial verification tasks
- **Escalation boundary:** implementation must stop if a purpose-bound authentication scheme cannot use the canonical platform-identity semantics without widening authority

### IVSD-F003 — Blanket value-object and catalog creation would turn clarity into maintenance burden

- **Lifecycle:** accepted
- **Severity:** medium
- **Claim type:** architecture design
- **Principles:** Excellence / Ihsan; Trust / Amanah; Avoiding Gharar
- **Domain:** Technical; Strategic
- **Stakeholders:** maintainers, integrators, self-hosters, API consumers
- **Provider-controlled decision:** whether every repeated string becomes a global constant/value object or only a real semantic concept receives a type
- **Evidence:** existing `CurrencyMetadata`, `Money`, `TenantDirectoryOperatorIdentity`, admission lookup/enum mirrors, broad transport/config meanings for email/country/slug
- **Validation level:** implementation traceability
- **Risk:** indiscriminate wrappers create conversion, serialization, migration, and generated-client churn while obscuring ownership
- **Mitigation:** `IVSD-M003`
- **Owner / next validation:** semantic-string taxonomy and bounded Domain value phase
- **Escalation boundary:** no new value type enters public wire or persistence contracts without proving the invariant it makes unrepresentable

### IVSD-F004 — Mechanical test cleanup can erase critical safety evidence

- **Lifecycle:** accepted
- **Severity:** critical
- **Claim type:** assurance governance
- **Principles:** Non-Harm / La Darar; Justice / Adl; Trust / Amanah
- **Domain:** Technical; Governance; Evaluation
- **Stakeholders:** tenants, ticket buyers, participants, operators, users whose PII or access is protected
- **Provider-controlled decision:** whether brittle tests are deleted before an equivalent or stronger invariant seam passes
- **Evidence:** `test-suite-rationalization` acceptance/prohibitions; money, state-machine, concurrency, tenant, security, privacy, HAL, BFF, provider, migration, and protocol cohorts in the submitted report
- **Validation level:** implementation traceability
- **Risk:** a structurally cleaner suite may provide less protection against cross-tenant access, stale authority, replay, money overflow, credential leakage, or inaccessible UI
- **Mitigation:** `IVSD-M004`
- **Owner / next validation:** each migration task plus phase verification
- **Escalation boundary:** deletion is blocked until the invariant disposition names a passing replacement or intentionally removed behavior

### IVSD-F005 — Missing contribution-contract ownership undermines accountable execution

- **Lifecycle:** accepted
- **Severity:** high
- **Claim type:** governance design
- **Principles:** Trust / Amanah; Promise-Keeping; Truthfulness / Sidq
- **Domain:** Governance; Technical
- **Stakeholders:** contributors, reviewers, maintainers, future agents
- **Provider-controlled decision:** whether mixed source/test refactors have an explicit scope, required guidance, forbidden moves, and verification contract
- **Evidence:** only `test-suite-rationalization` matches directly and it forbids `src/**`; its required triad is absent
- **Validation level:** implementation traceability
- **Risk:** contributors either violate scope or improvise inconsistent rules and test gates
- **Mitigation:** `IVSD-M005`
- **Owner / next validation:** governance foundation phase
- **Escalation boundary:** product-source implementation cannot start until the mixed-refactor intent/fallback contract is validated

### IVSD-F006 — UI test modernization must retain server-authored authority and accessibility

- **Lifecycle:** accepted
- **Severity:** high
- **Claim type:** design assurance
- **Principles:** Justice / Adl; Trust / Amanah; Excellence / Ihsan
- **Domain:** Design; Technical; Evaluation
- **Stakeholders:** keyboard and assistive-technology users, tenant operators, ordinary users
- **Provider-controlled decision:** whether typed bUnit migration preserves HAL relation gates, semantic status/error output, focus, and read-only behavior
- **Evidence:** tenant directory-operator, participant readiness, waitlist, transfer, and add-on component tests; repository HAL rule
- **Validation level:** implementation traceability
- **Risk:** replacing reflection with shallow typed rendering could improve compile-time safety while losing the behavior that matters
- **Mitigation:** `IVSD-M006`
- **Owner / next validation:** typed Blazor test phase
- **Escalation boundary:** no UI migration is complete if it reconstructs authority from roles/status or drops the existing accessibility invariant

## Recommendations

### IVSD-M001 — Use an executable-seam taxonomy

- **Lifecycle:** accepted
- Classify each candidate as public behavior, compiled architecture metadata, machine-consumed artifact, or prohibited source/prose assurance.
- Replace runtime name dispatch and raw product-source scraping with direct typed calls, rendered semantics, endpoint/model metadata, analyzers, or real-provider behavior.
- Preserve reflection when compiled type relationships or endpoint metadata are themselves the contract.

Rejected alternative: banning reflection and file reads globally. It would remove legitimate architecture, generated-contract, policy, and schema assurance.

### IVSD-M002 — Preserve one platform-identity authority

- **Lifecycle:** accepted
- Route platform user-ID derivation through `PlatformIdentityPrincipalExtensions`.
- Keep provider-subject, session-ID, machine-auth, setup-secret, scanner, and receipt schemes purpose-separated.
- Add fail-closed tests for malformed/non-GUID claims, fallback ordering, purpose-bound schemes, and tenant/resource isolation.

Rejected alternative: introduce a generic `AppClaimTypes` catalog and leave multiple extraction algorithms in place. Shared spelling without shared semantics does not create a single authority.

### IVSD-M003 — Type semantic identities, not every string

- **Lifecycle:** accepted
- Keep currency, country, email, tenant slug, transport, config, route, and database identifiers as strings where their owner already validates or the protocol requires them.
- Reuse existing `Money`, `CurrencyMetadata`, and capability-specific validators.
- Limit the new Domain-value candidate to AT Protocol DID, with explicit parsing, exact value preservation, and scalar wire/persistence conversions.

Rejected alternative: global `CurrencyCode`, `CountryCode`, `EmailAddress`, and generic `Slug` migration in one workstream.

### IVSD-M004 — Require invariant disposition before deletion

- **Lifecycle:** accepted
- For every reflected/source-shaped test cohort, record the protected behavior and its retained typed/behavioral replacement.
- Sequence red invariant anchors before security, concurrency, money, state-machine, privacy, or tenant implementation changes.
- Use deterministic event/state signals and real providers where query translation or locking is the behavior.

Rejected alternative: measure success by lower line count, fewer tests, or elimination of reflection calls.

### IVSD-M005 — Establish an explicit mixed-refactor contribution contract

- **Lifecycle:** accepted
- Add and validate a primary intent for mixed product-source/test strong-typing refactors.
- Copy the applicable invariant-disposition and source-assurance gates into the new primary intent; keep `test-suite-rationalization` related but do not inherit or widen its `src/**` prohibition or missing active-doc references.
- Add a benchmark scenario and governance decision entry so future contributors do not improvise the route.

Rejected alternative: silently widen `test-suite-rationalization` despite its explicit source prohibition.

### IVSD-M006 — Bind typed UI tests to HAL and accessibility outcomes

- **Lifecycle:** accepted
- Use generic bUnit rendering and typed parameters/services/models.
- Preserve exact relation matching, read-only/edit state, focus behavior, semantic live regions, and bounded error output.
- Treat true runtime-selected composition separately from tests that merely hide inaccessible or not-yet-created symbols.

Rejected alternative: replace `DynamicComponent` mechanically without classifying why a component is dynamic.

## Stakeholders

| Stakeholder | Interest / possible burden |
|---|---|
| End users and participants | Correct state, identity, ticketing, waitlist, transfer, and privacy behavior |
| Tenant and instance operators | Reliable admin authority, diagnostics, recovery, and predictable UI affordances |
| People whose PII is processed | No authority widening or telemetry leakage during refactor |
| Assistive-technology and keyboard users | Accessibility behavior remains protected |
| Maintainers and contributors | Compile-time discoverability, smaller test harnesses, truthful failures, bounded scope |
| Self-hosters and integrators | Stable wire/config protocols and no unnecessary migration burden |

## I-VSD Principles And Domains

Applicable principles:

- **Trust / Amanah:** security authority and test evidence must be dependable.
- **Truthfulness / Sidq:** green tests must correspond to observable or compiled contract truth.
- **Justice / Adl:** safety and accessibility burdens must not be shifted to users for developer convenience.
- **Non-Harm / La Darar:** refactoring must not weaken tenant, money, credential, privacy, or concurrency boundaries.
- **Promise-Keeping:** repository contribution and greenfield quality contracts must be followed.
- **Excellence / Ihsan:** remove unnecessary runtime dispatch while preserving deep, maintainable modules.
- **Avoiding Gharar:** explicit ownership and typed semantic contracts reduce uncertainty.

Primarily applicable domains: Technical, Governance, Evaluation, and Design. Strategic and Operational impact is indirect through maintainability and future reliability.

## Validation Gaps

- No stakeholder usability or maintainer-experience study was performed.
- No deployed incident or operational audit data was reviewed.
- The final knowledge-graph blast radius is unavailable until implementation start.
- The dirty shared worktree may change cited files before execution.
- Implementation verification, adversarial results, and operational evidence remain unavailable because this is a planning review.

## Escalation Needed

- No qualified Islamic scholarly review is required for this engineering refactor.
- Security escalation is required if canonical platform identity cannot replace a caller without changing the caller's purpose-bound scheme semantics.
- Governance escalation is required before any task widens permissions, removes critical coverage without replacement, or changes public wire/database contracts outside the approved plan.

## Evidence Reviewed

- `dev/active/strong-typing-reflection-remediation/strong-typing-reflection-remediation-evidence.md`
- `dev/active/strong-typing-reflection-remediation/strong-typing-reflection-remediation-plan.md`
- `dev/active/strong-typing-reflection-remediation/strong-typing-reflection-remediation-tasks.md`
- `dev/active/strong-typing-reflection-remediation/strong-typing-reflection-remediation-context.md`
- `AGENTS.md`, `.agents/CONTEXT_ENGINEERING.md`, implementation-plan/I-VSD/grill-me skills
- `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/TESTING.md`, `docs/ARCHITECTURE.md`, `docs/RECORD_CONTRACTS.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/DOMAIN.md`
- matched path rules and source/test files enumerated in the evidence packet
- official source register recorded in the evidence packet

## Missing Evidence

- implementation-start knowledge-graph callers/callees/affected flows;
- baseline build and selected project tests from the implementation revision;
- completed transient invariant-disposition evidence;
- adversarial identity and tenant-isolation results;
- generated-artifact determinism evidence from the unconditional implementation gate;
- stakeholder and operational validation.

## Context Inventory

- Repository architecture: Clean Architecture + CQRS + BFF + HAL + EF Core multi-provider persistence.
- Lifecycle: pre-release greenfield; clean replacement is preferred over compatibility.
- Existing authorities: platform identity extension, boundary-specific claim/header catalogs, `AuthorizationActions`/`ResourceKinds`, `RouteNames`, `Money`/`CurrencyMetadata`, lookup seeders.
- Overlap: paused `blazor-clean-code-refactor`; no active `test-suite-rationalization` triad exists.
- Worktree: extensive unrelated edits; implementation must isolate and avoid overwriting them.

## Common Overlooked Failures And Outcomes

- A mechanical `nameof` conversion can still pin implementation structure instead of behavior.
- A shared claim constant can preserve two conflicting fallback algorithms.
- A typed bUnit test can lose HAL and accessibility assertions while appearing “modernized.”
- A value converter can make Domain code look typed while public JSON, EF queries, and generated clients drift.
- A deleted red harness can remove the only concurrency or tenant-isolation test before its replacement runs.
- Route constant cleanup can accidentally change operation IDs or HAL resolution despite identical-looking names.
- Positive outcome: contributors get compile-time navigation and failures closer to the owning contract while critical behavior remains executable and observable.

## Planning Handoff

- Workstream: `strong-typing-reflection-remediation`
- Status: current
- Reviewed input revision: `sha256:1a2fa2e4cfaca23086cb49648c0111b5be9c68e85ab5abdddee08e20b1f9b157`
- Findings and mitigations: `IVSD-F001 -> IVSD-M001`; `IVSD-F002 -> IVSD-M002`; `IVSD-F003 -> IVSD-M003`; `IVSD-F004 -> IVSD-M004`; `IVSD-F005 -> IVSD-M005`; `IVSD-F006 -> IVSD-M006`
- Verified plan mappings: Plan Section 9 maps every finding/mitigation to named scenarios and exact implementation task ranges
- Escalations required before: product-source implementation if the mixed-refactor contribution contract is not validated; any security-authority change that cannot preserve purpose separation
- Refresh triggers: changed scope, identity fallback semantics, public wire/database contracts, value-object candidates, coverage-deletion policy, HAL/accessibility requirements, or task mappings

## Review Lifecycle

| Date | Previous status | New status | Trigger | Evidence/replacement |
|---|---|---|---|---|
| 2026-08-30 | none | draft / ready-for-planning | Implementation-plan intake and repository evidence packet completed | Evidence revision `sha256:1a2fa2e4cfaca23086cb49648c0111b5be9c68e85ab5abdddee08e20b1f9b157` |
| 2026-08-30 | draft / ready-for-planning | current / plan-aligned | Completed plan, task, context, scenario, and `IVSD-*` mapping revalidation | Same evidence revision plus the completed workstream triad |
