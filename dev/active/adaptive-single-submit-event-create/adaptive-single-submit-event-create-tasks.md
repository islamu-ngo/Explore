<!-- ABOUTME: Task breakdown for the adaptive single-submit Create Event implementation. -->
<!-- ABOUTME: Tracks implementation-ready work items across Application, API, Blazor UI, and tests. -->

# Tasks: Adaptive Single-Submit Event Create

> **Last Updated: 2026-05-02**

## Phase 0: Locked Decisions And Setup

- [ ] Confirm category/tag repository support for create graph assignment.
  - Acceptance: selected category/tag controls are either persisted by `CreateEventRequest` or hidden from Create Event before implementation merges.
- [ ] Confirm endpoint replacement path.
  - Acceptance: `POST /api/event` is the only canonical create endpoint and `/api/event/with-sessions` is removed or retired.
- [ ] Confirm event-level agenda-only scope.
  - Acceptance: Create Event v1 does not expose session-level agenda.

## Phase 1: Application DTOs And Validation

- [ ] Add `CreateEventRequest` under `Explore.Application/DTOs/Event/`.
  - Acceptance: DTO includes existing required event fields from `CreateEventDto`, fields currently missing from `CreateEventWithSessionsDto`, and nested collections for sessions, days, rooms, and agenda.
- [ ] Add nested session/day/room/event-agenda request DTOs under `Explore.Application/DTOs/Event/`.
  - Acceptance: nested DTOs include temporary keys for intra-request linking and do not require persisted IDs for records created in the same request.
- [ ] Add `CreateEventRequestValidator` under `Explore.Application/DTOs/Event/Validators/`.
  - Acceptance: validator manually supports event field rules, session time rules, lookup existence rules, organization/group mutual exclusion, room-location consistency, agenda time ranges, duplicate temporary keys, and missing reference detection.
- [ ] Add validator unit tests.
  - Acceptance: valid default single-session request passes; invalid times, missing sessions, invalid temp references, duplicate temp keys, and room/location mismatches fail with useful messages.

## Phase 2: Application Command Handler

- [ ] Replace `CreateEventCommand` to carry `CreateEventRequest`.
  - Acceptance: command returns `BaseCommandResponse<Guid>` and no parallel aggregate-named command remains long term.
- [ ] Replace `CreateEventCommandHandler` orchestration.
  - Acceptance: handler manually instantiates validator, resolves publisher actor, computes event summary dates server-side, and creates all submitted records inside `IUnitOfWork.ExecuteInTransactionAsync<T>`.
- [ ] Split handler orchestration into private methods.
  - Acceptance: handler has explicit methods for validation, publisher resolution, event build, days, rooms, sessions, aspects, languages, event-level agenda, category/tag assignment, and template instantiation.
- [ ] Implement temporary-key mapping inside the handler.
  - Acceptance: event-level agenda items can reference rooms/days created earlier in the same request.
- [ ] Implement server-side draft/publish handling.
  - Acceptance: submitted status is explicit and normal publish does not accidentally remain draft due to DTO defaults.
- [ ] Add idempotency and outbox guardrails.
  - Acceptance: transaction delegate performs DB work only, slug collision behavior is explicit, and future side effects are documented as transactional outbox messages.
- [ ] Add handler unit tests for default create.
  - Acceptance: one event and one session are created; no UI follow-up calls are required by design.
- [ ] Add handler unit tests for advanced create.
  - Acceptance: multi-session, multi-day, rooms, agenda, session languages, and aspects persist in the correct order.
- [ ] Add handler unit tests for transaction failure.
  - Acceptance: simulated failure after event creation prevents partial graph persistence.

## Phase 3: Persistence Gaps

- [ ] Audit repository contracts needed by the create graph handler.
  - Acceptance: every write needed by the handler has an entity-returning or entity-accepting repository method and no `IQueryable` leaks.
- [ ] Add missing repository methods only where required.
  - Acceptance: methods are cancellation-aware and tenant/soft-delete-safe.
- [ ] Add focused EF migration only if schema gaps are found.
  - Acceptance: migration is small, PascalCase named, and covered by persistence verification.

## Phase 4: API And Generated Client

- [ ] Add or replace the Event create endpoint in `Explore.API/Controllers/EventController.cs`.
  - Acceptance: `POST /api/event` is `[Authorize]`, uses `RouteNames.CreateEvent`, accepts `CreateEventRequest`, consumes JSON, returns `BaseCommandResponse<Guid>`, and returns `CreatedAtRoute` to event detail on success.
- [ ] Update `Explore.API/Hateoas/RouteNames.cs`.
  - Acceptance: route name exists for the canonical create endpoint and obsolete route names are removed if endpoints are deleted.
- [ ] Remove obsolete create endpoint(s) if the replacement strategy chooses deletion.
  - Acceptance: `/api/event/with-sessions` is removed or retired and there is one canonical create path used by the UI.
