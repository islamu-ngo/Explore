// ABOUTME: Resolves tenant IDs from exact custom domains on the Blazor host using the shared slug cache.
// ABOUTME: Runs only when custom-domain resolution is enabled in system resolver configuration.

using Event.Web.BffHosting.Abstractions;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;

namespace Explore.Blazor.Services.Resolvers;

public class CustomDomainTenantResolver
{
    private readonly IEventBffHostClassifier _hostClassifier;
    private readonly ITenantSlugCache _tenantSlugCache;

    public CustomDomainTenantResolver(ITenantSlugCache tenantSlugCache, IEventBffHostClassifier hostClassifier)
    {
        _tenantSlugCache = tenantSlugCache;
        _hostClassifier = hostClassifier;
    }

    public async Task<Guid?> ResolveAsync(HttpContext httpContext, ResolverConfigurationDto configuration, CancellationToken cancellationToken = default)
    {
        if (_hostClassifier.IsAdminHost(httpContext))
        {
            return null;
        }

        if (!configuration.CustomDomainEnabled || !configuration.AllowTenantCustomDomains)
        {
            return null;
        }

        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        return await _tenantSlugCache.GetTenantIdByDomainAsync(host, cancellationToken);
    }
}
