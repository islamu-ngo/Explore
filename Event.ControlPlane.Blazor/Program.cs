// ABOUTME: Composition root for the self-hostable Event control-plane Blazor BFF host.
// ABOUTME: Wires Infisical/env configuration, Keycloak OIDC, server-side BFF proxying, and Razor components.

using Event.ControlPlane.Blazor.Components;
using Event.ControlPlane.Blazor.Clients;
using Event.ControlPlane.Blazor.Extensions;
using Event.ControlPlane.Blazor.Services;
using Event.ControlPlane.Client;
using Event.ControlPlane.Client.Extensions;
using Event.ControlPlane.Client.Services;
using Event.Web.BffHosting.Authentication;
using Event.Web.BffHosting.Endpoints;
using Event.Web.BffHosting.Extensions;
using Event.Web.BffHosting.Proxy;
using Event.Web.BffHosting.Security;
using Explore.Secrets.Extensions;
using MudBlazor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext(),
    writeToProviders: true);

builder.Configuration.AddInfisicalControlPlaneCompatibility();

builder.AddServiceDefaults();
builder.AddOidcDiscoveryReadinessCheck();

builder.Services.AddSecretManagement(
    builder.Configuration,
    enableAuditing: true,
    enableRefreshService: true,
    enableSecretResolution: false);

builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions();

builder.Services.AddEventBffHosting(
    builder.Configuration,
    builder.Environment,
    EventBffHostProfile.ControlPlane);
builder.Services.AddEventBffKeycloakAuthentication(
    builder.Configuration,
    builder.Environment,
    EventBffHostProfile.ControlPlane);
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddEventApiProxy(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IEventBffCookieSessionHandler, ControlPlaneBffCookieSessionHandler>();

builder.Services.AddMudServices(config =>
{
    config.PopoverOptions.Duration = TimeSpan.FromMilliseconds(300);
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
});
builder.Services.AddEventControlPlaneClient();
builder.Services.AddTransient<EventBffBearerForwardingHandler>();
builder.Services.AddHttpClient(ControlPlaneBffCookieSessionHandler.AdminAuthorityHttpClientName, client =>
{
    client.BaseAddress = new Uri(EventApiBaseAddressResolver.Resolve(builder.Configuration));
    client.Timeout = TimeSpan.FromSeconds(5);
})
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false });
builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
    client.BaseAddress = new Uri(EventApiBaseAddressResolver.Resolve(builder.Configuration)))
    .AddHttpMessageHandler<EventBffBearerForwardingHandler>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false });
builder.Services.AddScoped<ControlPlaneApiAdapter>();
builder.Services.AddScoped<IControlPlaneOverviewService>(provider =>
    provider.GetRequiredService<ControlPlaneApiAdapter>());
builder.Services.AddScoped<IControlPlaneTenantService>(provider =>
    provider.GetRequiredService<ControlPlaneApiAdapter>());
builder.Services.AddScoped<IControlPlaneDomainService>(provider =>
    provider.GetRequiredService<ControlPlaneApiAdapter>());
builder.Services.AddScoped<IControlPlaneOperationsService>(provider =>
    provider.GetRequiredService<ControlPlaneApiAdapter>());
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseEventBffForwardedHeaders();
app.UseEventBffSecurityHeaders();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseEventBffAntiforgeryToken();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapEventBffAuthEndpoints();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(ControlPlaneClientAssembly.Value)
    .RequireAuthorization(EventBffAuthorizationPolicies.ControlPlaneAccess);

app.MapReverseProxy()
    .RequireAuthorization(EventBffAuthorizationPolicies.ControlPlaneAccess);

await app.RunAsync();
