# Cerbos Enterprise Authorization Review Plan

Last Updated: 2026-02-12

## Executive Summary

This plan defines an extensive, enterprise-grade refactor and hardening strategy for Cerbos-based authorization across API, Application, Infrastructure, and Blazor layers in the Explore solution. The current implementation already has strong foundations (MediatR authorization behavior, fallback authorization service, HATEOAS link filtering, and DB-first admin claim transformation), but it is not yet at enterprise-grade maturity in observability, test completeness, asynchronous boundaries, and operational governance.

The target state is a policy-driven, auditable, resilient, and maintainable authorization platform with:
- clear separation of concerns per Clean Architecture,
- deterministic policy decision paths,
- complete test and policy validation gates in CI,
- measurable SLOs for authorization latency and reliability,
- and documentation aligned with current implementation.

## Current State Analysis

### What is working well

1. Clean layering and dependency direction are respected:
- Application uses contracts and behavior (`Explore.Application/Behaviors/AuthorizationBehavior.cs`).
- Infrastructure implements policy engine adapter and fallback (`Explore.Infrastructure/Services/CerbosAuthorizationService.cs`, `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`).
- API composes and evaluates link authorization (`Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`).

2. Authorization model centralization exists:
- Typed action mapping in `Explore.Application/Authorization/CerbosPermissionAction.cs`.
- DTO-to-resource mapping in `Explore.Application/Authorization/CerbosResourceDescriptorRegistry.cs`.
- Admin claim-type constants in `Explore.Application/Authorization/AdminClaimTypes.cs`.

3. DB-first authority model is implemented:
- Claims transformation from admin context (`Explore.Infrastructure/Identity/AdminClaimsTransformation.cs`).
- Claims serialized to WASM (`Explore.Blazor/Program.cs:128`).

4. Link-level authorization and command-level authorization are both in place:
- HATEOAS batch authorization (`Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`).
- MediatR behavior with `[CerbosAuthorize]`, `IAuthorizedRequest`, `ISecureRequest` (`Explore.Application/Behaviors/AuthorizationBehavior.cs`).

### High-impact gaps

1. Documentation drift:
- `docs/SECURITY.md:57` still states Cerbos is not integrated, which is now incorrect.

2. Async boundary issue in HATEOAS evaluator:
- Synchronous blocking call (`GetAwaiter().GetResult()`) in `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs:56`.

3. Limited enterprise observability:
- No structured audit event stream for allow/deny decisions.
- No explicit authorization metrics SLOs at behavior/service boundaries.

4. Policy contract hardening needed:
- Principal/resource/action payload schema is implicit in code, not versioned as internal contract docs.
- No explicit safeguards for required resource attributes per resource-kind/action.

5. CI policy governance is incomplete:
- Cerbos policy compile/test gates are not explicitly formalized in the plan/runbook.

6. Test coverage is uneven:
- Good unit coverage exists for behavior and claims transformation, but weak integration coverage for full pipeline (API endpoint -> behavior -> Cerbos/fallback -> response/HATEOAS).

## Proposed Future State

### Architecture target

Authorization is standardized into 4 coordinated layers:

1. Domain layer
- No Cerbos SDK or transport concerns.
- Domain entities expose only business attributes; no policy-engine coupling.

2. Application layer
- Single orchestration entry points for authorization decisions in MediatR behavior.
- Strict use of typed action enum + resource registry for policy metadata.
- Clear conventions for when to use `IAuthorizedRequest` vs `[CerbosAuthorize] + ISecureRequest`.

3. Infrastructure layer
- Cerbos adapter handles transport, retries/timeouts, request correlation, and response parsing.
- Fallback service is explicit, measurable, and policy-aligned.
- Principal context builder is centralized and testable.

4. API/Blazor layers
- API endpoints remain thin and consistent (`GET=AllowAnonymous`, writes require auth).
- HATEOAS filtering is asynchronous and policy-consistent.
- Blazor uses claims for UX gating only; server remains final authority.

### Operational target

- Authorization decision audit events are queryable (who/what/action/result/reason/correlation-id).
- Cerbos policy compile/test runs in CI before deployment.
- Authorization SLOs and alerts are established.
- Security docs and runbooks match reality.

