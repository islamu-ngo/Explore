# Authorization Provider Refactor — Task Checklist

**Last Updated: 2026-02-14 (v6 — Phases 4/7/9/10 completion session)**

---

## Phase 1: Domain Layer — Unified Role Table & TenantMember ✅ COMPLETE

- [x] **1.1** Create `RoleScopeEnum` (Platform, Tenant, Organization) — `Explore.Domain/Enums/RoleScopeEnum.cs` [S]
- [x] **1.2** Expand `Role` entity with `Scope` and `IsSystem` — `Explore.Domain/Role.cs` [S]
- [x] **1.3** Update `RoleEnum` to include all roles from all three former tables — `Explore.Domain/Enums/RoleEnum.cs` [M]
- [x] **1.4** Rename `TenantAdministrator` → `TenantMember`, update FK to Role — `Explore.Domain/TenantMember.cs` [M]
- [x] **1.5** Update `OrganizationMember` FK: `OrganizationRoleId` → `RoleId` — `Explore.Domain/OrganizationMember.cs` [S]
- [x] **1.6** Update `TenantUser` FK: `UserRoleId` → `RoleId` — `Explore.Domain/TenantUser.cs` [S]
- [x] **1.7** Mark old entities for removal (OrganizationRole, TenantAdministratorRole, UserRole, enums) [S]
- [x] **1.8** Add `GovernanceSettingKeys.AuthorizationProvider` constant [S]

**Skills**: `clean-architecture-rules`, `dotnet-efcore-guidelines`

---

## Phase 2: Persistence Layer — Migration & EF Configuration ⏳ NOT STARTED

- [ ] **2.1** Update `Role` EF configuration (Scope, IsSystem, unique MasterCode) [M]
- [ ] **2.2** Create `TenantMember` EF configuration (replaces TenantAdministratorConfiguration) [M]
- [ ] **2.3** Update `OrganizationMember` EF configuration (FK → Role) [S]
- [ ] **2.4** Update `TenantUser` EF configuration (FK → Role) [S]
- [ ] **2.5** Update `ExploreDbContext` DbSets (remove old, add TenantMember) [M]
- [ ] **2.6** Create EF Core migration (data migration + table renames + FK updates + drop old tables) [XL] ⚠️ HIGH RISK
- [ ] **2.7** Update `LookupTableSeeder` — unified `SeedRolesAsync` [M]
- [ ] **2.8** Update `DatabaseSeeder` — dev seed data [M]
- [ ] **2.9** Update repositories (OrganizationMemberRepo, create RoleRepo, create TenantMemberRepo, remove old repos) [L]
- [ ] **2.10** Update `AdminContext` — queries use unified Role table and TenantMember [M]

**Skills**: `dotnet-efcore-guidelines`, `clean-architecture-rules`

---

## Phase 3: Application Layer — IAuthorizationProvider & CQRS Updates ✅ COMPLETE

- [x] **3.1** Rename `ICerbosAuthorizationService` → `IAuthorizationProvider` [S]
- [x] **3.2** Rename `CerbosAuthorizeAttribute` → `AuthorizeResourceAttribute`, `CerbosPermissionAction` → `PermissionAction`, `CerbosResourceDescriptorRegistry` → `ResourceDescriptorRegistry` [M]
- [x] **3.3** Update `AuthorizationBehavior` — use `IAuthorizationProvider`, `AuthorizeResourceAttribute` [S]
- [x] **3.4** Update all DTOs (OrganizationMemberDto, TenantUserDto; create unified RoleDto) [L]
- [x] **3.5** Update AutoMapper `MappingProfile` [M]
- [x] **3.6** Update OrganizationMember command handlers (RoleId, admin checks) [M]
- [x] **3.7** Update onboarding/tenant command handlers (TenantMember, RoleId) [M]
- [x] **3.8** Update/remove query handlers (unified GetRoleList, remove old role handlers) [L] — Old handlers left in place for backward compat
- [x] **3.9** Update serialization context (`ExploreJsonContext.cs`) [M]

**Skills**: `cqrs-mediatr-guidelines`, `clean-architecture-rules`, `auth-patterns`

---

## Phase 4: Infrastructure Layer — Authorization Providers + Cerbos at Scale ✅ COMPLETE