- [ ] Regenerate `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
  - Acceptance: generated client exposes the create graph API and builds.
- [ ] Update serializer context if required.
  - Acceptance: AOT/source-generated JSON paths compile with the new DTOs.

## Phase 5: Blazor Service Layer

- [ ] Update `IEventService` in `Explore.Blazor.Client/Services/EventService.cs`.
  - Acceptance: service exposes the create graph method and no longer requires create-page code to orchestrate session writes.
- [ ] Implement canonical create wrapper in `EventService`.
  - Acceptance: wrapper calls the generated `POST /api/event` client method, logs failures consistently, and returns `BaseCommandResponseOfGuid?` or the project-standard response type.
- [ ] Update `Explore.Blazor.Client.Tests/Services/EventServiceTests.cs`.
  - Acceptance: tests cover success, API failure, and exception logging behavior for create graph submission.

## Phase 6: Adaptive Create Page UI

- [ ] Introduce an adaptive scheduling view model for `CreateEvent.razor.cs`.
  - Acceptance: model represents default single-session state plus explicit flags for multi-session, multi-day, rooms, and agenda.
- [ ] Add create request mapper.
  - Acceptance: mapper produces one `CreateEventRequest` with temporary keys and all currently supported non-scheduling event fields.
- [ ] Replace submit choreography in `HandleSubmit`.
  - Acceptance: `HandleSubmit` performs validation, uploads/prepares assets, maps to `CreateEventRequest`, calls one service method, and navigates after success; it does not call session get/update/create methods.
- [ ] Replace the current “Scheduling (Days, Rooms & Agenda)” expansion panel.
  - Acceptance: default page shows only the single-session inputs; advanced scheduling appears via clear toggles/cards.
- [ ] Add multi-session summary cards and add/edit interaction.
  - Acceptance: users can add, edit, duplicate, delete, and reorder sessions without seeing database concepts.
- [ ] Add multi-day controls.
  - Acceptance: days can be derived from sessions and optionally labeled/described.
- [ ] Add room controls.
  - Acceptance: rooms are available only when location context makes sense and can be assigned to sessions/agenda via temporary keys.
- [ ] Add agenda controls.
  - Acceptance: event-level agenda rows can be linked to day/room without persisted IDs; session-level agenda is not exposed in Create Event v1.
- [ ] Preserve current strengths of the page.
  - Acceptance: publisher, title, image, appearance, format, location/meeting link, registration, templates, description, and options remain available unless explicitly superseded.

## Phase 7: Accessibility And Styling

- [ ] Add explicit labels to scheduling inputs.
  - Acceptance: no scheduling input relies on placeholder-only labeling.
- [ ] Add accessible icon button labels.
  - Acceptance: add/edit/delete/reorder/close buttons have discernible names.
- [ ] Add dynamic section announcements.
  - Acceptance: enabling advanced scheduling sections announces the change for assistive technology.
- [ ] Ensure keyboard support for scheduling operations.
  - Acceptance: session/agenda add, edit, delete, duplicate, and reorder flows work without a mouse.
- [ ] Clean scoped CSS for the create page.
  - Acceptance: unnecessary `::deep` usage is reduced, logical CSS properties are preferred where appropriate, and styling follows design-system tokens/wrappers where practical.

## Phase 8: Verification

- [ ] Add API integration tests under `Event.API.IntegrationTests`.
  - Acceptance: tests cover `POST /api/event` single-session `201 Created`, `CreatedAtRoute`, unauthorized request, invalid temp-key validation response, multi-session + rooms + agenda read-back, and `/api/event/with-sessions` removal if deleted.

- [ ] Update `Explore.Blazor.Client.Tests/Pages/Event/CreateEventTests.cs`.
  - Acceptance: tests assert default single-session submit uses one create graph service call and advanced toggles reveal expected controls.
- [ ] Run Application unit tests for event create.
  - Suggested command: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Events`
- [ ] Run Blazor client tests for Create Event and EventService.
  - Suggested command: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~CreateEvent|FullyQualifiedName~EventService`
- [ ] Run architecture tests.
  - Suggested command: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Run API integration tests.
  - Suggested command: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] Run the repo canonical check command if available.
  - Suggested command: follow `/check` or `AGENTS.md` verification policy before opening PR.

## Definition Of Done

- [ ] Create Event default path is visually simple and single-session-first.
- [ ] Advanced scheduling is progressively disclosed and accessible.
- [ ] Blazor submits exactly one create request for event creation.
- [ ] API/Application creates the full submitted event graph transactionally through `POST /api/event`.
- [ ] No selected scheduling room/agenda data is silently dropped.
- [ ] Tests cover default, advanced, validation, and transaction failure scenarios.
- [ ] Build and architecture verification pass.
