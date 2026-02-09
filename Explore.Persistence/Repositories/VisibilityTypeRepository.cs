using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class VisibilityTypeRepository : GenericRepository<VisibilityType, int>, IVisibilityTypeRepository
{
    public VisibilityTypeRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
