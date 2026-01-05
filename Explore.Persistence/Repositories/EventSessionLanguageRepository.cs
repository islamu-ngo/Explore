using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EventSessionLanguageRepository : GenericRepository<EventSessionLanguage, int>, IEventSessionLanguageRepository
    {
        private readonly ExploreDbContext _dbContext;

        public EventSessionLanguageRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<EventSessionLanguage>> GetBySession(Guid eventSessionId)
        {
            return await _dbContext.EventSessionLanguages
                .Include(l => l.Language)
                .Where(l => l.EventSessionId == eventSessionId)
                .ToListAsync();
        }
    }
}
