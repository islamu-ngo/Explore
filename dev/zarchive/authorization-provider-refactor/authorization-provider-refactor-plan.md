# Authorization Provider Refactor — Implementation Plan

**Last Updated: 2026-02-14 (v3 — Enterprise Hardening Merge from cerbos-enterprise-authorization-review)**

---

## Executive Summary

This refactor transforms the ISLAMU Event platform's authorization system from a Cerbos-coupled, multi-role-table architecture into a **unified, provider-agnostic, dynamically-configurable, Ummah-scale** authorization system. Four interconnected goals:

1. **Unified Role Table**: Consolidate `OrganizationRole`, `TenantAdministratorRole`, and `UserRole` into a single `Role` table. Remove `OrganizationRole` entity. Contextual permissions are provided by the membership tables (who has what role, where).
2. **TenantMember Pattern**: Rename `TenantAdministrator` to `TenantMember` to mirror the `OrganizationMember` pattern — consistent "member + role" semantics at both tenant and organization scope.
3. **IAuthorizationProvider Abstraction**: Decouple from Cerbos-specific naming. Introduce a runtime-switchable `IAuthorizationProvider` with two implementations: `CerbosAuthorizationProvider` (advanced PDP) and `LocalAuthorizationProvider` (simplified RBAC fallback). Enable switching via `SystemSetting` without app restart.
4. **Dynamic Permissions at Runtime**: Admins can create custom roles with granular permissions (e.g., "can edit event descriptions but not delete events") without restarting the app. A `Permission` entity defines the vocabulary, `RolePermission` assigns them, and a `PolicySyncService` pushes changes to Cerbos via the Admin API.

### The Self-Hosted "Holy Grail" — Three Deployment Tiers

| Tier | Name | Database | Cerbos | Ops Tax | Capability |
|------|------|----------|--------|---------|-----------|
| **1** | **The "Humble" Self-Hoster** | Single PostgreSQL (shared schemas) | Optional — `LocalAuthorizationProvider` works without it | Zero | Basic RBAC: role + permission lookup from DB |
| **2** | **The "Community" Hub** | Single PostgreSQL Cluster (Primary + Replica) | 1-2 instances, reads from replica | Low | Full ABAC with Cerbos, HA reads |
| **3** | **The "Ummah-Scale" Platform** | Two Separate DB Clusters (App DB + Cerbos DB) | N instances behind load balancer | Medium | Total isolation, horizontal scale, zero blast radius |

The Local provider covers ~5% of logic (the must-haves: CRUD gating by role). The 95% (madhab filters, gender rules, event-attribute-based policies) is Cerbos-only. This is intentional — not a bug.

### Why Separate Databases at Scale (Tier 3)

1. **Resource Isolation** ("The Bouncer Principle"): Heavy PostGIS spatial queries on the app DB can't spike I/O and slow down permission checks. Cerbos gets its own clear path.
2. **Security Boundary** ("Blast Radius"): If an SQL injection vulnerability is found in the application, the attacker can see Events and Users — but cannot touch Cerbos policies because that database is on a completely different connection string, potentially in a different network segment. They can't promote themselves to Admin.
3. **The Latency Myth**: With `compile.cacheDuration: 60s`, Cerbos instances talk to the DB once a minute (or on explicit reload). 99.9% of authorization checks happen in-memory. Separate DB latency is practically zero for the end-user.

---

## Current State Analysis

### Role Tables (4 separate tables — redundant)

| Table | Entity | Enum | Scope | Used By | Status |
|-------|--------|------|-------|---------|--------|
| `OrganizationRoles` | `OrganizationRole` | `OrganizationRoleEnum` (Creator, CoOwner, Admin, Moderator, Member, Viewer) | Global lookup | `OrganizationMember.OrganizationRoleId` | **REMOVE** — merge into `Role` |
| `TenantAdministratorRoles` | `TenantAdministratorRole` | `TenantAdministratorRoleEnum` (TenantOwner, TenantAdmin, TenantModerator) | Global lookup | `TenantAdministrator.TenantAdministratorRoleId` | **REMOVE** — merge into `Role` |
| `UserRoles` | `UserRole` | N/A | Tenant-scoped | `TenantUser.UserRoleId` | **REMOVE** — tenant-scoped roles become `Role` rows |
| `Roles` | `Role` | `RoleEnum` (SuperAdmin, Admin, Moderator, Editor, Member) | Global lookup | **Not actively referenced** | **KEEP & EXPAND** as the single Role table |

### Member/Admin Tables (inconsistent naming)

| Table | Entity | Pattern | Scope |
|-------|--------|---------|-------|
| `OrganizationMembers` | `OrganizationMember` | User + Organization + Role + Position | Tenant-scoped |
| `TenantAdministrators` | `TenantAdministrator` | User + Tenant + Role | Global |
| `TenantUsers` | `TenantUser` | User + Tenant + UserRole | Tenant-scoped |

**Problem**: `TenantAdministrator` doesn't follow the "Member" naming convention. `TenantUser` is basically a member too but with different semantics. We need consistency.

### Authorization Infrastructure (well-architected, Cerbos-named)

| Component | File | Description |
|-----------|------|-------------|
| `ICerbosAuthorizationService` | Application/Contracts/Infrastructure/ | Core contract (IsAllowedAsync, IsAllowedBatchAsync, CheckSettingAccessAsync) |
| `CerbosAuthorizationService` | Infrastructure/Services/ | HTTP client to Cerbos PDP |
| `FallbackAuthorizationService` | Infrastructure/Services/ | DB-driven fallback (already exists!) |
| `AuthorizationBehavior` | Application/Behaviors/ | MediatR pipeline enforcer |
| `CerbosAuthorizeAttribute` | Application/Authorization/ | Command decoration |
| `CerbosResourceDescriptorRegistry` | Application/Authorization/ | DTO → resource kind mapping |
| `CerbosSettings` | Infrastructure/Services/ | Config (Enabled, Endpoint) |
| DI Registration | Infrastructure/InfrastructureServicesRegistration.cs | Conditional: `Cerbos:Enabled` → real vs fallback |

**Key insight**: The abstraction already exists (`ICerbosAuthorizationService` with two implementations). We're renaming it and making the switch runtime-configurable instead of startup-only.

---

## Proposed Future State

### Single Role Table

```
Role (Unified)
├── Id (int, PK)
├── FullName (string, required)
├── MasterCode (string, required, unique)
├── Description (string, nullable)
├── Scope (RoleScopeEnum: Platform, Tenant, Organization)
└── IsSystem (bool) — prevents deletion of built-in roles
```

**Seed Data** (single table):

| Id | MasterCode | FullName | Scope | IsSystem |
|----|-----------|----------|-------|----------|
| 1 | `platform.super_admin` | Super Admin | Platform | true |
| 2 | `platform.admin` | Admin | Platform | true |
| 3 | `platform.moderator` | Moderator | Platform | true |
| 10 | `tenant.owner` | Tenant Owner | Tenant | true |
| 11 | `tenant.admin` | Tenant Admin | Tenant | true |
| 12 | `tenant.moderator` | Tenant Moderator | Tenant | true |
| 13 | `tenant.member` | Tenant Member | Tenant | true |
| 20 | `org.creator` | Creator | Organization | true |
| 21 | `org.co_owner` | Co-Owner | Organization | true |
| 22 | `org.admin` | Admin | Organization | true |
| 23 | `org.moderator` | Moderator | Organization | true |
| 24 | `org.member` | Member | Organization | true |
| 25 | `org.viewer` | Viewer | Organization | true |

The `MasterCode` is the stable identifier for code references (not the int `Id`). The `Scope` tells us where this role applies. `IsSystem` prevents users from deleting built-in roles.

### Consistent Member Pattern

```
TenantMember (renamed from TenantAdministrator)
├── Id (Guid)
├── UserId (FK → User)
├── TenantId (FK → Tenant)
├── RoleId (FK → Role)  // was TenantAdministratorRoleId
├── GrantedAt (DateTime)
└── GrantedBy (Guid?)

OrganizationMember (updated FK)
├── Id (Guid)
├── OrganizationId (FK → Organization)
├── UserId (FK → User)
├── RoleId (FK → Role)  // was OrganizationRoleId
├── OrganizationPositionId (FK → OrganizationPosition, nullable)
├── TenantId (FK → Tenant)
└── ... (audit + soft delete)
```

**TenantUser stays as-is** for now — it represents "user belongs to tenant" (not admin). Its `UserRoleId` FK also points to the unified `Role` table.

### IAuthorizationProvider (renamed from ICerbosAuthorizationService)

```csharp
public interface IAuthorizationProvider
{
    Task<bool> IsAllowedAsync(
        string resourceKind, string resourceId, string action,
        IDictionary<string, object>? resourceAttributes = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken ct = default);

    Task<bool> CheckSettingAccessAsync(
        string settingKey, string action,
        Guid? tenantId = null, Guid? organizationId = null,
        CancellationToken ct = default);
}
```

