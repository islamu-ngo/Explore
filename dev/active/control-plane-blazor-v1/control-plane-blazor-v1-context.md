<!-- ABOUTME: Living context for the Control Plane Blazor v1.0 planning and implementation workstream. -->
<!-- ABOUTME: Summarizes user intent, repository evidence, CTO decisions, constraints, risks, and handoff notes. -->

# Control Plane Blazor v1.0 Context

Last Updated: 2026-07-08 Europe/Brussels

## Purpose

This file is the handoff context for finishing the Control Plane Blazor app to a working v1.0 governance platform. Keep it current whenever implementation starts, pauses, or completes a slice.

Implementation started after the user explicitly said `Start implementation !`. Phase 1 connectivity, Phase 2A SaaS pricing-tier modeling, Phase 3A-3E tenant-plan persistence/Application behavior, the Phase 4 tenant-plan plus tenant effective-configuration API/HAL/generated-client/RCL service seams, Phase 4D tenant-configuration write contracts (lock/unlock/override), and Phase 4E cerbos policy test coverage for Control Plane setting keys are complete. Phase 4 is fully complete. Phase 5A (Plan Studio read surfaces + Tenant Configuration center with HAL-gated write controls) is complete; the next slice is Phase 5B (Plan Editor draft forms, assignment diff/apply/rollback UI, admin_portal settings, layout nav).

Product naming decision (2026-07-08): There are **two** Blazor apps, not three: (1) **AdminPortal** — a single app for one instance that hosts both Instance Console (instance admin) and Tenant Console (tenant admin, if instance admin allows); (2) **ConsolePortal** — a future separate app for managed-hosting providers managing many instances. Full hierarchy documented in `dev/active/controlplane.md`. Code project names (`Event.ControlPlane.Blazor`, `Event.ControlPlane.Client`) remain until a rename PR alongside the Tenant Console shell. Next major slice is Phase 5 Instance Console UI (Plan Studio + per-tenant configuration center).

## User Intent

The user first reported that the Control Plane Blazor app is currently useless because the main pages show unavailable/resource-not-found/fail-closed messages and several routes are placeholders.

The user then asked to improve the implementation plan as a senior CTO and add additional product capabilities:

- Per-tenant configuration managed by instance admins.
- Feature locking for tenants.
- A plan creator/template system that can customize a tenant.
- Storage limits and email sending limits.
- Locked/unlocked features based on the existing admin hierarchy and locking model.
- Broader enterprise-grade product scope beyond simply replacing placeholder pages.

The user then clarified that `tenant plan` means SaaS subscription/pricing tier: multiple tiers with different prices, and tenant provisioning by API should create/configure a tenant from the selected tier. Instance admins must be able to update plans through controlled versioned changes.

## Contract Baseline

No exact `.claude/contract/intents.yaml` intent covers completing the entire Control Plane app or adding Tenant Plan Studio. Use a composite contract per slice:

- BFF/host/auth failures: `bff-auth-bug`.
- UI affordances: `blazor-component-affordance`.
- New reads: `add-get-endpoint` and `add-cqrs-handler`.
- New mutations: `add-write-endpoint` and `add-cqrs-handler`.
- HAL links: `add-hal-link`.
- Persistence/query work: `update-repository-query` or `add-ef-migration`.
- Authorization/policy work: `cerbos-policy-change`.
- Generated client/OpenAPI changes: `openapi-contract-change`.

Global rules to preserve:

- Browser never sees tokens, setup secrets, API keys, provider secrets, or privileged headers.
- BFF forwards bearer tokens server-side.
- DB-derived admin authority is authoritative.
- User ID fallback remains `sub -> nameidentifier -> sid`.
- HAL `_links` gates UI actions.
- Controllers stay thin.
- Repositories return entities.
- Validators are manually instantiated.
- Tenant isolation filters remain active in runtime request paths.
- Project-level tests only; no solution-level `dotnet test`.

## Docs, Rules, Skills, and External Docs Loaded

Repository docs:

- `AGENTS.md`
- `.claude/contract/intents.yaml`
- `docs/QUICK_REFERENCE.md`
- `docs/GOVERNANCE.md`
- `docs/ARCHITECTURE.md`
- `docs/SECURITY-MODEL.md`
- `docs/BLAZOR.md`
- `docs/API.md`
- `docs/AUTHORIZATION.md`
- `docs/ADMIN_HIERARCHY.md`
- `docs/MULTI_TENANCY.md`
- `docs/STORAGE.md`
- `docs/OPERATIONS.md`
- `docs/DESIGN_SYSTEM.md`
- `docs/ACCESSIBILITY.md`

Rules:

- `.claude/rules/blazor-server.md`
- `.claude/rules/blazor-client.md`
- `.claude/rules/api-controllers.md`
- `.claude/rules/api-hateoas.md`
- `.claude/rules/application-layer.md`
- `.claude/rules/efcore-persistence.md`
- `.claude/rules/tests.md`

Skills:

- `senior-cto-feedback`
- `clean-architecture-rules`
- `cqrs-mediatr-guidelines`
- `dotnet-efcore-guidelines`
- `auth-patterns`
- `blazor-bff-patterns`
- `blazor-ui-conventions`
- `blazor-css-isolation`
- `design-system`
- `error-tracking`
- `outbox-pattern`

Context7 references:

- ASP.NET Core Blazor: Interactive Server registration, app-wide render mode, and additional assemblies for routable RCL components.
- YARP: transforms for path/header manipulation and server-side proxy behavior.
- MudBlazor: current layout/data-display primitives such as `MudStack`, `MudSimpleTable`, and `MudTable`.

## Current Architecture Map

Browser flow:

- Browser signs into the dedicated Control Plane BFF through Keycloak OIDC.
- BFF stores auth in an HttpOnly cookie.
- BFF shell policy requires instance-admin authority.
- RCL pages render through Interactive Server.
- RCL pages call host-neutral service interfaces.
- `Event.ControlPlane.Blazor` adapts those service interfaces to generated `IEventApiClient` calls.
- Typed API client forwards the bearer token server-side through `EventBffBearerForwardingHandler`.
- API controllers send MediatR requests.
- MediatR authorization checks DB-derived admin authority and resource policies.
- API returns DTOs and HAL links.
- RCL pages gate actions by `_links` only.

Governance flow to add:

- Instance admin creates or edits a SaaS tenant plan version with stable key, price amount, currency, billing period, and active-for-provisioning status.
- Application validates plan keys, locks, quota ceilings, and unsupported settings.
- API returns a plan diff before assignment.
- Plan assignment/provisioning writes or references existing typed settings, tenant settings, quota records, locks, and audit records.
- Tenant admins later see only delegated/unlocked settings and remain bounded by instance ceilings.

## Important Files

Existing host/RCL/API:

- `Event.ControlPlane.Blazor/Program.cs`
- `Event.ControlPlane.Blazor/Services/ControlPlaneApiAdapter.cs`
- `Event.ControlPlane.Blazor/Components/Pages/ControlPlaneSectionPage.razor`
- `Event.ControlPlane.Blazor/Clients/EventApiClient.g.cs`
- `Event.ControlPlane.Client/Routing/ControlPlaneRoutes.cs`
- `Event.ControlPlane.Client/Extensions/ServiceCollectionExtensions.cs`
- `Event.ControlPlane.Client/Services/UnconfiguredControlPlaneClient.cs`
- `Event.ControlPlane.Client/Pages/Overview/ControlPlaneOverviewPage.razor`
- `Event.ControlPlane.Client/Pages/Tenants/ControlPlaneTenantsPage.razor`
- `Event.ControlPlane.Client/Pages/Domains/ControlPlaneDomainsPage.razor`
- `Event.ControlPlane.Client/Pages/Operations/ControlPlaneOperationsPage.razor`
- `Explore.API/Controllers/ControlPlaneController.cs`
- `Explore.API/Controllers/SettingsController.cs`
- `Explore.API/Controllers/InstanceSettingsController.cs`
- `Explore.Application/Features/ControlPlane/**`
- `Explore.Application/Contracts/Identity/IAdminContext.cs`
- `Explore.Infrastructure/Identity/AdminContext.cs`

Likely new areas for tenant plans:

- `Explore.Domain/**/TenantPlan*.cs` or equivalent, if the design slice confirms Domain entities.
- `Explore.Application/Features/ControlPlane/Plans/**` or equivalent.
- `Explore.Persistence/Configurations/*TenantPlan*Configuration.cs`
- `Explore.Persistence/Repositories/*TenantPlan*Repository.cs`
- `Explore.API/Controllers/ControlPlaneController.cs` or a focused Control Plane plans controller if route grouping gets too large.
- `Event.ControlPlane.Client/Pages/Plans/**`
- `Event.ControlPlane.Client/Pages/TenantConfiguration/**`

## Current Evidence

Dedicated host:

- `Program.cs` already registers Control Plane BFF hosting, Keycloak authentication, antiforgery, API proxy, `ControlPlaneBffCookieSessionHandler`, MudBlazor, RCL services, bearer forwarding, generated `IEventApiClient`, and `ControlPlaneApiAdapter`.
- The component route map includes `AddAdditionalAssemblies(ControlPlaneClientAssembly.Value)` and `RequireAuthorization(EventBffAuthorizationPolicies.ControlPlaneAccess)`.

RCL:

- Overview, tenants, domains, and operations are real pages.
- Onboarding, health, storage, jobs, security, and policies are still placeholder routes through the host placeholder page.
- `UnconfiguredControlPlaneClient` intentionally fails closed when no host adapter is registered.

API/Application:

- `ControlPlaneController` already exposes overview, domains, operations, tenants list/detail, create tenant, tenant lifecycle commands, and deployment-mode operations.
- `SettingsController` already exposes tenant settings read/update/lock/unlock endpoints.
- `InstanceSettingsController` already exposes many instance governance surfaces.
- Application handlers already build redacted overview, DNS/domain guidance, operations status, deployment-mode runbook, deployment-mode transition, and tenant lifecycle transition.
- Generated clients now include current Control Plane endpoints plus tenant-plan list/detail, version draft/update/publish/archive/clone, validation, diff preview, assignment get/switch/apply/rollback, and tenant effective-configuration methods.

Settings, locks, quotas, and hierarchy:

- `docs/MULTI_TENANCY.md` documents 5-tier settings cascade and higher-scope locks.
- `docs/ADMIN_HIERARCHY.md` documents instance admin and tenant admin boundaries, delegation, audit, and emergency access.
- `docs/STORAGE.md` documents storage policy, quotas, tenant delegation lock, redacted UI, metrics, and backup/restore impact.
- Existing code/tests show storage quota, external API key quota, quota ProblemDetails, and quota metrics infrastructure.
- Tenant plans now have normalized persisted aggregates, Application CQRS commands/queries, Control Plane API/HAL endpoints, generated clients, and host-neutral Control Plane plan service contracts.
- Tenant effective configuration now has an Application read model, Control Plane API/HAL endpoint, generated clients, and a host-neutral Control Plane tenant-configuration service contract for read-only value source, lock source, plan assignment, and quota usage display.

Phase 2 model evidence:

- `Explore.Domain/Settings/SettingRegistry.cs` is a code-defined registry of allowed setting definitions and should be the validation source for plan setting keys.
- `Explore.Domain/Settings/SettingDefinition.cs` marks setting value type, category, scope, lockability, allowed values, and `IsSensitive`.
- Sensitive settings such as SMTP password, S3 access/secret keys, and AI assistant API key must never be stored in tenant plans.
- Tenant plans should reference registered non-sensitive settings and supported quota domains, not arbitrary JSON keys.
- Email sending limits are requested product scope, but no existing email-send quota service has been proven yet; model it only after enforcement is added or explicitly track it as future quota scope.

Tests:

- `Explore.Blazor.IntegrationTests` has Control Plane authorization and admin-host shell selector tests.
- `Event.API.IntegrationTests` has Control Plane policy/HATEOAS/single-tenant tests.
- Architecture tests enforce context/rule conventions.

## Implementation Progress

Completed Phase 1A on 2026-07-07:

- Re-read Phase 1 tasks, BFF/auth rules, and `docs/BLAZOR.md` before editing.
- Ran baseline `dotnet build --configuration Release --verbosity quiet`; result: 0 errors, 56 existing warnings.
- Proved the first connectivity root cause with a red `Explore.Blazor.IntegrationTests` regression: `AddInfisicalControlPlaneCompatibility_WhenAspireApiReferenceExists_DoesNotMapGenericApiEndpoint` failed because `Event.ControlPlane.Blazor` mapped generic `API_ENDPOINT` into `ExploreApi:BaseUrl` even when Aspire service discovery exposed `services:explore-api:https:0`.
- Fixed `Event.ControlPlane.Blazor/Extensions/ConfigurationExtensions.cs` so Aspire `explore-api` service discovery wins over generic Infisical `API_ENDPOINT`, matching the public Blazor host behavior.
- Added an aliased project reference from `Explore.Blazor.IntegrationTests` to `Event.ControlPlane.Blazor` because both web projects expose top-level `Program`.
- Verified `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`; result: 225 passed, 0 failed.
- Verified `dotnet build --configuration Release --verbosity quiet`; result: 0 errors, 56 existing warnings.

Files changed in Phase 1A:

- `Event.ControlPlane.Blazor/Extensions/ConfigurationExtensions.cs`
- `Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj`
- `Explore.Blazor.IntegrationTests/Extensions/ControlPlaneConfigurationExtensionsTests.cs`

Completed Phase 1B on 2026-07-07:

