// ABOUTME: Registers tiered rate limiting policies for the API.
// ABOUTME: Provides global, authenticated, write, and control-plane rate/concurrency tiers.

using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Explore.API.Authentication;
using Explore.API.ExceptionHandling;
using Explore.Application.Authentication;
using Explore.Application.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Extensions;

/// <summary>
/// Configures tiered rate limiting policies.
/// - Global: IP-based token bucket for all requests
/// - Authenticated: Per-user sliding window for authenticated endpoints
/// - Write: Stricter per-user limit for POST/PUT/DELETE operations
/// - SetupSecret: Existing fixed window for instance bootstrap
///
/// All limits are configurable via appsettings.json under "RateLimiting".
/// When behind a reverse proxy, client IP comes from HttpContext.Connection.RemoteIpAddress
/// after trusted forwarded headers have been applied by the main API pipeline.
/// </summary>
public static class RateLimitingExtensions
{
    public const string GlobalPolicy = "Global";
    public const string AuthenticatedPolicy = "Authenticated";
    public const string WritePolicy = "Write";
    public const string PublicIngestionPolicy = "PublicIngestion";
    public const string SetupSecretPolicy = "SetupSecret";
    public const string AnalyticsRelayPolicy = "AnalyticsRelay";
    public const string AiAssistantPolicy = "AiAssistant";
    public const string ControlPlanePolicy = "ControlPlane";

    private const string ControlPlanePathPrefix = "/api/admin/control-plane";

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var disableInTesting = configuration.GetValue("RateLimiting:DisableInTesting", true);

        // Disable rate limiting in test environments to prevent 429s during parallel test execution
        if (environment.EnvironmentName == "Testing" && disableInTesting)
        {
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("test"));

