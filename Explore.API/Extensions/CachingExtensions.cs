// ABOUTME: Registers output caching policies and HybridCache (L1+L2) for the API.
// ABOUTME: Provides 5 named output cache policies and configures in-memory + distributed hybrid caching.

using Explore.Application.Constants;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.API.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddApiCaching(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder.NoCache());

            // PublicData: truly public lookup endpoints (categories, tags, languages) — no auth variance needed
            options.AddPolicy("PublicData", builder => builder
                .Expire(TimeSpan.FromHours(1))
                .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host")
                .Tag("lookup-data"));

            // LookupData: kept for backward compatibility, same as PublicData
            options.AddPolicy("LookupData", builder => builder
                .Expire(TimeSpan.FromHours(1))
                .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host")
                .Tag("lookup-data"));

            // ListData: varies by Authorization for auth-aware HATEOAS links
            options.AddPolicy("ListData", builder => builder
                .Expire(TimeSpan.FromSeconds(30))
                .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host", "Authorization")
                .SetVaryByQuery("*")
                .Tag("list-data"));

            // DetailData: varies by Authorization for auth-aware HATEOAS links
            options.AddPolicy("DetailData", builder => builder
                .Expire(TimeSpan.FromSeconds(60))
                .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host", "Authorization")
                .SetVaryByRouteValue("id")
                .Tag("detail-data"));

            // TenantNav: tenant navigation links — short expiry, evicted on write by "tenant-nav" tag
            options.AddPolicy("TenantNav", builder => builder
                .Expire(TimeSpan.FromMinutes(5))
                .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host")
                .Tag("tenant-nav"));

            // SitemapData: public SEO sitemap — tenant/host aware, no auth variance.
            options.AddPolicy("SitemapData", builder => builder
                .Expire(TimeSpan.FromMinutes(30))
                .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host")
                .Tag("seo-sitemap"));
        });

        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024 * 10; // 10MB
            options.MaximumKeyLength = 512;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(30),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });

        return services;
    }
}
