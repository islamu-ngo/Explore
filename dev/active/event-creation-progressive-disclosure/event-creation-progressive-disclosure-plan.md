<!-- ABOUTME: Event creation composer plan corrected toward session/group based program management. -->
<!-- ABOUTME: Treats talks, workshops, lectures, and activities as EventSession program items, not child Event records. -->

# Event Creation Progressive Disclosure Plan

Last Updated: 2026-05-05 14:05 CEST

## Executive Direction

The Event Creation Composer remains a guided, Luma-inspired event draft surface, but the program model has been corrected:

> **Talks, workshops, lectures, panels, classes, activities, and conference parts are `EventSession` program items. They are not child `Event` records.**

The correct program architecture is:

```text
Event
  -> EventSessionGroup / Program section / Track / Devroom / Stage
      -> EventSession / Program item / Talk / Workshop / Panel / Activity
          -> EventAgendaItem / breakouts, sub-timing, logistics, internal agenda detail
```

`Event` is the full event/container with publishing, visibility, registration, theme, ownership, and readiness lifecycle. `EventSession` is the scheduled content unit. `EventSessionGroup` is the missing grouping layer for FOSDEM-style tracks, devrooms, stages, and program sections. `EventAgendaItem` remains the right model for breaks, meals, prayer, logistics, and timeline details that are not standalone content sessions.

The previous child-event/`ParentEventId` direction is no longer the product plan for program parts. Because the project is still in development mode and the hierarchy slice was introduced from a wrong product interpretation, the default decision is to **rollback `ParentEventId` fully** unless a concrete true-event-hierarchy use case is approved before implementation. Do not retain it casually: retained wrong abstractions create mental overhead and future agents may revive them accidentally.

## Corrected Vocabulary

| Concept | Domain model | User-facing language |
| --- | --- | --- |
| Full event/container | `Event` | Event, conference, workshop day, retreat, course |
| Program grouping | `EventSessionGroup` | Program section, track, devroom, stage, room track, section |
| Scheduled content item | `EventSession` | Session, talk, workshop, panel, lecture, class, activity, program item |
| Logistics/non-content entry | `EventAgendaItem` | Break, meal, prayer, transition, logistics item |
| Recurring/thematic collection | `EventSeries` | Series |
| True event hierarchy only | future explicit model, not current `ParentEventId` work | Out of scope unless a concrete use case is approved |

Do not expose `EventSessionGroup` as raw UI terminology. Use the event type/context to choose labels: “Track” for conference tracks, “Devroom” for FOSDEM-style rooms, “Program section” as the neutral default.

## Evidence And Rationale

### Repository evidence

- The existing domain already has a rich `EventSession` model with event link, day, time projection, room/location, title, description, capacity, registration mode, featured image, price, currency, Islamic aspect, template source fields, audit, soft-delete, and concurrency.
- Session-adjacent models already exist for speakers, languages, agenda items, custom properties, rooms, registration, and scheduling.
- `EventSeries` already means recurring/thematic grouping and must not be overloaded as program hierarchy.
- `docs/ARCHITECTURE.md` and surrounding domain docs historically described `Event` as the program/container and `EventSession` as the scheduled child aggregate. That direction matches the corrected model better than child events.

### External research evidence

- **FOSDEM** organizes its program as main tracks, developer rooms/devrooms, lightning talks, stands, BOFs, rooms, and talks. Devroom managers review and schedule talks. This is `Event -> devrooms/tracks -> sessions/talks`.
- **pretalx** models talks, workshops, and programming items as sessions/submissions with tracks, session types, rooms, speaker availability, and schedule-only entries for breaks or blockers.
- **Swapcard / enterprise event platforms** expose schedule pages, tracks/streams, session filtering, session groups, speakers, locations, and custom agenda views over sessions.
- **EF Core documentation** supports explicit many-to-many join entities with payload. That is the correct persistence pattern for `EventSessionGroupSession` because a session may appear in multiple groups (main track, beginner-friendly, Arabic-language program, highlights) and the join needs `IsPrimary`, `SortOrder`, audit, and tenant fields.
- **MudBlazor documentation** supports the intended UI with async `MudAutocomplete`, `MudSelect`, form validation, loading buttons, templates, and cancellation-aware search for group/speaker/language pickers.

