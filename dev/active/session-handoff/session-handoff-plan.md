<!-- ABOUTME: Strategic handoff plan for continuing corrected Event Program Management work. -->
<!-- ABOUTME: Captures verified current state, remaining phases, risks, and acceptance criteria. -->

# Session Handoff - Event Program Management Continuation Plan

Last Updated: 2026-05-05

## Executive Summary

This handoff plan preserves the current implementation state after correcting the event creation/program-management direction. The wrong child-event hierarchy model (`Event.ParentEventId`, parent candidates, child-event subevents/program endpoints) has been rolled back from server source and generated contracts. The codebase now follows the corrected model: `Event` is the event/program container, `EventSessionGroup` represents tracks/devrooms/stages/program sections, `EventSession` represents talks/workshops/panels/classes/activities, and `EventAgendaItem` represents logistics such as breaks, meals, prayer slots, and transitions.

The next session should continue from the verified read-model foundation into write-side Application/API contracts, authorization, tests, and Blazor create-flow integration. The work must preserve Clean Architecture boundaries, HAL-driven affordances, tenant isolation, and explicit EF Core join-entity modeling.

## Scope

### In Scope

- Continue Event Program Management after the corrected `EventSessionGroup` foundation.
- Add write commands and validation for session groups and session-group assignments.
- Add authorization resource kinds/actions and HAL write affordances.
- Integrate create-event composer flow with dedicated session/program-item creation.
- Add tests and documentation updates for the session-group/session model.

### Out of Scope

- Reintroducing `Event.ParentEventId` as the default program model.
- Modeling talks/workshops/lectures as child `Event` records.
- Building a giant inline nested session composer inside `CreateEvent.razor`.
- Implementing full speaker submission/review workflow before the basic session-group write path is stable.

## Verified Current State Analysis

The following paths/classes were verified by search/read before being listed here.

### Canonical Rules And References

- `CLAUDE.md` exists and states repository rules: repositories return entities, validators are manually instantiated, GET defaults `[AllowAnonymous]`, writes `[Authorize]`, and HAL links are the UI source of truth.
- `docs/ARCHITECTURE.md` exists and now states: `Event` is the event/program container; `EventSessionGroup` organizes tracks/devrooms/stages/program sections; `EventSession` is scheduled content; `EventAgendaItem` covers logistics.
- `docs/DOMAIN.md` exists and documents tenant/soft-delete/audit interfaces and domain-layer modeling conventions.
- `docs/SECURITY.md` exists and confirms Keycloak/BFF auth, Application authorization pipeline, Cerbos/fallback provider behavior, and fail-closed authorization.
- `docs/API.md` exists and confirms named routes, thin controllers, HAL response assembly, OpenAPI export, and no URL-segment API versioning.
- `dev/active/README.md` exists and defines the three-file dev-docs pattern used by this handoff.

### Implemented Domain Model

- `Explore.Domain/Event.cs` contains verified collections:
  - `Sessions`
  - `SessionGroups`
  - `AgendaItems`
- `Explore.Domain/EventSessionGroup.cs` exists and implements `ITenantEntity`, `IAuditableEntity`, `ISoftDeletable`, and `IConcurrencyAware`. It includes `EventId`, `Name`, `Slug`, optional `LocationId`, optional `RoomId` with `LocationRoom? Room`, `Color`, `SortOrder`, `IsPublished`, `TenantId`, audit fields, soft-delete fields, and `ConcurrencyStamp`.
- `Explore.Domain/EventSessionGroupSession.cs` exists and implements `ITenantEntity`, `IAuditableEntity`, and `ISoftDeletable`. It includes `EventSessionGroupId`, `EventSessionId`, `EventId`, `TenantId`, `IsPrimary`, `SortOrder`, audit fields, and soft-delete fields.
- `Explore.Domain/EventSession.cs` contains a verified `SessionGroups` collection of `EventSessionGroupSession` assignments.

### Implemented Persistence Model

