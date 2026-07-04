<!-- ABOUTME: Enterprise implementation plan for admin support access and break-glass impersonation. -->
<!-- ABOUTME: Replaces the old claim-spoofing plan with a source-grounded, auditable, Clean Architecture design. -->

# Admin Support Access / Break-Glass Impersonation Implementation Plan

Last Updated: 2026-07-04 Europe/Brussels

## 0. Planning Metadata

Status: Re-baselined implementation plan with core runtime/BFF/UI/audit and tenant evidence slices implemented. Focused support-access verification is green; broad API/Persistence suites are currently blocked by unrelated failures, and OpenAPI clean-state is blocked by already-dirty generated artifacts.

Planning command followed: `.claude/commands/dev-docs.md`.

Requested review mode: `$senior-cto-feedback`.

Feature name: Admin Support Access. The directory keeps the historical "admin impersonation" name, but the implementation must use support-access language in code and UX unless a workflow is explicitly acting as a named tenant user.

Planning intent classification:

| Scope | Intent / rule impact |
| --- | --- |
| Planning docs | No exact intent exists in `.claude/contract/intents.yaml`; this workstream follows the `dev/active/README.md` and `.claude/commands/dev-docs.md` documentation contract. |
| API write contract | `add-write-endpoint`, `api-controllers`, `application-layer`, `auth-patterns`, `cqrs-mediatr-guidelines`. |
| API read contract | `add-get-endpoint`, `api-controllers`, `api-hateoas`. |
| HAL affordances | `add-hal-link`, `api-hateoas`, `blazor-component-affordance`; clients gate by `_links`, never by role or claim inspection. |
| Persistence | `add-ef-migration`, `update-repository-query`, `domain`, `efcore-persistence`, `efcore-migrations`, `dotnet-efcore-guidelines`. |
| BFF/auth | `bff-auth-bug`, `blazor-server`, `blazor-bff-patterns`, `auth-patterns`. |
| UI | `blazor-component-affordance`, `blazor-client`, `blazor-ui-conventions`, `design-system`, `ACCESSIBILITY.md`. |
| Authorization policy | `cerbos-policy-change`, `AUTHORIZATION.md`, `SECURITY-MODEL.md`; local and Cerbos authorization must stay behaviorally equivalent. |

Authoritative docs and skills loaded for this plan:

- `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/DOMAIN.md`, `docs/MULTI_TENANCY.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/DESIGN_SYSTEM.md`, `docs/ACCESSIBILITY.md`, `docs/TESTING.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`.
- `.claude/rules/api-controllers.md`, `api-hateoas.md`, `application-layer.md`, `domain.md`, `efcore-persistence.md`, `efcore-migrations.md`, `blazor-server.md`, `blazor-client.md`, `tests.md`.
- Skills: `senior-cto-feedback`, `auth-patterns`, `blazor-bff-patterns`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `blazor-ui-conventions`, `error-tracking`.
- External research: Tavily research on enterprise JIT support access and break-glass controls; Context7 docs for ASP.NET Core, EF Core, and MudBlazor.

## 1. Executive Summary

The old plan must be replaced. It proposed adding impersonation claims to the BFF cookie and checking those claims directly in HAL policies. That conflicts with this repo's security model because browser-visible or cookie-derived authority cannot become the source of tenant data access, and HAL affordances must be driven by the API authorization pipeline.

The new design implements admin support access as a first-class, time-boxed, auditable support session. An instance admin never silently becomes a tenant admin and never receives a durable tenant role grant. Instead, the system creates a short-lived support-access session with explicit tenant scope, reason/ticket metadata, expiry, allowed mode, and immutable audit events. The BFF stores only an opaque active session reference server-side and forwards support-access context to the API through trusted server-owned headers after stripping any browser-supplied equivalents. The API validates the session against persisted state on every request, binds an `ISupportAccessContext`, and the existing authorization/HAL pipeline evaluates support-access permissions fail-closed.

Default posture:

- Disabled unless the instance explicitly enables support access.
- Read-only mode first; write mode requires a separate explicit setting and request.
- Mandatory reason and ticket/reference.
- Short maximum duration with forced expiry.
- One active support session per actor by default.
- Clear in-app banner and stop action for the admin.
- Tenant-visible audit evidence.
- No standing role grants, no spoofed user ID, no token exposure to WebAssembly.

Current implementation state:

- Core Domain/Application/Persistence support-access entities, lookups, repositories, settings, migration, and focused tests are implemented.
- API runtime support is implemented through CQRS commands/queries, `SupportAccessController`, HAL assemblers/link policies, support-access route names, OpenAPI schema registration, local fallback handling, and Cerbos policy/schema/tests.
- Runtime support context is explicit-header-only: the API honors support access only when the BFF/server forwards a trusted `X-Support-Access-Session-Id` and the persisted session remains active, actor-owned, tenant-matched, unexpired, and enabled by governance settings.
- BFF support is implemented through server-side active-session storage, support-header sanitization, YARP/HttpClient forwarding, and `/bff/support-access/current`, `/bff/support-access/sessions`, `/bff/support-access/sessions/current/stop`, session-history, audit-event, and force-stop routes.
- Blazor shell, operator-console, and tenant-facing evidence support are implemented through `SupportAccessClientService`, HAL-preserving support-access resource models, `SupportAccessBanner`, `SupportAccessConsoleSection`, and `TenantSupportAccessEvidenceSection`; authenticated browser E2E and full-project verification remain open hardening work.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Finding | Evidence |
| --- | --- |
| No first-class support-access or impersonation feature exists. | `rg` for impersonation/sudo/effective user terms found only the old workstream and unrelated fraud-reason text. CodeGraph exploration did not surface a production impersonation session model. |
| Domain entities live directly under `Explore.Domain/`, not `Explore.Domain/Entities/`. | `docs/DOMAIN.md`, `docs/CODEBASE_STRUCTURE.md`, current entities such as `Explore.Domain/AuditLog.cs` and `Explore.Domain/ConfigurationChangeLog.cs`. |
| Admin authority is DB-first and server-side. | `Explore.Application/Contracts/Identity/IAdminContext.cs`, `Explore.Infrastructure/Identity/AdminContext.cs`, `Explore.Application/Features/Users/Handlers/Queries/GetAdminAuthorityRequestHandler.cs`, `Explore.API/Controllers/UserController.cs`. |
| The BFF enriches admin claims server-side only. | `Explore.Blazor/Services/BffAdminClaimsTransformation.cs` fetches `/api/User/admin-authority`; `docs/SECURITY-MODEL.md` and `docs/BLAZOR.md` say browser auth serialization excludes admin/tenant/user authority claims. |
| BFF endpoints are extension based, not controller based. | `Explore.Blazor/Extensions/BffEndpointExtensions.cs`, `BffAuthEndpoints.cs`, `BffPreferenceEndpoints.cs`, `BffStorageEndpoints.cs`. |
| YARP is the trusted BFF boundary. | `Explore.Blazor/Extensions/YarpProxyExtensions.cs` strips browser-controlled headers, forwards bearer tokens, forwards server-resolved tenant slug, and injects setup secret only when required. |
| Browser-controlled privileged headers are already sanitized. | `Explore.Blazor/Services/BffProxyHeaderSanitizer.cs` removes `Authorization`, token headers, API keys, setup secret, and tenant headers before proxying. |
| Tenant isolation is fail-closed. | `docs/MULTI_TENANCY.md` requires tenant resolution to fail closed and filter bypass to be explicit, justified, and narrowly scoped. |
| Authorization is layered and must be parity-tested. | `docs/AUTHORIZATION.md`, `Explore.Application/Behaviors/AuthorizationBehavior.cs`, Cerbos/local provider model, HATEOAS four-phase candidate pipeline. |
| Audit infrastructure exists but is generic. | `Explore.Domain/AuditLog.cs`, `Explore.Persistence/Configurations/Entities/AuditLogConfiguration.cs`, `Explore.Domain/ConfigurationChangeLog.cs`; no support-session-aware audit model exists. |
| Multi-step writes use explicit `IUnitOfWork`. | `Explore.Application/Contracts/Persistence/IUnitOfWork.cs`, `Explore.Persistence/EfCoreUnitOfWork.cs`, command handler examples such as `PublishEventCommandHandler`. |
| External best practice favors JIT, least privilege, short sessions, ticketing, kill switch, and immutable audit. | Tavily research sources included NIST AC-6 least privilege, NIST/UpGuard AU-12 audit generation, OWASP Logging Cheat Sheet, Microsoft Entra PIM deployment planning, AWS temporary elevated access broker sample, and temporary elevated access guidance. |
| Framework docs support the BFF mechanics. | Context7 ASP.NET Core docs: secure minimal endpoints with `RequireAuthorization`, validate antiforgery on unsafe endpoints after auth, use cookie/auth events and claims transformation carefully. |
| Framework docs support persistence mechanics. | Context7 EF Core docs: concurrency tokens, global tenant query filters, audit persistence patterns, and migrations/indexes are appropriate for persisted sessions. |
| UI docs support expected controls. | Context7 MudBlazor docs: dialogs via `IDialogService`, forms/validation, buttons/actions, tables, and alert-like surfaces are available; repo wrappers must be preferred. |

### 2.2 Old Plan Defects

The previous plan is not safe to implement as written:

- It treats impersonation as a BFF cookie claim (`impersonated_tenant_id`, `impersonation_audit_id`) instead of a persisted, server-validated support session.
- It suggests HAL policy checks like `user.IsInRole(...)` and direct claim matching, which bypasses the repo's central authorization and HAL pipeline.
- It proposes `Explore.Domain/Entities/ImpersonationAuditLog.cs`, which violates the current Domain folder convention.
- It proposes a Blazor `Controller`, but current BFF endpoints are mapped through extension files.
- It does not define expiry, forced stop, idempotency, concurrency, kill switch, operator settings, tenant-visible evidence, write restrictions, or fail-closed behavior.
- It does not protect against browser-supplied support headers.
- It does not define Cerbos/local authorization parity.
- It does not define audit guarantees for mutating actions.
- It does not define OpenAPI/NSwag/HAL sequencing.

## 3. Future State

Admin Support Access gives an instance admin a time-boxed, explicitly scoped support session into a tenant context without granting durable tenant membership and without pretending that the admin is the tenant user.

### 3.1 Primary Use Cases

1. Instance admin starts a read-only support session for a tenant to inspect configuration or reproduce a tenant-reported issue.
2. Instance admin starts a write-capable support session only when instance policy permits it and the request includes a required ticket/reason.
3. Instance admin stops the active session manually.
4. The system expires active sessions automatically.
5. Tenant admins and instance admins can review support-access history and audit events.
6. Operators can disable support access globally and force-stop active sessions.

### 3.2 Non-Goals For The First Implementation Slice

