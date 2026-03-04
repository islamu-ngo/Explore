// ABOUTME: Repository contract for NotificationEntityType lookup table.
// ABOUTME: Extends generic repository for standard CRUD on notification entity types.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface INotificationEntityTypeRepository : IGenericRepository<NotificationEntityType, int>
{
}
