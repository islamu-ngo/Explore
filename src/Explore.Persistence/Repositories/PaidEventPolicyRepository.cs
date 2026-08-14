// ABOUTME: EF Core repository for instance and tenant paid-event policy versions.
// ABOUTME: Uses tenant-safe filters and entity tracking for active policy revision writes.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class PaidEventPolicyRepository(ExploreDbContext dbContext) : IPaidEventPolicyRepository
{
    public Task<PaidEventPolicyVersion?> GetActiveInstanceAsync(CancellationToken cancellationToken) =>
        dbContext.PaidEventPolicyVersions.SingleOrDefaultAsync(policy => policy.TenantId == null && policy.IsActive, cancellationToken);

    public Task<PaidEventPolicyVersion?> GetActiveTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        dbContext.PaidEventPolicyVersions.SingleOrDefaultAsync(policy => policy.TenantId == tenantId && policy.IsActive, cancellationToken);

    public async Task<PaidEventPolicyVersion[]> ListTenantHistoryAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await dbContext.PaidEventPolicyVersions
            .AsNoTracking()
            .Where(policy => policy.TenantId == tenantId)
            .OrderBy(policy => policy.VersionNumber)
            .ToArrayAsync(cancellationToken);

    public async Task AddAsync(PaidEventPolicyVersion policy, CancellationToken cancellationToken) =>
        await dbContext.PaidEventPolicyVersions.AddAsync(policy, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
