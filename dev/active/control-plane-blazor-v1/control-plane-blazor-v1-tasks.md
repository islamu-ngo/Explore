<!-- ABOUTME: Actionable task checklist for implementing Control Plane Blazor v1.0 as a governance platform. -->
<!-- ABOUTME: Breaks the work into verifiable slices with target files, risk boundaries, and project-level test gates. -->

# Control Plane Blazor v1.0 Tasks

Last Updated: 2026-07-08 Europe/Brussels

## How To Use

Execute tasks in order unless a runtime finding proves a dependency is wrong. Mark checkboxes only after code and verification are complete. Update `control-plane-blazor-v1-context.md` after every implementation session.

Implementation has started. Phase 1 connectivity, Phase 2A SaaS pricing-tier model tests, Phase 3A normalized persistence, Phase 3B CQRS reads/create-draft, Phase 3C template versioning/switching commands, Phase 3D template maintenance commands, Phase 3E plan-application side effects, and the Phase 4 tenant-plan API/HAL/generated-client/RCL plan service seam, tenant effective-configuration read seam, tenant-configuration lock/unlock/override write contracts, and cerbos policy test coverage are complete. Phase 4 is fully complete. Phase 5A (Plan Studio list/detail read surfaces + Tenant Configuration center with HAL-gated override/lock/unlock controls + 7 bUnit tests) is complete; the next slice is Phase 5B (Plan Editor draft forms, assignment diff/apply/rollback UI, admin_portal settings, layout nav).

## Phase 1: Prove And Fix Dedicated Host API Connectivity

- [x] Phase 1A baseline: `dotnet build --configuration Release --verbosity quiet` passed before edits with 0 errors and 56 existing warnings.
- [x] `Explore.Blazor.IntegrationTests/Extensions/ControlPlaneConfigurationExtensionsTests.cs`: Add a failing regression proving generic `API_ENDPOINT` must not override Aspire `services:explore-api` discovery in `Event.ControlPlane.Blazor`.
- [x] `Event.ControlPlane.Blazor/Extensions/ConfigurationExtensions.cs`: Preserve Aspire `explore-api` service discovery precedence over generic Infisical `API_ENDPOINT` when mapping `ExploreApi:BaseUrl`.
- [x] Verification: `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passed with 225 succeeded, 0 failed.
- [x] Verification: `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and 56 existing warnings.
- [x] `Explore.Blazor.IntegrationTests/Services/ControlPlaneHostRegistrationTests.cs`: Add host-level regression proving `Event.ControlPlane.Blazor` resolves overview, tenants, domains, and operations/runbook service interfaces to the real `ControlPlaneApiAdapter` instead of `UnconfiguredControlPlaneClient`.
- [x] `Event.ControlPlane.Blazor/Services/ControlPlaneApiAdapter.cs`: Trace generated `IEventApiClient` calls for `api_version`, `x_Api_Version`, media type, path, and base-address behavior.
- [x] `Event.ControlPlane.Blazor/Program.cs`: Verify typed client base address, bearer forwarding, `UseCookies=false`, proxy route protection, and dedicated adapter service registration for Control Plane host mode.
- [x] `Explore.API/Controllers/ControlPlaneController.cs`: Confirm route constants and generated client paths match the actual admin endpoints.
- [x] `Event.ControlPlane.Blazor/Clients/EventApiClient.g.cs`: Leave generated client unchanged because Phase 1 found no OpenAPI route/version contract drift.
- [x] `Event.ControlPlane.Client/Services/UnconfiguredControlPlaneClient.cs`: Keep fallback fail-closed behavior for unknown hosts; dedicated host regression proves it does not leak into the dedicated host service graph.
- [x] `Event.ControlPlane.Client/Pages/Overview/ControlPlaneOverviewPage.razor`: Replace dedicated-host false fail-closed copy with API-reachability-or-adapter remediation text.
- [x] Verification: `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passed with 226 succeeded, 0 failed.
- [x] Verification: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` not required in Phase 1 because no API contract changed.
- [x] Verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 259 succeeded, 1 skipped, 0 failed.
- [x] Verification: `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and 56 existing warnings.

## Phase 2: Tenant Governance Inventory And SaaS Tier Model Design

- [x] `docs/MULTI_TENANCY.md`, `docs/ADMIN_HIERARCHY.md`, `docs/STORAGE.md`, `docs/API.md`: Re-read relevant lock, settings, quota, admin-hierarchy, and API sections before design edits.
- [x] `Explore.API/Controllers/SettingsController.cs`: Inventory tenant settings read/update/lock/unlock endpoints and RouteNames.
- [x] `Explore.API/Controllers/InstanceSettingsController.cs`: Inventory instance governance endpoints for modules, branding, domains, tenant delegation, AI, storage, auth, authorization, and related settings.
- [x] `Explore.Application`, `Explore.Domain`, and `Explore.Infrastructure`: Inventory existing typed settings, `GovernanceSettingKeys`, `SettingRegistry`, sensitive setting metadata, quota services, storage policy resolver, external API key quota defaults, and quota ProblemDetails contracts.
- [x] Design decision: model tenant plans as SaaS pricing tiers with stable key, display name, price amount, currency, billing period, active-for-provisioning flag, and version semantics. Recorded in `control-plane-blazor-v1-context.md` and `control-plane-blazor-v1-plan.md`.
- [x] Design decision: keep Phase 2 as a pure Application validation/diff seam. No Domain entities, persistence, migrations, API endpoints, or Plan Studio UI were added in Phase 2A.
- [x] Design decision: define v1.0 supported quota domains for the first seam: storage bytes, AI tenant daily messages, external API monthly credits, and custom-property definitions per template. Email sending quota remains future until enforcement exists.
- [x] Design decision: define plan update semantics. Updating a published plan creates a new version; existing tenant assignments move only through explicit preview/apply/rollback, never silently.
- [x] Design decision: define provisioning semantics. Tenant provisioning APIs should accept a plan key/version and apply registered non-sensitive settings, locks, and supported quotas server-side.
- [x] `Event.Application.UnitTests/Features/ControlPlane/Plans/TenantPlanDraftValidatorTests.cs`: Add failing unit tests for pricing metadata, unsupported setting keys, sensitive setting rejection, quota validation, and diff generation before implementation.
- [x] `Explore.Application/Features/ControlPlane/Plans/TenantPlanModels.cs`: Add pure Application records, validator, supported quota keys, validation errors, and diff service for the first SaaS tier seam.
- [x] Verification: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed with 2029 succeeded, 0 failed.
- [x] Verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 259 succeeded, 1 skipped, 0 failed.
- [x] Verification: `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and 6221 existing warnings.

