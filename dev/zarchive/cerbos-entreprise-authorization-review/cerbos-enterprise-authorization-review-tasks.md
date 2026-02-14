# Cerbos Enterprise Authorization Review Tasks

Last Updated: 2026-02-12

## Phase 0 - Governance and Baseline Alignment

- [ ] **0.1 Update security docs to reflect current Cerbos integration**
  - Files: `docs/SECURITY.md`, `docs/ARCHITECTURE.md`
  - Acceptance Criteria:
    - [ ] Remove outdated “Cerbos not integrated” statement.
    - [ ] Document current authn/authz flow and fallback path.
  - Effort: S
  - Dependencies: None
  - Skills: `auth-patterns`, `clean-architecture-rules`

- [ ] **0.2 Add ADR for Cerbos transport/fallback strategy**
  - Files: `docs/ARCHITECTURAL_DECISIONS.md` (or existing ADR file)
  - Acceptance Criteria:
    - [ ] Captures rationale for HTTP adapter and migration triggers to gRPC SDK.
    - [ ] Captures fail-closed and fallback behavior policy.
  - Effort: S
  - Dependencies: 0.1
  - Skills: `clean-architecture-rules`, `auth-patterns`

## Phase 1 - Application Layer Hardening

- [ ] **1.1 Standardize authorization usage patterns**
  - Files: `Explore.Application/Behaviors/AuthorizationBehavior.cs`, docs
  - Acceptance Criteria:
    - [ ] Decision tree documented for `IAuthorizedRequest` vs `[CerbosAuthorize]` vs `ISecureRequest`.
    - [ ] At least one concrete code example per pattern.
  - Effort: S
  - Dependencies: 0.2
  - Skills: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

- [ ] **1.2 Enforce typed action/resource mapping consistency**
  - Files: `Explore.Application/Authorization/CerbosResourceDescriptorRegistry.cs`, HATEOAS policy call sites
  - Acceptance Criteria:
    - [ ] No new ad-hoc action strings where enum exists.
    - [ ] Registry remains single source of resource kind mapping.
  - Effort: M
  - Dependencies: 1.1
  - Skills: `cqrs-mediatr-guidelines`

- [ ] **1.3 Add structured decision logs in behavior**
  - File: `Explore.Application/Behaviors/AuthorizationBehavior.cs`
  - Acceptance Criteria:
    - [ ] Logs include resource/action/decision/correlation-id fields.
    - [ ] No secrets or raw tokens logged.
  - Effort: M
  - Dependencies: 1.1
  - Skills: `error-tracking`, `auth-patterns`

## Phase 2 - Infrastructure Refactor and Resilience

- [ ] **2.1 Extract Cerbos principal/resource payload builder**
  - Files: `Explore.Infrastructure/Services/CerbosAuthorizationService.cs`, new internal builder class
  - Acceptance Criteria:
    - [ ] Main service no longer constructs payload inline end-to-end.
    - [ ] Builder has unit tests for mapping correctness.
  - Effort: M
  - Dependencies: 1.2
  - Skills: `clean-architecture-rules`, `auth-patterns`

- [ ] **2.2 Harden resilience policy for Cerbos communication**
  - Files: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `Explore.Infrastructure/Services/CerbosAuthorizationService.cs`
  - Acceptance Criteria:
    - [ ] Timeout and retry/no-retry policy is explicit and documented.
    - [ ] Cerbos-down behavior tested and deterministic.
  - Effort: M
  - Dependencies: 2.1
  - Skills: `error-tracking`, `auth-patterns`

- [ ] **2.3 Make fallback behavior measurable and policy-aligned**
  - File: `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`
  - Acceptance Criteria:
    - [ ] Fallback invocations logged with structured fields.
    - [ ] Resource kinds not supported are explicitly documented and tested denied.
  - Effort: M
  - Dependencies: 2.2
  - Skills: `auth-patterns`

- [ ] **2.4 Add admin-cache invalidation strategy**
  - Files: `Explore.Infrastructure/Identity/AdminContext.cs` + admin role mutator paths
  - Acceptance Criteria:
    - [ ] Role change invalidates affected user admin cache.
    - [ ] Cache TTL/invalidation documented.
  - Effort: M
  - Dependencies: 2.3
  - Skills: `auth-patterns`, `dotnet-efcore-guidelines`

## Phase 3 - API and HATEOAS Authorization Quality

