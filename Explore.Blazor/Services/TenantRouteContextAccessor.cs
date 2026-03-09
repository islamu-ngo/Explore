// ABOUTME: Stores the current tenant slug in HttpContext.Items with a scoped fallback for Blazor circuits.
// ABOUTME: Supports trusted slug forwarding from the BFF without resolving tenant identity in the UI host.

using Microsoft.AspNetCore.Http;

namespace Explore.Blazor.Services;

public class TenantRouteContextAccessor : ITenantRouteContextAccessor
{
    public const string TenantSlugItemKey = "__tenant_slug";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _tenantSlug;

    public TenantRouteContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? TenantSlug
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.TryGetValue(TenantSlugItemKey, out var value) == true && value is string tenantSlug)
            {
                return tenantSlug;
            }

            return _tenantSlug;
        }
    }

    public void SetTenantSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return;
        }

        _tenantSlug = slug.Trim();

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items[TenantSlugItemKey] = _tenantSlug;
        }
    }

    public void Clear()
    {
        _tenantSlug = null;

        var httpContext = _httpContextAccessor.HttpContext;
        httpContext?.Items.Remove(TenantSlugItemKey);
    }
}
