// ABOUTME: Configures Cookie authentication and registers the DynamicAuthSchemeManager for the BFF.
// ABOUTME: No longer hardcodes Keycloak — OIDC schemes are registered dynamically from DB/env at startup.

using Explore.Blazor.Authentication;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;

namespace Explore.Blazor.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures BFF authentication with Cookie scheme (always present) and registers
    /// the <see cref="DynamicAuthSchemeManager"/> for runtime OIDC/OAuth scheme registration.
    /// Provider-specific schemes (Keycloak, Google, ATProto) are registered dynamically
    /// from database configuration and environment variables after the app starts.
    /// </summary>
    public static IServiceCollection AddBffAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        LogEnvironmentKeycloakStatus(configuration);

        services.AddAuthentication(options =>
            {
                // Cookie is always the default scheme (reads session from cookie).
                // DefaultChallengeScheme is also Cookie — its LoginPath="/login" redirects
                // unauthenticated users to the multi-provider login page.
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;

                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

        // Register the ATProto auth handler options so the scheme can be added dynamically.
        // The handler itself is registered by DynamicAuthSchemeManager when ATProto is enabled.
        services.AddOptions<AtprotoAuthenticationOptions>(Explore.Blazor.Constants.AuthSchemeNames.Atproto);

        // DynamicAuthSchemeManager is a singleton — it holds the set of registered schemes
        // and uses IAuthenticationSchemeProvider + IOptionsMonitorCache for runtime registration.
        services.AddSingleton<IDynamicAuthSchemeManager, DynamicAuthSchemeManager>();

        return services;
    }

    /// <summary>
    /// Initializes dynamic auth schemes from database and environment configuration.
    /// Must be called after <c>app.Build()</c> during application startup.
    /// </summary>
    public static async Task InitializeDynamicAuthSchemesAsync(this WebApplication app)
    {
        var schemeManager = app.Services.GetRequiredService<IDynamicAuthSchemeManager>();
        await schemeManager.InitializeAsync();
    }

    private static void LogEnvironmentKeycloakStatus(IConfiguration configuration)
    {
        var logger = Log.ForContext("SourceContext", "Startup");

        var authority = configuration["Keycloak:Authority"];
        var clientId = configuration["Keycloak:ClientId"];
        var clientSecret = configuration["Keycloak:ClientSecret"];

        if (!string.IsNullOrEmpty(authority) && !string.IsNullOrEmpty(clientId))
        {
            logger.Information(
                "Keycloak environment config detected — Authority: {Authority}, ClientId: {ClientId}, HasSecret: {HasSecret}. " +
                "Will be registered as auth scheme during initialization.",
                authority, clientId, !string.IsNullOrEmpty(clientSecret) ? "YES" : "NO");
        }
        else
        {
            logger.Information(
                "No Keycloak environment config detected — authentication providers will be configured during instance setup.");
        }
    }
}
