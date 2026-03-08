// ABOUTME: Maps all BFF server endpoints (auth, theme, setup-secret, upload-proxy, user info).
// ABOUTME: Supports multi-provider auth: challenge accepts ?provider= for Keycloak/Google/ATProto.

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Explore.Blazor.Constants;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Explore.Blazor.Extensions;

public static class BffEndpointExtensions
{
    private static readonly Regex GoogleClientIdPattern = new(
        @"^[0-9]+-[a-zA-Z0-9\-]+\.apps\.googleusercontent\.com$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

        app.MapPost("/bff/language", HandleLanguagePreference)
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
        if (!string.IsNullOrEmpty(schemeName))
        {
            if (!await IsProviderReadyAsync(ctx, schemeName))
            {
                ctx.Response.Redirect(BuildLoginRedirectUrl(returnUrl, provider));
                return;
            }
        }
        else
        {
            // No provider specified — try to find a single registered provider
            var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
            var registered = await schemeManager.GetRegisteredProviderSchemesAsync();
            var readySchemes = new List<string>();

            foreach (var registeredScheme in registered)
            {
                if (await IsProviderReadyAsync(ctx, registeredScheme))
                {
                    readySchemes.Add(registeredScheme);
                }
            }

            if (readySchemes.Count == 1)
            {
                schemeName = readySchemes[0];
                provider = MapSchemeToProviderQueryValue(schemeName) ?? provider;
            }
            else
            {
                // Multiple or no providers — redirect to login page for selection
                ctx.Response.Redirect(BuildLoginRedirectUrl(returnUrl));
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

            ctx.Response.Redirect(BuildLoginRedirectUrl(returnUrl, provider, challengeError: true));
        }
    }

    private static async Task HandleLoginRedirect(HttpContext ctx)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");
        var returnUrl = Uri.EscapeDataString(GetSafeReturnUrl(ctx, logger));
        var provider = ctx.Request.Query["provider"].ToString();

        if (string.IsNullOrWhiteSpace(provider))
        {
            provider = await ResolvePreferredProviderForDirectLoginAsync(ctx);
        }

        var redirectUrl = string.IsNullOrEmpty(provider)
            ? $"/auth/challenge?returnUrl={returnUrl}"
            : $"/auth/challenge?provider={Uri.EscapeDataString(provider)}&returnUrl={returnUrl}";

        ctx.Response.Redirect(redirectUrl);
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

    private static IResult HandleLanguagePreference(HttpContext ctx)
    {
        var lang = ctx.Request.Query["lang"].ToString().Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(lang) && lang.Length is >= 2 and <= 5)
        {
            var isDev = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            ctx.Response.Cookies.Append("lang", lang, new CookieOptions
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

    // NOTE: Handlers taking only HttpContext and returning Task<IResult> are silently
    // coerced to RequestDelegate (Func<HttpContext, Task>) by the minimal API factory,
    // causing IResult.ExecuteAsync to never run. We use direct response writing instead.
    private static async Task HandleSetupSecretAsync(HttpContext ctx)
    {
        var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var sessionService = ctx.RequestServices.GetRequiredService<ISetupSecretSessionService>();
        var payload = await ctx.Request.ReadFromJsonAsync<SetupSecretCookieRequest>();
        var secret = payload?.Secret?.Trim();

        if (string.IsNullOrWhiteSpace(secret))
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsJsonAsync(new { error = "Setup secret is required." });
            return;
        }

        var validation = await ValidateSetupSecretAsync(ctx, secret, ctx.RequestAborted);
        if (!validation.IsValid)
        {
            ClearSetupSecret(ctx, sessionService, !env.IsDevelopment());
            ctx.Response.StatusCode = validation.StatusCode;
            await ctx.Response.WriteAsJsonAsync(new { error = validation.Error });
            return;
        }

        PersistSetupSecret(ctx, sessionService, secret, !env.IsDevelopment());
    }

    private static async Task HandleSetupSecretSyncAsync(HttpContext ctx)
    {
        var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var sessionService = ctx.RequestServices.GetRequiredService<ISetupSecretSessionService>();
        var payload = await ctx.Request.ReadFromJsonAsync<SetupSecretCookieRequest>();
        var secret = payload?.Secret?.Trim();

        if (string.IsNullOrWhiteSpace(secret))
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsJsonAsync(new { error = "Setup secret is required." });
            return;
        }

        var userId = ResolveUserId(ctx);
        if (string.IsNullOrWhiteSpace(userId))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var validation = await ValidateSetupSecretAsync(ctx, secret, ctx.RequestAborted);
        if (!validation.IsValid)
        {
            ClearSetupSecret(ctx, sessionService, !env.IsDevelopment(), userId);
            ctx.Response.StatusCode = validation.StatusCode;
            await ctx.Response.WriteAsJsonAsync(new { error = validation.Error });
            return;
        }

        PersistSetupSecret(ctx, sessionService, secret, !env.IsDevelopment(), userId);
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

    private static async Task HandleGetProviders(HttpContext ctx)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");

        try
        {
            var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
            var registered = await schemeManager.GetRegisteredProviderSchemesAsync();

            var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var keycloakFromEnv = !string.IsNullOrEmpty(config["Keycloak:Authority"]);

            var providers = new List<object>();

            foreach (var scheme in registered)
            {
                if (!await IsProviderReadyAsync(ctx, scheme))
                {
                    continue;
                }

                providers.Add(new
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
            }

            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { providers });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in HandleGetProviders");
            if (!ctx.Response.HasStarted)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(
                    new { error = ex.Message, providers = Array.Empty<object>() });
            }
        }
    }

