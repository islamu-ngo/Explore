// ABOUTME: Centralizes all HttpClient registrations for the Blazor BFF server.
// ABOUTME: Eliminates repeated ConfigurePrimaryHttpMessageHandler blocks for dev cert bypass.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Contracts.Services.Footer;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Services;
using Microsoft.Extensions.Http.Resilience;
using Polly;

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
        services.AddTransient<TenantHeaderForwardingHandler>();
        services.AddTransient<SetupSecretForwardingHandler>();

        // Named "BffClient" — used by raw HTTP services (InstanceOnboarding, TenantOnboarding, etc.)
        services.AddApiClient("BffClient", apiBaseUrl, environment)
            .AddInteractiveResilience();

        // Named "BffSelfClient" — used by InteractiveServer components calling BFF endpoints on this server.
        // No BaseAddress here; components set it from NavigationManager.BaseUri at runtime.
        services.AddHttpClient("BffSelfClient")
            .ConfigureDevCertBypass(environment);

        // Named "S3Upload" — used by ImageStorageService for presigned URL uploads
        services.AddHttpClient("S3Upload", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .ConfigureDevCertBypass(environment)
        .AddBackgroundResilience();

        // Typed NSwag-generated API client
        services.AddTypedApiClient<IEventApiClient, EventApiClient>(apiBaseUrl, environment)
            .AddInteractiveResilience();

        // Typed services that need direct API access during InteractiveServer rendering
        services.AddTypedApiClient<ITenantNavigationService, TenantNavigationService>(apiBaseUrl, environment)
            .AddInteractiveResilience();
        services.AddTypedApiClient<IGroupService, GroupService>(apiBaseUrl, environment)
            .AddInteractiveResilience();
        services.AddTypedApiClient<IFooterAdminService, FooterAdminService>(apiBaseUrl, environment)
            .AddInteractiveResilience();
        services.AddTypedApiClient<ILocalizationAdminService, LocalizationAdminService>(apiBaseUrl, environment)
            .AddInteractiveResilience();

        // Admin claims transformation client (shorter timeout, no token forwarding handler)
        services.AddHttpClient(BffAdminClaimsTransformation.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        }).ConfigureDevCertBypass(environment)
          .AddAdminResilience();

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
        .AddHttpMessageHandler<TenantHeaderForwardingHandler>()
        .AddHttpMessageHandler<SetupSecretForwardingHandler>()
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
        .AddHttpMessageHandler<TenantHeaderForwardingHandler>()
        .AddHttpMessageHandler<SetupSecretForwardingHandler>()
        .ConfigureDevCertBypass(environment);
    }

    private static IHttpClientBuilder AddInteractiveResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromMilliseconds(250);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.DisableForUnsafeHttpMethods();
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5;
        });

        return builder;
    }

    private static IHttpClientBuilder AddAdminResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromMilliseconds(500);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.DisableForUnsafeHttpMethods();
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5;
        });

        return builder;
    }

    private static IHttpClientBuilder AddBackgroundResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
            options.Retry.MaxRetryAttempts = 4;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.DisableForUnsafeHttpMethods();
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            options.CircuitBreaker.FailureRatio = 0.5;
        });

        return builder;
    }

    /// <summary>
    /// In development, bypasses SSL certificate validation for localhost.
    /// Eliminates the repeated ConfigurePrimaryHttpMessageHandler blocks.
    /// </summary>
    private static IHttpClientBuilder ConfigureDevCertBypass(
        this IHttpClientBuilder builder,
        IWebHostEnvironment environment)
    {
        builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false
            };

            if (environment.IsDevelopment())
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    var isLocalhost = message.RequestUri?.Host.Contains("localhost") ?? false;
                    return isLocalhost || errors == System.Net.Security.SslPolicyErrors.None;
                };
            }

            return handler;
        });

        return builder;
    }

    private static string NormalizeBaseUrl(string url)
    {
        return url.EndsWith('/') ? url : url + "/";
    }
}
