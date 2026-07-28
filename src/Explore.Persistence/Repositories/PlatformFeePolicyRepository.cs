// ABOUTME: EF Core repository for the active instance-scoped platform fee policy version.
// ABOUTME: Loads immutable fixed-charge history as Domain entities without tenant-filter bypasses.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class PlatformFeePolicyRepository(ExploreDbContext dbContext) : IPlatformFeePolicyRepository
{
    public Task<PlatformFeePolicy?> GetActiveAsync(CancellationToken cancellationToken) =>
        dbContext.PlatformFeePolicies.AsNoTracking().Include(policy => policy.FixedCharges)
            .SingleOrDefaultAsync(policy => policy.IsActive, cancellationToken);

    public async Task AddAsync(PlatformFeePolicy policy, CancellationToken cancellationToken)
    {
        await dbContext.PlatformFeePolicies.AddAsync(policy, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PlatformFeePolicy policy, CancellationToken cancellationToken)
    {
        dbContext.Entry(policy).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
