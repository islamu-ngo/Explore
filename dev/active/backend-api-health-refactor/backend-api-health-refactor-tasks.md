<!-- ABOUTME: Tactical checklist for the backend/API health refactor implementation plan. -->
<!-- ABOUTME: Tracks reviewable slices, exact acceptance criteria, and verification commands. -->

# Backend API Health Refactor - Task Checklist

Last Updated: 2026-07-03 Europe/Brussels

## Status Summary

- **Overall status:** Implementation in progress.
- **Completed in this implementation slice:** Plan/context/tasks rewritten; user implementation approval recorded; current worktree inspected; first source slice selected; health response redaction implemented; health path product-doc drift corrected.
- **Current priority:** Finish Phase 0.3 endpoint inventory/risk-register reconciliation and Phase 0.4 full blocker recheck.
- **Next recommended slice:** Phase 0.3 and 0.4 before Phase 1 security/HAL work.

## Implementation Maintenance Rules

- [x] Before starting implementation, read plan/context/tasks.
- [x] Classify the exact implementation slice against `.claude/contract/intents.yaml`.
- [x] Read required docs/rules/skills for the files you will edit.
- [x] Inspect current `git status --short` and avoid unrelated dirty files.
- [x] After each completed task, update this checklist immediately.
- [ ] If architecture/scope changes, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Every final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Current-State Re-baseline

- [x] **0.1 User reviews and approves or corrects this re-baselined plan.**
  - **Files:** `backend-api-health-refactor-plan.md`, `backend-api-health-refactor-context.md`, `backend-api-health-refactor-tasks.md`.
  - **Acceptance:** Planning status becomes User-reviewed or Approved in plan/context.
  - **Validation:** User directive on 2026-07-03: "fully implement the implementation plan".
  - **Effort:** S.
  - **Dependencies:** none.

- [x] **0.2 Record current worktree state before source edits.**
  - **Files:** `backend-api-health-refactor-context.md`.
  - **Acceptance:** Context records current dirty source/doc files relevant to this workstream and states which changes are unrelated.
  - **Validation:** `git status --short` captured a heavily dirty worktree with many unrelated source/doc/test changes. This slice avoided unrelated changes except for a minimal compile fix in an already-modified storage upload validator that blocked API health-test execution.
  - **Effort:** S.
  - **Dependencies:** 0.1.

