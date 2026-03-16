Last Updated: 2026-03-16 Europe/Brussels

# Tasks: Event Scheduling Refactor

## Session Checkpoint (2026-03-16 Europe/Brussels)

- ✅ Registration wording revised: parent rows = intent/group semantics, child rows = concrete session entitlements/access.
- ✅ Same-room overlap enforcement strategy documented: async FluentValidation in create/update session DTO validators first.
- ✅ Plan sequencing tightened: additive schema first, isolated registration phase, NSwag boundary before broad Blazor work.
- 🟡 Current in-progress work is still planning-only: next step is PR-slice decomposition, not implementation.
- ⚠️ Keep the UI/UX section already present in the plan intact unless the user explicitly asks to revise it again.

## Phase 0 - Audit, ADR, and baseline

- [ ] Finalize ADR for `EventSeries`, `EventDay`, `EventAgendaItem`, registration scopes, and timezone rules.
- [ ] Reconcile this refactor with `dev/active/session-series-ux/` and record shared ownership boundaries.
- [ ] Stabilize build/test verification baseline enough for iterative refactor PRs.
- [ ] Capture migration/backfill assumptions for registrations and event days.
- [ ] Remove or minimize current `DtoPartials` scheduling workarounds by fixing schema/client drift in the proper contracts.
- [ ] Lock phase boundaries: additive schema first, registration isolated, NSwag boundary before broad Blazor work.
- [ ] Define atomic commit plan before implementation starts.
- [ ] Lock the registration wording so parent rows = intent/group and child rows = concrete session entitlements/access.

## Phase 1 - Domain and schema foundation

- [ ] Add `EventDay` domain entity.
- [ ] Add `EventAgendaItem` domain entity.
- [ ] Add `LocationRoom` domain entity.
- [ ] Add parent registration intent/group entity.
- [ ] Keep or adapt `EventRegistration` as the child concrete session entitlement/access entity.
- [ ] Add registration policy to `Event`.
- [ ] Refactor `EventSession` semantics and fields.
- [ ] Add schedule item type/kind support.
- [ ] Add session taxonomy junction entities.

## Phase 2 - Persistence and migrations

- [ ] Add DbSets for all new entities.
- [ ] Add EF configurations and named query filters.
- [ ] Add missing unique constraints on current junctions.
- [ ] Add cached local projection columns and indexes.
- [ ] Add room conflict protection.
- [ ] Implement same-room overlap checks in create/update session DTO validators with async repository-backed validation.
- [ ] Keep newly introduced rollout FKs nullable first.
- [ ] Create migration(s) for new schema.
- [ ] Backfill existing registrations into parent-child model.
- [ ] Backfill `EventDay` rows from existing data.
- [ ] Update `schemas/islamu-event.md`.

## Phase 3 - Application layer

- [ ] Refactor event create/update commands and validators.
- [ ] Refactor session create/update commands and validators.
- [ ] Add event agenda item commands/queries.
- [ ] Add event day commands/queries.
- [ ] Add registration-by-scope commands/queries.
- [ ] Add room management commands/queries.
- [ ] Add agenda projection queries grouped by local day and room.
- [ ] Update AutoMapper profile.

## Phase 4 - API and contracts

- [ ] Update event/session/registration DTOs.
- [ ] Add DTOs for event day, event agenda item, room, registration child rows, and policy/scope projections.
- [ ] Add/update API controllers/endpoints.
- [ ] Preserve ProblemDetails, HATEOAS, and auth conventions.
- [ ] Regenerate OpenAPI/NSwag contracts.

## Phase 5 - Blazor UI

- [ ] Overhaul `CreateEvent.razor` and `EditEvent.razor` to support event-level location, days, rooms, and registration policies.
- [ ] Implement `UIConfigurationService` for tenant UI preferences.
- [ ] Build "Miller Column" stack using `MudDrawer` (Primary: Detail, Secondary: Agenda, Tertiary: Session).
- [ ] Set custom `ZIndex` and `backdrop-filter: blur(0.75rem)` for stacked drawers.
- [ ] Build CSS Grid-based agenda component inside a `MudPaper` container.
- [ ] Implement full-width bands for shared items (`grid-column: 2 / -1`).
- [ ] Implement in-place management UI (Add Day/Room/Agenda) within `EventDetail.razor` and `EventList` sidebars, guarded by HATEOAS/authorization logic.
- [ ] Add inline "Edit" and "Delete" icons for sessions/items in the agenda grid for authorized users.
- [ ] Implement mobile room-focused agenda using `MudSwipeArea` or `MudTabs`.
- [ ] Add sticky positioning for agenda time axis and room headers.
- [ ] Create `.razor.css` files for all new components using BEM and `::deep`.
- [ ] Refactor registration UX using `MudRadioGroup` for policy-aware selection.
- [ ] Reuse existing `EventSeriesSection`, `SessionSummaryCard`, and session workflow abstractions.

## Phase 6 - Tests and docs

- [ ] Add/expand domain unit tests.
- [ ] Add/expand persistence integration tests.
- [ ] Add/expand application tests.
- [ ] Add/expand Blazor component tests.
- [ ] Update docs/ADR/schema/developer notes.

## Immediate Next Steps

- [ ] Break the epic into implementation PR slices. 🟡 CURRENT NEXT STEP
- [ ] Identify the first safe foundation PR.
- [ ] Decide the first PR: ADR/tests guardrails vs additive schema entities.
- [ ] Define the exact shape/naming of the parent registration table before code changes (`EventRegistrationIntent` vs `EventRegistrationGroup`).
- [ ] Identify repository/service contract additions needed for same-room overlap validator checks.
