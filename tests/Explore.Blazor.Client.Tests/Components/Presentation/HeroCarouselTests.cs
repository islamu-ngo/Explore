// ABOUTME: Focused bUnit coverage for the manual public-home featured event hero.
// ABOUTME: Verifies bounded slides, MangaDex-style image anatomy, controls, swipe behavior, and loading priority.

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

        await Assert.That(cut.Find("[data-testid='hero-counter']").TextContent.Trim()).IsEqualTo("NO. 1");

        cut.Find("button[aria-label='Next featured event']").Click();
        await Assert.That(cut.Find("[data-testid='hero-counter']").TextContent.Trim()).IsEqualTo("NO. 2");

        cut.Find("button[aria-label='Previous featured event']").Click();
        cut.Find("button[aria-label='Previous featured event']").Click();
        await Assert.That(cut.Find("[data-testid='hero-counter']").TextContent.Trim()).IsEqualTo("NO. 3");
    }

    [Test]
    public async Task HeroCarouselPointerSwipeChangesSlide()
    {
        var cut = Render(3);
        var hero = cut.Find("[data-testid='hero-carousel']");

        await hero.TriggerEventAsync("onpointerdown", new PointerEventArgs { ClientX = 240, PointerId = 1, IsPrimary = true });
        await hero.TriggerEventAsync("onpointerup", new PointerEventArgs { ClientX = 120, PointerId = 1, IsPrimary = true });

        await Assert.That(cut.Find("[data-testid='hero-counter']").TextContent.Trim()).IsEqualTo("NO. 2");
    }

    [Test]
    public async Task HeroCarouselRendersBackdropAndInsetPosterForActiveSlide()
    {
        var cut = Render(1);
        var slide = cut.Find("[data-testid='hero-slide']:not([hidden])");

        await Assert.That(slide.QuerySelectorAll(".hero-carousel__backdrop").Length).IsEqualTo(1);
        await Assert.That(slide.QuerySelectorAll(".hero-carousel__poster-image").Length).IsEqualTo(1);
        await Assert.That(slide.QuerySelector(".hero-carousel__backdrop")!.GetAttribute("alt")).IsEqualTo(string.Empty);
        await Assert.That(slide.QuerySelector(".hero-carousel__poster-image")!.GetAttribute("alt"))
            .IsEqualTo("Poster for Featured event 1");
    }

    [Test]
    public async Task ActiveSlideIsOneAccessibleEventLinkWithoutNestedCallToAction()
    {
        var cut = Render(1);
        var slide = cut.Find("[data-testid='hero-slide']:not([hidden])");
        var link = slide.QuerySelector("a.hero-carousel__slide-link")!;

        await Assert.That(link.GetAttribute("href")).IsEqualTo("/events/featured-event-1-EVT001");
        await Assert.That(link.GetAttribute("aria-label")).IsEqualTo("View event: Featured event 1");
        await Assert.That(slide.QuerySelectorAll(".hero-carousel__link").Length).IsEqualTo(0);
    }

    [Test]
    public async Task HeroCarouselPrioritizesOnlyActiveBackdropAndPoster()
    {
        var cut = Render(3);

        await Assert.That(cut.FindAll("img[fetchpriority='high'][loading='eager']").Count).IsEqualTo(2);
        await Assert.That(cut.FindAll("img[fetchpriority='low'][loading='lazy']").Count).IsEqualTo(4);
    }

    [Test]
    public async Task HeroCarouselUsesLocalImageForCanonicalPlaceholder()
    {
        var events = CreateEvents(1);
        events[0].FeaturedImageUri = "https://placeholder.islamu.org/event-default.jpg";

        var cut = _ctx.RenderMudComponent<HeroCarousel>(parameters => parameters
            .Add(component => component.Events, events));

        await Assert.That(cut.FindAll("img").All(image =>
                image.GetAttribute("src") == "/image/landing_image_nonuser.png"))
            .IsTrue();
    }

    [Test]
    public async Task HeaderOnlyHeroUsesOverflowSafeEmptyVariant()
    {
        var cut = _ctx.RenderMudComponent<HeroCarousel>(parameters => parameters
            .Add(component => component.Events, Array.Empty<EventListDto>())
            .Add(
                component => component.HeaderContent,
                (RenderFragment)(builder => builder.AddContent(0, "Browsing events in"))));

        await Assert.That(cut.Find("[data-testid='hero-carousel']").ClassList)
            .Contains("hero-carousel--empty");
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
                Slug = $"featured-event-{index}",
                PublicCode = $"EVT{index:D3}",
                Description = $"Description {index}",
                FeaturedImageUri = $"https://example.test/events/{index}.webp",
                ActorDisplayName = "Community organizer",
                EventFormatFullName = "In-Person",
                FirstSessionDate = new DateTimeOffset(2026, 8, index, 18, 0, 0, TimeSpan.Zero)
            })
            .ToList();
    }
}
