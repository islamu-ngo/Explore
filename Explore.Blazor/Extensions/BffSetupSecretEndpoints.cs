// ABOUTME: Setup-secret BFF endpoints: set, sync, and delete setup secrets.
// ABOUTME: Includes validation against the API, cookie management, and session persistence.

using Explore.Blazor.Services;
using Microsoft.AspNetCore.Mvc;

namespace Explore.Blazor.Extensions;

public static class BffSetupSecretEndpoints
{
    /// <summary>
    /// Maps setup-secret endpoints: POST /bff/setup-secret, POST /bff/setup-secret/sync, DELETE /bff/setup-secret.
    /// </summary>
    public static WebApplication MapSetupSecretEndpoints(this WebApplication app)
    {
        app.MapGet("/bff/setup-secret", HandleGetSetupSecretStatusAsync)
            .ExcludeFromDescription();

        // Note: antiforgery is intentionally omitted from setup-secret endpoints.
        // These run during initial instance bootstrap before any user session exists,
        // so there is no session to protect with CSRF. The setup secret itself serves
        // as the authorization credential and rate limiting provides abuse protection.
        // InteractiveServer Blazor components call these via server-to-server HTTP
        // (BffSelfClient) which cannot carry browser antiforgery cookies.
        app.MapPost("/bff/setup-secret", HandleSetupSecretAsync)
            .RequireRateLimiting(RateLimitingExtensions.SetupSecretPolicy)
            .ExcludeFromDescription();

        app.MapPost("/bff/setup-secret/sync", HandleSetupSecretSyncAsync)
            .RequireRateLimiting(RateLimitingExtensions.SetupSecretPolicy)
            .ExcludeFromDescription();

        app.MapDelete("/bff/setup-secret", HandleDeleteSetupSecret)
            .ExcludeFromDescription();

        return app;
    }

    // ──────────────────────────────────────────────
    // Endpoint handlers
    // ──────────────────────────────────────────────

    // NOTE: Handlers taking only HttpContext and returning Task<IResult> are silently
    // coerced to RequestDelegate (Func<HttpContext, Task>) by the minimal API factory,
    // causing IResult.ExecuteAsync to never run. We use direct response writing instead.
    private static async Task HandleGetSetupSecretStatusAsync(HttpContext ctx)
    {
        var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var sessionService = (ISetupSecretSessionService)ctx.RequestServices.GetRequiredService<SetupSecretSessionService>();
        var secret = ResolvePersistedSetupSecret(ctx, sessionService);

        if (string.IsNullOrWhiteSpace(secret))
        {
            await ctx.Response.WriteAsJsonAsync(new SetupSecretStatusResponse());
            return;
        }

        var validation = await ValidateSetupSecretAsync(ctx, secret, ctx.RequestAborted);
        if (!validation.IsValid)
        {
            ClearSetupSecret(ctx, sessionService, !env.IsDevelopment());
            await ctx.Response.WriteAsJsonAsync(new SetupSecretStatusResponse
            {
                HasPersistedSecret = false,
                IsValid = false,
                Error = validation.Error
            });
            return;
        }

        await ctx.Response.WriteAsJsonAsync(new SetupSecretStatusResponse
        {
            HasPersistedSecret = true,
            IsValid = true
        });
    }

