using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Queries;

public class GetEventDetailsRequestHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventTagsRepository _eventTagsRepository;
    private readonly IEventCategoriesRepository _eventCategoriesRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetEventDetailsRequestHandler> _logger;
    private readonly HybridCache _cache;
    private readonly IUserContext _userContext;
    private readonly GetEventDetailsRequestHandler _handler;

    public GetEventDetailsRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _eventTagsRepository = Substitute.For<IEventTagsRepository>();
        _eventCategoriesRepository = Substitute.For<IEventCategoriesRepository>();
        _mapper = Substitute.For<IMapper>();
        _objectStorageService = Substitute.For<IObjectStorageService>();
        _logger = Substitute.For<ILogger<GetEventDetailsRequestHandler>>();
        _cache = new TestHybridCache();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetEventDetailsRequestHandler(_eventRepository, _eventTagsRepository, _eventCategoriesRepository, _mapper, _objectStorageService, _logger, _cache, _userContext);
    }

    private sealed class TestHybridCache : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            return factory(state, cancellationToken);
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Test]
    public async Task Handle_WithValidId_ReturnsEventDto()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventDetailsRequest { Id = eventId };

        var eventEntity = new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Test Event",
            Subtitle = "Test Subtitle",
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
        var eventDto = new EventDto
        {
            Id = eventId,
            Title = "Test Event",
            Subtitle = "Test Subtitle",
            ActorDisplayName = string.Empty,
            ActorTypeFullName = string.Empty,
            EventStatusFullName = string.Empty,
            EventStatusMasterCode = string.Empty,
            VisibilityTypeFullName = string.Empty,
            VisibilityTypeMasterCode = string.Empty,
            EventFormatFullName = string.Empty,
            EventFormatMasterCode = string.Empty
        };

        _eventRepository.GetEventWithDetails(eventId).Returns(eventEntity);
        _mapper.Map<EventDto>(eventEntity).Returns(eventDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(eventId);
        await Assert.That(result.Title).IsEqualTo("Test Event");

        await _eventRepository.Received(1).GetEventWithDetails(eventId);
    }

    [Test]
    public async Task Handle_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventDetailsRequest { Id = eventId };

        _eventRepository.GetEventWithDetails(eventId).Returns((Explore.Domain.Event)null!);
        _mapper.Map<EventDto>(null).Returns((EventDto)null!);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
        await _eventRepository.Received(1).GetEventWithDetails(eventId);
    }
}
