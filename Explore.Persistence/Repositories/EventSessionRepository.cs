// ABOUTME: EventSession repository with the two-layer same-room overlap enforcement used by scheduling commands.
// ABOUTME: Layer A is a read for validators; Layer B wraps the re-check + save in a Serializable transaction.

using System.Data;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Specifications.EventSessions;
using Explore.Domain;
using Explore.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventSessionRepository : GenericRepository<EventSession, Guid>, IEventSessionRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventSessionRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventSession?> GetSessionWithDetails(Guid id)
    {
        return await _dbContext.EventSessions
            .AsNoTracking()
            .AsSplitQuery()
            .IncludeStandardDetails()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<EventSession>> GetSessionsByEvent(Guid eventId)
    {
        return await _dbContext.EventSessions
            .AsNoTracking()
            .AsSplitQuery()
            .IncludeStandardDetails()
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<List<EventSession>> GetSessionsByLocation(Guid locationId)
    {
        return await _dbContext.EventSessions
            .AsNoTracking()
            .Include(s => s.Event)
            .Include(s => s.RegistrationMode)
            .Include(s => s.IslamicAspect)
            .Where(s => s.LocationId == locationId)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<(List<EventSession> Items, int TotalCount)> GetSessionsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.EventSessions
            .AsNoTracking()
            .AsSplitQuery()
            .IncludeStandardDetails()
            .OrderByDescending(s => s.StartTime);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<EventSession> Items, int TotalCount)> GetSessionsWithDetailsPagedFiltered(
        int pageNumber, int pageSize, EventSessionQuerySpecification specification)
    {
        var query = _dbContext.EventSessions
            .AsNoTracking()
            .AsSplitQuery()
            .IncludeStandardDetails()
            .AsQueryable();

        query = ApplySessionProjectionFilters(query, specification);
        query = specification.Apply(query);

        if (!specification.HasSort)
        {
            query = query.OrderByDescending(s => s.StartTime);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<EventSession>> GetOverlappingSessionsInRoomAsync(
        Guid roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? excludeSessionId,
        CancellationToken cancellationToken)
    {
        return await BuildOverlapQuery(roomId, startUtc, endUtc, excludeSessionId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<EventSession> CreateWithRoomOverlapGuardAsync(
        EventSession session,
        CancellationToken cancellationToken)
    {
        if (session.RoomId is null)
        {
            return await Create(session);
        }

        await using var tx = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var conflicts = await BuildOverlapQuery(session.RoomId.Value, session.StartTime, session.EndTime, excludeSessionId: null)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (conflicts.Count > 0)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new RoomScheduleConflictException(session.RoomId.Value, conflicts);
        }

        await _dbContext.EventSessions.AddAsync(session, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return session;
    }

    public async Task UpdateWithRoomOverlapGuardAsync(
        EventSession session,
        CancellationToken cancellationToken)
    {
        if (session.RoomId is null)
        {
            await Update(session);
            return;
        }

        await using var tx = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var conflicts = await BuildOverlapQuery(session.RoomId.Value, session.StartTime, session.EndTime, session.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (conflicts.Count > 0)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new RoomScheduleConflictException(session.RoomId.Value, conflicts);
        }

        var entry = _dbContext.Entry(session);
        if (entry.State == EntityState.Detached)
        {
            _dbContext.EventSessions.Attach(session);
            entry.State = EntityState.Modified;
        }
        else
        {
            entry.State = EntityState.Modified;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private IQueryable<EventSession> BuildOverlapQuery(
        Guid roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? excludeSessionId)
    {
        var query = _dbContext.EventSessions
            .Where(s => s.RoomId == roomId)
            .Where(s => s.StartTime < endUtc && s.EndTime > startUtc);

        if (excludeSessionId.HasValue)
        {
            var excluded = excludeSessionId.Value;
            query = query.Where(s => s.Id != excluded);
        }

        return query;
    }

    private IQueryable<EventSession> ApplySessionProjectionFilters(
        IQueryable<EventSession> query, EventSessionQuerySpecification specification)
    {
        foreach (var projFilter in specification.ProjectionFilters)
        {
            query = projFilter.FilterType switch
            {
                EventSessionCustomPropertyProjectionFilterType.ExactMatch => ApplyExactMatchFilter(query, projFilter),
                EventSessionCustomPropertyProjectionFilterType.OptionMatch => ApplyOptionMatchFilter(query, projFilter),
                EventSessionCustomPropertyProjectionFilterType.OptionsMatchAny => ApplyOptionsMatchAnyFilter(query, projFilter),
                EventSessionCustomPropertyProjectionFilterType.TextSearch => ApplyTextSearchFilter(query, projFilter),
                EventSessionCustomPropertyProjectionFilterType.GlobalTextSearch => ApplyGlobalTextSearchFilter(query, projFilter),
                EventSessionCustomPropertyProjectionFilterType.Exists => ApplyExistsFilter(query, projFilter),
                EventSessionCustomPropertyProjectionFilterType.BooleanTrue => ApplyBooleanTrueFilter(query, projFilter),
                EventSessionCustomPropertyProjectionFilterType.NumberRange => ApplyNumberRangeFilter(query, projFilter),
                EventSessionCustomPropertyProjectionFilterType.DateRange => ApplyDateRangeFilter(query, projFilter),
                _ => query
            };
        }

        return query;
    }

    private IQueryable<EventSession> ApplyExactMatchFilter(IQueryable<EventSession> query, EventSessionCustomPropertyProjectionFilter filter)
    {
        var (ns, key, normalizedValue) = ((string, string, string))filter.Value;
        var visibleExposureLevels = CustomPropertyExposureScope.VisibleAtOrBelow(filter.ExposureCeiling);
        return query.Where(s => _dbContext.EventSessionCustomPropertyProjections.Any(p =>
            p.EventSessionId == s.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            visibleExposureLevels.Contains(p.ExposureLevel) &&
            p.IsFilterable &&
            p.NormalizedValue == normalizedValue));
    }

    private IQueryable<EventSession> ApplyOptionMatchFilter(IQueryable<EventSession> query, EventSessionCustomPropertyProjectionFilter filter)
    {
        var (ns, key, optionId) = ((string, string, Guid))filter.Value;
        var visibleExposureLevels = CustomPropertyExposureScope.VisibleAtOrBelow(filter.ExposureCeiling);
        return query.Where(s => _dbContext.EventSessionCustomPropertyProjections.Any(p =>
            p.EventSessionId == s.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            visibleExposureLevels.Contains(p.ExposureLevel) &&
            p.IsFilterable &&
            p.OptionId == optionId));
    }

    private IQueryable<EventSession> ApplyOptionsMatchAnyFilter(IQueryable<EventSession> query, EventSessionCustomPropertyProjectionFilter filter)
    {
        var (ns, key, optionIds) = ((string, string, List<Guid>))filter.Value;
        var visibleExposureLevels = CustomPropertyExposureScope.VisibleAtOrBelow(filter.ExposureCeiling);
        return query.Where(s => _dbContext.EventSessionCustomPropertyProjections.Any(p =>
            p.EventSessionId == s.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            visibleExposureLevels.Contains(p.ExposureLevel) &&
            p.IsFilterable &&
            p.OptionId != null &&
            optionIds.Contains(p.OptionId.Value)));
    }

    private IQueryable<EventSession> ApplyTextSearchFilter(IQueryable<EventSession> query, EventSessionCustomPropertyProjectionFilter filter)
    {
        var (ns, key, searchTerm) = ((string, string, string))filter.Value;
        var pattern = $"%{searchTerm}%";
        var visibleExposureLevels = CustomPropertyExposureScope.VisibleAtOrBelow(filter.ExposureCeiling);
        return query.Where(s => _dbContext.EventSessionCustomPropertyProjections.Any(p =>
            p.EventSessionId == s.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            visibleExposureLevels.Contains(p.ExposureLevel) &&
            p.IsSearchable &&
            p.NormalizedValue != null &&
            EF.Functions.ILike(p.NormalizedValue, pattern)));
    }

    private IQueryable<EventSession> ApplyGlobalTextSearchFilter(IQueryable<EventSession> query, EventSessionCustomPropertyProjectionFilter filter)
    {
        var searchTerm = (string)filter.Value;
        var pattern = $"%{searchTerm}%";
        var visibleExposureLevels = CustomPropertyExposureScope.VisibleAtOrBelow(filter.ExposureCeiling);
        return query.Where(s => _dbContext.EventSessionCustomPropertyProjections.Any(p =>
            p.EventSessionId == s.Id &&
            visibleExposureLevels.Contains(p.ExposureLevel) &&
            p.IsSearchable &&
            p.NormalizedValue != null &&
            EF.Functions.ILike(p.NormalizedValue, pattern)));
    }

    private IQueryable<EventSession> ApplyExistsFilter(IQueryable<EventSession> query, EventSessionCustomPropertyProjectionFilter filter)
    {
        var (ns, key) = ((string, string))filter.Value;
        var visibleExposureLevels = CustomPropertyExposureScope.VisibleAtOrBelow(filter.ExposureCeiling);
        return query.Where(s => _dbContext.EventSessionCustomPropertyProjections.Any(p =>
            p.EventSessionId == s.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            visibleExposureLevels.Contains(p.ExposureLevel) &&
            p.IsFilterable));
    }

    private IQueryable<EventSession> ApplyBooleanTrueFilter(IQueryable<EventSession> query, EventSessionCustomPropertyProjectionFilter filter)
    {
        var (ns, key) = ((string, string))filter.Value;
        var visibleExposureLevels = CustomPropertyExposureScope.VisibleAtOrBelow(filter.ExposureCeiling);
        return query.Where(s => _dbContext.EventSessionCustomPropertyProjections.Any(p =>
            p.EventSessionId == s.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            visibleExposureLevels.Contains(p.ExposureLevel) &&
            p.IsFilterable &&
            p.BooleanValue == true));
    }

    private IQueryable<EventSession> ApplyNumberRangeFilter(IQueryable<EventSession> query, EventSessionCustomPropertyProjectionFilter filter)
    {
        var (ns, key, min, max) = ((string, string, decimal?, decimal?))filter.Value;
        var visibleExposureLevels = CustomPropertyExposureScope.VisibleAtOrBelow(filter.ExposureCeiling);
        return query.Where(s => _dbContext.EventSessionCustomPropertyProjections.Any(p =>
            p.EventSessionId == s.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            visibleExposureLevels.Contains(p.ExposureLevel) &&
            p.IsFilterable &&
            p.NumberValue != null &&
            (!min.HasValue || p.NumberValue >= min.Value) &&
            (!max.HasValue || p.NumberValue <= max.Value)));
    }

    private IQueryable<EventSession> ApplyDateRangeFilter(IQueryable<EventSession> query, EventSessionCustomPropertyProjectionFilter filter)
    {
        var (ns, key, from, to) = ((string, string, DateTimeOffset?, DateTimeOffset?))filter.Value;
        var visibleExposureLevels = CustomPropertyExposureScope.VisibleAtOrBelow(filter.ExposureCeiling);
        return query.Where(s => _dbContext.EventSessionCustomPropertyProjections.Any(p =>
            p.EventSessionId == s.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            visibleExposureLevels.Contains(p.ExposureLevel) &&
            p.IsFilterable &&
            p.DateTimeValue != null &&
            (!from.HasValue || p.DateTimeValue >= from.Value) &&
            (!to.HasValue || p.DateTimeValue <= to.Value)));
    }
}
