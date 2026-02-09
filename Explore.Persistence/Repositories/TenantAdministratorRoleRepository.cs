// ABOUTME: Repository implementation for tenant administrator role lookup records.
// ABOUTME: Resolves roles by master code for deterministic tenant admin assignment.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class TenantAdministratorRoleRepository : GenericRepository<TenantAdministratorRole, int>, ITenantAdministratorRoleRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantAdministratorRoleRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantAdministratorRole?> GetByMasterCode(string masterCode)
    {
        return await _dbContext.TenantAdministratorRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MasterCode == masterCode);
    }
}
