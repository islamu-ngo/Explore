// ABOUTME: Persistence repository for user notification category preferences.
// ABOUTME: Provides tenant-scoped lookup helpers used by unsubscribe and dispatch-time consent checks.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class UserNotificationPreferenceRepository(ExploreDbContext dbContext)
    : GenericRepository<UserNotificationPreference, Guid>(dbContext), IUserNotificationPreferenceRepository
{
    public async Task<UserNotificationPreference?> GetByUserAndCategory(Guid tenantId, Guid userId, string category)
    {
        return await dbContext.UserNotificationPreferences
            .FirstOrDefaultAsync(preference =>
                preference.TenantId == tenantId
                && preference.UserId == userId
                && preference.Category == category);
    }

    public async Task<List<UserNotificationPreference>> GetAllForUser(Guid tenantId, Guid userId)
    {
        return await dbContext.UserNotificationPreferences
            .AsNoTracking()
            .Where(preference => preference.TenantId == tenantId && preference.UserId == userId)
            .OrderBy(preference => preference.Category)
            .ToListAsync();
    }
}
