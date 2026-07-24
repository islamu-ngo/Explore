// ABOUTME: bUnit tests for tenant settings availability across deployment modes.
// ABOUTME: Verifies tenant administrators are never redirected into instance-only administration.

using System.Text.Json;
using AngleSharp.Dom;
using Explore.Blazor.Client.Contracts.Services.Shell;
using Explore.Blazor.Client.Pages.Events;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class TenantAdminSettingsRedirectTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IInstanceOnboardingService _onboardingService;
    private readonly ITenantOnboardingService _tenantOnboardingService;
    private readonly ITenantBrandingSettingsAdminService _tenantBrandingSettingsAdminService;
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
        _tenantBrandingSettingsAdminService = _ctx.AddMockService<ITenantBrandingSettingsAdminService>();
        _ctx.AddMockService<IOrganizationService>();
        _ctx.AddMockService<IUiShellContextService>();
        _ctx.AddMockService<IShellPreferencesService>();
        _publicExperienceAdminService.GetSettingsAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantPublicExperienceAdminModel());
        _tenantStorageSettingsAdminService.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new HalResourceOfTenantStorageSettingsDto());

        _nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
        _nav.NavigateTo("/settings/admin");
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

        await Assert.That(_nav.Uri).EndsWith("/settings/admin");
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

        await Assert.That(_nav.Uri).EndsWith("/settings/admin");
    }

    [Test]
    public async Task TenantAdminSettingsLayout_RoleMetadataWithoutManageHal_FailsClosed()
    {
        _tenantOnboardingService.GetStatusAsync()
            .Returns(new TenantOnboardingStatusDto
            {
                IsAuthenticated = true,
                IsCurrentUserTenantAdministrator = true,
                IsCurrentUserPlatformAdministrator = true,
                TenantId = Guid.NewGuid()
            });
        Type layoutType = typeof(EventList).Assembly
            .GetTypes()
            .First(type => type.Name == "TenantAdminSettingsLayout" && typeof(IComponent).IsAssignableFrom(type));

        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, layoutType));
        cut.WaitForState(() => cut.Markup.Contains(
            "You do not have tenant administrator permissions",
            StringComparison.Ordinal));

        await Assert.That(cut.Markup).DoesNotContain("Event & Organization Policies", StringComparison.Ordinal);
        await _tenantOnboardingService.DidNotReceive().GetSettingsAsync();
    }

    [Test]
    public async Task TenantAdminSettingsLayout_PoliciesSection_DoesNotExposeBroadSave()
    {
        var tenantStatus = new TenantOnboardingStatusDto();
        tenantStatus.AdditionalProperties["_links"] = JsonSerializer.SerializeToElement(
            new Dictionary<string, object>
            {
                ["manage-tenant-settings"] = new { href = "/api/tenant-onboarding/policy-settings" }
            });
        _tenantOnboardingService.GetStatusAsync().Returns(tenantStatus);
        _tenantOnboardingService.GetManagementSettingsAsync().Returns(new TenantPolicySettingsDto());
        _onboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusDto
        {
            SelectedDeploymentMode = nameof(DeploymentMode.MultiTenant)
        });
        _tenantBrandingSettingsAdminService.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantBrandingSettingsAdminModel());

        Type layoutType = typeof(EventList).Assembly
            .GetTypes()
            .First(type => type.Name == "TenantAdminSettingsLayout" && typeof(IComponent).IsAssignableFrom(type));
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, layoutType));
        cut.WaitForState(() => cut.Markup.Contains("Event & Organization Policies", StringComparison.Ordinal));

        await Assert.That(cut.Markup).DoesNotContain("Save Tenant Settings", StringComparison.Ordinal);
        await _tenantOnboardingService.DidNotReceive().UpdateSettingsAsync(
            Arg.Any<TenantPolicySettingsDto>(),
            Arg.Any<bool>());
    }

    [Test]
    public async Task TenantAdminSettingsLayout_BrandingAndStorage_DoNotExposeBroadSave()
    {
        var management = CreateManagementModel();
        management.CanOverrideStorage = true;
        ConfigureAuthorizedManagement(management);
        _tenantBrandingSettingsAdminService.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantBrandingSettingsAdminModel
            {
                Exists = true,
                CanReplace = true,
                CanChangeDisplayName = true,
                ConcurrencyStamp = Guid.NewGuid()
            });
        _tenantStorageSettingsAdminService.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new HalResourceOfTenantStorageSettingsDto
            {
                TenantOverridesAllowed = true,
                TenantStorageLocked = false,
                IsReadOnly = false,
                _links = new Dictionary<string, HalLink>
                {
                    ["edit"] = new() { Href = "/api/tenant/settings/storage", Method = "PATCH" }
                }
            }.InitializeForEditing());
        var cut = RenderLayout();

        NavigateTo(cut, "Branding");
        await Assert.That(cut.Markup).Contains("Brand display name", StringComparison.Ordinal);
        await Assert.That(cut.Markup).DoesNotContain("Save Tenant Settings", StringComparison.Ordinal);

        NavigateTo(cut, "Object Storage");
        await Assert.That(cut.Markup).Contains("Tenant Storage Provider", StringComparison.Ordinal);
        await Assert.That(cut.Markup).DoesNotContain("Save Tenant Settings", StringComparison.Ordinal);
    }

    [Test]
    public async Task TenantAdminSettingsLayout_PublicExperience_KeepsBroadSaveAffordance()
    {
        ConfigureAuthorizedManagement(CreateManagementModel());
        var cut = RenderLayout();

        NavigateTo(cut, "Public Experience");

        await Assert.That(cut.Markup).Contains("Save Tenant Settings", StringComparison.Ordinal);
    }

    [Test]
    public async Task TenantAdminSettingsLayout_NavigatingWhilePolicyWritePending_BlocksBroadSaveUntilConfirmedModelSync()
    {
        var model = CreateManagementModel();
        ConfigureAuthorizedManagement(model);
        var pending = new TaskCompletionSource<BaseCommandResponseOfGuid>();
        _tenantOnboardingService.UpdateTenantSettingAsync(
                "events.user_submission_enabled",
                "true",
                Arg.Any<CancellationToken>())
            .Returns(pending.Task);
        _tenantOnboardingService.UpdateSettingsAsync(Arg.Any<TenantPolicySettingsDto>(), Arg.Any<bool>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });
        var cut = RenderLayout();
        cut.WaitForState(() => PolicyInput(cut, "Allow users to submit events").HasAttribute("disabled") == false);

        Task exactWrite = cut.InvokeAsync(() => PolicyInput(cut, "Allow users to submit events").Change(true));
        cut.WaitForState(() => PolicyInput(cut, "Allow users to submit events").HasAttribute("disabled"));
        NavigateTo(cut, "Domain");
        IElement broadSave = BroadSaveButton(cut);

        await Assert.That(broadSave.HasAttribute("disabled")).IsTrue();
        broadSave.Click();
        await _tenantOnboardingService.DidNotReceive().UpdateSettingsAsync(
            Arg.Any<TenantPolicySettingsDto>(),
            Arg.Any<bool>());

        pending.SetResult(new BaseCommandResponseOfGuid { Success = true });
        await exactWrite;
        cut.WaitForState(() => BroadSaveButton(cut).HasAttribute("disabled") == false);
        BroadSaveButton(cut).Click();

        await _tenantOnboardingService.Received(1).UpdateSettingsAsync(
            Arg.Is<TenantPolicySettingsDto>(settings => settings.AllowUserSubmittedEvents == true),
            false);
    }

    [Test]
    public async Task TenantAdminSettingsLayout_PolicyRecoveryRestoresModelBeforeBroadSaveReenables()
    {
        var model = CreateManagementModel();
        ConfigureAuthorizedManagement(model);
        var pending = new TaskCompletionSource<BaseCommandResponseOfGuid>();
        _tenantOnboardingService.UpdateTenantSettingAsync(
                "events.user_submission_enabled",
                "true",
                Arg.Any<CancellationToken>())
            .Returns(pending.Task);
        _tenantOnboardingService.UpdateSettingsAsync(Arg.Any<TenantPolicySettingsDto>(), Arg.Any<bool>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });
        var cut = RenderLayout();
        cut.WaitForState(() => PolicyInput(cut, "Allow users to submit events").HasAttribute("disabled") == false);

        Task exactWrite = cut.InvokeAsync(() => PolicyInput(cut, "Allow users to submit events").Change(true));
        cut.WaitForState(() => PolicyInput(cut, "Allow users to submit events").HasAttribute("disabled"));
        NavigateTo(cut, "Domain");
        await Assert.That(BroadSaveButton(cut).HasAttribute("disabled")).IsTrue();

        pending.SetResult(new BaseCommandResponseOfGuid { Success = false });
        await exactWrite;
        cut.WaitForState(() => BroadSaveButton(cut).HasAttribute("disabled") == false);
        BroadSaveButton(cut).Click();

        await _tenantOnboardingService.Received(1).UpdateSettingsAsync(
            Arg.Is<TenantPolicySettingsDto>(settings => settings.AllowUserSubmittedEvents == false),
            false);
    }

    [Test]
    public async Task TenantAdminSettingsLayout_WhenManagementSettingsLoadFails_RendersAlertWithoutWritableSurface()
    {
        ConfigureAuthorizedManagement(settings: null);

        var cut = RenderLayout();
        cut.WaitForState(() => cut.Markup.Contains("Tenant settings could not be loaded", StringComparison.Ordinal));

        await Assert.That(cut.FindAll("[role='alert']").Any(element =>
            element.TextContent.Contains("Tenant settings could not be loaded", StringComparison.Ordinal))).IsTrue();
        await Assert.That(cut.Markup).DoesNotContain("Event & Organization Policies", StringComparison.Ordinal);
        await Assert.That(cut.Markup).DoesNotContain("Save Tenant Settings", StringComparison.Ordinal);
    }

    private void ConfigureAuthorizedManagement(TenantPolicySettingsDto? settings)
    {
        var tenantStatus = new TenantOnboardingStatusDto();
        tenantStatus.AdditionalProperties["_links"] = JsonSerializer.SerializeToElement(
            new Dictionary<string, object>
            {
                ["manage-tenant-settings"] = new { href = "/api/tenant-onboarding/policy-settings" }
            });
        _tenantOnboardingService.GetStatusAsync().Returns(tenantStatus);
        _tenantOnboardingService.GetManagementSettingsAsync().Returns(settings);
        _tenantOnboardingService.GetTenantSettingsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => CreatePolicyCategory(call.ArgAt<string>(0)));
        _onboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusDto
        {
            SelectedDeploymentMode = nameof(DeploymentMode.MultiTenant)
        });
        _tenantBrandingSettingsAdminService.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantBrandingSettingsAdminModel());
    }

    private IRenderedComponent<DynamicComponent> RenderLayout()
    {
        Type layoutType = typeof(EventList).Assembly
            .GetTypes()
            .First(type => type.Name == "TenantAdminSettingsLayout" && typeof(IComponent).IsAssignableFrom(type));
        return _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, layoutType));
    }

    private static TenantPolicySettingsDto CreateManagementModel() => new()
    {
        AllowUserSubmittedEvents = false,
        AllowOrganizationSubmittedEvents = false,
        AllowGroupSubmittedEvents = false
    };

    private static IElement PolicyInput(IRenderedComponent<DynamicComponent> cut, string label) =>
        cut.FindAll("label").Single(element =>
                element.TextContent.Contains(label, StringComparison.OrdinalIgnoreCase))
            .QuerySelector("input")
        ?? throw new InvalidOperationException($"Policy switch input '{label}' not found.");

    private static void NavigateTo(IRenderedComponent<DynamicComponent> cut, string section) =>
        cut.FindAll(".mud-list-item").Single(element =>
            element.TextContent.Trim().Equals(section, StringComparison.OrdinalIgnoreCase)).Click();

    private static IElement BroadSaveButton(IRenderedComponent<DynamicComponent> cut) =>
        cut.FindAll("button").Single(element =>
            element.TextContent.Contains("Save Tenant Settings", StringComparison.Ordinal));

    private static SettingGroupResponseDto CreatePolicyCategory(string category) => category switch
    {
        "Events" => new SettingGroupResponseDto
        {
            Category = category,
            Settings =
            [
                EditableBoolean("events.user_submission_enabled"),
                EditableBoolean("events.organization_submission_enabled"),
                EditableBoolean("events.group_submission_enabled"),
                EditableBoolean("events.require_approval"),
                EditableBoolean("events.card_click_opens_detail_page")
            ]
        },
        "Organizations" => new SettingGroupResponseDto
        {
            Category = category,
            Settings =
            [
                EditableBoolean("organizations.verification_required"),
                EditableBoolean("organizations.self_registration_enabled")
            ]
        },
        "Groups" => new SettingGroupResponseDto
        {
            Category = category,
            Settings = [EditableBoolean("groups.self_registration_enabled")]
        },
        _ => new SettingGroupResponseDto { Category = category }
    };

    private static EffectiveSettingDto EditableBoolean(string key) => new()
    {
        Key = key,
        Value = "false",
        CanEdit = true,
        Source = SettingSource.TenantOverride,
        IsLocked = false
    };
}
