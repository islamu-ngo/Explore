using Explore.Application.Contracts.Infrastructure;

namespace Explore.API.Services;

/// <summary>
/// Provides tenant context by reading the X-Tenant-Id header from HTTP requests.
/// Falls back to default tenant ID if header is not present.
/// </summary>
public class TenantContext : ITenantContext
{
    private const string TenantIdHeaderName = "X-Tenant-Id";
    
    /// <summary>
    /// Default tenant ID matching the seeded tenant in the database.
    /// This MUST match SeedIds.DefaultTenantId in Explore.Persistence.
    /// </summary>
    private static readonly Guid DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current tenant ID from the X-Tenant-Id header.
    /// Falls back to the default tenant if header is missing or invalid.
    /// </summary>
    public Guid TenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return DefaultTenantId;
            }

            if (httpContext.Request.Headers.TryGetValue(TenantIdHeaderName, out var tenantIdHeader) &&
                Guid.TryParse(tenantIdHeader.FirstOrDefault(), out var tenantId))
            {
                return tenantId;
            }

            return DefaultTenantId;
        }
    }
}
