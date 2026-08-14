<!-- ABOUTME: API-wide plan for reducing accidental code, duplicated policy, and oversized responsibilities. -->
<!-- ABOUTME: Preserves every feature and trust boundary while making the smallest durable design simpler. -->

# API-Wide Code Liability Reduction — Implementation Plan

Last Updated: 2026-08-13 Europe/Brussels

## 1. Objective

Systematically reduce the lifetime cost of `Explore.API` and its immediate `Explore.Application` seams without weakening features, security, tenant isolation, HAL, OpenAPI, observability, or operational reliability. This is not a formatting campaign. It targets duplicated decisions, multiple ways to perform the same operation, presentation-owned orchestration, service-location, repeated scheduling loops, registration boilerplate, monolithic capability surfaces, dead compatibility code, and untested infrastructure seams.

The desired end state has:

- one authoritative path for current-user identity, command-failure mapping, query validation, HAL registration, and periodic worker lifecycle;
- controllers that translate HTTP into CQRS requests and translate results back, without owning identity reconstruction or business workflows;
- explicit public contracts beside endpoints, even when this prevents maximum LOC reduction;
- cohesive feature files/modules instead of 1,000–2,500-line mixed-responsibility classes;
- compile-time-visible registrations and no reflection magic, new framework, source generator, or package;
- direct deletion of obsolete development-era code, with no aliases, adapters, dual paths, or compatibility shims;
- phase-local regression evidence before behavior-bearing consolidation.

Success is measured by concepts and decisions removed, duplicated branches eliminated, dependency counts reduced, and feature contracts preserved. Net LOC should fall in consolidation phases, but no percentage target may justify hiding behavior.

## 2. Extensive Current-State Audit

### 2.1 Scale and hotspots

| Surface | Evidence | Liability signal |
|---|---|---|
| Whole API | 62,201 C# lines from current file inventory. | Large enough that one-shot modernization is unsafe. |
| Controllers | 119 files / 24,882 lines; 33 primary constructors and 83 explicit constructors. | Mixed conventions plus repeated translation logic. |
| HAL | 162 files / 13,834 lines; `HateoasAssemblerRegistration.cs` has 278 `AddScoped` calls. | Valuable affordance logic, but repetitive compile-time registration. |
| MCP | 14 files / 4,717 lines; `EventManagementMcpTools.cs` is 2,516 lines. | One class mixes tools, gating, sanitization, mapping, pagination, and formatting. |
| Hosting/extensions | 3,447 lines; `ApiHostServiceCollectionExtensions.cs` is 509 lines. | Composition root mixes unrelated feature registration and worker topology. |
| Background services | 33 files / 2,046 lines. | At least 11 workers repeat enabled/initial-delay/loop/error/delay/cancellation mechanics. |
| Problem mapping | `CommandResponseResultMapper.cs` is 643 lines, yet generic `MapCommandResponse` has only 15 controller call sites while many controllers retain private failure switches. | A shared authority exists but adoption and taxonomy are incomplete. |
| Query models | `QueryValidationRules.cs` is 443 lines and `PaginatedQueryRequests.cs` is 401 lines. | Shared validation exists, but many unrelated request families are accumulated in one file. |
| Oversized controllers | `RegistrationOrderController` 1,061 lines/34 actions; `EventController` 1,033/26; `WebhooksController` 1,025/21; `InstanceSettingsController` 854/47; `ControlPlaneController` 672/29. | Multiple resource families and helper policies live in single classes. |
| Private controller helpers | Event 26, Webhooks 22, Registration Order 21, AI Assistant 16, Event Session 14. | Controllers perform substantial mapping/policy/orchestration beyond HTTP adaptation. |
| Identity handling | 42 `FindFirst` occurrences and 44 `User.Find*` occurrences in controllers; `ExploreControllerBase` lazily resolves `IUserContext` from `HttpContext.RequestServices` and also reconstructs provider identities. | Three identity styles coexist: `IUserContext`, principal extensions, and manual claim parsing/service location. |
| Baseline quality | Release build: 0 errors, 758 warnings. | Buildable, but “zero debt” cannot honestly be claimed. This plan is warning-neutral; warning families require separately owned removal. |

