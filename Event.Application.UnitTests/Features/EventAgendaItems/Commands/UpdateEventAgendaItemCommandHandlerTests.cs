using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.Features.EventAgendaItems.Handlers.Commands;
using Explore.Application.Features.EventAgendaItems.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Interfaces;
using Explore.Domain.Services.Scheduling;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventAgendaItems.Commands;

public class UpdateEventAgendaItemCommandHandlerTests
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IMapper _mapper;
    private readonly UpdateEventAgendaItemCommandHandler _handler;

    public UpdateEventAgendaItemCommandHandlerTests()
    {
        _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _scheduleProjectionCalculator = new EventScheduleProjectionCalculator();
        _mapper = Substitute.For<IMapper>();

        _handler = new UpdateEventAgendaItemCommandHandler(
            _eventAgendaItemRepository,
            _eventRepository,
            _eventDayRepository,
            _scheduleProjectionCalculator,
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
        var command = new UpdateEventAgendaItemCommand
        {
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Id = agendaItemId,
                EventId = eventId,
                Title = "Updated Keynote",
                StartTime = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero),
                SortOrder = 2
            }
        };

        var existingItem = DataBuilder.EventAgendaItem.Generate();
        existingItem.Id = agendaItemId;
        existingItem.TenantId = tenantId;
        _eventAgendaItemRepository.GetById(agendaItemId).Returns(existingItem);

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        parentEvent.Timezone = "Europe/Brussels";
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.Exists(eventId).Returns(true);

        _eventDayRepository.FindByEventAndLocalDateAsync(eventId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((EventDay?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _eventAgendaItemRepository.Received(1).Update(Arg.Any<EventAgendaItem>());
    }

    [Test]
    public async Task Handle_WithNonExistentAgendaItem_ReturnsFailedResponse()
    {
        // Arrange
        var agendaItemId = Guid.NewGuid();
        var command = new UpdateEventAgendaItemCommand
        {
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Id = agendaItemId,
                EventId = Guid.NewGuid(),
                Title = "Ghost Item",
                StartTime = DateTimeOffset.Now.AddDays(1),
                EndTime = DateTimeOffset.Now.AddDays(1).AddHours(1),
                SortOrder = 1
            }
        };

        _eventAgendaItemRepository.GetById(agendaItemId).Returns((EventAgendaItem?)null);
        _eventRepository.Exists(command.EventAgendaItemDto.EventId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _eventAgendaItemRepository.DidNotReceive().Update(Arg.Any<EventAgendaItem>());
    }

    [Test]
    public async Task Handle_WithCrossTenantEvent_ReturnsFailedResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var agendaItemId = Guid.NewGuid();
        var command = new UpdateEventAgendaItemCommand
        {
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Id = agendaItemId,
                EventId = eventId,
                Title = "Cross-tenant",
                StartTime = DateTimeOffset.Now.AddDays(1),
                EndTime = DateTimeOffset.Now.AddDays(1).AddHours(1),
                SortOrder = 1
            }
        };

        var existingItem = DataBuilder.EventAgendaItem.Generate();
        existingItem.Id = agendaItemId;
        existingItem.TenantId = Guid.NewGuid(); // Different tenant
        _eventAgendaItemRepository.GetById(agendaItemId).Returns(existingItem);

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = Guid.NewGuid(); // Different tenant from item
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.Exists(eventId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _eventAgendaItemRepository.DidNotReceive().Update(Arg.Any<EventAgendaItem>());
    }
}
