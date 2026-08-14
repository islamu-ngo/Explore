<!-- ABOUTME: Decision-complete implementation plan for simplifying and hardening the authorization platform. -->
<!-- ABOUTME: Sequences containment, typed enforcement, provider parity, operations, bounded query protection, and legacy deletion. -->

# Authorization Platform Redesign — Implementation Plan

Last Updated: 2026-08-14 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Improve the authorization redesign implementation plan after deep repository analysis and current industry research; do not implement runtime behavior.
- **Task directory:** `dev/active/authorization-platform-redesign/`
- **Status:** Re-baselined after Senior CTO review; awaiting implementation approval.
- **Primary intent:** `cerbos-policy-change`.
- **Supporting intents when their slices activate:** `add-cqrs-handler`, `add-get-endpoint`, `add-write-endpoint`, `add-hal-link`, `update-repository-query`, `add-ef-migration`, `blazor-component-affordance`, `bff-auth-bug`, `openapi-contract-change`, and `ip-clean-room-governance`.
- **Breaking-change posture:** Development-only contracts may be removed directly. No compatibility adapters, dual production authorities, or legacy policy translation layer.
- **Permission posture:** This plan may preserve or narrow grants. Widening any permission requires separate explicit approval.
- **Estimated implementation size:** Large, delivered as six independently reviewable phases rather than one mega-PR.

## 1. Senior CTO Decision

**Verdict: split and simplify before implementation.**

The previous direction correctly identified authorization drift, weakly typed provider contracts, parity gaps, and disclosure risk. It was not approved as written because it combined platform hardening with creation of a new policy product. A Domain-owned generic policy AST, universal policy generations, an AST-to-Cerbos compiler, a policy control plane, administration APIs, and a Blazor Policy Studio would duplicate provider capabilities and create a second authorization language without a proven requirement.

The approved target for this workstream is smaller:

1. one typed Application-owned decision boundary;
2. one canonical enforcement flow shared by MediatR and HAL;
3. repository-native Local and Cerbos adapters verified by the same behavioral scenarios;
4. explicit provider selection, health, revision, telemetry, and bounded invalidation;
5. authorization-aware query constraints only for named disclosure-sensitive collections; and
6. deletion of obsolete contracts and bypasses after parity is proven.

## 2. Source-Grounded Current State

### 2.1 Verified architecture

- ASP.NET Core endpoint metadata establishes authentication requirements; loaded-resource authorization is enforced through the MediatR `AuthorizationBehavior` path.
- `RuntimeAuthorizationProvider` routes decisions between the repository-native Local implementation and Cerbos integration, with additional tenant/provider routing behavior.
- Local authorization is spread across `FallbackAuthorizationService` partials and feature-specific evaluators. Cerbos policies and mappings form a separate semantic implementation.
- `HateoasAuthorizationEvaluator` performs server-side capability evaluation for HAL. Blazor must render actions only when corresponding `_links` exist.
- Keycloak is the authentication authority. Event-owned memberships, legal entities, tenant state, consent, and resource facts remain the authorization data authority.
- Repository tenant filters are a hard isolation boundary. Repositories return entities and handlers map response DTOs.
- The BFF retains tokens in HttpOnly cookies and forwards credentials server-side; it is never an authorization decision point.

### 2.2 Verified problems to resolve

- Provider contracts use strings, dictionaries, or booleans where the platform needs explicit subject, resource, action, facts, decision, reason, and observed-provider metadata.
- Administrator, machine-caller, handler-owned, and provider-routing shortcuts can bypass or duplicate canonical evaluation.
- Local and Cerbos modes have partial structural tests but no complete provider-neutral behavioral corpus covering positive, negative, missing-fact, wrong-tenant, and HAL suppression cases.
- Some existing-event operations reuse broader capability names instead of expressing the action actually performed.
- Contact sharing, consent, guest capability, public visibility, and tenant BYO failure behavior need an explicit deny-first inventory before redesign work can safely proceed.
- Provider safe-mode and failure behavior is not represented as a clear, reversible, observable state model.
- Some protected collection paths may authorize after count, pagination, or projection, risking existence or count disclosure.

### 2.3 External evidence translated into repository-native requirements

- ASP.NET Core policy/resource authorization supports a typed requirement/handler and imperative decision boundary when the resource must be loaded before authorization.
- NIST zero-trust guidance separates policy decision and enforcement points and emphasizes least privilege and continuous evaluation.
- Cerbos already owns Cerbos policy validation, distribution, revisioning, and audit behavior. Event must adapt to that provider instead of compiling a new generic policy language into it.
- OpenID shared security-event standards are relevant to a future identity/session revocation integration, not to a new authorization publication protocol in this workstream.

