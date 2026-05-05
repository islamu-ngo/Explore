<!-- ABOUTME: Working context and evidence for event-scoped operational roles planning. -->
<!-- ABOUTME: Captures current repo state, research findings, CTO feedback, decisions, and quick resume notes. -->

# Event-Scoped Operational Roles Context

Last Updated: 2026-05-01 Europe/Brussels — implementation handoff after verified authority-command + local fallback slices

## User Intent

The user wants an implementation plan for restricting operational roles to **one specific event**. They believe the current role/role-scope model is mostly right, but correctly identified that a role such as “Registration Manager” must be attached to a specific event, not all events.

Constraints from the user:

- Follow `.claude/commands/dev-docs.md` conventions.
- Use current role, role-scope, permission, and Cerbos concepts.
- Do not add `OrganizerScope`, `Workspace`, `BusinessScope`, `SubTenant`, or similar fake scope concepts.
- Make the solution enterprise-grade, maintainable, Clean Architecture aligned, and authorization-safe.
- Use research and repo evidence before updating the docs.
- Backward compatibility is not a concern because the product is still in development.

## Key Decision

Adding more seeded roles and adding `RoleScopeEnum.Event` is necessary but not sufficient. `RoleScopeEnum` classifies where a role belongs; it does not bind a user to a specific event instance.

The plan therefore recommends:

1. Define the shared event-role authorization truth table first.
2. Add `RoleScopeEnum.Event`.
3. Add `EventRoleAssignment` as the resource-instance assignment model.
4. Add explicit lifecycle, audit, PostgreSQL-safe `Version` concurrency, and owner invariants.
5. Seed only `EventOwner`, `EventManager`, `RegistrationManager`, and `CheckInStaff` in the first slice.
6. Audit event-child `TenantId`/`EventId` propagation before fallback/Cerbos policy work.
7. Add a batch `IEventAuthoritySnapshotService`.
8. Implement fallback authorization before Cerbos and prove parity.
9. Keep HAL links as the UI source of truth.

## Current Implementation State

Four implementation slices are now complete and verified with targeted builds/tests:

### Slice 1 — Event Role Assignment Foundation

- Added `RoleScopeEnum.Event = 4` and first-release event role enum values: `EventOwner = 41`, `EventManager = 42`, `RegistrationManager = 43`, `CheckInStaff = 44`.
- Added `EventRoleAssignmentStatus` and `EventRoleAssignment` with lifecycle states, canonical effective predicate, audit fields, and app-managed `long Version` concurrency.
- Added Application contracts: `IEventRoleAssignmentRepository` and `IEventAuthoritySnapshotService` with `EventAuthoritySnapshot`/`EventAuthorityForUser` records.
- Added Persistence implementation: EF configuration, DbSet, tenant-only filter, repository, snapshot service, DI registration, and migration `20260430162948_AddEventRoleAssignments`.
- Updated lookup seeding for first-release event roles, event-scoped permissions, role-permission mappings, and dev-db scope correction for existing event permissions.
- Added domain tests for assignment lifecycle/effective state and an API integration test for `/api/role?scope=Event`.
- Updated `CreateCustomRoleCommandHandler` to reject custom event roles in v1 while preserving existing group custom-role support.

### Slice 2 — Event-Child Tenant/Event Context Propagation

- Added `EventId`/`TenantId` propagation to session agenda item DTOs and mapping, plus `EventId` mapping for registration list/detail read paths.
- Updated `ResourceDescriptors` so event, event list, event registration, and event session agenda item descriptors expose explicit `eventId` along with tenant context.
- Updated session agenda item and registration detail repositories/handlers to load parent session/event details before mapping, preventing missing `EventId` in HAL/resource authorization contexts.
- Updated local fallback authorization so event-family resources require valid `tenantId` + `eventId` before inherited tenant/org authority is considered.
- Updated batch fallback to reject event resources whose resource `tenantId` differs from the resolved batch profile tenant.
- Added unit tests for missing event ID denial, valid event context allow, cross-tenant batch denial, and registration create behavior.

### Slice 3 — Authority Ceiling and Event Role Assignment Commands

- Added `EventRoleAuthorityCeilingService` and wired it in `Explore.Application/ApplicationServicesRegistration.cs`.
- Added assignable event-role preset DTO/query/handler so UI/API surfaces can ask for safe presets without exposing every event role blindly.
- Added assign, revoke, update-window, and ownership-transfer commands with shared handler base logic.
- Enforced command invariants from Oracle review: direct `EventOwner` revoke now hard-fails with `event_owner_transfer_required`; ownership transfer validates the replacement owner is effective at transfer time with `event_ownership_transfer_invalid`.
- Added `event_role_assignment.changed` business metric and fixed metric label cardinality by using enum role names or `unknown`, not raw IDs.
- Added focused tests for authority ceiling and command invariants.

