<!-- ABOUTME: Task checklist for corrected Event Creation Composer program/session direction. -->
<!-- ABOUTME: Tracks completed backend/UI slices and remaining draft/program/session work. -->

# Event Creation Progressive Disclosure Tasks

Last Updated: 2026-05-07 00:05 CEST

## Current Handoff Status

The active model is:

```text
Event -> EventSessionGroup / Track / Devroom / Program section -> EventSession / Program item
```

`Event.ParentEventId` and child-event program modeling are rejected for talks/workshops/program items. The implementation has moved the product toward dedicated session composer pages and away from the giant Create/Edit Event shell composer.

## Phase 0 — Stop Wrong Direction ✅

- [x] Inventory `ParentEventId` / child-event code, migration, DTOs, handlers, routes, HAL links, tests, and docs.
- [x] Roll back program-only `ParentEventId` artifacts from active server source and generated contracts.
- [x] Remove active UX targets: old parent-event selector, old add-child-event action, child-event context/banner/program item.
- [x] Remove active API targets: parent candidates for program, child-event program summary, `add-subevent` link.
- [x] Confirm `EventSession` is not described as obsolete/legacy for talks/workshops.
- [x] Add automated rollback regression for no `Event.ParentEventId` / rejected route names.
- [x] Acceptance: active implementation uses `EventSession` and `EventSessionGroup`, not child events.

## Phase 1 — Add Program Group Model ✅

### Domain

- [x] Add `EventSessionGroup` entity.
- [x] Add `EventSessionGroupSession` explicit join entity.
- [x] Add/use `Event.Sessions`, `Event.SessionGroups`, and `Event.AgendaItems` navigations.
- [x] Never name `EventSession` collections `ChildEvents`.
- [x] Add group assignment collection navigation on `EventSession`.
- [x] Use `LocationRoom? Room`, not a new `Room` abstraction.
- [x] Include tenant, audit, soft-delete, and concurrency fields per project conventions.
- [x] Keep Domain free of EF/API/Blazor/MediatR references.

### Persistence

- [x] Add DbSets for `EventSessionGroup` and `EventSessionGroupSession`.
- [x] Add EF configurations.
- [x] Configure `(tenant_id, event_id, slug)` unique active index for groups.
- [x] Configure `(tenant_id, event_id, sort_order)` group sort index.
- [x] Configure membership and primary assignment indexes with active-row filters so soft-deleted joins do not block reassignment.
- [x] Enforce group/session/join consistency in Application handlers.
- [x] Configure delete behavior so groups do not delete sessions.
- [x] Generate focused migration and update model snapshot.
- [x] Add persistence tests for soft-deleted membership reuse and primary reassignment.

### Application

- [x] Add group DTOs and assignment DTOs.
- [x] Add `GetEventSessionGroupsQuery` / detail / sessions-by-group read handlers.
- [x] Add create/update/delete group commands and handlers.
- [x] Add assign/unassign session-group commands and handlers.
- [ ] Add reorder group/session command if needed by future UI.
- [x] Validate same tenant/event for group/session assignments.
- [ ] Add slug uniqueness validator coverage for group create/update if not already covered by persistence/DB behavior.

## Phase 2 — Application/API Program Contracts

### Event draft contracts

- [x] Add/finish `CreateEventDraftRequestDto`. Public API/OpenAPI/Blazor generated client now use scalar `CreateEventDraftRequestDto`; the backend maps it into the internal graph-shaped `CreateEventRequest` with empty session/day/room/agenda collections.
- [x] Remove Create Event seed-session bridge: backend create validation/handler now allow zero sessions, Create Event no longer renders `Initial session timing`, and authenticated API runtime coverage verifies an empty-program draft persists without session rows.
- [ ] Add/finish `UpdateEventDraftRequest`.
- [x] Remove client-controlled `EventStatusId` from draft creation path. `CreateEventDraftRequestDto` no longer exposes status; Application maps draft create to internal `EventStatusId = 1` server-side.
- [ ] Add idempotency key for draft create.
- [ ] Add concurrency handling for draft update.
- [x] Keep publish readiness separate from draft persistence in current bridge flow.

### Session contracts

