using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class OrganizationRoleRepository : GenericRepository<OrganizationRole, int>, IOrganizationRoleRepository
{
    public OrganizationRoleRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
