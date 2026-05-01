// ABOUTME: Auth-related BFF endpoints: challenge, login, signout, status, providers, debug, refresh-schemes.
// ABOUTME: Includes multi-provider resolution, provider readiness checks, and OIDC metadata validation.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

public static class BffAuthEndpoints
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
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapPost("/bff/auth/refresh-session", HandleRefreshSessionAsync)
            .ValidateAntiforgery()
            .RequireAuthorization()
            .ExcludeFromDescription();

        // InteractiveServer self-calls cannot reliably satisfy browser antiforgery semantics,
        // so the server-side onboarding flow uses this authenticated internal variant instead.
        app.MapPost("/bff/auth/refresh-session/internal", HandleRefreshSessionAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/auth/debug", HandleAuthDebugAsync).RequireAuthorization();
        }

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

        if (await ShouldGateForOnboardingAsync(ctx))
        {
            logger.LogInformation(
                "[AuthEndpoints] Redirecting /auth/challenge to /setup because onboarding is incomplete.");
            ctx.Response.Redirect("/setup");
            return;
        }

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

            Console.Error.WriteLine(
                $"[AuthEndpoints] Challenge FAILED: {ex.Message} | inner={ex.InnerException?.Message}");

            var errorDetail = Uri.EscapeDataString(
                $"challenge:{ex.InnerException?.Message ?? ex.Message}");
            var redirectUrl = BuildLoginRedirectUrl(returnUrl, provider, challengeError: true)
                + $"&errorDetail={errorDetail}";
            ctx.Response.Redirect(redirectUrl);
        }
    }

    private static async Task HandleLoginRedirect(HttpContext ctx)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");

        if (await ShouldGateForOnboardingAsync(ctx))
        {
            logger.LogInformation(
                "[AuthEndpoints] Redirecting /auth/login to /setup because onboarding is incomplete.");
            ctx.Response.Redirect("/setup");
            return;
        }

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

    private static async Task<bool> ShouldGateForOnboardingAsync(HttpContext ctx)
    {
        if (!string.IsNullOrWhiteSpace(ctx.Request.Cookies["setup-secret"]))
        {
            return false;
        }

        var provider = ctx.RequestServices.GetService<IBffOnboardingStatusProvider>();
        if (provider is null)
        {
            return false;
        }

        try
        {
            var status = await provider.GetStatusAsync(ctx.RequestAborted);
            return status.Known && !status.IsCompleted;
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static async Task HandleSignoutAsync(HttpContext ctx)
    {
        ctx.Response.Headers.CacheControl = "no-store, no-cache";
        ctx.Response.Headers.Pragma = "no-cache";

        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");
        var returnUrl = GetSafeReturnUrl(ctx, logger);

        logger.LogInformation(
            "[AuthEndpoints] /auth/signout hit - Url: {Url} ReturnUrl: {ReturnUrl}",
            ctx.Request.GetDisplayUrl(), returnUrl);

        var cookieAuthResult = AuthenticateResult.NoResult();
        try
        {
            cookieAuthResult = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[AuthEndpoints] Could not authenticate cookie session during signout");
        }

        try
        {
            // Always clear the local BFF cookie session. Remote provider signout is best-effort,
            // but local cookie clearing is the security boundary and must not be reported as success
            // if it fails.
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[AuthEndpoints] Could not clear cookie session during signout");
            if (!ctx.Response.HasStarted)
            {
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                ctx.Response.ContentType = "application/problem+json";
                await ctx.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                    title = "Logout Failed",
                    status = StatusCodes.Status500InternalServerError,
                    detail = "The local session could not be cleared. Please retry signout."
                });
            }

            return;
        }

        if (cookieAuthResult.Succeeded && cookieAuthResult.Principal?.Identity?.IsAuthenticated == true)
        {
            IReadOnlyCollection<string> registered = [];

            try
            {
                var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
                registered = await schemeManager.GetRegisteredProviderSchemesAsync();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "[AuthEndpoints] Could not enumerate remote signout providers");
            }

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
                            "[AuthEndpoints] Could not sign out of {Scheme} — falling back to local redirect",
                            scheme);
                    }
                }
            }
        }

        if (!ctx.Response.HasStarted)
        {
            ctx.Response.Redirect(returnUrl);
            logger.LogInformation("[AuthEndpoints] Signout completed with local redirect");
        }
    }

    private static async Task<IResult> HandleRefreshSessionAsync(
        HttpContext ctx,
        BffAdminClaimsTransformation adminClaimsTransformation,
        CancellationToken cancellationToken)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");

        var authResult = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.Succeeded || authResult.Principal is null || authResult.Properties is null)
        {
            logger.LogWarning("[AuthEndpoints] Refresh session failed because cookie authentication did not succeed");
            return Results.Unauthorized();
        }

        var accessToken = authResult.Properties.GetTokenValue("access_token");
        var tokenAssessment = AssessAccessToken(accessToken);
        if (!tokenAssessment.IsUsable)
        {
            logger.LogWarning(
                "[AuthEndpoints] Refresh session produced no API-usable bearer token | Reason={Reason} User={UserId}",
                tokenAssessment.Reason,
                ResolveUserId(authResult.Principal));
            return Results.Json(
                new { refreshed = false, reason = tokenAssessment.Reason },
                statusCode: StatusCodes.Status409Conflict);
        }

        // Invalidate onboarding status cache BEFORE enriching principal so that
        // EnrichPrincipalAsync fetches fresh status (e.g. "completed" after onboarding)
        // instead of serving a stale "not completed" entry from the cache.
        ctx.RequestServices.GetService<IBffOnboardingStatusProvider>()?.Invalidate();

        var adminClaimsUpdated = await adminClaimsTransformation.EnrichPrincipalAsync(
            authResult.Principal,
            authResult.Properties,
            forceRefresh: true,
            cancellationToken: cancellationToken);

        await ctx.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authResult.Principal,
            authResult.Properties);

        var tokenService = ctx.RequestServices.GetService<ICircuitAccessTokenService>();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            tokenService?.SetToken(accessToken);
        }

        logger.LogInformation(
            "[AuthEndpoints] Refresh session confirmed usable bearer token | User={UserId} TokenSummary={TokenSummary} AdminClaimsUpdated={AdminClaimsUpdated}",
            ResolveUserId(authResult.Principal),
            DescribeToken(accessToken),
            adminClaimsUpdated);

        return Results.Ok(new { refreshed = true, adminClaimsUpdated, token = tokenAssessment.Reason });
    }

    private static (bool IsUsable, string Reason) AssessAccessToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return (false, "missing_access_token");
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                return (false, "unreadable_access_token");
            }

            var token = handler.ReadJwtToken(accessToken);
            var validToUtc = token.ValidTo;
            if (validToUtc <= DateTime.UtcNow.AddSeconds(30))
            {
                return (false, $"expired_access_token:{validToUtc:o}");
            }

            return (true, $"valid_until:{validToUtc:o}");
        }
        catch (Exception)
        {
            return (false, "access_token_parse_failed");
        }
    }

    private static string DescribeToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return "missing";
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                return "unreadable_jwt";
            }

            var token = handler.ReadJwtToken(accessToken);
            var userId = ResolveUserId(token.Claims) ?? "unknown";
            var issuer = string.IsNullOrWhiteSpace(token.Issuer) ? "unknown" : token.Issuer;
            var audience = token.Audiences.FirstOrDefault()
                ?? token.Claims.FirstOrDefault(c => c.Type == "azp")?.Value
                ?? "unknown";
            return $"user={userId};validTo={token.ValidTo:o};iss={issuer};aud={audience}";
        }
        catch (Exception)
        {
            return "jwt_parse_failed";
        }
    }

    private static string? ResolveUserId(ClaimsPrincipal? principal) => ResolveUserId(principal?.Claims);

    private static string? ResolveUserId(IEnumerable<Claim>? claims)
    {
        if (claims is null)
        {
            return null;
        }

        return claims.FirstOrDefault(c => c.Type == "sub")?.Value
            ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
            ?? claims.FirstOrDefault(c => c.Type == "sid")?.Value;
    }

    private static IResult HandleAuthStatus(HttpContext ctx)
    {
        ctx.Response.Headers.CacheControl = "no-store, no-cache";
        ctx.Response.Headers.Pragma = "no-cache";

        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            return Results.Ok(new { isAuthenticated = true, name = ctx.User.Identity.Name });
        }

        return Results.Ok(new { isAuthenticated = false });
    }

    private static async Task<IResult> HandleAuthDebugAsync(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
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
            using var response = await httpClient.GetAsync(metadataAddress, cancellationToken);
            result["discoveryStatus"] = (int)response.StatusCode;
            result["discoverySuccess"] = response.IsSuccessStatusCode;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                result["discoveryDocument"] = JsonSerializer.Deserialize<object>(content);
            }
            else
            {
                result["discoveryError"] = await response.Content.ReadAsStringAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            result["discoveryError"] = ex.Message;
        }

        return Results.Ok(result);
    }

    private static async Task HandleGetProviders(HttpContext ctx)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");

        try
        {
            var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
            var registered = await schemeManager.GetRegisteredProviderSchemesAsync();

            logger.LogInformation(
                "[AuthEndpoints] /auth/providers — registered schemes: [{Schemes}]",
                string.Join(", ", registered));

            var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var keycloakFromEnv = !string.IsNullOrEmpty(config["Keycloak:Authority"]);

            var providers = new List<object>();

            foreach (var scheme in registered)
            {
                var ready = await IsProviderReadyAsync(ctx, scheme);
                if (!ready)
                {
                    // Provider is registered but discovery endpoint is unreachable.
                    // For env/config-sourced providers (authority + clientId present),
                    // still include them — the user configured them intentionally.
                    var hasMinimalConfig = HasMinimalProviderConfig(ctx, scheme);
                    if (!hasMinimalConfig)
                    {
                        logger.LogWarning(
                            "[AuthEndpoints] /auth/providers — scheme {Scheme} is registered but NOT ready and has no env config — skipping",
                            scheme);
                        continue;
                    }

                    logger.LogWarning(
                        "[AuthEndpoints] /auth/providers — scheme {Scheme} discovery check failed but env/config present — including anyway",
                        scheme);
                }

                providers.Add(new
                {
                    name = MapSchemeToProviderQueryValue(scheme) ?? scheme,
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

            logger.LogInformation(
                "[AuthEndpoints] /auth/providers — returning {Count} ready provider(s)",
                providers.Count);

            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { providers });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in HandleGetProviders");
            if (!ctx.Response.HasStarted)
            {
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Provider Resolution Failed",
                    Detail = ex.Message
                });
            }
        }
    }

    private static async Task HandleRefreshSchemesAsync(HttpContext ctx)
    {
        ctx.Response.Headers.CacheControl = "no-store, no-cache";
        ctx.Response.Headers.Pragma = "no-cache";

        var setupSecret = ctx.Request.Cookies["setup-secret"];
        var schemeManager = ctx.RequestServices.GetRequiredService<IDynamicAuthSchemeManager>();
        await schemeManager.RefreshSchemesAsync(setupSecret);

        var registered = await schemeManager.GetRegisteredProviderSchemesAsync();
        await ctx.Response.WriteAsJsonAsync(new { refreshed = true, providers = registered });
    }

    // ──────────────────────────────────────────────
    // Multi-provider auth helpers
    // ──────────────────────────────────────────────

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

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ctx.RequestAborted);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            // Force IPv4 to avoid Happy Eyeballs hanging on unreachable AAAA records
            // (same pattern as the OIDC backchannel handler in DynamicAuthSchemeManager).
            using var handler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(10),
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new System.Net.Sockets.Socket(
                        System.Net.Sockets.AddressFamily.InterNetwork,
                        System.Net.Sockets.SocketType.Stream,
                        System.Net.Sockets.ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                        return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };

            // Allow self-signed / dev certs for non-production Keycloak instances
            var environment = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (environment.IsDevelopment())
            {
                handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            }

            using var httpClient = new HttpClient(handler, disposeHandler: false);
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            logger.LogInformation(
                "[AuthEndpoints] Checking OIDC discovery for {Provider} at {MetadataAddress}",
                provider, metadataAddress);

            using var response = await httpClient.GetAsync(metadataAddress, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "[AuthEndpoints] {Provider} discovery endpoint returned status {StatusCode} at {MetadataAddress}",
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
                logger.LogWarning(
                    "[AuthEndpoints] {Provider} discovery document at {MetadataAddress} is missing required fields (issuer/authorization_endpoint/token_endpoint)",
                    provider, metadataAddress);
                return false;
            }

            if (requireGoogleIssuer
                && !string.Equals(issuer, "https://accounts.google.com", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(issuer, "accounts.google.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            logger.LogInformation(
                "[AuthEndpoints] {Provider} discovery check passed (issuer: {Issuer})",
                provider, issuer);
            return true;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "[AuthEndpoints] {Provider} discovery request TIMED OUT for {MetadataAddress}",
                provider, metadataAddress);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "[AuthEndpoints] {Provider} discovery check FAILED at {MetadataAddress}: {Error}",
                provider, metadataAddress, ex.Message);
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

    /// <summary>
    /// Checks if a provider has minimal configuration from env/secrets (authority + clientId)
    /// so it can be shown even when the discovery endpoint is temporarily unreachable.
    /// </summary>
    private static bool HasMinimalProviderConfig(HttpContext ctx, string scheme)
    {
        if (scheme == AuthSchemeNames.Atproto)
        {
            return true;
        }

        try
        {
            var optionsMonitor = ctx.RequestServices.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
            var options = optionsMonitor.Get(scheme);
            return !string.IsNullOrWhiteSpace(options.Authority)
                && !string.IsNullOrWhiteSpace(options.ClientId);
        }
        catch
        {
            return false;
        }
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
}
