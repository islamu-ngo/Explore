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
                Title = session.Title
            })
            .ToList();

        _eventSessionRepository.GetSessionsByEvent(eventId).Returns(sessions);
        _mapper.Map<List<EventSessionListDto>>(sessions).Returns(expectedDtos);

        var result = await _handler.Handle(
            new GetManagedSessionsByEventRequest { EventId = eventId },
            CancellationToken.None);

        await Assert.That(result).IsEquivalentTo(expectedDtos);
        await Assert.That(result.Select(dto => dto.LocationId)).IsEquivalentTo(sessions.Select(session => session.LocationId));
        await Assert.That(result.Select(dto => dto.LocationFullName)).IsEquivalentTo(sessions.Select(session => session.Location!.FullName));
        await Assert.That(result.Select(dto => dto.RoomId)).IsEquivalentTo(sessions.Select(session => session.RoomId));
        await Assert.That(result.Select(dto => dto.RoomName)).IsEquivalentTo(sessions.Select(session => session.Room!.Name));
        await _eventSessionRepository.Received(1).GetSessionsByEvent(eventId);
        await _eventSessionRepository.DidNotReceive()
            .GetPublicSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static EventSession CreateSession(Guid eventId, string title)
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            FullName = $"{title} venue",
            Country = "Belgium",
            City = "Brussels",
            Tenant = null!
        };
        var room = new LocationRoom
        {
            Id = Guid.NewGuid(),
            LocationId = location.Id,
            Location = location,
            Name = $"{title} room",
            Tenant = null!
        };

        return new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Tenant = null!,
            Title = title,
            LocationId = location.Id,
            Location = location,
            RoomId = room.Id,
            Room = room
        };
    }
}
