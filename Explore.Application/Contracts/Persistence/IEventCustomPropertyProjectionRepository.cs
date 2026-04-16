// ABOUTME: Repository contract for querying event custom-property projection rows.
// ABOUTME: Supports admin inspection, exposure-filtered reads, and governance reporting aggregation.

using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Persistence;

public interface IEventCustomPropertyProjectionRepository
{
    Task<List<EventCustomPropertyProjection>> GetForEventAsync(
        Guid eventId,
        ExposureLevel? exposureCeiling,
        CancellationToken cancellationToken);

    Task<int> CountActiveDefinitionsForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
}