    private static async Task<bool> IsProviderReadyAsync(HttpContext ctx, string scheme)
    {
        if (scheme == AuthSchemeNames.Atproto)
        {
            return true;
        }

        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");
        var optionsMonitor = ctx.RequestServices.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
        OpenIdConnectOptions options;

        try
        {
            options = optionsMonitor.Get(scheme);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not load OIDC options for scheme {Scheme}", scheme);
            return false;
        }

        if (scheme == AuthSchemeNames.Keycloak)
        {
            if (string.IsNullOrWhiteSpace(options.Authority) || string.IsNullOrWhiteSpace(options.ClientId))
            {
                return false;
            }

            var metadataAddress = string.IsNullOrWhiteSpace(options.MetadataAddress)
                ? $"{options.Authority.TrimEnd('/')}/.well-known/openid-configuration"
                : options.MetadataAddress;

            return await HasRequiredOidcMetadataAsync(ctx, metadataAddress, "keycloak", requireGoogleIssuer: false);
        }

        if (scheme == AuthSchemeNames.Google)
        {
            if (string.IsNullOrWhiteSpace(options.ClientId) || !GoogleClientIdPattern.IsMatch(options.ClientId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.ClientSecret))
            {
                return false;
            }

            return await HasRequiredOidcMetadataAsync(
                ctx,
                "https://accounts.google.com/.well-known/openid-configuration",
                "google",
                requireGoogleIssuer: true);
        }

        return true;
    }

    private static async Task<bool> HasRequiredOidcMetadataAsync(
        HttpContext ctx,
        string metadataAddress,
        string provider,
        bool requireGoogleIssuer)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");
        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ctx.RequestAborted);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            using var httpClient = clientFactory.CreateClient();
            using var response = await httpClient.GetAsync(metadataAddress, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug(
                    "Skipping {Provider} quick action: discovery endpoint returned status {StatusCode} at {MetadataAddress}",
                    provider,
                    (int)response.StatusCode,
                    metadataAddress);
                return false;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeoutCts.Token);
            var root = document.RootElement;

            if (!TryGetNonEmptyString(root, "issuer", out var issuer)
                || !TryGetNonEmptyString(root, "authorization_endpoint", out _)
                || !TryGetNonEmptyString(root, "token_endpoint", out _))
            {
                return false;
            }

            if (requireGoogleIssuer
                && !string.Equals(issuer, "https://accounts.google.com", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(issuer, "accounts.google.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Skipping {Provider} quick action: discovery request timed out for {MetadataAddress}", provider, metadataAddress);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Skipping {Provider} quick action: failed to validate discovery metadata at {MetadataAddress}", provider, metadataAddress);
            return false;
        }
    }

    private static bool TryGetNonEmptyString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var raw = property.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        value = raw;
        return true;
    }

    private static async Task HandleRefreshSchemesAsync(HttpContext ctx)
    {
        var setupSecret = ctx.Request.Cookies["setup-secret"];
        var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
        await schemeManager.RefreshSchemesAsync(setupSecret);

        var registered = await schemeManager.GetRegisteredProviderSchemesAsync();
        await ctx.Response.WriteAsJsonAsync(new { refreshed = true, providers = registered });
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

    private static async Task<string?> ResolvePreferredProviderForDirectLoginAsync(HttpContext ctx)
    {
        var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
        var registered = await schemeManager.GetRegisteredProviderSchemesAsync();
        var readyButtonProviders = new List<string>();

        foreach (var scheme in registered)
        {
            if (scheme is not (AuthSchemeNames.Keycloak or AuthSchemeNames.Google))
            {
                continue;
            }

            if (!await IsProviderReadyAsync(ctx, scheme))
            {
                continue;
            }

            var providerQuery = MapSchemeToProviderQueryValue(scheme);
            if (!string.IsNullOrWhiteSpace(providerQuery))
            {
                readyButtonProviders.Add(providerQuery);
            }
        }

        return readyButtonProviders.Count == 1 ? readyButtonProviders[0] : null;
    }

    private static string BuildLoginRedirectUrl(string returnUrl, string? provider = null, bool challengeError = false)
    {
        var queryParts = new List<string>
        {
            $"returnUrl={Uri.EscapeDataString(returnUrl)}"
        };

        if (challengeError)
        {
            queryParts.Add("challengeError=1");
        }

        if (!string.IsNullOrWhiteSpace(provider))
        {
            queryParts.Add($"provider={Uri.EscapeDataString(provider)}");
        }

        return "/login?" + string.Join("&", queryParts);
    }

    private static string? MapSchemeToProviderQueryValue(string scheme)
    {
        return scheme switch
        {
            AuthSchemeNames.Keycloak => "keycloak",
            AuthSchemeNames.Google => "google",
            AuthSchemeNames.Atproto => "atproto",
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
            ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? ctx.User.FindFirst("sid")?.Value;
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
