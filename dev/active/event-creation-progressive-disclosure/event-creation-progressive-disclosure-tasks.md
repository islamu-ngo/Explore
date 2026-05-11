<!-- ABOUTME: Task checklist for corrected Event Creation Composer program/session direction. -->
<!-- ABOUTME: Tracks completed backend/UI slices and remaining draft/program/session work. -->

# Event Creation Progressive Disclosure Tasks

Last Updated: 2026-05-07 13:04 CEST

## Current Handoff Status

The active model is:

```text
Event -> EventSessionGroup / Track / Devroom / Program section -> EventSession / Program item
```

`Event.ParentEventId` and child-event program modeling are rejected for talks/workshops/program items. The implementation has moved the product toward dedicated session composer pages and away from the giant Create/Edit Event shell composer.

### Remaining-task execution plan

The remaining unchecked items are now split into three execution lanes so Phase 7 does not accidentally become a full session-review product rebuild:

1. **Implement now:** machine-addressable Program Summary readiness paths, out-of-window session warnings, narrow ProblemDetails hardening for existing program/session validation failures, and stable component-level accessibility coverage for the existing Add Session/session composer flow.
2. **Implement only when contract-backed:** language summary display, speaker/category/tag/template/custom-field/image fields, and agenda logistics rendering. These require existing or newly explicit contracts before UI work is checked off.
3. **Planned/deferred:** full session lifecycle workflow, future event-scoped permission names, reorder command/audit, speaker management, reusable picker extraction, and conflict detection beyond currently supported data. These are documented as future-compatible unless the corresponding endpoint/UI exists in this implementation pass.

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
- [x] Add slug uniqueness validator coverage for group create/update if not already covered by persistence/DB behavior. Create/update handlers now pre-check event-local slug uniqueness across all active groups, including unpublished groups, before persistence; unit coverage protects case-insensitive duplicate rejection.

## Phase 2 — Application/API Program Contracts

### Event draft contracts

- [x] Add/finish `CreateEventDraftRequestDto`. Public API/OpenAPI/Blazor generated client now use scalar `CreateEventDraftRequestDto`; the backend maps it into the internal graph-shaped `CreateEventRequest` with empty session/day/room/agenda collections.
- [x] Remove Create Event seed-session bridge: backend create validation/handler now allow zero sessions, Create Event no longer renders `Initial session timing`, and authenticated API runtime coverage verifies an empty-program draft persists without session rows.
- [x] Finish and consume `UpdateEventDraftRequestDto`. Public API/OpenAPI/Blazor generated client now use scalar `UpdateEventDraftRequestDto` for draft update; Blazor `EventService.UpdateEventAsync(Guid, UpdateEventDraftRequestDto)` forwards it directly and Event Edit no longer writes lifecycle/session projection fields through draft save.
- [x] Remove client-controlled `EventStatusId` from draft creation path. `CreateEventDraftRequestDto` no longer exposes status; Application maps draft create to internal `EventStatusId = 1` server-side.
- [x] Add idempotency key for draft create. Blazor `EventService.CreateEventAsync` accepts an optional key and the concrete generated `EventApiClient` injects `Idempotency-Key` on `POST /api/event`, defaulting to a generated key for caller convenience.
- [x] Add stronger optimistic-concurrency/stale-write UX for draft update. `UpdateEventDraftRequestDto.ExpectedConcurrencyStamp` is required, Application rejects stale stamps with `ConcurrencyConflictException`, API documents the update conflict response, and Blazor maps 409 ProblemDetails to a refresh-and-retry message without navigating away.
- [x] Keep publish readiness separate from draft persistence in current bridge flow.

### Session contracts

