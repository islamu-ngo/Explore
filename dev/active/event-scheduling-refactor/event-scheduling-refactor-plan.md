Last Updated: 2026-04-11

# Plan: Event Scheduling Refactor

## Executive Summary

Refactor the current event scheduling model from a session-centric-but-limited structure into a production-grade scheduling domain that supports:

- `EventSeries` as an umbrella grouping, distinct from recurrence.
- `Event` as the concrete discoverable/registerable occurrence.
- `EventSession` as the schedulable content unit.
- `EventSessionAgendaItem` as the internal breakdown of a session.
- New `EventAgendaItem` for event-level timeline blocks outside any one session.
- New `EventDay` as an explicit event-local day aggregate with labels, publishing, registration, ordering, and admin landing sections.
- Parent-child registrations that preserve intent (`Event`, `Day`, `SessionSelection`) instead of only storing per-session rows.
- Cached local-day/local-time projections derived from UTC timestamps + `Event.Timezone`.
- A room-aware, day-based agenda UI in Blazor that scales from single-session events to conference-style programs.

This plan is incremental, layer-safe, and preserves existing Clean Architecture boundaries. It also assumes the repo currently has partially implemented `EventSeries`, current event/session CRUD, and session-scoped registration, but no `EventDay`, no `EventAgendaItem`, no room model, and no registration-intent parent model.

It also adopts an ultrawork-friendly sequencing rule set:

- isolate the registration semantic change into its own phase,
- keep new foreign keys nullable before enforcing stronger invariants,
- keep OpenAPI/NSwag regeneration as a hard boundary before broad Blazor client changes,
- use TDD-oriented implementation slices and atomic commits.

## Current State Analysis

### Verified domain and schema state

- `Explore.Domain/Event.cs`
  - Already contains `EventSeriesId`, `SeriesOrder`, `Timezone`, `FirstSessionDate`, `LastSessionDate`, `FirstSessionStartUtc`, `LastSessionStartUtc`, and `EventTimeZoneId`.
  - Does not contain `LocationId`, registration policy fields, `EventDay` navigation, or event-level agenda items.
- `Explore.Domain/EventSeries.cs`
  - Already exists as a tenant-scoped aggregate with `Title`, `Slug`, `Description`, `IsPublished`, `StartDateUtc`, `EndDateUtc`, and `Events`.
  - Current UI integration is incomplete; backend support exists.
- `Explore.Domain/EventSession.cs`
  - Already models `StartTime`, `EndTime`, `LocationId`, `Title`, `Description`, `Slug`, `MaxAudienceAttendees`, `CurrentAudienceAttendees`, `RegistrationModeId`, `Price`, and `CurrencyCode`.
  - Does not contain `RoomId`, session taxonomy, cached local-date/local-time fields, sort order, event-day linkage, or session-level taxonomy junctions.
- `Explore.Domain/EventSessionAgendaItem.cs`
  - Already exists with `EventSessionId`, `StartTime`, `EndTime`, `Title`, `Description`, and `LocationId`.
  - Does not contain a kind/type lookup, room linkage, sort order, or cached local projections.
- `Explore.Domain/EventRegistration.cs`
  - Is currently session-scoped: `UserId`, `EventSessionId`, `ApprovalStatusId`, `TenantId`, `AtprotoRecordId`.
  - Does not model `EventId`, `RegistrationScope`, selected local day, policy snapshot, or registration-child rows.
- `schemas/islamu-event.md`
  - Documents `event_sessions`, `event_session_agenda_items`, `event_categories`, `event_tags`, and `event_registrations` as current schema tables.
  - Confirms no `event_days`, `event_agenda_items`, `location_rooms`, `event_registration_sessions`, `event_session_categories`, or `event_session_tags`.

### Verified persistence/configuration state

- `Explore.Persistence/ExploreDbContext.cs`
  - Has `DbSet<EventSession>`, `DbSet<EventRegistration>`, `DbSet<EventSessionAgendaItem>`, and `DbSet<EventSeries>`.
  - No DbSets yet for `EventDay`, `EventAgendaItem`, `LocationRoom`, or registration-child tables.
- `Explore.Persistence/Configurations/Entities/EventConfiguration.cs`
  - Configures event indexes and non-negative price check.
  - Does not configure `EventSeries` relationship explicitly here, event-level location, registration policy, or event-day relations.
- `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`
  - Enforces price precision and basic FKs.
  - No `EndTime > StartTime` check constraint, room uniqueness, local-day cached indexes, or conflict-prevention constraints.
- `Explore.Persistence/Configurations/Entities/EventRegistrationConfiguration.cs`
  - Enforces unique `(EventSessionId, UserId)` via `ix_eventregistrations_session_user`.
  - This is incompatible with the target parent-child registration model and will need replacement/backfill.
- `Explore.Persistence/Configurations/Entities/EventSessionSpeakerConfiguration.cs`
  - Has foreign keys only.
  - Does not yet enforce unique `(EventSessionId, ActorId)` or speaker ordering/role fields.
- `Explore.Persistence/Configurations/Entities/EventCategoriesConfiguration.cs` and `Explore.Persistence/Configurations/Entities/EventTagsConfiguration.cs`
  - Have relationships only.
  - Do not yet enforce unique `(EventId, CategoryId)` or `(EventId, TagId)`.

### Verified application/API state

- `Explore.Application/DTOs/Event/CreateEventDto.cs`
  - Still lacks `EventSeriesId` and `SeriesOrder` even though the domain model already has them.
- `Explore.Blazor.Client/Clients/DtoPartials.cs`
  - Already injects `EventSeriesId` and `SeriesOrder` into generated `EventDto` as a client-side workaround.
  - Also injects `FeaturedImageId`/`FeaturedImageUri` session properties not present in the generated schema.
  - This is evidence of current API/NSwag drift that the refactor should eliminate rather than extend.
