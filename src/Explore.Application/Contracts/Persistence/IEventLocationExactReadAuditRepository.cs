// ABOUTME: Append-only persistence contract for exceptional exact EventLocation read evidence.
// ABOUTME: Exposes bounded tenant-filtered entity reads without update, delete, DTO, or queryable surfaces.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventLocationExactReadAuditRepository
{
    const int MaximumBatchSize = 256;

    Task<EventLocationExactReadAudit> AppendAsync(
        EventLocationExactReadAudit audit,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<EventLocationExactReadAudit>> GetByEventLocationsAsync(
        IReadOnlyCollection<Guid> eventLocationIds,
        CancellationToken cancellationToken);
}
