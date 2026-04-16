using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Specifications.Events;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Queries;

public class GetEventListRequestHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetEventListRequestHandler> _logger;
    private readonly HybridCache _cache;
    private readonly IModuleService _moduleService;
    private readonly ITenantContext _tenantContext;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly GetEventListRequestHandler _handler;

    public GetEventListRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _mapper = Substitute.For<IMapper>();
        _objectStorageService = Substitute.For<IObjectStorageService>();
        _logger = Substitute.For<ILogger<GetEventListRequestHandler>>();
        _cache = new TestHybridCache();
        _moduleService = Substitute.For<IModuleService>();
        _tenantContext = Substitute.For<ITenantContext>();
        _quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        _handler = new GetEventListRequestHandler(
            _eventRepository, _mapper, _objectStorageService, _logger, _cache, _moduleService, _tenantContext, _quotaResolver);

        // Default mock for mapper to avoid nulls in PaginatedResult
        _mapper.Map<List<EventListDto>>(Arg.Any<List<Explore.Domain.Event>>()).Returns(new List<EventListDto>());
    }

    private sealed class TestHybridCache : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            return factory(state, cancellationToken);
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
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
            Arg.Is<EventQuerySpecification>(s => s.SubqueryFilters.Any(f => f.FilterType == EventSubqueryFilterType.TagsIncludedAll && f.Value == tagIds)));
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
            Arg.Is<EventQuerySpecification>(s => s.SubqueryFilters.Any(f => f.FilterType == EventSubqueryFilterType.TagsIncludedAny && f.Value == tagIds)));
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
            Arg.Is<EventQuerySpecification>(s => s.SubqueryFilters.Any(f => f.FilterType == EventSubqueryFilterType.TagsExcludedAny && f.Value == tagIds)));
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
            Arg.Is<EventQuerySpecification>(s => s.SubqueryFilters.Any(f => f.FilterType == EventSubqueryFilterType.TagsExcludedAll && f.Value == tagIds)));
    }
}