## Implementation Phases (Clean Architecture Layers)

### Phase 0 - Governance and Baseline Alignment (Cross-layer)

#### Task 0.1: Align security documentation with current implementation
- Files: `docs/SECURITY.md`, `docs/ARCHITECTURE.md`
- Acceptance Criteria:
  - `docs/SECURITY.md` no longer states Cerbos as future-only.
  - Includes current flow: Keycloak authn -> Cerbos authz -> fallback behavior.
  - Includes Blazor claims serialization boundaries and caveats.
- Dependencies: None
- Effort: S
- Related Skills: `auth-patterns`, `clean-architecture-rules`

#### Task 0.2: Create authorization architecture decision log (ADR)
- File: `docs/ARCHITECTURAL_DECISIONS.md` (or existing ADR location)
- Acceptance Criteria:
  - Captures why Cerbos HTTP path is used and when gRPC SDK should be considered.
  - Captures fail-closed policy and fallback semantics.
- Dependencies: Task 0.1
- Effort: S
- Related Skills: `clean-architecture-rules`, `auth-patterns`

### Phase 1 - Application Layer Hardening

#### Task 1.1: Define authorization pattern selection rules
- Files: `Explore.Application/Behaviors/AuthorizationBehavior.cs`, new docs file under `docs/`
- Acceptance Criteria:
  - Rule-set documented: `IAuthorizedRequest` vs `[CerbosAuthorize]` vs `ISecureRequest`.
  - New examples mapped to existing command patterns.
- Dependencies: Phase 0
- Effort: S
- Related Skills: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

#### Task 1.2: Enforce typed action and resource descriptor usage consistency
- Files: `Explore.Application/Authorization/CerbosResourceDescriptorRegistry.cs`, call sites in API HATEOAS policies
- Acceptance Criteria:
  - No ad-hoc action strings where typed enum is feasible.
  - Resource-kind lookup path is single-source via registry.
- Dependencies: Task 1.1
- Effort: M
- Related Skills: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

#### Task 1.3: Add structured decision logging in authorization behavior
- File: `Explore.Application/Behaviors/AuthorizationBehavior.cs`
- Acceptance Criteria:
  - Emits structured fields: user-id (if available), resource-kind, resource-id, action, decision, correlation-id.
  - No sensitive token leakage.
- Dependencies: Task 1.1
- Effort: M
- Related Skills: `error-tracking`, `auth-patterns`

### Phase 2 - Infrastructure Layer Refactor and Resilience

#### Task 2.1: Introduce principal/resource request contract builder
- Files: `Explore.Infrastructure/Services/CerbosAuthorizationService.cs`, new internal mapper/builder class
- Acceptance Criteria:
  - Principal construction and resource payload mapping extracted from service method body.
  - Builder unit tested for deterministic output and edge cases.
- Dependencies: Phase 1
- Effort: M
- Related Skills: `clean-architecture-rules`, `auth-patterns`

#### Task 2.2: Resilience policy and timeout strategy hardening
- Files: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `Explore.Infrastructure/Services/CerbosAuthorizationService.cs`
- Acceptance Criteria:
  - Explicit timeout, retry/backoff policy documented and implemented (or explicitly no-retry with rationale).
  - Behavior under Cerbos downtime validated (fail-closed + fallback policy path clarity).
- Dependencies: Task 2.1
- Effort: M
- Related Skills: `error-tracking`, `auth-patterns`

#### Task 2.3: Make fallback behavior policy-compatible and explicit
- File: `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`
- Acceptance Criteria:
  - Fallback outcomes are traceable and measurable.
  - Resource-kind gaps are documented with explicit deny rationale.
- Dependencies: Task 2.2
- Effort: M
- Related Skills: `auth-patterns`, `clean-architecture-rules`

#### Task 2.4: Admin context cache governance
- Files: `Explore.Infrastructure/Identity/AdminContext.cs`, relevant admin mutator handlers/services
- Acceptance Criteria:
  - Cache key/TTL policy documented.
  - Invalidation strategy exists for admin-role changes.