- Added `Event.ControlPlane.Blazor/Program.Partial.cs` as a test-only seam for `WebApplicationFactory`, matching the existing public Blazor host pattern.
- Added `Explore.Blazor.IntegrationTests/Services/ControlPlaneHostRegistrationTests.cs` to boot the dedicated Control Plane host and prove the RCL service interfaces resolve to the real `ControlPlaneApiAdapter` instead of `UnconfiguredControlPlaneClient`.
- Verified the dedicated host DI path for overview, tenants, domains, and operations/runbook service interfaces. Operations and deployment-mode runbook share `IControlPlaneOperationsService`, so resolving that interface to the adapter prevents the runbook path from using fallback services.
- Updated `Event.ControlPlane.Client/Pages/Overview/ControlPlaneOverviewPage.razor` so the failure copy covers real dedicated-host API reachability failures instead of implying only a missing adapter.
- Re-ran `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`; result: 226 passed, 0 failed.
- Re-ran `dotnet build --configuration Release --verbosity quiet`; result: 0 errors, 56 existing warnings.
- Ran `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`; result: 259 passed, 1 skipped, 0 failed.
- Ran scoped `aft_inspect` for touched files; result: 0 diagnostics, with the caveat that C# and Razor LSP servers are not installed.

Files changed in Phase 1B:

- `Event.ControlPlane.Blazor/Program.Partial.cs`
- `Explore.Blazor.IntegrationTests/Services/ControlPlaneHostRegistrationTests.cs`
- `Event.ControlPlane.Client/Pages/Overview/ControlPlaneOverviewPage.razor`

Completed Phase 2A on 2026-07-07:

- Re-read tenant governance, admin hierarchy, storage, API, settings, and quota sources before modeling tenant plans as SaaS pricing tiers.
- Updated the workstream plan/context/tasks so `tenant plan` means a SaaS tier with stable key, display name, price amount, currency, billing period, active-for-provisioning state, settings, locks, quotas, and versioned update semantics.
- Added red Application unit tests for the first tenant-plan seam: valid priced tier validation, missing pricing currency, unsupported setting keys, sensitive setting rejection, negative quota rejection, and setting/lock diff generation.
- Added a pure Application model seam in `Explore.Application/Features/ControlPlane/Plans/TenantPlanModels.cs`; this intentionally avoids Domain entities, persistence, migrations, API endpoints, and UI until pricing-tier semantics are pinned.
- The model validates plan setting overrides against `SettingRegistry`, rejects sensitive settings, validates supported quota keys, and computes setting/lock diffs against effective tenant configuration.
- Verified `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`; result: 2029 passed, 0 failed.
- Verified `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`; result: 259 passed, 1 skipped, 0 failed.
- Verified `dotnet build --configuration Release --verbosity quiet`; result: 0 errors, 6221 existing warnings.
- Ran scoped `aft_inspect` for the new Application model and tests; result: 0 diagnostics, with the caveat that C# LSP is not installed.

Files changed in Phase 2A:

- `Explore.Application/Features/ControlPlane/Plans/TenantPlanModels.cs`
- `Event.Application.UnitTests/Features/ControlPlane/Plans/TenantPlanDraftValidatorTests.cs`
- `dev/active/control-plane-blazor-v1/control-plane-blazor-v1-plan.md`
- `dev/active/control-plane-blazor-v1/control-plane-blazor-v1-context.md`
- `dev/active/control-plane-blazor-v1/control-plane-blazor-v1-tasks.md`

Completed Phase 3A on 2026-07-07:

- Re-read Phase 3, EF Core, CQRS, Clean Architecture, Domain, and migration rules before persistence edits.
- Added a red persistence integration test for the missing tenant-plan schema, lookup seeding, normalized setting/quota child rows, duplicate setting/quota constraints, and one-active-assignment-per-tenant constraint.
- Added normalized Domain entities and lookup tables for SaaS tenant-plan persistence: plans, versions, version settings, version quotas, assignments, application logs, plan statuses, assignment statuses, and application statuses.
- Added Application repository contract `ITenantPlanRepository` returning entities, not DTOs.
- Added EF Core configurations, DbSets, repository implementation, DI registration, runtime lookup seeding, Respawn lookup preservation, and migration `20260707095103_AddTenantPlanGovernance`.
- Verified generated migration creates lookup tables, normalized plan/version/setting/quota/assignment/log tables, unique plan/version/setting/quota indexes, and a filtered unique active-assignment index on tenant.
- Verified `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`; result: 250 passed, 0 failed.
- Verified `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`; result: 2029 passed, 0 failed.
- Verified `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`; result: 317 passed, 0 failed.
- Verified `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`; result: 259 passed, 1 skipped, 0 failed.
- Verified `dotnet build --configuration Release --verbosity quiet`; result: 0 errors, 5965 existing warnings.
- Ran scoped `aft_inspect`; result: 0 diagnostics, with the caveat that C# LSP is not installed.

Files changed in Phase 3A:

- `Explore.Domain/Enums/TenantPlanStatusEnum.cs`
- `Explore.Domain/Enums/TenantPlanAssignmentStatusEnum.cs`
- `Explore.Domain/Enums/TenantPlanApplicationStatusEnum.cs`
- `Explore.Domain/TenantPlanStatus.cs`
- `Explore.Domain/TenantPlanAssignmentStatus.cs`
- `Explore.Domain/TenantPlanApplicationStatus.cs`
- `Explore.Domain/TenantPlan.cs`
- `Explore.Domain/TenantPlanVersion.cs`
- `Explore.Domain/TenantPlanVersionSetting.cs`
- `Explore.Domain/TenantPlanVersionQuota.cs`
- `Explore.Domain/TenantPlanAssignment.cs`
- `Explore.Domain/TenantPlanApplicationLog.cs`
- `Explore.Application/Contracts/Persistence/ITenantPlanRepository.cs`
- `Explore.Persistence/Configurations/Entities/TenantPlanConfigurations.cs`
- `Explore.Persistence/Repositories/TenantPlanRepository.cs`
- `Explore.Persistence/ExploreDbContext.DbSets.cs`
- `Explore.Persistence/PersistenceServicesRegistration.cs`
- `Explore.Persistence/Seed/LookupTableSeeder.cs`
- `Explore.Persistence/Migrations/20260707095103_AddTenantPlanGovernance.cs`
- `Explore.Persistence/Migrations/20260707095103_AddTenantPlanGovernance.Designer.cs`
- `Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs`
- `Event.Persistence.IntegrationTests/Repositories/TenantPlanPersistenceTests.cs`
- `Event.Persistence.IntegrationTests/Fixtures/PostgreSqlContainerFixture.cs`

Completed Phase 3B on 2026-07-07:

