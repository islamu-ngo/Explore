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
//using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;
// from this blazor bff template: https://github.com/damienbod/Blazor.BFF.OpenIDConnect.Template/blob/main/BlazorBffOpenIdConnect/Server/Program.cs

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add MudBlazor services + DI
builder.Services.AddMudServices();
builder.Services.AddScoped<IEventService, EventService>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient("ExploreApi", client =>
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
                // Accepter uniquement pour localhost
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
        
        //options.Scope.Clear();
        //options.Scope.Add("openid");
        //options.Scope.Add("profile");
        //options.Scope.Add("email");
        // If you created a custom audience scope for the API, request it here too
        // options.Scope.Add("aud-identity-api");
    });

builder.Services.AddAuthorization();

// Antiforgery for BFF endpoints
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

// Automatic user access token attach/refresh for calls to explore.api
builder.Services.AddOpenIdConnectAccessTokenManagement();

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
                    <h1>? Non authentifié</h1>
                    <p>Vous devez être connecté pour tester l'API.</p>
                    <a href='/login?returnUrl=/test-api'>Se connecter</a>
                </body>
                </html>
            ", "text/html");
        }

        try
        {
            var http = f.CreateClient("ExploreApi");

            // Récupérer le token pour l'afficher
            var token = await ctx.GetUserAccessTokenAsync();

            // Appel à l'API
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
                    
                    <h2>Utilisateur connecté</h2>
                    <p><strong>Nom:</strong> {ctx.User.Identity?.Name}</p>
                    
                    <h2>Access Token (Bearer)</h2>
                    <div class='token'>
                        <small>{token}</small>
                    </div>
                    
                    <h2>Résultat de l'appel à /weatherforecast</h2>
                    {(response.IsSuccessStatusCode
                        ? $"<p class='success'>? Succès - Status: {(int)response.StatusCode} {response.StatusCode}</p>"
                        : $"<p class='error'>? Échec - Status: {(int)response.StatusCode} {response.StatusCode}</p>")}
                    
                    <h3>Réponse JSON:</h3>
                    <pre>{System.Web.HttpUtility.HtmlEncode(content)}</pre>
                    
                    <h3>Claims utilisateur:</h3>
                    <pre>{string.Join("\n", ctx.User.Claims.Select(c => $"{c.Type}: {c.Value}"))}</pre>
                    
                    <hr>
                    <a href='/'>Retour à l'accueil</a> | 
                    <a href='/test-api'>Rafraîchir</a> | 
                    <a href='/logout'>Se déconnecter</a>
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
                    <a href='/test-api'>Réessayer</a> | 
                    <a href='/'>Retour à l'accueil</a>
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

// OIDC endpoints (instant 302 redirects)
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
var bff = app.MapGroup("/bff").RequireAuthorization();

// Example GET pass-through
bff.MapGet("/events", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApi");
    var r = await http.GetAsync("events");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

bff.MapGet("/weatherforecast", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("ExploreApi");
    var r = await http.GetAsync("weatherforecast");
    r.EnsureSuccessStatusCode();
    return Results.Stream(
        await r.Content.ReadAsStreamAsync(),
        r.Content.Headers.ContentType?.ToString()
    );
});

// Example POST with CSRF validation
bff.MapPost("/events", async (HttpContext ctx, IHttpClientFactory f) =>
{
    await antiforgery.ValidateRequestAsync(ctx);

    var http = f.CreateClient("ExploreApi");
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Explore.Blazor.Client._Imports).Assembly);

app.Run();
