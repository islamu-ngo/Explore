// ABOUTME: Implements tenant-visible reads over globally canonical AT Protocol records.
// ABOUTME: Uses presentation and outbound-ownership joins so canonical storage never bypasses tenant isolation.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Federation;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class AtprotoRecordRepository : IAtprotoRecordRepository
{
    private readonly ExploreDbContext _dbContext;

    public AtprotoRecordRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<AtprotoRecord>> GetAllAtprotoRecords() =>
        VisibleRecords().OrderByDescending(value => value.IndexedAt).ToListAsync();

    public Task<AtprotoRecord?> GetById(Guid id) =>
        VisibleRecords().SingleOrDefaultAsync(value => value.Id == id);

    public Task<AtprotoRecord?> GetAtprotoRecordByUri(string uri) =>
        VisibleRecords().SingleOrDefaultAsync(value => value.Uri == uri);

    public Task<List<AtprotoRecord>> GetAtprotoRecordsByDid(string did) =>
        VisibleRecords().Where(value => value.Did == did).ToListAsync();

    public Task<List<AtprotoRecord>> GetAtprotoRecordsByCollection(string collection) =>
        VisibleRecords().Where(value => value.Collection == collection).ToListAsync();

    public Task<bool> Exists(Guid id) =>
        VisibleRecords().AnyAsync(value => value.Id == id);

    public Task<AtprotoRecord?> GetOwnedRecordAsync(
        Guid tenantId,
        Guid userId,
        string sourceEntityType,
        Guid sourceEntityId,
        CancellationToken cancellationToken = default) =>
        _dbContext.AtprotoOutboundRecordOwnerships
            .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoTenantOperation)
            .AsNoTracking()
            .Where(value =>
                value.TenantId == tenantId &&
                value.UserId == userId &&
                value.SourceEntityType == sourceEntityType &&
                value.SourceEntityId == sourceEntityId)
            .Select(value => value.AtprotoRecord!)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<AtprotoOutboundRecordOwnership?> GetOwnedRecordForSourceAsync(
        Guid tenantId,
        string sourceEntityType,
        Guid sourceEntityId,
        CancellationToken cancellationToken = default) =>
        _dbContext.AtprotoOutboundRecordOwnerships
            .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoTenantOperation)
            .AsNoTracking()
            .Include(value => value.AtprotoRecord)
            .SingleOrDefaultAsync(value =>
                value.TenantId == tenantId
                && value.SourceEntityType == sourceEntityType
                && value.SourceEntityId == sourceEntityId,
                cancellationToken);

    public Task<AtprotoOutboundRecordOwnership?> GetOwnedRsvpForUserEventAsync(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        string sourceEntityType,
        string collection,
        CancellationToken cancellationToken = default) =>
        _dbContext.AtprotoOutboundRecordOwnerships
            .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoTenantOperation)
            .AsNoTracking()
            .Include(value => value.AtprotoRecord)
            .SingleOrDefaultAsync(value =>
                value.TenantId == tenantId
                && value.UserId == userId
                && value.SourceEntityType == sourceEntityType
                && value.AtprotoRecord != null
                && value.AtprotoRecord.Collection == collection
                && _dbContext.EventRegistrationIntents
                    .IgnoreAllFilters(TenantFilterBypassReasons.AtprotoTenantOperation)
                    .Any(intent =>
                        intent.Id == value.SourceEntityId
                        && intent.TenantId == tenantId
                        && intent.UserId == userId
                        && intent.EventId == eventId),
                cancellationToken);

    private IQueryable<AtprotoRecord> VisibleRecords() =>
        _dbContext.AtprotoRecords
            .AsNoTracking()
            .Where(record =>
                record.TombstonedAt == null &&
                (_dbContext.AtprotoRecordTenantPresentations.Any(presentation =>
                    presentation.AtprotoRecordId == record.Id && presentation.IsVisible) ||
                 _dbContext.AtprotoOutboundRecordOwnerships.Any(ownership =>
                    ownership.AtprotoRecordId == record.Id)));
}
