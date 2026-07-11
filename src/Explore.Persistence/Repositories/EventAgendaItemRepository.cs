// ABOUTME: EF implementation of IEventAgendaItemRepository - delegates CRUD to GenericRepository and adds event-scoped queries.
// ABOUTME: Reads are AsNoTracking so query handler use does not accidentally attach entities.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventAgendaItemRepository : GenericRepository<EventAgendaItem, Guid>, IEventAgendaItemRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventAgendaItemRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<EventAgendaItem>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _dbContext.EventAgendaItems
            .AsNoTracking()
            .Where(a => a.EventId == eventId)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }
}
