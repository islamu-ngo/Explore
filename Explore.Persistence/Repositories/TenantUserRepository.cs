using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class TenantUserRepository : GenericRepository<TenantUser, Guid>, ITenantUserRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantUserRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantUser?> GetByUserAndTenant(Guid userId, Guid tenantId)
    {
        return await _dbContext.TenantUsers
            .AsNoTracking()
            .Include(tu => tu.Role)
            .FirstOrDefaultAsync(tu => tu.UserId == userId && tu.TenantId == tenantId);
    }

    public async Task<List<TenantUser>> GetByUser(Guid userId)
    {
        return await _dbContext.TenantUsers
            .AsNoTracking()
            .Include(tu => tu.Tenant)
            .Include(tu => tu.Role)
            .Where(tu => tu.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<TenantUser>> GetByTenant(Guid tenantId)
    {
        return await _dbContext.TenantUsers
            .AsNoTracking()
            .Include(tu => tu.User)
            .Include(tu => tu.Role)
            .Where(tu => tu.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<TenantUser?> GetTenantUserWithDetails(Guid id)
    {
        return await _dbContext.TenantUsers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(tu => tu.User)
            .Include(tu => tu.Tenant)
            .Include(tu => tu.Role)
            .FirstOrDefaultAsync(tu => tu.Id == id);
    }

    public async Task<List<TenantUser>> GetTenantUsersWithDetails()
    {
        return await _dbContext.TenantUsers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(tu => tu.User)
            .Include(tu => tu.Tenant)
            .Include(tu => tu.Role)
            .ToListAsync();
    }
}
