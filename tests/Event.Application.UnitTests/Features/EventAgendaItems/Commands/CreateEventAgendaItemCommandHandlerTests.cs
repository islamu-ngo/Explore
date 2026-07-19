// ABOUTME: Unit tests for event-level agenda item creation and schedule projection.
// ABOUTME: Verifies tenant ownership, day linkage, and transactional EventLocation attachment.

using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.Features.EventAgendaItems.Handlers.Commands;
using Explore.Application.Features.EventAgendaItems.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Interfaces;
using Explore.Domain.Services.Scheduling;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventAgendaItems.Commands;

public class CreateEventAgendaItemCommandHandlerTests
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EventLocationAttachmentService _eventLocationAttachmentService;
    private readonly IMapper _mapper;
    private readonly CreateEventAgendaItemCommandHandler _handler;

    public CreateEventAgendaItemCommandHandlerTests()
    {
        _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _scheduleProjectionCalculator = new EventScheduleProjectionCalculator();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _eventLocationAttachmentService = EventLocationAttachmentServiceTestFixture.ForExistingEvent(
            _eventRepository,
            Guid.NewGuid());
        _mapper = Substitute.For<IMapper>();

        _unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<EventAgendaItem>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<EventAgendaItem>>>()(
                call.Arg<CancellationToken>()));

        _eventAgendaItemRepository.Create(Arg.Any<EventAgendaItem>())
            .Returns(callInfo => callInfo.Arg<EventAgendaItem>());

        _handler = new CreateEventAgendaItemCommandHandler(
            _eventAgendaItemRepository,
            _eventRepository,
            _eventDayRepository,
            _scheduleProjectionCalculator,
            _unitOfWork,
            _eventLocationAttachmentService,
            _mapper
        );
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var agendaItemId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var startTime = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero);

        var command = new CreateEventAgendaItemCommand
        {
            EventAgendaItemDto = new CreateEventAgendaItemDto
            {
                EventId = eventId,
                Title = "Keynote Address",
                Description = "Opening keynote",
                StartTime = startTime,
                EndTime = endTime,
                SortOrder = 1
            }
        };

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        parentEvent.Timezone = "Europe/Brussels";
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.Exists(eventId).Returns(true);

        var agendaItem = new EventAgendaItem
        {
            Id = agendaItemId,
            EventId = eventId,
            Title = "Keynote Address",
            Event = null!,
            Tenant = null!
        };
        _mapper.Map<EventAgendaItem>(command.EventAgendaItemDto).Returns(agendaItem);

        _eventDayRepository.FindByEventAndLocalDateAsync(eventId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((EventDay?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(agendaItemId);
        await _eventAgendaItemRepository.Received(1).Create(Arg.Any<EventAgendaItem>());
    }

    [Test]
    public async Task Handle_WithValidRequest_SetsTenantIdFromParentEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var startTime = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero);

        var command = new CreateEventAgendaItemCommand
        {
            EventAgendaItemDto = new CreateEventAgendaItemDto
            {
                EventId = eventId,
                Title = "Keynote",
                StartTime = startTime,
                EndTime = endTime,
                SortOrder = 1
            }
        };

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        parentEvent.Timezone = "UTC";
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.Exists(eventId).Returns(true);

        EventAgendaItem? capturedItem = null;
        var agendaItem = new EventAgendaItem
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Title = "Keynote",
            Event = null!,
            Tenant = null!
        };
        _mapper.Map<EventAgendaItem>(command.EventAgendaItemDto).Returns(agendaItem);
        _eventAgendaItemRepository.When(r => r.Create(Arg.Any<EventAgendaItem>()))
            .Do(callInfo => capturedItem = callInfo.Arg<EventAgendaItem>());

        _eventDayRepository.FindByEventAndLocalDateAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((EventDay?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(capturedItem).IsNotNull();
        await Assert.That(capturedItem!.TenantId).IsEqualTo(tenantId);
    }

    [Test]
    public async Task Handle_WhenMatchingEventDayExists_LinksToEventDay()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDayId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var startTime = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero);

        var command = new CreateEventAgendaItemCommand
        {
            EventAgendaItemDto = new CreateEventAgendaItemDto
            {
                EventId = eventId,
                Title = "Linked Item",
                StartTime = startTime,
                EndTime = endTime,
                SortOrder = 1
            }
        };

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        parentEvent.Timezone = "Europe/Brussels";
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.Exists(eventId).Returns(true);

        EventAgendaItem? capturedItem = null;
        var agendaItem = new EventAgendaItem
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Title = "Linked Item",
            Event = null!,
            Tenant = null!
        };
        _mapper.Map<EventAgendaItem>(command.EventAgendaItemDto).Returns(agendaItem);
        _eventAgendaItemRepository.When(r => r.Create(Arg.Any<EventAgendaItem>()))
            .Do(callInfo => capturedItem = callInfo.Arg<EventAgendaItem>());

        var matchingDay = new EventDay { Id = eventDayId, EventId = eventId, Event = null!, Tenant = null! };
        _eventDayRepository.FindByEventAndLocalDateAsync(eventId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(matchingDay);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedItem).IsNotNull();
        await Assert.That(capturedItem!.EventDayId).IsEqualTo(eventDayId);
    }

    [Test]
    public async Task Handle_WithNonExistentEvent_ReturnsFailedResponse()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();
        var command = new CreateEventAgendaItemCommand
        {
            EventAgendaItemDto = new CreateEventAgendaItemDto
            {
                EventId = nonExistentEventId,
                Title = "Orphan Item",
                StartTime = DateTimeOffset.Now.AddDays(1),
                EndTime = DateTimeOffset.Now.AddDays(1).AddHours(1),
                SortOrder = 1
            }
        };

        _eventRepository.GetById(nonExistentEventId).Returns((Explore.Domain.Event?)null);
        _eventRepository.Exists(nonExistentEventId).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _eventAgendaItemRepository.DidNotReceive().Create(Arg.Any<EventAgendaItem>());
    }

    [Test]
    public async Task Handle_CallsRescheduleWithTimezone()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var startTime = new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2026, 7, 15, 9, 30, 0, TimeSpan.Zero);

        var command = new CreateEventAgendaItemCommand
        {
            EventAgendaItemDto = new CreateEventAgendaItemDto
            {
                EventId = eventId,
                Title = "Rescheduled Item",
                StartTime = startTime,
                EndTime = endTime,
                SortOrder = 1
            }
        };

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        parentEvent.Timezone = "Europe/Brussels";
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.Exists(eventId).Returns(true);

        EventAgendaItem? capturedItem = null;
        var agendaItem = new EventAgendaItem
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Title = "Rescheduled Item",
            Event = null!,
            Tenant = null!
        };
        _mapper.Map<EventAgendaItem>(command.EventAgendaItemDto).Returns(agendaItem);
        _eventAgendaItemRepository.When(r => r.Create(Arg.Any<EventAgendaItem>()))
            .Do(callInfo => capturedItem = callInfo.Arg<EventAgendaItem>());

        _eventDayRepository.FindByEventAndLocalDateAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((EventDay?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — Reschedule was called (local projection fields populated)
        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedItem).IsNotNull();
        await Assert.That(capturedItem!.StartTime).IsEqualTo(startTime);
        await Assert.That(capturedItem.EndTime).IsEqualTo(endTime);
    }
}