- [ ] **0.3 Reconcile endpoint inventory and risk register with current source/OpenAPI.**
  - **Files:** `endpoint-inventory.md`, `backend-contract-risk-register.md`, generated OpenAPI/inventory artifacts only through documented generation commands if source contract changed.
  - **Acceptance:** Inventory rows are marked current or stale; high/critical rows link to risk IDs; no manual edits to generated `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, or `EventApiClient.g.cs`.
  - **Validation:** Contract-generation workflow when needed; otherwise source/readback evidence.
  - **Effort:** M.
  - **Dependencies:** 0.2.

- [ ] **0.4 Recheck verification blockers.**
  - **Files:** `backend-api-health-refactor-context.md`.
  - **Acceptance:** Current status is known for architecture context failures, API integration Docker/Testcontainers/host-shutdown issue, and Blazor build issue.
  - **Partial evidence 2026-07-03:** ServiceDefaults and Application project builds run; repo-level Release build passes; focused API health writer tests build and pass. Architecture tests, Docker/Testcontainers lanes, and full API integration suite are not yet rechecked.
  - **Validation:** Run only scoped commands needed to classify blockers; do not launch full suite unless source changes require it.
  - **Effort:** M.
  - **Dependencies:** 0.2.

- [x] **0.5 Decide first implementation PR boundary.**
  - **Files:** plan/context/tasks.
  - **Acceptance:** First source slice selected as Phase 2.5/2.6 health response redaction and health path doc reconciliation.
  - **Validation:** Context names changed files and tests.
  - **Effort:** S.
  - **Dependencies:** 0.3, 0.4.

## Phase 1: Security, Authorization, Tenant, And HAL Corrections

- [ ] **1.1 Finish P0 identity-bearing endpoint hardening.**
  - **Files:** `EventRegistrationController`, `TenantUserRoleGrantController`, `OrganizationMemberController`, related handlers/DTOs/tests as verified in Phase 0.
  - **Acceptance:** Anonymous requests fail or return safe public projections; identity/member/role/grant fields are not leaked.
  - **Validation:** API integration auth/field-shape tests; endpoint classification architecture tests.
  - **Effort:** L.
  - **Dependencies:** Phase 0.

- [ ] **1.2 Complete resource-action authorization parity for high-risk endpoint families.**
  - **Files:** `authorization-policy-matrix.md`, Application auth metadata, Cerbos/local fallback policies, API attributes.
  - **Acceptance:** Event registrations, tenant role grants, organization members, footer writes, AI assistant, storage objects, tenant storage, email admin, and bootstrap/admin flows have explicit resource/action policy decisions or documented deferrals.
  - **Validation:** `AuthorizationParityTests`, Application unit tests, API 401/403 ProblemDetails tests.
  - **Effort:** L.
  - **Dependencies:** 1.1.

- [ ] **1.3 Close HAL affordance drift.**
  - **Files:** HAL policies/assemblers and Blazor client components identified by Phase 0.
  - **Acceptance:** Edit/delete/publish/create/admin actions render from `_links`; local role/claim checks are limited to navigation/route/menu-level UX.
  - **Validation:** API HAL tests and `Explore.Blazor.Client.Tests` bUnit tests.
  - **Effort:** M.
  - **Dependencies:** 1.2.

- [ ] **1.4 Add semantic tenant-bypass proof for remaining bypass call sites.**
  - **Files:** Persistence repositories/services using tenant bypass, `tenant-execution-model.md`, tests.
  - **Acceptance:** Each bypass has a bounded predicate, reason, operation name, and test proving it does not leak the ambient/wrong tenant.
  - **Validation:** `Event.Persistence.IntegrationTests`; architecture guard that controllers cannot call raw bypass helpers.
  - **Effort:** M.
  - **Dependencies:** Phase 0.

- [ ] **1.5 Finish bootstrap/admin operational safety.**
  - **Files:** onboarding handlers/controllers, Keycloak/provider sync paths, audit infrastructure, docs.
  - **Acceptance:** Setup secret remains bootstrap-only; missing provider uses typed ProblemDetails; audit events emit for bootstrap start/success/failure/disablement; Keycloak rotation/sync tests cover backup-confirmed flows.
  - **Validation:** Application unit tests, API integration tests, operations/security docs review.
  - **Effort:** L.
  - **Dependencies:** 1.2.

## Phase 2: API Contract, Error Catalog, OpenAPI, And Operational Health

- [ ] **2.1 Reconfirm current ProblemDetails migration state.**
  - **Files:** `Explore.API/ExceptionHandling/**`, controllers, `api-error-catalog.md`, risk register.
  - **Acceptance:** Remaining ad hoc `BadRequest`, `Forbid`, `Unauthorized`, `Problem`, and raw command-envelope paths are listed with owner tasks or confirmed absent.
  - **Validation:** `rg`/CodeGraph source evidence plus architecture tests.
  - **Effort:** M.
  - **Dependencies:** Phase 0.

- [ ] **2.2 Add behavior-level ProblemDetails tests for representative status families.**
  - **Files:** `Event.API.IntegrationTests`.
  - **Acceptance:** 400 validation, 401 authentication required, 403 forbidden, 404 not found, 409 conflict/concurrency/duplicate, 429 rate-limited, and 500 production-safe examples have deterministic shapes.
  - **Validation:** API integration tests/snapshots with volatile fields scrubbed.
  - **Effort:** M.
  - **Dependencies:** 2.1.

- [ ] **2.3 Preserve route-name and HAL route guardrails.**
  - **Files:** `RouteNames`, HAL policies, route-name tests.
  - **Acceptance:** Every named endpoint maps to a constant and every constant resolves to exactly one endpoint after current branch changes.
  - **Validation:** `RouteNameCoverageTests`.
  - **Effort:** S-M.
  - **Dependencies:** Phase 0.

- [ ] **2.4 Regenerate OpenAPI/client artifacts only after source contract is stable.**
  - **Files:** `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CHANGELOG.md`.
  - **Acceptance:** Regenerated artifacts match source; breaking changes are documented; no generated artifact is hand-edited.
  - **Validation:** documented API build/client generation workflow plus contract tests.
  - **Effort:** M.
  - **Dependencies:** 2.1, 2.3 and any source contract slice.

- [x] **2.5 Audit health endpoint payload safety.**
  - **Files:** `Explore.ServiceDefaults/Extensions.cs`, `Explore.ServiceDefaults/HealthChecks/HealthCheckResponseWriter.cs`, `Event.API.IntegrationTests/Features/HealthCheckResponseWriterTests.cs`, `Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`.
  - **Acceptance:** `/health` and `/alive` responses no longer serialize raw `Exception.Message`; shared writer redacts suspicious descriptions and sensitive data keys/values while preserving bounded booleans/status/failure-code fields.
  - **Validation:** `dotnet build Explore.ServiceDefaults/Explore.ServiceDefaults.csproj --configuration Release --verbosity quiet`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/HealthCheckResponseWriterTests/*" --minimum-expected-tests 1`.
  - **Effort:** M.
  - **Dependencies:** Phase 0.

- [x] **2.6 Reconcile health path documentation.**
  - **Files:** `docs/MCP_DEBUGGING.md`, `docs/CONFIGURATION.md`.
  - **Acceptance:** Operator docs consistently state project paths: `/health` readiness, `/alive` liveness, `/metrics` Prometheus.
  - **Validation:** `rg -n "/health/ready|/health/live" docs` returns no product-doc matches.
  - **Effort:** S.
  - **Dependencies:** 2.5 can run before or after.

## Phase 3: Controller Decomposition Behind Stable Contracts

- [ ] **3.1 Select one controller/resource family for decomposition.**
  - **Files:** refreshed inventory, chosen controller, risk register.
  - **Acceptance:** Decision states why the split reduces complexity and which routes/contracts remain stable or intentionally break.
  - **Validation:** plan/context update.
  - **Effort:** S.
  - **Dependencies:** Phase 2 contract guardrails for that family.

- [ ] **3.2 Add characterization tests before splitting.**
  - **Files:** `Event.API.IntegrationTests`.
  - **Acceptance:** Current behavior for route names, status codes, HAL links, ProblemDetails, auth, and OpenAPI metadata is captured.
  - **Validation:** focused API tests pass/fail for the right reason.
  - **Effort:** M.
  - **Dependencies:** 3.1.

- [ ] **3.3 Split by resource/use-case cohesion.**
  - **Files:** chosen controller plus API-local mapper/services if needed.
  - **Acceptance:** Controllers dispatch MediatR, assemble HAL, map results; no repository injection or business orchestration.
  - **Validation:** API tests, architecture tests, OpenAPI diff review.
  - **Effort:** M-L.
  - **Dependencies:** 3.2.

- [ ] **3.4 Update API docs/changelog for any route/contract changes.**
  - **Files:** `docs/API.md`, `docs/API_CHANGELOG.md`, generated artifacts if required.
  - **Acceptance:** Docs match implemented route/contract behavior.
  - **Validation:** docs readback and contract tests.
  - **Effort:** S-M.
  - **Dependencies:** 3.3.

## Phase 4: Application/CQRS Use-Case Refactor

- [ ] **4.1 Select one oversized handler or query-envelope family.**
  - **Files:** refreshed risk register and chosen Application feature.
  - **Acceptance:** One slice is selected with tests, dependencies, and rollback/failure handling.
  - **Validation:** context update.
  - **Effort:** S.
  - **Dependencies:** Phase 1/2 where the endpoint is security/contract-sensitive.

- [ ] **4.2 Add characterization tests before refactor.**
  - **Files:** `Event.Application.UnitTests`, API tests if external response changes.
  - **Acceptance:** Existing behavior is covered before extraction or response-shape change.
  - **Validation:** targeted tests fail/pass as expected.
  - **Effort:** M.
  - **Dependencies:** 4.1.

- [ ] **4.3 Decompose handlers into narrow collaborators.**
  - **Files:** chosen handler, new Application services/collaborators, DI registration.
  - **Acceptance:** Handler coordinates validation, authorization context, transaction, collaborators, cache/audit/idempotency/concurrency, and response composition. Collaborators do not bypass MediatR behaviors or independently save inside the unit of work unless explicitly designed.
  - **Validation:** Application unit tests and architecture tests.
  - **Effort:** L.
  - **Dependencies:** 4.2.

- [ ] **4.4 Normalize query response contracts.**
  - **Files:** selected query requests/handlers/controllers/client contracts.
  - **Acceptance:** Queries return DTOs, nullable DTOs, `IReadOnlyList<TDto>`, or `PaginatedResult<TDto>`; command envelopes are removed from read data paths.
  - **Validation:** Application unit tests, API contract tests, OpenAPI/client regeneration when public contract changes.
  - **Effort:** M-L.
  - **Dependencies:** 4.1.

- [ ] **4.5 Add endpoint-specific idempotency, concurrency, and audit behavior.**
  - **Files:** chosen command handlers, middleware/config where applicable, audit/outbox services, tests.
  - **Acceptance:** Each high-risk write has an explicit posture: idempotent replay, duplicate conflict, optimistic concurrency, audit event, or documented deferral.
  - **Validation:** Application/API behavior tests; not architecture-only.
  - **Effort:** L.
  - **Dependencies:** 4.2.

## Phase 5: Persistence, Query Shape, Pagination, Indexes, And Reliability

- [ ] **5.1 Select persistence hotspots from evidence.**
  - **Files:** refreshed risk register, repositories/specs/configs/tests.
  - **Acceptance:** One repository/query/index/transaction risk is selected with expected behavior and test lane.
  - **Validation:** context update.
  - **Effort:** S.
  - **Dependencies:** Phase 0.

- [ ] **5.2 Separate read-only and mutation loading semantics.**
  - **Files:** selected repository contracts/implementations.
  - **Acceptance:** Read-only paths use `AsNoTracking`; mutation paths keep tracked loads where needed. No global generic-repository change without tests.
  - **Validation:** persistence tests and Application tests for update flows.
  - **Effort:** M.
  - **Dependencies:** 5.1.

- [ ] **5.3 Remove DTO-shaped repository coupling where selected.**
  - **Files:** e.g. `IEventAggregateViewRepository` or another verified target.
  - **Acceptance:** Persistence receives entities/specifications/query objects/read-model ports, not Application DTO filters.
  - **Validation:** architecture tests and repository/Application tests.
  - **Effort:** M.
  - **Dependencies:** 5.1.

- [ ] **5.4 Quarantine hard delete behavior.**
  - **Files:** `IGenericRepository`, concrete callers, admin lifecycle services/tests.
  - **Acceptance:** Production hard delete requires explicit admin/lifecycle path with authorization, audit, and tests, or remains unavailable.
  - **Validation:** architecture tests plus API/Application behavior tests for callers.
  - **Effort:** M-L.
  - **Dependencies:** 5.1.

- [ ] **5.5 Add cursor/keyset pagination only to selected high-volume endpoints.**
  - **Files:** selected API/Application/Persistence contracts.
  - **Acceptance:** Cursor format is opaque/versioned, bound to tenant/filter/sort where needed, stable under insertion/deletion, documented, and indexed.
  - **Validation:** API + persistence pagination tests; OpenAPI/client regeneration.
  - **Effort:** L.
  - **Dependencies:** 5.1 and contract approval.

- [ ] **5.6 Review indexes/migrations for selected query paths.**
  - **Files:** EF configurations, migrations, model assertions.
  - **Acceptance:** Indexes match query predicates/order; migration has rollback/reset notes for self-hosters; no partitioning unless ADR activation gates are met.
  - **Validation:** persistence model/index assertions and migration tests.
  - **Effort:** M-L.
  - **Dependencies:** 5.1.

- [ ] **5.7 Harden transaction/retry/outbox boundaries for selected background state transitions.**
  - **Files:** outbox/TickerQ/RabbitMQ/email dispatch selected services.
  - **Acceptance:** Manual transactions use `IUnitOfWork` or EF execution strategy; durable side effects are idempotent; ambiguous provider outcomes are recorded safely.
  - **Validation:** Application/Infrastructure/Persistence tests as appropriate.
  - **Effort:** L.
  - **Dependencies:** 5.1.

## Phase 6: Final Guardrails, Documentation, And Release Evidence

- [ ] **6.1 Resolve or explicitly defer all open high/critical risk rows.**
  - **Files:** `backend-contract-risk-register.md`.
  - **Acceptance:** Each row has status, owner, mitigation/detection, and next action or defer rationale.
  - **Validation:** readback.
  - **Effort:** M.
  - **Dependencies:** Phases 1-5 as applicable.

- [ ] **6.2 Convert temporary skips/failures into hard gates or explicit deferrals.**
  - **Files:** test projects and risk register.
  - **Acceptance:** No hidden permanent skips; every skip uses `Category:` and `Removal:`.
  - **Validation:** `Event.Architecture.Tests` skip governance.
  - **Effort:** M.
  - **Dependencies:** implementation slices.

- [ ] **6.3 Update product/operator docs.**
  - **Files:** `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/OPERATIONS.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/MULTI_TENANCY.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md` as touched.
  - **Acceptance:** Docs describe actual behavior, config, failure modes, upgrade/recovery notes, and breaking changes.
  - **Validation:** docs readback and architecture doc-quality tests where applicable.
  - **Effort:** M.
  - **Dependencies:** source slices complete.

- [ ] **6.4 Run final targeted verification.**
  - **Files:** context/tasks update with evidence.
  - **Acceptance:** Build and intent-derived per-project test commands are recorded, or blockers are documented with root cause and next recovery action.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet` plus project-level tests required by touched slices.
  - **Effort:** M-L.
  - **Dependencies:** 6.1-6.3.

## Verification Checklist

- [x] LSP diagnostics clean for modified source files when source is touched.
- [x] `dotnet build --configuration Release --verbosity quiet` passes before implementation handoff, unless a pre-existing blocker is documented.
- [ ] Intent minimum test projects pass individually with `dotnet test --project ... --configuration Release --verbosity quiet`.
- [x] API integration tests cover health behavior touched.
- [ ] Application unit tests cover handler orchestration, idempotency, concurrency, audit, and query contracts touched.
- [ ] Persistence integration tests cover tenant filters, bypasses, query/index/migration behavior touched.
- [ ] Blazor client tests cover HAL affordance gating touched.
- [ ] OpenAPI/client artifacts regenerated through documented workflow when public API contract changes.
- [x] Docs updated for health path behavior touched.
- [x] Dev docs refreshed before handoff.

## Remaining / Deferred Work

- Domain `DataAnnotations`/persistence annotation cleanup is not a Phase 6 catch-all. Create a dedicated slice/workstream unless Phase 0 selects a narrow aggregate with tests.
- PostgreSQL partitioning remains deferred per ADR-009 until activation gates are met.
- New `/health/ready` or `/health/live` aliases are deferred unless the user approves an operational endpoint migration plan.
- Repo-wide repository cancellation/no-tracking changes are deferred unless tied to selected hotspots and tests.
- Broad controller decomposition is deferred until security/HAL/API contract guardrails are green for the target family.
