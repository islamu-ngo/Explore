// ABOUTME: EF Core repository for tenant-bound ticket catalog graphs and event-owned child lookups.
// ABOUTME: Applies exact event and tenant predicates so child IDs cannot cross event boundaries.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class EventTicketCatalogRepository(ExploreDbContext dbContext) : IEventTicketCatalogRepository
{
    public Task<EventTicketCatalogVersion?> GetManagementCatalogAsync(Guid eventId, Guid tenantId, CancellationToken cancellationToken) =>
        ManagementGraph()
            .Where(catalog => catalog.EventId == eventId
                && catalog.TenantId == tenantId
                && catalog.TicketCatalogStatusId != (int)TicketCatalogStatusEnum.Retired)
            .OrderByDescending(catalog => catalog.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<EventTicketCatalogVersion?> GetPublishedCatalogAsync(Guid eventId, Guid tenantId, CancellationToken cancellationToken) =>
        CatalogDetailsQuery().FirstOrDefaultAsync(catalog => catalog.EventId == eventId
            && catalog.TenantId == tenantId
            && catalog.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Published, cancellationToken);

    public Task<EventTicketCatalogVersion?> GetDraftForUpdateAsync(Guid eventId, Guid tenantId, CancellationToken cancellationToken) =>
        ManagementGraph().FirstOrDefaultAsync(catalog => catalog.EventId == eventId
            && catalog.TenantId == tenantId
            && catalog.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Draft, cancellationToken);

    public Task<EventTicketCatalogVersion?> GetPublishedForUpdateAsync(Guid eventId, Guid tenantId, CancellationToken cancellationToken) =>
        ManagementGraph().FirstOrDefaultAsync(catalog => catalog.EventId == eventId
            && catalog.TenantId == tenantId
            && catalog.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Published, cancellationToken);

    public Task<EventTicketCatalogVersion?> GetByEventVersionAndTenantAsync(Guid eventId, int versionNumber, Guid tenantId, CancellationToken cancellationToken) =>
        CatalogDetailsQuery().FirstOrDefaultAsync(catalog => catalog.EventId == eventId && catalog.VersionNumber == versionNumber && catalog.TenantId == tenantId, cancellationToken);

    public Task<EventTicketType?> GetTicketTypeByIdEventAndTenantAsync(Guid ticketTypeId, Guid eventId, Guid tenantId, CancellationToken cancellationToken) =>
        dbContext.EventTicketTypes.AsNoTracking().FirstOrDefaultAsync(ticketType =>
            ticketType.Id == ticketTypeId
            && ticketType.TenantId == tenantId
            && dbContext.EventTicketCatalogVersions.Any(catalog => catalog.Id == ticketType.CatalogId && catalog.EventId == eventId && catalog.TenantId == tenantId), cancellationToken);

    public Task<EventCapacityPool?> GetCapacityPoolByIdEventAndTenantAsync(Guid capacityPoolId, Guid eventId, Guid tenantId, CancellationToken cancellationToken) =>
        dbContext.EventCapacityPools.AsNoTracking().FirstOrDefaultAsync(pool => pool.Id == capacityPoolId && pool.EventId == eventId && pool.TenantId == tenantId, cancellationToken);

    public async Task AddAsync(EventTicketCatalogVersion catalog, CancellationToken cancellationToken)
    {
        await dbContext.EventTicketCatalogVersions.AddAsync(catalog, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EventTicketCatalogVersion catalog, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddCapacityPoolAsync(EventCapacityPool pool, CancellationToken cancellationToken)
    {
        await dbContext.EventCapacityPools.AddAsync(pool, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCapacityPoolAsync(EventCapacityPool pool, CancellationToken cancellationToken)
    {
        dbContext.EventCapacityPools.Update(pool);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<EventTicketCatalogVersion> CatalogDetailsQuery() =>
        ManagementGraph().AsNoTracking();

    private IQueryable<EventTicketCatalogVersion> ManagementGraph() =>
        dbContext.EventTicketCatalogVersions
            .Include(catalog => catalog.TicketTypes)
            .ThenInclude(ticketType => ticketType.Entitlements);
}
