// ABOUTME: Repository contract for event-session agenda item entity reads.
// ABOUTME: Keeps agenda item query mapping in handlers and supports caller cancellation.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventSessionAgendaItemRepository : IGenericRepository<EventSessionAgendaItem, Guid>
{
    Task<EventSessionAgendaItem?> GetByIdWithDetails(Guid id, CancellationToken cancellationToken = default);
    Task<List<EventSessionAgendaItem>> GetBySession(Guid eventSessionId, CancellationToken cancellationToken = default);
    Task<(List<EventSessionAgendaItem> Items, int TotalCount)> GetAgendaItemsWithDetailsPaged(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
