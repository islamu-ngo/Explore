using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventCategoriesRepository : GenericRepository<EventCategories, Guid>, IEventCategoriesRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventCategoriesRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Event>> GetEventsByCategory(Guid categoryId)
    {
        var eventIds = await _dbContext.EventCategories
            .AsNoTracking()
            .Where(ec => ec.CategoryId == categoryId)
            .Select(ec => ec.EventId)
            .ToListAsync();

        return await _dbContext.Events
            .AsNoTrackingWithIdentityResolution()
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
            .Where(e => eventIds.Contains(e.Id))
            .ToListAsync();
    }

    public async Task<List<Category>> GetCategoriesByEvent(Guid eventId)
    {
        return await _dbContext.EventCategories
            .AsNoTracking()
            .Include(ec => ec.Category)
            .Where(ec => ec.EventId == eventId)
            .Select(ec => ec.Category)
            .ToListAsync();
    }

    public async Task<bool> Exists(Guid eventId, Guid categoryId)
    {
        return await _dbContext.EventCategories
            .AsNoTracking()
            .AnyAsync(ec => ec.EventId == eventId && ec.CategoryId == categoryId);
    }
}
