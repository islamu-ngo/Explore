// ABOUTME: Source-level SEO guards for public event detail structured metadata.
// ABOUTME: Keeps JSON-LD and crawler noindex behavior tied to centralized event detail helpers.

namespace Explore.Blazor.Client.Tests.Seo;

public sealed class EventDetailStructuredDataMetadataTests
{
    [Test]
    public async Task EventDetail_ShouldEmitJsonLdAndNoindexHeadMetadata()
    {
        var content = await ReadClientSourceAsync("Pages/Events/EventDetail.razor");

        await Assert.That(content).Contains("application/ld+json");
        await Assert.That(content).Contains("GetEventStructuredDataJson()");
        await Assert.That(content).Contains("ShouldRenderEventStructuredData()");
        await Assert.That(content).Contains("ShouldNoIndexEvent()");
        await Assert.That(content).Contains("name=\"robots\"");
        await Assert.That(content).Contains("noindex, nofollow");
    }

    [Test]
    public async Task EventDetailStructuredData_ShouldUseSafeJsonSerializationAndCanonicalUrl()
    {
        var content = await ReadClientSourceAsync("Pages/Events/EventDetail.razor.cs");

        await Assert.That(content).Contains("JsonSerializer.Serialize(data, EventStructuredDataJsonOptions)");
        await Assert.That(content).Contains("JsonSerializerDefaults.Web");
        await Assert.That(content).Contains("JsonIgnoreCondition.WhenWritingNull");
        await Assert.That(content).Contains("\"@context\"] = \"https://schema.org\"");
        await Assert.That(content).Contains("\"@type\"] = \"Event\"");
        await Assert.That(content).Contains("SchemaEventScheduled");
        await Assert.That(content).Contains("SchemaEventCancelled");
        await Assert.That(content).Contains("CanonicalUrlHelper.Build(Navigation, organizerProfileUrl)");
        await Assert.That(content).Contains("GetCanonicalUrl()");
    }

    [Test]
    public async Task EventDetailMetadata_ShouldUseWhiteLabelBrandingInsteadOfIslamuFallbacks()
    {
        var markup = await ReadClientSourceAsync("Pages/Events/EventDetail.razor");
        var code = await ReadClientSourceAsync("Pages/Events/EventDetail.razor.cs");

        await Assert.That(markup).Contains("content=\"@BrandDisplayName\"");
        await Assert.That(markup).DoesNotContain("ISLAMU Events");
        await Assert.That(code).Contains("IPublicExperienceService PublicExperienceService");
        await Assert.That(code).Contains("PublicExperienceService.GetCachedShellAsync()");
        await Assert.That(code).Contains("Event on {BrandDisplayName}");
        await Assert.That(code).Contains("GetCalendarUidHost()");
        await Assert.That(code).DoesNotContain("islamu.events");
    }

    [Test]
    public async Task EventDetailNoindex_ShouldFailClosedForNonPublicOrNonCrawlableEvents()
    {
        var content = await ReadClientSourceAsync("Pages/Events/EventDetail.razor.cs");

        await Assert.That(content).Contains("PublicVisibilityMasterCode");
        await Assert.That(content).Contains("VisibilityTypeMasterCode");
        await Assert.That(content).Contains("PublishedStatusMasterCode");
        await Assert.That(content).Contains("CompletedStatusMasterCode");
        await Assert.That(content).Contains("ShouldNoIndexEvent()");
        await Assert.That(content).Contains("!IsCrawlableStatus(_eventDetails.EventStatusMasterCode)");
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
