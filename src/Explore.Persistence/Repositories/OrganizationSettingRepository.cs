// ABOUTME: Repository implementation for OrganizationSetting entity providing data access
// for organization-specific setting overrides.

namespace Explore.Persistence.Repositories;

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

public class OrganizationSettingRepository : GenericRepository<OrganizationSetting, Guid>, IOrganizationSettingRepository
{
    private readonly ExploreDbContext _dbContext;

    public OrganizationSettingRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrganizationSetting?> GetByOrganizationAndKey(Guid organizationId, string key)
    {
        return await _dbContext.OrganizationSettingOverrides
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationTenant.OrganizationId == organizationId && s.SettingKey == key);
    }

    public async Task<List<OrganizationSetting>> GetAllForOrganization(Guid organizationId)
    {
        return await _dbContext.OrganizationSettingOverrides
            .AsNoTracking()
            .Where(s => s.OrganizationTenant.OrganizationId == organizationId)
            .ToListAsync();
    }

    public async Task<bool> RemoveOverride(Guid organizationId, string key)
    {
        var setting = await _dbContext.OrganizationSettingOverrides
            .FirstOrDefaultAsync(s => s.OrganizationTenant.OrganizationId == organizationId && s.SettingKey == key);

        if (setting == null)
            return false;

        _dbContext.OrganizationSettingOverrides.Remove(setting);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
