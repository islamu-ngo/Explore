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

public class CreateEventDayCommandHandlerTests
{
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly CreateEventDayCommandHandler _handler;

    public CreateEventDayCommandHandlerTests()
    {
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _mapper = Substitute.For<IMapper>();

        _eventDayRepository.Create(Arg.Any<EventDay>())
            .Returns(callInfo => callInfo.Arg<EventDay>());

        _handler = new CreateEventDayCommandHandler(
            _eventDayRepository,
            _eventRepository,
            Substitute.For<IStorageObjectRepository>(),
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
        var command = new CreateEventDayCommand
        {
            EventDayDto = new CreateEventDayDto
            {
                EventId = eventId,
                LocalDate = new DateOnly(2026, 7, 15),
                Label = "Day 1",
                IsPublished = true,
                SortOrder = 1,
                AllowsDayScopeRegistration = false
            }
        };

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.Exists(eventId).Returns(true);

        var eventDay = new EventDay { Id = eventDayId, Event = null!, Tenant = null! };
        _mapper.Map<EventDay>(command.EventDayDto).Returns(eventDay);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(eventDayId);
        await _eventDayRepository.Received(1).Create(Arg.Any<EventDay>());
    }

    [Test]
    public async Task Handle_WithValidRequest_SetsTenantIdFromParentEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateEventDayCommand
        {
            EventDayDto = new CreateEventDayDto
            {
                EventId = eventId,
                LocalDate = new DateOnly(2026, 7, 15),
                Label = "Day 1",
                IsPublished = true,
                SortOrder = 1,
                AllowsDayScopeRegistration = false
            }
        };

        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.Exists(eventId).Returns(true);

        EventDay? capturedDay = null;
        var eventDay = new EventDay { Id = Guid.NewGuid(), Event = null!, Tenant = null! };
        _mapper.Map<EventDay>(command.EventDayDto).Returns(eventDay);
        _eventDayRepository.When(r => r.Create(Arg.Any<EventDay>()))
            .Do(callInfo => capturedDay = callInfo.Arg<EventDay>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(capturedDay).IsNotNull();
        await Assert.That(capturedDay!.TenantId).IsEqualTo(tenantId);
    }

    [Test]
    public async Task Handle_WithNonExistentEvent_ReturnsFailedResponse()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();
        var command = new CreateEventDayCommand
        {
            EventDayDto = new CreateEventDayDto
            {
                EventId = nonExistentEventId,
                LocalDate = new DateOnly(2026, 7, 15),
                Label = "Day 1",
                IsPublished = true,
                SortOrder = 1,
                AllowsDayScopeRegistration = false
            }
        };

        _eventRepository.GetById(nonExistentEventId).Returns((Explore.Domain.Event?)null);
        _eventRepository.Exists(nonExistentEventId).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _eventDayRepository.DidNotReceive().Create(Arg.Any<EventDay>());
    }
}
