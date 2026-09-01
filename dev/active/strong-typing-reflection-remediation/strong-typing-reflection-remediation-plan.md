<!-- ABOUTME: Canonical architecture and phase plan for eliminating stringly typed runtime dispatch and false test assurance. -->
<!-- ABOUTME: Preserves compiled metadata, security boundaries, behavior, and scalar wire/storage contracts. -->

# Strong Typing And Reflection Debt Remediation — Implementation Plan

Last Updated: 2026-09-01 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Follow the submitted repository audit and plan a complete refactor of hardcoded semantic strings, weak typing, reflection-based contract runtimes, source-scraping tests, duplicated security literals, and justified Domain primitive debt. Follow repository conventions, Clean Architecture, enterprise-quality design, industry guidance, and greenfield breaking-change freedom.
- **Task directory:** `dev/active/strong-typing-reflection-remediation/`
- **Planning status:** Approved
- **Change classification:** **Behavioral Delta** overall, composed of:
  - security/federation behavior changes at malformed, conflicting, or purpose-bound identity inputs;
  - a new typed AT Protocol DID ingress boundary;
  - UI authority correction where local role/current-user inference currently supplements HAL;
  - non-behavioral test, metadata, route-name, header-name, and configuration-key refactors whose observable behavior must remain invariant.
- **Primary intent:** **NEW** `strong-typing-refactor` — mixed product-source and test strong-typing remediation. Phase 0 creates and validates this intent because no current intent permits the full requested scope.
- **Phase 0 bootstrap intent:** existing `create-agent-context-skill`, which owns edits to the intent registry, schema/README, benchmark routing, Governance, and contract validator. The new intent governs Phases 1–9 after its validator passes.
- **Related existing intent:** `test-suite-rationalization` informs invariant-disposition and anti-source-scraping gates, but is not inherited or widened: it forbids `src/**` and currently references a missing active triad.
- **Relevant skills:** `implementation-plan`, `i-vsd`, `grill-me`, `refactor-safely`, `clean-architecture-rules`, `criticality-guardrail`, `auth-patterns`, `dotnet-efcore-guidelines`, `blazor-ui-conventions`, `ip-clean-room`, `agentic-research`, `ast-grep`, `epistemic-mad-review`.
- **Relevant rules:** `.agents/rules/tests.md`, `.agents/rules/work-criticality-matrix.md`, `.agents/rules/auth-trust-boundaries.md`, `.agents/rules/privacy-and-pii.md`, `.agents/rules/application-layer.md`, `.agents/rules/domain.md`, `.agents/rules/efcore-persistence.md`, `.agents/rules/api-controllers.md`, `.agents/rules/api-hateoas.md`, `.agents/rules/blazor-server.md`, `.agents/rules/blazor-client.md`.
- **Primary layers:** Domain, Application, Persistence, Infrastructure, API, BFF, Blazor Client, test projects, agent contract, engineering tools, documentation.
- **Criticality:** Tier 1 Security overall, with Tier 2 Privacy and Tier 0/Tier 3 invariant cohorts embedded in the migration.
- **Complexity:** XL. The work crosses identity, federation, privacy erasure, API authorization, BFF/session semantics, generated-contract boundaries, multi-provider persistence metadata, concurrency tests, Blazor components/services, and repository governance.
- **Evidence packet:** [strong-typing-reflection-remediation-evidence.md](strong-typing-reflection-remediation-evidence.md)
- **Evidence revision:** `sha256:1a2fa2e4cfaca23086cb49648c0111b5be9c68e85ab5abdddee08e20b1f9b157`
- **I-VSD document:** [islamic-value-sensitive-design/i-vsd-strong-typing-reflection-remediation.md](../../../islamic-value-sensitive-design/i-vsd-strong-typing-reflection-remediation.md)
- **I-VSD reviewed input revision:** `sha256:1a2fa2e4cfaca23086cb49648c0111b5be9c68e85ab5abdddee08e20b1f9b157`
- **I-VSD status / disposition:** current / plan-aligned
- **CTO review:** Not reviewed
- **User approval:** Approved by the user on 2026-08-30; expanded on 2026-09-01 to finish the full workstream, including generated product-catalog collation migrations required by Task 6.5
- **Grill-Me intake:** The request explicitly requires complete remediation and no backward compatibility. Repository evidence resolved the material branches: create one dedicated mixed-source intent; retain legitimate compiled metadata; eliminate runtime-name behavior dispatch; preserve separate protocol/identity authorities; introduce only `AtprotoDid`; keep scalar wire/storage values; migrate exact Blazor overlaps without absorbing the paused broad clean-code program.

## 1. Executive Summary

This workstream replaces brittle runtime-name dispatch and raw implementation-token assurance with compile-time symbols, direct public contracts, rendered behavior, compiled metadata, structured parsers, and real-provider tests. It also consolidates platform-user identity derivation without collapsing distinct machine, session, provider, scanner, setup-secret, or receipt schemes.

The refactor is deliberately not a blanket ban on strings, reflection, `DynamicComponent`, or file parsing. Protocol identifiers, database metadata, generated artifacts, policy documents, and compiled architecture relationships remain string- or reflection-based where that is the actual contract. The decision criterion is ownership and executable meaning:

- use direct types and members when C# symbols exist;
- use observable behavior when implementation shape is not the product;
- use compiled reflection when the compiled surface is the invariant;
- parse machine-consumed artifacts through their real parser;
- retain physical database names as explicit metadata;
- type a primitive only when it represents one coherent, validated semantic identity.

The single new Domain value is `AtprotoDid`. It validates live AT Protocol identifier syntax at Domain/federation boundaries, preserves exact case-sensitive value semantics, and does not change JSON or database column representation. Currency, country, email, tenant slug, and admission lookup mirrors remain under their existing owners.

### Intended outcomes

- Renames and contract changes fail at compile time or through focused compiled-contract checks instead of delayed string-reflection failures.
- Critical money, tenant, security, privacy, state-machine, concurrency, HAL, and accessibility invariants remain continuously protected.
- Platform-user identity has one authority; purpose-bound schemes remain isolated.
- Raw C#/Razor/CSS/Markdown token tests are replaced by stronger executable seams.
- Standard HTTP headers and self-valued route constants use framework/compiler-backed names.
- No compatibility adapters, old overloads, reflective fallbacks, or legacy contract aliases survive the cutover.

### Non-goals

- No universal `AppClaimTypes`, `AppRoles`, `CurrencyCode`, `CountryCode`, `EmailAddress`, or generic `Slug` abstraction.
- No ban on compiled architecture reflection, EF model metadata, endpoint metadata, `DynamicComponent` used for actual runtime-selected composition, or structured machine-artifact parsing.
- No public API or generated-client shape change is expected. The only database metadata change is the provider-native binary collation required for exact DID identity semantics; column name/type/length, index, and filter shape remain unchanged.
- No hand edits to OpenAPI, generated clients, migrations, designers, or snapshots.
- No resumption of the paused Blazor service-decomposition, localization, styling, or broad clean-code program beyond exact ownership transferred in this plan.
- No backward-compatibility layer.

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context

The repository graph service was unavailable in this planning session. The following bounded slice was reconstructed from direct source, AST searches, compiled test contracts, and read-only scouts. Phase 0 must refresh it with graph evidence before product edits.

