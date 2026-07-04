<!-- ABOUTME: Resume context for the Event Instance Console and multi-tenant control-plane implementation plan. -->
<!-- ABOUTME: Captures current progress, key files, decisions, constraints, validation, risks, and handoff notes. -->

# Event Instance Console And Multi-Tenant Control Plane - Context

Last Updated: 2026-07-05 Europe/Brussels

## SESSION PROGRESS (2026-07-04 Europe/Brussels)

### Completed

- Created initial dev-docs planning set for `multi-tenant-control-plane`.
- Read `.claude/commands/dev-docs.md` and matched the required plan/context/tasks structure.
- Loaded the repository contract from `AGENTS.md`, intent registry, quick reference, governance docs, path rules, and relevant skills.
- Investigated current deployment-mode, multi-tenancy, BFF, admin settings, and onboarding implementation.
- Verified during initial planning that no `Explore.ControlPlane.*`, `Event.ControlPlane.Client`, or `Event.ControlPlane.Blazor` project existed; current dirty-worktree re-baseline separately found only an in-progress `Event.Web.BffHosting` candidate.
- Ran baseline build before writing the plan: `dotnet build --configuration Release --verbosity quiet` passed with 25 projects, 0 errors, and existing warnings.
- Post-doc whitespace and required-marker checks passed for the three new files.
- Applied Senior CTO feedback from user review: new planned control-plane projects now use `Event.*` names, and the separate control-plane app now has an explicit Keycloak OIDC confidential-client BFF security contract.
- Re-ran `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`; it passed with 239 succeeded and 1 intentionally skipped test.
- Applied latest CTO feedback: `Event.Web.BffHosting` is now a required shared BFF security/hosting foundation before `Event.ControlPlane.Blazor`, and future app projects remain out of scope.
- Re-baselined the planning docs against the current dirty worktree: an untracked `Event.Web.BffHosting/` candidate, modified `Explore.Blazor` BFF files, and an untracked BFF architecture-test candidate already exist and must be audited before Phase 1 can be marked complete.
- Reconciled plan/task phase numbering after inserting the shared BFF hosting phase; task checklist now reports 8 completed out of 70 total checklist items.
- Re-ran doc hygiene checks for this update: stale old-direction/future-project scan clean and trailing-whitespace scan clean. `Event.Architecture.Tests` passed earlier in the planning update with 239 succeeded and 1 intentionally skipped test; rerun it after accepting project/context-rule changes.
- Accepted the current `Event.Web.BffHosting` proxy/header foundation after repair: added the missing `Microsoft.Extensions.Hosting` import for `BffDevelopmentHostPolicy`, confirmed `Event.Web.BffHosting` builds, confirmed `Explore.Blazor` builds while consuming it, and confirmed `Event.Architecture.Tests` plus `Explore.Blazor.IntegrationTests` pass.
- `Event.Web.BffHosting` now owns shared YARP `/api/*` route/cluster construction, API base-address resolution, development TLS trust policy, privileged-header sanitization, token safety, and neutral adapter contracts for access-token, tenant, setup-secret, and support-access forwarding.
- `Explore.Blazor` now consumes the shared proxy foundation through `AddEventBffHosting(..., EventBffHostProfile.PublicWeb)` and `AddEventApiProxy(...)`, with host-specific adapter implementations in `Explore.Blazor/Services/EventBffHostingAdapters.cs`.
- Completed Phase 1 Task 1.2 auth extraction: `Event.Web.BffHosting` now owns shared safe auth diagnostics, provider-neutral Keycloak/Google OIDC option construction, token refresh cookie events, the OIDC scheme cookie key, and a named `HttpClientFactory` token-refresh backchannel.
- `Explore.Blazor` now consumes the shared auth primitives through `EventBffTokenRefreshCookieEvents`, `IEventBffOidcOptionsFactory`, and `ISafeAuthDiagnosticsPolicy`; host-specific dynamic provider orchestration remains in `DynamicAuthSchemeManager`, and host-specific admin-claim enrichment/circuit cleanup/setup redirects moved into `ExploreBffCookieSessionHandler`.
- Deleted the old `Explore.Blazor/Services/SafeAuthDiagnosticsPolicy.cs` and `Explore.Blazor/Services/TokenRefreshCookieEvents.cs` implementations after moving the reusable pieces into `Event.Web.BffHosting`.
- Focused validation for Phase 1.2 passed: `Event.Web.BffHosting` Release build passed with 0 warnings; `Explore.Blazor` Release build passed; safe auth diagnostics tests passed 2/2; BFF proxy header sanitizer tests passed 2/2; `EventWebBffHostingArchitectureTests` passed 3/3.
- Completed Phase 2 Tasks 2.1 and 2.2: created `Event.ControlPlane.Client` as a host-neutral .NET 10 Razor class library, added it to `Explore.sln` under the UI solution folder, added central package management for `Microsoft.AspNetCore.Components.Web`, and committed the new project lock file.
- `Event.ControlPlane.Client` now exposes `ControlPlaneClientAssembly.Value`, canonical `/admin/instance/*` route constants, stable route keys, a route catalog, and `AddEventControlPlaneClient(...)` for embedded and separate Blazor hosts.
- Added `Event.Architecture.Tests/EventControlPlaneClientArchitectureTests.cs`, covering RCL project shape, no forbidden project references/tokens, route-root composition under `/admin/instance`, and shared DI registration.
- Focused validation for Phase 2.1/2.2 passed: `Event.ControlPlane.Client` Release build passed with 0 warnings; filtered `EventControlPlaneClientArchitectureTests` passed 4/4. `Event.Architecture.Tests` build passed but emitted existing package warnings.
- Queried Context7 official ASP.NET Core docs for current Blazor/RCL guidance. The selected source was `/dotnet/aspnetcore.docs`; the retrieved guidance confirmed RCL sharing, host route discovery through additional assemblies, and `_content/{packageId}` static asset serving.
- Completed Phase 2 Task 2.3: added host-neutral control-plane contracts for HAL links/resources, link relation constants, result/problem states, command outcomes, overview status cards/warnings, tenant lists, domain lists, and overview/tenant/domain service interfaces.
- `AddEventControlPlaneClient(...)` now registers fail-closed default overview, tenant, and domain services through `TryAddScoped`, so hosts can override them with real API adapters while shared components remain free of generated clients and token storage.
- Expanded `EventControlPlaneClientArchitectureTests` to block generated-client/raw HTTP/local-auth coupling in `Event.ControlPlane.Client`, and to require HAL/failure-state/service contracts.
- Focused validation for Phase 2.3 passed: `Event.ControlPlane.Client` Release build passed with 0 warnings; filtered `EventControlPlaneClientArchitectureTests` passed 5/5. `Event.Architecture.Tests` build passed but emitted existing package warnings.
- Added the first separate `Event.ControlPlane.Blazor` host foundation: `Microsoft.NET.Sdk.Web` project, `event-shared-secrets` user-secrets ID, `Program.cs`, protected Razor shell, minimal standalone host CSS, fail-closed overview placeholder, `appsettings.json`, `appsettings.Development.json`, Dockerfile, and solution entry.
- Extended `Event.Web.BffHosting` with reusable Keycloak OIDC/cookie auth registration, control-plane coarse authorization policy, shared auth endpoints, forwarded-header/security-header/antiforgery token middleware, and default Keycloak client/config profile helpers.
- Added control-plane Infisical compatibility mapping in `Event.ControlPlane.Blazor/Extensions/ConfigurationExtensions.cs`. It maps dedicated control-plane keys such as `KEYCLOAK_CONTROL_PLANE_CLIENT_ID`, `KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET`, `KEYCLOAK_CONTROL_PLANE_AUTHORITY`, and `CONTROL_PLANE_API_ENDPOINT` into the BFF runtime config.
- Added `Event.ControlPlane.Blazor/Dockerfile` using the same multi-stage runtime-secret posture as the public Blazor image: no Infisical CLI, no baked secrets, runtime env-var and Infisical bootstrap support, locked restore inputs, and `Explore.Secrets` runtime loading.
- Updated Keycloak seed/sync files for the dedicated control-plane confidential client: `docker/keycloak/realm-export.json`, `docker/keycloak/ISLAMU-realm.test.json`, and `docker/keycloak/keycloak-init.sh`. The init script now syncs `KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET` when supplied and leaves the optional client unchanged when the separate app is not enabled yet.
- Added `Event.Architecture.Tests/EventControlPlaneBlazorArchitectureTests.cs`, now covering project references, shared BFF/client/secret usage, user-secrets ID, Dockerfile secret posture, no public Blazor shell coupling, and Keycloak realm/init coverage.
- Focused validation for the separate-host/Keycloak slice passed: `Event.Web.BffHosting` Release build passed with 0 warnings; `Event.ControlPlane.Blazor` Release build passed with existing transitive warnings; `jq empty` passed for both Keycloak JSON files; `bash -n docker/keycloak/keycloak-init.sh` passed; focused `EventControlPlaneBlazorArchitectureTests` passed 5/5; focused `EventWebBffHostingArchitectureTests` passed 3/3.
- Completed Phase 7 deployment wiring for the separate control-plane BFF: `docker-compose.yml` now exposes an optional `control-plane` profile/service, `.env.example` documents `CONTROL_PLANE_*` and `KEYCLOAK_CONTROL_PLANE_*`, `Explore.AppHost` now registers the `event-control-plane` resource, and self-hosting/config/secrets/security/operations/troubleshooting docs describe the dedicated client and secret boundary.
- Hardened the separate `Event.ControlPlane.Blazor` host to be Interactive Server-only: `Program.cs` maps only server interactivity, `Components/App.razor` applies `@rendermode="InteractiveServer"` to `HeadOutlet` and `Routes`, host imports expose `RenderMode`, and architecture tests now block InteractiveAuto/WebAssembly control-plane hosting while keeping `Event.ControlPlane.Client` render-mode neutral.
- Applied the latest render-mode clarification: the existing ISLAMU Event Blazor host (`Explore.Blazor` / `Explore.Blazor.Client`) may keep its current render-policy customization for public/community UX, but that configurability must not be copied into `Event.ControlPlane.Blazor`. The separate control-plane host remains Interactive Server-only by design.
- Re-queried Context7 official ASP.NET Core Blazor docs for the render-mode boundary. The implementation contract is now explicit: `Event.ControlPlane.Blazor` registers `AddInteractiveServerComponents()`, maps `AddInteractiveServerRenderMode()`, and uses `@rendermode="InteractiveServer"` for its root document only; it must not add WebAssembly/Auto render-mode APIs or a render-policy setting. `Event.ControlPlane.Client` remains a reusable RCL discovered by hosts through additional assemblies/static assets without forcing a render mode.
- Added a deployment architecture guard to `EventControlPlaneBlazorArchitectureTests` so Compose, `.env.example`, and Aspire must keep the optional self-hostable control-plane service aligned.
- Fixed a small existing BFF compile issue in `Explore.Blazor/Services/BffCookieForwardingHandler.cs`: the slash prefix check now uses `StartsWith("/", StringComparison.Ordinal)` rather than the invalid char overload.
- Completed Phase 2 Tasks 2.4 and 2.5: `Event.ControlPlane.Client` now references MudBlazor directly and exposes local `ControlPlaneActionButton`, `ControlPlanePageHeader`, and `ControlPlanePanel` primitives with colocated BEM/CSS-isolation files. This avoids `Explore.Blazor.Client` wrapper coupling while preserving Event design-token conventions.
- Expanded `EventControlPlaneClientArchitectureTests` to guard the design-system decision: no public `App*` wrapper coupling, local CSS isolation pairs required, CSS must use the `control-plane-` namespace, bare MudBlazor selectors are blocked, and physical direction CSS tokens are rejected.
- Wired `Event.ControlPlane.Blazor` for the MudBlazor primitives used by the shared RCL: direct package reference, `AddMudServices`, MudBlazor CSS/JS assets, and theme/popover/dialog/snackbar providers in the operator layout. `EventControlPlaneBlazorArchitectureTests` now guards this host readiness.
- Completed Phase 3 Task 3.1 API/Application inventory. Re-read `AGENTS.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, `docs/API.md`, `docs/ARCHITECTURE.md`, `docs/AUTHORIZATION.md`, `.claude/rules/api-controllers.md`, `.claude/rules/application-layer.md`, and `.claude/rules/api-hateoas.md`; queried Context7 `/dotnet/aspnetcore.docs` for current ASP.NET Core controller metadata guidance; attempted a broad CodeGraph survey, which timed out, then used targeted controller/handler/HAL/admin-context reads.
- Phase 3.1 conclusion: existing instance settings, onboarding preflight, storage, email dispatch, resolver/domain settings, auth provider, authz provider, localization, support access, and managed-provider provisioning endpoints are reusable source material; only storage and email dispatch already look close to control-plane-grade HAL/operator surfaces. Existing generic tenant CRUD is not sufficient as the final multi-tenant control-plane tenant lifecycle surface because it lacks dedicated multi-tenant-only route guards, dedicated control-plane read models, lifecycle-specific suspend/archive/purge operations, and a bounded operator overview.
- Completed Phase 3 Task 3.2: added the first dedicated control-plane API contract at `GET /api/admin/control-plane/overview`, guarded by `[RequireMultiTenant]`, `[Authorize]`, authenticated rate limiting, request timeouts, and a MediatR `ISecureRequest` query authorized as `ResourceKinds.InstanceSetting` / `AuthorizationActions.InstanceSettings.View`.
- Added `ControlPlaneOverviewDto` plus tenant status counts, provider summaries, and warnings. The handler reads deployment mode, tenant status totals, public/admin host configuration, auth provider status, authorization provider status, storage status, SMTP status, and instance governance state without returning secrets.
- Added HAL support for the overview through `ControlPlaneOverviewResourceAssembler` and `ControlPlaneOverviewLinkPolicy`. Links are limited to instance-setting navigation/action affordances such as `self`, `domains`, `storage`, `authentication`, and `authorization`, and each link carries instance-setting permission metadata so future UI keeps using `_links` instead of local role/claim checks.
- Registered the new overview resource in HATEOAS assembler registration, source-generated JSON context, route names, and OpenAPI HAL schema catalog. This also updated NSwag-generated Blazor client link anonymous type numbering, so affected client tests were adjusted to the regenerated types.
- Focused validation for Phase 3.2 passed: `dotnet build --configuration Release --verbosity quiet -clp:ErrorsOnly` passed with 0 errors; `Event.Architecture.Tests` passed 258/259 with 1 existing skip; `ControlPlaneOverviewHateoasTests` passed 1/1; `ContractInvariantsTests.OpenApiDocument_PublicHalDetailResourceSchemasAreNotEmpty` passed 1/1; affected Blazor client test classes passed (`EventTemplateHalResourceExtensionsTests`, `SupportAccessClientServiceTests`, `EventReportingServiceTests`, `EventReportModerationServiceTests`, `ModerationReportQueuePageTests`, and `ModerationReportDetailPanelTests`).
- Broad `Event.API.IntegrationTests` was rerun after the OpenAPI catalog fix and remains red with 47 failures out of 1632 tests. Observed categories were unrelated to the new control-plane overview path: PostgreSQL/Testcontainers timeout in `TickerQSchedulerOperationalStoreTests`, GatewayTimeouts in existing event/external API-key tests, existing storage HATEOAS `401 Unauthorized` expectations, existing auth matrix `401` versus `403` expectations, `ExecuteUpdateAsync` unsupported by the InMemory provider in production guardrail tests, a public query ProblemDetails content-type mismatch, and Keycloak/audience fixture failures.
- Implemented the non-destructive Phase 3.3 tenant lifecycle surface: bounded control-plane tenant list/detail DTOs, read queries/handlers, create route wrapping the existing tenant create command, status transition command/handler, lifecycle audit log persistence, status-specific HAL policies, source-generated JSON/OpenAPI/HAL schema registrations, and NSwag-generated client updates.
- The control-plane tenant endpoints are multi-tenant-only through `ControlPlaneController`, admin-classified, rate-limited/request-timeout bounded, and server-authorized. Read links use `ResourceKinds.InstanceSetting` / `AuthorizationActions.InstanceSettings.View`; lifecycle transitions use `InstanceSettings.Update`; the create link advertises the existing `ResourceKinds.Tenant` / `AuthorizationActions.Create` command contract.
- Direct request-time tenant data deletion remains deliberately unimplemented. Current lifecycle routes expose activate, suspend, archive, reactivate, and archived-only `schedule-purge`, where `schedule-purge` records audited destructive intent by moving the tenant to the non-active `Purged` state without deleting tenant data in the request path.
- Focused Phase 3.3 validation passed: `Explore.Application` Release build, `Explore.API` Release build, `Explore.Blazor.Client` Release build, `Event.Application.UnitTests` Release build, `Event.API.IntegrationTests` Release build, `TransitionControlPlaneTenantLifecycleCommandHandlerTests` 6/6, `ControlPlaneTenantHateoasTests` 4/4, `ApiContractInventory_Generate_WritesMarkdownToDocs` 1/1, and focused OpenAPI HAL schema invariant 1/1. Full solution Release build is blocked by unrelated dirty-worktree compile errors in `Event.API.IntegrationTests/Features/EventVisibilityContractTests.cs` and an unrelated duplicate-using diagnostic reported for `Explore.Blazor.Client/Contracts/Services/CustomProperties/ICustomPropertyDefinitionService.cs`.
- Completed Phase 3 Task 3.4: added `GET /api/admin/control-plane/domains`, a multi-tenant-only control-plane domain/DNS read model. The endpoint is authenticated, admin-classified, rate-limited/request-timeout bounded, MediatR-authorized as an instance-setting view, and HAL-wrapped through `ControlPlaneDomainResourceAssembler`.
- The domains read model derives public platform host, wildcard tenant host, dedicated admin host, custom-domain enablement, expected DNS record guidance, and warnings from existing instance domain settings plus configured public/control-plane origins. It intentionally does not perform external DNS lookups in this slice.
- Registered the domain resource in route names, HATEOAS assembler registration, source-generated JSON context, OpenAPI HAL schema catalog, generated OpenAPI, generated Blazor client, API contract inventory, and API changelog. The overview `domains` HAL link now points to the new control-plane domains resource, while the domains resource links back to overview and raw domain settings/edit affordances through HAL permission metadata.
- Focused Phase 3.4 validation passed: `Explore.Application` Release build, `Explore.API` Release build, `Event.Application.UnitTests` Release build, `GetControlPlaneDomainsQueryHandlerTests` 2/2, `ControlPlaneDomainHateoasTests` 1/1, `ControlPlaneOverviewHateoasTests` 1/1, and `ApiContractInventory_Generate_WritesMarkdownToDocs` 1/1. The first parallel test-project build surfaced a local missing `Explore.Domain.Enums` using in the new test; that was fixed before the focused validation passed.
- Final Phase 3.4 verification also passed: `Explore.Blazor.Client` Release build, focused OpenAPI HAL schema invariant, focused `EventControlPlaneBlazorArchitectureTests` 8/8, focused `SupportAccessClientServiceTests` 6/6, and `git diff --check` for touched files. The regenerated NSwag client shifted one support-access HAL link anonymous type to `Anonymous60`; `Explore.Blazor.Client/Services/SupportAccessClientService.cs` now maps that generated link shape so existing HAL affordance preservation continues to compile.
- Completed the remaining Phase 3.3 purge-scheduling boundary: archived tenants now receive a `schedule-purge` HAL affordance backed by `POST /api/admin/control-plane/tenants/{tenantId}/schedule-purge`. The action requires an operator reason, is authorized as an instance-setting update, writes `TenantLifecycleLog`, moves the tenant to the non-active `Purged` lifecycle state, and intentionally does not delete tenant data in the request path. Physical data deletion remains deferred to Phase 8 destructive-operation hardening.
- Completed Phase 3 Task 3.5: added `GET /api/admin/control-plane/operations`, a multi-tenant-only read model for general outbox, email dispatch, and storage status. The endpoint is authenticated, admin-classified, rate-limited/request-timeout bounded, MediatR-authorized as an instance-setting view, and HAL-wrapped through `ControlPlaneOperationsResourceAssembler`.
- The operations read model uses existing repositories/services instead of introducing a new operational store. It caps general outbox samples at 100, uses exact email dispatch health counts already available from `IEmailDispatchOutboxRepository`, reads storage/SMTP settings through existing instance setting services, emits warning codes for capped due backlog, failed outbox rows, email dead letters, stale processing, due backlog, missing SMTP, and unavailable storage, and does not expose tenant payloads, recipient details, provider secrets, object keys, raw provider errors, or mutation actions.
- Registered the operations resource in route names, HATEOAS assembler registration, source-generated JSON context, OpenAPI HAL schema catalog, generated OpenAPI, generated Blazor client, API contract inventory, and API changelog. The overview resource now emits an `operations` HAL link with instance-setting view permission metadata; the operations resource links back to overview and storage settings.
- Focused Phase 3.5 validation passed: `Explore.Application` Release build, `Explore.API` Release build, `Event.Application.UnitTests` Release build, `Event.API.IntegrationTests` Release build, `GetControlPlaneOperationsQueryHandlerTests` 1/1, `ControlPlaneOperationsHateoasTests` 1/1, `ControlPlaneOverviewHateoasTests` 1/1, focused OpenAPI HAL schema invariant 1/1, `ApiContractInventory_Generate_WritesMarkdownToDocs` 1/1, and `Explore.Blazor.Client` Release build. The regenerated client introduced another support-access HAL link anonymous type; `SupportAccessClientService` now keeps the generated mappings needed by existing support-access DTOs.
- Completed Phase 4 Tasks 4.1 and 4.2 for the embedded route foundation. `Explore.Blazor.Client` now references `Event.ControlPlane.Client`, registers `AddEventControlPlaneClient()`, and maps `ControlPlaneRoutes.Overview` to the shared `ControlPlaneOverviewPage` with the existing `AdminRouteGuard`.
- Added the first routable shared RCL page at `Event.ControlPlane.Client/Pages/Overview/ControlPlaneOverviewPage.razor` with CSS isolation. It uses host-provided `IControlPlaneOverviewService` and intentionally renders a fail-closed "Control-plane API unavailable" state until the embedding host registers a real adapter.
- Updated the separate `Event.ControlPlane.Blazor` root page so `/` redirects to the shared `/admin/instance` overview instead of carrying a duplicate `/admin/instance` placeholder. The separate host remains Interactive Server-only.
- Important route integration discovery: the existing `Explore.Blazor.Client` does not use the stock Blazor `Router` for its page table; it uses Blazouter with explicit `RouteConfig` entries. Context7 confirmed the stock `Router.AdditionalAssemblies` pattern for RCL route discovery, but embedded integration in this repo must register control-plane routes directly in `Explore.Blazor.Client/Routes.razor`.
- Focused Phase 4.1/4.2 validation passed: `Event.ControlPlane.Client`, `Event.ControlPlane.Blazor`, `Explore.Blazor.Client`, `Explore.Blazor`, `Explore.Blazor.Client.Tests`, and `Event.Architecture.Tests` Release builds reached 0 errors; focused `RoutesConfigurationTests` passed 11/11; focused `ControlPlaneOverviewPageTests` passed 1/1; focused `EventControlPlaneClientArchitectureTests` passed 6/6; focused `EventControlPlaneBlazorArchitectureTests` passed 8/8. Parallel build attempts initially hit transient static-web-assets/deps file locks, so subsequent verification was rerun sequentially.
- Completed Phase 4 Task 4.3 for embedded navigation. `Explore.Blazor.Client/Layout/NavMenu.razor` now routes BFF/API-confirmed multi-tenant instance admins to `ControlPlaneRoutes.Overview` with the label `Instance Console`; single-tenant instance admins keep the existing `/admin/tenant/settings` `Administration` path.
- `NavMenuAdminTests` now cover the multi-tenant Instance Console affordance and explicitly assert that browser-only admin claims and single-tenant administration states do not expose `/admin/instance` or the old `/admin/instance/settings` control-plane link.
- Focused Phase 4.3 validation passed: `Explore.Blazor.Client.Tests` Release build reached 0 errors with existing warnings, and focused `NavMenuAdminTests` passed 15/15. This bUnit render is the observable UI surface for the protected dropdown behavior in this slice.

### In Progress

- Phase 1 is complete for the current shared BFF hosting scope.
- Phase 2 is complete for the current RCL foundation. The control-plane client scaffold, route/DI foundation, host-neutral service contracts, local design primitives, and architecture guardrails are complete.
- Phase 3 is complete for the planned API/Application control-plane foundation: overview, tenant lifecycle, domains/DNS guidance, operations status, generated contracts, and API docs are in place.
- Phase 4 has started: embedded route and navigation foundations are complete for `/admin/instance`; real host API adapters/pages and broader single-tenant route/API suppression coverage remain.
- Phase 7 has started early because the user requested the separate app/Docker/Keycloak foundation. The app, Dockerfile, dedicated Keycloak seed, Compose profile, `.env.example`, Aspire resource, and operator docs are wired; auth integration/E2E tests remain in Phase 7.6.

### Next

1. Continue Phase 4.4 by building the overview, tenants, and domains shared RCL UI slice with real host API adapters.
2. Keep `Event.ControlPlane.Client` free of `Explore.Blazor.Client`, API, Application, Domain, Infrastructure, Persistence, generated-client dependencies, token storage, and local authorization decisions.
3. Add real host API adapters for overview/tenant/domain/operations service contracts before replacing the fail-closed page state with live data.
4. Add separate-app integration/E2E coverage in Phase 7.6 before claiming the separate host is production-ready.
5. Expand component coverage when real shared RCL pages are added, especially for HAL link affordance rendering and no local role/claim checks.

### Blockers

- None for Phase 4 planning.
- The wider worktree remains heavily dirty with unrelated changes; do not revert unrelated files.
- Full solution Release build was not rerun in this slice. Earlier broad build attempts were blocked by unrelated dirty-worktree errors: `EventVisibilityContractTests.cs` references a missing `EnsureTenantActorAsync`, and broader build output reported unrelated diagnostics outside this control-plane domains slice. Focused project builds for the touched control-plane/API/client surface passed.
- Broad API integration verification is currently blocked by unrelated runtime/auth/test-host failures. The latest full `Event.API.IntegrationTests` run passed 1582 tests, skipped 3, and failed 47; focused control-plane overview/HAL/OpenAPI tests passed.
- A concurrent static-web-assets build lock appeared during one targeted test attempt. Use `--no-build` for filtered TUnit runs after building once.

## Quick Resume

1. Read `dev/active/multi-tenant-control-plane/multi-tenant-control-plane-plan.md`.
2. Read `dev/active/multi-tenant-control-plane/multi-tenant-control-plane-tasks.md`.
3. Continue with Phase 4 Task 4.4 unless the user gives a narrower instruction.
4. Keep all three dev docs updated after each meaningful implementation slice.
5. Do not expose control-plane concepts in single-tenant mode.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `Explore.Infrastructure/DeploymentSettings.cs` | Existing | Infrastructure | Deployment mode settings. | Verified source for `Mode`, `DefaultTenantId`, `HidePlatformAdminInSingleTenant`, and helper flags. |
| `Explore.Infrastructure/Services/DeploymentModeProvider.cs` | Existing | Infrastructure | Runtime/configured deployment-mode resolution. | Persists post-onboarding mode authority and falls back safely pre-onboarding. |
| `Explore.Application/Contracts/Services/IDeploymentModeProvider.cs` | Existing | Application | Deployment-mode abstraction. | Use instead of reading config directly in application flow. |
| `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs` | Existing | API | API-authoritative tenant resolution. | Multi-tenant unresolved requests fail closed with 404. |
| `Explore.API/Middleware/ApiTenantPostAuthenticationMiddleware.cs` | Existing | API | API-key tenant reconciliation after auth. | Important for instance-admin API key behavior. |
| `Explore.API/Filters/BlockInSingleTenantAttribute.cs` | Existing | API | Single/multi-tenant endpoint visibility filters. | Use for multi-tenant-only control-plane endpoints where appropriate. |
| `Explore.Blazor/Extensions/YarpProxyExtensions.cs` | Existing/modified | Blazor BFF | Host adapter registration for shared API proxy. | Now delegates route/cluster/transform setup to `Event.Web.BffHosting.Proxy.AddEventApiProxy`. |
| `Explore.Blazor/Extensions/AuthenticationExtensions.cs` | Existing/modified | Blazor BFF | Host auth registration. | Uses `EventBffTokenRefreshCookieEvents`, shared safe auth diagnostics, and the `ExploreBffCookieSessionHandler` host adapter. |
| `Explore.Blazor/Services/TenantHeaderForwardingHandler.cs` | Existing | Blazor BFF | Trusted tenant header forwarding. | Do not let browser supply tenant authority. |
| `Explore.Blazor.Client/Routes.razor` | Existing | Blazor Client | Current route map. | Contains `/admin/instance/settings` and onboarding route guards. |
| `Explore.Blazor.Client/Routing/Guards/MultiTenantOnboardingRouteGuard.cs` | Existing | Blazor Client | Multi-tenant onboarding guard. | Existing mode-aware UI behavior. |
| `Explore.Blazor.Client/Routing/Guards/TenantAdminRouteGuard.cs` | Existing | Blazor Client | Tenant/admin route behavior. | Single-tenant instance admin can use tenant admin route where intended. |
| `Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor` | Existing | Blazor Client | Current instance settings page. | Keep as single-tenant administration abstraction. |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/*` | Existing | Blazor Client | Current instance settings sections. | Potential source material for control-plane pages, but do not duplicate. |
| `Event.Web.BffHosting/` | New / accepted Phase 1 foundation | Blazor/BFF | Shared ASP.NET Core browser-BFF hosting library. | Builds and is consumed by `Explore.Blazor` for YARP proxying, privileged-header stripping, token/tenant/setup/support forwarding adapters, API base resolution, token safety, dev TLS trust policy, safe auth diagnostics, provider-neutral OIDC option construction, and token refresh cookie events. |
| `Event.Web.BffHosting/Authentication/EventBffOidcOptionsFactory.cs` | New | Blazor/BFF | Shared OIDC option construction. | Centralizes PKCE, token persistence, safe OIDC events, scopes, metadata, callback paths, and IPv4 backchannel behavior without owning dynamic provider orchestration. |
| `Event.Web.BffHosting/Authentication/EventBffTokenRefreshCookieEvents.cs` | New | Blazor/BFF | Shared cookie token-refresh event. | Refreshes server-side access tokens using stored refresh tokens and delegates host-specific enrichment/cleanup/redirects to `IEventBffCookieSessionHandler`. |
| `Event.Web.BffHosting/Authentication/SafeAuthDiagnosticsPolicy.cs` | New | Blazor/BFF | Shared safe auth diagnostics. | Builds browser-safe login redirects with bounded error codes and correlation ids, without exposing provider/client-secret details. |
| `Explore.Blazor/Services/EventBffHostingAdapters.cs` | New | Blazor BFF | Host-specific adapter bridge into `Event.Web.BffHosting`. | Preserves circuit-aware token fallback, tenant route context, setup-secret resolver, and support-access session forwarding outside the shared library. |
| `Explore.Blazor/Services/ExploreBffCookieSessionHandler.cs` | New | Blazor BFF | Host-specific cookie session adapter. | Preserves admin claim enrichment, circuit token updates, auth cookie/session cleanup, and setup-aware expired-session redirects outside the shared library. |
| `Event.Architecture.Tests/EventWebBffHostingArchitectureTests.cs` | New | Tests | Boundary test for shared BFF hosting library. | Guards no project references, no forbidden layer tokens, and `Explore.Blazor` proxy delegation to shared BFF hosting. |
| `Event.ControlPlane.Client/` | New | Blazor Client Library | Shared control-plane Razor class library. | Scaffold exists with assembly marker, route constants/catalog, DI entry point, HAL/result contracts, overview/tenant/domain service contracts, fail-closed defaults, local `ControlPlane*` design primitives, and no forbidden project references. |
| `Event.Architecture.Tests/EventControlPlaneClientArchitectureTests.cs` | New | Tests | Boundary test for shared control-plane client library. | Guards RCL project shape, forbidden layer/token/raw-HTTP/generated-client dependencies, `/admin/instance` route-root composition, DI registration, HAL/failure-state contracts, local design primitives, and no public wrapper coupling. |
| `Event.ControlPlane.Blazor/` | New | Blazor BFF | Self-hostable control-plane app. | Scaffold exists with Keycloak OIDC BFF auth, shared BFF hosting, shared control-plane client registration, MudBlazor host support, Infisical/env loading, Dockerfile, protected shell placeholders, and Interactive Server-only composition. |
| `docker/keycloak/realm-export.json` | Existing/modified | DevOps/Auth | Local Keycloak realm export. | Now defines `islamu-event-control-plane` as a dedicated confidential BFF client with API audience mapper and local/admin-host redirect/logout URIs. |
| `docker/keycloak/ISLAMU-realm.test.json` | Existing/modified | DevOps/Auth | Test Keycloak realm fixture. | Now defines `islamu-event-control-plane` with deterministic `test-control-plane-secret`. |
| `docker/keycloak/keycloak-init.sh` | Existing/modified | DevOps/Auth | Compose Keycloak client-secret synchronization. | Now supports optional `KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET` synchronization for the dedicated control-plane client. |
| `.env.example` | Existing/modified | DevOps/Auth | Self-hosting environment template. | Documents control-plane HTTP port, API endpoint, and dedicated Keycloak client id/secret. |
| `docker-compose.yml` | Existing/modified | DevOps | Self-hosting topology. | Exposes optional `control-plane` profile/service for `Event.ControlPlane.Blazor`. |
| `Explore.AppHost/AppHost.cs` | Existing/modified | DevOps | Aspire orchestration. | Registers `event-control-plane`, wires API service discovery, and injects full-local Keycloak control-plane settings. |
| `docs/DEPLOYMENT_MODES.md` | Existing | Docs | Deployment-mode authority. | Must remain clear that mode is not a casual runtime toggle. |
| `docs/MULTI_TENANCY.md` | Existing | Docs | Tenant isolation and resolver model. | Control-plane host must not weaken fail-closed resolution. |
| `docs/BLAZOR.md` | Existing | Docs | Blazor/BFF architecture. | Update for shared library and separate app. |
| `docs/SELF_HOSTING.md` | Existing | Docs | Docker/self-hosting guidance. | Update embedded/dedicated/separate deployment shapes. |

