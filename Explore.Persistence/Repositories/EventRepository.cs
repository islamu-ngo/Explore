using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
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
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
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
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
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
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
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
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
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

    public async Task<(List<Event> Items, int TotalCount)> GetMyEventsWithDetailsPaged(string userId, int pageNumber, int pageSize)
    {
        Guid userGuid;
        bool isGuid = Guid.TryParse(userId, out userGuid);

        var query = _dbContext.Events
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
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
