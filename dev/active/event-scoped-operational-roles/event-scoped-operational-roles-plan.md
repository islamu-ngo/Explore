<!-- ABOUTME: Implementation plan for event-scoped operational roles and per-event authorization grants. -->
<!-- ABOUTME: Defines the Clean Architecture, Cerbos, HAL, persistence, and verification path for one-event role restrictions. -->

# Event-Scoped Operational Roles Implementation Plan

Last Updated: 2026-04-30

## Executive Summary

ISLAMU Event already has the correct primitives for role-based authorization: `Role`, `Permission`, `RolePermission`, and `RoleScopeEnum`. The missing piece is not more workspace, organizer, or subtenant scoping; the missing piece is a persisted event-instance assignment that says **this user has this role for this specific event**.

This plan adds event-scoped operational roles without introducing `OrganizerScope`, `Workspace`, `BusinessScope`, `SubTenant`, or a generic resource-assignment abstraction. The approved design is:

1. Define the shared event-role authorization truth table before implementation.
2. Add `RoleScopeEnum.Event` to classify event-level role/permission templates.
3. Add `EventRoleAssignment(TenantId, EventId, UserId, RoleId)` with explicit lifecycle, audit, PostgreSQL-safe `Version` concurrency, and owner invariants.
4. Persist/index assignments and seed only the first-release roles: `EventOwner`, `EventManager`, `RegistrationManager`, and `CheckInStaff`.
5. Audit and fix event-child `TenantId`/`EventId` propagation before authorization engines depend on those resources.
6. Add a batch event authority snapshot service so API/HAL authorization does not create N+1 query storms.
7. Implement local fallback authorization first, then Cerbos, from the same matrix and prove parity before API/HAL and Blazor work.
8. Keep Blazor role-agnostic by continuing to use HAL `_links` as the only UI action source of truth.

The result is straightforward for users: an authorized event owner/manager assigns someone as “Check-in Staff” or “Registration Manager” on one event team page, and that role only works for that event.

## Current State Analysis

All file paths below were verified before inclusion.

### Current Role and Permission Model

- `Explore.Domain/Role.cs` defines `Role` with `MasterCode`, `FullName`, `Description`, `Scope`, and `IsSystem`.
- `Explore.Domain/Permission.cs` defines permission capabilities by `ResourceKind`, `Action`, `MasterCode`, `Scope`, and active/filter flags.
- `Explore.Domain/RolePermission.cs` links roles to permissions.
- `Explore.Domain/Enums/RoleScopeEnum.cs` currently supports `Platform`, `Tenant`, `Organization`, and `Group`.
- `Explore.Domain/Enums/RoleEnum.cs` contains platform, tenant, organization, and group role constants.
- `Explore.Domain/Constants/PermissionCodes.cs` already contains event permission vocabulary such as `event:create`, `event:update`, and related resource/action constants.
- `Explore.Persistence/Seed/LookupTableSeeder.cs` seeds current roles, permissions, and role-permission mappings.

### Current Assignment Model

- `Explore.Domain/PlatformUserRole.cs` assigns global/platform roles.
- `Explore.Domain/TenantMember.cs` assigns tenant membership roles.
- `Explore.Domain/OrganizationMember.cs` assigns organization membership roles.
- `Explore.Domain/GroupMember.cs` assigns group membership roles.
- `Explore.Application/Features/OrganizationMembers/Requests/Commands/UpdateOrganizationMemberRoleCommand.cs` updates organization member roles.
- `Explore.Application/Features/TenantMembers/Requests/Commands/UpdateTenantMemberCommand.cs` updates tenant member roles.
- `Explore.Application/Features/GroupMembers/Requests/Commands/UpdateGroupMemberRoleCommand.cs` updates group member roles.

There is no verified `EventMember`, `EventRole`, or `EventRoleAssignment` model today. Existing assignment entities do not persist `EventId`, so current roles cannot be restricted to one event row.

### Current Event Authorization Flow

