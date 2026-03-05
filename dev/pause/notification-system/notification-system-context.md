# Notification System — Context

> Last Updated: 2026-03-04

## SESSION PROGRESS (2026-03-04)

### ✅ ALL PHASES COMPLETE
- **Phase 1 (Domain)**: Entity updated with IAuditableEntity + ISoftDeletable
- **Phase 2 (Persistence)**: EF config, repository, DI, seed data
- **Phase 3 (Application)**: DTOs, mappings, 3 query + 3 command handlers
- **Phase 4 (API)**: Controller (6 endpoints), HATEOAS link policies + assembler
- **Phase 5 (Testing)**: 18 unit tests across 6 test classes
- **Phase 6 (Documentation)**: DOMAIN.md + API.md updated
- **Refactor 1**: String Type/EntityType → lookup entities (NotificationType, NotificationEntityType) following ApprovalStatus pattern
- **Refactor 2**: Added notification scope + actor targeting (SourceActorId, RecipientContextActorId, NotificationScopeId using ActorType)

### 🟡 REMAINING
- **EF Migration** — not yet created (all previous migrations were deleted from repo; migration strategy TBD)
- **Git commit** — all notification work is uncommitted

### ⚠️ BLOCKERS
- None

---

## Key Design Decisions

### 1. Per-Row `is_read` Pattern (Not Watermark)
**Why**: Our scale (event platform) doesn't warrant watermark complexity. YouTube and Mastodon both use per-row read state.

### 2. Partial Index for Unread Notifications
**Why**: Most frequent query is "show me unread" and "how many unread?". PostgreSQL partial index on `WHERE is_read = false AND is_deleted = false`.

### 3. `ExecuteUpdateAsync` for Bulk Mark-All-Read
**Why**: Single SQL UPDATE, no entity tracking overhead. Timestamp cutoff prevents race conditions.

### 4. All Endpoints Require Authentication
**Why**: Notifications are personal data. Every endpoint must verify user ownership.

### 5. Lookup Entities Instead of Strings (Refactor 1)
**Why**: Replaced `string Type` and `string? EntityType` with proper FK lookup entities (`NotificationType`, `NotificationEntityType`) following the ApprovalStatus pattern for type safety and referential integrity.

### 6. Materialized Fan-Out with Scope Metadata (Refactor 2)
**Why**: Notifications stay per-human-user (`UserId` is the recipient). Fan-out from org/group → individual users happens at write time. Read path remains O(1). Added:
- `SourceActorId` (Guid?, FK→Actor) — who/what triggered the notification
- `RecipientContextActorId` (Guid?, FK→Actor) — which org/group context for UI differentiation
- `NotificationScopeId` (int, FK→ActorType) — reuses existing ActorType as scope classifier (User=1/Personal, Organization=2, Group=4, System=5)

### 7. Bots/System Are Senders, Not Recipients
**Why**: Bots need guaranteed delivery/ordering/retry. Notifications are best-effort for humans. Bots should consume domain events directly. But bots/system CAN be notification sources via `SourceActorId`.

---

## Current Notification Entity Fields

```csharp
public class Notification : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    Guid Id
    Guid UserId → User (recipient, always a human)
    int NotificationTypeId → NotificationType (lookup, 10 types)
    string Title
    string? Body
    bool IsRead
    DateTime? ReadAt
    int? NotificationEntityTypeId → NotificationEntityType (lookup, 6 types)
    string? EntityId (deep link target)
    int NotificationScopeId → ActorType (scope: User=1, Organization=2, Group=4, System=5)
    Guid? SourceActorId → Actor (who triggered it)
    Guid? RecipientContextActorId → Actor (org/group context)
    Guid TenantId → Tenant
    // + audit fields + soft delete fields
}
```

## Key Files (All Created/Modified)

### Domain
| File | Status | Notes |
|---|---|---|
| `Explore.Domain/Notification.cs` | ✅ Modified | Full entity with all FKs |
| `Explore.Domain/NotificationType.cs` | ✅ Created | Lookup entity (int Id, MasterCode, FullName, Description) |
| `Explore.Domain/NotificationEntityType.cs` | ✅ Created | Lookup entity |
| `Explore.Domain/Enums/NotificationTypeEnum.cs` | ✅ Created | 10 values |
| `Explore.Domain/Enums/NotificationEntityTypeEnum.cs` | ✅ Created | 6 values |

