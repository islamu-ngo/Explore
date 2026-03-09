// ABOUTME: Legacy API resolver that reads X-Tenant-Id directly from the request.
// ABOUTME: Retained as an unwired reference while standard routing uses API-authoritative middleware instead.

using Explore.Application.Contracts.Services;
namespace Explore.API.Services;

public class HeaderTenantResolver : ITenantResolver
{
    private const string TenantIdHeaderName = "X-Tenant-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderTenantResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Name => "header";

    public int Priority => 1;

    public Guid? ResolveTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        if (httpContext.Request.Headers.TryGetValue(TenantIdHeaderName, out var tenantIdHeader)
            && Guid.TryParse(tenantIdHeader.FirstOrDefault(), out var headerTenantId))
        {
            return headerTenantId;
        }

        return null;
    }
}
