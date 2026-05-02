<!-- ABOUTME: Implementation plan for the adaptive single-submit Create Event scheduling experience. -->
<!-- ABOUTME: Defines the Clean Architecture path for one create-graph API and progressive scheduling UI. -->

# Plan: Adaptive Single-Submit Event Create

> **Last Updated: 2026-05-02**

## Executive Summary

Refactor Create Event so the default experience stays optimized for the 99% case: one event, one session, one page, one submit button. Advanced scheduling complexity should stay hidden until the organizer explicitly needs multi-session, multi-day, rooms, or agenda detail.

The UI must stop creating an event and then performing follow-up session calls. Instead, the Blazor page submits one create graph request to the canonical Event API create endpoint. The Application handler owns the creation order inside one transaction: event, event days, rooms, sessions, event-level agenda items, language/aspect rows, category/tag assignment rows when supported, and any template/property rows that are part of the create graph.

Backward compatibility is not a constraint for this development-stage refactor. The target is one canonical `POST /api/event` contract named `CreateEventRequest`, one MediatR `CreateEventCommand`, and no long-term parallel create endpoint.

## Current State Analysis

### Confirmed Existing Files

| Area | File | Current Role |
|---|---|---|
| Create page | `Explore.Blazor.Client/Pages/Events/CreateEvent.razor` | Single-page event create UI with current Luma-inspired layout and an inline scheduling expansion panel. |
| Create page logic | `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs` | Large stateful code-behind that builds `CreateEventDto`, calls event create, then updates/creates sessions after event creation. |
| Create page styling | `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.css` | Page-specific styles for current layout and scheduling panel. |
| Blazor service | `Explore.Blazor.Client/Services/EventService.cs` | Exposes `CreateEventAsync(CreateEventDto)` only; generated client has `CreateEventWithSessionsAsync` but this service does not wrap it. |
| Generated API client | `Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Contains generated methods for `CreateEventAsync` and `CreateEventWithSessionsAsync`. |
| Event controller | `Explore.API/Controllers/EventController.cs` | Has `POST api/event` using `CreateEventDto` and `POST api/event/with-sessions` using `CreateEventWithSessionsDto`. |
| Route names | `Explore.API/Hateoas/RouteNames.cs` | Defines `CreateEvent` and `CreateEventWithSessions`. |
| Current broad DTO | `Explore.Application/DTOs/Event/CreateEventDto.cs` | Has core event fields, appearance/template/series/policy fields, and inline days/rooms/agenda, but no sessions collection. |
| Current single-transaction DTO | `Explore.Application/DTOs/Event/CreateEventWithSessionsDto.cs` | Has event fields and sessions only; no days, rooms, agenda, appearance, template, policy, or category/tag assignment. |
| Current nested session DTO | `Explore.Application/DTOs/Event/CreateEventSessionForEventDto.cs` | Has session title/time/location/capacity/registration/language/aspect fields, but no room/day/agenda nesting. |
| Current single-transaction command | `Explore.Application/Features/Events/Requests/Commands/CreateEventWithSessionsCommand.cs` | MediatR command carrying `CreateEventWithSessionsDto`. |
| Current single-transaction handler | `Explore.Application/Features/Events/Handlers/Commands/CreateEventWithSessionsCommandHandler.cs` | Creates event and sessions inside `IUnitOfWork.ExecuteInTransactionAsync`; does not create days, rooms, agenda, categories, tags, or template properties. |
| Current single-transaction validator | `Explore.Application/DTOs/Event/Validators/CreateEventWithSessionsDtoValidator.cs` | Validates event and session basics; does not validate days, rooms, agenda, or cross-record schedule graph rules. |
| Current broad create handler | `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` | Creates event, default session, optional inline days/rooms/agenda; cannot pass newly-created room IDs into agenda items from the current UI. |
| Unit tests | `Event.Application.UnitTests/Features/Events/Commands/CreateEventCommandHandlerTests.cs` | Existing handler coverage for current broad create path. |
| Validator tests | `Event.Application.UnitTests/Features/Events/Validators/CreateEventDtoValidatorTests.cs` | Existing validation coverage for current broad create path. |
| UI tests | `Explore.Blazor.Client.Tests/Pages/Event/CreateEventTests.cs` | Existing Create Event component tests expecting `CreateEventAsync(CreateEventDto)`. |
| Event service tests | `Explore.Blazor.Client.Tests/Services/EventServiceTests.cs` | Existing service test surface. |

### Current Behavioral Problems

1. Create Event currently submits `CreateEventDto` via `EventService.CreateEventAsync`, then performs session update/create calls from the UI after event creation.
2. Partial persistence is possible: event creation can succeed while default-session update or additional-session creation fails.
3. `CreateEventWithSessionsDto` is close to the desired transaction model but too narrow for the product requirement because it excludes days, rooms, event-level agenda, template fields, registration policy, appearance fields, and create graph references.
4. The inline scheduling panel stores rooms by UI index, but `InlineEventAgendaItemDto` expects `Guid? RoomId`; `PopulateInlineSchedulingOnDto()` currently sends `RoomId = null`, so newly-created rooms cannot be attached to agenda items in the same request.
5. The Create page exposes data-model concepts too directly. Users must reason about event days, rooms, agenda items, default sessions, and extra sessions instead of describing the event in progressively revealed organizer language.
6. Categories/tags can be selected in the UI but are not persisted because the current API does not expose event-category/event-tag assignment endpoints from this create flow.
7. Current tests assert the old service call shape and need to move to the new create graph contract.

### Research Inputs

Context7 documentation research for MudBlazor supports explicit labels, validation-bound form controls, `MudForm`/`MudTextField` validation patterns, dialog APIs via `IDialogService.ShowAsync`, and MudBlazor v9 patterns. Context7 documentation research for ASP.NET Core Blazor supports `EditForm` with `EditContext`, `ValidationMessageStore`, `OnValidSubmit` or explicit `OnSubmit` validation, nested object validation, and field-change subscriptions with disposal.

Tavily UX research supports progressive disclosure for complex forms: start with the simplest common path, reveal advanced fields only when selected, preserve entered data when sections collapse, avoid placeholder-only labels, keep related fields grouped, provide accessible dynamic content announcements, and offer keyboard-accessible alternatives to drag/drop. Tavily outbox/idempotency research reinforces the same rule as the repo outbox skill: database state and outgoing side-effect messages must be persisted atomically, while dispatchers and consumers must tolerate at-least-once delivery. Multi-step wizards are not appropriate here because the product requirement explicitly requires one page and one submit button.

## Proposed Future State

### Product UX

The page remains one Create Event page with one primary submit button and one draft submit affordance. The default visible scheduling UI is a compact single-session block embedded in the existing page:

| User Need | Default UI Behavior |
|---|---|
| Single-session event | Show title, description, event image, publisher, date/time, format, location/meeting link, registration/capacity, and optional language fields. |
| Needs another session | User enables “This event has multiple sessions”; show session summary cards and an add/edit session drawer/dialog. |
| Needs multiple days | User enables “This event spans multiple days”; derive days from sessions by default and allow optional day labels/descriptions. |
| Needs rooms | User selects an in-person or hybrid location and enables “Use rooms”; show room chips/cards and assign rooms from session/agenda editors. |
| Needs agenda | User enables “Add agenda”; show lightweight event-level agenda rows/cards scoped to selected day, with optional room/kind fields only when relevant. |

Advanced sections must be progressive, not hidden dead-ends. Collapsed sections preserve their data until the organizer explicitly clears them. Every reveal has clear copy explaining why the fields matter.

### API And Application Architecture

Create one canonical create graph command for event creation. This is an application-level transactional write graph, not a new strict DDD aggregate boundary. Avoid `Aggregate` naming so future contributors do not infer that `Event`, `EventSession`, `LocationRoom`, `EventAgendaItem`, category/tag assignments, and template instantiation are one domain aggregate.

| New Artifact | Purpose |
|---|---|
| `CreateEventRequest` | Root API contract for all event create data. |
| `CreateEventSessionRequest` | Nested session contract with optional client-side temporary key for linking related request records before database IDs exist. |
| `CreateEventDayRequest` | Nested day contract with optional client-side temporary key and date metadata. |
| `CreateEventRoomRequest` | Nested room contract with optional client-side temporary key and existing-location metadata. |
| `CreateEventAgendaItemRequest` | Nested event-level agenda item contract linking by temporary day/room keys instead of database IDs. |
| `CreateEventCommand` | MediatR request returning `BaseCommandResponse<Guid>`. |
| `CreateEventCommandHandler` | Transactional orchestration for the create graph. |
| `CreateEventRequestValidator` | Root and graph validation, manually instantiated in the handler. |

Temporary client keys should be strings or GUIDs generated by the UI model and used only inside the request. The handler maps them to persisted entity IDs after each creation step. This avoids exposing database-order concerns to the UI while allowing event-level agenda items to point at newly-created rooms or days.

### Create Graph Policy

| Concern | Decision |
|---|---|
| Endpoint | Replace `POST /api/event` with the new `CreateEventRequest` contract. |
| Deprecated endpoint | Delete or retire `POST /api/event/with-sessions`; do not keep a third long-term create endpoint. |
| Route name | Keep `RouteNames.CreateEvent` for the canonical create action when possible. Remove `RouteNames.CreateEventWithSessions` if the endpoint is deleted. |
| Contract naming | Use `CreateEventRequest`, nested `CreateEvent*Request` types, `CreateEventCommand`, and `CreateEventRequestValidator`. |
| Categories/tags | Persist selected category/tag assignments in the create graph if the existing repositories support it; otherwise hide those controls from Create Event. Selected-but-not-persisted state is not allowed. |
| Agenda | Use event-level agenda only in Create Event v1. |
| Session-level agenda | Defer to session editor/detail flows after create. |
| EventDay rows | Auto-create `EventDay` rows for each distinct local date when submitted sessions span more than one local date. For single-day events, create day rows only when the organizer explicitly enabled day metadata. |
| Rooms | Create `LocationRoom` rows only under an already-selected existing location. |
| Location creation | Out of scope for this phase. Use separate location creation UX later. |
| Template custom properties | Include stable existing event template instantiation. Defer session-template expansion unless existing code is already stable and directly required by the Create page. |

### Transaction Creation Order

The handler should execute inside `IUnitOfWork.ExecuteInTransactionAsync<T>` and create records in this order:

1. Resolve publisher actor and validate organization/group publishing rules.
2. Validate all lookup IDs and cross-record graph rules before writing.
3. Create the `Event` root with computed first/last session fields and appearance/template/policy fields.
4. Create deterministic `EventDay` rows for distinct multi-day session local dates, applying optional organizer-provided metadata; create single-day day rows only when explicit day metadata is enabled.
5. Create `LocationRoom` rows, storing a temporary-key to persisted-ID map.
6. Create `EventSession` rows, assigning event day and room IDs from maps when supplied.
7. Create session aspects and language join rows.
8. Create event-level agenda items, assigning event day and room IDs from maps when supplied.
9. Create event category/tag assignment rows when support exists; otherwise the UI must not send category/tag selections.
10. Instantiate stable event template custom properties if template selection is present.
11. Return the created event ID only after the full graph is successfully committed.

The transaction delegate must remain idempotent. Do not send emails, external HTTP requests, storage operations, or broker publishes inside the transaction delegate. Generate request temporary-key maps before writing or deterministically inside the handler. If slug generation is part of create, collision and retry behavior must be explicit. Any future external side effect must write an outbox message in the same database transaction and rely on the existing polling outbox processor; dispatchers must be idempotent because delivery is at-least-once.

## Implementation Phases By Clean Architecture Layer

### Phase 1: Application Contract And Validation

Goal: Define the canonical create request and graph validation rules.

Tasks:

| Task | Files | Acceptance Criteria |
|---|---|---|
| Add `CreateEventRequest` DTOs | `Explore.Application/DTOs/Event/` | DTOs carry event core fields, sessions, optional days, optional rooms, event-level agenda, appearance/template/policy fields, category/tag IDs when supported, and temporary keys for intra-request linking. |
| Add `CreateEventRequestValidator` | `Explore.Application/DTOs/Event/Validators/` | Validator covers current event/session rules plus nested day/room/event-agenda rules, cross-record references, time ranges, room-location consistency, duplicate temporary keys, and category/tag scope rules. |
| Lock category/tag scope | Application DTO/handler tests | Persist category/tag assignments in the same command if repository support exists; otherwise remove/hide those UI controls. No selected-but-unpersisted UI state remains. |

Design constraints:

1. Application must not reference `ExploreDbContext`, API, or Blazor.
2. Validators are manually instantiated in handlers, not injected through DI.
3. Repositories return entities; mapping stays in Application.
4. Use `CancellationToken` through all async calls.
5. Treat `CreateEventRequest` as an application write graph, not a domain aggregate boundary.

### Phase 2: Application Handler

Goal: Move all create orchestration from Blazor into one Application command handler.

Tasks:

| Task | Files | Acceptance Criteria |
|---|---|---|
| Replace current `CreateEventCommand` contract | `Explore.Application/Features/Events/Requests/Commands/` | Command returns `BaseCommandResponse<Guid>` and carries `CreateEventRequest`. No parallel aggregate-named command remains long term. |
| Replace current `CreateEventCommandHandler` orchestration | `Explore.Application/Features/Events/Handlers/Commands/` | Handler creates the full graph in one `IUnitOfWork.ExecuteInTransactionAsync<T>` call and returns the event ID. |
| Add graph mapping helpers inside handler | Handler-local private methods | Temporary day/session/room keys are converted into persisted IDs without leaking persistence sequencing to the UI. |
| Remove or retire client-side choreography | Existing create handler/service call path | Create page no longer updates default session or creates additional sessions after event creation. |

Handler structure should stay explicit and testable with private methods such as `ValidateRequestAsync`, `ResolvePublisherActorAsync`, `BuildEventEntity`, `CreateEventDaysAsync`, `CreateRoomsAsync`, `CreateSessionsAsync`, `CreateSessionAspectsAsync`, `CreateSessionLanguagesAsync`, `CreateEventAgendaItemsAsync`, `CreateCategoryAndTagAssignmentsAsync`, and `InstantiateTemplatePropertiesAsync`. Do not move this orchestration into domain services prematurely.

### Phase 3: Persistence And Domain Fit

Goal: Add only persistence changes that are required to support the create graph correctly.

Tasks:

| Task | Files | Acceptance Criteria |
|---|---|---|
| Verify repository capabilities | `Explore.Application/Contracts/Persistence/` and `Explore.Persistence/Repositories/` | Needed repositories can add event days, rooms, sessions, event-level agenda items, language rows, aspects, categories, and tags without exposing `IQueryable`. |
| Add missing repository methods only if required | Application contracts and Persistence implementations | Methods are entity-based, cancellation-aware, tenant-safe, and covered by unit/integration tests. |
| Add EF migration only if category/tag or schema gaps require it | `Explore.Persistence/Migrations/` | Migration is small, focused, PascalCase named, and does not edit existing applied migrations. |

### Phase 4: API Surface

Goal: Expose a single canonical create endpoint through the existing API conventions.

Tasks:

| Task | Files | Acceptance Criteria |
|---|---|---|
| Replace `POST api/event` contract | `Explore.API/Controllers/EventController.cs` | Endpoint is `[Authorize]`, uses `RouteNames.CreateEvent`, accepts `CreateEventRequest`, returns `CreatedAtRoute(RouteNames.GetEventById, ...)`, and includes response metadata. |
| Retire `POST api/event/with-sessions` | `Explore.API/Controllers/EventController.cs` and `Explore.API/Hateoas/RouteNames.cs` | There is one canonical create path. `CreateEventWithSessions` endpoint and route name are removed if no longer used. |
| Regenerate NSwag client | `Explore.Blazor.Client/Clients/EventApiClient.g.cs` and serializer context if needed | Generated client exposes the canonical create method and compiles. |

Security constraints:

1. Write endpoint remains `[Authorize]`.
2. Authorization remains enforced server-side by API/MediatR behavior; Blazor checks are UX only.
3. Organization/group publishing rules stay in the handler/authorization path, not only in the UI.
4. The create command must participate in the project MediatR authorization model where applicable; at minimum, publisher actor rules are enforced server-side.
5. Create affordances in the UI should follow HAL/action policy patterns where applicable and must not rely on local role checks alone.

### Phase 5: Blazor UI Model And Service

Goal: Keep the page visually familiar while replacing scheduling inputs with an adaptive organizer-oriented model.

Tasks:

| Task | Files | Acceptance Criteria |
|---|---|---|
| Add service wrapper | `Explore.Blazor.Client/Services/EventService.cs` | Service exposes the canonical `CreateEventAsync(CreateEventRequest request)` service-layer method around the generated NSwag client. |
| Add form view model | `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs` or extracted file | UI state uses organizer concepts: single session, multi-session enabled, multi-day enabled, rooms enabled, agenda enabled. |
| Add request mapper | Create page code-behind or dedicated client mapper | Mapper produces `CreateEventRequest` with temporary keys and no post-create API choreography. |
| Replace scheduling panel | `Explore.Blazor.Client/Pages/Events/CreateEvent.razor` | Default UI shows one compact session block; advanced scheduling sections are hidden until explicitly enabled. |
| Preserve current non-scheduling UI | Create page files | Existing publisher, title, image, appearance, location, format, registration, template, and option flows remain unless they conflict with create graph submission. |
| Improve accessibility | Razor and CSS | Inputs have explicit labels, dynamic section reveals are announced, icon buttons have aria labels, keyboard users can manage sessions/agenda, and mobile layout remains usable. |
| Align CSS | `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.css` | Remove unnecessary `::deep`, avoid directional CSS where logical properties are available, and use design tokens/wrappers where practical. |

### Phase 6: Tests And Verification

Goal: Prove the create graph path is atomic and the adaptive UI submits one request.

Tasks:

| Task | Files | Acceptance Criteria |
|---|---|---|
| Add handler unit tests | `Event.Application.UnitTests/Features/Events/Commands/` | Tests cover single-session default, multi-session, multi-day, rooms, event-level agenda with room/day links, category/tag policy, validation failures, and transaction failure behavior. |
| Add validator tests | `Event.Application.UnitTests/Features/Events/Validators/` | Tests cover nested validation and graph references. |
| Add API integration tests | `Event.API.IntegrationTests/` | Tests cover `POST /api/event` success, `CreatedAtRoute`, unauthorized request, invalid temp-key validation response, advanced graph persistence/read-back, and removal of `/api/event/with-sessions` if deleted. |
| Update UI tests | `Explore.Blazor.Client.Tests/Pages/Event/CreateEventTests.cs` | Tests assert one service call for default event, advanced sections reveal on toggles, and no session post-create calls occur. |
| Update service tests | `Explore.Blazor.Client.Tests/Services/EventServiceTests.cs` | Tests cover canonical create endpoint wrapper success/failure handling. |
| Run architecture tests | `Event.Architecture.Tests` | Clean Architecture, CQRS, and Blazor client architecture checks pass. |

## Detailed UX Requirements

### Default Single-Session Path

Acceptance criteria:

1. A user can create a normal single-session event without seeing day, room, or agenda terminology.
2. Date/time inputs still populate one persisted `EventSession`.
3. The event root `FirstSessionDate`, `LastSessionDate`, `FirstSessionStartUtc`, `LastSessionStartUtc`, and timezone fields are computed server-side from submitted session data.
4. The submit button creates the event and session atomically.
5. Draft and publish behavior is explicit and cannot accidentally remain draft due to default DTO state.

### Advanced Scheduling Path

Acceptance criteria:

1. Multi-session mode reveals session cards and an add/edit session interaction.
2. Multi-day mode deterministically creates day records for distinct local session dates and lets organizers optionally add labels/descriptions/banner copy.
3. Room mode is only available for in-person or hybrid events with an existing location context.
4. Agenda mode lets organizers add event-level agenda rows without needing to understand database entities.
5. Agenda rows can link to newly-created days and rooms through temporary keys.
6. Collapsing an advanced section does not silently delete data; clearing data is an explicit action.
7. Dynamic sections remain screen-reader and keyboard accessible.

## Risk Assessment And Mitigation

| Risk | Impact | Mitigation |
|---|---|---|
| Create request becomes a god DTO | Hard to maintain and validate | Include only fields visible/stable in Create Event now, keep nested request types small, and explicitly document out-of-scope future fields. |
| Handler becomes orchestration-heavy | Hard to test | Keep domain creation order explicit, use small private methods, and cover each branch with unit tests. |
| Temporary-key graph references are invalid | Broken room/day/agenda linkage | Validate duplicate/missing temp keys before writing and test invalid reference cases. |
| Partial persistence remains | User sees broken events | Remove UI post-create choreography and require all scheduling creation in one transaction. |
| Progressive disclosure hides important options | Users cannot find advanced features | Use clear toggles/cards with explanatory copy and visible summaries once enabled. |
| Accessibility regressions in dynamic sections | Poor keyboard/screen-reader UX | Use explicit labels, aria labels, live-region announcements, focus management, and bUnit accessibility-oriented tests where feasible. |
| NSwag/generated client drift | Build or runtime failures | Regenerate after API contract changes and update serializer context/tests in the same phase. |
| Category/tag scope expands unexpectedly | Plan churn | Make this a blocking implementation decision: persist through existing repositories if feasible, otherwise hide controls. |
| Retried transaction changes behavior | Duplicate or inconsistent data | Keep transaction delegate idempotent, avoid external side effects, use deterministic mapping, and write future side-effect messages through transactional outbox only. |

## Success Metrics

1. Default single-session event creation requires one API write call from Blazor.
2. Multi-session, multi-day, room, event-level agenda, and supported category/tag assignments are persisted atomically through one API request.
3. Create page no longer calls `GetSessionsByEventAsync`, `UpdateSessionAsync`, or `CreateSessionAsync` during submit.
4. Event-level agenda items can reference rooms created in the same request.
5. Create page tests cover default and advanced reveal flows.
6. Application tests cover transaction graph creation and validation failures.
7. API integration tests verify the canonical endpoint and retirement of the old with-sessions endpoint.
8. Architecture tests and project-specific test suites pass.

## Resources And Dependencies

1. Repo conventions in `CLAUDE.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/SECURITY.md`, `docs/BLAZOR.md`, `docs/ACCESSIBILITY.md`, and `docs/DESIGN_SYSTEM.md`.
2. Existing unit test projects: `Event.Application.UnitTests`, `Explore.Blazor.Client.Tests`, and `Event.Architecture.Tests`.
3. Existing generated client pipeline for `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
4. Existing transaction abstraction `IUnitOfWork.ExecuteInTransactionAsync<T>`.
5. Existing outbox pattern for future side effects: outbox messages are written transactionally and dispatched later by the polling processor with idempotent dispatchers.
6. Existing create handler logic that can be mined but should not remain split across UI calls.

## Effort Estimate

| Phase | Estimate |
|---|---|
| Application request DTO and validator | M |
| Application handler and tests | L |
| Repository/persistence gap closure | M, or L if category/tag schema work is included |
| API endpoint and NSwag regeneration | S |
| Blazor adaptive UI refactor | L |
| API/UI/service tests and verification | M |

Overall estimate: **L**. The primary complexity is not visual design; it is the create graph, validation, endpoint replacement, and preserving a simple default user experience while supporting advanced scheduling.

## Potential Risks & Unknowns

1. Category/tag persistence is a blocking decision: persist in create graph if existing repository support is sufficient; otherwise hide the controls.
2. Existing repositories may need small add/query methods for graph validation; confirm during implementation without leaking `IQueryable`.
3. Template custom-property instantiation may require careful ordering; include stable event-template instantiation and defer session-template expansion unless already reliable.
4. The exact generated-client command for NSwag regeneration should be confirmed from repo scripts before executing implementation.
5. API integration test fixtures may need authenticated-user setup work to cover `POST /api/event` realistically.
