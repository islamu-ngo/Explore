// ABOUTME: Persistence contract for immutable instance-scoped contribution-setting history.
// ABOUTME: Returns Domain entities with stored option choices rather than DTO projections.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IPlatformContributionSettingRepository
{
    Task<PlatformContributionSetting?> GetActiveAsync(CancellationToken cancellationToken);

    Task AddAsync(PlatformContributionSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(PlatformContributionSetting setting, CancellationToken cancellationToken);
}
