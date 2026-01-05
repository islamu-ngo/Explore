using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
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
                .Include(s => s.Event)
                .Include(s => s.Location)
                .Include(s => s.RegistrationMode)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<EventSession>> GetSessionsByEvent(Guid eventId)
        {
            return await _dbContext.EventSessions
                .Include(s => s.Location)
                .Include(s => s.RegistrationMode)
                .Where(s => s.EventId == eventId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<List<EventSession>> GetSessionsByLocation(Guid locationId)
        {
            return await _dbContext.EventSessions
                .Include(s => s.Event)
                .Where(s => s.LocationId == locationId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }
    }
}
