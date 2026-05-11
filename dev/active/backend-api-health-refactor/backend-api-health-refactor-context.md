<!-- ABOUTME: Research and repository context for the backend/API health refactor plan. -->
<!-- ABOUTME: Captures verified hotspots, governing conventions, and external documentation findings. -->

# Backend API Health Refactor Context

Last Updated: 2026-05-07 Europe/Brussels

## 1. Request Summary

Create and refine implementation planning docs for a backend/API refactor using repo conventions, Clean Architecture, enterprise-grade maintainability, industry best practices, Tavily research, and Context7 documentation. Backward compatibility is intentionally not required because the codebase is still in development mode, but breaking changes must be inventoried, documented, and reflected in OpenAPI/generated-client workflows before implementation.

CTO feedback approved the plan direction but required stricter scope control, mandatory artifacts, enforceable phase gates, tenant/auth precision, bootstrap/admin behavior, idempotency, optimistic concurrency, audit logging, rate-limit/cache classification, cursor-contract decisions, and database/index review.

## 2. Scope

In scope:
- `Explore.API`
- `Explore.Application`
- `Explore.Persistence`
- `Explore.Infrastructure`
- `Explore.Domain`
- backend test projects
- API/HAL/OpenAPI/security/operations docs
- authorization policy assets, Cerbos mappings, and HAL authorization metadata
- architecture and contract guardrails

Out of scope:
- Blazor UI rendering, styling, components, and user flows
- frontend client refactors except documenting downstream impact of API/HAL contract changes
- production migration rollout choreography beyond verification, changelog, OpenAPI, and documentation tasks

## 3. Governing Repo Rules

From `CLAUDE.md`, `.claude/contract/intents.yaml`, `.claude/rules/*`, and canonical docs:

- Domain has no EF Core, ASP.NET Core, MediatR, or infrastructure dependencies.
- Application references Domain and owns CQRS requests, handlers, DTO mapping, validators, specifications, use-case orchestration, idempotency semantics, and authorization metadata.
- Persistence implements repositories and EF Core details; repositories return entities and do not expose `IQueryable`.
- API is the composition/transport boundary: controllers dispatch MediatR, assemble HAL, return typed `ActionResult`, and use `RouteNames.Xxx`.
- Validators are manually instantiated in handlers/services; do not inject `IValidator<T>` through DI unless repo convention changes.
- GET endpoints are usually `[AllowAnonymous]`; writes are `[Authorize]`; privileged endpoints require real admin/policy/resource authorization.
- HATEOAS links are the client action source of truth and fail closed.
- Named EF Core filters should preserve tenant isolation when only soft-deleted rows are included.
- Chained `IExceptionHandler` plus RFC 7807 ProblemDetails is the expected API error model.
- Outbox/background dispatchers must be idempotent and retry-safe when used for durable side effects.

## 4. Related Active Workstreams

- `dev/active/api-contract-stabilization`: endpoint/OpenAPI inventory and route-name/contract stabilization. This backend health plan should consume its inventory and avoid duplicating generation ownership.
- `dev/active/openapi-modernization`: build-time OpenAPI generation modernization. This plan should align contract/error/route changes with it.
- `dev/active/eav-custom-properties`: overlaps with custom-property persistence, governance, and projection behavior.
- `dev/active/event-scoped-operational-roles`: overlaps with authorization policies and resource actions.
- `dev/pause/performance-optimization`: overlaps with keyset pagination, indexing, and query-shape improvements.

## 5. CTO Refinement Decisions

### Mandatory artifacts before behavior changes

Phase 0 must create implementation inputs, not afterthought documentation:

- `endpoint-inventory.md`
- `endpoint-classification.md`
- `backend-contract-risk-register.md`
- `authorization-policy-matrix.md`
- `tenant-execution-model.md`
- `api-error-catalog.md`

