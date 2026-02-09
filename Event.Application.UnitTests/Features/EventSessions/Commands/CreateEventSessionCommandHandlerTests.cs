using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Handlers.Commands;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventSessions.Commands;

public class CreateEventSessionCommandHandlerTests
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly CreateEventSessionCommandHandler _handler;

    public CreateEventSessionCommandHandlerTests()
    {
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _locationRepository = Substitute.For<ILocationRepository>();
        _registrationModeRepository = Substitute.For<IRegistrationModeRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _mapper = Substitute.For<IMapper>();

        _handler = new CreateEventSessionCommandHandler(
            _eventSessionRepository,
            _eventRepository,
            _locationRepository,
            _registrationModeRepository,
            _tenantContext,
            _mapper
        );
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new CreateEventSessionCommand
        {
            EventSessionDto = new CreateEventSessionDto
            {
                EventId = eventId,
                StartTime = DateTimeOffset.Now.AddDays(1),
                EndTime = DateTimeOffset.Now.AddDays(1).AddHours(2),
                Title = "Test Session",
                Description = "Test Description",
                MaxAudienceAttendees = 100
            }
        };

        _tenantContext.TenantId.Returns(tenantId);

        // Mock event existence validation
        var existingEvent = DataBuilder.Event.Generate();
        existingEvent.Id = eventId;
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        // Mock session creation
        var eventSession = new EventSession { Id = sessionId, Event = null!, Tenant = null! };
        _mapper.Map<EventSession>(command.EventSessionDto).Returns(eventSession);
        _eventSessionRepository.Create(Arg.Any<EventSession>()).Returns(eventSession);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(sessionId);
        await _eventSessionRepository.Received(1).Create(Arg.Any<EventSession>());
    }

    [Test]
    public async Task Handle_WithInvalidEventId_ReturnsFailedResponse()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();
        var command = new CreateEventSessionCommand
        {
            EventSessionDto = new CreateEventSessionDto
            {
                EventId = nonExistentEventId,
                StartTime = DateTimeOffset.Now.AddDays(1),
                EndTime = DateTimeOffset.Now.AddDays(1).AddHours(2),
                Title = "Test Session"
            }
        };

        // Mock event does not exist
        _eventRepository.GetById(nonExistentEventId).Returns((Explore.Domain.Event?)null);
        _eventRepository.Exists(nonExistentEventId).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _eventSessionRepository.DidNotReceive().Create(Arg.Any<EventSession>());
    }

    [Test]
    public async Task Handle_WithEndTimeBeforeStartTime_ReturnsValidationError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var command = new CreateEventSessionCommand
        {
            EventSessionDto = new CreateEventSessionDto
            {
                EventId = eventId,
                StartTime = DateTimeOffset.Now.AddDays(1).AddHours(2),
                EndTime = DateTimeOffset.Now.AddDays(1), // End before start
                Title = "Test Session"
            }
        };

        // Mock event exists
        var existingEvent = DataBuilder.Event.Generate();
        existingEvent.Id = eventId;
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _eventSessionRepository.DidNotReceive().Create(Arg.Any<EventSession>());
    }

    [Test]
    public async Task Handle_WithValidLocationId_AssociatesLocation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new CreateEventSessionCommand
        {
            EventSessionDto = new CreateEventSessionDto
            {
                EventId = eventId,
                LocationId = locationId,
                StartTime = DateTimeOffset.Now.AddDays(1),
                EndTime = DateTimeOffset.Now.AddDays(1).AddHours(2),
                Title = "Test Session"
            }
        };

        _tenantContext.TenantId.Returns(tenantId);

        // Mock event and location existence
        var existingEvent = DataBuilder.Event.Generate();
        existingEvent.Id = eventId;
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        var location = DataBuilder.Location.Generate();
        location.Id = locationId;
        _locationRepository.GetById(locationId).Returns(location);
        _locationRepository.Exists(locationId).Returns(true);

        // Mock session creation
        var eventSession = new EventSession { Id = sessionId, LocationId = locationId, Event = null!, Tenant = null! };
        _mapper.Map<EventSession>(command.EventSessionDto).Returns(eventSession);
        _eventSessionRepository.Create(Arg.Any<EventSession>()).Returns(eventSession);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(s => s.LocationId == locationId));
    }
}
