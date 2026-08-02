// ABOUTME: Applies the reusable ordered Explore.Blazor middleware pipeline and endpoint graph.
// ABOUTME: Maps YARP and proxy antiforgery only for the registered Split transport profile.

using Blazouter.Extensions;
using Blazouter.Server.Extensions;
using Event.Web.BffHosting.Proxy;
using Event.Web.BffHosting.Security;
using Explore.Blazor.Components;
using Explore.Blazor.Extensions;

namespace Explore.Blazor.Hosting;

public static class BlazorHostApplicationExtensions
{
    public static async Task<WebApplication> InitializeBlazorHostAsync(
        this WebApplication app,
        BlazorHostProfile profile)
    {
        ValidateProfile(app, profile);
        await app.InitializeDynamicAuthSchemesAsync();
        return app;
    }

    public static WebApplication UseBlazorHostMiddleware(
        this WebApplication app,
        BlazorHostProfile profile,
        GracefulShutdownState shutdownState)
    {
        ArgumentNullException.ThrowIfNull(shutdownState);
        ValidateProfile(app, profile);

        if (profile == BlazorHostProfile.Split)
        {
            app.UseForwardedHeadersMiddleware();
        }

        app.UseEventBffAdminHostAccessControl();
        if (profile == BlazorHostProfile.Split)
        {
            app.ConfigureGracefulShutdown(shutdownState);
        }

        app.UseBffSecurityHeaders();
        if (profile == BlazorHostProfile.Split)
        {
            app.MapDefaultEndpoints();
        }

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
            context => !context.Request.Path.StartsWithSegments(
                "/bff",
                StringComparison.OrdinalIgnoreCase),
            branch => branch.UseStatusCodePagesWithReExecute(
                "/errors/{0}",
                createScopeForStatusCodePages: true));
        app.UseHttpsRedirection();
        app.UseStartupRedirectMiddleware();
        app.UsePathTenantResolverMiddleware();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAntiforgeryTokenMiddleware();
        app.UseRequestLocalization();
        app.UseAccessTokenCaptureMiddleware();
        app.UseBffDiagnosticsMiddleware();
        app.UseOnboardingAuthGateMiddleware();
        app.UseAuthorization();
        if (profile == BlazorHostProfile.Split)
        {
            app.UseEventApiProxyAntiforgery();
        }

        app.UseRateLimiter();
        app.UseAntiforgery();
        return app;
    }

    public static WebApplication MapBlazorHostEndpoints(
        this WebApplication app,
        BlazorHostProfile profile)
    {
        ValidateProfile(app, profile);

        app.MapControllers();

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/test-endpoint",
                () => Results.Ok(new
                {
                    message = "Server endpoint works!",
                    timestamp = DateTime.UtcNow
                }))
                .WithName("TestEndpoint");
        }

        BffAuthEndpoints.MapAuthEndpoints(app);
        BffEndpointExtensions.MapBffEndpoints(app);
        app.MapStaticAssets();

        if (profile == BlazorHostProfile.Split)
        {
            app.MapReverseProxy();
        }

        app.MapRazorComponents<App>()
            .AddBlazouterSupport()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Explore.Blazor.Client._Imports).Assembly);

        return app;
    }

    private static void ValidateProfile(WebApplication app, BlazorHostProfile profile)
    {
        ArgumentNullException.ThrowIfNull(app);
        var registeredProfile = app.Services
            .GetRequiredService<BlazorHostProfileRegistration>()
            .Profile;
        if (registeredProfile != profile)
        {
            throw new InvalidOperationException(
                $"Blazor host profile '{profile}' does not match registered profile '{registeredProfile}'.");
        }
    }
}
