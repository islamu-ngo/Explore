// ABOUTME: bUnit tests for EventCard component verifying layout rendering and field visibility.
// ABOUTME: Tests settings-driven field show/hide across all three layout modes.

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
    [Arguments(LayoutMode.CompactGrid)]
    [Arguments(LayoutMode.DetailedList)]
    [Arguments(LayoutMode.SingleRow)]
    public async Task EventCardImagesAreAccessibleLazyAndLayoutStable(LayoutMode layout)
    {
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, layout));

        var image = cut.Find("img");

        await Assert.That(image.GetAttribute("alt")).IsEqualTo("Test Blazor Conference");
        await Assert.That(image.GetAttribute("loading")).IsEqualTo("lazy");
        await Assert.That(image.GetAttribute("decoding")).IsEqualTo("async");
        await Assert.That(image.GetAttribute("width")).IsNotNull().And.IsNotEmpty();
        await Assert.That(image.GetAttribute("height")).IsNotNull().And.IsNotEmpty();
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

    [Test]
    public async Task EventCard_RendersShareButton_WithAccessibleLabel()
    {
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.DetailedList)
            .Add(x => x.OnShareRequested, EventCallback.Factory.Create<EventListDto>(this, _ => { })));

        var shareButton = cut.Find("button[aria-label='Share event: Test Blazor Conference']");

        await Assert.That(shareButton).IsNotNull();
    }

    [Test]
    public async Task EventCard_WhenPast_RendersEndedBadgeAndSuppressesShareAction()
    {
        var eventDto = CreateTestEvent();
        eventDto.IsPast = true;

        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, eventDto)
            .Add(x => x.Layout, LayoutMode.DetailedList)
            .Add(x => x.OnShareRequested, EventCallback.Factory.Create<EventListDto>(this, _ => { })));

        await Assert.That(cut.Markup).Contains("event-card--past");
        await Assert.That(cut.Markup).Contains("Ended");
        await Assert.That(cut.Markup).DoesNotContain("Share event: Test Blazor Conference");
    }

    [Test]
    public async Task EventCard_ShareButton_InvokesShareCallbackWithoutSelectingCard()
    {
        var shareCount = 0;
        var selectCount = 0;
        EventListDto? sharedEvent = null;
        var eventDto = CreateTestEvent();

        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, eventDto)
            .Add(x => x.Layout, LayoutMode.DetailedList)
            .Add(x => x.OnClick, EventCallback.Factory.Create<EventListDto>(this, _ => selectCount++))
            .Add(x => x.OnShareRequested, EventCallback.Factory.Create<EventListDto>(this, evt =>
            {
                shareCount++;
                sharedEvent = evt;
            })));

        cut.Find("button[aria-label='Share event: Test Blazor Conference']").Click();

        await Assert.That(shareCount).IsEqualTo(1);
        await Assert.That(selectCount).IsEqualTo(0);
        await Assert.That(sharedEvent).IsSameReferenceAs(eventDto);
    }

    [Test]
    public async Task EventCardRendersLabeledKeyboardTarget()
    {
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.DetailedList));

        var card = cut.Find(".event-card");

        await Assert.That(card.GetAttribute("role")).IsEqualTo("button");
        await Assert.That(card.GetAttribute("tabindex")).IsEqualTo("0");
        await Assert.That(card.GetAttribute("aria-label")).IsEqualTo("View event: Test Blazor Conference");
    }

    [Test]
    public async Task EventCardKeyDownWithEnterInvokesCardCallback()
    {
        var selectCount = 0;
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.DetailedList)
            .Add(x => x.OnClick, EventCallback.Factory.Create<EventListDto>(this, _ => selectCount++)));

        await cut.Find(".event-card").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        await Assert.That(selectCount).IsEqualTo(1);
    }

    [Test]
    public async Task EventCardKeyDownWithSpaceInvokesCardCallback()
    {
        var selectCount = 0;
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.CompactGrid)
            .Add(x => x.OnClick, EventCallback.Factory.Create<EventListDto>(this, _ => selectCount++)));

        await cut.Find(".event-card").KeyDownAsync(new KeyboardEventArgs { Key = " " });

        await Assert.That(selectCount).IsEqualTo(1);
    }

    [Test]
    public async Task EventCardKeyDownWithUnrelatedKeyDoesNotInvokeCardCallback()
    {
        var selectCount = 0;
        var cut = _ctx.RenderMudComponent<EventCardComponent>(p => p
            .Add(x => x.Event, CreateTestEvent())
            .Add(x => x.Layout, LayoutMode.SingleRow)
            .Add(x => x.OnClick, EventCallback.Factory.Create<EventListDto>(this, _ => selectCount++)));

        await cut.Find(".event-card").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        await Assert.That(selectCount).IsEqualTo(0);
    }

    [Test]
    public async Task EventDetail_SourceIncludesVisibleShareAction()
    {
        var eventDetailPath = FindSourceFilePath("src", "Explore.Blazor.Client", "Pages", "Events", "EventDetail.razor");
        var source = await File.ReadAllTextAsync(eventDetailPath);

        await Assert.That(source).Contains("Share Event");
        await Assert.That(source).Contains("ShareEventAsync");
    }

    private static string FindSourceFilePath(params string[] relativeSegments)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine([current.FullName, .. relativeSegments]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate source file: {Path.Combine(relativeSegments)}");
    }
}
