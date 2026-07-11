// ABOUTME: EF implementation of IEventDayRepository - delegates CRUD to GenericRepository and adds tenant-aware validation queries.
// ABOUTME: Reads are AsNoTracking so validator use does not accidentally attach entities.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventDayRepository : GenericRepository<EventDay, Guid>, IEventDayRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventDayRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> BelongsToEventAsync(Guid eventDayId, Guid eventId, CancellationToken cancellationToken)
    {
        return await _dbContext.EventDays
            .AsNoTracking()
            .AnyAsync(d => d.Id == eventDayId && d.EventId == eventId, cancellationToken);
    }

    public async Task<List<EventDay>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _dbContext.EventDays
            .AsNoTracking()
            .Where(d => d.EventId == eventId)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.LocalDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<EventDay?> FindByEventAndLocalDateAsync(Guid eventId, DateOnly localDate, CancellationToken cancellationToken)
    {
        return await _dbContext.EventDays
            .AsNoTracking()
            .Where(d => d.EventId == eventId && d.LocalDate == localDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