- Added red Application unit tests for tenant-plan CQRS reads and draft lifecycle before handlers existed.
- Added Control Plane tenant-plan DTOs for list, detail, version settings, version quotas, and active tenant assignment state.
- Added secured Application queries for plan list, plan detail, and tenant active assignment state using the existing Control Plane `ISecureRequest` and `[AuthorizeResource]` pattern.
- Added secured create-draft command that reuses the Phase 2A tenant-plan validator, rejects sensitive or unsupported plan payloads, and persists a draft plan/version/settings/quotas through `ITenantPlanRepository`.
- Extended `ITenantPlanRepository` and `TenantPlanRepository` with `ListWithVersionsAsync`; `GetByKeyAsync` now includes versions, statuses, settings, and quotas for detail mapping.
- Added `ControlPlaneTenantPlanMapper` outside the `Handlers.Queries` namespace after architecture tests rejected a non-handler helper in a handler namespace.
- Verified `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`; result: 2034 passed, 0 failed.
- Verified `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`; result: 259 passed, 1 skipped, 0 failed.
- Verified `dotnet build --configuration Release --verbosity quiet`; result: 0 errors, 6225 existing warnings.
- Ran scoped `aft_inspect`; result: 0 diagnostics, with the caveat that C# LSP is not installed.

Files changed in Phase 3B:

- `Event.Application.UnitTests/Features/ControlPlane/Plans/TenantPlanCqrsHandlerTests.cs`
- `Explore.Application/DTOs/ControlPlane/ControlPlaneTenantPlanDtos.cs`
- `Explore.Application/Features/ControlPlane/Requests/Queries/GetControlPlaneTenantPlanListQuery.cs`
- `Explore.Application/Features/ControlPlane/Requests/Queries/GetControlPlaneTenantPlanDetailQuery.cs`
- `Explore.Application/Features/ControlPlane/Requests/Queries/GetControlPlaneTenantPlanAssignmentQuery.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/CreateControlPlaneTenantPlanDraftCommand.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Queries/GetControlPlaneTenantPlanListQueryHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Queries/GetControlPlaneTenantPlanDetailQueryHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Queries/GetControlPlaneTenantPlanAssignmentQueryHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/CreateControlPlaneTenantPlanDraftCommandHandler.cs`
- `Explore.Application/Features/ControlPlane/ControlPlaneTenantPlanMapper.cs`
- `Explore.Application/Contracts/Persistence/ITenantPlanRepository.cs`
- `Explore.Persistence/Repositories/TenantPlanRepository.cs`

Completed Phase 3C on 2026-07-07:

- Captured the clarified product semantics in tests: tenant plans are templates; publishing a new version must let the instance admin either leave existing tenants pinned or move existing active assignments to the new version.
- Added red Application unit tests for creating a new version draft without moving assignments, publishing while leaving existing tenants pinned, publishing while moving existing tenants, and switching a single tenant to a different plan version.
- Added secured commands for creating a plan version draft, publishing a plan version with an explicit existing-tenant policy, and switching one tenant to a target plan version.
- Added `TenantPlanExistingAssignmentPolicy` with `LeaveExistingTenantsPinned` and `MoveExistingTenantsToPublishedVersion` so the UI/API can later surface the required popup choice without inventing behavior.
- Extended `ITenantPlanRepository` and `TenantPlanRepository` with explicit version and assignment methods instead of using detached aggregate graph updates.
- Added `ControlPlaneTenantPlanDraftMapper` to share normalized plan/version/settings/quota materialization between initial draft creation and later version-draft creation.
- `PublishControlPlaneTenantPlanVersionCommandHandler` publishes the target version and either leaves active tenant assignments pinned or moves active assignments to the published version based on the explicit policy.
- `SwitchControlPlaneTenantPlanAssignmentCommandHandler` supersedes the current active assignment and creates a new active assignment for the selected published plan version; it does not copy settings into `TenantSetting` yet.
- Verified `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`; result: 2038 passed, 0 failed.
- Verified `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`; result: 259 passed, 1 skipped, 0 failed.
- Verified `dotnet build --configuration Release --verbosity quiet`; result: 0 errors, 6225 existing warnings.

Files changed in Phase 3C:

- `Event.Application.UnitTests/Features/ControlPlane/Plans/TenantPlanCqrsHandlerTests.cs`
- `Explore.Application/Contracts/Persistence/ITenantPlanRepository.cs`
- `Explore.Persistence/Repositories/TenantPlanRepository.cs`
- `Explore.Application/Features/ControlPlane/ControlPlaneTenantPlanDraftMapper.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/CreateControlPlaneTenantPlanDraftCommandHandler.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/CreateControlPlaneTenantPlanVersionDraftCommand.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/PublishControlPlaneTenantPlanVersionCommand.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/SwitchControlPlaneTenantPlanAssignmentCommand.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/CreateControlPlaneTenantPlanVersionDraftCommandHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/PublishControlPlaneTenantPlanVersionCommandHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/SwitchControlPlaneTenantPlanAssignmentCommandHandler.cs`

Completed Phase 3D on 2026-07-07:

- Added red Application unit tests for updating a draft version, archiving a version, cloning a version into a new draft plan, validating drafts without persistence, previewing setting diffs without persistence, and rolling back a tenant assignment.
- Added secured commands for updating a draft plan version, archiving a plan version, cloning a plan from an existing version, and rolling back a tenant assignment.
- Added secured queries for draft validation and diff preview.
- Extended `ITenantPlanRepository` and `TenantPlanRepository` with `ReplaceVersionContentAsync` and `GetAssignmentAsync`.
- Implemented `ReplaceVersionContentAsync` as a tracked EF update that replaces child setting/quota rows explicitly instead of relying on detached graph replacement.
- Added `ControlPlaneTenantPlanDraftMapper.ApplyToVersion` so draft update, create, and clone paths share normalized setting/quota materialization.
- Kept plan application side effects out of scope: no `TenantSetting` copy, no active-assignment resolver integration, no API/HAL, and no UI were added in Phase 3D.
- Verified `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`; result: 2044 passed, 0 failed.
- Verified `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`; result: 259 passed, 1 skipped, 0 failed.
- Verified `dotnet build --configuration Release --verbosity quiet`; result: 0 errors, 6225 existing warnings.
- Ran scoped `aft_inspect`; result: 0 diagnostics, with the caveat that C# LSP is not installed. Duplicate hints were left intentionally because the command surface is still stabilizing.

Files changed in Phase 3D:

