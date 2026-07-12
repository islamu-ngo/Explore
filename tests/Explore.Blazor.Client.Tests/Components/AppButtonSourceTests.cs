// ABOUTME: Source-level accessibility regression tests for the shared AppButton wrapper.
// ABOUTME: Guards explicit disabled-state colors against MudBlazor opacity composition.

namespace Explore.Blazor.Client.Tests.Components;

public class AppButtonSourceTests
{
    [Test]
    public async Task AppButtonStyles_ShouldKeepDisabledFilledActionsLegible()
    {
        var styles = await ReadAppButtonStylesAsync();

        await Assert.That(styles).Contains(".app-button ::deep .mud-button-root:disabled");
        await Assert.That(styles).Contains(".app-button ::deep .mud-button-filled:disabled");
        await Assert.That(styles).Contains("opacity: 1;");
        await Assert.That(styles).Contains("transition: none;");
        await Assert.That(styles).Contains("color: var(--mud-palette-action-disabled) !important;");
        await Assert.That(styles).Contains("background-color: var(--mud-palette-action-disabled-background) !important;");
    }

    private static async Task<string> ReadAppButtonStylesAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "Explore.Blazor.Client",
                "Components",
                "Common",
                "AppButton.razor.css");

            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("AppButton.razor.css was not found.");
    }
}
