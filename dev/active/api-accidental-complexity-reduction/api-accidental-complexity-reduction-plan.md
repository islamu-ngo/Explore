<!-- ABOUTME: Approved implementation plan to eliminate accidental complexity in the API and Application layers. -->
<!-- ABOUTME: Targets string-based error classification (36 controller sites), AuthorizationBehavior God object (818 lines), and controller duplication. -->

# API Accidental Complexity Reduction Plan

Last Updated: 2026-08-06 Europe/Brussels

## Status

- Planning status: **Approved**
- Implementation status: **Not started**
- Architecture direction: **Approved**
- First implementation gate: Phase 0 characterization tests before any refactoring
- Scope of this revision: planning documents only; no production code changed

## Executive Decision

The API codebase has two systemic accidental-complexity problems that this plan eliminates:

1. **String-based error classification.** 36 controller sites inspect `response.Message?.Contains("not found")` or `"administrators"` to select HTTP status codes. Renaming a handler message silently breaks status mapping. The `FailureCode` property already exists on `BaseCommandResponse<TKey>` but is only used in 105 of 264 command handlers.

2. **Authorization God Object.** `AuthorizationBehavior.cs` (818 lines, 21 constructor parameters, 12 `else-if` type-cast branches) violates the Open-Closed Principle and creates merge-conflict magnets. It mutates commands (sets TenantId, EventId, etc.) and performs pessimistic locking inside the authorization pipeline.

Both problems are grounded in verified evidence from 8 auditor subagents across 6 projects.

## Goals

- Replace all 36 string-inspection controller sites with typed `FailureCode` switching.
- Propagate `FailureCode` to all 159 handlers that currently use Message-only failure indication.
- Decompose `AuthorizationBehavior` from 818 lines to ~40 lines via per-command `IAuthorizationContextEnricher<TRequest>` implementations.
- Move command mutations out of the authorization pipeline into handlers.
- Add architecture test gates preventing regression of both anti-patterns.
- Deduplicate shared controller logic (e.g., `TryParseConcurrencyStamp`).
- Centralize Keycloak error strings and dictionary type-casting.

## Non-Goals

- Refactoring essential-complexity components (see Leave-Alone List).
- Roslyn analyzer enforcement (CA1502/CA1506). Deferred until architecture tests prove value.
- Cerbos-Fallback parity tests. Separate workstream.
- ATProto characterization tests. Separate workstream.
- Backward compatibility. The application is in development mode.

## Leave-Alone List (Essential Complexity — DO NOT REFACTOR)

1. **FallbackAuthorizationService** (1,353 lines) — IS the authorization policy.
2. **RegistrationOrderLifecycleService** (1,044 lines) — 13 state transitions.
3. **HierarchicalSettingsResolver** (524 lines) — Correct ordered reducer.
4. **EmailDispatchEligibilityEvaluator** (450 lines) — Correct sequential pipeline.
5. **AtprotoJetstreamRepository** (1,400 lines) — Protocol-mandated.
6. **KeycloakBootstrapService reconciliation flow** (2,952 lines) — Desired-state reconciliation.
7. **GetEventListRequestHandler.BuildSpecificationAsync** (190 lines) — Already specification pattern.
8. **EventLifecycleReadinessEvaluator** (200 lines) — Stateless evaluator.

## Architecture Invariants

1. Repositories return entities, not DTOs.
2. Validators are manually instantiated (no DI).
3. GET = `[AllowAnonymous]`, write = `[Authorize]`.
4. Every file starts with a two-line `ABOUTME:` comment summary.
5. HAL links are the single source of truth for UI.
6. Clean Architecture: Domain → Application → Infrastructure → API.
7. Never hand-edit EF Core migrations.
8. `FailureCode` is `string?` on `BaseCommandResponse<TKey>` — typed constants in `FailureCodes.cs`.
9. Error-to-HTTP mapping uses extension methods on `ControllerBase` in `CommandResponseResultMapper.cs`, not base class methods.
10. Authorization is fail-closed: commands without an enricher use `[AuthorizeResource]` attribute + `ISecureRequest` interface defaults.

## Source-Grounded Evidence

### Error Handling Current State

