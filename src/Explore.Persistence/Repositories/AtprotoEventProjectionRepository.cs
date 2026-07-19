// ABOUTME: Queries typed ATProto event projections through tenant presentation and local-echo boundaries.
// ABOUTME: Applies public filters, deterministic ordering, and bounded top-window pagination in PostgreSQL.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Federation;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class AtprotoEventProjectionRepository(ExploreDbContext dbContext)
    : IAtprotoEventProjectionRepository
{
    public async Task<(IReadOnlyList<AtprotoEventProjection> Items, int TotalCount)> GetPublicWindowAsync(
        AtprotoEventProjectionQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<AtprotoEventProjection> filtered = VisibleQuery();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm.Trim();
            filtered = filtered.Where(value =>
                EF.Functions.ILike(value.Name, $"%{term}%")
                || (value.Description != null && EF.Functions.ILike(value.Description, $"%{term}%")));
        }

        if (query.DateFrom.HasValue)
        {
            DateTimeOffset from = new(query.DateFrom.Value, TimeOnly.MinValue, TimeSpan.Zero);
            filtered = filtered.Where(value => value.StartsAt != null && value.StartsAt >= from);
        }

        if (query.DateTo.HasValue)
        {
            DateTimeOffset toExclusive = new(query.DateTo.Value.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);
            filtered = filtered.Where(value => value.StartsAt != null && value.StartsAt < toExclusive);
        }

        if (query.Modes is { Count: > 0 })
        {
            string[] modes = query.Modes.ToArray();
            filtered = filtered.Where(value => value.Mode != null && modes.Contains(value.Mode));
        }

        filtered = query.TemporalFilter switch
        {
            AtprotoEventTemporalFilter.CurrentOrUpcoming => filtered.Where(value =>
                value.StartsAt != null && (value.EndsAt ?? value.StartsAt) > query.Now),
            AtprotoEventTemporalFilter.Upcoming => filtered.Where(value => value.StartsAt > query.Now),
            AtprotoEventTemporalFilter.Ongoing => filtered.Where(value =>
                value.StartsAt != null && value.StartsAt <= query.Now && value.EndsAt > query.Now),
            AtprotoEventTemporalFilter.Past => filtered.Where(value =>
                value.StartsAt != null && (value.EndsAt ?? value.StartsAt) <= query.Now),
            _ => filtered
        };

        int totalCount = await filtered.CountAsync(cancellationToken);
        IReadOnlyList<AtprotoEventProjection> items = await Order(filtered, query.Sort, query.SortDescending)
            .Take(query.Take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<IReadOnlyList<AtprotoEventProjection>> GetVisibleByRecordIdsAsync(
        IReadOnlyCollection<Guid> atprotoRecordIds,
        CancellationToken cancellationToken)
    {
        if (atprotoRecordIds.Count == 0)
        {
            return [];
        }

        return await VisibleQuery(includeLocalEchoes: true)
            .Where(value => atprotoRecordIds.Contains(value.AtprotoRecordId))
            .OrderBy(value => value.AtprotoRecordId)
            .ToListAsync(cancellationToken);
    }

    public Task<AtprotoEventProjection?> GetVisibleByRecordIdAsync(
        Guid atprotoRecordId,
        CancellationToken cancellationToken) =>
        VisibleQuery(includeLocalEchoes: true)
            .SingleOrDefaultAsync(value => value.AtprotoRecordId == atprotoRecordId, cancellationToken);

    private IQueryable<AtprotoEventProjection> VisibleQuery(bool includeLocalEchoes = false)
    {
        IQueryable<AtprotoEventProjection> query = dbContext.AtprotoEventProjections
            .AsNoTracking()
            .Where(projection => dbContext.AtprotoRecordTenantPresentations.Any(presentation =>
                presentation.AtprotoRecordId == projection.AtprotoRecordId && presentation.IsVisible))
            .Where(projection => dbContext.AtprotoRecords.Any(record =>
                record.Id == projection.AtprotoRecordId && record.TombstonedAt == null));

        return includeLocalEchoes
            ? query
            : query.Where(projection => !dbContext.Events.Any(@event =>
                @event.AtprotoRecordId == projection.AtprotoRecordId));
    }

    private static IOrderedQueryable<AtprotoEventProjection> Order(
        IQueryable<AtprotoEventProjection> query,
        AtprotoEventDiscoverySort sort,
        bool descending) => (sort, descending) switch
        {
            (AtprotoEventDiscoverySort.Title, false) => query
                .OrderBy(value => value.Name)
                .ThenBy(value => value.AtprotoRecordId),
            (AtprotoEventDiscoverySort.Title, true) => query
                .OrderByDescending(value => value.Name)
                .ThenBy(value => value.AtprotoRecordId),
            (AtprotoEventDiscoverySort.CreatedAt, false) => query
                .OrderBy(value => value.CreatedAt)
                .ThenBy(value => value.AtprotoRecordId),
            (AtprotoEventDiscoverySort.CreatedAt, true) => query
                .OrderByDescending(value => value.CreatedAt)
                .ThenBy(value => value.AtprotoRecordId),
            (AtprotoEventDiscoverySort.Views, _) => query.OrderBy(value => value.AtprotoRecordId),
            (_, false) => query
                .OrderBy(value => value.StartsAt)
                .ThenBy(value => value.AtprotoRecordId),
            _ => query
                .OrderByDescending(value => value.StartsAt)
                .ThenBy(value => value.AtprotoRecordId)
        };
}
