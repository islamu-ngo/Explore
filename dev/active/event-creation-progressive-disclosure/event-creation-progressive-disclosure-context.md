<!-- ABOUTME: Current context for corrected Event Creation Composer toward session/group program management. -->
<!-- ABOUTME: Captures completed backend, HAL, Blazor composer, and handoff state for continuation. -->

# Event Creation Progressive Disclosure Context

Last Updated: 2026-05-07 02:25 CEST

## Current Source Of Truth

The active direction is **Event -> EventSessionGroup -> EventSession**, not child events.

Talks, workshops, lectures, panels, classes, and activities are `EventSession` records. Tracks, devrooms, stages, and neutral “program sections” are `EventSessionGroup` records. `EventSessionGroupSession` is the explicit assignment join. Logistics such as breaks, meals, prayer slots, and transitions remain `EventAgendaItem`/agenda concepts, not child events.

The Create/Edit Event shells are now event-draft shells only. They no longer host right-sidebar session editors, giant nested schedule composers, inline day/room/agenda builders, or first-session editing as the primary session workflow. Session creation/editing happens in dedicated routes.

## Implementation State

### Completed backend and API foundation

- Rolled back program-only child-event/`ParentEventId` direction from active server source and generated contracts.
- Added `EventSessionGroup` and `EventSessionGroupSession` domain/persistence model with tenant, audit, soft-delete, and assignment metadata.
- Added focused migration `Explore.Persistence/Migrations/20260505151339_AddEventSessionGroups.cs` and adjusted unique indexes so soft-deleted assignment rows do not block reassignment.
- Added Application contracts, DTOs, validators, commands, handlers, authorization resource kinds/descriptors/fallback/Cerbos policy, and API write/read endpoints for session groups and assignments.
- Added server-owned `GetEventSessionCreateContextRequest` and `GET /api/event/{id}/session-create-context` so dedicated session create receives inherited event defaults, timezone context, location/room options, and program section options from Application/API.
- Added server-owned `GetEventProgramSummaryRequest` and `GET /api/event/{id}/program-summary` so event shells can consume a canonical program summary grouped by program section, session group, and event-local day.
- Regenerated/updated `Explore.API/swagger.json` and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`; generated session-group concrete DTOs are currently insufficient for Blazor, so the client uses a local model for list reads.
- Added persistence regression tests for soft-deleted assignment reuse and primary reassignment.

### Completed HAL and Blazor shell direction

- Event HAL detail links now expose `program`, `program-summary`, `sessions`, `session-groups`, `add-session`, `session-create-context`, and `add-session-group` with event/tenant-scoped authorization metadata where required.
- `CreateEvent.razor` Add Session saves a draft and navigates to `/events/{eventId}/sessions/create`.
- `EventEdit.razor` Add Session navigates to `/events/{eventId}/sessions/create`; session edit navigates to `/events/{eventId}/sessions/{sessionId}/edit`.
- `EventEdit.razor` Add Session now saves the current event draft through the existing event update path before navigating, so dirty shell edits are not dropped when routing to the dedicated session composer.
- Create/Edit Event program summaries now expose the saved-empty-state affordances `[Add session]` and `[Add section/track]`; Event Edit opens a HAL-gated program sections dialog for list/create/edit/delete and one-section session assignment/unassignment using existing session-group APIs, including optional default location/room metadata.
- Event Edit program summaries now surface the saved session's time window, room/location, capacity, and registration mode from existing session data; speaker, language, and readiness details remain deferred until their contracts are available.
- Event Edit now asks `IEventService.GetEventProgramSummaryAsync(EventId)` for the server-backed Program Summary and renders grouped sections, session groups, local-day buckets, program items, and readiness guidance when available, falling back to local session data only when the endpoint is unavailable.
- Create Event keeps common presets/effects in the page-level `ThemeQuickBar`; `ThemeStudioTray` is now advanced-only and no longer duplicates quick preset/effect controls.
- Create/Edit Event shells no longer use `SessionEditorPanel`, `SessionEditorWorkflow`, `SessionImageUploadWorkflow`, `CreateSessionDialog`, `EditSessionDialog`, or `EventSessionEditor`; obsolete drawer-era files and stale tests were deleted.
- Create/Edit Event shells no longer render inline schedule logistics (`ScheduleTimelineComposer`, day/room/agenda managers, agenda grid/miller columns) as primary creation/editing UI.
- Create Event no longer renders the temporary first-session seed fields. Public API/OpenAPI/Blazor generated client create now uses scalar `CreateEventDraftRequestDto`; the backend maps it into the internal graph-shaped `CreateEventRequest` with empty session/day/room/agenda collections, forces `EventStatusId = 1` server-side, and persists a draft event with an empty program.

### Completed dedicated session composer work

- Added `/events/{eventId}/sessions/create` in `Explore.Blazor.Client/Pages/Events/Sessions/CreateSession.razor`.
- Added `/events/{eventId}/sessions/{sessionId}/edit` in `Explore.Blazor.Client/Pages/Events/Sessions/EditSession.razor`.
- Both pages use `IEventService`/BFF client wrappers, not raw HTTP, and gate writes with HAL links (`add-session` for create, `edit` for update).
- Both pages support title, description, date/start/end, UTC conversion through `DateTimeHelper`, capacity normalization, location selection, room selection with stale room clearing, and program section selection.
- Both pages now load registration mode lookup values through `IAdminService` and bind the existing session `RegistrationModeId` so creators can choose Open / Approval required / Invite only / Closed modes without changing the save contract.
- `CreateSession.razor` consumes server-backed create context for inherited defaults, timezone context, selector options, and setup notices while still gating save with the event `add-session` HAL link.
- `SessionCreateContextBanner` renders the server-owned create context as explicit timezone/date-window/setup guidance on the dedicated create page.
- Added client model `Explore.Blazor.Client/Models/EventSessionGroups/EventSessionGroupListModel.cs` because NSwag generated empty session-group DTO shells. `HalResourceExtensions.GetItems(HalCollectionResourceOfEventSessionGroupListDto)` deserializes HAL collection items into this model.
- `IEventService` now wraps:
  - `GetEventSessionCreateContextAsync(Guid eventId)`
  - `GetSessionGroupsByEventAsync(Guid eventId)`
  - `AssignSessionToGroupAsync(Guid eventId, Guid eventSessionGroupId, Guid eventSessionId, bool isPrimary = true, int sortOrder = 0)`
  - `UnassignSessionFromGroupAsync(Guid eventId, Guid eventSessionGroupId, Guid eventSessionId)`
- Oracle caught two assignment-flow blockers and they are fixed:
  - Create retry after a successful session save but failed group assignment now reuses `_savedSessionId` rather than creating duplicate sessions.
  - Clearing a program section on edit now calls the unassign endpoint instead of silently leaving the old assignment attached.

## Key Decisions Made This Session

1. **Dedicated session routes are the canonical session workflow.** Event shells can summarize and route, but cannot be the primary place for talk/workshop editing.
2. **Event shell schedule logistics are intentionally removed.** Day labels, rooms, agenda grids, and logistics belong in later program/agenda surfaces, not in the initial event draft shell.
3. **Draft creation is scalar and server-owned at the public boundary.** `CreateEventDraftRequestDto` is the create endpoint/client contract; it excludes program graph fields and client-controlled `EventStatusId`. The older graph-shaped `CreateEventRequest` remains an internal Application bridge for handler/import-style graph creation until follow-up refactoring removes or renames it.
4. **Program section assignment is optional but explicit.** Dedicated session pages load groups and assign/unassign after session save/update. Partial failures are surfaced without creating duplicates.
5. **Session create context is server-owned.** Create receives inherited defaults and selector options from `GetEventSessionCreateContextRequest`; richer option families such as speakers, languages, categories, tags, templates, and custom fields remain deferred.
6. **Program summary is server-owned.** Event shells consume `GetEventProgramSummaryRequest` for section/group/day/item metadata and readiness warnings instead of inferring the canonical program structure locally.
7. **Generated client gap is bridged locally.** Use `EventSessionGroupListModel` until OpenAPI/NSwag emits concrete `EventSessionGroupListDto` properties correctly.
8. **HAL remains the client affordance contract.** UI checks links instead of roles/claims.

## Important Files Modified

### Blazor shell and composer files

- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor`, `.razor.cs`, `.razor.css`
  - Removed drawer/session-editor and schedule-logistics shell UI.
  - Removed the temporary seed-session inputs; draft creation now submits an empty program unless a future contract-backed flow explicitly adds sessions.
  - Uses generated `CreateEventDraftRequestDto` for public draft create instead of the graph-shaped create request; create status is server-owned and no longer part of the form DTO.
  - Program summary shows Add Session and Add section/track affordances; Add Session saves draft and routes to dedicated session create page.
  - Theme quick bar remains page-level while the theme tray contains only advanced styling controls.
