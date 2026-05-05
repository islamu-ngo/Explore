<!-- ABOUTME: Task checklist for event-scoped operational roles implementation. -->
<!-- ABOUTME: Breaks the plan into CTO-prioritized phases with acceptance criteria, dependencies, effort, and relevant skills. -->

# Event-Scoped Operational Roles Tasks

Last Updated: 2026-05-01 Europe/Brussels — implementation handoff after verified authority-command + local fallback slices

## Current Implementation Status

- ✅ Foundation slice complete: ESOR-001 through ESOR-008 are implemented in code and verified with targeted builds/tests.
- ✅ Initial Application snapshot contract/service complete: `IEventAuthoritySnapshotService` exists in Application and has a Persistence implementation; future work may extend it with authority source/deny metadata.
- ✅ Event-child propagation/fail-closed fallback groundwork complete for core event-family resources: event, event session, event day, event agenda item, session agenda item, and event registration now require tenant/event context in local fallback.
- ✅ Authority ceiling and event role assignment command slice complete: assignable presets, assign/revoke/update-window/ownership-transfer commands, owner invariant guards, metrics, and focused tests are implemented.
- ✅ ESOR-012 local fallback event-role permission evaluation complete for supported event-family resources, including optimized batch snapshot resolution and per-event isolation tests.
- ✅ Oracle review completed for foundation, event-child, command/authority, and local fallback slices; blockers were fixed.
- ⚠️ ESOR-017 still has follow-up audit work for resource families not touched in this slice, especially contact-share consent, payment records, content review, check-in records, speaker coordination, and moderation objects.
- ⏭️ Next implementation priority: ESOR-013 through ESOR-016 Cerbos event-assignment payload/schema/policy/tests, then ESOR-018 parity, ESOR-019/020 API/HAL, stale-HAL tests, and Blazor UI last.

## Legend

- Priority: P0 critical, P1 high, P2 medium.
- Effort: S small, M medium, L large, XL extra large.
- Skills: `auth-patterns`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `error-tracking`, `blazor-ui-conventions`.

## Phase 0 — Truth Table and Parity Contract

### ✅ ESOR-000 — Define event-role authorization truth table

- Priority: P0
- Effort: M
- Dependencies: none
- Relevant skills: `auth-patterns`, `clean-architecture-rules`
- Work:
  - Define the shared allow predicate: tenant match, resource `eventId`, assignment tenant/event match, effective lifecycle state, role permission match, and event status permission.
  - Define effective lifecycle state as `Status == Active && StartsAtUtc <= now && (ExpiresAtUtc IS NULL || ExpiresAtUtc > now)` so authorization never waits for a background job to materialize `Expired`.
  - Define deny reason categories: `tenant_mismatch`, `missing_event_id`, `event_mismatch`, `assignment_not_found`, `assignment_not_effective`, `permission_missing`, `event_status_blocked`, `authority_ceiling_exceeded`, `last_owner_violation`, `instance_admin_normal_flow_denied`.
  - Define matrix rows for `EventOwner`, `EventManager`, `RegistrationManager`, and `CheckInStaff` only.
  - Define parity fixtures for local fallback and Cerbos from the same matrix.
- Acceptance criteria:
  - The matrix is documented before implementation.
  - Missing `eventId` is explicitly deny.
  - Local fallback and Cerbos tests can be generated or manually mirrored from the same matrix.
  - First-release role list is limited to four roles.

## Phase 1 — Domain and Seed Vocabulary

### ✅ ESOR-001 — Add Event role scope

- Priority: P0
- Effort: S
- Dependencies: ESOR-000
- Relevant skills: `clean-architecture-rules`
- Work:
  - Add `Event` to `Explore.Domain/Enums/RoleScopeEnum.cs`.
  - Review scope ordering and seed ID conventions before assigning numeric values.
- Acceptance criteria:
  - `RoleScopeEnum.Event` exists.
  - Existing platform/tenant/organization/group scopes continue to compile.
  - No persistence or API layer dependency is added to Domain.
  - Documentation and comments make clear that the enum is classification only, not the assignment mechanism.

- Status:
  - Implemented in `Explore.Domain/Enums/RoleScopeEnum.cs` as `Event = 4`.

### ✅ ESOR-002 — Add first-release event operational role constants