## Phase 3: Implement Tenant Plan Persistence And Application Layer

- [x] Phase 3A red test: `Event.Persistence.IntegrationTests/Repositories/TenantPlanPersistenceTests.cs` proved the missing schema with lookup seeding, normalized settings/quotas, duplicate constraints, and one-active-assignment-per-tenant coverage.
- [x] `Explore.Domain`: Add normalized lookup entities and enums for plan status, assignment status, and application status.
- [x] `Explore.Domain`: Add `TenantPlan`, `TenantPlanVersion`, `TenantPlanVersionSetting`, `TenantPlanVersionQuota`, `TenantPlanAssignment`, and `TenantPlanApplicationLog`.
- [x] `Explore.Application`: Add `ITenantPlanRepository` as the entity-returning repository boundary for plan key, version, and active assignment reads.
- [x] `Explore.Persistence`: Add DbSets, EF configurations, repository implementation, DI registration, lookup seeding, Respawn lookup preservation, unique constraints, and focused migration `20260707095103_AddTenantPlanGovernance`.
- [x] Persistence normalization: Use child rows for version settings and quotas instead of one opaque plan JSON blob.
- [x] Audit storage: Add `TenantPlanApplicationLog` with actor, tenant, plan/version, previous version, changed setting/quota key JSON, failure reason, and application status.
- [x] Verification: `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed with 250 succeeded, 0 failed.
- [x] Verification: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed with 2029 succeeded, 0 failed.
- [x] Verification: `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passed with 317 succeeded, 0 failed.
- [x] Verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 259 succeeded, 1 skipped, 0 failed.
- [x] Verification: `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and 5965 existing warnings.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add CQRS queries for plan list/detail/version history and tenant assignment state.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add create-draft command using the normalized tenant-plan repository boundary.
- [x] Validation: Ensure the create-draft command reuses the Phase 2A validator so plan payloads cannot contain secrets, raw provider credentials, tenant business data selectors, or unsupported setting keys.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add command for creating a new draft version of an existing plan without moving existing tenant assignments.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add command for publishing a plan version with explicit leave-existing-tenants-pinned vs move-existing-tenants policy.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add command for switching one tenant to a selected published plan version while superseding the previous active assignment.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add command for updating a draft version's pricing, settings, quotas, and provisioning metadata.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add command for archiving a plan version and disabling provisioning.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add command for cloning a plan version into a new draft plan.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add query for validating a draft without persistence.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add query for previewing setting diffs without persistence.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add command for rolling back a tenant assignment to a previous assignment.
- [x] `Explore.Application/Features/ControlPlane/Plans`: Add plan-application side effects by explicitly copying selected plan version settings into `TenantSetting` rows when an instance admin applies an assignment.
- [x] Validation: Enforce the first quota/lock guards before writes: block system-locked settings and block storage quota limits above the configured default tenant quota ceiling.
- [x] Assignment semantics: Preserve existing tenants on their assigned version unless instance admin explicitly chooses to move them while publishing a new version.
- [x] Assignment semantics: Allow instance admin to switch an individual tenant to another published plan version.
- [x] Assignment semantics: Applying a plan copies selected plan version settings into `TenantSetting`; active assignment remains the plan/version provenance record and runtime settings continue through the existing hierarchical resolver.
- [x] Outbox/idempotency: No outbox is required for Phase 3E because apply performs local tenant-setting upserts inside a database transaction and triggers no external side effects. Future external jobs/messages still require outbox.
- [x] Verification: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed with 2053 succeeded, 0 failed.
- [x] Verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 259 succeeded, 1 skipped, 0 failed.
- [x] Verification: `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and 6225 existing warnings.

