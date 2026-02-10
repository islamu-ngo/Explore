# Task Checklist: Admin Hierarchy Pages (Instance, Tenant, Organization)

Last Updated: 2026-02-10

## Status

Current state: **Phase 5 (Blazor UI) COMPLETE. All files created, build passes (0 errors, 0 warnings), all 195 tests pass. Uncommitted changes need to be committed.**

The original plan had 6 phases spanning domain → application → API → UI. 
**Phase 5 (Blazor UI) is being implemented FIRST** since all backend services already exist.
Remaining phases (0-4, 6) are deferred — they cover backend hardening, role policies, and audit logging.

## Quick Resume

- **Next action**: Create all 15 new Blazor files (see context.md for full list)
- **Plan reference**: `polished-sparking-goose.md`
- **Pattern reference**: `Explore.Blazor.Client/Components/Settings/SettingsLayout.razor`
- **Build command**: `dotnet build --configuration Release --verbosity quiet`

---

## Phase 5: Blazor UI — COMPLETE

### T5.1 Create Instance Admin Settings Pages
- [x] Create `Components/Admin/Instance/InstanceGovernanceSection.razor`
- [x] Create `Components/Admin/Instance/InstanceDomainSection.razor`
- [x] Create `Components/Admin/Instance/InstanceBrandingSection.razor`
- [x] Create `Components/Admin/Instance/InstanceModulesSection.razor`
- [x] Create `Components/Admin/Instance/InstanceAdminSettingsLayout.razor`
- [x] Create `Components/Admin/Instance/InstanceAdminSettingsLayout.razor.css`
- [x] Create `Pages/Admin/Instance/InstanceAdminSettings.razor` (page wrapper)
- [x] Build and verify

### T5.2 Create Tenant Admin Settings Pages
- [x] Create `Components/Admin/Tenant/TenantPoliciesSection.razor`
- [x] Create `Components/Admin/Tenant/TenantDomainSection.razor`
- [x] Create `Components/Admin/Tenant/TenantBrandingSection.razor`
- [x] Create `Components/Admin/Tenant/TenantAdminSettingsLayout.razor`
- [x] Create `Components/Admin/Tenant/TenantAdminSettingsLayout.razor.css`
- [x] Create `Pages/Admin/Tenant/TenantAdminSettings.razor` (page wrapper)
- [x] Build and verify

### T5.3 Create Organization Admin Settings Pages
- [x] Create `Components/Admin/Organization/OrganizationProfileSection.razor`
- [x] Create `Components/Admin/Organization/OrganizationMembersSection.razor`
- [x] Create `Components/Admin/Organization/OrganizationVerificationSection.razor`
- [x] Create `Components/Admin/Organization/OrganizationAdminSettingsLayout.razor`
- [x] Create `Components/Admin/Organization/OrganizationAdminSettingsLayout.razor.css`
- [x] Create `Pages/Admin/Organization/OrganizationAdminSettings.razor` (page wrapper)
- [x] Build and verify

### T5.4 Update Routes and Navigation
- [x] Add 3 using statements to Routes.razor (Instance, Tenant, Organization sub-namespaces)
- [x] Add 3 admin settings routes to Routes.razor
- [x] Add 8 missing lookup table routes to Routes.razor
- [x] Add Instance Settings + Tenant Settings links to NavMenu.razor admin dropdown
- [x] Add Settings cards section to AdminList.razor (before Lookup Tables)
- [x] Build and verify

### T5.5 Clean Up Old Files
- [x] Remove @page directive from InstanceSettings.razor (or delete file)
- [x] Remove @page directive from TenantPolicySettings.razor (or delete file)

### T5.6 Final Verification
- [x] Full build: `dotnet build --configuration Release --verbosity quiet`
- [x] Run all 7 test projects individually
- [x] Verify no duplicate @page routes

---

## Phase 6: Enterprise Cerbos Authorization Hardening — COMPLETE ✅

Implemented 2026-02-10. Follows requirements from `Admin-authorization-cerbos.md` and `GEMINI-FEEDBACK.md`.

### T6.1 Domain Layer
- [x] Create `ConfigurationScopeEnum` (System, Instance, Tenant, Organization)
- [x] Create `ConfigurationChangeLog` entity (audit trail for all config changes)

