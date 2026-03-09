// ABOUTME: Resolves tenant IDs from exact custom domains on the Blazor host using the shared slug cache.
// ABOUTME: Runs only when custom-domain resolution is enabled in system resolver configuration.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;

namespace Explore.Blazor.Services.Resolvers;

public class CustomDomainTenantResolver
{
    private readonly ITenantSlugCache _tenantSlugCache;

    public CustomDomainTenantResolver(ITenantSlugCache tenantSlugCache)
    {
        _tenantSlugCache = tenantSlugCache;
    }

    public async Task<Guid?> ResolveAsync(HttpContext httpContext, ResolverConfigurationDto configuration, CancellationToken cancellationToken = default)
    {
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
