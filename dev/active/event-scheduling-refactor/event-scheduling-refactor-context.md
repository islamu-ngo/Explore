Last Updated: 2026-04-13 Europe/Brussels

# Context: Event Scheduling Refactor

## SESSION PROGRESS (2026-04-12 Europe/Brussels)

### ✅ COMPLETED (12 implementation slices delivered)

**Plan update (pre-implementation):**
- Updated plan with 3 targeted changes: (1) justified EventDay as first-class entity, (2) reframed same-room FluentValidation as necessary-but-not-sufficient + concurrency enforcement, (3) locked local-projection recompute to a single domain service `IEventScheduleProjectionCalculator`.

**Slice 1 — Phase 1 additive schema foundation:**
- Created `LocationRoom`, `EventDay`, `EventAgendaItem`, `ScheduleItemKind` + enum domain entities.
- Created `IEventScheduleProjectionCalculator` + `EventScheduleProjectionCalculator` + `LocalScheduleProjection` in `Explore.Domain/Services/Scheduling/`.
- Extended `EventSession` with: `EventDayId` (nullable), `RoomId` (nullable), `SortOrder`, 6 cached local projection fields, `Reschedule()` and `ReprojectLocalTimes()` aggregate methods.
- Created EF configs, DbSets, named query filters, lookup seeder for all new entities.
- User ran migration after this slice.

**Slice 2 — Phase 1.6 + 1.8 + 2.3:**
- Created `EventRegistrationPolicy` lookup + enum (6 policy values).
- Added `Event.RegistrationPolicyId` (nullable FK).
- Created `EventSessionCategory` and `EventSessionTag` junction entities.
- Added unique constraints to existing junctions: `(TenantId, EventId, CategoryId)`, `(TenantId, EventId, TagId)`, `(TenantId, EventSessionId, ActorId)`.
- User ran migration after this slice.

**Slice 3 — Phase 2.6 + 3.2 (same-room overlap, two mandatory layers):**
- Created `RoomScheduleConflictException` in `Explore.Application/Exceptions/`.
- Extended `IEventSessionRepository` with `GetOverlappingSessionsInRoomAsync`, `CreateWithRoomOverlapGuardAsync`, `UpdateWithRoomOverlapGuardAsync`.
- Implemented Layer B: serializable transaction wrapping overlap re-check + save.
- Added `RoomId` to `CreateEventSessionDto` and `UpdateEventSessionDto`.
- Extended both session DTO validators with Layer A async overlap rule.
- Rewrote both session handlers: inject `IEventScheduleProjectionCalculator`, call `session.Reschedule()` for local projection writes, call guard methods, catch `RoomScheduleConflictException`.
- Registered `IEventScheduleProjectionCalculator` as singleton in `ApplicationServicesRegistration`.
- Updated `CreateEventSessionCommandHandlerTests` for new constructor + new repository calls.
- User ran migration after this slice (no schema change but confirms compilation).

**Slice 4 — Phase 1.5 (registration intent/group domain + persistence):**
- Created `RegistrationScope` lookup + enum (Event=1, Day=2, SessionSelection=3).
- Created `EventRegistrationIntent` entity (parent aggregate with EventId, UserId, RegistrationScopeId, optional SelectedEventDayId, optional RegistrationPolicySnapshotId, ApprovalStatusId, audit/soft-delete/concurrency).
- Added nullable `EventRegistrationIntentId` FK to `EventRegistration` (child role).
- Created EF configs, DbSets, query filters, lookup seeder.
- User ran migration after this slice.

**Slice 5 — Phase 3.5 (intent-first registration handler + validator):**
- Created `IEventDayRepository` + `EventDayRepository` (BelongsToEventAsync, GetByEventAsync).
- Created `IEventRegistrationIntentRepository` + `EventRegistrationIntentRepository` (FindExistingAsync, CreateWithChildrenAsync inside serializable tx).
- Created `RegistrationPolicyRules` in `Explore.Domain/Services/Registration/` — pure domain rules mapping policy to allowed scopes.
- **Repurposed `CreateEventRegistrationDto`** to intent-first shape: `EventId`, `UserId`, `RegistrationScopeId`, optional `SelectedEventDayId`, optional `SelectedSessionIds`, `ApprovalStatusId`. Removed `EventSessionId`, `TenantId`, `AtprotoRecordId`.
- Rewrote `CreateEventRegistrationDtoValidator` with policy enforcement, day ownership validation, session ownership validation.
- Rewrote `CreateEventRegistrationCommandHandler`: creates parent intent + derived child session rows atomically via `CreateWithChildrenAsync`. Includes idempotency check. Removed AutoMapper dependency.
- Updated `MappingProfile`: removed stale `CreateMap<CreateEventRegistrationDto, EventRegistration>`.
- Wired DI for both new repositories in `PersistenceServicesRegistration`.

