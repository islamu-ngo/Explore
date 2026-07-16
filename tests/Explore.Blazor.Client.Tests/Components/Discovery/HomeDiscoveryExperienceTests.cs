// ABOUTME: bUnit coverage for the composed public home discovery experience and coarse-area actions.
// ABOUTME: Verifies one payload, three layouts, honest states, transient geolocation, and preserved online context.

using Explore.Blazor.Client.Components.Discovery;
using Explore.Blazor.Client.Contracts.Interop;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Explore.Blazor.Client.Tests.Components.Discovery;

public sealed class HomeDiscoveryExperienceTests : IDisposable
{
    private readonly HomeDiscoveryTestContext context = new();
    private readonly IHomeDiscoveryService discoveryService = Substitute.For<IHomeDiscoveryService>();
    private readonly IHomeDiscoveryGeolocation geolocation = Substitute.For<IHomeDiscoveryGeolocation>();

    public HomeDiscoveryExperienceTests()
    {
        context.Services.RemoveAll<IHomeDiscoveryService>();
        context.Services.RemoveAll<IHomeDiscoveryGeolocation>();
        context.Services.RemoveAll<ITranslationService>();
        context.Services.RemoveAll<IAccessibilityAnnouncerService>();
        context.Services.AddSingleton(discoveryService);
        context.Services.AddSingleton(geolocation);
        var translation = Substitute.For<ITranslationService>();
        translation.T(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(call => call.ArgAt<string?>(1) ?? call.ArgAt<string>(0));
        context.Services.AddSingleton(translation);
        context.Services.AddSingleton(Substitute.For<IAccessibilityAnnouncerService>());
        context.Interop.SetupVoid("history.replaceState", _ => true).SetVoidResult();
    }

    public void Dispose() => context.Dispose();

    [Test]
    public async Task OneCompositePayloadRendersHeroAndThreeEventLayouts()
    {
        var areaId = Guid.NewGuid();
        discoveryService.LoadAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(CompleteHome(areaId));

        var cut = context.RenderMudComponent<HomeDiscoveryExperience>();
        cut.WaitForElement("[data-testid='home-discovery-context']", TimeSpan.FromSeconds(2));

        await Assert.That(cut.FindAll("h1").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll("[data-testid='hero-carousel']").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll(".event-card--DetailedList").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll(".event-card--SingleRow").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll(".event-card--CompactGrid").Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(cut.Markup).Contains("Upcoming in Brussels");
        await Assert.That(cut.Markup).Contains("Most viewed in Brussels");
        await Assert.That(cut.Markup).Contains("Most viewed online");
        await Assert.That(cut.Markup).Contains("Recently added");
        await discoveryService.Received(1).LoadAsync(null, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CurrentLocationActionIsHiddenWithoutCentroidAreas()
    {
        var home = CompleteHome(Guid.NewGuid());
        home.Context!.AvailableAreas!.Single().CentroidLatitude = null;
        home.Context.AvailableAreas.Single().CentroidLongitude = null;
        discoveryService.LoadAsync(null, null, Arg.Any<CancellationToken>()).Returns(home);

        var cut = context.RenderMudComponent<HomeDiscoveryExperience>();
        cut.WaitForElement("[data-testid='home-discovery-context']", TimeSpan.FromSeconds(2));

        await Assert.That(cut.Markup).DoesNotContain("Use my current location");
    }

    [Test]
    public async Task ExplicitLocationReducesOriginToAreaBeforeHistoryUpdateAndApiSelection()
    {
        var areaId = Guid.NewGuid();
        var initial = CompleteHome(areaId);
        var selected = CompleteHome(areaId);
        discoveryService.LoadAsync(null, null, Arg.Any<CancellationToken>()).Returns(initial);
        geolocation.GetCurrentPositionAsync(Arg.Any<CancellationToken>())
            .Returns(new HomeDiscoveryGeolocationResult(
                HomeDiscoveryGeolocationStatus.Available,
                50.8466,
                4.3528));
        discoveryService.FindClosestArea(
                Arg.Any<IEnumerable<PublicDiscoveryAreaDto>>(),
                50.8466,
                4.3528)
            .Returns(initial.Context!.AvailableAreas!.Single());
        discoveryService.SelectAreaAsync(areaId, Arg.Any<CancellationToken>()).Returns(selected);
        var cut = context.RenderMudComponent<HomeDiscoveryExperience>();
        cut.WaitForElement("[data-testid='home-discovery-context']", TimeSpan.FromSeconds(2));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Use my current location", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() =>
            Assert.That(context.Interop.Invocations.Count(invocation =>
                    invocation.Identifier == "history.replaceState"))
                .IsEqualTo(1));

        await discoveryService.Received(1).SelectAreaAsync(areaId, Arg.Any<CancellationToken>());
        var uri = context.Interop.Invocations
            .Single(invocation => invocation.Identifier == "history.replaceState")
            .Arguments[2]
            ?.ToString() ?? string.Empty;
        await Assert.That(uri).Contains($"areaId={areaId}");
        await Assert.That(uri).DoesNotContain("50.8466");
        await Assert.That(uri).DoesNotContain("4.3528");
    }

    [Test]
    public async Task DeniedLocationKeepsCurrentContextWithoutSelectionCall()
    {
        var areaId = Guid.NewGuid();
        discoveryService.LoadAsync(null, null, Arg.Any<CancellationToken>()).Returns(CompleteHome(areaId));
        geolocation.GetCurrentPositionAsync(Arg.Any<CancellationToken>())
            .Returns(new HomeDiscoveryGeolocationResult(HomeDiscoveryGeolocationStatus.Denied));
        var cut = context.RenderMudComponent<HomeDiscoveryExperience>();
        cut.WaitForElement("[data-testid='home-discovery-context']", TimeSpan.FromSeconds(2));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Use my current location", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.That(cut.Markup).Contains("Location permission was denied"));

        await discoveryService.DidNotReceive().SelectAreaAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OnlineActionPreservesSelectedAreaId()
    {
        var areaId = Guid.NewGuid();
        discoveryService.LoadAsync(null, null, Arg.Any<CancellationToken>()).Returns(CompleteHome(areaId));
        var online = CompleteHome(areaId);
        online.Context!.Mode = HomeDiscoveryMode.Online;
        discoveryService.SelectOnlineAsync(areaId, Arg.Any<CancellationToken>()).Returns(online);
        var cut = context.RenderMudComponent<HomeDiscoveryExperience>();
        cut.WaitForElement("[data-testid='home-discovery-context']", TimeSpan.FromSeconds(2));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Browse online events", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.That(cut.Markup).Contains("Browsing online events"));

        await discoveryService.Received(1).SelectOnlineAsync(areaId, Arg.Any<CancellationToken>());
        var uri = context.Interop.Invocations
            .Single(invocation => invocation.Identifier == "history.replaceState")
            .Arguments[2]
            ?.ToString() ?? string.Empty;
        await Assert.That(uri).Contains($"areaId={areaId}");
        await Assert.That(uri).Contains("mode=online");
    }

    [Test]
    public async Task FailedSectionShowsBoundedMessageWhileSuccessfulSectionsRemain()
    {
        var home = CompleteHome(Guid.NewGuid());
        home.UpcomingInArea = [];
        home.SectionStatuses!["upcoming"] = HomeDiscoverySectionStatus.Failed;
        discoveryService.LoadAsync(null, null, Arg.Any<CancellationToken>()).Returns(home);

        var cut = context.RenderMudComponent<HomeDiscoveryExperience>();
        cut.WaitForElement("[data-testid='home-discovery-context']", TimeSpan.FromSeconds(2));

        await Assert.That(cut.Markup).Contains("This section is temporarily unavailable");
        await Assert.That(cut.Markup).Contains("Most viewed online");
        await Assert.That(cut.Markup).Contains("Recently added");
    }

    private static HomeDiscoveryDto CompleteHome(Guid areaId)
    {
        var hero = Event("Hero event", "hero");
        var upcoming = Event("Upcoming event", "upcoming");
        var spotlight = Event("Spotlight event", "spotlight");
        var viewedArea = Event("Viewed area event", "viewed-area");
        var viewedOnline = Event("Viewed online event", "viewed-online");
        var curated = Event("Curated event", "curated");
        var recent = Event("Recent event", "recent");
        return new HomeDiscoveryDto
        {
            Context = new HomeDiscoveryContextDto
            {
                Mode = HomeDiscoveryMode.Area,
                SelectedAreaId = areaId,
                SelectedAreaDisplayName = "Brussels",
                AvailableAreas =
                [
                    new PublicDiscoveryAreaDto
                    {
                        Id = areaId,
                        DisplayName = "Brussels",
                        CentroidLatitude = 50.85,
                        CentroidLongitude = 4.35
                    }
                ]
            },
            Hero = [new EventDiscoveryItemDto { Event = hero }],
            UpcomingInArea = [new EventDiscoveryItemDto { Event = upcoming }],
            Spotlight = new HomeDiscoverySectionDto
            {
                Key = "spotlight",
                Label = "Community spotlight",
                Items = [new EventDiscoveryItemDto { Event = spotlight }]
            },
            MostViewedInArea = [new EventDiscoveryItemDto { Event = viewedArea }],
            MostViewedOnline = [new EventDiscoveryItemDto { Event = viewedOnline }],
            CuratedSections =
            [
                new HomeDiscoverySectionDto
                {
                    Key = "family",
                    Label = "Family programs",
                    Items = [new EventDiscoveryItemDto { Event = curated }]
                }
            ],
            RecentlyAdded = [new EventDiscoveryItemDto { Event = recent }],
            SectionStatuses = new Dictionary<string, HomeDiscoverySectionStatus>
            {
                ["hero"] = HomeDiscoverySectionStatus.Available,
                ["upcoming"] = HomeDiscoverySectionStatus.Available,
                ["spotlight"] = HomeDiscoverySectionStatus.Available,
                ["most-viewed-area"] = HomeDiscoverySectionStatus.Available,
                ["most-viewed-online"] = HomeDiscoverySectionStatus.Available,
                ["curated:family"] = HomeDiscoverySectionStatus.Available,
                ["recently-added"] = HomeDiscoverySectionStatus.Available
            }
        };
    }

    private static EventListDto Event(string title, string slug) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Slug = slug,
        PublicCode = "abc123",
        EventFormatId = 1,
        EventFormatFullName = "In person",
        FirstSessionDate = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero)
    };

    private sealed class HomeDiscoveryTestContext : BlazorTestContext
    {
        public Bunit.BunitJSInterop Interop => JSInterop;
    }
}
