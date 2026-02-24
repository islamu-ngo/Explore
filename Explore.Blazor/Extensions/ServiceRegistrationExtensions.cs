// ABOUTME: Registers server-specific services on top of the shared application services.
// ABOUTME: Shared services live in Explore.Blazor.Client.Extensions.ServiceCollectionExtensions.

using Explore.Blazor.Client.Configuration;
using Explore.Blazor.Client.Extensions;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Contracts;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;

namespace Explore.Blazor.Extensions;

public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers all application-level services by calling the shared registrations
    /// from the Client project, then adding server-specific overrides.
    /// On the server, IGroupService and ITenantNavigationService are typed HttpClients
    /// (registered in HttpClientExtensions), so they are NOT included here.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSharedApplicationServices();

        return services;
    }

    /// <summary>
    /// Registers services that are only needed on the Blazor Server (BFF) side.
    /// These include token management, analytics no-ops, and admin claims enrichment.
    /// </summary>
    public static IServiceCollection AddServerOnlyServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Server-specific IAnalyticsInterop (no-op, replaces shared registration)
        services.AddScoped<IAnalyticsInterop, ServerAnalyticsInterop>();
        services.AddScoped<ICircuitAccessTokenService, CircuitAccessTokenService>();
        services.AddSingleton<ISetupSecretSessionService, SetupSecretSessionService>();

        // BFF admin claims transformation — calls the API to resolve admin authority
        services.AddScoped<BffAdminClaimsTransformation>();
        services.AddScoped<IClaimsTransformation>(
            sp => sp.GetRequiredService<BffAdminClaimsTransformation>());

        // Multi-tenancy configuration
        services.Configure<TenantConfiguration>(
            configuration.GetSection("Explore:MultiTenancy"));

        return services;
    }
}
