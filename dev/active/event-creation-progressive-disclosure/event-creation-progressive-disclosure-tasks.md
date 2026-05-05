<!-- ABOUTME: Task checklist for corrected Event Creation Composer program/session direction. -->
<!-- ABOUTME: Replaces child-event subevent planning with EventSessionGroup and EventSession program management. -->

# Event Creation Progressive Disclosure Tasks

Last Updated: 2026-05-05 14:05 CEST

## Current Handoff Status

The plan has been corrected. “Subevent” in the prior discussion means **session/program item**, not child `Event`.

The active model is:

```text
Event -> EventSessionGroup / Track / Devroom / Program section -> EventSession / Program item
```

`Event.ParentEventId` is not the default program model. It must be rolled back if it was only introduced for program parts; do not retain it without an approved true-event-hierarchy use case.

## Phase 0 — Stop Wrong Direction

- [ ] Inventory all `ParentEventId` code, migration, DTOs, handlers, routes, HAL links, tests, and docs.
- [ ] Roll back `ParentEventId` unless a concrete true-event-hierarchy use case is approved before rollback starts.
- [ ] Remove the migration/code/routes/DTO fields introduced only for program parts.
- [ ] If an approved true hierarchy use case appears later, create a separate ADR/plan and reintroduce cleanly.
- [ ] Remove active UX targets: `old parent-event selector`, `old add-child-event action`, `old child-event context banner`, `old child-event program item`.
- [ ] Remove active API targets: parent candidates for program, child-event program summary, `add-subevent` link.
- [ ] Confirm `EventSession` is no longer described as obsolete/legacy for talks/workshops.
- [ ] Update `docs/ARCHITECTURE.md` if it currently says child `Event` records are the program model.
- [ ] Acceptance: grep finds no active “old child-event interpretation for talks/workshops” direction.

## Phase 1 — Add Program Group Model

### Domain

- [ ] Add `EventSessionGroup` entity.
- [ ] Add `EventSessionGroupSession` explicit join entity.
- [ ] Add `Event.Sessions`, `Event.SessionGroups`, and `Event.AgendaItems` navigations only if consistent with existing domain style.
- [ ] Never name `EventSession` collections `ChildEvents`.
- [ ] Add group assignment collection navigation on `EventSession` if useful.
- [ ] Use `LocationRoom? Room`, not a new `Room` abstraction.
- [ ] Include tenant, audit, soft-delete, and concurrency fields per project conventions.
- [ ] Keep Domain free of EF/API/Blazor/MediatR references.

### Persistence

- [ ] Add DbSets for `EventSessionGroup` and `EventSessionGroupSession`.
- [ ] Add EF configurations.
- [ ] Configure `(tenant_id, event_id, slug)` unique active index for groups.
- [ ] Configure `(tenant_id, event_id, sort_order)` group sort index.
- [ ] Configure `(tenant_id, event_session_group_id, event_session_id)` unique join index.
- [ ] Configure `(tenant_id, event_session_group_id, sort_order)` join sort index.
- [ ] Enforce `Group.EventId == Session.EventId == Join.EventId`.
- [ ] Enforce `Group.TenantId == Session.TenantId == Join.TenantId`.
- [ ] Configure restrictive/explicit delete behavior so groups do not accidentally delete sessions.
- [ ] Generate focused migration.
- [ ] Update model snapshot through EF tooling.

### Application

- [ ] Add group DTOs.
- [ ] Add group assignment DTOs.
- [ ] Add `GetEventSessionGroupsQuery`.
- [ ] Add `CreateEventSessionGroupCommand`.
- [ ] Add `UpdateEventSessionGroupCommand`.
- [ ] Add `AssignSessionToGroupCommand`.
- [ ] Add `ReorderEventSessionGroupSessionsCommand`.
- [ ] Validate same tenant and same event for every group/session assignment.
- [ ] Validate slug uniqueness within event.

## Phase 2 — Application/API Program Contracts

### Event draft contracts

- [ ] Add/finish `CreateEventDraftRequest`.
- [ ] Add/finish `UpdateEventDraftRequest`.
- [ ] Remove client-controlled `EventStatusId` from draft creation path.
- [ ] Add idempotency key for draft create.
- [ ] Add concurrency handling for draft update.
- [ ] Keep publish readiness separate from draft persistence.

### Session contracts

- [ ] Add `GetEventSessionCreateContextQuery`.
- [ ] Add `CreateEventSessionRequest` or `CreateEventSessionDraftRequest`.
- [ ] Add `UpdateEventSessionRequest`.
- [ ] Add `GetEventProgramSummaryQuery`.
- [ ] Add `EventSessionKind` lookup and seed values: Talk, Workshop, Panel, Lecture, Class, Activity, Keynote, LightningTalk, BOF, Demo, QAndA, Other.
- [ ] Ensure session create context includes inherited event defaults.
- [ ] Include available groups/tracks/devrooms in session create context.
- [ ] Include room/location/language/speaker/category/tag/template/custom-field options.
- [ ] Validate session time inside event window or map readiness warning if policy allows exceptions.
- [ ] Validate speaker/room conflicts where supported.

### API/HAL

- [ ] Add/verify event HAL links: `program`, `sessions`, `add-session`, `session-groups`, `add-session-group`.
- [ ] Add/verify session HAL links: `self`, `event`, `edit`, `delete`, `speakers`, `languages`, `groups`.
- [ ] Add group endpoints.
- [ ] Add program summary endpoint.
- [ ] Add session create context endpoint.
- [ ] Ensure Blazor uses HAL links instead of role checks.

