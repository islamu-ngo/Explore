// ABOUTME: Repository implementation for notification queries and bulk operations.
// ABOUTME: Uses ExecuteUpdateAsync for bulk mark-all-as-read, partial index for unread count.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class NotificationRepository : GenericRepository<Notification, Guid>, INotificationRepository
{
    private readonly ExploreDbContext _dbContext;

    public NotificationRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(List<Notification> Items, int TotalCount)> GetUserNotificationsPaged(
        Guid userId, int pageNumber, int pageSize, bool? isRead = null, int? notificationTypeId = null, int? notificationScopeId = null)
    {
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Include(n => n.NotificationType)
            .Include(n => n.NotificationEntityType)
            .Include(n => n.NotificationScope)
            .Include(n => n.SourceActor).ThenInclude(a => a!.Pii)
            .Include(n => n.RecipientContextActor).ThenInclude(a => a!.Pii)
            .Where(n => n.UserId == userId);

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        if (notificationTypeId.HasValue)
            query = query.Where(n => n.NotificationTypeId == notificationTypeId.Value);

        if (notificationScopeId.HasValue)
            query = query.Where(n => n.NotificationScopeId == notificationScopeId.Value);

        query = query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<int> GetUnreadCount(Guid userId, int? notificationScopeId = null)
    {
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead);

        if (notificationScopeId.HasValue)
            query = query.Where(n => n.NotificationScopeId == notificationScopeId.Value);

        return await query.CountAsync();
    }

    public async Task<bool> MarkAsRead(Guid notificationId, Guid userId)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> MarkAllAsRead(Guid userId, DateTime? cutoff = null)
    {
        var effectiveCutoff = cutoff ?? DateTime.UtcNow;

        return await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && n.CreatedAt <= effectiveCutoff)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow));
    }

    public async Task<Notification?> GetByIdForUser(Guid notificationId, Guid userId)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .Include(n => n.NotificationType)
            .Include(n => n.NotificationEntityType)
            .Include(n => n.NotificationScope)
            .Include(n => n.SourceActor).ThenInclude(a => a!.Pii)
            .Include(n => n.RecipientContextActor).ThenInclude(a => a!.Pii)
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
    }
}