- `Explore.Persistence/Configurations/Entities/EventSessionGroupConfiguration.cs` exists and maps event session groups with tenant/event slug uniqueness, sort index, optional location/room, and concurrency token.
- `Explore.Persistence/Configurations/Entities/EventSessionGroupSessionConfiguration.cs` exists and maps the explicit join entity with unique membership, one-primary-per-session-per-event filtered unique index, group sort index, tenant FK, and soft-delete default.
- `Explore.Persistence/ExploreDbContext.DbSets.cs` includes `EventSessionGroups` and `EventSessionGroupSessions`.
- `Explore.Persistence/ExploreDbContext.QueryFilters.cs` includes tenant + soft-delete filters for `EventSessionGroup` and `EventSessionGroupSession`.
- `Explore.Persistence/Migrations/20260505151339_AddEventSessionGroups.cs` exists and creates `event_session_groups` and `event_session_group_sessions`. Verified: no `ParentEventId`, `parent_event_id`, or `ChildEvents` remains in migrations.
- `Explore.Application/Contracts/Persistence/IEventSessionGroupRepository.cs` exists with `GetWithDetailsAsync(Guid id, CancellationToken)` and `GetByEventAsync(Guid eventId, CancellationToken)`.
- `Explore.Application/Contracts/Persistence/IEventSessionGroupSessionRepository.cs` exists with `GetByGroupAsync(Guid eventSessionGroupId, CancellationToken)` and `GetBySessionAsync(Guid eventSessionId, CancellationToken)`.

### Implemented Application/API Read Surface

- `Explore.Application/DTOs/EventSessionGroup/EventSessionGroupDto.cs` and `EventSessionGroupListDto.cs` exist.
- `Explore.Application/DTOs/EventSession/EventSessionGroupAssignmentDto.cs` exists and is exposed from session DTOs.
- Query request/handler files exist under `Explore.Application/Features/EventSessionGroups/` for:
  - get groups by event,
  - get group detail,
  - get sessions by group.
- `Explore.API/Controllers/EventSessionGroupController.cs` exists with public read endpoints:
  - `GET /api/eventsessiongroup/by-event/{eventId}`
  - `GET /api/eventsessiongroup/{id}`
  - `GET /api/eventsessiongroup/{id}/sessions`
- `Explore.API/Hateoas/RouteNames.cs` contains verified route names:
  - `GetEventSessionGroupsByEvent`
  - `GetEventSessionGroupById`
  - `GetEventSessionGroupSessions`
- `Explore.API/swagger.json` and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` contain the session-group operations and no rejected child-event endpoints.

### Verified Rollback State

Server source and generated contracts were checked for rejected child-event artifacts. No active matches remain for:

- `ParentEventId`
- `parent_event_id`
- `ParentEventTitle`
- `ChildEventCount`
- `GetEventParentCandidates`
- `GetEventSubevents`
- `GetEventProgram`
- `EventParentCandidateDto`
- `EventProgramDto`
- `LinkRelations.Program`

## Proposed Future State

The future state is a complete Event Program Management vertical slice:

1. Event creators save an event draft, then add program items through a dedicated session/program-item page.
2. Program sections (tracks/devrooms/stages/sections) are managed as `EventSessionGroup` records.
3. Session assignment to groups uses `EventSessionGroupSession`, preserving `IsPrimary`, `SortOrder`, tenant/event consistency, and soft-delete lifecycle.
4. Public clients can discover published groups and assigned sessions via HAL links.
5. Authorized users can create/update/delete/reorder groups and assign/unassign sessions through write endpoints gated by Application authorization and HAL affordances.
6. Blazor UI gates all program-management actions from HAL links and never locally inspects roles/claims.

## Implementation Phases

## Phase 1: Stabilize Current Foundation (S)

### Task 1.1: Confirm Generated Contract Consistency
- **Files**: `Explore.API/swagger.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- **Acceptance Criteria**:
  - [ ] OpenAPI includes all three session-group read endpoints.
  - [ ] Generated client includes matching `GetEventSessionGroup*Async` methods.
  - [ ] Rejected child-event endpoints remain absent.
- **Dependencies**: Current API build/export flow.
- **Related Skills**: `clean-architecture-rules`, `blazor-ui-conventions`
- **Effort**: S

### Task 1.2: Add Focused Regression Tests For Rollback
- **Files**: `Event.Architecture.Tests/`, `Event.Application.UnitTests/`, `Event.Persistence.IntegrationTests/`
- **Acceptance Criteria**:
  - [ ] Test verifies no `ParentEventId` property on `Event` or event DTO contracts.
  - [ ] Test verifies no rejected parent-candidate/subevents/program route names.
  - [ ] Persistence test verifies `EventSessionGroupSession` soft-delete filter hides deleted assignments.
