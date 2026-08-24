// ABOUTME: Unit tests for public event list query filtering and ownership scoping.
// ABOUTME: Verifies actor-backed organization/group filters without coupling tests to persistence.
using AutoMapper;
using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Specifications.Events;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Queries;

public class GetEventListRequestHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetEventListRequestHandler> _logger;
    private readonly TestHybridCache _cache;
    private readonly IModuleService _moduleService;
    private readonly ITenantContext _tenantContext;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly GetEventListRequestHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetEventListRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _mapper = Substitute.For<IMapper>();
        _objectStorageService = Substitute.For<IObjectStorageService>();
        _logger = Substitute.For<ILogger<GetEventListRequestHandler>>();
        _cache = new TestHybridCache();
        _moduleService = Substitute.For<IModuleService>();
        _tenantContext = Substitute.For<ITenantContext>();
        _quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        _tenantContext.TenantId.Returns(_tenantId);
        _handler = new GetEventListRequestHandler(
            _eventRepository, _actorRepository, _mapper, _objectStorageService, _logger, _cache, _moduleService, _tenantContext, _quotaResolver);

        // Default mock for mapper to avoid nulls in PaginatedResult
        _mapper.Map<List<EventListDto>>(Arg.Any<List<Explore.Domain.Event>>()).Returns(new List<EventListDto>());
    }

    [Test]
    public async Task Handle_DefaultRequest_AddsPublishedStatusAndCurrentOrUpcomingPublishedSessionFilters()
    {
        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event>(), 0));

        await _handler.Handle(new GetEventListRequest(), CancellationToken.None);

        await _eventRepository.Received(1).GetEventsWithDetailsPaged(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<EventQuerySpecification>(s => HasPublishedStatusFilter(s) && s.SubqueryFilters.Any(f =>
                f.FilterType == EventSubqueryFilterType.CurrentOrUpcomingPublishedSession)));
    }

    [Test]
    public async Task Handle_WithDateFilter_DoesNotAddCurrentOrUpcomingPublishedSessionFilter()
    {
        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event>(), 0));

        await _handler.Handle(new GetEventListRequest { DateFrom = new DateOnly(2026, 1, 1) }, CancellationToken.None);

        await _eventRepository.Received(1).GetEventsWithDetailsPaged(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<EventQuerySpecification>(s => HasPublishedStatusFilter(s) && s.SubqueryFilters.All(f =>
                f.FilterType != EventSubqueryFilterType.CurrentOrUpcomingPublishedSession)));
    }

    [Test]
    public async Task Handle_WithActorId_AddsActorFilterWithoutResolvingOrganizationOrGroup()
    {
        var actorId = Guid.NewGuid();
        _actorRepository.GetById(actorId).Returns(new Explore.Domain.Actor
        {
            Id = actorId,
            ActorType = null!,
            Pii = null!
        });
        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event>(), 0));

        await _handler.Handle(new GetEventListRequest { ActorId = actorId }, CancellationToken.None);

        await _actorRepository.DidNotReceiveWithAnyArgs().GetActorByOrganizationId(default);
        await _actorRepository.DidNotReceiveWithAnyArgs().GetActorByGroupId(default);
        await _eventRepository.Received(1).GetEventsWithDetailsPaged(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<EventQuerySpecification>(s => s != null && HasActorFilter(s, actorId)));
    }

    [Test]
    public async Task Handle_WithDeletedActorId_ReturnsEmptyPageWithoutQueryingEvents()
    {
        var actorId = Guid.NewGuid();
        _actorRepository.GetById(actorId).Returns(new Explore.Domain.Actor
        {
            Id = actorId,
            IsDeleted = true,
            ActorType = null!,
            Pii = null!
        });

        var result = await _handler.Handle(new GetEventListRequest { ActorId = actorId, PageNumber = 4, PageSize = 13 }, CancellationToken.None);

        await Assert.That(result.Items).IsEmpty();
        await Assert.That(result.TotalCount).IsEqualTo(0);
        await Assert.That(result.PageNumber).IsEqualTo(4);
        await Assert.That(result.PageSize).IsEqualTo(13);
        await _eventRepository.DidNotReceiveWithAnyArgs().GetEventsWithDetailsPaged(default, default, default!);
    }

    [Test]
    public async Task Handle_WithOrganizationId_ResolvesActorAndAddsActorFilter()
    {
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        _actorRepository.GetActorByOrganizationId(organizationId).Returns(new Explore.Domain.Actor
        {
            Id = actorId,
            ActorType = null!,
            Pii = null!
        });
        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event>(), 0));

        await _handler.Handle(new GetEventListRequest { OrganizationId = organizationId }, CancellationToken.None);

        await _eventRepository.Received(1).GetEventsWithDetailsPaged(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<EventQuerySpecification>(s => s != null && HasActorFilter(s, actorId)));
    }

    [Test]
    public async Task Handle_WithMissingOrganizationActor_ReturnsEmptyPageWithoutQueryingEvents()
    {
        var organizationId = Guid.NewGuid();
        _actorRepository.GetActorByOrganizationId(organizationId).Returns((Explore.Domain.Actor?)null);

        var result = await _handler.Handle(new GetEventListRequest { OrganizationId = organizationId, PageNumber = 3, PageSize = 11 }, CancellationToken.None);

        await Assert.That(result.Items).IsEmpty();
        await Assert.That(result.TotalCount).IsEqualTo(0);
        await Assert.That(result.PageNumber).IsEqualTo(3);
        await Assert.That(result.PageSize).IsEqualTo(11);
        await _eventRepository.DidNotReceiveWithAnyArgs().GetEventsWithDetailsPaged(default, default, default!);
    }

    [Test]
    public async Task Handle_WithMissingGroupActor_ReturnsEmptyPageWithoutQueryingEvents()
    {
        var groupId = Guid.NewGuid();
        _actorRepository.GetActorByGroupId(groupId).Returns((Explore.Domain.Actor?)null);

        var result = await _handler.Handle(new GetEventListRequest { GroupId = groupId, PageNumber = 2, PageSize = 7 }, CancellationToken.None);

        await Assert.That(result.Items).IsEmpty();
        await Assert.That(result.TotalCount).IsEqualTo(0);
        await Assert.That(result.PageNumber).IsEqualTo(2);
        await Assert.That(result.PageSize).IsEqualTo(7);
        await _eventRepository.DidNotReceiveWithAnyArgs().GetEventsWithDetailsPaged(default, default, default!);
    }

    [Test]
    public async Task Handle_WithSameRequestForDifferentTenants_UsesTenantScopedCacheEntries()
    {
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        var currentTenantId = tenantAId;

        _tenantContext.TenantId.Returns(_ => currentTenantId);
        _mapper.Map<List<EventListDto>>(Arg.Any<List<Explore.Domain.Event>>())
            .Returns(call => ((List<Explore.Domain.Event>)call[0]!).Select(CreateEventListDto).ToList());
        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns(_ =>
            {
                var title = currentTenantId == tenantAId ? "Tenant A Event" : "Tenant B Event";
                return (new List<Explore.Domain.Event> { CreateEventProbe(Guid.NewGuid(), title, currentTenantId) }, 1);
            });

        var tenantAResult = await _handler.Handle(new GetEventListRequest(), CancellationToken.None);
        currentTenantId = tenantBId;
        var tenantBResult = await _handler.Handle(new GetEventListRequest(), CancellationToken.None);

        await Assert.That(tenantAResult.Items.Single().Title).IsEqualTo("Tenant A Event");
        await Assert.That(tenantBResult.Items.Single().Title).IsEqualTo("Tenant B Event");
        await _eventRepository.Received(2).GetEventsWithDetailsPaged(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<EventQuerySpecification>());
    }

    [Test]
    public async Task Handle_TagsEventListCacheEntryWithCurrentTenant()
    {
        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event>(), 0));

        await _handler.Handle(new GetEventListRequest(), CancellationToken.None);

        await Assert.That(_cache.TagsByKey.Values.Single()).Contains(CacheTags.EventListByTenant(_tenantId));
    }

    [Test]
    public async Task Handle_CompoundDiscoveryRequest_UsesBoundedCacheKey()
    {
        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event>(), 0));

        await _handler.Handle(
            new GetEventListRequest
            {
                PageSize = 30,
                DateFrom = new DateOnly(2026, 7, 16),
                DateTo = new DateOnly(2026, 7, 23),
                FormatIds = [(int)EventFormatEnum.Local, (int)EventFormatEnum.Hybrid],
                LocationIds = [Guid.NewGuid()],
                SortBy = "views",
                SortDescending = true
            },
            CancellationToken.None);

        await Assert.That(_cache.TagsByKey.Keys.Single().Length).IsLessThanOrEqualTo(512);
    }

    [Test]
    public async Task Handle_AfterTenantListTagInvalidation_RequeriesRepository()
    {
        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns(
                (new List<Explore.Domain.Event> { CreateEventProbe(Guid.NewGuid(), "Initial", _tenantId) }, 1),
                (new List<Explore.Domain.Event> { CreateEventProbe(Guid.NewGuid(), "After invalidation", _tenantId) }, 1));
        _mapper.Map<List<EventListDto>>(Arg.Any<List<Explore.Domain.Event>>())
            .Returns(call => ((List<Explore.Domain.Event>)call[0]!).Select(CreateEventListDto).ToList());

        var initial = await _handler.Handle(new GetEventListRequest(), CancellationToken.None);
        await _cache.RemoveByTagAsync(CacheTags.EventListByTenant(_tenantId));
        var afterInvalidation = await _handler.Handle(new GetEventListRequest(), CancellationToken.None);

        await Assert.That(initial.Items.Single().Title).IsEqualTo("Initial");
        await Assert.That(afterInvalidation.Items.Single().Title).IsEqualTo("After invalidation");
        await _eventRepository.Received(2).GetEventsWithDetailsPaged(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<EventQuerySpecification>());
    }

    [Test]
    public async Task Handle_WithAbsoluteFeaturedImageUrl_ReturnsUrlWithoutPresigning()
    {
        var imageUrl = "https://placeholder.islamu.org/event-default.jpg";
        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event> { CreateEventProbe(Guid.NewGuid(), "With external image", _tenantId) }, 1));
        _mapper.Map<List<EventListDto>>(Arg.Any<List<Explore.Domain.Event>>())
            .Returns(_ =>
            [
                new EventListDto
                {
                    Id = Guid.NewGuid(),
                    Title = "With external image",
                    EventTypeFullName = string.Empty,
                    AudienceGenderFullName = string.Empty,
                    AudienceAgeFullName = string.Empty,
                    ActorDisplayName = string.Empty,
                    ActorTypeFullName = string.Empty,
                    EventStatusFullName = string.Empty,
                    VisibilityTypeFullName = string.Empty,
                    EventFormatFullName = string.Empty,
                    TenantId = _tenantId,
                    FeaturedImageUri = imageUrl
                }
            ]);

        var result = await _handler.Handle(new GetEventListRequest(), CancellationToken.None);

        await Assert.That(result.Items.Single().FeaturedImageUri).IsEqualTo(imageUrl);
        await _objectStorageService.DidNotReceiveWithAnyArgs()
            .GeneratePresignedDownloadUrl(default!, default!, default);
    }

    private static bool HasActorFilter(EventQuerySpecification specification, Guid actorId)
    {
        var probe = CreateEventProbe(actorId);
        var other = CreateEventProbe(Guid.NewGuid());
        return specification.Filters.Any(filter => filter.Predicate.Compile()(probe) && !filter.Predicate.Compile()(other));
    }

    private static bool HasPublishedStatusFilter(EventQuerySpecification specification)
    {
        var published = CreateEventProbe(Guid.NewGuid(), EventStatusEnum.Published);
        var completed = CreateEventProbe(Guid.NewGuid(), EventStatusEnum.Completed);

        return specification.Filters.Any(filter =>
            filter is EventFilter { FilterType: EventFilterType.Status } &&
            filter.Predicate.Compile()(published) &&
            !filter.Predicate.Compile()(completed));
    }

    private static Explore.Domain.Event CreateEventProbe(
        Guid actorId,
        EventStatusEnum status = EventStatusEnum.Draft) =>
        CreateEventProbe(actorId, "probe", Guid.NewGuid(), status);

    private static Explore.Domain.Event CreateEventProbe(
        Guid actorId,
        string title,
        Guid tenantId,
        EventStatusEnum status = EventStatusEnum.Draft) => new(status)
        {
            Title = title,
            Actor = null!,
            ActorId = actorId,
            TenantId = tenantId,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };

    private static EventListDto CreateEventListDto(Explore.Domain.Event @event) => new()
    {
        Id = @event.Id,
        Title = @event.Title,
        EventTypeFullName = string.Empty,
        AudienceGenderFullName = string.Empty,
        AudienceAgeFullName = string.Empty,
        ActorDisplayName = string.Empty,
        ActorTypeFullName = string.Empty,
        EventStatusFullName = string.Empty,
        VisibilityTypeFullName = string.Empty,
        EventFormatFullName = string.Empty,
        TenantId = @event.TenantId
    };

    private sealed class TestHybridCache : HybridCache
    {
        private readonly Dictionary<string, object?> _values = new();
        private readonly Dictionary<string, List<string>> _tagsByKey = new();

        public IReadOnlyDictionary<string, IReadOnlyList<string>> TagsByKey => _tagsByKey.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value);

        public override async ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            if (_values.TryGetValue(key, out var value) && value is T cached)
            {
                return cached;
            }

            var created = await factory(state, cancellationToken);
            _values[key] = created;
            _tagsByKey[key] = tags?.ToList() ?? [];
            return created;
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.Remove(key);
            _tagsByKey.Remove(key);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            var matchingKeys = _tagsByKey
                .Where(pair => pair.Value.Contains(tag))
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in matchingKeys)
            {
                _values.Remove(key);
                _tagsByKey.Remove(key);
            }

            return ValueTask.CompletedTask;
        }

        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            _values[key] = value;
            _tagsByKey[key] = tags?.ToList() ?? [];
            return ValueTask.CompletedTask;
        }
    }

    [Test]
    public async Task Handle_WithIncludedTags_AndMode_AddsTagsIncludedAllFilter()
    {
        // Arrange
        var tagIds = new List<Guid> { Guid.NewGuid() };
        var request = new GetEventListRequest
        {
            IncludedTagIds = tagIds,
            InclusionMode = TagFilterMode.And
        };

        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event>(), 0));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        await _eventRepository.Received(1).GetEventsWithDetailsPaged(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<EventQuerySpecification>(s => s != null && s.SubqueryFilters.Any(f => f.FilterType == EventSubqueryFilterType.TagsIncludedAll && f.Value == tagIds)));
    }

    [Test]
    public async Task Handle_WithIncludedTags_OrMode_AddsTagsIncludedAnyFilter()
    {
        // Arrange
        var tagIds = new List<Guid> { Guid.NewGuid() };
        var request = new GetEventListRequest
        {
            IncludedTagIds = tagIds,
            InclusionMode = TagFilterMode.Or
        };

        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event>(), 0));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        await _eventRepository.Received(1).GetEventsWithDetailsPaged(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<EventQuerySpecification>(s => s != null && s.SubqueryFilters.Any(f => f.FilterType == EventSubqueryFilterType.TagsIncludedAny && f.Value == tagIds)));
    }

    [Test]
    public async Task Handle_WithExcludedTags_OrMode_AddsTagsExcludedAnyFilter()
    {
        // Arrange
        var tagIds = new List<Guid> { Guid.NewGuid() };
        var request = new GetEventListRequest
        {
            ExcludedTagIds = tagIds,
            ExclusionMode = TagFilterMode.Or
        };

        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event>(), 0));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        await _eventRepository.Received(1).GetEventsWithDetailsPaged(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<EventQuerySpecification>(s => s != null && s.SubqueryFilters.Any(f => f.FilterType == EventSubqueryFilterType.TagsExcludedAny && f.Value == tagIds)));
    }

    [Test]
    public async Task Handle_WithExcludedTags_AndMode_AddsTagsExcludedAllFilter()
    {
        // Arrange
        var tagIds = new List<Guid> { Guid.NewGuid() };
        var request = new GetEventListRequest
        {
            ExcludedTagIds = tagIds,
            ExclusionMode = TagFilterMode.And
        };

        _eventRepository.GetEventsWithDetailsPaged(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventQuerySpecification>())
            .Returns((new List<Explore.Domain.Event>(), 0));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        await _eventRepository.Received(1).GetEventsWithDetailsPaged(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Is<EventQuerySpecification>(s => s != null && s.SubqueryFilters.Any(f => f.FilterType == EventSubqueryFilterType.TagsExcludedAll && f.Value == tagIds)));
    }
}
