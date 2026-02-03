// ABOUTME: Contract for the cascading settings resolver that implements 3-tier resolution:
// System default → Tenant override → with respect to IsLocked flag.

namespace Explore.Application.Contracts.Infrastructure;

using Explore.Domain;

/// <summary>
/// Service for resolving settings using the cascading settings engine.
/// Resolution order: 1) Check if locked at system level 2) Check tenant override 3) Fall back to system default.
/// </summary>
public interface ISettingsResolver
{
    /// <summary>
    /// Gets the effective value for a setting key, resolving through the cascade.
    /// </summary>
    /// <typeparam name="T">The expected type of the setting value.</typeparam>
    /// <param name="key">The setting key (e.g., "events.max_sessions_per_event").</param>
    /// <param name="tenantId">The tenant to resolve for (or null for system default only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved setting value, or default(T) if not found.</returns>
    Task<T?> GetSettingAsync<T>(string key, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective value for a setting key with metadata about how it was resolved.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="tenantId">The tenant to resolve for (or null for system default only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved setting with metadata.</returns>
    Task<ResolvedSetting?> GetSettingWithMetadataAsync(string key, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settings for a tenant with their effective values and resolution sources.
    /// </summary>
    /// <param name="tenantId">The tenant to get settings for (or null for system defaults only).</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of resolved settings.</returns>
    Task<IReadOnlyList<ResolvedSetting>> GetAllSettingsAsync(Guid? tenantId = null, string? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a setting can be overridden by a tenant.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the setting can be overridden, false if locked.</returns>
    Task<bool> CanOverrideAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a tenant-specific override for a setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to set (will be JSON serialized).</param>
    /// <param name="tenantId">The tenant to set the override for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false if setting is locked or doesn't exist.</returns>
    Task<bool> SetTenantOverrideAsync(string key, object value, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a tenant-specific override, reverting to system default.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="tenantId">The tenant to remove the override for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if an override was removed, false if none existed.</returns>
    Task<bool> RemoveTenantOverrideAsync(string key, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cache for a specific setting or all settings.
    /// </summary>
    /// <param name="key">The setting key to invalidate, or null for all.</param>
    /// <param name="tenantId">The tenant to invalidate for, or null for all tenants.</param>
    void InvalidateCache(string? key = null, Guid? tenantId = null);
}

/// <summary>
/// A resolved setting with metadata about how it was resolved.
/// </summary>
public class ResolvedSetting
{
    /// <summary>
    /// The setting key.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// The effective value (JSON string).
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// The data type of the value.
    /// </summary>
    public SettingValueType ValueType { get; init; }

    /// <summary>
    /// Where the value came from.
    /// </summary>
    public SettingSource Source { get; init; }

    /// <summary>
    /// Whether this setting is locked at system level.
    /// </summary>
    public bool IsLocked { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Category for grouping.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Allowed values if constrained.
    /// </summary>
    public string? AllowedValues { get; init; }
}

/// <summary>
/// Source of a resolved setting value.
/// </summary>
public enum SettingSource
{
    /// <summary>
    /// Value comes from the system default (no tenant override).
    /// </summary>
    SystemDefault = 0,

    /// <summary>
    /// Value comes from a tenant-specific override.
    /// </summary>
    TenantOverride = 1,

    /// <summary>
    /// Value comes from the system and is locked (cannot be overridden).
    /// </summary>
    SystemLocked = 2
}
