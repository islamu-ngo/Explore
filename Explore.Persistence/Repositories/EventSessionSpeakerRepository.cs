using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EventSessionSpeakerRepository : GenericRepository<EventSessionSpeaker, int>, IEventSessionSpeakerRepository
    {
        private readonly ExploreDbContext _dbContext;

        public EventSessionSpeakerRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<EventSessionSpeaker>> GetBySession(Guid eventSessionId)
        {
            return await _dbContext.EventSessionSpeakers
                .Include(s => s.Actor)
                .Where(s => s.EventSessionId == eventSessionId)
                .ToListAsync();
        }

        public async Task<List<EventSessionSpeaker>> GetByActor(Guid actorId)
        {
            return await _dbContext.EventSessionSpeakers
                .Include(s => s.EventSession)
                .Where(s => s.ActorId == actorId)
                .ToListAsync();
        }
    }
}