### Persistence
| File | Status | Notes |
|---|---|---|
| `Explore.Persistence/Configurations/Entities/NotificationConfiguration.cs` | ✅ Modified | FK rels, 4 indexes including partial |
| `Explore.Persistence/Configurations/Entities/NotificationTypeConfiguration.cs` | ✅ Created | |
| `Explore.Persistence/Configurations/Entities/NotificationEntityTypeConfiguration.cs` | ✅ Created | |
| `Explore.Persistence/ExploreDbContext.cs` | ✅ Modified | 2 new DbSets + SoftDelete filter |
| `Explore.Persistence/Repositories/NotificationRepository.cs` | ✅ Created | 5 custom methods |
| `Explore.Persistence/Repositories/NotificationTypeRepository.cs` | ✅ Created | |
| `Explore.Persistence/Repositories/NotificationEntityTypeRepository.cs` | ✅ Created | |
| `Explore.Persistence/PersistenceServicesRegistration.cs` | ✅ Modified | 3 DI registrations |
| `Explore.Persistence/Seed/LookupTableSeeder.cs` | ✅ Modified | 2 new seed methods |

### Application
| File | Status | Notes |
|---|---|---|
| `Explore.Application/Contracts/Persistence/INotificationRepository.cs` | ✅ Created | 5 methods with scope filter |
| `Explore.Application/Contracts/Persistence/INotificationTypeRepository.cs` | ✅ Created | |
| `Explore.Application/Contracts/Persistence/INotificationEntityTypeRepository.cs` | ✅ Created | |
| `Explore.Application/DTOs/Notification/NotificationDto.cs` | ✅ Created | Full detail with scope/actor fields |
| `Explore.Application/DTOs/Notification/NotificationListDto.cs` | ✅ Created | List with scope/actor fields |
| `Explore.Application/DTOs/Notification/UnreadCountDto.cs` | ✅ Created | |
| `Explore.Application/Profiles/MappingProfile.cs` | ✅ Modified | CreateNotificationMappings() |
| `Explore.Application/Features/Notifications/Requests/Queries/*.cs` | ✅ Created | 3 query requests |
| `Explore.Application/Features/Notifications/Handlers/Queries/*.cs` | ✅ Created | 3 query handlers |
| `Explore.Application/Features/Notifications/Requests/Commands/*.cs` | ✅ Created | 3 command requests |
| `Explore.Application/Features/Notifications/Handlers/Commands/*.cs` | ✅ Created | 3 command handlers |

### API
| File | Status | Notes |
|---|---|---|
| `Explore.API/Controllers/NotificationController.cs` | ✅ Created | 6 endpoints, all [Authorize] |
| `Explore.API/Hateoas/RouteNames.cs` | ✅ Modified | 6 route constants |
| `Explore.API/Hateoas/Policies/NotificationLinkPolicy.cs` | ✅ Created | Detail + Collection |
| `Explore.API/Hateoas/Assemblers/NotificationResourceAssembler.cs` | ✅ Created | |
| `Explore.API/Extensions/HateoasAssemblerRegistration.cs` | ✅ Modified | |

### Tests
| File | Status | Notes |
|---|---|---|
| `Event.Application.UnitTests/Features/Notifications/Queries/*.cs` | ✅ Created | 3 test classes (9 tests) |
| `Event.Application.UnitTests/Features/Notifications/Commands/*.cs` | ✅ Created | 3 test classes (9 tests) |

## Key Interfaces (Current Signatures)

### INotificationRepository
```csharp
public interface INotificationRepository : IGenericRepository<Notification, Guid>
{
    Task<(List<Notification> Items, int TotalCount)> GetUserNotificationsPaged(
        Guid userId, int pageNumber, int pageSize, bool? isRead = null,
        int? notificationTypeId = null, int? notificationScopeId = null);
    Task<int> GetUnreadCount(Guid userId, int? notificationScopeId = null);
    Task<bool> MarkAsRead(Guid notificationId, Guid userId);
    Task<int> MarkAllAsRead(Guid userId, DateTime? cutoff = null);
    Task<Notification?> GetByIdForUser(Guid notificationId, Guid userId);
}
```

### API Endpoints
```
GET    /api/notification?pageNumber&pageSize&isRead&notificationTypeId&notificationScopeId
GET    /api/notification/unread-count?notificationScopeId
GET    /api/notification/{id}
PATCH  /api/notification/{id}/read
POST   /api/notification/read-all
DELETE /api/notification/{id}
```

### Indexes
```
ix_notifications_tenant_user_unread  — (TenantId, UserId, IsRead, CreatedAt DESC)
ix_notifications_unread_by_user      — (TenantId, UserId, CreatedAt DESC) WHERE is_read=false AND is_deleted=false
ix_notifications_tenant_type         — (TenantId, NotificationTypeId)
ix_notifications_user_scope          — (UserId, NotificationScopeId, IsRead)
```

---

## Build & Test Status

- **Build**: ✅ 0 errors (warnings are pre-existing)
- **Application unit tests**: ✅ 363/363 (18 new notification tests)
- **Domain unit tests**: ✅ 79/79
- **Architecture tests**: ✅ 32/32
- **Total**: 474 tests passing

## Quick Resume

To continue work:
1. Read this file
2. Remaining work: EF migration creation, git commit
3. Future features: notification creation/dispatch handlers (domain events → fan-out), push delivery, preferences
