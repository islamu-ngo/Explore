using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Agenda;
using Explore.Application.Features.Agenda.Handlers.Queries;
using Explore.Application.Features.Agenda.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Agenda.Queries;

public class GetEventAgendaProjectionRequestHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly GetEventAgendaProjectionRequestHandler _handler;

    public GetEventAgendaProjectionRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();

        _handler = new GetEventAgendaProjectionRequestHandler(
            _eventRepository,
            _eventDayRepository,
            _eventSessionRepository,
            _eventAgendaItemRepository
        );
    }

    [Test]
    public async Task Handle_WithNonExistentEvent_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventAgendaProjectionRequest { EventId = eventId };

        _eventRepository.GetById(eventId).Returns((Explore.Domain.Event?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Handle_WithExistingEvent_ReturnsProjection()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventAgendaProjectionRequest { EventId = eventId };

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.Title = "Test Conference";
        parentEvent.Timezone = "Europe/Brussels";
        _eventRepository.GetById(eventId).Returns(parentEvent);

        _eventDayRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventDay>());
        _eventSessionRepository.GetSessionsByEvent(eventId)
            .Returns(new List<EventSession>());
        _eventAgendaItemRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventAgendaItem>());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.EventId).IsEqualTo(eventId);
        await Assert.That(result.EventTitle).IsEqualTo("Test Conference");
    }

    [Test]
    public async Task Handle_WithSessionsAndAgendaItems_GroupsByLocalDate()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventAgendaProjectionRequest { EventId = eventId };
        var localDate = new DateOnly(2026, 7, 15);

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.Title = "Multi-day Conference";
        parentEvent.Timezone = "Europe/Brussels";
        _eventRepository.GetById(eventId).Returns(parentEvent);

        var eventDay = new EventDay
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            LocalDate = localDate,
            Label = "Day 1",
            IsPublished = true,
            SortOrder = 1,
            AllowsDayScopeRegistration = false,
            Event = null!,
            Tenant = null!
        };
        _eventDayRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventDay> { eventDay });

        var calculator = Substitute.For<IEventScheduleProjectionCalculator>();

        var session = DataBuilder.EventSession.Generate();
        session.EventId = eventId;
        session.StartTime = new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero);
        session.EndTime = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);
        session.SortOrder = 1;
        calculator.Project(session.StartTime, session.EndTime, "Europe/Brussels")
            .Returns(new LocalScheduleProjection(localDate, localDate, new TimeOnly(10, 0), new TimeOnly(12, 0), 600, 720));
        session.ReprojectLocalTimes("Europe/Brussels", calculator);
        _eventSessionRepository.GetSessionsByEvent(eventId)
            .Returns(new List<EventSession> { session });

        var agendaItem = DataBuilder.EventAgendaItem.Generate();
        agendaItem.EventId = eventId;
        agendaItem.StartTime = new DateTimeOffset(2026, 7, 15, 7, 0, 0, TimeSpan.Zero);
        agendaItem.EndTime = new DateTimeOffset(2026, 7, 15, 7, 30, 0, TimeSpan.Zero);
        agendaItem.SortOrder = 0;
        calculator.Project(agendaItem.StartTime, agendaItem.EndTime, "Europe/Brussels")
            .Returns(new LocalScheduleProjection(localDate, localDate, new TimeOnly(9, 0), new TimeOnly(9, 30), 540, 570));
        agendaItem.ReprojectLocalTimes("Europe/Brussels", calculator);
        _eventAgendaItemRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventAgendaItem> { agendaItem });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Days).IsNotNull();
        await Assert.That(result.Days.Count).IsEqualTo(1);
        await Assert.That(result.Days[0].LocalDate).IsEqualTo(localDate);
        await Assert.That(result.Days[0].Entries.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Handle_WithNoSessionsOrAgendaItems_ReturnsEmptyDays()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventAgendaProjectionRequest { EventId = eventId };

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        _eventRepository.GetById(eventId).Returns(parentEvent);

        _eventDayRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventDay>());
        _eventSessionRepository.GetSessionsByEvent(eventId)
            .Returns(new List<EventSession>());
        _eventAgendaItemRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventAgendaItem>());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Days).IsNotNull();
        await Assert.That(result.Days.Count).IsEqualTo(0);
    }
}