## Phase 4: Add Tenant Plan API, HAL, OpenAPI, And Adapter Contracts

- [x] `Explore.API`: Add tenant-plan endpoints under the Control Plane admin surface using thin controllers and `RouteNames`.
- [x] `Explore.API`: Add tenant-plan validation, diff preview, assignment switch/apply, and rollback endpoints for the completed Application plan surface.
- [x] `Explore.API/Hateoas`: Add HAL links for tenant-plan create draft, create version draft, edit draft, publish, archive, clone, validate, and preview diff. Fail closed.
- [x] `Explore.API`: Add per-tenant effective configuration endpoint with value source, lock source, plan assignment, and quota usage.
- [x] `Explore.API/Hateoas`: Add tenant-configuration HAL links for plan assignment read, switch-plan, apply, and rollback. Fail closed.
- [x] `Explore.API/Hateoas`: Add tenant-configuration HAL links for lock, unlock, and override. Quota update deferred (storage quota from `IStoragePolicyResolver`, not tenant settings). Fail closed.
- [x] `Explore.Application`: Add authorization metadata for plan and tenant effective-configuration resources. Instance admin only by default; tenant admin views only unlocked tenant-scoped settings if later exposed.
- [x] `cerbos/tests`: Add plan/tenant-configuration policy coverage. Control Plane setting keys (`control-plane.tenant-plans`, `control-plane.tenant-plan-assignments`, `control-plane.tenant-effective-configuration`) inherit the existing `islamuevent_instance_setting` policy; test cases added to `islamuevent_instance_setting_test.yaml` proving instance admin allow and tenant admin/regular user view-only deny.
- [x] `Event.ControlPlane.Blazor/Clients/EventApiClient.g.cs`: Regenerate/update through the established OpenAPI workflow.
- [x] `Event.ControlPlane.Blazor/Services/ControlPlaneApiAdapter.cs`: Map generated tenant-plan, diff, and assignment calls to RCL service contracts.
- [x] `Event.ControlPlane.Client/Services`: Add host-neutral tenant-plan service interface and result models.
- [x] `Event.ControlPlane.Client/Services`: Add host-neutral tenant effective-configuration service interface and result models.
- [x] `Event.ControlPlane.Client/Services`: Add host-neutral tenant-configuration write service contracts (lock/unlock/override) on `IControlPlaneTenantConfigurationService`.
- [x] Verification: focused tenant-configuration write `Event.API.IntegrationTests` HAL/OpenAPI contract tests passed (4/4).
- [x] Verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-restore --verbosity quiet` passed with 259 succeeded, 1 skipped, 0 failed.
- [x] Verification: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore --verbosity quiet` passed with 1550 succeeded, 1 skipped, 0 failed.
- [x] Verification: `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and existing warnings.

## Phase 5: Build Plan Studio And Per-Tenant Configuration Center

Product decision: The separate app is now conceptually **AdminPortal** (see `dev/active/controlplane.md`). It is a single Blazor app serving both instance admins and tenant admins (if instance admin allows). "Two shells" means route-based authorization gating within the same app — `/instance/...` for instance admins, `/tenant/{slug}/...` for tenant admins. One `App.razor`, one layout, one shared component/service library, one BFF. Nav menu renders different sections based on authority. No duplicated projects or layouts. For v1.0, Phase 5 builds the Instance Console UI under `/instance/...`. Tenant Console routes are Phase 5b. Code project names (`Event.ControlPlane.Blazor`, `Event.ControlPlane.Client`) remain until a rename PR.

- [x] `Event.ControlPlane.Client/Routing/ControlPlaneRoutes.cs`: Route map already exists with `/admin/instance/...` prefix for instance-admin pages; `/tenant/{slug}/...` prefix reserved for future tenant-admin pages.
- [~] `Event.ControlPlane.Client/Pages/Plans`: Plan Studio list and detail pages built under the instance route group. List page shows plan inventory with HAL-gated create affordance; detail page shows versions with settings/quotas, HAL-gated publish/archive/clone actions, and inline draft-version editing. **Create-plan form remains deferred.**
- [x] Plan editor: Group settings by product domain: modules, storage, email, API keys, AI, MCP, rendering, footer, branding, domains, moderation/reporting, auth, authorization, onboarding defaults.
- [x] Plan editor: Show validation errors for unsupported keys, secret-like values, quota ceiling violations, and lock conflicts. Draft editor surfaces API validation errors and local parse errors before save.
- [x] Plan versioning: Implement publish/archive/clone affordances only when HAL links exist. Detail page gates publish (Draft→Published), archive (Published→Archived), and clone (Published→new plan) by version status.
- [~] Plan assignment: Implement diff preview, typed confirmation, apply result, and rollback where HAL links exist. Apply/rollback typed confirmation and result reload are implemented; target-version diff preview remains pending because the API currently exposes draft-vs-current preview rather than assignment-specific diff.
- [x] `Event.ControlPlane.Client/Pages/TenantConfiguration`: Build effective configuration view with source, lock source, assigned plan, usage, and audit history. Page shows plan assignment, grouped effective settings with value/lock/source, quota usage bars, and HAL-gated per-setting actions.
- [x] Tenant configuration: Add override/lock/unlock controls only when HAL links exist. Override uses inline edit (input+save), lock/unlock are one-click with busy state and reload.
- [x] Instance settings: Add `admin_portal.enabled`, `admin_portal.public_url`, `admin_portal.allow_tenant_admin_access` settings so instance admin can control whether tenant admins may use the AdminPortal in a future phase. Settings are registered in the governance registry, exposed through the instance governance aggregate, and available through `GET/PUT /api/instance/settings/admin-portal`.
- [~] Shared layout: One `App.razor`, one nav layout. Instance Console nav now renders from an explicit top-level route catalog (detail routes stay routable but are excluded from the sidebar), and authenticated-but-unauthorized users see a 403-style shell state. Tenant Console `/tenant/{slug}` authority-aware nav remains deferred to Phase 5b.
- [x] Accessibility: Ensure labels, keyboard operation, one `<h1>`, sequential headings, live command results, and focus management. Control Plane shell now has a skip link, named instance nav, main landmark/focus target, polite/assertive live regions, logical host CSS, success/error command-result roles, and no nested `<main>` in the authenticated forbidden state.
- [x] Verification: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed with 1557 succeeded, 1 skipped, 0 failed (7 new bUnit tests for Plan Detail + Tenant Configuration pages).
- [x] Verification: `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passed with 226 succeeded, 0 failed.
- [x] Verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 259 succeeded, 1 skipped, 0 failed.
- [x] Verification: `dotnet build --configuration Release --verbosity quiet` passed with 0 errors.
- [x] Verification: `./Explore.Blazor.Client.Tests --treenode-filter "/*/*/*/Navigation_ShouldExposeOnlyTopLevelInstanceRoutes" --minimum-expected-tests 1 --disable-logo --no-progress --no-ansi` passed with 1 succeeded, 0 failed.
- [x] Verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 262 succeeded, 1 skipped, 0 failed after adding Control Plane accessibility shell checks.
- [x] Verification: `./Explore.Blazor.Client.Tests --disable-logo --no-progress --no-ansi` passed with 1563 succeeded, 1 skipped, 0 failed after live-region/command-status accessibility fixes.
- [ ] Verification: Manual browser QA for Plan Studio and Tenant Configuration as instance admin and non-admin.

