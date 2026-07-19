// ABOUTME: Repository contract for EventAgendaItem - event-level timeline blocks (breaks, prayer, opening, logistics).
// ABOUTME: Provides tenant-aware reads for admin surfaces and agenda projection queries.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventAgendaItemRepository : IGenericRepository<EventAgendaItem, Guid>
{
    Task<List<EventAgendaItem>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken);

    Task<EventAgendaItem?> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<EventAgendaItem>> GetPublicByEventAsync(Guid eventId, CancellationToken cancellationToken);

    Task MoveToEventAsync(
        EventAgendaItem agendaItem,
        Guid eventId,
        EventLocation eventLocation,
        Guid? roomId,
        CancellationToken cancellationToken);
}
