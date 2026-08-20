// ABOUTME: Unit tests for public event agenda projection assembly.
// ABOUTME: Verifies sessions, agenda items, and public lifecycle guards.
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Agenda;
using Explore.Application.Features.Agenda.Handlers.Queries;
using Explore.Application.Features.Agenda.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
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
    private readonly IEventLocationDisclosureService _disclosureService;
    private readonly GetEventAgendaProjectionRequestHandler _handler;

    public GetEventAgendaProjectionRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
        _disclosureService = Substitute.For<IEventLocationDisclosureService>();

        _handler = new GetEventAgendaProjectionRequestHandler(
            _eventRepository,
            _eventDayRepository,
            _eventSessionRepository,
            _eventAgendaItemRepository,
            _disclosureService
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

        var parentEvent = DataBuilder.EventWithStatus(EventStatusEnum.Published).Generate();
        parentEvent.Id = eventId;
        parentEvent.Title = "Test Conference";
        parentEvent.Timezone = "Europe/Brussels";
        parentEvent.VisibilityTypeId = (int)VisibilityTypeEnum.Public;
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.IsPubliclyEligibleAsync(parentEvent.TenantId, eventId, Arg.Any<CancellationToken>()).Returns(true);

        _eventDayRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventDay>());
        _eventSessionRepository.GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventSession>());
        _eventAgendaItemRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventAgendaItem>());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.EventId).IsEqualTo(eventId);
        await Assert.That(result.EventTitle).IsEqualTo("Test Conference");
        await _eventSessionRepository.DidNotReceive().GetSessionsByEvent(Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WhenEventIsNotCentrallyPubliclyEligible_ReturnsNullWithoutLoadingProjectionInputs()
    {
        var eventId = Guid.NewGuid();
        var request = new GetEventAgendaProjectionRequest { EventId = eventId };

        var parentEvent = DataBuilder.EventWithStatus(EventStatusEnum.Draft).Generate();
        parentEvent.Id = eventId;
        parentEvent.VisibilityTypeId = (int)VisibilityTypeEnum.Public;
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.IsPubliclyEligibleAsync(parentEvent.TenantId, eventId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(request, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _eventDayRepository.DidNotReceive().GetByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventSessionRepository.DidNotReceive().GetPublicSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventAgendaItemRepository.DidNotReceive().GetPublicByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithSessionsAndAgendaItems_GroupsByLocalDate()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventAgendaProjectionRequest { EventId = eventId };
        var localDate = new DateOnly(2026, 7, 15);

        var parentEvent = DataBuilder.EventWithStatus(EventStatusEnum.Published).Generate();
        parentEvent.Id = eventId;
        parentEvent.Title = "Multi-day Conference";
        parentEvent.Timezone = "Europe/Brussels";
        parentEvent.VisibilityTypeId = (int)VisibilityTypeEnum.Public;
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.IsPubliclyEligibleAsync(parentEvent.TenantId, eventId, Arg.Any<CancellationToken>()).Returns(true);

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
        calculator.Project(session.StartTime.Value, session.EndTime.Value, "Europe/Brussels")
            .Returns(new LocalScheduleProjection(localDate, localDate, new TimeOnly(10, 0), new TimeOnly(12, 0), 600, 720));
        session.ReprojectLocalTimes("Europe/Brussels", calculator);
        _eventSessionRepository.GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventSession> { session });

        var agendaItem = DataBuilder.EventAgendaItem.Generate();
        agendaItem.EventId = eventId;
        agendaItem.StartTime = new DateTimeOffset(2026, 7, 15, 7, 0, 0, TimeSpan.Zero);
        agendaItem.EndTime = new DateTimeOffset(2026, 7, 15, 7, 30, 0, TimeSpan.Zero);
        agendaItem.SortOrder = 0;
        calculator.Project(agendaItem.StartTime, agendaItem.EndTime, "Europe/Brussels")
            .Returns(new LocalScheduleProjection(localDate, localDate, new TimeOnly(9, 0), new TimeOnly(9, 30), 540, 570));
        agendaItem.ReprojectLocalTimes("Europe/Brussels", calculator);
        _eventAgendaItemRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>())
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
    [Category("EventLocationPrivacy")]
    public async Task HandlePublicAgendaDoesNotExposePhysicalLocationOrRoomIds()
    {
        var eventId = Guid.NewGuid();
        var localDate = new DateOnly(2026, 7, 15);
        var tenantId = Guid.NewGuid();
        var parentEvent = DataBuilder.EventWithStatus(EventStatusEnum.Published).Generate();
        parentEvent.Id = eventId;
        parentEvent.VisibilityTypeId = (int)VisibilityTypeEnum.Public;
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.IsPubliclyEligibleAsync(parentEvent.TenantId, eventId, Arg.Any<CancellationToken>()).Returns(true);
        _eventDayRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);

        var calculator = Substitute.For<IEventScheduleProjectionCalculator>();
        var session = DataBuilder.EventSession.Generate();
        session.TenantId = tenantId;
        session.EventId = eventId;
        var eventLocation = EventLocation.CreatePhysical(
            tenantId,
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);
        session.AssignEventLocation(eventLocation);
        session.RoomId = Guid.NewGuid();
        session.StartTime = new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero);
        session.EndTime = session.StartTime.Value.AddHours(1);
        calculator.Project(session.StartTime.Value, session.EndTime.Value, Arg.Any<string>())
            .Returns(new LocalScheduleProjection(localDate, localDate, new TimeOnly(10, 0), new TimeOnly(11, 0), 600, 660));
        session.ReprojectLocalTimes("Europe/Brussels", calculator);

        _eventSessionRepository.GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([session]);
        _eventAgendaItemRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);
        _disclosureService.ResolveManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocationDisclosureRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, EventLocationDisclosureResult>
            {
                [eventLocation.Id] = EventLocationDisclosureResult.Public(
                    eventLocation.Id,
                    EventLocationDisclosureState.Available,
                    new EventLocationDisclosureValues(VenueName: "Conference Hall"))
            });

        var result = await _handler.Handle(
            new GetEventAgendaProjectionRequest { EventId = eventId },
            CancellationToken.None);
        var entry = result!.Days.Single().Entries.Single();

        await Assert.That(entry.LocationId is null && entry.RoomId is null).IsTrue();
        await Assert.That(entry.EventLocation?.Fields?.VenueName).IsEqualTo("Conference Hall");
        await _disclosureService.Received(1).ResolveManyAsync(
            Arg.Is<IReadOnlyCollection<EventLocationDisclosureRequest>>(requests =>
                requests.Count == 1 &&
                requests.Single() == new EventLocationDisclosureRequest(
                    tenantId,
                    eventId,
                    eventLocation.Id,
                    session.RoomId,
                    RequesterUserId: null,
                    EventLocationDisclosurePurpose.Public)),
            CancellationToken.None);
    }

    [Test]
    public async Task Handle_WithNoSessionsOrAgendaItems_ReturnsEmptyDays()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventAgendaProjectionRequest { EventId = eventId };

        var parentEvent = DataBuilder.EventWithStatus(EventStatusEnum.Published).Generate();
        parentEvent.Id = eventId;
        parentEvent.VisibilityTypeId = (int)VisibilityTypeEnum.Public;
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.IsPubliclyEligibleAsync(parentEvent.TenantId, eventId, Arg.Any<CancellationToken>()).Returns(true);

        _eventDayRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventDay>());
        _eventSessionRepository.GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventSession>());
        _eventAgendaItemRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new List<EventAgendaItem>());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Days).IsNotNull();
        await Assert.That(result.Days.Count).IsEqualTo(0);
    }
}