## Phase 3 — Create Event Action Bar

- [ ] Replace `old add-child-event flow` with `Add session`.
- [ ] If event is unsaved, `Add session` creates a draft first.
- [ ] If event is dirty, `Add session` updates the draft first.
- [ ] Navigate to `/events/{eventId}/sessions/create`.
- [ ] Use server session create context for inherited defaults.
- [ ] Return to Event composer after save/cancel.
- [ ] Refresh Program summary after returning.
- [ ] Announce navigation/save state for assistive tech.

## Phase 4 — Dedicated Session Create/Edit Page

- [ ] Add route `/events/{eventId}/sessions/create`.
- [ ] Add route `/events/{eventId}/sessions/{sessionId}/edit`.
- [ ] Page title defaults to `Add program item`.
- [ ] Support contextual labels: talk/workshop/panel/activity/session.
- [ ] Add fields for type, title, description, date/start/end/timezone.
- [ ] Add location and room selection.
- [ ] Add program section/track/devroom picker.
- [ ] Add speaker picker/management affordance.
- [ ] Add language picker.
- [ ] Add capacity/registration mode fields.
- [ ] Add category/tag/custom-property/template fields.
- [ ] Add image support if contract-backed.
- [ ] Save creates/updates `EventSession`.
- [ ] No right sidebar session editor as primary create UX.

## Phase 5 — Program Summary UI

- [ ] Replace final nested schedule composer target with `ProgramSection`.
- [ ] Add `ProgramDayGroup`.
- [ ] Add `ProgramItem`.
- [ ] Add `SessionGroupSection`.
- [ ] Add `SessionGroupPicker` where needed.
- [ ] Add `AddSessionAction`.
- [ ] Add `SessionCreateContextBanner`.
- [ ] Saved empty event state shows `[Add session]` and `[Add section/track]`.
- [ ] Unsaved event state shows `[Save draft and add session]`.
- [ ] Group sessions by program section and local day.
- [ ] Show time, room/location, speakers, language, capacity/registration, readiness warnings.
- [ ] Editing a program item navigates to dedicated session edit page.
- [ ] Include `EventAgendaItem` logistics in summary where useful.

## Phase 6 — Theme And Core UX Cleanup

- [ ] Event name remains the only bare editorial input.
- [ ] Other fields remain structured accessible controls.
- [ ] Theme quick bar updates page-level CSS variables.
- [ ] Theme tray contains advanced-only controls.
- [ ] Remove duplicated quick controls from tray.
- [ ] More Options contains rare fields only.
- [ ] Mobile order: event basics, theme, options summary, Program summary, actions.

## Phase 7 — Lifecycle, Permissions, And Hardening

- [ ] Plan future session lifecycle states: Draft, Submitted, InReview, Accepted, Rejected, Published, Cancelled.
- [ ] Add/plan event-scoped permissions: `event.program.view`, `event.program.manage`, `event.session.create`, `event.session.update`, `event.session.delete`, `event.session.review`, `event.session.assign_speaker`, `event.session.assign_group`, `event.session.publish`, `event.session_group.create`, `event.session_group.update`, `event.session_group.delete`, `event.session_group.reorder`.
- [ ] Add readiness paths for `program.sessions[*]`.
- [ ] Add readiness paths for `program.groups[*]`.
- [ ] Add readiness paths for `program.agenda[*]`.
- [ ] Add audit for group/session assignment.
- [ ] Add audit for reorder.
- [ ] Add ProblemDetails for program validation failures.
- [ ] Add architecture tests for new layers.
- [ ] Add Application unit tests for validators/handlers.
- [ ] Add Persistence integration tests for group lifecycle and assignments.
- [ ] Add API integration tests for HAL links and endpoints.
- [ ] Add Blazor/component tests for Add session flow.
- [ ] Add accessibility verification for keyboard/focus/announcements.

## Obsolete / Demoted Primary-Create Targets

Do not build these as the primary program path:

- [ ] `ScheduleComposer` as a giant nested session composer.
- [ ] right-sidebar `SessionEditorPanel` for initial talk/workshop creation.
- [ ] `SessionInlineEditor` for talks/workshops.
- [ ] `AddScheduleItemMenu` as the primary way to add talks/workshops.
- [ ] `old parent-event selector` for program parts.
- [ ] `old add-child-event action`.
- [ ] `old child-event context banner`.
- [ ] `old child-event program item`.
- [ ] child-event Program summary.
- [ ] `PopulateSchedulingOnRequest()` as long-term graph-shaped create mapping.

Keep `EventSession`; it is the central program item.

## Verification Checklist

- [ ] `git diff --check -- dev/active/event-creation-progressive-disclosure/*.md`
- [ ] `rg -n "child-event Program summary|parent candidate lookup as a required program feature|child-event program interpretation" dev/active/event-creation-progressive-disclosure`
- [ ] Confirm matches, if any, are rollback-only or approved true-hierarchy context.
- [ ] Confirm docs name `EventSessionGroup` and `EventSessionGroupSession` as the next model.
- [ ] Confirm docs require rollback of program-only `ParentEventId`.
- [ ] Confirm docs contain no `ChildEvents` session naming.
- [ ] Confirm docs say no giant nested session form and no primary right-sidebar session editor.
