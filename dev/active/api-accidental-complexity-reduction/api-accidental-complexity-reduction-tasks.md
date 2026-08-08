<!-- ABOUTME: Ordered implementation checklist for the approved API accidental complexity reduction workstream. -->
<!-- ABOUTME: Makes typed error contract, authorization redesign, architecture gates, and cleanup tasks executable. -->

# API Accidental Complexity Reduction Tasks

Last Updated: 2026-08-06 Europe/Brussels

## Status

- Planning: **Approved**
- Implementation: **Not started**
- Current task: **Awaiting user approval of plan**
- Current blocker: **None**
- Rule: complete tasks in order unless this document explicitly marks them parallel

### Progress Snapshot

- Completed: 0/16 implementation tasks
- Current priority: Phase 0 — Characterization Test Baseline
- Next recommended slice: Task 0.1 — Authorization behavior characterization tests

## Implementation Maintenance Rules

- Read the full workstream once at initial implementation start; on resume, read
  context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task `🟡 IN PROGRESS` when it is likely to span multiple
  edits or a handoff; skip this churn for tiny tasks completed immediately.
- Check a substantial completed task immediately; reconcile small completed tasks
  no later than phase end.
- Add discovered work where it belongs and keep completed count, priority, next
  slice, deferred work, and update date accurate.
- Check a phase complete only after all implementation and phase-verification
  checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material
  discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance criteria,
  risk, or validation strategy changes.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools MCP,
  or live services for verification.

## Phases and Tasks

### Phase 0: Characterization Test Baseline ⏳ NOT STARTED

- [ ] **0.1 Extend AuthorizationBehavior characterization tests**
  - **Files:** `tests/Event.Application.UnitTests/Behaviors/AuthorizationBehaviorTests.cs` (existing, 1,601 lines)
  - **Acceptance:** Each of the 12 else-if type-cast branches has a test verifying: (a) correct command mutation, (b) correct authorization context extraction, (c) correct repository call
  - **Effort:** M
  - **Dependencies:** None

- [ ] **0.2 Add controller error-mapping characterization tests**
  - **Files:** `tests/Event.API.IntegrationTests/Features/` (new test class)
  - **Acceptance:** Tests verify: `Message="X not found"` → 404, `Message` contains `"administrators"` → 403, default → 400
  - **Effort:** M
  - **Dependencies:** None

#### Phase 0 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

---

### Phase 1: Typed Error Contract ⏳ NOT STARTED

- [ ] **1.1 Add common failure codes to FailureCodes.cs**
  - **Files:** `src/Explore.Application/Responses/FailureCodes.cs` (existing)
  - **Acceptance:** `not_found`, `admin_required`, `concurrency_conflict` constants added with XML docs. Use `snake_case` matching existing conventions.
  - **Effort:** S
  - **Dependencies:** None

- [ ] **1.2 Add MapCommandResponse extension method**
  - **Files:** `src/Explore.API/ExceptionHandling/CommandResponseResultMapper.cs` (existing)
  - **Acceptance:** `MapCommandResponse<TKey>` extension on `ControllerBase` handles `not_found` → 404, `admin_required` → 403, `concurrency_conflict` → 409, default → 400. Tests cover all branches.
  - **Effort:** S
  - **Dependencies:** 1.1

- [ ] **1.3 Update handlers to set FailureCode (batch 1: "not found" handlers)**
  - **Files:** ~80 handler files across `src/Explore.Application/Features/` that return "not found" messages without FailureCode
  - **Acceptance:** Every handler that returns a "not found" failure also sets `FailureCode = FailureCodes.NotFound`. Existing `Message` text unchanged.
  - **Effort:** L
  - **Dependencies:** 1.1

- [ ] **1.4 Update handlers to set FailureCode (batch 2: remaining handlers)**
  - **Files:** ~79 remaining handler files across `src/Explore.Application/Features/`
  - **Acceptance:** Every handler that returns an admin/authorization failure sets `FailureCode = FailureCodes.AdminRequired`. Other systematic failures get appropriate codes.
  - **Effort:** L
  - **Dependencies:** 1.1

- [ ] **1.5 Replace string inspection in controllers with FailureCode switch**
  - **Files:** ~36 controller files in `src/Explore.API/Controllers/`
  - **Acceptance:** Zero occurrences of `Message?.Contains("not found")` or `Message?.Contains("administrators")` remain. All sites use FailureCode switch or `MapCommandResponse`.
  - **Effort:** L
  - **Dependencies:** 1.2, 1.3, 1.4

#### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

---

### Phase 2: Authorization Behavior Redesign ⏳ NOT STARTED

- [ ] **2.1 Create IAuthorizationContextEnricher\<TRequest\> interface and AuthorizationContext record**
  - **Files:** `src/Explore.Application/Authorization/IAuthorizationContextEnricher.cs` (new), `src/Explore.Application/Authorization/AuthorizationContext.cs` (new)
  - **Acceptance:** Interface has `Task<AuthorizationContext> ResolveAsync(TRequest, CancellationToken)`. Record has `ResourceId` and `Attributes`.
  - **Effort:** S
  - **Dependencies:** None