- `Explore.Blazor.Client/Pages/Events/EventEdit.razor`, `.razor.cs`, `.razor.css`
  - Removed drawer/session-editor and scheduling managers.
  - Program summary shows saved program item and section/track affordances without reintroducing child-event language.
  - Saved program item summaries prefer the server-backed Program Summary and include section/group/day/item metadata, time, location, capacity, registration mode, and readiness warning metadata when available.
  - Add section/track opens a program sections dialog when the event HAL exposes `add-session-group`; missing HAL shows a permission message.
  - Add Session reuses the event update path before navigating to `/events/{eventId}/sessions/create`; failed updates keep the user on Event Edit and surface the server message.
  - Session edit routes to dedicated edit page.
  - Shell update no longer creates/updates sessions.
  - Fixed `RegistrationPolicyId` preservation during populate/save.
- `Explore.Blazor.Client/Pages/Events/Dialogs/ProgramSectionsDialog.razor(.cs/.css)`
  - Lists existing program sections/tracks, preserves HAL item links through `EventSessionGroupListModel`, and supports create/edit/delete plus one-section session assignment/unassignment through `IEventService` wrappers.
  - Allows optional default location/room selection using `ILocationService`/`ILocationRoomService` and clears stale rooms when the location changes.
- `Explore.Blazor.Client/Pages/Events/Components/EventProgramSummaryView.razor(.css)`
  - Renders the server-backed Program Summary as accessible grouped sections, session groups, local-day buckets, program items, and readiness guidance.
