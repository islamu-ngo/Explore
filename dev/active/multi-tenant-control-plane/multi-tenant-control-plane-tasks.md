<!-- ABOUTME: Tactical checklist for the multi-tenant control-plane implementation workstream. -->
<!-- ABOUTME: Tracks planning, implementation slices, validation, deferred work, and doc-maintenance obligations. -->

# Multi-Tenant Control Plane - Task Checklist

Last Updated: 2026-07-05 Europe/Brussels

## Status Summary

- **Overall status:** In implementation. Phase 1 shared BFF hosting foundation is accepted; Phase 2 shared control-plane client scaffold, route/DI foundation, host-neutral service contracts, and local design primitives are accepted; Phase 3 API/Application work now has dedicated control-plane overview, bounded tenant lifecycle, domain/DNS guidance, and read-only operations status contracts; the first separate `Event.ControlPlane.Blazor` host/Docker/Keycloak/Compose/Aspire foundation exists and is Interactive Server-only; Phase 4 embedded route and navigation foundation now references the shared control-plane RCL from the existing Blazor client, routes `/admin/instance` to the shared overview page through Blazouter, and exposes that entry point only to multi-tenant instance admins.
- **Completed:** 33/70
- **Current priority:** Continue Phase 4 embedded Instance Console pages and host API adapters.
- **Next recommended slice:** Phase 4.4 build the overview, tenants, and domains UI slice from the shared RCL while preserving HAL-gated actions and single-tenant suppression.

## Implementation Maintenance Rules

- [ ] Before starting work, read plan/context/tasks.
- [ ] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.
- [ ] Do not report completion unless all three dev docs reflect the actual current state.

## Phase 0: Plan Review And Baseline - In Progress

- [x] Create `multi-tenant-control-plane-plan.md`.
  - **Acceptance:** Plan contains Sections 0-17 required by `.claude/commands/dev-docs.md`.
  - **Validation:** Manual structure check.
  - **Effort:** M
  - **Dependencies:** None
- [x] Create `multi-tenant-control-plane-context.md`.
  - **Acceptance:** Context includes session progress, quick resume, key files, decisions, constraints, validation, risks, and handoff.
  - **Validation:** Manual structure check.
  - **Effort:** S
  - **Dependencies:** None
- [x] Create `multi-tenant-control-plane-tasks.md`.
  - **Acceptance:** Checklist tracks implementation maintenance rules, phases, validation, and deferred work.
  - **Validation:** Manual structure check.
  - **Effort:** S
  - **Dependencies:** None
- [x] Run baseline build before planning edits.
  - **Acceptance:** `dotnet build --configuration Release --verbosity quiet` passes or failure is documented.
  - **Validation:** Build passed with existing warnings.
  - **Effort:** M
  - **Dependencies:** None
- [x] Apply Senior CTO feedback to the dev-docs workstream.
  - **Acceptance:** New planned projects use `Event.ControlPlane.*`; separate control-plane app security requires Keycloak OIDC confidential-client BFF auth; plan/context/tasks agree.
  - **Validation:** Manual workstream review and targeted search for stale `Explore.ControlPlane.*` project creation tasks.
  - **Effort:** M
  - **Dependencies:** User review feedback.
- [x] Apply CTO feedback making shared BFF hosting a required foundation.
  - **Acceptance:** Plan/context/tasks require `Event.Web.BffHosting` before `Event.ControlPlane.Blazor`; future app projects stay out of scope; Instance Console language is refined; current dirty-worktree `Event.Web.BffHosting` files are treated as a candidate to audit, not as completed work.
  - **Validation:** Manual workstream review and targeted search for stale "no third project" BFF guidance plus current dirty-worktree status check.
  - **Effort:** M
  - **Dependencies:** User CTO feedback.
- [x] Re-run architecture/context tests after the CTO update.
  - **Acceptance:** `Event.Architecture.Tests` reaches test execution and passes or produces actionable context-rule failures.
  - **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`
  - **Effort:** S
  - **Dependencies:** Senior CTO feedback applied.
- [x] User reviews the plan and approves or corrects scope.
  - **Acceptance:** Plan status changes from Draft to User-reviewed or Approved.
  - **Validation:** Active goal continuation explicitly requested full implementation of the plan.
  - **Effort:** S
  - **Dependencies:** Planning docs.
- [ ] Decide whether to add a dedicated intent to `.claude/contract/intents.yaml`.
  - **Files:** `.claude/contract/intents.yaml` existing; dev docs existing.
  - **Acceptance:** Decision recorded; if intent is added, architecture tests pass.
  - **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`
  - **Effort:** S
  - **Dependencies:** User review.
- [x] Re-baseline current dirty worktree before implementation or before accepting in-progress code.
  - **Files:** all future touched files.
  - **Acceptance:** Context file lists current branch status, unrelated dirty changes, the status of the existing `Event.Web.BffHosting`/`Explore.Blazor` extraction candidate, and any implementation-relevant changes since planning.
  - **Validation:** `git status --short`, targeted reads/searches, `dotnet build --configuration Release --verbosity quiet`.
  - **Effort:** M
  - **Dependencies:** User approval.

## Phase 1: Shared BFF Hosting Foundation - Completed

