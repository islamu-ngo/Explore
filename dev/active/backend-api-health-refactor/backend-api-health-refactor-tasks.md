<!-- ABOUTME: Executable task checklist for the backend/API health refactor workstream. -->
<!-- ABOUTME: Breaks the refactor into verifiable backend/API implementation slices. -->

# Backend API Health Refactor Tasks

Last Updated: 2026-05-07 Europe/Brussels

## Phase 0A — Mandatory Inventory Only

- [x] Create `dev/active/backend-api-health-refactor/endpoint-inventory.md`.
- [x] Populate endpoint inventory columns: `Endpoint`, `Method`, `Route`, `RouteName`, `Current Auth`, `Target Classification`, `AuthClassification`, `TenantMode`, `RateLimitPolicy`, `CachePolicy`, `HAL Links`, `OpenAPI OperationId`, `Risk`, `Action`.
- [x] Create `dev/active/backend-api-health-refactor/endpoint-classification.md`.
- [ ] Classify each endpoint as Public, Authenticated, Admin, host-admin, setup/bootstrap, or system/background.
- [ ] Mark every anonymous endpoint as intentional, suspicious, or queued for hardening.
- [x] Create `dev/active/backend-api-health-refactor/backend-contract-risk-register.md`.
- [ ] Add a risk-register row for every known breaking API, auth, route, response, HAL, OpenAPI, pagination, and tenant-behavior change.
- [x] Create `dev/active/backend-api-health-refactor/authorization-policy-matrix.md`.
- [ ] Fill policy matrix columns: `Resource`, `Action`, `API Policy`, `Handler Attribute`, `Cerbos Resource`, `HAL Rel`, `Default Roles`.
- [x] Create `dev/active/backend-api-health-refactor/tenant-execution-model.md`.
- [x] Define execution modes: `RuntimeTenantRequired`, `RuntimeTenantOptionalPublicRead`, `HostAdministration`, `BackgroundSystem`, `MigrationOrSeeding`, `DesignTime`.
- [x] Define absent-tenant behavior for each endpoint category and execution mode.
- [x] Create `dev/active/backend-api-health-refactor/api-error-catalog.md`.
- [x] Define initial typed problem codes: `validation_failed`, `tenant_required`, `resource_not_found`, `resource_conflict`, `forbidden`, `authentication_required`, `concurrency_conflict`, `duplicate_request`.

## Phase 0B — Non-Invasive Guardrails

- [ ] Add/enable route-name coverage tests for `RouteNames` constants and named endpoints.
- [x] Add architecture test: controllers must not inject repositories directly.
- [x] Add architecture test: API actions must not return Domain entities.
- [x] Add architecture test: API controllers must not call raw tenant-filter bypass helpers.
- [x] Add architecture test: write endpoints require auth metadata or documented exception.
- [x] Add architecture test: endpoint classification matches auth metadata.
- [x] Add skipped architecture test inventory for current missing baseline response metadata, linked to R-005.
- [x] Add architecture test: actions with existing response metadata must declare a 2xx success response.
- [ ] Enable architecture test: every action declares baseline response metadata.
- [ ] Add architecture test: every action declares relevant ProblemDetails error responses.
- [x] Add architecture test: privileged API policies cannot be authentication-only unless intentionally named as authenticated-only.
- [ ] Add architecture test: tenant filter bypass APIs require approved naming/attributes/tests.
- [x] Add test inventory for current ProblemDetails shape and required extensions.
- [ ] Add tenant-filter tests for present tenant, absent runtime tenant, explicit host-admin context, background system context, migration/seeding, and design-time context.
- [x] If any guardrail initially fails or remains skipped, link the skip/failure reason to `backend-contract-risk-register.md` and an implementation task.

## Phase 1A — Tenant Execution Model

- [ ] Implement explicit tenant execution mode representation in the appropriate Persistence/Infrastructure boundary.
- [ ] Ensure `RuntimeTenantOptionalPublicRead` cannot query all tenant-scoped rows by default.
- [ ] Define allowed call sites for `HostAdministration`, `BackgroundSystem`, `MigrationOrSeeding`, and `DesignTime`.
- [ ] Add structured logging fields for tenant execution mode, operation name, reason, actor user ID when available, tenant ID when available, and correlation ID.
- [ ] Add tests for absent-tenant behavior: no rows, 400, 401, 403, startup/config failure, or explicit system path as appropriate.

## Phase 1B — Query Filter Fail-Closed Implementation