```yaml
Target: strong-typing-reflection-remediation
Callers:
  - API controllers, middleware, and HAL policies deriving platform identity
  - Infrastructure current-user/admin contexts and claims transformations
  - BFF rate-limit, setup-secret, circuit, and session paths reading provider/session claims
  - Application admission contract tests using AdmissionContractRuntime
  - Persistence tests using reflection surfaces, EF metadata, and source reads
  - Blazor component/service tests using DynamicComponent and runtime type names
Callees:
  - canonical ClaimsPrincipal platform-identity and provider-resolution extensions
  - EF Core compiled model metadata and real PostgreSQL repositories
  - bUnit generic rendering and generated HAL contracts
  - route metadata, standard HTTP HeaderNames, and boundary-owned custom headers
  - repository-owned Roslyn analysis for recurrence prevention
ImpactedFlows:
  - authenticated platform identity and admin authorization: Tier 1
  - AT Protocol identity and privacy erasure: Tier 1 / Tier 2
  - ticketing recovery, transfer, add-ons, and fair-return waitlist: Tier 0 / Tier 3
  - tenant isolation and replay fences: Tier 1
  - HAL-driven Blazor actions and accessibility: Tier 4 with security impact
TestCoverage:
  - Event.Domain.UnitTests
  - Event.Application.UnitTests
  - Explore.Infrastructure.Tests
  - Event.Persistence.IntegrationTests
  - Event.API.IntegrationTests
  - Explore.Blazor.IntegrationTests
  - Explore.Blazor.Client.Tests
  - Event.Architecture.Tests
```

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Ticketing recovery options tests are already typed. | `tests/Explore.Secrets.UnitTests/TicketingRecoveryOperatorContractTests.cs` | High | The submitted report is stale for this file. |
| Admission Application tests still dispatch through a custom reflection runtime. | `tests/Event.Application.UnitTests/Contracts/Admissions/Support/AdmissionContractRuntime.cs` and consumers | High | Production contracts/services are public and directly referenceable. |
| Several persistence cohorts still use runtime type/method/property lookup. | Event add-on, recovery, transfer, participant-readiness, admission, and registration-workflow tests listed in the evidence packet | High | Some metadata assertions remain legitimate. |
| Fair-return queue order is source-scraped despite an existing behavioral seam. | `FairReturnWaitlistConcurrencyTests` plus `FairReturnWaitlistRepository.GetAccessAsync` and `FindNextEntryAsync` | High | Real PostgreSQL can prove priority, time, ID, and tenant independence. |
| Blazor tests mix typed and dynamic/reflected patterns. | Tenant directory-operator, participant readiness, waitlist, transfer, and add-on component tests | High | Public components/services have no accessibility blocker. |
| One canonical platform identity authority already exists. | `PlatformIdentityPrincipalExtensions`, `CurrentUserResolutionExtensions`, Quick Reference, auth rules | High | A new generic claims catalog would duplicate behavior. |
| Raw claim readers still bypass or duplicate the authority. | AST inventory and evidence packet | High | Each reader must be classified by purpose before migration. |
| Route names already have bidirectional compiled endpoint coverage. | `RouteNames.cs`, `RouteNameCoverageTests.cs` | High | Converting self-valued literals to `nameof` must not change values. |
| Standard health headers use raw strings. | `HealthCheckResponseWriter.cs` | High | Framework `HeaderNames` covers all standard names. |
| Currency/country/email/slug global value types are not justified. | `CurrencyMetadata`, `Money`, tenant identity validator, scalar persistence policy | High | Preserve scalar wire/storage contracts. |
| DID is a coherent semantic identity candidate. | `AtprotoIdentity`, Actor DID route, privacy-erasure tombstone, AT Protocol official specification | High | Live DID syntax must remain distinct from internal erased tombstones. |
| Existing test-only intent cannot govern product edits. | `.agents/contract/intents.yaml::test-suite-rationalization` | High | Dedicated mixed-source intent is required. |
| Broad Blazor work overlaps a paused workstream. | `dev/pause/blazor-clean-code-refactor/blazor-clean-code-refactor-context.md` | High | Exact transferred files must be marked there; broad decomposition stays paused. |

The full path-by-path evidence and official source register are in the immutable evidence packet.

### 2.2 Existing Implementation By Layer

#### Domain

- `Money` and `CurrencyMetadata` already own normalized, nonnegative integer minor-unit semantics.
- `TenantDirectoryOperatorIdentity` owns capability-specific country/email normalization.
- `AtprotoIdentity` stores DID as a mutable string and validates only immutability during metadata refresh; privacy erasure writes an internal `did:deleted:*` tombstone directly.
- Admission lookup entities and enum mirrors intentionally separate persistence/display metadata from compile-time stable IDs.

#### Application

- `PlatformIdentityPrincipalExtensions` owns the documented GUID platform-user fallback.
- `CurrentUserResolutionExtensions` resolves non-GUID provider subjects through the linked-account query.
- Admission contracts and services are public, but tests still use a prospective red-harness runtime.
- Privacy erasure directly mutates AT Protocol identity fields instead of asking the identity aggregate to perform the transition.

#### Persistence

- Multi-provider model ownership is scalar-first; semantic values are accepted at behavior boundaries while existing columns remain scalar.
- Critical tests combine real PostgreSQL behavior with EF metadata, tenant filters, unique indexes, concurrency tokens, PII absence, and generated migration operations.
- Several tests still locate CLR entities and members by runtime strings even though direct types exist.
- Fair-return ordering behavior is implemented in the repository and can be observed through queue position/allocation.

#### API

- Controllers generally use canonical principal helpers through `ExploreControllerBase`, but diagnostics, onboarding logs, idempotency identity, request logging, and one HAL policy contain direct claim reads.
- Four admin surfaces repeat `[Authorize(Roles = "Admin")]`.
- `RouteNames` is the canonical route catalog, but most self-valued constants duplicate their member names as string literals.
- Route coverage already proves catalog-to-endpoint and endpoint-to-catalog completeness.

#### BFF And Blazor Client

- BFF code reads `sub`, `nameidentifier`, and `sid` for rate partitions, setup sessions, circuit state, and admin transformations. These are often opaque provider/session concerns, not authoritative platform-user resolution.
- `EventBffHeaderNames` already owns privileged custom headers.
- `EventBffKeycloakAuthenticationOptions` repeats child configuration paths under one section.
- Client `AuthStateService` contains unused raw user/tenant claim methods; production uses it only as an authentication-state pass-through for dock persistence.
- `OrganizationMembers` supplements HAL mutation links with local role and raw-current-user inference.
- Blazor test projects already contain strong generic bUnit precedents but retain several reflected/dynamic public-component tests.

#### Tests And Engineering

- Compiled reflection is valid and valuable for route catalogs, public-surface absence, endpoint metadata, authorization parity, EF metadata, PII inventories, and generator contracts.
- Runtime behavior dispatch via `Assembly.GetType(string)`, `Activator`, `MethodInfo.Invoke`, dynamic property reads, or reflected service construction remains widespread in named cohorts.
- Raw source/prose assurance exists beyond the one report example.
- No repository-owned enforcement command currently distinguishes legitimate compiled metadata from prohibited behavior dispatch/source scraping.

### 2.3 Existing Tests And Coverage

Strengths:

- Real PostgreSQL concurrency and tenant tests exist.
- Exact event/state synchronization uses `TaskCompletionSource`; no fixed sleeps are needed.
- HAL/action and accessibility behavior is already covered in several typed Blazor tests.
- Route names have compiled bidirectional endpoint coverage.
- Identity fallback order has focused Infrastructure tests.
- Domain money and privacy-erasure invariants are strongly tested.

Gaps:

- Reflection runtimes delay failures until execution and hide symbol references from IDE/LSP tooling.
- Some “red” tests still test the existence/shape of already-shipped types rather than behavior.
- Source-token tests can pass without executing the query/flow and fail on behavior-preserving refactors.
- Browser-side raw claims and role checks duplicate server/HAL authority.
- DID syntax, safe diagnostics, live-vs-erased representation, and ingress ownership are not one typed contract.
- There is no recurrence gate for behavior-test runtime dispatch and raw source/prose assurance.

### 2.4 Documentation And Contracts

Relevant canonical documents:

- `docs/QUICK_REFERENCE.md`
- `docs/GOVERNANCE.md`
- `docs/TESTING.md`
- `docs/ARCHITECTURE.md`
- `docs/RECORD_CONTRACTS.md`
- `docs/SECURITY-MODEL.md`
- `docs/AUTHORIZATION.md`
- `docs/API.md`
- `docs/BLAZOR.md`
- `docs/DOMAIN.md`
- `.agents/contract/intents.yaml` and `schema.json`
- `.agents/benchmarks/cold-start-tasks.yaml`

External guidance:

- C# `nameof` compile-time symbol naming
- EF Core real-provider testing guidance
- ASP.NET Core standard `HeaderNames`
- bUnit typed component-parameter builders
- .NET value-object guidance
- W3C DID Core and the official AT Protocol DID profile

No package or dependency change is required.

### 2.5 Current Pain Points

- Refactors produce runtime test failures instead of compiler errors.
- IDE reference navigation cannot see reflected type/member names.
- Test-only runtimes duplicate construction, dispatch, conversion, and async unwrapping.
- Some tests assert implementation spelling rather than behavior.
- Identity fallback logic is duplicated with different semantics and orders.
- Browser-side local authority inference conflicts with HAL as the single action source.
- A generic “remove all strings” approach would create more abstractions than invariants.
- The intent registry has no valid mixed-source route for this work.
- The shared dirty worktree increases conflict risk; implementation must patch only owned files on `develop` and record unrelated state.

### 2.6 Unknowns After Investigation

Only implementation-local, non-architectural unknowns remain:

1. **Exact current occurrence counts at implementation start.** The dirty shared worktree may change counts. Phase 0 reruns graph and AST discovery, but the categories, phases, and ownership do not change.
2. **Which existing raw-source tests already have stronger parallel coverage.** Each owning phase resolves this through invariant disposition before deletion; it does not change the target seam or phase.
3. **Whether a Domain/API DID change produces generated or EF model drift.** The selected scalar wire/storage design should produce none. Any unexpected drift is a failure signal requiring root-cause correction, not a compatibility task.

No unknown remains that changes scope, architectural pattern, public contract, or task sequencing.

## 3. Proposed Future State: Behavioral Contract And Scenarios

### Requirement 3.1 — Public behavior and contracts remain stable

The system SHALL preserve existing successful public API, HAL, UI, persistence, and operational behavior unless a scenario below explicitly strengthens malformed or unauthorized input handling.

#### Scenario 3.1A — Behavior-preserving refactor

- **GIVEN** a valid request, persisted state, and authority that succeeds before the refactor
- **WHEN** the same operation executes after the refactor
- **THEN** its status, response shape, HAL relations, state transition, persistence effect, and generated client contract SHALL remain equivalent

#### Scenario 3.1B — No compatibility residue

- **GIVEN** all callers have migrated to the selected typed contract
- **WHEN** the old reflection runtime, overload, or adapter is removed
- **THEN** no fallback, alias, dual reader, deprecated route, or compatibility constructor SHALL remain

### Requirement 3.2 — Platform identity fails closed through one authority

The system MUST derive ambient platform-user identity through one documented fallback and MUST keep provider, session, machine, setup, scanner, support, and receipt schemes purpose-separated.

#### Scenario 3.2A — Conflicting GUID claims

- **GIVEN** an authenticated principal contains different GUID values in multiple fallback positions
- **WHEN** a platform-user operation resolves the caller
- **THEN** the documented priority SHALL select exactly one value consistently across API, authorization, idempotency, logging, and Infrastructure consumers

#### Scenario 3.2B — Non-GUID provider subject

- **GIVEN** an authenticated provider subject is not a platform GUID
- **WHEN** the caller requires a local platform user
- **THEN** the server SHALL resolve the linked local identity through the provider-link authority or fail with `401`; it MUST NOT reinterpret a session or unrelated claim as a user

#### Scenario 3.2C — Purpose-bound principal

- **GIVEN** an API key, setup-secret, scanner, managed-control-plane, AT Protocol session, or erasure-receipt principal
- **WHEN** ambient platform identity is requested outside that scheme's explicit adapter
- **THEN** the request MUST fail closed without widening authority

### Requirement 3.3 — Admin and client action authority does not drift

The system MUST preserve server-side authorization and SHALL expose client mutation affordances only through HAL relations.

#### Scenario 3.3A — Admin endpoint parity

- **GIVEN** one caller who currently satisfies the Admin endpoint gate and one who does not
- **WHEN** each invokes a migrated admin operation
- **THEN** the authorized caller SHALL remain authorized and the other SHALL receive the same fail-closed `401` or `403` outcome

#### Scenario 3.3B — HAL-only client mutation

- **GIVEN** a rendered resource without a mutation relation but with local role, status, or current-user facts that might suggest eligibility
- **WHEN** the client renders actions
- **THEN** the action MUST remain absent

### Requirement 3.4 — Query, ordering, tenancy, and concurrency remain observable

Critical persistence behavior MUST be proven through the real repository/provider rather than source text or reflected invocation.

#### Scenario 3.4A — Stable fair-return ordering

- **GIVEN** equivalent queued entries with controlled priority, enqueue time, stable identifier, and tenant
- **WHEN** queue position and allocation are evaluated
- **THEN** order SHALL be priority descending, enqueue time ascending, and stable identifier ascending

#### Scenario 3.4B — Cross-tenant independence

- **GIVEN** an otherwise equivalent higher-priority entry in another tenant
- **WHEN** a tenant's queue is evaluated
- **THEN** the other tenant's entry MUST have no effect

#### Scenario 3.4C — Concurrent winner

- **GIVEN** competing operations synchronized before the decisive write
- **WHEN** the release signal allows them to proceed
- **THEN** the existing single-winner, replay, fence, and bounded-loser outcomes MUST remain atomic and deterministic

### Requirement 3.5 — Live AT Protocol DIDs are typed without changing wire or storage

The system SHALL accept syntactically valid AT Protocol DIDs as exact case-sensitive identities, reject malformed live identifiers at ingress, and keep external JSON and database representation scalar.

#### Scenario 3.5A — Valid live DID

- **GIVEN** a valid generic AT Protocol DID using a supported or syntactically valid future method
- **WHEN** it enters a federation/authentication Domain operation
- **THEN** it SHALL parse once, preserve exact value and ordinal equality, and emit the same scalar value at wire/persistence egress

#### Scenario 3.5B — Invalid or oversized DID

- **GIVEN** a DID with invalid prefix/method syntax, forbidden query/fragment/whitespace/control characters, invalid terminal delimiter, or length above the protocol bound
- **WHEN** it reaches the trusted ingress boundary
- **THEN** it MUST fail before identity, authorization, repository lookup, or logging side effects

#### Scenario 3.5C — Privacy-erasure tombstone

- **GIVEN** a live AT Protocol identity is erased
- **WHEN** the authority-first erasure transaction applies
- **THEN** the aggregate SHALL replace the live identifier with its internal erased tombstone, clear provider metadata, preserve replay/anti-resurrection behavior, and never treat the tombstone as a live DID

### Requirement 3.6 — Typed Blazor tests retain authority and accessibility behavior

Blazor component/service tests SHALL use compile-time components, parameters, models, and service contracts when those symbols exist, while preserving exact HAL and accessibility outcomes.

#### Scenario 3.6A — Typed component rename

- **GIVEN** a directly referenced component or parameter is renamed
- **WHEN** the solution compiles
- **THEN** affected tests SHALL fail at compile time or participate in the symbol rename instead of failing through a runtime string

#### Scenario 3.6B — Read-only and editable resources

- **GIVEN** identical resource data with and without the exact edit relation
- **WHEN** the component renders
- **THEN** read-only fields, mutation controls, focus, status, and alert semantics SHALL match the server-advertised affordance

### Requirement 3.7 — Test evidence is truthful and recurrence is blocked

Behavior tests MUST NOT execute production behavior through runtime-name dispatch or assert raw C#/Razor/CSS/Markdown tokens. Compiled metadata and structured machine artifacts MAY remain when they are the contract.

#### Scenario 3.7A — Prohibited behavior dispatch

- **GIVEN** a behavior test attempts reflected construction, invocation, dynamic property access, or string-selected production type resolution
- **WHEN** the repository assurance audit runs
- **THEN** it SHALL fail with the exact prohibited category and source location

#### Scenario 3.7B — Legitimate compiled metadata

- **GIVEN** an architecture or persistence test inspects a compiled type, endpoint, EF model, generated schema, policy document, or machine manifest
- **WHEN** the assurance audit runs
- **THEN** it SHALL remain accepted by semantic category without a historical file allowlist

### Requirement 3.8 — Generated and provider artifacts do not drift

The refactor MUST NOT hand-edit or silently change generated API, client, migration, designer, snapshot, or provider artifacts.

#### Scenario 3.8A — Expected zero drift

- **GIVEN** the selected scalar wire/storage design
- **WHEN** OpenAPI/client and EF model checks run
- **THEN** generated artifacts SHALL remain unchanged

#### Scenario 3.8B — Unexpected drift

- **GIVEN** a generator or pending-model check reports a change
- **WHEN** the owning phase evaluates it
- **THEN** implementation MUST stop, correct the source model/design, and revalidate; it MUST NOT patch generated output or add a compatibility shim

## 4. Non-Negotiable Constraints