- `Explore.Application/Authorization/AuthorizationScope.cs`, `ISecureRequest.cs`, `IAuthorizedRequest.cs`, `ResourceDescriptors.cs`, and `ResourceDescriptorRegistry.cs` define resource authorization descriptors.
- `Explore.Application/Authorization/CapabilityCeilingService.cs` already models anti-escalation ceilings for assignable capabilities.
- `Explore.Application/Authorization/PermissionRegistryService.cs` provides cached permission catalog lookups.
- `Explore.Application/Services/EventActorResolver.cs` resolves organization/group actor permissions for event creation paths.
- `Explore.Application/Behaviors/AuthorizationBehavior.cs` invokes authorization checks for secured requests.
- `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs` switches between BYO Cerbos, instance Cerbos, and local fallback.
- `Explore.Infrastructure/Services/CerbosAuthorizationService.cs` sends authorization checks to Cerbos.
- `Explore.Infrastructure/Services/CerbosPrincipalBuilder.cs` currently shapes principal attributes with instance, tenant, and organization memberships.
- `Explore.Infrastructure/Services/FallbackAuthorizationService.cs` and `Explore.Infrastructure/Services/FallbackAuthorizationService.Evaluators.cs` provide local authorization fallback.

Current event checks are authorization-resource aware, but they are not backed by event-specific role assignments. Event operations are effectively governed through tenant/org/group membership permissions and resource attributes.

### Current Cerbos and HAL Model

- `cerbos/policies/derived_roles.yaml` derives instance, tenant, and organization admin roles from principal/resource attributes.
- `cerbos/policies/event.yaml`, `event_session.yaml`, `event_day.yaml`, `event_agenda_item.yaml`, `event_session_agenda_item.yaml`, `event_registration.yaml`, and `event_contact_share_consent.yaml` define event-family resource rules.
- `cerbos/policies/_schemas/principal.json`, `event.json`, `event_session.json`, and `organization_member.json` validate selected request attributes.
- `cerbos/tests/event_test.yaml` validates event policy behavior.
- `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs` gates HAL links through server authorization.
- `Explore.API/Hateoas/ResourceAssemblerBase.cs` already supports batched/deduped HAL authorization.
- `Explore.API/Hateoas/LinkDefinitionPermissionExtensions.cs` attaches permission requirements to links.
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`, `EventSessionLinkPolicy.cs`, `EventDayLinkPolicy.cs`, `EventAgendaItemLinkPolicy.cs`, `EventSessionAgendaItemLinkPolicy.cs`, and `EventRegistrationLinkPolicy.cs` define event-family affordances.
- `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs` and `Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` consume HAL links instead of checking roles directly.

Important current gap: event resource descriptors and policies need attribute parity. Every event and event-child authorization resource must consistently include `tenantId` and `eventId`; missing `eventId` must deny.

## External Research Findings

Cerbos guidance supports a static-policy/dynamic-context model:

- Resource-instance roles should be represented as application data and passed into authorization requests through `principal.attr` or `resource.attr`.
- Resource policies remain the primary grant point; derived roles are appropriate for contextual roles such as `event_owner`, `event_manager`, `registration_manager`, or `checkin_staff`.
- Schemas should validate every `request.resource.attr.*` and `request.principal.attr.*` field that policies read; malformed or missing event attributes should fail closed.
- Policy tests should be matrix-based and cover both allowed and denied paths.
- Local fallback must share the same input contract as Cerbos; the first release must only use semantics that can be reproduced exactly in both engines.

Primary references:

- Cerbos best practices: `https://docs.cerbos.dev/cerbos/latest/policies/best_practices.html`
- Cerbos derived roles: `https://docs.cerbos.dev/cerbos/latest/policies/derived_roles.html`
- Cerbos schemas: `https://docs.cerbos.dev/cerbos/latest/policies/schemas.html`
- Cerbos policy testing: `https://docs.cerbos.dev/cerbos/latest/tutorial/04_testing-policies.html`
- Cerbos policy compilation: `https://docs.cerbos.dev/cerbos/latest/policies/compile.html`
- EF Core app-managed concurrency tokens: `https://learn.microsoft.com/en-us/ef/core/saving/concurrency`
- EF Core indexes: `https://learn.microsoft.com/en-us/ef/core/modeling/indexes`
- Npgsql PostgreSQL `xmin` concurrency token alternative: `https://www.npgsql.org/efcore/modeling/concurrency.html`
- EF Core save interceptors/auditing: `https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors`
- .NET metrics instrumentation: `https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation`