**Slice 6 — Phase 2.7 (EventDayId auto-linking):**
- Added `FindByEventAndLocalDateAsync(Guid eventId, DateOnly localDate, CancellationToken)` to `IEventDayRepository` + `EventDayRepository`.
- Wired `IEventDayRepository` into `CreateEventSessionCommandHandler` and `UpdateEventSessionCommandHandler`.
- Both handlers now auto-link `EventSession.EventDayId` after `Reschedule()` computes `LocalStartDate`, by looking up the matching `EventDay` via `(EventId, LocalStartDate)`. Sets null when no matching day exists.
- Added 2 new tests to `CreateEventSessionCommandHandlerTests` (match found, no match).
- Created `UpdateEventSessionCommandHandlerTests` with 3 tests (match found, no match, re-link on reschedule to different day).
- Test count: 712 Application (was 707), 100 Domain, 190 Secrets — all green.

**Slice 7 — Phase 3.4 (EventDay CRUD):**
- Created DTOs: `CreateEventDayDto`, `UpdateEventDayDto`, `EventDayDto`, `EventDayListDto` + validators with date uniqueness per event.
- Created commands: `CreateEventDayCommand`, `UpdateEventDayCommand`, `DeleteEventDayCommand`.
- Created queries: `GetEventDaysByEventRequest`, `GetEventDayDetailRequest`.
- Created handlers for all CRUD + query operations.
- Added `ResourceKinds.EventDay` + `AuthorizationActions.EventDays` + Cerbos policy + FallbackAuthorizationService case.
- Added AutoMapper mappings for EventDay.

**Slice 8 — Phase 3.3 (EventAgendaItem CRUD):**
- Created `IEventAgendaItemRepository` + `EventAgendaItemRepository` with `GetByEventAsync`.
- Created DTOs: `CreateEventAgendaItemDto`, `UpdateEventAgendaItemDto`, `EventAgendaItemDto`, `EventAgendaItemListDto` + validators.
- Created commands: `CreateEventAgendaItemCommand`, `UpdateEventAgendaItemCommand`, `DeleteEventAgendaItemCommand`.
- Created queries: `GetEventAgendaItemsByEventRequest`, `GetEventAgendaItemDetailRequest`.
- Created handlers with Reschedule() for local projections + EventDayId auto-linking (same pattern as EventSession).
- Added `ResourceKinds.EventAgendaItem` + `AuthorizationActions.EventAgendaItems` + Cerbos policy + FallbackAuthorizationService case.
- Added AutoMapper mappings for EventAgendaItem.
- Wired DI for `IEventAgendaItemRepository` in `PersistenceServicesRegistration`.

**Slice 9 — Phase 3.1 (Event command refactor for RegistrationPolicyId + series wiring):**
- Added `RegistrationPolicyId`, `EventSeriesId`, `SeriesOrder` to `CreateEventDto` and `UpdateEventDto`.
- Created `IEventRegistrationPolicyRepository` + `EventRegistrationPolicyRepository` + DI registration.
- Extended both `CreateEventDtoValidator` and `UpdateEventDtoValidator` with async existence checks for `EventSeriesId` and `RegistrationPolicyId`, plus `SeriesOrder >= 0` rule.
- Wired new repositories into `CreateEventCommandHandler` and `UpdateEventCommandHandler` constructors + validator instantiation.
- Updated `CreateEventDtoValidatorTests` and `CreateEventCommandHandlerTests` for new constructor signatures.
- AutoMapper already maps these fields automatically (no explicit ignore rules on them).

