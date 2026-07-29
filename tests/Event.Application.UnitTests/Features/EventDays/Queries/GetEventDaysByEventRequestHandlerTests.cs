// ABOUTME: Unit tests for the public EventDay collection query handler.
// ABOUTME: Verifies central parent eligibility gates the child-day repository read.

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

public class GetEventDaysByEventRequestHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IMapper _mapper;
    private readonly GetEventDaysByEventRequestHandler _handler;

    public GetEventDaysByEventRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetEventDaysByEventRequestHandler(_eventRepository, _eventDayRepository, _mapper);
    }

    [Test]
    public async Task Handle_WithExistingEventDays_ReturnsMappedList()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventDaysByEventRequest { EventId = eventId };
        ConfigurePublicEvent(eventId);

        var eventDays = new List<EventDay>
        {
            DataBuilder.EventDay.Generate(),
            DataBuilder.EventDay.Generate()
        };

        var expectedDtos = new List<EventDayListDto>
        {
            new() { Id = eventDays[0].Id },
            new() { Id = eventDays[1].Id }
        };

        _eventDayRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDays);
        _mapper.Map<List<EventDayListDto>>(eventDays).Returns(expectedDtos);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Handle_WithNoEventDays_ReturnsEmptyList()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventDaysByEventRequest { EventId = eventId };
        ConfigurePublicEvent(eventId);

        _eventDayRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns(new List<EventDay>());
        _mapper.Map<List<EventDayListDto>>(Arg.Any<List<EventDay>>()).Returns(new List<EventDayListDto>());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_WhenParentEventIsNotCentrallyPubliclyEligible_ReturnsEmptyWithoutReadingDays()
    {
        var eventId = Guid.NewGuid();
        var request = new GetEventDaysByEventRequest { EventId = eventId };
        var parentEvent = ConfigurePublicEvent(eventId);
        _eventRepository.IsPubliclyEligibleAsync(parentEvent.TenantId, eventId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(request, CancellationToken.None);

        await Assert.That(result).IsEmpty();
        await _eventDayRepository.DidNotReceive().GetByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _mapper.DidNotReceive().Map<List<EventDayListDto>>(Arg.Any<List<EventDay>>());
    }

    [Test]
    public async Task HandleManaged_ReturnsDraftEventDaysWithoutPublicEligibilityProbe()
    {
        var eventId = Guid.NewGuid();
        var eventDays = new List<EventDay> { DataBuilder.EventDay.Generate() };
        var expected = new List<EventDayListDto> { new() { Id = eventDays[0].Id } };
        _eventDayRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDays);
        _mapper.Map<List<EventDayListDto>>(eventDays).Returns(expected);

        var result = await new GetManagedEventDaysByEventRequestHandler(
                _eventDayRepository,
                _mapper)
            .Handle(
            new GetManagedEventDaysByEventRequest { EventId = eventId },
            CancellationToken.None);

        await Assert.That(result).IsEquivalentTo(expected);
        await _eventRepository.DidNotReceive().IsPubliclyEligibleAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    private Explore.Domain.Event ConfigurePublicEvent(Guid eventId)
    {
        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = Guid.NewGuid();
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.IsPubliclyEligibleAsync(parentEvent.TenantId, eventId, Arg.Any<CancellationToken>()).Returns(true);
        return parentEvent;
    }
}
