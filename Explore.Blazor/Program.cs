using System.Net.Http.Headers;
using Blazouter.Extensions;
using Blazouter.Server.Extensions;
using Explore.Blazor;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Configuration;
using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Pages;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Contracts;
using Explore.Blazor.Components;
using Explore.Blazor.Extensions;
using Explore.Blazor.Services;
using Explore.Secrets.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using Serilog;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

// Graceful shutdown tracking for zero-downtime deployments
// SIGTERM: 25 second grace period (health returns 503, still accepts requests)
// SIGINT: Immediate shutdown
var isShuttingDown = false;
var shutdownCts = new CancellationTokenSource();
const int GracefulShutdownSeconds = 25;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext(),
    writeToProviders: true);

// Configure shutdown timeout for graceful termination
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(GracefulShutdownSeconds + 5);
});

// Set host shutdown timeout
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(GracefulShutdownSeconds + 5);
});

builder.Configuration.AddInfisicalBlazorCompatibility();

builder.AddServiceDefaults();

// Add secret management services (refresh service, health checks, metrics, audit logging)
// This adds observability and background refresh for secrets loaded via AddInfisicalBlazorCompatibility
builder.Services.AddSecretManagement(builder.Configuration);

// Add MudBlazor services + DI
builder.Services.AddMudServices();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IOrganizationMemberService, OrganizationMemberService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ILandingPageService, LandingPageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrganizationReviewService, OrganizationReviewService>();
builder.Services.AddScoped<IMapsService, MapsService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IEventRegistrationService, EventRegistrationService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IEventAspectService, EventAspectService>();
builder.Services.AddScoped<IAudienceAgeService, AudienceAgeService>();
builder.Services.AddScoped<IAudienceGenderService, AudienceGenderService>();
builder.Services.AddScoped<IEventFormatService, EventFormatService>();
builder.Services.AddScoped<IEventStatusService, EventStatusService>();
builder.Services.AddScoped<IEventTypeService, EventTypeService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<IMadhabService, MadhabService>();
builder.Services.AddScoped<IEventSessionSpeakerService, EventSessionSpeakerService>();
builder.Services.AddScoped<IActorService, ActorService>();
builder.Services.AddScoped<ILookupCacheService, LookupCacheService>();
builder.Services.AddScoped<IInstanceOnboardingService, InstanceOnboardingService>();
builder.Services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
builder.Services.AddScoped<IPublicExperienceService, PublicExperienceService>();
builder.Services.AddScoped<IRuntimeRenderPolicyService, RuntimeRenderPolicyService>();
builder.Services.AddScoped<IStartupRoutingService, StartupRoutingService>();
builder.Services.AddScoped<IEventCreationEligibilityService, EventCreationEligibilityService>();
builder.Services.AddScoped<IAnalyticsInterop, ServerAnalyticsInterop>();
builder.Services.AddScoped<ICircuitAccessTokenService, CircuitAccessTokenService>();
builder.Services.AddSingleton<ISetupSecretSessionService, SetupSecretSessionService>();
builder.Services.AddTransient<AccessTokenForwardingHandler>();
// Configure multi-tenancy settings
builder.Services.Configure<TenantConfiguration>(builder.Configuration.GetSection("Explore:MultiTenancy"));
// Register AuthStateService for centralized auth context
builder.Services.AddScoped<IAuthStateService, AuthStateService>();
// Register named HTTP client for S3 uploads (ImageStorageService)
builder.Services.AddHttpClient("S3Upload", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Allow large file uploads
});

// Register named HTTP client for BFF API calls from server-side Blazor
// This is used by ImageStorageService to get presigned URLs
builder.Services.AddHttpClient("BffClient", client =>
{
    // In server-side mode, we call the API directly with access token forwarding
    client.BaseAddress = new Uri(builder.Configuration["ExploreApi:BaseUrl"] ?? "https://localhost:7039/");
})
.AddHttpMessageHandler<AccessTokenForwardingHandler>()
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            var isLocalhost = message.RequestUri?.Host.Contains("localhost") ?? false;
            return isLocalhost || errors == System.Net.Security.SslPolicyErrors.None;
        };
    }
    return handler;
});

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true);

builder.Services.AddBlazouter();
builder.Services.AddHttpContextAccessor();

// Get the API base URL - for InteractiveServer mode, we call the API directly
// since there's no HttpContext during SignalR calls
var exploreApiBaseUrl = builder.Configuration["ExploreApi:BaseUrl"] ?? "https://localhost:7039/";
if (!exploreApiBaseUrl.EndsWith('/'))
{
    exploreApiBaseUrl += "/";
}