The code-review graph confirms large controller, command, handler, repository, and service communities, but its index is stale relative to current HEAD. It is valid only for high-level structure; every symbol/deletion decision must be reverified with current source, LSP, callers, and tests.

### 2.2 Existing strengths to retain

- Clean Architecture direction is established: API → Application → Domain, with Persistence/Infrastructure implementing lower-layer contracts.
- Controllers do not use `DbContext`, repositories, or `SaveChanges` directly.
- Chained `IExceptionHandler` implementations already centralize RFC 7807 exception responses.
- MediatR authorization and performance behaviors already cover genuine cross-cutting concerns.
- `ApiProblemFactory`, typed problem descriptors, `CommandResponseResultMapper`, `QueryValidationRules`, `ExploreControllerBase`, and HAL registration are existing attempts at centralization. The plan completes or simplifies them rather than adding parallel systems.
- API architecture tests enforce controller shape, named routes, response contracts, tenant-filter boundaries, validator pairing, and route uniqueness.

### 2.3 Gemini report disposition

| Proposal | Decision |
|---|---|
| Primary constructors/expression bodies | Use where they delete assignment-only code; never mandate a mass syntax rewrite. |
| Positional records everywhere | Reject. Equality, construction, binding, and mutability semantics must be chosen per contract. |
| Generic base lookup controllers | Reject. They hide explicit route/OpenAPI/HAL/cache differences. |
| Generic validation behavior | Reject. It violates the manual-validator invariant. |
| Central exception handling | Already present; consolidate callers around it. |
| Blanket EF projection/AutoMapper `ProjectTo` | Reject. Repositories return entities; handlers map DTOs. Optimize only measured queries under repository intent. |
| Minimal APIs | Reject. A second endpoint model adds concepts and conflicts with controller fitness tests. |
| LOC percentage target | Reject. Contract metadata is valuable code; behavior must not be hidden to hit a number. |

## 3. Contract Classification

### Applicable intents

- `add-get-endpoint` and `add-write-endpoint`: controller, authorization, response, and route invariants apply to refactored actions.
- `add-cqrs-handler`: applies when controller-owned workflow moves into Application.
- `add-hal-link`: applies to HAL policy/registration work; no relation or authorization change is intended.
- `openapi-contract-change`: explicitly **not intended**. Contract drift blocks a phase and requires separate approval/reclassification.
- `ci-cd-change`, `add-ef-migration`, and `update-repository-query`: not intended.
- `ip-clean-room-governance`: applies because the user supplied an external report and requested external research.

### Authoritative files and skills

`AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `.agents/rules/api-controllers.md`, `application-layer.md`, `api-hateoas.md`, `tests.md`, `ip-clean-room.md`, and the clean-architecture, CQRS/MediatR, auth-patterns, error-tracking, outbox-pattern, and IP clean-room skills as each phase activates them.

## 4. Non-Negotiable Design Rules

1. Preserve routes, verbs, route names, operation IDs, response bodies/statuses, cache policies, media types, authentication, authorization, idempotency, HAL relations, and generated-client inputs unless a separately approved contract removal says otherwise.
2. Repositories continue returning entities; DTO mapping stays in handlers. No controller gains persistence access.
3. Validators remain manually instantiated. No validator DI or validation pipeline.
4. Current user/tenant/capability values come from trusted server context, never caller-supplied fields.
5. Do not “simplify” transactional outbox, retry, fencing, unknown-outcome handling, tenant filters, or security checks.
6. One abstraction must replace at least three materially identical implementations and make failure behavior more consistent. Otherwise keep direct code.
7. No reflection assembly scanning, runtime convention registration, new package, source generator, or framework migration.
8. No compatibility shims. Delete an obsolete internal path and update all callers in the same phase.
9. Splitting a file is allowed only when it creates a stable capability boundary; splitting alone is not claimed as LOC reduction.
10. Every phase starts with behavior characterization for its risky seam and ends with one Release build plus one fastest relevant non-browser test project.
11. Documentation is part of the architecture, not post-work cleanup. Every phase updates its canonical docs in the same slice, removes superseded guidance, and verifies that agents cannot learn the retired pattern from current documentation.
12. Do not create duplicate “refactor notes” as permanent documentation. Update the owning canonical page, keep historical rationale in Git/ADR history, and record only durable non-obvious findings in the journal.

## 5. Target Architecture

The public control flow remains:

```text
HTTP/MCP input
  -> explicit presentation validation and trusted-context extraction
  -> MediatR command/query
  -> handler-owned validation, authorization enrichment, mapping, and use-case orchestration
  -> repository/entity or infrastructure contract
  -> typed result
  -> one API-owned result/problem/HAL mapper
