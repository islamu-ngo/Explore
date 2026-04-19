// ABOUTME: Centralizes middleware pipeline configuration and graceful shutdown for the Blazor BFF server.
// ABOUTME: Extracts forwarded headers, XSRF token distribution, startup redirect, and access token capture.

using Explore.Application.Contracts.Services;
using Explore.Blazor.Client.Pages;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Middleware;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;

namespace Explore.Blazor.Extensions;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Configures forwarded headers for reverse proxy / SSL termination (Coolify, Nginx).
    /// Restores the original request scheme and client IP from X-Forwarded-* headers.
    /// </summary>
    public static WebApplication UseForwardedHeadersMiddleware(this WebApplication app)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        // Clear defaults to trust all proxies — required for containerized environments
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        app.UseForwardedHeaders(options);

        var logger = app.Logger;
        app.Use((context, next) => LogForwardedHeadersAsync(context, next, logger));

        return app;
    }

    /// <summary>
    /// Distributes XSRF tokens via cookie on GET requests for the BFF antiforgery pattern.
    /// </summary>
    public static WebApplication UseAntiforgeryTokenMiddleware(this WebApplication app)
    {
        var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
        var secureCookie = !app.Environment.IsDevelopment();
        app.Use((ctx, next) => DistributeAntiforgeryTokenAsync(ctx, next, antiforgery, secureCookie));

        return app;
    }

    /// <summary>
    /// Redirects "/" to "/setup" when onboarding is incomplete, and vice versa.
    /// Resolves the startup gate before endpoint routing to avoid ambiguous "/" matches.
    /// </summary>
    public static WebApplication UseStartupRedirectMiddleware(this WebApplication app)
    {
        var logger = app.Logger;
        app.Use((ctx, next) => HandleStartupRedirectAsync(ctx, next, logger));

        return app;
    }

    public static WebApplication UsePathTenantResolverMiddleware(this WebApplication app)
    {
        app.UseMiddleware<PathTenantResolverMiddleware>();
        return app;
    }

    /// <summary>
    /// Captures the access token during the HTTP request pipeline for use in Blazor circuits.
    /// Avoids .GetAwaiter().GetResult() anti-pattern in synchronous component code.
    /// </summary>
    public static WebApplication UseAccessTokenCaptureMiddleware(this WebApplication app)
    {
        app.Use(CaptureAccessTokenAsync);
        return app;
    }

    /// <summary>
    /// Logs unauthenticated requests to protected BFF API endpoints for diagnostics.
    /// </summary>
    public static WebApplication UseBffDiagnosticsMiddleware(this WebApplication app)
    {
        var logger = app.Logger;
        app.Use((ctx, next) => LogUnauthenticatedBffRequestsAsync(ctx, next, logger));

        return app;
    }

    /// <summary>
    /// Registers graceful shutdown handlers for zero-downtime deployments.
    /// SIGTERM: 25 second grace period. SIGINT: Immediate shutdown.
    /// </summary>
    public static WebApplication ConfigureGracefulShutdown(
        this WebApplication app,
        GracefulShutdownState shutdownState)
    {
        app.Lifetime.ApplicationStopping.Register(() => OnApplicationStopping(app.Logger, shutdownState));
        Console.CancelKeyPress += (sender, e) => OnCancelKeyPress(app.Logger, shutdownState, e);
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnProcessExit(app.Logger, shutdownState);

        return app;
    }

    // --- Private middleware handlers -------------------------------------------------

    // Debug-level logging for auth-related paths; extracted from inline lambda to satisfy arch Rule 1.03.
    private static async Task LogForwardedHeadersAsync(HttpContext context, Func<Task> next, ILogger logger)
    {
        if (context.Request.Path.StartsWithSegments("/auth") ||
            context.Request.Path.StartsWithSegments("/login") ||
            context.Request.Path.StartsWithSegments("/logout"))
        {
            logger.LogDebug(
                "[ForwardedHeaders] Path: {Path}, Scheme: {Scheme}, Host: {Host}, Proto Header: {Proto}",
                context.Request.Path,
                context.Request.Scheme,
                context.Request.Host,
                context.Request.Headers["X-Forwarded-Proto"].ToString());
        }

        await next();
    }

    // Stores the XSRF request token in a non-HttpOnly cookie so the SPA can echo it as a header.
    private static async Task DistributeAntiforgeryTokenAsync(
        HttpContext ctx,
        Func<Task> next,
        IAntiforgery antiforgery,
        bool secureCookie)
    {
        if (HttpMethods.IsGet(ctx.Request.Method))
        {
            var tokens = antiforgery.GetAndStoreTokens(ctx);
            if (!string.IsNullOrEmpty(tokens.RequestToken))
            {
                ctx.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = secureCookie,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                });
            }
        }

        await next();
    }

    // Gates "/" and "/setup" against onboarding status so the landing page matches the tenant state.
    private static async Task HandleStartupRedirectAsync(HttpContext ctx, Func<Task> next, ILogger logger)
    {
        if (HttpMethods.IsGet(ctx.Request.Method) &&
            (string.Equals(ctx.Request.Path.Value, "/", StringComparison.Ordinal) ||
             string.Equals(ctx.Request.Path.Value, "/setup", StringComparison.Ordinal)))
        {
            var isCompleted = false;
            try
            {
                var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
                var statusClient = clientFactory.CreateClient("BffClient");
                var status = await statusClient.GetFromJsonAsync<InstanceOnboardingStatusModel>(
                    "api/InstanceOnboarding/status");
                isCompleted = status?.IsCompleted == true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to resolve instance onboarding status for startup redirect.");
            }

            if (string.Equals(ctx.Request.Path.Value, "/", StringComparison.Ordinal) && !isCompleted)
            {
                ctx.Response.Redirect("/setup");
                return;
            }

            if (string.Equals(ctx.Request.Path.Value, "/setup", StringComparison.Ordinal) && isCompleted)
            {
                ctx.Response.Redirect("/");
                return;
            }
        }

        await next();
    }

    // Surfaces the OIDC access token into HttpContext.Items and the per-circuit token service.
    private static async Task CaptureAccessTokenAsync(HttpContext ctx, Func<Task> next)
    {
        if (ctx.User?.Identity?.IsAuthenticated == true)
        {
            var accessToken = await ctx.GetTokenAsync("access_token");

            if (!string.IsNullOrEmpty(accessToken))
            {
                ctx.Items["AccessToken"] = accessToken;

                var tokenService = ctx.RequestServices.GetService<ICircuitAccessTokenService>();
                tokenService?.SetToken(accessToken);
            }
        }

        await next();
    }

    // Emits an Information-level log when anonymous traffic reaches an authorized BFF endpoint.
    private static async Task LogUnauthenticatedBffRequestsAsync(HttpContext ctx, Func<Task> next, ILogger logger)
    {
        if (ctx.Request.Path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = ctx.GetEndpoint();
            var requiresAuth = endpoint?.Metadata
                .GetMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>() != null;

            if (requiresAuth && ctx.User?.Identity?.IsAuthenticated != true)
            {
                logger.LogInformation(
                    "BFF: unauthenticated request to protected endpoint {Method} {Path}",
                    ctx.Request.Method, ctx.Request.Path);
            }
        }

        await next();
    }

    // --- Graceful shutdown handlers --------------------------------------------------

    private static void OnApplicationStopping(ILogger logger, GracefulShutdownState shutdownState)
    {
        shutdownState.IsShuttingDown = true;
        logger.LogInformation(
            "SIGTERM received. Starting graceful shutdown. Health checks return 503. " +
            "Accepting requests for {Seconds} more seconds...",
            GracefulShutdownState.GracePeriodSeconds);
    }

    private static void OnCancelKeyPress(
        ILogger logger,
        GracefulShutdownState shutdownState,
        ConsoleCancelEventArgs e)
    {
        logger.LogWarning("SIGINT received. Initiating immediate shutdown...");
        e.Cancel = false;
        shutdownState.CancellationTokenSource.Cancel();
        Environment.Exit(0);
    }

    private static void OnProcessExit(ILogger logger, GracefulShutdownState shutdownState)
    {
        if (!shutdownState.IsShuttingDown)
        {
            shutdownState.IsShuttingDown = true;
            logger.LogInformation(
                "Process exit signal received. Graceful shutdown with {Seconds} second grace period...",
                GracefulShutdownState.GracePeriodSeconds);

            Thread.Sleep(TimeSpan.FromSeconds(GracefulShutdownState.GracePeriodSeconds));
        }
    }
}

/// <summary>
/// Encapsulates mutable state needed by graceful shutdown handlers and health checks.
/// </summary>
public sealed class GracefulShutdownState
{
    public const int GracePeriodSeconds = 25;
    public bool IsShuttingDown { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; } = new();
}
