using Duende.AccessTokenManagement.OpenIdConnect;
using Explore.Blazor.Client.Pages;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Components;
using Explore.Blazor.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using System.Net.Http.Headers;
using Explore.Blazor;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add MudBlazor services + DI
builder.Services.AddMudServices();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IProgramService, ProgramService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IOrganizationMemberService, OrganizationMemberService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ILandingPageService, LandingPageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrganizationReviewService, OrganizationReviewService>();
builder.Services.AddScoped<IMapsService, MapsService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();

// Add HttpClient for server-side prerendering (without token)
builder.Services.AddScoped(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor.HttpContext?.Request;

    var baseAddress = request != null
        ? $"{request.Scheme}://{request.Host}"
        : builder.Configuration["SelfUrl"] ?? "https://localhost:7071";

    return new HttpClient { BaseAddress = new Uri(baseAddress) };
});

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpContextAccessor();

// NSwag-generated API client for type-safe API calls
// Uses Duende token management for automatic token attachment and refresh
builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ExploreApi:BaseUrl"]
            ?? "https://localhost:7039/"
        );
    })
    .AddUserAccessTokenHandler()
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

// Automatic user access token attach/refresh for calls to explore.api
builder.Services.AddOpenIdConnectAccessTokenManagement();

builder.Services.AddAuthorizationBuilder();

// Use PersistingServerAuthenticationStateProvider that persists auth state for WASM hydration
// This enables seamless auth state transfer from server to WASM during InteractiveAuto mode
builder.Services.AddScoped<AuthenticationStateProvider, Explore.Blazor.Services.PersistingServerAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

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

// ============================================================================
// BFF ENDPOINTS - Uses NSwag EventApiClient for type-safe API calls
// ============================================================================

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// API v1 proxy endpoints - matches NSwag client paths for WebAssembly
var apiV1 = app.MapGroup("/api/v1");

// Organization Endpoints
apiV1.MapGet("/Organization", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationAllAsync(),
        logger,
        "GET /api/v1/Organization"
    ));

apiV1.MapGet("/Organization/my", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().My2Async(),
        logger,
        "GET /api/v1/Organization/my"
    ))
    .RequireAuthorization();

apiV1.MapGet("/Organization/{id}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationGETAsync(id),
        logger,
        $"GET /api/v1/Organization/{id}"
    ));

apiV1.MapPost("/Organization", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<CreateOrganizationDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationPOSTAsync(dto),
        logger,
        "POST /api/v1/Organization"
    );
})
.RequireAuthorization();

apiV1.MapPut("/Organization/{id}", async (Guid id, HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<UpdateOrganizationDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationPUTAsync(id, dto),
        logger,
        $"PUT /api/v1/Organization/{id}"
    );
})
.RequireAuthorization();

apiV1.MapPut("/Organization/updatestatustype/{id}", async (Guid id, HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<UpdateOrganizationApprovalStatusDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteVoidAsync(
        () => ctx.GetApiClient().UpdatestatustypeAsync(id, dto),
        logger,
        $"PUT /api/v1/Organization/updatestatustype/{id}"
    );
})
.RequireAuthorization();

// Event Endpoints
apiV1.MapGet("/Event", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventAllAsync(),
        logger,
        "GET /api/v1/Event"
    ));

apiV1.MapGet("/Event/my", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().MyAsync(),
        logger,
        "GET /api/v1/Event/my"
    ))
    .RequireAuthorization();

apiV1.MapGet("/Event/{id}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventGETAsync(id),
        logger,
        $"GET /api/v1/Event/{id}"
    ));

apiV1.MapPost("/Event", async ([FromBody] CreateEventDto dto, HttpContext ctx, ILogger<Program> log) =>
{
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    log.LogInformation("[BFF] Creating event: {Title}, Org: {OrgId}", dto.Title, dto.OrganizationId);
    
    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventPOSTAsync(dto),
        log,
        "POST /api/v1/Event"
    );
})
.RequireAuthorization();

apiV1.MapPut("/Event/{id}", async (Guid id, [FromBody] UpdateEventDto dto, HttpContext ctx, ILogger<Program> log) =>
{
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventPUTAsync(id, dto),
        log,
        $"PUT /api/v1/Event/{id}"
    );
})
.RequireAuthorization();

apiV1.MapDelete("/Event/{id}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteVoidAsync(
        () => ctx.GetApiClient().EventDELETEAsync(id),
        logger,
        $"DELETE /api/v1/Event/{id}"
    ))
    .RequireAuthorization();

// User Endpoints
apiV1.MapPost("/User/sync", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().SyncAsync(),
        logger,
        "POST /api/v1/User/sync"
    ))
    .RequireAuthorization();

apiV1.MapGet("/User", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().UserGETAsync(),
        logger,
        "GET /api/v1/User"
    ))
    .RequireAuthorization();

