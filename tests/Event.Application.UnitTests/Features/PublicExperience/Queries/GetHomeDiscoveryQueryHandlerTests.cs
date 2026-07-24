// ABOUTME: Unit coverage for tenant-safe bounded home-discovery query composition.
// ABOUTME: Verifies area resolution, request filters, independent sections, curation, and partial-failure isolation.

using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.PublicExperience;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Features.Federation.Atproto.Requests.Queries;
using Explore.Application.Features.PublicExperience.Handlers.Queries;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using Explore.Application.Models.PublicExperience;
using Explore.Application.Responses;
using Explore.Application.Serialization;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Explore.Application.UnitTests.Features.PublicExperience.Queries;

public sealed class GetHomeDiscoveryQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly IRequestHandler<GetPublicEventDiscoveryRequest, PaginatedResult<EventDiscoveryItemDto>> _eventDiscoveryHandler =
        Substitute.For<IRequestHandler<GetPublicEventDiscoveryRequest, PaginatedResult<EventDiscoveryItemDto>>>();
    private readonly IRequestHandler<GetPublicExperienceShellQuery, PublicExperienceShellDto> _shellHandler =
        Substitute.For<IRequestHandler<GetPublicExperienceShellQuery, PublicExperienceShellDto>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly ILocationRepository _locationRepository = Substitute.For<ILocationRepository>();
    private readonly TimeProvider _timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero));
    private readonly ILogger<GetHomeDiscoveryQueryHandler> _logger = Substitute.For<ILogger<GetHomeDiscoveryQueryHandler>>();

    public GetHomeDiscoveryQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _shellHandler.Handle(Arg.Any<GetPublicExperienceShellQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PublicExperienceShellDto());
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.EventSectionPresets,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
    }

    [Test]
    public async Task RequestedAreaResolvesToTenantAreaAndFiltersContextualQueries()
    {
        var locationId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        ConfigureAreas(new PublicDiscoveryAreaConfig(
            areaId,
            "Brussels",
            "Brussels",
            "BE",
            50.85m,
            4.35m,
            [locationId],
            IsDefault: true));
        _locationRepository.GetExistingTenantLocationIdsAsync(
                TenantId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([locationId]);
        var requests = CaptureSuccessfulRequests();

        var result = await CreateHandler().Handle(new GetHomeDiscoveryQuery(areaId, "area"), CancellationToken.None);

        await Assert.That(result.Context.Mode).IsEqualTo(HomeDiscoveryMode.Area);
        await Assert.That(result.Context.SelectedAreaId).IsEqualTo(areaId);
        await Assert.That(result.Context.AvailableAreas.Single().DisplayName).IsEqualTo("Brussels");
        await Assert.That(requests.Any(request => request.LocationIds?.SequenceEqual([locationId]) == true)).IsTrue();
        await Assert.That(requests.Any(request => request.FormatIds?.SequenceEqual(
            [(int)EventFormatEnum.Digital, (int)EventFormatEnum.Hybrid]) == true)).IsTrue();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task AreaOnlyHomeDiscoveryDropsUpstreamProximityFieldsFromResultAndJson()
    {
        ConfigureAreas();
        var source = new EventDiscoveryItemDto
        {
            Event = CreateEvent("Area-only event"),
            DistanceMeters = 125,
            NearestSessionId = Guid.NewGuid(),
            NearestLocationId = Guid.NewGuid(),
            NearestLocationName = "Exact venue",
            NearestOccurrenceStartsAtUtc = _timeProvider.GetUtcNow().AddHours(1)
        };
        _eventDiscoveryHandler.Handle(Arg.Any<GetPublicEventDiscoveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => PaginatedResult<EventDiscoveryItemDto>.Create(
                [source],
                totalCount: 1,
                pageNumber: 1,
                pageSize: call.Arg<GetPublicEventDiscoveryRequest>()!.Criteria.PageSize));

        var result = await CreateHandler().Handle(
            new GetHomeDiscoveryQuery(Mode: "all"),
            CancellationToken.None);
        var item = result.Hero.Single();
        var json = JsonSerializer.Serialize(result, ExploreJsonContext.Default.HomeDiscoveryDto);

        await Assert.That(item.DistanceMeters).IsNull();
        await Assert.That(item.NearestSessionId).IsNull();
        await Assert.That(item.NearestLocationId).IsNull();
        await Assert.That(item.NearestLocationName).IsNull();
        await Assert.That(item.NearestOccurrenceStartsAtUtc).IsNull();
        await Assert.That(json).DoesNotContain("\"distanceMeters\"");
        await Assert.That(json).DoesNotContain("\"nearestSessionId\"");
        await Assert.That(json).DoesNotContain("\"nearestLocationId\"");
        await Assert.That(json).DoesNotContain("\"nearestLocationName\"");
        await Assert.That(json).DoesNotContain("\"nearestOccurrenceStartsAtUtc\"");
    }

    [Test]
    public async Task InvalidRequestedAreaFallsBackToDefaultActiveArea()
    {
        var defaultAreaId = Guid.NewGuid();
        ConfigureAreas(new PublicDiscoveryAreaConfig(
            defaultAreaId,
            "Default area",
            "Brussels",
            "BE",
            IsDefault: true));
        CaptureSuccessfulRequests();

        var result = await CreateHandler().Handle(new GetHomeDiscoveryQuery(Guid.NewGuid(), "area"), CancellationToken.None);

        await Assert.That(result.Context.SelectedAreaId).IsEqualTo(defaultAreaId);
        await Assert.That(result.Context.Mode).IsEqualTo(HomeDiscoveryMode.Area);
        await Assert.That(result.Hero).IsEmpty();
        await Assert.That(result.SectionStatuses["hero"]).IsEqualTo(HomeDiscoverySectionStatus.Empty);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task UnknownConfiguredLocationFailsClosedWithoutDiscoveryLocationFilter()
    {
        var staleLocationId = Guid.NewGuid();
        ConfigureAreas(new PublicDiscoveryAreaConfig(
            Guid.NewGuid(),
            "Stale area",
            "Brussels",
            "BE",
            LocationIds: [staleLocationId],
            IsDefault: true));
        _locationRepository.GetExistingTenantLocationIdsAsync(
                TenantId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        var requests = CaptureSuccessfulRequests();

        var result = await CreateHandler().Handle(
            new GetHomeDiscoveryQuery(Mode: "area"),
            CancellationToken.None);

        await Assert.That(result.Context.AvailableAreas).IsEmpty();
        await Assert.That(result.Context.SelectedAreaId).IsNull();
        await Assert.That(result.Hero).IsEmpty();
        await Assert.That(requests.Any(request => request.LocationIds is { Count: > 0 })).IsFalse();
        await _locationRepository.Received(1).GetExistingTenantLocationIdsAsync(
            TenantId,
            Arg.Is<IReadOnlyCollection<Guid>>(locationIds =>
                locationIds != null && locationIds.Count == 1 && locationIds.Contains(staleLocationId)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OnlineModeIncludesHybridInventoryAndScopesContextualQueries()
    {
        ConfigureAreas();
        var hybridEvent = CreateEvent("Hybrid event");
        hybridEvent.EventFormatId = (int)EventFormatEnum.Hybrid;
        hybridEvent.EventFormatFullName = "Hybrid";
        var requests = new List<GetEventListRequest>();
        _eventDiscoveryHandler.Handle(Arg.Any<GetPublicEventDiscoveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<GetPublicEventDiscoveryRequest>()!.Criteria;
                requests.Add(request);
                EventListDto[] events = request.SortBy == "views" &&
                                        request.FormatIds?.Contains((int)EventFormatEnum.Hybrid) == true
                    ? [hybridEvent]
                    : [];
                return Page(events, request.PageSize);
            });

        var result = await CreateHandler().Handle(
            new GetHomeDiscoveryQuery(Mode: "online"),
            CancellationToken.None);

        await Assert.That(result.Hero.Single().Event.Id).IsEqualTo(hybridEvent.Id);
        await Assert.That(result.Hero.Single().Event.EventFormatId).IsEqualTo((int)EventFormatEnum.Hybrid);
        await Assert.That(requests.Single(request => request.SortBy == "createdat").FormatIds)
            .IsEquivalentTo([(int)EventFormatEnum.Digital, (int)EventFormatEnum.Hybrid]);
    }

    [Test]
    public async Task MatchingEventsRemainAvailableAcrossSemanticSectionsWithoutUpcomingCutoff()
    {
        ConfigureAreas();
        var requests = new List<GetEventListRequest>();
        var shared = CreateEvent("Shared");
        var heroOnly = CreateEvent("Hero only");
        var upcomingOne = CreateEvent("Upcoming one");
        var upcomingTwo = CreateEvent("Upcoming two");
        _eventDiscoveryHandler.Handle(Arg.Any<GetPublicEventDiscoveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<GetPublicEventDiscoveryRequest>()!.Criteria;
                requests.Add(request);
                var events = request.SortBy == "views"
                    ? new[] { shared, heroOnly }
                    : new[] { shared, upcomingOne, upcomingTwo };
                return Page(events, request.PageSize);
            });

        var result = await CreateHandler().Handle(new GetHomeDiscoveryQuery(Mode: "all"), CancellationToken.None);

        await Assert.That(result.Hero.Select(item => item.Event.Id)).IsEquivalentTo(new[] { shared.Id, heroOnly.Id });
        await Assert.That(result.UpcomingInArea.Select(item => item.Event.Id))
            .IsEquivalentTo(new[] { shared.Id, upcomingOne.Id, upcomingTwo.Id });
        var upcomingRequest = requests.Single(request => request.SortBy == "date");
        await Assert.That(upcomingRequest.DateTo).IsNull();
        await Assert.That(upcomingRequest.PageSize).IsEqualTo(18);
    }

    [Test]
    public async Task OneSectionFailureDoesNotBlankSuccessfulSections()
    {
        ConfigureAreas();
        var recent = CreateEvent("Recent");
        _eventDiscoveryHandler.Handle(Arg.Any<GetPublicEventDiscoveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<GetPublicEventDiscoveryRequest>()!.Criteria;
                if (request.FormatIds?.SequenceEqual(
                        [(int)EventFormatEnum.Digital, (int)EventFormatEnum.Hybrid]) == true)
                    throw new InvalidOperationException("Online query unavailable");

                return Page(request.SortBy == "createdat" ? [recent] : [], request.PageSize);
            });

        var result = await CreateHandler().Handle(new GetHomeDiscoveryQuery(Mode: "all"), CancellationToken.None);

        await Assert.That(result.SectionStatuses["most-viewed-online"]).IsEqualTo(HomeDiscoverySectionStatus.Failed);
        await Assert.That(result.RecentlyAdded.Single().Event.Id).IsEqualTo(recent.Id);
        await Assert.That(result.SectionStatuses["recently-added"]).IsEqualTo(HomeDiscoverySectionStatus.Available);
    }

    [Test]
    public async Task OneSectionTimeoutDoesNotBlankSuccessfulSections()
    {
        ConfigureAreas();
        var recent = CreateEvent("Recent");
        var callCount = 0;
        _eventDiscoveryHandler.Handle(Arg.Any<GetPublicEventDiscoveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<GetPublicEventDiscoveryRequest>()!.Criteria;
                if (callCount++ == 0)
                    throw new OperationCanceledException(call.Arg<CancellationToken>());

                return Page(request.SortBy == "createdat" ? [recent] : [], request.PageSize);
            });

        var result = await CreateHandler().Handle(new GetHomeDiscoveryQuery(Mode: "all"), CancellationToken.None);

        await Assert.That(result.SectionStatuses["hero"]).IsEqualTo(HomeDiscoverySectionStatus.Failed);
        await Assert.That(result.RecentlyAdded.Single().Event.Id).IsEqualTo(recent.Id);
        await Assert.That(result.SectionStatuses["recently-added"]).IsEqualTo(HomeDiscoverySectionStatus.Available);
    }

    [Test]
    public async Task TimeoutBudgetsRemainBounded()
    {
        var handlerType = typeof(GetHomeDiscoveryQueryHandler);
        var sectionTimeout = (TimeSpan)handlerType
            .GetField("SectionTimeout", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        var compositeTimeout = (TimeSpan)handlerType
            .GetField("CompositeTimeout", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

        await Assert.That(sectionTimeout).IsEqualTo(TimeSpan.FromSeconds(1));
        await Assert.That(compositeTimeout).IsEqualTo(TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task MaximumCompositePayloadStaysWithinWireBudgets()
    {
        var locationId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        ConfigureAreas(new PublicDiscoveryAreaConfig(
            areaId,
            "Brussels",
            "Brussels",
            "BE",
            50.85m,
            4.35m,
            [locationId],
            IsDefault: true));
        _locationRepository.GetExistingTenantLocationIdsAsync(
                TenantId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([locationId]);
        ConfigureSetting(
            GovernanceSettingKeys.PublicExperience.EventSectionPresets,
            JsonSerializer.Serialize(new PublicEventSectionPresetsConfig(Presets:
            [
                new PublicEventSectionPresetConfig("spotlight", "Community spotlight"),
                .. Enumerable.Range(1, 5).Select(index =>
                    new PublicEventSectionPresetConfig($"curated-{index}", $"Curated {index}", SortOrder: index))
            ])));
        var eventSequence = 0;
        _eventDiscoveryHandler.Handle(Arg.Any<GetPublicEventDiscoveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<GetPublicEventDiscoveryRequest>()!.Criteria;
                var items = Enumerable.Range(0, request.PageSize)
                    .Select(_ => CreateLargeEvent(++eventSequence))
                    .ToArray();
                return Page(items, request.PageSize);
            });

        var result = await CreateHandler().Handle(
            new GetHomeDiscoveryQuery(areaId, "area"),
            CancellationToken.None);
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            result,
            ExploreJsonContext.Default.HomeDiscoveryDto);
        var brotliBytes = CompressedLength(payload, useBrotli: true);
        var gzipBytes = CompressedLength(payload, useBrotli: false);

        await Assert.That(payload.Length).IsLessThanOrEqualTo(256 * 1024);
        await Assert.That(brotliBytes).IsLessThanOrEqualTo(128 * 1024);
        await Assert.That(gzipBytes).IsLessThanOrEqualTo(132 * 1024);
    }

    [Test]
    public async Task UnsupportedCuratedPresetIsOmitted()
    {
        ConfigureAreas();
        var presets = new PublicEventSectionPresetsConfig(Presets:
        [
            new PublicEventSectionPresetConfig(
                "unsupported",
                "Unsupported",
                Filters: new PublicEventSectionEventFilter(CustomProperties:
                [
                    new PublicEventSectionCustomPropertyFilter("test", "kind", PublicEventSectionCustomPropertyOperator.Equals, ["value"])
                ])),
            new PublicEventSectionPresetConfig("curated", "Tenant picks")
        ]);
        ConfigureSetting(GovernanceSettingKeys.PublicExperience.EventSectionPresets, JsonSerializer.Serialize(presets));
        CaptureSuccessfulRequests();

        var result = await CreateHandler().Handle(new GetHomeDiscoveryQuery(Mode: "all"), CancellationToken.None);

        await Assert.That(result.CuratedSections.Select(section => section.Key)).IsEquivalentTo(["curated"]);
        await Assert.That(result.SectionStatuses.ContainsKey("curated:unsupported")).IsFalse();
    }

    private GetHomeDiscoveryQueryHandler CreateHandler() =>
        new(
            _eventDiscoveryHandler,
            _shellHandler,
            _tenantContext,
            _settingsResolver,
            _locationRepository,
            _timeProvider,
            _logger);

    private List<GetEventListRequest> CaptureSuccessfulRequests()
    {
        var requests = new List<GetEventListRequest>();
        _eventDiscoveryHandler.Handle(Arg.Any<GetPublicEventDiscoveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<GetPublicEventDiscoveryRequest>()!.Criteria;
                requests.Add(request);
                return Page([], request.PageSize);
            });
        return requests;
    }

    private void ConfigureAreas(params PublicDiscoveryAreaConfig[] areas)
    {
        ConfigureSetting(
            GovernanceSettingKeys.PublicExperience.DiscoveryAreas,
            JsonSerializer.Serialize(new PublicDiscoveryAreasConfig(Areas: areas)));
    }

    private void ConfigureSetting(string key, string value)
    {
        _settingsResolver.ResolveAsync<string>(
                key,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(value));
    }

    private static EventListDto CreateEvent(string title) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        EventTypeFullName = "Community",
        AudienceGenderFullName = "All",
        AudienceAgeFullName = "All",
        ActorDisplayName = "Organizer",
        ActorTypeFullName = "Organization",
        EventStatusFullName = "Published",
        VisibilityTypeFullName = "Public",
        EventFormatFullName = "In-Person"
    };

    private static EventListDto CreateLargeEvent(int seed) => new()
    {
        Id = Guid.NewGuid(),
        Title = Noise(seed, 500),
        Subtitle = Noise(seed + 1_000, 200),
        Description = Noise(seed + 2_000, 150),
        Slug = Noise(seed + 3_000, 500),
        PublicCode = Noise(seed + 4_000, 100),
        EventTypeId = 1,
        EventTypeFullName = Noise(seed + 5_000, 100),
        AudienceGenderId = 1,
        AudienceGenderFullName = Noise(seed + 6_000, 100),
        AudienceAgeId = 1,
        AudienceAgeFullName = Noise(seed + 7_000, 100),
        ActorId = Guid.NewGuid(),
        ActorDisplayName = Noise(seed + 8_000, 200),
        ActorTypeId = 2,
        ActorTypeFullName = Noise(seed + 9_000, 100),
        ActorProfilePictureUri = $"https://images.example/{Noise(seed + 10_000, 470)}",
        CurrencyCode = "EUR",
        FeaturedImageId = Guid.NewGuid(),
        FeaturedImageUri = $"https://images.example/{Noise(seed + 11_000, 470)}",
        EventStatusId = 2,
        EventStatusFullName = Noise(seed + 12_000, 100),
        VisibilityTypeId = 1,
        VisibilityTypeFullName = Noise(seed + 13_000, 100),
        EventFormatId = 1,
        EventFormatFullName = Noise(seed + 14_000, 100),
        FirstSessionDate = new DateOnly(2026, 8, 1),
        TenantId = TenantId
    };

    private static string Noise(int seed, int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        var random = new Random(seed);
        var builder = new StringBuilder(length);
        while (builder.Length < length)
            builder.Append(alphabet[random.Next(alphabet.Length)]);

        return builder.ToString();
    }

    private static int CompressedLength(byte[] payload, bool useBrotli)
    {
        using var output = new MemoryStream();
        using (Stream compressor = useBrotli
                   ? new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true)
                   : new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            compressor.Write(payload);
        }

        return checked((int)output.Length);
    }

    private static PaginatedResult<EventDiscoveryItemDto> Page(IEnumerable<EventListDto> items, int pageSize)
    {
        List<EventDiscoveryItemDto> values = items
            .Select(item => new EventDiscoveryItemDto { Event = item })
            .ToList();
        return PaginatedResult<EventDiscoveryItemDto>.Create(values, values.Count, 1, pageSize);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
