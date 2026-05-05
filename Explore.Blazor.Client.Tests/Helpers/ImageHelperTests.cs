using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Tests.Helpers;

public class ImageHelperTests
{
    [Test]
    public async Task GetEventImageUrl_ReturnsFeaturedImage_WhenProvided()
    {
        const string featuredImageUri = "https://cdn.example.test/event.jpg";

        var result = ImageHelper.GetEventImageUrl(featuredImageUri, "Annual Summit");

        await Assert.That(result).IsEqualTo(featuredImageUri);
    }

    [Test]
    public async Task GetEventImageUrl_ReturnsLocalSvgFallback_WhenImageMissing()
    {
        var result = ImageHelper.GetEventImageUrl(null, "Annual Summit", "4a90e2");

        await Assert.That(result).StartsWith("data:image/svg+xml;utf8,");
        await Assert.That(result).Contains(Uri.EscapeDataString("Annual Summit"));
        await Assert.That(result).DoesNotContain("placehold.co");
    }

    [Test]
    public async Task GetOrganizationPlaceholder_ReturnsLocalSvgFallback_WhenImageMissing()
    {
        var result = ImageHelper.GetOrganizationPlaceholder(null, "Community");

        await Assert.That(result).StartsWith("data:image/svg+xml;utf8,");
        await Assert.That(result).Contains(Uri.EscapeDataString("Community"));
        await Assert.That(result).DoesNotContain("placehold.co");
    }

    [Test]
    public async Task GetEventImageUrl_UsesDefaultColor_WhenColorIsInvalid()
    {
        var result = ImageHelper.GetEventImageUrl(null, "Annual Summit", "not-a-color");

        await Assert.That(result).Contains(Uri.EscapeDataString($"fill=\"#{EventColorHelper.DefaultColor}\""));
    }
}
