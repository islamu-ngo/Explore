// ABOUTME: Centralizes YARP reverse proxy configuration for the Blazor BFF server.
// ABOUTME: Handles route/cluster setup and request transforms (token, tenant, setup-secret forwarding).

using System.Net.Http.Headers;
using Explore.Application.Constants;
using Explore.Application.Contracts.Services;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Explore.Blazor.Extensions;

public static class YarpProxyExtensions
{
    /// <summary>
    /// Configures the YARP reverse proxy that forwards /api/* requests to the Event API.
    /// Includes transforms for Bearer token, Tenant-Id header, and X-Setup-Secret injection.
    /// </summary>
    public static IServiceCollection AddBffReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var apiBaseUrl = configuration["ExploreApi:BaseUrl"] ?? "https://localhost:7039/";
        if (!apiBaseUrl.EndsWith('/'))
        {
            apiBaseUrl += "/";
        }

        var routes = new[]
        {
            new RouteConfig
            {
                RouteId = "event-api",
                ClusterId = "event-api",
                Match = new RouteMatch { Path = "/api/{**catchall}" }
            }
        };

        var clusters = new[]
        {
            new ClusterConfig
            {
                ClusterId = "event-api",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["primary"] = new() { Address = apiBaseUrl }
                },
                HttpClient = new HttpClientConfig
                {
                    DangerousAcceptAnyServerCertificate = environment.IsDevelopment()
                }
            }
        };

        services.AddReverseProxy()
            .LoadFromMemory(routes, clusters)
            .AddTransforms(context =>
            {
                context.AddRequestTransform(async transformContext =>
                {
                    await ForwardBearerTokenAsync(transformContext);
                    ForwardTenantHeaders(transformContext);
                    await ForwardSetupSecretAsync(transformContext);
                });
            });

        return services;
    }

    private static async Task ForwardBearerTokenAsync(RequestTransformContext context)
    {
        var token = await context.HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
        {
            context.ProxyRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static void ForwardTenantHeaders(RequestTransformContext context)
    {
        context.ProxyRequest.Headers.Remove(TenantHeaderNames.TenantId);
        context.ProxyRequest.Headers.Remove(TenantHeaderNames.TenantSlug);

        var tenantRouteContextAccessor = context.HttpContext.RequestServices.GetRequiredService<ITenantRouteContextAccessor>();
        var tenantSlug = tenantRouteContextAccessor.TenantSlug;
        if (!string.IsNullOrWhiteSpace(tenantSlug))
        {
            context.ProxyRequest.Headers.Add(TenantHeaderNames.TenantSlug, tenantSlug);
        }
    }

    /// <summary>
    /// Strips the incoming X-Setup-Secret header to prevent client injection,
    /// then resolves the trusted value from header, cookie, or server-side session.
    /// </summary>
    private static async Task ForwardSetupSecretAsync(RequestTransformContext context)
    {
        var httpContext = context.HttpContext;

        // Strip first to prevent injection
        context.ProxyRequest.Headers.Remove("X-Setup-Secret");

        var setupSecret = httpContext.Request.Headers["X-Setup-Secret"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(setupSecret))
        {
            setupSecret = httpContext.Request.Cookies["setup-secret"];
        }

        if (string.IsNullOrWhiteSpace(setupSecret) &&
            httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var sessionService = httpContext.RequestServices
                    .GetRequiredService<ISetupSecretSessionService>();
                setupSecret = sessionService.GetForUser(userId);
            }
        }

        if (!string.IsNullOrWhiteSpace(setupSecret))
        {
            context.ProxyRequest.Headers.Add("X-Setup-Secret", setupSecret);
        }
    }
}