- Priority: P1
- Effort: S
- Dependencies: ESOR-001
- Relevant skills: `clean-architecture-rules`
- Work:
  - Add event role constants only if enum-backed seeded roles remain the project convention.
  - First-release roles only: `EventOwner`, `EventManager`, `RegistrationManager`, `CheckInStaff`.
  - Document backlog roles separately: `SpeakerCoordinator`, `Volunteer`, `EventModerator`, `FinanceManager`, `ContentEditor`, `Reviewer`, `Approver`.
- Acceptance criteria:
  - Role names are clear and user-facing labels are simple.
  - Role constants do not imply workspace/organizer/subtenant semantics.
  - No backlog role is seeded or exposed in v1.
  - Each role maps to permissions rather than hardcoded branch logic.

- Status:
  - Implemented in `Explore.Domain/Enums/RoleEnum.cs` as `EventOwner = 41`, `EventManager = 42`, `RegistrationManager = 43`, `CheckInStaff = 44`.

### ✅ ESOR-003 — Add granular event permission codes

- Priority: P0
- Effort: M
- Dependencies: ESOR-001, ESOR-000
- Relevant skills: `clean-architecture-rules`, `auth-patterns`
- Work:
  - Extend `Explore.Domain/Constants/PermissionCodes.cs` with missing event operational permissions.
  - Reuse existing codes where they already exist.
  - Add non-delegable permission constants for owner/transfer/delete/finance capabilities as needed.
- Acceptance criteria:
  - Permission names are resource/action based.
  - No duplicate permission code is introduced.
  - Permissions are granular enough for v1 owner, manager, registration, and check-in flows.
  - Permission names support the authority ceiling algorithm.

- Status:
  - Implemented in `Explore.Domain/Constants/PermissionCodes.cs` with event team, owner, transfer, finance, registration, and check-in constants.

### ✅ ESOR-004 — Add EventRoleAssignment entity

- Priority: P0
- Effort: M
- Dependencies: ESOR-001
- Relevant skills: `clean-architecture-rules`
- Work:
  - Create `Explore.Domain/EventRoleAssignment.cs`.
  - Include `Id`, `TenantId`, `EventId`, `UserId`, `RoleId`, `Status`, `StartsAtUtc`, `ExpiresAtUtc`, `RevokedAtUtc`, `RevokedByUserId`, `CreatedAtUtc`, `CreatedByUserId`, `UpdatedAtUtc`, `UpdatedByUserId`, and `Version`.
  - Model `Version` as an app-managed numeric concurrency token (`long` preferred by CTO feedback) incremented on lifecycle-changing commands.
  - Model lifecycle states as `Pending`, `Active`, `Revoked`, and `Expired`.
  - Add domain methods or invariants for lifecycle transitions without infrastructure dependencies.
- Acceptance criteria:
  - Assignment binds one user to one role for one event.
  - Entity is tenant-scoped.
  - Entity has no EF Core, MediatR, ASP.NET Core, or infrastructure dependency.
  - Domain invariants prevent empty IDs, invalid status transitions, and invalid validity windows.
  - Revoked assignments cannot grant authorization.
  - Rows whose `ExpiresAtUtc` is in the past cannot grant authorization even before `Status` is materialized to `Expired`.
  - Entity does not pretend PostgreSQL has SQL Server-style `byte[] rowversion`; provider-specific `xmin` is a deliberate alternative only if selected in Persistence and tests.

- Status:
  - Implemented in `Explore.Domain/EventRoleAssignment.cs` and `Explore.Domain/Enums/EventRoleAssignmentStatus.cs`.
  - Domain unit tests added in `Event.Domain.UnitTests/Entities/EventRoleAssignmentTests.cs`.

### ✅ ESOR-004A — Define owner invariants and ownership transfer behavior

- Priority: P0
- Effort: M
- Dependencies: ESOR-004
- Relevant skills: `clean-architecture-rules`, `auth-patterns`
- Work:
  - Define the invariant that every manageable event has at least one effective owner or inherited managing authority.
  - Define the last-direct-`EventOwner` revocation rule.
  - Define ownership transfer as a first-class command, not a generic assignment update.
  - Use assignment-only ownership for v1: event creation creates the creator's `EventRoleAssignment(EventOwner)` in the same transaction.
  - Define inherited managing authority tightly: tenant administrators yes; organization administrators only for events owned by that organization; group administrators only for events owned by that group when existing group authority applies; Instance Admins no normal flow; Event Managers no orphan recovery unless a later explicit `system_recovery` path is added.
