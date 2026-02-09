# Plan: Admin Hierarchy Pages (Instance, Tenant, Organization)

Last Updated: 2026-02-08

## Executive Summary

This implementation introduces three separate admin experiences with strict authority boundaries:

1. Instance Administrator pages for platform-wide governance.
2. Tenant Administrator pages for tenant policy and moderation controls.
3. Organization Administrator pages for organization operations and membership management.

The work will align the current codebase with project documentation around hierarchy, tenancy, runtime deployment switching, and lock-aware settings. The final outcome is enterprise-grade, maintainable, and follows Clean Architecture (Domain -> Application -> Infrastructure/Persistence -> API/BFF -> Blazor UI).

This plan is explicitly designed for the requirements:

- Instance admin can switch single-tenant and multi-tenant mode at runtime.
- Instance admin can govern event/module aspects available to tenants.
- Tenant admin can control user-submitted events, verification workflow behavior, and moderation defaults.
- Tenant admin can manage whether verification requirements are enforced or omitted (if delegation allows it).
- Admin experiences are different by role and by scope.

## Current State Analysis

## Documentation Intent (Target Behavior)

The following docs establish expected behavior:

- `docs/ADMIN_HIERARCHY.md`
- `docs/MULTI_TENANCY.md`
- `docs/DEPLOYMENT_MODES.md`
- `docs/OPERATIONS.md`
- `docs/EXTENSIBILITY.md`
- `docs/MODULAR_EVENTS.md`
- `docs/RENDER_POLICIES.md`
- `docs/SECURITY.md`

Expected system characteristics from docs:

- Scope-separated admin authority (instance vs tenant vs organization).
- Cascading settings with lock/delegation semantics.
- Runtime deployment mode switching.
- Tenant/module governance with constraints.
- Strict cross-tenant isolation.

## Observed Codebase Reality (As-Is)

### Blazor Admin UX

- Main admin page is currently centralized:
  - `Explore.Blazor.Client/Pages/Admin/AdminList.razor`
- Current admin route/menu behavior is generic and role-coarse:
  - `Explore.Blazor.Client/Layout/NavMenu.razor`
  - `Explore.Blazor.Client/Layout/NavMenu.razor.cs`
- Routing uses manual route registration and is incomplete relative to existing pages:
  - `Explore.Blazor/Components/Routes.razor`

### API Authorization

- Many admin controllers still use coarse `[Authorize(Roles = "Admin")]`:
  - `Explore.API/Controllers/TenantController.cs`
  - `Explore.API/Controllers/TenantSettingsController.cs`
  - `Explore.API/Controllers/OrganizationController.cs` (approval status write)
- Partial multi-role support exists for module enable/disable:
  - `Explore.API/Controllers/ModuleController.cs` (`Admin,TenantAdmin`)

### Tenancy and Runtime Deployment Mode

- Runtime docs expect settings-driven switching, but request-time mode handling is config-first:
  - `Explore.API/Services/TenantContext.cs`
  - `Explore.Infrastructure/DeploymentSettings.cs`
- BFF currently pushes default tenant header in key paths:
  - `Explore.Blazor/Program.cs`
  - `Explore.Blazor/Services/CircuitAccessTokenService.cs`

### Settings and Governance Foundation

Strong base exists:

- `Explore.Domain/SystemSetting.cs`
- `Explore.Domain/TenantSetting.cs`
- `Explore.Infrastructure/Services/SettingsResolver.cs`
- `Explore.Domain/Modules/ModuleDefinition.cs`
- `Explore.Domain/Modules/TenantCapability.cs`

Notable gaps:

- `Explore.Domain/TenantSettings.cs` is minimal.
- `Explore.Application/DTOs/TenantSettings/TenantSettingsDto.cs` uses `int Id` while domain uses `Guid`.
- Existing flows do not expose role-specific governance UIs/APIs.

## Key Gaps to Close

