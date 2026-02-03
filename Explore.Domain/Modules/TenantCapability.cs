// ABOUTME: Links modules to tenants, controlling which aspects are available.
// ABOUTME: Implements ITenantEntity for automatic tenant filtering.

using Explore.Domain.Interfaces;

namespace Explore.Domain.Modules;

/// <summary>
/// Links modules to tenants, controlling which aspects are available.
/// Each record represents an enabled module for a specific tenant.
/// </summary>
public class TenantCapability : ITenantEntity
{
    /// <summary>
    /// Primary key - UUID v7 for time-ordered IDs.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant this capability belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Navigation property to the tenant.
    /// </summary>
    public Tenant? Tenant { get; set; }

    /// <summary>
    /// Module being enabled for the tenant.
    /// </summary>
    public Guid ModuleId { get; set; }

    /// <summary>
    /// Navigation property to the module definition.
    /// </summary>
    public ModuleDefinition? Module { get; set; }

    /// <summary>
    /// Whether this module is currently enabled for the tenant.
    /// Can be temporarily disabled without removing the record.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// When this capability was first enabled.
    /// </summary>
    public DateTime EnabledAt { get; set; }

    /// <summary>
    /// User who enabled this capability.
    /// </summary>
    public Guid? EnabledBy { get; set; }

    /// <summary>
    /// Optional configuration overrides for this module (JSON).
    /// Allows tenant-specific module configuration.
    /// </summary>
    public string? ConfigurationJson { get; set; }
}
