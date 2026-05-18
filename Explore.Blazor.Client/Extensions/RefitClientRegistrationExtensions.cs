using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Blazor.Client.Services.Http;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Explore.Blazor.Client.Extensions;

/// <summary>
/// Extensions for registering Refit clients securely.
/// </summary>
public static class RefitClientRegistrationExtensions
{
    /// <summary>
    /// Creates the default Refit settings matching the application's JSON serialization standards.
    /// Preserves camelCase, enum string mapping, and problem details.
    /// </summary>
    public static RefitSettings CreateDefaultRefitSettings()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Important: Use strings for enums
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
        };
    }

    /// <summary>
    /// Registers a secure Refit client for BFF (Backend-for-Frontend) endpoints.
    /// Guarantees that cookies, unauthorized handlers, and antiforgery tokens are applied.
    /// </summary>
    /// <typeparam name="TInterface">The Refit API interface</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configureClient">Optional configuration for the underlying HttpClient</param>
    public static IHttpClientBuilder AddBffRefitClient<TInterface>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient>? configureClient = null) where TInterface : class
    {
        var builder = services.AddRefitClient<TInterface>(CreateDefaultRefitSettings());

        builder.ConfigureHttpClient((sp, client) =>
        {
            // If no explicit base address is configured, try to resolve a default
            // from the environment in a subsequent configuration step or here.
            // By default, BFF calls are same-origin.
            configureClient?.Invoke(sp, client);
        })
        // 1. Ensure cookies/credentials are included for authentication
        .AddHttpMessageHandler<BrowserCredentialsMessageHandler>()
        // 2. Add CSRF token for mutating requests
        .AddHttpMessageHandler<BffAntiforgeryMessageHandler>()
        // 3. Catch 401s and trigger re-auth
        .AddHttpMessageHandler<BffUnauthorizedHandler>();

        return builder;
    }
}