## Shared Event-Role Decision Matrix

This matrix is the source of truth for fallback, Cerbos, API/HAL, and tests. Implement it once as shared internal policy data and generate equivalent local and Cerbos tests from it.

### Base allow predicate

An event-scoped decision may allow only when all conditions are true:

1. Principal tenant matches resource tenant.
2. Resource carries a non-empty `eventId`.
3. Assignment tenant matches resource tenant.
4. Assignment event matches resource event.
5. Assignment is effective at the current time: `Status == Active && StartsAtUtc <= now && (ExpiresAtUtc IS NULL || ExpiresAtUtc > now)`.
6. Assignment role has the permission for the requested action.
7. Event status permits the requested action.

If any condition is false or unknown, the decision is deny. Missing `eventId` is always deny for event-child resources. Authorization must not depend on a background job materializing `Expired`; `Expired` is lifecycle/reporting state, while the effective predicate above is the security boundary.

### Deny reason categories

Use stable reason categories for tests, metrics, and troubleshooting:

- `tenant_mismatch`
- `missing_event_id`
- `event_mismatch`
- `assignment_not_found`
- `assignment_not_effective`
- `permission_missing`
- `event_status_blocked`
- `authority_ceiling_exceeded`
- `last_owner_violation`
- `instance_admin_normal_flow_denied`

### First-release role matrix

| Role | Purpose | First-release permission intent | Delegation intent |
| --- | --- | --- | --- |
| `EventOwner` | Accountable event authority | Full event-team management and ownership transfer within tenant/event rules | Can transfer ownership and manage event team, subject to last-owner invariant |
| `EventManager` | Operational event manager | Manage event operations, sessions, basic team staffing, and registration/check-in workflows | May assign `RegistrationManager` and `CheckInStaff`; cannot assign owner/finance/delete powers |
| `RegistrationManager` | Registration operations | View/manage registrations for the assigned event | Cannot assign roles in v1 unless explicitly granted later |
| `CheckInStaff` | Door/check-in operations | Perform check-in for the assigned event | Cannot assign roles |

Backlog roles that must not be seeded in the first implementation slice: `SpeakerCoordinator`, `Volunteer`, `EventModerator`, `FinanceManager`, `ContentEditor`, `Reviewer`, `Approver`.

## Proposed Future State

### Domain Model

Add `RoleScopeEnum.Event` for event-scoped role and permission definitions.

Add a tenant-scoped `EventRoleAssignment` entity:

- `Id` (`Guid`, UUIDv7)
- `TenantId`
- `EventId`
- `UserId`
- `RoleId`
- `Status` (`Pending`, `Active`, `Revoked`, `Expired`)
- `StartsAtUtc`
- `ExpiresAtUtc`
- `RevokedAtUtc`
- `RevokedByUserId`
- `CreatedAtUtc`
- `CreatedByUserId`
- `UpdatedAtUtc`
- `UpdatedByUserId`
- `Version` (`long`, app-managed EF concurrency token incremented on lifecycle-changing commands)

The entity is not a workspace and not a generic `ResourceRoleAssignment`. It is the event equivalent of a membership assignment: a direct statement that a user has a specific operational role for a specific event.

Use the app-managed numeric `Version` strategy for v1. Do not model PostgreSQL concurrency as a SQL Server-style `byte[] RowVersion`; PostgreSQL `xmin` is acceptable only if the implementation deliberately chooses the provider-specific strategy and updates this plan/tests accordingly.

Lifecycle transitions must be explicit:

- `Pending -> Active` when the grant reaches `StartsAtUtc` or is accepted/activated by the command model.
- `Pending -> Revoked` when a pending grant is cancelled.
- `Active -> Revoked` when an authorized actor revokes the grant.
- `Active -> Expired` when a cleanup/reporting workflow materializes expiry after `ExpiresAtUtc` is reached.
- `Expired` and `Revoked` are terminal for authorization and never grant permissions.