**Implementations**:
- `CerbosAuthorizationProvider` — wraps existing `CerbosAuthorizationService` logic
- `LocalAuthorizationProvider` — wraps existing `FallbackAuthorizationService` logic + explicit role-based RBAC

### Permission Entity (Dynamic Permissions Vocabulary)

```
Permission
├── Id (int, PK)
├── ResourceKind (string, required) — e.g., "event", "organization"
├── Action (string, required) — e.g., "create", "update", "delete"
├── FieldScope (string?, nullable) — e.g., "description", "title" (for field-level)
├── MasterCode (string, unique, required) — e.g., "event:update:description"
├── FullName (string, required) — e.g., "Edit Event Description"
├── Description (string?)
├── GroupName (string, required) — e.g., "Events" (for UI grouping)
├── Scope (RoleScopeEnum) — which scope level can use this permission
├── IsSystem (bool) — prevents deletion of built-in permissions
├── IsFiltered (bool) — hides from tenant/org admins (capability ceiling)
└── IsActive (bool) — soft-disable without deletion
```

**Seed Data**: 65+ permissions derived from the 18 resource kinds × 4 actions currently in the codebase (see `CerbosResourceDescriptorRegistry`). Field-level permissions (e.g., `event:update:description`) added as the system matures.

**MasterCode Convention**: `{resource_kind}:{action}` or `{resource_kind}:{action}:{field}` — matches Cerbos resource/action model exactly.

### RolePermission (Join Table)

```
RolePermission
├── RoleId (int, FK → Role)
├── PermissionId (int, FK → Permission)
├── GrantedAt (DateTime)
└── GrantedBy (Guid?)
```

**Composite PK**: (RoleId, PermissionId). A role "has" a set of permissions. When the `LocalAuthorizationProvider` evaluates access, it looks up `RolePermission` directly. When `CerbosAuthorizationProvider` is active, the `PolicySyncService` generates Cerbos derived roles from this same data.

### PolicySyncService (Cerbos Policy Generator)

```
PolicySyncService
├── GenerateDerivedRolePolicy(Role role, IReadOnlyList<Permission> permissions)
│   → Produces Cerbos derived role YAML from RolePermission data
├── GenerateResourcePolicy(string resourceKind, IReadOnlyList<Permission> permissions)
│   → Produces Cerbos resource policy YAML for a resource kind
├── PushPoliciesAsync(IReadOnlyList<CerbosPolicy> policies)
│   → POST /admin/policy to Cerbos Admin API
├── ReloadAllInstancesAsync()
│   → GET /admin/store/reload?wait=true on all known Cerbos instances
└── SyncAllPoliciesAsync()
    → Full resync: generate all policies from Permission + RolePermission tables
```

**Trigger points**: Called after `CreateRoleCommand`, `UpdateRolePermissionsCommand`, `DeleteRoleCommand`. Also callable manually via admin endpoint for full resync.

### Runtime Switching (no restart)

```
SystemSetting: "authorization.provider" = "local" | "cerbos"
                                                ↓
RuntimeAuthorizationProvider (wrapper)
    ├── reads SystemSetting with 1-min cache
    ├── delegates to CerbosAuthorizationProvider OR LocalAuthorizationProvider
    └── logs switch events to ConfigurationChangeLog
```

Configurable at:
- **Startup**: `Cerbos:Enabled` in appsettings / env vars / Infisical (existing pattern)
- **Runtime**: `authorization.provider` SystemSetting via admin UI (new)
- **Priority**: Runtime setting overrides startup config

### Cerbos Infrastructure (PostgreSQL Store + Horizontal Scale)

```
Cerbos PDP Configuration (.cerbos.yaml):

server:
  httpListenAddr: ":3592"
  grpcListenAddr: ":3593"
  adminAPI:
    enabled: true
    adminCredentials:
      username: ${CERBOS_ADMIN_USER}
      passwordHash: ${CERBOS_ADMIN_PASSWORD_HASH}

storage:
  driver: "overlay"
  overlay:
    baseDriver: postgres
    fallbackDriver: disk
    fallbackErrorThreshold: 5
    fallbackErrorWindow: 5s
  disk:
    directory: /policies/base          # Base policies shipped with container image
    watchForChanges: false
  postgres:
    url: "postgres://${CERBOS_PG_USER}:${CERBOS_PG_PASSWORD}@${CERBOS_PG_HOST}:5432/${CERBOS_PG_DB}?sslmode=require&search_path=cerbos"
    connPool:
      maxLifeTime: 5m
      maxIdleTime: 3m
      maxOpen: 10
      maxIdle: 5

compile:
  cacheDuration: 60s                   # Multi-instance eventual consistency
```

**Two-Layer Resilience**:
1. **Application layer**: `RuntimeAuthorizationProvider` → if all Cerbos instances unreachable → `LocalAuthorizationProvider` (reads Permission/RolePermission from app DB directly)
2. **Cerbos layer**: Overlay driver → if Cerbos PostgreSQL unreachable → falls back to base disk policies (degraded but functional)

**Multi-Instance Cache Consistency**:
- `compile.cacheDuration: 60s` — each Cerbos instance re-reads from PostgreSQL every 60 seconds (eventually consistent)
- `POST /admin/policy` to any instance — that instance updates its cache immediately, others within 60s
- `GET /admin/store/reload?wait=true` on all instances — for critical changes (e.g., revoking admin access), force immediate consistency

---

## Implementation Phases

### Phase 1: Domain Layer — Unified Role Table & TenantMember (Week 1)
**Effort: L | Risk: Medium | Skills: `clean-architecture-rules`, `dotnet-efcore-guidelines`**

The foundation. Everything else depends on this.

#### Task 1.1: Create `RoleScopeEnum`
- **File**: `Explore.Domain/Enums/RoleScopeEnum.cs`
- **Acceptance Criteria**:
  - [ ] Enum: `Platform = 0`, `Tenant = 1`, `Organization = 2`
  - [ ] File-scoped namespace
  - [ ] ABOUTME comment
- **Effort**: S
- **Dependencies**: None

#### Task 1.2: Expand `Role` Entity
- **File**: `Explore.Domain/Role.cs`
- **Acceptance Criteria**:
  - [ ] Add `Scope` property (`RoleScopeEnum`)
  - [ ] Add `IsSystem` property (`bool`)
  - [ ] Keep existing `Id`, `MasterCode`, `FullName`, `Description`
  - [ ] Remove unused `using` statements
  - [ ] ABOUTME comment updated
- **Effort**: S
- **Dependencies**: Task 1.1

#### Task 1.3: Update `RoleEnum` → Full Role Identifiers
- **File**: `Explore.Domain/Enums/RoleEnum.cs`
- **Acceptance Criteria**:
  - [ ] Expand to include all roles from all three former tables
  - [ ] Use stable int IDs matching seed data
  - [ ] Group by scope in comments (Platform, Tenant, Organization)
  - [ ] All existing references to `OrganizationRoleEnum` and `TenantAdministratorRoleEnum` can map to this
- **Effort**: M
- **Dependencies**: Task 1.2

