using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class OrganizationPositionRepository : GenericRepository<OrganizationPosition, int>, IOrganizationPositionRepository
{
    public OrganizationPositionRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
