using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class UserRoleRepository : GenericRepository<UserRole, int>, IUserRoleRepository
    {
        private readonly ExploreDbContext _dbContext;

        public UserRoleRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserRole?> GetByMasterCode(string masterCode)
        {
            return await _dbContext.UserRoles
                .FirstOrDefaultAsync(r => r.MasterCode == masterCode);
        }
    }
}
