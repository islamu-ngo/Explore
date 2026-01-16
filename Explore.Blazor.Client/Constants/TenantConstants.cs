namespace Explore.Blazor.Client.Constants;

/// <summary>
/// Static constants for tenant configuration.
/// For single-tenant mode, the default tenant ID is used for all requests.
/// </summary>
public static class TenantConstants
{
    /// <summary>
    /// HTTP header name for passing tenant ID to the API.
    /// </summary>
    public const string TenantIdHeaderName = "X-Tenant-Id";

    /// <summary>
    /// Default tenant ID matching the seeded tenant in the database.
    /// This MUST match SeedIds.DefaultTenantId in Explore.Persistence.
    /// </summary>
    public static readonly Guid DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
}
