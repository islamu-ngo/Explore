// ABOUTME: Repository implementation for NotificationEntityType lookup table.
// ABOUTME: Follows ApprovalStatusRepository pattern with generic repository base.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class NotificationEntityTypeRepository : GenericRepository<NotificationEntityType, int>, INotificationEntityTypeRepository
{
    public NotificationEntityTypeRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
