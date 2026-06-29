// ABOUTME: EF repository for event-session speaker link entities and speaker/session lookup projections.
// ABOUTME: Exposes duplicate-link reads used by grouped relationship update handlers.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventSessionSpeakerRepository : GenericRepository<EventSessionSpeaker, Guid>, IEventSessionSpeakerRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventSessionSpeakerRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<EventSessionSpeaker>> GetBySession(Guid eventSessionId)
    {
        return await _dbContext.EventSessionSpeakers
            .AsNoTracking()
            .Include(s => s.Actor)
                .ThenInclude(a => a!.Pii)
            .Where(s => s.EventSessionId == eventSessionId)
            .ToListAsync();
    }

    public async Task<List<EventSessionSpeaker>> GetByActor(Guid actorId)
    {
        return await _dbContext.EventSessionSpeakers
            .AsNoTracking()
            .Include(s => s.EventSession)
            .Where(s => s.ActorId == actorId)
            .ToListAsync();
    }

    public async Task<(List<EventSessionSpeaker> Items, int TotalCount)> GetSpeakersWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.EventSessionSpeakers
            .AsNoTracking()
            .Include(s => s.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(s => s.EventSession)
                .ThenInclude(es => es.Event)
            .OrderByDescending(s => s.Id);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<EventSessionSpeaker?> GetBySessionAndActor(Guid eventSessionId, Guid actorId, Guid? excludeId = null)
    {
        var query = _dbContext.EventSessionSpeakers
            .AsNoTracking()
            .Where(s => s.EventSessionId == eventSessionId && s.ActorId == actorId);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.FirstOrDefaultAsync();
    }
}