#### Task 1.4: Rename `TenantAdministrator` → `TenantMember`
- **File**: `Explore.Domain/TenantMember.cs` (rename from `TenantAdministrator.cs`)
- **Acceptance Criteria**:
  - [ ] Class renamed to `TenantMember`
  - [ ] `TenantAdministratorRoleId` → `RoleId` (FK → `Role`)
  - [ ] `TenantAdministratorRole` nav property → `Role`
  - [ ] Implements `ITenantEntity`, `IAuditableEntity`
  - [ ] Add audit fields (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
  - [ ] Keep `GrantedAt`, `GrantedBy` for backward compat
  - [ ] Old file `TenantAdministrator.cs` marked for deletion
- **Effort**: M
- **Dependencies**: Task 1.2

#### Task 1.5: Update `OrganizationMember` FK
- **File**: `Explore.Domain/OrganizationMember.cs`
- **Acceptance Criteria**:
  - [ ] `OrganizationRoleId` → `RoleId` (FK → `Role`)
  - [ ] `OrganizationRole` nav property → `Role`
  - [ ] Remove import of `OrganizationRole`
- **Effort**: S
- **Dependencies**: Task 1.2

#### Task 1.6: Update `TenantUser` FK
- **File**: `Explore.Domain/TenantUser.cs`
- **Acceptance Criteria**:
  - [ ] `UserRoleId` → `RoleId` (FK → `Role`)
  - [ ] `UserRole` nav property → `Role`
- **Effort**: S
- **Dependencies**: Task 1.2

#### Task 1.7: Mark Old Entities for Removal
- **Files to remove** (after migration):
  - `Explore.Domain/OrganizationRole.cs`
  - `Explore.Domain/TenantAdministratorRole.cs`
  - `Explore.Domain/TenantAdministrator.cs`
  - `Explore.Domain/UserRole.cs`
  - `Explore.Domain/Enums/OrganizationRoleEnum.cs`
  - `Explore.Domain/Enums/TenantAdministratorRoleEnum.cs`
- **Acceptance Criteria**:
  - [ ] All references updated before deletion
  - [ ] No compile errors after removal
- **Effort**: S
- **Dependencies**: All Phase 1 tasks

#### Task 1.8: Add `GovernanceSettingKeys.AuthorizationProvider`
- **File**: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- **Acceptance Criteria**:
  - [ ] Add `public const string AuthorizationProvider = "authorization.provider";`
  - [ ] Value semantics: `"local"` or `"cerbos"`
- **Effort**: S
- **Dependencies**: None

---

### Phase 2: Persistence Layer — Migration & EF Configuration (Week 1-2)
**Effort: XL | Risk: High | Skills: `dotnet-efcore-guidelines`, `clean-architecture-rules`**

Data migration is the riskiest part. Must preserve existing data.

#### Task 2.1: Update `Role` EF Configuration
- **File**: `Explore.Persistence/Configurations/Entities/RoleConfiguration.cs`
- **Acceptance Criteria**:
  - [ ] Configure `Scope` as required enum column
  - [ ] Configure `IsSystem` as required with default `false`
  - [ ] Configure `MasterCode` as unique
  - [ ] Composite index on (Scope, MasterCode) for fast lookups
- **Effort**: M
- **Dependencies**: Phase 1

#### Task 2.2: Create `TenantMember` EF Configuration
- **File**: `Explore.Persistence/Configurations/Entities/TenantMemberConfiguration.cs`
- **Acceptance Criteria**:
  - [ ] FK to User, Tenant, Role
  - [ ] Unique index on (TenantId, UserId)
  - [ ] Named query filter for tenant isolation if needed
  - [ ] Replaces `TenantAdministratorConfiguration.cs`
- **Effort**: M
- **Dependencies**: Phase 1

#### Task 2.3: Update `OrganizationMember` EF Configuration
- **File**: `Explore.Persistence/Configurations/Entities/OrganizationMemberConfiguration.cs`
- **Acceptance Criteria**:
  - [ ] FK changed from `OrganizationRole` → `Role`
  - [ ] Existing unique index preserved
- **Effort**: S
- **Dependencies**: Phase 1

#### Task 2.4: Update `TenantUser` EF Configuration
- **File**: `Explore.Persistence/Configurations/Entities/TenantUserConfiguration.cs`
- **Acceptance Criteria**:
  - [ ] FK changed from `UserRole` → `Role`
- **Effort**: S
- **Dependencies**: Phase 1

#### Task 2.5: Update `ExploreDbContext` DbSets
- **File**: `Explore.Persistence/ExploreDbContext.cs`
- **Acceptance Criteria**:
  - [ ] Remove `DbSet<OrganizationRole>`, `DbSet<TenantAdministratorRole>`, `DbSet<UserRole>`
  - [ ] Add `DbSet<TenantMember>` (rename from `TenantAdministrators`)
  - [ ] Verify global query filters updated
- **Effort**: M
- **Dependencies**: Tasks 2.1-2.4

#### Task 2.6: Create EF Core Migration
- **File**: New migration file
- **Acceptance Criteria**:
  - [ ] **Data migration**: Copy rows from `OrganizationRoles`, `TenantAdministratorRoles`, `UserRoles` → `Roles` with correct `Scope` and `MasterCode`
  - [ ] **FK migration**: Update `OrganizationMembers.OrganizationRoleId` → `OrganizationMembers.RoleId` pointing to new Role IDs
  - [ ] **FK migration**: Update `TenantAdministrators.TenantAdministratorRoleId` → `TenantMembers.RoleId`
  - [ ] **FK migration**: Update `TenantUsers.UserRoleId` → `TenantUsers.RoleId`
  - [ ] **Rename table**: `TenantAdministrators` → `TenantMembers`
  - [ ] **Drop old tables**: `OrganizationRoles`, `TenantAdministratorRoles`, `UserRoles`
  - [ ] Rollback migration tested
- **Effort**: XL
- **Dependencies**: Tasks 2.1-2.5

#### Task 2.7: Update Seed Data
- **File**: `Explore.Persistence/Seed/LookupTableSeeder.cs`
- **Acceptance Criteria**:
  - [ ] Replace `SeedOrganizationRolesAsync` + `SeedTenantAdministratorRolesAsync` with single `SeedRolesAsync`
  - [ ] Seed all roles with correct `Scope`, `MasterCode`, `IsSystem=true`
  - [ ] Idempotent (check-before-insert pattern preserved)
- **Effort**: M
- **Dependencies**: Task 2.6

#### Task 2.8: Update `DatabaseSeeder` (Dev Seed)
- **File**: `Explore.Persistence/Seed/DatabaseSeeder.cs`
- **Acceptance Criteria**:
  - [ ] Remove `SeedUserRolesAsync` (now part of `LookupTableSeeder.SeedRolesAsync`)
  - [ ] Update OrganizationMember seed to use new `RoleId`
  - [ ] Update TenantMember seed to use new `RoleId`
- **Effort**: M
- **Dependencies**: Task 2.7

#### Task 2.9: Update Repositories
- **Files**:
  - `Explore.Persistence/Repositories/OrganizationMemberRepository.cs` — Update eager-loading from `OrganizationRole` → `Role`; update `IsUserAdminOfOrganization` to check RoleId against org admin role IDs
  - Remove: `OrganizationRoleRepository.cs`, `TenantAdministratorRoleRepository.cs`, `UserRoleRepository.cs`
  - Create: `TenantMemberRepository.cs` (rename from any TenantAdministrator repo)
  - Create: `IRoleRepository.cs` with `GetByScope(RoleScopeEnum)`, `GetByMasterCode(string)`
  - Create: `RoleRepository.cs`
- **Acceptance Criteria**:
  - [ ] All repo interfaces updated in Application layer
  - [ ] All repo implementations updated in Persistence layer
  - [ ] DI registration updated in `PersistenceServicesRegistration.cs`
  - [ ] `LookupDataCache` updated to cache unified `Role` table
- **Effort**: L
- **Dependencies**: Task 2.6

#### Task 2.10: Update `AdminContext`
- **File**: `Explore.Infrastructure/Identity/AdminContext.cs`
- **Acceptance Criteria**:
  - [ ] `IsOrganizationAdminAsync` — query uses new Role table with org admin role IDs
  - [ ] `IsTenantAdminAsync` — query uses `TenantMember` table with tenant admin role IDs
  - [ ] `GetAdminOrganizationIdsAsync` — updated query
  - [ ] `GetAdminTenantIdsAsync` — updated query
- **Effort**: M
- **Dependencies**: Task 2.9

---

### Phase 3: Application Layer — IAuthorizationProvider & CQRS Updates (Week 2)
**Effort: XL | Risk: Medium | Skills: `cqrs-mediatr-guidelines`, `clean-architecture-rules`, `auth-patterns`**

#### Task 3.1: Rename `ICerbosAuthorizationService` → `IAuthorizationProvider`
- **File**: `Explore.Application/Contracts/Infrastructure/IAuthorizationProvider.cs` (rename)
- **Acceptance Criteria**:
  - [ ] Interface renamed, same method signatures
  - [ ] `AuthorizationCheck` record stays (no Cerbos prefix)
  - [ ] Old file marked for deletion
  - [ ] ABOUTME updated — no Cerbos reference in the interface doc
- **Effort**: S
- **Dependencies**: None

#### Task 3.2: Rename `CerbosAuthorizeAttribute` → `AuthorizeResourceAttribute`
- **File**: `Explore.Application/Authorization/AuthorizeResourceAttribute.cs`
- **Acceptance Criteria**:
  - [ ] Attribute renamed
  - [ ] `CerbosPermissionAction` → `PermissionAction`
  - [ ] `CerbosResourceDescriptorRegistry` → `ResourceDescriptorRegistry`
  - [ ] All commands using `[CerbosAuthorize]` updated to `[AuthorizeResource]`
- **Effort**: M
- **Dependencies**: Task 3.1

#### Task 3.3: Update `AuthorizationBehavior`
- **File**: `Explore.Application/Behaviors/AuthorizationBehavior.cs`
- **Acceptance Criteria**:
  - [ ] Dependency changed from `ICerbosAuthorizationService` → `IAuthorizationProvider`
  - [ ] References to `CerbosAuthorizeAttribute` → `AuthorizeResourceAttribute`
  - [ ] Log messages updated (remove "Cerbos" from log templates)
- **Effort**: S
- **Dependencies**: Tasks 3.1, 3.2

#### Task 3.4: Update All DTOs
- **Files**: `Explore.Application/DTOs/OrganizationMember/`, `DTOs/OrganizationRole/`, `DTOs/UserRole/`, `DTOs/TenantUser/`
- **Acceptance Criteria**:
  - [ ] `OrganizationMemberDto`: `OrganizationRoleId` → `RoleId`, `OrganizationRoleFullName` → `RoleName`
  - [ ] `AddOrganizationMemberDto`: `Role` property type stays `RoleEnum` (updated enum)
  - [ ] `TenantUserDto`: `UserRoleId` → `RoleId`, `UserRoleName` → `RoleName`
  - [ ] Remove `OrganizationRoleDto`, `OrganizationRoleListDto`, `UserRoleDto`, `UserRoleListDto`
  - [ ] Create unified `RoleDto` and `RoleListDto` with `Scope` property
- **Effort**: L
- **Dependencies**: Phase 1

#### Task 3.5: Update AutoMapper `MappingProfile`
- **File**: `Explore.Application/Profiles/MappingProfile.cs`
- **Acceptance Criteria**:
  - [ ] OrganizationMember → OrganizationMemberDto: map `Role.FullName` → `RoleName`
  - [ ] TenantUser → TenantUserDto: map `Role.FullName` → `RoleName`
  - [ ] Remove OrganizationRole/UserRole mappings
  - [ ] Add unified Role → RoleDto mapping
- **Effort**: M
- **Dependencies**: Task 3.4

#### Task 3.6: Update Command Handlers (OrganizationMember)
- **Files**: `Features/OrganizationMembers/Handlers/Commands/`
- **Acceptance Criteria**:
  - [ ] `AddOrganizationMemberCommandHandler`: Set `RoleId` instead of `OrganizationRoleId`
  - [ ] `UpdateOrganizationMemberRoleCommandHandler`: Update `RoleId`
  - [ ] `DeleteOrganizationMemberCommandHandler`: Check admin role via new Role IDs
  - [ ] All "is admin" checks use new Role MasterCodes or ID constants
- **Effort**: M
- **Dependencies**: Tasks 3.4, 2.9

#### Task 3.7: Update Command Handlers (Onboarding & Tenant)
- **Files**: `Features/InstanceOnboarding/`, `Features/TenantUsers/`
- **Acceptance Criteria**:
  - [ ] `CompleteInstanceOnboardingCommandHandler`: Create `TenantMember` with `RoleId` for tenant admin
  - [ ] TenantUser handlers: Use new `RoleId` FK
  - [ ] Validators: Check `RoleId` exists in Role table with correct Scope
- **Effort**: M
- **Dependencies**: Tasks 3.4, 2.9

#### Task 3.8: Update/Remove Query Handlers
- **Acceptance Criteria**:
  - [ ] Remove `GetOrganizationRoleListRequestHandler`, `GetOrganizationRoleDetailsRequestHandler`
  - [ ] Remove `GetUserRoleListRequestHandler`, `GetUserRoleDetailsRequestHandler`
  - [ ] Create unified `GetRoleListRequestHandler` with scope filter parameter
  - [ ] Create `GetRoleDetailsRequestHandler`
  - [ ] Update `GetOrganizationMembersRequestHandler` for new DTO
  - [ ] Update `GetMyOrganizationsRequestHandler` for new Role reference
- **Effort**: L
- **Dependencies**: Tasks 3.4, 3.5

#### Task 3.9: Update Serialization Context
- **File**: `Explore.Application/Serialization/ExploreJsonContext.cs`
- **Acceptance Criteria**:
  - [ ] Remove old DTO types (OrganizationRoleDto, UserRoleDto)
  - [ ] Add new RoleDto, RoleListDto types
  - [ ] Update OrganizationMemberDto type with new property names
- **Effort**: M
- **Dependencies**: Task 3.4

---

### Phase 4: Infrastructure Layer — Authorization Providers + Cerbos at Scale (Week 2-3)
**Effort: XL | Risk: Medium | Skills: `auth-patterns`, `clean-architecture-rules`, `error-tracking`**

The existing code is already well-structured. This phase renames the providers, adds the runtime wrapper, and wires up Cerbos with a **PostgreSQL-backed policy store** for multi-instance horizontal scaling.

#### Task 4.1: Rename `CerbosAuthorizationService` → `CerbosAuthorizationProvider`
- **File**: `Explore.Infrastructure/Services/CerbosAuthorizationProvider.cs`
- **Acceptance Criteria**:
  - [ ] Implements `IAuthorizationProvider`
  - [ ] Internal DTOs (CerbosPrincipal, CerbosResource, etc.) stay — they're Cerbos HTTP API specific
  - [ ] `CerbosSettings` stays (it's infrastructure config)
  - [ ] Principal building updated to use unified Role table context
  - [ ] Sends user's permissions as principal attributes (for Cerbos derived roles)
- **Effort**: M
- **Dependencies**: Task 3.1

#### Task 4.2: Rename `FallbackAuthorizationService` → `LocalAuthorizationProvider`
- **File**: `Explore.Infrastructure/Services/LocalAuthorizationProvider.cs`
- **Acceptance Criteria**:
  - [ ] Implements `IAuthorizationProvider`
  - [ ] Enhanced: reads `RolePermission` join table for dynamic permission checks
  - [ ] Keeps instance admin bypass, tenant admin check, org admin check
  - [ ] Log messages updated (remove "Fallback" → "Local")
  - [ ] Uses updated `AdminContext` with unified Role queries
  - [ ] Default deny: no match = false
- **Effort**: L
- **Dependencies**: Task 3.1, Phase 8 (Permission entity)

#### Task 4.3: Create `RuntimeAuthorizationProvider` (Wrapper)
- **File**: `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs`
- **Acceptance Criteria**:
  - [ ] Implements `IAuthorizationProvider`
  - [ ] Reads `GovernanceSettingKeys.AuthorizationProvider` from `ISystemSettingRepository`
  - [ ] Caches provider mode for 1 minute (IMemoryCache)
  - [ ] Delegates to `CerbosAuthorizationProvider` or `LocalAuthorizationProvider`
  - [ ] Falls back to `LocalAuthorizationProvider` if Cerbos unreachable AND setting is "cerbos"
  - [ ] Logs provider selection on every switch
  - [ ] Thread-safe
- **Effort**: L
- **Dependencies**: Tasks 4.1, 4.2

#### Task 4.4: Create `PolicySyncService` (Cerbos Policy Generator)
- **File**: `Explore.Infrastructure/Services/PolicySyncService.cs`
- **Acceptance Criteria**:
  - [ ] `IPolicySyncService` interface in Application layer (contract)
  - [ ] Generates Cerbos derived role YAML from `RolePermission` data
  - [ ] Generates Cerbos resource policy YAML per resource kind
  - [ ] Pushes policies via `POST /admin/policy` to Cerbos Admin API (JSON body)
  - [ ] `ReloadAllInstancesAsync()` — calls `GET /admin/store/reload?wait=true` on all Cerbos instances
  - [ ] `SyncAllPoliciesAsync()` — full resync from Permission + RolePermission tables
  - [ ] Uses `HttpClient` with Basic Auth for Admin API
  - [ ] Logs policy sync events (success/failure/duration)
  - [ ] Resilient: if Cerbos Admin API unreachable, logs error but does NOT fail the command that triggered it
- **Effort**: XL
- **Dependencies**: Phase 8 (Permission entity), Task 4.1

#### Task 4.5: Create `CerbosAdminApiSettings`
- **File**: `Explore.Infrastructure/Services/CerbosAdminApiSettings.cs`
- **Acceptance Criteria**:
  - [ ] `Endpoints` — list of all Cerbos instance URLs (for reload broadcast)
  - [ ] `AdminUsername` + `AdminPassword` — credentials for Admin API
  - [ ] `PolicyDatabaseUrl` — connection string to Cerbos PostgreSQL (for Tier 3 documentation)
  - [ ] Bound from `Cerbos:AdminApi` configuration section
- **Effort**: S
- **Dependencies**: None

#### Task 4.6: Update DI Registration
- **File**: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- **Acceptance Criteria**:
  - [ ] Register `CerbosAuthorizationProvider` as scoped (always, for runtime switching)
  - [ ] Register `LocalAuthorizationProvider` as scoped (always)
  - [ ] Register `IAuthorizationProvider` → `RuntimeAuthorizationProvider` as scoped
  - [ ] Register `IPolicySyncService` → `PolicySyncService` as scoped
  - [ ] Register named `HttpClient` for Cerbos Admin API with Basic Auth handler
  - [ ] Keep `CerbosSettings` binding from configuration
  - [ ] Add `CerbosAdminApiSettings` binding
- **Effort**: M
- **Dependencies**: Tasks 4.3, 4.4, 4.5

#### Task 4.7: Create Cerbos PostgreSQL Schema Init Script
- **File**: `cerbos/init/cerbos-schema.sql`
- **Acceptance Criteria**:
  - [ ] Creates `cerbos` schema in the target database
  - [ ] Creates tables required by Cerbos PostgreSQL driver (`policy`, `policy_dependency`, `policy_ancestor`, `attr_schema_defs`)
  - [ ] Script sourced from official Cerbos docs (they provide it)
  - [ ] Idempotent (IF NOT EXISTS)
  - [ ] Documented in ops guide with per-tier instructions
- **Effort**: M
- **Dependencies**: None

#### Task 4.8: Create Cerbos Base Policy Files
- **Files**: `cerbos/policies/base/*.yaml`
- **Acceptance Criteria**:
  - [ ] Base resource policies for all 18 resource kinds (system defaults)
  - [ ] Base derived roles for system roles (platform.super_admin, tenant.owner, org.admin, etc.)
  - [ ] These are shipped with the Cerbos container image (overlay disk fallback)
  - [ ] Dynamic custom role policies are stored in PostgreSQL only
  - [ ] Policies tested with `cerbos compile --verbose`
- **Effort**: L
- **Dependencies**: Phase 1 (Role seed data)

#### Task 4.9: Create Cerbos Docker Configuration
- **Files**: `cerbos/config/.cerbos.yaml`, `docker-compose.cerbos.yml`
- **Acceptance Criteria**:
  - [ ] `.cerbos.yaml` with overlay driver (postgres + disk fallback)
  - [ ] `compile.cacheDuration: 60s`
  - [ ] Admin API enabled with env var credentials
  - [ ] Docker Compose service definition for Cerbos
  - [ ] Health check endpoint configured
  - [ ] Environment variables documented for all three deployment tiers
- **Effort**: M
- **Dependencies**: Task 4.7

---

### Phase 5: API Layer — Controllers & HATEOAS (Week 3)
**Effort: M | Risk: Low | Skills: `cqrs-mediatr-guidelines`**

#### Task 5.1: Create Unified `RoleController`
- **File**: `Explore.API/Controllers/RoleController.cs`
- **Acceptance Criteria**:
  - [ ] `GET /api/roles?scope=organization` — list roles by scope
  - [ ] `GET /api/roles/{id}` — get role details
  - [ ] Replaces `OrganizationRoleController` and `UserRoleController`
  - [ ] Old controllers marked for deletion
- **Effort**: M
- **Dependencies**: Task 3.8

#### Task 5.2: Update HATEOAS Link Definitions
- **Files**: `Explore.API/Hateoas/`, `Explore.Application/Hateoas/LinkDefinition.cs`
- **Acceptance Criteria**:
  - [ ] `LinkDefinition`: `CerbosResourceKind` → `ResourceKind`, `CerbosAction` → `Action`, etc.
  - [ ] `HateoasAuthorizationEvaluator`: use `IAuthorizationProvider` instead of `ICerbosAuthorizationService`
  - [ ] `LinkDefinitionPermissionExtensions`: rename `WithCerbos()` → `WithPermission()`
  - [ ] All 15+ LinkPolicy files updated
  - [ ] `ResourceDescriptorRegistry` entries updated for new DTO names
- **Effort**: L
- **Dependencies**: Tasks 3.1, 3.2

#### Task 5.3: Update `OrganizationMemberController`
- **File**: `Explore.API/Controllers/OrganizationMemberController.cs`
- **Acceptance Criteria**:
  - [ ] Route names updated if any reference old role controllers
  - [ ] DTO types match new names
- **Effort**: S
- **Dependencies**: Task 3.6

#### Task 5.4: Add Authorization Provider Admin Endpoint
- **File**: `Explore.API/Controllers/SystemSettingsController.cs` (or new endpoint)
- **Acceptance Criteria**:
  - [ ] `GET /api/system-settings/authorization-provider` — returns current mode
  - [ ] `PUT /api/system-settings/authorization-provider` — switches mode
  - [ ] Protected by `[AuthorizeResource("instance_setting", PermissionAction.Update)]`
  - [ ] Logs to `ConfigurationChangeLog`
  - [ ] Validates: only accepts "local" or "cerbos"
  - [ ] Returns 400 if switching to "cerbos" but `CerbosSettings.Endpoint` is not configured
- **Effort**: M
- **Dependencies**: Task 4.3

---

### Phase 6: Blazor UI (Week 3)
**Effort: M | Risk: Low | Skills: `blazor-ui-conventions`, `blazor-css-isolation`**

#### Task 6.1: Update `RoleHelper`
- **File**: `Explore.Blazor.Client/Helpers/RoleHelper.cs`
- **Acceptance Criteria**:
  - [ ] Use unified `RoleEnum` instead of `OrganizationRole` client enum
  - [ ] `CanManage()` works with new role IDs
  - [ ] `GetRoleName()` and `GetRoleColor()` updated
- **Effort**: S
- **Dependencies**: Phase 3

#### Task 6.2: Update Client-Side Models
- **File**: `Explore.Blazor.Client/Models/Enums/`
- **Acceptance Criteria**:
  - [ ] Remove `OrganizationRole.cs` client enum
  - [ ] Add unified `RoleEnum.cs` or use shared DTO
- **Effort**: S
- **Dependencies**: Task 3.4

#### Task 6.3: Update UI Components
- **Files**: `OrganizationMembers.razor.cs`, `OrganizationDetails.razor.cs`, `MyOrganizations.razor.cs`, `MyEvents.razor.cs`, `CreateEvent.razor.cs`
- **Acceptance Criteria**:
  - [ ] Property names updated (`OrganizationRoleFullName` → `RoleName`, etc.)
  - [ ] Role dropdowns use unified role list (filtered by scope)
  - [ ] Admin permission checks use `RoleHelper.CanManage()` with new IDs
- **Effort**: M
- **Dependencies**: Tasks 6.1, 6.2

#### Task 6.4: Update Blazor Services
- **Files**: `OrganizationMemberService.cs`, `AdminService.cs`
- **Acceptance Criteria**:
  - [ ] `GetOrganizationRolesAsync()` → `GetRolesAsync(scope: "organization")`
  - [ ] API endpoint URLs updated to `/api/roles?scope=organization`
- **Effort**: S
- **Dependencies**: Task 5.1

#### Task 6.5: Update JSON Serialization Context
- **File**: `Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs`
- **Acceptance Criteria**:
  - [ ] Old DTO types removed, new types added
- **Effort**: S
- **Dependencies**: Task 3.9

---

### Phase 7: Testing & Documentation (Week 3-4)
**Effort: L | Risk: Low | Skills: `clean-architecture-rules`**

#### Task 7.1: Update Unit Tests
- **Files**: All test projects
- **Acceptance Criteria**:
  - [ ] `AuthorizationBehaviorTests` — uses `IAuthorizationProvider`, `AuthorizeResourceAttribute`
  - [ ] `CerbosAuthorizationServiceTests` → `CerbosAuthorizationProviderTests`
  - [ ] `FallbackAuthorizationServiceTests` → `LocalAuthorizationProviderTests`
  - [ ] New: `RuntimeAuthorizationProviderTests` — tests mode switching, caching, fallback
  - [ ] `CreateOrganizationCommandHandlerTests` — uses new `RoleId` FK
  - [ ] All `DataBuilder` fakers updated
  - [ ] Test constants updated (`AuthenticationTestConstants`)
- **Effort**: L
- **Dependencies**: All previous phases

#### Task 7.2: Update Integration Tests
- **Files**: `Event.API.IntegrationTests/`
- **Acceptance Criteria**:
  - [ ] `LookupTableControllerTests` — test new `/api/roles?scope=...` endpoint
  - [ ] `LinkTableControllerTests` — OrganizationMember tests with new DTO shape
  - [ ] `OrganizationMemberHateoasTests` — HATEOAS links work with renamed properties
  - [ ] `TenantControllerTests` — uses new `RoleId`
- **Effort**: M
- **Dependencies**: Phase 5

#### Task 7.3: Update Architecture Tests
- **File**: `Event.Architecture.Tests/`
- **Acceptance Criteria**:
  - [ ] Verify no references to old entity names remain
  - [ ] Verify Clean Architecture dependency rules hold
- **Effort**: S
- **Dependencies**: All phases

#### Task 7.4: Update Documentation
- **Files**: `docs/DOMAIN.md`, `docs/SECURITY.md`, `docs/CONFIGURATION.md`, `docs/ARCHITECTURE.md`
- **Acceptance Criteria**:
  - [ ] DOMAIN.md: Updated entity diagram (Role, TenantMember, updated OrganizationMember)
  - [ ] SECURITY.md: IAuthorizationProvider documentation, provider switching explained
  - [ ] CONFIGURATION.md: Add `authorization.provider` setting documentation, remove "Cerbos not integrated" note
  - [ ] CLAUDE.md: Updated if any rule references changed
- **Effort**: M
- **Dependencies**: All phases

#### Task 7.5: Update Cerbos Policies
- **Files**: `cerbos/policies/` (if they exist)
- **Acceptance Criteria**:
  - [ ] YAML policies updated to match new role MasterCodes
  - [ ] Derived roles updated for unified role context
  - [ ] Principal attributes updated (tenantMemberships, orgMemberships use new role IDs)
- **Effort**: M
- **Dependencies**: Phase 4

---

### Phase 8: Dynamic Permissions — Permission Entity, CRUD & Capability Ceiling (Week 3-4)
**Effort: XL | Risk: Medium | Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`**

This phase adds the Permission vocabulary, the RolePermission join table, admin commands for custom role/permission management, and the capability ceiling (anti-escalation) logic.

#### Task 8.1: Create `Permission` Entity
- **File**: `Explore.Domain/Permission.cs`
- **Acceptance Criteria**:
  - [ ] Properties: `Id` (int PK), `ResourceKind`, `Action`, `FieldScope` (nullable), `MasterCode` (unique), `FullName`, `Description` (nullable), `GroupName`, `Scope` (RoleScopeEnum), `IsSystem`, `IsFiltered`, `IsActive`
  - [ ] Implements `IAuditableEntity`
  - [ ] `MasterCode` format: `{resource_kind}:{action}` or `{resource_kind}:{action}:{field}`
  - [ ] File-scoped namespace, ABOUTME comment
- **Effort**: M
- **Dependencies**: Task 1.1 (RoleScopeEnum)

#### Task 8.2: Create `RolePermission` Entity
- **File**: `Explore.Domain/RolePermission.cs`
- **Acceptance Criteria**:
  - [ ] Properties: `RoleId` (int FK → Role), `PermissionId` (int FK → Permission), `GrantedAt` (DateTime), `GrantedBy` (Guid?)
  - [ ] Composite PK: (RoleId, PermissionId)
  - [ ] Navigation properties: `Role`, `Permission`
  - [ ] File-scoped namespace
- **Effort**: S
- **Dependencies**: Task 8.1, Task 1.2 (Role entity)

#### Task 8.3: EF Configuration for Permission + RolePermission
- **Files**: `Explore.Persistence/Configurations/Entities/PermissionConfiguration.cs`, `RolePermissionConfiguration.cs`
- **Acceptance Criteria**:
  - [ ] `Permission`: unique index on MasterCode, composite index on (ResourceKind, Action)
  - [ ] `RolePermission`: composite PK (RoleId, PermissionId), FKs to Role and Permission
  - [ ] Add `DbSet<Permission>` and `DbSet<RolePermission>` to `ExploreDbContext`
- **Effort**: M
- **Dependencies**: Tasks 8.1, 8.2

#### Task 8.4: Create Permission Seed Data
- **File**: `Explore.Persistence/Seed/LookupTableSeeder.cs` (extend `SeedRolesAsync` or new `SeedPermissionsAsync`)
- **Acceptance Criteria**:
  - [ ] Seed 65+ system permissions from the 18 resource kinds × 4 actions
  - [ ] All seeded permissions have `IsSystem = true`, `IsActive = true`
  - [ ] `instance_setting:update` marked as `IsFiltered = true` (hidden from non-super-admins)
  - [ ] Idempotent (check-before-insert)
  - [ ] Seed default `RolePermission` rows for system roles (e.g., `platform.super_admin` gets all permissions)
- **Effort**: L
- **Dependencies**: Task 8.3, Task 2.7

#### Task 8.5: Create Permission Repository
- **Files**: `Explore.Application/Contracts/Persistence/IPermissionRepository.cs`, `Explore.Persistence/Repositories/PermissionRepository.cs`
- **Acceptance Criteria**:
  - [ ] `GetByMasterCodeAsync(string masterCode)`
  - [ ] `GetByResourceKindAsync(string resourceKind)`
  - [ ] `GetByScopeAsync(RoleScopeEnum scope)`
  - [ ] `GetPermissionsForRoleAsync(int roleId)` — returns permissions via RolePermission join
  - [ ] `HasPermissionAsync(IEnumerable<int> roleIds, string permissionMasterCode)` — the core check for LocalAuthorizationProvider
  - [ ] `GetAssignablePermissionsAsync(IEnumerable<int> callerRoleIds, RoleScopeEnum targetScope)` — capability ceiling filter
  - [ ] Repositories return entities, never DTOs
- **Effort**: L
- **Dependencies**: Task 8.3

#### Task 8.6: Create `PermissionRegistryService` (Code-Defined Vocabulary)
- **File**: `Explore.Application/Authorization/PermissionRegistryService.cs`
- **Acceptance Criteria**:
  - [ ] Static class or singleton service
  - [ ] `AllPermissions` — returns all known MasterCodes (from DB at startup, cached)
  - [ ] `ValidateMasterCode(string code)` — checks format and existence
  - [ ] `GetPermissionsByGroup(string groupName)` — for UI dropdowns
  - [ ] `GetFilteredPermissions(bool excludeFiltered)` — capability ceiling: hides dangerous permissions from non-super-admins
  - [ ] Refreshes cache when permissions table changes
- **Effort**: M
- **Dependencies**: Task 8.5

#### Task 8.7: CQRS Commands — Create/Update/Delete Custom Role with Permissions
- **Files**: `Explore.Application/Features/Roles/Handlers/Commands/`
- **Acceptance Criteria**:
  - [ ] `CreateCustomRoleCommand` — creates a non-system Role with assigned Permissions
    - Validator: name not empty, scope valid, permissions exist and are assignable by caller
    - Handler: creates Role + RolePermission rows, calls `IPolicySyncService.PushPoliciesAsync()` if Cerbos is active
    - Returns `BaseCommandResponse<int>` (RoleId)
  - [ ] `UpdateRolePermissionsCommand` — replaces permissions for an existing custom role
    - Validator: role exists, is not system role, permissions are assignable by caller
    - Handler: replaces RolePermission rows, calls `IPolicySyncService.PushPoliciesAsync()`
    - **Capability ceiling**: caller can only assign permissions they themselves have
  - [ ] `DeleteCustomRoleCommand` — soft-deletes a non-system role
    - Validator: role exists, is not system role, no active members assigned
    - Handler: removes RolePermission rows, deactivates Role, calls `IPolicySyncService.PushPoliciesAsync()`
  - [ ] All commands protected by `[AuthorizeResource("role", PermissionAction.Create/Update/Delete)]`
- **Effort**: XL
- **Dependencies**: Tasks 8.5, 8.6, 4.4 (PolicySyncService)

#### Task 8.8: CQRS Queries — List/Get Permissions and Custom Roles
- **Files**: `Explore.Application/Features/Permissions/Handlers/Queries/`
- **Acceptance Criteria**:
  - [ ] `GetPermissionListRequest` — list all permissions, filterable by scope, group, and `excludeFiltered`
  - [ ] `GetRolePermissionsRequest` — get all permissions for a specific role
  - [ ] `GetAssignablePermissionsRequest` — get permissions the current user can assign (capability ceiling)
  - [ ] DTOs: `PermissionDto`, `PermissionListDto`, `RolePermissionDto`
- **Effort**: L
- **Dependencies**: Tasks 8.5, 8.6

#### Task 8.9: Capability Ceiling Logic (Anti-Escalation)
- **File**: `Explore.Application/Authorization/CapabilityCeilingService.cs`
- **Acceptance Criteria**:
  - [ ] **Rule 1**: You can only grant permissions you yourself have
  - [ ] **Rule 2**: `IsFiltered` permissions are hidden from tenant/org admins (only super-admins see them)
  - [ ] **Rule 3**: Scope boundary — a tenant admin can only create roles with `Scope = Organization` or lower, not `Scope = Platform`
  - [ ] **Rule 4**: System roles (`IsSystem = true`) cannot be modified or deleted
  - [ ] Used by validators in Task 8.7
  - [ ] Extensively tested (security-critical)
- **Effort**: L
- **Dependencies**: Tasks 8.5, 8.6

#### Task 8.10: EF Core Migration for Permissions
- **File**: New migration file
- **Acceptance Criteria**:
  - [ ] Creates `Permissions` table
  - [ ] Creates `RolePermissions` table with composite PK
  - [ ] Seeds system permissions and default RolePermission assignments
  - [ ] Can run independently of Phase 2 migration (or combined if phases are sequential)
  - [ ] Rollback tested
- **Effort**: L
- **Dependencies**: Tasks 8.3, 8.4

---

### Phase 9: Cerbos Infrastructure & Deployment Tiers (Week 4)
**Effort: L | Risk: Low | Skills: `auth-patterns`, `error-tracking`**

Documentation and configuration for the three deployment tiers.

#### Task 9.1: Document Tier 1 — "Humble" Self-Hoster
- **File**: `docs/DEPLOYMENT_TIERS.md` (new)
- **Acceptance Criteria**:
  - [ ] Single PostgreSQL instance, shared schemas (`public` + `cerbos`)
  - [ ] Cerbos is optional — `authorization.provider = "local"` works without it
  - [ ] If Cerbos desired: single instance, same docker-compose
  - [ ] Minimal ops guide: `docker-compose up -d`
- **Effort**: S
- **Dependencies**: Phase 4

#### Task 9.2: Document Tier 2 — "Community" Hub
- **File**: `docs/DEPLOYMENT_TIERS.md`
- **Acceptance Criteria**:
  - [ ] Single PostgreSQL cluster (Primary + Replica)
  - [ ] Cerbos instances connect to replica for reads (authorization checks hit cached policies, reload from replica)
  - [ ] Admin API writes go to primary (via PolicySyncService)
  - [ ] 1-2 Cerbos instances for HA
- **Effort**: S
- **Dependencies**: Phase 4

#### Task 9.3: Document Tier 3 — "Ummah-Scale" Platform
- **File**: `docs/DEPLOYMENT_TIERS.md`
- **Acceptance Criteria**:
  - [ ] Two separate database clusters: Cluster A (high-perf SSDs, PostGIS/Event data) + Cluster B (small, high-RAM for Cerbos policies)
  - [ ] N Cerbos instances behind load balancer, stateless, horizontally scalable
  - [ ] Total isolation: app DB vulnerability cannot touch authorization policies
  - [ ] `compile.cacheDuration: 60s` for eventual consistency
  - [ ] `GET /admin/store/reload?wait=true` broadcast for critical changes
  - [ ] Sharding guidance: if policies grow huge, shard by tenant or resource type behind proxy
- **Effort**: M
- **Dependencies**: Phase 4

#### Task 9.4: Update `docs/OPERATIONS.md` with Cerbos Production Guide
- **Acceptance Criteria**:
  - [ ] Cerbos PostgreSQL schema init instructions (per tier)
  - [ ] Cerbos container configuration reference
  - [ ] Admin API credential management (Infisical)
  - [ ] Monitoring: Cerbos exposes Prometheus metrics + audit logs
  - [ ] Backup strategy for Cerbos PostgreSQL
  - [ ] Upgrade procedure (Cerbos container version bumps)
- **Effort**: M
- **Dependencies**: Task 4.9

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|------------|
| Data migration corrupts role assignments | **Critical** | Low | Write reversible migration; test on staging DB copy first; backup before migration |
| 648 references create regression cascade | **High** | Medium | Compile-first approach: fix all compiler errors before running tests; use LSP rename |
| Runtime provider switch causes auth failures | **Medium** | Low | Default to LocalProvider on any error; circuit breaker pattern; extensive logging |
| HATEOAS links break for API consumers | **Medium** | Medium | Versioned API (v1 stays, v2 introduces new names); or rename all at once with docs |
| Cerbos principal format changes break policies | **Medium** | Low | Test Cerbos policies separately; keep principal attr names stable |
| Self-hosters with existing data lose role assignments | **Critical** | Medium | Migration must be idempotent and data-preserving; provide migration guide |
| PolicySyncService fails to push to Cerbos | **Medium** | Medium | Resilient: log error but don't fail the user's command; `SyncAllPoliciesAsync` for manual recovery; overlay disk fallback covers base policies |
| Privilege escalation via custom role creation | **Critical** | Low | Capability ceiling: Rule 1 (can't grant what you don't have), Rule 2 (IsFiltered hides dangerous perms), Rule 3 (scope boundary), Rule 4 (system roles immutable) |
| Cerbos PostgreSQL becomes single point of failure | **High** | Low | Overlay driver falls back to disk-based base policies; LocalAuthorizationProvider as application-level fallback; Tier 2/3 use replicas |
| Multi-instance cache staleness causes brief permission inconsistency | **Low** | High | `compile.cacheDuration: 60s` is acceptable for permission changes; critical changes (revoke admin) use explicit `/admin/store/reload` broadcast |

---

## Success Metrics

1. **Zero data loss**: All existing role assignments preserved after migration
2. **All tests pass**: Unit, integration, architecture, component tests green
3. **Runtime switching works**: Admin can toggle between "local" and "cerbos" without restart
4. **Self-hosted single binary**: App runs with `authorization.provider=local` and no Cerbos sidecar
5. **No Cerbos-specific names in Application layer**: All Application layer code uses provider-agnostic naming
6. **Build passes**: `dotnet build --configuration Release --verbosity quiet` — zero errors
7. **Documentation updated**: DOMAIN.md, SECURITY.md, CONFIGURATION.md, DEPLOYMENT_TIERS.md reflect new architecture
8. **Dynamic permissions work**: Admin can create a custom role, assign granular permissions, and the role is immediately usable for authorization
9. **Capability ceiling holds**: No admin can assign permissions they don't have; scope escalation impossible
10. **PolicySyncService works**: Custom role creation in the app produces valid Cerbos policies in the PostgreSQL store within seconds
11. **Multi-instance consistent**: N Cerbos instances all serve the same authorization decisions within 60 seconds of a policy change
12. **Three tiers documented**: Deployment documentation covers Humble (single PG), Community (PG cluster), and Ummah-Scale (separate clusters)

---

### Phase 10: Enterprise Hardening — Resilience, Observability & CI Governance (Week 5-6)
**Effort: XL | Risk: Medium | Skills: `auth-patterns`, `error-tracking`, `clean-architecture-rules`, `blazor-bff-patterns`**

*Merged from `cerbos-enterprise-authorization-review` plan (2026-02-12). Items already addressed by the refactor are excluded — only net-new enterprise items remain.*

**Merge audit (items from enterprise review already done):**
- ~~0.1 Security docs alignment~~ → Done in Phase 7.4
- ~~1.2 Typed action/resource consistency~~ → Done in Phase 3.2 (PermissionAction, ResourceDescriptorRegistry)
- ~~1.3 Structured decision logging~~ → Already exists in AuthorizationBehavior (allow/deny with correlationId)
- ~~2.3 Fallback measurability~~ → LocalAuthorizationProvider has structured logging
- ~~3.1 HATEOAS sync-over-async fix~~ → HateoasAuthorizationEvaluator is fully async (confirmed)
- ~~3.2 Endpoint auth convention consistency~~ → Standardized during refactor

#### Task 10.1: Create Authorization Architecture Decision Record (ADR)
- **File**: `docs/ARCHITECTURAL_DECISIONS.md` (new or extend existing)
- **Source**: Enterprise review 0.2
- **Acceptance Criteria**:
  - [ ] Captures rationale for HTTP adapter (not gRPC SDK) with migration triggers
  - [ ] Captures fail-closed + fallback semantics (RuntimeAuthorizationProvider → LocalAuthorizationProvider)
  - [ ] Captures PostgreSQL overlay storage + Admin API decision
  - [ ] Captures three deployment tiers rationale
  - [ ] Documents when/why to switch from Local to Cerbos provider
- **Effort**: S
- **Dependencies**: Phase 7.4 (docs already partially updated)

#### Task 10.2: Document Authorization Pattern Selection Rules
- **Files**: New doc `docs/AUTHORIZATION_PATTERNS.md` or section in ARCHITECTURE.md
- **Source**: Enterprise review 1.1
- **Acceptance Criteria**:
  - [ ] Decision tree: when to use `IAuthorizedRequest` vs `[AuthorizeResource]` vs `ISecureRequest`
  - [ ] Concrete code examples for each pattern mapped to existing command patterns
  - [ ] Documents the MediatR pipeline order: Validation → Authorization → Handler
  - [ ] Explains permission-based auth (HasPermissionInOrganization) vs Cerbos ABAC
- **Effort**: S
- **Dependencies**: None

#### Task 10.3: Extract Principal/Resource Payload Builder
- **Files**: `Explore.Infrastructure/Services/CerbosAuthorizationProvider.cs`, new `CerbosPrincipalBuilder.cs`
- **Source**: Enterprise review 2.1
- **Acceptance Criteria**:
  - [ ] Principal construction (user ID, roles, tenant memberships, org memberships, permissions) extracted to a dedicated builder class
  - [ ] Resource payload mapping extracted from service method body
  - [ ] Builder is unit-testable with deterministic output
  - [ ] Edge cases covered: anonymous user, user with no roles, user with multiple scopes
  - [ ] Existing CerbosAuthorizationProvider delegates to builder
- **Effort**: M
- **Dependencies**: None (refactoring only)

#### Task 10.4: Resilience Policy Hardening for Cerbos HTTP Communication
- **Files**: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `Explore.Infrastructure/Services/CerbosAuthorizationProvider.cs`
- **Source**: Enterprise review 2.2
- **Implementation guidance** (from research):
  - Use `AddResilienceHandler("cerbos-pdp-resilience", ...)` on the named HttpClient (Polly v8+ / `Microsoft.Extensions.Http.Resilience`)
  - `.AddTimeout(TimeSpan.FromSeconds(2))` per attempt
  - `.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions { FailureRatio = 0.5, SamplingDuration = 30s, MinimumThroughput = 10, BreakDuration = 15s })`
  - **No retry for auth checks** — fail-fast to LocalAuthorizationProvider is safer (retrying a denied check is a security risk, retrying a slow check adds latency)
  - Log circuit breaker state transitions (OPEN/HALF-OPEN/CLOSED) as structured events
- **Acceptance Criteria**:
  - [ ] Explicit timeout policy for Cerbos HTTP calls (2s per check, 5s for batch)
  - [ ] Circuit breaker: after N consecutive failures, open circuit and route to LocalAuthorizationProvider
  - [ ] No retry for authorization checks (explicit no-retry with documented rationale)
  - [ ] Polly resilience handler configured on the named HttpClient
  - [ ] Behavior under Cerbos downtime: deterministic fail-closed → LocalAuthorizationProvider
  - [ ] Metrics: circuit breaker state changes logged as structured events
- **Effort**: M
- **Dependencies**: None

#### Task 10.5: Admin Cache Invalidation Strategy
- **Files**: `Explore.Infrastructure/Identity/AdminContext.cs`, role/permission command handlers
- **Source**: Enterprise review 2.4
- **Acceptance Criteria**:
  - [ ] When `UpdateRolePermissionsCommand` or `DeleteCustomRoleCommand` executes, affected user authority caches are invalidated
  - [ ] AdminContext cache key/TTL strategy documented (currently 5-min sliding)
  - [ ] Option A: Explicit invalidation via `IMemoryCache.Remove(key)` for affected user IDs after role changes
  - [ ] Option B: Reduce TTL for permission-sensitive paths (1-min instead of 5-min)
  - [ ] Decision documented with trade-offs (eventual consistency vs responsiveness)
  - [ ] For multi-instance: note that IMemoryCache is instance-local — distributed cache (Redis) is Tier 2/3 concern
- **Effort**: M
- **Dependencies**: Phase 8.7 (role CQRS commands)

#### Task 10.6: Correlation-ID Propagation End-to-End
- **Files**: API middleware, AuthorizationBehavior, CerbosAuthorizationProvider, PolicySyncService
- **Source**: Enterprise review 3.3
- **Acceptance Criteria**:
  - [ ] Correlation-ID present in: API request log → AuthorizationBehavior decision log → Cerbos HTTP request header → PolicySync events
  - [ ] Uses existing OpenTelemetry trace context where available
  - [ ] Request trace links API call to Cerbos PDP decision log
  - [ ] Verify: `X-Request-ID` or `traceparent` header sent to Cerbos HTTP API
- **Effort**: S
- **Dependencies**: None (already partially done in AuthorizationBehavior)

#### Task 10.7: Formalize Client-Side Auth as UX-Only Documentation
- **Files**: `docs/SECURITY.md` (extend), Blazor route guards
- **Source**: Enterprise review 4.1
- **Acceptance Criteria**:
  - [ ] Explicit statement in SECURITY.md: "Client-side route guards and menu visibility are UX hints only. The MediatR AuthorizationBehavior on the server is the authoritative enforcement boundary."
  - [ ] Blazor client guards reference shared claim constants (AdminClaimTypes)
  - [ ] Document: what happens if a user manually navigates to a protected route (server rejects the API call)
- **Effort**: S
- **Dependencies**: None

#### Task 10.8: Org-Admin Route Access Policy Decision
- **Files**: `Explore.Blazor.Client/Routing/Guards/AdminRouteGuard.cs`, route definitions
- **Source**: Enterprise review 4.2
- **Acceptance Criteria**:
  - [ ] Decision documented: which admin surfaces org-admins can access vs platform/tenant-admins only
  - [ ] Route guard logic updated to match decision
  - [ ] Tests updated/added to verify guard behavior for org-admin, tenant-admin, platform-admin
  - [ ] Guard uses `PermissionCodes` or centralized constants (not magic strings)
- **Effort**: M
- **Dependencies**: Phase 6 (Blazor UI complete), Phase 8 (permissions)

#### Task 10.9: Cerbos Policy Compile/Test CI Gate
- **Files**: CI workflow files (`.github/workflows/`), `cerbos/policies/`
- **Source**: Enterprise review 5.1
- **Implementation guidance** (from research):
  - Use official GitHub Actions: `cerbos/cerbos-setup-action@v1` + `cerbos/cerbos-compile-action@v1`
  - Or Docker: `docker run -it -v $(pwd):/workspace ghcr.io/cerbos/cerbos:latest compile --tests=/workspace/cerbos/tests /workspace/cerbos/policies`
  - Cerbos has built-in audit logging (file, Kafka, or local backends) — configure `audit.decisionLogsEnabled: true` in `.cerbos.yaml`
  - Cerbos audit includes: callId, timestamp, principal, resource, actions, effect, policy source, metadata (correlation-id)
- **Acceptance Criteria**:
  - [ ] CI step uses `cerbos/cerbos-setup-action@v1` + `cerbos/cerbos-compile-action@v1`
  - [ ] CI fails on invalid policy syntax or logic errors
  - [ ] Policy test fixtures exist in `cerbos/tests/` directory
  - [ ] `cerbos compile --tests=./cerbos/tests` runs test cases against policies
  - [ ] Cerbos audit logging enabled in `.cerbos.yaml` config (`audit.decisionLogsEnabled: true`)
  - [ ] Documented in CI workflow and OPERATIONS.md
- **Effort**: M
- **Dependencies**: Phase 4.8 (base policy files exist)

#### Task 10.10: Permission Matrix Test Suite
- **Files**: `cerbos/tests/` (new), policy test YAML files
- **Source**: Enterprise review 5.2
- **Implementation guidance** (from research — Cerbos test fixture format):
  ```yaml
  # cerbos/tests/event_tests.yaml
  name: EventPolicyTestSuite
  description: Tests for event resource authorization
  principals:
    super_admin: { id: "sa-1", roles: ["platform.super_admin"] }
    org_admin: { id: "oa-1", roles: ["org.admin"], attr: { org_ids: ["org-1"] } }
    org_member: { id: "om-1", roles: ["org.member"], attr: { org_ids: ["org-1"] } }
    org_viewer: { id: "ov-1", roles: ["org.viewer"], attr: { org_ids: ["org-1"] } }
    anonymous: { id: "anon-1", roles: ["anonymous"] }
  resources:
    org1_event: { id: "evt-1", kind: "event", attr: { org_id: "org-1" } }
  tests:
    - name: "Event CRUD by role"
      input:
        principals: [super_admin, org_admin, org_member, org_viewer, anonymous]
        resources: [org1_event]
        actions: [create, read, update, delete]
      expected:
        - principal: super_admin
          resource: org1_event
          actions: { create: EFFECT_ALLOW, read: EFFECT_ALLOW, update: EFFECT_ALLOW, delete: EFFECT_ALLOW }
        - principal: org_viewer
          resource: org1_event
          actions: { create: EFFECT_DENY, read: EFFECT_ALLOW, update: EFFECT_DENY, delete: EFFECT_DENY }
  ```
- **Acceptance Criteria**:
  - [ ] Test matrix covers: platform.super_admin, tenant.owner, org.admin, org.member, org.viewer, authenticated (no org role), anonymous
  - [ ] Tests cover: CRUD for events, organizations, organization_members, tenant_settings, instance_settings
  - [ ] Tests cover: lock semantics (`isLockedByInstance`) and tenant/org boundary checks
  - [ ] Tests cover: custom roles with specific permissions (e.g., "can edit events but not delete")
  - [ ] Uses Cerbos policy test format: `principals`, `resources`, `actions`, `expected` (EFFECT_ALLOW/EFFECT_DENY)
  - [ ] Runs as part of CI gate (Task 10.9)
- **Effort**: L
- **Dependencies**: Task 10.9, Phase 4.8

#### Task 10.11: End-to-End Integration Authorization Tests
- **Files**: `Event.API.IntegrationTests/`
- **Source**: Enterprise review 6.2
- **Acceptance Criteria**:
  - [ ] API-level tests for allow/deny on representative write endpoints (create event, update org member role, delete event)
  - [ ] HATEOAS link filtering verified: admin sees edit/delete links, viewer does not
  - [ ] Tests verify both Cerbos and LocalAuthorizationProvider paths (using test configuration)
  - [ ] Tests cover permission-based auth (HasPermissionInOrganization) for org-scoped operations
  - [ ] Tests use `WebApplicationFactory<Program>` with test auth setup
- **Effort**: XL
- **Dependencies**: All previous phases

#### Task 10.12: Blazor Authorization Test Stabilization
- **Files**: `Explore.Blazor.Client.Tests/`
- **Source**: Enterprise review 6.3
- **Acceptance Criteria**:
  - [ ] Admin route guard tests pass for all admin levels (platform, tenant, org)
  - [ ] Menu visibility tests verify correct items shown per role
  - [ ] Pre-existing failures clearly distinguished from regressions
  - [ ] Tests resilient to UI structure changes (use semantic selectors, not implementation details)
- **Effort**: M
- **Dependencies**: Task 10.8

---

#### Cross-Cutting Enterprise Quality Gates (from Enterprise Review)

| Gate | Criteria | How to Verify |
|------|----------|---------------|
| **Architecture Compliance** | No dependency rule violations introduced | Architecture tests pass (Phase 7.3) |
| **Observability Compliance** | Structured decision logs for all deny paths, auth metrics visible | Verify AuthorizationBehavior logging + Prometheus metrics endpoint |
| **Policy Governance** | Cerbos policy compile/test required in CI | CI workflow includes cerbos compile step (Task 10.9) |
| **Documentation Parity** | Security and architecture docs match implementation | SECURITY.md, ARCHITECTURE.md, AUTHORIZATION_PATTERNS.md reviewed |
