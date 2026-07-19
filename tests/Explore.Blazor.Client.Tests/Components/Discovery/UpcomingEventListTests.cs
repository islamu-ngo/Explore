// ABOUTME: Focused bUnit coverage for the compact public-home upcoming-event update list.
// ABOUTME: Verifies vertical column grouping, accessible event links, metadata, and image fallback.

using Explore.Blazor.Client.Components.Discovery;

namespace Explore.Blazor.Client.Tests.Components.Discovery;

public sealed class UpcomingEventListTests : IDisposable
{
    private readonly BlazorTestContext context = new();

    [Test]
    public async Task RendersSixRowsPerColumnAsDirectEventLinks()
    {
        var cut = context.RenderMudComponent<UpcomingEventList>(parameters => parameters
            .Add(component => component.Events, CreateEvents(7)));

        await Assert.That(cut.FindAll("[data-testid='upcoming-event-column']").Count).IsEqualTo(2);
        await Assert.That(cut.FindAll("[data-testid='upcoming-event-row']").Count).IsEqualTo(7);
        await Assert.That(cut.FindAll(".event-card").Count).IsEqualTo(0);
        await Assert.That(cut.Find("[data-testid='upcoming-event-row']").GetAttribute("href"))
            .IsEqualTo("/events/upcoming-event-1-UP001");
        await Assert.That(cut.Markup).Contains("Sat, Aug 1");
        await Assert.That(cut.Markup).Contains("Community organizer");
    }

    [Test]
    public async Task MissingFeaturedImageUsesLocalGeneratedArtwork()
    {
        var events = CreateEvents(1);
        events[0].FeaturedImageUri = null;

        var cut = context.RenderMudComponent<UpcomingEventList>(parameters => parameters
            .Add(component => component.Events, events));

        await Assert.That(cut.Find("img").GetAttribute("src") ?? string.Empty)
            .StartsWith("data:image/svg+xml");
    }

    [Test]
    public async Task FederatedEventWithoutSourceAffordanceRendersWithoutLink()
    {
        var federatedEvent = CreateEvents(1).Single();
        federatedEvent.AdditionalProperties["eventDiscoverySource"] = "atproto";

        var cut = context.RenderMudComponent<UpcomingEventList>(parameters => parameters
            .Add(component => component.Events, new[] { federatedEvent }));
        var row = cut.Find("[data-testid='upcoming-event-row']");

        await Assert.That(row.HasAttribute("href")).IsFalse();
        await Assert.That(row.GetAttribute("role")).IsEqualTo("article");
        await Assert.That(row.GetAttribute("aria-label")).IsEqualTo("AT Protocol event: Upcoming event 1");
    }

    [Test]
    public async Task FederatedEventWithUnsafeSourceHrefRendersWithoutLink()
    {
        var federatedEvent = CreateEvents(1).Single();
        federatedEvent.AdditionalProperties["eventDiscoverySource"] = "atproto";
        federatedEvent.AdditionalProperties["_links"] = System.Text.Json.JsonSerializer.SerializeToElement(
            new Dictionary<string, HalLink>
            {
                ["source"] = new() { Href = "javascript:alert(document.cookie)", Method = "GET" }
            });

        var cut = context.RenderMudComponent<UpcomingEventList>(parameters => parameters
            .Add(component => component.Events, new[] { federatedEvent }));
        var row = cut.Find("[data-testid='upcoming-event-row']");

        await Assert.That(row.HasAttribute("href")).IsFalse();
        await Assert.That(row.GetAttribute("role")).IsEqualTo("article");
        await Assert.That(cut.Markup).DoesNotContain("javascript:");
    }

    public void Dispose()
    {
        context.Dispose();
        GC.SuppressFinalize(this);
    }

    private static List<EventListDto> CreateEvents(int count) => Enumerable.Range(1, count)
        .Select(index => new EventListDto
        {
            Id = Guid.NewGuid(),
            Title = $"Upcoming event {index}",
            Slug = $"upcoming-event-{index}",
            PublicCode = $"UP{index:D3}",
            FeaturedImageUri = $"https://example.test/upcoming/{index}.webp",
            ActorDisplayName = "Community organizer",
            EventFormatFullName = "In person",
            FirstSessionDate = new DateTimeOffset(2026, 8, index, 18, 0, 0, TimeSpan.Zero)
        })
        .ToList();
}
