// ABOUTME: Registers BFF-specific rate limiting policies for sensitive browser-facing endpoints.
// ABOUTME: Keys setup-secret throttling by authenticated user or antiforgery/session cookie before falling back to IP.

using System.Globalization;
using System.Threading.RateLimiting;

namespace Explore.Blazor.Extensions;

public static class RateLimitingExtensions
{
    public const string SetupSecretPolicy = "BffSetupSecret";

    public static IServiceCollection AddBffRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var disableInTesting = configuration.GetValue("RateLimiting:DisableInTesting", true);
        if (environment.EnvironmentName == "Testing" && disableInTesting)
        {
            services.AddRateLimiter(options =>
            {
                options.AddPolicy(SetupSecretPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
            });

            return services;
        }

        var section = configuration.GetSection("RateLimiting:SetupSecret");
        var permitLimit = section.GetValue("PermitLimit", 5);
        var windowSeconds = section.GetValue("WindowSeconds", 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc6585#section-4",
                    Title = "Too Many Requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Too many setup-secret attempts. Please retry after the period indicated in the Retry-After header."
                }, cancellationToken);
            };

            options.AddPolicy(SetupSecretPolicy, httpContext =>
            {
                var partitionKey = ResolveSetupSecretPartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    private static string ResolveSetupSecretPartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sid")?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"setup:user:{userId}";
        }

        var antiforgeryCookie = context.Request.Cookies["XSRF-TOKEN"];
        if (!string.IsNullOrWhiteSpace(antiforgeryCookie))
        {
            return $"setup:xsrf:{antiforgeryCookie}";
        }

        var setupSecretCookie = context.Request.Cookies["setup-secret"];
        if (!string.IsNullOrWhiteSpace(setupSecretCookie))
        {
            return $"setup:secret:{setupSecretCookie}";
        }

        return $"setup:ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
