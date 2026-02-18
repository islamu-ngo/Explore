// ABOUTME: Propagates or generates a correlation ID for every request.
// ABOUTME: Pushes the correlation ID into Serilog LogContext and the response headers.

using Serilog.Context;

namespace Explore.API.Middleware;

/// <summary>
/// Ensures every request has a correlation ID for distributed tracing.
/// Reads from incoming X-Correlation-ID / X-Request-ID headers, generates one if absent,
/// pushes it into Serilog LogContext, and echoes it on the response.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string RequestIdHeader = "X-Request-ID";
    private const string LogPropertyName = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? context.Request.Headers[RequestIdHeader].FirstOrDefault()
            ?? context.TraceIdentifier;

        context.Items[LogPropertyName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(LogPropertyName, correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
