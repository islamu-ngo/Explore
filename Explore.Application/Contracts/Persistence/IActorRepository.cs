using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IActorRepository : IGenericRepository<Actor, Guid>
    {
        Task<Actor?> GetActorWithDetails(Guid id);
        Task<Actor?> GetActorByDid(string did);
        Task<Actor?> GetActorByHandle(string handle);
        Task<List<Actor>> GetActorsByTenant(Guid tenantId);
    }
}
