using Duende.AccessTokenManagement.OpenIdConnect;
using Explore.Blazor.Client.Pages;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Components;
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

//using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;
// from this blazor bff template: https://github.com/damienbod/Blazor.BFF.OpenIDConnect.Template/blob/main/BlazorBffOpenIdConnect/Server/Program.cs

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add MudBlazor services + DI
builder.Services.AddMudServices();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IProgramService, ProgramService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IAdminService, AdminService>();

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
builder.Services.AddTransient<AuthorizationHandler>();

// HttpClient for authenticated requests
builder.Services.AddHttpClient("ExploreApi", client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ExploreApi:BaseUrl"]
            ?? "https://localhost:7039/"
        );
    })
    //.AddHttpMessageHandler<AuthorizationHandler>()
    .AddUserAccessTokenHandler()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                // Accepter uniquement pour localhost
                var isLocalhost = message.RequestUri?.Host.Contains("localhost") ?? false;
                return isLocalhost || errors == System.Net.Security.SslPolicyErrors.None;
            };
        }

        return handler;
    });

// HttpClient for public/anonymous requests (no token)
builder.Services.AddHttpClient("ExploreApiPublic", client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ExploreApi:BaseUrl"]
            ?? "https://localhost:7039/"
        );
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                // Accepter uniquement pour localhost
                var isLocalhost = message.RequestUri?.Host.Contains("localhost") ?? false;
                return isLocalhost || errors == System.Net.Security.SslPolicyErrors.None;
            };
        }

        return handler;
    });

//builder.Services.AddHttpClient("ExploreApi", client =>
//    {
//        client.BaseAddress = new Uri(
//            builder.Configuration["ExploreApi:BaseUrl"]
//            ?? "https://localhost:7039/"
//        );
//    })
//    .AddUserAccessTokenHandler()
//    .ConfigurePrimaryHttpMessageHandler(() =>
//    {
//        var handler = new HttpClientHandler();

//        if (builder.Environment.IsDevelopment())
//        {
//            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
//            {
//                // Accepter uniquement pour localhost
//                var isLocalhost = message.RequestUri?.Host.Contains("localhost") ?? false;
//                return isLocalhost || errors == System.Net.Security.SslPolicyErrors.None;
//            };
//        }

//        return handler;
//    });

builder.Services.AddOptions();

//var authBuilder = builder.Services.AddAuthentication("Keycloak");

//authBuilder
//    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddOpenIdConnect(authenticationScheme: "Keycloak", options =>
//    {
//        // From configuration/Infisical
//        options.Authority = builder.Configuration["Keycloak:Authority"];
//        options.ClientId = builder.Configuration["Keycloak:ClientId"];       // explore-blazor-server
//        options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"]; // confidential
//        options.ResponseType = "code";
//        options.UsePkce = true;
//        options.SaveTokens = true; // keep access/refresh tokens in auth cookie
//        options.GetClaimsFromUserInfoEndpoint = true;

//        options.RequireHttpsMetadata = string.Equals(
//            builder.Configuration["Keycloak:RequireHttpsMetadata"],
//            "true",
//            StringComparison.OrdinalIgnoreCase
//        );

//        //Default callback paths:
//        options.CallbackPath = "/signin-oidc";
//        options.SignedOutCallbackPath = "/signout-callback-oidc";

//        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//        options.ResponseType = OpenIdConnectResponseType.Code;

//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            NameClaimType = "preferred_username",
//            RoleClaimType = "roles" // add a Keycloak mapper to emit flat "roles"
//        };

//        //The Scope.Clear() + Scope.Add() pattern exists because:
//        //1.Default scopes: The OIDC handler adds default scopes automatically(openid, profile, and sometimes others depending on the library version)
//        //2.Explicit control: Some developers want to be 100 % sure of what's being requested
//        //3.Legacy / documentation: Many examples show this pattern for clarity 
//        //You don't actually need it

