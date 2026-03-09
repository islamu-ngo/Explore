// ABOUTME: Registers tiered rate limiting policies for the API.
// ABOUTME: Provides global IP-based, authenticated user, and write-operation rate limit tiers.

using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;

namespace Explore.API.Extensions;

/// <summary>
/// Configures tiered rate limiting policies.
/// - Global: IP-based token bucket for all requests
/// - Authenticated: Per-user sliding window for authenticated endpoints
/// - Write: Stricter per-user limit for POST/PUT/DELETE operations
/// - SetupSecret: Existing fixed window for instance bootstrap
///
/// All limits are configurable via appsettings.json under "RateLimiting".
/// When behind a reverse proxy (e.g., ngrok, Cloudflare), the global limiter
/// reads from X-Forwarded-For to identify the real client IP.
/// </summary>
public static class RateLimitingExtensions
{
    public const string GlobalPolicy = "Global";
    public const string AuthenticatedPolicy = "Authenticated";
    public const string WritePolicy = "Write";
    public const string SetupSecretPolicy = "SetupSecret";
    public const string AnalyticsRelayPolicy = "AnalyticsRelay";

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Disable rate limiting in test environments to prevent 429s during parallel test execution
        if (environment.EnvironmentName == "Testing")
        {
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("test"));

                options.AddPolicy(AuthenticatedPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
                options.AddPolicy(WritePolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
                options.AddPolicy(SetupSecretPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
                options.AddPolicy(AnalyticsRelayPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
            });

            return services;
        }

        var section = configuration.GetSection("RateLimiting");

        // Global limits (defaults if config absent)
        var globalTokenLimit = section.GetValue("Global:TokenLimit", 200);
        var globalReplenishPeriodSeconds = section.GetValue("Global:ReplenishPeriodSeconds", 10);
        var globalTokensPerPeriod = section.GetValue("Global:TokensPerPeriod", 40);

        // Authenticated limits
        var authPermitLimit = section.GetValue("Authenticated:PermitLimit", 200);
        var authWindowSeconds = section.GetValue("Authenticated:WindowSeconds", 60);
        var authSegments = section.GetValue("Authenticated:SegmentsPerWindow", 4);

        // Write limits
        var writePermitLimit = section.GetValue("Write:PermitLimit", 30);
        var writeWindowSeconds = section.GetValue("Write:WindowSeconds", 60);

        // Analytics relay limits
        var analyticsRelayPermitLimit = section.GetValue("AnalyticsRelay:PermitLimit", 120);
        var analyticsRelayWindowSeconds = section.GetValue("AnalyticsRelay:WindowSeconds", 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (ctx, token) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    ctx.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                ctx.HttpContext.Response.Headers["X-RateLimit-Limit"] = "0";
                ctx.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";

                await ctx.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit exceeded. Please retry after the period indicated in the Retry-After header."
                }, token);
            };

            // Global limiter: token bucket per IP
            // Supports X-Forwarded-For for reverse proxy deployments (ngrok, Cloudflare, etc.)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var remoteIp = ResolveClientIp(httpContext);

                if (remoteIp is not null && IPAddress.IsLoopback(remoteIp))
                {
                    return RateLimitPartition.GetNoLimiter(remoteIp.ToString());
                }

                return RateLimitPartition.GetTokenBucketLimiter(
                    remoteIp?.ToString() ?? "unknown",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = globalTokenLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(globalReplenishPeriodSeconds),
                        TokensPerPeriod = globalTokensPerPeriod,
                        AutoReplenishment = true
                    });
            });

            // Authenticated: sliding window per user identity
            options.AddPolicy(AuthenticatedPolicy, httpContext =>
            {
                var userId = httpContext.User.Identity?.Name ?? "anonymous";

                return RateLimitPartition.GetSlidingWindowLimiter(userId, _ =>
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromSeconds(authWindowSeconds),
                        SegmentsPerWindow = authSegments,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // Write operations: stricter fixed window per user
            options.AddPolicy(WritePolicy, httpContext =>
            {
                var userId = httpContext.User.Identity?.Name ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter($"write:{userId}", _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = writePermitLimit,
                        Window = TimeSpan.FromSeconds(writeWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            // Setup secret: preserve existing policy (fixed window per IP)
            options.AddPolicy(SetupSecretPolicy, httpContext =>
            {
                var ip = ResolveClientIp(httpContext)?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter($"setup:{ip}", _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.AddPolicy(AnalyticsRelayPolicy, httpContext =>
            {
                var ip = ResolveClientIp(httpContext)?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter($"analytics-relay:{ip}", _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = analyticsRelayPermitLimit,
                        Window = TimeSpan.FromSeconds(analyticsRelayWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Resolves the real client IP, accounting for reverse proxies.
    /// Checks X-Forwarded-For first (for ngrok, Cloudflare, etc.), falls back to RemoteIpAddress.
    /// </summary>
    private static IPAddress? ResolveClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For may contain multiple IPs: "client, proxy1, proxy2"
            var firstIp = forwardedFor.Split(',', StringSplitOptions.TrimEntries)[0];
            if (IPAddress.TryParse(firstIp, out var parsed))
            {
                return parsed;
            }
        }

        return context.Connection.RemoteIpAddress;
    }
}
