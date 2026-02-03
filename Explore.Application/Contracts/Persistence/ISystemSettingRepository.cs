// ABOUTME: Repository interface for SystemSetting entity providing data access
// for system-wide configuration settings with optional locking.

namespace Explore.Application.Contracts.Persistence;

using Explore.Domain;

/// <summary>
/// Repository for system-wide settings.
/// </summary>
public interface ISystemSettingRepository : IGenericRepository<SystemSetting, Guid>
{
    /// <summary>
    /// Gets a setting by its unique key.
    /// </summary>
    Task<SystemSetting?> GetByKey(string key);

    /// <summary>
    /// Gets all settings, optionally filtered by category.
    /// </summary>
    Task<List<SystemSetting>> GetAllSettings(string? category = null);

    /// <summary>
    /// Checks if a setting exists and is locked.
    /// </summary>
    Task<bool> IsLocked(string key);
}