- [x] Add `GetEventSessionCreateContextQuery`.
- [x] Add `CreateEventSessionRequest` or `CreateEventSessionDraftRequest` that replaces direct generated DTO use in Blazor. Dedicated create composer now binds `CreateEventSessionRequest`; `EventService` maps it to generated `CreateEventSessionDto` at the API boundary.
- [x] Add `UpdateEventSessionRequest` tuned for dedicated composer. Dedicated edit composer now binds `UpdateEventSessionRequest`; `EventService` maps it to generated `UpdateEventSessionDto` at the API boundary.
- [x] Add `GetEventProgramSummaryQuery` with server-owned sections, groups, local-day item grouping, and readiness warnings.
- [x] Add `EventSessionKind` lookup and seed values: Talk, Workshop, Panel, Lecture, Class, Activity, Keynote, LightningTalk, BOF, Demo, QAndA, Other. Added domain enum/entity, EF configuration/migration, runtime lookup seeding, repository/query/API endpoint, public authorization coverage, and lookup endpoint smoke coverage.
- [x] Ensure session create context includes inherited event defaults.
- [x] Include available groups/tracks/devrooms in session create context.
- [ ] Include room/location/language/speaker/category/tag/template/custom-field options.
- [x] Validate session time inside event window or map readiness warning if policy allows exceptions. Program Summary now emits warning-based `program.sessions[*].startTime` readiness paths for sessions outside the event date window rather than hard-blocking create/update.
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
- [x] If event is dirty after initial save, Add session should update the existing draft before navigating. Event Edit now uses the explicit public `UpdateEventDraftRequestDto` draft-update contract before routing to the dedicated session composer.
- [x] Navigate to `/events/{eventId}/sessions/create` after successful draft create.
- [x] Use server session create context for inherited defaults.
- [x] Return to Event composer after session save/cancel.
- [x] Refresh server-backed Program summary after returning. Dedicated session create/edit routes return to Event Edit with `programUpdated=1`; Event Edit reloads the server-backed summary on initialization and announces the refreshed summary for assistive tech.
- [x] Announce save/navigation state for assistive tech.

## Phase 4 — Dedicated Session Create/Edit Page

- [x] Add route `/events/{eventId}/sessions/create`.
- [x] Add route `/events/{eventId}/sessions/{sessionId}/edit`.
- [x] Page title defaults to `Add program item` / `Edit program item`; default form labels and validation copy use program-item vocabulary.
- [x] Support contextual labels: talk/workshop/panel/activity/session. Dedicated create/edit pages now expose a `Program item type` picker backed by persisted `EventSessionKindId`, and Program Summary renders the saved kind as chip/metadata copy.
- [x] Add fields for title, description, date/start/end.
- [x] Add explicit timezone field/context. Create and edit session pages now show server timezone/date-window context via `SessionCreateContextBanner`; timezone-aware conversion remains centralized through the existing `DateTimeHelper` UTC conversion path.
- [x] Add location and room selection with stale room clearing.
- [x] Add program section/track/devroom picker.
- [ ] Add speaker picker/management affordance.
- [x] Add language picker. Dedicated create/edit pages now load language lookup values, bind selected language IDs, and sync assignments through `IEventSessionLanguageService` after the session save succeeds.
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
- [ ] Show time, room/location, speakers, language, capacity/registration, readiness warnings. Event Edit now renders server-backed time, session kind, room/location, capacity/registration, and readiness warning metadata; speakers and language summary display remain contract-backed follow-up work.
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