- `Event.Application.UnitTests/Features/ControlPlane/Plans/TenantPlanCqrsHandlerTests.cs`
- `Explore.Application/Contracts/Persistence/ITenantPlanRepository.cs`
- `Explore.Persistence/Repositories/TenantPlanRepository.cs`
- `Explore.Application/Features/ControlPlane/ControlPlaneTenantPlanDraftMapper.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/UpdateControlPlaneTenantPlanVersionDraftCommand.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/ArchiveControlPlaneTenantPlanVersionCommand.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/CloneControlPlaneTenantPlanCommand.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/RollbackControlPlaneTenantPlanAssignmentCommand.cs`
- `Explore.Application/Features/ControlPlane/Requests/Queries/ValidateControlPlaneTenantPlanDraftQuery.cs`
- `Explore.Application/Features/ControlPlane/Requests/Queries/PreviewControlPlaneTenantPlanDiffQuery.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/UpdateControlPlaneTenantPlanVersionDraftCommandHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/ArchiveControlPlaneTenantPlanVersionCommandHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/CloneControlPlaneTenantPlanCommandHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/RollbackControlPlaneTenantPlanAssignmentCommandHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Queries/ValidateControlPlaneTenantPlanDraftQueryHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Queries/PreviewControlPlaneTenantPlanDiffQueryHandler.cs`

Completed Phase 3E on 2026-07-07:

- Added red Application unit tests for plan application side effects, system-lock enforcement, storage-quota ceiling enforcement, and transactional tenant-setting upsert behavior.
- Chose the Phase 3E side-effect strategy: explicit plan application copies the selected plan version's settings into `TenantSetting` rows. This reuses the existing hierarchical settings resolver, tenant setting lock model, and `(TenantId, SettingKey)` uniqueness instead of teaching every resolver path to read plan assignments.
- Added `TenantSettingOverrideUpsert` and `ITenantSettingRepository.UpsertManyForTenantAsync(...)` as the idempotent persistence boundary for applying plan settings to a tenant.
- Implemented tenant setting upsert in `TenantSettingRepository` with exact-tenant predicate bypass, updating existing rows and adding missing rows in one save.
- Added secured `ApplyControlPlaneTenantPlanAssignmentCommand` and `ApplyControlPlaneTenantPlanAssignmentCommandHandler`.
- The apply handler loads the active assignment, validates tenant/status, blocks system-locked settings with `tenant_plan_setting_locked`, blocks storage quota above the configured default tenant quota ceiling with `tenant_plan_quota_ceiling_exceeded`, and writes copied tenant settings inside `IUnitOfWork.ExecuteInTransactionAsync`.
- No external side effects were added, so no outbox is required in this slice. Future plan application work that sends messages, jobs, or provider calls must add outbox/idempotency explicitly.
- Verified `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`; result: 2053 passed, 0 failed.
- Verified `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`; result: 259 passed, 1 skipped, 0 failed.
- Verified `dotnet build --configuration Release --verbosity quiet`; result: 0 errors, 6225 existing warnings.
- Ran scoped `aft_inspect`; result: 0 diagnostics, with the caveat that C# LSP is not installed.

Files changed in Phase 3E:

- `Event.Application.UnitTests/Features/ControlPlane/Plans/TenantPlanCqrsHandlerTests.cs`
- `Explore.Application/Contracts/Persistence/ITenantSettingRepository.cs`
- `Explore.Persistence/Repositories/TenantSettingRepository.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/ApplyControlPlaneTenantPlanAssignmentCommand.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/ApplyControlPlaneTenantPlanAssignmentCommandHandler.cs`

Completed Phase 4A on 2026-07-07:

- Exposed the completed Application tenant-plan surface through thin Control Plane API endpoints instead of adding new Application or persistence behavior.
- Added route constants and controller actions for plan list/detail/create draft, version draft/update/publish/archive/clone, draft validation, diff preview, tenant assignment get/switch/apply, and rollback.
- Kept plan-template endpoints available without `[RequireMultiTenant]` so self-host/default templates can exist, while tenant-specific assignment endpoints remain `[RequireMultiTenant]`.
- Added HAL affordances for create version draft, validate, preview diff, update draft, publish, archive, and clone; links carry `InstanceSettings.View` or `InstanceSettings.Update` authorization metadata and command/query `SettingKey` values.
- Registered tenant-plan DTOs and HAL wrapper schemas in the OpenAPI HAL schema catalog so collection embedded items and detail resources stay typed.
- Verified focused tenant-plan API/HAL/OpenAPI contract tests passed. The full `Event.API.IntegrationTests` suite still has unrelated gateway-timeout failures clustered in external API key/owner-type and temporal event filter tests.

Files changed in Phase 4A:

- `Explore.API/Controllers/ControlPlaneController.cs`
- `Explore.API/Hateoas/RouteNames.cs`
- `Explore.API/Hateoas/Policies/ControlPlaneTenantPlanLinkPolicy.cs`
- `Explore.API/Hateoas/Assemblers/ControlPlaneTenantPlanResourceAssembler.cs`
- `Explore.API/Extensions/HateoasAssemblerRegistration.cs`
- `Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`
- `Event.API.IntegrationTests/Features/Hateoas/ControlPlaneTenantPlanHateoasTests.cs`

Completed Phase 4B on 2026-07-07:

- Ran the governed OpenAPI/client workflow: API build refreshed/validated `schemas/openapi.json`, then Blazor client builds regenerated `EventApiClient.g.cs` artifacts through NSwag. No generated file was hand-edited.
- Added host-neutral tenant-plan result models and `IControlPlanePlanService` in the shared Control Plane RCL so later Razor components can call service contracts instead of generated clients.
- Updated `UnconfiguredControlPlaneClient` to implement `IControlPlanePlanService` and fail closed with `NotConfigured` results when a host has no real adapter.
- Registered `IControlPlanePlanService` fallback in `Event.ControlPlane.Client` and mapped the dedicated `Event.ControlPlane.Blazor` host to the real `ControlPlaneApiAdapter`.
- Implemented tenant-plan adapter methods in `Event.ControlPlane.Blazor/Services/ControlPlaneApiAdapter.cs`, mapping generated HAL/DTOs to host-neutral contracts, preserving HAL links, converting pricing values, and translating API failures to safe `ControlPlaneResult`/`ControlPlaneCommandResult` values.
- Preserved explicit existing-tenant assignment semantics in `PublishPlanVersionAsync`: existing tenants stay pinned unless the instance admin chooses `MoveExistingTenantsToPublishedVersion`.
- Verified `Event.ControlPlane.Client` build, `Event.ControlPlane.Blazor` build, `Explore.Blazor.Client` build, `Explore.Blazor.Client.Tests`, `Event.Architecture.Tests`, and canonical release build all passed with 0 errors. Remaining warnings are pre-existing package/analyzer warnings.

Files changed in Phase 4B:

- `schemas/openapi.json`
- `Event.ControlPlane.Blazor/Clients/EventApiClient.g.cs`
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- `Event.ControlPlane.Client/Contracts/ControlPlanePlanModels.cs`
- `Event.ControlPlane.Client/Services/IControlPlanePlanService.cs`
- `Event.ControlPlane.Client/Services/UnconfiguredControlPlaneClient.cs`
- `Event.ControlPlane.Client/Extensions/ServiceCollectionExtensions.cs`
- `Event.ControlPlane.Blazor/Program.cs`
- `Event.ControlPlane.Blazor/Services/ControlPlaneApiAdapter.cs`