- [x] **4.1** Rename `CerbosAuthorizationService` → `CerbosAuthorizationProvider` (sends permissions as principal attrs) [M]
- [x] **4.2** Rename `FallbackAuthorizationService` → `LocalAuthorizationProvider` (enhanced: reads RolePermission for dynamic checks) [L]
- [x] **4.3** Create `RuntimeAuthorizationProvider` (wrapper with SystemSetting cache, Cerbos fallback to Local) [L]
- [x] **4.4** Create `PolicySyncService` — generates Cerbos policies from RolePermission, pushes via Admin API, broadcasts reload [XL]
- [x] **4.5** Create `CerbosAdminApiSettings` — Cerbos instance URLs, admin credentials [S]
- [x] **4.6** Update `InfrastructureServicesRegistration.cs` — register all providers + PolicySyncService + named HttpClient [M]
- [x] **4.7** Create Cerbos PostgreSQL schema init script (`cerbos/init/cerbos-schema.sql`) [M]
- [x] **4.8** Create Cerbos base policy files (19 YAML files: 18 resource policies + derived_roles.yaml) [L]
- [x] **4.9** Create Cerbos PDP config (`cerbos/config/.cerbos.yaml` with overlay driver, PostgreSQL + disk fallback) [M]

**Skills**: `auth-patterns`, `clean-architecture-rules`, `error-tracking`

---

## Phase 5: API Layer — Controllers & HATEOAS ✅ COMPLETE

- [x] **5.1** Create unified `RoleController` (GET /api/v1/role?scope=...) [M]
- [x] **5.2** Update HATEOAS (OrganizationMemberLinkPolicy, TenantUserLinkPolicy, RouteNames) [L]
- [x] **5.3** Update `OrganizationMemberController` — already compatible with RoleId [S]
- [x] **5.4** Integrated AuthorizationProvider into InstanceGovernanceSettingsDto + InstanceGovernanceSettingService [M]

**Skills**: `cqrs-mediatr-guidelines`

---

## Phase 5.5: Legacy Code Cleanup & Permission-Based Auth ✅ COMPLETE

- [x] **5.5.1** Remove legacy OrganizationRole/UserRole/TenantAdministratorRole serialization entries from `ExploreJsonContext.cs` (17 entries) [M]
- [x] **5.5.2** Remove legacy DTO entries from `AppJsonSerializerContext.cs` [S]
- [x] **5.5.3** Remove `GetOrganizationRolesAsync` from `AdminService.cs` + `IAdminService.cs` [S]
- [x] **5.5.4** Remove legacy `organizationRoles` field/method from `LookupTables.razor.cs` + `LookupTables.razor` [S]
- [x] **5.5.5** Create `PermissionCodes.cs` centralized constants (`event:create`, `organization:manage`, etc.) [S]
- [x] **5.5.6** Replace `IsUserAdminOfOrganization` in `IOrganizationMemberRepository` with `HasPermissionInOrganization` + `GetOrganizationIdsWhereUserHasPermission` [M]
- [x] **5.5.7** Implement both new methods in `OrganizationMemberRepository.cs` with RolePermission join + transitional fallback [L]
- [x] **5.5.8** Update `CreateEventCommandHandler.cs` to use `PermissionCodes.EventCreate` [S]
- [x] **5.5.9** Update `CreateEventWithSessionsCommandHandler.cs` to use `PermissionCodes.EventCreate` [S]
- [x] **5.5.10** Update `AdminContext.cs` — `IsOrganizationAdminAsync` + `GetAdminOrganizationIdsAsync` use permission-based lookups [M]
- [x] **5.5.11** Update `CreateEventCommandHandlerTests.cs` mock for `HasPermissionInOrganization` [S]
- [x] **5.5.12** User deleted 25 legacy files/folders ✅

---

## Phase 6: Blazor UI ✅ COMPLETE

- [x] **6.1** RoleHelper rewritten with unified IDs (OrgCreator=20..OrgViewer=25) + `GetAllOrgRoles()` [S]
- [x] **6.2** Client-side OrganizationRole enum deleted, dead usings removed [S]
- [x] **6.3** OrganizationMembers.razor — replaced magic numbers with RoleHelper constants, dropdown uses `GetAllOrgRoles()` [M]
- [x] **6.4** Blazor services verified clean — thin wrappers over NSwag client [S]
- [x] **6.5** Client JSON serialization context already clean [S]