External evidence is used only as sanitized functional guidance. No third-party code, policy source, AST, schema, SQL, migration, test, comment, asset, or distinctive prose may enter implementation context.

## 3. Target Architecture

### 3.1 Canonical flow

```text
Endpoint authentication metadata
  -> MediatR AuthorizationBehavior (authoritative command/query PEP)
  -> Application-owned typed authorization request
  -> Runtime provider router
  -> Local evaluator OR Cerbos adapter (PDP)
  -> typed decision with reason + provider metadata
  -> handler execution or fail-closed denial

HAL candidate actions
  -> same typed request/router/provider path
  -> materialize only allowed _links
  -> Blazor renders only returned affordances
```

### 3.2 Contract ownership

- **Domain:** business facts and invariants only. No policy AST, provider contract, Cerbos type, HTTP concern, or orchestration.
- **Application:** typed authorization request/decision contract, capability catalog, enforcement interfaces, and bounded query-constraint abstractions.
- **Infrastructure:** Local evaluator, Cerbos adapter, provider routing/configuration, cache integration, health probes, and telemetry implementation.
- **API:** endpoint authentication metadata, ProblemDetails mapping, HAL candidates/materialization, and composition.
- **Persistence:** entity-first repositories, tenant-safe specifications, persisted provider configuration only when required, and generated migrations.
- **Blazor/BFF:** transport and presentation only; no role/claim/provider-local authorization decisions.

### 3.3 Minimal typed decision model

Replace loosely typed calls with one request and one result shape owned by Application:

- request: subject, tenant, resource, action, typed facts, correlation context;
- decision: allow/deny, stable reason code, provider mode, observed provider revision, and evaluation metadata safe for telemetry;
- capability catalog: the closed inventory of supported resource/action combinations; and
- provider port: asynchronous, cancellation-aware evaluation of the typed request.

Typed facts may be capability-specific. They must not become a general expression tree or arbitrary policy dictionary.

### 3.4 Provider model

- **Local mode:** required, self-contained, repository-native code deployed with the application version.
- **Cerbos mode:** optional, explicit integration using Cerbos-native policy artifacts and observed revision/health.
- **Tenant external PDP:** not part of this workstream. If commissioned later, it must be a separate threat-modeled integration and may narrow but never widen the platform kernel.
- Missing subject, tenant, resource facts, provider health, selected revision, or cache certainty denies sensitive operations and suppresses HAL links unless an existing public-read contract explicitly applies.

### 3.5 Non-delegable kernel

Provider adapters cannot override platform invariants for tenant binding, suspended/deleted principals, support-session scope, machine-caller scope, public callback authenticity, or other repository-defined trust boundaries. Keep this kernel small and explicit; feature permissions remain provider decisions.

## 4. Non-Goals And Explicit Deferrals

The following are deleted from this workstream:

- Domain-owned generic policy AST or policy aggregate;
- universal Event-owned policy generations;
- AST-to-Cerbos compiler;
- generic policy store, publication control plane, or policy seed framework;
- compatibility shims for existing weakly typed authorization contracts; and
- BFF/UI role or claim gates for server actions.

The following require separate product workstreams and fresh approval:

- tenant-managed external PDP;
- policy administration API;
- Blazor Policy Studio or visual policy builder;
- user-authored policy DSL, import/export, migration tooling, or policy marketplace;
- Keycloak/OpenID CAEP session-event integration; and
- database row-level security.

## 5. Implementation Phases

Each phase is a separate review boundary. At phase end, run exactly one Release build and at most one fastest relevant non-browser test project. Do not start the next phase with a red baseline.

### Phase 0: Contain known exposures and freeze the capability inventory

**Risk owner:** Security.
**Purpose:** Reduce present risk without waiting for the platform rewrite and establish the behavioral baseline that later phases must preserve or narrow.

1. Inventory every current subject kind, resource kind, action, authority zone, handler declaration, endpoint auth attribute, HAL relation, Local evaluator path, Cerbos policy path, and exceptional bypass.
2. Convert the inventory into provider-neutral allow/deny scenarios, including missing subject, missing tenant, wrong tenant, missing resource, consent, guest capability, public visibility, machine caller, support session, provider failure, and HAL suppression.
3. Remove or fail closed verified ambient administrator, machine, handler-owned, event-action reuse, contact-sharing, guest/public, and BYO-provider gaps. Permission widening is forbidden.

