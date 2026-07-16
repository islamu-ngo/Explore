// ABOUTME: Focused bUnit coverage for the manual public-home featured event hero.
// ABOUTME: Verifies bounded slides, controls, swipe behavior, and image loading priority.

using Explore.Blazor.Client.Components.Presentation;

namespace Explore.Blazor.Client.Tests.Components.Presentation;

public sealed class HeroCarouselTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    [Test]
    public async Task HeroCarouselCapsSlidesAtTenAndHasNoAutoplayContract()
    {
        var cut = Render(12);

        await Assert.That(cut.FindAll("[data-testid='hero-slide']").Count).IsEqualTo(10);
        await Assert.That(cut.Markup).DoesNotContain("autoplay");
        await Assert.That(cut.Markup).DoesNotContain("autocycle");
    }

    [Test]
    public async Task HeroCarouselManualControlsAdvanceAndWrap()
    {
        var cut = Render(3);

        await Assert.That(cut.Find("[data-testid='hero-counter']").TextContent.Trim()).IsEqualTo("1 / 3");

        cut.Find("button[aria-label='Next featured event']").Click();
        await Assert.That(cut.Find("[data-testid='hero-counter']").TextContent.Trim()).IsEqualTo("2 / 3");

        cut.Find("button[aria-label='Previous featured event']").Click();
        cut.Find("button[aria-label='Previous featured event']").Click();
        await Assert.That(cut.Find("[data-testid='hero-counter']").TextContent.Trim()).IsEqualTo("3 / 3");
    }

    [Test]
    public async Task HeroCarouselPointerSwipeChangesSlide()
    {
        var cut = Render(3);
        var hero = cut.Find("[data-testid='hero-carousel']");

        await hero.TriggerEventAsync("onpointerdown", new PointerEventArgs { ClientX = 240, PointerId = 1, IsPrimary = true });
        await hero.TriggerEventAsync("onpointerup", new PointerEventArgs { ClientX = 120, PointerId = 1, IsPrimary = true });

        await Assert.That(cut.Find("[data-testid='hero-counter']").TextContent.Trim()).IsEqualTo("2 / 3");
    }

    [Test]
    public async Task HeroCarouselPrioritizesOnlyActiveImage()
    {
        var cut = Render(3);

        await Assert.That(cut.FindAll("img[fetchpriority='high'][loading='eager']").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll("img[fetchpriority='low'][loading='lazy']").Count).IsEqualTo(2);
    }

    [Test]
    public async Task HeroCarouselUsesLocalImageForCanonicalPlaceholder()
    {
        var events = CreateEvents(1);
        events[0].FeaturedImageUri = "https://placeholder.islamu.org/event-default.jpg";

        var cut = _ctx.RenderMudComponent<HeroCarousel>(parameters => parameters
            .Add(component => component.Events, events));

        await Assert.That(cut.Find("img").GetAttribute("src"))
            .IsEqualTo("/image/landing_image_nonuser.png");
    }

    public void Dispose()
    {
        _ctx.Dispose();
        GC.SuppressFinalize(this);
    }

    private IRenderedComponent<HeroCarousel> Render(int count)
    {
        return _ctx.RenderMudComponent<HeroCarousel>(parameters => parameters
            .Add(component => component.Events, CreateEvents(count)));
    }

    private static List<EventListDto> CreateEvents(int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => new EventListDto
            {
                Id = Guid.NewGuid(),
                Title = $"Featured event {index}",
                Description = $"Description {index}",
                FeaturedImageUri = $"https://example.test/events/{index}.webp",
                ActorDisplayName = "Community organizer",
                EventFormatFullName = "In-Person",
                FirstSessionDate = new DateTimeOffset(2026, 8, index, 18, 0, 0, TimeSpan.Zero)
            })
            .ToList();
    }
}