### T6.2 Application Layer — Contracts
- [x] Create `IAdminContext` interface (hybrid JWT + DB identity resolution)
- [x] Create `ICerbosAuthorizationService` interface (abstracts Cerbos PDP)
- [x] Create `IConfigurationChangeLogService` interface (audit logging)
- [x] Create `IAuthorizedRequest` marker interface (MediatR authorization)
- [x] Create `CerbosAuthorizeAttribute` metadata attribute
- [x] Create `AuthorizationException` (maps to HTTP 403)

### T6.3 Application Layer — Pipeline Behavior
- [x] Create `AuthorizationBehavior<TRequest, TResponse>` (checks IAuthorizedRequest or [CerbosAuthorize])
- [x] Register in `ApplicationServicesRegistration.cs` after PerformanceBehavior

### T6.4 Persistence Layer
- [x] Create `ConfigurationChangeLogConfiguration` EF configuration (indexes, UUID v7)
- [x] Create `IConfigurationChangeLogRepository` interface
- [x] Create `ConfigurationChangeLogRepository` implementation
- [x] Add `DbSet<ConfigurationChangeLog>` to `ExploreDbContext`
- [x] Register repository in `PersistenceServicesRegistration.cs`

### T6.5 Infrastructure Layer
- [x] Create `AdminContext` (JWT + DB fallback, IMemoryCache 5-min sliding window)
- [x] Create `CerbosAuthorizationService` (HTTP API to Cerbos PDP, no gRPC SDK needed)
- [x] Create `FallbackAuthorizationService` (DB-only when Cerbos unavailable)
- [x] Create `ConfigurationChangeLogService` (writes audit entries)
- [x] Add `CerbosSettings` configuration class (Enabled toggle + Endpoint)
- [x] Register all services in `InfrastructureServicesRegistration.cs`

### T6.6 Cerbos YAML Policies
- [x] Create `cerbos/policies/derived_roles.yaml` (instance_admin, tenant_admin, org_admin)
- [x] Create `cerbos/policies/instance_setting.yaml` (admin-only + user view)
- [x] Create `cerbos/policies/tenant_setting.yaml` (lock check: `isLockedByInstance != true`)
- [x] Create `cerbos/policies/organization.yaml` (hierarchy: instance > tenant > org)

### T6.7 Docker & DI
- [x] Add Cerbos sidecar to `docker-compose.yml` (profile: "authz", ports 3592/3593)
- [x] Conditional DI: `Cerbos:Enabled` toggles real vs fallback authorization

### T6.8 Unit Tests
- [x] AuthorizationBehavior tests (5 tests: IAuthorizedRequest allow/deny, CerbosAuthorize allow/deny, pass-through)
- [x] FallbackAuthorizationService tests (14 tests: instance/tenant/org hierarchy, lock semantics, unknown resource deny)
- [x] All tests passing across 5 test projects (199 total)

### T6.9 API Integration
- [x] AuthorizationException → 403 Forbidden in ExceptionMiddleware
- [x] Build: 0 errors
- [x] Architecture tests pass (Clean Architecture compliance)

### T6.10 EF Migration (Deferred)
- [ ] Generate migration when database is available: `dotnet ef migrations add AddConfigurationChangeLog --project Explore.Persistence --startup-project Explore.API`

---

## Remaining Deferred Phases

### Phase 0: Contract and Governance Alignment ⏳ DEFERRED
- [ ] T0.1 Freeze role ownership matrix
- [ ] T0.2 Freeze governance setting key catalog

### Remaining Backend Work ⏳ DEFERRED
- [ ] Implement CQRS commands for settings (UpdateInstanceGovernanceCommand, etc.)
- [ ] Wire [CerbosAuthorize] attribute onto existing controllers
- [ ] Add exception middleware mapping AuthorizationException → 403
- [ ] Add EF migration for ConfigurationChangeLog table
- [ ] Unit tests for AdminContext, AuthorizationBehavior, FallbackAuthorizationService
- [ ] Integration tests for scope matrix (200/401/403)
- [ ] Update Blazor sections to show lock source metadata (IsOverridden, Source, IsLockedByUpperLevel)
- [ ] Update docs and runbooks
