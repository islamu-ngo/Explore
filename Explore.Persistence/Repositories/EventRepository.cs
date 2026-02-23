using Explore.Application.Contracts.Persistence;
using Explore.Application.Specifications.Events;
using Explore.Domain;
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
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.PrimaryLanguage)
            .Include(e => e.TechAspect)
            .ToListAsync();
    }

    public async Task<Event?> GetEventWithDetails(Guid id)
    {
        return await _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ProfilePicture)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.AtprotoRecord)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.PrimaryLanguage)
            .Include(e => e.TechAspect)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Event>> GetMyEventsWithDetails(string userId)
    {
        Guid userGuid;
        bool isGuid = Guid.TryParse(userId, out userGuid);

        var query = _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.PrimaryLanguage)
            .Include(e => e.TechAspect)
            .AsQueryable();

        if (isGuid)
        {
            query = query.Where(e =>
                _dbContext.Users.AsNoTracking().Any(u => u.Id == userGuid && u.ActorId == e.ActorId) ||
                _dbContext.OrganizationMembers.AsNoTracking().Any(om =>
                    om.UserId == userGuid &&
                    _dbContext.Organizations.AsNoTracking().Any(o => o.Id == om.OrganizationId && o.ActorId == e.ActorId)));
        }

        return await query.ToListAsync();
    }

    public async Task<(List<Event> Items, int TotalCount)> GetEventsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.PrimaryLanguage)
            .Include(e => e.TechAspect)
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
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.PrimaryLanguage)
            .Include(e => e.TechAspect)
            .AsQueryable();

        // Apply subquery filters (require DbContext access for junction tables)
        query = ApplySubqueryFilters(query, specification);

        // Apply direct filters and sorting via specification
        query = (IQueryable<Event>)specification.Apply(query);

        // If no sort was specified by the specification, default to date descending
        if (!specification.HasSort)
        {
            query = query.OrderByDescending(e => e.FirstSessionDate);
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

                EventSubqueryFilterType.RegistrationMode => query.Where(e =>
                    _dbContext.EventSessions.Any(es =>
                        es.EventId == e.Id && es.RegistrationModeId == (int)subFilter.Value)),

                EventSubqueryFilterType.Language => query.Where(e =>
                    _dbContext.EventSessions.Any(es =>
                        es.EventId == e.Id &&
                        _dbContext.EventSessionLanguages.Any(esl =>
                            esl.EventSessionId == es.Id && esl.LanguageId == (int)subFilter.Value))),

                // JSONB containment: MetadataJson @> '{"key": "value"}'
                EventSubqueryFilterType.JsonContains => query.Where(e =>
                    e.MetadataJson != null &&
                    EF.Functions.JsonContains(e.MetadataJson, (string)subFilter.Value)),

                // JSONB key existence: MetadataJson ? 'key'
                EventSubqueryFilterType.JsonKeyExists => query.Where(e =>
                    e.MetadataJson != null &&
                    EF.Functions.JsonExists(e.MetadataJson, (string)subFilter.Value)),

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
                query = query.Where(e =>
                    !tagIds.All(tid =>
                        _dbContext.EventTags.Any(et => et.EventId == e.Id && et.TagId == tid)));
            }
        }

        return query;
    }

    public async Task<(List<Event> Items, int TotalCount)> GetMyEventsWithDetailsPaged(string userId, int pageNumber, int pageSize)
    {
        Guid userGuid;
        bool isGuid = Guid.TryParse(userId, out userGuid);

        var query = _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.PrimaryLanguage)
            .Include(e => e.TechAspect)
            .AsQueryable();

        if (isGuid)
        {
            query = query.Where(e =>
                _dbContext.Users.AsNoTracking().Any(u => u.Id == userGuid && u.ActorId == e.ActorId) ||
                _dbContext.OrganizationMembers.AsNoTracking().Any(om =>
                    om.UserId == userGuid &&
                    _dbContext.Organizations.AsNoTracking().Any(o => o.Id == om.OrganizationId && o.ActorId == e.ActorId)));
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