Endpoint inventory must include route, route name, current auth, target classification, HAL links, OpenAPI operation ID, rate-limit policy, cache policy, auth classification, tenant mode, risk, and action.

### Tenant execution model

The plan will use this design vocabulary:

```csharp
public enum TenantExecutionMode
{
    RuntimeTenantRequired,
    RuntimeTenantOptionalPublicRead,
    HostAdministration,
    BackgroundSystem,
    MigrationOrSeeding,
    DesignTime
}
```

`RuntimeTenantOptionalPublicRead` is not an all-tenant mode. It is valid only when tenant resolution comes from host/domain/path, the data is globally public platform-level data, or an explicit cross-tenant read model is invoked with authorization and audit logging.

Tenant bypass must be explicit, reason-coded, logged, tested, and unavailable from controllers as raw LINQ helpers. Preferred APIs are use-case-shaped, such as `crossTenantEventReadStore.GetTenantSummariesAsync(reason, cancellationToken)` or a scoped runner such as `tenantScope.RunAsHostAdministratorAsync(...)`.

### Authorization policy naming

Role-sounding policies should be replaced by capability/resource/action policies:

- `Templates.Manage`
- `Events.Edit`
- `Events.Publish`
- `CustomProperties.Govern`
- `PlatformNamespaces.Edit`
- `Modules.Manage`
- `StorageObjects.ReadPresigned`
- `TenantSettings.Manage`

The policy matrix must map API policy, handler attribute, Cerbos resource/action, HAL rel, and default roles.

### Bootstrap/admin story

Self-hosted deployments need a first-admin/bootstrap story covering setup secret, Keycloak group/role mapping, bootstrap disablement, auditability, SingleTenant/MultiTenant behavior, and missing auth-provider configuration behavior.

### Controller decomposition order

Controller splitting must happen after inventory, tenant/auth hardening, error/result mapping, route-name cleanup, OpenAPI metadata, and HAL guardrails. Splitting by method count alone is explicitly rejected.

### Enterprise reliability additions

The implementation must treat audit logging, idempotency, optimistic concurrency, transaction boundaries, rate-limit/cache posture, cursor contract design, and database/index review as first-class concerns.

## 6. Verified Codebase Hotspots

### Tenant filters

`Explore.Persistence/ExploreDbContext.QueryFilters.cs` repeatedly uses predicates shaped like:

```csharp
TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty)
```

Examples were confirmed at many query-filter definitions. In runtime request paths this is dangerous because absent tenant context broadens the query. The refactor should introduce explicit runtime/system/design-time modes and tests.

### Query-filter bypass helpers

`Explore.Persistence/QueryFilters/QueryFilterExtensions.cs` contains:

- `IncludeDeleted(...)`: positive pattern; disables only `SoftDelete`.
- `IgnoreTenantFilter(...)`: high-risk and should require explicit cross-tenant/system use.
- `IgnoreAllFilters(...)`: disables all filters and should be restricted or removed from runtime paths.

### Placeholder authorization policies

`Explore.API/Extensions/AuthenticationExtensions.cs` defines these policies as `RequireAuthenticatedUser()` only:

- `template_admin`
- `event_editor`
- `property_governance_admin`
- `platform_namespace_editor`

The names imply privilege, but the implementation only checks authentication. Replace with capability/resource/action policies and architecture tests.

### Fat controllers

`Explore.API/Controllers/EventController.cs` combines event listing, my-events, creation context, details, calendar export, create/readiness/publish/update/status/delete, Islamic aspect CRUD, and tech aspect CRUD. It also manually maps a large filter request and branches on command response messages/failure codes.

`Explore.API/Controllers/UserAppearanceController.cs` uses hard-coded route names for many endpoints instead of `RouteNames.Xxx`, while the controller convention requires the central route-name registry.

`Explore.API/Controllers/UserController.cs` contains a TODO for admin checks when viewing another user's organizations and returns ad hoc `BadRequest`/`Forbid` responses.