- [x] **1.1 Create or complete `Event.Web.BffHosting`.**
  - **Files:** `Event.Web.BffHosting/Event.Web.BffHosting.csproj` new/in-progress; `Abstractions/*` new; `Options/*` new; `Proxy/*` new/accepted for this slice; `Security/*` new/accepted for this slice; `Extensions/*` new/accepted for this slice.
  - **Acceptance:** Project builds; generated `bin/` and `obj/` outputs are not treated as source; accepted Phase 1 owns host profiles, proxy/header options, YARP API proxy registration, API base-address resolution, privileged-header sanitization, token safety, neutral host adapter contracts, reusable OIDC option construction, safe auth diagnostics, and token-refresh cookie events; no UI, generated-client, Application, Domain, Persistence, or provisioning dependencies.
  - **Validation:** `dotnet build Event.Web.BffHosting/Event.Web.BffHosting.csproj --configuration Release --verbosity quiet`; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** M
  - **Dependencies:** Phase 0 re-baseline.
- [x] **1.2 Move shared OIDC, cookie, token-refresh, and safe diagnostic primitives.**
  - **Files:** `Explore.Blazor/Extensions/AuthenticationExtensions.cs` existing/modified; `Explore.Blazor/Services/DynamicAuthSchemeManager.cs` existing/modified; `Explore.Blazor/Services/ExploreBffCookieSessionHandler.cs` new; `Explore.Blazor/Services/TokenRefreshCookieEvents.cs` removed; `Explore.Blazor/Services/SafeAuthDiagnosticsPolicy.cs` removed; `Event.Web.BffHosting/Authentication/*` new; `Event.Web.BffHosting/Extensions/ServiceCollectionExtensions.cs` modified.
  - **Acceptance:** Existing `Explore.Blazor` login/logout/token-refresh behavior remains intact through shared registration; no browser-visible secrets/tokens; OIDC errors are safely redacted; token-refresh HTTP backchannel is managed through a named `HttpClientFactory` client.
  - **Validation:** `dotnet build Event.Web.BffHosting/Event.Web.BffHosting.csproj --configuration Release --verbosity minimal --no-incremental` passed with 0 warnings; `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity minimal` passed; focused `SafeAuthDiagnosticsPolicyTests` passed 2/2.
  - **Effort:** L
  - **Dependencies:** 1.1