1. Repository dependency direction remains Domain → Application → Infrastructure/Persistence → API; Blazor remains isolated behind generated contracts/BFF.
2. Platform-user identity keeps the exact `sub -> nameidentifier -> sid -> internal_user_id` GUID order.
3. Purpose-bound schemes do not become ambient platform identity.
4. HAL relations remain the sole client mutation-affordance authority.
5. Repositories return entities, not DTOs or `IQueryable`.
6. Validators remain manually instantiated.
7. Tenant and soft-delete filters remain enabled unless an exact, tested named-filter exception applies.
8. Critical money, state-machine, concurrency, tenant, security, privacy, HAL, BFF, provider, migration, and protocol tests fail closed until stronger replacements pass.
9. Tests contain no fixed sleeps, timing-luck polling, mock-mirroring, framework cancellation assertions, or raw product-source/prose assurance.
10. Standard HTTP names use framework constants; custom protocol names remain with their owning boundary.
11. Database physical names, shadow properties, annotations, and structured protocol tokens remain explicit strings when they are the contract.
12. EF migrations, designers, snapshots, OpenAPI, and generated clients are never hand-edited.
13. No package or dependency is added unless separately justified and license-approved.
14. No secrets, tokens, connection strings, claim values, DIDs, PII, or provider payloads enter logs/evidence.
15. No backward-compatibility alias, adapter, overload, reader, route, DTO, or test is created.
16. Existing unrelated dirty-tree changes are not reverted, rewritten, staged, or claimed.

## 5. Architecture And Design Decisions

### Decision 5.1 — Add a dedicated mixed-source intent through the existing agent-context owner

- **Decision:** Use `create-agent-context-skill` as the Phase 0 bootstrap owner, create `strong-typing-refactor` as the primary cross-cutting intent for Phases 1–8, and copy the relevant invariant-disposition gates into it. Do not widen or route through the stale test-only intent.
- **Why:** The current test intent forbids product source and references missing active docs.
- **Alternatives considered:** widen `test-suite-rationalization`; use an undocumented fallback forever; split into disconnected test and product plans.
- **Rejected because:** widening violates its contract, an undocumented fallback is not repeatable, and split plans would lose one invariant/replacement sequence.
- **Consequences:** Phase 0 updates intent registry, benchmark routing, Governance decision guidance, and extends the existing canonical testing taxonomy with runtime-dispatch and automated-enforcement rules.
- **Files/layers:** `.agents/contract`, `.agents/benchmarks`, `docs/GOVERNANCE.md`, `docs/TESTING.md`, architecture tests.

### Decision 5.2 — Classify strings and reflection by semantic ownership

- **Decision:** Use four assurance seams: direct typed behavior, compiled metadata, structured machine artifacts, and physical/protocol metadata. Runtime-name behavior dispatch and raw product-source/prose assertion are prohibited.
- **Why:** A blanket ban destroys valid architecture and persistence tests; a syntax-only cleanup does not improve truthfulness.
- **Alternatives considered:** ban all reflection; replace every literal with a global constant; leave the current mixed style.
- **Consequences:** Each removed cohort needs invariant disposition and replacement-first sequencing.

### Decision 5.3 — Keep one platform identity authority, with purpose-specific adapters

- **Decision:** Platform GUID identity remains Application-owned. API/Infrastructure ambient-user callers converge on it. BFF helpers expose purpose-specific opaque provider subject/session values and never claim to resolve platform identity. Browser clients use BFF/API-confirmed state and HAL, not authority claims.
- **Why:** Blazor cannot reference Application, and provider/session claims are not always platform GUIDs.
- **Alternatives considered:** move a universal helper to a shared project; create `AppClaimTypes`; duplicate the fallback in each layer.
- **Rejected because:** a universal helper would collapse trust purposes; shared spelling alone does not create shared semantics.
- **Consequences:** Existing internal/admin/machine/BFF catalogs remain separate. Raw claim readers are classified before migration.

### Decision 5.4 — Use one API-local named Admin policy

- **Decision:** Replace repeated role attributes with one API-local policy that preserves the exact current `Admin` role requirement; MediatR/Cerbos/HAL remain the resource/action authority.
- **Why:** This removes repetition without pretending endpoint roles and Domain authorization actions are the same concept.
- **Alternatives considered:** global `AppRoles`; remove coarse gates; use client roles.
- **Consequences:** HTTP authorization parity must prove no cohort widening or narrowing.

### Decision 5.5 — Use compiler/framework names at protocol boundaries

- **Decision:** Route constants whose literal exactly equals their member name use `nameof`; intentional field/value divergences remain explicit and retain their route value. Standard headers use `Microsoft.Net.Http.Headers.HeaderNames`; custom headers stay in boundary catalogs; configuration child paths use section-local property names/binding.
- **Why:** These changes improve refactor safety without inventing a global string registry.
- **Consequences:** Route values, operation IDs, HAL links, header output, and configuration precedence remain byte-for-byte/semantically stable.

### Decision 5.6 — Introduce only `AtprotoDid`

- **Decision:** Add a small immutable live-DID value that follows official AT Protocol generic DID syntax and the 2048-character limit. It preserves exact case-sensitive value, does not normalize method-specific identity, has no implicit conversion, and has bounded/redacted diagnostic representation.
- **Why:** DID is a coherent immutable federation identity. Other submitted primitive candidates already have adequate scoped owners or multiple incompatible meanings.
- **Alternatives considered:** restrict to `did:plc`/`did:web`; use a universal DID library; add all submitted value objects; leave DID unchecked.
- **Rejected because:** syntax validity and method support are separate; a dependency is unnecessary; global primitive wrapping adds churn without invariant value.
- **Consequences:** live DID enters Domain behavior as `AtprotoDid`; entity storage, JSON, routes, and EF columns remain string; erased tombstones are internal aggregate state, not live values.

### Decision 5.7 — Preserve scalar persistence and generated contracts

- **Decision:** Domain methods/factories accept semantic values and assign scalar owner fields. Do not introduce broad EF converters, owned/complex types, or generated client types.
- **Why:** This matches the repository semantic-value persistence contract and avoids schema/OpenAPI churn.
- **Consequences:** pending-model and generated-artifact checks are expected to report zero diff.

### Decision 5.8 — Replace source assurance at the closest executable seam

- **Decision:** Query behavior uses real providers; API behavior uses HTTP/endpoint metadata; UI uses bUnit/rendered semantics; architecture uses compiled reflection/IL or a Roslyn rule; machine artifacts use their production parser; prose assurance is deleted.
- **Why:** The test must fail when behavior/contract breaks, not when formatting changes.
- **Consequences:** Phase 8 adds a repository-owned Roslyn assurance command with category rules and no historical file allowlist.

### Decision 5.9 — Coordinate exact Blazor ownership

- **Decision:** This workstream owns report-cited typed test migrations, removal of the shallow client auth-state wrapper/raw unused methods, and exact HAL-authority fixes discovered by the audit. It supersedes paused Tasks 16.1 and 16.6 with purpose-specific helpers and deletion of `AuthStateService`; paused Task 16.7 and Phase 6A remain paused because `DynamicAuthSchemeManager` provider configuration/concurrency is outside this report. Broad component/service decomposition remains paused.
- **Why:** The user requested complete remediation, but duplicate workstreams would create conflicting designs.
- **Consequences:** Implementation updates the paused workstream's context/task ownership when shared files are transferred.

## 6. Implementation Phases

### Phase 0 — Governance And Assurance Classification

- **Goal:** Establish a valid contribution contract, extend the existing executable-seam taxonomy with runtime-dispatch rules, capture a bounded graph/AST baseline, and design enforcement before product changes.
- **Depends on:** User approval.
- **Relevant files:**
  - existing `.agents/contract/intents.yaml`, `.agents/contract/schema.json`, `.agents/contract/README.md`
  - existing `.agents/benchmarks/cold-start-tasks.yaml`
  - existing `docs/GOVERNANCE.md`, `docs/TESTING.md`, `docs/QUICK_REFERENCE.md`
  - new/updated `tests/Event.Architecture.Tests/*StrongTyping*`
  - existing workstream evidence/plan/context/tasks
- **Related guidance:** `create-agent-context-skill` intent, implementation-plan, refactor-safely, criticality-guardrail, tests and work-criticality rules.
- **Acceptance criteria:**
  - Phase 0 is authorized by `create-agent-context-skill`, and the new intent has exact scope, skills, rules, tests, docs, safety gates, and forbidden moves;
  - stale missing-triad references are removed from canonical routing without creating duplicate active plans;
  - the scoped validator selects the new intent deterministically and the unscoped validator proves every intent reference, including removal of stale missing-triad references;
  - graph/AST evidence classifies current candidates without a permanent historical allowlist;
  - unrelated dirty-tree files are recorded and untouched.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revert only the new contract/docs/benchmark slice if routing is ambiguous. No product code has changed.

### Phase 1 — AT Protocol DID Semantic Boundary

