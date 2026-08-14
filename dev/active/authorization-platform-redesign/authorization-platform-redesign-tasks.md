<!-- ABOUTME: Hot execution ledger for the simplified authorization platform redesign. -->
<!-- ABOUTME: Tracks containment, typed enforcement, provider parity, operations, bounded query protection, and legacy deletion. -->

# Authorization Platform Redesign — Task Checklist

Last Updated: 2026-08-15 Europe/Brussels

## Status Summary

- **Overall status:** Implementation active; Phase 0 is complete and Phase 1 is in progress.
- **Completed:** 3/18 implementation tasks; phase verification is tracked separately.
- **Current priority:** Introduce the typed Application authorization boundary and trusted storage pre-create facts.
- **Next recommended slice:** Phase 1, Task 1.1: add the typed core contract and `StorageUploadIntentFacts` without provider or Domain leakage.

## Implementation Maintenance Rules

- Read all three artifacts once at first implementation start; on resume read context/tasks first and only the active plan phase.
- Mark one substantial task `🟡 IN PROGRESS`; check it immediately when acceptance is met; reconcile smaller items by phase end.
- Keep implementation completion separate from phase verification.
- Update completed count, current priority, next slice, discoveries, deferrals, and date when state changes.
- Update context after a decision, blocker, discovery, failed verification, phase completion, or handoff; update the plan only when strategy changes.
- Stay inside the active phase. Do not implement deferred external PDP, policy API, Policy Studio, DSL, import/export, CAEP, or RLS scope.
- At phase end run exactly one Release build and at most one selected non-browser test project. Never run solution-level `dotnet test`.
- Permission widening requires separate explicit approval. Breaking removal of development contracts does not require compatibility tests or shims.

## Phase 0: Containment and capability inventory ✅ COMPLETE

- [x] **Task 0.1 — Current capability inventory:** map every subject/resource/action, authority zone, endpoint auth attribute, MediatR authorization declaration, HAL relation, Local evaluator path, Cerbos policy path, and exceptional bypass; record exact files/symbols and owners.
- [x] **Task 0.2 — Provider-neutral baseline corpus:** add allow/deny scenarios for normal user, administrator, machine caller, support session, missing subject, missing tenant, wrong tenant, missing resource, consent/contact sharing, guest capability, public visibility, provider failure, and HAL suppression.
- [x] **Task 0.3 — Urgent containment:** remove, narrow, or fail closed verified administrator/machine/handler pass-through, action-reuse, consent, guest/public, and BYO-provider gaps without introducing the Phase 1 contract.

### Phase 0 Acceptance

- [x] Every current grant and bypass maps to a named capability/scenario; no permission is widened.
- [x] MediatR denials and HAL suppression agree for contained behavior.
- [x] Remaining ambiguity is a named blocker, not an implicit allow.

### Phase 0 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [x] Record exact results and any unrelated pre-existing failure in context.

## Phase 1: Typed Application decision contract 🟡 IN PROGRESS

- [ ] **Task 1.1 — Typed boundary:** add Application-owned request, decision, stable reason-code, provider-metadata, and closed capability-catalog contracts; prohibit arbitrary action/resource strings and fact dictionaries.
- [ ] **Task 1.2 — Trusted resolvers:** translate authenticated callers and loaded entities into typed subject/resource/facts while preserving tenant, support-session, machine-caller, callback, and suspended/deleted-principal boundaries.
- [ ] **Task 1.3 — Provider port adaptation:** place Local and Cerbos implementations behind one asynchronous, cancellation-aware typed port without moving provider concerns into Domain.

### Phase 1 Acceptance

- [ ] Domain owns only business facts/invariants and has no provider, Cerbos, HTTP, or policy-representation dependency.
- [ ] Missing/invalid subject, tenant, resource, or fact input produces a stable fail-closed decision.
- [ ] Architecture rules enforce the new ownership boundary.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Record exact results and any unrelated pre-existing failure in context.

## Phase 2: Canonical MediatR/HAL cutover and provider parity ⏳ NOT STARTED

- [ ] **Task 2.1 — Joint cutover:** switch `AuthorizationBehavior`, runtime routing, and `HateoasAuthorizationEvaluator` to the typed port in one review slice; remove production influence from the old contract.
- [ ] **Task 2.2 — HAL normalization:** normalize and batch candidate actions through the same request semantics, then materialize only allowed links; remove protected Blazor role/claim fallbacks.
- [ ] **Task 2.3 — Behavioral parity:** execute the Phase 0 corpus against Local and Cerbos adapters and add safe differential diagnostics for capability, expected/actual outcome, provider, reason, and observed revision.