### Route-name guardrails currently skipped

`Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs` has two important skipped tests:

- `RouteNames_EveryConstantResolvesToExactlyOneEndpoint`
- `EndpointRouteNames_EveryNamedEndpointHasMatchingConstant`

These should become non-skipped once route-name cleanup is part of the implementation slice.

### CQRS handler size and behavior bypass risk

`Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` has roughly thirty constructor dependencies and coordinates event creation, sessions, days, rooms, agenda, aspects, custom properties, projection refresh, metrics, cache invalidation, and unit-of-work behavior. It should be decomposed into narrow collaborators, not one large replacement application service.

Prior research also identified direct handler invocation in public experience shell query handling, which risks bypassing MediatR behaviors if confirmed during implementation.

### Persistence health

`Explore.Persistence/Repositories/EventRepository.cs`, `OrganizationRepository.cs`, and related repositories use repeated offset pagination (`Skip((pageNumber - 1) * pageSize)`), broad include graphs, and many EF async calls without cancellation tokens. High-volume public feeds should move toward opaque cursor/keyset pagination; remaining offset paths need stable ordering and validation.

## 7. External Documentation Findings

### ASP.NET Core via Context7

- Rate limiting requires configured middleware and named policies applied to endpoints/resources.
- Authorization policies should encode concrete requirements such as roles/claims/resource checks, not just endpoint naming.
- Protected endpoints should explicitly require authorization in the pipeline/metadata.
- ProblemDetails is the standard ASP.NET Core mechanism for consistent HTTP API errors.

### EF Core via Context7

- EF Core supports named query filters, including separate soft-delete and multi-tenancy filters.
- Multi-tenancy query filters should compare rows to the current tenant identifier, not treat missing tenant as a broad match in runtime code.
- EF Core 10 named filters can be disabled selectively, which supports `SoftDelete` inclusion without disabling `Tenant`.
- Query performance work should include query shape, stable ordering, pagination design, and supporting indexes.

### MediatR via Context7

- Pipeline behaviors are registered through `AddMediatR` package configuration patterns and execute in registration order depending on registration.
- Open generic behaviors are the intended mechanism for cross-cutting logging, validation, performance, and authorization behavior.
- Direct handler-to-handler calls can bypass expected pipeline behavior; use `IMediator` deliberately or extract query services when behavior bypass is intentional.

### Tavily research synthesis

The external research reinforced the same priorities as the repo: thin API boundary, RFC 7807 error consistency, resource/policy authorization, tenant-safe EF filters, cancellation-token propagation, keyset pagination for large datasets, OpenTelemetry-style observability, auditability, idempotency, optimistic concurrency, and layered architecture test guardrails.

## 8. Implementation Risks

- Tenant fail-closed changes can break tests or seed/design-time flows that accidentally rely on `TenantContext == null`; introduce explicit execution modes rather than reintroducing permissive filters.
- System/background context can become an unsafe replacement for `IgnoreAllFilters()` unless it is reason-coded, logged, restricted, and tested.
- Authorization hardening can expose intentionally-public endpoints that were never documented; classify endpoints before changing attributes.
- Bootstrap/admin hardening can lock out self-hosted deployments if first-admin behavior is not defined and tested before policy changes land.
- Controller decomposition can break HATEOAS links if route names are not migrated atomically with tests.
- Error normalization can expose client assumptions about old string errors; development-mode compatibility waiver permits breaking changes, but docs/API changelog must be updated.
- Idempotency and concurrency changes can affect command response contracts; model duplicate and conflict errors explicitly.
- Cursor pagination may require indexes, filter-hash binding, and response contract changes; coordinate with OpenAPI contract work.
- Database index changes need migration/model assertions so performance fixes do not drift.

## 9. Verification Commands to Prefer

Use targeted project checks:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

Avoid solution-level `dotnet test` unless repo policy changes.