- [ ] Replace permissive `TenantContext == null || ...` runtime query-filter semantics.
- [ ] Preserve migration/seeding/design-time behavior through explicit execution modes rather than null checks.
- [ ] Verify pooled `ExploreDbContext` custom state is reset or scoped safely.
- [ ] Add persistence integration tests for tenant-scoped entities under present tenant, absent runtime tenant, host-admin, and background modes.

## Phase 1C — Filter Bypass Quarantine

- [ ] Restrict `IgnoreTenantFilter()` usage to explicitly named cross-tenant/system services.
- [ ] Remove or quarantine `IgnoreAllFilters()` from normal runtime paths.
- [ ] Introduce reason enum/API such as `BeginSystemTenantScope(SystemTenantScopeReason reason, string operationName, Guid? actorUserId = null)` if this is the chosen design.
- [ ] Add audit logging for every tenant-filter bypass and cross-tenant admin read.
- [ ] Add tests proving controllers cannot call raw tenant-filter bypass helpers.

## Phase 1D — Authorization Policy Replacement

- [ ] Replace `template_admin` with a capability/resource/action policy such as `Templates.Manage`.
- [ ] Replace `event_editor` with capability policies such as `Events.Edit` and `Events.Publish` where appropriate.
- [ ] Replace `property_governance_admin` with `CustomProperties.Govern`.
- [ ] Replace `platform_namespace_editor` with `PlatformNamespaces.Edit`.
- [ ] Add policies for `Modules.Manage`, `StorageObjects.ReadPresigned`, and `TenantSettings.Manage` where inventory requires them.
- [ ] Map every changed API policy to `[AuthorizeResource]`/`ISecureRequest`, Cerbos resource/action, HAL rel, and default roles in `authorization-policy-matrix.md`.
- [ ] Audit `ModuleController`, template controllers, property governance endpoints, namespace endpoints, storage/presigned endpoints, and tenant settings endpoints for privileged requirements.
- [ ] Add/extend authorization parity tests for changed resources.

## Phase 1E — Self-Hosted Bootstrap/Admin Verification

- [ ] Define how the first platform/tenant admin is created in self-hosted deployments.
- [ ] Decide whether setup secret, Keycloak group/role mapping, or both are authoritative during bootstrap.
- [ ] Define how bootstrap mode is disabled after completion.
- [ ] Define audit events for bootstrap start, completion, failure, and disablement.
- [ ] Define SingleTenant and MultiTenant differences.
- [ ] Define behavior when auth provider configuration is missing.
- [ ] Add API/integration tests for bootstrap/admin setup behavior.

## Phase 2 — API Contract, Error Catalog, and Result Mapping

- [ ] Introduce `ApiProblemCodes` with stable typed codes from `api-error-catalog.md`.
- [ ] Introduce `ApiProblemFactory` with stable problem type/code/status mapping.
- [ ] Introduce a shared ProblemDetails writer for middleware-generated errors.
- [ ] Ensure every ProblemDetails response includes `traceId`, `timestamp`, and `correlationId` when available.
- [ ] Introduce `CommandResponseResultMapper` for `BaseCommandResponse<T>`, bool deletes, conflicts, duplicate requests, not-found, validation failures, and concurrency conflicts.
- [ ] Replace raw `BadRequest(string)` and ad hoc `Problem(...)` in high-risk controllers.
- [ ] Normalize authorization failure responses for missing identity vs forbidden resource access.
- [ ] Migrate hard-coded route names to `RouteNames.Xxx` constants.
- [ ] Unskip route-name coverage tests after constants/controllers are aligned.
- [ ] Normalize obvious action-word routes to resource-oriented route templates where inventory marks them risky.
- [ ] Update OpenAPI response metadata for ProblemDetails, endpoint classifications, auth classifications, rate-limit policies, cache policies, and tenant modes.
- [ ] Document preview/v1.0 contract stance: breaking changes allowed before v1.0, but documented and regenerated through OpenAPI/client workflows.
- [ ] Run API integration and architecture tests.

## Phase 3 — Controller Decomposition Behind Stable Contracts

