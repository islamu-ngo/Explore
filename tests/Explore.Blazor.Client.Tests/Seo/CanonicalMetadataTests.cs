// ABOUTME: Guards canonical metadata on public SEO entry points.
// ABOUTME: Keeps sitemap-facing Blazor routes aligned with centralized canonical URL generation.

using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Tests.Seo;

public sealed class CanonicalMetadataTests
{
    [Arguments("Community Dinner", "abc123", "community-dinner-abc123")]
    [Arguments(null, "abc123", "event-abc123")]
    [Test]
    public async Task EventUrlHelper_ShouldBuildPublicSlugCode(string? slug, string publicCode, string expected)
    {
        var result = EventUrlHelper.BuildPublicSlugCode(slug, publicCode);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task EventUrlHelper_ShouldRequirePublicCode()
    {
        await Assert.That(EventUrlHelper.BuildPublicSlugCode("Community Dinner", null)).IsNull();
    }

    [Arguments("Pages/HomeStart.razor")]
    [Arguments("Pages/Home.razor")]
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

        await Assert.That(content).Contains("EventUrlHelper.BuildPublicPath(_eventDetails?.Slug, _eventDetails?.PublicCode)");
        await Assert.That(content).Contains("CanonicalUrlHelper.Build(Navigation, path)");
    }

    [Test]
    public async Task EventDetailMetadata_ShouldUseAbsoluteUnversionedDynamicOgImageUrl()
    {
        var content = await ReadClientSourceAsync("Pages/Events/EventDetail.razor.cs");

        await Assert.That(content).Contains("EventUrlHelper.BuildPublicSlugCode(_eventDetails?.Slug, _eventDetails?.PublicCode)");
        await Assert.That(content).Contains("CanonicalUrlHelper.Build(Navigation, $\"/api/event/public/{slugCode}/og-image\")");
        await Assert.That(content).DoesNotContain("return GetFeaturedImagePublicUrl() ?? string.Empty;");
        await Assert.That(content).DoesNotContain("/api/v1/event/public/");
        await Assert.That(content).Contains("return $\"{baseUri}/api/storageobject/{imageId}/content\";");
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
