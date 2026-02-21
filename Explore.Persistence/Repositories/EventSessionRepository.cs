using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

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
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Event)
            .Include(s => s.Location)
                .ThenInclude(l => l!.Pii)
            .Include(s => s.RegistrationMode)
            .Include(s => s.IslamicAspect)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<EventSession>> GetSessionsByEvent(Guid eventId)
    {
        return await _dbContext.EventSessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Event)
            .Include(s => s.Location)
                .ThenInclude(l => l!.Pii)
            .Include(s => s.RegistrationMode)
            .Include(s => s.IslamicAspect)
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<List<EventSession>> GetSessionsByLocation(Guid locationId)
    {
        return await _dbContext.EventSessions
            .AsNoTracking()
            .Include(s => s.Event)
            .Include(s => s.RegistrationMode)
            .Include(s => s.IslamicAspect)
            .Where(s => s.LocationId == locationId)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<(List<EventSession> Items, int TotalCount)> GetSessionsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.EventSessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Event)
            .Include(s => s.Location)
                .ThenInclude(l => l!.Pii)
            .Include(s => s.RegistrationMode)
            .Include(s => s.IslamicAspect)
            .OrderByDescending(s => s.StartTime);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
