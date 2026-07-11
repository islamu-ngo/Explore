// ABOUTME: Repository implementation for SystemSetting entity providing data access
// for system-wide configuration settings with caching support.

namespace Explore.Persistence.Repositories;

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly ExploreDbContext _dbContext;
    private readonly ISettingMutationLock _mutationLock;

    public SystemSettingRepository(
        ExploreDbContext dbContext,
        ISettingMutationLock mutationLock)
    {
        _dbContext = dbContext;
        _mutationLock = mutationLock;
    }

    public async Task<SystemSetting?> GetByKey(
        string settingKey,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == settingKey, cancellationToken);
    }

    public Task<string?> UpsertAsync(
        SystemSetting setting,
        CancellationToken cancellationToken = default)
    {
        return _mutationLock.ExecuteAsync(
            setting.SettingKey,
            async token =>
            {
                SystemSetting? existing = await _dbContext.SystemSettings
                    .FirstOrDefaultAsync(candidate => candidate.SettingKey == setting.SettingKey, token);
                string? previousValue = existing?.Value;

                if (existing is null)
                {
                    _dbContext.SystemSettings.Add(setting);
                }
                else
                {
                    existing.Value = setting.Value;
                    existing.ValueType = setting.ValueType;
                    existing.IsLocked = setting.IsLocked;
                    existing.AllowedValues = setting.AllowedValues;
                    existing.Description = setting.Description;
                    existing.Category = setting.Category;
                    existing.DisplayOrder = setting.DisplayOrder;
                    existing.UpdatedAt = setting.UpdatedAt ?? DateTime.UtcNow;
                    existing.UpdatedBy = setting.UpdatedBy;
                }

                await _dbContext.SaveChangesAsync(token);
                return previousValue;
            },
            cancellationToken);
    }

    public async Task<List<SystemSetting>> GetAllSettings(
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SystemSettings.AsNoTracking();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(s => s.Category == category);
        }

        return await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsLocked(string settingKey, CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == settingKey, cancellationToken);

        return setting?.IsLocked ?? false;
    }
}
