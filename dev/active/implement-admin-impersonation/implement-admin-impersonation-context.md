<!-- ABOUTME: Current working context for the admin support access implementation workstream. -->
<!-- ABOUTME: Tracks source evidence, decisions, progress, blockers, and handoff notes for future sessions. -->

# Admin Support Access / Break-Glass Impersonation Context

Last Updated: 2026-07-04 Europe/Brussels

## Current Session Summary

The old implementation plan was reviewed and rejected as unsafe. It depended on BFF cookie impersonation claims, direct role/claim checks in HAL policies, a non-existent Blazor controller pattern, and the wrong Domain folder convention.

The workstream has been re-baselined around a first-class `SupportAccessSession` model: persisted, actor-bound, tenant-bound, mode-bound, time-bound, audited, and validated by the API on every support-context-bearing request. The BFF remains a token/header boundary and forwards only trusted server-owned support context.

The main implementation slice is now present across Domain, Application, Persistence, Infrastructure, API, BFF, and Blazor Client. It includes support-access lifecycle entities, lookup vocabularies, repository contracts, EF configurations, repository implementations, seeded governance settings, seeded lookup data, an EF migration, CQRS commands/queries/validators/handlers, local/Cerbos authorization vocabulary, HAL assemblers/link policies, API controller routes, trusted BFF forwarding, a global active-session banner, an operator support-access console, tenant-facing support evidence, and per-request audit middleware.

Remaining work is hardening rather than core architecture: full support-access browser flow automation with authenticated fixtures, OpenAPI/generated-client clean-state verification from a clean baseline, and rerunning broad API/Persistence verification after the unrelated failures documented below are fixed.

Current branch: `develop`. No new branch was created during the implementation slice because the worktree already contained broad unrelated in-progress changes.

## Source Evidence Loaded

Repository contract and docs:

- `AGENTS.md`
- `.claude/contract/intents.yaml`
- `.claude/commands/dev-docs.md`
- `dev/active/README.md`
- `docs/QUICK_REFERENCE.md`
- `docs/GOVERNANCE.md`
- `docs/OPERATIONS.md`
- `docs/SECURITY-MODEL.md`
- `docs/AUTHORIZATION.md`
- `docs/ARCHITECTURE.md`
- `docs/API.md`
- `docs/BLAZOR.md`
- `docs/DOMAIN.md`
- `docs/MULTI_TENANCY.md`
- `docs/CODEBASE_STRUCTURE.md`
- `docs/DESIGN_SYSTEM.md`
- `docs/ACCESSIBILITY.md`
- `docs/TESTING.md`
- `docs/CONFIGURATION.md`
- `docs/SELF_HOSTING.md`

Rules loaded:

- `.claude/rules/api-controllers.md`
- `.claude/rules/api-hateoas.md`
- `.claude/rules/application-layer.md`
- `.claude/rules/domain.md`
- `.claude/rules/efcore-persistence.md`
- `.claude/rules/efcore-migrations.md`
- `.claude/rules/blazor-server.md`
- `.claude/rules/blazor-client.md`
- `.claude/rules/tests.md`

Skills loaded:

- `senior-cto-feedback`
- `auth-patterns`
- `blazor-bff-patterns`
- `clean-architecture-rules`
- `cqrs-mediatr-guidelines`
- `dotnet-efcore-guidelines`
- `blazor-ui-conventions`
- `error-tracking`

Source files inspected:

- `Explore.Application/Contracts/Identity/IAdminContext.cs`
- `Explore.Infrastructure/Identity/AdminContext.cs`
- `Explore.Blazor/Services/BffAdminClaimsTransformation.cs`
- `Explore.API/Controllers/UserController.cs`
- `Explore.Blazor/Extensions/BffEndpointExtensions.cs`
- `Explore.Blazor/Extensions/BffAuthEndpoints.cs`
- `Explore.Blazor/Extensions/YarpProxyExtensions.cs`
- `Explore.Blazor/Services/BffProxyHeaderSanitizer.cs`
- `Explore.Blazor/Extensions/HttpClientExtensions.cs`
- `Explore.Application/Behaviors/AuthorizationBehavior.cs`
- `Explore.Domain/AuditLog.cs`
- `Explore.Persistence/Configurations/Entities/AuditLogConfiguration.cs`
- `Explore.Domain/ConfigurationChangeLog.cs`
- `Explore.Persistence/Configurations/Entities/ConfigurationChangeLogConfiguration.cs`
- `Explore.Application/Contracts/Persistence/IUnitOfWork.cs`
- `Explore.Persistence/EfCoreUnitOfWork.cs`
- `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs`

Research loaded:

- Tavily research on enterprise SaaS support access, JIT privileged access, break-glass controls, immutable audit, tenant isolation, and operator kill switches.
- Tavily follow-up research on support-access session expiry, visible operator/session context, ticket/reason capture, privileged delegated access, and immutable audit requirements.
- Context7 ASP.NET Core docs for `RequireAuthorization`, antiforgery on unsafe minimal API/BFF endpoints, cookie auth, and claims transformation patterns.
- Context7 YARP docs for request transforms that remove inbound headers and add trusted server-side forwarding headers before proxy dispatch.
- Context7 EF Core docs for concurrency tokens, global tenant query filters, migrations/indexes, and audit persistence patterns.
- Context7 MudBlazor docs for dialogs, forms/validation, action buttons, tables, and UI primitives.

## Current Decisions

Decision 1: Use `SupportAccessSession` language in code and UX.

Reason: "Impersonation" implies becoming another user. The safer enterprise model is support access with explicit actor and effective tenant context.

Decision 2: Preserve actor identity.

Reason: `ICurrentUserService.UserId` must remain the real authenticated instance admin. Support context is separate metadata for authorization and audit.

Decision 3: Persist sessions and validate per request.

Reason: persisted sessions support expiry, stop, force-stop, kill switch, audit review, and concurrency. Cookie claims alone are stale and over-trusted.

Decision 4: BFF forwards trusted support context.

Reason: existing BFF/YARP already strips browser credentials and re-adds server-owned auth, tenant, and setup-secret context. Support access should extend that boundary.

Decision 5: Authorization and HAL remain centralized.

Reason: UI affordances must come from `_links`; API policies must go through the local/Cerbos provider path and fail closed.

Decision 6: Read-only first, write mode gated.

Reason: external best practice favors least privilege, JIT, short-lived sessions, ticket/reason capture, and stricter controls for write-capable elevation.

## Critical Constraints To Preserve

- Do not add browser-visible support authority claims.
- Do not add durable `TenantUserRoleGrant` rows for support access.
- Do not replace the real current user with the target tenant user.
- Do not authorize tenant resource actions from client-supplied headers.
- Do not put business logic in controllers.
- Do not bypass tenant filters except for explicit, bounded support-session lookup/audit queries.
- Do not show Blazor action buttons from local role/claim checks.
- Do not log raw tokens, cookies, request/response bodies, reason text as labels, or unbounded ticket text in metrics.

## Quick Resume

Implementation progress recorded on 2026-07-04 Europe/Brussels:

- Added `SupportAccessSession` and `SupportAccessAuditEvent` in Domain with explicit lifecycle transitions, bounded text validation, JSON metadata validation, optimistic concurrency, and no `ITenantEntity` implementation.
- Added support-access lookup enums/entities for status, mode, end reason, and audit event type.
- Added Application contracts for trusted support context, session orchestration, session persistence, audit persistence, and canonical trusted header names.
- Added support-access authorization vocabulary through `ResourceKinds.SupportAccessSession` and `AuthorizationActions.SupportAccessSessions`.
- Added instance governance keys under `support_access.*`, registered fail-closed setting definitions, and seeded locked system settings.
- Added EF mappings, DbSets, repository implementations, DI registration, lookup seeding, and migration `20260703231445_AddSupportAccessSessions`.
- Added Domain lifecycle tests and Persistence integration tests for actor/tenant predicates, audit listing, and the database one-active-session invariant.
- Added support-access DTOs, mapper, CQRS commands/queries, manual validators, and handlers for start, stop, force-stop, current-session, session history, and audit history.
- Added `SupportAccessSessionService` in Infrastructure. Runtime support context is explicit-header-only: without the BFF/server-injected `X-Support-Access-Session-Id`, API requests remain inactive even if a persisted actor session exists.
- Added `SupportAccessController` under `/api/support-access` with HAL resources for session lifecycle/history and bounded ProblemDetails behavior.
- Added support-access HAL policy/assembler registration, OpenAPI schema registration, route names, Cerbos policy/schema/tests, and local fallback authorization handling.
- Added `BffSupportAccessSessionStore`, `SupportAccessForwardingHandler`, YARP forwarding, and `BffSupportAccessEndpoints`. The BFF stores only an opaque active-session reference keyed to authenticated user plus OIDC session id, strips browser-supplied support headers, and injects trusted support context only from the server-side store.
- Hardened BFF support header stripping to remove the entire `X-Support-Access-*` namespace, including future prefixed headers, before injecting the single trusted `X-Support-Access-Session-Id` value from an active owned store entry.
- Added `Event.API.IntegrationTests/Features/SupportAccessApiTests.cs` with focused API integration coverage for the support-access controller. The tests drive the real HTTP pipeline, MediatR handlers, EF in-memory persistence, HAL assemblers, and ProblemDetails mapping for disabled feature, required ticket, read duration cap, write-mode denial, one-active-session blocking, current-session discovery, owned stop, force-stop, and lifecycle audit history.
- Added local/Cerbos parity hardening for support-access authorization decisions. `FallbackAuthorizationServiceTests` now covers the lifecycle/evidence action matrix for instance admins, tenant admins, other-tenant admins, regular users, and batch HAL-style checks; `SupportAccessSessionServiceTests` covers explicit forwarded-header activation, disabled support-access fail-closed behavior, read-only vs write-mode `AllowsWrites`, and resolved-tenant mismatch; `CerbosPolicyContractTests` and `CerbosPolicyCompilationTests` now exercise `islamuevent_support_access_session` through the container-backed PDP.
- Added provider-layer support-access boundary enforcement in `RuntimeAuthorizationProvider`. The runtime provider now consumes `ISupportAccessSessionService`, annotates provider checks with bounded support metadata, denies inactive forwarded support sessions for tenant-scoped resources, denies read-only mutation checks, and denies write-mode checks whose resource tenant differs from the session target tenant before local RBAC, instance Cerbos, BYO Cerbos, or HAL batch evaluation runs. `SupportAccessSessionService` now distinguishes an invalid/stopped/expired/revoked/disabled forwarded session from no forwarded session via `ISupportAccessContext.WasForwarded`.
- Added `SupportAccessClientService` and `SupportAccessBanner` in the Blazor client shell. The banner reads BFF-confirmed current-session state, shows mode/tenant/expiry, and stops through the BFF endpoint; it does not inspect local roles or claims.
- Added `SupportAccessAuditMiddleware` after authorization in the API pipeline. It records bounded `RequestObserved` and `CommandCommitted` events for active support sessions without logging payloads or changing response behavior if audit persistence fails.
- Added support-access observability across the backend. Lifecycle handlers now emit bounded `Explore.Business` metrics and structured logs for start/stop/expire/force-stop outcomes; `SupportAccessSessionService` emits session-validation denial signals for kill-switch and write-mode shutdown; `RuntimeAuthorizationProvider` emits boundary-denial metrics, warning logs, and trace events for read-only/cross-tenant/inactive support checks; `SupportAccessAuditMiddleware` emits request-audit persistence metrics and trace tags. Force-stop audit attribution now records the revoking operator as the audit actor.
- Added BFF support-access history, audit, and force-stop endpoints. Unsafe force-stop validates antiforgery, forwards only through the server-side BFF client, and clears the BFF active-session store when the revoked session is the current cached session.
- Added Blazor client support-access resource models that preserve API HAL links and expose `CanStart`, `CanStop`, `CanForceStop`, and `CanViewAudit` only from `_links`, never local role or claim checks.
- Added `SupportAccessConsoleSection` under instance admin settings. The console loads tenants, starts bounded read/write support sessions with duration/reason/ticket metadata, lists session history, opens audit events per session, and force-stops sessions through a confirmation dialog.
- Added focused client and BFF tests for the operator-console slice: client tests prove HAL affordance preservation/removal and force-stop payload routing; BFF tests prove history forwarding, antiforgery fail-closed force-stop behavior, and current-session store clearing after force-stop.
- Added tenant-facing support evidence under tenant administration. The view is read-only, current-tenant scoped, and renders audit drill-in only from the session `audit-events` HAL link.
- Updated `DESIGN.md`, `docs/CONFIGURATION.md`, `docs/AUTHORIZATION.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, `docs/SELF_HOSTING.md`, `docs/MULTI_TENANCY.md`, `docs/TESTING.md`, and `schemas/islamu-event.md`.

Next recommended implementation step:

1. Add authenticated E2E/browser coverage for the active support-access banner, tenant evidence view, and full operator console start/history/audit/force-stop flow.
2. Re-run full API/Persistence integration suites after the unrelated auth/repository failures are fixed, then verify OpenAPI/NSwag clean-state from a clean generated-contract baseline.

## Verification Status

This session added provider-layer support-access boundary enforcement for handler and HAL authorization batches, hardened terminal-session context validation, implemented backend support-access observability, added the operator support-access console and BFF history/audit/force-stop endpoints, and confirmed focused runtime-provider/support-session/API/metrics/client/BFF tests pass.

Verification run after operator console/BFF endpoint implementation:

- `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed on rerun. 8 projects, 0 errors, pre-existing warnings. The first attempt caught unrelated concurrent `ExternalApiKeyRepository` edits mid-transition; `Explore.Persistence` passed immediately afterward with 4 projects and 0 errors.
- `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 5 projects, 0 errors, pre-existing warnings.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessClientServiceTests/*" --minimum-expected-tests 1`
  - Result: passed. 3 total, 3 succeeded.