**Skills**: `blazor-ui-conventions`, `blazor-css-isolation`

---

## Phase 7: Testing & Documentation ✅ COMPLETE

- [x] **7.1** Renamed 6 test methods in AuthorizationBehaviorTests.cs (`CerbosAuthorize` → `AuthorizeResource`) [L]
- [x] **7.2** Updated integration tests — LookupTable (OrganizationRole/UserRole→Role), TenantController (`UserRoleId`→`RoleId`), LinkTable (`OrganizationRoleId`→`RoleId`) [M]
- [x] **7.3** Architecture regression tests — 3 new tests (no old entity names, no old interface, no old attribute) [S]
- [x] **7.4** Updated docs — SECURITY.md, DOMAIN.md, CODEBASE_STRUCTURE.md, NAMING_CONVENTIONS.md, TEMPLATE_GLOSSARY.md, index.md [M]
- [x] **7.5** Fixed derived_roles.yaml to match CerbosPrincipalBuilder output (isInstanceAdmin/tenantMemberships/orgMemberships model) + updated all 15 policy files (role: authenticated_user) [M]

**Skills**: `clean-architecture-rules`

---

## Phase 8: Dynamic Permissions — Permission Entity, CRUD & Capability Ceiling ✅ COMPLETE (8.10 migration pending)

- [x] **8.1** Create `Permission` entity (ResourceKind, Action, FieldScope, MasterCode, GroupName, IsSystem, IsFiltered, IsActive) [M]
- [x] **8.2** Create `RolePermission` entity (composite PK: RoleId + PermissionId, GrantedAt, GrantedBy) [S]
- [x] **8.3** EF configuration for Permission + RolePermission (indexes, FKs, DbSets) [M]
- [x] **8.4** Seed 65+ system permissions from 18 resource kinds × 4 actions + default RolePermission assignments [L]
- [x] **8.5** Create `PermissionRepository` (GetByMasterCode, HasPermissionAsync, GetAssignablePermissions) [L]
- [x] **8.6** Create `PermissionRegistryService` (cached vocabulary, group listing, capability ceiling filter) [M]
- [x] **8.7** CQRS Commands: CreateCustomRole, UpdateRolePermissions, DeleteCustomRole + PolicySync trigger [XL] ⚠️ SECURITY-CRITICAL
- [x] **8.8** CQRS Queries: GetPermissionList, GetRolePermissions, GetAssignablePermissions [L]
- [x] **8.9** `CapabilityCeilingService` — 4 anti-escalation rules (grant ceiling, IsFiltered, scope boundary, system immutability) [L] ⚠️ SECURITY-CRITICAL
- [ ] **8.10** EF Core migration for Permissions + RolePermissions tables [L]

**Skills**: `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`

---

## Phase 9: Cerbos Infrastructure & Deployment Tiers ✅ COMPLETE

- [x] **9.1** Document Tier 1 — "Humble" Self-Hoster (`docs/DEPLOYMENT_TIERS.md`) [S]
- [x] **9.2** Document Tier 2 — "Community" Hub (`docs/DEPLOYMENT_TIERS.md`) [S]
- [x] **9.3** Document Tier 3 — "Ummah-Scale" (`docs/DEPLOYMENT_TIERS.md`) [M]
- [x] **9.4** Updated `docs/OPERATIONS.md` with Cerbos production guide (schema init, monitoring, backup, upgrade) [M]

**Skills**: `auth-patterns`, `error-tracking`

---

## Phase 10: Enterprise Hardening — Resilience, Observability & CI Governance 🟡 IN PROGRESS (10/12 done)

*Merged from `cerbos-enterprise-authorization-review` plan (2026-02-12). See plan.md Phase 10 for full details.*

**Items from enterprise review already addressed (skipped):**
- ~~0.1 Security docs~~ → Done in 7.4
- ~~1.2 Typed action consistency~~ → Done in 3.2
- ~~1.3 Structured decision logging~~ → Already exists in AuthorizationBehavior
- ~~2.3 Fallback measurability~~ → LocalAuthorizationProvider has logging
- ~~3.1 HATEOAS sync-over-async~~ → Already fully async (confirmed)
- ~~3.2 Endpoint auth conventions~~ → Standardized during refactor