1. Replace coarse admin role handling with scope-specific policy enforcement.
2. Create role-specific admin portals/routes/pages.
3. Make runtime deployment mode switching truly settings-driven.
4. Remove static tenant header assumptions in multi-tenant operational flows.
5. Formalize lock/delegation UX and API semantics.
6. Fill testing matrix for role/scope/isolation correctness.

## Proposed Future State

## Role-Separated Admin Portals

### 1) Instance Administrator Portal

Route group:

- `/admin/instance/*`

Primary capabilities:

- Runtime deployment mode switch (`SingleTenant` <-> `MultiTenant`).
- Global tenant registration and onboarding policy controls.
- Global module catalog governance.
- System lock/delegation configuration for tenant-overridable settings.
- Platform-level configuration audit visibility.

### 2) Tenant Administrator Portal

Route group:

- `/admin/tenant/*`

Primary capabilities:

- User-submitted event policy controls.
- Organization verification policy controls.
- Tenant-level module enablement from allowed global set.
- Tenant-level moderation defaults and policy overrides (when unlocked).

### 3) Organization Administrator Portal

Route group:

- `/admin/organization/{organizationId}/*`

Primary capabilities:

- Organization member/role management.
- Organization profile and operational settings.
- Organization verification submission and status visibility.
- Organization-level event defaults within tenant policy boundaries.

## Settings and Policy Cascade Model

Resolution order:

1. System default (`SystemSetting`).
2. System lock/delegation state (`IsLocked` + metadata).
3. Tenant override (`TenantSetting`) when allowed.
4. Organization-scoped options when delegated by tenant.

Every admin control must surface:

- Effective value.
- Source of value.
- Lock reason if not editable.

## Authorization Model

Introduce explicit policies:

- `InstanceAdminPolicy`
- `TenantAdminPolicy`
- `OrganizationAdminPolicy`

Use policy + claims + membership checks where required; reduce direct role-string checks in UI and handlers.

## Single-Tenant Behavior

In `SingleTenant` mode:

- UI may merge instance+tenant experiences for usability.
- Backend still enforces scope semantics.
- Multi-tenant-only endpoints/features are hidden or blocked.

In `MultiTenant` mode:

- Scope separation is explicit in menu, routes, and endpoint access.

## Clean Architecture Implementation Phases

## Phase 0: Contract and Governance Alignment

### Task 0.1: Freeze role ownership matrix

- Files:
  - `docs/ADMIN_HIERARCHY.md`
  - `docs/MULTI_TENANCY.md`
  - `docs/OPERATIONS.md`
  - `docs/API.md`
- Acceptance Criteria:
  - [ ] Every admin action has a single owner scope and delegation rule.
  - [ ] Conflicts between docs and current implementation are documented.
  - [ ] Tenant onboarding authority is explicit.
- Dependencies: none
- Effort: M
- Related Skills: `auth-patterns`, `clean-architecture-rules`

### Task 0.2: Governance setting key catalog

- Files:
  - `Explore.Domain/*` (new key catalog/constants)
  - `Explore.Persistence/Configurations/Entities/SystemSettingConfiguration.cs`
- Acceptance Criteria:
  - [ ] Canonical setting keys defined with value types and allowed values.
  - [ ] Lock behavior defined per key.
  - [ ] Categories and display order are stable.
- Dependencies: Task 0.1
- Effort: M
- Related Skills: `dotnet-efcore-guidelines`, `clean-architecture-rules`

## Phase 1: Domain Layer

### Task 1.1: Add governance enums and key constants

- Files:
  - `Explore.Domain/Enums/*` (new)
  - `Explore.Domain/*` (new constants)
- Acceptance Criteria:
  - [ ] Enums added for deployment mode, registration policy, verification policy, user-submission policy.
  - [ ] Constants map to seeded `SystemSetting` keys.
- Dependencies: Task 0.2
- Effort: M
- Related Skills: `clean-architecture-rules`

### Task 1.2: Fix tenant settings model consistency

- Files:
  - `Explore.Domain/TenantSettings.cs`
  - `Explore.Application/DTOs/TenantSettings/TenantSettingsDto.cs`
  - `Explore.Application/Profiles/MappingProfile.cs`
