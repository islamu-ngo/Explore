// ABOUTME: Repository contract for tenant-local user profile and moderation metadata.
// ABOUTME: Separates tenant admin profile edits from global User.Pii updates.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ITenantUserProfileRepository : IGenericRepository<TenantUserProfile, Guid>
{
    Task<TenantUserProfile?> GetByTenantUserAsync(Guid tenantUserId, CancellationToken cancellationToken = default);
}
