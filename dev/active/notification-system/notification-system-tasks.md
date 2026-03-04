# Notification System — Task Checklist

> Last Updated: 2026-03-03

## Phase 1: Domain Layer ⏳ NOT STARTED
- [ ] **1.1** Update `Notification.cs` — add `ISoftDeletable` + `IAuditableEntity` interfaces and fields
  - Acceptance: Entity compiles, interfaces implemented, existing fields preserved

## Phase 2: Persistence Layer ⏳ NOT STARTED
- [ ] **2.1** Update `NotificationConfiguration.cs` — add soft-delete filter, audit configs, partial index
  - Acceptance: Configuration compiles, partial index defined for `IsRead = false`
- [ ] **2.2** Create `INotificationRepository.cs` in `Explore.Application/Contracts/Persistence/`
  - Acceptance: Interface extends `IGenericRepository<Notification, Guid>`, includes 5 methods
- [ ] **2.3** Create `NotificationRepository.cs` in `Explore.Persistence/Repositories/`
  - Acceptance: Implements all interface methods, uses `ExecuteUpdateAsync` for bulk mark
- [ ] **2.4** Register repository in `PersistenceServicesRegistration.cs`
  - Acceptance: DI registration added
- [ ] **2.5** Create EF migration for audit fields + partial index
  - Acceptance: Migration applies cleanly

## Phase 3: Application Layer ⏳ NOT STARTED
- [ ] **3.1** Create DTOs: `NotificationDto`, `NotificationListDto`, `UnreadCountDto`
  - Acceptance: All DTOs created in `Explore.Application/DTOs/Notification/`
- [ ] **3.2** Create `NotificationProfile.cs` AutoMapper profile
  - Acceptance: Maps `Notification → NotificationDto` and `Notification → NotificationListDto`
- [ ] **3.3** Create `GetUserNotificationsRequest.cs` query
  - Acceptance: `IRequest<PaginatedResult<NotificationListDto>>` with PageNumber, PageSize, IsRead?, Type?
- [ ] **3.4** Create `GetUserNotificationsRequestHandler.cs`
  - Acceptance: Extracts UserId, calls repo, maps, returns paginated
- [ ] **3.5** Create `GetNotificationByIdRequest.cs` query
  - Acceptance: `IRequest<NotificationDto?>` with Guid Id
- [ ] **3.6** Create `GetNotificationByIdRequestHandler.cs`
  - Acceptance: Verifies ownership, throws NotFoundException if missing/wrong user
- [ ] **3.7** Create `GetUnreadCountRequest.cs` query
  - Acceptance: `IRequest<UnreadCountDto>`
- [ ] **3.8** Create `GetUnreadCountRequestHandler.cs`
  - Acceptance: Returns correct unread count
- [ ] **3.9** Create `MarkNotificationAsReadCommand.cs`
  - Acceptance: `IRequest<BaseCommandResponse<Guid>>` with Guid Id
- [ ] **3.10** Create `MarkNotificationAsReadCommandHandler.cs`
  - Acceptance: Verifies ownership, sets IsRead + ReadAt, idempotent
- [ ] **3.11** Create `MarkAllNotificationsAsReadCommand.cs`
  - Acceptance: `IRequest<BaseCommandResponse<Guid>>`, no properties needed
- [ ] **3.12** Create `MarkAllNotificationsAsReadCommandHandler.cs`
  - Acceptance: Uses bulk ExecuteUpdateAsync, returns affected count, idempotent
- [ ] **3.13** Create `DeleteNotificationCommand.cs`
  - Acceptance: `IRequest<bool>` with Guid Id
- [ ] **3.14** Create `DeleteNotificationCommandHandler.cs`
  - Acceptance: Verifies ownership, soft-deletes

## Phase 4: API Layer ⏳ NOT STARTED
- [ ] **4.1** Add notification route constants to `RouteNames.cs`
  - Acceptance: 6 constants in `#region Notification Routes`
- [ ] **4.2** Create `NotificationController.cs` with 6 endpoints
  - Acceptance: All [Authorize], HATEOAS, ProducesResponseType, CancellationToken
- [ ] **4.3** Create `NotificationLinkPolicy.cs` (detail + collection)
  - Acceptance: Detail: self, mark-read, delete; Collection: self, mark-all-read, unread-count
- [ ] **4.4** Create `NotificationResourceAssembler.cs`
  - Acceptance: Extends ResourceAssemblerBase, registered in DI
- [ ] **4.5** Register HATEOAS services in DI
  - Acceptance: Link policies + assembler registered

## Phase 5: Testing ⏳ NOT STARTED
- [ ] **5.1** Unit tests — Query handlers (5 tests minimum)
  - Acceptance: All query paths tested
- [ ] **5.2** Unit tests — Command handlers (7 tests minimum)
  - Acceptance: All command paths tested, ownership verified
- [ ] **5.3** Architecture tests pass
  - Acceptance: Existing + any new architecture tests green

## Phase 6: Documentation ⏳ NOT STARTED
- [ ] **6.1** Update `docs/DOMAIN.md` — add Notification
- [ ] **6.2** Update `docs/API.md` — add Notification endpoint group

---

## Summary

| Phase | Tasks | Effort |
|---|---|---|
| 1. Domain | 1 | S |
| 2. Persistence | 5 | M |
| 3. Application | 14 | L |
| 4. API | 5 | M |
| 5. Testing | 3 | M |
| 6. Documentation | 2 | S |
| **Total** | **30** | |
