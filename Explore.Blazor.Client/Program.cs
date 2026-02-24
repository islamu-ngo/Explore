using Blazouter.Extensions;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Configuration;
using Explore.Blazor.Client.Extensions;
using Explore.Blazor.Client.Routing.Guards;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Contracts;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();
builder.Services.AddBlazouter();
builder.Services.AddScoped<AuthenticatedRouteGuard>();
builder.Services.AddScoped<AdminRouteGuard>();
builder.Services.AddScoped<TenantAdminRouteGuard>();
builder.Services.AddScoped<OrgAdminRouteGuard>();
builder.Services.AddScoped<GroupAdminRouteGuard>();

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

// Register TenantConfiguration for single-tenant mode (default)
// In WASM, configuration section may not be available, so defaults from TenantConfiguration class are used
builder.Services.Configure<TenantConfiguration>(
    builder.Configuration.GetSection(TenantConfiguration.SectionName));

// Register lazy assembly loader for WASM lazy loading
builder.Services.AddScoped<ILazyAssemblyLoader, LazyAssemblyLoaderService>();

// ──────────────────────────────────────────────
// Shared application services (Server + WASM)
// ──────────────────────────────────────────────
builder.Services.AddSharedApplicationServices();

// WASM-specific services (different from server-side registrations)
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ITenantNavigationService, TenantNavigationService>();
builder.Services.AddScoped<IAnalyticsInterop, AnalyticsInterop>();

// Register message handler for S3 cross-origin uploads
builder.Services.AddTransient<S3UploadMessageHandler>();

// Register named HTTP client for S3 uploads (ImageStorageService)
// Uses S3UploadMessageHandler to set CORS mode for cross-origin PUT requests to Hetzner Object Storage
builder.Services.AddHttpClient("S3Upload", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Allow large file uploads
})
.AddHttpMessageHandler<S3UploadMessageHandler>();

builder.Services.AddScoped<BffClient>();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

// Add logging for debugging in WASM
builder.Logging.SetMinimumLevel(LogLevel.Information);

await builder.Build().RunAsync();