// User organizations - using the proper User/{userId}/organizations endpoint
apiV1.MapGet("/User/{userId:guid}/organizations", async (Guid userId, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationsAsync(userId),
        logger,
        $"GET /api/v1/User/{userId}/organizations"
    ))
    .RequireAuthorization();

apiV1.MapPut("/User", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<UpdateUserDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().UserPUTAsync(dto),
        logger,
        "PUT /api/v1/User"
    );
})
.RequireAuthorization();

apiV1.MapDelete("/User", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteVoidAsync(
        () => ctx.GetApiClient().UserDELETEAsync(),
        logger,
        "DELETE /api/v1/User"
    ))
    .RequireAuthorization();

// Lookup/Reference Data Endpoints (Public)
apiV1.MapGet("/EventType", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventTypeAllAsync(),
        logger,
        "GET /api/v1/EventType"
    ));

apiV1.MapGet("/EventFormat", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventFormatAllAsync(),
        logger,
        "GET /api/v1/EventFormat"
    ));

apiV1.MapGet("/AudienceGender", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().AudienceGenderAllAsync(),
        logger,
        "GET /api/v1/AudienceGender"
    ));

apiV1.MapGet("/AudienceAge", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().AudienceAgeAllAsync(),
        logger,
        "GET /api/v1/AudienceAge"
    ));

apiV1.MapGet("/StatusType", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().ApprovalStatusAllAsync(),
        logger,
        "GET /api/v1/StatusType"
    ));

// Organization Member Endpoints
apiV1.MapGet("/OrganizationMember/{organizationId}/invitations", async (Guid organizationId, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().InvitationsAsync(),
        logger,
        $"GET /api/v1/OrganizationMember/{organizationId}/invitations"
    ))
    .RequireAuthorization();

apiV1.MapPost("/OrganizationMember", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<AddOrganizationMemberDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationMemberPOSTAsync(dto),
        logger,
        "POST /api/v1/OrganizationMember"
    );
})
.RequireAuthorization();

apiV1.MapPost("/OrganizationMember/{id}/accept", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().AcceptAsync(id),
        logger,
        $"POST /api/v1/OrganizationMember/{id}/accept"
    ))
    .RequireAuthorization();

apiV1.MapPost("/OrganizationMember/{id}/decline", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().DeclineAsync(id),
        logger,
        $"POST /api/v1/OrganizationMember/{id}/decline"
    ))
    .RequireAuthorization();

// Organization Review Endpoints
apiV1.MapGet("/OrganizationReview/{organizationId}", async (Guid organizationId, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationReviewAllAsync(organizationId),
        logger,
        $"GET /api/v1/OrganizationReview/{organizationId}"
    ));

apiV1.MapPost("/OrganizationReview", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<CreateOrganizationReviewDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationReviewAsync(dto),
        logger,
        "POST /api/v1/OrganizationReview"
    );
})
.RequireAuthorization();

// StorageObject Endpoints
apiV1.MapGet("/StorageObject", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().StorageObjectAllAsync(),
        logger,
        "GET /api/v1/StorageObject"
    ));

apiV1.MapGet("/StorageObject/{id}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().StorageObjectGETAsync(id),
        logger,
        $"GET /api/v1/StorageObject/{id}"
    ));

apiV1.MapPost("/StorageObject/generate-upload-url", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<UploadRequestDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().GenerateUploadUrlAsync(dto),
        logger,
        "POST /api/v1/StorageObject/generate-upload-url"
    );
})
.RequireAuthorization();

apiV1.MapPost("/StorageObject", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<CreateStorageObjectDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().StorageObjectPOSTAsync(dto),
        logger,
        "POST /api/v1/StorageObject"
    );
})
.RequireAuthorization();

apiV1.MapDelete("/StorageObject/{id}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteVoidAsync(
        () => ctx.GetApiClient().StorageObjectDELETEAsync(id),
        logger,
        $"DELETE /api/v1/StorageObject/{id}"
    ))
    .RequireAuthorization();

// Maps utility endpoint
apiV1.MapGet("/Maps/embed-url", async (string query, IConfiguration config) =>
{
    try
    {
        var apiKey = config["GoogleMaps:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            return Results.Problem("Maps API key not configured", statusCode: 500);
        }

        var embedUrl = $"https://www.google.com/maps/embed/v1/place?key={apiKey}&q={Uri.EscapeDataString(query)}";
        return Results.Content($"\"{embedUrl}\"", "application/json");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating map embed URL");
        return Results.Problem($"Error getting map URL: {ex.Message}");
    }
});

// ============================================================================
// LEGACY BFF ENDPOINTS - For backward compatibility
// These proxy to the same NSwag client methods as above
// TODO: Migrate all clients to use /api/v1 paths and remove these
// ============================================================================

