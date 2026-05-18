# Notification System — API Implementation Plan

> Last Updated: 2026-03-03

## Executive Summary

Implement a complete notification API for the ISLAMU Event platform. The domain entity (`Notification`) and EF Core configuration already exist. This plan covers the remaining layers: Application (DTOs, CQRS handlers, validation), Persistence (repository), API (controller, HATEOAS), and Tests.

### Key Requirements
1. **List all notifications** — paginated, for the authenticated user
2. **Unread count** — efficient count of unread notifications
3. **Bulk mark-all-as-read** — single action marks all unread notifications as read (YouTube-style: opening the notification panel marks everything read)
4. **Mark single notification as read** — individual read status toggle

### Scope Exclusions
- UI/Blazor implementation (not in scope)
- Push notifications / WebSocket / real-time delivery (separate feature)
- Notification creation/dispatch system (notifications are created by other features — RSVP, approvals, etc.)
- Notification preferences/settings (future feature)

---

## Research Insights (YouTube, Mastodon, Enterprise Patterns)

### YouTube Pattern
- Bell icon shows unread count badge
- Clicking the bell opens a panel; **all visible unread notifications become "read" instantly** (bulk mark-all-as-read)
- Uses continuation tokens (cursor-based) for infinite scroll
- Notifications are typed (uploads, live, comments, etc.)

### Mastodon Pattern (Open Source Reference)
- `notifications` table: `id`, `activity_id`, `activity_type`, `account_id`, `from_account_id`, `type`, `filtered`, `group_key`, `created_at`
- Read state tracked per-notification at application level
- Bulk dismiss/clear endpoints exist
- Group key for notification grouping (e.g., "5 people liked your post")

### Enterprise Best Practices
- **Per-row `is_read`** (Pattern A) — simplest, works well for <1M notifications/user. Already what our entity uses.
- **Partial index** on `IsRead = false` — dramatically speeds unread queries and count
- **Cached unread count** — avoid COUNT(*) on every request; use HybridCache
- **Batch UPDATE** for mark-all-as-read — single SQL `UPDATE ... WHERE UserId = X AND IsRead = false`
- **Offset pagination** — consistent with our existing patterns

### Design Decision: Per-Row `is_read` (Pattern A)
Given our scale (event platform, not social media), per-row `is_read` with a partial index is the right choice. Watermark pattern (Pattern B) is overkill. The entity already has `IsRead` and `ReadAt` fields.

---

## Current State Analysis

### What Exists ✅
| Artifact | Path | Status |
|---|---|---|
| Domain entity | `Explore.Domain/Notification.cs` | ✅ Complete |
| EF Configuration | `Explore.Persistence/Configurations/Entities/NotificationConfiguration.cs` | ✅ Complete |
| DbContext registration | `Explore.Persistence/ExploreDbContext.cs` (line 361) | ✅ Complete |
| Tenant query filter | `ExploreDbContext.cs` (line 188) | ✅ Complete |

### What's Missing ❌
| Layer | Artifact | Notes |
|---|---|---|
| Application | `INotificationRepository` | Repository contract |
| Application | DTOs (List, Detail) | No Create DTO needed (system-generated) |
| Application | Query requests/handlers | GetUserNotifications, GetUnreadCount |
| Application | Command requests/handlers | MarkAsRead, MarkAllAsRead, DeleteNotification |
| Application | Validators | MarkAsRead validation |
| Application | AutoMapper profile | Entity ↔ DTO mapping |
| Persistence | `NotificationRepository` | With bulk update, unread count |
| Persistence | DI registration | `PersistenceServicesRegistration.cs` |
| Persistence | Migration | Add partial index for unread notifications |
| API | `NotificationController` | REST endpoints |
| API | RouteNames | Notification route constants |
| API | HATEOAS link policies | Detail + Collection link policies |
| API | HATEOAS resource assembler | Notification assembler |
| API | DI registration | HATEOAS services |
| Tests | Unit tests | Handler tests |
| Tests | Architecture tests | If needed |

---

## Entity Assessment

Current `Notification.cs` is well-structured for our needs:
```
Id (Guid), UserId, Type, Title, Body, IsRead, ReadAt, EntityType, EntityId, CreatedAt, TenantId
```

### Entity Change: Add ISoftDeletable

