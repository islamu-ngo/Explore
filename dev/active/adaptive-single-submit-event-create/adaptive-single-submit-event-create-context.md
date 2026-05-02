<!-- ABOUTME: Context notes for the adaptive single-submit Create Event implementation plan. -->
<!-- ABOUTME: Captures verified current-state findings, constraints, and research inputs for future implementation. -->

# Context: Adaptive Single-Submit Event Create

> **Last Updated: 2026-05-02**

## User Intent

The Create Event page is considered strong overall, except for the input methods around sessions, agenda, days, and rooms. The desired experience is adaptive: default to the 99% case of a single-session event, hide data-model complexity, and reveal additional inputs only when more scheduling complexity is needed.

Hard requirements:

1. One page.
2. One form.
3. One submit button for the create action.
4. No multi-step wizard.
5. API/Application owns all database creation order and ID passing.
6. UI does not create an event and then call additional endpoints to finish sessions/agenda.
7. Backward compatibility is not required because the project is in development mode.
8. Follow repo conventions, Clean Architecture, CQRS/MediatR, EF Core rules, accessibility, design-system guidance, and enterprise maintainability standards.

## Repo Conventions To Preserve

1. `CLAUDE.md` requires repositories to return entities, validators to be manually instantiated, int lookup IDs, Guid IDs for aggregate-root-style entities, write endpoints authorized, and HAL links as the source of action affordances.
2. `docs/ARCHITECTURE.md` defines Clean Architecture with Domain inward, Application owning CQRS and validation, Persistence implementing repositories, API/Blazor as composition roots, and BFF-safe Blazor service usage.
3. `docs/DOMAIN.md` positions `Event` as the parent program/container and `EventSession` as the scheduled child. Event data has universal fields, typed aspects, and custom-property extensions.
4. `docs/SECURITY.md` requires server-side authorization for writes; Blazor checks are UX only.
5. `docs/BLAZOR.md`, `docs/ACCESSIBILITY.md`, and `docs/DESIGN_SYSTEM.md` require MudBlazor v9 conventions, explicit labels, accessible icon buttons, keyboard/focus behavior, design tokens/wrappers, and avoidance of ad hoc styling where possible.

## Verified Current API Paths

`Explore.API/Controllers/EventController.cs` has two create endpoints:

1. `POST api/event`, route name `RouteNames.CreateEvent`, accepts `CreateEventDto`, and sends `CreateEventCommand`.
2. `POST api/event/with-sessions`, route name `RouteNames.CreateEventWithSessions`, accepts `CreateEventWithSessionsDto`, and sends `CreateEventWithSessionsCommand`.

Both are `[Authorize]` and return `CreatedAtRoute(RouteNames.GetEventById, ...)` on success.

`Explore.API/Hateoas/RouteNames.cs` defines both `CreateEvent` and `CreateEventWithSessions`.

Target decision: replace `POST api/event` with the new canonical `CreateEventRequest` contract, keep `RouteNames.CreateEvent` when possible, and delete or retire `POST api/event/with-sessions`. Do not keep three long-term create paths.

## Verified Current Application Contracts

`Explore.Application/DTOs/Event/CreateEventDto.cs` is broad for event fields and includes inline days, rooms, and agenda items. It does not include sessions as a collection. It includes appearance, template, series, and registration policy fields.

`Explore.Application/DTOs/Event/CreateEventWithSessionsDto.cs` supports single-transaction creation of event + sessions only. It excludes days, rooms, agenda, appearance, template, registration policy, categories, tags, and custom create graph references.

`Explore.Application/DTOs/Event/CreateEventSessionForEventDto.cs` supports session title, description, start/end, location, max attendees, registration mode, price/currency, Islamic aspect, and language IDs. It does not support room, event day, agenda, speaker, session template, featured image, or sort-order fields.

`Explore.Application/DTOs/Event/Validators/CreateEventWithSessionsDtoValidator.cs` validates the current event/session shape but not days, rooms, agenda, room-location consistency, temporary graph references, or agenda membership.

## Verified Current Handlers

`Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` creates an event, a default session, optional inline days, optional inline rooms, and optional inline event agenda items. Current inline scheduling cannot correctly connect newly-created rooms to agenda items from the UI because agenda items receive `RoomId = null`.

`Explore.Application/Features/Events/Handlers/Commands/CreateEventWithSessionsCommandHandler.cs` manually instantiates the validator, resolves the actor, computes first/last session fields from submitted sessions, creates `Event`, updates featured image actor, creates sessions, session Islamic aspect, and session languages inside `IUnitOfWork.ExecuteInTransactionAsync`. It does not create days, rooms, agenda, categories, tags, template custom properties, or appearance fields.

`Explore.Application/Contracts/Persistence/IUnitOfWork.cs` provides `ExecuteInTransactionAsync` and `ExecuteInTransactionAsync<T>`. The delegate must be idempotent because the Npgsql retrying execution strategy may retry it. No emails, HTTP calls, or broker publishes should run inside the transaction delegate.

Outbox skill guidance: if event creation later needs external side effects, write an outbox message inside the same DB transaction and let the polling processor dispatch it later. Dispatchers must be idempotent because at-least-once delivery can produce duplicates. Do not perform external I/O inside the create transaction delegate.

## Verified Current Blazor Behavior

`Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs` currently builds `CreateEventDto` and calls `EventService.CreateEventAsync(createDto)`. After success, it calls `GetSessionsByEventAsync(createdEventId)`, updates the default session with `UpdateSessionAsync`, and creates additional sessions with `CreateSessionAsync`.