## Phase 5b (Future): Tenant Console Routes In AdminPortal

Prerequisite: instance settings `admin_portal.allow_tenant_admin_access = true`. Same Blazor app, same BFF, same component library — just new route group and nav sections.

- [ ] Rename projects from `Event.ControlPlane.Blazor` / `Event.ControlPlane.Client` to `Event.AdminPortal.Blazor` / `Event.AdminPortal.Client`.
- [x] Add Tenant Console routes under `/tenant/{tenantSlug}/...` in the same app. Same layout, same service registrations, same BFF. Placeholder routes now cover overview, settings, branding, moderation, users, footer/navigation, reports, events, and policies.
- [~] Gate `/tenant/...` routes on instance setting + tenant admin authority + HAL links. Instance-admin shell policy still protects the host; tenant-specific setting/authority/HAL gating is pending the service-backed tenant console pages.
- [ ] Build tenant-scoped pages: tenant settings, branding, moderation queue, users/members, footer/navigation, reports, events/policies.
- [~] Nav menu: render `/instance/...` sections for instance admins, `/tenant/{slug}/...` sections for tenant admins, based on authority. The shared layout now renders tenant route templates when the current route is under `/tenant/{slug}`; authority-based visibility remains pending with tenant admin gating.
- [ ] Dynamic link from main web app tenant admin navbar to `admin_portal.public_url + "/tenant/{tenantSlug}"`.
- [ ] Verification: Manual browser QA as tenant admin with and without instance setting enabled.
- [x] Verification: `./Explore.Blazor.Client.Tests --treenode-filter "/*/*/*/TenantNavigation_ShouldExposeTenantRouteTemplatesOnly" --minimum-expected-tests 1 --disable-logo --no-progress --no-ansi` passed with 1 succeeded, 0 failed.
- [x] Verification: `dotnet build --configuration Release --verbosity quiet` passed with 0 errors after the Phase 5b tenant route scaffold.
- [x] Verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed with 262 succeeded, 1 skipped, 0 failed after allowing the approved `/tenant/{TenantSlug}` AdminPortal root.
- [x] Verification: `./Explore.Blazor.Client.Tests --treenode-filter "/*/*/*/Navigation_ShouldExposeOnlyTopLevelInstanceRoutes" --minimum-expected-tests 1 --disable-logo --no-progress --no-ansi` passed with 1 succeeded, 0 failed.

