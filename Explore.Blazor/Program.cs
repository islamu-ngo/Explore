using Blazouter.Extensions;
using Blazouter.Server.Extensions;
using Explore.Blazor;
using Explore.Blazor.Components;
using Explore.Blazor.Extensions;
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

builder.Services.AddSecretManagement(builder.Configuration);

// ──────────────────────────────────────────────
// Service Registration
// ──────────────────────────────────────────────

builder.Services.AddMudServices();
builder.Services.AddApplicationServices();
builder.Services.AddServerOnlyServices(builder.Configuration);
builder.Services.AddApiHttpClients(builder.Configuration, builder.Environment);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true);

builder.Services.AddBlazouter();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions();

builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddBffReverseProxy(builder.Configuration, builder.Environment);

builder.Services.AddAuthorizationBuilder();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Shutdown-aware health check for zero-downtime deployments (Coolify rolling updates)
builder.Services.AddHealthChecks()
    .AddCheck("shutdown", () =>
    {
        if (shutdownState.IsShuttingDown)
            return HealthCheckResult.Unhealthy("Application is shutting down");
        return HealthCheckResult.Healthy();
    }, tags: ["live", "ready"]);

// ──────────────────────────────────────────────
// Middleware Pipeline
// ──────────────────────────────────────────────

var app = builder.Build();

// Initialize dynamic auth schemes from DB + env vars (Keycloak, Google, ATProto).
// Must run after Build() so that IAuthenticationSchemeProvider and DI are available.
await app.InitializeDynamicAuthSchemesAsync();

app.UseForwardedHeadersMiddleware();
app.ConfigureGracefulShutdown(shutdownState);
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

app.UseHttpsRedirection();
app.UseAntiforgeryTokenMiddleware();
app.UseStartupRedirectMiddleware();
app.UseRouting();
app.UseAuthentication();
app.UseAccessTokenCaptureMiddleware();
app.UseBffDiagnosticsMiddleware();
app.UseAuthorization();
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

app.MapAuthEndpoints();
app.MapBffEndpoints();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddBlazouterSupport()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Explore.Blazor.Client._Imports).Assembly);

app.MapReverseProxy();

await app.RunAsync();