// NSwag-generated API client for type-safe API calls
// In InteractiveServer mode, HttpContext is null during SignalR calls
// We call the API directly and forward the access token via AccessTokenForwardingHandler
builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
    {
        client.BaseAddress = new Uri(exploreApiBaseUrl);
    })
    .AddHttpMessageHandler<AccessTokenForwardingHandler>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                var isLocalhost = message.RequestUri?.Host.Contains("localhost") ?? false;
                return isLocalhost || errors == System.Net.Security.SslPolicyErrors.None;
            };
        }

        return handler;
    });

// Register TenantNavigationService for server-side rendering
builder.Services.AddHttpClient<ITenantNavigationService, TenantNavigationService>(client =>
    {
        client.BaseAddress = new Uri(exploreApiBaseUrl);
    })
    .AddHttpMessageHandler<AccessTokenForwardingHandler>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                var isLocalhost = message.RequestUri?.Host.Contains("localhost") ?? false;
                return isLocalhost || errors == System.Net.Security.SslPolicyErrors.None;
            };
        }

        return handler;
    });

// Register GroupService with direct API access for InteractiveServer rendering.
builder.Services.AddHttpClient<IGroupService, GroupService>(client =>
    {
        client.BaseAddress = new Uri(exploreApiBaseUrl);
    })
    .AddHttpMessageHandler<AccessTokenForwardingHandler>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                var isLocalhost = message.RequestUri?.Host.Contains("localhost") ?? false;
                return isLocalhost || errors == System.Net.Security.SslPolicyErrors.None;
            };
        }

        return handler;
    });

builder.Services.AddOptions();

// Log Keycloak configuration (without secrets)
var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
var keycloakClientId = builder.Configuration["Keycloak:ClientId"];
var keycloakClientSecret = builder.Configuration["Keycloak:ClientSecret"];
var keycloakMetadataAddress = builder.Configuration["Keycloak:MetadataAddress"];

var startupLogger = Serilog.Log.ForContext("SourceContext", "Startup");
startupLogger.Information("Keycloak Configuration:");
startupLogger.Information("  Authority: {Authority}", keycloakAuthority ?? "(not set)");
startupLogger.Information("  ClientId: {ClientId}", keycloakClientId ?? "(not set)");
startupLogger.Information("  ClientSecret: {HasSecret}", string.IsNullOrEmpty(keycloakClientSecret) ? "NO" : "YES");

