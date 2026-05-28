<!-- ABOUTME: Current context for corrected Event Creation Composer toward session/group program management. -->
<!-- ABOUTME: Captures completed backend, HAL, Blazor composer, and handoff state for continuation. -->

# Event Creation Progressive Disclosure Context

Last Updated: 2026-05-07 14:50 CEST

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
- Regenerated/updated `schemas/openapi.json` and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`; generated session-group concrete DTOs are currently insufficient for Blazor, so the client uses a local model for list reads.
- Public event draft update now uses `UpdateEventDraftRequestDto` at the API/OpenAPI/generated Blazor boundary; status changes and publish remain explicit lifecycle endpoints, not draft-update side effects.
- Draft create now has client-side idempotency key forwarding: `EventService.CreateEventAsync(CreateEventDraftRequestDto, string?)` uses the concrete generated `EventApiClient` to send `Idempotency-Key` on `POST /api/event`, generating a key when the caller does not provide one.
- Draft update now has stale-write protection: `UpdateEventDraftRequestDto.ExpectedConcurrencyStamp` is required, `UpdateEventDraftCommandHandler` compares it with the current event stamp before mutation, and stale writes throw `ConcurrencyConflictException`.
- Event session group create/update handlers now pre-check event-local slug uniqueness across all active groups, including unpublished groups, before hitting database constraints; unit coverage protects duplicate create/update rejection.
- Added `EventSessionKind` lookup foundation with stable enum ids and runtime seeds for Talk, Workshop, Panel, Lecture, Class, Activity, Keynote, LightningTalk, BOF, Demo, QAndA, and Other. The read-only public `GET /api/eventsessionkind` endpoint mirrors the existing lookup-table pattern, and sessions now persist an optional nullable `EventSessionKindId` FK with generated client/API/Application/EF coverage.
- Program Summary readiness warnings now use machine-addressable program paths (`program.sessions[*]`, `program.groups`, `program.agenda[*]`) and warn on out-of-window sessions/agenda logistics instead of hard-blocking current create/update flows.
- Current session/session-group program write endpoints convert command validation failures to RFC7807 `ValidationProblemDetails`; successful writes still return `BaseCommandResponse<Guid>`.
- Added persistence regression tests for soft-deleted assignment reuse and primary reassignment.

### Completed HAL and Blazor shell direction

- Event HAL detail links now expose `program`, `program-summary`, `sessions`, `session-groups`, `add-session`, `session-create-context`, and `add-session-group` with event/tenant-scoped authorization metadata where required.
- `CreateEvent.razor` Add Session saves a draft and navigates to `/events/{eventId}/sessions/create`.
- `EventEdit.razor` Add Session navigates to `/events/{eventId}/sessions/create`; session edit navigates to `/events/{eventId}/sessions/{sessionId}/edit`.
- `EventEdit.razor` Add Session now saves the current event draft through `IEventService.UpdateEventAsync(Guid, UpdateEventDraftRequestDto)` before navigating, so dirty shell edits are not dropped when routing to the dedicated session composer.
- Event Edit draft save is lifecycle-neutral: it no longer writes `EventStatusId`, server-owned IDs, invalid organization/group source fields, or session-derived first/last projection fields through the draft update DTO.
- Event Edit includes the loaded event concurrency stamp in draft saves, refreshes the event/stamp after successful saves, and surfaces stale-write failures as a refresh-and-retry message instead of navigating away.
- Event Edit renders status as read-only display copy; publish/status transitions use explicit generated endpoints. The dead `eventStatuses` lookup was removed after the selector was removed.
- My Events list publishing now loads event detail first and passes the current `ConcurrencyStamp` into the explicit publish endpoint; it fails safely with refresh guidance if detail/stamp is unavailable.
- Create/Edit Event program summaries now expose the saved-empty-state affordances `[Add session]` and `[Add section/track]`; Event Edit opens a HAL-gated program sections dialog for list/create/edit/delete and one-section session assignment/unassignment using existing session-group APIs, including optional default location/room metadata.
- Event Edit program summaries now surface the saved session's time window, session kind, room/location, capacity, registration mode, and readiness warning metadata from server-backed session data; speaker and language summary details remain deferred until their contracts are available.
- Event Edit now asks `IEventService.GetEventProgramSummaryAsync(EventId)` for the server-backed Program Summary and renders grouped sections, session groups, local-day buckets, program items, and readiness guidance when available, falling back to local session data only when the endpoint is unavailable.
- Dedicated session create/edit pages now return to Event Edit with `programUpdated=1`; Event Edit reloads the server-backed Program Summary during normal initialization and announces that the summary refreshed when that marker is present.
- Create Event keeps common presets/effects in the page-level `ThemeQuickBar`; `ThemeStudioTray` is now advanced-only and no longer duplicates quick preset/effect controls.
- Create/Edit Event shells no longer use `SessionEditorPanel`, `SessionEditorWorkflow`, `SessionImageUploadWorkflow`, `CreateSessionDialog`, `EditSessionDialog`, or `EventSessionEditor`; obsolete drawer-era files and stale tests were deleted.
- Create/Edit Event shells no longer render inline schedule logistics (`ScheduleTimelineComposer`, day/room/agenda managers, agenda grid/miller columns) as primary creation/editing UI.
- Create Event no longer renders the temporary first-session seed fields. Public API/OpenAPI/Blazor generated client create now uses scalar `CreateEventDraftRequestDto`; the backend maps it into the internal graph-shaped `CreateEventRequest` with empty session/day/room/agenda collections, forces `EventStatusId = 1` server-side, and persists a draft event with an empty program.

### Completed dedicated session composer work

- Added `/events/{eventId}/sessions/create` in `Explore.Blazor.Client/Pages/Events/Sessions/CreateSession.razor`.
- Added `/events/{eventId}/sessions/{sessionId}/edit` in `Explore.Blazor.Client/Pages/Events/Sessions/EditSession.razor`.
- Both pages use `IEventService`/BFF client wrappers, not raw HTTP, and gate writes with HAL links (`add-session` for create, `edit` for update).
- Both pages support title, description, date/start/end, UTC conversion through `DateTimeHelper`, capacity normalization, location selection, room selection with stale room clearing, and program section selection.
- Both pages bind Blazor-facing `CreateEventSessionRequest` / `UpdateEventSessionRequest` models from `Explore.Blazor.Client/Models/EventSessions/`; `EventService` maps those models to generated `CreateEventSessionDto` / `UpdateEventSessionDto` only at the API client boundary.
- Both pages now load registration mode lookup values through `IAdminService` and bind the existing session `RegistrationModeId` so creators can choose Open / Approval required / Invite only / Closed modes without changing the save contract.
- Both pages now load session kind lookup values through `IAdminService.GetEventSessionKindsAsync()` and bind optional `EventSessionKindId` so creators can classify a program item as Talk / Workshop / Panel / Activity / etc. The saved type is mapped through `EventService`, persisted via the nullable FK, and rendered as a Program Summary chip/metadata value.
- Both pages now expose a contract-backed language multi-select using `IEventSessionLanguageService`. The service syncs selected language IDs through generated API client calls to the new session-language endpoints after session create/update succeeds.
- `CreateSession.razor` consumes server-backed create context for inherited defaults, timezone context, selector options, and setup notices while still gating save with the event `add-session` HAL link.
- `EditSession.razor` now also consumes server-backed create context for explicit timezone/date-window/setup guidance, matching the create-page context banner.
- `SessionCreateContextBanner` renders the server-owned create context as explicit timezone/date-window/setup guidance on the dedicated create and edit pages.
- Session create/edit save failures now send assertive accessibility announcements; successful saves announce that Program Summary will refresh before navigating back to Event Edit. Create Session component tests cover polite success, assertive local validation failure, and assertive API failure announcements.
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
5. **Session create context is server-owned.** Create receives inherited defaults and selector options from `GetEventSessionCreateContextRequest`; richer option families such as speakers, categories, tags, templates, and custom fields remain deferred. The language picker is implemented through the existing lookup service plus dedicated session-language assignment endpoints until/unless the create context grows a language option family.
6. **Program summary is server-owned.** Event shells consume `GetEventProgramSummaryRequest` for section/group/day/item metadata and readiness warnings instead of inferring the canonical program structure locally.
7. **Generated client gap is bridged locally.** Use `EventSessionGroupListModel` until OpenAPI/NSwag emits concrete `EventSessionGroupListDto` properties correctly.
8. **HAL remains the client affordance contract.** UI checks links instead of roles/claims.
9. **Draft update is narrow and lifecycle-neutral.** `UpdateEventDraftRequestDto` is the public update contract; publish/status calls stay explicit. `PublishEventRequestDto.ExpectedConcurrencyStamp` is nullable and `EventPublishReadinessDto` has no stamp, so list-page publish may omit a stamp and rely on server-side stale-list handling.
10. **Draft persistence now has first-pass double-submit/stale-write protection.** Create forwards an idempotency key from the Blazor generated-client path, while update requires an expected concurrency stamp and returns actionable stale-draft feedback.
11. **Oracle review fixed two contract regressions.** Draft update 409 is documented/generated as ProblemDetails to match global concurrency middleware; publish 409 remains documented/generated as `BaseCommandResponse<Guid>` because the controller returns command responses for publish concurrency conflicts.
12. **Remaining tasks are now lane-classified.** Implement now: Program Summary readiness paths, out-of-window session warnings, narrow ProblemDetails hardening for existing program/session validation failures, and stable component-level accessibility coverage for the current Add Session/session composer flow. Implement only when contract-backed: speaker/category/tag/template/custom-field/image fields, language display in Program Summary, and agenda logistics rendering. Planned/deferred: full session lifecycle, future permission vocabulary, reorder/audit, conflict detection, and reusable picker extraction until their endpoint/UI acceptance criteria exist.

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
  - Add Session saves via `UpdateEventDraftRequestDto` before navigating to `/events/{eventId}/sessions/create`; failed updates keep the user on Event Edit and surface the server message.
  - Session edit routes to dedicated edit page.
  - Shell update no longer creates/updates sessions, writes lifecycle status, or sends session-derived first/last projection fields.
  - Status is read-only in the edit shell, and the removed selector's dead `GetEventStatusesAsync` lookup was deleted.
  - Fixed `RegistrationPolicyId` preservation during populate/save.
- `Explore.Blazor.Client/Pages/Events/Dialogs/ProgramSectionsDialog.razor(.cs/.css)`
  - Lists existing program sections/tracks, preserves HAL item links through `EventSessionGroupListModel`, and supports create/edit/delete plus one-section session assignment/unassignment through `IEventService` wrappers.
  - Allows optional default location/room selection using `ILocationService`/`ILocationRoomService` and clears stale rooms when the location changes.
- `Explore.Blazor.Client/Pages/Events/Components/EventProgramSummaryView.razor(.css)`
  - Renders the server-backed Program Summary as accessible grouped sections, session groups, local-day buckets, program items, and readiness guidance.
- `Explore.Blazor.Client/Pages/Events/Sessions/CreateSession.razor`
- Dedicated create composer with server-backed create context, inherited defaults, timezone context, notices, location, room, program section, session kind, language, and registration mode support.
- `Explore.Blazor.Client/Pages/Events/Components/SessionCreateContextBanner.razor(.css)`
  - Reusable create-context banner for event timezone, event date window, and server setup notices.
- `Explore.Blazor.Client/Pages/Events/Sessions/EditSession.razor`
  - Dedicated edit composer with HAL-gated save, relationship validation, location/room/program section/registration mode support, and unassignment.
- `Explore.Blazor.Client/Services/EventService.cs`
  - Added program summary, session create context, and session-group list/create/update/assign/unassign wrappers.
  - `CreateEventAsync` now accepts generated `CreateEventDraftRequestDto` for public draft creation.
  - `UpdateEventAsync` now accepts generated `UpdateEventDraftRequestDto` and forwards it directly; `UpdateEventStatusAsync` uses the generated explicit status endpoint; `PublishEventAsync` uses the generated explicit publish endpoint with a required expected concurrency stamp.
  - `CreateEventAsync` accepts an optional idempotency key and uses the concrete generated client to send `Idempotency-Key` when possible.
- `Explore.Blazor.Client/Clients/EventApiClient.cs`
  - Adds the partial generated-client extension that scopes `Idempotency-Key` injection to create-event POST requests.
- `Explore.Blazor.Client/Pages/Events/MyEvents.razor.cs`
  - Loads event detail before list-page publish so the explicit publish endpoint receives the current concurrency stamp.
- `Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs`
  - Registers `UpdateEventDraftRequestDto` instead of removed `UpdateEventDto` for source-generated serialization.
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
  - Verifies Add/Edit session routing, program-section permission/summary behavior, server-backed Program Summary copy, shell no longer persists sessions, and draft save uses `UpdateEventDraftRequestDto`.
- `Explore.Blazor.Client.Tests/Services/EventServiceTests.cs`
  - Verifies event update service forwards `UpdateEventDraftRequestDto` directly to the generated client instead of wrapping removed `UpdateEventRequestDto`, stale update ProblemDetails maps to `event_draft_concurrency_conflict`, and create sends the configured `Idempotency-Key` header.
- `Explore.Blazor.Client.Tests/Pages/Event/MyEventsTests.cs`
  - Verifies list-page publish loads event detail and passes the current concurrency stamp to `PublishEventAsync`.
- `Event.Application.UnitTests/Features/Events/Commands/UpdateEventDraftCommandHandlerConcurrencyTests.cs`
  - Verifies stale `ExpectedConcurrencyStamp` values throw `ConcurrencyConflictException` before the event repository is updated.
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

Additional verification after switching the public update API/client contract to scalar `UpdateEventDraftRequestDto` and explicit lifecycle endpoints:

```bash
rtk dotnet build "Explore.Blazor.Client" --configuration Release --verbosity quiet
dotnet "Explore.Blazor.Client.Tests/bin/Release/net10.0/Explore.Blazor.Client.Tests.dll" --minimum-expected-tests 1 --no-progress
```

Result: LSP diagnostics were clean on modified Blazor client/test files; the direct Blazor client test assembly reported `1048 total / 1045 succeeded / 3 skipped`. An initial direct test run with `--maximum-failed-tests 1` hit unrelated transient `ShellDockChange_AfterHydration_DebouncesAutosaveWithShellKey`; rerunning without early abort passed. Oracle post-review found no blocking migration issues.

Additional verification after adding draft create idempotency and draft update stale-write protection:

```bash
rtk dotnet build "Explore.Blazor.Client" --configuration Release --verbosity quiet
rtk dotnet build "Explore.Blazor.Client.Tests" --configuration Release --verbosity quiet
rtk dotnet build "Event.Application.UnitTests" --configuration Release --verbosity quiet
rtk dotnet build "Event.API.IntegrationTests" --configuration Release --verbosity quiet
rtk dotnet test --project "Explore.Blazor.Client.Tests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventServiceTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Explore.Blazor.Client.Tests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventEditTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Event.Application.UnitTests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/UpdateEventDraftCommandHandlerConcurrencyTests/*" --minimum-expected-tests 1 --no-progress
```

Result: focused draft-create/update tests passed and modified-file LSP diagnostics were clean.

Additional verification after Oracle review fixes for update/publish 409 contracts and MyEvents publish concurrency:

```bash
rtk dotnet build "Explore.Blazor.Client" --configuration Release --verbosity quiet
rtk dotnet build "Explore.Blazor.Client.Tests" --configuration Release --verbosity quiet
rtk dotnet build "Event.API.IntegrationTests" --configuration Release --verbosity quiet
rtk dotnet test --project "Explore.Blazor.Client.Tests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventServiceTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Explore.Blazor.Client.Tests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventEditTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Explore.Blazor.Client.Tests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/MyEventsTests/*" --minimum-expected-tests 1 --no-progress
rtk dotnet test --project "Event.Application.UnitTests" --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/UpdateEventDraftCommandHandlerConcurrencyTests/*" --minimum-expected-tests 1 --no-progress
```

Result: all focused post-Oracle checks passed. Parallel Blazor client/test builds can race on shared Release WASM outputs; rerunning the client build sequentially passed.

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
- `rtk` runs in binlog-only mode for many tests, so exact counts may be unavailable from wrapper output. The direct Blazor client test assembly run above produced exact counts.

## Known Issues / Blockers / Deferred Work

- **Draft contracts are mostly stabilized.** `CreateEventDraftRequestDto` and `UpdateEventDraftRequestDto` are now the public API/OpenAPI/Blazor client create/update contracts and no longer expose session/day/room/agenda graph fields or client-controlled status. Create has Blazor-side idempotency key forwarding, and update has required expected-stamp stale-write protection. Remaining work is centralized ProblemDetails/command-response mapping and any server-side idempotency persistence/replay semantics beyond forwarding the header; the internal graph-shaped `CreateEventRequest` still exists as an Application bridge for handler/import-style graph creation.
- **Session create context is partial.** It now centralizes inherited event defaults, timezone context, locations, rooms, and program sections for create and edit composer guidance. Language assignment is covered by dedicated session-language endpoints plus the existing lookup service; speaker, category, tag, template, custom-field options, and readiness rules still need contract-backed expansion.
- **Event session kind is complete for the current composer slice.** `EventSessionKind` lookup schema, runtime seeds, public endpoint, persisted nullable FK, create/edit picker, and Program Summary chip/metadata rendering are in place.
- **Program summary UI is partially server-backed.** Event Edit now renders grouped server-backed sections/groups/days/items, but agenda/logistics plus speaker/language-specific summary rendering remain pending.
- **Session group management UI is partial.** Event Edit now supports HAL-gated list/create/edit/delete and one-section session assignment/unassignment for program sections/tracks with optional default location/room metadata. HAL policy coverage protects collection delete and assign-session affordances, endpoint-level API tests cover write-route authentication plus authenticated PostgreSQL-backed create/update/delete/assign/unassign persistence, and Application unit tests cover event-local active-slug duplicate rejection including unpublished groups. Reorder, bulk assignment management, and multi-group workflows remain pending.
- **Generated session-group DTOs are incomplete.** Local `EventSessionGroupListModel` is a bridge until NSwag/OpenAPI emits usable concrete DTO properties.
- **Unassign semantics are implemented in the composer service wrapper, but no standalone UI exists for managing multiple group assignments.** Current UI handles one selected primary section.
- **Dirty-tree warning.** The repository contains many unrelated modifications/deletions and is ahead of origin by 31 commits. Do not revert unrelated changes.
- **Phase 7 lifecycle/permission tasks are planning-complete, not runtime-complete.** The future states and permission vocabulary are documented for compatibility, but runtime session review/publish workflows and fine-grained permission policies are deferred until matching endpoints/actions exist.
- **Browser/E2E accessibility verification remains open.** Component-level Create Session announcement coverage exists, but the checklist's browser/E2E keyboard/focus/announcement item still needs an authenticated Aspire-backed Playwright flow.

## Next Immediate Steps

1. Add browser/E2E accessibility coverage for keyboard/focus/announcement behavior in the existing Add Session/session composer flow once the authenticated Aspire-backed Playwright setup is available.
2. Decide whether create draft idempotency needs server-side persistence/replay semantics in addition to the now-forwarded `Idempotency-Key` header.
3. Extend program section/track management with reorder, bulk/multi-group assignment workflows, and richer validation feedback only when UI acceptance criteria exist.
4. Expand session create context with contract-backed speaker, category, tag, template, custom-field options, and readiness rules; decide later whether language lookup should move into that context.
5. Add richer dedicated session fields: speaker management, categories/tags/custom fields/template/image, and readiness warnings when their contracts are explicit.
6. Extend grouped Program Summary UI with agenda/logistics, speakers, language metadata, and richer readiness remediation actions where useful.
7. Fix OpenAPI/NSwag session-group DTO generation so the local bridge model can be deleted.

## Quick Resume Checklist

1. Read this file and `event-creation-progressive-disclosure-tasks.md`.
2. Check `git status` and avoid unrelated dirty-tree changes.
3. If touching session group UI, inspect:
   - `Explore.Blazor.Client/Pages/Events/Sessions/CreateSession.razor`
   - `Explore.Blazor.Client/Pages/Events/Sessions/EditSession.razor`
   - `Explore.Blazor.Client/Services/EventService.cs`
   - `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`
   - `Explore.Blazor.Client/Models/EventSessionGroups/EventSessionGroupListModel.cs`
4. If touching event draft save/lifecycle, inspect `EventService.cs`, `EventEdit.razor(.cs)`, `MyEvents.razor.cs`, `AppJsonSerializerContext.cs`, `EventEditTests.cs`, and `EventServiceTests.cs`.
5. Run targeted builds/tests listed above before handoff.
6. Consult Oracle for any changes crossing HAL authorization, create/update semantics, or event/session/group assignment behavior.

## Rejected Direction Grep

Use this periodically:

```bash
rg -n "ParentEventId|ChildEvents|subevent|child-event|old add-child-event|SessionEditorPanel|SessionEditorWorkflow|ScheduleTimelineComposer|PopulateSchedulingOnRequest" \
  Explore.Blazor.Client/Pages/Events dev/active/event-creation-progressive-disclosure \
  --glob '!**/bin/**' --glob '!**/obj/**'
```

Expected: matches only in historical docs/obsolete target lists or deleted-file status, not active Create/Edit Event shell code.