- `Explore.Application/Profiles/MappingProfile.cs`
  - Already maps `EventSeriesTitle` into `EventListDto`.
  - Already maps current `Event`, `EventSession`, `EventSessionAgendaItem`, and `EventRegistration` DTOs.
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventWithSessionsCommandHandler.cs`
  - Creates an event, then creates sessions.
  - Computes `FirstSessionDate` and `LastSessionDate` from UTC session timestamps, not event-local day grouping.
  - No event-day materialization, no cached local projections, no series assignment, and no agenda-item split.
- `Explore.Application/Features/EventSessions/Handlers/Commands/CreateEventSessionCommandHandler.cs`
  - Validates simple session timing and location/registration-mode existence.
  - No room conflict checks, day linkage, or local cache recomputation.
- `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs`
  - Maps directly from DTO to `EventRegistration` and creates a session-level row.
  - No organizer policy, no scope validation, no parent-child intent model, and no event/day-level semantics.
- `Explore.API/Controllers/EventController.cs`
  - Already exposes event CRUD and event-with-sessions creation.
  - Still contains legacy error payloads and mixed API style in places.
- `Explore.API/Controllers/EventSessionController.cs`
  - Exposes session CRUD and `GET by-event/{eventId}`.
- `Explore.API/Controllers/EventRegistrationController.cs`
  - Exposes session-level registration CRUD and user/session lookups.
  - Current API contract is centered on session rows, not registration intent.
- `Explore.API/Controllers/EventSeriesController.cs`
  - `EventSeries` CRUD exists already, but controller versioning/style is inconsistent with the rest of the event surface.

### Verified Blazor/UI state

- `Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs`
  - Loads one event, its sessions, and agenda items only for the primary session.
  - Registration status is derived by checking whether the current user has any session registrations for sessions under the event.
  - UI currently assumes event registration is the sum of session registrations.
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs`
  - Uses inline event form state plus a session editor workflow pattern already being extracted in `dev/active/session-series-ux/`.
  - Still imports `SessionEditorModel` from `EventSessionEditor` instead of a clean shared domain-neutral model.
- `Explore.Blazor.Client/Pages/Events/EventEdit.razor.cs`
  - Mirrors the create page and preloads sessions from the API into `SessionEditorModel`.
- `Explore.Blazor.Client/Pages/Events/Dialogs/SessionSelectionDialog.razor`
  - Already supports a “Register for ALL Sessions” option, but that currently still returns a list of selected session IDs.
  - This is a key semantic mismatch: the UI offers a whole-event-like action, but the backend only stores session rows, so user intent is not preserved.
- `Explore.Blazor.Client/Pages/Events/Components/EventSessionManager.razor`
  - Shows a list of sessions and optional expansion into per-session agenda items.
  - No day grouping, no room view, no event-level agenda bands.
- `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor`
  - Registration dialog only accepts `EventSessionId` and creates a single session registration.
- `Explore.Blazor.Client/Pages/Events/Components/EventSeriesSection.razor`
  - Already exists because of the active `session-series-ux` track.
  - The scheduling refactor should extend the existing series/editor UX instead of planning to create a second component set.
- `Explore.Blazor.Client/Pages/Events/Components/SessionSummaryCard.razor`
  - Already exists.
- `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor`
  - Already exists.
- `Explore.Blazor.Client/Components/Collection/EventTimeline.razor`
  - Current timeline groups list cards by a date derived from `FirstSessionDate`/`LastSessionDate`, not by an agenda projection.
- `dev/active/session-series-ux/session-series-ux-plan.md`
  - Confirms an ongoing UI track already discovered that `EventSeries` backend exists but frontend wiring is partial.
  - This refactor must not duplicate or conflict with that work; it should build on it.

### Baseline verification

- `dotnet build --configuration Release --verbosity quiet` currently fails due to a pre-existing file lock on `Event.Architecture.Tests.dll` and emits baseline warnings (package vulnerabilities, analyzer warnings, deprecated package warning).
- The plan must therefore distinguish new refactor verification from current baseline issues and include a pre-flight build-cleanup step.

## Proposed Future State

### Target domain model

1. `EventSeries`
   - Remains the umbrella grouping for related events.
   - Explicitly documented as not recurrence.

2. `Event`
   - Gains optional `LocationId` and organizer-controlled registration policy fields.
   - Becomes the parent aggregate for `EventDay`, `EventAgendaItem`, and `EventSession`.

3. `EventDay`
   - New first-class modeled object per event-local day.
   - Fields: `Id`, `EventId`, `TenantId`, `LocalDate`, `Label`, `Description`, banner/note fields, `IsPublished`, `SortOrder`, registration visibility/settings, audit/soft delete/concurrency as appropriate.
   - Becomes the anchor for admin day landing sections and day-based registration.
   - **Justification for first-class entity (not a derived grouping).** `EventDay` is modeled as a persistent aggregate member — not computed on the fly from session local dates — because each of the following concerns requires stable identity, authored state, and per-day persistence that a derived projection cannot carry:
     - **Custom day labels** that matter in the product (e.g. "Opening Day", "Main Program", "Family Day", "Community Iftar"). These are authored strings, not inferable from session titles or dates.
     - **Day-specific descriptions and banners** that organizers edit independently of sessions and that must render even on days with zero sessions or before sessions are scheduled.
     - **Day-specific publishing state** (`IsPublished`, scheduled reveal) that lets organizers stage a multi-day program incrementally; a derived grouping cannot be unpublished because the underlying sessions still exist.
     - **Day-level admin UX** (reorder, rename, hide, lock, attach media) that needs a stable primary key so admin actions target a row, not a volatile local-date bucket that can shift when sessions are rescheduled.
     - **Day-level registration and business rules** (whole-day registration scope, per-day capacity snapshots, per-day policy overrides, per-day check-in windows) that require a persistent entity the registration-intent model can foreign-key to.
     - **Day-level taxonomy and ordering** beyond chronological sort, including sponsor attribution, track coloring, and curated position that is independent of `LocalDate`.
     None of the above can be represented as a derived `GROUP BY LocalDate` projection over sessions. Sessions can move, be deleted, or not yet exist; `EventDay` must outlive them and own authored state. This is the decisive reason the refactor elevates `EventDay` to a first-class entity rather than a read-model convenience.