if (string.IsNullOrEmpty(keycloakAuthority) || string.IsNullOrEmpty(keycloakClientId) || string.IsNullOrEmpty(keycloakClientSecret))
{
    startupLogger.Error("CRITICAL: Keycloak configuration is incomplete! Authentication will not work.");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";

        // Cookie expiration settings for better session management
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        // Cookie security settings
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddOpenIdConnect(options =>
    {
        // From configuration/Infisical
        options.Authority = keycloakAuthority;
        options.ClientId = keycloakClientId;
        options.ClientSecret = keycloakClientSecret;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        if (!string.IsNullOrEmpty(keycloakMetadataAddress))
        {
            options.MetadataAddress = keycloakMetadataAddress;
        }

        options.RequireHttpsMetadata = string.Equals(
            builder.Configuration["Keycloak:RequireHttpsMetadata"],
            "true",
            StringComparison.OrdinalIgnoreCase
        );

        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username"
        };

        // Request offline_access to get refresh token
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access");

        // OIDC event handlers for debugging authentication issues
        options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                startupLogger.Debug("[OIDC] Redirecting to IdP. RedirectUri: {RedirectUri}",
                    context.ProtocolMessage.RedirectUri);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                startupLogger.Error(context.Exception, "[OIDC] Authentication failed: {Error}",
                    context.Exception?.Message);
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                startupLogger.Error("[OIDC] Remote failure: {Error}, Description: {Description}",
                    context.Failure?.Message,
                    context.Properties?.Items);

                // Log the full error from Keycloak
                if (context.HttpContext.Request.Query.TryGetValue("error", out var error))
                {
                    startupLogger.Error("[OIDC] Keycloak error: {Error}", error);
                }
                if (context.HttpContext.Request.Query.TryGetValue("error_description", out var errorDesc))
                {
                    startupLogger.Error("[OIDC] Keycloak error_description: {ErrorDesc}", errorDesc);
                }

                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                startupLogger.Debug("[OIDC] Message received from IdP");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                startupLogger.Debug("[OIDC] Token validated for user: {User}",
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

// Antiforgery for BFF endpoints
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

// YARP reverse proxy for API forwarding (Duende-free)
// Note: exploreApiBaseUrl is defined earlier in the file
var proxyRoutes = new[]
{
    new RouteConfig
    {
        RouteId = "event-api",
        ClusterId = "event-api",
        Match = new RouteMatch
        {
            Path = "/api/{**catchall}"
        }
    }
};

var proxyClusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "event-api",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["primary"] = new() { Address = exploreApiBaseUrl }
        },
        HttpClient = new HttpClientConfig
        {
            DangerousAcceptAnyServerCertificate = builder.Environment.IsDevelopment()
        }
    }
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(proxyRoutes, proxyClusters)
    .AddTransforms(context =>
    {
        context.AddRequestTransform(async transformContext =>
        {
            var httpContext = transformContext.HttpContext;
            var token = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                transformContext.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            var incomingTenantId = httpContext.Request.Headers[TenantConstants.TenantIdHeaderName].FirstOrDefault();
            if (!transformContext.ProxyRequest.Headers.Contains(TenantConstants.TenantIdHeaderName) &&
                !string.IsNullOrWhiteSpace(incomingTenantId))
            {
                transformContext.ProxyRequest.Headers.Add(
                    TenantConstants.TenantIdHeaderName,
                    incomingTenantId);
            }

            // Setup secret header forwarding with injection prevention.
            // Strip first, then add trusted value from header/cookie/server-side user session.
            transformContext.ProxyRequest.Headers.Remove("X-Setup-Secret");
            var setupSecret = httpContext.Request.Headers["X-Setup-Secret"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(setupSecret))
            {
                setupSecret = httpContext.Request.Cookies["setup-secret"];
            }

            if (string.IsNullOrWhiteSpace(setupSecret) && httpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var setupSecretSessionService = httpContext.RequestServices.GetRequiredService<ISetupSecretSessionService>();
                    setupSecret = setupSecretSessionService.GetForUser(userId);
                }
            }

            if (!string.IsNullOrWhiteSpace(setupSecret))
            {
                transformContext.ProxyRequest.Headers.Add("X-Setup-Secret", setupSecret);
            }
        });
    });

builder.Services.AddAuthorizationBuilder();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Shutdown-aware health check for zero-downtime deployments (Coolify rolling updates)
// When SIGTERM is received, health checks return unhealthy so load balancer stops routing traffic
builder.Services.AddHealthChecks()
    .AddCheck("shutdown", () =>
    {
        if (isShuttingDown)
            return HealthCheckResult.Unhealthy("Application is shutting down");
        return HealthCheckResult.Healthy();
    }, tags: ["live", "ready"]);

var app = builder.Build();

// ============================================================================
// CRITICAL: Forwarded Headers for Reverse Proxy / SSL Termination (Coolify)
// ============================================================================
// When running behind a reverse proxy (Nginx, Coolify, cloud load balancers),
// the proxy terminates TLS and forwards requests to the app via HTTP internally.
// Without this, the app sees HTTP and generates http:// redirect URIs, causing
// OIDC "Invalid parameter: redirect_uri" errors because Keycloak expects https://.
//
// This middleware reads the X-Forwarded-Proto and X-Forwarded-For headers
// set by the proxy to restore the original request scheme and client IP.
// ============================================================================

// Configure forwarded headers options
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Clear the default known networks/proxies to trust all proxies
    // This is required for containerized environments like Coolify/Docker
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

// Log the detected scheme for debugging (debug level to avoid log spam)
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/auth") ||
        context.Request.Path.StartsWithSegments("/login") ||
        context.Request.Path.StartsWithSegments("/logout"))
    {
        app.Logger.LogDebug(
            "[ForwardedHeaders] Path: {Path}, Scheme: {Scheme}, Host: {Host}, Proto Header: {Proto}",
            context.Request.Path,
            context.Request.Scheme,
            context.Request.Host,
            context.Request.Headers["X-Forwarded-Proto"].ToString());
    }
    await next();
});

// Register graceful shutdown handlers for zero-downtime deployments
// SIGTERM: Start graceful shutdown with 25 second grace period
// SIGINT (Ctrl+C): Immediate shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    isShuttingDown = true;
    app.Logger.LogInformation(
        "SIGTERM received. Starting graceful shutdown. Health checks return 503. " +
        "Accepting requests for {Seconds} more seconds...",
        GracefulShutdownSeconds);
});