- Acceptance criteria:
  - Last owner cannot be revoked unless another owner exists or ownership transfer occurs atomically.
  - Pending owner assignment does not count as effective owner unless explicitly documented.
  - Concurrency/race handling is specified for simultaneous revoke/transfer commands.
  - No authoritative `Event.OwnerUserId` is introduced in v1; any display/cache field is explicitly non-authoritative.

- Status:
  - Assignment-only ownership decision is documented and the `Event` entity still has no owner field.
  - Command-level direct owner revoke and transfer enforcement is implemented with ESOR-010.
  - Oracle hardening applied: direct `EventOwner` revocation hard-fails with `event_owner_transfer_required`, and ownership transfer validates the replacement owner is effective at transfer time with `event_ownership_transfer_invalid`.

## Phase 2 — Persistence

### ✅ ESOR-005 — Configure EventRoleAssignment persistence

- Priority: P0
- Effort: M
- Dependencies: ESOR-004
- Relevant skills: `dotnet-efcore-guidelines`
- Work:
  - Add `DbSet<EventRoleAssignment>` in `Explore.Persistence/ExploreDbContext.DbSets.cs`.
  - Add `Explore.Persistence/Configurations/Entities/EventRoleAssignmentConfiguration.cs`.
  - Add named tenant query filter and keep it active for runtime paths.
  - Do not use normal soft delete for lifecycle; revoked/expired rows are retained as evidence and history.
  - Configure `Version` as an app-managed EF concurrency token with `.IsConcurrencyToken()` and increment on lifecycle transitions/authority-changing updates.
  - Document PostgreSQL `xmin` as an explicit provider-specific alternative, not the v1 default.
- Acceptance criteria:
  - Tenant query filter remains active for runtime paths.
  - Relationships to `Event`, `User`, and `Role` are configured without cascade surprises.
  - `Version` participates in update/delete concurrency checks.
  - Read paths can use `AsNoTracking()` safely.
  - Authorization never depends on soft delete alone.

- Status:
  - Implemented in `Explore.Persistence/Configurations/Entities/EventRoleAssignmentConfiguration.cs`, `Explore.Persistence/ExploreDbContext.DbSets.cs`, and `Explore.Persistence/ExploreDbContext.QueryFilters.cs`.
  - Entity uses tenant-only filter, no soft-delete filter, and `Version.IsConcurrencyToken()`.

### ✅ ESOR-006 — Add migration, indexes, and constraints

- Priority: P0
- Effort: M
- Dependencies: ESOR-005
- Relevant skills: `dotnet-efcore-guidelines`
- Work:
  - Add focused EF Core migration for event role assignments and seed updates.
  - Add indexes:
    - PostgreSQL partial unique index on `(TenantId, EventId, UserId, RoleId)` with filter `status IN ('Pending','Active')`.
    - `(TenantId, UserId, EventId, Status)` for authorization lookup.
    - `(TenantId, EventId, UserId, Status)` for event team listing.
  - Add validity-window constraint such as `ExpiresAtUtc IS NULL OR StartsAtUtc < ExpiresAtUtc`.
  - Record that future multiple scheduled assignments require revisiting this with a PostgreSQL date-range exclusion constraint; v1 allows one pending/active assignment per user/event/role.
- Acceptance criteria:
  - Migration is focused and PascalCase verb-noun named.
  - Existing applied migrations are not edited.
  - Other-tenant assignment lookup is impossible through filtered repositories.
  - Duplicate pending/active assignments are prevented by the partial unique index.
  - Terminal revoked/expired rows remain available for audit/history without blocking a future new assignment.

- Status:
  - Migration generated: `Explore.Persistence/Migrations/20260430162948_AddEventRoleAssignments.cs`.
  - Oracle verified partial-index filter column casing against generated migration (`status IN (1, 2)`).

### ✅ ESOR-007 — Add repository contract and implementation

