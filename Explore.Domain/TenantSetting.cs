// ABOUTME: Tenant-specific setting override for the cascading settings engine.
// Allows tenants to customize settings unless locked at the system level.

namespace Explore.Domain;

using Explore.Domain.Interfaces;

/// <summary>
/// Tenant-specific override for a system setting.
/// Only created when a tenant explicitly overrides the system default.
/// </summary>
public class TenantSetting : ITenantEntity
{
    /// <summary>
    /// Unique identifier for this tenant setting override.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The tenant this setting belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Navigation property to the tenant.
    /// </summary>
    public Tenant? Tenant { get; set; }

    /// <summary>
    /// The setting key being overridden (must match a SystemSetting.Key).
    /// </summary>
    public string SettingKey { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized value of the tenant's override.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// When this override was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Who created this override.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// When this override was last modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who last modified this override.
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}