## Non-Negotiable Product Rules

1. No giant nested session/program form inside Create Event.
2. No right-sidebar session editor as the primary create path.
3. `Add session` saves or updates the event draft, then navigates to a dedicated session/program-item page.
4. The Event composer shows a derived Program summary from sessions, groups, and agenda/logistics data.
5. `EventSession` remains the rich program-item model.
6. `EventSessionGroup` provides tracks/devrooms/sections/stages.
7. `ParentEventId` should be rolled back if it was only added for program parts; do not keep it as a retained default.
8. No backward compatibility constraint: remove or reshape development-only wrong-direction code instead of preserving it.

## Phase 0 — Stop Wrong Direction And Roll Back ParentEventId

### Goal

Stop the accidental child-event implementation path before more UI/API/docs depend on it.

### Required actions

- Inspect every code change introduced for program-oriented `Event.ParentEventId`.
- Roll back ParentEventId code/migration/routes/DTOs/HAL links if it exists only to support the rejected child-event program interpretation.
- Do not retain ParentEventId unless a concrete true-event-hierarchy use case is approved in writing before the rollback slice starts.
- If an approved true hierarchy use case appears later, reintroduce it cleanly with a separate ADR/plan rather than carrying this wrong-direction implementation forward.
- Remove/demote these as active program targets:
  - `old parent-event selector`
  - `old child-event context banner`
  - `old add-child-event action`
  - `old child-event program item`
  - parent candidate lookup as a required program feature
  - child-event Program summary
  - “old child-event interpretation for talks/workshops” language
- Keep `EventSession`; only demote inline/right-drawer editing as the primary create UX.

### Acceptance

- No active planning document treats child `Event` records as the default representation for talks/workshops/lectures/conference parts.
- `ParentEventId` appears only in rollback instructions or an explicitly approved true-event-hierarchy note.
- Next implementation slice starts from sessions/groups, not parent/child events.

## Phase 1 — Program Group Domain Model

### Domain entities

Add `EventSessionGroup`:

```csharp
public sealed class EventSessionGroup : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }
    public Guid? RoomId { get; set; }
    public LocationRoom? Room { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public ICollection<EventSessionGroupSession> Sessions { get; set; } = new List<EventSessionGroupSession>();
    // audit / soft-delete / concurrency fields
}
```

Add explicit join entity `EventSessionGroupSession`:

```csharp
public sealed class EventSessionGroupSession : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid EventSessionGroupId { get; set; }
    public EventSessionGroup EventSessionGroup { get; set; } = null!;
    public Guid EventSessionId { get; set; }
    public EventSession EventSession { get; set; } = null!;
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    // audit fields
}
```

Recommended `Event` navigation names:

```csharp
public ICollection<EventSession> Sessions { get; set; } = new List<EventSession>();
public ICollection<EventSessionGroup> SessionGroups { get; set; } = new List<EventSessionGroup>();
public ICollection<EventAgendaItem> AgendaItems { get; set; } = new List<EventAgendaItem>();
```

Never name `EventSession` collections `ChildEvents`. `EventSession` is a session/program item, not a child event. UI may call sessions “Program items,” “Talks,” or “Workshops,” but domain navigation names must not revive the child-event ambiguity.

Future lookup, not required for Phase 1: `SessionGroupType` (`Track`, `Devroom`, `Stage`, `Room`, `Theme`, `Audience`, `Custom`).

### Persistence rules

- `event_session_groups`: tenant scoped, soft-delete aware.
- Unique active slug per event: `(tenant_id, event_id, slug)` filtered by not deleted where supported.
- Sort index: `(tenant_id, event_id, sort_order)`.
- `event_session_group_sessions`: unique `(tenant_id, event_session_group_id, event_session_id)`.
- Join carries redundant `EventId` and `TenantId` intentionally for query performance and validation.
- Application validators must enforce `Group.EventId == Session.EventId == Join.EventId` and `Group.TenantId == Session.TenantId == Join.TenantId`; never trust the client.
- Support one primary group per event/session when the database can express it safely.
- Index `(tenant_id, event_session_group_id, sort_order)` for program rendering.
- Delete behavior should be restrictive or explicit; do not cascade-delete sessions accidentally when deleting a group.