## Key Decisions

| Decision | Status | Reason |
|---|---|---|
| Event Instance Console exists in both modes, but tenant/platform control-plane features are multi-tenant-only. | Planned | Single-tenant mode keeps the existing administration settings page as its current instance-console abstraction. |
| Create `Event.Web.BffHosting` as a required shared BFF hosting library before the separate app. | Implemented for Phase 1 scope | Proxy/header/token-adapter foundation, reusable OIDC option construction, shared safe auth diagnostics, and token-refresh cookie events are accepted. Later work may add control-plane-specific profile defaults and health checks. |
| Create `Event.ControlPlane.Client` as a shared Razor class library. | Implemented for scaffold, route/DI, service-contract, and design-primitive foundation | Both embedded and separate app must share the same control-plane implementation; `Explore.ControlPlane.*` must not be created for new projects. |
| Create `Event.ControlPlane.Blazor` as a separate self-hostable BFF app. | Scaffold and deployment foundation implemented | Separate app preserves server-side token handling and BFF security through `Event.Web.BffHosting`; integration/E2E coverage remains. |
| Authenticate `Event.ControlPlane.Blazor` through Keycloak OIDC. | Scaffold implemented | Operators sign in through Keycloak OIDC confidential-client BFF auth; integration/E2E coverage remains. |
| Add a dedicated Keycloak client such as `islamu-event-control-plane`. | Implemented for local/self-hosting foundation | Realm export, test realm, init script, Compose profile, Aspire full-local settings, and docs now support the client. |
| Keep render-mode configurability out of `Event.ControlPlane.Blazor`. | Implemented and guarded by architecture tests | `Explore.Blazor` may remain render-policy configurable for public/community routes; `Event.ControlPlane.Blazor` maps only Interactive Server and must not enable Auto/WebAssembly hosting. |
| Keep one control-plane capability, not two products. | Planned | Prevent duplicated auth, clients, layouts, components, and security decisions. |
| Do not add a single-tenant to multi-tenant toggle. | Planned | Existing docs require migration/runbook semantics for mode changes. |
| Use HAL links for resource action affordances. | Required | Project invariant. |
| Prefer async/audited jobs for destructive operations. | Planned | Tenant purge, restore, dead-letter replay, and similar operations need audit/retry safety. |
| Document separate UI host limitations. | Planned | Separate UI does not solve shared API/database saturation by itself. |

