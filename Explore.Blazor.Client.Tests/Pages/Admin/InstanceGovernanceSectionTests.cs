// ABOUTME: Component tests for InstanceGovernanceSection render-policy preset UX behavior.
// ABOUTME: Verifies recommended default preselection, highlighted styling, and advanced preset selection flow.

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
        var model = new InstanceGovernanceSettingsModel
        {
            RenderPolicyPreset = string.Empty,
            EnableAdvancedRenderPolicyOverrides = false
        };

        var cut = RenderGovernanceSection(model);

        await Assert.That(model.RenderPolicyPreset).IsEqualTo("SeoBalanced");

        var selectedCard = cut.FindAll(".instance-governance__preset-card--selected")
            .Single(x => x.TextContent.Contains("SEO Balanced", StringComparison.OrdinalIgnoreCase));

        await Assert.That(selectedCard.ClassList.Contains("instance-governance__preset-card--recommended")).IsTrue();
        await Assert.That(cut.Markup).Contains("Recommended");
    }

    [Test]
    public async Task RenderPolicyPreset_SelectCustomAdvanced_EnablesAdvancedPanel()
    {
        var model = new InstanceGovernanceSettingsModel
        {
            RenderPolicyPreset = "SeoBalanced",
            EnableAdvancedRenderPolicyOverrides = false
        };

        var cut = RenderGovernanceSection(model);

        var customAdvancedCard = cut.FindAll(".instance-governance__preset-card")
            .Single(x => x.TextContent.Contains("Custom Advanced", StringComparison.OrdinalIgnoreCase));

        customAdvancedCard.Click();

        await Assert.That(model.RenderPolicyPreset).IsEqualTo("CustomAdvanced");
        await Assert.That(model.EnableAdvancedRenderPolicyOverrides).IsTrue();
        await Assert.That(cut.FindAll(".instance-governance__advanced-panel").Count).IsEqualTo(1);
    }

    [Test]
    public async Task RenderPolicyPreset_RendersPresetHelpTooltipTriggers()
    {
        var model = new InstanceGovernanceSettingsModel
        {
            RenderPolicyPreset = "SeoBalanced",
            EnableAdvancedRenderPolicyOverrides = false
        };

        var cut = RenderGovernanceSection(model);

        await Assert.That(cut.FindAll(".instance-governance__preset-card .mud-tooltip-root").Count).IsEqualTo(4);
        await Assert.That(cut.FindAll(".mud-tooltip-root").Count).IsGreaterThanOrEqualTo(5);
        await Assert.That(cut.Markup).Contains("Runtime Render Policy");
    }

    private IRenderedComponent<DynamicComponent> RenderGovernanceSection(InstanceGovernanceSettingsModel model)
    {
        var componentType = typeof(IInstanceOnboardingService).Assembly.GetType("Explore.Blazor.Client.Components.Admin.Instance.InstanceGovernanceSection")
                            ?? throw new InvalidOperationException("InstanceGovernanceSection component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p =>
             p.Add(x => x.Type, componentType)
             .Add(x => x.Parameters, new Dictionary<string, object>
             {
                 ["Model"] = model
             }));
    }
}
