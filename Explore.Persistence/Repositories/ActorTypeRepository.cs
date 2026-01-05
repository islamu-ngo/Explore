using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories
{
    public class ActorTypeRepository : GenericRepository<ActorType, int>, IActorTypeRepository
    {
        public ActorTypeRepository(ExploreDbContext dbContext) : base(dbContext)
        {
        }
    }
}
