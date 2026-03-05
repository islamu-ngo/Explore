# Notification System — Task Checklist

> Last Updated: 2026-03-04

## Phase 1: Domain Layer ✅ COMPLETE
- [x] **1.1** Update `Notification.cs` — add `ISoftDeletable` + `IAuditableEntity` interfaces and fields

## Phase 2: Persistence Layer ✅ COMPLETE
- [x] **2.1** Update `NotificationConfiguration.cs` — add soft-delete filter, audit configs, partial index
- [x] **2.2** Create `INotificationRepository.cs` in `Explore.Application/Contracts/Persistence/`
- [x] **2.3** Create `NotificationRepository.cs` in `Explore.Persistence/Repositories/`
- [x] **2.4** Register repository in `PersistenceServicesRegistration.cs`
- [ ] **2.5** Create EF migration ⏳ DEFERRED (all previous migrations deleted from repo)

## Phase 3: Application Layer ✅ COMPLETE
- [x] **3.1** Create DTOs: `NotificationDto`, `NotificationListDto`, `UnreadCountDto`
- [x] **3.2** Create AutoMapper mappings in `MappingProfile.cs` (not separate file — follows project convention)
- [x] **3.3** Create `GetUserNotificationsRequest.cs` query
- [x] **3.4** Create `GetUserNotificationsRequestHandler.cs`
- [x] **3.5** Create `GetNotificationByIdRequest.cs` query
- [x] **3.6** Create `GetNotificationByIdRequestHandler.cs`
- [x] **3.7** Create `GetUnreadCountRequest.cs` query
- [x] **3.8** Create `GetUnreadCountRequestHandler.cs`
- [x] **3.9** Create `MarkNotificationAsReadCommand.cs`
- [x] **3.10** Create `MarkNotificationAsReadCommandHandler.cs`
- [x] **3.11** Create `MarkAllNotificationsAsReadCommand.cs`
- [x] **3.12** Create `MarkAllNotificationsAsReadCommandHandler.cs`
- [x] **3.13** Create `DeleteNotificationCommand.cs`
- [x] **3.14** Create `DeleteNotificationCommandHandler.cs`

## Phase 4: API Layer ✅ COMPLETE
- [x] **4.1** Add notification route constants to `RouteNames.cs`
- [x] **4.2** Create `NotificationController.cs` with 6 endpoints
- [x] **4.3** Create `NotificationLinkPolicy.cs` (detail + collection)
- [x] **4.4** Create `NotificationResourceAssembler.cs`
- [x] **4.5** Register HATEOAS services in DI

## Phase 5: Testing ✅ COMPLETE
- [x] **5.1** Unit tests — Query handlers (9 tests: 3 per handler)
- [x] **5.2** Unit tests — Command handlers (9 tests: 3 per handler)
- [x] **5.3** Architecture tests pass (32/32)

## Phase 6: Documentation ✅ COMPLETE
- [x] **6.1** Update `docs/DOMAIN.md` — add Notification
- [x] **6.2** Update `docs/API.md` — add Notification endpoint group

## Refactor 1: Lookup Entities ✅ COMPLETE (2026-03-03)
- [x] **R1.1** Create `NotificationTypeEnum` (10 values) + `NotificationEntityTypeEnum` (6 values)
- [x] **R1.2** Create `NotificationType` + `NotificationEntityType` domain lookup entities
- [x] **R1.3** Update `Notification.cs` — replace string fields with int FK + nav props
- [x] **R1.4** Create EF configurations for both lookup entities
- [x] **R1.5** Update `NotificationConfiguration.cs` — FK relationships
- [x] **R1.6** Add DbSets to `ExploreDbContext.cs`
- [x] **R1.7** Create repository interfaces and implementations for lookup entities
- [x] **R1.8** Register in DI
- [x] **R1.9** Add seed data methods to `LookupTableSeeder.cs`
- [x] **R1.10** Update DTOs, mappings, request, handler, controller
- [x] **R1.11** Update all 6 test files

## Refactor 2: Scope + Actor Targeting ✅ COMPLETE (2026-03-04)
- [x] **R2.1** Add `NotificationScopeId` (int, FK→ActorType) to Notification entity
- [x] **R2.2** Add `SourceActorId` (Guid?, FK→Actor) to Notification entity
- [x] **R2.3** Add `RecipientContextActorId` (Guid?, FK→Actor) to Notification entity
- [x] **R2.4** Update `NotificationConfiguration.cs` — 3 new FK rels + scope index
- [x] **R2.5** Update `INotificationRepository` + `NotificationRepository` — scope filter, nav prop includes
- [x] **R2.6** Update DTOs — 6 new fields (ScopeId/Name, SourceActorId/Name, RecipientContextActorId/Name)
- [x] **R2.7** Update AutoMapper mappings — map Pii.DisplayName for actor names
- [x] **R2.8** Update request, handler, controller — scope filter parameter
- [x] **R2.9** Update all test files for new required fields + mock signatures

## Remaining Work
- [ ] **EF Migration** — create when migration strategy is confirmed
- [ ] **Git commit** — all notification work is uncommitted
- [ ] **Future: Notification dispatch** — domain event handlers that fan-out notifications to users
- [ ] **Future: Push delivery** — WebSocket/SSE for real-time notification delivery
- [ ] **Future: Preferences** — per-user notification preference settings

---

## Summary

| Phase | Tasks | Status |
|---|---|---|
| 1. Domain | 1 | ✅ Complete |
| 2. Persistence | 5 | ✅ 4/5 (migration deferred) |
| 3. Application | 14 | ✅ Complete |
| 4. API | 5 | ✅ Complete |
| 5. Testing | 3 | ✅ Complete |
| 6. Documentation | 2 | ✅ Complete |
| R1. Lookup Entities | 11 | ✅ Complete |
| R2. Scope + Actors | 9 | ✅ Complete |
| **Total** | **50** | **49/50 done** |