- Priority: P0
- Effort: M
- Dependencies: ESOR-005
- Relevant skills: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`
- Work:
  - Add `IEventRoleAssignmentRepository` in Application contracts.
  - Add Persistence implementation.
  - Support lookup by event/user, event team listing, and effective owner checks.
  - Support batch lookup for authority snapshots.
- Acceptance criteria:
  - Repository returns entities, not DTOs.
  - Repository does not expose `IQueryable`.
  - Authorization lookup uses tenant-safe predicates and cancellation tokens.
  - Last-owner queries run inside the same transaction/concurrency boundary as revoke/transfer commands.

- Status:
  - Implemented `Explore.Application/Contracts/Persistence/IEventRoleAssignmentRepository.cs` and `Explore.Persistence/Repositories/EventRoleAssignmentRepository.cs`.
  - Last-owner query helper exists and is consumed by the ESOR-010 command slice.

## Phase 3 — Seed Roles and Permissions

### ✅ ESOR-008 — Seed first-release event roles, permissions, and mappings

- Priority: P0
- Effort: M
- Dependencies: ESOR-001, ESOR-002, ESOR-003
- Relevant skills: `dotnet-efcore-guidelines`, `auth-patterns`
- Work:
  - Update `Explore.Persistence/Seed/LookupTableSeeder.cs` or current seed location.
  - Seed only `EventOwner`, `EventManager`, `RegistrationManager`, and `CheckInStaff` with `RoleScopeEnum.Event`.
  - Seed event-scoped permissions required by those four roles.
  - Link roles to permission bundles through `RolePermission`.
- Acceptance criteria:
  - Event roles cannot include tenant, organization, group, platform, or instance administration permissions.
  - `EventManager` cannot receive owner-transfer, event-delete, or finance-management permission in v1.
  - Permission bundles match plain-language role expectations.
  - Seed IDs/codes remain deterministic.

- Status:
  - Implemented in `Explore.Persistence/Seed/LookupTableSeeder.cs`.
  - Includes v1 role seed, event permission scope correction for existing dev DB rows, and event role-permission mapping seed.
  - Oracle-driven follow-up added `event_day:*` and `event_agenda_item:*` permission constants, seed rows, and event-role grants so fallback-supported resource kinds have matching seeded permissions.

## Phase 4 — Event-Child Propagation Audit

### ✅/⚠️ ESOR-017 — Propagate TenantId and EventId across event-child resources

- Priority: P0
- Effort: L
- Dependencies: ESOR-000, ESOR-008
- Relevant skills: `auth-patterns`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`
- Work:
  - Audit event-child resource descriptors for sessions, event days, agenda items, session agenda items, registrations, payments, content review, check-in, speaker coordination, and moderation.
  - Ensure every authorization resource carries `TenantId` and `EventId`.
  - Centralize parent lookup when a child resource does not store `EventId` directly.
  - Complete this before fallback or Cerbos event-role rules are implemented.
- Acceptance criteria:
  - Missing `EventId` fails closed for all event-child resources.
  - Parent lookup is tenant-safe.
  - Tests cover unresolved parent denial.
  - Fallback and Cerbos consume the same tenant/event resource contract.

- Status:
  - Core event-family propagation implemented for event, event session, event day, event agenda item, event session agenda item, and event registration descriptors/fallback paths.
  - `EventSessionAgendaItem` and `EventRegistration` read paths now include parent `EventSession/Event` where needed so DTO `EventId` mapping is reliable.
  - Local fallback single and batch paths now fail closed on missing event context and tenant mismatch for event-family resources.
  - Unit tests added in `Event.Application.UnitTests/Behaviors/FallbackAuthorizationServiceTests.cs`.
  - Follow-up audit remains for contact-share consent, payments, content review, check-in, speaker coordination, and moderation objects.

### ESOR-017A — Complete remaining event-child propagation audit