```

Presentation primitives are few and explicit:

- `IUserContext`/principal extensions are the sole already-authenticated identity authority; provider account bootstrap is one named service/query, never base-controller claim reconstruction.
- `CommandResponseResultMapper` plus typed descriptors is the sole reusable BaseCommandResponse → RFC 7807 mapping authority.
- `QueryValidationRules` stays the shared rule set, while query request types are grouped by capability rather than one grab-bag file.
- HAL registrations remain explicit at compile time but use small generic registration helpers for the repeated detail/collection/assembler triples.
- A single tested periodic-worker lifecycle owns cancellation, initial delay, interval waiting, scope creation boundary, and exception containment; each worker supplies only enablement, schedule, and one iteration.

## 6. Implementation Phases

### Phase 1 — Contract pins, dead-path inventory, and low-risk controller normalization

#### 1.1 Pin externally observable API invariants
- **Files:** existing `tests/Event.Architecture.Tests/ApiConventionTests.cs`, `ApiContractArchitectureTests.cs`, `EndpointClassificationArchitectureTests.cs`, and focused tests for the controller cohorts below.
- **Work:** inventory all actions in the five hotspot controllers and the consolidation cohorts; record verb/template/name/auth/classification/cache/success/error/HAL semantics. Add only missing assertions required to detect planned regressions.
- **Acceptance:** every later phase has a machine-checkable contract pin or an identified existing test; no LOC/style tests.
- **Effort:** L
- **Dependencies:** none

#### 1.2 Delete confirmed compatibility/dead presentation paths
- **Files:** bounded discovery across `src/Explore.API`; exact files recorded in tasks before edit.
- **Work:** use LSP references, graph callers/importers, route reflection, OpenAPI generation tests, and generated-client references. Delete only zero-caller internal helpers, obsolete aliases, duplicate registrations, unreachable branches, and superseded route constants/actions whose absence is already intended by tests.
- **Acceptance:** every deletion has evidence; all callers removed in the same change; no public removal without reclassification; no replacement wrapper.
- **Effort:** M
- **Dependencies:** 1.1

#### 1.3 Normalize truly mechanical controller adapters
- **Files:** all GET-only/pass-through controllers proven safe by inventory, not a predeclared numeric quota.
- **Work:** primary constructors and expression bodies only where they remove a field, assignment constructor, local, and immediate return without changing binding or metadata.
- **Acceptance:** net-negative diff, zero behavior changes, no base lookup controller.
- **Effort:** M
- **Dependencies:** 1.1

#### 1.4 Converge API contract and controller documentation
- **Files:** `docs/API.md`, `docs/API_CONTRACT_INVENTORY.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/QUICK_REFERENCE.md`, and `dev/_journal/journal.md` only for a new durable finding.
- **Work:** remove references to deleted aliases/paths, document the authoritative controller-adapter rule, and ensure contract inventory and current routes agree. Do not document primary constructors as an objective; document thin, explicit, behavior-preserving adapters.
- **Acceptance:** every removed path/pattern is absent from canonical docs and examples; no stale file name, route, or compatibility advice remains; documentation links and two-line `ABOUTME:` headers are valid.
- **Effort:** M
- **Dependencies:** 1.2, 1.3

#### Phase 1 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 2 — One identity authority and no controller service location

#### 2.1 Characterize identity resolution paths
- **Files:** `ExploreControllerBase.cs`, `ApiAuthenticationPrincipalExtensions.cs`, `IUserContext` and implementation, `ResolveCurrentUserIdByIdentityRequest`/handler, and all controller `FindFirst`/`ResolveCurrentUserIdAsync` call sites.
- **Work:** classify authenticated internal identity, provider bootstrap identity, API-key identity, and diagnostic-only claims. Pin fallback order `sub → nameidentifier → sid`, provider/DID behavior, email-verification rules, and failure semantics.
- **Acceptance:** a call-site matrix identifies the authoritative replacement for every manual claim path; ambiguous bootstrap behavior blocks migration rather than being guessed.
- **Effort:** L
- **Dependencies:** Phase 1

#### 2.2 Replace service-location and duplicate claim parsing
- **Files:** `ExploreControllerBase.cs`, identified controllers, Application identity contracts/handler, API authentication tests.
- **Work:** inject the existing identity abstraction explicitly; move provider-identity resolution behind one named trusted service/query; replace private `TryGetCurrentUserId`, `FindFirst`, and repeated unauthorized mapping. Remove superseded base helpers and claim logic after all callers migrate.
- **Acceptance:** no `HttpContext.RequestServices` in controllers; ordinary controllers do not parse identity claims; diagnostics may read display-safe claims explicitly; trust/fallback behavior remains tested.
- **Effort:** XL
- **Dependencies:** 2.1

#### 2.3 Make the identity authority unambiguous in documentation
- **Files:** `docs/API.md` authentication sections, `docs/ARCHITECTURE.md` request flow, `docs/AUTHORIZATION.md`, `docs/AUTHORIZATION_PATTERNS.md`, `docs/QUICK_REFERENCE.md`, and `docs/CODEBASE_STRUCTURE.md`.
- **Work:** document the exact authenticated-user, provider-bootstrap, machine-principal, and diagnostic boundaries; delete examples that parse claims or use controller service location; name the sole fallback order and trusted abstraction.
- **Acceptance:** an agent following any canonical auth example reaches the same authority; no current doc recommends `HttpContext.RequestServices`, ad hoc `FindFirst`, caller-supplied identity, or superseded base helpers.
- **Effort:** M
- **Dependencies:** 2.2

#### Phase 2 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 3 — One command-result and ProblemDetails mapping authority

#### 3.1 Inventory failure taxonomies and characterize status mapping
- **Files:** `CommandResponseResultMapper.cs`, `ApiProblemFactory.cs`, problem descriptors/codes, and controllers with private `Map*Failure`, `To*Problem`, or FailureCode switches.
- **Work:** group only semantically identical mappings. Pin status, problem type/code/title/detail, extensions, retry headers, resource identifiers, and security-safe detail rules.
- **Acceptance:** mapping matrix distinguishes reusable common cases from feature-specific cases; no “default BadRequest” collapse of distinct failures.
- **Effort:** L
- **Dependencies:** Phase 1

#### 3.2 Generalize the existing mapper by typed policy, not controller inheritance
- **Files:** existing exception-handling mapper/factory/descriptors and focused architecture/API tests.
- **Work:** add the smallest typed mapping input needed for repeated not-found/validation/conflict/forbidden/gone/provider failures. Keep HTTP concerns in API and delete specialized mapper methods made redundant.
- **Acceptance:** mapper API is smaller than the removed private mappings; response parity tests pass; no Application HTTP dependency.
- **Effort:** L
- **Dependencies:** 3.1

#### 3.3 Migrate high-duplication controller cohorts
- **Files:** `WebhooksController.cs`, `RegistrationOrderController.cs`, `EventTicketingController.cs`, `EventParticipationController.cs`, `WebhookBulkReplaysController.cs`, `WebhookProviderPublicationsController.cs`, `ControlPlaneController.cs`, then other proven matches.
- **Work:** replace private response switches with the shared mapper and delete dead descriptors/helpers.
- **Acceptance:** one mapping decision per failure code; controller action bodies express request construction and success shape only; net LOC decreases without status/detail drift.
- **Effort:** XL
- **Dependencies:** 3.2

#### 3.4 Converge error-contract documentation and examples
- **Files:** `docs/API.md` response/error sections, `docs/API_COOKBOOK.md`, `docs/API_CONTRACT_INVENTORY.md`, `docs/QUICK_REFERENCE.md`, and `docs/TESTING.md` contract-test guidance.
- **Work:** document the single BaseCommandResponse/ProblemDetails mapping authority, typed exception boundaries, safe-detail rules, and permitted feature-specific policies. Delete private-switch examples and ambiguous “return BadRequest” guidance.
- **Acceptance:** documented status/problem mappings match tests and implementation; every example uses the current mapper/factory path; no duplicate error taxonomy is introduced.
- **Effort:** M
- **Dependencies:** 3.3

#### Phase 3 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 4 — Thin the five hotspot controller families

#### 4.1 Move non-HTTP orchestration out of controllers
- **Files:** hotspot controllers plus their existing Application requests/handlers and tests.
- **Work:** identify controller helpers that normalize business inputs, coordinate several queries, compute readiness, or construct use-case results. Move those decisions into one existing/new CQRS request handler per use case. Keep HAL, headers, `ActionResult`, URL generation, and ProblemDetails in API.
- **Acceptance:** no controller-owned business workflow; repositories still return entities; handler tests cover moved logic; controller dependency count falls where orchestration services become unnecessary.
- **Effort:** XL
- **Dependencies:** Phases 2–3

#### 4.2 Partition by stable route capability without changing routes
- **Files:** `EventController.cs`, `RegistrationOrderController.cs`, `WebhooksController.cs`, `InstanceSettingsController.cs`, `ControlPlaneController.cs`, RouteNames/HAL policies, architecture tests.
- **Work:** after shared logic is removed, split only along stable capabilities: Event discovery/calendar/management/moderation; registration guest/authenticated/management; webhook consumers/endpoints/messages/incoming operations; instance governance/storage/auth/operations; control-plane plans/settings/tenant lifecycle. Preserve exact class-level/action-level metadata and route templates.
- **Acceptance:** no resulting controller exceeds the agreed review ceiling unless a documented cohesive exception remains; no helper is duplicated between new controllers; OpenAPI operation set and HAL URLs are unchanged.
- **Effort:** XL per family, executed one family at a time
- **Dependencies:** 4.1

#### 4.3 Update capability ownership and endpoint maps per controller family
- **Files:** `docs/API.md`, `docs/API_CONTRACT_INVENTORY.md`, `docs/CODEBASE_STRUCTURE.md`, and feature-specific canonical docs already referenced by the moved endpoints.
- **Work:** after each family lands, update controller/file ownership, endpoint group descriptions, CQRS responsibility, and navigation links. Remove old monolith ownership statements immediately; do not wait for all five families.
- **Acceptance:** repository paths, controller names, responsibility descriptions, and endpoint inventories match the completed family; no doc sends agents to the retired monolith for moved behavior.
- **Effort:** M per family
- **Dependencies:** 4.2 for that family

#### Phase 4 verification (repeat once per controller family, not per task)
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 5 — Compile-time HAL registration reduction

#### 5.1 Characterize the registration graph
- **Files:** `HateoasAssemblerRegistration.cs`, `HateoasServiceExtensions.cs`, all `ILinkPolicy`, `ICollectionLinkPolicy`, and `IResourceAssembler` contracts, HAL architecture tests.
- **Work:** classify the 278 registrations into detail+collection+assembler triples, detail-only, collection-only, shared-policy, and exceptional lifetime cases. Add a service-resolution test for every registered closed contract.
- **Acceptance:** every registration has exactly one category and expected lifetime; duplicate/missing registrations fail tests.
- **Effort:** M
- **Dependencies:** Phase 1

#### 5.2 Introduce minimal generic registration helpers and migrate triples
- **Files:** registration extension files and tests only.
- **Work:** add compile-time generic helpers for patterns occurring at least three times; retain explicit type arguments at each call site; do not scan assemblies or infer by naming.
- **Acceptance:** the service graph is identical; registrations remain searchable; `AddScoped` boilerplate materially decreases; exceptional registrations stay direct.
- **Effort:** L
- **Dependencies:** 5.1

#### 5.3 Update HAL authoring and registration guidance
- **Files:** `docs/API.md` HAL section, `docs/ARCHITECTURE.md` API representation, `docs/CODEBASE_STRUCTURE.md`, `docs/QUICK_REFERENCE.md`, and `.agents/rules/api-hateoas.md` only if its path-specific delta changes.
- **Work:** document the compile-time helper categories, exceptional direct registrations, lifetimes, service-resolution gate, and prohibition on reflection scanning. Remove three-call examples where the helper is now authoritative.
- **Acceptance:** a new HAL resource can be registered from one canonical example without guessing; explicit link-policy ownership and fail-closed authorization remain prominent.
- **Effort:** S
- **Dependencies:** 5.2

#### Phase 5 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 6 — One correct periodic worker lifecycle

#### 6.1 Pin scheduler and cancellation semantics
- **Files:** the repeated retention, cleanup, delivery, dispatch, sync, replay, and reconciliation `BackgroundService` implementations plus their tests/health checks.
- **Work:** characterize enabled behavior, initial delay, interval unit, async scope creation, cancellation during delay, exception containment, stop logging, dry-run/batch settings, and health effects.
- **Acceptance:** tests distinguish intentional worker differences from copied loop mechanics.
- **Effort:** L
- **Dependencies:** Phase 1

#### 6.2 Consolidate repeated timer-loop mechanics
- **Files:** new or existing API-host scheduling primitive, qualifying workers, worker tests.
- **Work:** create one small lifecycle abstraction that accepts explicit enabled/initial-delay/interval values and executes one overridden/delegated iteration. Preserve worker-specific work and safe logging. Migrate only workers matching the characterized lifecycle; leave queue/event-driven workers alone.
- **Acceptance:** at least three loops are deleted per abstraction; cancellation does not log errors; each iteration gets a fresh async scope when required; worker-specific retry/fencing/outbox semantics remain below the scheduling wrapper.
- **Effort:** XL
- **Dependencies:** 6.1

#### 6.3 Converge worker lifecycle and operations documentation
- **Files:** `docs/ARCHITECTURE.md` background-services section, `docs/OPERATIONS.md` lifecycle/shutdown/runbook sections, `docs/OUTBOX_PATTERN.md` where outbox scheduling is affected, `docs/TESTING.md`, and `docs/CODEBASE_STRUCTURE.md`.
- **Work:** document lifecycle ownership versus worker-specific processing, cancellation/error/scope rules, health behavior, and which workers intentionally remain event/queue driven. Delete copied per-worker loop guidance.
- **Acceptance:** operator behavior and implementation guidance agree; no doc suggests catching shutdown cancellation as an error, reusing scoped services, or moving outbox work into request paths.
- **Effort:** M
- **Dependencies:** 6.2

#### Phase 6 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 7 — MCP capability decomposition without protocol drift

#### 7.1 Characterize MCP tools, gates, disclosure ceilings, and descriptors
- **Files:** `EventManagementMcpTools.cs`, descriptors, readiness mapper, projected tool factory, MCP tests, AI context security docs.
- **Work:** map every tool to authorization, HAL gate, query, bound, truncation field, sanitization/disclosure ceiling, descriptor, and serialization context.
- **Acceptance:** no tool can be moved until its complete security and truncation contract is pinned.
- **Effort:** L
- **Dependencies:** Phase 1

#### 7.2 Partition the monolith and consolidate proven pure helpers
- **Files:** `EventManagementMcpTools*.cs`, descriptor/readiness files, tests.
- **Work:** partition by public discovery, management readiness, program, custom properties, registration/team, and templates. Centralize only identical bounds/trim/page/truncation helpers; keep location disclosure and AI sanitization explicit and fail-closed.
- **Acceptance:** tool names/descriptions/schema/output remain identical; class responsibilities become navigable; no security gate or truncation indicator is lost; duplicated pure helpers are deleted.
- **Effort:** XL
- **Dependencies:** 7.1

#### 7.3 Update MCP capability, security, and debugging documentation
- **Files:** `docs/ARCHITECTURE.md` MCP boundary, `docs/MCP_DEBUGGING.md`, `docs/API.md` where REST/HAL is the MCP authority, `docs/AI_CONTEXT_SECURITY.md`, `docs/CODEBASE_STRUCTURE.md`, and a new ADR only if an architectural decision changes (never rewrite accepted ADR history).
- **Work:** document the capability files, common gating path, bounds/truncation contract, disclosure ceilings, and serialization authority. Remove references to the monolith and duplicated tool-authoring patterns.
- **Acceptance:** MCP contributors can locate each capability and cannot bypass REST HAL authorization or AI disclosure sanitation by following docs; tool names/protocol examples remain current.
- **Effort:** M
- **Dependencies:** 7.2

#### Phase 7 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 8 — Composition-root cohesion and final debt ratchet

#### 8.1 Extract feature-cohesive registration methods without hiding topology
- **Files:** `ApiHostServiceCollectionExtensions.cs`, existing API extension modules, hosting tests.
- **Work:** move coherent registration blocks for OpenAPI, background processing, health, and MCP into named methods/files. Keep concrete registrations and conditional topology visible; do not introduce reflection or a module framework.
- **Acceptance:** host composition reads as an ordered list of capabilities; each extension owns one concern; registration order and environment/deployment gates are tested.
- **Effort:** L
- **Dependencies:** Phases 5–7

#### 8.2 Add forward-only architecture ratchets for eliminated liabilities
- **Files:** architecture tests and canonical governance only for rules proven by completed phases.
- **Work:** enforce no controller service location/manual identity parsing, no duplicate command-response switches where shared policy applies, explicit HAL registrations through approved helpers, and use of the periodic lifecycle by matching workers. Do not enforce primary-constructor syntax, LOC limits, or file counts.
- **Acceptance:** tests prevent reintroduction of removed concepts while allowing feature-specific exceptions with named reasons.
- **Effort:** M
- **Dependencies:** 8.1 and all completed consolidations

#### 8.3 Perform canonical documentation convergence and stale-guidance audit
- **Files:** `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/GOVERNANCE.md`, `docs/QUICK_REFERENCE.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `docs/index.md`, matching `.agents/rules/*.md`, and skill guidance only where the implemented architecture changed its contract.
- **Work:** search the repository for retired class names, helpers, patterns, file paths, code examples, and conflicting definitions. Consolidate duplicated rules into the highest-authority canonical page and replace lower-level duplication with links. State the long-term rule: functionality is the asset; every additional path, abstraction, compatibility layer, and duplicated decision is a liability requiring evidence.
- **Acceptance:** zero references to retired production patterns outside historical archives/ADRs; canonical docs have one owner per rule; agent rules link to current authority; docs index is navigable; no aspirational claim contradicts repository reality.
- **Effort:** L
- **Dependencies:** 8.2