Completed Phase 4C on 2026-07-07:

- Added a read-only tenant effective-configuration Application query that resolves all registered settings for an explicit tenant context through `IHierarchicalSettingsResolver`.
- The effective-configuration read model includes setting value source, lock source, sensitive-value masking, active plan assignment, assigned plan-version quotas, and storage quota usage from the existing tenant storage service.
- Added a thin Control Plane HAL endpoint for `GET tenants/{tenantId}/effective-configuration` with `RouteNames`, response metadata, `[RequireMultiTenant]`, and existing instance-setting view authorization metadata.
- Added HAL affordances for reading the plan assignment, switching a tenant plan, applying an assignment, and rolling back an assignment. Lock, unlock, override, and quota-update links remain pending until write contracts exist.
- Registered effective-configuration DTOs in the OpenAPI HAL schema catalog and regenerated generated clients only through the governed API/NSwag build workflow.
- Added host-neutral tenant effective-configuration result models and `IControlPlaneTenantConfigurationService` in the shared Control Plane RCL.
- Updated `UnconfiguredControlPlaneClient` to fail closed for tenant-configuration reads and mapped the dedicated `Event.ControlPlane.Blazor` host to the real `ControlPlaneApiAdapter`.
- Implemented dedicated BFF adapter mapping from generated effective-configuration HAL DTOs to host-neutral RCL contracts while preserving `_links` for later UI affordance gating.
- Verified focused tenant effective-configuration API/HAL/OpenAPI contract tests passed. The full `Event.API.IntegrationTests` suite still has unrelated gateway-timeout failures outside this surface.
- Verified `Explore.Application` build, `Event.ControlPlane.Client` build, `Event.ControlPlane.Blazor` build, `Explore.Blazor.Client` build, `Explore.Blazor.Client.Tests`, `Event.Architecture.Tests`, and canonical release build all passed with 0 errors. Remaining warnings are pre-existing package/analyzer warnings.

Files changed in Phase 4C:

- `Explore.Application/DTOs/ControlPlane/ControlPlaneTenantConfigurationDtos.cs`
- `Explore.Application/Features/ControlPlane/Requests/Queries/GetControlPlaneTenantEffectiveConfigurationQuery.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Queries/GetControlPlaneTenantEffectiveConfigurationQueryHandler.cs`
- `Explore.API/Controllers/ControlPlaneController.cs`
- `Explore.API/Hateoas/RouteNames.cs`
- `Explore.API/Hateoas/Policies/ControlPlaneTenantEffectiveConfigurationLinkPolicy.cs`
- `Explore.API/Hateoas/Assemblers/ControlPlaneTenantEffectiveConfigurationResourceAssembler.cs`
- `Explore.API/Extensions/HateoasAssemblerRegistration.cs`
- `Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`
- `Event.API.IntegrationTests/Features/Hateoas/ControlPlaneTenantEffectiveConfigurationHateoasTests.cs`
- `schemas/openapi.json`
- `Event.ControlPlane.Blazor/Clients/EventApiClient.g.cs`
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- `Event.ControlPlane.Client/Contracts/ControlPlaneTenantConfigurationModels.cs`
- `Event.ControlPlane.Client/Services/IControlPlaneTenantConfigurationService.cs`
- `Event.ControlPlane.Client/Services/UnconfiguredControlPlaneClient.cs`
- `Event.ControlPlane.Client/Extensions/ServiceCollectionExtensions.cs`
- `Event.ControlPlane.Blazor/Program.cs`
- `Event.ControlPlane.Blazor/Services/ControlPlaneApiAdapter.cs`

## CTO Decisions

- Keep API connectivity as the first implementation slice.
- Treat Tenant Plan Studio as a foundation feature, not a UI-only feature.
- Reuse hierarchical settings, typed setting documents, quota records, locks, and admin hierarchy instead of building a second governance system.
- Add an explicit plan aggregate only after a design spike proves the shape.
- Phase 2A chose a pure Application validation/diff seam first; Domain entities and persistence start in Phase 3 only after this contract remains stable.
- Phase 3A added the normalized tenant-plan persistence foundation with lookup tables and child rows instead of a single JSON blob, matching the user’s SaaS-tier and normalized-DB requirement.
- Phase 3B added the first Application CQRS contract over that persistence foundation: list/detail/version/active-assignment reads and create-draft writes.
- Phase 3C encoded tenant plans as templates with explicit version-publishing semantics: existing tenant assignments stay pinned unless the instance admin explicitly chooses to move them.
- Phase 3C added individual tenant plan switching as an Application command, but intentionally did not copy plan settings into tenant settings yet.
- Phase 3D added remaining safe template commands for update draft, archive, clone, validate, preview diff, and rollback assignment, still without applying plan values into tenant settings.
- Phase 3E chose explicit copy-to-`TenantSetting` for plan application. Active plan assignments remain the record of which plan/version a tenant uses, while effective runtime settings continue to flow through the existing hierarchical setting resolver.
- Phase 3E added transactional, idempotent tenant-setting upsert for plan application and blocked system-locked settings plus storage quotas above the configured instance ceiling before any write occurs.
- Phase 4A exposed the existing Application tenant-plan surface through thin Control Plane endpoints and HAL affordances rather than adding new business behavior in the API layer.
- Phase 4A kept HAL `_links` as the UI action source: generated clients and RCL services carry affordances forward, and Razor components must still gate actions by links instead of local roles or claims.
- Phase 4B updated generated clients only through the OpenAPI/NSwag build workflow and kept generated files hand-edit-free.
- Phase 4B added a host-neutral `IControlPlanePlanService` and dedicated BFF adapter mapping so later Plan Studio components can stay transport-agnostic and receive safe failure results.
- Phase 4B intentionally left the embedded `Explore.Blazor.Client` adapter on fallback for plan services until an embedded-host UI needs the same service surface.
- Phase 4C made tenant effective configuration read-only first: it reuses the hierarchical resolver, tenant-plan assignment repository, and tenant storage setting service instead of introducing a second settings/quota model.
- Phase 4C masks sensitive setting values in the Control Plane effective-configuration read model while still exposing source, lock, type, and category metadata.
- Phase 4C carries tenant-configuration HAL links through generated clients and the dedicated BFF adapter so later UI can gate assignment actions by `_links`; lock/unlock/override/quota-update actions wait for explicit write contracts.
- Phase 4D added new Control Plane-scoped commands with explicit `Guid tenantId` because existing `SettingsController` commands resolve tenant from `ITenantContext` (current authenticated tenant), making them unsuitable for instance-admin actions on arbitrary tenants.
- Phase 4D lock/unlock commands are idempotent (success whether applied or already in target state). Set command preserves existing `IsLocked` state via read-before-write to avoid silently dropping a tenant lock.
- Phase 4D HAL affordances expose per-setting `override`/`lock`/`unlock` links only for non-sensitive settings. Sensitive settings get no action links — secrets cannot be overridden through HAL affordances.
- Phase 4D deferred quota update because storage quota comes from `IStoragePolicyResolver`, not tenant settings.
- Require preview/diff/apply/rollback/audit for plan assignment.
- Split implementation by risk boundary: connectivity, data/model, Application/API/HAL, RCL UI, operations/docs/tests.

