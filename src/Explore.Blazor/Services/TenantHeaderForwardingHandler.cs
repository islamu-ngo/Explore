// ABOUTME: DelegatingHandler that forwards tenant context headers to outgoing API requests.
// ABOUTME: Adds X-Tenant-Slug and X-Forwarded-Host so the API can resolve the correct tenant.

using Event.Web.BffHosting.Security;
using Explore.Blazor.Client.Contracts.Services;

namespace Explore.Blazor.Services;

/// <summary>
/// Forwards the current tenant slug and host to outgoing API requests via HTTP headers.
/// </summary>
public class TenantHeaderForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantRouteContextAccessor _tenantRouteContextAccessor;

    public TenantHeaderForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        ITenantRouteContextAccessor tenantRouteContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantRouteContextAccessor = tenantRouteContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var tenantSlug = _tenantRouteContextAccessor.TenantSlug;
        if (!request.Headers.Contains(EventBffHeaderNames.TenantSlug) &&
            !string.IsNullOrWhiteSpace(tenantSlug))
        {
            request.Headers.Add(EventBffHeaderNames.TenantSlug, tenantSlug);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var forwardedHost = httpContext?.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(forwardedHost))
        {
            forwardedHost = httpContext?.Request.Host.Value;
        }

        if (!request.Headers.Contains("X-Forwarded-Host") && !string.IsNullOrWhiteSpace(forwardedHost))
        {
            request.Headers.Add("X-Forwarded-Host", forwardedHost);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
