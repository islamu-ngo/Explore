// ABOUTME: bUnit tests for TenantAdminSettings redirects based on public onboarding deployment status.
// ABOUTME: Verifies single-tenant redirects, multi-tenant rendering, and unavailable-status errors.

using Explore.Blazor.Client.Pages.Events;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class TenantAdminSettingsRedirectTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IInstanceOnboardingService _onboardingService;
    private readonly ITenantOnboardingService _tenantOnboardingService;
    private readonly ITenantPublicExperienceAdminService _publicExperienceAdminService;
    private readonly ITenantStorageSettingsAdminService _tenantStorageSettingsAdminService;
    private readonly BunitNavigationManager _nav;

    public TenantAdminSettingsRedirectTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.AddShellStateMocks();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Instance Admin", "admin@example.com");

        _onboardingService = _ctx.AddMockService<IInstanceOnboardingService>();
        _tenantOnboardingService = _ctx.AddMockService<ITenantOnboardingService>();
        _publicExperienceAdminService = _ctx.AddMockService<ITenantPublicExperienceAdminService>();
        _tenantStorageSettingsAdminService = _ctx.AddMockService<ITenantStorageSettingsAdminService>();
        _ctx.AddMockService<ITenantBrandingSettingsAdminService>();
        _publicExperienceAdminService.GetSettingsAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantPublicExperienceAdminModel());
        _tenantStorageSettingsAdminService.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new HalResourceOfTenantStorageSettingsDto());

        _nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        _nav.NavigateTo("/admin/tenant/settings");
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task TenantAdminSettings_SingleTenantMode_RedirectsToInstanceSettings()
    {
        _onboardingService.GetStatusAsync()
            .Returns(new InstanceOnboardingStatusDto { SelectedDeploymentMode = nameof(DeploymentMode.SingleTenant) });

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
        await _tenantOnboardingService.DidNotReceive().GetStatusAsync();
    }

    [Test]
    public async Task TenantAdminSettings_MultiTenantMode_DoesNotRedirect()
    {
        _onboardingService.GetStatusAsync()
            .Returns(new InstanceOnboardingStatusDto { SelectedDeploymentMode = nameof(DeploymentMode.MultiTenant) });

        var componentType = typeof(EventList).Assembly
            .GetTypes()
            .First(type => type.Name == "TenantAdminSettings" && typeof(IComponent).IsAssignableFrom(type));

        _ctx.Render<DynamicComponent>(p =>
            p.Add(x => x.Type, componentType));

        await Assert.That(_nav.Uri).EndsWith("/admin/tenant/settings");
    }

    [Test]
    public async Task TenantAdminSettings_WhenStatusIsUnavailable_ShowsErrorAndDoesNotRedirect()
    {
        _onboardingService.GetStatusAsync()
            .Returns((InstanceOnboardingStatusDto?)null);

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
