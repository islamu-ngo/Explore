// ABOUTME: Registers server-specific services on top of the shared application services.
// ABOUTME: Shared services live in Explore.Blazor.Client.Extensions.ServiceCollectionExtensions.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Blazor.Client.Configuration;
using Explore.Blazor.Client.Contracts.Interop;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Extensions;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Services;
using Explore.Infrastructure.Services;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;

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
        services.AddScoped<ICookieConsentInterop, ServerCookieConsentInterop>();
        services.AddScoped<Explore.Blazor.Client.Services.CookieConsentStateService>();
        services.AddScoped<ICircuitAccessTokenService, CircuitAccessTokenService>();
        services.AddSingleton<SetupSecretSessionService>();
        services.AddSingleton<ISetupSecretSessionService>(sp => sp.GetRequiredService<SetupSecretSessionService>());
        services.AddMemoryCache();
        RegisterResolverConfigDataServices(services, configuration);
        services.AddScoped<IResolverConfigService, ResolverConfigService>();
        services.AddScoped<ITenantRouteContextAccessor, TenantRouteContextAccessor>();
        services.AddScoped<CircuitHandler, TenantCircuitHandler>();

        // BFF admin claims transformation — calls the API to resolve admin authority
        services.AddScoped<BffAdminClaimsTransformation>();
        services.AddScoped<IClaimsTransformation>(
            sp => sp.GetRequiredService<BffAdminClaimsTransformation>());

        // Multi-tenancy configuration
        services.Configure<TenantConfiguration>(
            configuration.GetSection("Explore:MultiTenancy"));

        return services;
    }

    private static void RegisterResolverConfigDataServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:DefaultConnection"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in configuration.");
        }

        services.AddPooledDbContextFactory<ExploreDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ExploreDbContext>>();
            return factory.CreateDbContext();
        });

        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
    }
}
