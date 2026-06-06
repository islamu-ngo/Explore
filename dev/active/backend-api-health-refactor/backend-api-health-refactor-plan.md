<!-- ABOUTME: Implementation plan for the backend/API health refactor workstream. -->
<!-- ABOUTME: Prioritizes security, API contract, CQRS, persistence, and test guardrail hardening. -->

# Backend API Health Refactor Plan

Last Updated: 2026-05-07 Europe/Brussels

## 1. Goal

Refactor the backend/API codebase toward an enterprise-grade, highly maintainable Clean Architecture implementation without preserving development-era compatibility mistakes. This workstream is backend/API-only: `Explore.API`, `Explore.Application`, `Explore.Persistence` (including `ApiTickerQDbContext`), `Explore.Infrastructure` (including storage and messaging implementations), `Explore.Domain`, backend tests, documentation, OpenAPI/HAL contracts, and authorization policy assets are in scope; Blazor UI behavior and styling are out of scope except where API/HAL contracts require documentation of downstream impact.

## 2. Success Criteria

- Phase 0 produces mandatory implementation artifacts before behavior changes begin.
- Runtime tenant isolation fails closed for normal request paths and cannot silently return cross-tenant data when tenant context is absent.
- Tenant/system execution paths are explicit, named, logged, reason-coded, and test-covered; background workers (RabbitMQ consumers, TickerQ scheduled tasks) and storage reconciliation workers run safely under `BackgroundSystem` context without leaving permanent filter-bypass loopholes.
- API authorization policies express capabilities/resources/actions rather than role-sounding placeholders backed only by `RequireAuthenticatedUser()`. This covers core entities as well as AI assistants, file storage, email admin, and scheduling subsystems.
- First-admin/bootstrap behavior is documented and verified for self-hosted SingleTenant and MultiTenant deployments, including Keycloak integration and secret rotation.
- Error responses use one RFC 7807 ProblemDetails factory/writer with typed problem codes and required `traceId`, `timestamp`, and `correlationId` extensions.
- Route names, endpoint classifications, HAL affordances, OpenAPI operation IDs, rate-limit policies, cache policies, tenant modes, and authorization posture are inventory-backed and test-enforced.
- Controllers become thin dispatch/representation boundaries only after contract/error/route stability is established, ensuring controllers like `AiAssistantController`, `StorageObjectController`, and `EmailDispatchAdminController` remain thin HTTP adaptors.
- CQRS handlers remain single-purpose, cancellation-aware, idempotency-aware where relevant (e.g., `Idempotency-Key` processing), and do not bypass MediatR behaviors by directly invoking other handlers.
- Persistence repositories remain entity-first, tenant-safe, cancellation-aware, index-conscious, and projection/read-model exceptions are explicit.
- Audit logging, duplicate-submit protection, optimistic concurrency, and transaction boundaries (e.g., RabbitMQ outbox states and TickerQ schedule triggers) are treated as first-class backend reliability requirements.

## 3. Constraints and Non-Goals

- No backward compatibility requirement before v1.0: route names, response shapes, command/query contracts, policy names, and internal abstractions may change when the new design is cleaner.
- All breaking changes must still be documented in `docs/API_CHANGELOG.md`, reflected in OpenAPI, and followed by generated-client regeneration where applicable.
- No Blazor UI refactor in this workstream.
- Do not weaken public endpoint access accidentally; classify every endpoint before changing authorization.
- Do not split major controllers until endpoint inventory, endpoint classification, error mapping, route names, and OpenAPI/HAL guardrails are stable.
- Do not remove tenant/soft-delete filters to make tests pass.
- Do not introduce DTO-returning persistence repositories as a shortcut; use Application-owned DTO mapping or explicitly named read-model ports.
- Do not create a replacement `CreateEventApplicationService` god object while decomposing `CreateEventCommandHandler`.
- Do not use type-safety suppressions or swallow exceptions.

## 4. Mandatory Phase 0 Artifacts

Phase 0 is not optional preparation. It produces implementation inputs and controls scope before code changes.