## Phase 6: Complete Existing Overview, Tenants, Domains, And Operations Pages

- [ ] `Event.ControlPlane.Client/Pages/Overview/ControlPlaneOverviewPage.razor`: Show real overview summaries, warnings, and remediation links to plans, tenants, domains, health, storage, security, and policies.
- [ ] `Event.ControlPlane.Client/Pages/Tenants/ControlPlaneTenantsPage.razor`: Enable create tenant when HAL create link exists.
- [ ] Tenant create wizard: Include plan selection and preview resulting locks/quotas/default settings before create.
- [ ] Tenant lifecycle: Ensure activate/suspend/archive/reactivate/purge commands keep audit, typed confirmations, and HAL gating.
- [ ] `Event.ControlPlane.Client/Pages/Domains/ControlPlaneDomainsPage.razor`: Replace disabled Verify/Test/Retry buttons with HAL-gated command-backed controls.
- [ ] `Explore.Application/Features/ControlPlane`: Add domain verify/test/retry commands and handlers if no reusable backend already exists.
- [ ] `Explore.API/Controllers/ControlPlaneController.cs`: Add domain action endpoints with `RouteNames`, response metadata, rate limits, timeout policy, and resource authorization.
- [ ] `Event.ControlPlane.Client/Pages/Operations/ControlPlaneOperationsPage.razor`: Separate runbook unavailable, API unavailable, unauthorized, degraded, locked, and quota-exceeded states.
- [ ] Verification: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet build --configuration Release --verbosity quiet`.

## Phase 7: Replace Health, Storage, Jobs, Security, And Policies Placeholders

- [ ] `Event.ControlPlane.Client/Services`: Add `IControlPlaneHealthService` and result contracts.
- [ ] `Explore.Application/Features/ControlPlane`: Add redacted health summary query using readiness, API, database, auth, authorization, storage, email, outbox, and federation signals.
- [ ] `Event.ControlPlane.Client/Pages/Health/ControlPlaneHealthPage.razor`: Build a real health dashboard with accessible status cards and remediation copy.
- [ ] `Event.ControlPlane.Client/Services`: Add `IControlPlaneStorageService` and result contracts reusing existing storage settings/quota semantics.
- [ ] `Event.ControlPlane.Client/Pages/Storage/ControlPlaneStoragePage.razor`: Build storage diagnostics, provider state, quota usage, delegation lock, and safe test/recalculate actions.
- [ ] `Event.ControlPlane.Client/Services`: Add `IControlPlaneJobsService` and result contracts.
- [ ] `Explore.Application/Features/ControlPlane`: Add jobs/outbox/email/moderation status query and safe retry commands for retryable/dead-letter records.
- [ ] `Event.ControlPlane.Client/Pages/Jobs/ControlPlaneJobsPage.razor`: Build jobs dashboard with guarded retry affordances.
- [ ] `Event.ControlPlane.Client/Services`: Add `IControlPlaneSecurityService` and result contracts.
- [ ] `Explore.Application/Features/ControlPlane`: Add redacted security posture query for auth, authorization, CORS/origins, headers, secret/config status, admin authority source, and emergency access posture.
- [ ] `Event.ControlPlane.Client/Pages/Security/ControlPlaneSecurityPage.razor`: Build security posture page without exposing secrets.
- [ ] `Event.ControlPlane.Client/Services`: Add `IControlPlanePolicyService` and result contracts.
- [ ] `Explore.Application/Features/ControlPlane`: Add policy/authorization provider status, tenant lock posture, policy sync state, policy-change outbox state, and safe resync/reload command if supported.
- [ ] `Event.ControlPlane.Client/Pages/Policies/ControlPlanePoliciesPage.razor`: Build policies page.
- [ ] `Event.ControlPlane.Blazor/Components/Pages/ControlPlaneSectionPage.razor`: Remove placeholder routes as each real page lands.
- [ ] Verification: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet build --configuration Release --verbosity quiet`.

