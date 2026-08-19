<!-- ABOUTME: Hot execution ledger for the simplified authorization platform redesign. -->
<!-- ABOUTME: Tracks containment, typed enforcement, provider parity, operations, bounded query protection, and legacy deletion. -->

# Authorization Platform Redesign — Task Checklist

Last Updated: 2026-08-18 Europe/Brussels

## Status Summary

- **Overall status:** Implementation active. Phase 0 is complete. Phase 1's typed port exists, but its no-dictionary/no-legacy-influence criterion is reopened after independent review. Phase 2 UI fallbacks and six focused provider regressions are repaired; canonical EventTeam enforcement and full provider parity remain open.
- **Completed:** 5/19 implementation tasks; phase verification is tracked separately.
- **Current priority:** Finish EventTeam's shared MediatR/HAL `events.manage-team` path, remove legacy dictionary influence from migrated requests, then execute the full provider-neutral Local/Cerbos corpus with safe diagnostics.
- **Next recommended slice:** Complete the EventTeam canonical server path and trusted typed facts, then execute every Phase 0 scenario category against both provider semantics. The seven-case event-view adapter test is smoke coverage only. HAL consolidation is a later Phase 5 task and must wait for parity.

## Implementation Maintenance Rules

- Read all three artifacts once at first implementation start; on resume read context/tasks first and only the active plan phase.
- Mark one substantial task `🟡 IN PROGRESS`; check it immediately when acceptance is met; reconcile smaller items by phase end.
- Keep implementation completion separate from phase verification.
- Update completed count, current priority, next slice, discoveries, deferrals, and date when state changes.
- Update context after a decision, blocker, discovery, failed verification, phase completion, or handoff; update the plan only when strategy changes.
- Stay inside the active phase. Do not implement deferred external PDP, policy API, Policy Studio, DSL, import/export, CAEP, or RLS scope.
- At phase end run exactly one Release build and at most one selected non-browser test project. Never run solution-level `dotnet test`.
- Permission widening requires separate explicit approval. Breaking removal of development contracts does not require compatibility tests or shims.
- HAL policy consolidation is authorization work: preserve detail/collection separation, typed-fact precedence, fail-closed omission, explicit registration, and route-name/controller metadata alignment. Do not replace it with a generic policy DSL, reflection registry, or one policy god-class.

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

- [ ] **Task 1.1 — Typed boundary 🟡 REOPENED:** the typed contracts and closed capability catalog exist, but `AuthorizationRequest.ResourceAttributes` still has production influence when typed facts are absent.
- [x] **Task 1.2 — Trusted resolvers:** translate authenticated callers and loaded entities into typed subject/resource/facts while preserving tenant, support-session, machine-caller, callback, and suspended/deleted-principal boundaries.
- [x] **Task 1.3 — Provider port adaptation:** place Local and Cerbos implementations behind one asynchronous, cancellation-aware typed port without moving provider concerns into Domain.

### Phase 1 Acceptance

- [x] Domain owns only business facts/invariants and has no provider, Cerbos, HTTP, or policy-representation dependency.
- [x] Missing/invalid subject, tenant, resource, or fact input produces a stable fail-closed decision.
- [x] Architecture rules enforce the new ownership boundary.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Record exact results and any unrelated pre-existing failure in context.

## Phase 2: Canonical MediatR/HAL cutover and provider parity 🟡 IN PROGRESS

- [ ] **Task 2.1 — Joint cutover 🟡 REOPENED:** MediatR/HAL call the typed port, but migrated requests still fall back to arbitrary resource-attribute dictionaries when typed facts are absent.
- [ ] **Task 2.2 — HAL normalization 🟡 IN PROGRESS:** organization-member, EventTeam, and event-publisher client fallbacks are removed and EventTeam preserves collection/item links; its CQRS reads/writes must still enforce the same typed `events.manage-team` request as HAL.
- [ ] **Task 2.3 — Behavioral parity 🟡 NEEDS FIX:** the six focused Cerbos/runtime regressions are green, but the current seven-case event-view adapter smoke test neither covers the full Phase 0 corpus nor executes live Cerbos policy semantics.

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
- [ ] **Task 5.2 — HAL policy and route-surface consolidation:** after Phase 2 parity, inventory the 82 policy files (9,978 lines), `RouteNames.cs`, registrations, controller route metadata, and affected HAL tests; consolidate repeated policy plumbing into bounded feature-scoped compile-time modules/shared builders while keeping custom resource authorization explicit.
  - **Files:** existing `src/Explore.API/Hateoas/Policies/**/*.cs`, `src/Explore.API/Hateoas/RouteNames.cs`, `src/Explore.API/Extensions/HateoasAssemblerRegistration.cs`, `src/Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`, `src/Explore.API/Hateoas/LinkDefinitionPermissionExtensions.cs`, `tests/Event.API.IntegrationTests/Features/Hateoas/**/*.cs`, `tests/Event.Architecture.Tests/HateoasRegistrationGraphTests.cs`, and affected `tests/Explore.Blazor.Client.Tests/**/*.cs` HAL-affordance tests.
  - **Acceptance:** the before/after relation-action-route inventory has no accidental drops or permission widenings; detail and collection policies remain separate; `RequirePermission` uses the typed-fact path; registration remains explicit and compile-time; every remaining route name matches controller metadata; fail-closed omission and HAL-only client gating remain covered; superseded development-only policy types and route aliases are deleted without compatibility shims.
  - **Effort:** XL
  - **Dependencies:** 2.1, 2.2, 2.3, 5.1
- [ ] **Task 5.3 — Freeze tests/contracts:** remove only tests for deleted shapes, preserve/strengthen behavioral assertions, update OpenAPI/generated clients when contracts changed, and prove no obsolete dependency or symbol remains.
- [ ] **Task 5.4 — Canonical documentation:** update authorization, patterns, security, configuration, operations, self-hosting/recovery, and affected API documentation to describe one typed decision path, explicit Local/Cerbos modes, and the consolidated HAL authorization surface.

### Phase 5 Acceptance

- [ ] One canonical authorization contract, enforcement path, and provider model remains.
- [ ] HAL policy and route plumbing is consolidated without a giant policy class, dynamic registry, lost affordance, or permission widening.
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
