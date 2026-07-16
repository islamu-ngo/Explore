// ABOUTME: Append-only entity persistence contract for EventLocation disclosure policy evidence.
// ABOUTME: Exposes ordered no-tracking history without update, delete, DTO, or queryable surfaces.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventLocationDisclosureAuditRepository
{
    Task<EventLocationDisclosureAudit> AppendAsync(
        EventLocationDisclosureAudit audit,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<EventLocationDisclosureAudit>> GetByEventLocationAsync(
        Guid eventLocationId,
        CancellationToken cancellationToken);
}
