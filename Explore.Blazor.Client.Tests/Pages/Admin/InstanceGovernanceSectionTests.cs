// ABOUTME: Component tests for InstanceGovernanceSection render-policy preset UX behavior and single-tenant visibility rules.
// ABOUTME: Verifies recommended default preselection, highlighted styling, advanced preset selection, and self-service toggle visibility.

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class InstanceGovernanceSectionTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public InstanceGovernanceSectionTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Instance Admin", "admin@example.com");
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task RenderPolicyPreset_DefaultsToRecommendedAndHighlightsCard()
    {
        var renderPolicy = new RenderPolicyModel
        {
            RenderPolicyPreset = string.Empty,
            EnableAdvancedRenderPolicyOverrides = false
        };

        var cut = RenderGovernanceSection(renderPolicy: renderPolicy);

        await Assert.That(renderPolicy.RenderPolicyPreset).IsEqualTo("AllInteractiveServer");

        var selectedCard = cut.FindAll(".instance-governance__preset-card--selected")
            .Single(x => x.TextContent.Contains("All Interactive Server", StringComparison.OrdinalIgnoreCase));

        await Assert.That(selectedCard.ClassList.Contains("instance-governance__preset-card--recommended")).IsTrue();
        await Assert.That(cut.Markup).Contains("Recommended");
    }

    [Test]
    public async Task RenderPolicyPreset_SelectCustomAdvanced_EnablesAdvancedPanel()
    {
        var renderPolicy = new RenderPolicyModel
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
        var renderPolicy = new RenderPolicyModel
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

    private IRenderedComponent<DynamicComponent> RenderGovernanceSection(
        TenantDelegationModel? delegation = null,
        EventPolicyModel? eventPolicy = null,
        OrganizationPolicyModel? orgPolicy = null,
        RenderPolicyModel? renderPolicy = null,
        string deploymentMode = "SingleTenant")
    {
        var componentType = typeof(IInstanceOnboardingService).Assembly.GetType("Explore.Blazor.Client.Pages.Admin.Instance.Components.InstanceGovernanceSection")
                            ?? throw new InvalidOperationException("InstanceGovernanceSection component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p =>
             p.Add(x => x.Type, componentType)
             .Add(x => x.Parameters, new Dictionary<string, object>
             {
                 ["Delegation"] = delegation ?? new TenantDelegationModel(),
                 ["EventPolicy"] = eventPolicy ?? new EventPolicyModel(),
                 ["OrganizationPolicy"] = orgPolicy ?? new OrganizationPolicyModel(),
                 ["RenderPolicy"] = renderPolicy ?? new RenderPolicyModel(),
                 ["DeploymentMode"] = deploymentMode
             }));
    }
}
