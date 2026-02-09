// ABOUTME: Repository implementation for tenant administrator assignments.
// ABOUTME: Provides tenant/user scoped membership queries with role details.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class TenantAdministratorRepository : GenericRepository<TenantAdministrator, Guid>, ITenantAdministratorRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantAdministratorRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantAdministrator?> GetByTenantAndUser(Guid tenantId, Guid userId)
    {
        return await _dbContext.TenantAdministrators
            .AsNoTracking()
            .Include(x => x.TenantAdministratorRole)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId);
    }

    public async Task<List<TenantAdministrator>> GetByTenant(Guid tenantId)
    {
        return await _dbContext.TenantAdministrators
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.TenantAdministratorRole)
            .Where(x => x.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<bool> IsTenantAdministrator(Guid tenantId, Guid userId)
    {
        return await _dbContext.TenantAdministrators
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.UserId == userId);
    }
}
