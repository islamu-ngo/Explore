// ABOUTME: Exposes shared browser-BFF hosting registration helpers for Event web hosts.
// ABOUTME: Keeps host profile and API proxy defaults consistent across public and control-plane hosts.

using Event.Web.BffHosting.Authentication;
using Event.Web.BffHosting.Options;
using Event.Web.BffHosting.Proxy;
using Event.Web.BffHosting.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Event.Web.BffHosting.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventBffHosting(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        EventBffHostProfile hostProfile)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var apiBaseAddress = EventApiBaseAddressResolver.Resolve(configuration);

        services.AddOptions<EventBffHostingOptions>()
            .Bind(configuration.GetSection(EventBffHostingOptions.SectionName))
            .PostConfigure(options =>
            {
                options.HostProfile = hostProfile;
                options.ApiBaseAddress = apiBaseAddress;
            });

        services.TryAddSingleton<ISafeAuthDiagnosticsPolicy, SafeAuthDiagnosticsPolicy>();
        services.TryAddSingleton<IEventBffOidcOptionsFactory, EventBffOidcOptionsFactory>();
        services.TryAddScoped<IEventBffCookieSessionHandler, NoopEventBffCookieSessionHandler>();
        services.TryAddScoped<EventBffTokenRefreshCookieEvents>();
        services.AddHttpClient(EventBffTokenRefreshCookieEvents.TokenRefreshHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        })
            .ConfigurePrimaryHttpMessageHandler(EventBffOidcOptionsFactory.CreateIpv4BackchannelHandler);

        _ = BffDevelopmentHostPolicy.IsDevelopmentTrustedBaseAddress(apiBaseAddress, environment);

        return services;
    }
}