- **Dependencies**: Phase 1.1
- **Related Skills**: `clean-architecture-rules`, `dotnet-efcore-guidelines`
- **Effort**: M

## Phase 2: Domain/Application Write Contracts (L)

### Task 2.1: Create Session Group Write DTOs
- **Files To Create**:
  - `Explore.Application/DTOs/EventSessionGroup/CreateEventSessionGroupRequest.cs`
  - `Explore.Application/DTOs/EventSessionGroup/UpdateEventSessionGroupRequest.cs`
  - `Explore.Application/DTOs/EventSessionGroup/AssignSessionToGroupRequest.cs`
- **Acceptance Criteria**:
  - [ ] Create/update contracts include `EventId`, `Name`, `Slug`, `Description`, `LocationId`, `RoomId`, `Color`, `SortOrder`, `IsPublished`, and concurrency where needed.
  - [ ] Assignment contract includes `EventSessionGroupId`, `EventSessionId`, `EventId`, `IsPrimary`, `SortOrder`.
  - [ ] Client never controls `TenantId`.
- **Dependencies**: Phase 1
- **Related Skills**: `cqrs-mediatr-guidelines`
- **Effort**: M

### Task 2.2: Add Validators With Event/Tenant Consistency Rules
- **Files To Create**:
  - `Explore.Application/DTOs/EventSessionGroup/Validators/CreateEventSessionGroupRequestValidator.cs`
  - `Explore.Application/DTOs/EventSessionGroup/Validators/UpdateEventSessionGroupRequestValidator.cs`
  - `Explore.Application/DTOs/EventSessionGroup/Validators/AssignSessionToGroupRequestValidator.cs`
- **Acceptance Criteria**:
  - [ ] Validator manually instantiated in handlers, not injected through DI.
  - [ ] Group/session assignment validator enforces `Group.EventId == Session.EventId == Request.EventId`.
  - [ ] Validator relies on repository methods; it does not use `ExploreDbContext` directly.
  - [ ] Room/location validation ensures selected `LocationRoom` belongs to the selected location/event context where applicable.
- **Dependencies**: Task 2.1
- **Related Skills**: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`
- **Effort**: M

### Task 2.3: Add Commands And Handlers
- **Files To Create**:
  - `Explore.Application/Features/EventSessionGroups/Requests/Commands/CreateEventSessionGroupCommand.cs`
  - `UpdateEventSessionGroupCommand.cs`
  - `DeleteEventSessionGroupCommand.cs`
  - `AssignSessionToGroupCommand.cs`
  - `UnassignSessionFromGroupCommand.cs`
  - corresponding handlers under `Handlers/Commands/`
- **Acceptance Criteria**:
  - [ ] Commands return `BaseCommandResponse<Guid>` for create/update/assignment and `bool` for delete/unassign where project pattern fits.
  - [ ] Commands invalidate related event/session/group cache keys if caching is introduced.
  - [ ] Handlers pass `CancellationToken` through every repository call.
  - [ ] Soft-delete is used for delete paths; no hard delete in runtime handlers.
- **Dependencies**: Task 2.2
- **Related Skills**: `cqrs-mediatr-guidelines`, `clean-architecture-rules`
- **Effort**: L

## Phase 3: Authorization And HAL Write Affordances (M)

### Task 3.1: Extend Authorization Catalog
- **Files To Verify/Update**:
  - `Explore.Application/Authorization/AuthorizationActions.cs`
  - `Explore.Application/Authorization/ResourceKinds.cs`
  - `Explore.Application/Authorization/ResourceDescriptors.cs`
  - Cerbos policy files under `cerbos/policies/`
- **Acceptance Criteria**:
  - [ ] Add actions for `event.session_group.create`, `event.session_group.update`, `event.session_group.delete`, `event.session_group.reorder`, `event.session.assign_group` if the current catalog supports these action names.
  - [ ] Add/update resource descriptors so authorization attributes can resolve group/session/event context.
  - [ ] Policies fail closed for writes.
  - [ ] Existing read endpoints remain public-only for published groups.
- **Dependencies**: Phase 2
- **Related Skills**: `auth-patterns`
- **Effort**: M

### Task 3.2: Add HAL Write Links
- **Files To Update**:
  - `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
  - `Explore.API/Hateoas/Policies/EventSessionGroupLinkPolicy.cs`
  - `Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs`
