# Authorization Provider Refactor — Context

**Last Updated: 2026-02-14 (v4 — Enterprise Hardening Merge from cerbos-enterprise-authorization-review)**

---

## SESSION PROGRESS (2026-02-13)

### Completed (Session 1 — Initial Plan)
- Full codebase analysis of all 4 role tables and their references (648+ across all layers)
- Mapped entire Cerbos authorization infrastructure (services, behaviors, attributes, HATEOAS)
- Analyzed runtime configuration patterns (SystemSetting, TenantCapability, IOptionsMonitor)
- Created comprehensive plan v1 with 7 phases and 45 tasks
- Created all three dev docs files

### Completed (Session 2+3 — Dynamic Permissions + Cerbos at Scale)
- Researched Cerbos Admin API, PostgreSQL storage driver, policy stores, runtime policy management
- Researched dynamic RBAC/ABAC .NET patterns (AuthPermissions.AspNetCore, OrchardCore, ABP Framework)
- Built complete permission inventory: 65+ permissions from 18 resource kinds × 4 actions
- Designed Permission entity, RolePermission join table, PermissionRegistryService, CapabilityCeilingService
- Designed PolicySyncService (generates Cerbos policies from RolePermission data, pushes via Admin API)
- Researched Cerbos horizontal scaling: PostgreSQL store + `compile.cacheDuration` + `/admin/store/reload`
- Discovered overlay storage driver (postgres base + disk fallback) for Cerbos-level resilience
- Confirmed with Cerbos team (community forum): tens of thousands of policies tested, reload is lightweight
- Designed three deployment tiers: Humble (shared PG), Community (PG cluster), Ummah-Scale (separate clusters)
- Updated plan v2 with Phases 8 (Dynamic Permissions) and 9 (Cerbos Infrastructure & Deployment Tiers)
- Updated Phase 4 from disk-based to PostgreSQL store + Admin API with 9 tasks

### Completed (Sessions 4+5 — Implementation)
- **Phase 1**: Domain Layer — RoleScopeEnum, expanded Role entity with Scope/IsSystem, expanded RoleEnum (Platform/Tenant/Organization), TenantMember entity (renamed from TenantAdministrator), OrganizationMember FK rename (OrganizationRoleId→RoleId), TenantUser FK rename (UserRoleId→RoleId), GovernanceSettingKeys.AuthorizationProvider constant
- **Phase 8.1-8.2**: Permission entity (ResourceKind, Action, FieldScope, MasterCode, GroupName, Scope, IsSystem, IsFiltered, IsActive), RolePermission entity (composite PK: RoleId + PermissionId)
- **Phase 3**: Renamed ICerbosAuthorizationService→IAuthorizationProvider, CerbosAuthorizeAttribute→AuthorizeResourceAttribute, CerbosPermissionAction→PermissionAction, CerbosResourceDescriptorRegistry→ResourceDescriptorRegistry, updated AuthorizationBehavior, updated all Application handlers/MappingProfile/test files for property renames
- **Phase 4**: RuntimeAuthorizationProvider (SystemSetting cache, Cerbos fallback to Local), CerbosAdminApiSettings, PolicySyncService (generates Cerbos policies from RolePermission, pushes via Admin API), updated InfrastructureServicesRegistration (register both concrete providers + Runtime + PolicySync + CerbosAdminApi config)
- **Phase 8.3**: EF configurations for Role (unique MasterCode, scope index), Permission (unique MasterCode, resource/action index), RolePermission (composite PK, FK to Role + Permission)
- **Phase 8.4**: Permission seed data — 65 system permissions across 18 resource kinds, Role seed data for unified table
- **Phase 8.5**: IRoleRepository + RoleRepository (GetByScope, GetByMasterCode, GetPermissionsForRole), IPermissionRepository + PermissionRepository (HasPermission, GetAssignable with capability ceiling), DI registration for both

### Completed (Session 6 — DTO & Handler Unification)
- **Phase 3.4**: Updated all DTOs:
  - OrganizationMemberDto: `OrganizationRoleId`→`RoleId`, `OrganizationRoleFullName`→`RoleName`
  - AddOrganizationMemberDto: `OrganizationRoleEnum`→`RoleEnum` (default `RoleEnum.OrgMember`)
  - UpdateOrganizationMemberRoleDto: `OrganizationRoleEnum`→`RoleEnum`
  - OrganizationInvitationDto: `OrganizationRoleEnum`→`RoleEnum`
  - OrganizationListDto: `OrganizationRoleEnum?`→`RoleEnum?`
  - TenantUserDto/TenantUserListDto: `UserRoleId`→`RoleId`, `UserRoleName`→`RoleName`
  - CreateTenantUserDto/UpdateTenantUserDto: `UserRoleId`→`RoleId`
  - Created unified RoleDto and RoleListDto (with Scope, IsSystem)