## Open Questions

Phase 1A answered with runtime evidence:

- Generated client paths and API route prefixes align for overview, domains, operations, tenants, and deployment-mode runbook.
- The first proven root cause was base-address precedence: dedicated Control Plane compatibility mapping let generic `API_ENDPOINT` override Aspire `explore-api` service discovery by setting `ExploreApi:BaseUrl`.

Phase 1B answered with host-level evidence:

- The dedicated host can be booted through `WebApplicationFactory<Program>` with an explicit partial `Program` seam.
- The dedicated host overrides the RCL fallback services with `ControlPlaneApiAdapter` for overview, tenants, domains, and operations/runbook service interfaces.
- No generated-client/API contract drift was proven in Phase 1, so `EventApiClient.g.cs` was not regenerated.
- The overview page now uses failure copy that covers both API reachability and missing-adapter cases.

Remaining Phase 1 follow-up only if runtime evidence reopens it:

- If a deployed host still returns 404 after the Aspire base-address fix, capture the actual request URL/host and response body before changing API version/header behavior.

Tenant plan design must answer before code:

- Phase 2A answered the first seam: plan drafts are validated Application records that reference registered non-sensitive settings and supported quota keys.
- Phase 2A supported quota keys are storage bytes, AI daily tenant messages, external API monthly credits, and custom-property definitions per template. Email dispatch remains future until enforcement exists.
- Phase 3A answered the persistence shape: tenant plans are normalized persisted aggregates with version, setting, quota, assignment, and application-log tables plus lookup statuses.
- Phase 3B answered the first CQRS seam: list/detail/version/active-assignment reads and create-draft writes exist in Application, but plan application side effects are intentionally not implemented yet.
- Phase 3C answered template update semantics: publishing a new plan version can either leave existing tenant assignments pinned or move active assignments to the new version based on an explicit instance-admin policy.
- Phase 3C answered individual switching semantics: instance admins can supersede one tenant's current active assignment and create a new active assignment for a selected published plan version.
- Phase 3D answered draft update, archive, clone, validate, preview-diff, and rollback-assignment command semantics.
- Phase 3E answered side-effect strategy: applying a plan copies selected plan version settings into `TenantSetting` rows, while assignments keep plan/version provenance.
- Phase 3E added the first lock/quota enforcement: system-locked settings block plan application and storage quota limits cannot exceed the configured default tenant quota ceiling.
- Phase 4A answered API/HAL exposure for the completed plan surface: Control Plane now has plan, version, validation, diff, and assignment endpoints with route names and HAL metadata.
- Phase 4B answered generated-client and RCL service-seam exposure for tenant plans: generated clients include the new endpoints and the dedicated Control Plane host maps them through `IControlPlanePlanService`.
- Phase 4C answered read-only tenant effective-configuration shape: each response includes resolved setting values, value source, lock source, sensitive-value masking, active plan assignment, assigned plan quota limits, storage usage, and HAL assignment affordances.
- Phase 4C answered generated-client and RCL service-seam exposure for tenant effective configuration: generated clients include `GetControlPlaneTenantEffectiveConfigurationAsync` and the dedicated Control Plane host maps it through `IControlPlaneTenantConfigurationService`.
- Later slices can refine quota ceilings per domain and add richer lock-loosening checks as more quota/lock domains become first-class.
- A later Phase 3 slice must decide which plan changes require typed confirmation at API/UI boundaries.
- Phase 3A chose dedicated `TenantPlanApplicationLog` storage for plan-application audit history.
- Remaining Phase 4 work must answer tenant-configuration write actions for lock, unlock, override, and quota update, plus policy coverage for those actions.

## Working Assumptions

- v1.0 should complete the existing Control Plane architecture and extend it into a governance platform.
- The dedicated host should show precise API/config/remediation errors, not generic adapter-not-configured copy.
- Placeholder routes can be replaced without backward-compatibility ceremony because the project is in development.
- Existing persisted tenants, settings, and quota data still require safe migration behavior.
- All destructive, lock-changing, quota-changing, and plan-assignment actions require typed confirmation and server-side authorization.
- Onboarding should be implemented after health/storage/security/policies and plan readiness because it aggregates those statuses.

## Verification Commands

Use these project-level commands as slices require:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## Handoff Guidance

Phase 1 is complete. The dedicated host now preserves Aspire API service discovery over generic `API_ENDPOINT`, has a host-registration regression proving real adapter wiring, and has less misleading overview failure copy.

Phase 2A is complete. Tenant plans now have a tested Application-layer SaaS tier validation/diff seam. The next slice should be Phase 3: persistence and Application CQRS for versioned tenant plans, plan assignments, and plan application logs. Do not build Plan Studio UI before the persistence/API/HAL contract exists.

Phase 3A is complete. Tenant plans now have normalized Domain entities, lookup statuses, EF Core configurations, repository boundary, runtime lookup seeding, migration, and persistence tests. The next slice should be Phase 3B: Application CQRS for plan list/detail/version history, draft creation/update, publish/archive/clone, assignment preview, assignment, and rollback. Do not build API/HAL or Plan Studio UI before CQRS contracts are in place.

Phase 3B is complete for the first CQRS seam. Application now supports plan list/detail/active-assignment reads and create-draft writes. The next slice should remain in Phase 3: add update draft, publish, archive, clone, validate/preview diff, assign, and rollback commands, including quota-ceiling checks and assignment side-effect semantics. Do not build API/HAL or Plan Studio UI before those Application contracts exist.

Phase 3C is complete for plan-template versioning and switching semantics. Application now supports creating a new plan version draft, publishing a plan version with an explicit leave-existing-vs-move-existing policy, and switching a tenant to a selected published version. The next slice should remain in Phase 3: add update draft, archive, clone, validate/preview diff, rollback, quota-ceiling checks, and the actual plan-application side-effect strategy. Do not build API/HAL or Plan Studio UI before those Application contracts exist.

Phase 3D is complete for safe template maintenance commands. Application now supports updating draft version content, archiving versions, cloning versions into new draft plans, validating drafts, previewing diffs, and rolling back assignments. The next slice should remain in Phase 3: enforce quota ceilings and lock-loosening rules, then decide and implement the actual plan-application side-effect strategy with idempotency/outbox if it writes multiple tenant settings. Do not build API/HAL or Plan Studio UI before that Application behavior exists.

