// ABOUTME: Registers the shared YARP API proxy and server-owned BFF request transforms.
// ABOUTME: Strips browser-controlled privileged headers before adding trusted token, tenant, setup, and support context.

using System.Net.Http.Headers;
using Event.Web.BffHosting.Abstractions;
using Event.Web.BffHosting.Authentication;
using Event.Web.BffHosting.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace Event.Web.BffHosting.Proxy;

public static class EventApiProxyExtensions
{
    public static IApplicationBuilder UseEventApiProxyAntiforgery(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(ValidateApiProxyAntiforgeryAsync);
    }

    public static IServiceCollection AddEventApiProxy(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.TryAddScoped<IEventBffAccessTokenProvider, AuthenticationTicketEventBffAccessTokenProvider>();
        services.TryAddScoped<IEventBffTenantHintProvider, NoopEventBffTenantHintProvider>();
        services.TryAddScoped<IEventBffSetupSecretProvider, NoopEventBffSetupSecretProvider>();
        services.TryAddScoped<IEventBffSupportAccessProvider, NoopEventBffSupportAccessProvider>();

        var apiBaseUrl = EventApiBaseAddressResolver.Resolve(configuration);

        var routes = new[]
        {
            new RouteConfig
            {
                RouteId = "event-api-vapid-public-key",
                ClusterId = "event-api",
                Match = new RouteMatch { Path = "/vapid-public-key" }
            },
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
                    DangerousAcceptAnyServerCertificate =
                        BffDevelopmentHostPolicy.IsDevelopmentTrustedBaseAddress(apiBaseUrl, environment)
                },
                HttpRequest = new ForwarderRequestConfig
                {
                    ActivityTimeout = TimeSpan.FromSeconds(65)
                }
            }
        };

        services.AddReverseProxy()
            .LoadFromMemory(routes, clusters)
            .AddTransforms(context =>
            {
                context.AddRequestTransform(async transformContext =>
                {
                    BffProxyHeaderSanitizer.RemoveBrowserControlledHeaders(transformContext.ProxyRequest);
                    await ForwardBearerTokenAsync(transformContext);
                    ForwardTenantHeaders(transformContext);
                    await ForwardSetupSecretAsync(transformContext);
                    await ForwardSupportAccessAsync(transformContext);
                });
            });

        return services;
    }

    private static async Task ValidateApiProxyAntiforgeryAsync(HttpContext context, Func<Task> next)
    {
        if (!RequiresAntiforgeryValidation(context))
        {
            await next();
            return;
        }

        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Antiforgery validation failed", context.RequestAborted);
            return;
        }

        await next();
    }

    private static bool RequiresAntiforgeryValidation(HttpContext context)
    {
        var request = context.Request;
        return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            && IsUnsafeMethod(request.Method)
            && !RequiresSetupSecret(request.Method, request.Path)
            && !IsAnonymousOnboardingPath(request.Path);
    }

    private static bool IsUnsafeMethod(string method)
    {
        return HttpMethods.IsPost(method)
            || HttpMethods.IsPut(method)
            || HttpMethods.IsPatch(method)
            || HttpMethods.IsDelete(method);
    }

    private static async Task ForwardBearerTokenAsync(RequestTransformContext context)
    {
        if (IsAnonymousOnboardingPath(context.HttpContext.Request.Path))
        {
            context.ProxyRequest.Headers.Authorization = null;
            return;
        }

        var provider = context.HttpContext.RequestServices.GetRequiredService<IEventBffAccessTokenProvider>();
        var token = await provider.ResolveAccessTokenAsync(
            context.HttpContext,
            context.HttpContext.RequestAborted);

        if (!string.IsNullOrWhiteSpace(token))
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
        context.ProxyRequest.Headers.Remove(EventBffHeaderNames.TenantId);
        context.ProxyRequest.Headers.Remove(EventBffHeaderNames.TenantSlug);

        var provider = context.HttpContext.RequestServices.GetRequiredService<IEventBffTenantHintProvider>();
        var tenantSlug = provider.ResolveTenantSlug(context.HttpContext);
        if (!string.IsNullOrWhiteSpace(tenantSlug))
        {
            context.ProxyRequest.Headers.Add(EventBffHeaderNames.TenantSlug, tenantSlug);
        }
    }

    private static async Task ForwardSetupSecretAsync(RequestTransformContext context)
    {
        var httpContext = context.HttpContext;

        _ = context.ProxyRequest.Headers.Remove(EventBffHeaderNames.SetupSecret);

        if (!RequiresSetupSecret(httpContext.Request.Method, httpContext.Request.Path))
        {
            return;
        }

        var provider = httpContext.RequestServices.GetRequiredService<IEventBffSetupSecretProvider>();
        var setupSecret = await provider.ResolveSetupSecretAsync(httpContext, httpContext.RequestAborted);
        if (!string.IsNullOrWhiteSpace(setupSecret))
        {
            context.ProxyRequest.Headers.Add(EventBffHeaderNames.SetupSecret, setupSecret);
        }
    }

    private static bool RequiresSetupSecret(string method, PathString path)
    {
        if (HttpMethods.IsPatch(method)
            && (string.Equals(
                    path.Value,
                    "/api/instance/settings/auth-provider",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    path.Value,
                    "/api/instance/settings/authz-provider",
                    StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return path.StartsWithSegments("/api/InstanceOnboarding/complete", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api/InstanceOnboarding/validate-secret", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(
                "/api/InstanceOnboarding/auth-provider-configuration/keycloak-bootstrap",
                StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(
                "/api/InstanceOnboarding/auth-provider-configuration",
                StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(
                "/api/InstanceOnboarding/authz-provider-configuration",
                StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ForwardSupportAccessAsync(RequestTransformContext context)
    {
        RemoveSupportAccessHeaders(context.ProxyRequest);

        var provider = context.HttpContext.RequestServices.GetRequiredService<IEventBffSupportAccessProvider>();
        var sessionId = await provider.ResolveSupportAccessSessionIdAsync(
            context.HttpContext,
            context.HttpContext.RequestAborted);

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            context.ProxyRequest.Headers.TryAddWithoutValidation(
                EventBffHeaderNames.SupportAccessSessionId,
                sessionId);
        }
    }

    private static void RemoveSupportAccessHeaders(HttpRequestMessage request)
    {
        var headerNames = request.Headers
            .Select(header => header.Key)
            .Where(EventBffHeaderNames.IsSupportAccessHeader)
            .ToArray();

        foreach (var headerName in headerNames)
        {
            _ = request.Headers.Remove(headerName);
        }
    }
}
