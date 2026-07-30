// ABOUTME: EF Core repository for tenant-bound ticket catalog graphs and event-owned child lookups.
// ABOUTME: Applies exact event and tenant predicates so child IDs cannot cross event boundaries.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Domain;
using Explore.Domain.Enums;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class EventTicketCatalogRepository(ExploreDbContext dbContext) : IEventTicketCatalogRepository
{
    private const string PublishedCatalogUniqueIndexName = "ix_event_ticket_catalog_versions_tenant_id_event_id";
    private const string CatalogVersionNumberUniqueIndexName = "ix_event_ticket_catalog_versions_tenant_id_event_id_version_nu";
    private const string CapacityPoolNameUniqueIndexName = "ix_event_capacity_pools_tenant_id_event_id_name";

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

    public Task<EventTicketCatalogVersion?> GetOrderCatalogAsync(
        Guid catalogId,
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken) =>
        CatalogDetailsQuery().FirstOrDefaultAsync(catalog => catalog.Id == catalogId
            && catalog.EventId == eventId
            && catalog.TenantId == tenantId, cancellationToken);

    public async Task<EventTicketCatalogVersion?> GetDraftCatalogForUpdateAsync(
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await LockCatalogRowAsync(eventId, tenantId, TicketCatalogStatusEnum.Draft, cancellationToken);
        return await ManagementGraph().FirstOrDefaultAsync(catalog => catalog.EventId == eventId
            && catalog.TenantId == tenantId
            && catalog.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Draft, cancellationToken);
    }

    public async Task<EventTicketCatalogVersion?> GetPublishedForUpdateAsync(
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await LockCatalogRowAsync(eventId, tenantId, TicketCatalogStatusEnum.Published, cancellationToken);
        return await ManagementGraph().FirstOrDefaultAsync(catalog => catalog.EventId == eventId
            && catalog.TenantId == tenantId
            && catalog.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Published, cancellationToken);
    }

    public Task<EventTicketCatalogVersion?> GetByEventVersionAndTenantAsync(Guid eventId, int versionNumber, Guid tenantId, CancellationToken cancellationToken) =>
        CatalogDetailsQuery().FirstOrDefaultAsync(catalog => catalog.EventId == eventId && catalog.VersionNumber == versionNumber && catalog.TenantId == tenantId, cancellationToken);

    public Task<EventTicketType?> GetTicketTypeByIdEventAndTenantAsync(Guid ticketTypeId, Guid eventId, Guid tenantId, CancellationToken cancellationToken) =>
        dbContext.EventTicketTypes.AsNoTracking().FirstOrDefaultAsync(ticketType =>
            ticketType.Id == ticketTypeId
            && ticketType.TenantId == tenantId
            && dbContext.EventTicketCatalogVersions.Any(catalog => catalog.Id == ticketType.CatalogId && catalog.EventId == eventId && catalog.TenantId == tenantId), cancellationToken);

    public Task<EventCapacityPool?> GetCapacityPoolByIdEventAndTenantAsync(Guid capacityPoolId, Guid eventId, Guid tenantId, CancellationToken cancellationToken) =>
        dbContext.EventCapacityPools
            .AsNoTracking()
            .Include(pool => pool.CapacityHoldPolicy)
            .FirstOrDefaultAsync(pool => pool.Id == capacityPoolId && pool.EventId == eventId && pool.TenantId == tenantId, cancellationToken);

    public async Task<EventCapacityPool?> GetActiveCapacityPoolForUpdateAsync(
        Guid capacityPoolId,
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await LockCapacityPoolRowAsync(capacityPoolId, eventId, tenantId, cancellationToken);
        return await dbContext.EventCapacityPools
            .Include(pool => pool.CapacityHoldPolicy)
            .FirstOrDefaultAsync(pool =>
                pool.Id == capacityPoolId
                && pool.EventId == eventId
                && pool.TenantId == tenantId, cancellationToken);
    }

    public Task<bool> HasLiveTicketTypeReferencesAsync(
        Guid capacityPoolId,
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken) =>
        dbContext.EventTicketTypes.AnyAsync(ticketType =>
            ticketType.CapacityPoolId == capacityPoolId
            && ticketType.TenantId == tenantId
            && !ticketType.IsDeleted
            && dbContext.EventTicketCatalogVersions.Any(catalog =>
                catalog.Id == ticketType.CatalogId
                && catalog.EventId == eventId
                && catalog.TenantId == tenantId
                && !catalog.IsDeleted
                && (catalog.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Draft
                    || catalog.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Published)),
            cancellationToken);

    public async Task AddAsync(EventTicketCatalogVersion catalog, CancellationToken cancellationToken)
    {
        await dbContext.EventTicketCatalogVersions.AddAsync(catalog, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EventTicketCatalogVersion catalog, CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveEntitlementsAsync(
        IEnumerable<TicketTypeEntitlement> entitlements,
        CancellationToken cancellationToken)
    {
        dbContext.TicketTypeEntitlements.RemoveRange(entitlements);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task AddCapacityPoolAsync(EventCapacityPool pool, CancellationToken cancellationToken)
    {
        await dbContext.EventCapacityPools.AddAsync(pool, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCapacityPoolAsync(EventCapacityPool pool, CancellationToken cancellationToken)
    {
        dbContext.EventCapacityPools.Update(pool);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw CreateConcurrencyConflictException(exception);
        }
        catch (DbUpdateException exception) when (IsRecognizedUniqueViolation(exception))
        {
            throw CreateConcurrencyConflictException(exception);
        }
    }

    private static ConcurrencyConflictException CreateConcurrencyConflictException(Exception exception) => new(
        ConcurrencyConflictException.ConcurrentUpdate,
        "Ticketing data was modified by another request. Reload and retry.",
        innerException: exception);

    private static bool IsRecognizedUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: PublishedCatalogUniqueIndexName or CatalogVersionNumberUniqueIndexName or CapacityPoolNameUniqueIndexName
        };

    private async Task LockCatalogRowAsync(
        Guid eventId,
        Guid tenantId,
        TicketCatalogStatusEnum status,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return;
        }

        EnsureActiveTransaction();
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT id
            FROM event_ticket_catalog_versions
            WHERE event_id = {eventId}
              AND tenant_id = {tenantId}
              AND ticket_catalog_status_id = {(int)status}
              AND is_deleted = false
            FOR UPDATE
            """, cancellationToken);
    }

    private async Task LockCapacityPoolRowAsync(
        Guid capacityPoolId,
        Guid eventId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return;
        }

        EnsureActiveTransaction();
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT id
            FROM event_capacity_pools
            WHERE id = {capacityPoolId}
              AND event_id = {eventId}
              AND tenant_id = {tenantId}
              AND is_deleted = false
            FOR UPDATE
            """, cancellationToken);
    }

    private void EnsureActiveTransaction()
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Ticketing row locks require an active unit-of-work transaction.");
        }
    }

    private IQueryable<EventTicketCatalogVersion> CatalogDetailsQuery() =>
        ManagementGraph().AsNoTracking();

    private IQueryable<EventTicketCatalogVersion> ManagementGraph() =>
        dbContext.EventTicketCatalogVersions
            .Include(catalog => catalog.TicketCatalogStatus)
            .Include(catalog => catalog.TicketTypes)
            .ThenInclude(ticketType => ticketType.TicketPricingMode)
            .Include(catalog => catalog.TicketTypes)
            .ThenInclude(ticketType => ticketType.ParticipantDataCollectionMode)
            .Include(catalog => catalog.TicketTypes)
            .ThenInclude(ticketType => ticketType.Entitlements)
            .ThenInclude(entitlement => entitlement.EntitlementScopeType)
            .Include(catalog => catalog.TicketTypes)
            .ThenInclude(ticketType => ticketType.Entitlements)
            .ThenInclude(entitlement => entitlement.EntitlementSelectionRule);
}
