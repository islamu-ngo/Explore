// ABOUTME: Logs structured metadata for every HTTP request (method, path, status, duration, user, tenant).
// ABOUTME: Designed for observability; never logs sensitive data such as headers or bodies.

using System.Security.Claims;
using System.Diagnostics;
using Explore.Application.Constants;
using Explore.Application.Contracts.Services;

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

            var userId = context.User?.FindFirst("sub")?.Value
                ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User?.FindFirst("sid")?.Value;

            var tenantId = tenantContextAccessor.TenantId?.ToString();
            var tenantSlug = context.Request.Headers[TenantHeaderNames.TenantSlug].FirstOrDefault();
            var correlationId = context.Items["CorrelationId"] as string;
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

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
