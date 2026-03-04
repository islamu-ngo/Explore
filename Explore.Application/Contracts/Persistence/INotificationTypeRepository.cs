// ABOUTME: Repository contract for NotificationType lookup table.
// ABOUTME: Extends generic repository for standard CRUD on notification types.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface INotificationTypeRepository : IGenericRepository<NotificationType, int>
{
}