### Application rules

- Group belongs to same tenant/event as assigned sessions.
- Slug unique within event.
- Group color must use design-token-compatible color validation or a constrained hex format.
- Group assignments must preserve tenant boundaries and event ownership.
- Reordering is explicit and audited.

## Phase 2 — Application/API Program Contracts

### Event draft contracts

Create/reshape draft endpoints before UI hardening:

- `CreateEventDraftRequest`
- `UpdateEventDraftRequest`
- no client-controlled `EventStatusId`
- idempotency key for draft creation
- optimistic concurrency on update
- centralized ProblemDetails mapping
- publish readiness remains separate from draft persistence

### Session/program-item contracts

Add or refine:

- `GetEventSessionCreateContextQuery`
- `CreateEventSessionRequest` or `CreateEventSessionDraftRequest`
- `UpdateEventSessionRequest`
- `EventSessionKind` lookup (`Talk`, `Workshop`, `Panel`, `Lecture`, `Class`, `Activity`, `Keynote`, `LightningTalk`, `BOF`, `Demo`, `QAndA`, `Other`)
- `GetEventProgramSummaryQuery`
- `GetEventSessionGroupsQuery`
- `CreateEventSessionGroupCommand`
- `UpdateEventSessionGroupCommand`
- `AssignSessionToGroupCommand`
- `ReorderEventSessionGroupSessionsCommand`

Session create context should inherit event defaults:

- timezone and event date range
- default location/rooms
- default registration/capacity policy
- languages
- speaker candidates
- categories/tags
- custom property definitions/templates
- available program sections/tracks/devrooms

### HAL affordances

Event detail links:

- `program`
- `sessions`
- `add-session`
- `session-groups`
- `add-session-group`
- existing `publish-readiness`, `publish`, `edit`, `delete`, registration links

Session links:

- `self`
- `event`
- `edit`
- `delete`
- `speakers`
- `languages`
- `groups`
- future `submit`, `review`, `approve`, `reject`, `publish`

Blazor must gate actions by HAL links and creation context, not role checks.

## Phase 3 — Create Event Action Bar

Target action bar:

```text
[Save draft] [Add session] [Review and publish]
```

Behavior:

- If the event is unsaved and user selects `Add session`, create a draft first.
- If the event has unsaved changes, update the draft first.
- Then navigate to `/events/{eventId}/sessions/create`.
- Pass inherited defaults through server context, not fragile client state.
- On return, refresh Program summary.

The button label may adapt by event type (`Add talk`, `Add workshop`, `Add activity`) but maps to `EventSession`.

## Phase 4 — Dedicated Session Create/Edit Page

Routes:

- `/events/{eventId}/sessions/create`
- `/events/{eventId}/sessions/{sessionId}/edit`

Page title: `Add program item` by default, with contextual labels for talk/workshop/activity.

Fields:

- session/program item type
- title and description
- date/start/end/timezone
- location and room
- program section/track/devroom picker
- speakers
- languages
- capacity and registration mode
- price if applicable
- categories/tags
- featured image
- template/custom fields

UX requirements:

- dedicated page, not a drawer in the Create Event page
- accessible labels for every field
- async pickers use cancellation-aware search
- focus and announcements for save/return flows
- session save returns to Event composer or Program summary based on origin

## Phase 5 — Program Summary UI

Replace the old nested schedule composer target with a lightweight summary over sessions/groups/agenda.

Target components:

- `ProgramSection`
- `ProgramDayGroup`
- `ProgramItem`
- `SessionGroupSection`
- `SessionGroupPicker`
- `AddSessionAction`
- `SessionCreateContextBanner`

Display states:

- Unsaved event: “Save a draft to add sessions.” `[Save draft and add session]`
- Saved empty event: `[Add session]` `[Add section/track]`
- Saved event with sessions: grouped by section/track and local day.

Program items show:

- title
- type (talk/workshop/panel/activity)
- time range
- room/location
- speakers
- language
- capacity/registration state
- readiness warnings
- edit link to dedicated session page

`EventAgendaItem` remains available for breaks/meals/prayer/logistics and may appear between sessions in the derived summary.

Program summary must compose both content sessions and logistics agenda items, for example:

```text
09:00 Talk
10:00 Workshop
12:00 Lunch
13:30 Panel
15:30 Prayer break
```

Do not force lunch, prayer, breaks, and transitions to become sessions unless they need session-specific behavior such as speakers, capacity, registration, review, or a public detail page.

## Phase 6 — Theme And Core UX Cleanup

Keep the accepted theme direction:

- event name is the only bare editorial/Luma-style input, with accessible label
- all other fields are structured controls
- `ThemeQuickBar` remains below image with quick presets/colors/effects
- the full composer/page is the live preview through CSS custom properties
- `ThemeStudioTray` is advanced-only and must not duplicate quick controls
- More Options remains rare/advanced fields only

## Phase 7 — First-Class Session Pages And Collaboration Readiness

Sessions can have standalone public detail pages and shareable URLs without becoming `Event` records:

```text
/events/{eventSlug}/sessions/{sessionSlug}
/e/{eventSlug}/program/{sessionSlug}
```

This resolves the “talks feel like full pages” concern while preserving the correct aggregate model.

Plan future session lifecycle fields on `EventSession`:

```text
Draft -> Submitted -> InReview -> Accepted -> Rejected -> Published -> Cancelled
```

Do not implement the full lifecycle in Phase 1 unless needed, but keep contracts and permissions compatible with it.

Anticipate event-scoped program permissions:

```text
event.program.view
event.program.manage
event.session.create
event.session.update
event.session.delete
event.session.review
event.session.assign_speaker
event.session.assign_group
event.session.publish
event.session_group.create
event.session_group.update
event.session_group.delete
event.session_group.reorder
```

## Phase 8 — Hardening

- Draft/create/update idempotency.
- Concurrency stamps on event and session edits.
- Tenant-safe repositories and query filters.
- Audit entries for session/group assignment and reorder.
- Readiness paths for `program.sessions[*]`, `program.groups[*]`, and `program.agenda[*]`.
- ProblemDetails for validation failures.
- Architecture tests for layer boundaries.
- API integration tests for HAL affordances.
- Accessibility checks for keyboard-only Add session flow.

## Obsolete Or Demoted Targets

These are obsolete as the primary Create Event program path:

- `ScheduleComposer` as a giant nested session form
- right-sidebar `SessionEditorPanel` as primary creation UX
- `SessionInlineEditor` for talks/workshops
- `AddScheduleItemMenu` as the primary way to add talks/workshops
- `old parent-event selector` for program parts
- `old add-child-event action`
- `old child-event context banner`
- `old child-event program item`
- child-event Program summary
- `PopulateSchedulingOnRequest()` as long-term graph-shaped create mapping

`EventSession` itself is not obsolete. It is the central program-item model.

## Naming And Folder Scope

The current folder name can remain for continuity, but the scope is now broader than progressive disclosure. Treat this effort internally as `event-program-management` or `event-composer-program-refactor` when naming future branches, ADRs, or follow-up docs.

## Current Implementation Status

Implemented before this correction:

- progressive Create Event page shell
- ThemeQuickBar and ThemeStudioTray prototypes
- Event Options summary rows
- transitional `ScheduleTimelineComposer`
- transitional `SessionEditorPanel`/workflow
- a wrong-direction `ParentEventId` hierarchy slice may exist and must be rolled back if it was only introduced for program parts

Missing for corrected direction:

- `EventSessionGroup`
- `EventSessionGroupSession`
- session create context
- dedicated session create/edit page
- Program summary over sessions/groups
- Add session HAL affordance
- group/session assignment commands

## Risks

| Risk | Mitigation |
| --- | --- |
| ParentEventId code keeps influencing program UX | Phase 0 rollback-first policy and grep acceptance checks |
| Session form becomes a giant nested block again | Dedicated page only; Program summary is read-only/lightweight |
| One session needs multiple groupings | Use explicit join entity, not only `EventSession.SessionGroupId` |
| Track/devroom manager roles arrive later | Keep group/session assignment audited and authorization-ready |
| Agenda items get confused with sessions | Sessions are content; agenda items are logistics/internal timing |
| UI exposes domain jargon | Use Program section/Track/Devroom, not `EventSessionGroup` |
