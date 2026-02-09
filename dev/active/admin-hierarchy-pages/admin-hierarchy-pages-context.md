# Context: Admin Hierarchy Pages (Instance, Tenant, Organization)

Last Updated: 2026-02-08

## SESSION PROGRESS (2026-02-06)

### Completed

- Completed deep documentation review for architecture, domain, security, operations, tenancy, and governance.
- Completed broad code analysis across Blazor, API, Application, Infrastructure, Persistence, and Domain layers.
- Produced full implementation strategy in `dev/active/admin-hierarchy-pages/admin-hierarchy-pages-plan.md`.
- Identified current implementation gaps and mapped them to phased tasks.
- Persisting task management artifacts:
- `admin-hierarchy-pages-context.md` (this file)
- `admin-hierarchy-pages-tasks.md`

### In Progress

- Implemented first-run onboarding flow scaffolding:
  - `InstanceBootstrapState` and onboarding status/complete/update endpoints.
  - `StartupGate` route orchestration (`instance -> tenant -> public home route`).
- Implemented tenant and instance policy settings expansion for:
  - Home page selection (event list vs landing page).
  - Domain governance (instance base domain, subdomain/custom-domain policies).
  - White-label branding fields (display name, logo, favicon, optional custom CSS URL).
- Added public experience read model for anonymous-safe home/branding resolution:
  - `api/v1/PublicExperience/settings`
  - `PublicExperienceService` in Blazor client.
- Added runtime lookup seeding baseline (`LookupTableSeeder`) and removed `HasData` dependencies from lookup configs.

### Blockers

- No technical blockers for planning.
- One product/ownership clarification needed before implementation start:
  - Tenant onboarding policy ownership and delegation boundaries (instance-only vs partially delegated).

## Goal of This Workstream

Create maintainable, enterprise-grade admin experiences with scope-separated behavior:

1. Instance Administrator
2. Tenant Administrator
3. Organization Administrator

Scope-specific behavior must include:

- Runtime deployment mode control.
- Module/aspect governance.
- User-submitted events policy.
- Organization verification policy and workflow controls.
- Tenant onboarding policy controls.

## What Was Analyzed

## Core Project References

- `CLAUDE.md`
- `docs/PROJECT.md`
- `docs/ARCHITECTURE.md`
- `docs/DOMAIN.md`
- `docs/SECURITY.md`
- `docs/CONFIGURATION.md`
- `docs/GOVERNANCE.md`
- `docs/OPERATIONS.md`
- `docs/TROUBLESHOOTING.md`
- `docs/FEDERATION.md`
- `docs/API.md`
- `docs/BLAZOR.md`
- `docs/ADMIN_HIERARCHY.md`
- `docs/MULTI_TENANCY.md`
- `docs/DEPLOYMENT_MODES.md`
- `docs/EXTENSIBILITY.md`
- `docs/MODULAR_EVENTS.md`
- `docs/RENDER_POLICIES.md`

## Skills Reviewed

- `.claude/skills/clean-architecture-rules/SKILL.md`
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`
- `.claude/skills/blazor-ui-conventions/SKILL.md`
- `.claude/skills/auth-patterns/SKILL.md`
- `.claude/skills/blazor-bff-patterns/SKILL.md`
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md`

## Key Code Areas Reviewed

### Blazor and Navigation

- `Explore.Blazor/Components/Routes.razor`
- `Explore.Blazor/Program.cs`
- `Explore.Blazor/Services/CircuitAccessTokenService.cs`
- `Explore.Blazor.Client/Layout/NavMenu.razor`
- `Explore.Blazor.Client/Layout/NavMenu.razor.cs`
- `Explore.Blazor.Client/Pages/Admin/*`
- `Explore.Blazor.Client/Services/AdminService.cs`
- `Explore.Blazor.Client/Services/AuthStateService.cs`
- `Explore.Blazor.Client/Providers/TenantContextProvider.razor`

### API and Authorization

- `Explore.API/Controllers/TenantController.cs`
- `Explore.API/Controllers/TenantSettingsController.cs`
- `Explore.API/Controllers/ModuleController.cs`
- `Explore.API/Controllers/OrganizationController.cs`
- `Explore.API/Controllers/OrganizationMemberController.cs`
- `Explore.API/Services/TenantContext.cs`
- `Explore.API/Filters/BlockInSingleTenantAttribute.cs`

