using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

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
            .AsNoTracking()
            .Include(a => a.Location)
                .ThenInclude(l => l!.Pii)
            .Where(a => a.EventSessionId == eventSessionId)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<(List<EventSessionAgendaItem> Items, int TotalCount)> GetAgendaItemsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.EventSessionAgendaItems
            .AsNoTracking()
            .Include(a => a.EventSession)
                .ThenInclude(s => s.Event)
            .Include(a => a.Location)
                .ThenInclude(l => l!.Pii)
            .OrderByDescending(a => a.StartTime);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
