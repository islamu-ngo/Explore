using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Specifications.Events;
using Explore.Domain;
using Explore.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
{
    private static readonly Func<ExploreDbContext, Guid, Task<Event?>> GetByIdCompiled =
        EF.CompileAsyncQuery((ExploreDbContext ctx, Guid id) =>
            ctx.Events
                .AsNoTracking()
                .FirstOrDefault(e => e.Id == id));

    private readonly ExploreDbContext _dbContext;

    public EventRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new async Task<Event?> GetById(Guid id)
    {
        return await GetByIdCompiled(_dbContext, id);
    }

    public async Task<List<Event>> GetEventsWithDetails()
    {
        return await _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .IncludeStandardDetails()
            .ToListAsync();
    }

    public async Task<Event?> GetEventWithDetails(Guid id)
    {
        return await _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .IncludeStandardDetails()
            .Include(e => e.AtprotoRecord)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Event>> GetMyEventsWithDetails(string userId)
    {
        Guid userGuid;
        bool isGuid = Guid.TryParse(userId, out userGuid);

        var query = _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .IncludeStandardDetails()
            .AsQueryable();

        if (isGuid)
        {
            var userActorIds = _dbContext.Users
                .Where(u => u.Id == userGuid)
                .Select(u => u.ActorId);

            var orgActorIds = _dbContext.OrganizationMembers
                .Where(om => om.UserId == userGuid)
                .SelectMany(om => _dbContext.Organizations
                    .Where(o => o.Id == om.OrganizationId)
                    .Select(o => o.ActorId));

            var allActorIds = userActorIds.Union(orgActorIds);

            query = query.Where(e => allActorIds.Contains(e.ActorId));
        }

        return await query.ToListAsync();
    }

    public async Task<(List<Event> Items, int TotalCount)> GetEventsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .IncludeStandardDetails()
            .OrderByDescending(e => e.FirstSessionDate);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<(List<Event> Items, int TotalCount)> GetEventsWithDetailsPaged(
        int pageNumber, int pageSize, EventQuerySpecification specification)
    {
        var query = _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .IncludeStandardDetails()
            .AsQueryable();

        query = ApplySubqueryFilters(query, specification);
        query = ApplyProjectionFilters(query, specification);

        // Apply direct filters and sorting via specification
        var now = DateTimeOffset.UtcNow;
        query = specification.Apply(query, now);

        // If no sort was specified by the specification, default to date descending
        if (!specification.HasSort)
        {
            query = query.OrderByDescending(e => e.FirstSessionStartUtc);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Applies subquery filters that require access to junction table DbSets,
    /// and JSONB filters that use PostgreSQL-specific operators.
    /// These filters use correlated subqueries (EXISTS pattern) for efficient SQL translation.
    /// </summary>
    private IQueryable<Event> ApplySubqueryFilters(
        IQueryable<Event> query, EventQuerySpecification specification)
    {
        foreach (var subFilter in specification.SubqueryFilters)
        {
            query = subFilter.FilterType switch
            {
                EventSubqueryFilterType.Category => query.Where(e =>
                    _dbContext.EventCategories.Any(ec =>
                        ec.EventId == e.Id && ec.CategoryId == (Guid)subFilter.Value)),

                EventSubqueryFilterType.CategoriesIncludedAny => query.Where(e =>
                    _dbContext.EventCategories.Any(ec =>
                        ec.EventId == e.Id && ((List<Guid>)subFilter.Value).Contains(ec.CategoryId))),

                EventSubqueryFilterType.CategoriesExcludedAny => query.Where(e =>
                    !_dbContext.EventCategories.Any(ec =>
                        ec.EventId == e.Id && ((List<Guid>)subFilter.Value).Contains(ec.CategoryId))),

                // CategoriesIncludedAll and CategoriesExcludedAll are handled below (require loops)
                EventSubqueryFilterType.CategoriesIncludedAll => query,
                EventSubqueryFilterType.CategoriesExcludedAll => query,

                EventSubqueryFilterType.TagsIncludedAny => query.Where(e =>
                    _dbContext.EventTags.Any(et =>
                        et.EventId == e.Id && ((List<Guid>)subFilter.Value).Contains(et.TagId))),

                EventSubqueryFilterType.TagsExcludedAny => query.Where(e =>
                    !_dbContext.EventTags.Any(et =>
                        et.EventId == e.Id && ((List<Guid>)subFilter.Value).Contains(et.TagId))),

                // TagsIncludedAll and TagsExcludedAll are handled below (require loops)
                EventSubqueryFilterType.TagsIncludedAll => query,
                EventSubqueryFilterType.TagsExcludedAll => query,

                EventSubqueryFilterType.Location => query.Where(e =>
                    _dbContext.EventSessions.Any(es =>
                        es.EventId == e.Id && es.LocationId == (Guid)subFilter.Value)),

                EventSubqueryFilterType.Locations => query.Where(e =>
                    _dbContext.EventSessions.Any(es =>
                        es.EventId == e.Id && es.LocationId != null &&
                        ((List<Guid>)subFilter.Value).Contains(es.LocationId.Value))),

                EventSubqueryFilterType.RegistrationMode => query.Where(e =>
                    _dbContext.EventSessions.Any(es =>
                        es.EventId == e.Id && es.RegistrationModeId == (int)subFilter.Value)),

                EventSubqueryFilterType.RegistrationModes => query.Where(e =>
                    _dbContext.EventSessions.Any(es =>
                        es.EventId == e.Id && es.RegistrationModeId != null &&
                        ((List<int>)subFilter.Value).Contains(es.RegistrationModeId.Value))),

                EventSubqueryFilterType.Language => query.Where(e =>
                    _dbContext.EventSessions.Any(es =>
                        es.EventId == e.Id &&
                        _dbContext.EventSessionLanguages.Any(esl =>
                            esl.EventSessionId == es.Id && esl.LanguageId == (int)subFilter.Value))),

                EventSubqueryFilterType.Languages => query.Where(e =>
                    _dbContext.EventSessions.Any(es =>
                        es.EventId == e.Id &&
                        _dbContext.EventSessionLanguages.Any(esl =>
                            esl.EventSessionId == es.Id &&
                            ((List<int>)subFilter.Value).Contains(esl.LanguageId)))),

                EventSubqueryFilterType.FutureOnly => query.Where(e =>
                    e.LastSessionStartUtc == null || e.LastSessionStartUtc > (DateTimeOffset)subFilter.Value),

                EventSubqueryFilterType.TemporalView => ApplyTemporalFilter(query, subFilter),

                _ => query
            };

            // Loop-based filters that can't be expressed in a switch expression
            if (subFilter.FilterType == EventSubqueryFilterType.TagsIncludedAll)
            {
                var tagIds = (List<Guid>)subFilter.Value;
                foreach (var tagId in tagIds)
                {
                    query = query.Where(e =>
                        _dbContext.EventTags.Any(et => et.EventId == e.Id && et.TagId == tagId));
                }
            }
            else if (subFilter.FilterType == EventSubqueryFilterType.TagsExcludedAll)
            {
                var tagIds = (List<Guid>)subFilter.Value;
                // Logically: NOT (Exists(T1) AND Exists(T2) AND ...)
                // Is equivalent to: NOT Exists(T1) OR NOT Exists(T2) OR ...
                // But for EF compatibility, we use a single Where with a manual expression if needed,
                // or just stay with the existing one if we can fix it.
                // The issue is tid is a variable from outside.

                // Let's try a more robust pattern:
                query = query.Where(e => _dbContext.EventTags
                    .Where(et => et.EventId == e.Id && tagIds.Contains(et.TagId))
                    .Select(et => et.TagId)
                    .Distinct()
                    .Count() < tagIds.Count);
            }
            else if (subFilter.FilterType == EventSubqueryFilterType.CategoriesIncludedAll)
            {
                var categoryIds = (List<Guid>)subFilter.Value;
                foreach (var categoryId in categoryIds)
                {
                    query = query.Where(e =>
                        _dbContext.EventCategories.Any(ec => ec.EventId == e.Id && ec.CategoryId == categoryId));
                }
            }
            else if (subFilter.FilterType == EventSubqueryFilterType.CategoriesExcludedAll)
            {
                var categoryIds = (List<Guid>)subFilter.Value;
                query = query.Where(e => _dbContext.EventCategories
                    .Where(ec => ec.EventId == e.Id && categoryIds.Contains(ec.CategoryId))
                    .Select(ec => ec.CategoryId)
                    .Distinct()
                    .Count() < categoryIds.Count);
            }
        }

        return query;
    }

    private IQueryable<Event> ApplyProjectionFilters(
        IQueryable<Event> query, EventQuerySpecification specification)
    {
        foreach (var projFilter in specification.ProjectionFilters)
        {
            query = projFilter.FilterType switch
            {
                EventCustomPropertyProjectionFilterType.ExactMatch => ApplyExactMatchFilter(query, projFilter),
                EventCustomPropertyProjectionFilterType.OptionMatch => ApplyOptionMatchFilter(query, projFilter),
                EventCustomPropertyProjectionFilterType.OptionsMatchAny => ApplyOptionsMatchAnyFilter(query, projFilter),
                EventCustomPropertyProjectionFilterType.TextSearch => ApplyTextSearchFilter(query, projFilter),
                EventCustomPropertyProjectionFilterType.GlobalTextSearch => ApplyGlobalTextSearchFilter(query, projFilter),
                EventCustomPropertyProjectionFilterType.Exists => ApplyExistsFilter(query, projFilter),
                EventCustomPropertyProjectionFilterType.BooleanTrue => ApplyBooleanTrueFilter(query, projFilter),
                EventCustomPropertyProjectionFilterType.NumberRange => ApplyNumberRangeFilter(query, projFilter),
                EventCustomPropertyProjectionFilterType.DateRange => ApplyDateRangeFilter(query, projFilter),
                _ => query
            };
        }

        return query;
    }

    private IQueryable<Event> ApplyExactMatchFilter(IQueryable<Event> query, EventCustomPropertyProjectionFilter filter)
    {
        var (ns, key, normalizedValue) = ((string, string, string))filter.Value;
        return query.Where(e => _dbContext.EventCustomPropertyProjections.Any(p =>
            p.EventId == e.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            p.IsFilterable &&
            p.NormalizedValue == normalizedValue));
    }

    private IQueryable<Event> ApplyOptionMatchFilter(IQueryable<Event> query, EventCustomPropertyProjectionFilter filter)
    {
        var (ns, key, optionId) = ((string, string, Guid))filter.Value;
        return query.Where(e => _dbContext.EventCustomPropertyProjections.Any(p =>
            p.EventId == e.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            p.IsFilterable &&
            p.OptionId == optionId));
    }

    private IQueryable<Event> ApplyOptionsMatchAnyFilter(IQueryable<Event> query, EventCustomPropertyProjectionFilter filter)
    {
        var (ns, key, optionIds) = ((string, string, List<Guid>))filter.Value;
        return query.Where(e => _dbContext.EventCustomPropertyProjections.Any(p =>
            p.EventId == e.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            p.IsFilterable &&
            p.OptionId != null &&
            optionIds.Contains(p.OptionId.Value)));
    }

    private IQueryable<Event> ApplyTextSearchFilter(IQueryable<Event> query, EventCustomPropertyProjectionFilter filter)
    {
        var (ns, key, searchTerm) = ((string, string, string))filter.Value;
        var pattern = $"%{searchTerm}%";
        return query.Where(e => _dbContext.EventCustomPropertyProjections.Any(p =>
            p.EventId == e.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            p.IsSearchable &&
            p.NormalizedValue != null &&
            EF.Functions.ILike(p.NormalizedValue, pattern)));
    }

    private IQueryable<Event> ApplyGlobalTextSearchFilter(IQueryable<Event> query, EventCustomPropertyProjectionFilter filter)
    {
        var searchTerm = (string)filter.Value;
        var pattern = $"%{searchTerm}%";
        return query.Where(e => _dbContext.EventCustomPropertyProjections.Any(p =>
            p.EventId == e.Id &&
            p.IsSearchable &&
            p.NormalizedValue != null &&
            EF.Functions.ILike(p.NormalizedValue, pattern)));
    }

    private IQueryable<Event> ApplyExistsFilter(IQueryable<Event> query, EventCustomPropertyProjectionFilter filter)
    {
        var (ns, key) = ((string, string))filter.Value;
        return query.Where(e => _dbContext.EventCustomPropertyProjections.Any(p =>
            p.EventId == e.Id &&
            p.Namespace == ns &&
            p.Key == key));
    }

    private IQueryable<Event> ApplyBooleanTrueFilter(IQueryable<Event> query, EventCustomPropertyProjectionFilter filter)
    {
        var (ns, key) = ((string, string))filter.Value;
        return query.Where(e => _dbContext.EventCustomPropertyProjections.Any(p =>
            p.EventId == e.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            p.IsFilterable &&
            p.BooleanValue == true));
    }

    private IQueryable<Event> ApplyNumberRangeFilter(IQueryable<Event> query, EventCustomPropertyProjectionFilter filter)
    {
        var (ns, key, min, max) = ((string, string, decimal?, decimal?))filter.Value;
        return query.Where(e => _dbContext.EventCustomPropertyProjections.Any(p =>
            p.EventId == e.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            p.IsFilterable &&
            p.NumberValue != null &&
            (!min.HasValue || p.NumberValue >= min.Value) &&
            (!max.HasValue || p.NumberValue <= max.Value)));
    }

    private IQueryable<Event> ApplyDateRangeFilter(IQueryable<Event> query, EventCustomPropertyProjectionFilter filter)
    {
        var (ns, key, from, to) = ((string, string, DateTimeOffset?, DateTimeOffset?))filter.Value;
        return query.Where(e => _dbContext.EventCustomPropertyProjections.Any(p =>
            p.EventId == e.Id &&
            p.Namespace == ns &&
            p.Key == key &&
            p.IsFilterable &&
            p.DateTimeValue != null &&
            (!from.HasValue || p.DateTimeValue >= from.Value) &&
            (!to.HasValue || p.DateTimeValue <= to.Value)));
    }

    private static IQueryable<Event> ApplyTemporalFilter(IQueryable<Event> query, EventSubqueryFilter filter)
    {
        var (view, now) = ((TemporalView, DateTimeOffset))filter.Value;

        return view switch
        {
            TemporalView.Upcoming => query.Where(e => e.FirstSessionStartUtc != null && e.FirstSessionStartUtc > now),
            TemporalView.Ongoing => query.Where(e => e.FirstSessionStartUtc != null && e.FirstSessionStartUtc <= now && e.LastSessionStartUtc != null && e.LastSessionStartUtc > now),
            TemporalView.Past => query.Where(e => e.LastSessionStartUtc != null && e.LastSessionStartUtc <= now),
            TemporalView.UpcomingAndOngoing => query.Where(e => e.LastSessionStartUtc == null || e.LastSessionStartUtc > now),
            TemporalView.All => query,
            _ => query
        };
    }

    public async Task<(List<Event> Items, int TotalCount)> GetMyEventsWithDetailsPaged(string userId, int pageNumber, int pageSize)
    {
        Guid userGuid;
        bool isGuid = Guid.TryParse(userId, out userGuid);

        var query = _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .IncludeStandardDetails()
            .AsQueryable();

        if (isGuid)
        {
            var userActorIds = _dbContext.Users
                .Where(u => u.Id == userGuid)
                .Select(u => u.ActorId);

            var orgActorIds = _dbContext.OrganizationMembers
                .Where(om => om.UserId == userGuid)
                .SelectMany(om => _dbContext.Organizations
                    .Where(o => o.Id == om.OrganizationId)
                    .Select(o => o.ActorId));

            var allActorIds = userActorIds.Union(orgActorIds);

            query = query.Where(e => allActorIds.Contains(e.ActorId));
        }

        var orderedQuery = query.OrderByDescending(e => e.FirstSessionDate);
        var totalCount = await orderedQuery.CountAsync();
        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