- **Phase 3.5**: Updated MappingProfile: renamed ForMember targets, added Role→RoleDto/RoleListDto mappings
- **Phase 3.6**: Updated all handlers using OrganizationRoleEnum→RoleEnum:
  - AddOrganizationMemberCommandHandler, UpdateOrganizationMemberRoleCommandHandler, DeleteOrganizationMemberCommandHandler
  - CreateOrganizationCommandHandler, UpdateOrganizationDetailsCommandHandler, DeleteEventCommandHandler
  - GetUserOrganizationsRequestHandler, GetMyOrganizationsRequestHandler
- **Phase 3.7 (partial)**: Updated TenantUser validators + handlers: IUserRoleRepository→IRoleRepository
  - CreateTenantUserDtoValidator, UpdateTenantUserDtoValidator
  - CreateTenantUserCommandHandler, UpdateTenantUserCommandHandler
  - DeleteEventCommandHandler (IUserRoleRepository→IRoleRepository)
- **Phase 3.8 (partial)**: Created unified role queries:
  - GetRoleListRequest + GetRoleListRequestHandler (with scope filter)
  - GetRoleDetailsRequest + GetRoleDetailsRequestHandler
  - Old OrganizationRole/UserRole handlers left in place (controllers still reference them until Phase 5)
- **Phase 3.9**: Updated ExploreJsonContext: added RoleDto/RoleListDto entries (all 10 variants: base, List, IReadOnlyList, HalResource, HalCollectionResource, HalCollectionEmbedded)
- Updated HATEOAS policies: OrganizationMemberLinkPolicy, TenantUserLinkPolicy (old property names→new)
- OrganizationRoleEnum fully eliminated from Application + Infrastructure layers

### Completed (Session 7 — Permission CQRS & DI)
- **Phase 8.6**: PermissionRegistryService created (interface + implementation with IMemoryCache, 10-min TTL)
- **Phase 8.9**: CapabilityCeilingService created (interface + implementation with 4 anti-escalation rules)
- **Phase 8.8**: Permission DTOs created (PermissionDto, PermissionListDto, RolePermissionDto) + 3 query handlers (GetPermissionList, GetRolePermissions, GetAssignablePermissions) + AutoMapper mappings
- **Phase 8.7**: Role CQRS commands complete:
  - CreateCustomRoleCommand + handler (MasterCode generation, capability ceiling, AssignPermissions, PolicySync)
  - UpdateRolePermissionsCommand + handler (system immutability check, ReplacePermissions, PolicySync)
  - DeleteCustomRoleCommand + handler (active member check, RemoveAllPermissions, HardDelete, PolicySync)
- **IRoleRepository extended**: Added `AssignPermissionsAsync`, `ReplacePermissionsAsync`, `RemoveAllPermissionsAsync`, `HasActiveMembersAsync`
- **RoleRepository implemented**: All 4 new methods using `_dbContext.RolePermissions` and member queries
- **DI registration**: ICapabilityCeilingService + IPermissionRegistryService registered in ApplicationServicesRegistration
- **Serialization context**: PermissionDto, PermissionListDto, RolePermissionDto added to ExploreJsonContext (all 6 variant sections × 3 types = 18 entries)
- **AutoMapper**: Permission→PermissionDto and Permission→PermissionListDto mappings added to MappingProfile
- **Phase 5.1**: RoleController created (unified GET /api/v1/role with scope filter, GET /api/v1/role/{id})

### Completed (Session 8 — Legacy Code Cleanup + Permission-Based Auth Refactoring)
- **Legacy code removal** from 12+ files (all OrganizationRole/UserRole/TenantAdministratorRole references):
  - `ExploreJsonContext.cs`: Removed 17 remaining serialization entries across all 6 variant sections
  - `AppJsonSerializerContext.cs`: Removed OrganizationRoleDto, OrganizationRoleListDto, UserRoleDto, UserRoleListDto, OrganizationRole enum entries
  - `AdminService.cs` + `IAdminService.cs`: Removed `GetOrganizationRolesAsync` method from both interface and implementation
  - `LookupTables.razor.cs`: Removed `organizationRoles` field, `LoadOrganizationRolesAsync` call and method
  - `LookupTables.razor`: Removed "Org Roles" MudTabPanel markup
