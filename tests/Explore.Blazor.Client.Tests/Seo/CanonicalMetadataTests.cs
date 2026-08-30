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

}