- `Explore.Blazor.Client/Pages/Events/Sessions/CreateSession.razor`
  - Dedicated create composer with server-backed create context, inherited defaults, timezone context, notices, location, room, program section, and registration mode support.
- `Explore.Blazor.Client/Pages/Events/Components/SessionCreateContextBanner.razor(.css)`
  - Reusable create-context banner for event timezone, event date window, and server setup notices.
- `Explore.Blazor.Client/Pages/Events/Sessions/EditSession.razor`
  - Dedicated edit composer with HAL-gated save, relationship validation, location/room/program section/registration mode support, and unassignment.
- `Explore.Blazor.Client/Services/EventService.cs`
  - Added program summary, session create context, and session-group list/create/update/assign/unassign wrappers.
  - `CreateEventAsync` now accepts generated `CreateEventDraftRequestDto` for public draft creation.
- `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`
  - Added `EventSessionDto.HasHalLink(...)`, `EventSessionGroupListModel.HasHalLink(...)`, and session-group HAL collection deserialization.
- `Explore.Blazor.Client/Models/EventSessionGroups/EventSessionGroupListModel.cs`
  - Local typed model for session-group list items until generated DTOs are fixed; preserves HAL `_links` via extension data for item-level edit affordances.

### Removed obsolete drawer-era files

