// ABOUTME: Repository implementation for NotificationType lookup table.
// ABOUTME: Follows ApprovalStatusRepository pattern with generic repository base.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class NotificationTypeRepository : GenericRepository<NotificationType, int>, INotificationTypeRepository
{
    public NotificationTypeRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
