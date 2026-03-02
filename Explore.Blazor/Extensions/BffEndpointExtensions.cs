// ABOUTME: Maps all BFF server endpoints (auth, theme, setup-secret, upload-proxy, user info).
// ABOUTME: Supports multi-provider auth: challenge accepts ?provider= for Keycloak/Google/ATProto.

using System.Net.Http.Headers;
using Explore.Blazor.Client.Pages;
using Explore.Blazor.Constants;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;

namespace Explore.Blazor.Extensions;

public static class BffEndpointExtensions
{
    /// <summary>
    /// Maps authentication endpoints: /auth/challenge, /auth/login, /auth/signout, /auth/status.
    /// Also maps /auth/providers and /auth/debug (dev mode only).
    /// </summary>
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/auth/challenge", HandleChallengeAsync);

        app.MapGet("/auth/login", HandleLoginRedirect);

        app.MapGet("/auth/signout", HandleSignoutAsync);

        app.MapGet("/auth/status", HandleAuthStatus);

        app.MapGet("/auth/providers", HandleGetProviders);

        app.MapPost("/bff/auth/refresh-schemes", HandleRefreshSchemesAsync)
            .ExcludeFromDescription();

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/auth/debug", HandleAuthDebugAsync).RequireAuthorization();
        }

        return app;
    }

    /// <summary>
    /// Maps BFF endpoints: /bff/theme, /bff/me, /bff/setup-secret, /bff/storage/upload-proxy.
    /// </summary>
    public static WebApplication MapBffEndpoints(this WebApplication app)
    {
        app.MapPost("/bff/theme", HandleThemePreference)
            .ExcludeFromDescription();

        app.MapPost("/bff/storage/upload-proxy", HandleStorageUploadProxyAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();

        app.MapPost("/bff/setup-secret", HandleSetupSecretAsync)
            .ExcludeFromDescription();

        app.MapPost("/bff/setup-secret/sync", HandleSetupSecretSyncAsync)
            .ExcludeFromDescription();

        app.MapDelete("/bff/setup-secret", HandleDeleteSetupSecret)
            .ExcludeFromDescription();

        app.MapGet("/bff/me", HandleGetCurrentUser);

        return app;
    }

    // ──────────────────────────────────────────────
    // Auth endpoint handlers
    // ──────────────────────────────────────────────

    private static async Task HandleChallengeAsync(HttpContext ctx)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");
        var returnUrl = GetSafeReturnUrl(ctx, logger);
        var provider = ctx.Request.Query["provider"].ToString();

        logger.LogInformation(
            "[AuthEndpoints] /auth/challenge hit - Provider: {Provider}, Url: {Url}, ReturnUrl: {ReturnUrl}",
            provider, ctx.Request.GetDisplayUrl(), returnUrl);

        // Resolve which auth scheme to challenge
        var schemeName = ResolveProviderScheme(provider);
        if (string.IsNullOrEmpty(schemeName))
        {
            // No provider specified — try to find a single registered provider
            var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
            var registered = await schemeManager.GetRegisteredProviderSchemesAsync();

            if (registered.Count == 1)
            {
                schemeName = registered[0];
            }
            else
            {
                // Multiple or no providers — redirect to login page for selection
                ctx.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
                return;
            }
        }

        try
        {
            await ctx.ChallengeAsync(
                schemeName,
                new AuthenticationProperties { RedirectUri = returnUrl });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[AuthEndpoints] Error during {Provider} login challenge: {Error}",
                schemeName, ex.InnerException?.Message ?? ex.Message);

            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsJsonAsync(new { error = "Login failed. Please try again later." });
        }
    }

    private static Task HandleLoginRedirect(HttpContext ctx)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");
        var returnUrl = Uri.EscapeDataString(GetSafeReturnUrl(ctx, logger));
        var provider = ctx.Request.Query["provider"].ToString();

        var redirectUrl = string.IsNullOrEmpty(provider)
            ? $"/auth/challenge?returnUrl={returnUrl}"
            : $"/auth/challenge?provider={Uri.EscapeDataString(provider)}&returnUrl={returnUrl}";

        ctx.Response.Redirect(redirectUrl);
        return Task.CompletedTask;
    }

    private static async Task HandleSignoutAsync(HttpContext ctx)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");
        var returnUrl = GetSafeReturnUrl(ctx, logger);

        logger.LogInformation(
            "[AuthEndpoints] /auth/signout hit - Url: {Url} ReturnUrl: {ReturnUrl}",
            ctx.Request.GetDisplayUrl(), returnUrl);

        try
        {
            // Always sign out of the cookie session
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // If user authenticated via an OIDC provider, sign out of that too
            // (triggers RP-initiated logout at the IdP for Keycloak/Google)
            var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
            var registered = await schemeManager.GetRegisteredProviderSchemesAsync();

            foreach (var scheme in registered)
            {
                if (scheme is AuthSchemeNames.Keycloak or AuthSchemeNames.Google)
                {
                    try
                    {
                        await ctx.SignOutAsync(scheme,
                            new AuthenticationProperties { RedirectUri = returnUrl });
                        logger.LogInformation("[AuthEndpoints] Signed out of {Scheme}", scheme);
                        return; // OIDC signout handles redirect
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex,
                            "[AuthEndpoints] Could not sign out of {Scheme} — user may not have used this provider",
                            scheme);
                    }
                }
            }

            // Fallback: redirect manually if no OIDC signout performed
            ctx.Response.Redirect(returnUrl);
            logger.LogInformation("[AuthEndpoints] Signout completed (cookie only)");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AuthEndpoints] Error during signout");
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsJsonAsync(new { error = "Logout failed. Please try again later." });
        }
    }

    private static IResult HandleAuthStatus(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            return Results.Ok(new { isAuthenticated = true, name = ctx.User.Identity.Name });
        }

        return Results.Ok(new { isAuthenticated = false });
    }

    private static async Task<IResult> HandleAuthDebugAsync(
        IConfiguration config,
        IHttpClientFactory httpClientFactory)
    {
        var authority = config["Keycloak:Authority"];
        var metadataAddress = config["Keycloak:MetadataAddress"]
            ?? $"{authority}/.well-known/openid-configuration";

        var result = new Dictionary<string, object?>
        {
            ["authority"] = authority,
            ["metadataAddress"] = metadataAddress,
            ["hasClientId"] = !string.IsNullOrEmpty(config["Keycloak:ClientId"]),
            ["hasClientSecret"] = !string.IsNullOrEmpty(config["Keycloak:ClientSecret"])
        };

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var response = await httpClient.GetAsync(metadataAddress);
            result["discoveryStatus"] = (int)response.StatusCode;
            result["discoverySuccess"] = response.IsSuccessStatusCode;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                result["discoveryDocument"] = System.Text.Json.JsonSerializer.Deserialize<object>(content);
            }
            else
            {
                result["discoveryError"] = await response.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex)
        {
            result["discoveryError"] = ex.Message;
        }

        return Results.Ok(result);
    }

    // ──────────────────────────────────────────────
    // BFF endpoint handlers
    // ──────────────────────────────────────────────

    private static IResult HandleThemePreference(HttpContext ctx)
    {
        var theme = ctx.Request.Query["theme"].ToString();
        if (theme is "dark" or "light")
        {
            var isDev = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            ctx.Response.Cookies.Append("theme", theme, new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(365),
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = false,
                Secure = !isDev
            });
            return Results.Ok();
        }

        return Results.BadRequest();
    }

    private static async Task<IResult> HandleStorageUploadProxyAsync(
        HttpContext ctx,
        IHttpClientFactory clientFactory,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        const long maxUploadBytes = 10 * 1024 * 1024;
        var logger = loggerFactory.CreateLogger("StorageUploadProxy");

        if (!ctx.Request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "Request must be multipart/form-data." });
        }

        var form = await ctx.Request.ReadFormAsync(cancellationToken);
        var uploadUrl = form["uploadUrl"].ToString();
        var contentType = form["contentType"].ToString();
        var file = form.Files.GetFile("file");

        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "File is required." });
        }

        if (file.Length > maxUploadBytes)
        {
            return Results.BadRequest(new { error = "File exceeds max size (10MB)." });
        }

        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var uploadUri) ||
            !string.Equals(uploadUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "Invalid upload URL." });
        }

        var query = uploadUri.Query;
        if (!query.Contains("X-Amz-Algorithm", StringComparison.OrdinalIgnoreCase) ||
            !query.Contains("X-Amz-Signature", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Rejected upload proxy request for non-presigned URL host {Host}",
                uploadUri.Host);
            return Results.BadRequest(new { error = "Upload URL must be pre-signed." });
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;
        }

        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaTypeHeader))
        {
            return Results.BadRequest(new { error = "Invalid content type." });
        }

        try
        {
            using var s3Client = clientFactory.CreateClient("S3Upload");
            await using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = mediaTypeHeader;

            using var response = await s3Client.PutAsync(uploadUri, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "Upload proxy failed for host {Host}. Status={StatusCode}, Body={Body}",
                    uploadUri.Host, (int)response.StatusCode, responseBody);

                return Results.Json(
                    new { error = "Storage upload failed.", statusCode = (int)response.StatusCode },
                    statusCode: StatusCodes.Status502BadGateway);
            }

            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upload proxy exception for host {Host}", uploadUri.Host);
            return Results.Json(
                new { error = "Storage upload failed due to an internal proxy error." },
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> HandleSetupSecretAsync(HttpContext ctx)
    {
        var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var sessionService = ctx.RequestServices.GetRequiredService<ISetupSecretSessionService>();
        var payload = await ctx.Request.ReadFromJsonAsync<SetupSecretCookieRequest>();
        var secret = payload?.Secret?.Trim();

        if (string.IsNullOrWhiteSpace(secret))
        {
            return Results.BadRequest(new { error = "Setup secret is required." });
        }

        var validation = await ValidateSetupSecretAsync(ctx, secret, ctx.RequestAborted);
        if (!validation.IsValid)
        {
            ClearSetupSecret(ctx, sessionService, !env.IsDevelopment());
            return Results.Json(new { error = validation.Error }, statusCode: validation.StatusCode);
        }

        PersistSetupSecret(ctx, sessionService, secret, !env.IsDevelopment());
        return Results.Ok();
    }

    private static async Task<IResult> HandleSetupSecretSyncAsync(HttpContext ctx)
    {
        var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var sessionService = ctx.RequestServices.GetRequiredService<ISetupSecretSessionService>();
        var payload = await ctx.Request.ReadFromJsonAsync<SetupSecretCookieRequest>();
        var secret = payload?.Secret?.Trim();

        if (string.IsNullOrWhiteSpace(secret))
        {
            return Results.BadRequest(new { error = "Setup secret is required." });
        }

        var userId = ResolveUserId(ctx);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var validation = await ValidateSetupSecretAsync(ctx, secret, ctx.RequestAborted);
        if (!validation.IsValid)
        {
            ClearSetupSecret(ctx, sessionService, !env.IsDevelopment(), userId);
            return Results.Json(new { error = validation.Error }, statusCode: validation.StatusCode);
        }

        PersistSetupSecret(ctx, sessionService, secret, !env.IsDevelopment(), userId);
        return Results.Ok();
    }

    private static IResult HandleDeleteSetupSecret(HttpContext ctx)
    {
        var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var sessionService = ctx.RequestServices.GetRequiredService<ISetupSecretSessionService>();
        ClearSetupSecret(ctx, sessionService, !env.IsDevelopment());
        return Results.Ok();
    }

    private static IResult HandleGetCurrentUser(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var safeClaims = new[]
        {
            "preferred_username", "email", "name", "given_name", "family_name", "sub"
        };

        return Results.Ok(new
        {
            Name = ctx.User.Identity?.Name,
            Claims = ctx.User.Claims
                .Where(c => safeClaims.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
                .Select(c => new { c.Type, c.Value })
        });
    }

    // ──────────────────────────────────────────────
    // Multi-provider auth helpers
    // ──────────────────────────────────────────────

    private static async Task<IResult> HandleGetProviders(HttpContext ctx)
    {
        var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
        var registered = await schemeManager.GetRegisteredProviderSchemesAsync();

        var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
        var keycloakFromEnv = !string.IsNullOrEmpty(config["Keycloak:Authority"]);

        var providers = registered.Select(scheme => new
        {
            name = scheme,
            displayName = scheme switch
            {
                AuthSchemeNames.Keycloak => "Keycloak",
                AuthSchemeNames.Google => "Google",
                AuthSchemeNames.Atproto => "AT Protocol",
                _ => scheme
            },
            type = scheme switch
            {
                AuthSchemeNames.Atproto => "handle_input",
                _ => "button"
            },
            recommended = scheme == AuthSchemeNames.Keycloak && keycloakFromEnv
        });

        return Results.Ok(new { providers });
    }

    private static async Task<IResult> HandleRefreshSchemesAsync(HttpContext ctx)
    {
        // Pass the setup secret from cookie so the manager can call the internal endpoint
        // that returns credentials (client secrets) needed for OIDC scheme registration.
        var setupSecret = ctx.Request.Cookies["setup-secret"];
        var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
        await schemeManager.RefreshSchemesAsync(setupSecret);

        var registered = await schemeManager.GetRegisteredProviderSchemesAsync();
        return Results.Ok(new { refreshed = true, providers = registered });
    }

    /// <summary>
    /// Maps a provider query parameter to its auth scheme name.
    /// Returns null if the provider is unknown or not specified.
    /// </summary>
    private static string? ResolveProviderScheme(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return null;

        return provider.ToLowerInvariant() switch
        {
            "keycloak" => AuthSchemeNames.Keycloak,
            "google" => AuthSchemeNames.Google,
            "atproto" => AuthSchemeNames.Atproto,
            _ => null
        };
    }

    // ──────────────────────────────────────────────
    // Shared helpers
    // ──────────────────────────────────────────────

    private static string GetSafeReturnUrl(HttpContext ctx, ILogger logger)
    {
        var returnUrl = ctx.Request.Query["returnUrl"].ToString();

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (returnUrl.StartsWith('/') &&
            !returnUrl.StartsWith("//", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("/\\", StringComparison.Ordinal))
        {
            return returnUrl;
        }

        logger.LogWarning("[AuthEndpoints] Invalid returnUrl '{ReturnUrl}' - defaulting to /", returnUrl);
        return "/";
    }

    private static string? ResolveUserId(HttpContext ctx)
    {
        return ctx.User.FindFirst("sub")?.Value
            ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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

            if (response.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                return new SetupSecretValidationResult(
                    false, StatusCodes.Status410Gone,
                    body?.Error ?? "Setup already completed.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return new SetupSecretValidationResult(
                    false, StatusCodes.Status502BadGateway,
                    "Could not validate setup secret at this time.");
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
            sessionService.SetForUser(resolvedUserId, secret);
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
            sessionService.ClearForUser(resolvedUserId);
        }
    }

    // ──────────────────────────────────────────────
    // Internal DTOs (file-scoped equivalent in extension class)
    // ──────────────────────────────────────────────

    private sealed class SetupSecretCookieRequest
    {
        public string? Secret { get; set; }
    }

    private sealed class SetupSecretValidationResponse
    {
        public bool Valid { get; set; }
        public string? Error { get; set; }
    }

    private sealed record SetupSecretValidationResult(bool IsValid, int StatusCode, string Error);
}