### Slice 4 — ESOR-012 Local Fallback Event-Role Permission Evaluation

- `FallbackAuthorizationService` now consumes `IEventAuthoritySnapshotService` for event-scoped resource decisions.
- Single-check fallback now preserves admin/instance bypass precedence, then evaluates per-event assignment permissions using permission codes shaped as `{resourceKind}:{action}`.
- Batch fallback now resolves a shared authority snapshot per user/tenant/event-set slice for optimized batches and keeps small batches on the per-check path.
- Missing tenant/event context, tenant mismatch, and other-event assignments fail closed.
- Oracle found a blocker where `event_day:*` and `event_agenda_item:*` were fallback-supported resource kinds but missing from the seeded permission vocabulary/grants. Fixed by adding constants, seed rows, role grants, and regression tests.
- Added regression tests for event-day/event-agenda-item role permissions, optimized-batch one-shot snapshot resolution, and per-event isolation.

### Verification Completed

- `rtk dotnet build "Explore.Domain/Explore.Domain.csproj" --configuration Release --verbosity quiet` — passed.
- `rtk dotnet build "Explore.Application/Explore.Application.csproj" --configuration Release --verbosity quiet` — passed after both slices.
- `rtk dotnet build "Explore.Persistence/Explore.Persistence.csproj" --configuration Release --verbosity quiet` — passed after both slices.
- `rtk dotnet build "Explore.Infrastructure/Explore.Infrastructure.csproj" --configuration Release --verbosity quiet` — passed after fallback changes.
- `rtk dotnet build "Explore.API/Explore.API.csproj" --configuration Release --verbosity quiet` — passed after foundation slice.
- `rtk dotnet build "Event.Application.UnitTests/Event.Application.UnitTests.csproj" --configuration Release --verbosity quiet` — passed after event-child fixes.
- `rtk dotnet test --project "Event.Domain.UnitTests/Event.Domain.UnitTests.csproj" --configuration Release --verbosity quiet` — passed.
- `rtk dotnet test --project "Event.Application.UnitTests/Event.Application.UnitTests.csproj" --configuration Release --verbosity quiet` — passed after Oracle blocker fixes.
- `rtk dotnet test --project "Event.Architecture.Tests/Event.Architecture.Tests.csproj" --configuration Release --verbosity quiet` — passed.
- `rtk dotnet build "Event.API.IntegrationTests/Event.API.IntegrationTests.csproj" --configuration Release --verbosity quiet` — passed.
- `rtk dotnet build "Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj" --configuration Release --verbosity quiet` — passed.

Latest post-Oracle ESOR-012 verification also passed:

- `rtk dotnet build "Explore.Persistence/Explore.Persistence.csproj" --configuration Release --verbosity quiet` — passed with 0 errors.
- `rtk dotnet build "Event.Application.UnitTests/Event.Application.UnitTests.csproj" --configuration Release --verbosity quiet` — passed with 0 errors.
- `rtk dotnet test --project "Event.Application.UnitTests/Event.Application.UnitTests.csproj" --configuration Release --verbosity quiet` — passed.
- `rtk dotnet test --project "Event.Architecture.Tests/Event.Architecture.Tests.csproj" --configuration Release --verbosity quiet` — passed.

Known verification caveat: full solution build remains blocked by pre-existing unrelated issues, including `Explore.Persistence/Repositories/UserNotificationPreferenceRepository.cs` accessing `GenericRepository<UserNotificationPreference, Guid>._dbContext` plus existing analyzer/package warnings. Targeted builds/tests for this feature pass.

### Oracle Reviews Completed

- Foundation review found no blockers after verifying the PostgreSQL partial unique index filter matches the lowercase `status` migration column.
- Event-child fallback review found two blockers that were fixed:
  1. Batch fallback originally allowed cross-tenant event resources for tenant admins because it trusted the current tenant admin profile without checking resource `tenantId`; fixed by requiring event context tenant to match `AuthorityProfile.TenantId`.
  2. New `EventId` DTO mapping was unreliable on detail/by-session paths because repositories did not include `EventSession`; fixed with `GetByIdWithDetails(...)` repository methods and parent includes.
