// ABOUTME: Unit tests for public event detail query visibility behavior.
// ABOUTME: Verifies moderated/hidden events are not exposed through the public detail handler.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Queries;

public class GetEventDetailsRequestHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventDetailsProjectionService _detailsProjectionService;
    private readonly HybridCache _cache;
    private readonly IUserContext _userContext;
    private readonly GetEventDetailsRequestHandler _handler;

    public GetEventDetailsRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _detailsProjectionService = Substitute.For<IEventDetailsProjectionService>();
        _cache = new TestHybridCache();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetEventDetailsRequestHandler(_eventRepository, _detailsProjectionService, _cache, _userContext);
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

        var eventDto = new EventDto
        {
            Id = eventId,
            Title = "Test Event",
            Subtitle = "Test Subtitle",
            EventStatusId = (int)EventStatusEnum.Published,
            ActorDisplayName = string.Empty,
            ActorTypeFullName = string.Empty,
            EventStatusFullName = string.Empty,
            EventStatusMasterCode = string.Empty,
            VisibilityTypeFullName = string.Empty,
            VisibilityTypeMasterCode = string.Empty,
            EventFormatFullName = string.Empty,
            EventFormatMasterCode = string.Empty
        };

        _detailsProjectionService.BuildAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(eventId);
        await Assert.That(result.Title).IsEqualTo("Test Event");

        await _detailsProjectionService.Received(1).BuildAsync(eventId, Arg.Any<CancellationToken>());
        await _detailsProjectionService.Received(1).ResolveImageUrlsAsync(eventDto, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithModeratedEvent_ReturnsNullAndDoesNotResolveImages()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var eventDto = new EventDto
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Test Event",
            ActorDisplayName = string.Empty,
            ActorTypeFullName = string.Empty,
            EventStatusId = (int)EventStatusEnum.Moderated,
            EventStatusFullName = "Moderated",
            EventStatusMasterCode = "MODERATED",
            VisibilityTypeFullName = string.Empty,
            VisibilityTypeMasterCode = string.Empty,
            EventFormatFullName = string.Empty,
            EventFormatMasterCode = string.Empty
        };

        _detailsProjectionService.BuildAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDto);

        var result = await _handler.Handle(new GetEventDetailsRequest { Id = eventId }, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _detailsProjectionService.DidNotReceive().ResolveImageUrlsAsync(Arg.Any<EventDto>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventDetailsRequest { Id = eventId };

        _detailsProjectionService.BuildAsync(eventId, Arg.Any<CancellationToken>()).Returns((EventDto?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
        await _detailsProjectionService.Received(1).BuildAsync(eventId, Arg.Any<CancellationToken>());
        await _detailsProjectionService.DidNotReceive().ResolveImageUrlsAsync(Arg.Any<EventDto>(), Arg.Any<CancellationToken>());
    }
}