- Priority: P0
- Effort: M
- Dependencies: ESOR-017
- Relevant skills: `auth-patterns`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`
- Work:
  - Audit `EventContactShareConsent` resource behavior and decide how optional source event IDs should participate in event-scoped authorization without breaking consent export semantics.
  - Audit payment records, content review objects, check-in records, speaker coordination objects, and moderation objects once their current DTO/resource descriptors are located.
  - Add tests for each event-child family that must fail closed on unresolved `EventId`.
- Acceptance criteria:
  - Every event-child resource family either carries tenant/event context or has a documented non-event-scoped exception.
  - Parent lookup is tenant-safe for every resource that lacks a direct `EventId` column.
  - Fallback and future Cerbos inputs use the same resource contract.

## Phase 5 — Application CQRS and Authority Services

### ⏭️ ESOR-009 — Add event team DTOs and queries

- Priority: P1
- Effort: M
- Dependencies: ESOR-007, ESOR-008, ESOR-017
- Relevant skills: `cqrs-mediatr-guidelines`, `auth-patterns`
- Work:
  - Add DTOs for event team members and assignment lifecycle details.
  - Add query to list event team.
  - Add query to inspect current user event permissions if needed by API/HAL.
- Acceptance criteria:
  - Queries return DTOs, not entities.
  - Validators are manually instantiated.
  - Cancellation tokens flow end to end.
  - Revoked/expired assignments appear only in history/admin views, not effective authority views.

### ✅ ESOR-009A — Add assignable event-role presets query

- Priority: P0
- Effort: M
- Dependencies: ESOR-008, ESOR-011, ESOR-011A
- Relevant skills: `cqrs-mediatr-guidelines`, `auth-patterns`
- Work:
  - Add `GetAssignableEventRolePresets(eventId, assignerUserId)`.
  - Return only roles the actor can assign after authority ceiling evaluation.
  - Include plain-language descriptions and capability summaries.
- Acceptance criteria:
  - Query does not expose every `RoleScopeEnum.Event` role blindly.
  - `EventManager` can see allowed presets such as `RegistrationManager` and `CheckInStaff` only when policy permits.
  - Non-assignable owner/finance/delete powers are not offered through normal presets.
  - Output is UI-friendly and contains no security internals.

- Status:
  - Implemented with `Explore.Application/DTOs/EventRoleAssignment/EventRolePresetDto.cs`, `GetAssignableEventRolePresetsRequest`, and `GetAssignableEventRolePresetsRequestHandler`.
  - Uses authority ceiling instead of exposing all event-scoped roles blindly.

### ✅ ESOR-010 — Add assign/revoke/update/transfer commands

- Priority: P0
- Effort: L
- Dependencies: ESOR-007, ESOR-008, ESOR-004A, ESOR-011A
- Relevant skills: `cqrs-mediatr-guidelines`, `auth-patterns`, `error-tracking`
- Work:
  - Add assign event role command.
  - Add revoke event role command.
  - Add update assignment lifecycle/window command if needed.
  - Add transfer event ownership command.
  - Invalidate relevant authorization/cache entries.
  - Emit audit/log/metric events for every transition and denied owner/ceiling attempt.
- Acceptance criteria:
  - Normal assignment management is limited to tenant/org/event authority, not Instance Admin normal flows.
  - A user cannot assign a role whose permissions exceed the assigner’s authority ceiling.
  - Last-owner and ownership transfer invariants are enforced transactionally with `Version` concurrency handling.
  - Commands return `BaseCommandResponse<Guid>` or bool according to repo convention.
  - Revoked/expired assignments stop granting authorization.

- Status:
  - Implemented assign, revoke, update-window, and ownership-transfer command requests/handlers under `Explore.Application/Features/EventRoleAssignments/`.
  - Shared command logic lives in `EventRoleAssignmentCommandHandlerBase.cs`.
  - Tests added in `Event.Application.UnitTests/Features/EventRoleAssignments/Commands/EventRoleAssignmentCommandHandlerTests.cs`.
  - `event_role_assignment.changed` metric added in `Explore.Application/Telemetry/BusinessMetrics.cs`; metric labels avoid raw user/event IDs.

### ✅/⏭️ ESOR-011 — Add batch event authority snapshot service

- Priority: P0
- Effort: L
- Dependencies: ESOR-007, ESOR-008, ESOR-017
- Relevant skills: `cqrs-mediatr-guidelines`, `auth-patterns`, `dotnet-efcore-guidelines`
- Work:
  - Add `IEventAuthoritySnapshotService.GetForUserAndEventsAsync(Guid tenantId, Guid userId, IReadOnlyCollection<Guid> eventIds, CancellationToken cancellationToken)` in the Application contract namespace (for example `Explore.Application.Contracts.Services`).
  - Return `EventAuthoritySnapshot` with `IReadOnlyDictionary<Guid, EventAuthorityForUser>`.
  - Normalize role codes, permission codes, `IsOwner`, and `IsManager`.
  - Implement in Persistence or Infrastructure according to repository conventions; all consumers depend only on the Application contract.
  - Internally compute effective lifecycle state, inherited managing authority, authority source, and deny reasons where needed.
- Acceptance criteria:
  - Service supports batch event checks without HAL N+1 query storms.
  - Output is deterministic and tenant-scoped.
  - Missing or unresolved events return no authority/fail-closed behavior.
  - Service is reusable by commands, HAL, fallback request construction, and Cerbos request construction.
  - Infrastructure/fallback does not depend on Persistence implementation details.

- Status:
  - Application contract and Persistence-backed implementation are in place.
  - Current output includes role codes, permission codes, `IsOwner`, and `IsManager`.
  - Future command/HAL/Cerbos work may need authority source and deny-reason metadata added without leaking Persistence details.

### ✅ ESOR-011A — Implement deterministic authority ceiling service/policy

- Priority: P0
- Effort: M
- Dependencies: ESOR-011
- Relevant skills: `auth-patterns`, `cqrs-mediatr-guidelines`
- Work:
  - Implement `Assignable permissions = assigner's effective permissions on this same event - forbidden-delegation permissions - system-reserved permissions`.
  - Implement `CanAssign(role) = role.permissions ⊆ assignablePermissions`.
  - Exclude `event:manage-owner`, `event:transfer-ownership`, `event:delete`, and `event:manage-finance` from normal delegation.
  - Track authority source for audit/debug output: `event_assignment`, `organization_admin`, `tenant_admin`, or `system_recovery`.
