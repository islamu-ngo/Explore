// ABOUTME: EF repository for tenant-filtered EventLocation mutation and bounded disclosure reads.
// ABOUTME: Returns entities only and makes tracking behavior explicit at each persistence boundary.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class EventLocationRepository(ExploreDbContext dbContext) : IEventLocationRepository
{
    public async Task<EventLocation> AddAsync(
        EventLocation eventLocation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventLocation);
        RequireTenant(eventLocation.TenantId);
        EventLocationDisclosureAudit initialAudit = eventLocation.CreateInitialDisclosureAudit();
        dbContext.EventLocations.Add(eventLocation);
        dbContext.EventLocationDisclosureAudits.Add(initialAudit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return eventLocation;
    }

    public Task<EventLocation?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        RequireId(id, nameof(id));
        RequireTenant();
        return dbContext.EventLocations
            .Include(item => item.Location)
            .Include(item => item.FullDetailsAudience)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<EventLocation>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        RequireTenant();
        Guid[] normalizedIds = ids.Distinct().ToArray();
        if (normalizedIds.Length > IEventLocationRepository.MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ids),
                $"EventLocation batches cannot exceed {IEventLocationRepository.MaximumBatchSize} unique ids.");
        }

        if (normalizedIds.Length == 0)
        {
            return [];
        }

        return await dbContext.EventLocations
            .AsNoTracking()
            .Include(item => item.Location)
            .Include(item => item.FullDetailsAudience)
            .Where(item => normalizedIds.Contains(item.Id))
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<EventLocation?> FindActivePhysicalAsync(
        Guid eventId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        RequireId(eventId, nameof(eventId));
        RequireId(locationId, nameof(locationId));
        RequireTenant();
        return dbContext.EventLocations
            .AsNoTracking()
            .Include(item => item.Location)
            .Include(item => item.FullDetailsAudience)
            .SingleOrDefaultAsync(
                item => item.EventId == eventId
                    && item.LocationId == locationId
                    && !item.IsToBeAnnounced,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private static void RequireId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A non-empty id is required.", parameterName);
        }
    }

    private void RequireTenant(Guid? entityTenantId = null)
    {
        if (dbContext.IsTenantFilterBypassed)
        {
            return;
        }

        Guid tenantId = dbContext.TenantFilterTenantId
            ?? throw new InvalidOperationException("A tenant context is required for EventLocation persistence.");
        if (entityTenantId.HasValue && entityTenantId.Value != tenantId)
        {
            throw new InvalidOperationException("EventLocation must belong to the current tenant.");
        }
    }
}