- No standing tenant role grants are created.
- No permanent "act as user" token is minted.
- No browser-visible authority claims are added.
- No email/slack notification delivery is required in the first slice; audit events and structured logs are required. Notifications can use outbox in a later slice.
- No backwards compatibility with the old plan is needed because no production implementation exists.

### 3.3 Terminology

| Term | Meaning |
| --- | --- |
| Actor | The real authenticated instance admin operating the session. This remains the value used for `ICurrentUserService.UserId`. |
| Target tenant | The tenant whose data may be accessed under the support session. |
| Target user | Optional display persona only when the product explicitly needs "view as user"; it must not replace the actor for audit. |
| Support-access session | Persisted, time-boxed grant that authorizes a bounded support context. |
| Effective context | Tenant/scope/mode attributes used by authorization and audit; not a replacement identity. |
| Break-glass | Emergency write-capable support access with stricter controls and higher audit severity. |

## 4. Constraints

Architecture constraints:

- Clean Architecture dependencies remain inward: Domain has no outward dependencies; Application owns CQRS contracts and abstractions; Infrastructure/Persistence implement them; API and Blazor compose and expose.
- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Lookups use `int`; aggregate/session IDs use UUIDv7 `Guid`; cursor IDs use `long`.
- Multi-step writes use `IUnitOfWork.ExecuteInTransactionAsync`; delegates must be retry-safe and must not perform external I/O.
- New route names must be added to `RouteNames` and referenced by HAL policy code.
- API write endpoints require `[Authorize]`, explicit `ProblemDetails` response metadata, and idempotency where relevant.
- Browser/WebAssembly must never receive bearer tokens, support-access authority claims, target tenant grants, or support session secrets.
- HAL links are the only source of UI action affordances.
- Cerbos and local authorization behavior must match.

Security constraints:

- Instance admin authority alone is not tenant data authority.
- Support access must be disabled by default until instance policy enables it.
- Session validation must fail closed if the session is missing, expired, stopped, revoked, disabled by policy, tenant-mismatched, or actor-mismatched.
- Support access must not bypass tenant filters globally. Any filter bypass must be explicit, reasoned, and limited to session lookup/audit queries.
- Inbound browser-supplied `X-Support-Access-*`, tenant, token, API key, and setup-secret headers must be stripped before proxying.
- Audit evidence must preserve actor, target tenant, session ID, scope/mode, reason/ticket, timing, outcome, and correlation ID.
- Logs and metrics must not include raw tokens, cookies, payloads, or unbounded user-provided text.

## 5. Architecture Decisions

### AD-001: Model Support Access As A Persisted Session, Not A Claim

Create a Domain aggregate named `SupportAccessSession` with lifecycle methods for `Start`, `Stop`, `Expire`, and `Revoke`. The session is the authority record. The BFF cookie may reference an active session ID, but the API must validate the persisted session on every request before any support-access permission is honored.

Rationale: claims are stale and too easy to over-trust. A persisted session supports forced stop, expiry, audit review, concurrency control, and operator kill switches.

### AD-002: Preserve Actor Identity

`ICurrentUserService.UserId` must continue to return the real authenticated user. Do not replace it with the target tenant user. Introduce a separate `ISupportAccessContext` for support metadata:

- `IsActive`
- `SessionId`
- `ActorUserId`
- `TargetTenantId`
- `TargetTenantUserId`
- `Mode`
- `StartedAtUtc`
- `ExpiresAtUtc`
- `ReasonCode`
- `TicketReference`

Rationale: audit and accountability are lost if the application believes the instance admin is the tenant user.

### AD-003: Use Trusted BFF Header Injection

Add support-access forwarding to the existing BFF proxy boundary:

- Extend `BffProxyHeaderSanitizer` to remove all browser-supplied `X-Support-Access-*` headers.
- Add a BFF server service that resolves the active support-access session from server-side auth properties/cookie state and persisted API state.
- Extend `YarpProxyExtensions` and relevant server-side `HttpClient` handlers to add only server-owned support-access headers, such as `X-Support-Access-Session-Id`.
- The API ignores support-access headers unless the bearer token actor matches the persisted session actor and the session is active.

Rationale: this matches the existing token, tenant, and setup-secret forwarding pattern.

### AD-004: Authorize Through The Existing Provider Pipeline

Support access must be added to the authorization layer, not scattered through controllers or UI code. The Application authorization context must include support-access attributes, and both the local provider and Cerbos policies must understand them.

Resource/action examples:

- Resource kind: `support_access_session`
- Actions: `start`, `stop`, `view`, `list`, `view_audit`, `force_stop`
- Tenant resource decisions: support access may satisfy tenant-scoped read actions only when the target tenant matches and the session mode allows the action.
- Write actions require explicit `AllowWriteSupportAccess` policy, session mode `Write`, and action allow-list parity in local and Cerbos policies.

### AD-005: Audit Is Part Of The Security Boundary

Add first-class support-access audit entities instead of overloading generic `AuditLog` only:

- `SupportAccessSession`: current lifecycle and immutable identifiers.
- `SupportAccessAuditEvent`: append-only lifecycle and request/action evidence.

Mutating tenant actions under support access must not commit unless support-access action audit evidence is persisted in the same transaction or an explicit fail-closed path blocks the command. Read audit can be recorded through API middleware or Application behavior with best-effort retries, but failures must be observable and rate limited.

### AD-006: Read-Only Is The Default Mode

Initial support sessions are read-only by default. Write-capable sessions require:

- Instance setting enabled.
- Explicit mode selection.
- Mandatory reason and ticket/reference.
- Shorter maximum duration.
- Separate HAL affordance and authorization decision.
- Higher severity audit event.

