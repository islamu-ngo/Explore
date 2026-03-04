# Notification System — Context

> Last Updated: 2026-03-03

## SESSION PROGRESS (2026-03-03)

### ✅ COMPLETED
- Research phase: YouTube, Mastodon, enterprise notification patterns (Tavily)
- Current state analysis: identified existing entity + EF config
- Plan creation: full implementation plan with 6 phases, 25 tasks
- Context file creation

### 🟡 IN PROGRESS
- Task breakdown file creation

### ⚠️ BLOCKERS
- None

---

## Key Design Decisions

### 1. Per-Row `is_read` Pattern (Not Watermark)
**Why**: The entity already has `IsRead` + `ReadAt`. Our scale (event platform) doesn't warrant the complexity of a watermark/high-water-mark pattern. YouTube and Mastodon both use per-row read state at similar or larger scales.

### 2. Partial Index for Unread Notifications
**Why**: The most frequent query is "show me unread notifications" and "how many unread?". A partial index on `WHERE is_read = false` dramatically reduces index size and speeds up both queries. PostgreSQL-specific optimization.

### 3. `ExecuteUpdateAsync` for Bulk Mark-All-Read
**Why**: Loading thousands of entities into memory just to flip a boolean is wasteful. EF Core 7+ `ExecuteUpdateAsync` generates a single SQL UPDATE, no entity tracking overhead. This is the standard enterprise pattern for bulk operations.

### 4. All Endpoints Require Authentication
**Why**: Unlike most entities where GET is `[AllowAnonymous]`, notifications are personal data. Every endpoint must verify the user owns the notification. This deviates from the typical project pattern but is correct for this feature.

### 5. Timestamp Guard on Mark-All-Read
**Why**: To prevent a race condition where a new notification arrives during the bulk update, the mark-all-read command should use `WHERE CreatedAt <= @cutoff` (where cutoff is the request timestamp). This ensures newly arrived notifications remain unread.

### 6. ISoftDeletable for Notification Deletion
**Why**: Users may want to dismiss/delete notifications, but for audit trail and data recovery, soft delete is preferred. The entity currently lacks `ISoftDeletable` — adding it is a migration task.

---

## Key Files (Existing)

| File | Purpose | Notes |
|---|---|---|
| `Explore.Domain/Notification.cs` | Domain entity | Has Id, UserId, Type, Title, Body, IsRead, ReadAt, EntityType, EntityId, CreatedAt, TenantId |
| `Explore.Persistence/Configurations/Entities/NotificationConfiguration.cs` | EF config | Indexes, constraints, relationships |
| `Explore.Persistence/ExploreDbContext.cs` | DbContext | `DbSet<Notification>` at line 361, tenant filter at line 188 |

## Key Files (To Create)

| File | Purpose |
|---|---|
| `Explore.Application/Contracts/Persistence/INotificationRepository.cs` | Repository interface |
| `Explore.Persistence/Repositories/NotificationRepository.cs` | Repository implementation |
| `Explore.Application/DTOs/Notification/NotificationDto.cs` | Detail DTO |
| `Explore.Application/DTOs/Notification/NotificationListDto.cs` | List DTO |
| `Explore.Application/DTOs/Notification/UnreadCountDto.cs` | Count DTO |
| `Explore.Application/Profiles/NotificationProfile.cs` | AutoMapper profile |
| `Explore.Application/Features/Notifications/Requests/Queries/*.cs` | Query requests (3 files) |
| `Explore.Application/Features/Notifications/Handlers/Queries/*.cs` | Query handlers (3 files) |
| `Explore.Application/Features/Notifications/Requests/Commands/*.cs` | Command requests (3 files) |
| `Explore.Application/Features/Notifications/Handlers/Commands/*.cs` | Command handlers (3 files) |
| `Explore.API/Controllers/NotificationController.cs` | API controller |
| `Explore.API/Hateoas/Policies/NotificationLinkPolicy.cs` | HATEOAS link policies |
| `Explore.API/Hateoas/Assemblers/NotificationResourceAssembler.cs` | HATEOAS assembler |

## Key Interfaces (Signatures)

### INotificationRepository
```csharp
public interface INotificationRepository : IGenericRepository<Notification, Guid>
{
    Task<(List<Notification> Items, int TotalCount)> GetUserNotificationsPaged(
        Guid userId, int pageNumber, int pageSize, bool? isRead = null, string? type = null);
    Task<int> GetUnreadCount(Guid userId);
    Task<bool> MarkAsRead(Guid notificationId, Guid userId);
    Task<int> MarkAllAsRead(Guid userId, DateTime? cutoff = null);
    Task<Notification?> GetByIdForUser(Guid notificationId, Guid userId);
}
```

### API Endpoints
```
GET    /api/notification                    → PaginatedResult<NotificationListDto>
GET    /api/notification/unread-count       → UnreadCountDto
GET    /api/notification/{id}               → NotificationDto
PUT    /api/notification/{id}/read          → BaseCommandResponse<Guid>
PUT    /api/notification/mark-all-read      → BaseCommandResponse<Guid>
DELETE /api/notification/{id}               → 204 NoContent
```

---

## Dependencies / External References

- **Identity extraction**: Follow `sub` → `nameidentifier` → `sid` fallback (docs/SECURITY.md)
- **HATEOAS pattern**: Match `EventRegistration` assembler/policy pattern exactly
- **Pagination**: Use `PaginatedResult<T>.NormalizeParameters()` and `PaginatedResult<T>.Create()`
- **RouteNames**: Add constants following existing naming convention
- **DI Registration**: `PersistenceServicesRegistration.cs` for repository, API layer for HATEOAS

## Quick Resume

To continue implementation:
1. Read this file + `notification-system-plan.md` + `notification-system-tasks.md`
2. Check task list for current phase
3. Start with Phase 1 (Domain layer) if not yet started
4. Build + test after each phase