- [ ] **2.2 Create per-command authorization enrichers (12 enrichers)**
  - **Files:** 12 new enricher files co-located with their command handlers in `src/Explore.Application/Features/`
  - **Acceptance:** Each enricher resolves the same authorization context as the current else-if branch. Enrichers do NOT mutate commands. Each enricher has unit tests.
  - **Effort:** XL
  - **Dependencies:** 2.1

- [ ] **2.3 Slim AuthorizationBehavior to O(1) dispatch**
  - **Files:** `src/Explore.Application/Behaviors/AuthorizationBehavior.cs` (existing, 818 lines)
  - **Acceptance:** Constructor has ≤4 params (`IAuthorizationProvider`, `ILogger`, optional `IAuthorizationContextEnricher<TRequest>`, optional `ITenantContext`). No else-if branches. Fails closed. All existing `AuthorizationBehaviorTests` pass.
  - **Effort:** L
  - **Dependencies:** 2.2, Phase 0 characterization tests green

- [ ] **2.4 Move command mutations to handlers**
  - **Files:** 12 handler files that currently receive mutations from `AuthorizationBehavior`
  - **Acceptance:** Each handler sets its own enrichment properties (`TenantId`, `EventId`, etc.) after authorization. No mutation happens in authorization pipeline.
  - **Effort:** M
  - **Dependencies:** 2.3

- [ ] **2.5 Register enrichers in DI and remove obsolete IAuthorizedRequest**
  - **Files:** DI registration file, `src/Explore.Application/Authorization/IAuthorizedRequest.cs`
  - **Acceptance:** All enrichers registered as `IAuthorizationContextEnricher<TConcreteCommand>`. `IAuthorizedRequest` removed (already `[Obsolete]` with 0 usages).
  - **Effort:** S
  - **Dependencies:** 2.4

#### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

---

### Phase 3: Architecture Test Gates ⏳ NOT STARTED

- [ ] **3.1 Add AuthorizationBehavior dependency isolation test**
  - **Files:** `tests/Event.Architecture.Tests/` (new test class)
  - **Acceptance:** NetArchTest rule: `AuthorizationBehavior` must NOT depend on `Explore.Application.Features` namespace. Test passes.
  - **Effort:** S
  - **Dependencies:** Phase 2

- [ ] **3.2 Add string-based error classification ban test**
  - **Files:** `tests/Event.Architecture.Tests/` (new test class)
  - **Acceptance:** Source scan test: No controller file contains `Message?.Contains` pattern. Test passes.
  - **Effort:** S
  - **Dependencies:** Phase 1

#### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

### Phase 4: Keycloak Error Code Typing ⏳ NOT STARTED

*Independent of other phases. Can be parallelized.*

- [ ] **4.1 Add KeycloakFailureCodes constant class and replace inline strings**
  - **Files:** `src/Explore.Infrastructure/Services/Keycloak/KeycloakFailureCodes.cs` (new), `src/Explore.Infrastructure/Services/Keycloak/KeycloakBootstrapService.cs` (existing)
  - **Acceptance:** ~12 inline string literals replaced with constant references. Zero behavioral change. `KeycloakFailureCodes` follows `FailureCodes` pattern.
  - **Effort:** S
  - **Dependencies:** None

#### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1`

---

### Phase 5: Controller Deduplication ⏳ NOT STARTED

- [ ] **5.1 Extract TryParseConcurrencyStamp to ExploreControllerBase**
  - **Files:** `src/Explore.API/Controllers/ExploreControllerBase.cs` (existing), `src/Explore.API/Controllers/EventController.cs` (existing), `src/Explore.API/Controllers/OrganizationController.cs` (existing)
  - **Acceptance:** Single `TryParseConcurrencyStamp` method in base class. Both controllers use it. No behavioral change.
  - **Effort:** S
  - **Dependencies:** None

- [ ] **5.2 Migrate controllers to use MapCommandResponse where applicable**
  - **Files:** Controller files with repetitive response mapping
  - **Acceptance:** Controllers with simple `NotFound`/`AdminRequired`/default patterns use `MapCommandResponse`. Controllers with domain-specific patterns (storage, reports, AI) keep their specialized mappers.
  - **Effort:** M
  - **Dependencies:** 1.2, 1.5

#### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

---

### Phase 6: Attribute Type Resolution Helper ⏳ NOT STARTED

*Independent of other phases. Can be parallelized.*

- [ ] **6.1 Create AttributeResolver utility and replace inline type-casting**
  - **Files:** `src/Explore.Application/Helpers/AttributeResolver.cs` (new), `src/Explore.Infrastructure/Services/FallbackAuthorizationService.Evaluators.cs` (existing)
  - **Acceptance:** `TryGetGuid` and `TryGetInt` methods replace ~8 inline type-casting blocks. Unit tests for the utility. Zero behavioral change.
  - **Effort:** S
  - **Dependencies:** None

#### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

---

## Remaining / Deferred Work

- **Roslyn analyzer enforcement** (CA1502, CA1506): Deferred. Current
  `TreatWarningsAsErrors=false`. Evaluate after Phase 3 architecture tests
  prove value. Risk of build noise.
- **Cerbos-Fallback parity tests**: Out of scope. Recommended as separate
  workstream.
- **ATProto characterization tests**: Out of scope. Recommended as separate
  workstream.
- **FailureCode audit for remaining handlers**: Phase 1 covers the 36
  controller-facing handlers. Other handlers without FailureCode should be
  addressed incrementally.
