// ABOUTME: EF Core repository for immutable notification fanout occurrences.
// ABOUTME: Resolves worker pointers with an exact tenant-and-occurrence predicate.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Models.InternalEvents;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class NotificationFanoutOccurrenceRepository
    : GenericRepository<NotificationFanoutOccurrence, Guid>, INotificationFanoutOccurrenceRepository
{
    private readonly ExploreDbContext dbContext;

    public NotificationFanoutOccurrenceRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<NotificationFanoutOccurrence?> GetByPointerAsync(
        NotificationFanoutOccurrenceRequested pointer,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        if (pointer.Version != NotificationFanoutOccurrenceRequested.CurrentVersion)
        {
            return null;
        }

        IQueryable<NotificationFanoutOccurrence> query = dbContext.NotificationFanoutOccurrences
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Where(occurrence => occurrence.TenantId == pointer.TenantId
                && occurrence.Id == pointer.OccurrenceId);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken);
    }
}