    private static async Task HandleSetupSecretAsync(HttpContext ctx)
    {
        var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var sessionService = (ISetupSecretSessionService)ctx.RequestServices.GetRequiredService<SetupSecretSessionService>();
        var payload = await ctx.Request.ReadFromJsonAsync<SetupSecretCookieRequest>();
        var secret = payload?.Secret?.Trim();

        if (string.IsNullOrWhiteSpace(secret))
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "Setup secret is required."
            });
            return;
        }

        var validation = await ValidateSetupSecretAsync(ctx, secret, ctx.RequestAborted);
        if (!validation.IsValid)
        {
            ClearSetupSecret(ctx, sessionService, !env.IsDevelopment());
            ctx.Response.StatusCode = validation.StatusCode;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = validation.StatusCode,
                Title = "Setup Secret Validation Failed",
                Detail = validation.Error
            });
            return;
        }

        PersistSetupSecret(ctx, sessionService, secret, !env.IsDevelopment());
    }

    private static async Task HandleSetupSecretSyncAsync(HttpContext ctx)
    {
        var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var sessionService = (ISetupSecretSessionService)ctx.RequestServices.GetRequiredService<SetupSecretSessionService>();
        var payload = await ctx.Request.ReadFromJsonAsync<SetupSecretCookieRequest>();
        var userId = ResolveUserId(ctx);
        if (string.IsNullOrWhiteSpace(userId))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var secret = payload?.Secret?.Trim();
        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = ResolvePersistedSetupSecret(ctx, sessionService);
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "Setup secret is required."
            });
            return;
        }

        var validation = await ValidateSetupSecretAsync(ctx, secret, ctx.RequestAborted);
        if (!validation.IsValid)
        {
            ClearSetupSecret(ctx, sessionService, !env.IsDevelopment(), userId);
            ctx.Response.StatusCode = validation.StatusCode;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = validation.StatusCode,
                Title = "Setup Secret Validation Failed",
                Detail = validation.Error
            });
            return;
        }

        PersistSetupSecret(ctx, sessionService, secret, !env.IsDevelopment(), userId);
    }

    private static IResult HandleDeleteSetupSecret(HttpContext ctx)
    {
        var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var sessionService = (ISetupSecretSessionService)ctx.RequestServices.GetRequiredService<SetupSecretSessionService>();
        ClearSetupSecret(ctx, sessionService, !env.IsDevelopment());
        return Results.Ok();
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static string? ResolveUserId(HttpContext ctx)
    {
        return ctx.User.FindFirst("sub")?.Value
            ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? ctx.User.FindFirst("sid")?.Value;
    }

    private static string? ResolvePersistedSetupSecret(
        HttpContext ctx,
        ISetupSecretSessionService sessionService)
    {
        var setupSecret = ctx.Request.Cookies["setup-secret"];
        if (!string.IsNullOrWhiteSpace(setupSecret))
        {
            return setupSecret.Trim();
        }

        var userId = ResolveUserId(ctx);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return GetPersistedSecretForUser(sessionService, userId)?.Trim();
    }

    private static async Task<SetupSecretValidationResult> ValidateSetupSecretAsync(
        HttpContext ctx,
        string secret,
        CancellationToken cancellationToken)
    {
        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("SetupSecretGateway");

        try
        {
            var client = clientFactory.CreateClient("BffClient");
            var payload = new SetupSecretCookieRequest { Secret = secret };
            using var response = await client.PostAsJsonAsync(
                "api/InstanceOnboarding/validate-secret", payload, cancellationToken);

            SetupSecretValidationResponse? body = null;
            try
            {
                body = await response.Content.ReadFromJsonAsync<SetupSecretValidationResponse>(
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not parse setup secret validation response body.");
            }

            // Preserve upstream meaning: previously every non-2xx (incl. 429 from rate limiter and
            // 403 from invalid secret) was flattened to a generic 502, masking the real failure
            // and confusing UX during onboarding flows that re-validate frequently.
            if (response.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                return new SetupSecretValidationResult(
                    false, StatusCodes.Status410Gone,
                    body?.Error ?? "Setup already completed.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return new SetupSecretValidationResult(
                    false, StatusCodes.Status429TooManyRequests,
                    body?.Error ?? "Too many setup secret validation attempts. Please wait a minute and try again.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new SetupSecretValidationResult(
                    false, StatusCodes.Status403Forbidden,
                    body?.Error ?? "Invalid setup secret.");
            }

            if ((int)response.StatusCode >= 500)
            {
                return new SetupSecretValidationResult(
                    false, StatusCodes.Status502BadGateway,
                    "Could not validate setup secret at this time.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return new SetupSecretValidationResult(
                    false, (int)response.StatusCode,
                    body?.Error ?? "Setup secret validation failed.");
            }

            if (body?.Valid == true)
            {
                return new SetupSecretValidationResult(true, StatusCodes.Status200OK, string.Empty);
            }

            return new SetupSecretValidationResult(
                false, StatusCodes.Status400BadRequest,
                body?.Error ?? "Invalid setup secret.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Setup secret validation request failed.");
            return new SetupSecretValidationResult(
                false, StatusCodes.Status503ServiceUnavailable,
                "Could not validate setup secret at this time.");
        }
    }

    private static void PersistSetupSecret(
        HttpContext ctx,
        ISetupSecretSessionService sessionService,
        string secret,
        bool secureCookie,
        string? userId = null)
    {
        ctx.Response.Cookies.Append("setup-secret", secret, new CookieOptions
        {
            MaxAge = TimeSpan.FromMinutes(60),
            Path = "/",
            SameSite = SameSiteMode.Lax,
            HttpOnly = true,
            Secure = secureCookie
        });

        var resolvedUserId = string.IsNullOrWhiteSpace(userId) ? ResolveUserId(ctx) : userId;
        if (!string.IsNullOrWhiteSpace(resolvedUserId))
        {
            SetPersistedSecretForUser(sessionService, resolvedUserId, secret);
        }
    }

    private static void ClearSetupSecret(
        HttpContext ctx,
        ISetupSecretSessionService sessionService,
        bool secureCookie,
        string? userId = null)
    {
        ctx.Response.Cookies.Delete("setup-secret", new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.Lax,
            HttpOnly = true,
            Secure = secureCookie
        });

        var resolvedUserId = string.IsNullOrWhiteSpace(userId) ? ResolveUserId(ctx) : userId;
        if (!string.IsNullOrWhiteSpace(resolvedUserId))
        {
            ClearPersistedSecretForUser(sessionService, resolvedUserId);
        }
    }

    private static string? GetPersistedSecretForUser(ISetupSecretSessionService sessionService, string userId)
    {
        return typeof(ISetupSecretSessionService)
            .GetMethod(nameof(ISetupSecretSessionService.GetForUser), [typeof(string)])?
            .Invoke(sessionService, [userId]) as string;
    }

    private static void SetPersistedSecretForUser(ISetupSecretSessionService sessionService, string userId, string secret)
    {
        typeof(ISetupSecretSessionService)
            .GetMethod(nameof(ISetupSecretSessionService.SetForUser), [typeof(string), typeof(string)])?
            .Invoke(sessionService, [userId, secret]);
    }

    private static void ClearPersistedSecretForUser(ISetupSecretSessionService sessionService, string userId)
    {
        typeof(ISetupSecretSessionService)
            .GetMethod(nameof(ISetupSecretSessionService.ClearForUser), [typeof(string)])?
            .Invoke(sessionService, [userId]);
    }

    // ──────────────────────────────────────────────
    // Internal DTOs
    // ──────────────────────────────────────────────

    internal sealed class SetupSecretCookieRequest
    {
        public string? Secret { get; set; }
    }

    internal sealed class SetupSecretValidationResponse
    {
        public bool Valid { get; set; }
        public string? Error { get; set; }
    }

    internal sealed class SetupSecretStatusResponse
    {
        public bool HasPersistedSecret { get; set; }
        public bool IsValid { get; set; }
        public string? Error { get; set; }
    }

    internal sealed record SetupSecretValidationResult(bool IsValid, int StatusCode, string Error);
}