                options.AddPolicy(GlobalPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
                options.AddPolicy(AuthenticatedPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
                options.AddPolicy(WritePolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
                options.AddPolicy(PublicIngestionPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
                options.AddPolicy(SetupSecretPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
                options.AddPolicy(AnalyticsRelayPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
                options.AddPolicy(AiAssistantPolicy, _ =>
                    RateLimitPartition.GetNoLimiter<string>("test"));
                options.AddPolicy(ControlPlanePolicy, _ =>
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

        var publicIngestionPermitLimit = section.GetValue("PublicIngestion:PermitLimit", 60);
        var publicIngestionWindowSeconds = section.GetValue("PublicIngestion:WindowSeconds", 60);

        // Analytics relay limits
        var analyticsRelayPermitLimit = section.GetValue("AnalyticsRelay:PermitLimit", 120);
        var analyticsRelayWindowSeconds = section.GetValue("AnalyticsRelay:WindowSeconds", 60);

        // AI assistant send limits
        var aiAssistantPermitLimit = section.GetValue("AiAssistant:PermitLimit", 12);
        var aiAssistantWindowSeconds = section.GetValue("AiAssistant:WindowSeconds", 60);

        // Setup-secret bootstrap limits
        var setupSecretPermitLimit = section.GetValue("SetupSecret:PermitLimit", 5);
        var setupSecretWindowSeconds = section.GetValue("SetupSecret:WindowSeconds", 60);

        var controlPlanePermitLimit = section.GetValue("ControlPlane:PermitLimit", 60);
        var controlPlaneWindowSeconds = section.GetValue("ControlPlane:WindowSeconds", 60);
        var controlPlaneConcurrencyLimit = section.GetValue("ControlPlane:ConcurrencyLimit", 4);
        var controlPlaneQueueLimit = section.GetValue("ControlPlane:QueueLimit", 0);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (ctx, token) =>
            {
                var policyName = InferPolicyName(ctx.HttpContext);
                var apiKeyPrincipal = ctx.HttpContext.User.TryGetApiKeyPrincipalContext();
                if (apiKeyPrincipal is not null)
                {
                    var metrics = ctx.HttpContext.RequestServices.GetRequiredService<BusinessMetrics>();
                    metrics.RecordExternalApiKeyThrottle(
                        policyName.ToLowerInvariant(),
                        apiKeyPrincipal.TenantId?.ToString() ?? "platform",
                        apiKeyPrincipal.OwnerType.ToString());

                    var loggerFactory = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("ExternalApiKeyRateLimiting");
                    logger.LogWarning(
                        "External API key {KeyId} for tenant {TenantId} was throttled by rate-limit policy {Policy} on {Path}.",
                        apiKeyPrincipal.KeyId,
                        apiKeyPrincipal.TenantId?.ToString() ?? "platform",
                        policyName,
                        ctx.HttpContext.Request.Path);
                }

                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    ctx.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                ctx.HttpContext.Response.Headers["X-RateLimit-Limit"] = ResolvePolicyLimit(policyName).ToString(NumberFormatInfo.InvariantInfo);
                ctx.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";

                var problemDetailsService = ctx.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = ctx.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc6585#section-4",
                        Title = "Too Many Requests",
                        Status = StatusCodes.Status429TooManyRequests,
                        Detail = "Rate limit exceeded. Please retry after the period indicated in the Retry-After header.",
                        Instance = ctx.HttpContext.Request.Path,
                        Extensions =
                        {
                            ["code"] = ApiProblemCodes.RateLimited
                        }
                    }
                });
            };

            // API-key callers are partitioned by authenticated key id; other callers remain IP-based.
            // RemoteIpAddress is already proxy-aware when UseForwardedHeaders is configured.
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(CreateControlPlaneConcurrencyPartition),
                PartitionedRateLimiter.Create<HttpContext, string>(CreateGlobalPartition));
            options.AddPolicy(GlobalPolicy, CreateGlobalPartition);

            // Authenticated: sliding window per user identity
            options.AddPolicy(AuthenticatedPolicy, httpContext =>
            {
                var userId = GetAuthenticatedPartitionKey(httpContext);

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
                var userId = GetAuthenticatedPartitionKey(httpContext);

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

            options.AddPolicy(PublicIngestionPolicy, httpContext =>
            {
                var ip = ResolveClientIp(httpContext)?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter($"public-ingestion:{ip}", _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = publicIngestionPermitLimit,
                        Window = TimeSpan.FromSeconds(publicIngestionWindowSeconds),
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
                        PermitLimit = setupSecretPermitLimit,
                        Window = TimeSpan.FromSeconds(setupSecretWindowSeconds),
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

            options.AddPolicy(AiAssistantPolicy, httpContext =>
            {
                var userId = GetAuthenticatedPartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter($"ai-assistant:{userId}", _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = aiAssistantPermitLimit,
                        Window = TimeSpan.FromSeconds(aiAssistantWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.AddPolicy(ControlPlanePolicy, httpContext =>
            {
                var userId = GetAuthenticatedPartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter($"control-plane:{userId}", _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = controlPlanePermitLimit,
                        Window = TimeSpan.FromSeconds(controlPlaneWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;

        RateLimitPartition<string> CreateGlobalPartition(HttpContext httpContext)
        {
            if (IsControlPlaneRequest(httpContext))
            {
                return RateLimitPartition.GetNoLimiter(ControlPlanePolicy);
            }

            string? managedInstancePartition = GetManagedInstancePartitionKey(httpContext);
            if (managedInstancePartition is not null)
            {
                return RateLimitPartition.GetTokenBucketLimiter(
                    managedInstancePartition,
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = globalTokenLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(globalReplenishPeriodSeconds),
                        TokensPerPeriod = globalTokensPerPeriod,
                        AutoReplenishment = true
                    });
            }

            var apiKeyId = httpContext.User.GetApiKeyId();
            if (!string.IsNullOrWhiteSpace(apiKeyId))
            {
                return RateLimitPartition.GetTokenBucketLimiter(
                    $"api-key:{apiKeyId}",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = globalTokenLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(globalReplenishPeriodSeconds),
                        TokensPerPeriod = globalTokensPerPeriod,
                        AutoReplenishment = true
                    });
            }

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
        }

        RateLimitPartition<string> CreateControlPlaneConcurrencyPartition(HttpContext httpContext)
        {
            if (!IsControlPlaneRequest(httpContext))
            {
                return RateLimitPartition.GetNoLimiter("non-control-plane");
            }

            return RateLimitPartition.GetConcurrencyLimiter(ControlPlanePolicy, _ =>
                new ConcurrencyLimiterOptions
                {
                    PermitLimit = controlPlaneConcurrencyLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = controlPlaneQueueLimit
                });
        }

        int ResolvePolicyLimit(string policyName) => policyName switch
        {
            AuthenticatedPolicy => authPermitLimit,
            WritePolicy => writePermitLimit,
            PublicIngestionPolicy => publicIngestionPermitLimit,
            SetupSecretPolicy => setupSecretPermitLimit,
            AnalyticsRelayPolicy => analyticsRelayPermitLimit,
            AiAssistantPolicy => aiAssistantPermitLimit,
            ControlPlanePolicy => controlPlanePermitLimit,
            _ => globalTokenLimit
        };
    }

    /// <summary>
    /// Resolves the client IP after any trusted forwarded-header processing.
    /// </summary>
    private static IPAddress? ResolveClientIp(HttpContext context)
    {
        return context.Connection.RemoteIpAddress;
    }

    private static string InferPolicyName(HttpContext context)
    {
        if (IsControlPlaneRequest(context))
        {
            return ControlPlanePolicy;
        }

        if (context.Request.Path.StartsWithSegments("/api/setup", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments("/setup", StringComparison.OrdinalIgnoreCase))
        {
            return SetupSecretPolicy;
        }

        if (context.Request.Path.StartsWithSegments("/api/analytics", StringComparison.OrdinalIgnoreCase))
        {
            return AnalyticsRelayPolicy;
        }

        if (context.Request.Path.StartsWithSegments("/api/integrations", StringComparison.OrdinalIgnoreCase))
        {
            return PublicIngestionPolicy;
        }

        if (context.Request.Path.StartsWithSegments("/api/ai/assistant", StringComparison.OrdinalIgnoreCase))
        {
            return AiAssistantPolicy;
        }

        if (HttpMethods.IsPost(context.Request.Method)
            || HttpMethods.IsPut(context.Request.Method)
            || HttpMethods.IsPatch(context.Request.Method)
            || HttpMethods.IsDelete(context.Request.Method))
        {
            return WritePolicy;
        }

        return context.User.Identity?.IsAuthenticated == true ? AuthenticatedPolicy : GlobalPolicy;
    }

    private static bool IsControlPlaneRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments(ControlPlanePathPrefix, StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetAuthenticatedPartitionKey(HttpContext context)
    {
        string? managedInstancePartition = GetManagedInstancePartitionKey(context);
        if (managedInstancePartition is not null)
        {
            return managedInstancePartition;
        }

        var apiKeyId = context.User.GetApiKeyId();
        if (!string.IsNullOrWhiteSpace(apiKeyId))
        {
            return $"api-key:{apiKeyId}";
        }

        var userId = context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sid")
            ?? context.User.Identity?.Name;

        return string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId;
    }

    private static string? GetManagedInstancePartitionKey(HttpContext context)
    {
        string? rawManagedInstanceId = context.User.FindFirstValue(
            ManagedControlPlaneAuthenticationDefaults.ManagedInstanceIdClaim);
        return Guid.TryParse(rawManagedInstanceId, out Guid managedInstanceId)
            && managedInstanceId != Guid.Empty
                ? $"managed-instance:{managedInstanceId:D}"
                : null;
    }
}