- [ ] Confirm Phase 0 and Phase 2 exit criteria before splitting major controllers.
- [ ] Split by resource/use-case cohesion, not method count alone.
- [ ] Evaluate event controller split candidates: `EventsController`, `MyEventsController`, `EventLifecycleController`, `EventPublishingController`, `EventCalendarController`, `EventAspectsController`.
- [ ] Implement only controller splits justified by inventory/risk-register entries.
- [ ] Extract event filter request mapping into API mapper/binder classes.
- [ ] Move calendar filename/canonical URL/file descriptor construction out of the controller.
- [ ] Split settings/admin controllers only where governance, infrastructure diagnostics, auth provider config, setup/onboarding, and footer/settings governance are clearly separate resources/use cases.
- [ ] Extract repeated current-user resolution into shared controller helper or handler authorization path.
- [ ] Ensure all new actions use `RouteNames.Xxx`, explicit response metadata, endpoint classification, auth classification, rate-limit policy, and cache policy metadata.
- [ ] Add behavior-level API tests for decomposed controllers.
- [ ] Update `docs/API.md` and `docs/API_CHANGELOG.md` for route/contract changes.

## Phase 4 — CQRS/Application Use-Case Refactor

- [ ] Decompose `CreateEventCommandHandler` into narrow collaborators, not a single replacement god service.
- [ ] Candidate collaborators: `EventDraftFactory`, `EventScheduleGraphWriter`, `EventTaxonomyAssignmentWriter`, `EventTemplateInstantiationService`, `EventCustomPropertyInitializer`, `EventProjectionRefreshCoordinator`, `EventCreationCacheInvalidator`, `EventCreationMetricsRecorder`.
- [ ] Keep the create-event handler responsible for validation, authorization context, transaction orchestration, idempotency boundary, and response composition only.
- [ ] Define transaction boundary for aggregate creation.
- [ ] Ensure extracted collaborators do not independently call `SaveChangesAsync` unless explicitly part of the unit-of-work design.
- [ ] Define idempotency behavior for create, publish, update, registration, approval/cancellation, setup/bootstrap, and payment-adjacent actions.
- [ ] Add duplicate-submit protection for create-event browser submits.
- [ ] Add optimistic concurrency behavior for event lifecycle/status/settings mutations.
- [ ] Standardize `concurrency_conflict` and `duplicate_request` ProblemDetails outcomes.
- [ ] Remove obsolete `IAuthorizedRequest` compatibility path if no longer needed.
- [ ] Standardize authorization metadata on sensitive commands with `[AuthorizeResource]` and `ISecureRequest`.
- [ ] Replace direct handler-to-handler calls with `IMediator` dispatch for independent use cases or explicit Application query/read services for internal reads.
- [ ] Normalize query handlers to return DTOs, nullable DTOs, or `PaginatedResult<TDto>`, not command envelopes.
- [ ] Centralize cache key/tag construction and mutation invalidation.
- [ ] Extract repeated storage-object URL/presigned URL mapping into a cancellation-aware service.
- [ ] Define audit event taxonomy for tenant/admin/security-sensitive operations.
- [ ] Add audit logging for event publish/unpublish, auth-relevant data changes, presigned URL generation, custom-property governance, templates, modules, namespaces, and tenant settings changes.
- [ ] Ensure audit events include tenant ID, actor ID, resource ID, action, outcome, correlation ID, and reason where relevant.
- [ ] Add unit tests for decomposed event creation, authorization metadata, idempotency, concurrency, cache invalidation, audit logging, and public shell composition.
- [ ] Run Application unit and architecture tests.

## Phase 5A — Persistence Cancellation Tokens

- [ ] Add `CancellationToken` parameters to repository contracts and implementations touched by this workstream.
- [ ] Pass cancellation tokens into EF async calls (`ToListAsync`, `FirstOrDefaultAsync`, `CountAsync`, `ExecuteDeleteAsync`, `SaveChangesAsync`).
- [ ] Add architecture tests or targeted tests for cancellation-aware repository async methods.

## Phase 5B — Tenant-Safe Repository and Query Contracts

- [ ] Preserve entity-first repositories except for explicitly named read-model/query-store ports.
- [ ] Convert projection/report-returning repositories to explicitly named read-model/query-store ports.
- [ ] Ensure cross-tenant read models require reason, authorization, and audit logging.
- [ ] Add persistence tests for tenant isolation and soft delete across changed contracts.

## Phase 5C — Query Shape Cleanup

- [ ] Split large query/filter logic in `EventRepository` and `EventSessionRepository` into specification appliers/query objects.
- [ ] Replace broad include graphs with projection-specific read models or explicit detail query methods only where hotspots are confirmed.
- [ ] Verify `AsNoTracking` for read-only queries unless mutation/tracking is required.

## Phase 5D — Cursor Pagination Contract