**Slice 10 — Phase 2.2 (Partial unique indexes for EventRegistrationIntent):**
- Added 3 filtered unique indexes to `EventRegistrationIntentConfiguration`:
  - Event-scope: `(TenantId, EventId, UserId)` WHERE `registration_scope_id = 1 AND is_deleted = false`
  - Day-scope: `(TenantId, EventId, UserId, SelectedEventDayId)` WHERE `registration_scope_id = 2 AND is_deleted = false`
  - SessionSelection-scope: `(TenantId, EventId, UserId)` WHERE `registration_scope_id = 3 AND is_deleted = false`
- Prevents duplicate active intents per scope. Soft-deleted rows are excluded so re-registration after cancellation works.

**Slice 11 — Phase 3.6 (Agenda projection query):**
- Created `EventAgendaProjectionDto`, `AgendaDayGroupDto`, `AgendaScheduleEntryDto` in `DTOs/Agenda/`.
- Created `GetEventAgendaProjectionRequest` + `GetEventAgendaProjectionRequestHandler` in `Features/Agenda/`.
- Handler merges `EventSession` + `EventAgendaItem` into unified `AgendaScheduleEntryDto` entries discriminated by `EntryType`.
- Groups by `LocalStartDate`, enriches with `EventDay` metadata (label, description, publishing state).
- Days without an `EventDay` row still appear (derived from session/agenda dates).
- Sorted by EventDay.SortOrder then chronological date; entries within a day sorted by start minute then sort order.

**Slice 12 — Room management (LocationRoom CRUD):**
- Created `ILocationRoomRepository` + `LocationRoomRepository` with `GetByLocationAsync`.
- Created DTOs: `CreateLocationRoomDto`, `UpdateLocationRoomDto`, `LocationRoomDto`, `LocationRoomListDto` + validators.
- Created full CRUD commands/queries + handlers following existing patterns.
- Added `ResourceKinds.LocationRoom` + `AuthorizationActions.LocationRooms` + Cerbos policy + FallbackAuthorizationService case.
- Added AutoMapper mappings + DI registration.

### 🟡 REMAINING WORK (by priority)

1. **Phase 2.5**: EventDay backfill migration from existing sessions.
2. **Phase 4**: API controllers + NSwag boundary.
3. **Phase 5**: Blazor UI (CSS grid agenda, Miller columns, policy-aware registration UX).
4. **Phase 6**: Tests + docs.

### ⚠️ KNOWN ISSUES / DRIFT

- **NSwag client stale.** `Explore.Blazor.Client/Clients/EventApiClient.g.cs` generated client still has the old `CreateEventRegistrationDto { EventSessionId, UserId, ... }` shape. Compile-safe (different namespace), runtime-broken until NSwag regeneration. Phase 4/6 fix.
- **`Explore.Blazor.Client` has pre-existing compile errors** from in-progress `blazor-localization` branch (`IAccessibilityAnnouncerService` missing). Not introduced by scheduling refactor.
- **`EventSession.EventDayId` auto-links on create/update** via `FindByEventAndLocalDateAsync`. Existing rows still have null until Phase 2.5 backfill migration runs.
- **`EventRegistration.EventRegistrationIntentId` is nullable.** New handler creates linked rows, but legacy rows have null. Backfill migration needed.

### KEY CONVENTIONS DISCOVERED THIS SESSION

- **No base entity class.** Entities implement subsets of `{ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware}`.
- **Concurrency:** `Guid ConcurrencyStamp` + `.IsConcurrencyToken()` in EF config. Auto-updated by `SaveChangesAsync`.
- **Lookup shape:** `int Id` (manual), `required string MasterCode`, `required string FullName`, `string? Description`. Seeded in `LookupTableSeeder`.
- **Validators manually instantiated** in handlers — no DI. Repositories passed as constructor args.
- **GuidVersion7ValueGenerator** used for Guid PKs in EF configs (or `HasDefaultValueSql("uuidv7()")` for some entities).
- **Domain services** can go in `Explore.Domain/Services/` — no precedent existed but architecture tests allow it (no forbidden deps).
- **Query filters** use named filters (`QueryFilterNames.Tenant`, `QueryFilterNames.SoftDelete`) applied in `ExploreDbContext.OnModelCreating`.

### FILES CREATED THIS SESSION (new, did not exist before)

