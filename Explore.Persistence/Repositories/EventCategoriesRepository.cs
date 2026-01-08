using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EventCategoriesRepository : GenericRepository<EventCategories, Guid>, IEventCategoriesRepository
    {
        private readonly ExploreDbContext _dbContext;

        public EventCategoriesRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Event>> GetEventsByCategory(Guid categoryId)
        {
            return await _dbContext.EventCategories
                .Include(ec => ec.Event)
                .Where(ec => ec.CategoryId == categoryId)
                .Select(ec => ec.Event)
                .ToListAsync();
        }

        public async Task<List<Category>> GetCategoriesByEvent(Guid eventId)
        {
            return await _dbContext.EventCategories
                .Include(ec => ec.Category)
                .Where(ec => ec.EventId == eventId)
                .Select(ec => ec.Category)
                .ToListAsync();
        }

        public async Task<bool> Exists(Guid eventId, Guid categoryId)
        {
            return await _dbContext.EventCategories
                .AnyAsync(ec => ec.EventId == eventId && ec.CategoryId == categoryId);
        }
    }
}