| Evidence | Source | Finding |
|----------|--------|---------|
| `BaseCommandResponse<TKey>` shape | `src/Explore.Application/Responses/BaseCommandResponse.cs` | Properties: `Id`, `Success`, `Message`, `Errors`, `FailureCode` (string?), `QuotaExceeded` |
| Existing failure codes | `src/Explore.Application/Responses/FailureCodes.cs` | 11 codes: `QuotaExceeded`, 9 storage upload codes, `DeploymentModeChangeBlockedByActiveTenants` |
| String inspection sites | 36 controller files | 22 × `.Contains("not found")` → 404, 7 × `.Contains("administrators")` → 403, 11 × exact string match → 404 |
| Handler FailureCode usage | 264 command handlers in `Features/` | 105 set FailureCode, 159 use Message-only |
| `MapCommandResponse` | Does NOT exist | Must be created as extension method |
| `ToConflictProblem` | Does NOT exist | Must be created as extension method |
| Existing specialized mappers | `CommandResponseResultMapper.cs` | `ToCommandValidationProblem`, `ToCommandConflictProblem`, `ToStorageUploadProblem`, `ToEventReportProblem`, `ToAuthProviderProblem`, `ToEmailDispatchProblem`, `ToAiAssistantProblem` |

### Authorization Current State

| Evidence | Source | Finding |
|----------|--------|---------|
| AuthorizationBehavior size | `src/Explore.Application/Behaviors/AuthorizationBehavior.cs` | 818 lines, 21 constructor params (2 required + 19 optional) |
| Type-cast branches | Same file | 12 `else-if (request is ConcreteCommand)` branches |
| Command mutation | Same file | Sets TenantId, EventId, etc. on concrete command objects |
| `ISecureRequest` | `src/Explore.Application/Authorization/ISecureRequest.cs` | Interface with default-null `ResourceId` and `ResourceAttributes` |
| `AuthorizeResourceAttribute` | `src/Explore.Application/Authorization/AuthorizeResourceAttribute.cs` | `[AttributeUsage(Class)]` with `Resource` and `Action` properties |
| `IAuthorizedRequest` | `src/Explore.Application/Authorization/IAuthorizedRequest.cs` | `[Obsolete]` — 0 production usages |
| `IAuthorizationContextEnricher` | Does NOT exist | Must be created |
| Implementations of `ISecureRequest` | 35+ request classes | Already provide `ResourceId` and `ResourceAttributes` |
| Uses of `[AuthorizeResource]` | 300+ request classes | Already specify `Resource` and `Action` |

### Controller Duplication

| Evidence | Source | Finding |
|----------|--------|---------|
| `TryParseConcurrencyStamp` | `EventController.cs` L1033-1049, `OrganizationController.cs` L400-416 | Identical 17-line method duplicated |
| Controller count | `src/Explore.API/Controllers/` | 117 controller files |

### Test Infrastructure

| Evidence | Source | Finding |
|----------|--------|---------|
| Auth behavior tests | `tests/Event.Application.UnitTests/Behaviors/AuthorizationBehaviorTests.cs` | 1,601 lines of existing coverage |
| FailureCode tests | Multiple integration test files | 600+ test locations |
| Architecture framework | `Event.Architecture.Tests` | NetArchTest.Rules 1.3.2 already referenced |
| Build props | `Directory.Build.props` | `TreatWarningsAsErrors=false`, `AnalysisMode=Recommended` |

## Architecture Decisions

### ADR-1: Typed FailureCodes over string Messages

- **Decision:** Add `NotFound`, `AdminRequired`, `ConcurrencyConflict` (and more as needed) to `FailureCodes.cs`. All 264 handlers set `FailureCode`.
- **Rationale:** Eliminates fragile `.Contains()` in controllers. `FailureCode` property already exists on `BaseCommandResponse<TKey>`.
- **Naming convention:** Use `snake_case` (matching existing `quota_exceeded`, `storage_upload_too_large`).
- **Consequence:** 159 handler files require mechanical update; each adds `FailureCode = FailureCodes.NotFound` alongside existing `Message`.

### ADR-2: IAuthorizationContextEnricher<TRequest> interface

- **Decision:** Create `IAuthorizationContextEnricher<TRequest>` resolved via DI for O(1) dispatch. Each enricher is co-located with its command handler.
- **Rationale:** Replaces 12 `else-if` branches with decentralized, single-responsibility enrichers.
- **Consequence:** `AuthorizationBehavior` shrinks from 818 to ~40 lines. 12 enricher classes created.

### ADR-3: Extension methods for response mapping

- **Decision:** Add `MapCommandResponse<TKey>` as an extension method in `CommandResponseResultMapper.cs`, NOT as a base class method.
- **Rationale:** Follows existing pattern. All other mappers (`ToNotFoundProblem`, `ToForbiddenProblem`, etc.) are already extension methods.

### ADR-4: Command mutations move to handlers

- **Decision:** Authorization pipeline becomes non-mutating. Each handler sets its own enrichment properties (TenantId, EventId, etc.) after authorization.
- **Rationale:** Mutations inside authorization violate separation of concerns and make the behavior hard to test.