```
Explore.Domain/LocationRoom.cs
Explore.Domain/EventDay.cs
Explore.Domain/EventAgendaItem.cs
Explore.Domain/ScheduleItemKind.cs
Explore.Domain/EventRegistrationPolicy.cs
Explore.Domain/EventRegistrationIntent.cs
Explore.Domain/RegistrationScope.cs
Explore.Domain/EventSessionCategory.cs
Explore.Domain/EventSessionTag.cs
Explore.Domain/Enums/ScheduleItemKindEnum.cs
Explore.Domain/Enums/EventRegistrationPolicyEnum.cs
Explore.Domain/Enums/RegistrationScopeEnum.cs
Explore.Domain/Services/Scheduling/IEventScheduleProjectionCalculator.cs
Explore.Domain/Services/Scheduling/EventScheduleProjectionCalculator.cs
Explore.Domain/Services/Scheduling/LocalScheduleProjection.cs
Explore.Domain/Services/Registration/RegistrationPolicyRules.cs
Explore.Persistence/Configurations/Entities/LocationRoomConfiguration.cs
Explore.Persistence/Configurations/Entities/EventDayConfiguration.cs
Explore.Persistence/Configurations/Entities/EventAgendaItemConfiguration.cs
Explore.Persistence/Configurations/Entities/ScheduleItemKindConfiguration.cs
Explore.Persistence/Configurations/Entities/EventRegistrationPolicyConfiguration.cs
Explore.Persistence/Configurations/Entities/EventRegistrationIntentConfiguration.cs
Explore.Persistence/Configurations/Entities/RegistrationScopeConfiguration.cs
Explore.Persistence/Configurations/Entities/EventSessionCategoryConfiguration.cs
Explore.Persistence/Configurations/Entities/EventSessionTagConfiguration.cs
Explore.Persistence/Repositories/EventDayRepository.cs
Explore.Persistence/Repositories/EventRegistrationIntentRepository.cs
Explore.Application/Contracts/Persistence/IEventDayRepository.cs
Explore.Application/Contracts/Persistence/IEventRegistrationIntentRepository.cs
Explore.Application/Contracts/Persistence/IEventAgendaItemRepository.cs
Explore.Application/Contracts/Persistence/ILocationRoomRepository.cs
Explore.Application/Contracts/Persistence/IEventRegistrationPolicyRepository.cs
Explore.Application/Exceptions/RoomScheduleConflictException.cs
Explore.Application/DTOs/EventDay/ (CreateEventDayDto, UpdateEventDayDto, EventDayDto, EventDayListDto + Validators/)
Explore.Application/DTOs/EventAgendaItem/ (CreateEventAgendaItemDto, UpdateEventAgendaItemDto, EventAgendaItemDto, EventAgendaItemListDto + Validators/)
Explore.Application/DTOs/LocationRoom/ (CreateLocationRoomDto, UpdateLocationRoomDto, LocationRoomDto, LocationRoomListDto + Validators/)
Explore.Application/DTOs/Agenda/ (EventAgendaProjectionDto, AgendaDayGroupDto, AgendaScheduleEntryDto)
Explore.Application/Features/EventDays/ (Requests/Commands + Requests/Queries + Handlers/Commands + Handlers/Queries — full CRUD)
Explore.Application/Features/EventAgendaItems/ (Requests/Commands + Requests/Queries + Handlers/Commands + Handlers/Queries — full CRUD)
Explore.Application/Features/LocationRooms/ (Requests/Commands + Requests/Queries + Handlers/Commands + Handlers/Queries — full CRUD)
Explore.Application/Features/Agenda/Requests/Queries/GetEventAgendaProjectionRequest.cs
Explore.Application/Features/Agenda/Handlers/Queries/GetEventAgendaProjectionRequestHandler.cs
Explore.Persistence/Repositories/EventAgendaItemRepository.cs
Explore.Persistence/Repositories/LocationRoomRepository.cs
Explore.Persistence/Repositories/EventRegistrationPolicyRepository.cs
Event.Application.UnitTests/Features/EventSessions/Commands/UpdateEventSessionCommandHandlerTests.cs
cerbos/policies/event_day.yaml + cerbos/policies/_schemas/event_day.json
cerbos/policies/event_agenda_item.yaml + cerbos/policies/_schemas/event_agenda_item.json
cerbos/policies/location_room.yaml + cerbos/policies/_schemas/location_room.json
```

### FILES MODIFIED THIS SESSION (existed before, edited)