- `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor(.css)`
- `Explore.Blazor.Client/Pages/Events/Components/EventSessionEditor.razor(.css)`
- `Explore.Blazor.Client/Pages/Events/Dialogs/CreateSessionDialog.*`
- `Explore.Blazor.Client/Pages/Events/Dialogs/EditSessionDialog.*`
- `Explore.Blazor.Client/Pages/Events/Workflows/SessionEditorWorkflow.cs`
- `Explore.Blazor.Client/Pages/Events/Workflows/SessionImageUploadWorkflow.cs`
- Obsolete tests for those components/workflows.

### Test files added/updated

- `Explore.Blazor.Client.Tests/Pages/Event/CreateSessionTests.cs`
  - Verifies program-item copy, create mapping, server-owned registration default, location/room context, program assignment, assignment retry idempotency, and HAL no-link behavior.
- `Explore.Blazor.Client.Tests/Pages/Event/EditSessionTests.cs`
  - Verifies update mapping, location/room stale clearing, program assignment/unassignment, wrong-event guard, and missing edit HAL behavior.
- `Explore.Blazor.Client.Tests/Pages/Event/EventEditTests.cs`
  - Verifies Add/Edit session routing, program-section permission/summary behavior, server-backed Program Summary copy, and shell no longer persists sessions.
- `Explore.Blazor.Client.Tests/Pages/Event/ProgramSectionsDialogTests.cs`
  - Verifies program section create DTO mapping for location/room metadata, stale room clearing after location changes, HAL-gated delete confirmation/reload behavior, and one-section assign/unassign service mapping.
- `Explore.Blazor.Client.Tests/Components/Event/EventProgramSummaryViewTests.cs`
  - Verifies grouped Program Summary rendering, item metadata, readiness guidance, and empty state copy.
- `Explore.Blazor.Client.Tests/Pages/Event/CreateEventTests.cs`
  - Verifies Program Summary, removed schedule/drawer/seed-session UI, Add Session draft navigation with zero sessions, and publish dialog wording.
- `Event.Application.UnitTests/Features/Events/Validators/CreateEventRequestValidatorTests.cs`
  - Verifies `CreateEventRequest` validation allows zero-session event drafts while still validating optional program graph items when present.
- `Event.Application.UnitTests/Features/Events/Commands/CreateEventCommandHandlerTests.cs`
  - Verifies zero-session event drafts create the event aggregate without creating `EventSession` rows or first/last session projections.
- `Explore.Blazor.Client.Tests/Models/SessionEditorModelTests.cs`
  - Reduced to summary mapping tests after drawer-era helper methods were removed.
- `Event.Application.UnitTests/Features/EventSessions/Queries/GetEventSessionCreateContextRequestHandlerTests.cs`
  - Verifies event-scoped defaults, location/room/group options, setup notices, and missing-event behavior.
- `Event.Application.UnitTests/Features/EventPrograms/Queries/GetEventProgramSummaryRequestHandlerTests.cs`
  - Verifies event-local grouping, section/group/day/item metadata, missing-event behavior, unassigned sessions, and readiness warnings.
- `Event.API.IntegrationTests/Features/Hateoas/EventSessionGroupHateoasTests.cs`
  - Verifies collection-item session-group HAL delete metadata (`DELETE`, route name, event-scoped route values, auth, and delete permission resource) and assign-session metadata (`POST`, route name, auth, update permission resource) used by the Program Sections dialog.
- `Event.API.IntegrationTests/Features/EventSessionGroupControllerTests.cs`
  - Verifies session-group write endpoints require authentication for create, update, delete, assign, and unassign routes.
- `Event.API.IntegrationTests/Features/EventSessionGroupRealRuntimeTests.cs`
  - Verifies authenticated session-group create, update, delete, assign, and unassign routes against PostgreSQL-backed API runtime and persisted state.