4. `EventSession`
   - Reframed as the schedulable content unit.
   - Gains `RoomId`, session taxonomy junctions, cached local projections, sort order, and possibly `EventDayId` or derived/event-day association rules.

5. `EventSessionAgendaItem`
   - Stays session-owned.
   - Gains schedule item kind/type lookup, optional room support if needed, and cached local projections only if queries require it.

6. `EventAgendaItem`
   - New event-level timeline concept for shared blocks such as breaks, logistics, prayer, opening, closing.
   - Supports optional `LocationId`, `RoomId`, `Kind`, `SortOrder`, and cached local projections.

7. `LocationRoom`
   - New room concept under `Location` for conference-style scheduling.
   - Supports optional slug, capacity, sort order, tenant scoping, and room conflict checks.

8. Registration
   - Add a new parent registration-intent/group layer above the current session access rows.
   - Keep session-level rows as the concrete access/entitlement records because sessions remain the real scheduled access unit in the platform.
   - Recommended parent naming: `EventRegistrationIntent` or `EventRegistrationGroup`.
   - Keep `EventRegistration` attached to concrete session-level records where practical to reduce semantic churn and simplify migration.
   - Add parent-to-child linkage so organizer policy and user intent are preserved while attendance, capacity, and session access stay concrete and understandable.
   - New enum/value on the parent layer: `RegistrationScope = Event | Day | SessionSelection`.
   - New event policy enum/value: `EventRegistrationPolicy = WholeEventOnly | WholeDayOnly | SessionSelectionOnly | WholeEventOrDay | WholeEventOrSession | Flexible`.

9. Taxonomy
   - Keep event-level `event_categories` and `event_tags` as broad umbrella taxonomy.
   - Add `event_session_categories` and `event_session_tags` for program-level precision.

10. Cached local projections
   - Add derived fields on `EventSession` and `EventAgendaItem` at minimum:
     - `LocalStartDate`
     - `LocalEndDate`
     - `LocalStartTime`
     - `LocalEndTime`
     - `LocalStartMinuteOfDay`
    - `LocalEndMinuteOfDay`
   - Recompute whenever UTC times or event timezone change.
   - **Ownership of the recompute (decided, not scattered).** The recompute responsibility lives in exactly one place: a **stateless domain service** `IEventScheduleProjectionCalculator` in `Explore.Domain/Services/Scheduling/` with a concrete `EventScheduleProjectionCalculator` implementation. All local projection writes go through this service. It is the single authority for converting UTC instants plus `Event.Timezone` into the cached local columns.
     - **Invocation contract.** `EventSession` and `EventAgendaItem` expose aggregate-style methods (e.g. `Reschedule(DateTime startUtc, DateTime endUtc, IEventScheduleProjectionCalculator calc)`, `ReprojectLocalTimes(string timezone, IEventScheduleProjectionCalculator calc)`) that accept the calculator and update their own cached fields. Setting raw UTC properties directly from handlers is disallowed by convention and enforced by architecture tests.
     - **Event-level timezone change triggers fan-out.** When `Event.ChangeTimezone(...)` is called, the event aggregate is responsible for iterating its loaded `Sessions` and `AgendaItems` collections and invoking their `ReprojectLocalTimes` methods with the calculator. No handler re-derives local fields inline. No validator writes local fields. No mapping profile writes local fields. No EF value converter writes local fields.
     - **Handler role is orchestration only.** Command handlers load the aggregate, call the aggregate method (which internally uses the calculator), and persist. Handlers never read the timezone, call `TimeZoneInfo`, or touch `LocalStart*`/`LocalEnd*` directly.
     - **Why a domain service and not a pure method on the entity.** Timezone resolution needs a `TimeZoneInfo` lookup and DST-aware conversion logic that is shared identically between `EventSession`, `EventAgendaItem`, and `EventDay` materialization during backfill. A stateless domain service keeps the logic in one type, lets backfill migrations reuse it without duplicating entity code, and keeps entities persistence-ignorant while still centralizing the rule.
     - **Out of scope for the calculator.** The calculator does not read from `DbContext`, does not know about `EventDay` membership, and does not enforce business rules; it only converts UTC + IANA timezone into the six local fields. Business rules (e.g. session must belong to an `EventDay` whose `LocalDate` matches the computed `LocalStartDate`) live in validators and aggregate invariants, which consume the calculator's output.

### Locked planning decisions

To avoid ambiguity and keep migration risk controlled, the implementation plan assumes these decisions unless later evidence from the codebase forces a safer compatibility variant:

1. `EventDay` is introduced as a first-class entity and `EventSession.EventDayId` starts nullable during transition.
2. `EventAgendaItem` is a new event-level entity, not a rename of `EventSessionAgendaItem`.
3. A new parent registration-intent/group record is introduced above the current session-level registration/access rows.
4. `LocationRoom` extends the current location model; session/event agenda room FKs start nullable.
5. Session taxonomy uses lookup/junction support, not ad hoc string fields.
6. Cached local projections are persisted query helpers, never user-authored fields.
7. Existing session-level registration APIs remain temporarily compatible while the new parent-intent plus child-entitlement model is introduced.
8. Same room plus overlapping time is invalid and is enforced in **two layers that are both mandatory from day one** — neither is optional, and the FluentValidation layer is explicitly described as necessary but not sufficient:
   - **Layer A — Async FluentValidation rule (user-facing, fail-fast).** A `CreateEventSessionCommandValidator` / `UpdateEventSessionCommandValidator` async rule queries the repository for persisted sessions in the same `RoomId` whose UTC `[StartTime, EndTime)` interval overlaps the candidate. On hit, returns a 400 ProblemDetails with a clear, localizable message. This is the good UX layer.
   - **Why Layer A alone is insufficient.** Application-level validation cannot protect against:
     - concurrent writes from two admins scheduling overlapping sessions in the same room within the same millisecond,
     - racing requests where both validators read the pre-insert state and both pass,
     - out-of-band mutations (admin tools, data imports, SQL fixups, background jobs) that bypass the command pipeline entirely,
     - data-import paths and bulk seeders that do not execute FluentValidation.
     Treating Layer A as sufficient is the classic check-then-act race that this refactor explicitly rejects.
   - **Layer B — Optimistic concurrency enforcement via the existing concurrency-stamp pattern.** `EventSession` already carries a concurrency token consistent with the rest of the codebase; the same mechanism is reused here — do not invent a new one. When a session's `StartTime`, `EndTime`, or `RoomId` changes, the update is issued with `ExpectedConcurrencyStamp`, and EF Core throws `DbUpdateConcurrencyException` if the row moved under us. In addition, the **room-level concurrency guard**: loading the set of candidate-overlap sessions for Layer A also loads their concurrency stamps; the command handler re-checks those stamps as part of the save (or uses a short serializable transaction scoped to `(TenantId, RoomId, LocalStartDate)`) so that any racing insert/update of a neighboring session forces a retry rather than a silent overlap. This is the correctness layer.
   - **No backwards-compatibility shims.** The project is in development mode. If existing sessions, configurations, or tests conflict with the two-layer enforcement, they are updated or deleted — not worked around. Do not add feature flags, do not keep a "legacy path without room checks," do not emit warnings instead of errors. Break and fix.
   - Deeper persistence hardening (exclusion constraints, generated range columns) remains a future option but is not a prerequisite because Layer B already closes the race window using tools the codebase already ships.

### Target UX & MudBlazor v9 Implementation

- **Paned "Command Center" Flow (MudDrawer Stack):**
  - **Right Sidebar (Primary):** `MudDrawer` with `Anchor="Anchor.End"` and `Variant="DrawerVariant.Temporary"`. Used for Event Details.
  - **Left Sidebar (Secondary):** `MudDrawer` with `Anchor="Anchor.Start"` and `Variant="DrawerVariant.Temporary"`. Used for the Agenda Timeline.
  - **Stacked Session Sidebar (Tertiary):** A second `MudDrawer` on the right, or a custom `div` transition that slides to the left of the Primary sidebar. This creates the "Miller Column" stack.
  - **Overlay & Transitions:** Use `MudOverlay` with `LockScroll="true"` and `DarkBackground="true"`. Apply `backdrop-filter: blur(0.75rem)` to drawer content for a premium "glass" effect, consistent with `MainLayout.razor.css`.
  - **Z-Index Layering:** Overlay (1399), Right Sidebar (1400), Left Sidebar (1410), Stacked Sidebar (1420).

- **Google Calendar-Style Agenda Grid:**
  - **CSS Grid Layout:** Use a custom CSS Grid inside a `MudPaper`.
    - Columns: `[TimeAxis] 60px [Room1] 1fr [Room2] 1fr ...`.
    - Rows: Dynamic based on time increments (e.g., 30min slots).
  - **EventAgendaItem (Full-Width):** `grid-column: 2 / -1` to span all rooms.
  - **EventSession (Room-Bound):** `grid-column: n + 1` where `n` is the room index.
  - **Sticky Context:** Use `position: sticky` for the time-axis column and room-header row.
  - **Interactive Elements:** `MudPaper` cards for sessions with `MudTooltip` for quick info and `OnClick` to trigger the detailed stack/popover.

- **In-Place Agenda Management (Authorized Users):**
  - **Contextual Editing:** When a user is authorized (`_canEdit` or HATEOAS `edit` link exists), the `EventDetail` and `EventList` sidebars transform into management surfaces.
  - **Agenda Controls:** Add buttons for "Add Day", "Add Room", and "Add Agenda Item" directly within the agenda grid.
  - **Inline Actions:** Individual sessions and agenda items in the grid display "Edit" and "Delete" icons on hover/selection.
  - **Consistency:** Use the same `MudDrawer` stack (Miller Columns) for editing as used for viewing, allowing for a seamless transition between "inspecting" and "modifying".

- **Configurable Tenant Behavior:**
  - Leverage a `UIConfigurationService` (scoped) to hold tenant preferences.
  - Use `MudPopover` for the Google Calendar-style "Quick Info" popups if the tenant preference is set to `Popover`.
  - Components will dynamically switch between `MudPopover` and `MudDrawer` stacking based on `UIConfigurationService`.
  - **Admin Preference:** Allow tenants to configure if "Edit Mode" should be the default for authorized users upon opening the detail view.

- **Styling Conventions:**
  - **CSS Isolation:** Use `.razor.css` files with `::deep` for MudBlazor child components.
  - **BEM Methodology:** Follow the `block__element--modifier` naming convention as seen in `MainLayout.razor.css`.
  - **Design System:** Use MudBlazor theme variables (`var(--mud-palette-primary)`, `var(--mud-palette-surface)`, etc.) and custom `isl-space` variables for spacing.

## Implementation Phases

### Phase 0: Audit, alignment, and safety rails

1. Freeze the target terminology and naming before coding.
2. Align with current `session-series-ux` work so the UI refactor composes cleanly.
3. Clean up pre-flight build blockers enough to verify the scheduling branch safely.
4. Write ADR/docs before schema churn so migration intent is explicit.

### Phase 1: Additive schema foundation only

1. Add new domain/persistence concepts with no breaking rewrites:
   - `EventDay`
   - `EventAgendaItem`
   - `LocationRoom`
   - lookup/type support
