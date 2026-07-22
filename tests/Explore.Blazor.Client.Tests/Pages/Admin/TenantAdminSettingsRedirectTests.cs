// ABOUTME: bUnit tests for tenant settings availability across deployment modes.
// ABOUTME: Verifies tenant administrators are never redirected into instance-only administration.

using Explore.Blazor.Client.Contracts.Services.Shell;
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
        _ctx.AddMockService<IUiShellContextService>();
        _ctx.AddMockService<IShellPreferencesService>();
        _publicExperienceAdminService.GetSettingsAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantPublicExperienceAdminModel());
        _tenantStorageSettingsAdminService.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new HalResourceOfTenantStorageSettingsDto());

        _nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        _nav.NavigateTo("/settings/tenant");
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task TenantAdminSettings_SingleTenantMode_DoesNotRedirectTenantAdministrator()
    {
        _onboardingService.GetStatusAsync()
            .Returns(new InstanceOnboardingStatusDto { SelectedDeploymentMode = nameof(DeploymentMode.SingleTenant) });

        var componentType = typeof(EventList).Assembly
            .GetTypes()
            .First(type => type.Name == "TenantAdminSettings" && typeof(IComponent).IsAssignableFrom(type));

        _ctx.Render<DynamicComponent>(p =>
            p.Add(x => x.Type, componentType));

        await Assert.That(_nav.Uri).EndsWith("/settings/tenant");
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

        await Assert.That(_nav.Uri).EndsWith("/settings/tenant");
    }

}
