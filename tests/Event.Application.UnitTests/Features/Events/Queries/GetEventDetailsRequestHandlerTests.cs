// ABOUTME: Unit tests for public event detail query visibility behavior.
// ABOUTME: Verifies moderated/hidden events are not exposed through the public detail handler.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Caching;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Features.RegistrationForms.Requests.Queries;
using Explore.Application.DTOs.RegistrationForms;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Queries;

public class GetEventDetailsRequestHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventDetailsProjectionService _detailsProjectionService;
    private readonly TestHybridCache _cache;
    private readonly ISender _sender;
    private readonly GetEventDetailsRequestHandler _handler;

    public GetEventDetailsRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _detailsProjectionService = Substitute.For<IEventDetailsProjectionService>();
        _cache = new TestHybridCache();
        _sender = Substitute.For<ISender>();
        _handler = new GetEventDetailsRequestHandler(
            _eventRepository, _detailsProjectionService, _cache, _sender);
    }

    [Test]
    public async Task Handle_WithCacheRehydratedFlagFalse_EnrichesFromOptionalQuestionnaireQuery()
    {
        Guid eventId = Guid.CreateVersion7();
        EventDto cachedEvent = EligibleEvent(eventId);
        cachedEvent.ParticipationConfiguration!.HasValidOptionalQuestionnaire = false;
        var descriptor = new OptionalQuestionnaireDto(
            eventId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            "en",
            "hash",
            "{}",
            "{}",
            "{}",
            "{}",
            Guid.CreateVersion7());
        _detailsProjectionService.BuildAsync(eventId, Arg.Any<CancellationToken>()).Returns(cachedEvent);
        _eventRepository.IsPubliclyEligibleAsync(cachedEvent.TenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(true);
        _sender.Send(
                Arg.Is<GetOptionalQuestionnaireQuery>(query => query.EventId == eventId),
                Arg.Any<CancellationToken>())
            .Returns(descriptor);

        EventDto? result = await _handler.Handle(
            new GetEventDetailsRequest { Id = eventId }, CancellationToken.None);

        await Assert.That(result!.ParticipationConfiguration!.HasValidOptionalQuestionnaire).IsTrue();
        await Assert.That(cachedEvent.ParticipationConfiguration.HasValidOptionalQuestionnaire).IsFalse();
    }

    [Test]
    public async Task Handle_WhenOptionalQuestionnaireIsMissing_KeepsServerOnlyFlagFalse()
    {
        Guid eventId = Guid.CreateVersion7();
        EventDto cachedEvent = EligibleEvent(eventId);
        _detailsProjectionService.BuildAsync(eventId, Arg.Any<CancellationToken>()).Returns(cachedEvent);
        _eventRepository.IsPubliclyEligibleAsync(cachedEvent.TenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(true);
        _sender.Send(Arg.Any<GetOptionalQuestionnaireQuery>(), Arg.Any<CancellationToken>())
            .Returns((OptionalQuestionnaireDto?)null);

        EventDto? result = await _handler.Handle(
            new GetEventDetailsRequest { Id = eventId }, CancellationToken.None);

        await Assert.That(result!.ParticipationConfiguration!.HasValidOptionalQuestionnaire).IsFalse();
    }

    private sealed class TestHybridCache : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            LastTags = tags?.ToArray();
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

        public IReadOnlyCollection<string>? LastTags { get; private set; }
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
            TenantId = Guid.NewGuid(),
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
        _eventRepository.IsPubliclyEligibleAsync(eventDto.TenantId, eventId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(eventId);
        await Assert.That(result.Title).IsEqualTo("Test Event");
        await Assert.That(result.IsPubliclyEligible).IsTrue();
        await Assert.That(result.IsManagementView).IsFalse();
        await Assert.That(_cache.LastTags).IsEquivalentTo([
            CacheTags.Events,
            CacheTags.EventDetails,
            CacheTags.Event(eventId)
        ]);

        await _detailsProjectionService.Received(1).BuildAsync(eventId, Arg.Any<CancellationToken>());
        await _eventRepository.Received(1).IsPubliclyEligibleAsync(eventDto.TenantId, eventId, Arg.Any<CancellationToken>());
        await _detailsProjectionService.Received(1).ResolveImageUrlsAsync(
            Arg.Is<EventDto>(dto => !ReferenceEquals(dto, eventDto)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithCachedCandidate_ReturnsRequestLocalCopy()
    {
        var eventId = Guid.NewGuid();
        var eventDto = new EventDto
        {
            Id = eventId,
            TenantId = Guid.NewGuid(),
            Title = "Cached Event",
            FeaturedImageUri = "event-image-key",
            ActorDisplayName = string.Empty,
            ActorTypeFullName = string.Empty,
            EventStatusFullName = "Published",
            EventStatusMasterCode = "PUBLISHED",
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            EventFormatFullName = string.Empty,
            EventFormatMasterCode = string.Empty
        };
        _detailsProjectionService.BuildAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDto);
        _eventRepository.IsPubliclyEligibleAsync(eventDto.TenantId, eventId, Arg.Any<CancellationToken>()).Returns(true);
        _detailsProjectionService.ResolveImageUrlsAsync(Arg.Any<EventDto>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.ArgAt<EventDto>(0).FeaturedImageUri = "https://cdn.example/event.jpg";
                return Task.CompletedTask;
            });

        var result = await _handler.Handle(new GetEventDetailsRequest { Id = eventId }, CancellationToken.None);

        await Assert.That(ReferenceEquals(result, eventDto)).IsFalse();
        await Assert.That(result.IsPubliclyEligible).IsTrue();
        await Assert.That(result.IsManagementView).IsFalse();
        await Assert.That(result.FeaturedImageUri).IsEqualTo("https://cdn.example/event.jpg");
        await Assert.That(eventDto.IsPubliclyEligible).IsFalse();
        await Assert.That(eventDto.FeaturedImageUri).IsEqualTo("event-image-key");
    }

    [Test]
    public async Task Handle_WithIneligibleEvent_ReturnsNullWithoutResolvingImages()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var eventDto = new EventDto
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Ineligible Event",
            ActorDisplayName = string.Empty,
            ActorTypeFullName = string.Empty,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatusFullName = "Published",
            EventStatusMasterCode = "PUBLISHED",
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            EventFormatFullName = string.Empty,
            EventFormatMasterCode = string.Empty
        };

        _detailsProjectionService.BuildAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDto);
        _eventRepository.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new GetEventDetailsRequest { Id = eventId }, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _eventRepository.Received(1).IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>());
        await _detailsProjectionService.DidNotReceive().ResolveImageUrlsAsync(Arg.Any<EventDto>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEligibilityRepositoryFails_PropagatesWithoutResolvingImages()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var eventDto = new EventDto
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Uncertain Event",
            ActorDisplayName = string.Empty,
            ActorTypeFullName = string.Empty,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatusFullName = "Published",
            EventStatusMasterCode = "PUBLISHED",
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            EventFormatFullName = string.Empty,
            EventFormatMasterCode = string.Empty
        };

        _detailsProjectionService.BuildAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDto);
        var repositoryException = new InvalidOperationException("eligibility unavailable");
        _eventRepository.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(repositoryException));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new GetEventDetailsRequest { Id = eventId }, CancellationToken.None));

        await Assert.That(exception).IsSameReferenceAs(repositoryException);
        await _detailsProjectionService.DidNotReceive().ResolveImageUrlsAsync(Arg.Any<EventDto>(), Arg.Any<CancellationToken>());
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
        _eventRepository.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>()).Returns(false);

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

    private static EventDto EligibleEvent(Guid eventId) => new()
    {
        Id = eventId,
        TenantId = Guid.CreateVersion7(),
        Title = "Cached Event",
        EventStatusId = (int)EventStatusEnum.Published,
        ActorDisplayName = string.Empty,
        ActorTypeFullName = string.Empty,
        EventStatusFullName = "Published",
        EventStatusMasterCode = "PUBLISHED",
        VisibilityTypeFullName = "Public",
        VisibilityTypeMasterCode = "PUBLIC",
        EventFormatFullName = string.Empty,
        EventFormatMasterCode = string.Empty,
        ParticipationConfiguration = new EventParticipationConfigurationDto
        {
            EventId = eventId,
            ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.WalkIn,
            AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.NotApplicable
        }
    };
}