`PopulateInlineSchedulingOnDto()` maps inline days and rooms into `CreateEventDto`, but maps inline agenda items with `RoomId = null`. The UI has an internal room index, but the API DTO expects a persisted room ID.

`Explore.Blazor.Client/Services/EventService.cs` exposes `CreateEventAsync(CreateEventDto)` and wraps `_apiClient.CreateEventAsync(createDto)`. It does not expose a wrapper for `CreateEventWithSessionsAsync`, although `Explore.Blazor.Client/Clients/EventApiClient.g.cs` contains a generated `CreateEventWithSessionsAsync` method for `api/event/with-sessions`.

`Explore.Blazor.Client.Tests/Pages/Event/CreateEventTests.cs` currently asserts `CreateEventAsync(CreateEventDto)` behavior and must be updated for the create graph contract.

## Domain Model Context

The event scheduling domain has these key concepts:

1. `Event` is the parent aggregate-like program/container with publication, appearance, template, policy, timezone, and first/last session summary fields.
2. `EventSession` is the scheduled child with start/end time, local projections, optional event day, location, optional room, registration mode, capacity, price, and typed aspects.
3. `EventDay` groups schedules by local date and can carry labels, descriptions, banners, publication state, sort order, and day-scope registration behavior.
4. `LocationRoom` belongs to a location and can be assigned to sessions or agenda items.
5. `EventAgendaItem` is an event-level agenda item with optional event day, time range, location, room, kind, and sort order.
6. Existing session-level agenda item infrastructure also exists through `EventSessionAgendaItemController` and related route names, but Create Event v1 should use event-level agenda only. Session-level agenda is deferred to session editor/detail flows.

## Final Create Graph Decisions

1. Endpoint: replace `POST api/event`; delete or retire `POST api/event/with-sessions`.
2. Route name: keep `RouteNames.CreateEvent` for canonical create where possible.
3. Contract name: `CreateEventRequest`, not `CreateEventAggregateDto`.
4. Command name: `CreateEventCommand`, not a parallel aggregate-named command.
5. Agenda: event-level agenda only in Create Event v1.
6. Categories/tags: persist through the create graph if existing repositories support it; otherwise hide the Create Event controls.
7. Days: auto-create day rows for distinct multi-day local session dates; single-day rows only when day metadata is explicitly enabled.
8. Rooms: create rooms only under an already-selected existing location.
9. Location creation: out of scope.
10. Authorization: endpoint `[Authorize]` plus server-side publisher/actor authorization in Application/MediatR path.

## Recommended Contract Shape

Use a root `CreateEventRequest` with nested collections and temporary keys:

```csharp
public sealed class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? GroupId { get; set; }
    public int EventStatusId { get; set; }
    public int VisibilityTypeId { get; set; }
    public int EventFormatId { get; set; }
    public string? Timezone { get; set; }
    public List<CreateEventSessionRequest> Sessions { get; set; } = new();
    public List<CreateEventDayRequest> Days { get; set; } = new();
    public List<CreateEventRoomRequest> Rooms { get; set; } = new();
    public List<CreateEventAgendaItemRequest> AgendaItems { get; set; } = new();
}
```

The actual implementation should include all required current event fields, not only this sketch. Temporary keys are needed because the UI cannot know database IDs before submit.

## Research Notes

Context7 MudBlazor research:

1. Use MudBlazor v9 patterns and `IDialogService.ShowAsync` for dialog flows.
2. Use form controls with explicit labels and validation binding.
3. Use proper dialog providers and close APIs.

Context7 Blazor research:

1. `EditForm` can use an explicit `EditContext` for complex forms.
2. `ValidationMessageStore` supports custom graph-level validation messages.
3. `OnValidSubmit` or `OnSubmit` with `editContext.Validate()` are valid patterns.
4. Field-change subscriptions must be unsubscribed during disposal.

Tavily UX research:

1. Progressive disclosure reduces complexity when advanced options are rare.
2. The path to advanced options must be obvious and reversible.
3. Placeholder-only labels hurt usability and accessibility.
4. Dynamic form sections need clear grouping and state preservation.
5. Drag/drop should not be the only way to reorder or organize schedule content.

Tavily outbox/idempotency research:

1. The outbox pattern solves the dual-write problem by saving outgoing messages in the same database transaction as business state.
2. Dispatchers and consumers must be idempotent because at-least-once delivery may duplicate work.
3. External calls should not be mixed into the core database transaction.

## Implementation Guardrails

1. Do not introduce direct API calls from components; use the Blazor service layer.
2. Do not keep client-side post-create session choreography.
3. Do not use persisted IDs for records that are created in the same request; use temporary keys.
4. Do not add backward-compatibility shims unless a concrete external consumer is identified.
5. Do not inject validators through DI.
6. Do not return DTOs from repositories.
7. Do not expose `IQueryable` from repositories.
8. Do not disable tenant filters at runtime.
9. Do not call generated NSwag clients directly from Razor components; use the service layer.
10. Do not add external side effects inside `IUnitOfWork` transaction delegates; use transactional outbox for future side effects.

## Former Open Decisions Now Locked

1. New create graph replaces `POST api/event`; no third endpoint.
2. Categories/tags are blocking scope: persist if repository support is sufficient; otherwise hide controls.
3. Agenda is event-level only for Create Event v1.
4. `EventDay` rows are deterministic for multi-day session dates.
5. Rooms require an existing selected location; new `Location` creation is deferred.
