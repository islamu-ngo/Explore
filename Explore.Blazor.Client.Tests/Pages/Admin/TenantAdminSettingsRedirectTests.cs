// ABOUTME: bUnit tests for TenantAdminSettings redirect behaviour based on deployment mode.
// ABOUTME: Verifies single-tenant redirects, multi-tenant no-redirect, and error fallback rendering.

using Explore.Blazor.Client.Pages.Events;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class TenantAdminSettingsRedirectTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IInstanceOnboardingService _onboardingService;
    private readonly BunitNavigationManager _nav;

    public TenantAdminSettingsRedirectTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.AddShellStateMocks();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Instance Admin", "admin@example.com");

        _onboardingService = _ctx.AddMockService<IInstanceOnboardingService>();
        _ctx.AddMockService<ITenantOnboardingService>();

        _nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        _nav.NavigateTo("/admin/tenant/settings");
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task TenantAdminSettings_SingleTenantMode_RedirectsToInstanceSettings()
    {
        _onboardingService.GetDeploymentModeAsync()
            .Returns(new DeploymentModeModel { Mode = "SingleTenant" });

        var componentType = typeof(EventList).Assembly
            .GetTypes()
            .First(type => type.Name == "TenantAdminSettings" && typeof(IComponent).IsAssignableFrom(type));

        var cut = _ctx.Render<DynamicComponent>(p =>
            p.Add(x => x.Type, componentType));

        cut.WaitForAssertion(() =>
        {
            if (!_nav.Uri.Contains("/admin/instance/settings", StringComparison.Ordinal))
                throw new InvalidOperationException("Expected redirect to /admin/instance/settings.");
        });

        await Assert.That(_nav.Uri).Contains("/admin/instance/settings");
    }

    [Test]
    public async Task TenantAdminSettings_MultiTenantMode_DoesNotRedirect()
    {
        _onboardingService.GetDeploymentModeAsync()
            .Returns(new DeploymentModeModel { Mode = "MultiTenant" });

        var componentType = typeof(EventList).Assembly
            .GetTypes()
            .First(type => type.Name == "TenantAdminSettings" && typeof(IComponent).IsAssignableFrom(type));

        _ctx.Render<DynamicComponent>(p =>
            p.Add(x => x.Type, componentType));

        await Assert.That(_nav.Uri).EndsWith("/admin/tenant/settings");
    }

    [Test]
    public async Task TenantAdminSettings_WhenModeResolutionThrows_ShowsErrorAndDoesNotRedirect()
    {
        _onboardingService.GetDeploymentModeAsync()
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        var componentType = typeof(EventList).Assembly
            .GetTypes()
            .First(type => type.Name == "TenantAdminSettings" && typeof(IComponent).IsAssignableFrom(type));

        var cut = _ctx.Render<DynamicComponent>(p =>
            p.Add(x => x.Type, componentType));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Unable to determine deployment mode", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Expected error message not rendered.");
        });

        await Assert.That(_nav.Uri).EndsWith("/admin/tenant/settings");
        await Assert.That(cut.Markup).Contains("Unable to determine deployment mode", StringComparison.OrdinalIgnoreCase);
    }
}
