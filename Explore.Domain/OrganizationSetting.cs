// ABOUTME: Organization-specific setting override for the hierarchical settings engine.
// Allows organizations to customize settings unless locked at Instance or Tenant scope.

namespace Explore.Domain;

using Explore.Domain.Interfaces;

/// <summary>
/// Organization-specific override for a system or tenant setting.
/// Only created when an organization explicitly overrides the parent scope default.
/// </summary>
public class OrganizationSetting : ITenantEntity, IAuditableEntity
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
    /// The organization this setting belongs to.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Navigation property to the organization.
    /// </summary>
    public required Organization Organization { get; set; }

    /// <summary>
    /// The setting key being overridden.
    /// </summary>
    public required string SettingKey { get; set; }

    /// <summary>
    /// JSON-serialized value of the organization's override.
    /// </summary>
    public required string Value { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
