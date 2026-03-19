// ABOUTME: Deployment mode for the application instance (single-tenant vs multi-tenant).
// ABOUTME: Defined in Domain so Application and Infrastructure layers can both reference it.

namespace Explore.Domain.Enums;

/// <summary>
/// Deployment mode for the application.
/// </summary>
public enum DeploymentMode
{
    /// <summary>
    /// Single-tenant mode: One tenant, simplified administration.
    /// Tenant resolution is skipped, all entities use DefaultTenantId.
    /// Platform-admin endpoints may be hidden based on configuration.
    /// </summary>
    SingleTenant = 1,

    /// <summary>
    /// Multi-tenant mode: Multiple tenants with full isolation.
    /// Tenant resolved from subdomain or X-Tenant-Id header.
    /// Full platform-admin functionality available.
    /// </summary>
    MultiTenant = 2
}