Effective authorization is always computed from status plus the UTC validity window: `Status == Active && StartsAtUtc <= now && (ExpiresAtUtc IS NULL || ExpiresAtUtc > now)`. A row whose `ExpiresAtUtc` is in the past denies immediately even if its stored status has not yet been materialized to `Expired`.

### Owner Invariants and Ownership Transfer

Every manageable event must have at least one effective owner or inherited managing authority. The last direct `EventOwner` assignment cannot be revoked unless another owner exists or ownership is transferred in the same transaction.

Owner rules:

- V1 ownership source of truth is assignment-only: event creation creates an initial `EventRoleAssignment(EventOwner)` for the creator in the same transaction.
- Do not add `Event.OwnerUserId` as a second authority source in v1; a denormalized display/cache field is acceptable only if it is explicitly non-authoritative.
- Ownership transfer is a first-class command, not a special case of generic update.
- Last-owner checks and ownership transfer must be transaction-safe and `Version` concurrency-aware to prevent concurrent revokes/transfers from orphaning an event.
- Pending owner assignments do not satisfy the “effective owner” invariant unless the product explicitly defines pending ownership as effective.
- Inherited managing authority is intentionally narrow for v1: tenant administrators may manage; organization administrators may manage only events owned by their organization; group administrators may manage only events owned by their group if current group authority already covers that event; Instance Admins are denied in normal flows; Event Managers do not satisfy orphan recovery unless a later explicit `system_recovery` flow is designed.

### Authority Ceiling

Assignment authority must be deterministic and reusable:

```text
Assignable permissions = assigner's effective permissions on this same event
                         - forbidden-delegation permissions
                         - system-reserved permissions

CanAssign(role) = role.permissions ⊆ assignablePermissions
```

Non-delegable permissions for v1 include:

- `event:manage-owner`
- `event:transfer-ownership`
- `event:delete`
- `event:manage-finance`

The same ceiling logic must be reused by commands, `GetAssignableEventRolePresets`, HAL action generation, local fallback checks, and Cerbos request construction. Do not expose every `RoleScopeEnum.Event` role as assignable. The ceiling must distinguish direct and inherited authority sources for audit/debug output: `event_assignment`, `organization_admin`, `tenant_admin`, and `system_recovery`.

### Authority Snapshot Service

Add an application-layer batch service to avoid HAL N+1 queries:

```csharp
Task<EventAuthoritySnapshot> GetForUserAndEventsAsync(
    Guid tenantId,
    Guid userId,
    IReadOnlyCollection<Guid> eventIds,
    CancellationToken cancellationToken);
```

Snapshot shape:

```csharp
public sealed record EventAuthoritySnapshot(
    Guid TenantId,
    Guid UserId,
    IReadOnlyDictionary<Guid, EventAuthorityForUser> Events);

public sealed record EventAuthorityForUser(
    IReadOnlySet<string> RoleCodes,
    IReadOnlySet<string> PermissionCodes,
    bool IsOwner,
    bool IsManager);
```

The `IEventAuthoritySnapshotService` interface and DTOs live in the Application layer; implementation lives in Persistence or Infrastructure according to repository conventions. Infrastructure, fallback, Cerbos request construction, API/HAL, and commands depend on the Application contract, never on a Persistence detail. The implementation may include internal decision details, authority source, source assignment IDs, and deny reasons for audit/debug output, but the public application contract should remain stable and batch-oriented.

### User Experience

Expose this as an event team management capability, not as an advanced security screen:

- Event page action: “Team” or “Staff”.
- Authorized actors assign simple preset roles returned by `GetAssignableEventRolePresets(eventId, assignerUserId)`.
- Each preset displays plain-language capabilities.
- UI never decides access by role name, claim, or enum. It shows actions only when HAL links exist.

### Instance Admin Boundary

Instance Admins operate infrastructure. They must not normally access tenant business data, modify tenant content, override tenant business rules, or casually manage event teams through the normal event-team UI.

