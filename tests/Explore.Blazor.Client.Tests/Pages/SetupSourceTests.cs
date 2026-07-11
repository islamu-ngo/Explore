// ABOUTME: Source-level regression tests for setup page navigation boundaries.
// ABOUTME: Guards setup-secret cookie visibility across Blazor Server circuit reloads.

namespace Explore.Blazor.Client.Tests.Pages;

public class SetupSourceTests
{
    [Test]
    public async Task Setup_ShouldForceReload_WhenContinuingToAuthProviderConfiguration()
    {
        var source = await ReadSetupSourceAsync();

        await Assert.That(source).Contains("Navigation.NavigateTo(\"/onboarding/auth-provider\", forceLoad: true);");
        await Assert.That(source).Contains("Force a fresh request so BFF self-clients can forward the new HttpOnly cookie.");
    }

    private static async Task<string> ReadSetupSourceAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Explore.Blazor.Client",
                "Pages",
                "Setup.razor");

            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Setup.razor was not found.");
    }
}