```
Explore.Domain/Event.cs — added RegistrationPolicyId FK
Explore.Domain/EventSession.cs — added EventDayId, RoomId, SortOrder, 6 local projection fields, aggregate methods
Explore.Domain/EventRegistration.cs — added EventRegistrationIntentId (nullable)
Explore.Persistence/ExploreDbContext.cs — added DbSets, query filters for all new entities
Explore.Persistence/Configurations/Entities/EventConfiguration.cs — wired RegistrationPolicy FK
Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs — wired Room/EventDay FKs, new indexes, EndAfterStart check
Explore.Persistence/Configurations/Entities/EventRegistrationConfiguration.cs — wired parent intent FK, intent index
Explore.Persistence/Configurations/Entities/EventCategoriesConfiguration.cs — added unique index
Explore.Persistence/Configurations/Entities/EventTagsConfiguration.cs — added unique index
Explore.Persistence/Configurations/Entities/EventSessionSpeakerConfiguration.cs — added unique index
Explore.Persistence/Seed/LookupTableSeeder.cs — added seeders for ScheduleItemKind, EventRegistrationPolicy, RegistrationScope
Explore.Persistence/PersistenceServicesRegistration.cs — added DI for new repositories
Explore.Persistence/Repositories/EventSessionRepository.cs — added overlap guard methods
Explore.Application/DTOs/EventSession/CreateEventSessionDto.cs — added RoomId
Explore.Application/DTOs/EventSession/UpdateEventSessionDto.cs — added RoomId
Explore.Application/DTOs/EventSession/Validators/CreateEventSessionDtoValidator.cs — added IEventSessionRepository + overlap rule
Explore.Application/DTOs/EventSession/Validators/UpdateEventSessionDtoValidator.cs — added IEventSessionRepository + overlap rule
Explore.Application/DTOs/EventRegistration/CreateEventRegistrationDto.cs — REWRITTEN to intent-first shape
Explore.Application/DTOs/EventRegistration/Validators/CreateEventRegistrationDtoValidator.cs — REWRITTEN with policy enforcement
Explore.Application/Features/EventSessions/Handlers/Commands/CreateEventSessionCommandHandler.cs — added calculator, guard methods, EventDayId auto-linking
Explore.Application/Features/EventSessions/Handlers/Commands/UpdateEventSessionCommandHandler.cs — added calculator, guard methods, EventDayId auto-linking
Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs — REWRITTEN for intent-first flow
Explore.Application/Features/EventRegistrations/Requests/Commands/CreateEventRegistrationCommand.cs — ResourceId now keyed on EventId
Explore.Application/Profiles/MappingProfile.cs — removed stale CreateMap
Explore.Application/ApplicationServicesRegistration.cs — registered IEventScheduleProjectionCalculator singleton
Event.Application.UnitTests/Features/EventSessions/Commands/CreateEventSessionCommandHandlerTests.cs — updated for new constructor + guard method + EventDayId tests
Explore.Application/Contracts/Persistence/IEventDayRepository.cs — added FindByEventAndLocalDateAsync
Explore.Persistence/Repositories/EventDayRepository.cs — implemented FindByEventAndLocalDateAsync
Explore.Application/DTOs/Event/CreateEventDto.cs — added RegistrationPolicyId, EventSeriesId, SeriesOrder
Explore.Application/DTOs/Event/UpdateEventDto.cs — added RegistrationPolicyId, EventSeriesId, SeriesOrder
Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs — added series + policy existence checks
Explore.Application/DTOs/Event/Validators/UpdateEventDtoValidator.cs — added series + policy existence checks
Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs — wired series + policy repositories
Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs — wired series + policy repositories
Event.Application.UnitTests/Features/Events/Commands/CreateEventCommandHandlerTests.cs — updated for new constructor
Event.Application.UnitTests/Features/Events/Validators/CreateEventDtoValidatorTests.cs — updated for new constructor
Explore.Application/Authorization/ResourceKinds.cs — added EventDay, EventAgendaItem, LocationRoom
Explore.Application/Authorization/AuthorizationActions.cs — added EventDays, EventAgendaItems, LocationRooms classes
Explore.Infrastructure/Services/FallbackAuthorizationService.cs — added cases for event_day, event_agenda_item, location_room
Explore.Application/Profiles/MappingProfile.cs — added EventDay, EventAgendaItem, LocationRoom mappings
Explore.Persistence/PersistenceServicesRegistration.cs — added DI for EventAgendaItem, LocationRoom, EventRegistrationPolicy repos
Explore.Persistence/Configurations/Entities/EventRegistrationIntentConfiguration.cs — added 3 partial unique indexes
dev/active/event-scheduling-refactor/event-scheduling-refactor-plan.md — updated with EventDay justification, two-layer overlap, projection calculator ownership
```

