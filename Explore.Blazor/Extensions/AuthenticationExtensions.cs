// ABOUTME: Centralizes Cookie + OIDC (Keycloak) authentication configuration for the Blazor BFF server.
// ABOUTME: Extracts the ~120 lines of auth setup from Program.cs into a focused, testable extension.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Explore.Blazor.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures BFF authentication with Cookie + OpenID Connect (Keycloak).
    /// Includes PKCE, offline_access, sliding expiration, and OIDC event logging.
    /// </summary>
    public static IServiceCollection AddBffAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var keycloakAuthority = configuration["Keycloak:Authority"];
        var keycloakClientId = configuration["Keycloak:ClientId"];
        var keycloakClientSecret = configuration["Keycloak:ClientSecret"];
        var keycloakMetadataAddress = configuration["Keycloak:MetadataAddress"];

        LogKeycloakConfiguration(keycloakAuthority, keycloakClientId, keycloakClientSecret);

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
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
            })
            .AddOpenIdConnect(options =>
            {
                ConfigureOpenIdConnect(
                    options,
                    keycloakAuthority,
                    keycloakClientId,
                    keycloakClientSecret,
                    keycloakMetadataAddress,
                    configuration);
            });

        return services;
    }

    private static void ConfigureOpenIdConnect(
        OpenIdConnectOptions options,
        string? authority,
        string? clientId,
        string? clientSecret,
        string? metadataAddress,
        IConfiguration configuration)
    {
        var logger = Log.ForContext("SourceContext", "OIDC");

        options.Authority = authority;
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        if (!string.IsNullOrEmpty(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        options.RequireHttpsMetadata = string.Equals(
            configuration["Keycloak:RequireHttpsMetadata"],
            "true",
            StringComparison.OrdinalIgnoreCase);

        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username"
        };

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access");

        options.Events = CreateOidcEvents(logger);
    }

    private static OpenIdConnectEvents CreateOidcEvents(Serilog.ILogger logger)
    {
        return new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                logger.Debug("[OIDC] Redirecting to IdP. RedirectUri: {RedirectUri}",
                    context.ProtocolMessage.RedirectUri);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                logger.Error(context.Exception, "[OIDC] Authentication failed: {Error}",
                    context.Exception?.Message);
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                logger.Error("[OIDC] Remote failure: {Error}, Description: {Description}",
                    context.Failure?.Message,
                    context.Properties?.Items);

                if (context.HttpContext.Request.Query.TryGetValue("error", out var error))
                {
                    logger.Error("[OIDC] Keycloak error: {Error}", error);
                }
                if (context.HttpContext.Request.Query.TryGetValue("error_description", out var errorDesc))
                {
                    logger.Error("[OIDC] Keycloak error_description: {ErrorDesc}", errorDesc);
                }

                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                logger.Debug("[OIDC] Message received from IdP");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                logger.Debug("[OIDC] Token validated for user: {User}",
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    }

    private static void LogKeycloakConfiguration(
        string? authority,
        string? clientId,
        string? clientSecret)
    {
        var logger = Log.ForContext("SourceContext", "Startup");
        logger.Information("Keycloak Configuration:");
        logger.Information("  Authority: {Authority}", authority ?? "(not set)");
        logger.Information("  ClientId: {ClientId}", clientId ?? "(not set)");
        logger.Information("  ClientSecret: {HasSecret}",
            string.IsNullOrEmpty(clientSecret) ? "NO" : "YES");

        if (string.IsNullOrEmpty(authority) ||
            string.IsNullOrEmpty(clientId) ||
            string.IsNullOrEmpty(clientSecret))
        {
            logger.Error(
                "CRITICAL: Keycloak configuration is incomplete! Authentication will not work.");
        }
    }
}
