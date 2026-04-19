// ABOUTME: Runtime authentication scheme manager that dynamically registers OIDC/OAuth schemes.
// ABOUTME: Reads auth config from API + env vars, registers Keycloak/Google/ATProto schemes without restart.

using System.Net;
using System.Net.Sockets;
using Explore.Blazor.Constants;
using Explore.Blazor.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Explore.Blazor.Services;

public class DynamicAuthSchemeManager : IDynamicAuthSchemeManager, IDisposable
{
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IOptionsMonitorCache<OpenIdConnectOptions> _oidcOptionsCache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IDataProtectionProvider _dataProtection;
    private readonly ILogger<DynamicAuthSchemeManager> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly object _registeredSchemesSync = new();

    /// <summary>
    /// Tracks which dynamic provider schemes are currently registered.
    /// </summary>
    private readonly HashSet<string> _registeredSchemes = [];

    // Tracks the last-known Keycloak client secret so it can be preserved
    // when ApplyConfiguration is called without secrets (includeSecrets: false).
    private string? _currentKeycloakSecret;

    public DynamicAuthSchemeManager(
        IAuthenticationSchemeProvider schemeProvider,
        IOptionsMonitorCache<OpenIdConnectOptions> oidcOptionsCache,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IDataProtectionProvider dataProtection,
        ILogger<DynamicAuthSchemeManager> logger)
    {
        _schemeProvider = schemeProvider;
        _oidcOptionsCache = oidcOptionsCache;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _environment = environment;
        _dataProtection = dataProtection;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _logger.LogInformation("Initializing dynamic auth schemes...");

            // 1. Check environment variables for Keycloak (legacy/Docker config)
            var envAuthority = _configuration["Keycloak:Authority"];
            var envClientId = _configuration["Keycloak:ClientId"];
            var envClientSecret = _configuration["Keycloak:ClientSecret"];
            var envMetadataAddress = _configuration["Keycloak:MetadataAddress"];
            var envGoogleClientId = _configuration["Google:ClientId"];
            var envGoogleClientSecret = _configuration["Google:ClientSecret"];

            if (!string.IsNullOrEmpty(envAuthority) && !string.IsNullOrEmpty(envClientId))
            {
                _logger.LogInformation(
                    "Keycloak config detected in environment variables — registering Keycloak scheme from env " +
                    "(authority={Authority}, clientId={ClientId}, hasSecret={HasSecret})",
                    envAuthority, envClientId, !string.IsNullOrEmpty(envClientSecret));

                if (string.IsNullOrEmpty(envClientSecret))
                {
                    _logger.LogWarning(
"⚠️ KEYCLOAK_BLAZOR_CLIENT_SECRET is NOT set — Keycloak confidential clients will fail " +
                    "token exchange with 'unauthorized_client'. Ensure KEYCLOAK_BLAZOR_CLIENT_SECRET is set " +
                    "in Infisical (path: /keycloak) or as an environment variable.");
                }

                RegisterKeycloakScheme(envAuthority, envClientId, envClientSecret, envMetadataAddress);
            }

            if (!string.IsNullOrEmpty(envGoogleClientId))
            {
                _logger.LogInformation("Google config detected in environment/configuration — registering Google scheme from env");
                RegisterGoogleScheme(envGoogleClientId, envGoogleClientSecret);
            }

            // 2. Read DB configuration via API (may override or add providers).
            //    At startup, the setup secret is not available, so we read without secrets.
            //    Env-var Keycloak is the only scheme that works at cold start.
            //    After setup flow, RefreshSchemesAsync is called with secrets from the cookie.
            AuthProviderConfigurationResponse? dbConfig = null;
            try
            {
                dbConfig = await FetchConfigFromApiAsync(includeSecrets: false, setupSecret: null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Could not read auth provider configuration from API at startup — " +
                    "this is expected on first run before setup is completed");
            }

            if (dbConfig is not null)
            {
                ApplyConfiguration(dbConfig);
            }

            _logger.LogInformation(
                "Dynamic auth scheme initialization complete. Registered providers: [{Providers}]",
                string.Join(", ", SnapshotRegisteredSchemes()));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RefreshSchemesAsync(string? setupSecret = null)
    {
        await _lock.WaitAsync();
        try
        {
            _logger.LogInformation("Refreshing dynamic auth schemes from API...");

            var config = await FetchConfigFromApiAsync(
                includeSecrets: !string.IsNullOrEmpty(setupSecret),
                setupSecret: setupSecret);

            if (config is not null)
            {
                ApplyConfiguration(config);
            }

            _logger.LogInformation(
                "Dynamic auth scheme refresh complete. Registered providers: [{Providers}]",
                string.Join(", ", SnapshotRegisteredSchemes()));
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task<IReadOnlyList<string>> GetRegisteredProviderSchemesAsync()
    {
        return Task.FromResult<IReadOnlyList<string>>(SnapshotRegisteredSchemes().AsReadOnly());
    }

    private List<string> SnapshotRegisteredSchemes()
    {
        lock (_registeredSchemesSync)
        {
            return [.. _registeredSchemes];
        }
    }

    private async Task<AuthProviderConfigurationResponse?> FetchConfigFromApiAsync(
        bool includeSecrets,
        string? setupSecret)
    {
        var client = _httpClientFactory.CreateClient("BffClient");

        var endpoint = includeSecrets && !string.IsNullOrEmpty(setupSecret)
            ? "api/InstanceOnboarding/auth-provider-configuration/internal"
            : "api/InstanceOnboarding/auth-provider-configuration";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

        if (!string.IsNullOrEmpty(setupSecret))
        {
            request.Headers.TryAddWithoutValidation("X-Setup-Secret", setupSecret);
        }

        using var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug(
                "Auth config API returned {StatusCode} for {Endpoint}",
                (int)response.StatusCode, endpoint);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AuthProviderConfigurationResponse>();
    }

    private void ApplyConfiguration(AuthProviderConfigurationResponse config)
    {
        // Keycloak: register if enabled and has credentials (env vars may already have registered it)
        if (config.KeycloakEnabled &&
            !string.IsNullOrEmpty(config.KeycloakAuthority) &&
            !string.IsNullOrEmpty(config.KeycloakClientId))
        {
            if (!_registeredSchemes.Contains(AuthSchemeNames.Keycloak))
            {
                RegisterKeycloakScheme(
                    config.KeycloakAuthority,
                    config.KeycloakClientId,
                    config.KeycloakClientSecret,
                    metadataAddress: null);
            }
            else
            {
                // Preserve the existing secret from env vars when DB config has no secret
                // (e.g., at startup when includeSecrets: false)
                var effectiveSecret = !string.IsNullOrEmpty(config.KeycloakClientSecret)
                    ? config.KeycloakClientSecret
                    : GetCurrentKeycloakSecret();

                UpdateKeycloakSchemeOptions(
                    config.KeycloakAuthority,
                    config.KeycloakClientId,
                    effectiveSecret);
            }
        }
        else if (!config.KeycloakEnabled && _registeredSchemes.Contains(AuthSchemeNames.Keycloak))
        {
            // Only remove if NOT configured via env vars (env vars take priority)
            var envAuthority = _configuration["Keycloak:Authority"];
            if (string.IsNullOrEmpty(envAuthority))
            {
                RemoveScheme(AuthSchemeNames.Keycloak);
            }
            else
            {
                _logger.LogWarning(
                    "Keycloak is disabled in DB settings but configured via environment variables — keeping scheme registered");
            }
        }

        // Google: register if enabled and has credentials
        if (config.GoogleSsoEnabled && !string.IsNullOrEmpty(config.GoogleClientId))
        {
            if (!_registeredSchemes.Contains(AuthSchemeNames.Google))
            {
                RegisterGoogleScheme(config.GoogleClientId, config.GoogleClientSecret);
            }
            else
            {
                UpdateGoogleSchemeOptions(config.GoogleClientId, config.GoogleClientSecret);
            }
        }
        else if (!config.GoogleSsoEnabled && _registeredSchemes.Contains(AuthSchemeNames.Google))
        {
            // Only remove if NOT configured via env vars (env vars take priority)
            var envGoogleClientId = _configuration["Google:ClientId"];
            if (string.IsNullOrEmpty(envGoogleClientId))
            {
                RemoveScheme(AuthSchemeNames.Google);
            }
            else
            {
                _logger.LogWarning(
                    "Google is disabled in DB settings but configured via environment variables — keeping scheme registered");
            }
        }

        // ATProto: register handler if enabled (no OIDC — custom handler)
        if (config.AtprotoLoginEnabled)
        {
            if (!_registeredSchemes.Contains(AuthSchemeNames.Atproto))
            {
                RegisterAtprotoScheme(config.AtprotoPublicUrl);
            }
        }
        else if (!config.AtprotoLoginEnabled && _registeredSchemes.Contains(AuthSchemeNames.Atproto))
        {
            RemoveScheme(AuthSchemeNames.Atproto);
        }
    }

    private void RegisterKeycloakScheme(
        string authority,
        string clientId,
        string? clientSecret,
        string? metadataAddress)
    {
        var options = CreateKeycloakOptions(authority, clientId, clientSecret, metadataAddress);
        new OpenIdConnectPostConfigureOptions(_dataProtection)
            .PostConfigure(AuthSchemeNames.Keycloak, options);
        _oidcOptionsCache.TryAdd(AuthSchemeNames.Keycloak, options);

        var scheme = new AuthenticationScheme(
            AuthSchemeNames.Keycloak,
            displayName: "Keycloak",
            typeof(OpenIdConnectHandler));

        _schemeProvider.TryAddScheme(scheme);
        lock (_registeredSchemesSync)
        {
            _registeredSchemes.Add(AuthSchemeNames.Keycloak);
        }

        if (!string.IsNullOrEmpty(clientSecret?.Trim()))
            _currentKeycloakSecret = clientSecret;

        var trimmedSecret = clientSecret?.Trim();
        _logger.LogInformation(
            "[OIDC-DIAG] Registered Keycloak OIDC scheme (authority={Authority}, clientId={ClientId}, " +
            "secretLength={SecretLen}, secretPrefix={SecretPrefix})",
            authority, clientId, trimmedSecret?.Length ?? 0,
            trimmedSecret?.Length > 4 ? trimmedSecret[..4] + "..." : "(none)");
    }

    private void UpdateKeycloakSchemeOptions(string authority, string clientId, string? clientSecret)
    {
        _oidcOptionsCache.TryRemove(AuthSchemeNames.Keycloak);
        var options = CreateKeycloakOptions(authority, clientId, clientSecret, metadataAddress: null);
        new OpenIdConnectPostConfigureOptions(_dataProtection)
            .PostConfigure(AuthSchemeNames.Keycloak, options);
        _oidcOptionsCache.TryAdd(AuthSchemeNames.Keycloak, options);

        if (!string.IsNullOrEmpty(clientSecret?.Trim()))
            _currentKeycloakSecret = clientSecret;

        _logger.LogInformation(
            "Updated Keycloak OIDC scheme options (authority: {Authority}, secretLength: {SecretLen})",
            authority, clientSecret?.Trim().Length ?? 0);
    }

    private string? GetCurrentKeycloakSecret()
    {
        return _currentKeycloakSecret ?? _configuration["Keycloak:ClientSecret"];
    }

    private OpenIdConnectOptions CreateKeycloakOptions(
        string authority,
        string clientId,
        string? clientSecret,
        string? metadataAddress)
    {
        var trimmedSecret = clientSecret?.Trim();

        var cookieSecure = _environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        var options = new OpenIdConnectOptions
        {
            Authority = authority,
            ClientId = clientId,
            ClientSecret = trimmedSecret ?? string.Empty,
            UsePkce = true,
            SaveTokens = true,
            GetClaimsFromUserInfoEndpoint = true,
            RequireHttpsMetadata = !_environment.IsDevelopment(),
            CallbackPath = "/signin-oidc",
            SignedOutCallbackPath = "/signout-callback-oidc",
            SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            ResponseType = OpenIdConnectResponseType.Code,
            // Use query response mode so the callback is a GET redirect instead of a
            // cross-site POST (form_post). Lax cookies are sent on top-level GET navigations
            // but NOT on cross-site POSTs, which causes correlation failures with form_post.
            // PKCE protects the authorization code in the query string.
            ResponseMode = OpenIdConnectResponseMode.Query,
            PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable,
            // Force IPv4 for backchannel calls (token exchange, discovery, userinfo).
            // Self-hosted Keycloak domains may have unreachable AAAA records that cause
            // .NET's Happy Eyeballs to hang before falling back to IPv4.
            BackchannelHttpHandler = CreateIpv4Handler(),
            CorrelationCookie =
            {
                SameSite = SameSiteMode.Lax,
                SecurePolicy = cookieSecure
            },
            NonceCookie =
            {
                SameSite = SameSiteMode.Lax,
                SecurePolicy = cookieSecure
            },
            TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "preferred_username"
            },
            Events = CreateRemoteFailureEvents()
        };

        if (!string.IsNullOrEmpty(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access");

        return options;
    }

    private void RegisterGoogleScheme(string clientId, string? clientSecret)
    {
        var options = CreateGoogleOptions(clientId, clientSecret);
        new OpenIdConnectPostConfigureOptions(_dataProtection)
            .PostConfigure(AuthSchemeNames.Google, options);
        _oidcOptionsCache.TryAdd(AuthSchemeNames.Google, options);

        var scheme = new AuthenticationScheme(
            AuthSchemeNames.Google,
            displayName: "Google",
            typeof(OpenIdConnectHandler));

        _schemeProvider.TryAddScheme(scheme);
        lock (_registeredSchemesSync)
        {
            _registeredSchemes.Add(AuthSchemeNames.Google);
        }

        _logger.LogInformation("Registered Google OIDC scheme");
    }

    private void UpdateGoogleSchemeOptions(string clientId, string? clientSecret)
    {
        _oidcOptionsCache.TryRemove(AuthSchemeNames.Google);
        var options = CreateGoogleOptions(clientId, clientSecret);
        new OpenIdConnectPostConfigureOptions(_dataProtection)
            .PostConfigure(AuthSchemeNames.Google, options);
        _oidcOptionsCache.TryAdd(AuthSchemeNames.Google, options);

        _logger.LogInformation("Updated Google OIDC scheme options");
    }

    private OpenIdConnectOptions CreateGoogleOptions(string clientId, string? clientSecret)
    {
        var trimmedSecret = clientSecret?.Trim();
        var cookieSecure = _environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        return new OpenIdConnectOptions
        {
            Authority = "https://accounts.google.com",
            ClientId = clientId,
            ClientSecret = trimmedSecret ?? string.Empty,
            UsePkce = true,
            SaveTokens = true,
            GetClaimsFromUserInfoEndpoint = true,
            RequireHttpsMetadata = true,
            CallbackPath = "/signin-google",
            SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            ResponseType = OpenIdConnectResponseType.Code,
            ResponseMode = OpenIdConnectResponseMode.Query,
            PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable,
            CorrelationCookie =
            {
                SameSite = SameSiteMode.Lax,
                SecurePolicy = cookieSecure
            },
            NonceCookie =
            {
                SameSite = SameSiteMode.Lax,
                SecurePolicy = cookieSecure
            },
            TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name"
            },
            Scope = { "openid", "profile", "email" },
            Events = CreateRemoteFailureEvents()
        };
    }

    private void RegisterAtprotoScheme(string publicUrl)
    {
        // ATProto uses a custom authentication handler (not standard OIDC).
        // Phase 2 registers a placeholder scheme. Full FishyFlip integration comes in a later phase.
        // For now, we register the scheme name so the login UI can enumerate it,
        // but the actual handler is a stub that returns NoResult until implemented.

        var scheme = new AuthenticationScheme(
            AuthSchemeNames.Atproto,
            displayName: "AT Protocol",
            typeof(Authentication.AtprotoAuthenticationHandler));

        _schemeProvider.TryAddScheme(scheme);
        lock (_registeredSchemesSync)
        {
            _registeredSchemes.Add(AuthSchemeNames.Atproto);
        }

        _logger.LogInformation("Registered ATProto authentication scheme (public URL: {PublicUrl})", publicUrl);
    }

    private void RemoveScheme(string schemeName)
    {
        _schemeProvider.RemoveScheme(schemeName);

        // Clean up cached options for OIDC-based schemes
        if (schemeName is AuthSchemeNames.Keycloak or AuthSchemeNames.Google)
        {
            _oidcOptionsCache.TryRemove(schemeName);
        }

        lock (_registeredSchemesSync)
        {
            _registeredSchemes.Remove(schemeName);
        }
        _logger.LogInformation("Removed auth scheme: {SchemeName}", schemeName);
    }

    /// <summary>
    /// Creates OIDC events that handle remote authentication failures gracefully
    /// by redirecting to the login page instead of showing a raw exception.
    /// </summary>
    private OpenIdConnectEvents CreateRemoteFailureEvents()
    {
        return new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthEndpoints");
                logger.LogInformation(
                    "[OIDC-DIAG] Redirecting to IDP: {AuthorizationEndpoint}, clientId={ClientId}, " +
                    "redirectUri={RedirectUri}, responseType={ResponseType}, scope={Scope}",
                    context.ProtocolMessage.AuthorizationEndpoint,
                    context.ProtocolMessage.ClientId,
                    context.ProtocolMessage.RedirectUri,
                    context.ProtocolMessage.ResponseType,
                    context.ProtocolMessage.Scope);
                return Task.CompletedTask;
            },
            OnAuthorizationCodeReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthEndpoints");
                var secret = context.TokenEndpointRequest?.ClientSecret;
                logger.LogInformation(
                    "[OIDC-DIAG] Authorization code received. " +
                    "tokenEndpoint={TokenEndpoint}, clientId={ClientId}, secretLength={SecretLen}, " +
                    "secretPrefix={SecretPrefix}",
                    context.TokenEndpointRequest?.TokenEndpoint,
                    context.TokenEndpointRequest?.ClientId,
                    secret?.Length ?? 0,
                    secret?.Length > 4 ? secret[..4] + "..." : "(empty)");
                return Task.CompletedTask;
            },
            OnTokenResponseReceived = context =>
            {
                // Store the OIDC scheme name so TokenRefreshCookieEvents knows which IdP to call
                context.Properties?.Items[TokenRefreshCookieEvents.OidcSchemePropertyKey] = context.Scheme.Name;

                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthEndpoints");
                logger.LogInformation(
                    "[OIDC-DIAG] Token response received (idToken={HasIdToken}, accessToken={HasAccessToken}, " +
                    "error={Error}, errorDescription={ErrorDescription})",
                    !string.IsNullOrEmpty(context.TokenEndpointResponse?.IdToken),
                    !string.IsNullOrEmpty(context.TokenEndpointResponse?.AccessToken),
                    context.TokenEndpointResponse?.Error,
                    context.TokenEndpointResponse?.ErrorDescription);
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthEndpoints");

                var provider = context.Scheme.Name.ToLowerInvariant();
                var returnUrl = context.Properties?.RedirectUri ?? "/";

                // Log credentials diagnostics for token exchange failures
                var oidcOptions = context.HttpContext.RequestServices
                    .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                    .Get(context.Scheme.Name);

                var secret = oidcOptions.ClientSecret;
                var innerMsg = context.Failure?.InnerException?.Message;
                var errorMsg = context.Failure?.Message ?? "unknown";
                var exType = context.Failure?.GetType().Name ?? "unknown";
                var secretLen = secret?.Length ?? 0;
                var secretPrefix = secret?.Length > 4 ? secret[..4] + "..." : "(none)";

                logger.LogError(
                    context.Failure,
                    "[OIDC-DIAG] Remote authentication FAILURE for {Provider}: {Error} " +
                    "(innerError={InnerError}, exceptionType={ExType}, " +
                    "clientId={ClientId}, secretLength={SecretLen}, secretPrefix={SecretPrefix}, " +
                    "authority={Authority}, callbackPath={CallbackPath})",
                    provider, errorMsg,
                    innerMsg,
                    exType,
                    oidcOptions.ClientId,
                    secretLen,
                    secretPrefix,
                    oidcOptions.Authority,
                    oidcOptions.CallbackPath);

                // Write to stderr so it's visible even when Aspire dashboard misses structured logs
                Console.Error.WriteLine(
                    $"[OIDC-DIAG] FAILURE: {errorMsg} | inner={innerMsg} | type={exType} | " +
                    $"clientId={oidcOptions.ClientId} | secretLen={secretLen} | secretPrefix={secretPrefix} | " +
                    $"authority={oidcOptions.Authority}");

                // Include error detail in redirect URL for immediate browser visibility
                var errorDetail = Uri.EscapeDataString(
                    $"{errorMsg}|secretLen={secretLen}|clientId={oidcOptions.ClientId}");
                var redirectUrl = $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}" +
                                  $"&challengeError=1&provider={Uri.EscapeDataString(provider)}" +
                                  $"&errorDetail={errorDetail}";
                context.Response.Redirect(redirectUrl);
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    }

    /// <summary>
    /// Creates an HTTP handler that forces IPv4 connections. Self-hosted domains
    /// (Keycloak, Infisical) may have unreachable AAAA records; .NET's Happy Eyeballs
    /// tries IPv6 first and hangs before falling back to IPv4.
    /// </summary>
    private static SocketsHttpHandler CreateIpv4Handler()
    {
        return new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(10),
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