### Domain and Persistence Foundations

- `Explore.Domain/SystemSetting.cs`
- `Explore.Domain/TenantSetting.cs`
- `Explore.Domain/TenantSettings.cs`
- `Explore.Domain/Modules/ModuleDefinition.cs`
- `Explore.Domain/Modules/TenantCapability.cs`
- `Explore.Domain/Enums/RoleEnum.cs`
- `Explore.Domain/Enums/OrganizationRoleEnum.cs`
- `Explore.Domain/Enums/ApprovalStatusEnum.cs`
- `Explore.Persistence/Configurations/Entities/SystemSettingConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/TenantSettingConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/ModuleDefinitionConfiguration.cs`
- `Explore.Persistence/ExploreDbContext.cs`
- `Explore.Infrastructure/Services/SettingsResolver.cs`
- `Explore.Infrastructure/Services/ModuleService.cs`
- `Explore.Infrastructure/DeploymentSettings.cs`

## Findings Summary (Critical)

1. Current admin UX is largely single-dashboard (`/admin`) and not scope-separated.
2. Role checks are mostly coarse (`Admin`) across APIs and UI.
3. Runtime deployment mode switching is expected by docs but currently not fully settings-resolved in request path.
4. Tenant header behavior in BFF is currently default-driven, which is risky for full multi-tenant behavior.
5. Governance settings foundation exists but lacks complete role-specific APIs and pages.
6. `TenantSettingsDto.Id` type mismatch (`int`) vs domain (`Guid`) should be corrected.

## Key Design Decisions Captured

1. Introduce three portal groups (`/admin/instance`, `/admin/tenant`, `/admin/organization/{id}`).
2. Use lock-aware effective setting UX (value + source + editability reason).
3. Enforce lock/policy rules in handlers (not only in UI).
4. Move authorization to explicit scope policies rather than broad role checks.
5. Keep Cerbos as policy artifacts/documentation only for now; no false assumption of runtime integration.
6. Keep strict tenant isolation and remove static tenant assumptions in multi-tenant flows.

## Cross-Cutting Constraints

- Must preserve Clean Architecture direction.
- Must follow CQRS + MediatR patterns.
- Must keep validator pattern aligned with project governance (manual instantiation in handlers where validators are used in that style).
- Must preserve BFF security architecture and avoid browser token exposure.
- Must preserve EF Core tenant isolation query filter behavior.
- Must include migration and test coverage.

## Open Questions Requiring Confirmation

1. Tenant onboarding policy ownership:
   - instance-admin only, or delegatable to tenant-admin under certain conditions?
2. Verification decision authority:
   - tenant-admin only, or instance-admin override path with audit?
3. Single-tenant UX:
   - merge instance+tenant pages in one UX shell, or keep visibly separate?
4. Role claim mapping contract from IdP:
   - exact claims and names for `InstanceAdmin`, `TenantAdmin`, `OrganizationAdmin`.
5. Legacy compatibility:
   - keep `/admin` as redirect shell for one release or hard switch.

## Dependencies and Sequencing

Recommended sequence:

1. Contract freeze and setting key catalog (Phase 0).
2. Domain/model consistency work (Phase 1).
3. Application command/query slices + authorization guards (Phase 2).
4. Persistence + runtime mode resolution + audit logging (Phase 3).
5. API/BFF scope endpoints and tenant propagation hardening (Phase 4).
6. Blazor role-specific pages and navigation (Phase 5).
7. Full test matrix + docs/runbook updates (Phase 6).

## Where to Continue From

Primary reference files:

- Plan:
  - `dev/active/admin-hierarchy-pages/admin-hierarchy-pages-plan.md`
- Tasks:
  - `dev/active/admin-hierarchy-pages/admin-hierarchy-pages-tasks.md`

Immediate next execution step:

- Start with Phase 0, Task 0.1 (role ownership matrix freeze), then Task 0.2.

## Quick Resume Checklist

- [ ] Re-read plan sections for Phase 0 and Phase 1.
- [ ] Confirm open ownership questions with product/security stakeholders.
- [ ] Begin implementation branch with scope-policy contract changes first.
- [ ] Keep this context file updated after each major milestone.
