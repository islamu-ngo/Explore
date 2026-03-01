// ABOUTME: Group-specific setting override for the hierarchical settings engine.
// Allows groups to customize settings unless locked at a higher scope.

namespace Explore.Domain;

using Explore.Domain.Interfaces;

/// <summary>
/// Group-specific override for a setting.
/// Only created when a group explicitly overrides the parent scope default.
/// </summary>
public class GroupSetting : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The tenant this setting belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Navigation property to the tenant.
    /// </summary>
    public required Tenant Tenant { get; set; }

    /// <summary>
    /// The group this setting belongs to.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Navigation property to the group.
    /// </summary>
    public required Group Group { get; set; }

    /// <summary>
    /// The setting key being overridden.
    /// </summary>
    public required string SettingKey { get; set; }

    /// <summary>
    /// JSON-serialized value of the group's override.
    /// </summary>
    public required string Value { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