//        //options.Scope.Clear();
//        //options.Scope.Add("openid");
//        //options.Scope.Add("profile");
//        //options.Scope.Add("email");
//        // If you created a custom audience scope for the API, request it here too
//        // options.Scope.Add("aud-identity-api");
//    });

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        //options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        // Optional: secure cookie tweaks
        // o.Cookie.SameSite = SameSiteMode.Lax;
        // o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    //.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    .AddOpenIdConnect(options =>
    {
        // From configuration/Infisical
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.ClientId = builder.Configuration["Keycloak:ClientId"];       // explore-blazor-server
        options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"]; // confidential
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true; // keep access/refresh tokens in auth cookie
        options.GetClaimsFromUserInfoEndpoint = true;

        options.RequireHttpsMetadata = string.Equals(
            builder.Configuration["Keycloak:RequireHttpsMetadata"],
            "true",
            StringComparison.OrdinalIgnoreCase
        );

        //Default callback paths:
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username",
            RoleClaimType = "roles" // add a Keycloak mapper to emit flat "roles"
        };

        //The Scope.Clear() + Scope.Add() pattern exists because:
        //1.Default scopes: The OIDC handler adds default scopes automatically(openid, profile, and sometimes others depending on the library version)
        //2.Explicit control: Some developers want to be 100 % sure of what's being requested
        //3.Legacy / documentation: Many examples show this pattern for clarity 
        //You don't actually need it

        // CRITICAL: Add offline_access to get refresh token and save access token
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access"); // NEEDED to get refresh_token and save tokens
        // If you created a custom audience scope for the API, request it here too
        // options.Scope.Add("aud-identity-api");
    });

// Antiforgery for BFF endpoints
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

// Automatic user access token attach/refresh for calls to explore.api
//builder.Services.AddOpenIdConnectAccessTokenManagement(options =>
//{
//    options.ChallengeScheme = "Keycloak";
//    options.
//});

builder.Services.AddOpenIdConnectAccessTokenManagement();


builder.Services.AddAuthorizationBuilder();
//builder.Services.AddAuthorization();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
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
    app.MapGet("/test-api", async (HttpContext ctx, IHttpClientFactory f) =>
    {
        if (!ctx.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Content(@"
                <html>
                <body>
                    <h1>? Non authentifi�</h1>
                    <p>Vous devez �tre connect� pour tester l'API.</p>
                    <a href='/login?returnUrl=/test-api'>Se connecter</a>
                </body>
                </html>
            ", "text/html");
        }

        try
        {
            var http = f.CreateClient("ExploreApi");

            // R�cup�rer le token pour l'afficher
            var token = await ctx.GetUserAccessTokenAsync();

            // Appel � l'API
            var response = await http.GetAsync("weatherforecast");
            var content = await response.Content.ReadAsStringAsync();

            var html = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 20px; }}
                        .success {{ color: green; }}
                        .error {{ color: red; }}
                        pre {{ background: #f4f4f4; padding: 10px; border-radius: 5px; overflow-x: auto; }}
                        .token {{ word-break: break-all; background: #ffffcc; padding: 10px; }}
                    </style>
                </head>
                <body>
                    <h1>Test d'appel API</h1>
                    
                    <h2>Utilisateur connect�</h2>
                    <p><strong>Nom:</strong> {ctx.User.Identity?.Name}</p>
                    
                    <h2>Access Token (Bearer)</h2>
                    <div class='token'>
                        <small>{token}</small>
                    </div>
                    
                    <h2>R�sultat de l'appel � /weatherforecast</h2>
                    {(response.IsSuccessStatusCode
                        ? $"<p class='success'>? Succ�s - Status: {(int)response.StatusCode} {response.StatusCode}</p>"
                        : $"<p class='error'>? �chec - Status: {(int)response.StatusCode} {response.StatusCode}</p>")}
                    
                    <h3>R�ponse JSON:</h3>
                    <pre>{System.Web.HttpUtility.HtmlEncode(content)}</pre>
                    
                    <h3>Claims utilisateur:</h3>
                    <pre>{string.Join("\n", ctx.User.Claims.Select(c => $"{c.Type}: {c.Value}"))}</pre>
                    
                    <hr>
                    <a href='/'>Retour � l'accueil</a> | 
                    <a href='/test-api'>Rafra�chir</a> | 
                    <a href='/logout'>Se d�connecter</a>
                </body>
                </html>
            ";

            return Results.Content(html, "text/html");
        }
        catch (Exception ex)
        {
            var errorHtml = $@"
                <html>
                <body>
                    <h1 style='color: red;'>? Erreur lors de l'appel API</h1>
                    <h2>Exception:</h2>
                    <pre>{System.Web.HttpUtility.HtmlEncode(ex.ToString())}</pre>
                    <hr>
                    <a href='/test-api'>R�essayer</a> | 
                    <a href='/'>Retour � l'accueil</a>
                </body>
                </html>
            ";
            return Results.Content(errorHtml, "text/html");
        }
    });
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

app.MapGet("/authentication/login", () =>
{
    return TypedResults.Challenge(
        new AuthenticationProperties { RedirectUri = "/" }
        , ["Keycloak"]);
}).AllowAnonymous();

//OIDC endpoints(instant 302 redirects)
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
    // return redirect to home page!
});

// BFF endpoints (server proxies to explore.api with the user token)
var bff = app.MapGroup("/bff");

// Public endpoints (no authentication required)
var publicBff = bff.MapGroup("/api");

publicBff.MapGet("/Program", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync("api/Program");
    
    // Log the response for debugging
    var content = await r.Content.ReadAsStringAsync();
    Console.WriteLine($"Program API Response: {content}");
    
    r.EnsureSuccessStatusCode();
    return Results.Content(content, "application/json");
});