- [ ] Define opaque versioned cursor format.
- [ ] Bind cursors to tenant ID and filter/sort hash where needed to prevent unsafe cursor reuse.
- [ ] Define sort direction, previous/next behavior, filter-change behavior, deletion/insertion stability, and total-count policy.
- [ ] Define which endpoints remain offset-based and why.
- [ ] Introduce cursor/keyset pagination for high-volume public event/session/list feeds.
- [ ] Add stable ordering and page-input validation for remaining offset pagination.
- [ ] Update OpenAPI response schema for cursor-paginated endpoints.

## Phase 5E — Index and Migration Review

- [ ] Review indexes for high-volume event/session/feed queries.
- [ ] Ensure tenant-scoped high-volume queries have tenant ID in useful composite indexes.
- [ ] Review public event listing indexes around `TenantId`, `Status`, `Visibility`, `StartDate`/`CreatedAt`, `Id`, and `DeletedAt`/`IsDeleted`.
- [ ] Ensure soft-delete predicates remain index-friendly.
- [ ] Add migration tests/model assertions for critical indexes.

## Phase 5F — Transaction Retry Strategy

- [ ] Wrap manual transactions in EF execution strategies or route them through `IUnitOfWork`.
- [ ] Add persistence integration tests for transaction retry behavior.
- [ ] Verify outbox/background dispatch remains idempotent where durable side effects are involved.
- [ ] Run Persistence integration and architecture tests.

## Phase 6 — Final Guardrails, Docs, and Cleanup

- [ ] Convert Phase 0B temporary skipped/failing tests into passing hard gates or explicitly deferred risks.
- [ ] Add/finalize architecture test: protected writes have endpoint auth and handler resource metadata where applicable.
- [ ] Add/finalize architecture test: endpoint classification matches auth metadata.
- [ ] Add/finalize architecture test: every action declares success and ProblemDetails responses.
- [ ] Add/finalize architecture test: no direct handler injection into handlers/controllers unless explicitly allowed.
- [ ] Add/finalize architecture test: repository interfaces expose cancellation-aware async methods.
- [ ] Add/finalize architecture test: tenant filter bypass APIs require approved naming/attributes/tests.
- [ ] Add/finalize architecture test: high-risk POSTs have idempotency posture documented/tested.
- [ ] Add/finalize architecture or integration tests: lifecycle/status/settings mutations have optimistic concurrency posture documented/tested.
- [ ] Add/finalize audit coverage tests for tenant/admin/security-sensitive operations.
- [ ] Update `docs/API.md` for new controller/error/route/rate-limit/cache conventions.
- [ ] Update `docs/SECURITY-MODEL.md` and `docs/AUTHORIZATION.md` for new policy/resource authorization rules and bootstrap/admin behavior.
- [ ] Update `docs/MULTI_TENANCY.md` for fail-closed tenant context semantics and execution modes.
- [ ] Update `docs/OPERATIONS.md` for ProblemDetails/observability/rate-limit/audit behavior if changed.
- [ ] Update `docs/API_CHANGELOG.md` with development-mode breaking API changes.
- [ ] Regenerate OpenAPI/generated clients where API surface changes require it.
- [ ] Run full targeted verification set: build, architecture, API integration, Application unit, Persistence integration.

## Definition of Done

- [ ] All mandatory Phase 0 artifacts exist and are current.
- [ ] All planned backend/API slices implemented or explicitly deferred with rationale in `backend-contract-risk-register.md`.
- [ ] No known tenant-isolation fail-open paths remain in runtime queries.
- [ ] No privileged policy name maps to authentication-only behavior unless intentionally named as authenticated-only.
- [ ] Self-host first-admin/bootstrap behavior is documented, tested, auditable, and disableable.
- [ ] Route-name coverage tests are enabled and passing.
- [ ] ProblemDetails contract and typed error catalog are centralized and tested.
- [ ] Controllers comply with thin-controller conventions and were decomposed only after stable contracts existed.
- [ ] CQRS handlers comply with repo response, authorization, validation, transaction, idempotency, concurrency, and cancellation conventions.
- [ ] Persistence repositories comply with entity-first, tenant-safe, cancellation-aware, query-shape, pagination, index, and transaction conventions.
- [ ] Audit logging covers tenant/admin/security-sensitive operations.
- [ ] High-risk POSTs and lifecycle/status/settings updates have idempotency/concurrency posture implemented or explicitly deferred with risk rationale.
- [ ] OpenAPI, generated clients, docs, and API changelog reflect all breaking changes.
- [ ] Canonical targeted verification passes.