**Acceptance criteria**

- Every active grant is traceable to a named capability and scenario.
- Known ambiguous/bypass paths are removed, narrowed, or recorded as blocking defects.
- MediatR denial and HAL link suppression agree for the contained behaviors.
- No new abstraction from later phases is introduced solely to ship containment.

### Phase 1: Introduce the typed Application decision contract

**Risk owner:** Architecture.
**Purpose:** Replace string/dictionary/bool boundaries without introducing a new policy language.

1. Add the typed request, decision, reason-code, provider-metadata, and capability catalog contracts in Application.
2. Add trusted resolvers that translate authenticated caller and loaded Event entities into typed subjects, resources, and facts while preserving tenant authority.
3. Adapt existing providers behind the typed port, temporarily keeping old implementation internals only until the joint cutover in Phase 2.

**Acceptance criteria**

- Domain remains free of provider and policy-representation concerns.
- The new boundary has no arbitrary action/resource strings or fact dictionaries.
- Cancellation flows end to end and decision reasons are stable but non-sensitive.
- Architecture tests enforce layer ownership.

### Phase 2: Cut MediatR and HAL to one canonical path and prove provider parity

**Risk owner:** Authorization correctness.
**Purpose:** Make command/query enforcement and affordance materialization consume the same decision semantics.

1. Switch `AuthorizationBehavior`, runtime routing, and `HateoasAuthorizationEvaluator` to the typed port in one atomic slice.
2. Normalize batch HAL requests before evaluation and materialize only links with allowed decisions.
3. Run the Phase 0 provider-neutral corpus against Local and Cerbos modes; add differential diagnostics that identify capability, expected result, actual result, provider, and revision without logging sensitive facts.

**Acceptance criteria**

- Local and Cerbos pass the same required scenarios.
- Endpoint/MediatR enforcement and HAL affordances cannot disagree for the same normalized request.
- Missing/invalid facts and provider uncertainty fail closed.
- Blazor contains no local role/claim fallback for protected actions.
- No dual production decision path remains after cutover.

### Phase 3: Make provider selection and failure behavior operable

**Risk owner:** Operations and self-hosting.
**Purpose:** Make Local and optional Cerbos modes explicit, observable, recoverable, and safe across replicas.

1. Replace one-way safe mode and implicit routing with versioned provider selection/configuration and a reversible health state model.
2. Record bounded decision metrics and traces: capability/resource category, outcome, reason code, provider mode, observed revision, duration, and correlation ID. Never record raw tokens or sensitive fact values; do not use tenant/user/resource IDs as unbounded metric dimensions.
3. Reuse existing cache and transactional outbox patterns for Event-owned provider-setting invalidation. Use Cerbos-native artifact/revision behavior for Cerbos policy distribution; do not build an Event policy publisher.

**Acceptance criteria**

- Local mode has no Cerbos runtime dependency and remains the self-contained default.
- Cerbos mode exposes liveness/readiness, selected mode, observed revision, last success/failure, and operator recovery guidance.
- Sensitive writes deny when the selected provider is unhealthy or revision certainty is unavailable.
- Cross-replica convergence has a documented bound, invalidation path, stale-state behavior, and recovery procedure; it does not claim impossible global atomicity.
- Configuration and health changes are auditable and tenant-safe.

### Phase 4: Protect named disclosure-sensitive queries before pagination

**Risk owner:** Tenant isolation and data disclosure.
**Purpose:** Prevent existence/count leaks without inventing a universal policy query planner.

1. Select the exact collections whose membership or count is authorization-sensitive; public collections remain out of scope.
2. Define typed, provider-neutral query constraints only for the capability subset that can be translated safely into existing Application specifications.
3. Apply tenant filters and authorization constraints before `Count`, `Skip`, `Take`, and projection. Unsupported sensitive conditions deny rather than post-filtering a paginated result.

**Acceptance criteria**

- Named sensitive queries cannot disclose unauthorized rows, counts, existence, or pagination shape.
- Repository tenant filters remain enabled and authoritative.
- Repositories still return entities; handlers retain DTO mapping.
- No `IQueryable` or provider-specific policy representation escapes Persistence.
- Query and detail authorization scenarios agree for equivalent resources.

### Phase 5: Delete legacy surfaces and freeze the contract

**Risk owner:** Maintainability.
**Purpose:** Finish the breaking cutover instead of preserving two systems.

