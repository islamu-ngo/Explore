// ABOUTME: Legacy Blazor resolver that reads X-Tenant-Id directly from the request.
// ABOUTME: Retained as an unwired reference while the Blazor host now forwards route slug context only.

using Explore.Application.Contracts.Services;
using Explore.Blazor.Client.Constants;

namespace Explore.Blazor.Services;

public class BlazorHeaderTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BlazorHeaderTenantResolver(IHttpContextAccessor httpContextAccessor)
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

        if (httpContext.Request.Headers.TryGetValue(TenantConstants.TenantIdHeaderName, out var tenantIdHeader)
            && Guid.TryParse(tenantIdHeader.FirstOrDefault(), out var tenantId))
        {
            return tenantId;
        }

        return null;
    }
}
