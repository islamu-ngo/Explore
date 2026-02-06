// ABOUTME: Repository implementation for SystemSetting entity providing data access
// for system-wide configuration settings with caching support.

namespace Explore.Persistence.Repositories;

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

public class SystemSettingRepository : GenericRepository<SystemSetting, Guid>, ISystemSettingRepository
{
    private readonly ExploreDbContext _dbContext;

    public SystemSettingRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemSetting?> GetByKey(string settingKey)
    {
        return await _dbContext.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == settingKey);
    }

    public async Task<List<SystemSetting>> GetAllSettings(string? category = null)
    {
        var query = _dbContext.SystemSettings.AsNoTracking();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(s => s.Category == category);
        }

        return await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<bool> IsLocked(string settingKey)
    {
        var setting = await _dbContext.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == settingKey);

        return setting?.IsLocked ?? false;
    }
}
