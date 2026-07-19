// ABOUTME: Unit tests for UpdateEventSessionCommandHandler validation and schedule re-linking behavior.
// ABOUTME: Verifies event-session updates, event-day movement, and Islamic aspect invariants.

using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Features.EventSessions.Handlers.Commands;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventSessions.Commands;

public class UpdateEventSessionCommandHandlerTests
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly IEventSessionKindRepository _eventSessionKindRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EventLocationAttachmentService _eventLocationAttachmentService;
    private readonly HybridCache _cache;
    private readonly UpdateEventSessionCommandHandler _handler;

    public UpdateEventSessionCommandHandlerTests()
    {
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _locationRepository = Substitute.For<ILocationRepository>();
        _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
        _registrationModeRepository = Substitute.For<IRegistrationModeRepository>();
        _eventSessionKindRepository = Substitute.For<IEventSessionKindRepository>();
        _eventSessionIslamicAspectRepository = Substitute.For<IEventSessionIslamicAspectRepository>();
        _scheduleProjectionCalculator = new EventScheduleProjectionCalculator();
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _eventLocationAttachmentService = EventLocationAttachmentServiceTestFixture.ForExistingEvent(
            _eventRepository,
            Guid.NewGuid());
        _cache = Substitute.For<HybridCache>();
        _unitOfWork
            .ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<bool>>>();
                return operation!(call.Arg<CancellationToken>());
            });

        _handler = new UpdateEventSessionCommandHandler(
            _eventSessionRepository,
            _eventRepository,
            _locationRepository,
            _locationRoomRepository,
            _registrationModeRepository,
            _eventSessionKindRepository,
            _eventSessionIslamicAspectRepository,
            _scheduleProjectionCalculator,
            _eventDayRepository,
            _unitOfWork,
            _eventLocationAttachmentService,
            _cache
        );
    }

    [Test]
    public async Task Handle_WithFixedIslamicAspectPrayerFields_ReturnsValidationError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateScheduleCommand(
            sessionId,
            eventId,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            "Fixed Session");
        command.EventSessionDto.IslamicAspect = new UpdateEventSessionIslamicAspectUpdateDto
        {
            Value = OptionalUpdate<EventSessionIslamicAspectDto?>.Set(
                new EventSessionIslamicAspectDto
                {
                    StartTimeType = SessionStartTimeType.Fixed,
                    ReferencePrayer = PrayerTime.Dhuhr,
                    OffsetMinutes = 0
                })
        };

        _eventRepository.Exists(eventId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains(EventSessionIslamicAspectValidationRules.SchedulingStateMessage);
        await _eventSessionRepository.DidNotReceive()
            .UpdateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRescheduledToMatchingEventDay_LinksSessionToEventDay()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var eventDayId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var timezone = "Europe/Brussels";
        var startUtc = new DateTimeOffset(2026, 7, 20, 7, 0, 0, TimeSpan.Zero);
        var endUtc = new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);
        var expectedLocalDate = new DateOnly(2026, 7, 20);

        var concurrencyStamp = Guid.NewGuid();
        var command = CreateScheduleCommand(sessionId, eventId, concurrencyStamp, startUtc, endUtc, "Rescheduled Session");

        var existingEvent = DataBuilder.Event.Generate();
        existingEvent.Id = eventId;
        existingEvent.TenantId = tenantId;
        existingEvent.Timezone = timezone;
        existingEvent.EventTimeZoneId = timezone;
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        var existingSession = new EventSession
        {
            Id = sessionId,
            EventId = eventId,
            TenantId = tenantId,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            ConcurrencyStamp = concurrencyStamp,
            Event = null!,
            Tenant = null!
        };
        _eventSessionRepository.GetById(sessionId).Returns(existingSession);

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

        _eventSessionIslamicAspectRepository.GetById(sessionId).Returns((EventSessionIslamicAspect?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(existingSession.EventDayId).IsEqualTo(eventDayId);
    }

    [Test]
    public async Task Handle_WhenRescheduledToDateWithNoEventDay_SetsEventDayIdToNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var timezone = "Europe/Brussels";
        var startUtc = new DateTimeOffset(2026, 7, 20, 7, 0, 0, TimeSpan.Zero);
        var endUtc = new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);

        var concurrencyStamp = Guid.NewGuid();
        var command = CreateScheduleCommand(sessionId, eventId, concurrencyStamp, startUtc, endUtc, "Orphan Session");

        var existingEvent = DataBuilder.Event.Generate();
        existingEvent.Id = eventId;
        existingEvent.TenantId = tenantId;
        existingEvent.Timezone = timezone;
        existingEvent.EventTimeZoneId = timezone;
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        var existingSession = new EventSession
        {
            Id = sessionId,
            EventId = eventId,
            TenantId = tenantId,
            EventDayId = Guid.NewGuid(), // previously linked to a day
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            ConcurrencyStamp = concurrencyStamp,
            Event = null!,
            Tenant = null!
        };
        _eventSessionRepository.GetById(sessionId).Returns(existingSession);

        _eventDayRepository.FindByEventAndLocalDateAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((EventDay?)null);

        _eventSessionIslamicAspectRepository.GetById(sessionId).Returns((EventSessionIslamicAspect?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(existingSession.EventDayId).IsNull();
    }

    [Test]
    public async Task Handle_WhenRescheduledToDifferentDay_ReLinksToNewEventDay()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var oldDayId = Guid.NewGuid();
        var newDayId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var timezone = "Europe/Brussels";
        // Reschedule from July 20 to July 21
        var newStartUtc = new DateTimeOffset(2026, 7, 21, 7, 0, 0, TimeSpan.Zero);
        var newEndUtc = new DateTimeOffset(2026, 7, 21, 9, 0, 0, TimeSpan.Zero);
        var newExpectedLocalDate = new DateOnly(2026, 7, 21);

        var concurrencyStamp = Guid.NewGuid();
        var command = CreateScheduleCommand(sessionId, eventId, concurrencyStamp, newStartUtc, newEndUtc, "Moved Session");

        var existingEvent = DataBuilder.Event.Generate();
        existingEvent.Id = eventId;
        existingEvent.TenantId = tenantId;
        existingEvent.Timezone = timezone;
        existingEvent.EventTimeZoneId = timezone;
        _eventRepository.GetById(eventId).Returns(existingEvent);
        _eventRepository.Exists(eventId).Returns(true);

        var existingSession = new EventSession
        {
            Id = sessionId,
            EventId = eventId,
            TenantId = tenantId,
            EventDayId = oldDayId, // linked to old day
            StartTime = new DateTimeOffset(2026, 7, 20, 7, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.Zero),
            ConcurrencyStamp = concurrencyStamp,
            Event = null!,
            Tenant = null!
        };
        _eventSessionRepository.GetById(sessionId).Returns(existingSession);

        var newDay = new EventDay
        {
            Id = newDayId,
            EventId = eventId,
            LocalDate = newExpectedLocalDate,
            Event = null!,
            Tenant = null!
        };
        _eventDayRepository.FindByEventAndLocalDateAsync(eventId, newExpectedLocalDate, Arg.Any<CancellationToken>())
            .Returns(newDay);

        _eventSessionIslamicAspectRepository.GetById(sessionId).Returns((EventSessionIslamicAspect?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(existingSession.EventDayId).IsEqualTo(newDayId);
        await Assert.That(existingSession.EventDayId).IsNotEqualTo(oldDayId);
    }

    private static UpdateEventSessionCommand CreateScheduleCommand(
        Guid sessionId,
        Guid eventId,
        Guid concurrencyStamp,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string title) => new()
        {
            EventSessionId = sessionId,
            ExpectedConcurrencyStamp = concurrencyStamp,
            EventSessionDto = new UpdateEventSessionDto
            {
                Event = new UpdateEventSessionEventDto { EventId = eventId },
                Schedule = new UpdateEventSessionScheduleDto
                {
                    StartTime = OptionalUpdate<DateTimeOffset?>.Set(startUtc),
                    EndTime = OptionalUpdate<DateTimeOffset?>.Set(endUtc),
                    EndTimeType = OptionalUpdate<SessionEndTimeType>.Set(SessionEndTimeType.Fixed)
                },
                Title = new UpdateEventSessionTitleDto
                {
                    Value = OptionalUpdate<string?>.Set(title)
                }
            }
        };
}