## Session 2 Progress (2026-04-13 Europe/Brussels)

### ✅ Build Fixes (NSwag DTO regeneration)
- Fixed 3 build errors in Blazor.Client from `CreateEventRegistrationDto.EventSessionId` removal (intent-first rewrite).
- Fixed 3 build errors in Blazor.Client.Tests for same DTO shape change.
- All pages now use intent-first DTO: EventId, UserId, RegistrationScopeId=3 (SessionSelection), SelectedSessionIds.

### ✅ Phase 4 — API Layer (COMPLETE)
- **ResourceDescriptors**: EventDay, EventAgendaItem, LocationRoom added to `ResourceDescriptors.cs`.
- **RouteNames**: 3 new regions (EventDay, EventAgendaItem, LocationRoom) + agenda projection.
- **Controllers**: 3 new (EventDayController, EventAgendaItemController, LocationRoomController) + 3 lookup controllers (RegistrationScope, EventRegistrationPolicy, ScheduleItemKind).
- **HATEOAS**: 3 link policy files (detail+collection), 3 resource assemblers, 9 DI registrations.
- **DTOs updated**: EventSessionDto (EventDayId, RoomId, local projection, SortOrder, RoomName), EventDto (RegistrationPolicyId/FullName/MasterCode), EventRegistrationDto (EventId, EventTitle, EventRegistrationIntentId).
- **New DTOs**: EventRegistrationIntentDto/ListDto, RegistrationScopeDto/ListDto, EventRegistrationPolicyDto/ListDto, ScheduleItemKindDto/ListDto.
- **AutoMapper**: Full mapping sections for all new/changed DTOs including nav property ForMember mappings.
- **Repository eager-loading**: .Include(Room) in EventSession repo (4 methods), .Include(RegistrationPolicy) in Event repo (6 methods).
- **Lookup infrastructure**: Repository contracts, implementations, MediatR queries, handlers, DI for RegistrationScope + ScheduleItemKind.

### ✅ Phase 6 — Tests + Docs (MOSTLY COMPLETE)
- **Domain unit tests**: 192 passing — EventScheduleProjectionCalculator (14 tests, DST, timezone fallback), RegistrationPolicyRules (21 parameterized cases), EventSession Reschedule/Reproject (9 tests), EventAgendaItem (13 tests), EventDay/LocationRoom/EventRegistrationIntent (8 tests each).
- **Application unit tests**: 822 passing — 17 handler test files covering all 15 CRUD handlers + AgendaProjection + RegistrationScope. DataBuilder extended with 6 new entity Fakers.
- **Schema docs**: `schemas/islamu-event.md` updated with all new entities, relationships, enums.
- **Architecture tests**: 72 passing (reflection-based, auto-covers new entities).