2. Keep new FKs nullable at introduction time.
3. Add architecture tests and named query-filter coverage for all new tenant entities.

### Phase 2: Existing entity wiring and additive extensions

1. Confirm and normalize `EventSeries` usage across DTOs, handlers, and UI.
2. Add nullable FK wiring to current entities for event day/room/type support.
3. Add cached local projection fields.
4. Add missing unique indexes and constraints.

### Phase 3: Registration refactor in isolation

1. Add the parent registration-intent/group layer without destroying the meaning of current session-level rows.
2. Keep `EventRegistration` as the child concrete entitlement/access row where practical, or adapt it explicitly as the child layer if naming requires migration.
3. Add event registration policy fields on `Event`.
4. Preserve backward compatibility for existing session-level flows during rollout.

### Phase 4: Persistence, migrations, and backfill

1. Update EF configurations, DbSets, repository interfaces/implementations.
2. Add migration(s) with data backfill strategy.
3. Backfill existing session-level registrations into the new parent-child model.
4. Backfill event-day rows from existing sessions using event timezone.
5. Rebuild schema docs / DBML reference.

### Phase 5: Application and API refactor

1. Refactor create/update event/session/agenda/registration commands and validators.
2. Add queries for:
   - event agenda by local day,
   - session detail with session agenda,
   - registration options/policies,
   - admin day management surfaces.
3. Update DTOs/contracts, mapping profile, and OpenAPI-driven client surface.
4. Preserve HATEOAS/API conventions.

### Phase 6: OpenAPI/NSwag boundary

1. Regenerate OpenAPI/swagger and NSwag client only after API contracts settle for the phase.
2. Remove client-side DTO shims where the formal schema now covers them.

### Phase 7: Blazor UI refactor

1. Overhaul `CreateEvent.razor` and `EditEvent.razor` to support `EventDay`, `LocationRoom`, `EventAgendaItem`, and the new Registration Policy model.
2. Build "Miller Column" stack orchestration using `MudDrawer` and custom `ZIndex` layers.
3. Build the CSS Grid agenda (vertical time axis, horizontal room columns, full-width bands) inside a `MudPaper` container.
4. Implement the `UIConfigurationService` (scoped) to hold tenant preferences for sidebar/redirect/popover behaviors.
5. Implement in-place management UI (Add/Edit/Delete for days/rooms/agenda) within `EventDetail.razor` and the `EventList` sidebar, guarded by HATEOAS/authorization logic.
6. Apply CSS isolation and BEM conventions to all new components (`.razor.css` with `::deep`).
7. Refactor registration UX to policy-aware flows using `MudRadioGroup` or `MudToggleGroup`.
8. Keep session detail separate from event-level agenda.

### Phase 8: Hardening

1. Add conflict prevention and performance tuning.
2. Tighten docs, tests, observability, and rollout notes.
3. Validate migration/backfill assumptions against realistic fixtures.

## Atomic Commit Strategy

Recommended commit decomposition:

1. ADR + planning docs + architecture test updates.
2. New additive domain entities only.
3. New EF configurations/DbSets/query filters only.
4. Additive migration for new tables only.
5. Nullable FK additions to existing entities/configurations.
6. Unique/index/constraint hardening.
7. Registration model entity/config changes only.
8. Registration handlers/validators/tests only.
9. Event day + agenda query/handler additions only.
10. API controller/RouteNames/HATEOAS additions only.
11. OpenAPI + NSwag regeneration only.
12. Blazor services only.
13. Blazor page/component updates only.
14. Final docs/schema/ADR cleanup.

## Detailed Tasks

### Phase 0: Planning and repo alignment

#### 0.1 Lock terminology and invariants
- Effort: M
- Related Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] ADR defines `EventSeries`, `Event`, `EventDay`, `EventSession`, `EventAgendaItem`, and `EventSessionAgendaItem` distinctly.
  - [ ] ADR states that business grouping uses event timezone, not viewer timezone.
  - [ ] ADR states that recurrence is out of scope.

#### 0.2 Reconcile with existing active UI work
- Effort: M
- Related Skills: `blazor-ui-conventions`
- Acceptance Criteria:
  - [ ] Plan explicitly identifies overlap with `dev/active/session-series-ux/`.
  - [ ] Agenda/registration refactor reuses existing shared workflows where appropriate.
  - [ ] No duplicate event-series UX track is created.
  - [ ] Existing `EventSeriesSection`, `SessionSummaryCard`, and session workflow abstractions are treated as extension points, not new work from scratch.

#### 0.3 Stabilize verification baseline
- Effort: M
- Related Skills: `agentic-research`
- Acceptance Criteria:
  - [ ] File-lock/build issue is understood and documented.
  - [ ] Refactor branch has a reliable build/test verification sequence.
  - [ ] Pre-existing warnings/failures are listed separately from refactor regressions.

### Phase 1: Domain layer

#### 1.1 Add `EventDay`
- Effort: L
- Related Skills: `clean-architecture-rules`, `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] New domain entity exists with event-local date, label, description, note/banner/publish/order metadata.
  - [ ] Supports tenant, audit, soft delete, and concurrency consistently with surrounding aggregates.
  - [ ] Encodes that rows belong to one event only.
  - [ ] Initial relationship strategy keeps `EventSession.EventDayId` nullable during rollout.
  - [ ] ADR section explicitly documents *why* `EventDay` is a persistent entity and not a derived grouping. Required reasons to enumerate: custom day labels, day-specific descriptions/banners, day-specific publishing state, day-level admin UX (reorder/lock/hide/attach media), and day-level registration/business rules that require a stable primary key the registration-intent model can foreign-key to. If any of these five justifications no longer holds, the decision must be revisited.

#### 1.2 Add `EventAgendaItem`
- Effort: L
- Related Skills: `clean-architecture-rules`, `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Domain entity exists with `EventId`, UTC start/end, `Title`, `Description`, `LocationId`, optional `RoomId`, `Kind`, `SortOrder`, audit data, and cached local projections.
  - [ ] Cannot exist without a parent event.
  - [ ] No rename of `EventSessionAgendaItem` is required in the first rollout.