## Constraints And Rules To Remember

- Repositories return entities, never DTOs.
- Validators are manually instantiated where the project pattern requires it.
- Use `int` for lookups, `Guid` UUIDv7 for aggregates, and `long` for cursors.
- GET/write attributes must follow project rules, with control-plane endpoints enforcing instance-admin authority.
- HAL `_links` are the source of truth for edit/delete/suspend/purge/retry and similar UI actions.
- `Event.Web.BffHosting` is required and must stay limited to authentication, cookies, proxying, header security, diagnostics, health, and options validation.
- `Event.Web.BffHosting` must not contain UI pages/components, generated clients, Application handlers, Domain entities, Persistence repositories, Keycloak provisioning scripts, Docker Compose definitions, or tenant lifecycle business logic.
- BFF tokens stay server-side; browser code never receives tokens.
- `Event.ControlPlane.Blazor` must use Keycloak OIDC Authorization Code flow plus PKCE with a confidential client and HttpOnly cookies.
- `Event.ControlPlane.Blazor` must stay Interactive Server-only. Its composition root should use `AddInteractiveServerComponents()` and `AddInteractiveServerRenderMode()` only. Do not add `RuntimeRenderPolicyService`, `AddInteractiveWebAssemblyComponents()`, `AddInteractiveWebAssemblyRenderMode()`, InteractiveAuto, InteractiveWebAssembly, a WebAssembly client bundle, or host configuration that changes the separate app's render mode. The existing `Explore.Blazor` host may keep its default Interactive Server posture plus current render-policy customization.
- Keycloak client secrets remain server-side through env/config/secret provider paths and must never appear in browser config, logs, or diagnostics.
- Non-instance-admin authenticated users must not enter the separate control-plane shell.
- BFF strips browser-supplied privileged headers and forwards trusted tenant hints only.
- API tenant resolution is authoritative and fail-closed.
- Single-tenant mode must hide tenant/platform control-plane concepts and keep the current administration settings abstraction.
- All new files require two `ABOUTME:` lines.
- Do not revert unrelated user changes in the dirty worktree.
- Do not run solution-level `dotnet test`; run project test commands.
- New planned BFF/control-plane project names use `Event.*`. Existing `Explore.*` projects remain unchanged unless a separate repository-wide rename is approved.