- **Permission-based authorization refactoring** (replaces rigid role-based checks):
  - Created `Explore.Domain/Constants/PermissionCodes.cs` — centralized `{resource_kind}:{action}` format constants
  - Updated `IOrganizationMemberRepository`: replaced `IsUserAdminOfOrganization` with `HasPermissionInOrganization` + `GetOrganizationIdsWhereUserHasPermission`
  - Implemented both methods in `OrganizationMemberRepository.cs` with RolePermission join queries + transitional fallback (when RolePermission table is empty, falls back to legacy admin role ID checks)
  - Updated `CreateEventCommandHandler.cs`: uses `PermissionCodes.EventCreate`
  - Updated `CreateEventWithSessionsCommandHandler.cs`: uses `PermissionCodes.EventCreate`
  - Updated `AdminContext.cs`: `IsOrganizationAdminAsync` uses `PermissionCodes.OrganizationManage`, `GetAdminOrganizationIdsAsync` replaced hardcoded `m.RoleId <= 3` with `GetOrganizationIdsWhereUserHasPermission`
  - Updated `CreateEventCommandHandlerTests.cs`: mock uses `HasPermissionInOrganization` + `PermissionCodes.EventCreate`

### Completed (Session 9+10 — File Cleanup, Phase 5.4, Phase 6, Phase 7)
- **User deleted all 25 legacy files/folders** — build went from 6 errors to 0
- **Post-deletion fixes** (6 compile errors from Blazor client referencing deleted enum):
  - Rewrote `RoleHelper.cs` with unified int constants (OrgCreator=20..OrgViewer=25) matching `RoleEnum`
  - Removed dead `using Explore.Blazor.Client.Models.Enums;` from 3 files (AppJsonSerializerContext.cs, OrganizationDetails.razor.cs, OrganizationDetails.razor)
  - Rewrote `EditMemberRoleDialog.razor` to use `RoleHelper.GetAssignableOrgRoles()` instead of deleted enum iteration
- **Phase 5.4**: Integrated AuthorizationProvider into `InstanceGovernanceSettingsDto` + `InstanceGovernanceSettingService` (read/write/normalize, allowed values: "local"/"cerbos", Category: "Security")
- **Phase 6 COMPLETE**:
  - 6.1: RoleHelper rewritten with unified IDs + `GetAllOrgRoles()` for filter dropdowns
  - 6.2: Client-side OrganizationRole enum deleted, dead usings removed
  - 6.3: OrganizationMembers.razor — replaced all magic numbers (3x `!=4` → `RoleHelper.CanManage`, `!=1` → `!= RoleHelper.OrgCreator`, dropdown uses `GetAllOrgRoles()`)
  - 6.4: Services are clean — thin wrappers over NSwag client, no role-specific logic
  - 6.5: Serialization context already clean
- **Phase 7 IN PROGRESS**:
  - 7.1: Renamed 6 test methods in AuthorizationBehaviorTests.cs (`CerbosAuthorize` → `AuthorizeResource`)
  - 7.2: Updated integration tests — LookupTableControllerTests (replaced OrganizationRole/UserRole endpoints with unified Role endpoints), TenantControllerTests (`UserRoleId`→`RoleId`), LinkTableControllerTests (`OrganizationRoleId`→`RoleId`)
  - 7.4: Updated docs — SECURITY.md (IAuthorizationProvider, AuthorizeResource), DOMAIN.md (unified Role, removed OrganizationRole), CODEBASE_STRUCTURE.md, NAMING_CONVENTIONS.md, TEMPLATE_GLOSSARY.md, index.md

### Completed (Session 11 — Enterprise Hardening Plan Merge)
- **Merged `cerbos-enterprise-authorization-review` plan** into `authorization-provider-refactor` as Phase 10
- **Codebase verification**: Explored agent confirmed current state of all 8 authorization infrastructure files
- **Key confirmations** (enterprise review items already addressed by refactor):
  - HateoasAuthorizationEvaluator is fully async — no GetAwaiter().GetResult() (enterprise 3.1 ✅)
  - AuthorizationBehavior already has structured decision logging with correlationId (enterprise 1.3 ✅)
  - Security docs updated (enterprise 0.1 ✅, done in Phase 7.4)
  - Typed action/resource mapping standardized (enterprise 1.2 ✅, done in Phase 3.2)
  - Fallback measurability covered (enterprise 2.3 ✅, LocalAuthorizationProvider has structured logging)
