using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddMudServices();

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

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

builder.Services.AddScoped<BffClient>();

builder.Services.AddAuthorizationCore();



// Add a basic AuthenticationStateProvider that always returns not authenticated for WebAssembly
builder.Services.AddScoped<AuthenticationStateProvider, AnonymousAuthenticationStateProvider>();

await builder.Build().RunAsync();

// wanted to have webassembly as client for keycloak but went with blazor server the correct choice so these are not needed so commented
//var keycloakConfig = builder.Configuration.GetSection("Keycloak");

//var authority = keycloakConfig["Authority"];
//var clientId = keycloakConfig["ClientId"];
//var realm = keycloakConfig["Realm"];

//if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(clientId))
//{
//    throw new InvalidOperationException("Keycloak configuration is missing");
//}

//builder.Services.AddOidcAuthentication(options =>
//{
//    options.ProviderOptions.Authority = authority;
//    options.ProviderOptions.ClientId = clientId;
//    options.ProviderOptions.ResponseType = "code";
//    options.ProviderOptions.DefaultScopes.Add("openid");
//    options.ProviderOptions.DefaultScopes.Add("profile");
//    options.ProviderOptions.DefaultScopes.Add("email");
//});