// Handle SIGINT for immediate shutdown
Console.CancelKeyPress += (sender, e) =>
{
    app.Logger.LogWarning("SIGINT received. Initiating immediate shutdown...");
    e.Cancel = false; // Allow the process to terminate immediately
    shutdownCts.Cancel();
    Environment.Exit(0);
};

// Handle SIGTERM with grace period (Unix systems)
AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    if (!isShuttingDown)
    {
        isShuttingDown = true;
        app.Logger.LogInformation(
            "Process exit signal received. Graceful shutdown with {Seconds} second grace period...",
            GracefulShutdownSeconds);

        // Wait for grace period to allow in-flight requests to complete
        Thread.Sleep(TimeSpan.FromSeconds(GracefulShutdownSeconds));
    }
};

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
app.Use(async (ctx, next) =>
{
    if (HttpMethods.IsGet(ctx.Request.Method))
    {
        var tokens = antiforgery.GetAndStoreTokens(ctx);
        if (!string.IsNullOrEmpty(tokens.RequestToken))
        {
            ctx.Response.Cookies.Append(
                "XSRF-TOKEN",
                tokens.RequestToken,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = !app.Environment.IsDevelopment(),
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                }
            );
        }
    }

    await next();
});

// Resolve root entry through startup gate before endpoint routing.
// This avoids ambiguous "/" endpoint matches and centralizes onboarding/home-page selection.
app.Use(async (ctx, next) =>
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
            var status = await statusClient.GetFromJsonAsync<InstanceOnboardingStatusModel>("api/InstanceOnboarding/status");
            isCompleted = status?.IsCompleted == true;
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to resolve instance onboarding status for startup redirect.");
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
});

app.UseRouting();

app.UseAuthentication();

// Capture access token during HTTP request pipeline (async-safe) for use in Blazor components
// This avoids the .GetAwaiter().GetResult() anti-pattern in App.razor's synchronous code block
app.Use(async (ctx, next) =>
{
    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        var accessToken = await ctx.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(accessToken))
        {
            ctx.Items["AccessToken"] = accessToken;

            // Store in scoped token service for API calls during this request
            var tokenService = ctx.RequestServices.GetService<ICircuitAccessTokenService>();
            tokenService?.SetToken(accessToken);
        }
    }

    await next();
});

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase))
    {
        var endpoint = ctx.GetEndpoint();
        var requiresAuth = endpoint?.Metadata.GetMetadata<IAuthorizeData>() != null;

        if (requiresAuth && ctx.User?.Identity?.IsAuthenticated != true)
        {
            app.Logger.LogInformation(
                "BFF: unauthenticated request to protected endpoint {Method} {Path}",
                ctx.Request.Method,
                ctx.Request.Path
            );
        }
    }

    await next();
});

app.UseAuthorization();
app.UseAntiforgery();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // Test endpoint to verify routing works
    app.MapGet("/test-endpoint", () => Results.Ok(new { message = "Server endpoint works!", timestamp = DateTime.UtcNow }))
        .WithName("TestEndpoint");
}

static string GetSafeReturnUrl(HttpContext ctx, Microsoft.Extensions.Logging.ILogger logger)
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

// Authentication endpoints - using /auth/* paths to avoid conflict with Blazor routes.
// The Blazor shim components at /login and /logout force-load these server endpoints.
app.MapGet("/auth/challenge", async ctx =>
{
    var returnUrl = GetSafeReturnUrl(ctx, app.Logger);

    var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
    app.Logger.LogDebug(
        "[AuthEndpoints] /auth/challenge - Config check: Authority={Authority}, HasClientId={HasClientId}, HasSecret={HasSecret}",
        config["Keycloak:Authority"],
        !string.IsNullOrEmpty(config["Keycloak:ClientId"]),
        !string.IsNullOrEmpty(config["Keycloak:ClientSecret"]));

    app.Logger.LogInformation(
        "[AuthEndpoints] /auth/challenge hit - Url: {Url} ReturnUrl: {ReturnUrl}",
        ctx.Request.GetDisplayUrl(),
        returnUrl);

    try
    {
        await ctx.ChallengeAsync(
            OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties
            {
                RedirectUri = returnUrl
            });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[AuthEndpoints] Error during login challenge. Authority: {Authority}, ClientId: {ClientId}, HasSecret: {HasSecret}, InnerException: {Inner}",
            config["Keycloak:Authority"],
            config["Keycloak:ClientId"],
            !string.IsNullOrEmpty(config["Keycloak:ClientSecret"]),
            ex.InnerException?.Message);
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new
        {
            error = "Login failed. Please try again later."
        });
    }
});