- Acceptance Criteria:
  - [ ] `TenantSettingsDto.Id` is `Guid`.
  - [ ] Mapping is type-consistent end-to-end.
  - [ ] Direction of `TenantSettings` vs `TenantSetting` use is explicit.
- Dependencies: Task 0.2
- Effort: M
- Related Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`

### Task 1.3: Add governance change log domain entity

- Files:
  - `Explore.Domain/*` (new entity)
- Acceptance Criteria:
  - [ ] Every configuration change can be audited with actor, scope, before/after values.
  - [ ] Supports instance and tenant scope.
- Dependencies: Task 0.1
- Effort: M
- Related Skills: `dotnet-efcore-guidelines`

## Phase 2: Application Layer (CQRS/MediatR)

### Task 2.1: Instance admin command/query slice

- Files:
  - `Explore.Application/Features/Admin/Instance/*` (new)
  - `Explore.Application/DTOs/Admin/Instance/*` (new)
- Acceptance Criteria:
  - [ ] Query returns effective system governance state.
  - [ ] Commands update system settings with lock semantics.
  - [ ] Validators are manually instantiated in handlers.
- Dependencies: Tasks 1.1, 1.3
- Effort: L
- Related Skills: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

### Task 2.2: Tenant admin command/query slice

- Files:
  - `Explore.Application/Features/Admin/Tenant/*` (new)
  - `Explore.Application/DTOs/Admin/Tenant/*` (new)
- Acceptance Criteria:
  - [ ] Queries return tenant effective values + source metadata.
  - [ ] Commands enforce lock denial where applicable.
  - [ ] User-submitted event and verification policies are supported.
- Dependencies: Tasks 1.1, 1.2
- Effort: L
- Related Skills: `cqrs-mediatr-guidelines`

### Task 2.3: Organization admin command/query slice

- Files:
  - `Explore.Application/Features/Admin/Organization/*` (new)
  - existing organization/member handlers as needed
- Acceptance Criteria:
  - [ ] Membership/role logic aligns with `OrganizationRoleEnum`.
  - [ ] Verification submission/status flow is represented.
  - [ ] Tenant constraints are enforced.
- Dependencies: Task 2.2
- Effort: L
- Related Skills: `cqrs-mediatr-guidelines`, `auth-patterns`

### Task 2.4: Unified admin scope context query

- Files:
  - `Explore.Application/Features/Admin/Common/*` (new)
- Acceptance Criteria:
  - [ ] UI can query allowed scopes from one endpoint/handler path.
  - [ ] Context contains tenant/org constraints and lock state.
- Dependencies: Tasks 2.1, 2.2, 2.3
- Effort: M
- Related Skills: `cqrs-mediatr-guidelines`

### Task 2.5: Shared application-level authorization guards

- Files:
  - `Explore.Application/Contracts/Authorization/*` (new)
  - relevant handlers
- Acceptance Criteria:
  - [ ] Scope checks are centralized, not duplicated.
  - [ ] Handlers consume shared guard contracts.
- Dependencies: Tasks 2.1, 2.2, 2.3
- Effort: M
- Related Skills: `clean-architecture-rules`, `auth-patterns`

## Phase 3: Infrastructure and Persistence

### Task 3.1: Schema and seed updates

- Files:
  - `Explore.Persistence/Configurations/Entities/SystemSettingConfiguration.cs`
  - `Explore.Persistence/Configurations/Entities/TenantSettingConfiguration.cs`
  - `Explore.Persistence/Migrations/*` (new)
  - audit entity config (new)
- Acceptance Criteria:
  - [ ] New governance keys seeded.
  - [ ] Migration adds required constraints/indexes.
  - [ ] No key duplication.
- Dependencies: Tasks 1.1, 1.3
- Effort: L
- Related Skills: `dotnet-efcore-guidelines`

### Task 3.2: Harden settings resolver/repositories

- Files:
  - `Explore.Infrastructure/Services/SettingsResolver.cs`
  - `Explore.Persistence/Repositories/SystemSettingRepository.cs`
  - `Explore.Persistence/Repositories/TenantSettingRepository.cs`
- Acceptance Criteria:
  - [ ] Resolver returns effective value source and lock metadata.
  - [ ] Cache invalidation works for instance/tenant updates.
- Dependencies: Task 3.1
- Effort: M
- Related Skills: `dotnet-efcore-guidelines`

### Task 3.3: Runtime deployment mode via settings engine

- Files:
  - `Explore.API/Services/TenantContext.cs`
  - `Explore.Infrastructure/DeploymentSettings.cs`
  - service registration files as needed
- Acceptance Criteria:
  - [ ] Mode changes take effect at runtime without restart.
  - [ ] Safe fallback for missing/invalid setting.
- Dependencies: Tasks 2.1, 3.2
- Effort: L
- Related Skills: `auth-patterns`, `dotnet-efcore-guidelines`

### Task 3.4: Persist configuration audit events

- Files:
  - infrastructure services and application handler integration
- Acceptance Criteria:
  - [ ] Every governance write operation emits a persisted audit record.
  - [ ] Records include actor, scope, key, before/after.
- Dependencies: Tasks 1.3, 2.1, 2.2
- Effort: M
- Related Skills: `error-tracking`, `clean-architecture-rules`

## Phase 4: API and BFF

### Task 4.1: Scope-specific admin controllers

- Files:
  - `Explore.API/Controllers/Admin/InstanceAdminController.cs` (new)
  - `Explore.API/Controllers/Admin/TenantAdminController.cs` (new)
  - `Explore.API/Controllers/Admin/OrganizationAdminController.cs` (new)
  - HATEOAS route/policy files as needed
- Acceptance Criteria:
  - [ ] Endpoint groups are scope-aligned.
  - [ ] Endpoint metadata/docs complete.
- Dependencies: Tasks 2.1, 2.2, 2.3
- Effort: L
- Related Skills: `auth-patterns`, `cqrs-mediatr-guidelines`

### Task 4.2: Register policy-based authorization

- Files:
  - `Explore.API/Program.cs`
  - authorization handlers (new)
- Acceptance Criteria:
  - [ ] `InstanceAdminPolicy`, `TenantAdminPolicy`, `OrganizationAdminPolicy` registered.
  - [ ] Endpoint authorization migrated from coarse role checks where applicable.
- Dependencies: Task 2.5
- Effort: M
- Related Skills: `auth-patterns`

### Task 4.3: Tenant propagation hardening in BFF

- Files:
  - `Explore.Blazor/Program.cs`
  - `Explore.Blazor/Services/CircuitAccessTokenService.cs`
  - `Explore.Blazor.Client/Providers/TenantContextProvider.razor`
  - `Explore.Blazor.Client/Services/AuthStateService.cs`
- Acceptance Criteria:
  - [ ] Multi-tenant mode uses resolved tenant context, not static default header.
  - [ ] Single-tenant fallback remains deterministic.
  - [ ] Tenant mismatch is detected and logged.
- Dependencies: Task 3.3
- Effort: L
- Related Skills: `blazor-bff-patterns`, `auth-patterns`

### Task 4.4: Cerbos policy artifacts (future readiness)

- Files:
  - `docs/security/cerbos/admin-policies.yaml` (new)
  - `docs/security/cerbos/README.md` (new)
- Acceptance Criteria:
  - [ ] Policy drafts cover scope-separated admin actions.
  - [ ] Documentation states Cerbos is not runtime-enabled yet.
- Dependencies: Tasks 4.1, 4.2
- Effort: S
- Related Skills: `auth-patterns`

## Phase 5: Blazor UI (Hybrid)

### Task 5.1: Refactor admin route and information architecture

- Files:
  - `Explore.Blazor/Components/Routes.razor`
  - `Explore.Blazor.Client/Layout/NavMenu.razor`
  - `Explore.Blazor.Client/Layout/NavMenu.razor.cs`
- Acceptance Criteria:
  - [ ] Admin routes are complete and grouped by scope.
  - [ ] Navigation is role/scope-aware.
- Dependencies: Task 4.1
- Effort: M
- Related Skills: `blazor-ui-conventions`

### Task 5.2: Implement Instance Admin pages

- Files:
  - `Explore.Blazor.Client/Pages/Admin/Instance/*` (new)
  - `Explore.Blazor.Client/Services/InstanceAdminService.cs` (new)
- Acceptance Criteria:
  - [ ] Runtime mode switch UI with confirmation and safeguards.
  - [ ] Global modules and lock/delegation controls.
- Dependencies: Tasks 4.1, 4.3
- Effort: L
- Related Skills: `blazor-ui-conventions`, `blazor-bff-patterns`

### Task 5.3: Implement Tenant Admin pages

- Files:
  - `Explore.Blazor.Client/Pages/Admin/Tenant/*` (new)
  - `Explore.Blazor.Client/Services/TenantAdminService.cs` (new)
- Acceptance Criteria:
  - [ ] User-submitted event policy control page.
  - [ ] Organization verification policy control page.
  - [ ] Lock-aware control behavior with source badges.
- Dependencies: Tasks 4.1, 4.3, 5.2
- Effort: L
- Related Skills: `blazor-ui-conventions`

### Task 5.4: Implement Organization Admin pages

- Files:
  - `Explore.Blazor.Client/Pages/Admin/Organization/*` (new)
  - `Explore.Blazor.Client/Services/OrganizationAdminService.cs` (new)
- Acceptance Criteria:
  - [ ] Membership and role management pages.
  - [ ] Verification submission/status page.
  - [ ] Organization scope restrictions enforced.
- Dependencies: Tasks 2.3, 4.1, 5.3
- Effort: L
- Related Skills: `blazor-ui-conventions`, `auth-patterns`

### Task 5.5: Shared governance components

- Files:
  - `Explore.Blazor.Client/Components/Admin/Common/*` (new)
- Acceptance Criteria:
  - [ ] Reusable lock indicators, source chips, confirmation dialogs.
  - [ ] Consistent styling and behavior.
- Dependencies: Tasks 5.2, 5.3, 5.4
- Effort: M
- Related Skills: `blazor-ui-conventions`

### Task 5.6: Single-tenant merged UX behavior

- Files:
  - admin layout/nav components and route guards
- Acceptance Criteria:
  - [ ] Combined UX in single-tenant mode.
  - [ ] Clear separation restored in multi-tenant mode.
- Dependencies: Tasks 3.3, 5.1
- Effort: M
- Related Skills: `blazor-ui-conventions`, `auth-patterns`

## Phase 6: Testing, Operations, and Documentation

### Task 6.1: Unit tests (application/domain)

- Files/Projects:
  - `Event.Application.UnitTests/*`
  - `Event.Domain.UnitTests/*`
- Acceptance Criteria:
  - [ ] Governance handlers covered for success/failure/lock denial.
  - [ ] Settings resolution source behavior covered.
- Dependencies: Phases 1-3
- Effort: L
- Related Skills: `cqrs-mediatr-guidelines`

### Task 6.2: API integration tests (scope matrix)

- Files/Projects:
  - `Event.API.IntegrationTests/*`
  - `Event.API.IntegrationTests/Fixtures/TestAuthHandler.cs`
- Acceptance Criteria:
  - [ ] 200/401/403 matrix for all admin scope endpoints.
  - [ ] Tenant isolation tests for context/header mismatch.
- Dependencies: Phase 4
- Effort: L
- Related Skills: `auth-patterns`

### Task 6.3: Blazor tests (role-aware nav/page gating)

- Files/Projects:
  - `Explore.Blazor.Client.Tests/*`
- Acceptance Criteria:
  - [ ] Role/scope menu visibility tested.
  - [ ] Route access tested for each admin scope.
  - [ ] Lock/read-only UI states tested.
- Dependencies: Phase 5
- Effort: M
- Related Skills: `blazor-ui-conventions`

### Task 6.4: Documentation and runbook updates

- Files:
  - `docs/ADMIN_HIERARCHY.md`
  - `docs/MULTI_TENANCY.md`
  - `docs/DEPLOYMENT_MODES.md`
  - `docs/SECURITY.md`
  - `docs/API.md`
  - `docs/OPERATIONS.md`
- Acceptance Criteria:
  - [ ] Scope-separated routes/endpoints documented.
  - [ ] Runtime mode switch procedure and rollback documented.
  - [ ] Governance audit and incident procedure documented.
- Dependencies: Phases 3-5
- Effort: M
- Related Skills: `clean-architecture-rules`, `auth-patterns`

## Detailed Role-Driven Page Inventory

### Instance Admin

- `/admin/instance/overview`
- `/admin/instance/deployment-mode`
- `/admin/instance/tenant-onboarding`
- `/admin/instance/module-catalog`
- `/admin/instance/policy-locks`
- `/admin/instance/config-audit`

### Tenant Admin

- `/admin/tenant/overview`
- `/admin/tenant/event-submission-policy`
- `/admin/tenant/organization-verification-policy`
- `/admin/tenant/modules`
- `/admin/tenant/moderation`

### Organization Admin

- `/admin/organization/{organizationId}/overview`
- `/admin/organization/{organizationId}/members`
- `/admin/organization/{organizationId}/verification`
- `/admin/organization/{organizationId}/event-defaults`

## Risk Assessment and Mitigations

| Risk | Severity | Mitigation |
|---|---|---|
| Role/claim mismatch between IdP and API policies | High | Define mapping contract + integration tests early in Phase 4/6. |
| Static tenant header causes cross-tenant defects | Critical | Complete Task 4.3 before enabling tenant admin release. |
| Runtime mode switching introduces inconsistent behavior | High | Resolve mode via settings + cache invalidation + rollback flow. |
| Lock bypass via direct API call | High | Enforce lock checks in handlers, not only UI. |
| Manual route list drift in Blazor | Medium | Centralize route definitions + add route coverage tests. |
| Migration conflicts in active branches | Medium | Isolate migration updates and verify idempotent scripts. |

## Success Metrics

### Security/Correctness

- 100 percent admin endpoint policy coverage in integration tests.
- Zero tenant isolation violations in test matrix.
- Zero lock bypass defects in governance command tests.

### Product Behavior

- Runtime deployment mode change effective on next request.
- Tenant admins can configure submission/verification policies within delegated boundaries.
- Organization admins can operate only in their organization scope.

### Maintainability

- No Clean Architecture boundary violations introduced.
- Shared reusable admin components for lock/source UX.
- Setting key catalog is explicit and documented.

## Required Resources and Dependencies

### Technical Dependencies

- IdP role mapping updates for `InstanceAdmin`, `TenantAdmin`, `OrganizationAdmin`.
- EF migration execution window and rollback verification.
- OpenAPI/NSwag refresh process for new admin endpoints.

### Team Dependencies

- Product owner decision on delegation of tenant onboarding controls.
- Security review for policy model.
- QA support for role matrix and runtime mode switch regression scenarios.

## Effort Estimate

| Phase | Effort | Duration (approx) |
|---|---|---|
| Phase 0 | M | 3-4 days |
| Phase 1 | M | 4-6 days |
| Phase 2 | XL | 8-12 days |
| Phase 3 | L | 6-9 days |
| Phase 4 | L | 5-8 days |
| Phase 5 | XL | 10-15 days |
| Phase 6 | L | 6-9 days |

Total estimated range: 42-63 engineering days (single engineer equivalent), excluding external approvals.

## Exit Criteria (Definition of Done)

- [ ] All phase acceptance criteria completed.
- [ ] API + Blazor scope/role test matrix green.
- [ ] Migrations verified in dev/staging with rollback path.
- [ ] Docs and operational runbooks updated.
- [ ] Admin pages are role-separated, lock-aware, and functional in single and multi-tenant modes.
