// ABOUTME: Repository contract for the keyless EventWithSessions aggregate read model and its supporting metadata.
// ABOUTME: Returns read entities plus source-of-truth definition entities needed to enrich view JSON facet payloads.

using Explore.Domain;
using Explore.Domain.Views;

namespace Explore.Application.Contracts.Persistence;

public interface IEventAggregateViewRepository
{
    Task<EventWithSessionsView?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken);

    Task<(List<EventWithSessionsView> Items, int TotalCount)> GetPagedAsync(
        EventAggregateViewFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<List<EventCustomPropertyDefinition>> GetEventDefinitionsByEventIdsAsync(
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken);

    Task<List<EventSessionCustomPropertyDefinition>> GetSessionDefinitionsForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken);
}