#### Phase 8 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## 7. Explicitly Rejected “Optimizations”

- Generic controller inheritance, CRUD controller generators, repository-returned DTOs, injected validators, service locator helpers, reflection registration, convention-only routes, blanket records, Minimal APIs, blanket AutoMapper projection, catch-all failure mapping, removal of HAL metadata, removing cancellation tokens, consolidating distinct security paths, and replacing durable workers/outboxes with in-request work.
- Splitting every large file without first removing cross-capability logic.
- Warning suppression, generated-code exclusion, or nullable weakening to claim a clean build.
- Benchmarks/LOC dashboards that become permanent maintenance surfaces without a measured performance question.

## 8. Cross-Cutting Quality Classification

| Concern | Plan treatment |
|---|---|
| Security/auth | Phase 2 centralizes trusted identity; all fail-closed behavior pinned. |
| Authorization/HAL | Server policies remain authoritative; Phase 5 changes registration only. |
| Multi-tenancy | No query-filter bypass or caller-supplied tenant authority; controller-to-handler moves preserve tenant context. |
| Privacy/AI | MCP disclosure ceilings, sanitization, and safe diagnostics are immutable contracts. |
| Reliability | Phase 6 preserves retry/outbox/fencing and improves cancellation consistency. |
| Observability | Problem types/codes and structured logs are pinned; no payload/secret leakage. |
| OpenAPI/BFF | No operation or schema change; generated client remains the Blazor boundary. |
| Persistence | No migration/model work and no repository DTO leakage. |
| Dependencies/IP | No dependency change; external report supplied hypotheses only. |
| Compatibility | No shims. Intentional internal removals update all callers atomically; public contract change requires separate approval. |