- **12 net-new enterprise tasks** added as Phase 10 (resilience, CI governance, policy tests, integration tests, ADRs)
- **Research collected** (Tavily + librarian):
  - Cerbos CI: Use `cerbos/cerbos-setup-action@v1` + `cerbos/cerbos-compile-action@v1` in GitHub Actions
  - Cerbos test fixtures: YAML format with principals, resources, actions, expected (EFFECT_ALLOW/EFFECT_DENY)
  - Cerbos audit: Built-in decision logs (file/Kafka/local backends), JSON format with callId, principal, resource, actions, effect
  - Polly resilience: `AddResilienceHandler` with timeout (2s) + circuit breaker (no retry for auth — fail-fast to Local)
  - HATEOAS async: batch authorization checks via `IsAllowedBatchAsync` (already implemented)
- **`cerbos-enterprise-authorization-review` archived** to `dev/archive/`
- Total plan now: 88 tasks across 10 phases (+1 sub-phase), estimated 7-8 weeks

### 🟡 IN PROGRESS
- Phase 7 remaining: 7.3 (architecture tests — verify no old entity names remain), 7.5 (Cerbos YAML policies)
- Phase 8.10 (EF Core migration for Permission/RolePermission tables)

### Blockers
- Phase 2 (EF Core migration) deferred — HIGH RISK, needs staging DB copy for testing
- Cerbos Docker/config files (4.7-4.9) — infrastructure-only, not blocking code

### Build & Tests
- **0 errors, 660+ tests pass** (230 unit + 61 domain + 24 architecture + 406 Blazor — integration tests not run locally)

---

## Key Decisions Made

### 1. Single Role Table Strategy
**Decision**: Expand existing `Role` entity with `Scope` and `IsSystem` columns. Use `MasterCode` as the stable code reference (e.g., `org.admin`), not integer IDs.

**Why**: Avoids creating yet another new entity. The `Role` entity already exists and is currently unused. Adding scope + system flag makes it the universal role container.

### 2. TenantAdministrator → TenantMember (not TenantUser merge)
**Decision**: Rename `TenantAdministrator` to `TenantMember` to match `OrganizationMember` pattern. Keep `TenantUser` separate.

**Why**: `TenantUser` represents "user belongs to tenant" (a basic membership). `TenantMember` represents "user has an administrative role in tenant" (elevated access). Different semantics, different tables. Merging them would conflate "membership" with "administration."

### 3. IAuthorizationProvider Naming (not IPermissionService)
**Decision**: Use `IAuthorizationProvider` as the interface name, with `CerbosAuthorizationProvider` and `LocalAuthorizationProvider` implementations.

**Why**: "Provider" matches the PDP concept. "Service" is already overloaded. The existing `FallbackAuthorizationService` name implied it was a backup — `LocalAuthorizationProvider` makes it clear this is a first-class implementation choice.

### 4. RuntimeAuthorizationProvider Wrapper Pattern
**Decision**: Instead of changing DI registration at runtime (impossible in .NET), create a wrapper that reads `SystemSetting` to decide which inner provider to delegate to.

**Why**: .NET DI container is built at startup. You can't swap registrations at runtime. The wrapper pattern with a cached setting read is the established pattern in this codebase (see `ModuleService` with 5-min cache).

### 5. LocalProvider = 5% Logic, Not Full Cerbos Clone
**Decision**: `LocalAuthorizationProvider` only does basic RBAC (is user admin? allow/deny). It does NOT replicate Cerbos's ABAC capabilities (madhab filters, event attributes, derived roles).

**Why**: This is the "Holy Grail" tradeoff. Casual self-hosters get a working system. Enterprise users who need cultural filtering spin up Cerbos. We don't duplicate 95% of complex logic.

### 6. Keep RoleEnum for Code References
**Decision**: Maintain a `RoleEnum` with all built-in role IDs for type-safe code references, but the `MasterCode` string is the true stable identifier.

**Why**: Enum gives compile-time safety for common checks (is admin?). MasterCode gives extensibility (custom roles can be added without enum changes).

### 7. PostgreSQL Store for Cerbos (NOT Disk, NOT Hub)
**Decision**: Use Cerbos's PostgreSQL storage driver with the Admin API for policy management. No disk-based policies for dynamic content. No Cerbos Hub (too expensive for non-profit).

**Why**: The platform is designed for horizontal scale — API, Blazor, and Cerbos deployed as separate containers, potentially on different servers. Disk-based approaches require shared volumes which breaks this model. PostgreSQL store allows N stateless Cerbos instances sharing the same policy database. Admin API enables programmatic policy push from our .NET app.

### 8. Separate Cerbos PostgreSQL at Scale (Tier 3)
**Decision**: At Ummah-Scale, use a physically separate PostgreSQL cluster for Cerbos policies. At lower tiers, shared PostgreSQL with separate schemas is acceptable.

