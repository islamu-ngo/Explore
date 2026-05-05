<!-- ABOUTME: Current context for correcting Event Creation Composer toward session/group program management. -->
<!-- ABOUTME: Captures the rollback-first decision for ParentEventId and next implementation slices. -->

# Event Creation Progressive Disclosure Context

Last Updated: 2026-05-05 14:05 CEST

## Current Source Of Truth

The active direction is **Event -> EventSessionGroup -> EventSession**, not child events.

The earlier “subevent” wording meant sessions/program items. Talks, workshops, lectures, panels, classes, and activities are `EventSession` records. Tracks/devrooms/program sections are the missing grouping layer, `EventSessionGroup`. The Create Event page should help create the event draft and then route users to a dedicated session/program-item page.

## Correction Summary

- Stop using child `Event` records as the primary model for talks/workshops/program parts.
- Review the recently added `Event.ParentEventId` implementation.
- Roll it back fully if it was only introduced for program management.
- Do not retain it unless a concrete true-event-hierarchy use case is approved before rollback work starts.
- Keep and invest in `EventSession` as the rich program-item aggregate.
- Add `EventSessionGroup` / `EventSessionGroupSession` for tracks/devrooms/program sections.
- Program summary is derived from sessions, session groups, and agenda/logistics entries.

## Research Findings

### Repository-first evidence

- The repo already has rich `EventSession` infrastructure for scheduled content: timing, room/location, title, description, capacity, registration mode, image, price, aspects, template fields, audit, soft-delete, and concurrency.
- Session speakers, languages, agenda items, registration, rooms, and custom property models already surround `EventSession`.
- `EventSeries` is a recurring/thematic collection and is not a program grouping model.
- Existing architecture docs historically aligned with `Event` as the container and `EventSession` as scheduled child aggregate.

### External evidence

- FOSDEM uses main tracks, devrooms, rooms, lightning talks, BOFs, and talks. Devroom managers review and schedule talks.
- pretalx models talks/workshops/programming items as sessions/submissions with tracks, session types, rooms, speaker availability, and schedule-only entries for breaks/blockers.
- Swapcard and enterprise platforms expose agenda/schedule pages over sessions with filters, tracks/streams, locations, speakers, and custom fields.
- EF Core documentation supports explicit many-to-many join entities with payload, matching `EventSessionGroupSession`.
- MudBlazor supports the future UI with async autocomplete, selects, validated forms, templates, and loading buttons.

## Active Files

Planning docs:

- `dev/active/event-creation-progressive-disclosure/event-creation-progressive-disclosure-plan.md`
- `dev/active/event-creation-progressive-disclosure/event-creation-progressive-disclosure-context.md`
- `dev/active/event-creation-progressive-disclosure/event-creation-progressive-disclosure-tasks.md`

Existing code anchors:

- `Explore.Domain/Event.cs`
- `Explore.Domain/EventSession.cs`
- `Explore.Domain/EventAgendaItem.cs`
- `Explore.Domain/EventSessionAgendaItem.cs`
- `Explore.Domain/EventSeries.cs`
- `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
- `Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs`
- `Explore.Blazor.Client/Pages/Events/Components/ScheduleTimelineComposer.razor`
- `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor`

## Drift To Correct

The current docs previously over-rotated toward `Event.ParentEventId`. That drift must be removed from active program planning.

Wrong active targets:

- parent/child `Event` hierarchy for talks/workshops
- `old parent-event selector` in Create Event as program control
- `old add-child-event flow`
- `old child-event context banner`
- child-event Program summary
- parent candidates as required program lookup
- treating `EventSession` as legacy or lower-level only

Correct targets:

- `Add session`
- dedicated session create/edit page
- `EventSessionGroup`
- `EventSessionGroupSession`
- Program summary over groups/sessions/agenda
- session create context
- HAL-driven session/group affordances

## Next Implementation Slice

### Phase 0 — Stop Wrong Direction

- Inventory `ParentEventId` code/migration/routes/DTOs/handlers/tests.
- Roll it back if it exists only for program parts.
- Remove all Create Event program dependencies on `ParentEventId`.
- Update architecture docs if they were changed to say child events are program items.
- Confirm planning docs contain no active child-event program flow.

### Phase 1 — Program Group Model

- Add `EventSessionGroup` and `EventSessionGroupSession`.
- Use `LocationRoom? Room`, not a new `Room` abstraction.
- Use `Event.Sessions`, `Event.SessionGroups`, and `Event.AgendaItems`; never name sessions `ChildEvents`.
- Enforce join consistency: group/session/join `EventId` and `TenantId` must match.
- Configure EF indexes and delete behavior.
- Add repositories returning entities.
- Add Application DTOs/commands/queries/validators.
- Add migration.
- Add architecture and persistence tests.

### Phase 2 — Program Contracts

- Draft event contracts.
- Session create context.
- Session create/update contracts.
- Program summary query.
- Session group CRUD/assignment/reorder commands.
- HAL links: `program`, `sessions`, `add-session`, `session-groups`, `add-session-group`.

## UI Direction

Create Event action bar:

```text
[Save draft] [Add session] [Review and publish]
```

`Add session` behavior:

1. Save event draft if unsaved.
2. Update draft if dirty.
3. Navigate to `/events/{eventId}/sessions/create`.
4. Dedicated session page loads inherited defaults from the server.
5. Saving session returns to event composer/program summary.

The Create Event page does not host a giant session form. It shows a read-only/lightweight Program summary and actions.

## Accessibility And Design Constraints

- Event name is the only bare editorial input and still needs an accessible label.
- All other controls are structured fields with labels.
- Session/group pickers use keyboard-accessible MudBlazor components.
- Dynamic Program summary updates are announced.
- Focus is restored when returning from session create/edit.
- CSS uses design tokens and logical properties.
- HAL links, not role checks, control action visibility.

## Open Decisions

- Whether there is any approved true-event-hierarchy use case that justifies not rolling back the current `ParentEventId` code. Default is rollback.
- Use `EventSessionKind` for session type lookup; UI may label it “Program item type.”
- Whether a primary group should be stored only in the join (`IsPrimary`) or duplicated as `EventSession.PrimarySessionGroupId` for query convenience.
- Whether schedule-only logistics live solely as `EventAgendaItem` or require a lightweight event-level agenda item for non-session breaks.
- Future `SessionGroupType` lookup (`Track`, `Devroom`, `Stage`, `Room`, `Theme`, `Audience`, `Custom`).
- Future session lifecycle: Draft, Submitted, InReview, Accepted, Rejected, Published, Cancelled.
- First-class session URLs: `/events/{eventSlug}/sessions/{sessionSlug}` or `/e/{eventSlug}/program/{sessionSlug}`.

## Verification For This Documentation Update

Run:

```bash
git diff --check -- dev/active/event-creation-progressive-disclosure/*.md
rg -n "child-event Program summary|parent candidate lookup as a required program feature|child-event program interpretation" dev/active/event-creation-progressive-disclosure
```

Expected grep result: no active target language. `ParentEventId` may appear only in rollback or explicitly approved true-event-hierarchy context.
