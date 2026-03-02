// ABOUTME: Repository implementation for tenant member assignments.
// ABOUTME: Provides tenant/user scoped membership queries with role details.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class TenantMemberRepository : GenericRepository<TenantMember, Guid>, ITenantMemberRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantMemberRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantMember?> GetByTenantAndUser(Guid tenantId, Guid userId)
    {
        return await _dbContext.TenantMembers
            .AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId);
    }

    public async Task<List<TenantMember>> GetByTenant(Guid tenantId)
    {
        return await _dbContext.TenantMembers
            .AsNoTracking()
            .Include(x => x.User)
                .ThenInclude(u => u!.Pii)
            .Include(x => x.Role)
            .Where(x => x.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<List<TenantMember>> GetByUserId(Guid userId)
    {
        return await _dbContext.TenantMembers
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Role)
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }

    public async Task<bool> IsTenantMember(Guid tenantId, Guid userId)
    {
        return await _dbContext.TenantMembers
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.UserId == userId);
    }

    public async Task<bool> IsTenantAdmin(Guid tenantId, Guid userId)
    {
        return await _dbContext.TenantMembers
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId
                && x.UserId == userId
                && x.RoleId == (int)RoleEnum.TenantAdmin);
    }
}