- Dependencies: Task 2.3
- Effort: M
- Related Skills: `auth-patterns`, `dotnet-efcore-guidelines`

### Phase 3 - API Layer and HATEOAS Authorization Quality

#### Task 3.1: Remove sync-over-async from HATEOAS evaluator
- File: `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`
- Acceptance Criteria:
  - No `GetAwaiter().GetResult()` in request path.
  - Async call chain preserved from evaluator to resource assembler.
  - Existing link authorization behavior preserved.
- Dependencies: Phase 2
- Effort: L
- Related Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`

#### Task 3.2: Standardize endpoint-level auth conventions where drift exists
- Files: API controllers under `Explore.API/Controllers/`
- Acceptance Criteria:
  - Conventions match project policy and explicit exceptions are documented.
  - No hidden admin-only behavior without explicit policy checks.
- Dependencies: Task 3.1
- Effort: M
- Related Skills: `auth-patterns`, `clean-architecture-rules`

#### Task 3.3: Add authorization correlation propagation in API pipeline
- Files: API middleware, behavior logging paths
- Acceptance Criteria:
  - Correlation-id present in API logs and authorization decision logs.
  - Request trace links API call to Cerbos decision.
- Dependencies: Task 3.1
- Effort: M
- Related Skills: `error-tracking`

### Phase 4 - Blazor/BFF and Client-side Authorization Composition

#### Task 4.1: Document and enforce UI-only scope of client-side authorization
- Files: `Explore.Blazor.Client/Routing/Guards/AdminRouteGuard.cs`, `Explore.Blazor.Client/Layout/NavMenu.razor.cs`, docs
- Acceptance Criteria:
  - Explicit statement: client guards are UX only; server is authority.
  - Claims used by UI route/menu guards reference shared constants or centralized contract.
- Dependencies: Phase 1
- Effort: S
- Related Skills: `blazor-bff-patterns`, `auth-patterns`

#### Task 4.2: Route-guard policy refinement for org-admin scenarios
- Files: `Explore.Blazor.Client/Routing/Guards/AdminRouteGuard.cs`, route map definitions
- Acceptance Criteria:
  - Decision documented whether organization admin should access specific admin surfaces.
  - Route guard behavior and tests updated accordingly.
- Dependencies: Task 4.1
- Effort: M
- Related Skills: `blazor-bff-patterns`, `blazor-ui-conventions`

### Phase 5 - Policy Engineering and CI/CD Governance

#### Task 5.1: Formalize Cerbos policy compile/test in CI
- Files: CI workflow files, `cerbos/policies/`
- Acceptance Criteria:
  - CI fails on invalid policies.
  - Policy test suite exists for critical resources/actions.
- Dependencies: Phase 2
- Effort: M
- Related Skills: `auth-patterns`, `clean-architecture-rules`

#### Task 5.2: Policy decision matrix for critical workflows
- Files: policy tests under `cerbos/` (or designated tests folder), docs
- Acceptance Criteria:
  - Matrix covers instance admin / tenant admin / org admin / authenticated user / anonymous where relevant.
  - Includes lock semantics (`isLockedByInstance`) and tenant/org boundary checks.
- Dependencies: Task 5.1
- Effort: L
- Related Skills: `auth-patterns`

### Phase 6 - Test Strategy and Verification

#### Task 6.1: Expand unit tests for Cerbos adapter and fallback parity
- Files: `Event.Application.UnitTests` and/or Infrastructure test project
- Acceptance Criteria:
  - Tests for HTTP status failures, null/empty responses, action mapping, attribute mapping.
  - Fallback parity tests for core resources.
- Dependencies: Phase 2
- Effort: L
- Related Skills: `cqrs-mediatr-guidelines`, `auth-patterns`

#### Task 6.2: Add integration tests for end-to-end authorization paths
- Files: `Event.API.IntegrationTests` and relevant fixtures
- Acceptance Criteria:
  - API-level verification for allow/deny on representative endpoints.
  - HATEOAS link filtering verified against policy outcomes.
- Dependencies: Phases 3 and 5
- Effort: XL
- Related Skills: `auth-patterns`, `clean-architecture-rules`

#### Task 6.3: Blazor authorization behavior test stabilization
- Files: `Explore.Blazor.Client.Tests`
- Acceptance Criteria:
  - Admin route/menu guard tests pass and are resilient to UI structure changes.
  - Client test failures clearly separate pre-existing failures vs newly introduced regressions.
- Dependencies: Phase 4
- Effort: M
- Related Skills: `blazor-bff-patterns`, `blazor-ui-conventions`

## Detailed Refactor Backlog (Priority Ordered)

1. Replace sync-over-async in HATEOAS evaluator (`Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs:56`).
2. Add structured decision audit logs in authorization behavior (`Explore.Application/Behaviors/AuthorizationBehavior.cs:94`).
3. Extract Cerbos request payload builder from `CerbosAuthorizationService`.
4. Formalize policy compile/test in CI for `cerbos/policies/`.
5. Update outdated security documentation (`docs/SECURITY.md:57`).
6. Add cache invalidation strategy for admin authority cache.

## Risk Assessment and Mitigation

### Risk 1: Authorization regressions during async refactor
- Severity: High
- Mitigation:
  - Snapshot current behavior with characterization tests before refactor.
  - Feature-flag async evaluator path if needed.

### Risk 2: Policy drift between Cerbos and fallback service
- Severity: High
- Mitigation:
  - Maintain explicit fallback matrix docs + tests.
  - Alert whenever fallback is used in production.

### Risk 3: Performance degradation with richer logging/metrics
- Severity: Medium
- Mitigation:
  - Keep logs structured and sampled where needed.
  - Use non-blocking exporters and bounded buffers.

### Risk 4: Multi-tenant boundary mistakes in principal/resource attrs
- Severity: Critical
- Mitigation:
  - Contract tests for tenant/org edge cases.
  - Mandatory review checklist for tenant-scoped authorization changes.

### Risk 5: Documentation remains stale after changes
- Severity: Medium
- Mitigation:
  - Add doc-update checklist item to every auth-related PR.
  - Include docs in definition-of-done for authorization tickets.

## Success Metrics

### Security and correctness
- 0 critical authz defects in release cycle.
- 100% of critical write paths covered by policy decision tests.
- 100% of authorization denials have structured audit records.

### Performance and reliability
- P95 authorization decision latency <= 20ms at API boundary.
- Cerbos decision failure rate < 0.1% under normal conditions.
- Fallback invocation rate near 0 in healthy environments.

### Maintainability
- No sync-over-async in authorization path.
- Single documented pattern selection guidance for MediatR authorization.
- Security documentation fully aligned with implementation.

## Required Resources and Dependencies

### People/roles
- 1 backend lead (authorization + architecture)
- 1 API engineer (HATEOAS + controllers + integration tests)
- 1 Blazor engineer (route/menu authorization composition)
- 1 DevOps engineer (CI policy compile/test + observability plumbing)

### Technical dependencies
- Cerbos policy test harness in CI
- Logging/metrics backend (current OpenTelemetry stack)
- Stable test environment for integration tests (including Cerbos availability)

### External references used
- Context7: Cerbos docs, ASP.NET Core authorization docs, .NET 10/C#14 references.
- Tavily: Cerbos policy compile/testing and audit-log best practices.

## Effort Estimates

- Phase 0: S (1-2 days)
- Phase 1: M (3-5 days)
- Phase 2: L (1-2 weeks)
- Phase 3: M-L (1 week)
- Phase 4: M (3-5 days)
- Phase 5: M-L (1 week)
- Phase 6: L-XL (1-2 weeks)

Total estimated implementation window: 5-8 weeks (single squad), 3-5 weeks (parallel squad execution).

## Recommended Execution Order

1. Phase 0 + Task 3.1 first (doc alignment + async safety fix)
2. Phase 2 and Phase 5 in parallel (adapter hardening + policy CI)
3. Phase 1 and Phase 3 together (behavior consistency + API observability)
4. Phase 4 after server-side guarantees are stable
5. Phase 6 continuously, with end-to-end integration tests as final quality gate