- **Acceptance Criteria**:
  - [ ] Event detail exposes `add-session-group` only when authorized.
  - [ ] Group detail exposes `edit`, `delete`, and `sessions` links where allowed.
  - [ ] Session detail exposes group assignment links only when authorized.
  - [ ] Clients can drive all UI actions from `_links` without local role checks.
- **Dependencies**: Task 3.1
- **Related Skills**: `auth-patterns`, `blazor-ui-conventions`
- **Effort**: M

## Phase 4: API Write Surface (M)

### Task 4.1: Add EventSessionGroupController Write Endpoints
- **File**: `Explore.API/Controllers/EventSessionGroupController.cs`
- **Acceptance Criteria**:
  - [ ] POST create endpoint is `[Authorize]`, named in `RouteNames`, and returns `CreatedAtRoute`.
  - [ ] PUT update endpoint handles concurrency if request includes a concurrency stamp.
  - [ ] DELETE endpoint soft-deletes group or returns validation error if policy disallows deleting non-empty groups.
  - [ ] Assignment endpoints validate group/session/event consistency through Application handlers.
  - [ ] Endpoints include explicit `[ProducesResponseType]` metadata.
- **Dependencies**: Phases 2 and 3
- **Related Skills**: `auth-patterns`, `cqrs-mediatr-guidelines`
- **Effort**: M

