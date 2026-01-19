using Explore.Blazor.Client.Configuration;
using Explore.Blazor.Client.Pages;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Extensions;
using Explore.Blazor.Components;
using Blazouter.Extensions;
using Blazouter.Server.Extensions;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using System.Net.Http.Headers;
using Explore.Blazor;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

// Graceful shutdown tracking for zero-downtime deployments
var isShuttingDown = false;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddInfisicalBlazorCompatibility();

builder.AddServiceDefaults();

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
builder.Services.AddTransient<ServerCookieForwardingHandler>();
builder.Services.AddScoped<ICircuitAccessTokenService, CircuitAccessTokenService>();
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
if (!exploreApiBaseUrl.EndsWith("/", StringComparison.Ordinal))
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

builder.Services.AddOptions();

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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddOpenIdConnect(options =>
    {
        // From configuration/Infisical
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.ClientId = builder.Configuration["Keycloak:ClientId"];
        options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

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
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };

        // Request offline_access to get refresh token
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access");
    });

// Antiforgery for BFF endpoints
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

// YARP reverse proxy for API forwarding (Duende-free)
// Note: exploreApiBaseUrl is defined earlier in the file
var proxyRoutes = new[]
{
    new RouteConfig
    {
        RouteId = "explore-api",
        ClusterId = "explore-api",
        Match = new RouteMatch
        {
            Path = "/api/v1/{**catchall}"
        }
    }
};

var proxyClusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "explore-api",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["primary"] = new() { Address = exploreApiBaseUrl }
        }
    }
};

// Default tenant ID - MUST match Explore.API.Services.TenantContext.DefaultTenantId
// and Explore.Persistence.SeedIds.DefaultTenantId for proper multi-tenant isolation
const string DefaultTenantId = "018e4e5c-7f00-7000-8000-000000000001";

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

            // Always add X-Tenant-Id header for multi-tenant isolation
            // This ensures the API knows which tenant the request belongs to
            if (!transformContext.ProxyRequest.Headers.Contains("X-Tenant-Id"))
            {
                transformContext.ProxyRequest.Headers.Add("X-Tenant-Id", DefaultTenantId);
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

// Register graceful shutdown handler for zero-downtime deployments
// When Coolify sends SIGTERM, we set isShuttingDown to true
// This causes health checks to return 503, so load balancer stops routing traffic
app.Lifetime.ApplicationStopping.Register(() =>
{
    isShuttingDown = true;
    app.Logger.LogInformation("Application is shutting down, health checks will return unhealthy...");
});

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
                    Secure = ctx.Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                }
            );
        }
    }

    await next();
});

app.UseRouting();

app.UseAuthentication();

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

// Authentication endpoints
app.MapGet("/login", async ctx =>
{
    var returnUrl = ctx.Request.Query["returnUrl"].ToString();
    await ctx.ChallengeAsync(
        OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties
        {
            RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl
        }
    );
});

app.MapGet("/logout", async ctx =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignOutAsync(
        OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" }
    );
});

// Public endpoint to check authentication status
app.MapGet("/auth/status", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        return Results.Ok(new
        {
            isAuthenticated = true,
            name = ctx.User.Identity.Name,
            claims = ctx.User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
    else
    {
        return Results.Ok(new { isAuthenticated = false });
    }
});

app.MapGet("/bff/me", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        Name = ctx.User.Identity?.Name,
        Claims = ctx.User.Claims.Select(c => new { c.Type, c.Value })
    });
});

app.MapStaticAssets();
app.MapReverseProxy();

// Map Blazor components with Blazouter routing
// AddAdditionalAssemblies is required for component rendering in WASM mode
// Blazouter's Router handles client-side routing via RouteConfig
app.MapRazorComponents<App>()
    .AddBlazouterSupport()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Explore.Blazor.Client._Imports).Assembly);

await app.RunAsync();
