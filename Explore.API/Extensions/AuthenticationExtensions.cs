// ABOUTME: Registers multi-auth (JWT Bearer + API Key) authentication and authorization for the API.
// ABOUTME: Dispatches X-API-Key requests to the ApiKey handler; all others go through Keycloak JWT Bearer.

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Explore.API.Authentication;
using Explore.Application.Constants;
using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Explore.API.Extensions;

public static class AuthenticationExtensions
{
    // Security: audiences validated in both 'aud' and 'azp' claims (Keycloak BFF pattern)
    private static readonly string[] DefaultValidAudiences = ["islamu-event-api", "islamu-event-blazor"];

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Dynamic JWT authority: Authority / MetadataAddress / ValidIssuer are applied
        // by DynamicJwtBearerPostConfigureOptions from env + DB (IAuthProviderConfigurationService).
        // Handlers call IJwtAuthorityRefreshNotifier.ReloadAsync() after onboarding/save-config
        // to hot-swap Keycloak metadata without restarting the API.
        var validAudiences = configuration.GetSection("Keycloak:ValidAudiences").Get<string[]>()
            ?? configuration.GetSection("Authentication:ValidAudiences").Get<string[]>()
            ?? DefaultValidAudiences;
        var validAudienceSet = new HashSet<string>(validAudiences.Where(audience => !string.IsNullOrWhiteSpace(audience)), StringComparer.Ordinal);

        services.AddSingleton<DynamicJwtConfigurationService>();
        services.AddSingleton<IJwtAuthorityRefreshNotifier>(sp =>
            sp.GetRequiredService<DynamicJwtConfigurationService>());
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, DynamicJwtBearerPostConfigureOptions>();
        services.AddHostedService<JwtAuthorityWarmupHostedService>();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = ApiAuthenticationSchemeNames.MultiAuth;
                options.DefaultAuthenticateScheme = ApiAuthenticationSchemeNames.MultiAuth;
                options.DefaultChallengeScheme = ApiAuthenticationSchemeNames.MultiAuth;
            })
            .AddPolicyScheme(ApiAuthenticationSchemeNames.MultiAuth, ApiAuthenticationSchemeNames.MultiAuth, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    if (context.Request.Headers.ContainsKey(ApiAuthenticationHeaderNames.ApiKey))
                    {
                        return ApiAuthenticationSchemeNames.ApiKey;
                    }

                    return JwtBearerDefaults.AuthenticationScheme;
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = string.Equals(
                    configuration["Keycloak:RequireHttpsMetadata"],
                    "true",
                    StringComparison.OrdinalIgnoreCase
                );

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Security: custom audience check covers both 'aud' and 'azp' claims
                    // because Keycloak puts the client ID in 'azp' rather than 'aud'
                    ValidateAudience = true,
                    AudienceValidator = (audiences, securityToken, _) =>
                    {
                        var audienceList = audiences?.ToList() ?? new List<string>();

                        if (audienceList.Exists(validAudienceSet.Contains))
                        {
                            return true;
                        }

                        if (securityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtToken)
                        {
                            var azp = jwtToken.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;
                            if (!string.IsNullOrEmpty(azp) && validAudienceSet.Contains(azp))
                            {
                                return true;
                            }
                        }

                        return false;
                    },

                    ValidateIssuer = true,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),

                    ValidateIssuerSigningKey = true,

                    NameClaimType = "preferred_username"
                };

                options.BackchannelHttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                    PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
                    ConnectTimeout = TimeSpan.FromSeconds(10),
                    KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
                    SslOptions = environment.IsDevelopment()
                        ? new SslClientAuthenticationOptions
                        {
                            RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                                IsAllowedDevelopmentCertificate(sender, errors, environment)
                        }
                        : default,
                    ConnectCallback = async (context, cancellationToken) =>
                    {
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                        catch
                        {
                            socket.Dispose();
                            throw;
                        }
                    }
                };

                // Security: PII-safe logging — only exception messages, never token claims or raw values
                options.Events = new JwtBearerEvents
                {
                    // Performance: skip JWT token validation entirely for onboarding paths.
                    // When Keycloak is unreachable, OIDC metadata discovery times out (3+ minutes),
                    // blocking [AllowAnonymous] endpoints that don't need authentication at all.
                    // Only POST /complete requires JWT auth (has [Authorize]) and is excluded.
                    OnMessageReceived = context =>
                    {
                        var path = context.HttpContext.Request.Path;
                        if (path.StartsWithSegments("/api/instanceonboarding", StringComparison.OrdinalIgnoreCase)
                            && path.Value?.EndsWith("/complete", StringComparison.OrdinalIgnoreCase) != true)
                        {
                            context.Token = null;
                        }

                        return Task.CompletedTask;
                    },

                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning(
                            "[JWT] Authentication failed for {Method} {Path}: {Error}",
                            context.Request.Method,
                            context.Request.Path,
                            context.Exception?.Message);
                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogDebug(
                            "[JWT] Challenge issued for {Path}. Error: {Error}, Description: {Desc}",
                            context.Request.Path,
                            context.Error,
                            context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiAuthenticationSchemeNames.ApiKey,
                options =>
                {
                    configuration.GetSection(ApiKeyAuthenticationOptions.SectionName).Bind(options);

                    if (string.IsNullOrWhiteSpace(options.HeaderName))
                    {
                        options.HeaderName = ApiAuthenticationHeaderNames.ApiKey;
                    }
                });

        services.AddAuthorizationBuilder()
            .AddPolicy("template_admin", policy => policy.RequireAuthenticatedUser())
            .AddPolicy("event_editor", policy => policy.RequireAuthenticatedUser())
            .AddPolicy("property_governance_admin", policy => policy.RequireAuthenticatedUser())
            .AddPolicy("platform_namespace_editor", policy => policy.RequireAuthenticatedUser());

        return services;
    }

    private static bool IsAllowedDevelopmentCertificate(object? sender, SslPolicyErrors errors, IWebHostEnvironment environment)
    {
        if (errors == SslPolicyErrors.None)
        {
            return true;
        }

        if (!environment.IsDevelopment())
        {
            return false;
        }

        return sender is SslStream { TargetHostName: { } host }
               && IsDevelopmentTrustedHost(host);
    }

    private static bool IsDevelopmentTrustedHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("100.64.0.2", StringComparison.OrdinalIgnoreCase)
            || IsTailscaleAddress(host))
        {
            return true;
        }

        var additionalHosts = Environment.GetEnvironmentVariable("BFF_DEV_TRUSTED_HOSTS");
        if (string.IsNullOrWhiteSpace(additionalHosts))
        {
            return false;
        }

        return additionalHosts
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(h => host.Equals(h, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTailscaleAddress(string host)
    {
        if (!IPAddress.TryParse(host, out var address))
        {
            return false;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        // Tailscale/CGNAT range: 100.64.0.0/10
        return bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127;
    }
}
