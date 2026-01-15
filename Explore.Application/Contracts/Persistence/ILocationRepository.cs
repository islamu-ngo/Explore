using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface ILocationRepository : IGenericRepository<Location, Guid>
    {
        Task<List<Location>> GetLocationsByTenant(Guid tenantId);
        Task<List<Location>> GetLocationsByCity(string city);
        Task<List<Location>> GetLocationsByCountry(string country);
        Task<(List<Location> Items, int TotalCount)> GetLocationsWithDetailsPaged(int pageNumber, int pageSize);
    }
}
