using Blazouter.Extensions;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddMudServices();
builder.Services.AddBlazouter();

// Register the message handler that adds credentials to requests
builder.Services.AddTransient<BrowserCredentialsMessageHandler>();

// Register handler for 401 responses that triggers a server-side login
builder.Services.AddTransient<BffUnauthorizedHandler>();

// Configure default HttpClient for WASM with credentials
// This HttpClient sends cookies with all requests
builder.Services.AddHttpClient("BffClient", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<BrowserCredentialsMessageHandler>()
.AddHttpMessageHandler<BffUnauthorizedHandler>();

// Register a default HttpClient for general use (also with credentials)
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("BffClient");
});

// Register NSwag-generated API client for WASM
// In WASM mode, the client calls through BFF endpoints (same origin)
// The BFF handles authentication token attachment
// IMPORTANT: AddHttpMessageHandler to send credentials (cookies) with every request
builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
{
    // Use base address pointing to self - BFF will proxy to API
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<BrowserCredentialsMessageHandler>()
.AddHttpMessageHandler<BffUnauthorizedHandler>();

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
builder.Services.AddScoped<IAudienceAgeService, AudienceAgeService>();
builder.Services.AddScoped<IAudienceGenderService, AudienceGenderService>();
builder.Services.AddScoped<IEventFormatService, EventFormatService>();
builder.Services.AddScoped<IEventStatusService, EventStatusService>();
builder.Services.AddScoped<IEventTypeService, EventTypeService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<IMadhabService, MadhabService>();
builder.Services.AddScoped<IEventSessionSpeakerService, EventSessionSpeakerService>();
builder.Services.AddScoped<IActorService, ActorService>();

// Register AuthStateService for centralized auth context
builder.Services.AddScoped<IAuthStateService, AuthStateService>();

// Register named HTTP client for S3 uploads (ImageStorageService)
builder.Services.AddHttpClient("S3Upload", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Allow large file uploads
});

builder.Services.AddScoped<BffClient>();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

// Add logging for debugging in WASM
builder.Logging.SetMinimumLevel(LogLevel.Information);

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
