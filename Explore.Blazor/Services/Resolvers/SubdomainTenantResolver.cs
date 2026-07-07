// ABOUTME: Resolves tenant IDs from host subdomains on the Blazor host using the shared slug cache.
// ABOUTME: Runs only when subdomain resolution is enabled in system resolver configuration.

using Event.Web.BffHosting.Abstractions;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;

namespace Explore.Blazor.Services.Resolvers;

public class SubdomainTenantResolver
{
    private readonly IEventBffHostClassifier _hostClassifier;
    private readonly ITenantSlugCache _tenantSlugCache;

    public SubdomainTenantResolver(ITenantSlugCache tenantSlugCache, IEventBffHostClassifier hostClassifier)
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

        if (!configuration.SubdomainEnabled || string.IsNullOrWhiteSpace(configuration.InstanceBaseDomain))
        {
            return null;
        }

        var host = NormalizeHost(httpContext.Request.Host.Host);
        var baseDomain = NormalizeHost(configuration.InstanceBaseDomain);
        if (host == null || baseDomain == null)
        {
            return null;
        }

        if (string.Equals(host, baseDomain, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = "." + baseDomain;
        if (!host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var subdomain = host[..^suffix.Length];
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return null;
        }

        return await _tenantSlugCache.GetTenantIdByDomainAsync(subdomain, cancellationToken);
    }

    private static string? NormalizeHost(string? host)
    {
        return string.IsNullOrWhiteSpace(host)
            ? null
            : host.Trim().TrimEnd('.').ToLowerInvariant();
    }
}