Phase 3E is complete for the first plan-application side-effect path. Application now copies selected plan version settings into tenant settings only when an instance admin explicitly applies an assignment, and it blocks system-locked settings plus over-ceiling storage quotas before writing. The next slice can move to Phase 4 API/HAL/OpenAPI/adapter contracts for the completed Application surface, or stay in Phase 3 only if more quota domains must be enforced before API exposure. Do not build Plan Studio UI before API/HAL and RCL service contracts exist.

Phase 4A is complete for tenant-plan API/HAL/OpenAPI contracts. Control Plane now exposes thin endpoints and HAL affordances for plan templates, version lifecycle, validation, diff preview, and assignment apply/rollback over the completed Application surface. Focused tenant-plan API/HAL/OpenAPI tests are green; full API integration remains blocked by unrelated gateway-timeout failures outside this surface.

Phase 4B is complete for the generated-client and dedicated-host RCL service seam. Generated clients include tenant-plan methods, `Event.ControlPlane.Client` has host-neutral plan contracts and fail-closed fallback behavior, and `Event.ControlPlane.Blazor` maps those operations through `ControlPlaneApiAdapter` while preserving HAL links and safe error translation.

Phase 4C is complete for the read-only tenant effective-configuration API/HAL/generated-client/RCL service seam. Control Plane can now read one tenant's resolved settings, value and lock sources, active plan assignment, quota limits, storage usage, and assignment HAL affordances through `IControlPlaneTenantConfigurationService`.

Phase 4D is complete for tenant-configuration write contracts (lock/unlock/override). Application now has explicit-tenantId commands (`LockControlPlaneTenantSettingCommand`, `UnlockControlPlaneTenantSettingCommand`, `SetControlPlaneTenantSettingCommand`) because existing `SettingsController` commands resolve tenant from `ITenantContext` (current authenticated tenant) and are unsuitable for instance-admin actions on arbitrary tenants. Lock/unlock are idempotent; set preserves existing `IsLocked` state via read-before-write. HAL affordances on the effective-configuration resource now expose per-setting `override` (PUT), `lock` (POST), or `unlock` (DELETE) links for non-sensitive settings, gated by `InstanceSettings.Update`. Sensitive settings get no action links. Generated clients were regenerated through the build workflow. `IControlPlaneTenantConfigurationService` was extended with write methods; `UnconfiguredControlPlaneClient` fails closed; dedicated `ControlPlaneApiAdapter` maps generated client methods through existing `SendTenantPlanCommandAsync` with safe error translation. Quota update was deferred because storage quota comes from `IStoragePolicyResolver`, not tenant settings.

Files changed in Phase 4D:
- `Explore.Application/Features/ControlPlane/Requests/Commands/LockControlPlaneTenantSettingCommand.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/UnlockControlPlaneTenantSettingCommand.cs`
- `Explore.Application/Features/ControlPlane/Requests/Commands/SetControlPlaneTenantSettingCommand.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/LockControlPlaneTenantSettingCommandHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/UnlockControlPlaneTenantSettingCommandHandler.cs`
- `Explore.Application/Features/ControlPlane/Handlers/Commands/SetControlPlaneTenantSettingCommandHandler.cs`
- `Explore.API/Hateoas/RouteNames.cs`
- `Explore.API/Hateoas/Policies/ControlPlaneTenantEffectiveConfigurationLinkPolicy.cs`
- `Explore.API/Controllers/ControlPlaneController.cs`
- `Event.API.IntegrationTests/Features/Hateoas/ControlPlaneTenantEffectiveConfigurationHateoasTests.cs`
- `schemas/openapi.json`
- `Event.ControlPlane.Blazor/Clients/EventApiClient.g.cs`
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- `Event.ControlPlane.Client/Services/IControlPlaneTenantConfigurationService.cs`
- `Event.ControlPlane.Client/Services/UnconfiguredControlPlaneClient.cs`
- `Event.ControlPlane.Blazor/Services/ControlPlaneApiAdapter.cs`

The next slice should stay in Phase 4: add Cerbos/local policy coverage for plan and tenant-configuration resources if required, or explicitly defer it. Plan Studio UI remains blocked until explicitly started.

Phase 4E is complete for Cerbos policy test coverage. Control Plane setting keys (`control-plane.tenant-plans`, `control-plane.tenant-plan-assignments`, `control-plane.tenant-effective-configuration`) inherit the existing `islamuevent_instance_setting` policy; test cases were added to `islamuevent_instance_setting_test.yaml` proving instance admin allow and tenant admin/regular user view-only deny. Phase 4 is now fully complete.

Phase 5A is complete for Plan Studio read surfaces and Tenant Configuration center. The Plan Studio list page (`/admin/instance/plans`) shows plan inventory with pricing, provisioning status, version numbers, and HAL-gated create affordance. The Plan Detail page (`/admin/instance/plans/{Key}`) shows all versions with per-version settings/quotas tables, status badges (Draft/Published/Archived), and HAL-gated lifecycle actions: publish (Draft→Published), archive (Published→Archived), clone (Published→new plan with key/name confirmation form), and create version draft. The Tenant Configuration page (`/admin/instance/tenants/{TenantId}/configuration`) shows plan assignment facts, grouped effective settings by category with value/lock-source/source display, quota usage with meter bars, sensitive-value masking (••••••••), and HAL-gated per-setting actions: override (inline edit with input+save), lock, and unlock. All actions are gated by `_links` presence only, never local role inspection. Sensitive settings get no action links. 7 new bUnit tests cover fail-closed state, HAL-gated rendering, publish flow, lock flow, and override flow.

Files changed in Phase 5A:
- `Event.ControlPlane.Client/Pages/Plans/ControlPlanePlansPage.razor` (fixed broken `@key` syntax from prior session)
- `Event.ControlPlane.Client/Pages/Plans/ControlPlanePlanDetailPage.razor` (new)
- `Event.ControlPlane.Client/Pages/Plans/ControlPlanePlanDetailPage.razor.css` (new)
- `Event.ControlPlane.Client/Pages/TenantConfiguration/ControlPlaneTenantConfigurationPage.razor` (new)
- `Event.ControlPlane.Client/Pages/TenantConfiguration/ControlPlaneTenantConfigurationPage.razor.css` (new)
- `Explore.Blazor.Client.Tests/Pages/Admin/ControlPlanePlanDetailAndTenantConfigPageTests.cs` (new, 7 tests)

The next slice should continue Phase 5: build the Plan Editor (create/edit draft forms with domain-grouped settings, validation error display), implement plan assignment diff preview/apply/rollback UI, add `admin_portal.*` instance settings, and verify shared layout nav rendering by authority. Manual browser QA remains required after the editor lands.

Keep this file updated with:

- Slices completed.
- Files changed.
- Test commands and results.
- New decisions.
- New risks.
- Any findings that belong in `dev/_journal/journal.md`.
