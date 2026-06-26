// ABOUTME: Unit tests for public event session detail query handler mapping behavior.
// ABOUTME: Verifies public repository reads are mapped to nullable detail DTO responses.

using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Handlers.Queries;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventSessions.Queries;

public class GetEventSessionDetailsRequestHandlerTests
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IMapper _mapper;
    private readonly GetEventSessionDetailsRequestHandler _handler;

    public GetEventSessionDetailsRequestHandlerTests()
    {
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetEventSessionDetailsRequestHandler(_eventSessionRepository, _mapper);
    }

    [Test]
    public async Task Handle_WithExistingSession_ReturnsSessionDto()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new GetEventSessionDetailsRequest { Id = sessionId };

        var eventSession = DataBuilder.EventSession.Generate();
        eventSession.Id = sessionId;
        eventSession.Title = "Test Session";

        var expectedDto = new EventSessionDto
        {
            Id = sessionId,
            Title = "Test Session",
            EventTitle = string.Empty
        };

        _eventSessionRepository
            .GetPublicSessionWithDetailsAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(eventSession);
        _mapper.Map<EventSessionDto>(eventSession).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(sessionId);
        await Assert.That(result.Title).IsEqualTo("Test Session");
    }

    [Test]
    public async Task Handle_WithNonExistentSession_ReturnsNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new GetEventSessionDetailsRequest { Id = sessionId };

        _eventSessionRepository
            .GetPublicSessionWithDetailsAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns((EventSession?)null);
        _mapper.Map<EventSessionDto>(Arg.Any<EventSession?>()).Returns((EventSessionDto?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Handle_ReturnsSessionWithLocationDetails()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var request = new GetEventSessionDetailsRequest { Id = sessionId };

        var eventSession = DataBuilder.EventSession.Generate();
        eventSession.Id = sessionId;
        eventSession.LocationId = locationId;
        eventSession.Location = DataBuilder.Location.Generate();
        eventSession.Location.Id = locationId;
        eventSession.Location.FullName = "Test Location";

        var expectedDto = new EventSessionDto
        {
            Id = sessionId,
            LocationId = locationId,
            LocationFullName = "Test Location",
            EventTitle = string.Empty
        };

        _eventSessionRepository
            .GetPublicSessionWithDetailsAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(eventSession);
        _mapper.Map<EventSessionDto>(eventSession).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.LocationId).IsEqualTo(locationId);
        await Assert.That(result.LocationFullName).IsEqualTo("Test Location");
    }
}