## Phase 3.1 API/Application Inventory (2026-07-04 Europe/Brussels)

### Evidence Collected

- Rules/docs: `AGENTS.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, `docs/API.md`, `docs/ARCHITECTURE.md`, `docs/AUTHORIZATION.md`, `.claude/rules/api-controllers.md`, `.claude/rules/application-layer.md`, `.claude/rules/api-hateoas.md`.
- External docs: Context7 `/dotnet/aspnetcore.docs` confirmed ASP.NET Core controller attribute routing and `[ProducesResponseType]` metadata remain the correct baseline for future endpoints; this matches the repo's controller-authoring rules.
- Search/read evidence: `Explore.API/Controllers/InstanceSettingsController.cs`, `TenantController.cs`, `InstanceOnboardingController.cs`, `SystemController.cs`, `EmailDispatchAdminController.cs`, `TenantStorageSettingsController.cs`, `StorageObjectController.cs`, `SupportAccessController.cs`, `ManagedProviderProvisioningController.cs`, `LocalizationAdminController.cs`, `UiThemeAdminController.cs`, `Explore.API/Filters/BlockInSingleTenantAttribute.cs`, `Explore.API/Hateoas/RouteNames.cs`, `Explore.API/Hateoas/Policies/TenantLinkPolicy.cs`, `EmailDispatchStatusLinkPolicy.cs`, `StorageAdminLinkPolicy.cs`, `Explore.API/Hateoas/Assemblers/TenantResourceAssembler.cs`, `StorageAdminResourceAssemblers.cs`, `Explore.Application/Contracts/Identity/IAdminContext.cs`, `Explore.Infrastructure/Identity/AdminContext.cs`, and the relevant `Explore.Application/Features/*` handlers/requests listed below.

### Reusable Now

- **Visibility and mode gates:** `BlockInSingleTenantAttribute` and `RequireMultiTenantAttribute` already provide server-side single/multi-tenant endpoint filtering. Use them for new control-plane endpoints instead of client-only route hiding.
- **Instance-admin authority:** `IAdminContext` and `AdminContext` are the current DB-first authority source for platform roles/bootstrap-owner fallback. Control-plane endpoints should use this authority or MediatR authorization metadata, not local Blazor role checks.
- **Onboarding/preflight source material:** `SystemController` exposes public `GET api/System/onboarding-status` and `GET api/System/onboarding-preflight`; handlers `GetSystemOnboardingStatusQueryHandler` and `GetOnboardingPreflightQueryHandler` already compute deployment mode, setup secret state, default tenant checks, auth readiness, canonical host checks, and operational warnings.
- **Instance settings:** `InstanceSettingsController` already covers deployment mode, domains, resolver configuration, storage, SMTP, auth provider, authorization provider, analytics, footer, modules, render policy, and governance settings. These routes remain useful for settings pages but are not an aggregated control-plane overview.
- **Storage:** `GetInstanceStorageSettings`, `UpdateInstanceStorageSettings`, `TestInstanceStorageConnection`, and `RecalculateInstanceStorageUsage` already expose instance storage settings/status. `InstanceStorageSettingsLinkPolicy` emits `self`, `edit`, `provider-test`, and `recalculate-usage` HAL affordances through `AuthorizationActions.InstanceSettings.*`.
- **Email dispatch operations:** `EmailDispatchAdminController`, `GetEmailDispatchStatusQuery`, `GetEmailDispatchStatusQueryHandler`, and `EmailDispatchStatusLinkPolicy` already provide tenant-scoped, sanitized status rows plus HAL-gated `replay` and `park` actions. This is the best existing pattern for future operations/job surfaces.
- **Tenant status primitives:** `TenantStatusEnum` and seed data already include `Provisioning`, `Active`, `Suspended`, `Archived`, and `Purged`, so Phase 3.3 can reuse those status values if lifecycle actions are added.
- **Managed-provider provisioning:** `ManagedProviderProvisioningController` already requires instance-admin authority before provisioning customer tenants/users. It is useful source material for provisioning flows, but it is automation/bootstrap-oriented rather than a general operator tenant-lifecycle API.
- **Support access:** `SupportAccessController` and `SupportAccessLinkPolicy` provide authenticated support session/audit surfaces with HAL. These belong in the security/support section, not in the first overview endpoint.
- **Localization and UI themes:** `LocalizationAdminController` and `UiThemeAdminController` are authenticated admin surfaces. They are not core control-plane overview APIs, but their provider/status patterns may feed a later policies/health page.

### Source Material Only, Not Final Control-Plane Contract

- **Generic tenant CRUD:** `TenantController` exposes `GET api/tenant`, `GET api/tenant/count`, `GET api/tenant/{id}`, `POST api/tenant`, `PUT api/tenant/{id}`, and `DELETE api/tenant/{id}`. Requests for create/update/delete use `AuthorizeResource(ResourceKinds.Tenant, ...)` and `TenantLinkPolicy` emits create/edit/delete links. However, the controller is only `[Authorize]`/`EndpointClass.Authenticated`, is not multi-tenant-only, and the list/detail handlers just map repository entities to DTOs without a dedicated instance-admin-only control-plane read model. Treat this as source material, not the final tenant lifecycle surface.
- **Storage object CRUD:** `StorageObjectController` is object/file management, not an instance storage overview. It should not be used as the first control-plane storage dashboard contract.
- **Deployment mode update route:** `InstanceSettingsController.UpdateDeploymentMode` intentionally returns a validation problem telling operators to set `DEPLOYMENT_MODE` before first-run onboarding. This aligns with the no-casual-toggle rule and should not become a normal control-plane switch.
- **Auth provider status endpoints:** `auth-provider/status` and `authz-provider/status` are anonymous "configured?" probes. A post-onboarding control-plane overview needs an authenticated, consolidated provider summary that avoids leaking secrets and avoids relying on anonymous status probes as operator evidence.

### Missing For Phase 3.2+

- Dedicated `ControlPlaneController` and `RouteNames` entries for a multi-tenant-only, instance-admin-only overview route.
- Application-layer `Explore.Application/Features/ControlPlane/Queries/*` read model that returns version, deployment mode, public/admin host hints, tenant counts by status, provider summaries, storage/email/job warnings, and HAL links without tenant business data.
- HAL assembler/link policy for the overview resource so navigation/actions are emitted by the API, not derived from Blazor claims.
- Dedicated tenant lifecycle read/actions beyond generic CRUD: suspend, archive, reactivate, purge scheduling, domain status, audit, idempotency, and async destructive execution.
- Domain/DNS checklist read model for public platform host, wildcard tenant host, control-plane host, and custom tenant domains.
- Operations summary beyond EmailDispatch: general outbox/dead-letter, background workers, backup readiness, migration readiness, and storage cleanup warnings.
- OpenAPI/API changelog updates once new endpoints are added.

## Validation Baseline

Baseline already run during planning:

```bash
dotnet build --configuration Release --verbosity quiet
```

Result: passed with 25 projects, 0 errors, and existing warnings.

Post-doc verification:

```bash
git diff --check -- dev/active/multi-tenant-control-plane/multi-tenant-control-plane-plan.md dev/active/multi-tenant-control-plane/multi-tenant-control-plane-context.md dev/active/multi-tenant-control-plane/multi-tenant-control-plane-tasks.md
rg -n "^(<!-- ABOUTME|Last Updated:|## 0\\.|## 17\\.|## SESSION PROGRESS|## Status Summary)" dev/active/multi-tenant-control-plane
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj
```

Results:

- `git diff --check` passed.
- Required `ABOUTME`, `Last Updated`, and key dev-docs sections are present.
- `Event.Architecture.Tests` passed with 240 total, 239 succeeded, 0 failed, and 1 intentionally skipped API contract metadata test.
- Latest checklist count is 22 completed out of 70 total checklist items.
- Latest stale-reference scan found no old "do not create BFF project" direction and no future app project list in the workstream docs.
- Latest trailing-whitespace scan was clean for the three workstream files.
- This wording-only re-baseline reran `git diff --check` and targeted stale/future-scope searches; architecture tests were not rerun after this final documentation adjustment.
- Use the project-level architecture command above; this repo's TUnit runner rejected the earlier `--filter` argument form.

Minimum validation after implementation depends on touched layers:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj
```

Add these when relevant:

```bash
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj
```

Phase 1 proxy/header slice validation on 2026-07-04:

```bash
dotnet build Event.Web.BffHosting/Event.Web.BffHosting.csproj --configuration Release --verbosity quiet
dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
```

Results:

- `Event.Web.BffHosting` build passed: 1 project, 0 errors, 0 warnings.
- `Explore.Blazor` build passed: 9 projects, 0 errors, existing package/analyzer warnings.
- `Event.Architecture.Tests` passed: 243 total, 242 succeeded, 1 intentionally skipped API metadata test.
- `Explore.Blazor.IntegrationTests` passed: 186 total, 186 succeeded, 0 skipped.
- Full solution build passed: 26 projects, 0 errors, existing package warnings.

Phase 1 OIDC/cookie/token-refresh slice validation on 2026-07-04:

```bash
dotnet build Event.Web.BffHosting/Event.Web.BffHosting.csproj --configuration Release --verbosity minimal --no-incremental
dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity minimal
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/SafeAuthDiagnosticsPolicyTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffProxyHeaderSanitizerTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventWebBffHostingArchitectureTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
```

Results:

- `Event.Web.BffHosting` build passed: 1 project, 0 errors, 0 warnings.
- `Explore.Blazor` build passed: 9 projects, 0 errors, existing package/analyzer warnings.
- `SafeAuthDiagnosticsPolicyTests` passed: 2 total, 2 succeeded.
- `BffProxyHeaderSanitizerTests` passed: 2 total, 2 succeeded.
- `EventWebBffHostingArchitectureTests` passed: 3 total, 3 succeeded.
- Full `Explore.Blazor.IntegrationTests` currently fails only unrelated SupportAccess test `StartWhenApiSucceedsStoresSessionAndPreservesFlattenedHalBody`; the same run reported 186 succeeded out of 187.
- Full `Event.Architecture.Tests` currently fails only unrelated SupportAccess raw HTTP JSON helper rule for `Explore.Blazor.Client/Services/SupportAccessClientService.cs`.

Phase 2 control-plane client scaffold validation on 2026-07-04:

```bash
dotnet build Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --configuration Release --verbosity minimal --no-incremental
dotnet build Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity minimal --no-incremental
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventControlPlaneClientArchitectureTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
dotnet restore Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --locked-mode --verbosity minimal
```

Results:

- `Event.ControlPlane.Client` build passed: 1 project, 0 errors, 0 warnings.
- `Event.Architecture.Tests` build passed: 8 projects, 0 errors, existing package warnings.
- `EventControlPlaneClientArchitectureTests` passed: 4 total, 4 succeeded.
- `Event.ControlPlane.Client` locked restore passed using the new `packages.lock.json`.

Phase 2 control-plane service-contract validation on 2026-07-04:

```bash
dotnet build Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --configuration Release --verbosity minimal --no-incremental
dotnet build Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity minimal --no-incremental
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventControlPlaneClientArchitectureTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
```

Results:

- `Event.ControlPlane.Client` build passed: 1 project, 0 errors, 0 warnings.
- `Event.Architecture.Tests` build passed: 8 projects, 0 errors, existing package warnings.
- `EventControlPlaneClientArchitectureTests` passed: 5 total, 5 succeeded.

Phase 2 control-plane design-primitive validation on 2026-07-04:

```bash
dotnet restore Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --use-lock-file --verbosity minimal
dotnet restore Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj --use-lock-file --verbosity minimal
dotnet build Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly
dotnet build Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly
dotnet build Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/EventControlPlaneClientArchitectureTests/*" --minimum-expected-tests 1
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/EventControlPlaneBlazorArchitectureTests/*" --minimum-expected-tests 1
```

Results:

- `Event.ControlPlane.Client` restore updated the lock file with direct `MudBlazor` dependency.
- `Event.ControlPlane.Blazor` restore passed with existing AutoMapper advisory warnings and the host lock file now includes direct `MudBlazor` dependency.
- `Event.ControlPlane.Client` build passed: 1 project, 0 errors, 0 warnings.
- `Event.ControlPlane.Blazor` build passed: 7 projects, 0 errors, existing warnings.
- `Event.Architecture.Tests` build passed: 8 projects, 0 errors, existing package/analyzer warnings.
- `EventControlPlaneClientArchitectureTests` passed: 6 total, 6 succeeded.
- `EventControlPlaneBlazorArchitectureTests` passed: 8 total, 8 succeeded.

Phase 7 control-plane deployment wiring validation on 2026-07-04:

```bash
docker compose --env-file .env.example --profile control-plane config
dotnet build Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj --configuration Release --verbosity minimal --no-incremental
dotnet build Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly
dotnet build Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity minimal --no-incremental
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/EventControlPlaneBlazorArchitectureTests/*" --minimum-expected-tests 1
jq empty docker/keycloak/realm-export.json
jq empty docker/keycloak/ISLAMU-realm.test.json
bash -n docker/keycloak/keycloak-init.sh
git diff --check -- docker-compose.yml .env.example Explore.AppHost/AppHost.cs Explore.AppHost/Explore.AppHost.csproj Event.Architecture.Tests/EventControlPlaneBlazorArchitectureTests.cs docs/SELF_HOSTING.md docs/CONFIGURATION.md docs/SECRETS.md docs/SECURITY-MODEL.md docs/OPERATIONS.md docs/TROUBLESHOOTING.md Explore.Blazor/Services/BffCookieForwardingHandler.cs dev/active/multi-tenant-control-plane/multi-tenant-control-plane-plan.md dev/active/multi-tenant-control-plane/multi-tenant-control-plane-context.md dev/active/multi-tenant-control-plane/multi-tenant-control-plane-tasks.md
```

Results:

- Docker Compose config rendered the optional `islamu-event-control-plane` service with `control-plane` profile, internal API endpoint, dedicated Keycloak client id/secret, internal metadata address, and port `7003`.
- `Event.ControlPlane.Blazor` build passed after the Interactive Server import fix: 7 projects, 0 errors, existing transitive warnings.
- `Event.Architecture.Tests` build passed: 8 projects, 0 errors, existing package warnings.
- `EventControlPlaneBlazorArchitectureTests` passed: 7 total, 7 succeeded, including the Interactive Server-only and shared RCL neutrality assertions.
- Keycloak realm JSON syntax and `keycloak-init.sh` shell syntax passed.
- `dotnet build Explore.AppHost/Explore.AppHost.csproj --configuration Release --verbosity quiet --no-incremental` passed after the small `BffCookieForwardingHandler` overload fix; result was 14 projects, 0 errors, existing warnings.
- `git diff --check` passed for touched deployment/docs/dev-doc files.

Phase 3.1 API/Application inventory validation on 2026-07-04:

```bash
git diff --check -- dev/active/multi-tenant-control-plane/multi-tenant-control-plane-plan.md dev/active/multi-tenant-control-plane/multi-tenant-control-plane-context.md dev/active/multi-tenant-control-plane/multi-tenant-control-plane-tasks.md
rg -n "Phase 3.1 API/Application Inventory|Completed: 30/70|Phase 3: Control-Plane API And Application Capabilities - Completed" dev/active/multi-tenant-control-plane
```

Results:

- `git diff --check` passed for the three workstream files.
- Targeted status scan was later superseded by the Phase 4.3 navigation update; current checklist state is `33/70`, with Phase 4.4 shared control-plane pages as the next resume target.

Phase 3.5 operations status validation on 2026-07-04:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly
dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly
dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/GetControlPlaneOperationsQueryHandlerTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ControlPlaneOverviewHateoasTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ControlPlaneOperationsHateoasTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ContractInvariantsTests/OpenApiDocument_PublicHalDetailResourceSchemasAreNotEmpty" --minimum-expected-tests 1 --log-level Error --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/*/ApiContractInventory_Generate_WritesMarkdownToDocs" --minimum-expected-tests 1 --log-level Error --no-progress
dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly
```

Results:

- `Explore.Application` build passed: 2 projects, 0 errors, existing warnings.
- `Explore.API` build passed: 7 projects, 0 errors, existing warnings, and regenerated `schemas/openapi.json` plus `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
- `Event.Application.UnitTests` build passed after correcting the storage-provider enum in the new unit test.
- `Event.API.IntegrationTests` build passed.
- `GetControlPlaneOperationsQueryHandlerTests` passed: 1 total, 1 succeeded.
- `ControlPlaneOverviewHateoasTests` passed: 1 total, 1 succeeded.
- `ControlPlaneOperationsHateoasTests` passed: 1 total, 1 succeeded.
- OpenAPI HAL schema invariant passed: 1 total, 1 succeeded.
- API contract inventory generator passed: 1 total, 1 succeeded, updating `docs/API_CONTRACT_INVENTORY.md`.
- `Explore.Blazor.Client` build passed after preserving regenerated support-access HAL link mapper overloads.
- Current checklist state is `33/70`, with Phase 4.4 shared control-plane pages as the next resume target.

