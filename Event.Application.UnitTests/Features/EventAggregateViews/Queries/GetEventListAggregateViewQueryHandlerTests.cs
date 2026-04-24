// ABOUTME: Unit tests for the paginated aggregate event list query handler.
// ABOUTME: Covers pagination, filter propagation, and exposure-ceiling filtering on each list item facet preview.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAggregateView;
using Explore.Application.Features.EventAggregateViews.Handlers.Queries;
using Explore.Application.Features.EventAggregateViews.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Views;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventAggregateViews.Queries;

public class GetEventListAggregateViewQueryHandlerTests
{
    private readonly IEventAggregateViewRepository _repository;
    private readonly HybridCache _cache;
    private readonly ILogger<GetEventListAggregateViewQueryHandler> _logger;
    private readonly GetEventListAggregateViewQueryHandler _handler;

    public GetEventListAggregateViewQueryHandlerTests()
    {
        _repository = Substitute.For<IEventAggregateViewRepository>();
        _cache = new TestHybridCache();
        _logger = Substitute.For<ILogger<GetEventListAggregateViewQueryHandler>>();
        _handler = new GetEventListAggregateViewQueryHandler(_repository, _cache, _logger);
    }

    [Test]
    public async Task Handle_ReturnsPaginatedResultFromRepository()
    {
        var eventId = Guid.NewGuid();
        _repository.GetPagedAsync(Arg.Any<AggregateViewFilterDto>(), 2, 1, Arg.Any<CancellationToken>())
            .Returns((
                [CreateView(eventId, "Second Event", DateTimeOffset.Parse("2026-04-25T09:00:00+00:00"))],
                3));
        _repository.GetEventDefinitionsByEventIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([CreateDefinition(eventId, "public-facet", ExposureLevel.Public)]);

        var result = await _handler.Handle(
            new GetEventListAggregateViewQuery(new AggregateViewFilterDto(), ExposureLevel.Public, 2, 1),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.PageNumber).IsEqualTo(2);
        await Assert.That(result.Id.PageSize).IsEqualTo(1);
        await Assert.That(result.Id.TotalCount).IsEqualTo(3);
        await Assert.That(result.Id.Items.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Handle_ForwardsTitleAndDateFiltersToRepository()
    {
        var filter = new AggregateViewFilterDto
        {
            Title = "Ramadan",
            StartAtFrom = DateTimeOffset.Parse("2026-04-01T00:00:00+00:00"),
            StartAtTo = DateTimeOffset.Parse("2026-04-30T23:59:59+00:00")
        };

        _repository.GetPagedAsync(Arg.Any<AggregateViewFilterDto>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<EventWithSessionsView>(), 0));
        _repository.GetEventDefinitionsByEventIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _handler.Handle(new GetEventListAggregateViewQuery(filter, ExposureLevel.Public, 1, 20), CancellationToken.None);

        await _repository.Received(1).GetPagedAsync(
            Arg.Is<AggregateViewFilterDto>(x =>
                x.Title == filter.Title &&
                x.StartAtFrom == filter.StartAtFrom &&
                x.StartAtTo == filter.StartAtTo),
            1,
            20,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_AppliesExposureCeilingToEachItemsFacetList()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        _repository.GetPagedAsync(Arg.Any<AggregateViewFilterDto>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((
            [
                CreateView(firstId, "First Event", DateTimeOffset.Parse("2026-04-24T09:00:00+00:00")),
                CreateView(secondId, "Second Event", DateTimeOffset.Parse("2026-04-25T09:00:00+00:00"))
            ],
            2));
        _repository.GetEventDefinitionsByEventIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                CreateDefinition(firstId, "public-facet", ExposureLevel.Public),
                CreateDefinition(firstId, "internal-facet", ExposureLevel.Internal),
                CreateDefinition(secondId, "public-facet", ExposureLevel.Public),
                CreateDefinition(secondId, "internal-facet", ExposureLevel.Internal)
            ]);

        var result = await _handler.Handle(new GetEventListAggregateViewQuery(new AggregateViewFilterDto(), ExposureLevel.Public, 1, 20), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.Items.Count).IsEqualTo(2);
        await Assert.That(result.Id.Items.All(x => x.SearchableFacets.Count == 1)).IsTrue();
        await Assert.That(result.Id.Items.All(x => x.SearchableFacets[0].Key == "public-facet")).IsTrue();
    }

    private static EventWithSessionsView CreateView(Guid eventId, string title, DateTimeOffset startAt)
        => new()
        {
            EventId = eventId,
            TenantId = Guid.NewGuid(),
            Title = title,
            Slug = title.ToLowerInvariant().Replace(' ', '-'),
            Description = "desc",
            StartAt = startAt,
            EndAt = startAt.AddHours(2),
            Status = "Published",
            Visibility = "Public",
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Parse("2026-04-01T00:00:00+00:00"),
            UpdatedAt = DateTimeOffset.Parse("2026-04-02T00:00:00+00:00"),
            IslamicTheme = null,
            Madhab = null,
            IsRamadan = null,
            PrayerAware = null,
            TechStack = null,
            DifficultyLevel = null,
            TargetAudience = null,
            SessionCount = 1,
            FirstSessionStartAt = startAt,
            LastSessionEndAt = startAt.AddHours(2),
            HasInPersonSessions = true,
            HasVirtualSessions = false,
            AggregatedSessionIslamicThemes = null,
            EventCustomPropertyFacets = "{\"tenant.custom/public-facet\":[\"public\"],\"tenant.custom/internal-facet\":[\"internal\"]}",
            EventSessionCustomPropertyFacets = "{}"
        };

    private static EventCustomPropertyDefinition CreateDefinition(Guid eventId, string key, ExposureLevel exposureLevel)
        => new()
        {
            Id = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
            EventId = eventId,
            TenantId = Guid.NewGuid(),
            Namespace = "tenant.custom",
            Key = key,
            DisplayName = key,
            PropertyType = PropertyType.Text,
            IsRequired = false,
            IsMulti = true,
            IsActive = true,
            SortOrder = 1,
            ExposureLevel = exposureLevel,
            IsSearchable = true,
            IsFilterable = true,
            IsExportable = true,
            IsModerationRelevant = false,
            IsAnalyticsRelevant = false,
            IsSystemOwned = false,
            InstantiatedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

    private sealed class TestHybridCache : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
            => factory(state, cancellationToken);

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
