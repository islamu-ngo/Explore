// ABOUTME: Unit tests for registration-scoped attendee event calendar exports.
// ABOUTME: Verifies attendee-purpose exact disclosure and fail-closed registration authorization.

using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Queries;

public sealed class GetAttendeeEventCalendarExportRequestHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventSessionRepository _sessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IEventLocationDisclosureService _disclosureService = Substitute.For<IEventLocationDisclosureService>();
    private readonly GetAttendeeEventCalendarExportRequestHandler _handler;

    public GetAttendeeEventCalendarExportRequestHandlerTests()
    {
        _handler = new GetAttendeeEventCalendarExportRequestHandler(
            _eventRepository,
            _sessionRepository,
            _disclosureService);
    }

    [Test]
    [Category("CalendarPrivacy")]
    public async Task Handle_ForRegisteredAttendee_UsesAttendeeDisclosureAndReturnsExactLocation()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        EventSession session = CreateSessionWithPlacement(eventId, tenantId);
        Guid eventLocationId = session.EventLocationId!.Value;
        _eventRepository.GetEventWithDetails(eventId).Returns(CreatePublishedEvent(eventId, tenantId));
        _sessionRepository.GetSessionsByEvent(eventId).Returns([session]);
        _disclosureService.ResolveManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocationDisclosureRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, EventLocationDisclosureResult>
            {
                [eventLocationId] = EventLocationDisclosureResult.Attendee(
                    eventLocationId,
                    EventLocationDisclosureState.Available,
                    new EventLocationDisclosureValues(
                        Country: "Belgium",
                        City: "Brussels",
                        VenueName: "PRIVATE-HOME-CALENDAR-CANARY",
                        RoomName: "FAMILY-ROOM-CANARY",
                        StreetAddress: "17 Confidential Crescent",
                        Postcode: "SECRET-1040"))
            });

        var result = await _handler.Handle(
            new GetAttendeeEventCalendarExportRequest(eventId),
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Location).Contains("PRIVATE-HOME-CALENDAR-CANARY");
        await Assert.That(result.Location).Contains("FAMILY-ROOM-CANARY");
        await Assert.That(result.Location).Contains("17 Confidential Crescent");
        await Assert.That(result.Location).Contains("SECRET-1040");
        await _disclosureService.Received(1).ResolveManyAsync(
            Arg.Is<IReadOnlyCollection<EventLocationDisclosureRequest>>(requests =>
                requests.Count == 1
                && requests.Single().TenantId == tenantId
                && requests.Single().EventId == eventId
                && requests.Single().EventLocationId == eventLocationId
                && requests.Single().RoomId == session.RoomId
                && requests.Single().RequesterUserId == null
                && requests.Single().Purpose == EventLocationDisclosurePurpose.Attendee),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("CalendarPrivacy")]
    public async Task Handle_WithoutRegistrationAuthority_ReturnsNull()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        EventSession session = CreateSessionWithPlacement(eventId, tenantId);
        Guid eventLocationId = session.EventLocationId!.Value;
        _eventRepository.GetEventWithDetails(eventId).Returns(CreatePublishedEvent(eventId, tenantId));
        _sessionRepository.GetSessionsByEvent(eventId).Returns([session]);
        _disclosureService.ResolveManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocationDisclosureRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, EventLocationDisclosureResult>
            {
                [eventLocationId] = EventLocationDisclosureResult.Suppressed(
                    eventLocationId,
                    EventLocationDisclosurePurpose.Attendee,
                    EventLocationDisclosureState.Hidden)
            });

        var result = await _handler.Handle(
            new GetAttendeeEventCalendarExportRequest(eventId),
            CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    private static Explore.Domain.Event CreatePublishedEvent(Guid eventId, Guid tenantId)
    {
        return new Explore.Domain.Event
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Attendee Calendar Event",
            Description = "Registration-scoped calendar description",
            Slug = "attendee-calendar-event",
            EventStatusId = (int)EventStatusEnum.Published,
            VisibilityTypeId = (int)VisibilityTypeEnum.Private,
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static EventSession CreateSessionWithPlacement(Guid eventId, Guid tenantId)
    {
        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventId = eventId,
            Event = null!,
            Tenant = null!,
            StartTime = new DateTimeOffset(2026, 7, 19, 16, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 7, 19, 17, 0, 0, TimeSpan.Zero),
            EventSessionStatusId = (int)EventSessionStatusEnum.Published
        };
        EventLocation placement = EventLocation.CreatePhysical(
            tenantId,
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);
        session.AssignEventLocation(placement);
        session.RoomId = Guid.NewGuid();
        return session;
    }
}