Per project conventions (AGENTS.md rule #10), entities should include auditing fields. The notification entity should implement `ISoftDeletable` to support user-initiated deletion without data loss. We also need `IAuditableEntity` for consistency.

**Decision**: Add `ISoftDeletable` + audit fields to `Notification` entity. This allows users to "dismiss" notifications without permanent deletion and enables the soft-delete query filter.

### Database Enhancement: Partial Index

Add a PostgreSQL partial index for unread notifications — the most common query pattern:
```sql
CREATE INDEX ix_notifications_unread_by_user
ON notifications (tenant_id, user_id, created_at DESC)
WHERE is_read = false;
```

This makes unread count and unread notification listing extremely fast.

---

## API Endpoints Design

All notification endpoints require authentication (`[Authorize]`) — notifications are personal.

| Method | Endpoint | Description | Response |
|---|---|---|---|
| `GET` | `/api/notification` | List user's notifications (paginated) | `PaginatedResult<NotificationListDto>` |
| `GET` | `/api/notification/unread-count` | Get unread notification count | `{ unreadCount: int }` |
| `GET` | `/api/notification/{id}` | Get single notification detail | `NotificationDto` |
| `PUT` | `/api/notification/{id}/read` | Mark single notification as read | `BaseCommandResponse<Guid>` |
| `PUT` | `/api/notification/mark-all-read` | Bulk mark all unread as read | `BaseCommandResponse<Guid>` |
| `DELETE` | `/api/notification/{id}` | Soft-delete a notification | `204 NoContent` |

### Query Parameters for GET /api/notification
- `pageNumber` (int, default 1)
- `pageSize` (int, default 20, max 100)
- `isRead` (bool?, optional filter — null = all, true = read only, false = unread only)
- `type` (string?, optional filter — e.g., "registration.confirmed")

### Response: Unread Count
```json
{
  "unreadCount": 5
}
```

### Bulk Mark-All-Read Behavior (YouTube-Style)
- Marks all `IsRead = false` notifications for the current user as read
- Sets `ReadAt = DateTime.UtcNow` for all affected notifications
- Returns count of notifications marked as read in the response message
- Idempotent: calling when all are already read returns success with 0 count

---

## Implementation Phases

### Phase 1: Domain Layer Updates
**Effort: S | Skills: `clean-architecture-rules`**

#### Task 1.1: Update Notification Entity with ISoftDeletable + IAuditableEntity
- **File**: `Explore.Domain/Notification.cs`
- **Changes**: Implement `ISoftDeletable`, `IAuditableEntity` interfaces, add missing audit/soft-delete fields
- **Acceptance Criteria**:
  - [ ] Entity implements `ISoftDeletable` (IsDeleted, DeletedAt, DeletedBy)
  - [ ] Entity implements `IAuditableEntity` (CreatedBy, UpdatedAt, UpdatedBy)
  - [ ] Existing fields preserved (no breaking changes)

---

### Phase 2: Persistence Layer
**Effort: M | Skills: `dotnet-efcore-guidelines`**

#### Task 2.1: Update NotificationConfiguration for New Fields + Partial Index
- **File**: `Explore.Persistence/Configurations/Entities/NotificationConfiguration.cs`
- **Changes**: Add soft-delete query filter, audit field configs, partial index for unread
- **Acceptance Criteria**:
  - [ ] Soft-delete query filter registered in `ExploreDbContext`
  - [ ] Partial index `ix_notifications_unread_by_user` on `(TenantId, UserId, CreatedAt DESC) WHERE IsRead = false`
  - [ ] Audit fields configured

#### Task 2.2: Create INotificationRepository Interface
- **File**: `Explore.Application/Contracts/Persistence/INotificationRepository.cs`
- **Methods**:
  - `GetUserNotificationsPaged(Guid userId, int pageNumber, int pageSize, bool? isRead, string? type)`
  - `GetUnreadCount(Guid userId)`
  - `MarkAsRead(Guid notificationId, Guid userId)`
  - `MarkAllAsRead(Guid userId)` → returns count of affected rows
  - `GetByIdForUser(Guid notificationId, Guid userId)`
- **Acceptance Criteria**:
  - [ ] Interface extends `IGenericRepository<Notification, Guid>`
  - [ ] All methods documented with XML comments
  - [ ] Bulk mark returns affected count (int)

#### Task 2.3: Create NotificationRepository Implementation
- **File**: `Explore.Persistence/Repositories/NotificationRepository.cs`
- **Key implementation details**:
  - `GetUserNotificationsPaged`: `AsNoTracking`, filter by UserId + optional isRead/type, order by CreatedAt DESC
  - `GetUnreadCount`: `CountAsync(n => !n.IsRead && n.UserId == userId)` — leverages partial index
  - `MarkAllAsRead`: Raw SQL `ExecuteUpdateAsync` for batch update efficiency (no entity loading)
  - `MarkAsRead`: Load entity, set IsRead + ReadAt, save
- **Acceptance Criteria**:
  - [ ] Bulk mark uses `ExecuteUpdateAsync` (not load-all-then-save)
  - [ ] Pagination returns `(List<Notification>, int)` tuple
  - [ ] All queries use `AsNoTracking()` for reads

#### Task 2.4: Register Repository in DI
- **File**: `Explore.Persistence/PersistenceServicesRegistration.cs`
- **Acceptance Criteria**:
  - [ ] `services.AddScoped<INotificationRepository, NotificationRepository>()` added

#### Task 2.5: Create EF Migration
- **Command**: `dotnet ef migrations add AddNotificationAuditAndPartialIndex`
- **Acceptance Criteria**:
  - [ ] Migration adds audit/soft-delete columns to notifications table
  - [ ] Migration adds partial index for unread notifications
  - [ ] Migration applies cleanly

---

### Phase 3: Application Layer
**Effort: L | Skills: `cqrs-mediatr-guidelines`, `clean-architecture-rules`**

#### Task 3.1: Create Notification DTOs
- **Files**:
  - `Explore.Application/DTOs/Notification/NotificationDto.cs` (detail)
  - `Explore.Application/DTOs/Notification/NotificationListDto.cs` (list)
  - `Explore.Application/DTOs/Notification/UnreadCountDto.cs` (count response)
- **NotificationDto fields**: Id, UserId, Type, Title, Body, IsRead, ReadAt, EntityType, EntityId, CreatedAt, TenantId
- **NotificationListDto fields**: Id, Type, Title, Body (truncated?), IsRead, ReadAt, EntityType, EntityId, CreatedAt
- **UnreadCountDto fields**: UnreadCount (int)
- **Acceptance Criteria**:
  - [ ] Separate DTOs for list vs detail views
  - [ ] UnreadCountDto is a simple wrapper

#### Task 3.2: Create AutoMapper Profile
- **File**: `Explore.Application/Profiles/NotificationProfile.cs`
- **Acceptance Criteria**:
  - [ ] Maps `Notification → NotificationDto`
  - [ ] Maps `Notification → NotificationListDto`

#### Task 3.3: Create Query — GetUserNotificationsRequest
- **File**: `Explore.Application/Features/Notifications/Requests/Queries/GetUserNotificationsRequest.cs`
- **Properties**: PageNumber, PageSize, IsRead (bool?), Type (string?)
- **Acceptance Criteria**:
  - [ ] Implements `IRequest<PaginatedResult<NotificationListDto>>`

#### Task 3.4: Create Handler — GetUserNotificationsRequestHandler
- **File**: `Explore.Application/Features/Notifications/Handlers/Queries/GetUserNotificationsRequestHandler.cs`
- **Logic**: Extract UserId from claims, call repository, map to DTOs, return paginated result
- **Acceptance Criteria**:
  - [ ] Extracts UserId from `IHttpContextAccessor` or identity service
  - [ ] Applies optional isRead/type filters
  - [ ] Returns `PaginatedResult<NotificationListDto>`

#### Task 3.5: Create Query — GetNotificationByIdRequest
- **File**: `Explore.Application/Features/Notifications/Requests/Queries/GetNotificationByIdRequest.cs`
- **Acceptance Criteria**:
  - [ ] Implements `IRequest<NotificationDto?>`
  - [ ] Includes `Guid Id` property

#### Task 3.6: Create Handler — GetNotificationByIdRequestHandler
- **File**: `Explore.Application/Features/Notifications/Handlers/Queries/GetNotificationByIdRequestHandler.cs`
- **Logic**: Get by ID for the authenticated user, throw NotFoundException if not found
- **Acceptance Criteria**:
  - [ ] Verifies notification belongs to authenticated user
  - [ ] Throws `NotFoundException` for missing/wrong-user

#### Task 3.7: Create Query — GetUnreadCountRequest
- **File**: `Explore.Application/Features/Notifications/Requests/Queries/GetUnreadCountRequest.cs`
- **Acceptance Criteria**:
  - [ ] Implements `IRequest<UnreadCountDto>`

#### Task 3.8: Create Handler — GetUnreadCountRequestHandler
- **File**: `Explore.Application/Features/Notifications/Handlers/Queries/GetUnreadCountRequestHandler.cs`
- **Logic**: Call repository.GetUnreadCount for authenticated user
- **Acceptance Criteria**:
  - [ ] Returns `UnreadCountDto` with accurate count
  - [ ] Uses partial index for performance

#### Task 3.9: Create Command — MarkNotificationAsReadCommand
- **File**: `Explore.Application/Features/Notifications/Requests/Commands/MarkNotificationAsReadCommand.cs`
- **Properties**: `Guid Id`
- **Acceptance Criteria**:
  - [ ] Implements `IRequest<BaseCommandResponse<Guid>>`

#### Task 3.10: Create Handler — MarkNotificationAsReadCommandHandler
- **File**: `Explore.Application/Features/Notifications/Handlers/Commands/MarkNotificationAsReadCommandHandler.cs`
- **Logic**: Verify ownership, set IsRead=true + ReadAt=now, save
- **Acceptance Criteria**:
  - [ ] Verifies notification belongs to authenticated user
  - [ ] Idempotent (marking already-read notification succeeds)
  - [ ] Sets `ReadAt` timestamp

#### Task 3.11: Create Command — MarkAllNotificationsAsReadCommand
- **File**: `Explore.Application/Features/Notifications/Requests/Commands/MarkAllNotificationsAsReadCommand.cs`
- **Acceptance Criteria**:
  - [ ] Implements `IRequest<BaseCommandResponse<Guid>>`
  - [ ] No properties needed (user extracted from context)

#### Task 3.12: Create Handler — MarkAllNotificationsAsReadCommandHandler
- **File**: `Explore.Application/Features/Notifications/Handlers/Commands/MarkAllNotificationsAsReadCommandHandler.cs`
- **Logic**: Call repository.MarkAllAsRead(userId), return count in message
- **Acceptance Criteria**:
  - [ ] Uses bulk `ExecuteUpdateAsync` via repository
  - [ ] Returns affected count in response message
  - [ ] Idempotent (0 affected = still success)

#### Task 3.13: Create Command — DeleteNotificationCommand
- **File**: `Explore.Application/Features/Notifications/Requests/Commands/DeleteNotificationCommand.cs`
- **Acceptance Criteria**:
  - [ ] Implements `IRequest<bool>`
  - [ ] Includes `Guid Id`

#### Task 3.14: Create Handler — DeleteNotificationCommandHandler
- **File**: `Explore.Application/Features/Notifications/Handlers/Commands/DeleteNotificationCommandHandler.cs`
- **Logic**: Verify ownership, soft-delete
- **Acceptance Criteria**:
  - [ ] Verifies notification belongs to authenticated user
  - [ ] Uses soft delete (ISoftDeletable)

---

### Phase 4: API Layer
**Effort: M | Skills: `clean-architecture-rules`**

#### Task 4.1: Add Notification Route Names
- **File**: `Explore.API/Hateoas/RouteNames.cs`
- **Constants**:
  - `GetNotifications`, `GetNotificationById`, `GetUnreadNotificationCount`
  - `MarkNotificationAsRead`, `MarkAllNotificationsAsRead`, `DeleteNotification`
- **Acceptance Criteria**:
  - [ ] Constants added in a `#region Notification Routes` section

#### Task 4.2: Create NotificationController
- **File**: `Explore.API/Controllers/NotificationController.cs`
- **Endpoints** (all `[Authorize]`):
  - `GET /api/notification` (Name = GetNotifications) → paginated list with optional filters
  - `GET /api/notification/unread-count` (Name = GetUnreadNotificationCount) → unread count
  - `GET /api/notification/{id}` (Name = GetNotificationById) → single detail
  - `PUT /api/notification/{id}/read` (Name = MarkNotificationAsRead) → mark single as read
  - `PUT /api/notification/mark-all-read` (Name = MarkAllNotificationsAsRead) → bulk mark
  - `DELETE /api/notification/{id}` (Name = DeleteNotification) → soft delete
- **Acceptance Criteria**:
  - [ ] All endpoints require `[Authorize]`
  - [ ] Uses HATEOAS assembler for responses
  - [ ] Proper `[ProducesResponseType]` attributes
  - [ ] CancellationToken on all endpoints
  - [ ] Output cache on GET endpoints (ListData/DetailData policies)

#### Task 4.3: Create Notification HATEOAS Link Policies
- **Files**:
  - `Explore.API/Hateoas/Policies/NotificationLinkPolicy.cs`
- **Links**:
  - Detail: self, mark-as-read, delete
  - Collection: self, mark-all-read, unread-count
- **Acceptance Criteria**:
  - [ ] `NotificationDetailLinkPolicy` implements `ILinkPolicy<NotificationDto>`
  - [ ] `NotificationCollectionLinkPolicy` implements `ICollectionLinkPolicy<NotificationListDto>`
  - [ ] All links use RouteNames constants

#### Task 4.4: Create Notification Resource Assembler
- **File**: `Explore.API/Hateoas/Assemblers/NotificationResourceAssembler.cs`
- **Acceptance Criteria**:
  - [ ] Extends `ResourceAssemblerBase<NotificationDto, NotificationListDto>`
  - [ ] Registered in DI

#### Task 4.5: Register HATEOAS Services in DI
- **File**: Appropriate DI registration file (e.g., `Program.cs` or extensions)
- **Acceptance Criteria**:
  - [ ] Link policies registered
  - [ ] Resource assembler registered

---

### Phase 5: Testing
**Effort: M | Skills: `clean-architecture-rules`**

#### Task 5.1: Unit Tests — Query Handlers
- **File**: `Event.Application.UnitTests/Features/Notifications/...`
- **Tests**:
  - GetUserNotifications returns paginated results
  - GetUserNotifications filters by isRead
  - GetNotificationById returns notification for owner
  - GetNotificationById throws NotFoundException for non-owner
  - GetUnreadCount returns correct count
- **Acceptance Criteria**:
  - [ ] All query handler paths tested
  - [ ] Mock repository

#### Task 5.2: Unit Tests — Command Handlers
- **Tests**:
  - MarkAsRead succeeds for owner
  - MarkAsRead idempotent (already read)
  - MarkAsRead throws for non-owner
  - MarkAllAsRead returns affected count
  - MarkAllAsRead idempotent (0 affected)
  - DeleteNotification succeeds for owner
  - DeleteNotification throws for non-owner
- **Acceptance Criteria**:
  - [ ] All command handler paths tested
  - [ ] Verify ownership checks

#### Task 5.3: Architecture Tests
- **File**: `Event.Architecture.Tests/...`
- **Acceptance Criteria**:
  - [ ] Notification follows same architecture patterns as other features
  - [ ] Verify any new architecture tests pass

---

### Phase 6: Documentation
**Effort: S**

#### Task 6.1: Update Domain Documentation
- **File**: `docs/DOMAIN.md` — add Notification to core aggregates list
- **Acceptance Criteria**:
  - [ ] Notification listed with its fields and relationships

#### Task 6.2: Update API Documentation
- **File**: `docs/API.md` — add Notification endpoint group
- **Acceptance Criteria**:
  - [ ] New endpoint group documented

---

## Risk Assessment

### Potential Risks & Unknowns

1. **Bulk mark-all-as-read under load**: If a user has thousands of unread notifications, `ExecuteUpdateAsync` could generate significant WAL. Mitigation: at our scale this is negligible; if it becomes an issue, batch the UPDATE in chunks of 1000.

2. **Unread count performance**: Even with the partial index, `COUNT(*)` can be slow with millions of rows per user. Mitigation: Use HybridCache with short TTL (30s) on the unread count endpoint. Invalidate on mark-as-read/mark-all operations.

3. **Race condition on mark-all-as-read**: If a new notification arrives between the user clicking "mark all" and the UPDATE executing, the new notification could be incorrectly marked as read. Mitigation: Use `WHERE CreatedAt <= @cutoff` timestamp in the bulk update.

4. **Entity change migration risk**: Adding `ISoftDeletable` + `IAuditableEntity` fields to an existing table requires a migration. If the table already has data, new columns need defaults. Mitigation: `IsDeleted` defaults to `false`, audit fields are nullable.

5. **Missing user identity service**: Need to verify how other features extract the authenticated UserId. The project uses a fallback chain (`sub` → `nameidentifier` → `sid`). Need to ensure Notification handlers follow the same pattern.

---

## Success Metrics

- [ ] All 6 API endpoints respond correctly
- [ ] Unread count query uses partial index (verify with EXPLAIN ANALYZE)
- [ ] Mark-all-as-read completes in <100ms for up to 10,000 unread notifications
- [ ] All unit tests pass
- [ ] Build succeeds with no warnings
- [ ] Architecture tests pass
- [ ] HATEOAS links render correctly with proper authorization