## Current Known Risks / Unknowns

- Later `Event.Web.BffHosting` expansion must stay profile/config/health focused and avoid absorbing UI, business, generated-client, or provisioning responsibilities.
- Future `Event.ControlPlane.Client` components/static assets still need embedded-host verification. The separate-host render-mode contract is now proven by architecture coverage; component behavior still needs focused UI tests when real shared pages are added.
- The control-plane configuration shape is now documented: `Event.ControlPlane.Blazor` maps `KEYCLOAK_CONTROL_PLANE_*` and `CONTROL_PLANE_API_ENDPOINT` into `Bff:Authentication:*` and `ExploreApi:BaseUrl`, while Compose may set `Bff:Authentication:MetadataAddress` directly for internal Keycloak discovery.
- External Keycloak production onboarding still needs UI/runbook depth for provisioning and drift repair of the control-plane confidential client beyond the local Compose/Aspire seed.
- Phase 3.1 inventory originally found no dedicated `ControlPlaneController`/Application `ControlPlane` feature. That foundation now exists for overview, domains, bounded tenant lifecycle, archived-only purge scheduling, and operations status; re-read current code before adding more operation/job endpoints.
- Dedicated admin host must not conflict with tenant host/domain resolution.
- Separate app self-hosting needs truthful docs: it is a separate UI host, not a true reserved-resource management plane.
- Future control-plane operational summaries and repair actions must not leak tenant business data to instance admins.