- `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 9 projects, 0 errors, pre-existing warnings.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/BffSupportAccessEndpointsTests/*" --minimum-expected-tests 1`
  - Result: passed. 3 total, 3 succeeded.
- `git diff --check -- Explore.Blazor/Extensions/BffSupportAccessEndpoints.cs Explore.Blazor/Extensions/BffEndpointExtensions.cs Explore.Blazor.Client/Contracts/Services/SupportAccess/ISupportAccessClientService.cs Explore.Blazor.Client/Contracts/Services/SupportAccess/SupportAccessResourceModels.cs Explore.Blazor.Client/Services/SupportAccessClientService.cs Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor Explore.Blazor.Client/Pages/Admin/Instance/Components/SupportAccessConsoleSection.razor Explore.Blazor.Client/Pages/Admin/Instance/Components/SupportAccessConsoleSection.razor.css Explore.Blazor.Client.Tests/Services/SupportAccessClientServiceTests.cs Explore.Blazor.IntegrationTests/Endpoints/BffSupportAccessEndpointsTests.cs dev/active/implement-admin-impersonation/implement-admin-impersonation-context.md dev/active/implement-admin-impersonation/implement-admin-impersonation-plan.md dev/active/implement-admin-impersonation/implement-admin-impersonation-tasks.md`
  - Result: passed.