**Why**: Resource isolation (PostGIS queries can't starve permission checks), security boundary (SQL injection in app can't touch authorization policies), and the latency myth (Cerbos caches in memory, talks to DB once per minute — separate DB has zero perceptible latency impact).

### 9. Overlay Storage Driver (Cerbos-Level Fallback)
**Decision**: Use Cerbos's `overlay` driver with `postgres` as base and `disk` as fallback. Base policies shipped with the container image provide degraded-but-functional authorization if PostgreSQL is unreachable.

**Why**: Two-layer resilience. Application layer: RuntimeAuthorizationProvider falls back to LocalAuthorizationProvider. Cerbos layer: overlay driver falls back to disk-based base policies. Belt AND suspenders.

### 10. Eventually Consistent Multi-Instance (`compile.cacheDuration: 60s`)
**Decision**: Each Cerbos instance caches compiled policies for 60 seconds. For critical changes (revoking admin access), broadcast `GET /admin/store/reload?wait=true` to all instances.

**Why**: Confirmed by Cerbos team: "The reload call just makes Cerbos clear its cache. Reloading them into the cache has very little overhead." 60-second eventual consistency is perfectly acceptable for permission changes. Immediate consistency is available when needed via explicit reload.

### 11. Permission MasterCode Convention
**Decision**: `{resource_kind}:{action}` or `{resource_kind}:{action}:{field}`. Examples: `event:update`, `event:update:description`, `organization_member:create`.

**Why**: Maps directly to Cerbos resource kind + action model. Field-level granularity (`:field` suffix) enables "can edit descriptions but not delete events" without changing the Cerbos integration model.