## Handoff Notes

- **Current state:** Phase 1 shared BFF hosting foundation accepted. `Event.Web.BffHosting` builds cleanly and `Explore.Blazor` consumes it for shared YARP proxying, privileged-header stripping, safe auth diagnostics, reusable OIDC option construction, and token-refresh cookie events. `Event.ControlPlane.Client` now exists as a host-neutral Razor class library scaffold with route constants/catalog, assembly marker, DI entry point, HAL/result contracts, overview/tenant/domain service contracts, fail-closed default services, local `ControlPlane*` design primitives, a shared routable overview page, and architecture guardrails. `Explore.Blazor.Client` now references the RCL, registers the embedded `/admin/instance` Blazouter route with the existing instance-admin guard, and exposes the `Instance Console` dropdown entry only when BFF/API status confirms a multi-tenant instance admin. Single-tenant instance admins still use the existing administration settings route. `Event.ControlPlane.Blazor` now exists as a protected Interactive Server-only separate BFF host scaffold with Dockerfile, shared BFF/control-plane references, env/Infisical/user-secret loading, local Keycloak seed support, Docker Compose profile, Aspire resource, and operator docs; `/` redirects to the shared overview; integration/E2E tests remain. Phase 3 is complete for the planned API/Application foundation: overview, bounded tenant lifecycle, domain/DNS guidance, read-only operations status, generated contracts, and API changelog/inventory coverage.
- **Next action:** Continue Phase 4.4 by building the overview, tenants, and domains UI slice in `Event.ControlPlane.Client`, backed by host API adapters and HAL-gated actions.
- **Blockers:** No blockers for Phase 4. Broad full-suite verification has unrelated SupportAccess failures listed in the validation section.
- **Modified files:** `Directory.Packages.props`, `Explore.sln`, `Event.ControlPlane.Client/*`, `Event.ControlPlane.Blazor/*`, `Event.Web.BffHosting/*`, `Event.Architecture.Tests/EventControlPlaneClientArchitectureTests.cs`, `Event.Architecture.Tests/EventControlPlaneBlazorArchitectureTests.cs`, `docker/keycloak/*`, `docker-compose.yml`, `.env.example`, `Explore.AppHost/*`, selected operator docs, new/modified control-plane API/Application/HAL/OpenAPI files, generated API contract artifacts, and the three `dev/active/multi-tenant-control-plane/*` docs.
- **Validation:** `Event.ControlPlane.Client` build passed with 0 warnings; filtered `EventControlPlaneClientArchitectureTests` passed 6/6; `Event.ControlPlane.Blazor` build passed with 0 errors and existing transitive warnings; Docker Compose control-plane config rendered; AppHost build passed; filtered `EventControlPlaneBlazorArchitectureTests` passed 8/8; Keycloak JSON/script syntax passed; `Explore.Application` Release build passed; `Explore.API` Release build passed; `Explore.Blazor.Client` Release build passed; `Event.Application.UnitTests` Release build passed; `Event.API.IntegrationTests` Release build passed; focused tenant lifecycle tests passed; focused domain tests passed; focused operations tests passed; API contract inventory generator passed; focused OpenAPI HAL schema invariant passed; latest focused `Explore.Blazor.Client.Tests` build passed with 0 errors and focused `NavMenuAdminTests` passed 15/15. Full solution Release build is blocked by unrelated dirty-worktree errors noted above; full `Explore.Blazor.IntegrationTests` and full `Event.Architecture.Tests` have unrelated SupportAccess issues noted above.
- **Documentation impact:** Dev-docs and operator docs updated for the accepted Phase 1 BFF hosting foundation, accepted Phase 2.1/2.2/2.3/2.4/2.5 control-plane client foundation, Phase 7.2/7.4/7.5 deployment foundation, Phase 3.1 API/Application inventory, Phase 3.2 overview endpoint, Phase 3.3 tenant lifecycle surface, Phase 3.4 domain/DNS endpoint, Phase 3.5 operations endpoint, Phase 4.1/4.2 embedded route foundation, Phase 4.3 embedded navigation gating, API contract artifacts, and render-mode boundary clarification.
- **Risks:** The current `schedule-purge` action records audited destructive intent and disables the tenant by moving it to `Purged`; it must not be expanded into direct request-time physical data deletion. Irreversible deletion still needs delayed/audited Phase 8 execution. The next UI work must not expose tenant/platform control-plane concepts in single-tenant mode and must preserve HAL as the action-affordance authority.
- **Notes for next contributor/agent:** Do not start from memory. Continue from the current Phase 4.4 state, re-read the shared RCL page/service files and embedded host adapter points before editing, and keep physical purge deletion out of request handlers.
