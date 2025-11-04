using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
    {
        private readonly ExploreDbContext _dbContext;
        public EventRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Event>> GetEventsWithDetails()
        {
            var events = await _dbContext.Events
                .Include(e => e.EventType)
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .ToListAsync();
            return events;
        }

        public async Task<Event> GetEventWithDetails(Guid id)
        {
            var eventEntity = await _dbContext.Events
                .Include(e => e.EventType)
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .FirstOrDefaultAsync(e => e.Id == id);
            return eventEntity;
        }
    }
}