- Local fallback event-role review found one blocker that was fixed:
  1. `event_day:*` and `event_agenda_item:*` permissions were supported by fallback resource-kind evaluation but absent from permission constants/seeds/role grants; fixed in `PermissionCodes`, `LookupTableSeeder`, and fallback regression tests.

## CTO Feedback Deltas Incorporated

- `RoleScopeEnum.Event` is only classification; `EventRoleAssignment(TenantId, EventId, UserId, RoleId)` is the real event-instance grant.
- Do not create `OrganizerScope`, `Workspace`, `BusinessScope`, `SubTenant`, or generic `ResourceRoleAssignment(ScopeType, ScopeId, UserId, RoleId)`.
- `EventRoleAssignment` must use explicit lifecycle: `Pending`, `Active`, `Revoked`, `Expired`.
- Required assignment fields: `Id`, `TenantId`, `EventId`, `UserId`, `RoleId`, `Status`, `StartsAtUtc`, `ExpiresAtUtc`, `RevokedAtUtc`, `RevokedByUserId`, `CreatedAtUtc`, `CreatedByUserId`, `UpdatedAtUtc`, `UpdatedByUserId`, `Version`.
- PostgreSQL concurrency strategy is app-managed numeric `Version` for v1; increment on lifecycle-changing commands and configure as an EF concurrency token. Do not model this as SQL Server-style `byte[] RowVersion`; PostgreSQL `xmin` is an explicit provider-specific alternative only if deliberately selected.
- Effective authorization must not wait for a cleanup job to materialize `Expired`: use `Status == Active && StartsAtUtc <= now && (ExpiresAtUtc IS NULL || ExpiresAtUtc > now)`.
- Revoked and expired rows are evidence/history; normal flows must not soft-delete assignment lifecycle rows. Any future admin archive/cleanup must not become the authorization boundary.
- V1 uniqueness is a PostgreSQL partial unique index on `(tenant_id, event_id, user_id, role_id) WHERE status IN ('Pending','Active')`; future multiple scheduled assignments would require revisiting with date-range exclusion constraints.
- Every manageable event must have at least one effective owner or inherited managing authority.
- Last direct `EventOwner` assignment cannot be revoked unless another owner exists or ownership transfers atomically.
- Ownership is assignment-only in v1: event creation creates an initial `EventRoleAssignment(EventOwner)` for the creator in the same transaction; no authoritative `Event.OwnerUserId` is added.
- Inherited managing authority is tightly bounded: tenant admins may manage; organization admins may manage events owned by their organization; group admins may manage owned events only where existing group authority applies; Instance Admins have no normal flow; Event Managers do not provide orphan recovery without an explicit future `system_recovery` path.
- Ownership transfer must be a first-class command.
- Instance Admins must not casually manage event teams through normal flows; emergency access must be separate, explicit, and audited if added later.
- Add `IEventAuthoritySnapshotService.GetForUserAndEventsAsync(Guid tenantId, Guid userId, IReadOnlyCollection<Guid> eventIds, CancellationToken cancellationToken)` to prevent HAL/query N+1 storms.
- Shared allow predicate: principal tenant matches resource tenant, resource has `eventId`, assignment tenant/event match resource tenant/event, assignment is active/effective at current time, role has permission, and event status permits action.
- Local fallback must not be weaker than Cerbos; Cerbos/local parity is release-blocking.
- Authority ceiling must be deterministic: assignable permissions are assigner same-event effective permissions minus forbidden delegation and system-reserved permissions.
- Non-delegable permissions include `event:manage-owner`, `event:transfer-ownership`, `event:delete`, and `event:manage-finance`.
- Authority ceiling/audit must distinguish authority source: `event_assignment`, `organization_admin`, `tenant_admin`, or `system_recovery`.
- Split role templates from assignable presets; add `GetAssignableEventRolePresets(eventId, assignerUserId)`.
- Event-child resources must carry `TenantId` and `EventId`; missing `eventId` must deny.
- Event-child propagation moves before fallback and Cerbos implementation.
- Cerbos principal payloads must be scoped to only assignments relevant to the checked resource event or explicit event batch; never send all event roles.
- Add stale HAL tests: after revoke/expiry, a refreshed event detail omits the link and direct protected API calls return `403`.
- `IEventAuthoritySnapshotService` interface/DTOs live in Application; implementations live in Persistence/Infrastructure by convention and consumers depend only on the Application contract.
- Add audit/observability for assign/update/revoke/expire/transfer and denies.
- First implementation slice seeds only four roles: `EventOwner`, `EventManager`, `RegistrationManager`, `CheckInStaff`.
- Priority order is truth table, event scope, assignment lifecycle/invariants, persistence/indexes, seed four roles, event-child propagation, snapshot service, authority ceiling, fallback, Cerbos, parity tests, API/HAL, Blazor UI last.