If emergency access is required later, it must be a separate audited flow with explicit reason capture, structured logging, and no normal UI affordance. Normal event-team management is tenant/org/event authority only.

### Event-Child Resource Propagation

Every event-child authorization resource must carry `TenantId` and `EventId`; missing `eventId` must deny. This includes:

- sessions
- event days and agenda items
- session agenda items
- registrations
- payment records
- content review objects
- check-in records
- speaker coordination objects
- moderation objects

Centralize parent-event lookup where a child row does not directly carry `EventId`, and add tests that verify fail-closed behavior when the parent cannot be resolved. This propagation audit happens before fallback or Cerbos event-role rules are implemented so both engines rely on the same tenant/event resource contract.

## Implementation Phases by CTO Priority

### Phase 0 — Truth Table and Parity Contract

1. Encode the shared event-role decision matrix in documentation and tests before production code.
2. Define stable deny reason categories.
3. Define first-release roles and permission bundles.
4. Define local fallback and Cerbos parity fixtures from the same matrix.

### Phase 1 — Domain

1. Add `Event` to `Explore.Domain/Enums/RoleScopeEnum.cs`.
2. Add first-release event operational role constants to `Explore.Domain/Enums/RoleEnum.cs` only if enum-backed role IDs remain the seeding convention.
3. Extend `Explore.Domain/Constants/PermissionCodes.cs` with missing event operational permission codes.
4. Add `Explore.Domain/EventRoleAssignment.cs` with lifecycle status, timestamps, audit fields, and app-managed `Version` concurrency.
5. Add ownership-transfer and last-owner invariants to the command/domain model without adding external dependencies to Domain.

### Phase 2 — Persistence and Indexes

1. Add `DbSet<EventRoleAssignment>` in `Explore.Persistence/ExploreDbContext.DbSets.cs`.
2. Add EF configuration in `Explore.Persistence/Configurations/Entities/EventRoleAssignmentConfiguration.cs`.
3. Add repository implementation in `Explore.Persistence/Repositories/EventRoleAssignmentRepository.cs`.
4. Add a focused EF Core migration.
5. Configure `Version` as an app-managed EF concurrency token; increment it on lifecycle transitions and authority-changing updates.
6. Do not use normal soft delete for assignment lifecycle. Revoked and expired rows are evidence and remain queryable for history/audit; if future admin cleanup archives rows, authorization must still depend on lifecycle and validity-window predicates, not soft delete.
7. Add indexes:
   - PostgreSQL partial unique index on `(TenantId, EventId, UserId, RoleId)` where status is `Pending` or `Active`
   - fast authorization lookup on `(TenantId, UserId, EventId, Status)`
   - event team listing on `(TenantId, EventId, UserId, Status)`
8. Add validity-window constraint such as `ExpiresAtUtc IS NULL OR StartsAtUtc < ExpiresAtUtc`.
9. Keep tenant query filters active on every runtime path.
10. If future multiple scheduled assignments are allowed, revisit the uniqueness rule with a PostgreSQL date-range exclusion constraint; v1 allows only one pending/active assignment per user/event/role.

### Phase 3 — Seed First-Release Roles and Permissions

1. Seed only `EventOwner`, `EventManager`, `RegistrationManager`, and `CheckInStaff` with `RoleScopeEnum.Event`.
2. Seed only the permissions required by those roles for v1.
3. Link roles to permission bundles through `RolePermission`.
4. Move all other event operational roles to backlog documentation until the v1 matrix is proven.

### Phase 4 — Event-Child Resource Propagation Audit

1. Audit event-child descriptors/resources for sessions, event days, agenda items, session agenda items, registrations, payment records, content review, check-in, speaker coordination, and moderation.
2. Ensure every authorization resource carries `TenantId` and `EventId` directly or through a centralized tenant-safe parent lookup.
3. Add fail-closed tests for missing/unresolved parent event IDs.
4. Complete this before fallback or Cerbos policy implementation.

### Phase 5 — Application CQRS and Authority Snapshot Service