## Implementation Phases

### Phase 0: Characterization Test Baseline

**Goal:** Write tests capturing current behavior before any refactoring.

**Depends on:** Nothing.

**Files:**
- `tests/Event.Application.UnitTests/Behaviors/AuthorizationBehaviorTests.cs` (extend existing 1,601-line file)
- `tests/Event.API.IntegrationTests/Features/` (new error mapping test class)

**Tasks:**
- 0.1 Extend AuthorizationBehavior tests to cover all 12 mutation branches
- 0.2 Add controller error-mapping characterization tests (not-found → 404, administrators → 403)

**Acceptance:**
- Each of the 12 else-if branches has a test for: (a) correct command mutation, (b) correct authorization context extraction, (c) correct repository call.
- Controller tests verify: `Message="X not found"` → 404, `Message` contains `"administrators"` → 403, default → 400.

**Phase-end verification:**
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

**Rollback:** Revert test additions.

---

### Phase 1: Typed Error Contract

**Goal:** Eliminate 36 string-inspection sites in API controllers.

**Depends on:** Phase 0.

**Files:**
- `src/Explore.Application/Responses/FailureCodes.cs` (add constants)
- `src/Explore.API/ExceptionHandling/CommandResponseResultMapper.cs` (add `MapCommandResponse`)
- ~159 handler files in `src/Explore.Application/Features/` (add FailureCode)
- ~36 controller files in `src/Explore.API/Controllers/` (replace string inspection)

**Tasks:**
- 1.1 Add `NotFound`, `AdminRequired`, `ConcurrencyConflict` to `FailureCodes.cs`
- 1.2 Add `MapCommandResponse<TKey>` extension method to `CommandResponseResultMapper.cs`
- 1.3 Update ~80 "not found" handlers to set `FailureCode = FailureCodes.NotFound`
- 1.4 Update ~79 remaining handlers to set appropriate `FailureCode` values
- 1.5 Replace string inspection in ~36 controllers with `FailureCode` switch or `MapCommandResponse`

**Acceptance:**
- Zero occurrences of `Message?.Contains("not found")` or `Message?.Contains("administrators")` in controllers.
- All 264 command handlers set `FailureCode` on failure.
- `MapCommandResponse` handles `NotFound` → 404, `AdminRequired` → 403, `ConcurrencyConflict` → 409, default → 400.

**Phase-end verification:**
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

**Rollback:** `git reset --hard` to pre-Phase 1 state.

---

### Phase 2: Authorization Behavior Redesign

**Goal:** Decompose the 818-line God object into per-command enrichers.

**Depends on:** Phase 0 (characterization tests green).

**Files:**
- `src/Explore.Application/Authorization/IAuthorizationContextEnricher.cs` (new)
- `src/Explore.Application/Authorization/AuthorizationContext.cs` (new)
- 12 new enricher files co-located with handlers in `src/Explore.Application/Features/`
- `src/Explore.Application/Behaviors/AuthorizationBehavior.cs` (reduce 818 → ~40 lines)
- 12 handler files (receive mutations from enricher outputs)
- DI registration
- `src/Explore.Application/Authorization/IAuthorizedRequest.cs` (delete)

**Tasks:**
- 2.1 Create `IAuthorizationContextEnricher<TRequest>` interface and `AuthorizationContext` record
- 2.2 Create 12 per-command enrichers co-located with handlers
- 2.3 Slim `AuthorizationBehavior` to O(1) DI dispatch (~40 lines)
- 2.4 Move command mutations to handlers
- 2.5 Register enrichers in DI, remove obsolete `IAuthorizedRequest`

**Acceptance:**
- Constructor has ≤4 params (`IAuthorizationProvider`, `ILogger`, optional `IAuthorizationContextEnricher<TRequest>`, optional `ITenantContext`).
- No `else-if` branches. Fails closed.
- Enrichers do NOT mutate commands.
- All 1,601+ existing `AuthorizationBehaviorTests` pass.

**Phase-end verification:**
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

**Rollback:** `git reset --hard` to pre-Phase 2 state.

---

### Phase 3: Architecture Test Gates

**Goal:** Add enforcement tests to prevent regressions.

**Depends on:** Phase 1 and Phase 2.

**Files:**
- `tests/Event.Architecture.Tests/` (new test classes)

**Tasks:**
- 3.1 Add NetArchTest rule: `AuthorizationBehavior` must NOT depend on `Explore.Application.Features` namespace
- 3.2 Add source scan test: No controller file contains `Message?.Contains` pattern

