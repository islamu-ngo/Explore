// ABOUTME: Unit tests for CreateEventSessionCommandHandler validation and schedule projection behavior.
// ABOUTME: Verifies event-session creation, day linking, room guards, and Islamic aspect invariants.

using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Features.EventSessions.Handlers.Commands;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
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
    private readonly IEventSessionKindRepository _eventSessionKindRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventSessionTemplateRepository _eventSessionTemplateRepository;
    private readonly IEventSessionCustomPropertyRepository _eventSessionCustomPropertyRepository;
    private readonly IEventSessionCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly IEventSessionTemplateInstantiationService _instantiationService;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EventLocationAttachmentService _eventLocationAttachmentService;
    private readonly IMapper _mapper;
    private readonly CreateEventSessionCommandHandler _handler;

    public CreateEventSessionCommandHandlerTests()
    {
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _locationRepository = Substitute.For<ILocationRepository>();
        _registrationModeRepository = Substitute.For<IRegistrationModeRepository>();
        _eventSessionKindRepository = Substitute.For<IEventSessionKindRepository>();
        _eventSessionIslamicAspectRepository = Substitute.For<IEventSessionIslamicAspectRepository>();
        _eventSessionTemplateRepository = Substitute.For<IEventSessionTemplateRepository>();
        _eventSessionCustomPropertyRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        _projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        _instantiationService = Substitute.For<IEventSessionTemplateInstantiationService>();
        _scheduleProjectionCalculator = new EventScheduleProjectionCalculator();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _eventLocationAttachmentService = EventLocationAttachmentServiceTestFixture.ForExistingEvent(
            _eventRepository,
            Guid.NewGuid());
        _mapper = Substitute.For<IMapper>();
        _unitOfWork
            .ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<EventSession>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<EventSession>>>()(
                call.Arg<CancellationToken>()));

        _handler = new CreateEventSessionCommandHandler(
            _eventSessionRepository,
            _eventRepository,
            _locationRepository,
            _registrationModeRepository,
            _eventSessionKindRepository,
            _eventSessionIslamicAspectRepository,
            _eventSessionTemplateRepository,
            _eventSessionCustomPropertyRepository,
            _projectionUpdater,
            _instantiationService,
            _scheduleProjectionCalculator,
            _eventDayRepository,
            _storageObjectRepository,
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

        // Mock event existence validation
        var existingEvent = DataBuilder.Event.Generate();
        existingEvent.Id = eventId;
        existingEvent.TenantId = Guid.NewGuid();
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        // Mock session creation
        var eventSession = new EventSession { Id = sessionId, EventId = eventId, Event = null!, Tenant = null! };
        _mapper.Map<EventSession>(command.EventSessionDto).Returns(eventSession);
        _eventSessionRepository
            .CreateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>())
            .Returns(eventSession);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(sessionId);
        await _eventSessionRepository.Received(1)
            .CreateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>());
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
        await Assert.That(result.IsSuccess).IsFalse();
        await _eventSessionRepository.DidNotReceive().Create(Arg.Any<EventSession>());
    }

    [Test]
    public async Task Handle_WhenFeaturedImageIsCrossTenant_RejectsBeforeMappingOrCreation()
    {
        var eventId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        _eventRepository.Exists(eventId).Returns(true);
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _storageObjectRepository.GetById(imageId).Returns(new StorageObject
        {
            Id = imageId,
            TenantId = Guid.NewGuid(),
            Tenant = null!,
            FileType = null!,
            Uri = "storage://session.png",
            Provider = "local",
            FullName = "session.png",
            SafeDisplayName = "session.png",
            Extension = "png",
            ContentType = "image/png",
            Purpose = StorageObjectPurposes.EventImage,
            Visibility = StorageObjectVisibilities.PublicImage,
            LifecycleState = StorageObjectLifecycleStates.Active
        });
        var command = new CreateEventSessionCommand
        {
            EventSessionDto = new CreateEventSessionDto
            {
                EventId = eventId,
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                Title = "Unsafe image",
                FeaturedImageId = imageId
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        _mapper.DidNotReceiveWithAnyArgs().Map<EventSession>(default!);
        await _eventSessionRepository.DidNotReceive()
            .CreateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<EventSession>>>(), default);
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
        existingEvent.TenantId = Guid.NewGuid();
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await _eventSessionRepository.DidNotReceive().Create(Arg.Any<EventSession>());
    }

    [Test]
    public async Task Handle_WithFixedIslamicAspectPrayerFields_ReturnsValidationError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var command = new CreateEventSessionCommand
        {
            EventSessionDto = new CreateEventSessionDto
            {
                EventId = eventId,
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
                Title = "Fixed Session",
                IslamicAspect = new EventSessionIslamicAspectDto
                {
                    StartTimeType = SessionStartTimeType.Fixed,
                    ReferencePrayer = PrayerTime.Dhuhr,
                    OffsetMinutes = 0
                }
            }
        };

        _eventRepository.Exists(eventId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).Contains(EventSessionIslamicAspectValidationRules.SchedulingStateMessage);
        await _eventSessionRepository.DidNotReceive()
            .CreateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithRelativeIslamicAspectOffsetOutOfRange_ReturnsValidationError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var command = new CreateEventSessionCommand
        {
            EventSessionDto = new CreateEventSessionDto
            {
                EventId = eventId,
                LocationId = locationId,
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
                Title = "Relative Session",
                IslamicAspect = new EventSessionIslamicAspectDto
                {
                    StartTimeType = SessionStartTimeType.RelativeToPrayer,
                    ReferencePrayer = PrayerTime.Asr,
                    OffsetMinutes = 181
                }
            }
        };

        _eventRepository.Exists(eventId).Returns(true);
        _locationRepository.Exists(locationId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).Contains(EventSessionIslamicAspectValidationRules.OffsetRangeMessage);
        await _eventSessionRepository.DidNotReceive()
            .CreateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithValidLocationId_AssociatesLocation()
    {
        // Arrange
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

        // Mock event and location existence
        var existingEvent = DataBuilder.Event.Generate();
        existingEvent.Id = eventId;
        existingEvent.TenantId = Guid.NewGuid();
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        var location = DataBuilder.Location.Generate();
        location.Id = locationId;
        _locationRepository.GetById(locationId).Returns(location);
        _locationRepository.Exists(locationId).Returns(true);

        // Mock session creation
        var eventSession = new EventSession
        {
            Id = sessionId,
            EventId = eventId,
            LocationId = locationId,
            Event = null!,
            Tenant = null!
        };
        _mapper.Map<EventSession>(command.EventSessionDto).Returns(eventSession);
        _eventSessionRepository
            .CreateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>())
            .Returns(eventSession);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _eventSessionRepository.Received(1)
            .CreateWithRoomOverlapGuardAsync(
                Arg.Is<EventSession>(s => s.LocationId == locationId),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenMatchingEventDayExists_LinksSessionToEventDay()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var eventDayId = Guid.NewGuid();
        var timezone = "Europe/Brussels";
        var startUtc = new DateTimeOffset(2026, 6, 15, 8, 0, 0, TimeSpan.Zero);
        var endUtc = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var expectedLocalDate = new DateOnly(2026, 6, 15);

        var command = new CreateEventSessionCommand
        {
            EventSessionDto = new CreateEventSessionDto
            {
                EventId = eventId,
                StartTime = startUtc,
                EndTime = endUtc,
                Title = "Day-linked Session"
            }
        };

        var existingEvent = DataBuilder.Event.Generate();
        existingEvent.Id = eventId;
        existingEvent.TenantId = Guid.NewGuid();
        existingEvent.Timezone = timezone;
        existingEvent.EventTimeZoneId = timezone;
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        var matchingDay = new EventDay
        {
            Id = eventDayId,
            EventId = eventId,
            LocalDate = expectedLocalDate,
            Event = null!,
            Tenant = null!
        };
        _eventDayRepository.FindByEventAndLocalDateAsync(eventId, expectedLocalDate, Arg.Any<CancellationToken>())
            .Returns(matchingDay);

        EventSession? capturedSession = null;
        var eventSession = new EventSession { Id = sessionId, EventId = eventId, Event = null!, Tenant = null! };
        _mapper.Map<EventSession>(command.EventSessionDto).Returns(eventSession);
        _eventSessionRepository
            .CreateWithRoomOverlapGuardAsync(Arg.Do<EventSession>(s => capturedSession = s), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<EventSession>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(capturedSession).IsNotNull();
        await Assert.That(capturedSession!.EventDayId).IsEqualTo(eventDayId);
    }

    [Test]
    public async Task Handle_WhenNoMatchingEventDayExists_SetsEventDayIdToNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var timezone = "Europe/Brussels";
        var startUtc = new DateTimeOffset(2026, 6, 15, 8, 0, 0, TimeSpan.Zero);
        var endUtc = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

        var command = new CreateEventSessionCommand
        {
            EventSessionDto = new CreateEventSessionDto
            {
                EventId = eventId,
                StartTime = startUtc,
                EndTime = endUtc,
                Title = "Orphan Session"
            }
        };

        var existingEvent = DataBuilder.Event.Generate();
        existingEvent.Id = eventId;
        existingEvent.TenantId = Guid.NewGuid();
        existingEvent.Timezone = timezone;
        existingEvent.EventTimeZoneId = timezone;
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        _eventDayRepository.FindByEventAndLocalDateAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((EventDay?)null);

        EventSession? capturedSession = null;
        var eventSession = new EventSession { Id = sessionId, EventId = eventId, Event = null!, Tenant = null! };
        _mapper.Map<EventSession>(command.EventSessionDto).Returns(eventSession);
        _eventSessionRepository
            .CreateWithRoomOverlapGuardAsync(Arg.Do<EventSession>(s => capturedSession = s), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<EventSession>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(capturedSession).IsNotNull();
        await Assert.That(capturedSession!.EventDayId).IsNull();
    }
}
