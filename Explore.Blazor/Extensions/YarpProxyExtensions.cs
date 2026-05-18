// ABOUTME: Centralizes YARP reverse proxy configuration for the Blazor BFF server.
// ABOUTME: Handles route/cluster setup and request transforms (token, tenant, setup-secret forwarding).

using System.Net.Http.Headers;
using Explore.Application.Constants;
using Explore.Application.Contracts.Services;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
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
        var apiBaseUrl = ResolveApiBaseUrl(configuration);

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
                    DangerousAcceptAnyServerCertificate = IsDevelopmentTrustedHost(apiBaseUrl, environment)
                },
                HttpRequest = new ForwarderRequestConfig
                {
                    ActivityTimeout = TimeSpan.FromSeconds(30)
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

    private static bool IsDevelopmentTrustedHost(string baseAddress, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return false;
        }

        if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var destinationUri)
            || string.IsNullOrWhiteSpace(destinationUri.Host))
        {
            return false;
        }

        return IsDevelopmentTrustedHost(destinationUri.Host);
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
        if (!System.Net.IPAddress.TryParse(host, out var address))
        {
            return false;
        }

        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        // Tailscale/CGNAT range: 100.64.0.0/10
        return bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127;
    }

    private static string ResolveApiBaseUrl(IConfiguration configuration)
    {
        var explicitUrl = configuration["ExploreApi:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(explicitUrl))
        {
            return explicitUrl.EndsWith('/') ? explicitUrl : explicitUrl + "/";
        }

        var aspireHttps = configuration["services__explore-api__https__0"];
        if (!string.IsNullOrWhiteSpace(aspireHttps))
        {
            return aspireHttps.EndsWith('/') ? aspireHttps : aspireHttps + "/";
        }

        var aspireHttp = configuration["services__explore-api__http__0"];
        if (!string.IsNullOrWhiteSpace(aspireHttp))
        {
            return aspireHttp.EndsWith('/') ? aspireHttp : aspireHttp + "/";
        }

        return "https://localhost:7039/";
    }

    private static async Task ForwardBearerTokenAsync(RequestTransformContext context)
    {
        if (IsAnonymousOnboardingPath(context.HttpContext.Request.Path))
        {
            context.ProxyRequest.Headers.Authorization = null;
            return;
        }

        var token = await context.HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token) && CircuitTokenStore.IsTokenUsable(token))
        {
            context.ProxyRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static bool IsAnonymousOnboardingPath(PathString path)
    {
        if (!path.StartsWithSegments("/api/InstanceOnboarding", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.Value is null
            || !path.Value.EndsWith("/complete", StringComparison.OrdinalIgnoreCase);
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
    /// Strips any client-supplied X-Setup-Secret header and forwards only a BFF-resolved trusted value.
    /// </summary>
    private static async Task ForwardSetupSecretAsync(RequestTransformContext context)
    {
        var httpContext = context.HttpContext;

        _ = context.ProxyRequest.Headers.Remove("X-Setup-Secret");

        var resolver = httpContext.RequestServices.GetRequiredService<ISetupSecretResolver>();
        var setupSecret = resolver.Resolve(httpContext);
        if (setupSecret.Found && !string.IsNullOrWhiteSpace(setupSecret.Secret))
        {
            context.ProxyRequest.Headers.Add("X-Setup-Secret", setupSecret.Secret);
        }
    }
}
