using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

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
            .AsNoTracking()
            .Include(l => l.Language)
            .Where(l => l.EventSessionId == eventSessionId)
            .ToListAsync();
    }

    public async Task<(List<EventSessionLanguage> Items, int TotalCount)> GetLanguagesWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.EventSessionLanguages
            .AsNoTracking()
            .Include(l => l.Language)
            .Include(l => l.EventSession)
                .ThenInclude(s => s.Event)
            .OrderByDescending(l => l.Id);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