## Verified Current Repo Evidence

### Role and Permission Domain

- `Explore.Domain/Role.cs`
- `Explore.Domain/Permission.cs`
- `Explore.Domain/RolePermission.cs`
- `Explore.Domain/PlatformUserRole.cs`
- `Explore.Domain/TenantMember.cs`
- `Explore.Domain/OrganizationMember.cs`
- `Explore.Domain/GroupMember.cs`
- `Explore.Domain/Enums/RoleScopeEnum.cs`
- `Explore.Domain/Enums/RoleEnum.cs`
- `Explore.Domain/Constants/PermissionCodes.cs`

Current scopes after this session: `Platform`, `Tenant`, `Organization`, `Group`, `Event`.

Current assignment containers:

- platform user role
- tenant member role
- organization member role
- group member role

`EventRoleAssignment` now exists and is tenant-scoped, event-scoped, lifecycle-aware, and intentionally not soft-deletable.

### Application and Authorization

- `Explore.Application/Authorization/AuthorizationScope.cs`
- `Explore.Application/Authorization/ISecureRequest.cs`
- `Explore.Application/Authorization/IAuthorizedRequest.cs`
- `Explore.Application/Authorization/ResourceDescriptors.cs`
- `Explore.Application/Authorization/ResourceDescriptorRegistry.cs`
- `Explore.Application/Authorization/PermissionRegistryService.cs`
- `Explore.Application/Authorization/CapabilityCeilingService.cs`
- `Explore.Application/Behaviors/AuthorizationBehavior.cs`
- `Explore.Application/Services/EventActorResolver.cs`
- `Explore.Application/Features/Roles/Requests/Commands/CreateCustomRoleCommand.cs`
- `Explore.Application/Features/Roles/Requests/Commands/UpdateRolePermissionsCommand.cs`
- `Explore.Application/Features/Permissions/Requests/Queries/GetAssignablePermissionsRequest.cs`
- `Explore.Application/Features/Permissions/Requests/Queries/GetRolePermissionsRequest.cs`
- `Explore.Application/Features/OrganizationMembers/Requests/Commands/UpdateOrganizationMemberRoleCommand.cs`
- `Explore.Application/Features/TenantMembers/Requests/Commands/UpdateTenantMemberCommand.cs`
- `Explore.Application/Features/GroupMembers/Requests/Commands/UpdateGroupMemberRoleCommand.cs`

Foundation and Application command gaps are closed for the current slice: authorization now has a persisted `EventId`-specific assignment model, an Application snapshot contract, deterministic authority ceiling, assignment/revoke/update/transfer commands, and local fallback event-role permission evaluation. Remaining work is Cerbos payload/policy parity, API/HAL surface, and UI.

### Persistence

- `Explore.Persistence/Configurations/Entities/RoleConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/RolePermissionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/PlatformUserRoleConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/OrganizationMemberConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/TenantMemberConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/GroupMemberConfiguration.cs`
- `Explore.Persistence/Repositories/RoleRepository.cs`
- `Explore.Persistence/Repositories/PermissionRepository.cs`
- `Explore.Persistence/Repositories/PlatformUserRoleRepository.cs`
- `Explore.Persistence/Repositories/OrganizationMemberRepository.cs`
- `Explore.Persistence/Repositories/GroupMemberRepository.cs`
- `Explore.Persistence/Repositories/TenantAdministratorRepository.cs`
- `Explore.Persistence/Seed/LookupTableSeeder.cs`

Migrations currently include role/membership structures in:

- `Explore.Persistence/Migrations/20260418184237_init.cs`
- `Explore.Persistence/Migrations/20260419064321_init2.cs`
- `Explore.Persistence/Migrations/20260424230410_init3.cs`
- `Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs`

### API and HAL

- `Explore.API/Controllers/RoleController.cs`
- `Explore.API/Controllers/OrganizationMemberController.cs`
- `Explore.API/Controllers/TenantMemberController.cs`
- `Explore.API/Controllers/GroupMemberController.cs`
- `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`
- `Explore.API/Hateoas/ResourceAssemblerBase.cs`
- `Explore.API/Hateoas/LinkDefinitionPermissionExtensions.cs`
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
- `Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs`
- `Explore.API/Hateoas/Policies/EventDayLinkPolicy.cs`
- `Explore.API/Hateoas/Policies/EventAgendaItemLinkPolicy.cs`
- `Explore.API/Hateoas/Policies/EventSessionAgendaItemLinkPolicy.cs`
- `Explore.API/Hateoas/Policies/EventRegistrationLinkPolicy.cs`

