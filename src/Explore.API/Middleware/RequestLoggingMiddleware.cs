// ABOUTME: Logs structured metadata for every HTTP request (method, path, status, duration, user, tenant).
// ABOUTME: Designed for observability; never logs sensitive data such as headers or bodies.

using System.Diagnostics;
using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Routing;

namespace Explore.API.Middleware;

/// <summary>
/// Logs structured metadata for every HTTP request.
/// Records method, path, status code, duration, user ID, and tenant ID.
/// Sensitive data (authorization headers, request/response bodies) is never logged.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContextAccessor tenantContextAccessor)
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            var correlationId = context.Items["CorrelationId"] as string;
            if (TryGetAdmissionRouteIdentity(context, out string routeIdentity))
            {
                _logger.LogInformation(
                    "HTTP {Method} {Route} responded {StatusCode} in {ElapsedMs:0.00}ms | CorrelationId={CorrelationId}",
                    context.Request.Method,
                    routeIdentity,
                    context.Response.StatusCode,
                    elapsed.TotalMilliseconds,
                    correlationId ?? "-");
            }
            else
            {
                var userId = context.User?.FindFirst("sub")?.Value
                    ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var tenantId = tenantContextAccessor.TenantId?.ToString();
                var tenantSlug = context.Request.Headers[
                    Explore.Application.Constants.TenantHeaderNames.TenantSlug].FirstOrDefault();
                var authHeaderPresent = context.Request.Headers.ContainsKey("Authorization");
                var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;

                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:0.00}ms | User={UserId} Authenticated={IsAuthenticated} AuthHeaderPresent={AuthHeaderPresent} Tenant={TenantId} TenantSlug={TenantSlug} CorrelationId={CorrelationId}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    elapsed.TotalMilliseconds,
                    userId ?? "-",
                    isAuthenticated,
                    authHeaderPresent,
                    tenantId ?? "-",
                    tenantSlug ?? "-",
                    correlationId ?? "-");
            }
        }
    }

    internal static bool TryGetAdmissionRouteIdentity(HttpContext context, out string routeIdentity)
    {
        routeIdentity = string.Empty;
        if (context.GetEndpoint() is not RouteEndpoint endpoint)
            return false;

        string pattern = endpoint.RoutePattern.RawText ?? string.Empty;
        if (!pattern.Contains("/admission/", StringComparison.OrdinalIgnoreCase))
            return false;

        routeIdentity = "/" + pattern.Trim('/').ToLowerInvariant();
        return true;
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
