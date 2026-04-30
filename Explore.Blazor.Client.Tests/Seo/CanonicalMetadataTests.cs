// ABOUTME: Guards canonical metadata on public SEO entry points.
// ABOUTME: Keeps sitemap-facing Blazor routes aligned with centralized canonical URL generation.

namespace Explore.Blazor.Client.Tests.Seo;

public sealed class CanonicalMetadataTests
{
    [Arguments("Pages/HomeStart.razor")]
    [Arguments("Pages/Home.razor")]
    [Arguments("Pages/Landing/LandingPageForNonUsers.razor")]
    [Arguments("Pages/Events/EventList.razor")]
    [Arguments("Pages/Organizations/OrganizationProfile.razor")]
    [Arguments("Pages/Organizations/OrganizationDetails.razor")]
    [Test]
    public async Task Page_ShouldUse_CanonicalUrlHelper(string relativePath)
    {
        var content = await ReadClientSourceAsync(relativePath);

        await Assert.That(content).Contains("rel=\"canonical\"");
        await Assert.That(content).Contains("CanonicalUrlHelper.Build");
    }

    [Test]
    public async Task EventDetail_ShouldUse_CanonicalUrlHelper_ForCanonicalUrl()
    {
        var content = await ReadClientSourceAsync("Pages/Events/EventDetail.razor.cs");

        await Assert.That(content).Contains("private string GetCanonicalUrl()\n    {\n        return CanonicalUrlHelper.Build(Navigation");
    }

    private static async Task<string> ReadClientSourceAsync(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Explore.Blazor.Client", relativePath);
            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate Explore.Blazor.Client/{relativePath} from test base directory.");
    }
}