app.MapGet("/auth/login", ctx =>
{
    var returnUrl = Uri.EscapeDataString(GetSafeReturnUrl(ctx, app.Logger));
    ctx.Response.Redirect($"/auth/challenge?returnUrl={returnUrl}");
    return Task.CompletedTask;
});

app.MapGet("/auth/signout", async ctx =>
{
    var returnUrl = GetSafeReturnUrl(ctx, app.Logger);

    app.Logger.LogInformation(
        "[AuthEndpoints] /auth/signout hit - Url: {Url} ReturnUrl: {ReturnUrl}",
        ctx.Request.GetDisplayUrl(),
        returnUrl);

    try
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignOutAsync(
            OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = returnUrl });
        app.Logger.LogInformation("[AuthEndpoints] Signout completed");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[AuthEndpoints] Error during signout");
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = "Logout failed. Please try again later." });
    }
});

// Authentication status endpoint - returns minimal safe information only
app.MapGet("/auth/status", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        return Results.Ok(new
        {
            isAuthenticated = true,
            name = ctx.User.Identity.Name
        });
    }
    else
    {
        return Results.Ok(new { isAuthenticated = false });
    }
});

// OIDC debug endpoint - Development only, requires authentication
if (app.Environment.IsDevelopment())
{
    app.MapGet("/auth/debug", async (IConfiguration config, IHttpClientFactory httpClientFactory) =>
    {
        var authority = config["Keycloak:Authority"];
        var metadataAddress = config["Keycloak:MetadataAddress"] ?? $"{authority}/.well-known/openid-configuration";

        var result = new Dictionary<string, object?>
        {
            ["authority"] = authority,
            ["metadataAddress"] = metadataAddress,
            ["hasClientId"] = !string.IsNullOrEmpty(config["Keycloak:ClientId"]),
            ["hasClientSecret"] = !string.IsNullOrEmpty(config["Keycloak:ClientSecret"])
        };

        // Try to fetch OIDC discovery document
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
    }).RequireAuthorization();
}

// Theme preference endpoint - sets cookie for SSR theme rendering
app.MapPost("/bff/theme", (HttpContext ctx) =>
{
    var theme = ctx.Request.Query["theme"].ToString();
    if (theme is "dark" or "light")
    {
        ctx.Response.Cookies.Append("theme", theme, new CookieOptions
        {
            MaxAge = TimeSpan.FromDays(365),
            Path = "/",
            SameSite = SameSiteMode.Lax,
            HttpOnly = false,
            Secure = !app.Environment.IsDevelopment()
        });
        return Results.Ok();
    }
    return Results.BadRequest();
}).ExcludeFromDescription();

app.MapPost("/bff/storage/upload-proxy", async (
    HttpContext ctx,
    IHttpClientFactory clientFactory,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
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
        logger.LogWarning("Rejected upload proxy request for non-presigned URL host {Host}", uploadUri.Host);
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
                uploadUri.Host,
                (int)response.StatusCode,
                responseBody);

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
})
.RequireAuthorization()
.ExcludeFromDescription();

app.MapPost("/bff/setup-secret", async (HttpContext ctx) =>
{
    var setupSecretSessionService = ctx.RequestServices.GetRequiredService<ISetupSecretSessionService>();
    var payload = await ctx.Request.ReadFromJsonAsync<SetupSecretCookieRequest>();
    var secret = payload?.Secret?.Trim();

    if (string.IsNullOrWhiteSpace(secret))
    {
        return Results.BadRequest(new { error = "Setup secret is required." });
    }

    var validation = await ValidateSetupSecretAsync(ctx, secret, ctx.RequestAborted);
    if (!validation.IsValid)
    {
        ClearSetupSecret(ctx, setupSecretSessionService, !app.Environment.IsDevelopment());
        return Results.Json(new { error = validation.Error }, statusCode: validation.StatusCode);
    }

    PersistSetupSecret(ctx, setupSecretSessionService, secret, !app.Environment.IsDevelopment());

    return Results.Ok();
}).ExcludeFromDescription();

