using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IActorRepository : IGenericRepository<Actor, Guid>
    {
        Task<Actor?> GetActorWithDetails(Guid id);
        Task<Actor?> GetActorByDid(string did);
        Task<Actor?> GetActorByHandle(string handle);
        Task<List<Actor>> GetActorsByTenant(Guid tenantId);
        Task<bool> DidExists(string did);
        Task<(List<Actor> Items, int TotalCount)> GetActorsWithDetailsPaged(int pageNumber, int pageSize);

        /// <summary>
        /// Gets the Actor associated with a specific User (personal actor).
        /// </summary>
        Task<Actor?> GetActorByUserId(Guid userId);

        /// <summary>
        /// Gets the Actor associated with a specific Organization.
        /// </summary>
        Task<Actor?> GetActorByOrganizationId(Guid organizationId);
    }
}