- [x] Plan future session lifecycle states: Draft, Submitted, InReview, Accepted, Rejected, Published, Cancelled. Documented as future-compatible only; do not implement the full lifecycle until review/submission workflows exist.
- [x] Add/plan event-scoped permissions: `event.program.view`, `event.program.manage`, `event.session.create`, `event.session.update`, `event.session.delete`, `event.session.review`, `event.session.assign_speaker`, `event.session.assign_group`, `event.session.publish`, `event.session_group.create`, `event.session_group.update`, `event.session_group.delete`, `event_session_group.reorder`. Documented as a future permission vocabulary; only permissions required by current endpoints should be runtime-enforced now.
- [x] Add readiness paths for `program.sessions[*]`, `program.groups[*]`, and `program.agenda[*]`. Program Summary warnings now use machine-addressable paths such as `program.sessions[0].title`, `program.sessions[0].sessionGroupId`, `program.groups`, and `program.agenda[0].startTime`.
- [x] Add audit-backed group/session assignment command paths.
- [ ] Add audit for reorder.
- [x] Add ProblemDetails for program validation failures beyond current command response behavior. Current session/session-group program write endpoints now convert command validation failures to RFC7807 `ValidationProblemDetails` while leaving successful command responses unchanged.
- [x] Add architecture tests for rollback and HAL conventions touched by this work.
- [x] Add Application unit tests for session-group handlers.
- [x] Add Persistence integration tests for group assignment lifecycle.
- [x] Add API integration tests for session-group write HAL links/endpoints. HAL policy coverage protects collection-item delete and assign-session route/permission metadata; endpoint-level API tests cover anonymous create/update/delete/assign/unassign write route authentication and authenticated PostgreSQL-backed create/update/delete/assign/unassign persistence; Blazor covers the Event Edit permission gate, direct group-list section count behavior, program section create location/room DTO mapping, HAL-gated delete behavior, and one-section assign/unassign service mapping.
- [x] Add Blazor component tests for Add session flow and dedicated composers.
- [ ] Add accessibility verification for keyboard/focus/announcements in browser/E2E. Component-level announcement coverage now protects Create Session success, local validation failure, and API failure announcements; full browser/E2E remains open because it requires the authenticated Aspire flow.

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
- [x] LSP diagnostics clean on modified Blazor client/test files for the `UpdateEventDraftRequestDto` migration.
- [x] LSP diagnostics clean on modified draft idempotency/concurrency files.
- [x] `rtk dotnet build "Explore.Blazor.Client" --configuration Release --verbosity quiet`
- [x] `dotnet "Explore.Blazor.Client.Tests/bin/Release/net10.0/Explore.Blazor.Client.Tests.dll" --minimum-expected-tests 1 --no-progress` — `1048 total / 1045 succeeded / 3 skipped`.
- [x] First direct Blazor client test run with `--maximum-failed-tests 1` hit unrelated transient `ShellDockChange_AfterHydration_DebouncesAutosaveWithShellKey`; rerun without early abort passed.
- [x] Focused draft refinements verification covered `EventServiceTests`, `EventEditTests`, `UpdateEventDraftCommandHandlerConcurrencyTests`, Blazor client build/tests, Application unit-test build/tests, and API integration-test build.
- [x] Oracle final review blockers addressed: update 409 metadata/generated client now match ProblemDetails stale-write handling; publish 409 metadata/generated client now match the controller's `BaseCommandResponse<Guid>` conflict shape; MyEvents loads event detail to pass a real publish concurrency stamp.
- [x] Focused post-Oracle verification covered `MyEventsTests`, `EventServiceTests`, `EventEditTests`, `UpdateEventDraftCommandHandlerConcurrencyTests`, Blazor client/test builds, and API integration-test build.
- [x] Small remaining-task verification covered `CreateSessionTests`, `EditSessionTests`, `EventEditTests`, `EventSessionGroupCommandHandlerTests`, Blazor client build, Blazor client test build, and Application unit-test build.
- [x] EventSessionKind lookup verification covered C# LSP diagnostics, `Explore.Application`, `Explore.Persistence`, `Explore.API`, and `Event.API.IntegrationTests` Release builds, focused `LookupTableControllerTests/EventSessionKind*`, focused `EndpointAuthorizationMatrixTests/Matrix_Public_EventSessionKinds_AnonymousOK`, and `git diff --check` on touched lookup files.
- [x] Program readiness path verification covered C# LSP diagnostics, `Explore.Application` and `Event.Application.UnitTests` Release builds, focused `GetEventProgramSummaryRequestHandlerTests`, and `git diff --check` on touched readiness/docs files.
- [x] Program ProblemDetails verification covered C# LSP diagnostics, sequential `Explore.API` Release build, focused `EventSessionGroupRealRuntimeTests`, and `git diff --check` on touched API/test files.
- [x] Create Session accessibility announcement verification covered C# LSP diagnostics and focused `CreateSessionTests` for polite success, assertive local validation failure, and assertive API failure announcements.
- [x] Language picker/session-language sync verification covered C# LSP diagnostics on the new controller/service/tests plus modified composer tests, `Explore.API` Release build, `Explore.Blazor.Client` Release build, full `Explore.Blazor.Client.Tests` (`1057 succeeded / 1 skipped`), `Event.Application.UnitTests`, `Event.Architecture.Tests`, and full `Explore.sln` Release build.
- [x] `rtk git diff --check -- dev/active/event-creation-progressive-disclosure/*.md` and touched HAL/test files
- [x] `rg -n "child-event Program summary|parent candidate lookup as a required program feature|child-event program interpretation" dev/active/event-creation-progressive-disclosure` — expected matches remain only in this checklist and historical plan rejection notes.
- [x] Confirm active Create/Edit Event shell files no longer reference `SessionEditorPanel`, `SessionEditorWorkflow`, `ScheduleTimelineComposer`, or `PopulateSchedulingOnRequest`.
