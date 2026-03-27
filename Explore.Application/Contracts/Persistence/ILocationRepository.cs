using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ILocationRepository : IGenericRepository<Location, Guid>
{
    Task<List<Location>> GetLocationsByTenant(Guid tenantId);
    Task<List<Location>> GetLocationsByCity(string city);
    Task<List<Location>> GetLocationsByCountry(string country);
    Task<(List<Location> Items, int TotalCount)> GetLocationsWithDetailsPaged(int pageNumber, int pageSize);

    /// <summary>
    /// Permanently deletes PII data for a location (GDPR erasure).
    /// Uses ExecuteDeleteAsync for efficient bulk deletion without loading entities.
    /// </summary>
    /// <returns>Number of PII records deleted.</returns>
    Task<int> ForgetPiiAsync(Guid locationId);
}