Required artifacts under this workstream:

1. `endpoint-inventory.md`
   - Minimum columns: `Endpoint | Method | Route | RouteName | Current Auth | Target Classification | AuthClassification | TenantMode | RateLimitPolicy | CachePolicy | HAL Links | OpenAPI OperationId | Risk | Action`.
2. `endpoint-classification.md`
   - Declares Public, Authenticated, Admin, host-admin, setup/bootstrap, and system/background endpoint intent.
3. `backend-contract-risk-register.md`
   - Lists breaking changes, risk owner, implementation phase, tests, OpenAPI/client impact, and docs impact.
4. `authorization-policy-matrix.md`
   - Columns: `Resource | Action | API Policy | Handler Attribute | Cerbos Resource | HAL Rel | Default Roles`.
5. `tenant-execution-model.md`
   - Defines tenant execution modes, allowed call sites, failure behavior, bypass reasons, logging, and tests.
6. `api-error-catalog.md`
   - Defines typed problem codes, HTTP status mapping, response extensions, examples, and tests.

Phase 0 split:

- **Phase 0A — Inventory only.** Produce the artifacts from current code/OpenAPI/metadata. Do not change behavior.
- **Phase 0B — Non-invasive guardrails.** Add tests and architecture checks. They may initially fail or be skipped only when linked to explicit risk-register tasks. Do not change endpoint behavior yet.

## 5. Core Design Decisions

### 5.1 Tenant execution model

Use an explicit execution-mode concept as the basis for query-filter behavior and system/background exceptions:

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

`RuntimeTenantOptionalPublicRead` must not mean “query all tenants.” It is allowed only when a tenant is resolved from host/domain/path, the query targets globally public platform-level data, or the query goes through an explicit cross-tenant read model with authorization and audit logging.

Tenant-bypass APIs must require a reason and operation name, for example:

```csharp
BeginSystemTenantScope(SystemTenantScopeReason reason, string operationName, Guid? actorUserId = null)
```

Normal tenant-scoped absent-tenant behavior must be fail-closed: no rows, 400, 401, 403, startup/configuration failure, or explicit system path depending on context; never silent all-tenant reads. Background processes (such as RabbitMQ messaging consumers and TickerQ scheduled jobs) must run in explicitly initialized, audited, and reason-coded `BackgroundSystem` scopes.

### 5.2 Capability/resource/action policy names

Replace role-sounding policies such as `template_admin`, `event_editor`, `property_governance_admin`, and `platform_namespace_editor` with capability names such as:

- `Templates.Manage`
- `Events.Edit`
- `Events.Publish`
- `CustomProperties.Govern`
- `PlatformNamespaces.Edit`
- `Modules.Manage`
- `StorageObjects.Read` / `StorageObjects.Write` / `StorageObjects.ReadPresigned`
- `TenantSettings.Manage` / `TenantStorage.Manage`
- `AiAssistant.Interact` / `AiAssistant.Manage`
- `Email.Dispatch` / `Email.Manage`
- `InstanceSettings.Manage`

Every privileged API policy must map to handler authorization metadata, Cerbos resource/action naming, HAL rels, and default roles in `authorization-policy-matrix.md`.

### 5.3 Self-host bootstrap/admin behavior

The auth hardening phase must define and test first-admin behavior:

- how the first platform/tenant admin is created;
- whether Keycloak groups/roles, setup secret, or both are authoritative;
- whether bootstrap can be disabled after completion;
- how bootstrap actions are audited;
- how SingleTenant and MultiTenant modes differ;
- what happens when auth provider configuration is missing.

### 5.4 Error catalog and result mapping

Use typed error codes as the stable API error language:

```csharp
public static class ApiProblemCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string TenantRequired = "tenant_required";
    public const string ResourceNotFound = "resource_not_found";
    public const string ResourceConflict = "resource_conflict";
    public const string Forbidden = "forbidden";
    public const string AuthenticationRequired = "authentication_required";
    public const string ConcurrencyConflict = "concurrency_conflict";
    public const string DuplicateRequest = "duplicate_request";
}
```