- Acceptance criteria:
  - Ceiling is reused by commands and `GetAssignableEventRolePresets`.
  - Ceiling is reflected in HAL action decisions.
  - Tests prove `EventManager` cannot assign `EventOwner` or finance/delete capabilities.
  - Direct assignment authority and inherited authority are distinguishable in audit/debug traces.

- Status:
  - Implemented in `Explore.Application/Authorization/EventRoleAuthorityCeilingService.cs` and registered in Application DI.
  - Reused by assignment commands and assignable preset query.
  - Focused tests added in `Event.Application.UnitTests/Authorization/EventRoleAuthorityCeilingServiceTests.cs`.
  - HAL-specific reuse remains pending in ESOR-020.

## Phase 6 — Local Fallback Authorization First

### ✅ ESOR-012 — Update fallback authorization parity

- Priority: P0
- Effort: L
- Dependencies: ESOR-000, ESOR-011, ESOR-011A, ESOR-017
- Relevant skills: `auth-patterns`, `dotnet-efcore-guidelines`
- Work:
  - Extend `Explore.Infrastructure/Services/FallbackAuthorizationService.cs` and evaluator partials to honor event role assignments.
  - Implement the shared matrix exactly.
  - Enforce tenant match, event match, lifecycle effectiveness, permission match, event status, and missing-event deny.
  - Use the canonical effective predicate: `Status == Active && StartsAtUtc <= now && (ExpiresAtUtc IS NULL || ExpiresAtUtc > now)`.
  - Deny normal Instance Admin event-team management flows.
- Acceptance criteria:
  - Fallback decisions match the matrix before Cerbos implementation begins.
  - Other-event and other-tenant assignments deny.
  - Pending/revoked/expired assignments deny.
  - Expired-by-time rows deny before any cleanup/materialization job runs.
  - Missing `eventId` denies.
  - Authorization parity tests cover the new behavior.

- Status:
  - Full local fallback event-role permission evaluation is implemented in `Explore.Infrastructure/Services/FallbackAuthorizationService*.cs` using `IEventAuthoritySnapshotService`.
  - Single-check fallback enforces tenant/event context and same-event permission matching via `{resourceKind}:{action}` permission codes.
  - Batch fallback resolves a shared authority snapshot for optimized event batches while preserving per-event isolation.
  - Oracle blockers fixed: cross-tenant batch mismatch denial and missing `event_day:*`/`event_agenda_item:*` permission constants/seeds/grants.
  - Regression tests added in `Event.Application.UnitTests/Behaviors/FallbackAuthorizationServiceTests.cs` for event-day/event-agenda-item allow, optimized one-shot snapshot resolution, and other-event deny.

## Phase 7 — Cerbos Policies Second

### ESOR-013 — Hydrate event assignments into authorization context

- Priority: P0
- Effort: L
- Dependencies: ESOR-011, ESOR-012, ESOR-017
- Relevant skills: `auth-patterns`, `clean-architecture-rules`
- Work:
  - Extend the authorization request-building path to load relevant event assignment data.
  - Hydrate only assignments relevant to the resource event being checked or the explicit batch of resource events.
  - Include assignment roles/permissions in principal or resource attrs with explicit event ID matching.
  - Explicitly forbid sending unrelated event assignments or all event roles in the Cerbos principal payload.
- Acceptance criteria:
  - Cerbos receives enough context to decide per-event roles.
  - Payload does not include unnecessary assignments for unrelated events.
  - Missing event ID fails closed.
  - Payload is size- and privacy-bounded by checked `tenantId` plus checked `eventId`/batch `eventIds`.

### ESOR-014 — Extend Cerbos schemas

