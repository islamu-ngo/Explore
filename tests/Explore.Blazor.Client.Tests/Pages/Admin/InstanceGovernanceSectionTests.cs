// ABOUTME: Component tests for InstanceGovernanceSection render-policy preset UX behavior and single-tenant visibility rules.
// ABOUTME: Verifies recommended default preselection, highlighted styling, advanced preset selection, and self-service toggle visibility.

using Explore.Blazor.Client.Contracts.ControlPlane;
using Explore.Blazor.Client.Contracts.Services.ControlPlane;
using Explore.Blazor.Client.Contracts.Services.Federation;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class InstanceGovernanceSectionTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IControlPlaneOperationsService _operationsService;
    private readonly IAtprotoFederationSettingsService _settingsService;

    public InstanceGovernanceSectionTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Instance Admin", "admin@example.com");
        _operationsService = Substitute.For<IControlPlaneOperationsService>();
        _operationsService.GetDeploymentModeRunbookAsync(Arg.Any<CancellationToken>())
            .Returns(new HalResourceOfControlPlaneDeploymentModeRunbookDto());
        _settingsService = Substitute.For<IAtprotoFederationSettingsService>();
        _settingsService.GetInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(CreateAtprotoSettings());
        _ctx.Services.AddSingleton(_operationsService);
        _ctx.Services.AddSingleton(_settingsService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task RenderPolicyPreset_DefaultsToRecommendedAndHighlightsCard()
    {
        var renderPolicy = new RenderPolicySettingsDto
        {
            RenderPolicyPreset = string.Empty,
            EnableAdvancedRenderPolicyOverrides = false
        };

        var cut = RenderGovernanceSection(renderPolicy: renderPolicy);

        await Assert.That(renderPolicy.RenderPolicyPreset).IsEmpty();

        var selectedCard = cut.FindAll(".instance-governance__preset-card--selected")
            .Single(x => x.TextContent.Contains("All Interactive Server", StringComparison.OrdinalIgnoreCase));

        await Assert.That(selectedCard.ClassList.Contains("instance-governance__preset-card--recommended")).IsTrue();
        await Assert.That(cut.Markup).Contains("Recommended");
    }

    [Test]
    public async Task RenderPolicyPreset_SelectCustomAdvanced_EnablesAdvancedPanel()
    {
        var renderPolicy = new RenderPolicySettingsDto
        {
            RenderPolicyPreset = "SeoBalanced",
            EnableAdvancedRenderPolicyOverrides = false
        };

        var cut = RenderGovernanceSection(renderPolicy: renderPolicy);

        var customAdvancedCard = cut.FindAll(".instance-governance__preset-card")
            .Single(x => x.TextContent.Contains("Custom Advanced", StringComparison.OrdinalIgnoreCase));

        customAdvancedCard.Click();

        await Assert.That(renderPolicy.RenderPolicyPreset).IsEqualTo("CustomAdvanced");
        await Assert.That(renderPolicy.EnableAdvancedRenderPolicyOverrides).IsTrue();
        await Assert.That(cut.FindAll(".instance-governance__advanced-panel").Count).IsEqualTo(1);
    }

    [Test]
    public async Task RenderPolicyPreset_RendersPresetHelpTooltipTriggers()
    {
        var renderPolicy = new RenderPolicySettingsDto
        {
            RenderPolicyPreset = "SeoBalanced",
            EnableAdvancedRenderPolicyOverrides = false
        };

        var cut = RenderGovernanceSection(renderPolicy: renderPolicy);

        await Assert.That(cut.FindAll(".instance-governance__preset-card .mud-tooltip-root").Count).IsEqualTo(5);
        await Assert.That(cut.FindAll(".mud-tooltip-root").Count).IsGreaterThanOrEqualTo(5);
        await Assert.That(cut.Markup).Contains("Runtime Render Policy");
    }

    [Test]
    public async Task GovernanceSection_SingleTenant_NoSelfServiceRegistrationToggle()
    {
        var cut = RenderGovernanceSection(deploymentMode: "SingleTenant");

        await Assert.That(cut.Markup).DoesNotContain("Allow tenant self-service registration", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task GovernanceSection_MultiTenant_HasSelfServiceRegistrationToggle()
    {
        var cut = RenderGovernanceSection(deploymentMode: "MultiTenant");

        await Assert.That(cut.Markup).Contains("Allow tenant self-service registration", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task GovernanceSection_DeploymentMode_IsOperatorControlledStatusOnly()
    {
        var cut = RenderGovernanceSection(displayMode: "advanced");

        await Assert.That(cut.Markup).Contains("Deployment mode is locked after onboarding", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("DEPLOYMENT_MODE=multi_tenant", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("Enable Multi-Tenant Mode", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("Revert to Single-Tenant", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task GovernanceSection_DeploymentModeRunbook_RendersHalGatedTransitionAndSubmitsTypedConfirmation()
    {
        _operationsService.GetDeploymentModeRunbookAsync(Arg.Any<CancellationToken>())
            .Returns(CreateRunbook(Links(ControlPlaneLinkRelations.TransitionToMultiTenant)));
        _operationsService.TransitionDeploymentModeAsync(
                "MultiTenant",
                "ENABLE MULTI_TENANT",
                "tenant launch",
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfControlPlaneDeploymentModeTransitionDto
            {
                Success = true,
                Message = "Deployment mode transition accepted."
            });

        var cut = RenderGovernanceSection(displayMode: "advanced");

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Deployment mode runbook", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected deployment-mode runbook to render.");
            }
        });

        await Assert.That(cut.Markup).Contains("Current mode", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("SingleTenant", StringComparison.Ordinal);
        await Assert.That(cut.Markup).Contains("Active tenants", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Multi-Tenant", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Find("button[aria-label='Run deployment mode transition to MultiTenant']").HasAttribute("disabled")).IsTrue();

        cut.Find("input[aria-label='Deployment mode confirmation']").Change("wrong");
        await Assert.That(cut.Find("button[aria-label='Run deployment mode transition to MultiTenant']").HasAttribute("disabled")).IsTrue();

        cut.Find("input[aria-label='Deployment mode confirmation']").Change("ENABLE MULTI_TENANT");
        cut.Find("textarea[aria-label='Deployment mode transition reason']").Change("tenant launch");
        await Assert.That(cut.Find("button[aria-label='Run deployment mode transition to MultiTenant']").HasAttribute("disabled")).IsFalse();

        cut.Find("button[aria-label='Run deployment mode transition to MultiTenant']").Click();

        await _operationsService.Received(1).TransitionDeploymentModeAsync(
            "MultiTenant",
            "ENABLE MULTI_TENANT",
            "tenant launch",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GovernanceSection_DeploymentModeRunbook_WithoutTransitionLink_HidesTransitionControl()
    {
        _operationsService.GetDeploymentModeRunbookAsync(Arg.Any<CancellationToken>())
            .Returns(CreateRunbook(new Dictionary<string, HalLink>()));

        var cut = RenderGovernanceSection(displayMode: "advanced");

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Multi-Tenant", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected deployment-mode target to render.");
            }
        });

        await Assert.That(cut.Markup).Contains("Transition requires a server-provided HAL affordance.", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("Run deployment-mode runbook", StringComparison.OrdinalIgnoreCase);
        await _operationsService.DidNotReceive().TransitionDeploymentModeAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AtprotoGovernance_WithoutHalAffordances_DisablesUpdateAndHidesLockActions()
    {
        _settingsService.GetInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(CreateAtprotoSettings());

        var cut = RenderGovernanceSection(displayMode: "advanced");
        cut.WaitForState(() => cut.Markup.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));
        var label = cut.FindAll("label")
            .Single(element => element.TextContent.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));
        var input = label.QuerySelector("input") ?? throw new InvalidOperationException("AT Protocol events switch input not found.");

        await Assert.That(input.HasAttribute("disabled")).IsTrue();
        await Assert.That(cut.Markup).Contains("server has not granted an update affordance", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("Lock tenant override", StringComparison.OrdinalIgnoreCase);
        await _settingsService.DidNotReceive().UpdateInstanceAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AtprotoGovernance_WithoutHalAffordances_RendersServerSourceAndReason()
    {
        var settings = CreateAtprotoSettings();
        var eventsSetting = settings.Settings.Single(setting =>
            setting.Key == "federation.atproto_events_enabled");
        eventsSetting.Source = (int)SettingSource.SystemLocked;
        eventsSetting.IsLocked = true;
        eventsSetting.Reason = "Instance policy denied this mutation.";
        _settingsService.GetInstanceAsync(Arg.Any<CancellationToken>()).Returns(settings);

        var cut = RenderGovernanceSection(displayMode: "advanced");
        cut.WaitForState(() => cut.Markup.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));

        await Assert.That(cut.Markup).Contains("Source: System locked", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Lock: Locked", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Instance policy denied this mutation.", StringComparison.Ordinal);
        await Assert.That(cut.FindAll("[role='status']").Any(element =>
            element.TextContent.Contains("Instance policy denied this mutation.", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task AtprotoGovernance_UnknownProfileWithoutHal_FallsBackAndStaysDisabled()
    {
        var settings = CreateAtprotoSettings();
        settings.Settings.Single(setting =>
            setting.Key == "federation.atproto_event_validation_profile").Value = "\"unknown_profile\"";
        _settingsService.GetInstanceAsync(Arg.Any<CancellationToken>()).Returns(settings);

        var cut = RenderGovernanceSection(displayMode: "advanced");
        cut.WaitForState(() => cut.Markup.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));
        var validationInput = cut.Find("input[aria-label='Event creation validation']");

        await Assert.That(validationInput.GetAttribute("value")).IsEqualTo("platform");
        await Assert.That(validationInput.HasAttribute("disabled")).IsTrue();
        await _settingsService.DidNotReceive().UpdateInstanceAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AtprotoGovernance_WithHalAffordances_UpdatesAndLocksExactSetting()
    {
        const string key = "federation.atproto_events_enabled";
        _settingsService.GetInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(CreateAtprotoSettings($"update-{key}", $"lock-{key}"));
        _settingsService.UpdateInstanceAsync(
                key,
                "true",
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });
        _settingsService.SetInstanceLockAsync(
                key,
                true,
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });

        var cut = RenderGovernanceSection(displayMode: "advanced");
        cut.WaitForState(() => cut.Markup.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));
        var label = cut.FindAll("label")
            .Single(element => element.TextContent.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));
        (label.QuerySelector("input") ?? throw new InvalidOperationException("AT Protocol events switch input not found."))
            .Change(true);
        cut.Find("button[aria-label='Lock AT Protocol events for tenants']").Click();

        await _settingsService.Received(1).UpdateInstanceAsync(
            key,
            "true",
            Arg.Any<CancellationToken>());
        await _settingsService.Received(1).SetInstanceLockAsync(
            key,
            true,
            Arg.Any<CancellationToken>());
        await Assert.That(cut.Markup).Contains(
            "One switch enables both fetching community events and publishing eligible local events",
            StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("application commits and validates the event before any PDS record", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("decentralization", StringComparison.OrdinalIgnoreCase);
    }

    private IRenderedComponent<DynamicComponent> RenderGovernanceSection(
        TenantDelegationSettingsDto? delegation = null,
        EventPolicyDto? eventPolicy = null,
        OrganizationPolicyDto? orgPolicy = null,
        RenderPolicySettingsDto? renderPolicy = null,
        string deploymentMode = "SingleTenant",
        string displayMode = "full")
    {
        var componentType = typeof(IInstanceOnboardingService).Assembly.GetType("Explore.Blazor.Client.Pages.Admin.Instance.Components.InstanceGovernanceSection")
                            ?? throw new InvalidOperationException("InstanceGovernanceSection component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p =>
             p.Add(x => x.Type, componentType)
             .Add(x => x.Parameters, new Dictionary<string, object>
             {
                 ["Delegation"] = delegation ?? new TenantDelegationSettingsDto(),
                 ["EventPolicy"] = eventPolicy ?? new EventPolicyDto(),
                 ["OrganizationPolicy"] = orgPolicy ?? new OrganizationPolicyDto(),
                 ["RenderPolicy"] = renderPolicy ?? new RenderPolicySettingsDto(),
                 ["DeploymentMode"] = deploymentMode,
                 ["DisplayMode"] = displayMode
             }));
    }

    private static HalResourceOfControlPlaneDeploymentModeRunbookDto CreateRunbook(
        IDictionary<string, HalLink> links) => new()
        {
            CurrentMode = "SingleTenant",
            ActiveTenantCount = 1,
            GeneratedAtUtc = new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero),
            TargetOptions =
            [
                new ControlPlaneDeploymentModeTargetOptionDto
                {
                    TargetMode = "MultiTenant",
                    Label = "Multi-Tenant",
                    Description = "Allow tenant routing and tenant self-service administration.",
                    Allowed = true,
                    ConfirmationText = "ENABLE MULTI_TENANT"
                }
            ],
            Steps =
            [
                new ControlPlaneDeploymentModeRunbookStepDto
                {
                    Key = "backup",
                    Title = "Back up instance data",
                    Description = "Create a fresh backup before changing tenant routing.",
                    Severity = "warning"
                }
            ],
            _links = links
        };

    private static IDictionary<string, HalLink> Links(params string[] relations) =>
        relations.ToDictionary(
            relation => relation,
            relation => new HalLink { Href = $"/api/control-plane/deployment-mode/{relation}", Method = "POST" },
            StringComparer.OrdinalIgnoreCase);

    private static HalResourceOfSettingGroupResponseDto CreateAtprotoSettings(params string[] relations) =>
        new()
        {
            Category = "AtprotoFederation",
            Settings =
            [
                new Explore.Blazor.Client.Clients.Settings
                {
                    Key = "federation.atproto_events_enabled",
                    Value = "false",
                    CanEdit = true,
                    IsLocked = false,
                    IsLockable = true
                },
                new Explore.Blazor.Client.Clients.Settings
                {
                    Key = "federation.atproto_event_validation_profile",
                    Value = "\"platform\"",
                    CanEdit = true,
                    IsLocked = false,
                    IsLockable = true
                }
            ],
            _links = relations.ToDictionary(
                relation => relation,
                relation => new HalLink { Href = $"/api/settings/instance/atproto-federation/{relation}" },
                StringComparer.Ordinal)
        };
}