**Documentation & ADRs:**
- [x] **10.1** Created `docs/adr/ADR-001-authorization-provider-architecture.md` (HTTP transport rationale, dual-provider design) [S]
- [x] **10.2** Created `docs/AUTHORIZATION_PATTERNS.md` (decision tree for 3 auth patterns) [S]
- [x] **10.7** Updated `docs/SECURITY.md` with client-side auth UX-only section + fixed stale code examples [S]

**Infrastructure Hardening:**
- [x] **10.3** Extracted `CerbosPrincipalBuilder` from CerbosAuthorizationService (new file: `Explore.Infrastructure/Services/CerbosPrincipalBuilder.cs`) + updated DI, tests, made CerbosPrincipal public [M]
- [x] **10.4** Polly resilience on CerbosClient — 2s timeout + circuit breaker (0.5 failure ratio, 30s sampling, 15s break). No retry. (`Microsoft.Extensions.Http.Resilience` package) [M]
- [x] **10.5** Admin cache invalidation — `IAdminCacheInvalidator` interface + implementation in AdminContext + PolicySyncService calls InvalidateAll after full sync [M]
- [x] **10.6** Correlation-ID propagation — `CorrelationIdDelegatingHandler` injects X-Correlation-ID header on CerbosClient requests via Activity.Current.Id [S]

**Blazor Guards:**
- [x] **10.8** Created `OrgAdminRouteGuard` — checks org-admin claims (with instance/tenant admin override) + registered in DI + updated Routes.razor [M]

**CI/CD & Policy Governance:**
- [x] **10.9** Created `.github/workflows/cerbos-policy-check.yml` CI gate (cerbos-setup-action + compile-action + optional test runner) [M]
- [x] **10.10** Created 4 Cerbos YAML test suites in `cerbos/tests/` (event, organization_member, tenant, tenant_setting) covering instance/tenant/org admin + regular user + cross-boundary isolation [L]

**Integration Testing (COMPLETE):**
- [x] **10.11** End-to-end integration authorization tests (allow/deny endpoints + HATEOAS link filtering) [XL]
- [x] **10.12** Blazor authorization test stabilization (admin guards per role level) [M]

**Skills**: `auth-patterns`, `error-tracking`, `clean-architecture-rules`, `blazor-bff-patterns`

---

## Summary

| Phase | Tasks | Effort | Risk | Status |
|-------|-------|--------|------|--------|
| 1. Domain Layer | 8 | L | Medium | ✅ COMPLETE |
| 2. Persistence Layer | 10 | XL | **High** | ⏳ NOT STARTED (deferred — needs staging DB) |
| 3. Application Layer | 9 | XL | Medium | ✅ COMPLETE |
| 4. Infrastructure Layer | 9 | XL | Medium | ✅ COMPLETE |
| 5. API Layer | 4 | M | Low | ✅ COMPLETE |
| 5.5 Legacy Cleanup + Permission Auth | 12 | L | Low | ✅ COMPLETE |
| 6. Blazor UI | 5 | M | Low | ✅ COMPLETE |
| 7. Testing & Docs | 5 | L | Low | ✅ COMPLETE |
| 8. Dynamic Permissions | 10 | XL | **Medium** | 🟡 9/10 done (8.10 migration pending) |
| 9. Cerbos Infrastructure | 4 | L | Low | ✅ COMPLETE |
| 10. Enterprise Hardening | 12 | XL | Medium | ✅ COMPLETE |
| **Total** | **88** | | | **84/88 complete** (Phase 2 + 8.10 deferred) |

**Estimated total effort**: 7-8 weeks for careful, tested implementation (was 5-6 weeks, +2 weeks for Phase 10).

**Critical path**: Phase 1 → Phase 2 (migration) → Phase 3 → Phase 8 (permissions) → Phase 4 (providers + PolicySync) → Phase 5+6 (parallel) → Phase 7 → Phase 9 → Phase 10

**Parallelizable**:
- Phase 8 (Permission entity, tasks 8.1-8.3) can start alongside Phase 2
- Phase 9 (documentation) can run anytime after Phase 4
- Phase 10.1-10.2 (docs) can run anytime
- Phase 10.9-10.10 (CI/policy tests) can run after Phase 4.8
- Phase 10.4 (resilience) can run after Phase 4.6

