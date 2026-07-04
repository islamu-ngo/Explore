using Blazouter.Extensions;
using Blazouter.Server.Extensions;
using Event.Web.BffHosting.Authentication;
using Event.Web.BffHosting.Extensions;
using Explore.Blazor;
using Explore.Blazor.Components;
using Explore.Blazor.Extensions;
using Explore.Blazor.HealthChecks;
using Explore.Blazor.Services;
using Explore.Persistence;
using Explore.Secrets.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MudBlazor.Services;
using Serilog;

// ──────────────────────────────────────────────
// Builder Configuration
// ──────────────────────────────────────────────

var shutdownState = new GracefulShutdownState();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext(),
    writeToProviders: true);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout =
        TimeSpan.FromSeconds(GracefulShutdownState.GracePeriodSeconds + 5);
});

builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout =
        TimeSpan.FromSeconds(GracefulShutdownState.GracePeriodSeconds + 5);
});

builder.Configuration.AddInfisicalBlazorCompatibility();

builder.AddServiceDefaults();
builder.AddResilientDistributedCache(connectionName: "cache");
builder.AddDistributedCacheReadinessCheck();
builder.AddOidcDiscoveryReadinessCheck();

builder.Services.AddSecretManagement(builder.Configuration);

// ──────────────────────────────────────────────
// Service Registration
// ──────────────────────────────────────────────

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
builder.Services.AddApplicationServices();
builder.Services.AddServerOnlyServices(builder.Configuration);
builder.Services.AddApiHttpClients(builder.Configuration, builder.Environment);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options =>
    {
        options.SerializationCallback = AuthStateSerializationPolicy.SerializeDisplaySafeClaimsAsync;
    });

builder.Services.AddBlazouter();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions();

builder.Services.AddEventBffHosting(
    builder.Configuration,
    builder.Environment,
    EventBffHostProfile.PublicWeb);
builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddBffRateLimiting(builder.Configuration, builder.Environment);
builder.Services.AddBffReverseProxy(builder.Configuration, builder.Environment);

builder.Services.AddAuthorizationBuilder();
builder.Services.AddCascadingAuthenticationState();

// ──────────────────────────────────────────────
// Localization — CultureRegistry is the compile-time allowlist.
// Runtime governance (enabled_languages / kill-switches) is enforced higher up.
// ──────────────────────────────────────────────
builder.Services.AddLocalization();
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    var cultures = Explore.Domain.Common.Localization.CultureRegistry.GetAll()
        .Select(entry => new System.Globalization.CultureInfo(entry.Code))
        .ToArray();

    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en");
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider());
    options.RequestCultureProviders.Insert(1, new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider());
});

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Shutdown-aware health check for zero-downtime deployments (Coolify rolling updates)
builder.Services.AddHealthChecks()
    .AddCheck("shutdown", () =>
    {
        if (shutdownState.IsShuttingDown)
            return HealthCheckResult.Unhealthy("Application is shutting down");
        return HealthCheckResult.Healthy();
    }, tags: ["live", "ready"])
    .AddDbContextCheck<ExploreDbContext>("database", tags: ["ready"])
    .AddCheck<ApiReadinessHealthCheck>(
        "explore-api",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "api", "infrastructure"]);

// ──────────────────────────────────────────────
// Middleware Pipeline
// ──────────────────────────────────────────────

var app = builder.Build();

// Initialize dynamic auth schemes from DB + env vars (Keycloak, Google, ATProto).
// Must run after Build() so that IAuthenticationSchemeProvider and DI are available.
await app.InitializeDynamicAuthSchemesAsync();

app.UseForwardedHeadersMiddleware();
app.ConfigureGracefulShutdown(shutdownState);
app.UseBffSecurityHeaders();
app.MapDefaultEndpoints();

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

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/bff", StringComparison.OrdinalIgnoreCase),
    branch => branch.UseStatusCodePagesWithReExecute("/errors/{0}", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();
app.UseAntiforgeryTokenMiddleware();
app.UseStartupRedirectMiddleware();
app.UsePathTenantResolverMiddleware();
app.UseRouting();
app.UseAuthentication();
app.UseRequestLocalization();
app.UseAccessTokenCaptureMiddleware();
app.UseBffDiagnosticsMiddleware();
app.UseOnboardingAuthGateMiddleware();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

// ──────────────────────────────────────────────
// Endpoints
// ──────────────────────────────────────────────

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/test-endpoint",
        () => Results.Ok(new { message = "Server endpoint works!", timestamp = DateTime.UtcNow }))
        .WithName("TestEndpoint");
}

BffAuthEndpoints.MapAuthEndpoints(app);
BffEndpointExtensions.MapBffEndpoints(app);
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddBlazouterSupport()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Explore.Blazor.Client._Imports).Assembly);

app.MapReverseProxy();

await app.RunAsync();
