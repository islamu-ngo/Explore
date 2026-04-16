// ABOUTME: Repository contract for querying event session custom-property projection rows.
// ABOUTME: Supports admin inspection and exposure-filtered reads for session scope.

using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Persistence;

public interface IEventSessionCustomPropertyProjectionRepository
{
    Task<List<EventSessionCustomPropertyProjection>> GetForSessionAsync(
        Guid eventSessionId,
        ExposureLevel? exposureCeiling,
        CancellationToken cancellationToken);
}