1. Add `IEventRoleAssignmentRepository` to Application contracts.
2. Add `IEventAuthoritySnapshotService` with the batch contract above.
3. Add DTOs for event team members and assignable role presets.
4. Add `GetAssignableEventRolePresets(eventId, assignerUserId)` that returns only roles the actor may assign.
5. Add commands:
   - assign event role
   - revoke event role
   - update assignment lifecycle/window
   - transfer event ownership
6. Enforce authority ceiling and owner invariants inside transaction/concurrency boundaries.
7. Add cache invalidation hooks for assignment changes and role-permission changes.
8. Record direct/inherited authority source for audit/debug paths without leaking high-cardinality labels into metrics.

### Phase 6 — Local Fallback Authorization First

1. Extend `FallbackAuthorizationService` and evaluator partials to honor event role assignments.
2. Implement the shared decision matrix exactly, including deny reasons.
3. Enforce tenant match, event match, lifecycle effectiveness, permission match, event status, and missing-event deny.
4. Add local fallback matrix tests before Cerbos policy work.

### Phase 7 — Cerbos Second

1. Extend `CerbosPrincipalBuilder` or the authorization request construction path to include only event assignments relevant to the checked resource event or checked resource batch.
2. Extend `principal.json` with the event assignment payload shape.
3. Ensure event-family resource schemas require `eventId` and `tenantId` where needed.
4. Add derived roles in `derived_roles.yaml` for first-release event operational roles.
5. Update `event.yaml` and event-child policies to allow matching event-scoped roles.
6. Add Cerbos policy tests for same-event allow, other-event deny, other-tenant deny, missing-eventId deny, revoked/expired deny, authority-ceiling deny, and instance-admin-normal-flow deny.
7. Never send all of a principal's event roles. The payload must be scoped to matching `tenantId` plus target `eventId`/batch `eventIds` to cap size and avoid privacy leakage.

### Phase 8 — Parity Tests

1. Generate or maintain local fallback and Cerbos tests from the same matrix.
2. Update `Event.Architecture.Tests/AuthorizationParityTests.cs` so every new `ResourceKind`/descriptor/fallback switch/schema/policy stays aligned.
3. Compare decisions for missing event ID, revoked roles, expired roles, pending roles, owner transfer, last-owner violation, and assignment authority ceiling.
4. Treat Cerbos/local drift as release-blocking.

### Phase 9 — API and HAL

1. Add event team endpoints under the event resource, for example `/api/events/{eventId}/team`.
2. Protect writes with `[Authorize]` plus handler/resource authorization, not controller-only role checks.
3. Add or update HAL policies so event detail exposes team/manage/check-in links only when server authorization allows them.
4. Use `IEventAuthoritySnapshotService` for batch event authority lookup in HAL paths.
5. Ensure resource descriptors for event and event-child resources always carry `tenantId` and `eventId`.
6. Add stale HAL tests: a `CheckInStaff` user sees a check-in link, the role is revoked/expired, the next event-detail load omits the link, and a direct protected API call returns `403`.

### Phase 10 — Blazor Last

1. Add an event team management component/page using existing Blazor and MudBlazor conventions.
2. Use HAL links to decide whether team management, check-in, and registration actions are visible.
3. Keep labels simple and operational: “Event Owner”, “Event Manager”, “Registration Manager”, “Check-in Staff”.
4. Do not expose role-scope internals to normal users.
5. Do not inspect claims, JWT roles, Keycloak roles, or role names in Blazor.

### Phase 11 — Audit, Observability, and Verification

1. Emit audit events for assign, update, revoke, expire, transfer ownership, and failed last-owner/authority-ceiling attempts.
2. Include structured log fields: `tenantId`, `eventId`, `targetUserId`, `roleId`, `actorUserId`, `correlationId`, previous status, new status, authority source, decision engine, resource kind, action, safe assignment/snapshot reference where available, and deny reason.
3. Add metric `event_role_assignment.changed`.
4. Add authorization deny metrics by reason category without high-cardinality labels such as raw user IDs or event IDs.
5. Run the verification commands listed in the task checklist.

## Detailed Tasks With Acceptance Criteria