Tests must verify consistent code/status mapping for controllers, middleware-generated errors, validation failures, command responses, idempotency duplicates, and optimistic-concurrency conflicts.

### 5.5 Event creation decomposition

Do not extract one large replacement application service. Decompose `CreateEventCommandHandler` into narrow collaborators:

- `EventDraftFactory`
- `EventScheduleGraphWriter`
- `EventTaxonomyAssignmentWriter`
- `EventTemplateInstantiationService`
- `EventCustomPropertyInitializer`
- `EventProjectionRefreshCoordinator`
- `EventCreationCacheInvalidator`
- `EventCreationMetricsRecorder`

The main handler remains the use-case coordinator: validate request, authorize actor/resource, open transaction, call cohesive collaborators, commit, and return result. Use MediatR for independent use cases; use Application services for internal steps of one use case; never inject handlers directly.

### 5.6 Idempotency, concurrency, audit, and pagination

- High-risk POST/actions need idempotency behavior: create event, publish/unpublish, registration, approval/cancellation, setup/bootstrap, sending AI assistant prompts (`SendAiMessageCommand` via `Idempotency-Key` header), confirming AI proposed actions, storage upload session registration, outbox replay actions, and payment-adjacent actions.
- Event lifecycle/status/settings updates and AI assistant run state transitions need optimistic concurrency and `concurrency_conflict` ProblemDetails.
- Duplicate submissions need `duplicate_request` ProblemDetails or replay-safe cached responses.
- Audit tenant/admin/security-sensitive operations: tenant settings, tenant storage settings, event lifecycle, auth-relevant data (Keycloak realm updates, client secret rotations), presigned URL generation, upload sessions, tenant-filter bypass, cross-tenant admin views, custom-property governance, templates, modules, namespaces, outbox paused/resumed/parked/replayed states, and TickerQ scheduler mutations.
- Public high-volume feed pagination and private AI assistant conversation history feeds should use an opaque versioned cursor response. Do not expose raw `(CreatedAt, Id)` cursor tuples externally.

Preferred cursor shape:

```json
{
  "items": [],
  "page": {
    "nextCursor": "...",
    "previousCursor": null,
    "hasMore": true
  }
}
```

Cursor design must answer versioning, sort direction, tenant/filter hash binding, filter-change behavior, deleted/inserted row stability, total-count policy, and which endpoints remain offset-based.

### 5.7 Storage isolation and upload sessions

Ensure instance and tenant storage admin actions cannot bypass configuration locks. Uploads from Blazor BFF to storage must flow through provider-neutral upload sessions (`/api/storageobject/upload-sessions`). Streaming file content must rely on metadata-backed IDs (`/api/storageobject/{id}/content`), and S3 provider health probes must be safely isolated in `/health` without exposing secrets or paths.

### 5.8 Messaging outbox, RabbitMQ, and TickerQ scheduling

Ensure background processing of outbox events (like `OutboxProcessor` and RabbitMQ consumer services) and scheduling triggers (like TickerQ jobs for email outbox drains, stale processing recovery, and event reminders) operate with transactional safety. Transitions of EmailDispatch outbox state (pending, sent, retry, dead-letter, paused, parked) must be idempotent, concurrency-safe, and run in a quarantined database context (`ApiTickerQDbContext`).

## 6. Evidence Base

Repo evidence is captured in `backend-api-health-refactor-context.md`. The highest-risk confirmed hotspots are:

- `Explore.Persistence/ExploreDbContext.QueryFilters.cs`: many tenant filters include `TenantContext == null || ...`, which makes missing tenant context permissive.
- `Explore.API/Extensions/AuthenticationExtensions.cs`: `template_admin`, `event_editor`, `property_governance_admin`, and `platform_namespace_editor` currently require only authentication.
- `Explore.API/Controllers/EventController.cs`: combines discovery, my-events, calendar export, lifecycle mutations, publishing, deletion, and aspect CRUD.
- `Explore.API/Controllers/AiAssistantController.cs`, `StorageObjectController.cs`, and `EmailDispatchAdminController.cs`: expose newly added complex surfaces (AI assistant run/proposed actions, provider-neutral upload sessions, outbox pause/park/replay controls) but only apply general class-level `[Authorize]` without capability-based policy checks.
- `Explore.API/Scheduling/EmailDispatchTickerQJobs.cs` and `EventLifecycleTickerQJobs.cs`: background scheduler jobs executing state transitions and triggers via `ApiTickerQDbContext` and external messaging providers.
- `Explore.Infrastructure/Messaging/EmailDispatchRabbitMqConsumerService.cs`: processes RabbitMQ messages in background threads where tenant isolation and scope setup must be strictly verified.
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`: large create-graph handler with dozens of dependencies.
- `Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs`: the two route-name drift tests are skipped.
- `Explore.Persistence/Repositories/EventRepository.cs` and related repositories: repeated offset pagination and missing `CancellationToken` forwarding. This must also be checked for new AI assistant conversation history, storage object, and email outbox listings.

External evidence used for the plan:

- Context7 ASP.NET Core docs: `ProblemDetails`, authorization policies/resource authorization, and rate limiting middleware/policies.
- Context7 EF Core docs: named query filters, multi-tenancy filters, and selective disabling of named filters.
- Context7 MediatR docs: open pipeline behavior registration and behavior execution order.
- Tavily research: reinforced thin controllers, RFC 7807 standardization, resource authorization, EF tenant filters, cancellation propagation, keyset pagination, OpenTelemetry observability, idempotency, auditability, and architecture guardrails as enterprise API refactor anchors.

## 7. Phased Implementation

### Phase 0 — Mandatory Inventory, Classification, and Non-Invasive Guardrails

Purpose: make the current contract visible before changing behavior.

Actions:
- Phase 0A: produce the six mandatory artifacts listed in section 4.
- Phase 0B: add non-invasive tests for route-name constants, endpoint classification, auth metadata, ProblemDetails metadata, controller repository injection, domain entity API responses, and query-filter bypass rules.
- Allow initially failing/skipped tests only when the skip reason links to a specific risk-register row and follow-up task.

Exit criteria:
- Endpoint inventory includes route, auth, HAL, OpenAPI, rate-limit, cache, tenant mode, risk, and action columns.
- Authorization-policy matrix and tenant-execution-model documents are approved implementation inputs.
- No behavior-changing phase can start without a risk-register entry for each known breaking change.

### Phase 1 — Tenant Isolation and Authorization Hardening

Purpose: remove the highest-risk cross-tenant and privileged-action ambiguity without trying to solve every controller ownership issue at once.

Subphases:
- **1A Tenant execution model:** implement explicit execution modes and absent-tenant failure behavior. Background worker threads (RabbitMQ outbox consumer, TickerQ jobs, storage reconciliation worker) must run safely under `BackgroundSystem` context.
- **1B Query filter fail-closed implementation:** replace permissive `TenantContext == null || ...` runtime semantics. Ensure pooled state and `ApiTickerQDbContext` behave safely under fail-closed filters.
- **1C Filter bypass quarantine:** remove or restrict `IgnoreAllFilters()` and require named reason-coded system/host-admin APIs.
- **1D Authorization policy replacement:** replace placeholder policies with capability/resource/action policies. This includes new policies for AI (`AiAssistant.Interact`/`AiAssistant.Manage`), Storage (`StorageObjects.Read`/`StorageObjects.Write`), Tenant Storage Settings (`TenantStorage.Manage`), and Email Admin (`Email.Dispatch`/`Email.Manage`).
- **1E Self-host bootstrap/admin verification:** define first-admin, setup-secret/Keycloak role behavior, Keycloak client secret rotation and backup-confirmed sync, disablement, audit, and missing-auth-provider handling.

Exit criteria:
- Tenant-filter tests prove fail-closed runtime behavior and explicit system/background/design-time behavior.
- Cross-tenant/system operations use explicit methods, reason enums, structured logging, and no controller-level LINQ filter bypass.
- Privileged policies cannot be satisfied by authentication alone unless intentionally named as authenticated-only.
- Bootstrap/admin setup behavior is documented, tested, and audited.

### Phase 2 — API Contract, Error Catalog, and Result Mapping

Purpose: make the API predictable before endpoints move across controllers.

Actions:
- Introduce `ApiProblemCodes`, `ApiProblemFactory`, and a shared ProblemDetails writer for controllers and middleware. Ensure AI assistant run failures, S3 provider errors, and outbox transition errors map correctly.
- Replace raw `BadRequest(...)`, string `Forbid(...)`, ad hoc `Problem(...)`, and command-message branching with typed problem codes (specifically on `UserController`, `AiAssistantController`, `StorageObjectController`, `EmailDispatchAdminController`).
- Introduce a `CommandResponseResultMapper` for `BaseCommandResponse<T>`, bool deletes, conflicts, duplicate requests, not-found, validation failures, and concurrency conflicts.
- Unskip/fix route-name coverage tests and migrate hard-coded endpoint names to `RouteNames.Xxx` constants (including the new AI assistant, storage upload, and email outbox routes).
- Normalize OpenAPI operation IDs, response metadata, endpoint classification extensions, HAL link references, rate-limit policy metadata, and cache policy metadata.
- Define the preview contract stance: breaking changes allowed before v1.0, but documented and regenerated through OpenAPI/client workflows.

Exit criteria:
- All errors include RFC 7807 fields plus repo-required extensions.
- Problem codes are tested for consistent status mapping.
- Named endpoints have matching constants and constants resolve to one endpoint.
- OpenAPI metadata exposes stable endpoint classifications, rate-limit/cache policy posture, and expected ProblemDetails responses.

### Phase 3 — Controller Decomposition Behind Stable Contracts

Purpose: reduce transport-layer complexity after contract/error/route stability exists.

Actions:
- Split by resource/use-case cohesion, not method count alone.
- Candidate event boundaries: `EventsController`, `MyEventsController`, `EventLifecycleController`, `EventPublishingController`, `EventCalendarController`, and `EventAspectsController`; implement only splits justified by route inventory and risk.
- Ensure `AiAssistantController`, `StorageObjectController`, and `EmailDispatchAdminController` remain thin dispatch boundaries with logic delegated to Application handlers.
- Split large settings/admin controllers only after Phase 2 establishes response and route-name stability.
- Extract API request-to-query mappers for large filter request types instead of inline controller mapping.
- Move file-response details such as calendar filename/canonical URL construction into API representation services or Application file descriptor results.

Exit criteria:
- Controllers have low constructor counts, no repository injection, no business orchestration, and consistent response mapping.
- Endpoint tests cover old high-risk resources through the new route names/contracts.

### Phase 4 — CQRS/Application Use-Case Refactor

Purpose: restore cohesive use cases and predictable MediatR behavior.

Actions:
- Decompose `CreateEventCommandHandler` into the narrow collaborators listed in section 5.5.
- Decompose/standardize AI assistant prompt/confirmation and storage upload session commands/handlers.
- Define aggregate-creation transaction boundaries; extracted collaborators must not independently call `SaveChangesAsync` unless explicitly part of the unit-of-work design.
- Add idempotency behavior for create/publish/update, sending AI messages, finalizing uploads, outbox replays, and duplicate browser submits.
- Add optimistic concurrency for event lifecycle/status/settings mutations and AI assistant run state changes.
- Remove obsolete `IAuthorizedRequest` compatibility path and use `[AuthorizeResource]` plus `ISecureRequest` consistently.
- Replace direct handler-to-handler calls with MediatR dispatch or Application query services that do not bypass behaviors accidentally.
- Normalize query handlers to return DTOs or `PaginatedResult<TDto>`, not command response envelopes.
- Centralize cache keys/tags and invalidation policy.
- Add audit events for event lifecycle, Keycloak configurations, upload sessions, outbox pause/park/replay controls, and security-sensitive use cases.

Exit criteria:
- Handler constructor counts and responsibilities are reduced.
- Cross-cutting behaviors still run for validation, authorization, logging/performance, and caching.
- Application tests cover decomposed event creation, idempotency, optimistic concurrency, audit event emission, transaction boundaries, and authorization metadata.

### Phase 5 — Persistence Correctness, Query Shape, Pagination, Indexes, and Transactions

Purpose: make data access efficient, cancellation-aware, tenant-safe, and contract-conscious.

Subphases:
- **5A Cancellation tokens:** add `CancellationToken` parameters to repository contracts and pass tokens into EF async calls (including AI assistant history, storage queries, outbox transitions, and TickerQ db operations).
- **5B Tenant-safe repository/query contracts:** preserve entity-first repositories; make cross-tenant read models explicit.
- **5C Query shape cleanup:** remove broad includes only on confirmed hotspots and replace with projection-specific read models/detail queries.
- **5D Cursor pagination:** introduce opaque versioned cursor pagination for public high-volume feeds, AI assistant conversations, and storage object listings, and define remaining offset-based endpoints.
- **5E Index and migration review:** review high-volume event/session/feed indexes, tenant composite indexes, soft-delete predicates, and model assertions/migration tests. Review index coverage for AI run states, storage metadata, and outbox tables.
- **5F Transaction retry strategy:** route manual transactions through `IUnitOfWork` or EF execution strategies (specifically for outbox updates and TickerQ operational state transitions).

Exit criteria:
- Repository APIs preserve entity-first boundaries except for explicitly named read-model ports.
- High-volume paths have deterministic order, opaque cursor contracts, and supporting indexes where needed.
- Persistence integration tests cover tenant isolation, soft delete, pagination stability, critical indexes/model assertions, and transaction retry behavior.

### Phase 6 — Final Guardrails, Documentation, and Cleanup

Purpose: prevent regression after conventions land. Guardrails should be introduced as conventions land; Phase 6 hardens and removes temporary allowances.

Actions:
- Convert temporary skipped/failing Phase 0B tests into passing hard gates.
- Add or finalize architecture tests for auth metadata, endpoint classification, route-name constants, ProblemDetails metadata, no repository injection into controllers, no domain entity API responses, no direct handler injection, cancellation-aware repository interfaces, safe query-filter bypass, audit coverage, idempotency on high-risk POSTs, and optimistic concurrency on lifecycle/status/settings updates.
- Update `docs/API.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/MULTI_TENANCY.md`, `docs/OPERATIONS.md`, and `docs/API_CHANGELOG.md` for changed contracts.
- Keep `dev/active/api-contract-stabilization` and `dev/active/openapi-modernization` aligned; this workstream owns the backend refactor, those workstreams own their narrower inventories/generation concerns.

Exit criteria:
- Canonical targeted verification passes.
- Documentation reflects the new API conventions and breaking changes.
- Phase 0 artifacts no longer contain unresolved high-risk rows without explicit deferment.

## 8. Verification Policy

Run relevant checks after each slice, not only at the end.

Minimum per slice:
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

Add by touched layer:
- API/controller/auth/error changes: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- Application/CQRS changes: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- Persistence/query changes: `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

Do not use solution-level `dotnet test` unless repo guidance changes.

## 9. Sequencing Recommendation

Use this order:

0. Inventory and classification.
1. Tenant execution model and fail-closed filters.
2. Authorization policy matrix and privileged policy hardening.
3. ProblemDetails/error catalog/result mapping.
4. Route names, HAL, OpenAPI operation consistency.
5. Controller decomposition.
6. CreateEvent/CQRS decomposition.
7. Repository cancellation/query-shape/pagination/index work.
8. Audit logging, idempotency, and concurrency hardening.
9. Final architecture tests and docs.

Audit/idempotency/concurrency must be designed early and implemented alongside the use-case and persistence phases; do not postpone them as cosmetic cleanup.