- Priority: P0
- Effort: M
- Dependencies: ESOR-013
- Relevant skills: `auth-patterns`
- Work:
  - Update `cerbos/policies/_schemas/principal.json` with event assignment payload shape.
  - Ensure event-family resource schemas include required event/tenant attributes.
- Acceptance criteria:
  - Schema validation rejects malformed event assignment payloads.
  - Missing `eventId` or `tenantId` denies critical event actions.
  - Schema shape mirrors fallback input contract.

### ESOR-015 — Add event derived roles and resource rules

- Priority: P0
- Effort: L
- Dependencies: ESOR-014
- Relevant skills: `auth-patterns`
- Work:
  - Update `cerbos/policies/derived_roles.yaml` with first-release event operational derived roles.
  - Update event-family policies so assignments apply only when `resource.id` or `resource.attr.eventId` matches.
  - Encode the same lifecycle and deny behavior as fallback.
- Acceptance criteria:
  - `CheckInStaff` can perform check-in only for the assigned event.
  - `RegistrationManager` can manage registrations only for the assigned event.
  - `EventManager` permissions are same-event only and respect authority ceiling for team actions.
  - Deny wins for missing eventId, other tenant, other event, revoked, expired, pending, and instance-admin-normal-flow scenarios.

### ESOR-016 — Add Cerbos policy tests

- Priority: P0
- Effort: M
- Dependencies: ESOR-015
- Relevant skills: `auth-patterns`
- Work:
  - Extend `cerbos/tests/event_test.yaml` or add focused event-role policy tests.
  - Cover allow and deny scenarios for every first-release operational role.
- Acceptance criteria:
  - Same-event allow tests pass.
  - Other-event deny tests pass.
  - Other-tenant deny tests pass.
  - Missing-eventId deny tests pass.
  - Revoked/expired/pending deny tests pass.
  - Cerbos test matrix mirrors fallback matrix.

## Phase 8 — Parity Tests

### ESOR-018 — Prove Cerbos/local parity

- Priority: P0
- Effort: L
- Dependencies: ESOR-012, ESOR-016, ESOR-017
- Relevant skills: `auth-patterns`
- Work:
  - Update `Event.Architecture.Tests/AuthorizationParityTests.cs` for new resources, schemas, descriptors, and fallback switch cases.
  - Add unit/integration parity tests comparing fallback and Cerbos behavior for the shared matrix.
  - Include missing event ID, lifecycle states, last-owner attempts, ownership transfer, and authority ceiling.
- Acceptance criteria:
  - Cerbos/local drift is release-blocking.
  - Local fallback is not weaker than Cerbos.
  - Tests prove identical decisions for first-release event-role scenarios.

## Phase 9 — API and HAL

### ESOR-019 — Add event team API endpoints

- Priority: P1
- Effort: M
- Dependencies: ESOR-009, ESOR-010, ESOR-018
- Relevant skills: `auth-patterns`, `cqrs-mediatr-guidelines`
- Work:
  - Add endpoints under event resource, such as `/api/events/{eventId}/team`.
  - Add endpoint/query for assignable presets.
  - Use MediatR requests and resource authorization.
- Acceptance criteria:
  - GET endpoints follow project anonymous/auth rules as appropriate.
  - Writes require `[Authorize]` and handler/resource authorization.
  - Endpoints do not inspect roles directly when HAL/resource authorization should decide.
  - Instance Admin normal UI/API flow cannot manage event teams.

### ESOR-020 — Update event HAL policies

- Priority: P0
- Effort: M
- Dependencies: ESOR-011, ESOR-018, ESOR-019
- Relevant skills: `auth-patterns`
- Work:
  - Update event-family HAL policies to include event team/manage/check-in/registration links where relevant.
  - Use `IEventAuthoritySnapshotService` for batched event authority lookup.
  - Ensure resource attributes include `tenantId` and `eventId` consistently.
  - Add stale HAL revocation/expiry coverage.
- Acceptance criteria:
  - Links appear only when authorization permits the specific event action.
  - Blazor can remain role-agnostic.
  - Missing event assignment removes the link.
  - After a `CheckInStaff` role is revoked or expires, a refreshed event-detail response omits the check-in link and a direct protected API call returns `403`.
  - HAL paths do not create avoidable N+1 assignment lookups.

## Phase 10 — Blazor Event Team UX

### ESOR-021 — Add event team management UI