## 9. Implementation-Agent Contract

1. Read context/tasks first, then only the current phase and referenced rules/skills.
2. Complete one risk-coherent phase/family at a time; do not run a repository-wide mechanical rewrite.
3. Write characterization evidence before consolidating identity, failures, workers, HAL registrations, or MCP security logic.
4. Mark substantial tasks immediately in `tasks.md`; reconcile small tasks by phase end.
5. Update context for a phase, decision, blocker, failure, material discovery, or handoff; update this plan only for strategy/scope/acceptance changes.
6. Run one Release build and at most one selected non-browser test project at phase end. Do not start Aspire, Docker, browsers, or live services under this planning workflow.
7. Any contract drift, weakened error detail safety, tenant ambiguity, missing HAL link, or worker semantic mismatch blocks the phase.
8. Report `Implemented`, `Verified`, `Remaining`, `Next`, and `Docs updated` after each slice.
9. A phase is not complete until its documentation task is complete. “Code works but docs later” is prohibited technical debt.
10. Documentation updates must delete or correct stale guidance, not merely append a new section that leaves contradictory examples searchable.

## 10. Research And Provenance

- User-supplied Gemini report: hypothesis source only; its examples, organization, percentages, and claims are not implementation authority.
- Tavily was invoked for official/industry research and returned usage-limit status 432 for every request; no Tavily content influenced this plan.
- Context7 was not exposed in the active MCP inventory; no Context7 result is claimed.
- All architecture and implementation choices were independently derived from repository code, tests, rules, and existing native patterns.
- No third-party source, snippet, AST, SQL, migration, test, comment, asset, or copied documentation prose entered the plan.

## 11. Potential Risks And Unknowns

The highest-risk phases are identity centralization, failure mapping, worker lifecycle consolidation, and MCP decomposition because superficially similar code contains security or reliability differences. Characterization tasks are therefore mandatory and exclusions are valid outcomes. The 758-warning baseline remains real debt outside this workstream; after API liability reduction, it should be reclassified by warning family and owning project rather than suppressed or mixed into these refactors.
