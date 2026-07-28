// ABOUTME: EF Core repository for the active instance-scoped platform contribution-setting version.
// ABOUTME: Loads stored contribution choices as entities for immutable revision handling.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class PlatformContributionSettingRepository(ExploreDbContext dbContext) : IPlatformContributionSettingRepository
{
    public Task<PlatformContributionSetting?> GetActiveAsync(CancellationToken cancellationToken) =>
        dbContext.PlatformContributionSettings.AsNoTracking().Include(setting => setting.Options)
            .SingleOrDefaultAsync(setting => setting.IsActive, cancellationToken);

    public async Task AddAsync(PlatformContributionSetting setting, CancellationToken cancellationToken)
    {
        await dbContext.PlatformContributionSettings.AddAsync(setting, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PlatformContributionSetting setting, CancellationToken cancellationToken)
    {
        dbContext.Entry(setting).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
