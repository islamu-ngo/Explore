// ABOUTME: Accessibility ratchets for whole-instance configuration-manifest administration.
// ABOUTME: Pins semantic hierarchy, localized guidance, focus recovery hooks, reflow, and RTL-safe styles.

using Explore.Blazor.Client.Contracts.ControlPlane;
using Explore.Blazor.Client.Contracts.Services.ControlPlane;
using Explore.Blazor.Client.Pages.Admin.Instance.Components;

namespace Explore.Blazor.Client.Tests.Accessibility;

public sealed class ConfigurationManifestAdministrationAccessibilityTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task Section_UsesSequentialHeadingAndLabeledNativeActionGroup()
    {
        var service = Substitute.For<IConfigurationManifestExportService>();
        service.GetCapabilitiesAsync(Arg.Any<CancellationToken>())
            .Returns(new HalResourceOfControlPlaneOverviewDto
            {
                _links = new Dictionary<string, HalLink>
                {
                    [ControlPlaneLinkRelations.ExportConfigurationOverrides] =
                        new HalLink { Href = "/ignored", Method = "GET" },
                    [ControlPlaneLinkRelations.ExportConfigurationPortable] =
                        new HalLink { Href = "/ignored", Method = "GET" }
                }
            });
        _ctx.Services.AddSingleton(service);

        var cut = _ctx.Render<ConfigurationManifestExportSection>();
        cut.WaitForAssertion(() => cut.Find("#configuration-manifest-export-heading"));

        await Assert.That(cut.Find("#configuration-manifest-export-heading").TagName)
            .IsEqualTo("H2");
        await Assert.That(cut.Find("[role='group']").GetAttribute("aria-label"))
            .IsEqualTo("Configuration manifest downloads");
        await Assert.That(cut.Find("ul.configuration-manifest-export__guidance")
                .GetAttribute("dir"))
            .IsEqualTo("auto");
        await Assert.That(cut.FindAll("li[dir='auto']")).Count().IsEqualTo(3);
        await Assert.That(cut.FindAll("button[dir='auto']")).Count().IsEqualTo(2);
        await Assert.That(cut.FindAll(
                "button[aria-label='Download configuration overrides'][dir='auto']"))
            .HasSingleItem();
        await Assert.That(cut.FindAll(
                "button[aria-label='Download portable configuration'][dir='auto']"))
            .HasSingleItem();
    }

    [Test]
    public async Task Source_UsesLocalizationFocusAndAnnouncementsWithoutAuthorityProse()
    {
        string source = await File.ReadAllTextAsync(SourcePath(
            "Pages",
            "Admin",
            "Instance",
            "Components",
            "ConfigurationManifestExportSection.razor"));

        await Assert.That(source).Contains("ITranslationService");
        await Assert.That(source).Contains("IAccessibilityFocusService");
        await Assert.That(source).Contains("IAccessibilityAnnouncerService");
        await Assert.That(source).Contains("FocusByIdAsync");
        await Assert.That(source).Contains("AnnouncePoliteAsync");
        await Assert.That(source).DoesNotContain("instance.configuration-manifest.export");
        await Assert.That(source).DoesNotContain("InstanceSettings.View");
        await Assert.That(source).DoesNotContain("IsInRole");
    }

    [Test]
    public async Task ScopedStyles_UseLogicalReflowFocusAndForcedColorRules()
    {
        string css = await File.ReadAllTextAsync(SourcePath(
            "Pages",
            "Admin",
            "Instance",
            "Components",
            "ConfigurationManifestExportSection.razor.css"));

        await Assert.That(css).Contains("padding-inline");
        await Assert.That(css).Contains("border-inline-start");
        await Assert.That(css).Contains(":focus-visible");
        await Assert.That(css).Contains("@media (max-width:");
        await Assert.That(css).Contains("@media (forced-colors: active)");
        await Assert.That(css).DoesNotContain("left:");
        await Assert.That(css).DoesNotContain("right:");
    }

    private static string SourcePath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return Path.Combine(
                    [directory.FullName, "src", "Explore.Blazor.Client", .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