#### 1.3 Add `LocationRoom`
- Effort: M
- Related Skills: `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Room belongs to a `Location` and tenant.
  - [ ] Supports `Name`, optional `Slug`, optional `Capacity`, `SortOrder`.
  - [ ] `EventSession` and optional `EventAgendaItem` can reference it.

#### 1.4 Refactor `EventSession`
- Effort: XL
- Related Skills: `clean-architecture-rules`, `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Adds room support, cached local projections, and ordering.
  - [ ] Preserves existing pricing/capacity/registration semantics while expanding to multi-day/conference cases.
  - [ ] End-time validation and room conflict rules are planned.

#### 1.5 Refactor `EventRegistration` into parent intent
- Effort: XL
- Related Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Plan introduces a new parent registration-intent/group layer rather than redefining all existing session-level rows as abstract registrations.
  - [ ] Parent rows store `EventId`, `UserId`, `RegistrationScope`, optional `SelectedLocalDate`, status fields, and optional policy snapshot/metadata.
  - [ ] Child rows remain the concrete access/entitlement rows at session scope.
  - [ ] Existing session-level semantics are preserved via migration/backfill.
  - [ ] Existing session-level registration contract remains temporarily compatible.

#### 1.6 Add registration policy to `Event`
- Effort: M
- Related Skills: `clean-architecture-rules`
- Acceptance Criteria:
  - [ ] `Event` stores organizer-controlled scope policy.
  - [ ] Allowed registration paths can be derived without guessing from sessions.

#### 1.7 Add schedule item type/kind lookup support
- Effort: M
- Related Skills: `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Event agenda items and session agenda items can classify items such as `Intro`, `Talk`, `Q&A`, `Break`, `Prayer`, `Outro`, `Logistics`, `Custom`.
  - [ ] Lookup seeding and enum/table strategy are consistent.

#### 1.8 Add session taxonomy junctions
- Effort: M
- Related Skills: `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] `event_session_categories` and `event_session_tags` exist.
  - [ ] Event vs session taxonomy semantics are documented and distinct.

### Phase 2: Persistence and migrations

#### 2.1 Add DbSets and EF configurations for new entities
- Effort: L
- Related Skills: `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] `ExploreDbContext` contains verified DbSets for all new tables.
  - [ ] Configurations define keys, FKs, delete behavior, named query filters, and indexes.
  - [ ] Every new tenant-scoped entity is covered by architecture/query-filter verification.

#### 2.2 Replace session-registration uniqueness with scope-aware uniqueness
- Effort: L
- Related Skills: `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Unique parent whole-event registration intent per user/event.
  - [ ] Unique parent day-registration intent per user/event/local-date.
  - [ ] Unique child entitlement rows per parent/session.
  - [ ] Existing session-level uniqueness is transitioned safely rather than broken abruptly.

#### 2.3 Add missing unique constraints on current and new junctions
- Effort: M
- Related Skills: `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Unique `(EventId, CategoryId)`.
  - [ ] Unique `(EventId, TagId)`.
  - [ ] Unique `(EventSessionId, ActorId)`.
  - [ ] Unique session taxonomy junctions.

#### 2.4 Add cached local projection fields and indexes
- Effort: L
- Related Skills: `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] New local projection columns exist on `EventSession` and `EventAgendaItem`.
  - [ ] Indexes support `TenantId + LocalStartDate + LocalStartTime` style queries.
  - [ ] Source of truth remains UTC timestamps + event timezone.

#### 2.5 Add `EventDay` materialization/backfill
- Effort: XL
- Related Skills: `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Migration creates `EventDay` rows for existing events based on existing sessions and event timezone.
  - [ ] Existing events with missing timezone are handled explicitly by migration strategy.
  - [ ] Backfill is deterministic and idempotent where possible.

#### 2.6 Add room conflict protection (two mandatory layers)
- Effort: L
- Related Skills: `dotnet-efcore-guidelines`, `cqrs-mediatr-guidelines`
- Acceptance Criteria:
  - [ ] Same room cannot host overlapping sessions for the same tenant/location.
  - [ ] **Layer A (necessary, not sufficient):** async FluentValidation rule in create/update session command validators rejects same-room overlap with a clear ProblemDetails error. This is the UX layer.
  - [ ] **Layer B (sufficiency):** the existing concurrency-stamp pattern on `EventSession` is reused to guarantee correctness under concurrent writes. Stamps of overlap-candidate rows read during Layer A are re-verified at save time, so any racing insert/update in the same room forces `DbUpdateConcurrencyException` and a retry. No new concurrency primitive is introduced.
  - [ ] No backwards-compatibility path: if legacy tests, fixtures, or seed data rely on overlap being silently allowed, they are updated in the same change-set. The project is in development mode; break and fix.
  - [ ] Out-of-band mutation paths (seeders, imports, background jobs) that touch `EventSession` schedule fields are routed through the same aggregate method / domain service used by command handlers, so Layer B applies there too.
  - [ ] Constraint strategy for later persistence hardening (e.g. PostgreSQL exclusion constraints on a generated tstzrange) is documented as an optional future upgrade, not as a prerequisite.

#### 2.7 Keep new foreign keys additive first
- Effort: M
- Related Skills: `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] `EventDayId`, `RoomId`, and related rollout FKs are introduced nullable first.
  - [ ] Requiredness, if any, is enforced later by validation/business rules before schema hardening.

### Phase 3: Application layer

#### 3.1 Refactor event commands/validators
- Effort: XL
- Related Skills: `cqrs-mediatr-guidelines`, `clean-architecture-rules`
- Acceptance Criteria:
  - [ ] Event create/update supports event series, event location, registration policy, and day-aware timezone validation.
  - [ ] Validators stay manually instantiated.

