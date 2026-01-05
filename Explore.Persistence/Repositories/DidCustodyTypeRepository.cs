using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories
{
    public class DidCustodyTypeRepository : GenericRepository<DidCustodyType, int>, IDidCustodyTypeRepository
    {
        public DidCustodyTypeRepository(ExploreDbContext dbContext) : base(dbContext)
        {
        }
    }
}