No `PermissionController` was found during exploration.

### Cerbos and Fallback

- `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs`
- `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`
- `Explore.Infrastructure/Services/FallbackAuthorizationService.Evaluators.cs`
- `Explore.Infrastructure/Services/CerbosAuthorizationService.cs`
- `Explore.Infrastructure/Services/CerbosPrincipalBuilder.cs`
- `Explore.Infrastructure/Services/PolicySyncService.cs`
- `cerbos/policies/derived_roles.yaml`
- `cerbos/policies/event.yaml`
- `cerbos/policies/event_session.yaml`
- `cerbos/policies/event_day.yaml`
- `cerbos/policies/event_agenda_item.yaml`
- `cerbos/policies/event_session_agenda_item.yaml`
- `cerbos/policies/event_registration.yaml`
- `cerbos/policies/event_contact_share_consent.yaml`
- `cerbos/policies/_schemas/principal.json`
- `cerbos/policies/_schemas/event.json`
- `cerbos/policies/_schemas/event_session.json`
- `cerbos/policies/_schemas/organization_member.json`
- `cerbos/config/.cerbos.yaml`
- `cerbos/tests/event_test.yaml`

Current Cerbos principal attributes include instance, tenant, and organization membership information. There is still no Cerbos event-role assignment payload. Local fallback now has final event-role assignment authority for the supported event-family resources, so the next Cerbos work must mirror this exact tenant/event/permission contract. The repository intentionally supports both Cerbos and local fallback, so parity is not optional.

### Blazor HAL Consumption

- `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`
- `Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs`

Blazor should remain a HAL consumer. It must not decide access by role or claim inspection.

## External Research Summary

Cerbos official guidance supports the proposed model:

- Keep policies static and put request-specific facts in principal/resource attributes.
- Use resource ID keyed lookups for resource-instance roles.
- Use derived roles for contextual roles like owner, registration manager, or check-in staff.
- Validate request payloads with schemas.
- Policy tests should cover allowed and denied paths.
- Resource policies should remain resource-oriented and matrix-tested.

References:

- `https://docs.cerbos.dev/cerbos/latest/policies/best_practices.html`
- `https://docs.cerbos.dev/cerbos/latest/policies/derived_roles.html`
- `https://docs.cerbos.dev/cerbos/latest/policies/schemas.html`
- `https://docs.cerbos.dev/cerbos/latest/tutorial/04_testing-policies.html`
- `https://docs.cerbos.dev/cerbos/latest/policies/compile.html`

EF Core, Npgsql, and observability research supports:

- App-managed concurrency tokens for concurrent team edits and last-owner revocation protection; v1 uses numeric `Version` instead of SQL Server-style rowversion assumptions.
- PostgreSQL `xmin` is available as a deliberate provider-specific alternative, but is not the default v1 choice.
- PostgreSQL partial unique indexes via EF `HasFilter(...)` for one pending/active assignment per user/event/role.
- Composite indexes for fast authorization lookup and event-team listing.
- Central audit stamping through DbContext save hooks or interceptors.
- Structured logs plus metrics for lifecycle changes and authorization deny reasons.

References:

- `https://learn.microsoft.com/en-us/ef/core/saving/concurrency`
- `https://learn.microsoft.com/en-us/ef/core/modeling/indexes`
- `https://www.npgsql.org/efcore/modeling/concurrency.html`
- `https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors`
- `https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation`

## Oracle Review Summary

Oracle confirmed and hardened the plan:

- The truth table must be the source of truth for implementation.
- Last-owner protection and ownership transfer must be transaction-safe with explicit concurrency handling, not only pre-check queries.
- Lifecycle transitions must be explicit, including who/what moves assignments through `Pending -> Active -> Revoked/Expired`.
- Effective authorization must use the time-window predicate and not depend on materialized `Expired` state.
- V1 ownership should be assignment-only, with initial `EventOwner` assignment created transactionally during event creation.
- Inherited authority sources must be narrow and testable, and authority source must be explainable in audit/debug output.
- Authority ceiling should be computed once and reused by commands, HAL, Cerbos input generation, and fallback checks.
- `IEventAuthoritySnapshotService` should be batch-oriented and avoid HAL/query storms.
- The snapshot interface and DTOs belong in Application; implementation details stay in Persistence/Infrastructure.
- Cerbos and fallback tests should be generated or mirrored from the same decision matrix.
- Every event-child authorization resource must persist or resolve `TenantId` and `EventId`, and deny when `EventId` cannot be resolved.
- Event-child propagation must happen before fallback and Cerbos rules depend on event-scoped assignments.
- Cerbos payloads must include only assignments relevant to the checked tenant/event or explicit batch.
- Stale HAL behavior after revoke/expiry/permission change must be tested.
- Audit must include actor, subject, event, tenant, previous/new lifecycle state, authority source, decision engine, deny reason, and correlation ID.
- Metrics should avoid high-cardinality labels such as raw user IDs or event IDs.

## Quick Resume

Next implementation session should start with:

1. Read this context file plus `event-scoped-operational-roles-tasks.md` before coding.
2. Continue with Cerbos parity and API/HAL work, not the already-completed authority-command/fallback slices.
3. Use the existing `EventRoleAssignment` entity, `IEventRoleAssignmentRepository`, `IEventAuthoritySnapshotService`, `EventRoleAuthorityCeilingService`, and event role commands; do not create a generic resource-assignment abstraction.
4. Preserve the canonical effective predicate: `Status == Active && StartsAtUtc <= now && (ExpiresAtUtc IS NULL OR ExpiresAtUtc > now)`.
5. Preserve event-child fail-closed behavior: event-family fallback decisions need valid `tenantId` + `eventId`, and batch fallback must reject resource tenant mismatches.
6. Add Cerbos payload/schema/policy tests that mirror the verified fallback matrix before shipping API/HAL affordances as complete.
7. Use the local fallback tests as the executable reference for Cerbos parity.
8. Keep all UI/HAL work role-agnostic; Blazor must consume links only.

## Verification Notes

Implementation has progressed through the foundation, event-child context, authority-command, and local fallback event-role permission slices. Targeted builds/tests pass after the latest Oracle-driven fixes. Remaining work is listed in the task checklist; no commit has been made.

## Handoff Notes

- Current active goal: continue event role management implementation with Cerbos parity, event-team API/HAL, stale-HAL tests, and Blazor HAL consumption after the verified local fallback slice.
- Do not redo completed foundation/command/fallback work unless tests fail: `RoleScopeEnum.Event`, `EventRoleAssignment`, EF config/migration, repository, snapshot service, seed roles/permissions, event-child fail-closed fallback, authority ceiling, event role assignment commands, and local fallback event-role permission checks are already in the working tree.
- Important modified files from this session include:
  - `Explore.Domain/EventRoleAssignment.cs`
  - `Explore.Domain/Enums/EventRoleAssignmentStatus.cs`
  - `Explore.Application/Contracts/Persistence/IEventRoleAssignmentRepository.cs`
  - `Explore.Application/Contracts/Services/IEventAuthoritySnapshotService.cs`
  - `Explore.Persistence/Configurations/Entities/EventRoleAssignmentConfiguration.cs`
  - `Explore.Persistence/Migrations/20260430162948_AddEventRoleAssignments.cs`
  - `Explore.Persistence/Services/EventAuthoritySnapshotService.cs`
  - `Explore.Application/Authorization/EventRoleAuthorityCeilingService.cs`
  - `Explore.Application/Features/EventRoleAssignments/**`
  - `Explore.Infrastructure/Services/FallbackAuthorizationService*.cs`
  - `Explore.Application/Authorization/ResourceDescriptors.cs`
  - `Event.Domain.UnitTests/Entities/EventRoleAssignmentTests.cs`
  - `Event.Application.UnitTests/Authorization/EventRoleAuthorityCeilingServiceTests.cs`
  - `Event.Application.UnitTests/Features/EventRoleAssignments/Commands/EventRoleAssignmentCommandHandlerTests.cs`
  - `Event.Application.UnitTests/Behaviors/FallbackAuthorizationServiceTests.cs`
- The working tree contains many unrelated pre-existing modifications from other active workstreams. Use path-scoped diffs before summarizing or committing.

## Session Handoff — 2026-05-03 Europe/Brussels

No implementation work was performed for this active task during the sidebar dock refactor handoff session. Existing context, plan, and task files remain the authoritative state for this workstream. Do not infer progress or blockers here from the sidebar/dock-specific changes unless a future session explicitly broadens scope.
