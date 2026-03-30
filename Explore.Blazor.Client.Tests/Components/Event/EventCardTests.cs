// ABOUTME: bUnit tests for EventCard component verifying layout rendering and field visibility.
// ABOUTME: Tests settings-driven field show/hide across all three layout modes.

using Explore.Blazor.Client.Models;
using EventCardComponent = Explore.Blazor.Client.Pages.Events.Components.EventCard;

namespace Explore.Blazor.Client.Tests.Components.Event;

public class EventCardTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private static readonly DateTimeOffset TestDate = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly string ExpectedDetailedDate = TestDate.ToString("MMM dd, yyyy");

    public EventCardTests()
    {
        _ctx = new BlazorTestContext();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private static EventListDto CreateTestEvent() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Test Blazor Conference",
        Description = "A conference about Blazor development and modern web.",
        FirstSessionDate = TestDate,
        ActorDisplayName = "Test Organization",
        ActorTypeFullName = "Organization",
        EventTypeFullName = "Conference",
        EventStatusFullName = "Upcoming",
        EventFormatFullName = "In-Person",
        AudienceGenderFullName = "All",
        AudienceAgeFullName = "Adults",
        VisibilityTypeFullName = "Public",
        Price = 0
    };

    [Test]
    public async Task EventCard_RendersTitle_InDetailedListLayout()
    {
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.DetailedList));

        await Assert.That(cut.Markup).Contains("Test Blazor Conference");
    }

    [Test]
    public async Task EventCard_RendersTitle_InCompactGridLayout()
    {
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.CompactGrid));

        await Assert.That(cut.Markup).Contains("Test Blazor Conference");
    }

    [Test]
    public async Task EventCard_RendersTitle_InSingleRowLayout()
    {
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.SingleRow));

        await Assert.That(cut.Markup).Contains("Test Blazor Conference");
    }

    [Test]
    public async Task EventCard_ShowsAllFields_WhenNoCardFieldVisibility()
    {
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.DetailedList));

        // All fields visible by default when CardFieldVisibility is null
        await Assert.That(cut.Markup).Contains(ExpectedDetailedDate);
        await Assert.That(cut.Markup).Contains("Test Organization");
        await Assert.That(cut.Markup).Contains("Free");
    }

    [Test]
    public async Task EventCard_HidesDateField_WhenVisibilityDisabled()
    {
        var visibility = new Dictionary<string, bool>
        {
            ["event_list.card.show_date"] = false
        };

        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.DetailedList)
            .Add(x => x.CardFieldVisibility, visibility));

        // Date hidden but title still visible
        await Assert.That(cut.Markup).DoesNotContain(ExpectedDetailedDate);
        await Assert.That(cut.Markup).Contains("Test Blazor Conference");
    }

    [Test]
    public async Task EventCard_HidesOrganizerField_WhenVisibilityDisabled()
    {
        var visibility = new Dictionary<string, bool>
        {
            ["event_list.card.show_organizer"] = false
        };

        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.DetailedList)
            .Add(x => x.CardFieldVisibility, visibility));

        await Assert.That(cut.Markup).DoesNotContain("Test Organization");
        await Assert.That(cut.Markup).Contains("Test Blazor Conference");
    }

    [Test]
    public async Task EventCard_HidesPriceField_WhenVisibilityDisabled()
    {
        var visibility = new Dictionary<string, bool>
        {
            ["event_list.card.show_price"] = false
        };

        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.DetailedList)
            .Add(x => x.CardFieldVisibility, visibility));

        await Assert.That(cut.Markup).DoesNotContain("Free");
        await Assert.That(cut.Markup).Contains("Test Blazor Conference");
    }

    [Test]
    public async Task EventCard_AppliesCorrectCssClass_ForLayout()
    {
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.CompactGrid));

        await Assert.That(cut.Markup).Contains("event-card--CompactGrid");
    }
}