- [x] **1.3 Move shared YARP proxy and privileged-header security primitives.**
  - **Files:** `Explore.Blazor/Extensions/YarpProxyExtensions.cs` existing; `Explore.Blazor/Services/TenantHeaderForwardingHandler.cs` existing; `Event.Web.BffHosting/Proxy/*` new; `Event.Web.BffHosting/Security/*` new.
  - **Acceptance:** Browser-supplied `X-Tenant-Slug`, `X-Setup-Secret`, support/break-glass headers, and tokens cannot become trusted downstream state; trusted tenant hints come only from server context.
  - **Validation:** `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** L
  - **Dependencies:** 1.1
- [x] **1.4 Make `Explore.Blazor` consume `Event.Web.BffHosting`.**
  - **Files:** `Explore.Blazor/Program.cs` existing; `Explore.Blazor/Extensions/*` existing; `Explore.Blazor/appsettings*.json` existing; `Explore.Blazor.IntegrationTests/*` existing.
  - **Acceptance:** Public web host uses `EventBffHostProfile.PublicWeb`; existing public/tenant/admin web behavior remains stable for the accepted Phase 1 slice; `/api/*` YARP route/cluster/transform setup, safe auth diagnostics, reusable OIDC option construction, and token-refresh cookie events delegate to `Event.Web.BffHosting`.
  - **Validation:** `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet`; architecture tests; Blazor integration tests.
  - **Effort:** L
  - **Dependencies:** 1.3
- [x] **1.5 Add accepted-slice BFF hosting architecture and security coverage.**
  - **Files:** `Event.Architecture.Tests/*` existing/new; `Explore.Blazor.IntegrationTests/*` existing/new.
  - **Acceptance:** Tests fail if `Event.Web.BffHosting` references UI/business/generated-client projects, if `Explore.Blazor` stops delegating `/api/*` YARP proxy setup to `Event.Web.BffHosting`, or if accepted proxy/header/auth-diagnostics behavior regresses. Separate-host token/client-secret browser-state assertions remain part of the `Event.ControlPlane.Blazor` matrix.
  - **Validation:** `EventWebBffHostingArchitectureTests` passed 3/3; `BffProxyHeaderSanitizerTests` passed 2/2; `SafeAuthDiagnosticsPolicyTests` passed 2/2. Full broad suites currently have unrelated SupportAccess failures documented in context.
  - **Effort:** M
  - **Dependencies:** 1.1-1.4

## Phase 2: Shared Control-Plane Client Library - Completed

- [x] **2.1 Create `Event.ControlPlane.Client` Razor class library.**
  - **Files:** `Event.ControlPlane.Client/Event.ControlPlane.Client.csproj` new; `Event.ControlPlane.Client/_Imports.razor` new; `Event.ControlPlane.Client/ControlPlaneClientAssembly.cs` new; `Event.ControlPlane.Client/packages.lock.json` new; `Explore.sln` modified; `Directory.Packages.props` modified.
  - **Acceptance:** Project builds; references are host-neutral; no dependency on `Explore.Blazor.Client`, API, Infrastructure, Persistence, or Domain.
  - **Validation:** `dotnet build Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --configuration Release --verbosity minimal --no-incremental` passed with 0 errors and 0 warnings; filtered `EventControlPlaneClientArchitectureTests` passed 4/4.
  - **Effort:** M
  - **Dependencies:** Phase 0 re-baseline.
- [x] **2.2 Add route constants and service registration extension.**
  - **Files:** `Event.ControlPlane.Client/Routing/ControlPlaneRoutes.cs` new; `Event.ControlPlane.Client/Routing/ControlPlaneRouteKeys.cs` new; `Event.ControlPlane.Client/Routing/ControlPlaneRouteDescriptor.cs` new; `Event.ControlPlane.Client/Routing/IControlPlaneRouteCatalog.cs` new; `Event.ControlPlane.Client/Routing/ControlPlaneRouteCatalog.cs` new; `Event.ControlPlane.Client/Extensions/ServiceCollectionExtensions.cs` new; `Event.Architecture.Tests/EventControlPlaneClientArchitectureTests.cs` new.
  - **Acceptance:** Embedded and separate hosts can register shared routes/services without duplicating route strings.
  - **Validation:** `dotnet build Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --configuration Release --verbosity minimal --no-incremental` passed; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventControlPlaneClientArchitectureTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` passed 4/4.
  - **Effort:** S
  - **Dependencies:** 2.1
- [x] **2.3 Define host-neutral control-plane service contracts.**
  - **Files:** `Event.ControlPlane.Client/Contracts/*` new; `Event.ControlPlane.Client/Services/*` new; `Event.ControlPlane.Client/Extensions/ServiceCollectionExtensions.cs` modified; `Event.ControlPlane.Client/_Imports.razor` modified; `Event.Architecture.Tests/EventControlPlaneClientArchitectureTests.cs` modified.
  - **Acceptance:** Components depend on contracts, not generated clients; contracts model HAL links, failure states, command outcomes, overview, tenant, and domain read models; default services fail closed until hosts register API adapters.
  - **Validation:** `dotnet build Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --configuration Release --verbosity minimal --no-incremental` passed with 0 errors and 0 warnings; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventControlPlaneClientArchitectureTests/*" --minimum-expected-tests 1 --log-level Error --no-progress` passed 5/5.
  - **Effort:** M
  - **Dependencies:** 2.1
- [x] **2.4 Resolve shared design-system dependency without duplication.**
  - **Files:** `Event.ControlPlane.Client/Event.ControlPlane.Client.csproj` modified; `Event.ControlPlane.Client/_Imports.razor` modified; `Event.ControlPlane.Client/Components/Common/ControlPlaneActionButton.razor` new; `ControlPlaneActionButton.razor.css` new; `ControlPlanePageHeader.razor` new; `ControlPlanePageHeader.razor.css` new; `ControlPlanePanel.razor` new; `ControlPlanePanel.razor.css` new; `Event.ControlPlane.Blazor/*` modified for MudBlazor host support; docs modified.
  - **Acceptance:** Control-plane components use Event design-token, BEM, CSS isolation, and MudBlazor v9 conventions through local `ControlPlane*` primitives; no `Explore.Blazor.Client` reference, no `App*` public-wrapper coupling, no new broad shared UI-kit project; separate host provides MudBlazor services, providers, and static assets.
  - **Validation:** `dotnet restore Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --use-lock-file --verbosity minimal` passed; `dotnet restore Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj --use-lock-file --verbosity minimal` passed with existing advisory warnings; `dotnet build Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly` passed with 0 errors and 0 warnings; `dotnet build Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly` passed with 0 errors and existing warnings; focused `EventControlPlaneClientArchitectureTests` passed 6/6; focused `EventControlPlaneBlazorArchitectureTests` passed 8/8.
  - **Effort:** M
  - **Dependencies:** 2.1
- [x] **2.5 Add architecture coverage for the new shared UI library.**
  - **Files:** `Event.Architecture.Tests/EventControlPlaneClientArchitectureTests.cs` modified.
  - **Acceptance:** Tests catch forbidden project/client boundaries, public wrapper coupling, missing local CSS isolation pairs, missing CSS ABOUTME headers, non-control-plane BEM namespaces, bare MudBlazor selectors, and physical direction CSS tokens.
  - **Validation:** `dotnet build Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly` passed with 0 errors and existing warnings; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/EventControlPlaneClientArchitectureTests/*" --minimum-expected-tests 1` passed 6/6; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/EventControlPlaneBlazorArchitectureTests/*" --minimum-expected-tests 1` passed 8/8.
  - **Effort:** M
  - **Dependencies:** 2.1

## Phase 3: Control-Plane API And Application Capabilities - Completed

- [x] **3.1 Inventory existing admin endpoints and handlers.**
  - **Files:** `Explore.API/Controllers/*` existing; `Explore.Application/Features/InstanceOnboarding/*` existing; tenant/settings handlers existing.
  - **Acceptance:** Context file lists reusable endpoints and missing endpoints for overview, tenants, domains, jobs, storage, security, policies, and backups.
  - **Validation:** Targeted controller, handler, route-name, HAL policy, admin-context, tenant-status, and Context7 ASP.NET Core docs reads recorded in context on 2026-07-04.
  - **Effort:** M
  - **Dependencies:** Phase 0 re-baseline.
- [x] **3.2 Add control-plane overview query and endpoint.**
  - **Files:** `Explore.Application/DTOs/ControlPlane/ControlPlaneOverviewDto.cs`; `Explore.Application/Features/ControlPlane/Requests/Queries/GetControlPlaneOverviewQuery.cs`; `Explore.Application/Features/ControlPlane/Handlers/Queries/GetControlPlaneOverviewQueryHandler.cs`; `Explore.API/Controllers/ControlPlaneController.cs`; `Explore.API/Hateoas/Assemblers/ControlPlaneOverviewResourceAssembler.cs`; `Explore.API/Hateoas/Policies/ControlPlaneOverviewLinkPolicy.cs`; `Explore.API/Hateoas/RouteNames.cs`; `Explore.API/Extensions/HateoasAssemblerRegistration.cs`; `Explore.Application/Serialization/ExploreJsonContext.cs`; `Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`; `schemas/openapi.json`; `docs/API_CONTRACT_INVENTORY.md`; `docs/API_CHANGELOG.md`; `Explore.Blazor.Client/Clients/EventApiClient.g.cs`; `Event.API.IntegrationTests/Features/Hateoas/ControlPlaneOverviewHateoasTests.cs`.
  - **Acceptance:** Multi-tenant instance admins receive a safe HAL overview with version, deployment mode, host/domain configuration, tenant counts/status counts, provider summaries, warnings, and instance-settings links. Single-tenant mode remains blocked server-side by `RequireMultiTenant` and continues to use the existing settings abstraction.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet -clp:ErrorsOnly` passed with 0 errors and existing warnings; `Event.Architecture.Tests` passed 258/259 with 1 existing skip; focused `ControlPlaneOverviewHateoasTests` passed; focused OpenAPI HAL schema invariant passed; affected Blazor client fixture tests passed. Full `Event.API.IntegrationTests` remains red with 47 unrelated existing runtime/auth/test-host failures.
  - **Effort:** L
  - **Dependencies:** 3.1
- [x] **3.3 Add tenant lifecycle read/actions surface.**
  - **Files:** `Explore.Application/DTOs/ControlPlane/ControlPlaneTenantDtos.cs`; `Explore.Application/Features/ControlPlane/*`; `Explore.API/Controllers/ControlPlaneController.cs`; `Explore.API/Hateoas/Policies/ControlPlaneTenantLinkPolicy.cs`; `Explore.API/Hateoas/Assemblers/ControlPlaneTenantResourceAssembler.cs`; `Explore.API/Hateoas/RouteNames.cs`; `Explore.API/Extensions/HateoasAssemblerRegistration.cs`; `Explore.Application/Serialization/ExploreJsonContext.cs`; `Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`; `schemas/openapi.json`; `docs/API_CONTRACT_INVENTORY.md`; `docs/API_CHANGELOG.md`; `Explore.Blazor.Client/Clients/EventApiClient.g.cs`; focused unit/HAL tests.
  - **Progress:** Bounded list/detail read models, tenant create route, status-specific HAL affordances, audited activate/suspend/archive/reactivate transitions, and archived-only `schedule-purge` destructive-intent recording are implemented. Suspend/archive/schedule-purge require a reason. Reads and lifecycle transitions are multi-tenant-only, admin-classified, rate-limited/request-timeout bounded, and authorization-gated through MediatR/HAL metadata. `schedule-purge` records an audited move to the non-active `Purged` lifecycle state and does not delete tenant data in the request path.
  - **Remaining:** Irreversible tenant data deletion remains deferred to Phase 8 destructive-operation hardening.
  - **Acceptance:** Tenant create/provision/suspend/archive/purge scheduling is instance-admin-only, HAL-gated, audited, and tenant-safe.
  - **Validation:** `Explore.Application` Release build passed; `Explore.API` Release build passed; `Explore.Blazor.Client` Release build passed; `Event.Application.UnitTests` Release build passed; `Event.API.IntegrationTests` Release build passed; focused `TransitionControlPlaneTenantLifecycleCommandHandlerTests` passed 6/6; focused `ControlPlaneTenantHateoasTests` passed 4/4; `ApiContractInventory_Generate_WritesMarkdownToDocs` passed 1/1; focused OpenAPI HAL schema invariant passed 1/1. Full solution Release build is currently blocked by unrelated dirty-worktree compile errors in `EventVisibilityContractTests` and an unrelated duplicate using reported in the broader build output.
  - **Effort:** XL
  - **Dependencies:** 3.1
- [x] **3.4 Add domains and DNS verification read model.**
  - **Files:** `Explore.Application/DTOs/ControlPlane/ControlPlaneDomainDtos.cs`; `Explore.Application/Features/ControlPlane/Requests/Queries/GetControlPlaneDomainsQuery.cs`; `Explore.Application/Features/ControlPlane/Handlers/Queries/GetControlPlaneDomainsQueryHandler.cs`; `Explore.API/Controllers/ControlPlaneController.cs`; `Explore.API/Hateoas/Assemblers/ControlPlaneDomainResourceAssembler.cs`; `Explore.API/Hateoas/Policies/ControlPlaneDomainLinkPolicy.cs`; `Explore.API/Hateoas/RouteNames.cs`; `Explore.API/Extensions/HateoasAssemblerRegistration.cs`; `Explore.Application/Serialization/ExploreJsonContext.cs`; `Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`; `schemas/openapi.json`; `docs/API_CONTRACT_INVENTORY.md`; `docs/API_CHANGELOG.md`; `Explore.Blazor.Client/Clients/EventApiClient.g.cs`; focused unit/HAL tests.
  - **Acceptance:** API returns public platform, wildcard tenant, admin host, and custom-domain guidance/status. The implementation derives guidance from existing instance domain settings and configured public/control-plane origins; it does not perform external DNS lookups.
  - **Validation:** `Explore.Application` Release build passed; `Explore.API` Release build passed; `Event.Application.UnitTests` Release build passed after a local using fix; focused `GetControlPlaneDomainsQueryHandlerTests` passed 2/2; focused `ControlPlaneDomainHateoasTests` passed 1/1; focused `ControlPlaneOverviewHateoasTests` passed 1/1; `ApiContractInventory_Generate_WritesMarkdownToDocs` passed 1/1; `Explore.Blazor.Client` Release build passed after adding the regenerated support-access `Anonymous60` HAL link mapper; focused OpenAPI HAL schema invariant passed 1/1; focused `EventControlPlaneBlazorArchitectureTests` passed 8/8; focused `SupportAccessClientServiceTests` passed 6/6; `git diff --check` passed for touched files.
  - **Effort:** L
  - **Dependencies:** 3.1
- [x] **3.5 Add operations/jobs/outbox/email/storage/provider status read models.**
  - **Files:** `Explore.Application/DTOs/ControlPlane/ControlPlaneOperationsDto.cs`; `Explore.Application/Features/ControlPlane/Requests/Queries/GetControlPlaneOperationsQuery.cs`; `Explore.Application/Features/ControlPlane/Handlers/Queries/GetControlPlaneOperationsQueryHandler.cs`; `Explore.API/Controllers/ControlPlaneController.cs`; `Explore.API/Hateoas/Assemblers/ControlPlaneOperationsResourceAssembler.cs`; `Explore.API/Hateoas/Policies/ControlPlaneOperationsLinkPolicy.cs`; `Explore.API/Hateoas/Policies/ControlPlaneOverviewLinkPolicy.cs`; `Explore.API/Hateoas/RouteNames.cs`; `Explore.API/Extensions/HateoasAssemblerRegistration.cs`; `Explore.Application/Serialization/ExploreJsonContext.cs`; `Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`; `schemas/openapi.json`; `docs/API_CONTRACT_INVENTORY.md`; `docs/API_CHANGELOG.md`; `Explore.Blazor.Client/Clients/EventApiClient.g.cs`; `Explore.Blazor.Client/Services/SupportAccessClientService.cs`; focused unit/HAL tests.
  - **Acceptance:** Operators can see bounded general outbox, email dispatch, and storage operational status without tenant data leakage. This slice is read-only: no replay, purge, storage cleanup, provider repair, or other mutation action is exposed by the operations resource.
  - **Validation:** `Explore.Application` Release build passed; `Explore.API` Release build passed; `Event.Application.UnitTests` Release build passed; `Event.API.IntegrationTests` Release build passed; focused `GetControlPlaneOperationsQueryHandlerTests` passed 1/1; focused `ControlPlaneOperationsHateoasTests` passed 1/1; focused `ControlPlaneOverviewHateoasTests` passed 1/1; focused OpenAPI HAL schema invariant passed 1/1; API contract inventory generator passed 1/1; `Explore.Blazor.Client` Release build passed after preserving regenerated support-access HAL link mapping overloads.
  - **Effort:** XL
  - **Dependencies:** 3.1
- [x] **3.6 Regenerate/update API contract artifacts if endpoints change.**
  - **Files:** `schemas/openapi.json` existing; `docs/API_CONTRACT_INVENTORY.md` existing; `docs/API_CHANGELOG.md` existing; `Explore.Blazor.Client/Clients/EventApiClient.g.cs` generated.
  - **Acceptance:** OpenAPI, generated client, API inventory, and changelog match the implemented control-plane overview, tenant lifecycle, domains, and operations endpoints.
  - **Validation:** `ApiContractInventory_Generate_WritesMarkdownToDocs` passed 1/1; focused OpenAPI HAL schema invariant passed 1/1; `Explore.Blazor.Client` Release build passed with regenerated API client types.
  - **Effort:** M
  - **Dependencies:** API endpoint tasks.

## Phase 4: Embedded Instance Console And Multi-Tenant Control-Plane UI - In Progress

- [x] **4.1 Reference `Event.ControlPlane.Client` from `Explore.Blazor.Client`.**
  - **Files:** `Explore.Blazor.Client/Explore.Blazor.Client.csproj` modified; `Explore.Blazor.Client/Program.cs` modified.
  - **Acceptance:** Embedded client builds and registers shared control-plane client services without a dependency cycle.
  - **Validation:** `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly` passed with 0 errors and existing warnings; focused `EventControlPlaneClientArchitectureTests` passed 6/6.
  - **Effort:** S
  - **Dependencies:** 2.1
- [x] **4.2 Register embedded control-plane routes under `/admin/instance/*`.**
  - **Files:** `Explore.Blazor.Client/Routes.razor` modified; `Event.ControlPlane.Client/Pages/Overview/ControlPlaneOverviewPage.razor` new; `Event.ControlPlane.Client/Pages/Overview/ControlPlaneOverviewPage.razor.css` new; `Event.ControlPlane.Blazor/Components/Pages/ControlPlaneHome.razor` modified; `Explore.Blazor.Client.Tests/Routing/RoutesConfigurationTests.cs` modified; `Explore.Blazor.Client.Tests/Pages/Admin/ControlPlaneOverviewPageTests.cs` new.
  - **Acceptance:** Instance admins can route to the shared control-plane overview through the existing admin guard; the shared overview fails closed when no host API adapter is registered. Single-tenant route suppression remains covered by the existing admin guard and broader Phase 4.5 regression work.
  - **Validation:** `dotnet build Event.ControlPlane.Client/Event.ControlPlane.Client.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly` passed with 0 errors and 0 warnings; `dotnet build Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly` passed with 0 errors and existing warnings; `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly` passed with 0 errors and existing warnings; `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly` passed with 0 errors and existing warnings; focused `RoutesConfigurationTests` passed 11/11; focused `ControlPlaneOverviewPageTests` passed 1/1; focused `EventControlPlaneBlazorArchitectureTests` passed 8/8.
  - **Effort:** M
  - **Dependencies:** 4.1
- [x] **4.3 Add embedded control-plane navigation and shell behavior.**
  - **Files:** `Explore.Blazor.Client/Layout/NavMenu.razor` modified; `Explore.Blazor.Client.Tests/Layout/NavMenuAdminTests.cs` modified.
  - **Acceptance:** Control-plane nav appears only in multi-tenant mode for BFF/API-confirmed instance admins; single-tenant instance admins keep the existing administration settings link; public/tenant nav remains unchanged.
  - **Validation:** `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly` passed with 0 errors and existing warnings; focused `NavMenuAdminTests` passed 15/15.
  - **Effort:** M
  - **Dependencies:** 4.2
- [ ] **4.4 Build overview, tenants, and domains first slice.**
  - **Files:** `Event.ControlPlane.Client/Pages/Overview/*` new; `Pages/Tenants/*` new; `Pages/Domains/*` new; CSS isolation files new.
  - **Acceptance:** Pages have `PageTitle`, `h1`, accessible controls, responsive layout, and HAL-gated actions.
  - **Validation:** bUnit tests plus browser/visual smoke if available.
  - **Effort:** L
  - **Dependencies:** 3.2, 3.3, 3.4
- [ ] **4.5 Add single-tenant suppression regression tests.**
  - **Files:** Blazor client tests existing/new; API integration tests existing/new.
  - **Acceptance:** Single-tenant admins do not see tenant/platform control-plane navigation/routes; existing settings page remains the single-tenant instance-console abstraction.
  - **Validation:** Blazor client tests and API integration tests.
  - **Effort:** M
  - **Dependencies:** 4.2, 4.3

## Phase 5: Multi-Tenant Onboarding Control-Plane And DNS Guidance - Not Started

- [ ] **5.1 Add multi-tenant administration access choice.**
  - **Files:** onboarding DTOs/settings handlers existing/new; onboarding UI existing/new.
  - **Acceptance:** Multi-tenant onboarding asks how platform administration should be accessed; single-tenant onboarding does not.
  - **Validation:** Application unit tests, API integration tests, component tests.
  - **Effort:** M
  - **Dependencies:** 3.4
- [ ] **5.2 Add DNS checklist and preflight results.**
  - **Files:** DNS checklist read model new; onboarding components existing/new.
  - **Acceptance:** Checklist shows public platform, wildcard tenant, control-plane host, and custom-domain CNAME guidance; skipped DNS is shown as an actionable warning.
  - **Validation:** Unit/component tests.
  - **Effort:** L
  - **Dependencies:** 5.1
- [ ] **5.3 Persist only runtime-relevant onboarding settings.**
  - **Files:** onboarding command handlers existing; settings services existing.
  - **Acceptance:** Persisted values affect host/control-plane behavior; informational choices are not over-modeled.
  - **Validation:** Application and persistence tests if storage changes.
  - **Effort:** M
  - **Dependencies:** 5.1

## Phase 6: Dedicated Control-Plane Hostname Using Existing App Image - Not Started

- [ ] **6.1 Add static admin host configuration and classification.**
  - **Files:** configuration settings classes existing/new; BFF host files existing; `Event.Web.BffHosting/*` new.
  - **Acceptance:** `admin.example.org` style hosts are recognized after trusted forwarded headers; invalid config fails clearly.
  - **Validation:** Unit/integration tests.
  - **Effort:** M
  - **Dependencies:** 1.4, 3.4
- [ ] **6.2 Implement host-based shell separation in the existing app.**
  - **Files:** `Explore.Blazor` existing; `Explore.Blazor.Client` shell/routing existing; control-plane shell new.
  - **Acceptance:** Admin host renders control-plane shell; public and tenant hosts keep their shells; instance-admin auth is enforced.
  - **Validation:** Blazor integration and component tests.
  - **Effort:** L
  - **Dependencies:** 4.2, 6.1
- [ ] **6.3 Add dedicated-host security options.**
  - **Files:** `Event.Web.BffHosting/*` new; BFF auth config existing; rate limiting config existing; security/config docs existing.
  - **Acceptance:** Optional IP allowlist, stricter CSP, cookie naming/domain guidance, and tighter mutation rate limits are implemented or explicitly documented as deferred.
  - **Validation:** Integration tests for protected host behavior.
  - **Effort:** L
  - **Dependencies:** 6.1
- [ ] **6.4 Update reverse-proxy and self-hosting docs for dedicated host.**
  - **Files:** `docs/SELF_HOSTING.md`; `docs/CONFIGURATION.md`; `docs/DEPLOYMENT_MODES.md`.
  - **Acceptance:** Docs show public host, wildcard tenant host, admin host, and forwarded-header requirements.
  - **Validation:** Docs review and build.
  - **Effort:** M
  - **Dependencies:** 6.1, 6.2

## Phase 7: Separate Self-Hostable Control Plane Blazor/BFF App - In Progress

- [x] **7.1 Scaffold `Event.ControlPlane.Blazor`.**
  - **Files:** `Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj` new; `Program.cs` new; `appsettings.json` new; solution file existing.
  - **Acceptance:** App builds, references `Event.Web.BffHosting` and `Event.ControlPlane.Client`, authenticates through Keycloak OIDC as a confidential BFF client, denies non-instance-admin users through the control-plane BFF policy, renders a protected Interactive Server-only control-plane root after auth, registers `AddInteractiveServerComponents()`, maps `AddInteractiveServerRenderMode()`, does not expose a render-mode setting or Auto/WebAssembly fallback, provides MudBlazor services/providers/assets for shared RCL primitives, and uses the shared `event-shared-secrets` user-secrets ID plus Infisical compatibility loading.
  - **Validation:** `dotnet build Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly` passed with 0 errors and the existing transitive warning backlog; focused `EventControlPlaneBlazorArchitectureTests` passed 7/7 after the Interactive Server-only guard was added.
  - **Effort:** L
  - **Dependencies:** 1.4, 2.1, 3.2, 4.4
- [x] **7.2 Define dedicated Keycloak OIDC client and secret boundary.**
  - **Files:** `docker/keycloak/realm-export.json` existing; `docker/keycloak/keycloak-init.sh` existing; `.env.example` existing; `Explore.AppHost/AppHost.cs` existing; `docs/CONFIGURATION.md` existing; `docs/SELF_HOSTING.md` existing; `docs/SECURITY-MODEL.md` existing.
  - **Acceptance:** Dedicated client such as `islamu-event-control-plane` has documented redirect/logout URIs, server-only client secret handling, local Compose/Aspire provisioning, and external-Keycloak guidance; browser-visible config never contains secrets.
  - **Progress:** `realm-export.json` and `ISLAMU-realm.test.json` now define `islamu-event-control-plane`; `keycloak-init.sh` synchronizes `KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET` when supplied; `.env.example`, Compose, AppHost, configuration/secrets/security/self-hosting/troubleshooting docs now document and wire the dedicated secret boundary.
  - **Validation:** `jq empty docker/keycloak/realm-export.json`; `jq empty docker/keycloak/ISLAMU-realm.test.json`; `bash -n docker/keycloak/keycloak-init.sh`; focused `EventControlPlaneBlazorArchitectureTests` passed 6/6.
  - **Effort:** L
  - **Dependencies:** 7.1
- [x] **7.3 Consume shared BFF hosting with the control-plane profile.**
  - **Files:** `Event.Web.BffHosting/*` new; `Event.ControlPlane.Blazor/*` new; `Explore.Blazor.IntegrationTests/*` existing/new.
  - **Acceptance:** `Event.ControlPlane.Blazor` uses `EventBffHostProfile.ControlPlane`; no local duplicate OIDC/YARP/header setup; BFF authentication/proxy/security primitives are shared; browser token/client-secret storage is blocked by architecture coverage; `Event.ControlPlane.Client` stays render-mode neutral while the separate host maps only Interactive Server; the separate host does not import the public app's render-policy customization, `AddInteractiveWebAssemblyComponents()`, `AddInteractiveWebAssemblyRenderMode()`, InteractiveAuto, or a WebAssembly client bundle.
  - **Validation:** `dotnet build Event.Web.BffHosting/Event.Web.BffHosting.csproj --configuration Release --verbosity minimal --no-incremental`; `dotnet build Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj --configuration Release --verbosity quiet --no-incremental -clp:ErrorsOnly`; focused `EventControlPlaneBlazorArchitectureTests` passed 8/8 with the Interactive Server-only, shared RCL neutrality, and MudBlazor host-readiness assertions.
  - **Effort:** L
  - **Dependencies:** 1.5, 7.1, 7.2
- [x] **7.4 Add Docker Compose profile and image configuration.**
  - **Files:** `docker-compose.yml` existing; Dockerfile new if needed; `.env.example` existing.
  - **Acceptance:** Self-hosters can run the separate control-plane app as an optional profile/service; Keycloak client secret, authority, metadata, callback, logout, TLS, and reverse-proxy settings are documented.
  - **Validation:** `docker compose --env-file .env.example --profile control-plane config` passed; focused `EventControlPlaneBlazorArchitectureTests` passed 6/6; `git diff --check` passed for touched deployment/docs files.
  - **Effort:** L
  - **Dependencies:** 7.1, 7.2
- [x] **7.5 Add Aspire AppHost resource.**
  - **Files:** `Explore.AppHost/AppHost.cs` existing; launch settings existing.
  - **Acceptance:** Aspire can start/describe the control-plane app resource without breaking existing topology and supplies local Keycloak control-plane client settings in full-local mode.
  - **Validation:** `dotnet build Explore.AppHost/Explore.AppHost.csproj --configuration Release --verbosity quiet --no-incremental` passed with existing warnings; focused `EventControlPlaneBlazorArchitectureTests` passed 6/6.
  - **Effort:** M
  - **Dependencies:** 7.1, 7.2
- [ ] **7.6 Add separate app integration/E2E tests.**
  - **Files:** existing Blazor integration/E2E fixtures.
  - **Acceptance:** Tests cover Keycloak OIDC challenge redirect, callback failure handling, cookie issuance, non-instance-admin denial, root overview, proxy behavior, header stripping, shell isolation, and no browser-visible tokens/client secrets.
  - **Validation:** Project-specific integration/E2E commands.
  - **Effort:** L
  - **Dependencies:** 7.1, 7.2, 7.3

## Phase 8: Hardening, Docs, And Release Readiness - Not Started

- [ ] **8.1 Review destructive operations for audit, idempotency, and async execution.**
  - **Files:** control-plane mutation handlers/controllers/audit/outbox files existing/new.
  - **Acceptance:** Tenant purge, restore, dead-letter replay, backup/restore, and similar actions have confirmation, audit, and recovery behavior.
  - **Validation:** Unit/integration tests.
  - **Effort:** L
  - **Dependencies:** Phase 3 mutations.
- [ ] **8.2 Add observability and operator-visible failure states.**
  - **Files:** health/logging/metrics files existing/new; troubleshooting docs.
  - **Acceptance:** Control-plane and shared BFF hosting failures have structured logs, status cards, and troubleshooting guidance.
  - **Validation:** Tests/manual smoke.
  - **Effort:** M
  - **Dependencies:** Phase 3 operations.
- [ ] **8.3 Update product and architecture docs.**
  - **Files:** `docs/ADMIN_GUIDE.md`; `docs/DEPLOYMENT_MODES.md`; `docs/MULTI_TENANCY.md`; `docs/SELF_HOSTING.md`; `docs/CONFIGURATION.md`; `docs/BLAZOR.md`; `docs/SECURITY-MODEL.md`; `docs/OPERATIONS.md`; `docs/CODEBASE_STRUCTURE.md`.
  - **Acceptance:** Docs describe `Event.Web.BffHosting`, Instance Console language, implemented deployment shapes, and multi-tenant-only tenant/platform capabilities without listing future app projects.
  - **Validation:** Architecture/context tests and docs review.
  - **Effort:** L
  - **Dependencies:** Phases 1-7.
- [ ] **8.4 Update API changelog and OpenAPI schema.**
  - **Files:** `docs/API_CHANGELOG.md`; `schemas/openapi.json`.
  - **Acceptance:** New/changed endpoints are reflected in API docs/contracts.
  - **Validation:** API contract/inventory tests.
  - **Effort:** M
  - **Dependencies:** Phase 3 endpoints.
- [ ] **8.5 Refresh dev docs and final handoff.**
  - **Files:** `dev/active/multi-tenant-control-plane/*`.
  - **Acceptance:** Plan/context/tasks reflect final state, validation, remaining work, and next steps.
  - **Validation:** Manual final review.
  - **Effort:** S
  - **Dependencies:** All completed implementation slices.

## Verification Checklist

- [ ] LSP diagnostics clean for modified files where applicable.
- [x] `dotnet build --configuration Release --verbosity quiet` passes. On 2026-07-04 it passed with 26 projects, 0 errors, and existing package warnings.
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj` passes. Current broad run is blocked by unrelated SupportAccess raw HTTP JSON helper failure; focused `EventWebBffHostingArchitectureTests` passed 3/3 for this slice.
- [ ] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj` passes when Application changes.
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` passes when persistence/migrations change.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj` passes when API changes.
- [ ] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj` passes when BFF changes. Current broad run is blocked by unrelated `BffSupportAccessEndpointsTests`; focused BFF auth/header tests passed 4/4 for this slice.
- [x] `Event.Web.BffHosting` architecture/security checks cover forbidden dependencies, `Explore.Blazor` delegation to the shared proxy, privileged-header stripping, safe OIDC failure redaction, shared token-refresh registration, and server-side token-forwarding behavior for the accepted Phase 1 slice.
- [x] Existing dirty-worktree BFF extraction files are accepted only after build, architecture checks, and BFF security tests pass; otherwise they are refined or replaced during Phase 1.
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj` passes when UI/client changes.
- [ ] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` passes when infrastructure/config/provider changes.
- [ ] E2E/manual browser smoke covers embedded, dedicated-host, and separate-app UI where feasible.
- [ ] Docker Compose and Aspire smoke checks run or skipped with documented reason when DevOps changes.
- [ ] Keycloak OIDC control-plane client smoke covers challenge, callback, logout, missing config, non-instance-admin denial, and no browser-visible token/client-secret leakage.
- [ ] Docs updated where behavior/config/operations/API changed.
- [ ] Dev docs refreshed with final state and remaining work.

## Remaining / Deferred Work

- True reserved-resource management API/worker for operational rescue under public traffic saturation. Deferred until the shared control-plane model is stable.
- Single-tenant to multi-tenant migration wizard/runbook. Deferred and must not become a casual settings toggle.
- Enterprise managed-hosting/fleet-console features beyond this one-instance self-hostable control-plane UI are out of scope and not planned as future app projects in this workstream.
- Mandatory MFA for instance admins. Document as a Keycloak realm/client policy expectation unless current auth provider work already supports enforcing it in-app.
- Full backup/restore orchestration if no current backend exists. Plan should start with readiness/status and add execution only after backend design is approved.
