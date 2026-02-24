// ABOUTME: Centralizes all HttpClient registrations for the Blazor BFF server.
// ABOUTME: Eliminates repeated ConfigurePrimaryHttpMessageHandler blocks for dev cert bypass.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Contracts;
using Explore.Blazor.Services;

namespace Explore.Blazor.Extensions;

public static class HttpClientExtensions
{
    /// <summary>
    /// Registers all API-facing HttpClient instances used by the Blazor BFF server.
    /// Each client calls the Event API directly with access token forwarding.
    /// </summary>
    public static IServiceCollection AddApiHttpClients(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var apiBaseUrl = NormalizeBaseUrl(
            configuration["ExploreApi:BaseUrl"] ?? "https://localhost:7039/");

        services.AddTransient<AccessTokenForwardingHandler>();

        // Named "BffClient" — used by raw HTTP services (InstanceOnboarding, TenantOnboarding, etc.)
        services.AddApiClient("BffClient", apiBaseUrl, environment);

        // Named "BffSelfClient" — used by InteractiveServer components calling BFF endpoints on this server.
        // No BaseAddress here; components set it from NavigationManager.BaseUri at runtime.
        services.AddHttpClient("BffSelfClient")
            .ConfigureDevCertBypass(environment);

        // Named "S3Upload" — used by ImageStorageService for presigned URL uploads
        services.AddHttpClient("S3Upload", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Typed NSwag-generated API client
        services.AddTypedApiClient<IEventApiClient, EventApiClient>(apiBaseUrl, environment);

        // Typed services that need direct API access during InteractiveServer rendering
        services.AddTypedApiClient<ITenantNavigationService, TenantNavigationService>(apiBaseUrl, environment);
        services.AddTypedApiClient<IGroupService, GroupService>(apiBaseUrl, environment);

        // Admin claims transformation client (shorter timeout, no token forwarding handler)
        services.AddHttpClient(BffAdminClaimsTransformation.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        }).ConfigureDevCertBypass(environment);

        return services;
    }

    private static IHttpClientBuilder AddApiClient(
        this IServiceCollection services,
        string name,
        string baseUrl,
        IWebHostEnvironment environment)
    {
        return services.AddHttpClient(name, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<AccessTokenForwardingHandler>()
        .ConfigureDevCertBypass(environment);
    }

    private static IHttpClientBuilder AddTypedApiClient<TInterface, TImplementation>(
        this IServiceCollection services,
        string baseUrl,
        IWebHostEnvironment environment)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return services.AddHttpClient<TInterface, TImplementation>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<AccessTokenForwardingHandler>()
        .ConfigureDevCertBypass(environment);
    }

    /// <summary>
    /// In development, bypasses SSL certificate validation for localhost.
    /// Eliminates the repeated ConfigurePrimaryHttpMessageHandler blocks.
    /// </summary>
    private static IHttpClientBuilder ConfigureDevCertBypass(
        this IHttpClientBuilder builder,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            builder.ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    var isLocalhost = message.RequestUri?.Host.Contains("localhost") ?? false;
                    return isLocalhost || errors == System.Net.Security.SslPolicyErrors.None;
                };
                return handler;
            });
        }

        return builder;
    }

    private static string NormalizeBaseUrl(string url)
    {
        return url.EndsWith('/') ? url : url + "/";
    }
}