### Task 4.2: Regenerate OpenAPI And Blazor Client
- **Files**: `Explore.API/swagger.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- **Acceptance Criteria**:
  - [ ] OpenAPI includes write operations with stable operation IDs.
  - [ ] Generated client compiles.
  - [ ] No rejected child-event hierarchy endpoints reappear.
- **Dependencies**: Task 4.1
- **Related Skills**: `blazor-ui-conventions`
- **Effort**: S

## Phase 5: Blazor Program Management UI (XL)

### Task 5.1: Add Session Group Service Wrapper
- **Files To Verify/Create**:
  - `Explore.Blazor.Client/Services/`
  - generated `EventApiClient.g.cs`
- **Acceptance Criteria**:
  - [ ] Blazor components call service wrappers, not raw HTTP or direct API client code where project wrappers exist.
  - [ ] Service returns typed DTOs and preserves HAL links.
  - [ ] Errors map to user-visible ProblemDetails messaging.
- **Dependencies**: Phase 4
- **Related Skills**: `blazor-ui-conventions`, `auth-patterns`
- **Effort**: M

### Task 5.2: Replace Inline Program Editing With Dedicated Session Flow
- **Files To Update**:
  - `Explore.Blazor.Client/Pages/Events/CreateEvent.razor`
  - `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs`
  - existing session editor/create pages if present after verification
- **Acceptance Criteria**:
  - [ ] Event composer action bar includes Save draft, Add session, Review and publish.
  - [ ] Add session saves/updates event draft before navigating to dedicated session/program-item creation.
  - [ ] Program summary renders sessions/groups/agenda items as read-only/lightweight summary in the event composer.
  - [ ] No giant nested session form in the event create page.
- **Dependencies**: Phase 4
- **Related Skills**: `blazor-ui-conventions`, `design-system`
- **Effort**: XL

### Task 5.3: Build Program Section UI Components
- **Files To Create**:
  - `Explore.Blazor.Client/Components/Events/ProgramSection.razor`
  - `ProgramItem.razor`
  - `SessionGroupPicker.razor`
- **Acceptance Criteria**:
  - [ ] UI labels use product language: Track, Devroom, Stage, Program section, Session, Program item.
  - [ ] Internal name `EventSessionGroup` does not leak into user-facing labels.
  - [ ] Components use MudBlazor v9 APIs and BEM/CSS isolation.
  - [ ] All action affordances are gated by HAL links.
- **Dependencies**: Phase 5.1
- **Related Skills**: `blazor-ui-conventions`, `design-system`
- **Effort**: L

## Phase 6: Testing, Observability, And Documentation (L)

### Task 6.1: Add Unit And Integration Tests
- **Acceptance Criteria**:
  - [ ] Application unit tests cover validators and command handlers.
  - [ ] Persistence integration tests cover unique indexes, soft-delete filters, one-primary-group rule, and tenant isolation.
  - [ ] API integration tests cover read/write endpoints and HAL links.
  - [ ] Blazor tests cover Add session navigation and HAL-gated actions.
- **Dependencies**: Phases 2-5
- **Related Skills**: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `blazor-ui-conventions`
- **Effort**: L

### Task 6.2: Update Architecture And Active Docs
- **Files**:
  - `docs/ARCHITECTURE.md`
  - `docs/DOMAIN.md`
  - `docs/API.md`
  - `dev/active/event-creation-progressive-disclosure/*`
  - `dev/active/session-handoff/*`
- **Acceptance Criteria**:
  - [ ] Domain docs include `EventSessionGroup` and `EventSessionGroupSession` in core event model.
  - [ ] API docs list session-group read/write endpoints once implemented.
  - [ ] Active docs no longer refer to child `Event` records as program items.
- **Dependencies**: All prior phases
- **Related Skills**: `clean-architecture-rules`
- **Effort**: M

## Risk Assessment And Mitigation

| Risk | Impact | Likelihood | Mitigation |
|---|---:|---:|---|
| Group/session/event tenant mismatch in assignments | High | Medium | Enforce in Application validators and persistence tests; never trust client-supplied `TenantId`. |
| Public endpoint exposes unpublished groups/sessions | High | Medium | Keep public repository methods filtered by `IsPublished`; add separate authenticated management queries later. |
| HAL write links drift from Cerbos authorization | High | Medium | Add resource descriptors and authorization-backed link policies before exposing Blazor actions. |
| Generated OpenAPI/client reintroduces stale child-event artifacts | Medium | Low | Keep grep/OpenAPI assertions in verification checklist. |
| Inline create page becomes too complex | Medium | High | Keep dedicated session/program-item pages; event composer only shows read-only summary and high-level actions. |
| Full solution build noise hides real failures | Medium | Medium | Use canonical per-project test policy; document unrelated E2E fixture/analyzer issue separately. |

## Success Metrics

- Zero `ParentEventId`/child-event program artifacts in server source, migrations, OpenAPI, or generated client.
- `EventSessionGroup` and `EventSessionGroupSession` write paths pass unit/integration tests.
- Public program APIs only expose published groups and visible sessions.
- Blazor event composer can save draft and navigate to Add session without inline nested form.
- HAL links fully drive program-management actions.
- Builds/tests remain green for touched projects.

## Required Resources And Dependencies

- Clean Architecture/CQRS skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`.
- EF Core migration and query-filter discipline: `dotnet-efcore-guidelines`.
- Auth/HAL affordance discipline: `auth-patterns`.
- Blazor/MudBlazor UI discipline: `blazor-ui-conventions`, `design-system`.
- OpenAPI/NSwag generation flow through Aspire/API export and Blazor client build.
- Existing dirty repository state must be managed carefully; do not revert unrelated user changes.

## Effort Estimate

| Phase | Effort | Notes |
|---|---:|---|
| Phase 1: Stabilize current foundation | S-M | Mostly regression tests and contract checks. |
| Phase 2: Domain/Application write contracts | L | Validators and consistency rules are the core complexity. |
| Phase 3: Authorization/HAL | M | Depends on existing authorization catalogs and Cerbos policy shape. |
| Phase 4: API write surface | M | Thin controllers if Application layer is solid. |
| Phase 5: Blazor UI | XL | Create-flow navigation and program summary require careful UX/accessibility. |
| Phase 6: Tests/docs | L | Integration and API tests likely uncover edge cases. |

## Critique Your Own Plan!

The highest-risk area is the write-side assignment flow: `EventSessionGroupSession` carries redundant `EventId` and `TenantId` for query/index efficiency, so every command must prove `Group.EventId == Session.EventId == Request.EventId` and tenant consistency before writing. If this validation is missed or duplicated inconsistently, the database can contain cross-event assignments that look valid through one query path but break program summary and authorization behavior.
