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
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IMapper _mapper;
    private readonly GetEventDaysByEventRequestHandler _handler;

    public GetEventDaysByEventRequestHandlerTests()
    {
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetEventDaysByEventRequestHandler(_eventDayRepository, _mapper);
    }

    [Test]
    public async Task Handle_WithExistingEventDays_ReturnsMappedList()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventDaysByEventRequest { EventId = eventId };

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

        _eventDayRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns(new List<EventDay>());
        _mapper.Map<List<EventDayListDto>>(Arg.Any<List<EventDay>>()).Returns(new List<EventDayListDto>());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
    }
}
