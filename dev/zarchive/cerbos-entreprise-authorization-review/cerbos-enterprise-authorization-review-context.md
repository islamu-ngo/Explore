# Cerbos Enterprise Authorization Review Context

Last Updated: 2026-02-12

## Session Progress

### Completed
- Gathered internal architecture and implementation context from:
  - `CLAUDE.md`
  - `docs/ARCHITECTURE.md`
  - `docs/DOMAIN.md`
  - `docs/SECURITY.md`
  - `docs/GOVERNANCE.md`
  - `.claude/skills/*` relevant to auth, clean architecture, CQRS, and Blazor BFF.
- Mapped current Cerbos integration across API, Application, Infrastructure, and Blazor.
- Collected external references via Context7 and Tavily on:
  - Cerbos PDP API usage and policy testing,
  - ASP.NET Core authorization/resource-based patterns,
  - .NET 10 / C# 14 relevant updates,
  - SOLID/Clean Architecture guidance for policy-engine integration.
- Produced strategic implementation plan:
  - `dev/active/cerbos-enterprise-authorization-review/cerbos-enterprise-authorization-review-plan.md`

### In Scope
- Extensive architecture and implementation review for enterprise-grade Cerbos authorization integration.
- Refactor and modernization plan for maintainability, resilience, observability, and policy governance.

### Out of Scope (for this planning deliverable)
- Direct code refactoring.
- Policy rewrites.
- CI pipeline changes implementation.

## Key Findings Snapshot

1. Strong current foundation:
- MediatR behavior-based server-side authorization exists (`Explore.Application/Behaviors/AuthorizationBehavior.cs`).
- Cerbos adapter and fallback are implemented (`Explore.Infrastructure/Services/CerbosAuthorizationService.cs`, `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`).
- HATEOAS link-level filtering exists (`Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`).
- DB-first admin claims transformation to Blazor exists (`Explore.Infrastructure/Identity/AdminClaimsTransformation.cs`, `Explore.Blazor/Program.cs:128`).

2. Major refactor hotspots:
- Sync-over-async in HATEOAS evaluator (`Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs:56`).
- Insufficient structured decision auditing/metrics in auth path.
- Security doc drift (`docs/SECURITY.md:57` claims Cerbos is future-only).
- Need stronger contract governance for principal/resource/action payloads and required attributes.

3. Testing and policy governance gaps:
- Unit tests exist for core pieces but integration coverage is not enterprise-complete.
- Policy compile/test workflow not yet formalized as mandatory CI gate.

## Architectural Decisions to Preserve

1. Keep Clean Architecture boundaries strict:
- Domain stays policy-engine agnostic.
- Application defines auth contracts/behaviors.
- Infrastructure owns policy-engine transport and fallback.
- API/Blazor remain orchestration and UX layers.

2. Preserve DB-first authority model:
- Client claims are UX hints.
- Server-side MediatR/Cerbos remains final enforcement boundary.

3. Preserve centralized registries/constants:
- Continue using `CerbosResourceDescriptorRegistry` and `CerbosPermissionAction`.

## Critical Files (Working Set)

### Application Layer
- `Explore.Application/Behaviors/AuthorizationBehavior.cs`
- `Explore.Application/Authorization/CerbosAuthorizeAttribute.cs`
- `Explore.Application/Authorization/IAuthorizedRequest.cs`
- `Explore.Application/Authorization/ISecureRequest.cs`
- `Explore.Application/Authorization/CerbosResourceDescriptorRegistry.cs`
- `Explore.Application/Authorization/CerbosPermissionAction.cs`
- `Explore.Application/Authorization/AdminClaimTypes.cs`
- `Explore.Application/Contracts/Infrastructure/ICerbosAuthorizationService.cs`

### Infrastructure Layer
- `Explore.Infrastructure/Services/CerbosAuthorizationService.cs`
- `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- `Explore.Infrastructure/Identity/AdminClaimsTransformation.cs`
- `Explore.Infrastructure/Identity/AdminContext.cs`

### API Layer
- `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`
- `Explore.API/Hateoas/LinkDefinitionPermissionExtensions.cs`
- `Explore.API/ExceptionHandling/GlobalExceptionHandler.cs`
- `Explore.API/Controllers/*`

### Blazor Layer
- `Explore.Blazor/Program.cs`
- `Explore.Blazor.Client/Routing/Guards/AdminRouteGuard.cs`
- `Explore.Blazor.Client/Layout/NavMenu.razor.cs`

### Policies and Docs
- `cerbos/policies/*`
- `docs/SECURITY.md`
- `docs/ARCHITECTURE.md`
- `docs/GOVERNANCE.md`

### Tests
- `Event.Application.UnitTests/Behaviors/AuthorizationBehaviorTests.cs`
- `Event.Application.UnitTests/Behaviors/AdminClaimsTransformationTests.cs`
- `Event.Application.UnitTests/Behaviors/FallbackAuthorizationServiceTests.cs`
- `Explore.Blazor.Client.Tests/Routing/Guards/AdminRouteGuardTests.cs`

## External References Baseline

1. Cerbos
- Cerbos API and policy docs (latest), including `/api/check/resources` and policy compile/testing guidance.

2. ASP.NET Core
- Resource-based authorization and custom authorization handler guidance.
- Claims transformation (`IClaimsTransformation`) best practices.

3. .NET/C#
- .NET 10 ASP.NET updates relevant to auth pipeline and diagnostics.
- C# 14 language updates with practical maintainability relevance.

## Dependencies and Constraints

1. Project constraints from `CLAUDE.md`
- Maintain clean architecture boundaries.
- Keep security-sensitive behavior explicit and testable.
- Prefer systematic, non-shortcut refactors.

2. Authorization model constraints
- Keycloak for authentication.
- Cerbos for policy decisions.
- Fallback authorization path remains available.

3. Enterprise constraints
- Need auditable decision records.
- Need deterministic CI gates for policy validity.
- Need low-latency and resilient behavior under Cerbos disruption.

## Quick Resume Instructions

1. Read the plan first:
- `dev/active/cerbos-enterprise-authorization-review/cerbos-enterprise-authorization-review-plan.md`

2. Start with highest-risk items:
- HATEOAS async boundary fix,
- decision audit logging and metrics,
- security docs alignment.

3. Execute via tasks checklist:
- `dev/active/cerbos-enterprise-authorization-review/cerbos-enterprise-authorization-review-tasks.md`