var bff = app.MapGroup("/bff");
var publicBff = bff.MapGroup("/api");

// Organization endpoints (legacy)
publicBff.MapGet("/Organization", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationAllAsync(),
        logger,
        "GET /bff/api/Organization"
    ));

publicBff.MapGet("/Organization/my", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().My2Async(),
        logger,
        "GET /bff/api/Organization/my"
    ))
    .RequireAuthorization();

publicBff.MapGet("/Organization/{id:guid}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationGETAsync(id),
        logger,
        $"GET /bff/api/Organization/{id}"
    ));

publicBff.MapPost("/Organization", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<CreateOrganizationDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationPOSTAsync(dto),
        logger,
        "POST /bff/api/Organization"
    );
})
.RequireAuthorization();

publicBff.MapPut("/Organization/{id:guid}", async (Guid id, HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<UpdateOrganizationDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationPUTAsync(id, dto),
        logger,
        $"PUT /bff/api/Organization/{id}"
    );
})
.RequireAuthorization();

// Event endpoints (legacy)
publicBff.MapGet("/Event/my", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().MyAsync(),
        logger,
        "GET /bff/api/Event/my"
    ))
    .RequireAuthorization();

publicBff.MapPost("/Event", async ([FromBody] CreateEventDto dto, HttpContext ctx, ILogger<Program> log) =>
{
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventPOSTAsync(dto),
        log,
        "POST /bff/api/Event"
    );
})
.RequireAuthorization();

publicBff.MapGet("/Event/{id:guid}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventGETAsync(id),
        logger,
        $"GET /bff/api/Event/{id}"
    ));

publicBff.MapPut("/Event/{id:guid}", async (Guid id, [FromBody] UpdateEventDto dto, HttpContext ctx, ILogger<Program> log) =>
{
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventPUTAsync(id, dto),
        log,
        $"PUT /bff/api/Event/{id}"
    );
})
.RequireAuthorization();

publicBff.MapDelete("/Event/{id:guid}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteVoidAsync(
        () => ctx.GetApiClient().EventDELETEAsync(id),
        logger,
        $"DELETE /bff/api/Event/{id}"
    ))
    .RequireAuthorization();

// User endpoints (legacy)
publicBff.MapPost("/User/sync", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().SyncAsync(),
        logger,
        "POST /bff/api/User/sync"
    ))
    .RequireAuthorization();

// Lookup data endpoints (legacy)
publicBff.MapGet("/EventType", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventTypeAllAsync(),
        logger,
        "GET /bff/api/EventType"
    ));

publicBff.MapGet("/EventFormat", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().EventFormatAllAsync(),
        logger,
        "GET /bff/api/EventFormat"
    ));

publicBff.MapGet("/AudienceGender", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().AudienceGenderAllAsync(),
        logger,
        "GET /bff/api/AudienceGender"
    ));

publicBff.MapGet("/AudienceAge", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().AudienceAgeAllAsync(),
        logger,
        "GET /bff/api/AudienceAge"
    ));

publicBff.MapGet("/StatusType", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().ApprovalStatusAllAsync(),
        logger,
        "GET /bff/api/StatusType"
    ));

// Admin endpoints (legacy)
publicBff.MapGet("/admin/organizations", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationAllAsync(),
        logger,
        "GET /bff/api/admin/organizations"
    ));

publicBff.MapGet("/admin/organizations/{id:guid}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationGETAsync(id),
        logger,
        $"GET /bff/api/admin/organizations/{id}"
    ));

publicBff.MapPut("/admin/organizations/{id}/status", async (Guid id, HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<UpdateOrganizationApprovalStatusDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteVoidAsync(
        () => ctx.GetApiClient().UpdatestatustypeAsync(id, dto),
        logger,
        $"PUT /bff/api/admin/organizations/{id}/status"
    );
});

// Organization Review endpoints (legacy)
var protectedBff = bff.MapGroup("/api").RequireAuthorization();

protectedBff.MapGet("/OrganizationReview/{organizationId}", async (Guid organizationId, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationReviewAllAsync(organizationId),
        logger,
        $"GET /bff/api/OrganizationReview/{organizationId}"
    ));

protectedBff.MapPost("/OrganizationReview", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<CreateOrganizationReviewDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().OrganizationReviewAsync(dto),
        logger,
        "POST /bff/api/OrganizationReview"
    );
});

// User info endpoint
bff.MapGet("/me", (HttpContext ctx) =>
{
    var u = ctx.User;
    return Results.Ok(new
    {
        name = u.Identity?.Name,
        claims = u.Claims.Select(c => new { c.Type, c.Value })
    });
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Explore.Blazor.Client._Imports).Assembly);

app.Run();