- `Event.API.IntegrationTests/Features/EventControllerRealRuntimeTests.cs`
  - Verifies authenticated public draft create uses `CreateEventDraftRequestDto`, persists an event with no session rows or first/last session projections, rejects blank titles, omits client-controlled status, and does not persist graph rows from the draft contract.

## Verification Completed In This Session

Use targeted project verification; the full repo has unrelated dirty-tree churn.

Passed after the latest session-group composer Oracle fixes:

```bash
rtk dotnet build "Explore.Blazor.Client/Explore.Blazor.Client.csproj" --configuration Release --verbosity minimal
rtk dotnet build "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --verbosity minimal
rtk dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --no-build --verbosity minimal
rtk dotnet test --project "Event.Architecture.Tests/Event.Architecture.Tests.csproj" --configuration Release --no-build --verbosity minimal
```

Additional verification after adding API/HAL policy coverage for the session-group collection delete affordance:

```bash
rtk dotnet build "Event.API.IntegrationTests" --configuration Release --verbosity quiet
rtk dotnet test --project "Event.API.IntegrationTests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventSessionGroupHateoasTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Event.Architecture.Tests" --configuration Release --no-build --verbosity quiet
rtk git diff --check -- "Explore.API/Hateoas/Policies/EventSessionGroupLinkPolicy.cs" "Event.API.IntegrationTests/Features/Hateoas/EventSessionGroupHateoasTests.cs" "dev/active/event-creation-progressive-disclosure/event-creation-progressive-disclosure-tasks.md" "dev/active/event-creation-progressive-disclosure/event-creation-progressive-disclosure-context.md"
```

Additional verification after adding endpoint-level session-group write authentication coverage:

```bash
rtk dotnet build "Event.API.IntegrationTests" --configuration Release --verbosity quiet
rtk dotnet test --project "Event.API.IntegrationTests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventSessionGroupControllerTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Event.API.IntegrationTests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventSessionGroupRealRuntimeTests/*" --minimum-expected-tests 1 --no-progress
```

Additional verification after removing the Create Event seed-session bridge:

```bash
rtk dotnet build "Explore.Application" --configuration Release --verbosity quiet
rtk dotnet build "Event.Application.UnitTests" --configuration Release --verbosity quiet
rtk dotnet build "Explore.Blazor.Client" --configuration Release --verbosity quiet
rtk dotnet build "Explore.Blazor.Client.Tests" --configuration Release --verbosity quiet
rtk dotnet build "Event.API.IntegrationTests" --configuration Release --verbosity quiet
rtk dotnet test --project "Event.Application.UnitTests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/CreateEventRequestValidatorTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Event.Application.UnitTests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/CreateEventCommandHandlerTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Explore.Blazor.Client.Tests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/CreateEventTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Event.API.IntegrationTests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventControllerRealRuntimeTests/*" --minimum-expected-tests 1 --no-progress
```

Additional verification after switching the public create API/client contract to scalar `CreateEventDraftRequestDto`:

```bash
rtk dotnet build "Explore.Blazor.Client" --configuration Release --verbosity quiet
rtk dotnet build "Explore.Blazor.Client.Tests" --configuration Release --verbosity quiet
rtk dotnet build "Event.API.IntegrationTests" --configuration Release --verbosity quiet
rtk dotnet test --project "Explore.Blazor.Client.Tests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/CreateEventTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Explore.Blazor.Client.Tests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventServiceTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Event.API.IntegrationTests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventControllerRealRuntimeTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Event.Architecture.Tests" --configuration Release --no-build --verbosity quiet
```

