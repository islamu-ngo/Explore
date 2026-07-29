// ABOUTME: Unit tests for the public EventDay detail query handler.
// ABOUTME: Verifies parent eligibility is checked before mapping a child day.

using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventDay;
using Explore.Application.Features.EventDays.Handlers.Queries;
using Explore.Application.Features.EventDays.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventDays.Queries;

public class GetEventDayDetailRequestHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IMapper _mapper;
    private readonly GetEventDayDetailRequestHandler _handler;

    public GetEventDayDetailRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetEventDayDetailRequestHandler(_eventRepository, _eventDayRepository, _mapper);
    }

    [Test]
    public async Task Handle_WithExistingEventDay_ReturnsDto()
    {
        // Arrange
        var eventDayId = Guid.NewGuid();
        var request = new GetEventDayDetailRequest { Id = eventDayId };

        var eventDay = DataBuilder.EventDay.Generate();
        eventDay.Id = eventDayId;
        eventDay.Label = "Day 1";
        eventDay.EventId = Guid.NewGuid();
        eventDay.TenantId = Guid.NewGuid();
        _eventRepository.IsPubliclyEligibleAsync(eventDay.TenantId, eventDay.EventId, Arg.Any<CancellationToken>()).Returns(true);

        var expectedDto = new EventDayDto
        {
            Id = eventDayId,
            Label = "Day 1"
        };

        _eventDayRepository.GetById(eventDayId).Returns(eventDay);
        _mapper.Map<EventDayDto>(eventDay).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(eventDayId);
        await Assert.That(result.Label).IsEqualTo("Day 1");
    }

    [Test]
    public async Task Handle_WithNonExistentEventDay_ReturnsNull()
    {
        // Arrange
        var eventDayId = Guid.NewGuid();
        var request = new GetEventDayDetailRequest { Id = eventDayId };

        _eventDayRepository.GetById(eventDayId).Returns((EventDay?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Handle_WhenParentEventIsNotCentrallyPubliclyEligible_ReturnsNullWithoutMappingDay()
    {
        var eventDayId = Guid.NewGuid();
        var eventDay = DataBuilder.EventDay.Generate();
        eventDay.Id = eventDayId;
        eventDay.EventId = Guid.NewGuid();
        eventDay.TenantId = Guid.NewGuid();
        _eventDayRepository.GetById(eventDayId).Returns(eventDay);
        _eventRepository.IsPubliclyEligibleAsync(eventDay.TenantId, eventDay.EventId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new GetEventDayDetailRequest { Id = eventDayId },
            CancellationToken.None);

        await Assert.That(result).IsNull();
        _mapper.DidNotReceive().Map<EventDayDto>(eventDay);
    }
}