**Acceptance:**
- Both architecture tests pass and prevent future violations.

**Phase-end verification:**
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

---

### Phase 4: Keycloak Error Code Typing

**Goal:** Replace ~12 inline string literals in KeycloakBootstrapService.

**Depends on:** Nothing (independent).

**Files:**
- `src/Explore.Infrastructure/Services/Keycloak/KeycloakFailureCodes.cs` (new)
- `src/Explore.Infrastructure/Services/Keycloak/KeycloakBootstrapService.cs` (existing)

**Tasks:**
- 4.1 Create `KeycloakFailureCodes` constant class, replace ~12 inline strings

**Phase-end verification:**
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1
```

---

### Phase 5: Controller Deduplication

**Goal:** Extract shared helper logic.

**Depends on:** Phase 1.

**Files:**
- `src/Explore.API/Controllers/ExploreControllerBase.cs` (existing)
- `src/Explore.API/Controllers/EventController.cs` (existing)
- `src/Explore.API/Controllers/OrganizationController.cs` (existing)

**Tasks:**
- 5.1 Extract `TryParseConcurrencyStamp` to `ExploreControllerBase`
- 5.2 Migrate controllers to use `MapCommandResponse` where applicable

**Phase-end verification:**
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

---

### Phase 6: Attribute Type Resolution Helper

**Goal:** Centralize dictionary type-casting.

**Depends on:** Nothing (independent).

**Files:**
- `src/Explore.Application/Helpers/AttributeResolver.cs` (new)
- `src/Explore.Infrastructure/Services/FallbackAuthorizationService.Evaluators.cs` (existing)

**Tasks:**
- 6.1 Create `AttributeResolver` utility with `TryGetGuid` and `TryGetInt` methods
- 6.2 Replace ~8 inline type-casting blocks

**Phase-end verification:**
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

## Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection |
|------|-----------|--------|------------|----------|
| Missing an else-if branch during enricher extraction → broken auth | Low | **High** | Phase 0 characterization tests map 1:1 to branches | `AuthorizationBehaviorTests` failure |
| Dropping a pessimistic lock in AuthorizationBehavior | Medium | **High** | Explicit review during Task 2.3; integration tests | Race condition in integration tests |
| Unidentified string matching elsewhere in codebase | Medium | Low | Phase 3 source-scan architecture test | `ControllerRulesTests` failure |
| Handler with non-standard failure pattern breaks with FailureCode | Low | Low | Characterization tests in Phase 0; incremental handler updates | Unit test failure |

## Success Metrics

- **0** instances of `.Contains("not found")` or `.Contains("administrators")` in controllers.
- **`AuthorizationBehavior.cs`** under 100 lines.
- Architecture tests enforce string-inspection ban and auth dependency isolation.
- All phase-end `dotnet build` and `dotnet test` commands green.
- All 1,601+ existing `AuthorizationBehaviorTests` remain green.

## Documentation Impact

- **`docs/QUICK_REFERENCE.md`**: Update to document `FailureCode` enforcement convention and `IAuthorizationContextEnricher<TRequest>` pattern.
- **This plan**: Update phase status as implementation progresses.

## Security Considerations

- Decomposing `AuthorizationBehavior` must preserve exact order of operations: context enrichment happens before policy evaluation.
- Pessimistic locking currently inside `AuthorizationBehavior` must be transitioned without dropping concurrency safety.
- `ISecureRequest` remains the authoritative marker for protected endpoints.
- Authorization remains fail-closed: commands without an enricher fall back to `[AuthorizeResource]` attribute + `ISecureRequest` defaults.

## Multi-Tenancy Impact

- Tenant boundaries are enforced via properties (TenantId) set dynamically. The new enrichers maintain this boundary identically.
- No change to tenant isolation semantics.

## Observability Impact

- Strongly typed `FailureCode` strings improve structured log querying (e.g., searching for `FailureCode="not_found"` across all endpoints).
- No new metrics or trace changes.

## Implementation Agent Contract

Require implementation agents to:

1. Read plan, context, and tasks once at initial start; on resume, read context and tasks first, then only relevant plan sections.
2. Start from the highest-priority unchecked task.
3. Treat `tasks.md` as the hot execution ledger.
4. Update context after a phase, decision, blocker, or handoff.
5. Update the plan only when scope, architecture, or acceptance criteria change.
6. Run phase verification only after all phase tasks complete.
7. Never report completion when repository reality and the task ledger disagree.

Require every implementation summary to teach: what changed and why; architecture patterns used; important files and responsibilities; data/control flow; verification performed; remaining work.

## Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```