1. Delete obsolete provider contracts, pass-through kinds, bypass lists, duplicate evaluators, stale safe-mode behavior, compatibility adapters, and local UI/BFF authorization gates.
2. Delete tests and documentation that describe removed contracts, replacing them with coverage and canonical docs for the typed path. Do not weaken behavioral assertions.
3. Update `docs/AUTHORIZATION.md`, `docs/AUTHORIZATION_PATTERNS.md`, `docs/SECURITY-MODEL.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, API/OpenAPI contracts when changed, and operator recovery material.

**Acceptance criteria**

- One canonical decision contract and enforcement model remains.
- Search and architecture checks find no obsolete contract or forbidden dependency direction.
- Local and Cerbos conformance, API authorization/HAL behavior, tenant isolation, BFF boundary, and self-hosting recovery are documented and verified.
- No AST, compiler, policy store, admin API, external PDP, or Policy Studio scope remains hidden in implementation.

## 6. Testing Strategy

- Use TUnit project-specific tests; never run solution-level `dotnet test`.
- Extend the existing authorization parity/architecture suite for contract ownership and provider-neutral scenarios.
- Use API integration tests for endpoint authentication, MediatR denial, ProblemDetails, HAL link presence/absence, and provider failure behavior.
- Use Persistence integration tests only when Phase 4 changes query specifications or a generated migration is required.
- Use BFF/Blazor tests only when removing local affordance gates or changing generated contracts.
- Include negative and cross-tenant cases first. No backward-compatibility assertions are required for removed development contracts.

## 7. Security And Privacy Requirements

- Deny by default and enforce every protected operation server-side.
- A hidden Blazor control is not authorization; HAL links are only presentation affordances backed by the same server decision path.
- Never disable the runtime Tenant query filter to simplify authorization.
- Never log JWTs, raw claims, policy facts, request bodies, or sensitive resource identifiers.
- Correlate decisions without high-cardinality metric labels.
- Preserve antiforgery, BFF token secrecy, trusted tenant resolution, support-session boundaries, machine-caller scope, and public callback verification.

## 8. Migration And Compatibility

- This is a breaking development migration. Replace old internal contracts directly after the Phase 2 parity gate.
- Do not run old and new providers as independent production authorities. A temporary differential test harness may observe both but must not influence production allow/deny results.
- If provider configuration persistence changes, fix the entity/configuration model and generate the migration; never hand-edit migration or snapshot artifacts.
- Local policy behavior deploys with the application version. Cerbos policy behavior uses Cerbos-native artifacts/revisions. Event-owned configuration changes use existing transactional patterns.

## 9. Risks And Required Decisions

| Risk | Required treatment |
|---|---|
| Existing grant inventory is incomplete | Block deletion until every active handler/HAL/provider path maps to a scenario. |
| Local/Cerbos semantics cannot be made equivalent for a capability | Narrow the capability or document an explicit mode-specific unsupported case that denies safely; do not invent a generic DSL. |
| Provider revision is not observable | Cerbos mode is not release-ready until decisions and health expose the observed revision safely. |
| Cache invalidation semantics are unclear | Reuse existing cache/outbox infrastructure and define a measured convergence bound before enabling cached authorization. |
| Query predicates cannot represent a sensitive policy | Deny the query or redesign that named endpoint; do not post-filter after pagination. |
| Operator recovery depends on unavailable external services | Keep Local mode self-contained and document Cerbos as optional with explicit readiness/failure behavior. |

## 10. Definition Of Done

- All six phases and their single phase-end gates pass.
- Permissions are preserved or narrowed unless a separate widening approval is recorded.
- MediatR and HAL share one typed, fail-closed decision path.
- Local and Cerbos pass one provider-neutral behavioral corpus.
- Provider mode, health, revision, correlation, recovery, and bounded invalidation are operator-visible.
- Named sensitive queries constrain authorization before count/pagination/projection.
- Legacy contracts and speculative platform/product scope are deleted or deferred explicitly.
- Canonical docs, configuration, operations, and security guidance describe the shipped behavior.

## 11. Implementation Agent Contract

- Read `AGENTS.md`, classify the active task in `.agents/contract/intents.yaml`, and load every required doc/rule/skill before editing.
- Read all three workstream artifacts at first start. On resume, read context/tasks first and only the relevant plan phase.
- Mark substantial tasks immediately in `authorization-platform-redesign-tasks.md`; update context for decisions, blockers, verification, and handoff; change this plan only when strategy changes.
- Stay within the active phase. Do not pull deferred product scope forward.
- At phase end, run one Release build and at most one fastest relevant non-browser test project, then record exact results in context.
