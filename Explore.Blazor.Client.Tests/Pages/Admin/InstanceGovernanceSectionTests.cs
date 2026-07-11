// ABOUTME: Component tests for InstanceGovernanceSection render-policy preset UX behavior and single-tenant visibility rules.
// ABOUTME: Verifies recommended default preselection, highlighted styling, advanced preset selection, and self-service toggle visibility.

using Explore.Blazor.Client.Contracts.ControlPlane;
using Explore.Blazor.Client.Contracts.Services.ControlPlane;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class InstanceGovernanceSectionTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IControlPlaneOperationsService _operationsService;

    public InstanceGovernanceSectionTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Instance Admin", "admin@example.com");
        _operationsService = Substitute.For<IControlPlaneOperationsService>();
        _operationsService.GetDeploymentModeRunbookAsync(Arg.Any<CancellationToken>())
            .Returns(new HalResourceOfControlPlaneDeploymentModeRunbookDto());
        _ctx.Services.AddSingleton(_operationsService);
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
}
