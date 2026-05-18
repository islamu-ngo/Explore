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
using Explore.Blazor.Client.Routing.Guards;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Http;
using Explore.Blazor.Services.Preferences;
using Explore.Blazor.Services;
using Explore.Infrastructure.Services;
using Explore.Persistence;
using Explore.Persistence.Extensions;
using Explore.Persistence.Repositories;
using Explore.Secrets.Bootstrap;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
        services.AddTransient<BrowserCredentialsMessageHandler>();
        services.AddTransient<BffAntiforgeryMessageHandler>();
        services.AddTransient<BffUnauthorizedHandler>();

        services.AddSharedApplicationServices();
        services.AddScoped<BffClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var navigation = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
            var http = factory.CreateClient("BffSelfClient");
            http.BaseAddress = new Uri(navigation.BaseUri);
            return new BffClient(http);
        });
        services.AddScoped<IBffClient>(sp => sp.GetRequiredService<BffClient>());
        services.AddScoped<AuthenticatedRouteGuard>();
        services.AddScoped<MultiTenantOnboardingRouteGuard>();
        services.AddScoped<AdminRouteGuard>();
        services.AddScoped<TenantAdminRouteGuard>();
        services.AddScoped<OrgAdminRouteGuard>();
        services.AddScoped<GroupAdminRouteGuard>();

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
        services.AddScoped<ICircuitUserContext, CircuitUserContext>();
        services.AddScoped<IBffAuthCookieStore, BffAuthCookieStore>();
        services.AddSingleton<ICircuitTokenStore, CircuitTokenStore>();
        services.AddScoped<ICircuitAccessTokenService, CircuitAccessTokenService>();
        services.AddSingleton<SetupSecretSessionService>();
        services.AddSingleton<ISetupSecretSessionService>(sp => sp.GetRequiredService<SetupSecretSessionService>());
        services.Configure<SetupSecretResolverOptions>(options =>
        {
            options.DevelopmentSecret = configuration["Setup:Secret"]?.Trim()
                ?? configuration["Explore:Setup:Secret"]?.Trim()
                ?? configuration["SETUP_SECRET"]?.Trim();
        });
        services.AddSingleton<ISetupSecretCookieProtector, SetupSecretCookieProtector>();
        services.AddScoped<ISetupSecretResolver, SetupSecretResolver>();
        services.AddScoped<IStorageUploadSessionStore, StorageUploadSessionStore>();
        services.AddSingleton<IBffPreferenceCookieService, BffPreferenceCookieService>();
        services.AddSingleton<IBffPreferenceValidationService, BffPreferenceValidationService>();
        services.AddSingleton<IBffPreferenceForwardingService, BffPreferenceForwardingService>();
        services.AddMemoryCache();
        RegisterResolverConfigDataServices(services, configuration);
        services.AddScoped<IResolverConfigService, ResolverConfigService>();
        services.AddScoped<ITenantRouteContextAccessor, TenantRouteContextAccessor>();
        services.AddScoped<CircuitHandler, TenantCircuitHandler>();
        services.AddScoped<CircuitHandler, TokenCircuitHandler>();

        // BFF admin claims enrichment — invoked at cookie/session boundaries, not per request.
        services.AddScoped<BffAdminClaimsTransformation>();

        services.AddSingleton<IBffOnboardingStatusProvider, BffOnboardingStatusProvider>();

        // Multi-tenancy configuration
        services.Configure<TenantConfiguration>(
            configuration.GetSection("Explore:MultiTenancy"));

        return services;
    }

    private static void RegisterResolverConfigDataServices(IServiceCollection services, IConfiguration configuration)
    {
        // Precedence: explicit ConnectionStrings:DefaultConnection (tests / overrides)
        // -> BootstrapSecretLoader (Infisical -> POSTGRESQL_* env -> Postgresql:* config). No URL form.
        var connectionString = configuration["ConnectionStrings:DefaultConnection"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            using var bootstrapLoggerFactory = LoggerFactory.Create(static builder =>
            {
                builder.AddSimpleConsole(static options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss.fff ";
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });
            var bootstrapLogger = bootstrapLoggerFactory.CreateLogger("Explore.Blazor.Bootstrap");

            var credentials = BootstrapSecretLoader.LoadPostgresConnectionString(configuration, bootstrapLogger);
            connectionString = credentials.ConnectionString;
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

        services.AddExploreDataProtection(connectionString);
    }
}
