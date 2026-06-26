// ABOUTME: Unit tests for the public event calendar export query.
// ABOUTME: Verifies draft/private events are excluded before the API serializes .ics files.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Queries;

public sealed class GetEventCalendarExportRequestHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventSessionRepository _sessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly GetEventCalendarExportRequestHandler _handler;

    public GetEventCalendarExportRequestHandlerTests()
    {
        _handler = new GetEventCalendarExportRequestHandler(_eventRepository, _sessionRepository);
    }

    [Test]
    public async Task Handle_ForPublishedPublicEvent_ReturnsPrimarySessionExport()
    {
        var eventId = Guid.NewGuid();
        var laterStart = new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero);
        var earlierStart = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);

        _eventRepository.GetEventWithDetails(eventId)
            .Returns(CreateEvent(eventId, EventStatusEnum.Published, VisibilityTypeEnum.Public));
        _sessionRepository.GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns([
                CreateSession(eventId, laterStart, laterStart.AddHours(1)),
                CreateSession(eventId, earlierStart, earlierStart.AddHours(2))
            ]);

        var result = await _handler.Handle(new GetEventCalendarExportRequest(eventId), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.EventId).IsEqualTo(eventId);
        await Assert.That(result.Title).IsEqualTo("Calendar Event");
        await Assert.That(result.StartsAtUtc).IsEqualTo(earlierStart);
        await Assert.That(result.EndsAtUtc).IsEqualTo(earlierStart.AddHours(2));
        await _sessionRepository.DidNotReceive().GetSessionsByEvent(Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WhenPublicSessionQueryReturnsNoSessions_ReturnsNull()
    {
        var eventId = Guid.NewGuid();
        _eventRepository.GetEventWithDetails(eventId)
            .Returns(CreateEvent(eventId, EventStatusEnum.Published, VisibilityTypeEnum.Public));
        _sessionRepository.GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new GetEventCalendarExportRequest(eventId), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _sessionRepository.Received(1).GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>());
        await _sessionRepository.DidNotReceive().GetSessionsByEvent(Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_ForDraftEvent_ReturnsNullWithoutLoadingSessions()
    {
        var eventId = Guid.NewGuid();
        _eventRepository.GetEventWithDetails(eventId)
            .Returns(CreateEvent(eventId, EventStatusEnum.Draft, VisibilityTypeEnum.Public));

        var result = await _handler.Handle(new GetEventCalendarExportRequest(eventId), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _sessionRepository.DidNotReceive().GetSessionsByEvent(Arg.Any<Guid>());
        await _sessionRepository.DidNotReceive().GetPublicSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ForPublishedPrivateEvent_ReturnsNullWithoutLoadingSessions()
    {
        var eventId = Guid.NewGuid();
        _eventRepository.GetEventWithDetails(eventId)
            .Returns(CreateEvent(eventId, EventStatusEnum.Published, VisibilityTypeEnum.Private));

        var result = await _handler.Handle(new GetEventCalendarExportRequest(eventId), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _sessionRepository.DidNotReceive().GetSessionsByEvent(Arg.Any<Guid>());
        await _sessionRepository.DidNotReceive().GetPublicSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static Explore.Domain.Event CreateEvent(
        Guid eventId,
        EventStatusEnum status,
        VisibilityTypeEnum visibility)
    {
        return new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Calendar Event",
            Description = "Calendar description",
            Slug = "calendar-event",
            EventStatusId = (int)status,
            VisibilityTypeId = (int)visibility,
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static EventSession CreateSession(
        Guid eventId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        return new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Tenant = null!,
            StartTime = startsAt,
            EndTime = endsAt
        };
    }
}