- [x] Add `GetEventSessionCreateContextQuery`.
- [ ] Add `CreateEventSessionRequest` or `CreateEventSessionDraftRequest` that replaces direct generated DTO use in Blazor.
- [ ] Add `UpdateEventSessionRequest` tuned for dedicated composer.
- [x] Add `GetEventProgramSummaryQuery` with server-owned sections, groups, local-day item grouping, and readiness warnings.
- [ ] Add `EventSessionKind` lookup and seed values: Talk, Workshop, Panel, Lecture, Class, Activity, Keynote, LightningTalk, BOF, Demo, QAndA, Other.
- [x] Ensure session create context includes inherited event defaults.
- [x] Include available groups/tracks/devrooms in session create context.
- [ ] Include room/location/language/speaker/category/tag/template/custom-field options.
- [ ] Validate session time inside event window or map readiness warning if policy allows exceptions.
- [ ] Validate speaker/room conflicts where supported.

### API/HAL

- [x] Add/verify event HAL links: `program`, `sessions`, `add-session`, `session-groups`, `add-session-group`.
- [x] Add/verify session HAL links used by dedicated composer: `self`, `event`, `edit`, `delete`, `session-groups`/group assignment data.
- [x] Add group endpoints and assignment endpoints.
- [x] Add program summary endpoint (`GET /api/event/{id}/program-summary`) and `program-summary` HAL affordance.
- [x] Add session create context endpoint.
- [x] Ensure current Blazor flows use HAL links instead of role checks.

## Phase 3 — Create Event Action Bar ✅

- [x] Replace old child-event flow with Add session.
- [x] If event is unsaved, Add session creates a draft first.
- [x] If event is dirty after initial save, Add session should update the existing draft before navigating. Event Edit now reuses the event update path before routing to the dedicated session composer; true Create Event draft-update contracts remain a separate backend contract task.
- [x] Navigate to `/events/{eventId}/sessions/create` after successful draft create.
- [x] Use server session create context for inherited defaults.
- [x] Return to Event composer after session save/cancel.
- [ ] Refresh server-backed Program summary after returning.
- [x] Announce save/navigation state for assistive tech.

## Phase 4 — Dedicated Session Create/Edit Page

- [x] Add route `/events/{eventId}/sessions/create`.
- [x] Add route `/events/{eventId}/sessions/{sessionId}/edit`.
- [x] Page title defaults to `Add program item` / `Edit program item`; default form labels and validation copy use program-item vocabulary.
- [ ] Support contextual labels: talk/workshop/panel/activity/session.
- [x] Add fields for title, description, date/start/end.
- [ ] Add explicit timezone field/context. Create page now shows server timezone/date-window context via `SessionCreateContextBanner`; edit page and timezone-aware conversion remain pending.
- [x] Add location and room selection with stale room clearing.
- [x] Add program section/track/devroom picker.
- [ ] Add speaker picker/management affordance.
- [ ] Add language picker.
- [x] Add capacity field.
- [x] Add registration mode picker. Create uses server-owned default from session create context; create/edit pages can change the existing `RegistrationModeId` via lookup values.
- [ ] Add category/tag/custom-property/template fields.
- [ ] Add image support if contract-backed.
- [x] Save creates/updates `EventSession`.
- [x] Assign/unassign selected program section after session save/update.
- [x] Prevent duplicate session creation when group assignment fails after session creation.
- [x] No right sidebar session editor as primary create UX.

## Phase 5 — Program Summary UI

- [x] Replace final nested schedule composer target with lightweight Program Summary in Create Event.
- [x] Add server-backed `ProgramSection` / `SessionGroupSection` contract. UI component extraction remains future work.
- [x] Add server-backed `ProgramDayGroup` contract grouped by event-local day.
- [x] Add server-backed `ProgramItem` contract with time, location/room, capacity, registration, and readiness warning metadata.
- [ ] Add reusable `SessionGroupPicker` if dedicated composer group select grows beyond simple `MudSelect`.
- [x] Add Session action in Create/EventEdit shells routes to dedicated composer.
- [x] Add `SessionCreateContextBanner` from server context.
- [x] Saved empty event state shows `[Add session]` and `[Add section/track]`; Event Edit now opens a HAL-gated program sections dialog for list/create/edit/delete and one-section session assignment/unassignment using existing session-group APIs, including optional default location/room metadata.
- [x] Unsaved event state shows a save-draft-and-add-session flow.
- [x] Group sessions by program section and local day in the server-backed Program Summary contract and render the grouped Event Edit Program Summary view from that contract.
- [ ] Show time, room/location, speakers, language, capacity/registration, readiness warnings. Event Edit now renders server-backed time, room/location, capacity/registration, and readiness warning metadata; speakers and language remain contract-backed follow-up work.
- [x] Editing a program item navigates to dedicated session edit page.
- [ ] Include `EventAgendaItem` logistics in summary where useful.