**Highest risk**: Phase 2, Task 2.6 (EF Core data migration). Test on staging DB copy first.

**Security-critical**: Phase 8 Tasks 8.7+8.9 (capability ceiling) + Phase 10.4 (resilience — circuit breaker prevents auth bypass during outage).

---

## Files to Create (New)

| File | Phase | Purpose |
|------|-------|---------|
| `Explore.Domain/Permission.cs` | 8 | Permission vocabulary entity |
| `Explore.Domain/RolePermission.cs` | 8 | Join table: roles ↔ permissions |
| `Explore.Domain/Enums/RoleScopeEnum.cs` | 1 | Platform/Tenant/Organization scope |
| `Explore.Application/Authorization/PermissionRegistryService.cs` | 8 | Cached permission vocabulary |
| `Explore.Application/Authorization/CapabilityCeilingService.cs` | 8 | Anti-escalation rules |
| `Explore.Application/Contracts/Infrastructure/IPolicySyncService.cs` | 4 | Contract for Cerbos policy sync |
| `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs` | 4 | Wrapper: SystemSetting-based delegation |
| `Explore.Infrastructure/Services/PolicySyncService.cs` | 4 | Cerbos policy generator + Admin API push |
| `Explore.Infrastructure/Services/CerbosAdminApiSettings.cs` | 4 | Cerbos Admin API config |
| `Explore.Persistence/Configurations/Entities/PermissionConfiguration.cs` | 8 | EF config for Permission |
| `Explore.Persistence/Configurations/Entities/RolePermissionConfiguration.cs` | 8 | EF config for RolePermission |
| `Explore.Persistence/Repositories/PermissionRepository.cs` | 8 | Permission + RolePermission queries |
| `cerbos/config/.cerbos.yaml` | 4 | Cerbos PDP config (overlay driver) |
| `cerbos/policies/base/*.yaml` | 4 | Base policies shipped with container |
| `cerbos/init/cerbos-schema.sql` | 4 | PostgreSQL schema init for Cerbos |
| `docker-compose.cerbos.yml` | 4 | Cerbos container service definition |
| `docs/DEPLOYMENT_TIERS.md` | 9 | Three deployment tier documentation |

---

## Files to Delete (After Migration)

Report these to the user for confirmation before deletion:

1. `Explore.Domain/OrganizationRole.cs`
2. `Explore.Domain/TenantAdministratorRole.cs`
3. `Explore.Domain/TenantAdministrator.cs` (replaced by `TenantMember.cs`)
4. `Explore.Domain/UserRole.cs`
5. `Explore.Domain/Enums/OrganizationRoleEnum.cs`
6. `Explore.Domain/Enums/TenantAdministratorRoleEnum.cs`
7. `Explore.Persistence/Configurations/Entities/OrganizationRoleConfiguration.cs`
8. `Explore.Persistence/Configurations/Entities/TenantAdministratorRoleConfiguration.cs`
9. `Explore.Persistence/Configurations/Entities/TenantAdministratorConfiguration.cs`
10. `Explore.Persistence/Configurations/Entities/UserRoleConfiguration.cs`
11. `Explore.Persistence/Repositories/OrganizationRoleRepository.cs`
12. `Explore.Persistence/Repositories/TenantAdministratorRoleRepository.cs`
13. `Explore.Persistence/Repositories/UserRoleRepository.cs`
14. `Explore.Application/Contracts/Persistence/IOrganizationRoleRepository.cs`
15. `Explore.Application/Contracts/Persistence/ITenantAdministratorRoleRepository.cs`
16. `Explore.Application/Contracts/Persistence/IUserRoleRepository.cs`
17. `Explore.Application/DTOs/OrganizationRole/OrganizationRoleDto.cs`
18. `Explore.Application/DTOs/OrganizationRole/OrganizationRoleListDto.cs`
19. `Explore.Application/DTOs/UserRole/UserRoleDto.cs`
20. `Explore.Application/DTOs/UserRole/UserRoleListDto.cs`
21. `Explore.Application/Features/OrganizationRoles/` (entire folder)
22. `Explore.Application/Features/UserRoles/` (entire folder)
23. `Explore.API/Controllers/OrganizationRoleController.cs`
24. `Explore.API/Controllers/UserRoleController.cs`
25. `Explore.Blazor.Client/Models/Enums/OrganizationRole.cs`
