// ABOUTME: Repository interface for UserAppearanceProfile — user-owned theme snapshots with lineage tracking.
// ABOUTME: Supports finding profiles by user/scope, finding existing clones, and managing defaults.

namespace Explore.Application.Contracts.Persistence;

using Explore.Domain;

public interface IUserAppearanceProfileRepository : IGenericRepository<UserAppearanceProfile, Guid>
{
    Task<IReadOnlyList<UserAppearanceProfile>> GetProfilesForUserAsync(Guid userId, Guid? tenantId, bool includeArchived = false);
    Task<UserAppearanceProfile?> GetDefaultProfileAsync(Guid userId, Guid? tenantId);
    Task<UserAppearanceProfile?> GetExistingCloneAsync(Guid userId, Guid? tenantId, Guid sourcePresetId);
    Task ClearDefaultAsync(Guid userId, Guid? tenantId, Guid? excludingProfileId = null);
}
