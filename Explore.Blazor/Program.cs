using MudBlazor.Services;
using Explore.Blazor.Client.Pages;
using Explore.Blazor.Components;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add custom services
builder.Services.AddScoped<IEventService, EventService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
    {
        o.LoginPath = "/login";
        o.LogoutPath = "/logout";
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        // From configuration/Infisical
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.ClientId = builder.Configuration["Keycloak:ClientId"];       // explore-web
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

        // Default callback paths:
        // options.CallbackPath = "/signin-oidc";
        // options.SignedOutCallbackPath = "/signout-callback-oidc";

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

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Explore.Blazor.Client._Imports).Assembly);

app.Run();