## Phase 6 — Theme And Core UX Cleanup

- [x] Event name remains the only bare editorial input in Create Event.
- [x] Other event-shell fields are structured accessible controls.
- [x] Theme quick bar remains page-level.
- [x] Theme tray advanced-only cleanup reviewed: tray now contains precise styling controls and explanatory copy only.
- [x] Remove duplicated quick controls from tray if still present; presets/effect chips remain in the page-level quick bar.
- [x] More Options contains rare fields only.
- [x] Mobile order is closer to target: event basics, theme, options summary, Program summary, actions.

## Phase 7 — Lifecycle, Permissions, And Hardening

- [ ] Plan future session lifecycle states: Draft, Submitted, InReview, Accepted, Rejected, Published, Cancelled.
- [ ] Add/plan event-scoped permissions: `event.program.view`, `event.program.manage`, `event.session.create`, `event.session.update`, `event.session.delete`, `event.session.review`, `event.session.assign_speaker`, `event.session.assign_group`, `event.session.publish`, `event.session_group.create`, `event.session_group.update`, `event.session_group.delete`, `event_session_group.reorder`.
- [ ] Add readiness paths for `program.sessions[*]`, `program.groups[*]`, and `program.agenda[*]`.
- [x] Add audit-backed group/session assignment command paths.
- [ ] Add audit for reorder.
- [ ] Add ProblemDetails for program validation failures beyond current command response behavior.
- [x] Add architecture tests for rollback and HAL conventions touched by this work.
- [x] Add Application unit tests for session-group handlers.
- [x] Add Persistence integration tests for group assignment lifecycle.
- [x] Add API integration tests for session-group write HAL links/endpoints. HAL policy coverage protects collection-item delete and assign-session route/permission metadata; endpoint-level API tests cover anonymous create/update/delete/assign/unassign write route authentication and authenticated PostgreSQL-backed create/update/delete/assign/unassign persistence; Blazor covers the Event Edit permission gate, direct group-list section count behavior, program section create location/room DTO mapping, HAL-gated delete behavior, and one-section assign/unassign service mapping.
- [x] Add Blazor component tests for Add session flow and dedicated composers.
- [ ] Add accessibility verification for keyboard/focus/announcements in browser/E2E.

## Obsolete / Demoted Primary-Create Targets ✅

Do not rebuild these as the primary program path:

- [x] `ScheduleComposer` / `ScheduleTimelineComposer` as a giant nested session composer in Create/Edit Event shells.
- [x] Right-sidebar `SessionEditorPanel` for initial talk/workshop creation.
- [x] `SessionInlineEditor` / `EventSessionEditor` for talks/workshops.
- [x] `AddScheduleItemMenu` as primary way to add talks/workshops.
- [x] Old parent-event selector for program parts.
- [x] Old add-child-event action.
- [x] Old child-event context banner.
- [x] Old child-event program item.
- [x] Child-event Program summary.
- [x] `PopulateSchedulingOnRequest()` as long-term graph-shaped create mapping.

Keep `EventSession`; it is the central program item.

## Current Verification Checklist

- [x] `rtk dotnet build "Explore.Blazor.Client/Explore.Blazor.Client.csproj" --configuration Release --verbosity minimal`
- [x] `rtk dotnet build "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --verbosity minimal`
- [x] `rtk dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --no-build --verbosity minimal`
- [x] `rtk dotnet build "Event.API.IntegrationTests" --configuration Release --verbosity quiet`
- [x] `rtk dotnet test --project "Event.API.IntegrationTests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventSessionGroupHateoasTests/*" --minimum-expected-tests 1 --no-progress`
- [x] `rtk dotnet test --project "Event.Architecture.Tests/Event.Architecture.Tests.csproj" --configuration Release --no-build --verbosity minimal`
- [x] `rtk git diff --check -- dev/active/event-creation-progressive-disclosure/*.md` and touched HAL/test files
- [x] `rg -n "child-event Program summary|parent candidate lookup as a required program feature|child-event program interpretation" dev/active/event-creation-progressive-disclosure` — expected matches remain only in this checklist and historical plan rejection notes.
- [x] Confirm active Create/Edit Event shell files no longer reference `SessionEditorPanel`, `SessionEditorWorkflow`, `ScheduleTimelineComposer`, or `PopulateSchedulingOnRequest`.
