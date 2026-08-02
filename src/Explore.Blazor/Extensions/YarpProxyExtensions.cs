// ABOUTME: Adapts Explore.Blazor host services into the shared Event.Web.BffHosting API proxy.
// ABOUTME: Keeps host-specific token, tenant, setup-secret, and support-session resolution outside the shared library.

using Event.Web.BffHosting.Abstractions;
using Event.Web.BffHosting.Proxy;
using Explore.Blazor.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Explore.Blazor.Extensions;

public static class YarpProxyExtensions
{
    public static IServiceCollection AddBffTrustedRequestEnrichment(this IServiceCollection services)
    {
        services.TryAddScoped<IEventBffAccessTokenProvider, ExploreBffAccessTokenProvider>();
        services.TryAddScoped<IEventBffTenantHintProvider, ExploreBffTenantHintProvider>();
        services.TryAddScoped<IEventBffSetupSecretProvider, ExploreBffSetupSecretProvider>();
        services.TryAddScoped<IEventBffSupportAccessProvider, ExploreBffSupportAccessProvider>();
        services.TryAddScoped<Event.Web.BffHosting.Security.EventBffRequestEnricher>();
        return services;
    }

    public static IServiceCollection AddBffReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        return services
            .AddBffTrustedRequestEnrichment()
            .AddEventApiProxy(configuration, environment);
    }
}
