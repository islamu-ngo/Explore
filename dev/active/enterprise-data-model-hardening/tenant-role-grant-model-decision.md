<!-- ABOUTME: Records the Phase 2.1 tenant role grant model decision. -->
<!-- ABOUTME: Defines the target entity shape, constraints, migration path, and API contract direction. -->

# Tenant Role Grant Model Decision

Last Updated: 2026-05-27 Europe/Brussels

## Decision

Replace `TenantMember` with `TenantUserRoleGrant`.

`TenantUser` remains the tenant-local authority root for a global `User`: it owns tenant participation, status, moderation, actor/profile linkage, soft-delete state, and the one-row-per-tenant/user invariant. Tenant roles become auditable grant rows hanging from that tenant-local user root.

## Target Entity Shape

`TenantUserRoleGrant` should be a strict tenant entity and auditable role-grant record:

- `Id: Guid`
- `TenantId: Guid`
- `TenantUserId: Guid`
- `RoleId: int`
- `RoleScopeId: int`, always `RoleScopeEnum.Tenant`
- `GrantedAt: DateTime`
- `GrantedBy: Guid?`
- `RevokedAt: DateTime?`
- `RevokedBy: Guid?`
- `RevocationReason: string?`
- audit fields from `IAuditableEntity`

Authority checks must consider only grants where:

- the related `TenantUser` has the same `TenantId`;
- the related `TenantUser` is active and not soft-deleted;
- the grant has not been revoked;
- the role is tenant-scoped.

## Persistence Rules

- Add `TenantUser` alternate key `{ TenantId, Id }`.
- Add `TenantUserRoleGrant` composite FK `{ TenantId, TenantUserId } -> TenantUser { TenantId, Id }`.
- Add `Role` alternate key `{ Id, RoleScopeId }`.
- Add `TenantUserRoleGrant` composite FK `{ RoleId, RoleScopeId } -> Role { Id, RoleScopeId }`.
- Add check constraint `role_scope_id = 1` so the database rejects platform, organization, group, and event roles in tenant grants.
- Add unique active-grant index on `{ TenantId, TenantUserId, RoleId }` filtered to `revoked_at IS NULL`.
- Keep historical revoked rows instead of hard-deleting role-grant evidence.

This follows the EF Core alternate-key/foreign-key pattern confirmed through Context7 official EF Core documentation: `HasPrincipalKey` is required when a relationship targets an alternate key, while unique indexes alone cannot be used as FK targets.

## Migration Path

1. Create `tenant_user_role_grants`.
2. Backfill from `tenant_members` by joining `tenant_members.{tenant_id,user_id}` to `tenant_users.{tenant_id,user_id}`.
3. Fail the migration if any `TenantMember` has no matching `TenantUser`.
4. Fail the migration if any `TenantMember.RoleId` is not tenant-scoped.
5. Copy `GrantedAt`, `GrantedBy`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, and `UpdatedBy`.
6. Drop `tenant_members` and remove the old domain/persistence model in the same development-mode breaking slice.
7. Replace public API/HAL/Cerbos/Blazor tenant-member contracts with explicit tenant role grant contracts.

## API And Application Direction

Remove update-in-place semantics for tenant role authority. A role grant is created or revoked, not mutated into a different role.

Replace:

- `TenantMemberDto`
- `TenantMemberListDto`
- `CreateTenantMemberDto`
- `UpdateTenantMemberDto`
- former tenant-member controller/routes
- `ITenantMemberRepository`

With:

- `TenantUserRoleGrantDto`
- `TenantUserRoleGrantListDto`
- `CreateTenantUserRoleGrantDto`
- `RevokeTenantUserRoleGrantDto` or command-only revoke request
- tenant role grant controller/routes
- tenant role grant repository/service contracts

The create contract should accept `TenantUserId` and `RoleId`, not arbitrary `UserId`; tenant-local participation must exist before authority is granted. Provisioning/onboarding flows that create the first admin should create the `TenantUser` and `TenantUserRoleGrant` in one transaction.

## Alternatives Rejected

- Keep `TenantMember`: preserves the current split where role authority is connected to tenant-local lifecycle only by repository logic.
- Add `RoleId` to `TenantUser`: supports only one tenant role and destroys grant history.
- Generic cross-scope `UserRoleAssignment`: obscures tenant-specific lifecycle and makes scope-specific FKs/HAL policies harder to enforce.
- Rename `TenantMember` without reshaping it: improves naming but does not fix the database invariant.

## Next Implementation Slice

Task 2.2/2.3 implemented the domain/persistence model, migration, schema DBML update, repository contract, authority checks, provisioning updates, and targeted tests. Task 2.4 implemented the public API/HAL/Cerbos/OpenAPI/Blazor-client replacement with `/api/tenant-user-role-grants`, `TenantUserRoleGrantDto/ListDto`, create/revoke semantics, and resource kind `islamuevent_tenant_user_role_grant`.