- **Goal:** Make live DID syntax and equality a Domain invariant while preserving scalar wire/storage and privacy-erasure tombstones.
- **Depends on:** Phase 0.
- **Relevant files:**
  - new `src/Explore.Domain/ValueObjects/AtprotoDid.cs`
  - existing `src/Explore.Domain/AtprotoIdentity.cs`
  - every graph/LSP-discovered `AtprotoIdentity` construction, refresh, direct DID mutation, repository, fixture, seed, benchmark, serializer, and test caller across `src/**`, `tests/**`, and `tests/Event.Benchmarks/**`
  - existing `tests/Event.Domain.UnitTests/Entities/AtprotoIdentityLifecycleTests.cs`
  - existing `tests/Event.Domain.UnitTests/PrivacyErasureContractTests.cs`
  - `docs/RECORD_CONTRACTS.md`, `docs/DOMAIN.md`
- **Related guidance:** clean-architecture-rules, record contracts, privacy rule, auth patterns, official AT Protocol DID specification.
- **Acceptance criteria:**
  - Scenario 3.5 valid/invalid/erasure behavior is red-anchored before production changes;
  - live DID construction is explicit, exact, case-sensitive, bounded, and has no implicit conversion;
  - Domain entity creation/refresh accepts the semantic value and the aggregate owns live-DID/erasure mutation;
  - every compile-time caller migrates atomically in this phase so the solution-wide Release build passes without a string overload, public setter, or compatibility adapter;
  - scalar `Did` persistence/wire shape and indexes remain unchanged;
  - Application/Persistence/test/benchmark callers use the new factory/transition or exact scalar egress while their behavioral hardening remains assigned to later owning phases;
  - the aggregate erasure transition never parses its internal tombstone as a live DID;
  - diagnostic/log output does not reveal a raw DID.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove the new value and restore the last typed Domain boundary only if wire/model drift cannot be eliminated without changing the approved architecture. Do not retain dual string/value overloads.

### Phase 2 — Platform Identity Authority

- **Goal:** Converge Application/Infrastructure ambient platform-user resolution and safe identity diagnostics on one authority.
- **Depends on:** Phase 1.
- **Relevant files:**
  - new `src/Explore.Application/Authentication/PlatformIdentityClaimTypes.cs`
  - existing `PlatformIdentityPrincipalExtensions.cs`, `CurrentUserResolutionExtensions.cs`
  - existing `src/Explore.Infrastructure/Identity/AdminContext.cs`, `AdminClaimsTransformation.cs`, `UserContext.cs`
  - existing `src/Explore.Infrastructure/Services/CurrentUserService.cs`
  - bounded AT Protocol Infrastructure adapters that parse or emit live DIDs
  - existing `src/Explore.Secrets/Providers/AuditingSecretProviderDecorator.cs`
  - existing Infrastructure identity/security tests
  - `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`