#### 3.2 Refactor session commands/validators
- Effort: XL
- Related Skills: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Session create/update enforces new semantics and room/day integrity.
  - [ ] Local projection recomputation is centralized in the `IEventScheduleProjectionCalculator` domain service and invoked exclusively through aggregate methods on `EventSession` / `Event`. Handlers never touch `LocalStart*` / `LocalEnd*` directly; validators never write local fields; mapping profiles never compute them. Architecture tests enforce this.
  - [ ] When `Event.Timezone` changes, the `Event` aggregate fans out the re-projection to all loaded `Sessions` and `AgendaItems` via the calculator in a single unit of work.
  - [ ] Same-room overlap rejection is represented as validator-driven fail-fast behavior with clear error messages (Layer A) **and** re-verified via the existing concurrency-stamp pattern at save time (Layer B). Both layers are mandatory.
  - [ ] Tests cover the race window: a handler test asserts that two parallel overlap attempts cannot both commit — exactly one wins and the other raises `DbUpdateConcurrencyException`.

#### 3.3 Add event agenda item commands/queries
- Effort: L
- Related Skills: `cqrs-mediatr-guidelines`
- Acceptance Criteria:
  - [ ] CRUD exists for event-level agenda items.
  - [ ] Read models expose them separately from session agenda items.

#### 3.4 Add event-day commands/queries
- Effort: XL
- Related Skills: `cqrs-mediatr-guidelines`
- Acceptance Criteria:
  - [ ] Admin can create/update labels, descriptions, banners/notes, publish state, and sort order.
  - [ ] Event day landing sections have read models.

#### 3.5 Refactor registration commands/queries by scope
- Effort: XL
- Related Skills: `cqrs-mediatr-guidelines`, `clean-architecture-rules`
- Acceptance Criteria:
  - [ ] Separate request contracts support `Event`, `Day`, and `SessionSelection` flows.
  - [ ] Organizer policy is enforced fail-fast.
  - [ ] Session selections must belong to the same event.
  - [ ] Parent intent records preserve why the user registered.
  - [ ] Child session rows preserve what access the user actually received.

#### 3.6 Add optimized agenda projection queries
- Effort: XL
- Related Skills: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Query returns a unified event agenda grouped by local day and room.
  - [ ] Event-level bands and room-bound session blocks are distinct in the projection.

### Phase 4: API and contracts

#### 4.1 Update DTOs and mapping
- Effort: XL
- Related Skills: `cqrs-mediatr-guidelines`
- Acceptance Criteria:
  - [ ] Event DTOs expose series, policy, event days, and agenda projection summaries as needed.
  - [ ] Session DTOs expose room, local projections, and session taxonomy.
  - [ ] Registration DTOs reflect parent-child intent model.

#### 4.2 Add/update API endpoints
- Effort: XL
- Related Skills: `cqrs-mediatr-guidelines`, `clean-architecture-rules`
- Acceptance Criteria:
  - [ ] Endpoints exist for event days, event agenda items, room management, and registration-by-scope.
  - [ ] GET stays `[AllowAnonymous]` where appropriate; writes stay `[Authorize]`.
  - [ ] New endpoints follow ProblemDetails, HATEOAS, and output-cache conventions where applicable.

#### 4.3 Regenerate OpenAPI/NSwag surface
- Effort: M
- Related Skills: `agentic-research`
- Acceptance Criteria:
  - [ ] Client contract drift is resolved.
  - [ ] Blazor generated client/types reflect new DTOs.
  - [ ] Existing `Explore.Blazor.Client/Clients/DtoPartials.cs` workarounds for scheduling fields are removed or reduced because the official schema catches up.
  - [ ] Regeneration happens only after API contract changes for the slice are complete.

### Phase 5: Blazor UI

#### 5.1 Page and Editor Overhaul
- Effort: XL
- Related Skills: `blazor-ui-conventions`
- Acceptance Criteria:
  - [ ] `CreateEvent.razor` and `EditEvent.razor` support event series, event-level location/room management, event-day definitions, and registration policy selection.
  - [ ] Session management in the editor is room-aware and date-aware.
  - [ ] UI reflects the new registration intent model (e.g., selecting registration scope).

#### 5.2 Agenda view & In-Place Management
- Effort: XL
- Related Skills: `blazor-ui-conventions`, `blazor-css-isolation`
- Acceptance Criteria:
  - [ ] Desktop/Tablet uses a CSS Grid `(Time x Rooms)`.
  - [ ] Vertical time axis with horizontal room columns.
  - [ ] Shared `EventAgendaItem` blocks render as full-width bands (spanning all room columns).
  - [ ] Sticky headers for columns and rows.
  - [ ] Mobile view collapses to single-room focus mode with room-toggle.
  - [ ] Interactive blocks support both popovers and sidebar stacking depending on configuration.
  - [ ] **Authorized Users:** Agenda view displays management controls (Add Day, Edit Room, etc.) and inline "Edit" actions for sessions/items.

#### 5.3 Session detail view
- Effort: M
- Related Skills: `blazor-ui-conventions`
- Acceptance Criteria:
  - [ ] Only session-owned agenda items appear.
  - [ ] Event-level agenda items do not bleed into session detail.

#### 5.4 Registration UX refactor
- Effort: XL
- Related Skills: `blazor-ui-conventions`
- Acceptance Criteria:
  - [ ] Whole-event, day, and selected-session flows render based on organizer policy.
  - [ ] Clear messaging explains what is included by each scope.
  - [ ] EventDetail no longer infers event registration by checking raw session rows only.
  - [ ] The current “Register for ALL Sessions” UX is replaced or reinterpreted so whole-event intent is explicitly modeled instead of serialized as a session ID list.

