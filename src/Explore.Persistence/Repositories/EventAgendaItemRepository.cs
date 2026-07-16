// ABOUTME: EF implementation of IEventAgendaItemRepository - delegates CRUD to GenericRepository and adds event-scoped queries.
// ABOUTME: Reads are AsNoTracking so query handler use does not accidentally attach entities.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.Extensions;
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

    public async Task<EventAgendaItem?> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.EventAgendaItems
            .AsNoTracking()
            .WherePubliclyEligible()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<List<EventAgendaItem>> GetPublicByEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _dbContext.EventAgendaItems
            .AsNoTracking()
            .WherePubliclyEligible()
            .Where(item => item.EventId == eventId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.StartTime)
            .ToListAsync(cancellationToken);
    }
}
