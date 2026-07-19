// ABOUTME: Application boundary for bounded purpose-scoped EventLocation disclosure batches.
// ABOUTME: Returns one immutable fail-closed result per unambiguous EventLocation request.

using Explore.Application.Contracts.LocationPrivacy;

namespace Explore.Application.Contracts.Services;

public interface IEventLocationDisclosureService
{
    const int MaximumBatchSize = 256;

    Task<IReadOnlyDictionary<Guid, EventLocationDisclosureResult>> ResolveManyAsync(
        IReadOnlyCollection<EventLocationDisclosureRequest> requests,
        CancellationToken cancellationToken);
}