#### 5.5 Timezone display UX
- Effort: M
- Related Skills: `blazor-ui-conventions`
- Acceptance Criteria:
  - [ ] Event timezone is the default schedule display context.
  - [ ] Viewer-local display is optional and presentation-only.
  - [ ] JS timezone detection is deferred safely for InteractiveAuto/prerendering.

#### 5.6 Tenant UI Configuration
- Effort: M
- Related Skills: `blazor-ui-conventions`
- Acceptance Criteria:
  - [ ] Tenant admins can configure interaction behaviors:
    - `EventClickAction`: `OpenSidebar` | `Redirect`
    - `AgendaItemClickAction`: `OpenPopover` | `OpenStackedSidebar`
  - [ ] UI state container respects tenant configuration during navigation/interaction orchestration.

### Phase 6: Tests and documentation

#### 6.1 Domain/unit tests
- Effort: L
- Related Skills: `clean-architecture-rules`
- Acceptance Criteria:
  - [ ] Covers timezone-day derivation, cache recomputation, scope-policy enforcement, and agenda semantics.

#### 6.2 Persistence/integration tests
- Effort: XL
- Related Skills: `dotnet-efcore-guidelines`
- Acceptance Criteria:
  - [ ] Covers mappings, migrations, unique constraints, soft delete, room conflicts, and event-day backfill.

#### 6.3 Application tests
- Effort: XL
- Related Skills: `cqrs-mediatr-guidelines`
- Acceptance Criteria:
  - [ ] Covers event/day/session registration flows and invalid combinations.

#### 6.4 Blazor/bUnit tests
- Effort: L
- Related Skills: `blazor-ui-conventions`
- Acceptance Criteria:
  - [ ] Covers agenda rendering, room filters, registration options, and timezone display behavior.

#### 6.5 Documentation updates
- Effort: L
- Related Skills: `agentic-research`
- Acceptance Criteria:
  - [ ] Update domain docs, schema docs, ADR, and developer notes.
  - [ ] Explicitly document why `EventDay` exists, why `EventSeries` is not recurrence, and why event timezone anchors business logic.

## Risk Assessment and Mitigation

| Risk | Impact | Mitigation |
|---|---|---|
| Backfilling current session-level registrations into parent-child registrations can distort semantics | High | Preserve original row meaning in migration metadata, backfill conservatively, add migration verification fixtures |
| Missing or inconsistent event timezones break day derivation | High | Add timezone audit and pre-migration remediation path before enforcing day-based logic |
| Existing UI assumes registration == any session row | High | Refactor read models and UI together; add compatibility adapters temporarily |
| Current active `session-series-ux` work diverges from this refactor | Medium | Reuse/extend that track instead of creating parallel abstractions |
| OpenAPI/NSwag churn creates wide diffs | Medium | Stage DTO/API changes tightly and regenerate once per contract milestone |
| Room conflict prevention is hard to guarantee with only app-level checks | High | Mandatory two-layer enforcement: async FluentValidation for UX (Layer A, necessary but not sufficient) plus reuse of the existing concurrency-stamp pattern on `EventSession` to close the check-then-act race (Layer B). Both layers land in the same PR; no legacy bypass path is kept |
| Local projection recompute logic becomes scattered across handlers, validators, mappers, and seeders | High | Single domain service `IEventScheduleProjectionCalculator` owns the UTC→local conversion; entities expose aggregate methods that accept the calculator; architecture tests ban direct writes to `LocalStart*`/`LocalEnd*` from anywhere else |
| Build baseline is already noisy | Medium | Track pre-existing warnings separately and verify touched paths/project slices incrementally |
| Deep cascade/delete and soft-delete interactions around `Event -> EventDay -> EventSession` can create subtle data behavior | Medium | Keep EventSession linked directly to Event, introduce EventDay as grouping FK first, and test delete behavior explicitly |

## Success Metrics

- Event detail can render a unified multi-day agenda with rooms and shared event bands.
- Registration flows preserve intent and support policy-aware UX.
- Queries group by event-local day correctly across DST boundaries.
- Admin can manage day labels, publish state, and room assignments explicitly.
- New schema supports performance-sensitive day agenda queries without repeated runtime timezone conversion.
- Each phase exits with the relevant test slice green and no unplanned contract drift.

## Required Resources and Dependencies

- Internal: `Explore.Domain`, `Explore.Application`, `Explore.Persistence`, `Explore.API`, `Explore.Blazor.Client`, test projects.
- Internal docs: `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/API.md`, `docs/SECURITY.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/CODEBASE_INSIGHTS.md`, `docs/QUICK_REFERENCE.md`.
- Skills: `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `blazor-ui-conventions`, `blazor-css-isolation`, `agentic-research`.
- External guidance already incorporated: official .NET/EF Core/Blazor docs for timezone conversion, DST ambiguity, named query filters, indexing, virtualization, and prerender-safe JS interop.

## Effort Estimates

- Phase 0: M
- Phase 1: XL
- Phase 2: XL
- Phase 3: XL
- Phase 4: XL
- Phase 5: XL
- Phase 6: L

Overall estimate: XL epic split across multiple PRs/issues. Recommended decomposition: foundation schema, registration refactor, agenda projection/API, UI/admin, hardening/docs.

## Potential Risks & Unknowns

The highest-risk area is the registration migration. The repo currently stores only session-level registrations in `Explore.Domain/EventRegistration.cs` and `event_registrations`, so the safest direction is not to redefine those rows abstractly but to add a parent intent/group layer above them. That keeps the child rows as concrete session entitlements while preserving intent and organizer policy semantics separately. The second major risk is event-day derivation when existing events have incomplete or inconsistent timezone data; without a clean timezone baseline, `EventDay` and cached local projection fields can be populated incorrectly.

The main sequencing trap is trying to do schema, API, NSwag, and Blazor in one jump. The safer path is additive schema first, isolated registration refactor second, API stabilization third, NSwag regeneration fourth, and only then broad Blazor updates.