Verification run after support-access observability hardening:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 2 projects, 0 errors, pre-existing warnings.
- `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 3 projects, 0 errors, pre-existing warnings.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 7 projects, 0 errors, pre-existing warnings.
- `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 3 projects, 0 errors, pre-existing warnings.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 8 projects, 0 errors, pre-existing warnings.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/BusinessMetricsSupportAccessTests/*" --minimum-expected-tests 1`
  - Result: passed. 2 total, 2 succeeded.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessApiTests/*" --minimum-expected-tests 1`
  - Result: passed. 8 total, 8 succeeded.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessSessionServiceTests/*" --minimum-expected-tests 1`
  - Result: passed. 10 total, 10 succeeded.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/RuntimeAuthorizationProviderTests/*" --minimum-expected-tests 1`
  - Result: passed. 21 total, 21 succeeded.

Verification run after provider-layer support-access boundary hardening:

- `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 4 projects, 0 errors, pre-existing warnings.
- `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore --no-dependencies -clp:ErrorsOnly`
  - Result: passed after focused terminal-session test additions. Changed test project compiled, 0 errors, pre-existing warnings.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/RuntimeAuthorizationProviderTests/*" --minimum-expected-tests 1`
  - Result: passed. 21 total, 21 succeeded.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessSessionServiceTests/*" --minimum-expected-tests 1`
  - Result: passed. 10 total, 10 succeeded.
- `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result on rerun after unrelated worktree drift was initially blocked by dirty `UserAuthenticationToken` changes, then passed after the worktree's concurrent alignment. This support-access slice did not modify those files.

Previous session added local fallback, support-session context, and Cerbos PDP contract coverage for support-access authorization parity, hardened the workstream evidence, and confirmed focused provider/policy tests pass.

Verification run after local/Cerbos parity hardening:

- `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 4 projects, 0 errors, pre-existing warnings.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 8 projects, 0 errors, pre-existing warnings.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessSessionServiceTests/*|/*/*/FallbackAuthorizationServiceTests/*SupportAccessSession*" --minimum-expected-tests 1`
  - Result: passed. 7 total, 7 succeeded.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/CerbosPolicyContractTests/*" --minimum-expected-tests 1`
  - Result: passed. 103 total, 103 succeeded.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/CerbosPolicyCompilationTests/*" --minimum-expected-tests 1`
  - Result: passed. 40 total, 40 succeeded.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - Result: passed. 240 total, 239 succeeded, 1 skipped pre-existing API response metadata test.
- `git diff --check`
  - Result: passed.
- `dotnet build --configuration Release --verbosity quiet`
  - Result: passed. 25 projects, 0 errors, pre-existing warnings.

Tavily MCP was attempted for this slice but both research and search endpoints returned plan-limit errors. Context7 Cerbos documentation was loaded for resource policies, schemas, and policy-test conventions; ordinary web search was used as a fallback for enterprise support-access/audit best-practice context.

Previous session added focused API integration/contract coverage for support-access controller behavior, hardened the workstream evidence, and confirmed architecture invariants still pass.

Verification run after API contract hardening:

- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 8 projects, 0 errors, pre-existing package/analyzer warnings.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessApiTests/*"`
  - Result: passed. 8 total, 8 succeeded.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - Result: passed. 240 total, 239 succeeded, 1 skipped pre-existing API response metadata test.
- `dotnet build --configuration Release --verbosity quiet`
  - Result: passed. 25 projects, 0 errors, pre-existing package/analyzer warnings.

This slice also used Context7 to confirm current TUnit test-application filtering syntax. TUnit uses `--treenode-filter "/*/*/ClassName/*"` for class-scoped runs; VSTest-style `--filter` is not supported by this project runner.

Previous session hardened BFF support-access header stripping/forwarding and updated the active workstream docs.

Verification run on 2026-07-04 Europe/Brussels:

- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextSchemaTests`
  - Result: failed before test execution because the TUnit test application does not accept `--filter`; zero tests ran.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - Result: passed. 240 total, 239 succeeded, 1 skipped pre-existing API response metadata test. Rerun after final context update also passed with the same result.
- `dotnet build --configuration Release --verbosity quiet`
  - Result: passed. 25 projects, 0 errors, 8101 pre-existing warnings.

Verification run after the foundational implementation slice:

- `dotnet build Explore.Domain/Explore.Domain.csproj --configuration Release --verbosity quiet`
  - Result: passed.
- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`
  - Result: passed with an existing package warning.
- `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet`
  - Result: passed with pre-existing/generated warnings.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`
  - Result: passed with package warnings.
- `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
  - Result: passed, 313 total.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - Result: passed, 1857 total.
- `Event.Persistence.IntegrationTests/bin/Release/net10.0/Event.Persistence.IntegrationTests --no-ansi --disable-logo --maximum-failed-tests 2`
  - Result: 200 passed, 1 failed. The new support-access repository tests passed; the failing test is the unrelated pre-existing `EventQuerySpecificationTests.PublicDiscoveryScheduleFilter_ExcludesEventsWhosePublishedSessionsHaveEnded`.
- `dotnet build --configuration Release --verbosity quiet`
  - Result: blocked by unrelated dirty work in `Event.API.IntegrationTests/Features/WebhooksControllerTests.cs`, which is missing required `WebhookEndpointDto.ProviderModeName`.

Verification run after the runtime/BFF/UI/audit implementation slice:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`
  - Result: passed with an existing package advisory warning.
- `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet`
  - Result: passed.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`
  - Result: passed with pre-existing/generated warnings.
- `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet`
  - Result: passed with pre-existing/generated warnings.
- `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet`
  - Result: passed on rerun. The first parallel attempt hit a transient PDB file lock while the Blazor host build was active.
- `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet --no-restore`
  - Result: passed after the Blazor client convention fix that moved `SupportAccessCommandResult` out of the pure interface file and switched support-access commands to `IBffClient.SendAsync`.
- `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
  - Result: passed, 313 total.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - Result: passed, 1860 total.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - Result: passed, 240 total, 239 succeeded, 1 skipped pre-existing API metadata test.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - Result: failed at compile time due unrelated dirty test code: `Explore.Blazor.Client.Tests/Components/Moderation/ModerationReportDetailPanelTests.cs(152,21): error CS0117: 'Signals' does not contain a definition for 'CorrelationId'`.
- Headless Chrome visual smoke at 375, 768, and 1280 pixel widths against `Explore.Blazor`
  - Result: setup route rendered coherently and the support banner stayed hidden without an authenticated active support session. Evidence screenshots are under `dev/active/implement-admin-impersonation/evidence/visual-qa/`. Active-session banner interaction still needs an authenticated API-backed browser test.

Verification run after BFF trust-boundary hardening:

- `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly`
  - Result: passed. 9 projects, 0 errors, 979 pre-existing warnings.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/SupportAccessForwardingHandlerTests/*" --minimum-expected-tests 1`
  - Result: passed. 4 total, 4 succeeded.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffProxyHeaderSanitizerTests/*" --minimum-expected-tests 1`
  - Result: passed. 2 total, 2 succeeded.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - Result: passed. 240 total, 239 succeeded, 1 skipped pre-existing API metadata test.
- `git diff --check -- Explore.Application/Constants/SupportAccessHeaderNames.cs Explore.Blazor/Services/BffProxyHeaderSanitizer.cs Explore.Blazor/Services/SupportAccessForwardingHandler.cs Explore.Blazor.IntegrationTests/Services/BffProxyHeaderSanitizerTests.cs Explore.Blazor.IntegrationTests/Handlers/SupportAccessForwardingHandlerTests.cs dev/active/implement-admin-impersonation/implement-admin-impersonation-tasks.md dev/active/implement-admin-impersonation/implement-admin-impersonation-context.md`
  - Result: passed.

Tenant-facing evidence implementation update, 2026-07-04 Europe/Brussels:

- Phase/tasks touched: Phase 5 tenant-facing support-access evidence view, Phase 6 support-access docs, Phase 7 focused Blazor client verification.
- Files changed: `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantSupportAccessEvidenceSection.razor`, `TenantSupportAccessEvidenceSection.razor.css`, `TenantAdminSettingsLayout.razor`, `Explore.Blazor.Client.Tests/Pages/Admin/TenantSupportAccessEvidenceSectionTests.cs`, `docs/BLAZOR.md`, `docs/MULTI_TENANCY.md`, `docs/SELF_HOSTING.md`, `docs/SECURITY-MODEL.md`, `docs/TESTING.md`, `schemas/islamu-event.md`, and this active workstream ledger.
- Decision: the first release does require tenant-visible evidence because the plan Definition of Done and trust model both require it. The tenant view is read-only and intentionally omits start/force-stop controls; audit drill-in renders only from the `audit-events` HAL link on each support session resource.
- Research/tooling: Context7 was used for current MudBlazor and ASP.NET Core Blazor guidance. Tavily MCP was attempted for enterprise support-access research, but the MCP account returned usage-limit error `432`; implementation proceeded from the already-approved plan, repo conventions, and official docs.
- Verification:
  - `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 5 projects, 0 errors, pre-existing warnings.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/TenantSupportAccessEvidenceSectionTests/*"` passed: 3 tests.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/SupportAccessClientServiceTests/*" --minimum-expected-tests 1` passed: 3 tests.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet` passed: 240 total, 239 succeeded, 1 skipped pre-existing API metadata test.
  - `git diff --check` passed for the touched tenant evidence, docs, and workstream files.
  - `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 8 projects, 0 errors, pre-existing warnings.
  - Standalone Blazor host started on `http://127.0.0.1:5107`, but app routes including `/admin/tenant/settings` returned 404 while the API/onboarding/auth runtime was unavailable. Authenticated visual QA for the tenant evidence tab remains pending.
- Full `Explore.Blazor.Client.Tests` and `Explore.Blazor.IntegrationTests` are now green after the tenant evidence slice. Full `Event.API.IntegrationTests` and `Event.Persistence.IntegrationTests` were run and are not green because of unrelated failures outside the support-access focused coverage. API result: 1583 total, 1555 succeeded, 25 failed, 3 skipped; representative failures are in MCP auth, AI assistant disabled flow, storage-object HATEOAS anonymous reads, endpoint authorization matrix authenticated requests, and security JWT audience tests. Persistence result: 215 total, 214 succeeded, 1 failed; failing test is `EventRegistrationRepositoryTests.GetRegistrationsByEventWithDetailsPaged_ReturnsOnlyRequestedEventRows` because `items[0].User?.Pii` was unexpectedly null.
- OpenAPI/NSwag clean-state remains unverified because `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, and `docs/API_CONTRACT_INVENTORY.md` were already dirty before the clean-state step. Authenticated browser support-access flow and visual QA for the new authenticated tenant evidence tab still need a running API/onboarding/auth scenario.

The documented filtered command remains here for traceability, but future sessions should use the repo-supported TUnit filtering syntax or run the whole architecture project:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextSchemaTests
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AgentContextLinkTests
dotnet build --configuration Release --verbosity quiet
```

## Open Product Questions

- Should v1 include true target-user "view as" mode, or only tenant-context support access?
- Should write mode require second-person approval before first release?
- Should support access be configurable globally only, or both globally and per tenant?
- Should tenant admins receive immediate notifications in v1, or is audit/history enough for the first implementation slice?

Recommended defaults:

- Tenant-context support access only for v1.
- Write mode disabled by default.
- Global instance setting first.
- Add outbox hooks for future notifications, but do not block v1 on delivery channels.
