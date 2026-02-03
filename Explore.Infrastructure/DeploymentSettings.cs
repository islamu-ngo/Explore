// ABOUTME: Configuration settings for deployment mode (single-tenant vs multi-tenant).
// ABOUTME: Enables a single binary to run in both modes based on configuration.

namespace Explore.Infrastructure;

/// <summary>
/// Configuration settings for deployment mode.
/// Bind from appsettings.json section "Deployment".
/// </summary>
public class DeploymentSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Deployment";

    /// <summary>
    /// Deployment mode: SingleTenant or MultiTenant.
    /// Default: MultiTenant.
    /// </summary>
    public DeploymentMode Mode { get; set; } = DeploymentMode.MultiTenant;

    /// <summary>
    /// Default tenant ID used in SingleTenant mode.
    /// All entities will use this tenant ID.
    /// </summary>
    public Guid DefaultTenantId { get; set; } = Guid.Empty;

    /// <summary>
    /// Whether to hide SuperAdmin endpoints in SingleTenant mode.
    /// When true, tenant management and system settings endpoints return 404.
    /// Default: true.
    /// </summary>
    public bool HideSuperAdminInSingleTenant { get; set; } = true;

    /// <summary>
    /// Default tenant subdomain used for URL generation in SingleTenant mode.
    /// </summary>
    public string? DefaultTenantSubdomain { get; set; }

    /// <summary>
    /// Whether the deployment is in single-tenant mode.
    /// </summary>
    public bool IsSingleTenant => Mode == DeploymentMode.SingleTenant;

    /// <summary>
    /// Whether the deployment is in multi-tenant mode.
    /// </summary>
    public bool IsMultiTenant => Mode == DeploymentMode.MultiTenant;
}

/// <summary>
/// Deployment mode for the application.
/// </summary>
public enum DeploymentMode
{
    /// <summary>
    /// Single-tenant mode: One tenant, simplified administration.
    /// Tenant resolution is skipped, all entities use DefaultTenantId.
    /// SuperAdmin endpoints may be hidden based on configuration.
    /// </summary>
    SingleTenant = 1,

    /// <summary>
    /// Multi-tenant mode: Multiple tenants with full isolation.
    /// Tenant resolved from subdomain or X-Tenant-Id header.
    /// Full SuperAdmin functionality available.
    /// </summary>
    MultiTenant = 2
}
