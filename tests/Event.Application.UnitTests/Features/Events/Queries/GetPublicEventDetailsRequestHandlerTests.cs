// ABOUTME: Unit tests for the public event detail query visibility and tenant-isolation behavior.
// ABOUTME: Verifies only published public events are returned and cached cross-tenant responses cannot be reused.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Queries;

public sealed class GetPublicEventDetailsRequestHandlerTests
{
    private readonly IEventDetailsProjectionService _detailsProjectionService = Substitute.For<IEventDetailsProjectionService>();
    private readonly GetPublicEventDetailsRequestHandler _handler;

    public GetPublicEventDetailsRequestHandlerTests()
    {
        _handler = new GetPublicEventDetailsRequestHandler(_detailsProjectionService);
    }

    [Test]
    public async Task Handle_ForPublishedPublicEvent_ReturnsResolvedDetails()
    {
        EventDto eventDto = CreateEvent(EventStatusEnum.Published, VisibilityTypeEnum.Public);
        _detailsProjectionService.BuildByPublicCodeAsync("safe", Arg.Any<CancellationToken>())
            .Returns(eventDto);

        EventDto? result = await _handler.Handle(Request(), CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(eventDto);
        await _detailsProjectionService.Received(1).ResolveImageUrlsAsync(eventDto, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ForPublishedPrivateEvent_ReturnsNullWithoutResolvingImages()
    {
        EventDto eventDto = CreateEvent(EventStatusEnum.Published, VisibilityTypeEnum.Private);
        _detailsProjectionService.BuildByPublicCodeAsync("safe", Arg.Any<CancellationToken>())
            .Returns(eventDto);

        EventDto? result = await _handler.Handle(Request(), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _detailsProjectionService.DidNotReceive().ResolveImageUrlsAsync(eventDto, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_DoesNotReusePublicCodeResponseAcrossTenantScopedProjectionReads()
    {
        EventDto firstTenantEvent = CreateEvent(EventStatusEnum.Published, VisibilityTypeEnum.Public);
        EventDto secondTenantEvent = CreateEvent(EventStatusEnum.Published, VisibilityTypeEnum.Public);
        _detailsProjectionService.BuildByPublicCodeAsync("safe", Arg.Any<CancellationToken>())
            .Returns(firstTenantEvent, secondTenantEvent);

        EventDto? firstResult = await _handler.Handle(Request(), CancellationToken.None);
        EventDto? secondResult = await _handler.Handle(Request(), CancellationToken.None);

        await Assert.That(firstResult).IsSameReferenceAs(firstTenantEvent);
        await Assert.That(secondResult).IsSameReferenceAs(secondTenantEvent);
        await _detailsProjectionService.Received(2).BuildByPublicCodeAsync("safe", Arg.Any<CancellationToken>());
    }

    private static GetPublicEventDetailsRequest Request() => new()
    {
        SlugCode = "secure-event-tenant-safe"
    };

    private static EventDto CreateEvent(EventStatusEnum status, VisibilityTypeEnum visibility) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Title = "Public event",
        ActorDisplayName = "Organizer",
        ActorTypeFullName = "Organization",
        EventStatusId = (int)status,
        EventStatusFullName = status.ToString(),
        EventStatusMasterCode = status.ToString().ToLowerInvariant(),
        VisibilityTypeId = (int)visibility,
        VisibilityTypeFullName = visibility.ToString(),
        VisibilityTypeMasterCode = visibility.ToString().ToLowerInvariant(),
        EventFormatFullName = "In person",
        EventFormatMasterCode = "in-person"
    };
}