publicBff.MapGet("/Program/{id}", async (Guid id, IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync($"api/Program/{id}");
    
    // Log the response for debugging
    var content = await r.Content.ReadAsStringAsync();
    Console.WriteLine($"Program/{id} API Response Status: {r.StatusCode}");
    Console.WriteLine($"Program/{id} API Response: {content}");
    
    r.EnsureSuccessStatusCode();
    return Results.Content(content, "application/json");
});

publicBff.MapGet("/EventType", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync("api/EventType");
    
    // Log the response for debugging
    var content = await r.Content.ReadAsStringAsync();
    Console.WriteLine($"EventType API Response: {content}");
    
    r.EnsureSuccessStatusCode();
    return Results.Content(content, "application/json");
});

publicBff.MapGet("/AudienceGender", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync("api/AudienceGender");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapGet("/AudienceAge", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync("api/AudienceAge");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapGet("/ProgramType", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync("api/ProgramType");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapPost("/Organization", async (
    HttpContext ctx, 
    IHttpClientFactory f) =>
{
    Console.WriteLine("=== BFF Organization POST Request ===");
    Console.WriteLine($"User authenticated: {ctx.User?.Identity?.IsAuthenticated}");
    Console.WriteLine($"User name: {ctx.User?.Identity?.Name}");
    
    if (ctx.User?.Identity?.IsAuthenticated != true)
    {
        Console.WriteLine("ERROR: User not authenticated!");
        return Results.Unauthorized();
    }
    
    // Get access token from authentication properties
    var accessToken = await ctx.GetTokenAsync("access_token");
    
    if (string.IsNullOrEmpty(accessToken))
    {
        Console.WriteLine("ERROR: No access token found. User may need to re-login with offline_access scope.");
        return Results.Problem("No access token available. Please logout and login again.", statusCode: 401);
    }
    
    Console.WriteLine($"Access token retrieved: {accessToken.Substring(0, Math.Min(20, accessToken.Length))}...");
    
    // Use public client and manually add token
    var http = f.CreateClient("ExploreApiPublic");
    
    // Read organization data
    var org = await ctx.Request.ReadFromJsonAsync<object>();
    
    Console.WriteLine($"Organization data received: {System.Text.Json.JsonSerializer.Serialize(org)}");
    
    try
    {
        // Create request with Authorization header
        var request = new HttpRequestMessage(HttpMethod.Post, "api/Organization");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(org);
        
        Console.WriteLine("Sending request to API with Bearer token...");
        
        var response = await http.SendAsync(request);
        
        Console.WriteLine($"API Response Status: {response.StatusCode}");
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API Error: {errorContent}");
        }
        
        response.EnsureSuccessStatusCode();
        
        return Results.Stream(
            await response.Content.ReadAsStreamAsync(),
            response.Content.Headers.ContentType?.ToString()
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"BFF Exception: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        throw;
    }
});

publicBff.MapGet("/Organization/my", async (HttpContext ctx, IHttpClientFactory f) =>
{
    if (ctx.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    // Get access token
    var accessToken = await ctx.GetTokenAsync("access_token");
    
    if (string.IsNullOrEmpty(accessToken))
    {
        return Results.Problem("No access token available. Please logout and login again.", statusCode: 401);
    }

    var http = f.CreateClient("ExploreApiPublic");
    var request = new HttpRequestMessage(HttpMethod.Get, "api/Organization/my");
    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    
    var response = await http.SendAsync(request);
    response.EnsureSuccessStatusCode();
    
    return Results.Stream(
        await response.Content.ReadAsStreamAsync(),
        response.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapGet("/Event/my", async (HttpContext ctx, IHttpClientFactory f, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("=== BFF Event/my Request ===");
        logger.LogInformation($"User authenticated: {ctx.User?.Identity?.IsAuthenticated}");
        logger.LogInformation($"User name: {ctx.User?.Identity?.Name}");
        
        if (ctx.User?.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("User not authenticated");
            return Results.Unauthorized();
        }

        // Get access token
        var accessToken = await ctx.GetTokenAsync("access_token");
        
        if (string.IsNullOrEmpty(accessToken))
        {
            logger.LogError("No access token found");
            return Results.Problem("No access token available. Please logout and login again.", statusCode: 401);
        }
        
        logger.LogInformation($"Access token retrieved: {accessToken.Substring(0, Math.Min(20, accessToken.Length))}...");

        var http = f.CreateClient("ExploreApiPublic");
        var request = new HttpRequestMessage(HttpMethod.Get, "api/Event/my");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        logger.LogInformation("Sending request to API...");
        var response = await http.SendAsync(request);
        
        logger.LogInformation($"API Response Status: {response.StatusCode}");
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogError($"API Error Response: {errorContent}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return Results.Unauthorized();
            }
            
            return Results.Problem(
                detail: errorContent,
                statusCode: (int)response.StatusCode
            );
        }
        
        return Results.Stream(
            await response.Content.ReadAsStreamAsync(),
            response.Content.Headers.ContentType?.ToString()
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Exception in BFF Event/my endpoint");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500
        );
    }
});

publicBff.MapPost("/Event", async (HttpContext ctx, IHttpClientFactory f) =>
{
    if (ctx.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var accessToken = await ctx.GetTokenAsync("access_token");
    
    if (string.IsNullOrEmpty(accessToken))
    {
        return Results.Problem("No access token available. Please logout and login again.", statusCode: 401);
    }

    var http = f.CreateClient("ExploreApiPublic");
    var request = new HttpRequestMessage(HttpMethod.Post, "api/Event");
    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    request.Content = new StreamContent(ctx.Request.Body)
    {
        Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") }
    };
    
    var response = await http.SendAsync(request);
    
    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        return Results.Problem(
            detail: errorContent,
            statusCode: (int)response.StatusCode
        );
    }
    
    return Results.Stream(
        await response.Content.ReadAsStreamAsync(),
        response.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapGet("/Event/{id:guid}", async (Guid id, HttpContext ctx, IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync($"api/Event/{id}");
    
    if (r.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }
    
    r.EnsureSuccessStatusCode();
    
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapPut("/Event/{id:guid}", async (Guid id, HttpContext ctx, IHttpClientFactory f) =>
{
    if (ctx.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var accessToken = await ctx.GetTokenAsync("access_token");
    
    if (string.IsNullOrEmpty(accessToken))
    {
        return Results.Problem("No access token available. Please logout and login again.", statusCode: 401);
    }

    var http = f.CreateClient("ExploreApiPublic");
    var request = new HttpRequestMessage(HttpMethod.Put, $"api/Event/{id}");
    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    request.Content = new StreamContent(ctx.Request.Body)
    {
        Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") }
    };
    
    var response = await http.SendAsync(request);
    
    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        return Results.Problem(
            detail: errorContent,
            statusCode: (int)response.StatusCode
        );
    }
    
    return Results.Stream(
        await response.Content.ReadAsStreamAsync(),
        response.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapDelete("/Event/{id:guid}", async (Guid id, HttpContext ctx, IHttpClientFactory f) =>
{
    if (ctx.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var accessToken = await ctx.GetTokenAsync("access_token");
    
    if (string.IsNullOrEmpty(accessToken))
    {
        return Results.Problem("No access token available. Please logout and login again.", statusCode: 401);
    }

    var http = f.CreateClient("ExploreApiPublic");
    var request = new HttpRequestMessage(HttpMethod.Delete, $"api/Event/{id}");
    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    
    var response = await http.SendAsync(request);
    
    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }
    
    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        return Results.Problem(
            detail: errorContent,
            statusCode: (int)response.StatusCode
        );
    }
    
    return Results.NoContent();
});

publicBff.MapGet("/Organization/{id:guid}", async (Guid id, HttpContext ctx, IHttpClientFactory f) =>
{
    if (ctx.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var accessToken = await ctx.GetTokenAsync("access_token");
    
    if (string.IsNullOrEmpty(accessToken))
    {
        return Results.Problem("No access token available. Please logout and login again.", statusCode: 401);
    }

    var http = f.CreateClient("ExploreApiPublic");
    var request = new HttpRequestMessage(HttpMethod.Get, $"api/Organization/{id}");
    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    
    var response = await http.SendAsync(request);
    
    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }
    
    response.EnsureSuccessStatusCode();
    
    return Results.Stream(
        await response.Content.ReadAsStreamAsync(),
        response.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapPut("/Organization/{id:guid}", async (Guid id, HttpContext ctx, IHttpClientFactory f) =>
{
    if (ctx.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var accessToken = await ctx.GetTokenAsync("access_token");
    
    if (string.IsNullOrEmpty(accessToken))
    {
        return Results.Problem("No access token available. Please logout and login again.", statusCode: 401);
    }

    var http = f.CreateClient("ExploreApiPublic");
    var request = new HttpRequestMessage(HttpMethod.Put, $"api/Organization/{id}");
    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    request.Content = new StreamContent(ctx.Request.Body)
    {
        Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") }
    };
    
    var response = await http.SendAsync(request);
    response.EnsureSuccessStatusCode();
    
    return Results.Stream(
        await response.Content.ReadAsStreamAsync(),
        response.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapGet("/StatusType", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync("api/StatusType");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapGet("/StatusType", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync("api/StatusType");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

// Admin endpoints
publicBff.MapGet("/admin/organizations", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync("api/Admin/organizations");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapGet("/admin/organizations/{id:guid}", async (Guid id, IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.GetAsync($"api/Admin/organizations/{id}");
    if (r.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

publicBff.MapPut("/admin/organizations/{id}/status", async (Guid id, HttpContext ctx, IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApiPublic");
    var r = await http.PutAsync($"api/Admin/organizations/{id}/status", new StreamContent(ctx.Request.Body)
    {
        Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") }
    });
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

// Protected endpoints (require authentication)
var protectedBff = bff.MapGroup("/api").RequireAuthorization();

// Example GET pass-through
protectedBff.MapGet("/events", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApi"); // Automatically includes user access token
    var r = await http.GetAsync("events");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

protectedBff.MapGet("/weatherforecast", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApi"); // Automatically includes user access token
    var r = await http.GetAsync("weatherforecast");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

// User profile endpoint - uses authenticated client with automatic token management
protectedBff.MapGet("/userprofile/me", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApi"); // Automatically includes user access token via AddUserAccessTokenHandler
    var r = await http.GetAsync("api/userprofile/me");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

// Example POST with CSRF validation
protectedBff.MapPost("/events", async (HttpContext ctx, IHttpClientFactory f) =>
{
    await antiforgery.ValidateRequestAsync(ctx);

    var http = f.CreateClient("ExploreApi"); // Automatically includes user access token
    var req = new HttpRequestMessage(HttpMethod.Post, "events")
    {
        Content = new StreamContent(ctx.Request.Body)
    };
    req.Content.Headers.ContentType =
        new MediaTypeHeaderValue(ctx.Request.ContentType ?? "application/json");

    var r = await http.SendAsync(req);
    r.EnsureSuccessStatusCode();
    return Results.NoContent();
});

// Optional: user info echo
bff.MapGet("/me", (HttpContext ctx) =>
{
    var u = ctx.User;
    return Results.Ok(new
    {
        name = u.Identity?.Name,
        claims = u.Claims.Select(c => new { c.Type, c.Value })
    });
});

// Public endpoint to check authentication status without triggering redirect
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Explore.Blazor.Client._Imports).Assembly);

app.Run();
