using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EventSessionAgendaItemRepository : GenericRepository<EventSessionAgendaItem, Guid>, IEventSessionAgendaItemRepository
    {
        private readonly ExploreDbContext _dbContext;

        public EventSessionAgendaItemRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<EventSessionAgendaItem>> GetBySession(Guid eventSessionId)
        {
            return await _dbContext.EventSessionAgendaItems
                .Include(a => a.Location)
                .Where(a => a.EventSessionId == eventSessionId)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }
    }
}