- [ ] **3.1 Remove sync-over-async from HATEOAS authorization**
  - File: `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`
  - Acceptance Criteria:
    - [ ] No `GetAwaiter().GetResult()` in link evaluation flow.
    - [ ] Link decisions remain behaviorally equivalent (verified by tests).
  - Effort: L
  - Dependencies: 2.2
  - Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`

- [ ] **3.2 Re-validate endpoint authorization convention consistency**
  - Files: `Explore.API/Controllers/*`
  - Acceptance Criteria:
    - [ ] Public GET endpoints and protected write endpoints are consistent with policy.
    - [ ] Exceptions are explicit and documented.
  - Effort: M
  - Dependencies: 3.1
  - Skills: `auth-patterns`

- [ ] **3.3 Add correlation-id propagation in authorization path**
  - Files: API middleware + authorization behavior/service logs
  - Acceptance Criteria:
    - [ ] Correlation-id appears in API and auth decision logs.
    - [ ] Traceability from request to decision confirmed.
  - Effort: M
  - Dependencies: 3.1
  - Skills: `error-tracking`

## Phase 4 - Blazor/BFF Authorization Composition

- [ ] **4.1 Formalize client authorization scope as UX-only**
  - Files: `Explore.Blazor.Client/Routing/Guards/AdminRouteGuard.cs`, `Explore.Blazor.Client/Layout/NavMenu.razor.cs`, docs
  - Acceptance Criteria:
    - [ ] Docs explicitly state server-side authorization is authoritative.
    - [ ] Client guards use centralized claim contracts consistently.
  - Effort: S
  - Dependencies: 1.1
  - Skills: `blazor-bff-patterns`, `auth-patterns`

- [ ] **4.2 Resolve org-admin route access policy decision**
  - Files: route guards + route definitions + tests
  - Acceptance Criteria:
    - [ ] Decision documented: whether org admins can access specific admin routes.
    - [ ] Guard logic and tests aligned to that decision.
  - Effort: M
  - Dependencies: 4.1
  - Skills: `blazor-bff-patterns`, `blazor-ui-conventions`

## Phase 5 - Cerbos Policy Engineering and CI Governance

- [ ] **5.1 Add mandatory Cerbos compile/test CI gate**
  - Files: CI workflows + `cerbos/policies/*`
  - Acceptance Criteria:
    - [ ] CI fails on invalid Cerbos policies.
    - [ ] Policy tests execute as part of pipeline.
  - Effort: M
  - Dependencies: 2.1
  - Skills: `auth-patterns`

- [ ] **5.2 Build critical permission matrix test suite**
  - Files: `cerbos/` policy tests + docs
  - Acceptance Criteria:
    - [ ] Covers instance admin, tenant admin, org admin, authenticated user, anonymous where applicable.
    - [ ] Covers lock semantics and tenant/org boundaries.
  - Effort: L
  - Dependencies: 5.1
  - Skills: `auth-patterns`

## Phase 6 - Testing and Quality Gates

- [ ] **6.1 Expand unit tests for Cerbos adapter + fallback parity**
  - Files: test projects under `Event.Application.UnitTests` / Infrastructure tests
  - Acceptance Criteria:
    - [ ] Covers failure modes, response mapping, attribute mapping, fallback parity.
  - Effort: L
  - Dependencies: 2.4
  - Skills: `cqrs-mediatr-guidelines`, `auth-patterns`

- [ ] **6.2 Add end-to-end integration authorization tests**
  - Files: `Event.API.IntegrationTests`
  - Acceptance Criteria:
    - [ ] Covers representative allow/deny endpoint scenarios.
    - [ ] Covers HATEOAS link visibility outcomes.
  - Effort: XL
  - Dependencies: 3.3, 5.2
  - Skills: `auth-patterns`, `clean-architecture-rules`

- [ ] **6.3 Stabilize Blazor authorization tests**
  - Files: `Explore.Blazor.Client.Tests`
  - Acceptance Criteria:
    - [ ] Admin route/menu tests pass reliably.
    - [ ] Pre-existing failures and new regressions are clearly distinguished.
  - Effort: M
  - Dependencies: 4.2
  - Skills: `blazor-bff-patterns`, `blazor-ui-conventions`

## Cross-Cutting Delivery Gates

- [ ] **Gate A: Architecture compliance**
  - [ ] No dependency rule violations introduced.
  - [ ] Layer ownership of auth concerns is maintained.

- [ ] **Gate B: Observability compliance**
  - [ ] Structured decision logs emitted for all deny paths.
  - [ ] Auth metrics visible on dashboards.

- [ ] **Gate C: Policy governance compliance**
  - [ ] Cerbos policy compile/test required in CI.
  - [ ] Policy test matrix maintained.

- [ ] **Gate D: Documentation parity**
  - [ ] Security and architecture docs match implementation.

## Suggested Execution Sequence

1. 0.1 -> 0.2 -> 3.1
2. 2.1 -> 2.2 -> 2.3 -> 2.4
3. 1.1 -> 1.2 -> 1.3 + 3.2 -> 3.3
4. 5.1 -> 5.2
5. 4.1 -> 4.2
6. 6.1 -> 6.2 -> 6.3