### 12. Capability Ceiling (Anti-Escalation)
**Decision**: Four rules prevent privilege escalation: (1) Can only grant permissions you have, (2) `IsFiltered` hides dangerous perms from non-super-admins, (3) Scope boundary (tenant admin can't create platform-scope roles), (4) System roles are immutable.

**Why**: Zero-trust. The system is default-deny. Every permission grant is explicit and bounded by the granter's own ceiling. Inspired by AuthPermissions.AspNetCore's `AutoGenerateFilter` pattern.

### 14. Enterprise Hardening as Phase 10 (Merged from cerbos-enterprise-authorization-review)
**Decision**: Merge the 12 remaining net-new items from the enterprise review into the authorization-provider-refactor plan as Phase 10, rather than maintaining two separate plans. Archive the enterprise review.

**Why**: The enterprise review was created before the refactor began (2026-02-12), based on old naming (CerbosAuthorizationService, FallbackAuthorizationService, etc.). The refactor (sessions 1-10) already addressed 6 of the 18 enterprise items. Maintaining two plans creates confusion. Phase 10 captures what's left: resilience (Polly circuit breaker), CI governance (Cerbos compile/test gates), policy test matrix, integration tests, ADRs, and Blazor guard refinement.

**What was already done** (no need to re-implement):
- HATEOAS async boundary — already fully async
- Structured decision logging — already in AuthorizationBehavior with correlationId
- Security docs alignment — done in Phase 7.4
- Typed action/resource consistency — done in Phase 3.2
- Fallback measurability — LocalAuthorizationProvider has structured logging

### 13. Permission-Based Auth Over Role-Based Auth (HasPermissionInOrganization)
**Decision**: Replace `IsUserAdminOfOrganization` (checks if user has OrgCreator/OrgCoOwner/OrgAdmin role) with `HasPermissionInOrganization` (checks if user's role has the specific permission via RolePermission join table). Added a transitional fallback: when RolePermission table is empty (no permissions seeded yet), fall back to legacy admin role ID checks.

**Why**: The old approach was rigid — hardcoded role IDs meant adding a new role required code changes. The new approach is data-driven: permissions are assigned to roles in the database, so custom roles can get specific permissions without code changes. The transitional fallback ensures the system keeps working during the migration period before permission seed data is deployed.

---

## Key Files (Current State)

### Domain Layer — Entities to Modify
| File | What Changes |
|------|-------------|
| `Explore.Domain/Role.cs` | Add `Scope`, `IsSystem` properties |
| `Explore.Domain/OrganizationMember.cs` | `OrganizationRoleId` → `RoleId` |
| `Explore.Domain/TenantUser.cs` | `UserRoleId` → `RoleId` |
| `Explore.Domain/TenantAdministrator.cs` | Rename → `TenantMember.cs`, `TenantAdministratorRoleId` → `RoleId` |

### Domain Layer — Files to Remove
| File | Reason |
|------|--------|
| `Explore.Domain/OrganizationRole.cs` | Merged into `Role` |
| `Explore.Domain/TenantAdministratorRole.cs` | Merged into `Role` |
| `Explore.Domain/UserRole.cs` | Merged into `Role` |
| `Explore.Domain/Enums/OrganizationRoleEnum.cs` | Merged into expanded `RoleEnum` |
| `Explore.Domain/Enums/TenantAdministratorRoleEnum.cs` | Merged into expanded `RoleEnum` |

### Authorization Infrastructure — Files to Rename
| Current File | New Name |
|-------------|----------|
| `Application/Contracts/Infrastructure/ICerbosAuthorizationService.cs` | `IAuthorizationProvider.cs` |
| `Application/Authorization/CerbosAuthorizeAttribute.cs` | `AuthorizeResourceAttribute.cs` |
| `Application/Authorization/CerbosPermissionAction.cs` | `PermissionAction.cs` |
| `Application/Authorization/CerbosResourceDescriptorRegistry.cs` | `ResourceDescriptorRegistry.cs` |
| `Infrastructure/Services/CerbosAuthorizationService.cs` | `CerbosAuthorizationProvider.cs` |
| `Infrastructure/Services/FallbackAuthorizationService.cs` | `LocalAuthorizationProvider.cs` |

### Domain Layer — New Entities
| File | Purpose |
|------|---------|
| `Explore.Domain/Permission.cs` | Permission vocabulary entity (ResourceKind + Action + FieldScope) |
| `Explore.Domain/RolePermission.cs` | Join table: which roles have which permissions |
| `Explore.Domain/Enums/RoleScopeEnum.cs` | Platform/Tenant/Organization scope enum |

### Domain Layer — New Constants
| File | Purpose |
|------|---------|
| `Explore.Domain/Constants/PermissionCodes.cs` | Centralized `{resource_kind}:{action}` permission constants (event:create, organization:manage, etc.) |

### Authorization Infrastructure — New Files
| File | Purpose |
|------|---------|
| `Infrastructure/Services/RuntimeAuthorizationProvider.cs` | Wrapper that delegates based on SystemSetting |
| `Infrastructure/Services/PolicySyncService.cs` | Generates Cerbos policies from RolePermission, pushes via Admin API |
| `Infrastructure/Services/CerbosAdminApiSettings.cs` | Config: Cerbos instance URLs, admin credentials |
| `Application/Authorization/PermissionRegistryService.cs` | Code-defined permission vocabulary, cached from DB |
| `Application/Authorization/CapabilityCeilingService.cs` | Anti-escalation rules for role/permission management |
| `Application/Contracts/Infrastructure/IPolicySyncService.cs` | Contract for policy sync to Cerbos |
| `Persistence/Configurations/Entities/PermissionConfiguration.cs` | EF config for Permission entity |
| `Persistence/Configurations/Entities/RolePermissionConfiguration.cs` | EF config for RolePermission join table |
| `Persistence/Repositories/PermissionRepository.cs` | Permission + RolePermission queries |

### Cerbos Infrastructure — New Files
| File | Purpose |
|------|---------|
| `cerbos/config/.cerbos.yaml` | Cerbos PDP config (overlay driver, PostgreSQL + disk fallback) |
| `cerbos/policies/base/*.yaml` | Base resource + derived role policies shipped with container |
| `cerbos/init/cerbos-schema.sql` | PostgreSQL schema init script for Cerbos tables |
| `docker-compose.cerbos.yml` | Cerbos container service definition |
| `docs/DEPLOYMENT_TIERS.md` | Three deployment tier documentation |

### Key Existing Patterns to Follow
| Pattern | File | How It Works |
|---------|------|-------------|
| Module cache + invalidation | `Infrastructure/Services/ModuleService.cs` | IMemoryCache with 5-min TTL, explicit invalidation on change |
| Conditional DI registration | `Infrastructure/InfrastructureServicesRegistration.cs` | `if (cerbosEnabled)` → register real vs fallback |
| Cascading settings | `SystemSetting` → `TenantSetting` | System-level default, tenant can override unless locked |
| Config change audit | `ConfigurationChangeLogService` | Every setting change logged with old/new value, scope, user |
| Lookup seed pattern | `LookupTableSeeder.cs` | Check-before-insert, runs in all environments |

---

## Architecture Diagram (Future State)

```
Authorization Request Flow (Application Layer):
                                                        
  Command/Query → AuthorizationBehavior → IAuthorizationProvider
                                                 ↓
                                     RuntimeAuthorizationProvider
                                      (reads SystemSetting with 1-min cache)
                                           ↓          ↓
                         ┌─────────────────┘          └──────────────────┐
                         ↓                                               ↓
           CerbosAuthorizationProvider                   LocalAuthorizationProvider
            (HTTP → Cerbos PDP pool)                     (DB → Permission + RolePermission)
            Full ABAC: derived roles,                    Basic RBAC: role-based lookup,
            conditions, cultural filters                 default deny, zero infra needed
                                                        

Cerbos Infrastructure (Horizontal Scale):
                                                        
  ┌───────────────────────────────────────────────────────────────────┐
  │                    Cerbos PDP Pool (N instances)                   │
  │                    Behind load balancer, stateless                 │
  │                                                                   │
  │  Instance 1 ──┐                                                   │
  │  Instance 2 ──┼── All read from same PostgreSQL (Cerbos schema)   │
  │  Instance 3 ──┘   compile.cacheDuration: 60s (eventually consist.)│
  │                                                                   │
  │  Storage: overlay driver                                          │
  │    ├── base: postgres (dynamic policies from Admin API)           │
  │    └── fallback: disk (base policies shipped with container)      │
  └───────────────────────────────────────────────────────────────────┘
            ↑ POST /admin/policy              ↑ GET /admin/store/reload
            │ (push new policies)             │ (force cache clear)
            │                                 │
  ┌─────────┴─────────────────────────────────┴───────┐
  │              PolicySyncService (.NET)               │
  │  Generates Cerbos derived role + resource policies  │
  │  from Permission + RolePermission tables            │
  │  Triggered by: CreateCustomRoleCommand,             │
  │                UpdateRolePermissionsCommand          │
  └────────────────────────────────────────────────────┘


Dynamic Permission Model:
                                                        
  Permission (vocabulary)          RolePermission (assignment)
  ├── MasterCode: "event:update"   ├── RoleId → Role
  ├── ResourceKind: "event"        └── PermissionId → Permission
  ├── Action: "update"
  ├── Scope: Organization           Role (unified)
  ├── IsSystem: true                ├── MasterCode: "org.editor"
  ├── IsFiltered: false             ├── Scope: Organization
  └── IsActive: true                ├── IsSystem: false (custom)
                                    └── Permissions: [event:update, event:update:description]

  Capability Ceiling:
  ├── Rule 1: Can only grant permissions you have
  ├── Rule 2: IsFiltered hides dangerous perms from non-super-admins
  ├── Rule 3: Scope boundary (tenant admin can't create platform roles)
  └── Rule 4: System roles (IsSystem=true) are immutable


Deployment Tiers:
                                                        
  Tier 1 "Humble":     [App] → [Single PostgreSQL (public + cerbos schemas)]
                        Cerbos optional — LocalAuthorizationProvider works alone

  Tier 2 "Community":  [App] → [PG Primary + Replica]
                        [Cerbos x2] → reads from Replica, writes to Primary

  Tier 3 "Ummah":      [App] → [PG Cluster A: PostGIS/Events]
                        [Cerbos xN] → [PG Cluster B: Cerbos policies]
                        Total isolation, horizontal scale, zero blast radius
```

---

## Research Artifacts (Collected, Not Yet Lost)

These are key findings from background research agents that informed the plan. If you need the raw data, the plan already incorporates all conclusions.

### Permission Inventory (from codebase analysis — bg_f0da053d)
- **18 resource kinds** in `CerbosResourceDescriptorRegistry`: organization, tenant_setting, event, tenant, user, tenant_user, tag, storage_object, organization_review, organization_member, location, indexed_did, event_session, event_session_agenda_item, event_registration, category, atproto_record, instance_setting
- **4 actions**: Read, Create, Update, Delete (in `CerbosPermissionAction` enum)
- **65+ unique resource:action pairs** currently used across HATEOAS LinkPolicies, `IsAllowedAsync` calls, and `[CerbosAuthorize]` attributes
- **Notable gaps**: `storage_object` has no `update`, `atproto_record` has no `update`, `tenant_setting` has no `create`/`delete`, `user` has no `create`/`delete`, `indexed_did` has no `delete`

### Cerbos Scaling Research (from Tavily + Context7 + Cerbos community)
- Cerbos PostgreSQL driver requires **manual table creation** — they provide a SQL script, don't auto-migrate (security: minimal privileges)
- Cerbos **overlay driver** = postgres base + disk fallback with circuit breaker (`fallbackErrorThreshold: 5`, `fallbackErrorWindow: 5s`)
- **Charith (Cerbos team)** confirmed: "Cerbos is not bounded by the number of policies. Both us and some of our customers have independently tested Cerbos with tens of thousands of policies successfully."
- **Sharding**: "If the number of policies gets too large, you have the option of sharding them over several Cerbos instances." Not native — done via proxy routing.
- **Admin API** requires Basic Auth, mutable store (postgres/mysql/sqlite3), endpoints: `POST /admin/policy`, `GET /admin/store/reload?wait=true`, `GET /admin/policies`, `POST /admin/policy/delete`
- **Multi-instance consistency**: `compile.cacheDuration: 60s` for eventual consistency; `compile.cacheSize: 0` to disable cache entirely (not recommended); explicit `/admin/store/reload` on all instances for immediate consistency

### Dynamic Permission Patterns (from .NET research — bg_e179f90f)
- **AuthPermissions.AspNetCore** (JonPSmith): Enum-based permissions packed as unicode chars in claims. `AutoGenerateFilter` attribute hides dangerous perms from tenant admins. Production-tested.
- **Permission naming**: Industry standard is `{Module}.{Action}.{Field}` or `{resource}:{action}:{field}`. Both work; we chose colon-separator to match Cerbos's resource:action model.
- **Capability ceiling**: The "you can only grant what you have" rule is standard in multi-tenant RBAC. AuthPermissions.AspNetCore uses `PermissionDisplay.GetPermissionsToDisplay(excludeFiltered: true)`.
- **Caching strategy**: L1 (IMemoryCache per instance) + L2 (DB as source of truth). For our case, the DB is the only level since we're not doing claims-based permission packing.

---

## Dependencies & Prerequisites

| Dependency | Status | Notes |
|-----------|--------|-------|
| All tests passing | Must verify | Run full test suite before starting |
| No pending migrations | Must verify | Check migration state |
| Staging DB backup | Required | Before migration testing |
| Cerbos PostgreSQL schema script | Need to obtain | From Cerbos docs or `cerbosctl` — tables: policy, policy_dependency, policy_ancestor, attr_schema_defs |
| Cerbos container image | Available | `ghcr.io/cerbos/cerbos:latest` (Apache 2.0 licensed) |

---

## Current State Summary (for next session)

### What exists now
- **Phases 1, 3, 5, 5.5, 6, 8 (except 8.10) are COMPLETE** — Domain, Application, API, Legacy Cleanup, Blazor UI, Dynamic Permissions all done
- **Phases 4.1–4.6 are COMPLETE** — Infrastructure providers done (4.7-4.9 Cerbos Docker/config deferred)
- **Phase 7 IN PROGRESS** — Test renames done (7.1, 7.2), docs updated (7.4). Remaining: 7.3, 7.5
- **25 legacy files deleted by user** — build is clean: 0 errors, 660+ tests pass
- **Build status**: 0 errors, 0 test failures

### Remaining work
1. **Phase 7.3**: Architecture tests — verify no old entity names remain (currently passing)
2. **Phase 7.5**: Cerbos YAML policies (role MasterCodes, principal attributes)
3. **Phase 4.7-4.9**: Cerbos Docker/config files (infrastructure-only)
4. **Phase 8.10**: EF Core migration for Permission/RolePermission tables
5. **Phase 2**: EF Core data migration (HIGH RISK, deferred until staging DB)
6. **Phase 9**: Deployment tier documentation
7. **Phase 10**: Enterprise Hardening (12 tasks merged from enterprise review):
   - 10.1-10.2, 10.7: Documentation & ADRs (S effort, parallelizable now)
   - 10.4: Resilience policy hardening — Polly circuit breaker on Cerbos HttpClient (M, HIGH PRIORITY)
   - 10.9-10.10: Cerbos CI gate + permission matrix tests (M+L, HIGH PRIORITY, after 4.8)
   - 10.3, 10.5, 10.6: Infrastructure refinements (M each)
   - 10.8: Blazor org-admin route policy (M)
   - 10.11-10.12: Integration + Blazor test stabilization (XL+M, last)

---

## Quick Resume Instructions

1. Read this file first — it has full session state and all decisions
2. Read `authorization-provider-refactor-tasks.md` for the task checklist
3. **FIRST**: Check if user has deleted the 25 files yet. If not, remind them.
4. Build to verify: `dotnet build --configuration Release --verbosity quiet`
5. Pick next task from remaining work list above
6. Run `dotnet build` after each change to catch cascading issues early
7. Load skills: `clean-architecture-rules`, `dotnet-efcore-guidelines`, `cqrs-mediatr-guidelines`, `auth-patterns`