Additional earlier targeted verification passed for backend/session-group work:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity minimal
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity minimal
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build --verbosity minimal
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity minimal
```

Notes:

- Razor files have no configured LSP server in this environment; they were validated through builds/tests.
- Warnings are existing/noisy package/analyzer warnings unless noted otherwise.
- `rtk` runs in binlog-only mode for tests, so exact counts are unavailable from wrapper output, but commands completed successfully.

## Known Issues / Blockers / Deferred Work

- **Draft contracts are partially complete.** `CreateEventDraftRequestDto` is now the public API/OpenAPI/Blazor client create contract and no longer exposes session/day/room/agenda graph fields or client-controlled status. `UpdateEventDraftRequest`, idempotency key, and concurrency semantics remain pending; the internal graph-shaped `CreateEventRequest` still exists as an Application bridge for handler/import-style graph creation.
- **Session create context is partial.** It now centralizes inherited event defaults, timezone context, locations, rooms, and program sections. Language, speaker, category, tag, template, custom-field options, and readiness rules still need contract-backed expansion.
- **Program summary UI is partially server-backed.** Event Edit now renders grouped server-backed sections/groups/days/items, but agenda/logistics plus speaker/language-specific rendering remain pending.
- **Session group management UI is partial.** Event Edit now supports HAL-gated list/create/edit/delete and one-section session assignment/unassignment for program sections/tracks with optional default location/room metadata. HAL policy coverage protects collection delete and assign-session affordances, and endpoint-level API tests cover write-route authentication plus authenticated PostgreSQL-backed create/update/delete/assign/unassign persistence. Reorder, bulk assignment management, and multi-group workflows remain pending.
- **Generated session-group DTOs are incomplete.** Local `EventSessionGroupListModel` is a bridge until NSwag/OpenAPI emits usable concrete DTO properties.
- **Unassign semantics are implemented in the composer service wrapper, but no standalone UI exists for managing multiple group assignments.** Current UI handles one selected primary section.
- **Dirty-tree warning.** The repository contains many unrelated modifications/deletions and is ahead of origin by 31 commits. Do not revert unrelated changes.

## Next Immediate Steps

1. Add explicit `UpdateEventDraftRequest` and decide idempotency/concurrency semantics for draft persistence.
2. Extend program section/track management with reorder, bulk/multi-group assignment workflows, and richer validation feedback using existing session-group APIs.
3. Expand session create context with language, speaker, category, tag, template, custom-field options, and readiness rules.
4. Add richer dedicated session fields: program item type/kind, language picker, speaker management, categories/tags/custom fields/template/image, and readiness warnings.
5. Extend grouped Program Summary UI with agenda/logistics, speakers, language metadata, and richer readiness remediation actions where useful.
6. Fix OpenAPI/NSwag session-group DTO generation so the local bridge model can be deleted.

## Quick Resume Checklist

1. Read this file and `event-creation-progressive-disclosure-tasks.md`.
2. Check `git status` and avoid unrelated dirty-tree changes.
3. If touching session group UI, inspect:
   - `Explore.Blazor.Client/Pages/Events/Sessions/CreateSession.razor`
   - `Explore.Blazor.Client/Pages/Events/Sessions/EditSession.razor`
   - `Explore.Blazor.Client/Services/EventService.cs`
   - `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`
   - `Explore.Blazor.Client/Models/EventSessionGroups/EventSessionGroupListModel.cs`
4. Run targeted builds/tests listed above before handoff.
5. Consult Oracle for any changes crossing HAL authorization, create/update semantics, or event/session/group assignment behavior.

## Rejected Direction Grep

Use this periodically:

```bash
rg -n "ParentEventId|ChildEvents|subevent|child-event|old add-child-event|SessionEditorPanel|SessionEditorWorkflow|ScheduleTimelineComposer|PopulateSchedulingOnRequest" \
  Explore.Blazor.Client/Pages/Events dev/active/event-creation-progressive-disclosure \
  --glob '!**/bin/**' --glob '!**/obj/**'
```

Expected: matches only in historical docs/obsolete target lists or deleted-file status, not active Create/Edit Event shell code.
