// ABOUTME: Unit tests for the authorized management event-session list query.
// ABOUTME: Verifies management reads intentionally include draft/internal sessions through the broad repository path.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Handlers.Queries;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessions.Queries;

[Category("EventLocationPrivacy")]
public sealed class GetManagedSessionsByEventRequestHandlerTests
{
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetManagedSessionsByEventRequestHandler _handler;

    public GetManagedSessionsByEventRequestHandlerTests()
    {
        _handler = new GetManagedSessionsByEventRequestHandler(_eventSessionRepository, _mapper);
    }

    [Test]
    public async Task Handle_ReturnsSessionsFromBroadManagementRepositoryRead()
    {
        var eventId = Guid.NewGuid();
        var sessions = new List<EventSession>
        {
            CreateSession(eventId, "Published Session"),
            CreateSession(eventId, "Draft Session")
        };
        var expectedDtos = sessions
            .Select(session => new EventSessionListDto
            {
                Id = session.Id,
                EventId = session.EventId,
                EventTitle = string.Empty,
                Title = session.Title,
                LocationId = Guid.NewGuid(),
                LocationFullName = "Managed venue",
                RoomId = Guid.NewGuid(),
                RoomName = "Managed room"
            })
            .ToList();

        _eventSessionRepository.GetSessionsByEvent(eventId).Returns(sessions);
        _mapper.Map<List<EventSessionListDto>>(sessions).Returns(expectedDtos);

        var result = await _handler.Handle(
            new GetManagedSessionsByEventRequest { EventId = eventId },
            CancellationToken.None);

        await Assert.That(result).IsEquivalentTo(expectedDtos);
        await Assert.That(result.All(dto => dto.LocationId.HasValue && dto.RoomId.HasValue)).IsTrue();
        await _eventSessionRepository.Received(1).GetSessionsByEvent(eventId);
        await _eventSessionRepository.DidNotReceive()
            .GetPublicSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static EventSession CreateSession(Guid eventId, string title)
    {
        return new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Tenant = null!,
            Title = title
        };
    }
}
