using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EventTagsRepository : GenericRepository<EventTags, Guid>, IEventTagsRepository
    {
        private readonly ExploreDbContext _dbContext;

        public EventTagsRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Event>> GetEventsByTag(Guid tagId)
        {
            return await _dbContext.EventTags
                .Include(et => et.Event)
                .Where(et => et.TagId == tagId)
                .Select(et => et.Event)
                .ToListAsync();
        }

        public async Task<List<Tag>> GetTagsByEvent(Guid eventId)
        {
            return await _dbContext.EventTags
                .Include(et => et.Tag)
                .Where(et => et.EventId == eventId)
                .Select(et => et.Tag)
                .ToListAsync();
        }

        public async Task<bool> Exists(Guid eventId, Guid tagId)
        {
            return await _dbContext.EventTags
                .AnyAsync(et => et.EventId == eventId && et.TagId == tagId);
        }
    }
}
