// ABOUTME: EF Core repository for event-session agenda item reads.
// ABOUTME: Provides no-tracking detail/session/list queries with caller cancellation propagation.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventSessionAgendaItemRepository : GenericRepository<EventSessionAgendaItem, Guid>, IEventSessionAgendaItemRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventSessionAgendaItemRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventSessionAgendaItem?> GetByIdWithDetails(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventSessionAgendaItems
            .AsNoTracking()
            .Include(a => a.EventSession)
                .ThenInclude(s => s.Event)
            .Include(a => a.Location)
                .ThenInclude(l => l!.Pii)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<EventSessionAgendaItem>> GetBySession(
        Guid eventSessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventSessionAgendaItems
            .AsNoTracking()
            .Include(a => a.EventSession)
            .Include(a => a.Location)
                .ThenInclude(l => l!.Pii)
            .Where(a => a.EventSessionId == eventSessionId)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<EventSessionAgendaItem?> GetPublicByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventSessionAgendaItems
            .AsNoTracking()
            .Include(item => item.EventSession)
                .ThenInclude(session => session.Event)
            .Include(item => item.Location)
                .ThenInclude(location => location!.Pii)
            .WherePubliclyEligible()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<List<EventSessionAgendaItem>> GetPublicBySessionAsync(
        Guid eventSessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventSessionAgendaItems
            .AsNoTracking()
            .Include(item => item.EventSession)
                .ThenInclude(session => session.Event)
            .Include(item => item.Location)
                .ThenInclude(location => location!.Pii)
            .WherePubliclyEligible()
            .Where(item => item.EventSessionId == eventSessionId)
            .OrderBy(item => item.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<EventSessionAgendaItem> Items, int TotalCount)> GetAgendaItemsWithDetailsPaged(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EventSessionAgendaItems
            .AsNoTracking()
            .Include(a => a.EventSession)
                .ThenInclude(s => s.Event)
            .Include(a => a.Location)
                .ThenInclude(l => l!.Pii)
            .OrderByDescending(a => a.StartTime);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<EventSessionAgendaItem> Items, int TotalCount)> GetPublicAgendaItemsWithDetailsPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EventSessionAgendaItems
            .AsNoTracking()
            .Include(item => item.EventSession)
                .ThenInclude(session => session.Event)
            .Include(item => item.Location)
                .ThenInclude(location => location!.Pii)
            .WherePubliclyEligible()
            .OrderByDescending(item => item.StartTime);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