### Key Files Created/Modified in Session 2
```
# Phase 4 — API Layer
Explore.Application/Authorization/ResourceDescriptors.cs — 3 new entries
Explore.API/Hateoas/RouteNames.cs — 3 new regions
Explore.API/Controllers/EventDayController.cs — NEW
Explore.API/Controllers/EventAgendaItemController.cs — NEW
Explore.API/Controllers/LocationRoomController.cs — NEW
Explore.API/Controllers/RegistrationScopeController.cs — NEW
Explore.API/Controllers/EventRegistrationPolicyController.cs — NEW
Explore.API/Controllers/ScheduleItemKindController.cs — NEW
Explore.API/Hateoas/Policies/EventDayLinkPolicy.cs — NEW
Explore.API/Hateoas/Policies/EventAgendaItemLinkPolicy.cs — NEW
Explore.API/Hateoas/Policies/LocationRoomLinkPolicy.cs — NEW
Explore.API/Hateoas/Assemblers/EventDayResourceAssembler.cs — NEW
Explore.API/Hateoas/Assemblers/EventAgendaItemResourceAssembler.cs — NEW
Explore.API/Hateoas/Assemblers/LocationRoomResourceAssembler.cs — NEW
Explore.API/Extensions/HateoasAssemblerRegistration.cs — 9 DI registrations
Explore.Application/DTOs/EventSession/EventSessionDto.cs — 9 new fields
Explore.Application/DTOs/EventSession/EventSessionListDto.cs — 7 new fields
Explore.Application/DTOs/Event/EventDto.cs — 3 RegistrationPolicy fields
Explore.Application/DTOs/Event/EventListDto.cs — 2 RegistrationPolicy fields
Explore.Application/DTOs/EventRegistration/EventRegistrationDto.cs — EventId, EventTitle, IntentId
Explore.Application/DTOs/EventRegistration/EventRegistrationListDto.cs — IntentId
Explore.Application/DTOs/EventRegistrationIntent/ — NEW (2 files)
Explore.Application/DTOs/RegistrationScope/ — NEW (2 files)
Explore.Application/DTOs/EventRegistrationPolicy/ — NEW (2 files)
Explore.Application/DTOs/ScheduleItemKind/ — NEW (2 files)
Explore.Application/Contracts/Persistence/IRegistrationScopeRepository.cs — NEW
Explore.Application/Contracts/Persistence/IScheduleItemKindRepository.cs — NEW
Explore.Persistence/Repositories/RegistrationScopeRepository.cs — NEW
Explore.Persistence/Repositories/ScheduleItemKindRepository.cs — NEW
Explore.Application/Features/RegistrationScopes/ — NEW (query + handler)
Explore.Application/Features/EventRegistrationPolicies/ — NEW (query + handler)
Explore.Application/Features/ScheduleItemKinds/ — NEW (query + handler)
Explore.Application/Profiles/MappingProfile.cs — all new mappings
Explore.Persistence/Repositories/EventSessionRepository.cs — .Include(Room) x4
Explore.Persistence/Repositories/EventRepository.cs — .Include(RegistrationPolicy) x6
Explore.Persistence/PersistenceServicesRegistration.cs — 2 new repo registrations

# Phase 6 — Tests + Docs
Event.Application.UnitTests/Common/DataBuilder.cs — 6 new Fakers
Event.Domain.UnitTests/Services/Scheduling/EventScheduleProjectionCalculatorTests.cs — NEW
Event.Domain.UnitTests/Services/Registration/RegistrationPolicyRulesTests.cs — NEW
Event.Domain.UnitTests/Entities/EventSessionRescheduleTests.cs — NEW
Event.Domain.UnitTests/Entities/EventAgendaItemTests.cs — NEW
Event.Domain.UnitTests/Entities/EventDayTests.cs — NEW
Event.Domain.UnitTests/Entities/LocationRoomTests.cs — NEW
Event.Domain.UnitTests/Entities/EventRegistrationIntentTests.cs — NEW
Event.Application.UnitTests/Features/EventDays/ — 5 test files (CRUD)
Event.Application.UnitTests/Features/EventAgendaItems/ — 5 test files (CRUD)
Event.Application.UnitTests/Features/LocationRooms/ — 5 test files (CRUD)
Event.Application.UnitTests/Features/Agenda/Queries/ — 1 test file
Event.Application.UnitTests/Features/RegistrationScopes/Queries/ — 1 test file
schemas/islamu-event.md — all new entities, relationships, enums
```

## Quick Resume for Next Session

1. Read this context file + `event-scheduling-refactor-tasks.md`.
2. Run `dotnet build --configuration Release --verbosity quiet` and the per-project test matrix from CLAUDE.md.
3. **🟡 BLOCKER: User must regenerate swagger.json + EventApiClient.g.cs** to pick up 6 new controllers (EventDay, EventAgendaItem, LocationRoom, RegistrationScope, EventRegistrationPolicy, ScheduleItemKind). Phase 5 Blazor UI is blocked on this.
4. **Recommended first action:** NSwag regeneration, then Phase 5 Blazor UI.
5. Build: 0 errors. Tests: 72 Architecture / 192 Domain / 822 Application / 190 Secrets (all green).
6. **Phase 4 (API layer) is now complete.** All controllers, HATEOAS, DTOs, AutoMapper, and repository eager-loading for the scheduling refactor are in place.
7. **Phase 6 tests mostly complete.** Domain + application unit tests all passing. Remaining: persistence integration tests, Blazor component tests.