app.MapPost("/bff/setup-secret/sync", async (HttpContext ctx) =>
{
    var setupSecretSessionService = ctx.RequestServices.GetRequiredService<ISetupSecretSessionService>();
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
        ClearSetupSecret(ctx, setupSecretSessionService, !app.Environment.IsDevelopment(), userId);
        return Results.Json(new { error = validation.Error }, statusCode: validation.StatusCode);
    }

    PersistSetupSecret(ctx, setupSecretSessionService, secret, !app.Environment.IsDevelopment(), userId);

    return Results.Ok();
}).ExcludeFromDescription();

app.MapDelete("/bff/setup-secret", (HttpContext ctx) =>
{
    var setupSecretSessionService = ctx.RequestServices.GetRequiredService<ISetupSecretSessionService>();
    ClearSetupSecret(ctx, setupSecretSessionService, !app.Environment.IsDevelopment());

    return Results.Ok();
}).ExcludeFromDescription();

app.MapGet("/bff/me", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    // Return only safe, non-sensitive claims needed by the frontend
    var safeClaims = new[] { "preferred_username", "email", "name", "given_name", "family_name", "sub" };
    return Results.Ok(new
    {
        Name = ctx.User.Identity?.Name,
        Claims = ctx.User.Claims
            .Where(c => safeClaims.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
            .Select(c => new { c.Type, c.Value })
    });
});

app.MapStaticAssets();

// Map Blazor components with Blazouter routing BEFORE reverse proxy
// AddAdditionalAssemblies is required for component rendering in WASM mode
// Blazouter's Router handles client-side routing via RouteConfig
// IMPORTANT: This is mapped BEFORE /api proxy to allow Blazor to handle all non-API routes
// BUT authentication endpoints (/login, /logout) are mapped EARLIER with higher priority
app.MapRazorComponents<App>()
    .AddBlazouterSupport()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Explore.Blazor.Client._Imports).Assembly);

// Map reverse proxy LAST - it should only handle /api/* routes
app.MapReverseProxy();

await app.RunAsync();

static async Task<SetupSecretValidationGatewayResult> ValidateSetupSecretAsync(HttpContext ctx, string secret, CancellationToken cancellationToken)
{
    var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
    var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("SetupSecretGateway");

    try
    {
        var client = clientFactory.CreateClient("BffClient");
        var payload = new SetupSecretCookieRequest { Secret = secret };
        using var response = await client.PostAsJsonAsync("api/InstanceOnboarding/validate-secret", payload, cancellationToken);

        SetupSecretValidationResponse? body = null;
        try
        {
            body = await response.Content.ReadFromJsonAsync<SetupSecretValidationResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not parse setup secret validation response body.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Gone)
        {
            return new SetupSecretValidationGatewayResult(
                IsValid: false,
                StatusCode: StatusCodes.Status410Gone,
                Error: body?.Error ?? "Setup already completed.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return new SetupSecretValidationGatewayResult(
                IsValid: false,
                StatusCode: StatusCodes.Status502BadGateway,
                Error: "Could not validate setup secret at this time.");
        }

        if (body?.Valid == true)
        {
            return new SetupSecretValidationGatewayResult(
                IsValid: true,
                StatusCode: StatusCodes.Status200OK,
                Error: string.Empty);
        }

        return new SetupSecretValidationGatewayResult(
            IsValid: false,
            StatusCode: StatusCodes.Status400BadRequest,
            Error: body?.Error ?? "Invalid setup secret.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Setup secret validation request failed.");
        return new SetupSecretValidationGatewayResult(
            IsValid: false,
            StatusCode: StatusCodes.Status503ServiceUnavailable,
            Error: "Could not validate setup secret at this time.");
    }
}

static void PersistSetupSecret(
    HttpContext ctx,
    ISetupSecretSessionService setupSecretSessionService,
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
        setupSecretSessionService.SetForUser(resolvedUserId, secret);
    }
}

static void ClearSetupSecret(
    HttpContext ctx,
    ISetupSecretSessionService setupSecretSessionService,
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
        setupSecretSessionService.ClearForUser(resolvedUserId);
    }
}

static string? ResolveUserId(HttpContext ctx)
{
    return ctx.User.FindFirst("sub")?.Value
        ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}

file sealed class SetupSecretCookieRequest
{
    public string? Secret { get; set; }
}

file sealed class SetupSecretValidationResponse
{
    public bool Valid { get; set; }
    public string? Error { get; set; }
}

file sealed record SetupSecretValidationGatewayResult(bool IsValid, int StatusCode, string Error);
