using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventDay;
using Explore.Application.Features.EventDays.Handlers.Commands;
using Explore.Application.Features.EventDays.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventDays.Commands;

public class UpdateEventDayCommandHandlerTests
{
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly UpdateEventDayCommandHandler _handler;

    public UpdateEventDayCommandHandlerTests()
    {
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new UpdateEventDayCommandHandler(
            _eventDayRepository,
            _eventRepository,
            _mapper
        );
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDayId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new UpdateEventDayCommand
        {
            EventDayDto = new UpdateEventDayDto
            {
                Id = eventDayId,
                EventId = eventId,
                LocalDate = new DateOnly(2026, 7, 16),
                Label = "Day 2 Updated",
                IsPublished = true,
                SortOrder = 2,
                AllowsDayScopeRegistration = true
            }
        };

        var existingDay = DataBuilder.EventDay.Generate();
        existingDay.Id = eventDayId;
        existingDay.TenantId = tenantId;
        _eventDayRepository.GetById(eventDayId).Returns(existingDay);

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.Exists(eventId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _eventDayRepository.Received(1).Update(Arg.Any<EventDay>());
    }

    [Test]
    public async Task Handle_WithNonExistentEventDay_ReturnsFailedResponse()
    {
        // Arrange
        var eventDayId = Guid.NewGuid();
        var command = new UpdateEventDayCommand
        {
            EventDayDto = new UpdateEventDayDto
            {
                Id = eventDayId,
                EventId = Guid.NewGuid(),
                LocalDate = new DateOnly(2026, 7, 16),
                Label = "Day 2",
                IsPublished = true,
                SortOrder = 1,
                AllowsDayScopeRegistration = false
            }
        };

        _eventDayRepository.GetById(eventDayId).Returns((EventDay?)null);
        _eventRepository.Exists(command.EventDayDto.EventId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _eventDayRepository.DidNotReceive().Update(Arg.Any<EventDay>());
    }

    [Test]
    public async Task Handle_WithCrossTenantEvent_ReturnsFailedResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDayId = Guid.NewGuid();
        var command = new UpdateEventDayCommand
        {
            EventDayDto = new UpdateEventDayDto
            {
                Id = eventDayId,
                EventId = eventId,
                LocalDate = new DateOnly(2026, 7, 16),
                Label = "Day 2",
                IsPublished = true,
                SortOrder = 1,
                AllowsDayScopeRegistration = false
            }
        };

        var existingDay = DataBuilder.EventDay.Generate();
        existingDay.Id = eventDayId;
        existingDay.TenantId = Guid.NewGuid(); // Different tenant
        _eventDayRepository.GetById(eventDayId).Returns(existingDay);

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = Guid.NewGuid(); // Different tenant from day
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.Exists(eventId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _eventDayRepository.DidNotReceive().Update(Arg.Any<EventDay>());
    }
}
