// ABOUTME: Source-level responsive layout guards for the setup language and theme switchers.
// ABOUTME: Prevents setup controls from obscuring headings or focused form controls on narrow viewports.

namespace Explore.Blazor.Client.Tests.Layout;

public sealed class SetupLayoutSourceTests
{
    [Test]
    public async Task SetupLayout_ShouldKeepMobileSwitchersInDocumentFlow()
    {
        var markup = await ReadClientSourceAsync("Layout/SetupLayout.razor");
        var styles = await ReadClientSourceAsync("Layout/SetupLayout.razor.css");

        await Assert.That(markup).Contains("setup-layout-root__switchers");
        await Assert.That(styles).Contains(".setup-layout-root__switchers");
        await Assert.That(styles).Contains("inset-inline-end");
        await Assert.That(styles).Contains("@media (max-width: 48rem)");
        await Assert.That(styles).Contains("position: static");
        await Assert.That(styles).Contains("justify-content: flex-end");
        await Assert.That(styles).Contains("::deep :focus-visible");
        await Assert.That(styles).Contains("z-index: var(--mud-zindex-appbar");
        await Assert.That(styles).DoesNotContain("999999");
        await Assert.That(styles).DoesNotContain("padding-left");
    }

    private static async Task<string> ReadClientSourceAsync(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Explore.Blazor.Client", relativePath);
            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate src/Explore.Blazor.Client/{relativePath} from test base directory.");
    }
}
