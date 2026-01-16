namespace Explore.Blazor.Client.Configuration;

/// <summary>
/// Multi-tenancy configuration for ISLAMU Event.
///
/// DEPLOYMENT MODES:
/// - MODE 1 (Default): Single-Instance Deployment
///   • EnableMultiTenancy = false
///   • One organization/community per deployment
///   • Uses DefaultTenantId for all operations
///   • Simpler configuration and maintenance
///
/// - MODE 2 (Future): Multi-Tenant SaaS Deployment
///   • EnableMultiTenancy = true
///   • Multiple isolated tenants
///   • Requires tenant ID in user claims
///   • Each tenant has custom domain/branding
///
/// Environment Variables (follows EXPLORE__* convention):
/// - EXPLORE__MULTITENANCY__ENABLED=false (default)
/// - EXPLORE__MULTITENANCY__DEFAULT_TENANT=default (slug)
/// </summary>
public class TenantConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// Maps to "Explore:MultiTenancy" section
    /// </summary>
    public const string SectionName = "Explore:MultiTenancy";

    /// <summary>
    /// Enables multi-tenancy mode.
    ///
    /// Default: false (single-instance mode)
    /// - When false: Always uses DefaultTenantId, tenant ID in claims ignored
    /// - When true: Requires valid tenant ID in user JWT claims
    ///
    /// Environment variable: EXPLORE__MULTITENANCY__ENABLED
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The default tenant ID used for single-instance deployments (Mode 1).
    /// This UUID maps to the tenant record in the database.
    ///
    /// Default: 018e4e5c-7f00-7000-8000-000000000001 (matches SeedIds.DefaultTenantId)
    ///
    /// Note: This tenant must exist in the database (tenant table).
    /// The application should create this tenant on first run if it doesn't exist.
    /// IMPORTANT: This MUST match Explore.API.Services.TenantContext.DefaultTenantId
    /// and Explore.Persistence.SeedIds.DefaultTenantId for tenant isolation to work correctly.
    /// </summary>
    public Guid DefaultTenantId { get; set; } = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");

    /// <summary>
    /// The slug/identifier for the default tenant.
    /// Used in URLs and subdomain routing (future feature).
    ///
    /// Default: "default"
    /// Environment variable: EXPLORE__MULTITENANCY__DEFAULT_TENANT
    /// </summary>
    public string DefaultTenant { get; set; } = "default";

    /// <summary>
    /// The display name of the default tenant.
    /// Shown in UI when tenant name is displayed.
    ///
    /// Default: "Default"
    /// </summary>
    public string DefaultTenantName { get; set; } = "Default";

    /// <summary>
    /// Validates that the configuration is properly set.
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool IsValid()
    {
        // DefaultTenantId must not be empty
        if (DefaultTenantId == Guid.Empty)
            return false;

        // DefaultTenant slug must not be empty/whitespace
        if (string.IsNullOrWhiteSpace(DefaultTenant))
            return false;

        return true;
    }
}
