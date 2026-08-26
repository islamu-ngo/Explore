// ABOUTME: Loads and updates exact tenant-event admission targets for operational controls.
// ABOUTME: Returns only Domain targets so Application owns mapping and operational decisions.

using Explore.Application.Contracts.Admissions;
using Explore.Domain;
using Explore.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class AdmissionTargetOperationsRepository(ExploreDbContext dbContext)
    : IAdmissionTargetOperationsRepository
{
    public async Task<AdmissionTarget?> GetAsync(
        Guid tenantId,
        Guid eventId,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            await RelationalEntityRowFence.AcquireAsync<AdmissionTarget>(
                dbContext,
                tenantId,
                "id",
                targetId,
                cancellationToken);
            return await dbContext.AdmissionTargets.SingleOrDefaultAsync(target =>
                target.TenantId == tenantId &&
                target.EventId == eventId &&
                target.Id == targetId,
                cancellationToken);
        }

        return await dbContext.AdmissionTargets.AsNoTracking().SingleOrDefaultAsync(target =>
            target.TenantId == tenantId &&
            target.EventId == eventId &&
            target.Id == targetId,
            cancellationToken);
    }

    public Task<AdmissionTarget> UpdateAsync(
        AdmissionTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        dbContext.AdmissionTargets.Update(target);
        return Task.FromResult(target);
    }
}
