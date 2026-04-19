// ABOUTME: Registers multi-auth (JWT Bearer + API Key) authentication and authorization for the API.
// ABOUTME: Dispatches X-API-Key requests to the ApiKey handler; all others go through Keycloak JWT Bearer.

using Explore.API.Authentication;
using Explore.Application.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Explore.API.Extensions;

public static class AuthenticationExtensions
{
    // Security: audiences validated in both 'aud' and 'azp' claims (Keycloak BFF pattern)
    private static readonly string[] ValidAudiences =
    [
        "islamu-event-api",
        "islamu-event-blazor"
    ];

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var authority = configuration["Keycloak:Authority"];

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

                options.Authority = authority;
                options.MetadataAddress = configuration["Keycloak:MetadataAddress"];

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Security: custom audience check covers both 'aud' and 'azp' claims
                    // because Keycloak puts the client ID in 'azp' rather than 'aud'
                    ValidateAudience = true,
                    AudienceValidator = (audiences, securityToken, _) =>
                    {
                        var audienceList = audiences?.ToList() ?? new List<string>();

                        if (audienceList.Exists(aud => ValidAudiences.Contains(aud)))
                        {
                            return true;
                        }

                        if (securityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtToken)
                        {
                            var azp = jwtToken.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;
                            if (!string.IsNullOrEmpty(azp) && ValidAudiences.Contains(azp))
                            {
                                return true;
                            }
                        }

                        return false;
                    },

                    ValidateIssuer = true,
                    ValidIssuer = authority,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),

                    ValidateIssuerSigningKey = true,

                    NameClaimType = "preferred_username"
                };

                if (environment.IsDevelopment())
                {
                    options.BackchannelHttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                }

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
                            && !path.Value.EndsWith("/complete", StringComparison.OrdinalIgnoreCase))
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

        services.AddAuthorizationBuilder();

        return services;
    }
}
