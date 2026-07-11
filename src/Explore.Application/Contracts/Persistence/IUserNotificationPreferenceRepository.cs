// ABOUTME: Repository contract for per-user notification category preferences.
// ABOUTME: Keeps unsubscribe and email dispatch checks entity-first without leaking EF queries upward.

namespace Explore.Application.Contracts.Persistence;

using Explore.Domain;

public interface IUserNotificationPreferenceRepository : IGenericRepository<UserNotificationPreference, Guid>
{
    Task<UserNotificationPreference?> GetByUserAndCategory(Guid tenantId, Guid userId, string category);

    Task<List<UserNotificationPreference>> GetAllForUser(Guid tenantId, Guid userId);
}
