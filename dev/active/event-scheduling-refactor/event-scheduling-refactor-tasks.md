Last Updated: 2026-04-13 Europe/Brussels

# Tasks: Event Scheduling Refactor

## Session Checkpoint (2026-04-12 Europe/Brussels)

- ✅ Plan updated with EventDay justification, two-layer overlap enforcement, projection calculator ownership.
- ✅ 12 implementation slices delivered, all passing (65 Architecture + 100 Domain + 712 Application + 190 Secrets tests).
- ✅ Registration intent-first flow is live: handler creates parent + children atomically.
- ✅ Phase 2.7 EventDayId auto-linking: both session handlers now auto-link EventDayId after Reschedule().
- ✅ Phase 3.3+3.4: Full CRUD for EventDay and EventAgendaItem with DTOs, validators, handlers, auth, Cerbos policies.
- 🟡 NSwag client stale — Blazor.Client generated DTO still has old shape. Phase 4/6 fix.
- ⚠️ Blazor.Client has pre-existing compile errors from blazor-localization branch (not from this refactor).
- ⚠️ Architecture test: 1 pre-existing failure (GovernanceReportFilter naming) — not from this refactor.

## Phase 0 - Audit, ADR, and baseline

- [x] Stabilize build/test verification baseline enough for iterative refactor PRs.
- [x] Lock phase boundaries: additive schema first, registration isolated, NSwag boundary before broad Blazor work.
- [x] Define atomic commit plan before implementation starts.
- [x] Lock the registration wording so parent rows = intent/group and child rows = concrete session entitlements/access.
- [ ] Finalize ADR for `EventSeries`, `EventDay`, `EventAgendaItem`, registration scopes, and timezone rules.
- [ ] Reconcile this refactor with `dev/active/session-series-ux/` and record shared ownership boundaries.

## Phase 1 - Domain and schema foundation

- [x] Add `EventDay` domain entity.
- [x] Add `EventAgendaItem` domain entity.
- [x] Add `LocationRoom` domain entity.
- [x] Add parent registration intent/group entity (`EventRegistrationIntent`).
- [x] Wire `EventRegistration` as the child concrete session entitlement/access entity (nullable `EventRegistrationIntentId`).
- [x] Add registration policy to `Event` (`RegistrationPolicyId` FK).
- [x] Refactor `EventSession` semantics and fields (local projections, RoomId, EventDayId, SortOrder, aggregate methods).
- [x] Add schedule item type/kind support (`ScheduleItemKind` + enum).
- [x] Add session taxonomy junction entities (`EventSessionCategory`, `EventSessionTag`).
- [x] Add `IEventScheduleProjectionCalculator` domain service + implementation.
- [x] Add `RegistrationPolicyRules` domain service.
- [x] Add `RegistrationScope` lookup + enum.

## Phase 2 - Persistence and migrations

- [x] Add DbSets for all new entities.
- [x] Add EF configurations and named query filters.
- [x] Add missing unique constraints on current junctions.
- [x] Add cached local projection columns and indexes.
- [x] Add room conflict protection (two-layer: async validator + serializable tx guard).
- [x] Implement same-room overlap checks in session validators with async repo-backed validation.
- [x] Keep newly introduced rollout FKs nullable first.
- [x] Create migration(s) for new schema. (User ran migrations after each slice.)
- [x] Phase 2.2: Add partial unique indexes for EventRegistrationIntent parent uniqueness.
- [ ] Phase 2.5: Backfill `EventDay` rows from existing sessions using event timezone.
- [x] Phase 2.7: Auto-link `EventSession.EventDayId` during Reschedule (match EventDay by EventId + LocalStartDate).
- [ ] Backfill existing `EventRegistration` rows into parent-child model (set `EventRegistrationIntentId` NOT NULL after).
- [x] Update `schemas/islamu-event.md`. ✅ All new entities, relationships, enums added.

## Phase 3 - Application layer

- [x] Refactor session create/update commands and validators (overlap guard + projection calculator).
- [x] Refactor registration commands/validators (intent-first flow with policy enforcement).
- [x] Update AutoMapper profile (removed stale CreateMap).
- [x] Register `IEventScheduleProjectionCalculator` singleton in DI.
- [x] Add `IEventDayRepository` + `IEventRegistrationIntentRepository` + DI wiring.
- [x] Phase 3.1: Refactor event create/update commands for RegistrationPolicyId + series wiring.
- [x] Phase 3.3: Add event agenda item CRUD commands/queries.
- [x] Phase 3.4: Add event day CRUD commands/queries.
- [x] Phase 3.6: Add agenda projection queries grouped by local day and room.
- [x] Add room management commands/queries.

## Phase 4 - API and contracts

- [x] Update event/session/registration DTOs for read models.
- [x] Add DTOs for event day, event agenda item, room, registration intent, and policy/scope projections.
- [x] Add/update API controllers/endpoints.
- [x] Preserve ProblemDetails, HATEOAS, and auth conventions.
- [ ] Regenerate OpenAPI/NSwag contracts. 🟡 USER ACTION — run swagger + NSwag regen to pick up 6 new controllers.

## Phase 5 - Blazor UI

- [ ] Overhaul `CreateEvent.razor` and `EditEvent.razor` to support days, rooms, registration policies.
- [ ] Build CSS Grid-based agenda component.
- [ ] Build "Miller Column" stack using `MudDrawer`.
- [ ] Implement in-place management UI (Add Day/Room/Agenda).
- [ ] Refactor registration UX using `MudRadioGroup` for policy-aware selection.
- [ ] Update Blazor services and pages for new `CreateEventRegistrationDto` shape.
- [ ] Reuse existing `EventSeriesSection`, `SessionSummaryCard`, and session workflow abstractions.

## Phase 6 - Tests and docs

- [x] Add/expand domain unit tests (timezone projection, policy rules, aggregate methods). ✅ 192 tests passing
- [ ] Add/expand persistence integration tests (overlap guard serializable tx, backfill).
- [x] Add/expand application tests (handler tests for EventDay, EventAgendaItem, LocationRoom CRUD + AgendaProjection + RegistrationScope). ✅ 822 tests passing
- [ ] Add/expand Blazor component tests.
- [x] Update docs/ADR/schema/developer notes (schemas/islamu-event.md updated). ✅

## Immediate Next Steps

- [x] Phase 2.7: Auto-link `EventSession.EventDayId` in `Reschedule()`. ✅ DONE
- [x] Phase 3.3 + 3.4: Event agenda item + event day CRUD commands. ✅ DONE
- [x] Phase 3.1: Event command refactor for RegistrationPolicyId. ✅ DONE
- [x] Phase 2.2: Partial unique indexes for EventRegistrationIntent parent uniqueness. ✅ DONE
- [x] Phase 3.6: Agenda projection queries grouped by local day and room. ✅ DONE
- [x] Add room management commands/queries. ✅ DONE
- [ ] Phase 2.5: EventDay backfill migration from existing sessions. 🟡 Skipped (dev mode)
- [x] Phase 4: API controllers + NSwag boundary. ✅ DONE (controllers + DTOs + HATEOAS + AutoMapper + repos)
- [ ] NSwag regeneration: User must regenerate swagger.json + EventApiClient.g.cs. 🟡 BLOCKER for Phase 5
- [ ] Phase 5: Blazor UI. 🟡 BLOCKED on NSwag regen
- [x] Phase 6: Domain tests (192 passing) + Application tests (822 passing) + Schema docs. ✅ DONE
- [ ] Phase 6 remaining: Persistence integration tests, Blazor component tests.