The detailed task checklist lives in `event-scoped-operational-roles-tasks.md`.

## Risk Assessment and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| `RoleScopeEnum.Event` misunderstood as the actual assignment mechanism | Users still cannot restrict a role to one event | Document that assignment requires `EventRoleAssignment.EventId`; enum is classification only |
| Cerbos and fallback auth drift | Different environments allow different actions | Shared decision matrix, generated parity tests, and fallback-first implementation |
| Concurrent revokes/transfers orphan an event | Event becomes unmanageable or insecure | Enforce last-owner/transfer invariants inside transaction with app-managed `Version` concurrency handling |
| Missing `eventId` in resource attributes | Event roles deny unexpectedly or over-allow if incorrectly handled | Schema validation and fail-closed tests for missing attributes |
| Authority over-delegation | Event managers can grant owner/finance/delete power | Central authority ceiling service with non-delegable permission set |
| HAL N+1 query storm | Event lists/team pages become slow | Batch `IEventAuthoritySnapshotService` and use deduped HAL authorization |
| UI becomes too complex | Users feel they need security training | Present assignable presets with plain-language descriptions |
| Assignment cache stale after changes | Revoked users retain access briefly | Explicit cache invalidation on assignment/role-permission writes |
| Cross-tenant leakage | Critical security issue | Tenant-scoped entity, tenant query filters, indexes including `TenantId`, and other-tenant deny tests |
| Instance Admin normal-flow overreach | Infrastructure admins can modify tenant business data | Deny normal event-team flows; require separate emergency access later if needed |

## Success Metrics

- A user can be assigned `CheckInStaff` for Event A and cannot check in attendees for Event B.
- A user can be assigned `RegistrationManager` for Event A and cannot manage registrations for Event B.
- `EventManager` can assign allowed operational presets but cannot assign `EventOwner` or finance/delete powers.
- Revoking the last direct `EventOwner` is denied unless another owner exists or ownership is transferred atomically.
- Revoked, expired, and pending assignments do not grant authorization unless product explicitly defines pending as effective.
- Expired-by-time rows deny immediately even before a cleanup job materializes `Status = Expired`.
- Stale HAL revocation behavior is proven: after a `CheckInStaff` assignment is revoked, the next event-detail response removes the check-in link and direct API calls return `403`.
- Missing `eventId` denies for event-child resources.
- Cerbos and fallback authorization return equivalent decisions for event-scoped role scenarios.
- Blazor remains role-agnostic and is driven only by HAL links.
- No workspace/organizer/subtenant/generic resource assignment model is introduced.
- All architecture, unit, integration, Cerbos policy, parity, and build verification commands pass.

## Required Resources and Dependencies

- Existing role/permission system in Domain and Persistence.
- Existing Cerbos infrastructure and local fallback provider.
- Existing HATEOAS link authorization pipeline.
- EF Core migration for `EventRoleAssignment` and seed updates.
- Product vocabulary for final first-release role descriptions.
- Shared event-role decision matrix and parity fixtures.

## Effort Estimate

Overall estimate: **Large**.

- Truth table and parity contract: M
- Domain + seed model: M
- Persistence + migration: M
- Application handlers/queries/snapshot service: L
- Fallback + Cerbos parity: L
- API/HAL: M
- Blazor event team UI: M
- Tests and verification: L

Expected implementation time: **4–6 focused engineering days**, depending on how much event-child resource propagation must be centralized before policy work.

## Non-Goals

- No `OrganizerScope`.
- No `Workspace` domain model.
- No `BusinessScope` or `SubTenant`.
- No generic `ResourceRoleAssignment(ScopeType, ScopeId, UserId, RoleId)`.
- No UI role checks in Blazor.
- No JWT/global IdP/Keycloak role for per-event staff.
- No normal Instance Admin event-team management flow.
- No global `RegistrationManager` behavior blurred with event-level registration manager.
- No broad event access based only on role name without matching `TenantId`, `EventId`, lifecycle, and permission.
- No normal soft-delete lifecycle for event role assignments.
- No custom event roles UI or event-role inheritance in v1.
