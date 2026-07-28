// ABOUTME: Unit tests for authorized management event detail visibility.
// ABOUTME: Verifies management reads remain available while public eligibility only controls HAL affordances.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Queries;

public sealed class GetEventManagementDetailsRequestHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventDetailsProjectionService _detailsProjectionService = Substitute.For<IEventDetailsProjectionService>();
    private readonly GetEventManagementDetailsRequestHandler _handler;

    public GetEventManagementDetailsRequestHandlerTests()
    {
        _handler = new GetEventManagementDetailsRequestHandler(_eventRepository, _detailsProjectionService);
    }

    [Test]
    public async Task Handle_ForPubliclyIneligibleEvent_ReturnsManagementDetailsWithoutPublicAffordanceState()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var eventDto = new EventDto
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Moderated management event",
            ActorDisplayName = "Organizer",
            ActorTypeFullName = "User",
            EventStatusFullName = "Moderated",
            EventStatusMasterCode = "MODERATED",
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            EventFormatFullName = "In person",
            EventFormatMasterCode = "IN_PERSON"
        };

        _detailsProjectionService.BuildAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDto);
        _eventRepository.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new GetEventManagementDetailsRequest { Id = eventId },
            CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(eventDto);
        await Assert.That(result.IsManagementView).IsTrue();
        await Assert.That(result.IsPubliclyEligible).IsFalse();
        await _eventRepository.Received(1).IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>());
        await _detailsProjectionService.Received(1).ResolveImageUrlsAsync(eventDto, Arg.Any<CancellationToken>());
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
            Title = "Uncertain management event",
            ActorDisplayName = "Organizer",
            ActorTypeFullName = "User",
            EventStatusFullName = "Published",
            EventStatusMasterCode = "PUBLISHED",
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            EventFormatFullName = "In person",
            EventFormatMasterCode = "IN_PERSON"
        };

        _detailsProjectionService.BuildAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDto);
        var repositoryException = new InvalidOperationException("eligibility unavailable");
        _eventRepository.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(repositoryException));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new GetEventManagementDetailsRequest { Id = eventId },
                CancellationToken.None));

        await Assert.That(exception).IsSameReferenceAs(repositoryException);
        await _detailsProjectionService.DidNotReceive().ResolveImageUrlsAsync(Arg.Any<EventDto>(), Arg.Any<CancellationToken>());
    }
}