- **Related guidance:** auth-patterns, criticality-guardrail, privacy rules, API controller rules.
- **Acceptance criteria:**
  - hostile-principal tests cover conflicting, malformed, unauthenticated, provider-linked, and excluded schemes;
  - all ambient platform-user callers that may reference Application use the canonical resolver;
  - standard JWT claim names and the internal platform claim name have one spelling owner;
  - provider/session/diagnostic reads retain explicit purpose and never masquerade as platform identity;
  - Infrastructure AT Protocol adapters parse live DIDs at ingress and emit exact scalar values without logging them;
  - Infrastructure/Secrets logs expose only bounded reason/presence facts, not raw claim values or claim inventories.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1`
- **Rollback / failure handling:** A caller whose scheme semantics cannot use the platform resolver stays purpose-specific with a named test and reason. It does not receive a fallback adapter.

### Phase 3 — API Authorization And Protocol Literals

- **Goal:** Remove repeated API role/header/route literals while proving unchanged authorization, endpoint identity, HAL resolution, and HTTP output.
- **Depends on:** Phase 2.
- **Relevant files:**
  - new API-local Admin policy/name owner under `src/Explore.API/Authentication/`
  - four report-cited controllers with raw Admin roles
  - `src/Explore.API/Controllers/ActorController.cs` and its DID request path
  - API `SupportAccessLinkPolicy`, idempotency identity, request logging, onboarding, and diagnostics files
  - `src/Explore.API/Hateoas/RouteNames.cs`
  - `src/Explore.ServiceDefaults/HealthChecks/HealthCheckResponseWriter.cs`
  - API authorization, route coverage, health, OpenAPI, and generated-contract tests
- **Related guidance:** API controller/HATEOAS rules, auth patterns, HeaderNames official docs.
- **Acceptance criteria:**
  - Scenario 3.3 Admin allow/deny behavior is characterized before structural policy changes, and malformed DID ingress is red-anchored before the API parser changes;
  - exact self-valued route constants use `nameof`, intentional divergent field/value pairs remain explicit, and all route values/endpoint mappings remain identical;
  - standard health header names use framework constants and the custom health header remains locally owned;
  - the four Admin surfaces use one API-local policy and retain defense-in-depth authorization;
  - API ambient platform identity, idempotency, support, and production logs use canonical/purpose-specific authorities and fail closed;
  - the public DID route parses once and preserves its existing scalar route/OpenAPI contract;
  - API route/HAL/header behavior remains stable and exact generator inputs are handed to Phase 8 for byte-level OpenAPI/client determinism.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Any route, operation ID, generated client, or authorization cohort drift blocks the phase. Restore the previous source slice and correct the policy/catalog source; never patch generated output.

### Phase 4 — BFF Claim And Configuration Boundaries

- **Goal:** Replace repeated BFF provider/session claim spelling with purpose-specific helpers while preserving token, session, and configuration behavior; unauthenticated setup attempts intentionally harden to a stable network partition so caller-controlled cookie rotation cannot bypass the sole limiter.
- **Depends on:** Phase 2 and Phase 3.
- **Relevant files:**
  - new `src/Event.Web.BffHosting/Security/EventBffPrincipalExtensions.cs`
  - existing `src/Event.Web.BffHosting/Security/EventBffHeaderNames.cs`, request enrichment, and sanitizer
  - existing `src/Event.Web.BffHosting/Authentication/EventBffKeycloakAuthenticationOptions.cs`
  - existing `src/Explore.Blazor/Services/TokenCircuitHandler.cs`
  - existing `src/Explore.Blazor/Extensions/RateLimitingExtensions.cs`, `BffSetupSecretEndpoints.cs`, admin-claim transformation, and session refresh
  - `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`
- **Related guidance:** blazor-bff patterns, blazor-ui conventions, auth patterns, HAL rule.
- **Acceptance criteria:**
  - BFF helpers name their exact subject/session/rate-partition purpose and preserve opaque values;
  - custom privileged headers remain under the BFF catalog and standard headers use framework names;
  - repeated Keycloak child configuration paths are section/property-bound without changing fallback precedence;
  - authenticated rate partitions remain purpose-bound, while unauthenticated setup attempts use stable effective remote IP and ignore arbitrary cookie rotation;
  - BFF logs do not enumerate or echo claim types/values.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Preserve BFF token secrecy, fail-closed rate limiting, and antiforgery/session behavior. If a client needs missing authority metadata, record the missing HAL/status contract instead of restoring claim inspection.

### Phase 5 — Typed Application Contract Tests

- **Goal:** Delete the admission/location reflection execution layer after direct typed contracts protect every invariant.
- **Depends on:** Phases 0 and 1.
- **Relevant files:**
  - `tests/Event.Application.UnitTests/Contracts/Admissions/**`
  - `tests/Event.Application.UnitTests/Contracts/Admissions/Support/AdmissionContractRuntime.cs`
  - reflection-backed admission port fake/support files
  - `tests/Event.Application.UnitTests/Features/Locations/Commands/LocationAddressWriteContractTests.cs`
  - `src/Explore.Application/Services/PrivacyErasureApplier.cs`
  - bounded AT Protocol Application requests/handlers/ports that accept or emit live DIDs
  - public Application admission contracts/services and Domain values only if a production testability defect is proven
  - `docs/TESTING.md`, `docs/SECURITY-MODEL.md`
- **Related guidance:** tests rule, CQRS/Clean Architecture, refactor-safely.
- **Acceptance criteria:**
  - every admission runtime consumer directly constructs the shipped request/result/service/interface;
  - provider neutrality, constructor dependencies, outcomes, tenant authority, and async behavior remain covered without dynamic conversion/invocation;
  - positive location behavior directly uses `GeoCoordinate` and `Location`;
  - only narrow compiled negative-surface checks remain for forbidden setters/methods;
  - Application federation/authentication behavior is adversarially verified around the Phase 1 mechanical cutover, and privacy erasure proves the aggregate erasure transition;
  - `AdmissionContractRuntime` and obsolete support adapters are deleted after replacements pass.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep the old runtime only until the last typed replacement passes in the same phase; never merge both as permanent parallel harnesses.

### Phase 6 — Persistence Behavior And Metadata

- **Goal:** Convert reflected aggregate/repository behavior and source-scraped ordering to typed real-provider tests while retaining legitimate compiled database metadata.
- **Depends on:** Phases 0, 1, and 5.
- **Relevant files:**
  - `EventAddOnPersistenceTests.cs`
  - `TicketingLifecycleRecoveryInvariantTests.cs`
  - `TicketTransferConcurrencyTests.cs`
  - `ParticipantAdmissionEligibilityPersistenceTests.cs`
  - `AdmissionCheckInPersistenceRedTests.cs`
  - `AdmissionTicketPersistenceRedTests.cs`
  - `RegistrationWorkflowPersistenceTests.cs`
  - `FairReturnWaitlistConcurrencyTests.cs`
  - bounded AT Protocol repository queries/configurations that compare or persist live DID scalars
  - associated reflection surfaces/source-read helpers
  - owning Domain entities, repositories, configurations only when a public typed seam is missing
  - `docs/TESTING.md`, `docs/RECORD_CONTRACTS.md`
- **Related guidance:** dotnet-efcore, tests, criticality guardrail, real-provider official guidance.
- **Acceptance criteria:**
  - add-on, recovery, transfer, readiness, and admission behavior uses direct types/repositories;
  - CLR metadata uses `typeof`/`nameof` where practical;
  - shadow state, physical names, annotations, generated columns, constraints, and migration operations retain explicit metadata strings;
  - fair-return queue position/allocation proves priority/time/ID order and tenant independence in PostgreSQL;
  - concurrency tests subscribe to exact gates before release and use no sleeps;
  - money overflow, replay, PII absence, tenant filters, fences, credential secrecy, and state transitions retain equal or stronger coverage;
  - DID lookups compare the exact semantic value while source configuration preserves the scalar column/index/filter contract;
  - application-provider migrations and snapshots are generated through `dotnet ef`, never hand-edited, to record PostgreSQL `C`, SQLite `BINARY`, SQL Server `Latin1_General_100_BIN2`, and MariaDB/MySQL `ascii_bin` collations;
  - a five-provider product-catalog model contract in this phase proves zero pending model change after generation and explicitly excludes DataProtection/privacy-authority catalogs that do not contain AT Protocol identity state.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** A metadata assertion is removed only after its invariant is retained elsewhere. Provider/model drift blocks the phase and is corrected at the owning source.

### Phase 7 — Generated Contract Determinism

- **Goal:** Prove route, operation, DTO, and generated client contracts remain deterministic and scalar after API/federation changes.
- **Depends on:** Phases 1 and 3.
- **Relevant files:**
  - `tests/Explore.GeneratedContracts.Tests/**`
  - `schemas/openapi_islamu-event.json` as generator-owned evidence
  - `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` as generator-owned evidence
  - `docs/API_CONTRACT_INVENTORY.md` as generator-owned evidence
  - `eng/tools/Explore.ApiContractInventory/**`
  - existing NSwag/MSBuild/Roslyn generator inputs
- **Related guidance:** record contracts, API rules, generated-client ownership.
- **Acceptance criteria:**
  - pre-phase hashes capture the OpenAPI schema, generated client, and API contract inventory;
  - `dotnet msbuild src/Explore.API/Explore.API.csproj -target:GenerateOpenApiDocuments -property:Configuration=Release` refreshes the API-owned schema from the latest compiled API;
  - `dotnet msbuild src/Explore.Blazor.Client/Explore.Blazor.Client.csproj -target:GenerateApiClient -property:Configuration=Release` consumes that schema and runs the NSwag/Roslyn pipeline;
  - `dotnet run --project eng/tools/Explore.ApiContractInventory/Explore.ApiContractInventory.csproj --configuration Release --no-launch-profile` regenerates the inventory after client generation;
  - generated-contract tests prove DID remains a string on the wire and route/operation identifiers retain their existing values;
  - post-generation schema/client/inventory hashes are byte-identical to the pre-phase baseline;
  - no generated artifact is hand-edited and no compatibility DTO/route appears.
- **Phase-end verification:**
  - `dotnet msbuild src/Explore.API/Explore.API.csproj -target:GenerateOpenApiDocuments -property:Configuration=Release`
  - `dotnet msbuild src/Explore.Blazor.Client/Explore.Blazor.Client.csproj -target:GenerateApiClient -property:Configuration=Release`
  - `dotnet run --project eng/tools/Explore.ApiContractInventory/Explore.ApiContractInventory.csproj --configuration Release --no-launch-profile`
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.GeneratedContracts.Tests/Explore.GeneratedContracts.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Fix the server/generator source and regenerate. Unexpected intentional wire drift requires plan/I-VSD/release re-baselining before proceeding.

### Phase 8 — Typed Blazor Contract Tests

- **Goal:** Make public component/service test dependencies compile-time visible while preserving HAL, accessibility, async, and lifecycle behavior.
- **Depends on:** Phases 4 and 7.
- **Relevant files:**
  - tenant directory-operator component/service tests
  - participant-readiness component tests
  - fair-return waitlist component tests
  - ticket-transfer component tests
  - report-listed add-on tests as the retained typed precedent
  - bounded remaining test-only `DynamicComponent`/runtime-type uses discovered in Phase 0
  - client `AuthStateService`, `IAuthStateService`, dock persistence, DI registration, and tests
  - `OrganizationMembers.razor.cs` and matching component tests
  - exact production components/services only when a public test seam is missing
  - paused Blazor workstream context/tasks
- **Related guidance:** bUnit official guidance, blazor-ui conventions, accessibility/HAL rules.
- **Acceptance criteria:**
  - public directly referenceable components use generic rendering and typed parameter selectors;
  - public services/models are directly instantiated/invoked/read;
  - waitlist tests execute HAL-driven rendered behavior instead of existence checks;
  - redundant transfer existence tests are removed;
  - the shallow client auth-state wrapper/raw user-and-tenant claim methods are removed and dock persistence uses established authentication state;
  - organization-member mutations render solely from HAL relations, not local role/current-user inference;
  - focus, live region, alert, read-only, conflict, and exact relation behavior remains covered;
  - legitimate runtime-selected composition remains classified and does not gain public API solely for testing.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** If a component is intentionally non-public/runtime-selected, test it through its owning rendered parent or compiled descriptor. Do not restore string type lookup as a fallback.

### Phase 9 — Recurrence Guard And Release Closure

- **Goal:** Migrate remaining raw source/prose assurance, add a repository-owned semantic Roslyn audit, close documentation/I-VSD mappings, and prepare governed release metadata.
- **Depends on:** Phases 0–8.
- **Relevant files:**
  - new `eng/tools/Explore.AssuranceAudit/**` or the smallest fitting existing Roslyn tool
  - existing `Event.Architecture.Tests` synthetic rule/compiled-boundary tests
  - remaining raw C#/Razor/CSS/Markdown assurance candidates classified in Phase 0
  - structured machine-artifact tests that remain
  - canonical docs updated by the owning migration
  - `docs/releases/changes/<generated Change-Id>.yaml`
  - workstream triad and I-VSD report
- **Related guidance:** ast-grep, IP clean-room, test governance, release engineering, epistemic MAD review.
- **Acceptance criteria:**
  - the audit rejects runtime reflective behavior dispatch, string-selected production types, and raw source/prose assurance in behavior tests;
  - it permits compiled metadata and real structured parsers by semantic category, not file allowlist;
  - remaining prohibited candidates are migrated at their owning executable seam;
  - generated/OpenAPI/EF checks show expected zero drift;
  - Tier 1/Tier 2 review evidence contains no identity attribution and maps every validated defect to a concrete invariant test;
  - final docs and I-VSD mappings match implementation;
  - the release fragment classifies Breaking, Security, Migration, Configuration, OpenAPI, and Operator impact;
  - the final Conventional Commit composition includes the generated `Change-Id`; an actual commit occurs only with explicit user authorization.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep the assurance command non-blocking until all current prohibited candidates are migrated, then enable it atomically. Do not introduce a permanent baseline/allowlist of debt.

## 7. Testing Strategy

### Invariant anchors

- **Domain:** DID syntax/value equality, aggregate live-vs-erased transition, money and privacy invariants.
- **Infrastructure/API:** hostile principal matrices, provider-linked identity, excluded schemes, admin allow/deny parity, safe logs.
- **Persistence:** PostgreSQL ordering, tenant isolation, replay, fences, state machines, PII/credential absence, model metadata.
- **Blazor:** exact HAL relations, read-only/edit behavior, focus/live semantics, deterministic async completion.
- **Architecture/tooling:** contribution routing, compiled boundary enforcement, semantic source-assurance categories, route catalog invariance.

### High-leverage adversarial scenarios

- conflicting GUID claims and non-GUID provider subjects;
- purpose-bound principals presented to ambient identity;
- cross-tenant queue entries and replay IDs;
- simultaneous one-winner allocation/transfer/recovery operations;
- malformed/oversized/case-invalid DID input and privacy-erasure tombstones;
- local role/status facts without HAL relations;
- runtime reflection/source-scraping reintroduction.

### Test quality rules

- replacement tests must fail for the original regression, not implementation spelling;
- no mock call-count mirrors for internal repositories/services;
- no EF provider fakes for provider-sensitive queries;
- no sleeps or time polling;
- no raw source/prose assertions;
- every removed cohort has an invariant disposition before deletion;
- phase-level project gates run after the owning phase, with one Release build and one selected project test.

### Phase verification lane

| Phase | Selected project |
|---|---|
| 0 | `Event.Architecture.Tests` |
| 1 | `Event.Domain.UnitTests` |
| 2 | `Explore.Infrastructure.Tests` fast non-runtime lane |
| 3 | `Event.API.IntegrationTests` |
| 4 | `Explore.Blazor.IntegrationTests` |
| 5 | `Event.Application.UnitTests` |
| 6 | `Event.Persistence.IntegrationTests` |
| 7 | `Explore.GeneratedContracts.Tests` |
| 8 | `Explore.Blazor.Client.Tests` |
| 9 | `Event.Architecture.Tests` for final cross-layer enforcement |

The Phase 9 architecture run is a deliberate repeat: Phase 0 validates intent/routing scaffolding; Phase 9 validates the completed compiled boundaries and assurance tool.

## 8. Documentation, Configuration, And Operations Impact

### Documentation

Expected updates:

- `docs/GOVERNANCE.md` — intent decision route and semantic string/reflection ownership.
- `docs/TESTING.md` — executable-seam taxonomy, runtime dispatch prohibition, source/prose policy, recurrence command.
- `docs/RECORD_CONTRACTS.md` — `AtprotoDid` and scalar owner persistence.
- `docs/DOMAIN.md` — live DID and erased-tombstone ownership.
- `docs/SECURITY-MODEL.md` / `docs/AUTHORIZATION.md` — platform identity vs purpose-bound claims and API Admin policy.
- `docs/BLAZOR.md` — BFF opaque subject/session helpers, client authority removal, exact transferred work.
- `docs/API_CHANGELOG.md` only if implementation produces an intentional public wire change; expected disposition is not applicable.
- paused Blazor context/tasks — ownership transfer, not architectural duplication.

### Configuration

- No new environment variable, secret, appsettings key, or Infisical path.
- Existing BFF authentication key precedence remains unchanged.
- No configuration migration or alias.

### Operations

- No new service, job, health probe, metric, or deployment resource.
- Logs become safer by removing raw claim inventories/values and raw DID diagnostics.
- No database reset, migration, or operator action is expected.

### 8.1 Release And Changelog Strategy

Classification: **Tier 2 change fragment**, because the work touches security identity, privacy-bearing federation identity, authorization gates, and breaking internal source contracts even though public wire/schema is expected to remain stable.

Final implementation action:

```bash
dotnet run --project eng/release/src/ISLAMU.ReleaseEngineering/ISLAMU.ReleaseEngineering.csproj -- \
  create-change \
  --target develop \
  --type refactor \
  --scope architecture \
  --title "Remove reflection and stringly typed assurance debt" \
  --summary "Typed contracts and executable behavior replace runtime-name dispatch and raw source assurance."
```

The generated fragment must record:

- Breaking: documented internal source/test cutover; no compatibility layer.
- Security: documented identity/authorization boundary hardening.
- Migration: not applicable unless an unexpected model delta is approved.
- Configuration: not applicable.
- OpenAPI: not applicable with zero-diff evidence.
- Operator: not applicable unless implementation discovers an operational behavior change.

Expected commit subject for composition:

```text
refactor(architecture): replace stringly typed assurance seams
```

Required terminal footer: generated `Change-Id: CHG-...`. An actual commit is outside this plan unless the user explicitly authorizes it.

## 9. Islamic Value-Sensitive Design And Moral Boundaries

I-VSD report: [i-vsd-strong-typing-reflection-remediation.md](../../../islamic-value-sensitive-design/i-vsd-strong-typing-reflection-remediation.md)

| I-VSD IDs | Finding / mitigation | Scenario mapping | Task mapping | Disposition |
|---|---|---|---|---|
| `IVSD-F001` / `IVSD-M001` | False assurance from runtime-name/source-shaped tests | 3.4, 3.7 | 5.1–5.5, 6.1–6.5, 8.1–8.6, 9.1–9.3 | Implement |
| `IVSD-F002` / `IVSD-M002` | Duplicate identity derivation | 3.2, 3.3 | 2.1–2.4, 3.1–3.5, 4.1–4.2, 8.4–8.5 | Implement |
| `IVSD-F003` / `IVSD-M003` | Over-modeling primitive strings | 3.5, 3.8 | 1.1–1.3, 2.4, 3.4, 5.5, 6.5, 7.1–7.2 | Implement narrow DID only |
| `IVSD-F004` / `IVSD-M004` | Critical invariant loss during cleanup | 3.1, 3.4, 3.6, 3.7 | Every replacement task and each phase gate | Fail-closed gate |
| `IVSD-F005` / `IVSD-M005` | Missing accountable contribution route | 3.7, 3.8 | 0.1–0.3, 9.1–9.4 | Implement dedicated intent |
| `IVSD-F006` / `IVSD-M006` | HAL/accessibility loss during typed UI migration | 3.3, 3.6 | 8.1–8.6 | Implement |

### Scenario-To-Task Traceability

| Scenario | Owning tasks |
|---|---|
| 3.1A Behavior-preserving refactor | 1.3, 2.2–2.4, 3.2–3.5, 4.2, 5.1–5.5, 6.1–6.5, 7.1–7.2, 8.1–8.6, all phase gates |
| 3.1B No compatibility residue | 1.3, 5.3, 6.4, 8.4, 9.3 |
| 3.2A Conflicting GUID claims | 2.1–2.3, 3.5, 4.1–4.2 |
| 3.2B Non-GUID provider subject | 2.1–2.3, 3.5 |
| 3.2C Purpose-bound principal | 2.1–2.4, 3.5, 4.1–4.2 |
| 3.3A Admin endpoint parity | 3.1–3.2 |
| 3.3B HAL-only client mutation | 3.3, 8.1, 8.3, 8.5 |
| 3.4A Stable fair-return ordering | 6.1 |
| 3.4B Cross-tenant independence | 6.1–6.3 |
| 3.4C Concurrent winner | 6.1–6.4 |
| 3.5A Valid live DID | 1.1–1.3, 2.4, 3.4, 5.5, 6.5, 7.1 |
| 3.5B Invalid or oversized DID | 1.1–1.3, 2.4, 3.4 |
| 3.5C Privacy-erasure tombstone | 1.1–1.3, 5.5 |
| 3.6A Typed component rename | 8.1–8.3, 8.6 |
| 3.6B Read-only and editable resources | 8.1–8.3, 8.5 |
| 3.7A Prohibited behavior dispatch | 5.1–5.4, 6.2–6.4, 8.1–8.6, 9.1–9.3 |
| 3.7B Legitimate compiled metadata | 0.2–0.3, 6.3, 9.1–9.3 |
| 3.8A Expected zero drift | 6.5, 7.1–7.2 |
| 3.8B Unexpected drift | 6.5, 7.2, 9.3 |

Applicable principles: Trust/Amanah, Truthfulness/Sidq, Justice/Adl, Non-Harm/La Darar, Promise-Keeping, Excellence/Ihsan, and avoiding excessive uncertainty. No religious-legal ruling or scholarly escalation is required.

## 10. Security, Authorization, Privacy, And Abuse Considerations

### Authentication and identity

- Ambient platform GUID resolution is one pure authority.
- Provider-linked resolution is explicit and asynchronous.
- Session IDs, API-key claims, setup/scanner/support/receipt credentials remain purpose-bound.
- Browser display claims are not an authorization or tenant source.

### Authorization

- API Admin policy preserves the existing role requirement.
- MediatR/Cerbos/local fallback remain authoritative for resources/actions.
- HAL remains the client affordance source.
- No permission is widened to make a typed test easier.

### Tenant isolation

- Tenant filters remain enabled.
- Queue/order/state tests include cross-tenant adversaries.
- BFF tenant headers remain server-owned and sanitized.

### Privacy

- Raw claims, DIDs, tenant/user IDs, and available-claim inventories are removed from logs.
- DID diagnostics are bounded/redacted.
- Privacy erasure keeps authority-first order, internal tombstone semantics, and anti-resurrection behavior.
- No whole-record structured logging is added.

### Abuse and replay

- Idempotency identity must not collapse unrelated purpose-bound principals.
- Transfer/recovery/waitlist replay/fence tests remain real-provider and deterministic.
- No runtime type name or source text becomes a security gate.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Rationale |
|---|---|---|
| Multi-tenancy | Applicable | Identity, EF filters, queue ordering, replay, and BFF tenant headers are touched. |
| Federation | Applicable | `AtprotoDid` is the only new semantic value and must remain interoperable. |
| Localization | Not applicable | No user copy or localization contract is added; existing message semantics remain. |
| Accessibility | Applicable | Typed component tests must preserve focus, read-only state, status, alerts, and keyboard-visible controls. |
| Product behavior | Applicable but bounded | Valid flows remain stable; malformed identity/DID and local UI authority behavior becomes more fail-closed. |
| Self-hosting | Applicable indirectly | No new dependency/configuration; scalar contracts and provider portability remain. |

## 12. Observability And Operations

- Logs use bounded reason/presence categories, not raw claim values, DIDs, tenant/user IDs, or provider errors.
- Rate-limit partition behavior remains stable and digest-based where already required.
- Existing correlation and trace behavior remains.
- The assurance tool emits deterministic file/category/location diagnostics and no source content beyond the bounded matched syntax location needed for remediation.
- No metric, trace, health, or alert cardinality changes are planned.
- Unexpected OpenAPI, generated-client, or EF model drift is an operator-visible planning failure, not an accepted output.

## 13. Migration And Compatibility Plan

### Database

- Expected schema impact: one generated application-catalog collation migration per supported provider so live DID identity remains ordinal across PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL.
- DID remains a scalar string owner field; no new table, column, owned type, complex type, or broad value converter.
- Existing column name/type/length, unique indexes, tenant filters, and privacy metadata remain; provider snapshots change only to record the explicit binary collation contract.
- Phase 6 generates the five application-provider migrations and then runs the product-catalog pending-model contract to record zero remaining drift. DataProtection and privacy-erasure-authority catalogs are excluded because they do not persist AT Protocol identity state.
- Migrations and snapshots are generated through the approved provider workflow; no hand edits.

### API and generated client

- Expected wire impact: none.
- Route values and operation IDs remain unchanged.
- Generated client is not manually edited.
- Phase 7 unconditionally executes the API OpenAPI target, client generation target, contract inventory generator, one Release solution build, and the generated-contract project gate against pre-phase hashes.
- Any intentional public break requires plan re-baselining, OpenAPI regeneration, API changelog, and release impact update.

### Source compatibility

- Internal source/test contracts break cleanly.
- All callers migrate in the owning phase.
- Old reflection helpers, string overloads, and adapters are deleted.
- No compatibility shim is allowed.

### Rollback

- Each phase is independently reviewable and green.
- Before persistence/wire change there is no data rollback.
- If a phase fails, restore only its owned files to the previous known-good content using file-edit tools; do not use destructive Git commands.
- Security or privacy failures block forward progress.

## 14. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection signal | Owner/task |
|---|---:|---:|---|---|---|
| Critical invariant lost while deleting a brittle test | Medium | Critical | Replacement-first invariant disposition | Missing scenario mapping or mutation proof | 5.x–9.x |
| Identity fallback or scheme semantics drift | Medium | Critical | Hostile-principal matrix; purpose classification | Changed resolved ID or unexpected 200/403 | 2.x–4.x |
| DID migration changes wire/schema or erasure behavior | Medium | High | Scalar owner design; red erasure tests | Generated diff, pending model, replay failure | 1.x–7.x |
| Legitimate metadata reflection is deleted | Medium | High | Semantic taxonomy; retain compiled contracts | Missing architecture/model failure signal | 0.x, 6.x, 7.x, 9.x |
| Roslyn guard becomes a historical allowlist | Medium | High | Category rules and synthetic fixtures only | File-specific exception inventory grows | 9.1–9.3 |
| Blazor action authority regresses | Low | High | HAL-only component tests | Button without relation or missing focus state | 4.x, 8.x |
| Dirty shared worktree causes overwrite/conflict | High | High | Exact owned-file ledger, surgical patches, no reverts | Unrelated diff changes | every phase |
| Route/operation/generated client drift | Low | High | Bidirectional route tests and zero-diff checks | Operation ID/client method changes | 3.x, 7.x |
| Scope expands into paused Blazor refactor | Medium | Medium | Exact ownership transfer and excluded broad work | Shared phase/task duplication | 4.x, 8.x |
| Phase verification becomes too broad/slow | Medium | Medium | One build + one owning test project per phase | Repeated or unrelated test commands | every phase |

## 15. Success Metrics And Definition Of Done

The workstream is complete only when:

- every submitted report item is verified as remediated, retained with reason, already typed, or cited-but-absent with its current replacement/location explicitly resolved;
- every transitive consumer of deleted helpers is migrated;
- no behavior test in governed scope uses runtime-name production dispatch;
- no governed product test asserts raw C#/Razor/CSS/Markdown tokens;
- valid compiled metadata and machine-artifact tests remain;
- platform identity and purpose-bound schemes satisfy Scenario 3.2;
- all four Admin surfaces satisfy Scenario 3.3A;
- fair-return ordering/tenancy and critical races satisfy Scenario 3.4;
- DID and erasure satisfy Scenario 3.5 with zero wire/schema drift;
- typed Blazor tests satisfy Scenario 3.6 and HAL-only actions;
- the new intent and assurance command prevent recurrence without debt allowlists;
- each phase's Release build and selected test project passes once;
- generated artifact/model checks are clean or the plan has been re-baselined;
- docs, paused-workstream ownership, I-VSD mapping, release fragment, and task ledger match repository reality;
- no unrelated dirty-tree change is modified or claimed.

## 16. Implementation Agent Contract — Keep Dev Docs Current

1. At first implementation start or cold resume, read task-owned context and the current task first, then retrieve only the relevant plan phase/decision.
2. Keep a `path + heading/symbol + revision` ledger. Do not reread unchanged artifacts during an uninterrupted session.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `tasks.md` as the hot ledger. Mark substantial work in progress and check it immediately when acceptance is met; reconcile small related tasks no later than phase end.
5. Keep implementation-task and phase-verification status separate.
6. Update completed count, current priority, next slice, discovered work, deferred work, and date whenever task state changes.
7. Update context after a phase, decision, blocker, failed validation, material discovery, scope change, or handoff.
8. Update this plan only when scope, architecture, order, acceptance, risk, or validation strategy changes.
9. Record failed validation and recovery action without marking the phase complete.
10. Before pause, compaction, transfer, or PR creation, reconcile tasks, add a dated handoff, and list unrelated dirty files to avoid.
11. Run the phase's one Release build and selected project test only after phase implementation tasks; targeted red/green selectors are limited to the named invariant during implementation.
12. Regenerate governed artifacts only through repository commands and only when the owning source intentionally changes.
13. Never report completion while the task ledger, I-VSD mapping, or repository state disagrees.

Every implementation summary must teach:

- what changed and why;
- the architecture/design pattern and owning layer;
- important files/classes/services/components and responsibilities;
- data/control flow and trust boundaries;
- relevant Clean Architecture, HAL, tenant, security, privacy, concurrency, and deterministic-test practices;
- exact verification, remaining work, next slice, and dev-doc state.

## 17. Progress Reporting Contract

After each implementation slice:

```text
Implemented: developer teaching summary
Verified: exact targeted and phase evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: tasks yes/no; context/plan updated or unchanged with reason
```

## 18. Potential Risks And Unknowns

The hardest part is not replacing reflection syntax; it is proving that each removed harness protected a real invariant and that the replacement fails for the same break. Identity and DID work are the highest-risk production slices because apparently equivalent string cleanup can change who the server believes the caller is, how tenants are isolated, or whether erased federation identities can reappear. The Roslyn recurrence guard is the highest maintenance risk: it must encode semantic categories without becoming another stale source-file allowlist.
