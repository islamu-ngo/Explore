// ABOUTME: Adapts Explore.Blazor host services into the shared Event.Web.BffHosting API proxy.
// ABOUTME: Keeps host-specific token, tenant, setup-secret, and support-session resolution outside the shared library.

using Event.Web.BffHosting.Abstractions;
using Event.Web.BffHosting.Proxy;
using Explore.Blazor.Services;

namespace Explore.Blazor.Extensions;

public static class YarpProxyExtensions
{
    public static IServiceCollection AddBffReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddScoped<IEventBffAccessTokenProvider, ExploreBffAccessTokenProvider>();
        services.AddScoped<IEventBffTenantHintProvider, ExploreBffTenantHintProvider>();
        services.AddScoped<IEventBffSetupSecretProvider, ExploreBffSetupSecretProvider>();
        services.AddScoped<IEventBffSupportAccessProvider, ExploreBffSupportAccessProvider>();

        return services.AddEventApiProxy(configuration, environment);
    }
}
