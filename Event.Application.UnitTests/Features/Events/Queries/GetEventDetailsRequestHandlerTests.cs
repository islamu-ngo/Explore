using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Queries;

public class GetEventDetailsRequestHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly GetEventDetailsRequestHandler _handler;

    public GetEventDetailsRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new GetEventDetailsRequestHandler(_eventRepository, _mapper);
    }

    [Test]
    public async Task Handle_WithValidId_ReturnsEventDto()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventDetailsRequest { Id = eventId };
        
        var eventEntity = new Explore.Domain.Event { Id = eventId, Title = "Test Event" };
        var eventDto = new EventDto { Id = eventId, Title = "Test Event" };

        _eventRepository.GetEventWithDetails(eventId).Returns(eventEntity);
        _mapper.Map<EventDto>(eventEntity).Returns(eventDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(eventId);
        await Assert.That(result.Title).IsEqualTo("Test Event");
        
        await _eventRepository.Received(1).GetEventWithDetails(eventId);
    }

    [Test]
    public async Task Handle_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new GetEventDetailsRequest { Id = eventId };

        _eventRepository.GetEventWithDetails(eventId).Returns((Explore.Domain.Event)null!);
        _mapper.Map<EventDto>(null).Returns((EventDto)null!);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
        await _eventRepository.Received(1).GetEventWithDetails(eventId);
    }
}
