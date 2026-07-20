// ABOUTME: Repository contract for location entity reads and PII erasure.
// ABOUTME: Keeps location query mapping in handlers and supports caller cancellation.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ILocationRepository : IGenericRepository<Location, Guid>
{
    Task<List<Location>> GetLocationsByTenant(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetExistingTenantLocationIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> candidateLocationIds,
        CancellationToken cancellationToken = default);
    Task<List<Location>> GetLocationsByCity(string city, CancellationToken cancellationToken = default);
    Task<List<Location>> GetLocationsByCountry(string country, CancellationToken cancellationToken = default);
    Task<List<Location>> GetOwnedPrivateHomesForGlobalErasureAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default);
    Task<(List<Location> Items, int TotalCount)> GetLocationsWithDetailsPaged(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes PII data for a location (GDPR erasure).
    /// Uses ExecuteDeleteAsync for efficient bulk deletion without loading entities.
    /// </summary>
    /// <returns>Number of PII records deleted.</returns>
    Task<int> ForgetPiiAsync(Guid locationId, CancellationToken cancellationToken = default);
}