### Phase 2 Acceptance

- [ ] Local and Cerbos pass the same required positive, negative, cross-tenant, missing-fact, provider-failure, and HAL scenarios.
- [ ] MediatR and HAL cannot disagree for the same normalized request.
- [ ] No dual production decision authority or compatibility translation remains after cutover.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] Record exact results and any unrelated pre-existing failure in context.

## Phase 3: Provider operations, health, telemetry, and invalidation ⏳ NOT STARTED

- [ ] **Task 3.1 — Explicit provider state:** replace implicit routing/one-way safe mode with versioned provider selection and reversible health state; keep Local self-contained and Cerbos optional.
- [ ] **Task 3.2 — Safe observability:** emit bounded decision metrics/traces and correlated audit fields for capability/resource category, outcome, reason, provider, observed revision, and duration without tokens, sensitive facts, or high-cardinality dimensions.
- [ ] **Task 3.3 — Bounded convergence:** reuse existing cache/outbox infrastructure for Event-owned setting invalidation; document stale behavior, convergence bound, readiness degradation, fail-closed writes, recovery, and Cerbos-native revision behavior.

### Phase 3 Acceptance

- [ ] Operators can distinguish Local/Cerbos mode, selected configuration version, observed revision, health, last failure/success, and recovery action.
- [ ] Sensitive writes deny on provider health/revision uncertainty.
- [ ] No new authorization-specific publication or distributed event subsystem exists.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Record exact results and any unrelated pre-existing failure in context.

## Phase 4: Bounded sensitive-query authorization ⏳ NOT STARTED

- [ ] **Task 4.1 — Sensitive collection inventory:** name the exact collections where unauthorized membership, existence, count, or pagination shape is sensitive; keep public collections out of scope.
- [ ] **Task 4.2 — Typed query constraints:** define only the safe capability subset that translates into existing Application specifications; unsupported sensitive conditions deny.
- [ ] **Task 4.3 — Pre-pagination enforcement:** apply tenant and authorization constraints before `Count`, `Skip`, `Take`, and projection while preserving entity-returning repositories and handler DTO mapping.

### Phase 4 Acceptance

- [ ] Named sensitive queries cannot disclose unauthorized rows, counts, existence, or pagination shape.
- [ ] Tenant filters remain active; no `IQueryable`, DTO, or provider policy representation escapes Persistence.
- [ ] Equivalent detail and query decisions agree for the same resource.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] Record exact results and any unrelated pre-existing failure in context.

## Phase 5: Legacy deletion, documentation, and contract freeze ⏳ NOT STARTED

- [ ] **Task 5.1 — Delete obsolete code:** remove old provider contracts, pass-through kinds, bypass lists, duplicate evaluators, stale safe-mode behavior, compatibility adapters, and UI/BFF-local protected-action gates.
- [ ] **Task 5.2 — Freeze tests/contracts:** remove only tests for deleted shapes, preserve/strengthen behavioral assertions, update OpenAPI/generated clients when contracts changed, and prove no obsolete dependency or symbol remains.
- [ ] **Task 5.3 — Canonical documentation:** update authorization, patterns, security, configuration, operations, self-hosting/recovery, and affected API documentation to describe one typed decision path and explicit Local/Cerbos modes.

### Phase 5 Acceptance

- [ ] One canonical authorization contract, enforcement path, and provider model remains.
- [ ] No hidden AST/compiler/store/admin/external-PDP/Policy-Studio implementation remains.
- [ ] Authorization parity, HAL behavior, tenant isolation, BFF boundary, provider recovery, and breaking deletion are documented and verified.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Record exact results and any unrelated pre-existing failure in context.

## Remaining / Deferred Work

- [ ] Create a separate workstream only if a validated product requirement emerges for tenant external PDP integration.
- [ ] Create a separate product/UX workstream before any policy administration API, visual builder, DSL, import/export, or Policy Studio work.
- [ ] Create a separate identity-security workstream for Keycloak/OpenID CAEP events if session revocation becomes a requirement.
- [ ] Keep database row-level security, the existing SSH.NET advisory, and unrelated stale contract-path cleanup outside this workstream.