- Priority: P2
- Effort: M
- Dependencies: ESOR-019, ESOR-020
- Relevant skills: `blazor-ui-conventions`, `auth-patterns`
- Work:
  - Add event team UI under event details or event management.
  - Use simple role labels and descriptions from assignable presets.
  - Consume HAL links for affordances.
- Acceptance criteria:
  - Users assign roles through simple presets.
  - UI does not expose `RoleScopeEnum` or security internals.
  - UI does not inspect claims, JWT/Keycloak roles, or role names directly.
  - Instance Admin emergency access is not surfaced as normal UI.

## Phase 11 — Audit, Observability, and Verification

### ESOR-022 — Add audit events, structured logs, and metrics

- Priority: P0
- Effort: M
- Dependencies: ESOR-010, ESOR-012, ESOR-018
- Relevant skills: `error-tracking`, `auth-patterns`
- Work:
  - Emit audit events for assign, update, revoke, expire, ownership transfer, authority-ceiling deny, and last-owner deny.
  - Add structured log fields: `tenantId`, `eventId`, `targetUserId`, `roleId`, `actorUserId`, `correlationId`, previous status, new status, authority source, safe authority source ID where available, decision engine, resource kind, action, safe assignment/snapshot reference where available, and deny reason.
  - Add metric `event_role_assignment.changed`.
  - Add authorization deny metrics by reason category.
- Acceptance criteria:
  - No audit event swallows exceptions.
  - Logs include correlation ID.
  - Metrics avoid high-cardinality labels such as raw user IDs or event IDs.
  - Assignment lifecycle changes can be reconstructed from audit records.
  - Audit/debug traces explain whether authority came from `event_assignment`, `organization_admin`, `tenant_admin`, or `system_recovery`.

### ESOR-023 — Add and run tests

- Priority: P0
- Effort: L
- Dependencies: ESOR-001 through ESOR-022 as applicable
- Relevant skills: all loaded skills
- Work:
  - Add Domain tests for `EventRoleAssignment` invariants.
  - Add Application unit tests for queries/commands/snapshot service/authority ceiling.
  - Add Persistence integration tests for filters/index/concurrency behavior.
  - Add API integration tests for endpoint authorization and HAL links.
  - Add Cerbos policy tests.
  - Add stale HAL tests for revoke, expiry, ownership transfer, and role-permission change where applicable.
- Acceptance criteria:
  - Same-event grants work.
  - Other-event grants deny.
  - Other-tenant grants deny.
  - Pending/revoked/expired assignments deny.
  - Expired-by-time assignments deny even before materialized `Expired` status.
  - Last-owner invariant holds under concurrency.
  - Stale HAL scenario passes: link disappears on next fetch and direct API call returns `403`.
  - Cerbos and fallback parity is proven.

### ESOR-024 — Final build and architecture verification

- Priority: P0
- Effort: M
- Dependencies: ESOR-023
- Relevant skills: all loaded skills
- Work:
  - Run architecture tests, related unit/integration tests, Cerbos policy tests, and release build.
- Acceptance criteria:
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.CleanArchitectureTests` passes.
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AuthorizationParityTests` passes.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passes.
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passes.
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passes.
  - `dotnet build --configuration Release --verbosity quiet` passes.

## Explicit Non-Tasks

- Do not add `OrganizerScope`.
- Do not add `Workspace`.
- Do not add `BusinessScope`.
- Do not add `SubTenant`.
- Do not add generic `ResourceRoleAssignment(ScopeType, ScopeId, UserId, RoleId)`.
- Do not make event roles global JWT/Keycloak roles.
- Do not gate Blazor buttons by role name or claim inspection.
- Do not grant event permissions without matching `TenantId`, `EventId`, lifecycle, and permission.
- Do not expose normal Instance Admin event-team management UI or API flows.
- Do not blur global `RegistrationManager` semantics with event-level `RegistrationManager`.
- Do not seed backlog roles in the first implementation slice.
- Do not add finance/payment manager, reviewer/approver, speaker coordinator, volunteer, event moderator, emergency Instance Admin UI, custom event roles UI, or event-role inheritance in v1.
- Do not use normal soft delete as assignment lifecycle.
- Do not use SQL Server-style byte-array rowversion for PostgreSQL event-role assignment concurrency.

## Session Handoff — 2026-05-03 Europe/Brussels

- [x] No task-state changes were made for this workstream during the sidebar dock refactor handoff session.
- [ ] Reconfirm this workstream's current state from its existing context/plan before resuming implementation.
