// ABOUTME: Reports bounded primary-database availability for admission scanner health.
// ABOUTME: Exposes no entity, identifier, credential, or provider diagnostic detail.

using Explore.Application.Contracts.Admissions;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence;

public sealed class AdmissionCheckInDatabaseHealthProbe(ExploreDbContext dbContext)
    : IAdmissionCheckInHealthProbe
{
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
        dbContext.Database.CanConnectAsync(cancellationToken);
}