## Phase 8: Add Onboarding As Aggregated Remediation

- [ ] `Event.ControlPlane.Client/Services`: Add `IControlPlaneOnboardingService` only after health/storage/security/policies/plans service contracts exist.
- [ ] `Explore.Application/Features/ControlPlane`: Add onboarding checklist query that aggregates existing overview, plan readiness, tenant, domain, health, storage, security, policy, and quota signals without duplicating logic.
- [ ] `Explore.API/Controllers/ControlPlaneController.cs`: Add onboarding endpoint and HAL link.
- [ ] `Event.ControlPlane.Client/Pages/Onboarding/ControlPlaneOnboardingPage.razor`: Build first-run/remediation checklist with direct links to unresolved sections.
- [ ] `Event.ControlPlane.Blazor/Components/Pages/ControlPlaneSectionPage.razor`: Remove onboarding placeholder route.
- [ ] Verification: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet build --configuration Release --verbosity quiet`.

## Phase 9: Accessibility, Design-System, And Responsive Hardening

- [ ] Every Control Plane page: Ensure exactly one `<h1>` and sequential headings.
- [ ] Every form: Ensure visible labels, validation messages, keyboard submission, and accessible errors.
- [ ] Every icon-only or compact action: Add meaningful `aria-label`.
- [ ] Command result panels: Announce success/failure through existing accessibility announcement patterns.
- [ ] CSS files: Use colocated `.razor.css`, BEM classes, CSS logical properties, and avoid unscoped `.mud-*` overrides.
- [ ] Layout: Verify desktop and mobile behavior with current MudBlazor primitives and local Control Plane wrappers.
- [ ] Copy: Replace raw implementation messages with operator-oriented remediation text.
- [ ] Verification: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: Manual browser QA across `/admin/instance` routes as an instance admin and non-admin.

## Phase 10: Security, Authorization, Abuse, And Tenant-Isolation Tests

- [ ] Add/extend tests proving non-admin users cannot render the Control Plane shell.
- [ ] Add/extend tests proving DB-derived instance-admin authority, not Keycloak roles, controls shell/API access.
- [ ] Add/extend tests proving user ID fallback remains `sub -> nameidentifier -> sid` for Control Plane API authorization.
- [ ] Add/extend tests proving browser-supplied privileged headers are stripped before proxy/API forwarding.
- [ ] Add/extend tests proving hidden HAL actions cannot be invoked successfully without server authorization.
- [ ] Add/extend tests proving plan payloads and diagnostics do not include tokens, setup secrets, API keys, raw connection strings, provider credentials, or tenant business data.
- [ ] Add/extend tests proving plan application cannot bypass instance locks or quota ceilings.
- [ ] Add/extend tests proving tenant admins can only modify unlocked/delegated settings.
- [ ] Add/extend rate-limit/timeout tests for expensive probe/retry/diff/apply endpoints.
- [ ] Verification: `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.

