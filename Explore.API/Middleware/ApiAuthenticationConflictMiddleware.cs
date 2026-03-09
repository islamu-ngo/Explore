// ABOUTME: Rejects API requests that send conflicting direct-auth credentials.
// ABOUTME: Keeps auth dispatch deterministic and fail-closed before authentication handlers run.

using Explore.Application.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Middleware;

public sealed class ApiAuthenticationConflictMiddleware
{
    private readonly RequestDelegate _next;

    public ApiAuthenticationConflictMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetailsService)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var hasAuthorizationHeader = context.Request.Headers.ContainsKey("Authorization");
        var hasApiKeyHeader = context.Request.Headers.ContainsKey(ApiAuthenticationHeaderNames.ApiKey);

        if (hasAuthorizationHeader && hasApiKeyHeader)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Conflicting authentication credentials",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Detail = "Send either Authorization or X-API-Key, but not both.",
                    Instance = context.Request.Path
                }
            });
            return;
        }

        await _next(context);
    }
}