### AD-007: No Compatibility Shims

There is no production impersonation implementation to preserve. Rename concepts and routes to support access where practical. Do not carry the old `impersonated_tenant_id` / `impersonation_audit_id` claim design forward.

## 6. Implementation Phases

### Phase 1: Foundation, Contracts, And Policy Surface

#### Task 1.1: Define Support-Access Constants And Settings

Files likely in scope:

- `Explore.Application/Constants/*`
- `Explore.Application/Options/*` or existing configuration/governance setting model
- `docs/CONFIGURATION.md`
- `docs/SELF_HOSTING.md`

Implementation detail:

- Add support-access header constants.
- Add policy/config keys:
  - `SupportAccess:Enabled` or governance key `support_access.enabled` default `false`.
  - `SupportAccess:MaxReadOnlyMinutes` default 30.
  - `SupportAccess:MaxWriteMinutes` default 10.
  - `SupportAccess:AllowWriteMode` default `false`.
  - `SupportAccess:RequireTicketReference` default `true`.
  - `SupportAccess:OneActiveSessionPerActor` default `true`.
- Document environment variable equivalents and self-hosting defaults.

Acceptance:

- Defaults are fail-closed.
- Config docs explain the kill switch and operational impact.

#### Task 1.2: Add Application Contracts

Files likely in scope:

- `Explore.Application/Contracts/Identity/ISupportAccessContext.cs`
- `Explore.Application/Contracts/Identity/ISupportAccessSessionService.cs`
- `Explore.Application/Contracts/Persistence/ISupportAccessSessionRepository.cs`
- `Explore.Application/Contracts/Persistence/ISupportAccessAuditEventRepository.cs`

Implementation detail:

- Keep contracts in Application.
- Repositories return Domain entities.
- Context carries support metadata but never replaces the real user.

Acceptance:

- Application has no dependency on API, Blazor, Persistence, or Infrastructure implementations.

#### Task 1.3: Extend Authorization Vocabulary

Files likely in scope:

- `Explore.Application/Authorization/*`
- `Explore.Application/Constants/AuthorizationActions.cs`
- Cerbos policies under `cerbos/**`
- Local fallback policy provider/test fixtures
- `docs/AUTHORIZATION.md`

Implementation detail:

- Add support-access resource kinds/actions.
- Add support-access attributes to authorization context/principal/resource mapping.
- Define read/write action behavior and tenant-match requirement.
- Add parity tests covering local provider and Cerbos decisions.

Acceptance:

- Unknown support-access state denies.
- Active read-only session cannot perform writes.
- Active write session cannot cross target tenant.
- Local and Cerbos decisions match.

### Phase 2: Domain And Persistence

#### Task 2.1: Add Domain Entities And Lookups

Files likely in scope:

- `Explore.Domain/SupportAccessSession.cs`
- `Explore.Domain/SupportAccessAuditEvent.cs`
- `Explore.Domain/Enums/*`
- `Explore.Domain/Lookups/*` if lookup classes follow existing patterns

Implementation detail:

`SupportAccessSession` fields:

- `Id` UUIDv7 `Guid`
- `ActorUserId`
- `TargetTenantId`
- optional `TargetTenantUserId`
- `StatusId` lookup: `PendingApproval`, `Active`, `Stopped`, `Expired`, `Revoked`
- `ModeId` lookup: `ReadOnly`, `Write`
- `ReasonCode` / `ReasonText`
- `TicketReference`
- optional `ApprovedByUserId`
- `StartedAtUtc`
- `ExpiresAtUtc`
- `EndedAtUtc`
- `EndReasonId`
- `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- `ConcurrencyStamp` or row-version equivalent consistent with local conventions

`SupportAccessAuditEvent` fields:

- `Id` UUIDv7 `Guid`
- `SupportAccessSessionId`
- `OccurredAtUtc`
- `EventTypeId` lookup: `Started`, `Stopped`, `Expired`, `Revoked`, `Denied`, `RequestObserved`, `CommandCommitted`
- `ActorUserId`
- `TargetTenantId`
- optional `TargetTenantUserId`
- `RouteName` or `RequestName`
- `ResourceKind`
- `ResourceId`
- `Action`
- `Outcome`
- `HttpStatusCode`
- `CorrelationId` / `TraceId`
- sanitized metadata JSON

Acceptance:

- Domain methods enforce valid transitions.
- Stopped/expired/revoked sessions cannot be reactivated.
- Expiry is calculated from injected time in handlers, not `DateTime.Now`.

#### Task 2.2: Configure EF Core Mapping And Migrations

Files likely in scope:

- `Explore.Persistence/Configurations/Entities/SupportAccessSessionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/SupportAccessAuditEventConfiguration.cs`
- `Explore.Persistence/ExploreDbContext.DbSets.cs`
- `Explore.Persistence/Migrations/*`
- `schemas/islamu-event.md`

Implementation detail:

- Use `IEntityTypeConfiguration<T>`.
- Add FK to `Tenant` for `TargetTenantId`.
- Add FK to `User` for `ActorUserId` and optional `TargetTenantUserId` where possible.
- Add indexes:
  - active session lookup by actor/status/expiry
  - active session lookup by id/actor/status
  - audit lookup by tenant/session/time
  - audit lookup by actor/time
  - optional partial unique index for one active session per actor when feasible
- Seed lookup data with stable `int` IDs.
- Do not rewrite old migrations.

Acceptance:

- Migration `Down` reverses the schema.
- Query filters are explicit. If session/audit entities are not `ITenantEntity`, repositories must always filter by actor/target tenant/admin authority.
- Persistence integration tests prove no cross-tenant reads through default query paths.

#### Task 2.3: Add Repositories

Files likely in scope:

- `Explore.Persistence/Repositories/SupportAccessSessionRepository.cs`
- `Explore.Persistence/Repositories/SupportAccessAuditEventRepository.cs`
- `Explore.Persistence/PersistenceServicesRegistration.cs`

Implementation detail:

- Repositories return entities.
- Read-only query methods use `AsNoTracking()`.
- Session validation query must include session ID, actor ID, status, expiry, and target tenant.
- Any filter bypass has a code-level reason string and bounded predicate.

Acceptance:

- There is no generic "get all sessions across all tenants" method without explicit admin scope parameters.

### Phase 3: Application, API, And Authorization Runtime

#### Task 3.1: Add CQRS Requests, Validators, Handlers, DTOs

Files likely in scope:

- `Explore.Application/Features/SupportAccess/Requests/Commands/*`
- `Explore.Application/Features/SupportAccess/Requests/Queries/*`
- `Explore.Application/Features/SupportAccess/Handlers/*`
- `Explore.Application/DTOs/SupportAccess/*`

Commands:

- `StartSupportAccessSessionCommand`
- `StopSupportAccessSessionCommand`
- `ForceStopSupportAccessSessionCommand`

Queries:

- `GetCurrentSupportAccessSessionRequest`
- `ListSupportAccessSessionsRequest`
- `GetSupportAccessAuditEventsRequest`

Implementation detail:

- Manually instantiate validators.
- Start command validates instance admin authority through `IAdminContext`.
- Start command validates enabled setting, target tenant active state, mode availability, duration cap, reason, ticket/reference, and one-active-session policy.
- Stop command is idempotent for the actor's own active session.
- Force stop requires instance-admin authorization and writes a `Revoked` event.
- Multi-step session/audit writes use `IUnitOfWork.ExecuteInTransactionAsync`.

Acceptance:

- No handler mutates `TenantUserRoleGrant`.
- No handler emits external I/O inside the transaction.
- Every state change creates audit evidence.

#### Task 3.2: Add API Endpoints

Files likely in scope:

- `Explore.API/Controllers/SupportAccessController.cs`
- `Explore.API/RouteNames.cs`
- OpenAPI/NSwag generated artifacts if this API is client-visible
- `docs/API.md`
- `docs/API_CHANGELOG.md`

Routes:

- `POST /api/support-access/sessions`
- `POST /api/support-access/sessions/{sessionId:guid}/stop`
- `POST /api/support-access/sessions/{sessionId:guid}/force-stop`
- `GET /api/support-access/sessions/current`
- `GET /api/support-access/sessions`
- `GET /api/support-access/sessions/{sessionId:guid}/audit-events`

Implementation detail:

- Controller is thin: validate route/body basics, dispatch MediatR, return `ActionResult`.
- All endpoints require `[Authorize]`.
- Write endpoints include `ProblemDetails` metadata and idempotency where applicable.
- Route names are constants.
- Rate limits use existing authenticated/admin patterns.

Acceptance:

- 401 when unauthenticated.
- 403 when not instance admin.
- 404/403 behavior does not leak tenant existence to unauthorized users.
- 409 for concurrency conflicts.
- 422/400 for validation failures per existing project conventions.

#### Task 3.3: Add Support-Access Runtime Binding

Files likely in scope:

- `Explore.API/Middleware/*`
- `Explore.Infrastructure/Identity/*`
- `Explore.Application/Behaviors/*`
- `Explore.API/Program.cs` or middleware extension files

Implementation detail:

- API middleware reads trusted support-access header only after authentication.
- Middleware validates persisted session and binds scoped `ISupportAccessContext`.
- Validation checks actor, status, expiry, target tenant, settings kill switch, and tenant mismatch.
- Middleware records denied/expired events with throttling to avoid log floods.
- Authorization behavior and provider include support-access context in resource decisions.

Acceptance:

- Missing/invalid session header behaves as normal non-support request.
- Invalid active-session claim/header cannot grant access.
- Expired session denies and can mark itself expired.

#### Task 3.4: HAL And Affordance Integration

Files likely in scope:

- `Explore.API/Hateoas/**/*.cs`
- `Explore.Blazor.Client/**`

Implementation detail:

- Add links for support-access start/stop/audit only through HAL policies.
- Existing tenant resource edit/delete links must appear for support access only when the authorization provider allows that action.
- UI components use `HasHalLink`/link presence only.

Acceptance:

- No Blazor component checks `IsInstanceAdmin`, support-access claims, or roles to decide whether to show tenant action buttons.
- HAL fails closed when route or authorization evaluation is uncertain.

### Phase 4: BFF Session Boundary

#### Task 4.1: Add BFF Support-Access Endpoints

Files likely in scope:

- `Explore.Blazor/Extensions/BffSupportAccessEndpoints.cs`
- `Explore.Blazor/Extensions/BffEndpointExtensions.cs`
- `Explore.Blazor/Services/BffSupportAccessSessionStore.cs`
- `Explore.Blazor/Services/SupportAccessForwardingHandler.cs`

BFF routes:

- `GET /bff/support-access/current`
- `POST /bff/support-access/sessions`
- `POST /bff/support-access/sessions/current/stop`
- `GET /bff/support-access/tenants/{targetTenantId}/sessions`
- `GET /bff/support-access/tenants/{targetTenantId}/sessions/{sessionId}/audit-events`
- `POST /bff/support-access/sessions/{sessionId}/force-stop`

Implementation detail:

- Unsafe endpoints call `.ValidateAntiforgery()` and `.RequireAuthorization()`.
- Endpoints call the API through server-side HttpClient with token forwarding.
- BFF stores only opaque active session reference in server-side distributed cache keyed to authenticated user and OIDC session id.
- List/audit endpoints buffer API responses before returning them so the outbound API response is not disposed before ASP.NET Core writes the BFF response body.
- Force-stop clears the server-side active-session reference when the revoked session is the operator's current cached support session.
- Current-session response returns safe display fields: session ID, target tenant id, mode, expiry, reason/ticket summary, and API HAL/action links where available. It does not return tokens or authority claims.

Acceptance:

- Start/stop cannot be called without auth and antiforgery token.
- Browser-visible auth state still excludes admin/tenant/support authority claims.

#### Task 4.2: Extend Header Sanitization And YARP Forwarding

Files likely in scope:

- `Explore.Blazor/Services/BffProxyHeaderSanitizer.cs`
- `Explore.Blazor/Extensions/YarpProxyExtensions.cs`
- `Explore.Blazor/Services/SupportAccessForwardingHandler.cs`
- `Explore.Blazor/Extensions/HttpClientExtensions.cs`

Implementation detail:

- Strip all inbound support-access headers:
  - `X-Support-Access-Session-Id`
  - `X-Support-Access-Target-Tenant-Id`
  - `X-Support-Access-Mode`
  - any agreed future prefix.
- Re-add only server-resolved session ID when active.
- Ensure the API validates the forwarded session against the resolved tenant context before honoring support access.
- Direct server-side API clients use the same forwarding handler ordering as token/tenant/setup handlers.

Acceptance:

- Integration tests prove a browser-supplied support-access header is ignored.
- Integration tests prove BFF adds trusted session context only for an active persisted session owned by the current actor.

### Phase 5: Blazor UX

#### Task 5.1: Add Client Service Layer

Files likely in scope:

- `Explore.Blazor.Client/Contracts/Services/SupportAccess/ISupportAccessClientService.cs`
- `Explore.Blazor.Client/Services/SupportAccessClientService.cs`
- DTOs/models under existing client conventions

Implementation detail:

- Components call service methods, not generated clients directly.
- Service calls BFF endpoints.
- `ApiException`/ProblemDetails are mapped to user-safe errors and structured logs.

Acceptance:

- No token, raw cookie, or support-access header is handled in client code.

#### Task 5.2: Add Admin Start UX

Files likely in scope:

- Admin/settings area components following current routing conventions
- `*.razor.css` CSS isolation files

Implementation detail:

- Tenant search/select.
- Optional target-user id capture remains display/evidence metadata only and does not replace actor identity.
- Mode segmented control: read-only first, write disabled unless policy allows.
- Duration selector capped by policy.
- Reason and ticket/reference fields.
- Clear warning that actions are audited and visible.
- Start button enabled only when HAL/status allows it.
- Use repo wrappers where available (`AppButton`, `AppIconButton`, `AppDialogShell`, etc.) and MudBlazor v9 APIs through those wrappers where conventions require.

Acceptance:

- Accessible labels and validation messages.
- Focus returns after dialog close.
- No visible instruction text that substitutes for proper controls.

#### Task 5.3: Add Persistent Active Session Banner

Files likely in scope:

- `Explore.Blazor.Client/Layout/*`
- support-access state provider/service

Implementation detail:

- App-wide banner visible during active support access.
- Shows target tenant, mode, expiry countdown, ticket/reference summary, and stop action.
- Uses `role="status"` or equivalent accessible announcement pattern.
- Stop action is keyboard reachable and HAL/status gated.
- Banner must not overlap layout content on mobile or desktop.

Acceptance:

- Visual QA verifies banner does not obscure key navigation/content.
- Stop action clears session state and support links disappear after refresh.

#### Task 5.4: Add Audit Viewer

Files likely in scope:

- Admin support-access history page/components

Implementation detail:

- Instance admins see support sessions and audit events through explicit target-tenant scope in the operator console.
- Tenant-facing audit evidence remains a release-scope decision; do not expose cross-tenant history without API authorization and scoped query parameters.
- Use table/list patterns with pagination and filters.
- Never show raw payloads, tokens, or sensitive reason text beyond the intended audience.

Acceptance:

- HAL-gated access.
- Tenant admin cannot see another tenant's sessions.

### Phase 6: Observability, Operations, And Documentation

#### Task 6.1: Add Logs, Metrics, And Traces

Files likely in scope:

- API/Application observability helpers
- `docs/OPERATIONS.md`
- `docs/SECURITY-MODEL.md`

Implementation detail:

- Structured logs include support session ID, actor user ID, target tenant ID, route/request name, decision, and correlation ID where safe.
- Metrics use low-cardinality labels: action, mode, outcome, status. Avoid raw user ID, tenant slug, reason, ticket, or route explosion as labels.
- Traces add support-access session ID as an attribute where safe.
- Emit alert-worthy logs for start, write-mode start, force stop, expiry, denied cross-tenant attempt, and disabled-by-kill-switch.

Acceptance:

- No token/cookie/body payload logging.
- Metrics scrape remains low-cardinality.

#### Task 6.2: Add Operator Controls And Runbooks

Files likely in scope:

- `docs/OPERATIONS.md`
- `docs/CONFIGURATION.md`
- `docs/SELF_HOSTING.md`
- optional admin UI for forced stop

Implementation detail:

- Document global kill switch.
- Document how to list active sessions.
- Document how to force-stop sessions.
- Document retention and backup expectations.
- Document deployment defaults for self-hosters.

Acceptance:

- A self-hosted operator can disable support access without redeploying when using governance settings, or with restart when using config-only mode.

#### Task 6.3: Update Security And Product Docs

Files likely in scope:

- `docs/SECURITY-MODEL.md`
- `docs/AUTHORIZATION.md`
- `docs/MULTI_TENANCY.md`
- `docs/BLAZOR.md`
- `docs/API.md`
- `docs/API_CHANGELOG.md`
- `docs/TESTING.md`

Implementation detail:

- Document support-access threat model.
- Document BFF header stripping/forwarding.
- Document tenant isolation behavior.
- Document Cerbos/local policy parity.
- Document tenant-visible audit.
- Document generated client workflow if public API contract changes.

Acceptance:

- Docs distinguish actor identity from effective support context.

## 7. Testing Strategy

Minimum test projects by phase:

| Area | Required tests |
| --- | --- |
| Domain lifecycle | `Event.Domain.UnitTests` |
| CQRS handlers/validators/auth context | `Event.Application.UnitTests` |
| Infrastructure admin/support context | Infrastructure test project if present; otherwise relevant unit/integration tests |
| EF mappings/repositories/migrations | `Event.Persistence.IntegrationTests` |
| API endpoints/ProblemDetails/HAL/idempotency | `Event.API.IntegrationTests` |
| BFF endpoints/header stripping/antiforgery | `Explore.Blazor.IntegrationTests` |
| Blazor service/components/HAL gating | `Explore.Blazor.Client.Tests` |
| Architecture/docs/rules | `Event.Architecture.Tests` |
| E2E UX | `Explore.Blazor.Client.E2ETests` or Aspire/manual QA when full browser flow is available |

Critical scenarios:

- Non-admin cannot start support access.
- Instance admin cannot start when feature disabled.
- Instance admin cannot start without required ticket/reason.
- Duration is capped.
- One-active-session policy blocks a second active session or idempotently returns the current one, depending on final contract.
- Read-only support session cannot perform write actions.
- Write support session cannot access a different tenant.
- Stopped/expired/revoked session denies.
- Browser-supplied support headers are ignored.
- BFF trusted header is forwarded only for an active owned session.
- HAL links appear/disappear based on support-access authorization.
- Tenant admin can view only their tenant's support audit evidence.
- Mutating support action records audit evidence.
- Kill switch immediately denies new and existing sessions.

Verification commands for the final implementation:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

OpenAPI/client generation if API contract changes:

```bash
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal --no-restore -maxcpucount:1
dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal
```

## 8. Docs, Config, And Ops Updates

Required docs before implementation is complete:

- `docs/API.md`: routes, auth, ProblemDetails, idempotency, rate limits, HAL links.
- `docs/API_CHANGELOG.md`: breaking pre-v1 contract additions/removals.
- `docs/AUTHORIZATION.md`: support-access resource/action model and Cerbos/local parity.
- `docs/SECURITY-MODEL.md`: threat model, BFF boundary, header stripping, audit, privacy redaction.
- `docs/BLAZOR.md`: BFF endpoints, client service pattern, banner/status UX.
- `docs/MULTI_TENANCY.md`: target tenant binding and filter-bypass rules.
- `docs/CONFIGURATION.md`: settings, defaults, environment variables.
- `docs/SELF_HOSTING.md`: operator enablement, defaults, kill switch.
- `docs/OPERATIONS.md`: runbooks, metrics, logs, retention.
- `docs/TESTING.md`: support-access verification matrix.
- `schemas/islamu-event.md`: new tables/indexes/lookups.

## 9. Security, Auth, Privacy, And Abuse Controls

Security model:

- Only authenticated instance admins can request support access.
- Support access is disabled by default.
- API validates session state every request.
- Session state is actor-bound, tenant-bound, mode-bound, and time-bound.
- Write mode is separately disabled by default.
- Cross-tenant mismatch denies and records an audit event.
- Browser never receives support authority or raw privileged headers.
- No session is authorized from a client-supplied header alone.

Privacy:

- Reason/ticket text is stored as audit evidence but must not become a metric label.
- Logs must not include raw request/response bodies.
- Audit metadata must be sanitized and intentionally shaped.
- Tenant-visible audit should show enough to establish trust without leaking unrelated instance-admin data.

Abuse resistance:

- Add rate limits to start/stop/force-stop endpoints.
- Add one-active-session-per-actor default.
- Add force-stop/kill switch.
- Alert on write-mode and denied cross-tenant attempts.
- Require session expiry and no indefinite extension.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, Product

Multi-tenancy:

- The active support session target tenant must match the resolved request tenant.
- Instance admin support access must not weaken tenant filter behavior.
- Cross-tenant audit list is instance-admin only and must use explicit scope parameters.
- Tenant admins can view only their tenant's support-access evidence.

Federation:

- The session actor is the local application user resolved from IdP claims using existing fallback order.
- Do not require provider-specific claims beyond the existing user resolution and admin authority model.
- If future re-auth/MFA is added, use provider-agnostic policy hooks and document provider support differences.

Localization:

- UI copy, validation messages, and ProblemDetails titles should be localizable where the surrounding feature uses localization infrastructure.

Accessibility:

- Start/stop dialogs need labels, validation summaries, focus management, and keyboard paths.
- Active banner must be perceivable without relying on color alone.
- Stop button needs a clear accessible label.
- Audit tables need responsive labels and pagination controls.

Product/trust:

- UX should say "Support access active" / "Viewing tenant with support access", not "you are Alice", unless a future target-user mode is explicitly implemented.
- The active session banner is mandatory for the actor.
- Tenant-visible audit is mandatory for trust.

## 11. Observability

Metrics:

- `explore_support_access_session_started_total{mode,outcome}`
- `explore_support_access_session_stopped_total{reason,outcome}`
- `explore_support_access_authorization_denied_total{reason,mode}`
- `explore_support_access_active_sessions` gauge

Keep labels bounded. Do not label by actor user ID, raw tenant slug, reason text, ticket, request path, or arbitrary resource ID.

Logs:

- Structured event on session start/stop/expire/revoke/deny.
- Include correlation ID/trace ID.
- Include session ID and target tenant ID where safe.
- Do not include tokens, cookies, raw body payloads, or full reason text at high-volume levels.

Traces:

- Attach support session ID and mode to request/activity tags when active.
- Authorization behavior traces should indicate support-access evaluation result.

Alerts:

- Write-mode session started.
- Force-stop invoked.
- Disabled-by-policy access attempt.
- Cross-tenant mismatch.
- Audit persistence failure.

## 12. Migration And Compatibility

Compatibility stance: no backwards compatibility required. This repository is still in development and the old plan has not been implemented.

Migration tasks:

- Add new tables and lookup seed data.
- Add generated migration and update snapshot.
- No data backfill is required unless implementation discovers hidden support-access data.
- If route names or API clients are generated, regenerate clients and update contract docs.

Rollback:

- Migration `Down` drops support-access tables/indexes/lookups in reverse order.
- Feature can be disabled through kill switch/config before rollback.

## 13. Risk Register

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Support access accidentally grants broad tenant admin rights. | Critical | No role grants; provider checks active session, tenant match, mode, action allow-list; parity tests. |
| Browser injects support headers. | Critical | Extend sanitizer and YARP tests; API validates session actor/status/expiry. |
| Audit is missing for mutating actions. | Critical | Treat action audit as part of command transaction or fail closed. |
| Tenant data leaks through cross-tenant audit queries. | High | Explicit scope parameters, tenant admin filtering, integration tests. |
| Session continues after kill switch or expiry. | High | Per-request validation checks settings and expiry; forced stop path; tests. |
| UI shows actions based on roles. | High | HAL-only affordance contract and Blazor tests. |
| Metrics/logs leak PII or unbounded values. | Medium | Bounded labels and sanitized structured metadata. |
| Write-mode support access is overused. | Medium | Disabled by default, shorter TTL, audit alerts. |
| Implementation sprawls across layers. | Medium | Follow phase split and Clean Architecture dependencies. |

## 14. Success Metrics And Definition Of Done

Definition of Done:

- Support access can be enabled/disabled by instance policy.
- Instance admin can start and stop a read-only tenant-scoped support session.
- API validates active support session on every proxied request.
- BFF strips browser support headers and injects only trusted server-owned context.
- HAL affordances reflect support-access permissions.
- Active session banner is visible and stop action works.
- Tenant-visible audit evidence exists.
- Local and Cerbos authorization parity is tested.
- Required docs and configuration references are updated.
- Build and required test projects pass.

Success metrics:

- 100 percent of support session starts/stops recorded in audit.
- 0 tenant-crossing authorization test failures.
- 0 browser authority/token exposure findings.
- Support-access metrics visible in Prometheus.
- Operator runbook can disable or force-stop sessions.

## 15. Implementation Agent Contract

Before editing code, the implementation agent must:

1. Re-read `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `.claude/contract/intents.yaml`, and the matching `.claude/rules/*.md` for files being changed.
2. Re-read this plan, context, and tasks file.
3. Confirm the exact phase and task IDs being implemented.
4. Update `implement-admin-impersonation-context.md` with session notes before making substantial changes.
5. Keep changes inside the phase scope unless a dependency is explicitly discovered and recorded.
6. Run the minimum tests for touched layers.
7. Update docs in the same PR slice when behavior/config/API contracts change.

Do not:

- Add browser-visible support authority claims.
- Replace `ICurrentUserService.UserId` with a target user.
- Create tenant admin role grants for support access.
- Add controller business logic.
- Add ad-hoc repository DTO projections for command-side persistence.
- Bypass tenant filters without explicit bounded reason.
- Show edit/delete UI based on local role checks.

## 16. Progress Reporting Contract

Each implementation session must update `implement-admin-impersonation-context.md` with:

- Date/time in Europe/Brussels.
- Phase/task IDs touched.
- Files changed.
- Decisions made.
- Tests run and outcomes.
- Remaining risks or blockers.

Each completed task in `implement-admin-impersonation-tasks.md` must include:

- Commit/branch reference if available.
- Verification command or test evidence.
- Doc updates completed.

## 17. Potential Risks And Unknowns

Open product decisions:

- Should the first release support only tenant-context support access, or also a true target-user "view as" mode?
- Should write mode require a second approver before first release?
- Should tenant admins receive real-time notification when support access starts, or is audit/history sufficient for the first slice?
- Should support access be enableable per tenant, globally, or both?

Recommended default decisions if the user does not override:

- Implement tenant-context support access first.
- Keep target-user mode out of the first slice.
- Keep write mode disabled by default.
- Add hooks/outbox event for future notification but do not require delivery infrastructure in the first implementation.
- Allow global instance setting first, then per-tenant opt-in/out as a later hardening slice.
