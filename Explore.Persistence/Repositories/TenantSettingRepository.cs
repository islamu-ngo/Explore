// ABOUTME: Repository implementation for TenantSetting entity providing data access
// for tenant-specific setting overrides.

namespace Explore.Persistence.Repositories;

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

public class TenantSettingRepository : GenericRepository<TenantSetting, Guid>, ITenantSettingRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantSettingRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantSetting?> GetByTenantAndKey(Guid tenantId, string key)
    {
        return await _dbContext.TenantSettingOverrides
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SettingKey == key);
    }

    public async Task<List<TenantSetting>> GetAllForTenant(Guid tenantId)
    {
        return await _dbContext.TenantSettingOverrides
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<bool> RemoveOverride(Guid tenantId, string key)
    {
        var setting = await _dbContext.TenantSettingOverrides
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SettingKey == key);

        if (setting == null)
            return false;

        _dbContext.TenantSettingOverrides.Remove(setting);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