## Phase 11: Documentation And Operational Runbooks

- [ ] `docs/BLAZOR.md`: Document final Control Plane host/RCL responsibilities and route map.
- [ ] `docs/SECURITY-MODEL.md`: Document final Control Plane auth, BFF, token forwarding, admin authority, and no-secret boundaries.
- [ ] `docs/API.md`: Document new Control Plane endpoint groups, HATEOAS affordances, quota ProblemDetails, and versioning behavior.
- [ ] `docs/AUTHORIZATION.md`: Document new resource/action policies or Cerbos/local fallback updates.
- [ ] `docs/ADMIN_HIERARCHY.md`: Document Plan Studio and tenant configuration authority boundaries.
- [ ] `docs/MULTI_TENANCY.md`: Document tenant plan assignment, effective settings, lock source, and single-tenant caveats.
- [ ] `docs/STORAGE.md`: Document storage plan/quota integration and backup/restore notes.
- [ ] `docs/OPERATIONS.md`: Document operator workflows for plans, health, storage, jobs, security, policies, onboarding, and deployment-mode changes.
- [ ] `docs/DESIGN_SYSTEM.md`: Update only if new reusable Control Plane primitives become canonical.
- [ ] `dev/_journal/journal.md`: Add durable findings for non-obvious route/API/versioning/authorization/settings/plan behavior.
- [ ] Verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
- [ ] Verification: `dotnet build --configuration Release --verbosity quiet`.

## Final v1.0 Acceptance Checklist

- [ ] `/admin/instance` renders real overview data or precise remediation, not adapter placeholder copy.
- [ ] Plan Studio supports create, validate, publish, archive, clone, version history, diff, assignment, and rollback where supported.
- [ ] Per-tenant configuration shows effective values, value source, lock source, assigned plan, overrides, quota usage, and audit history.
- [ ] `/admin/instance/tenants` supports list, create with plan selection, lifecycle actions, and audit visibility.
- [ ] `/admin/instance/domains` supports DNS guidance and verify/test/retry actions.
- [ ] `/admin/instance/operations` supports operations status, deployment-mode runbook, and safe mode transitions.
- [ ] `/admin/instance/onboarding` is a real checklist page.
- [ ] `/admin/instance/health` is a real health page.
- [ ] `/admin/instance/storage` is a real storage/quota page.
- [ ] `/admin/instance/jobs` is a real jobs page.
- [ ] `/admin/instance/security` is a real security page.
- [ ] `/admin/instance/policies` is a real policies/locks page.
- [ ] No v1.0 route shows `Not connected yet`, `reserved for shared control-plane implementation`, or dedicated-host adapter-not-configured copy.
- [ ] All action buttons are backed by API/Application commands and HAL links.
- [ ] No browser-visible token, setup secret, API key, connection string, provider credential, privileged header, or tenant business data exists.
- [ ] Plan application cannot bypass instance locks, tenant isolation, quota ceilings, or server authorization.
- [ ] All verification commands relevant to changed slices pass.
