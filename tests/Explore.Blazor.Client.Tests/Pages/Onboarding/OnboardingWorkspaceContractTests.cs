// ABOUTME: Failing-first source contract for the planned display-only onboarding workspace primitive.
// ABOUTME: Freezes required semantics and rejects nested-main or browser-storage shortcuts before implementation.

namespace Explore.Blazor.Client.Tests.Pages.Onboarding;

public sealed class OnboardingWorkspaceContractTests
{
    [Test]
    public async Task DesignContract_ShouldDefineIslamuNativeWorkspaceBoundaries()
    {
        var design = await ReadRepositoryFileAsync(Path.Combine("docs", "DESIGN.md"));

        await Assert.That(design).Contains("### OnboardingWorkspace");
        await Assert.That(design).Contains("display/navigation-only");
        await Assert.That(design).Contains("visible ordinals belong in the setup-summary rail");
        await Assert.That(design).Contains("Authentication, Site profile, Authorization, and Readiness/Launch");
        await Assert.That(design).Contains("right summary rail occupying one quarter of the page");
        await Assert.That(design).Contains("right rail occupies one third");
        await Assert.That(design).Contains("sticky top navigation bar");
        await Assert.That(design).Contains("theme and language controls");
        await Assert.That(design).Contains("balanced footer actions");
        await Assert.That(design).Contains("loading, error, locked remediation, skipped/configured, complete, current, upcoming, and dirty");
        await Assert.That(design).Contains("LTR/RTL, light/dark themes, forced colors, `prefers-reduced-motion`");
        await Assert.That(design).Contains("Never persist setup secrets");
    }

    [Test]
    public async Task OnboardingWorkspace_ShouldDeclareCanonicalSemanticAndSecurityContract()
    {
        var source = await ReadRepositoryFileAsync(Path.Combine(
            "src",
            "Explore.Blazor.Client",
            "Pages",
            "Onboarding",
            "Components",
            "OnboardingWorkspace.razor"));
        var summarySource = await ReadRepositoryFileAsync(Path.Combine(
            "src",
            "Explore.Blazor.Client",
            "Pages",
            "Onboarding",
            "Components",
            "OnboardingSummary.razor"));

        await Assert.That(source).DoesNotContain("<header");
        await Assert.That(source.IndexOf("@HeadingContent", StringComparison.Ordinal))
            .IsLessThan(source.IndexOf("<nav", StringComparison.Ordinal));
        await Assert.That(source).Contains("<nav");
        await Assert.That(source).Contains("aria-current");
        await Assert.That(source).Contains("onboarding-workspace__current-step");
        await Assert.That(source).DoesNotContain("onboarding-workspace__step-label");
        await Assert.That(summarySource).Contains("onboarding-workspace__summary-step-number");
        await Assert.That(source).Contains("onboarding-workspace__mobile-menu-icon");
        await Assert.That(source).Contains("<section");
        await Assert.That(source).Contains("<aside");
        await Assert.That(source).Contains("<details");
        await Assert.That(source).Contains("<footer");
        await Assert.That(source).DoesNotContain("<main");
        await Assert.That(source).DoesNotContain("localStorage");
        await Assert.That(source).DoesNotContain("sessionStorage");
    }

    private static async Task<string> ReadRepositoryFileAsync(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);

            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Repository file '{relativePath}' was not found.");
    }
}
