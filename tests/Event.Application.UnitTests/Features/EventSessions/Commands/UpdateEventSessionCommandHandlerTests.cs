// ABOUTME: Unit tests for UpdateEventSessionCommandHandler validation and schedule re-linking behavior.
// ABOUTME: Verifies event-session updates, event-day movement, and Islamic aspect invariants.

using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Features.EventSessions.Handlers.Commands;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Application.Services.Registration;
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
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 20, 45, 0, TimeSpan.Zero);

    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly IEventSessionKindRepository _eventSessionKindRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EventLocationAttachmentService _eventLocationAttachmentService;
    private readonly HybridCache _cache;
    private readonly FanoutFixture _fanout;
    private readonly IRefundCampaignRepository _refundCampaignRepository;
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
        _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _eventLocationAttachmentService = EventLocationAttachmentServiceTestFixture.ForExistingEvent(
            _eventRepository,
            Guid.NewGuid());
        _cache = Substitute.For<HybridCache>();
        _fanout = new FanoutFixture();
        _refundCampaignRepository = Substitute.For<IRefundCampaignRepository>();
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());
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
            _storageObjectRepository,
            _unitOfWork,
            _eventLocationAttachmentService,
            _cache,
            _fanout.Coordinator,
            Substitute.For<IEventLifecycleScheduler>(),
            _refundCampaignRepository,
            userContext,
            new FixedTimeProvider(Now)
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
    public async Task Handle_WhenFeaturedImageIsCrossTenant_RejectsBeforeMutationOrSave()
    {
        Guid eventId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid imageId = Guid.NewGuid();
        Guid originalImageId = Guid.NewGuid();
        Guid concurrencyStamp = Guid.NewGuid();
        var session = new EventSession
        {
            Id = sessionId,
            EventId = eventId,
            TenantId = tenantId,
            FeaturedImageId = originalImageId,
            ConcurrencyStamp = concurrencyStamp,
            Event = null!,
            Tenant = null!
        };
        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        _eventSessionRepository.GetById(sessionId).Returns(session);
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
        var command = new UpdateEventSessionCommand
        {
            EventSessionId = sessionId,
            ExpectedConcurrencyStamp = concurrencyStamp,
            EventSessionDto = new UpdateEventSessionDto
            {
                FeaturedImage = new UpdateEventSessionFeaturedImageDto
                {
                    Value = OptionalUpdate<Guid?>.Set(imageId)
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(session.FeaturedImageId).IsEqualTo(originalImageId);
        await _eventSessionRepository.DidNotReceive()
            .UpdateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPublishedScheduleChanges_CreatesOneImmutableSessionOccurrence()
    {
        Guid eventId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid concurrencyStamp = Guid.NewGuid();
        DateTimeOffset previousStart = Now.AddDays(1);
        DateTimeOffset previousEnd = previousStart.AddHours(1);
        DateTimeOffset newStart = previousStart.AddHours(1);
        DateTimeOffset newEnd = previousEnd.AddHours(2);
        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        parentEvent.Title = "Parent event";
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        var session = new EventSession(EventSessionStatusEnum.Published)
        {
            Id = sessionId,
            EventId = eventId,
            TenantId = tenantId,
            Title = "Published session",
            StartTime = previousStart,
            EndTime = previousEnd,
            ConcurrencyStamp = concurrencyStamp,
            Event = null!,
            Tenant = null!
        };
        _eventRepository.Exists(eventId).Returns(true);
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventSessionRepository.GetById(sessionId).Returns(session);

        var result = await _handler.Handle(
            CreateScheduleCommand(sessionId, eventId, concurrencyStamp, newStart, newEnd, session.Title),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(_fanout.CreatedOccurrences).Count().IsEqualTo(1);
        await Assert.That(_fanout.OutboxPointers).Count().IsEqualTo(1);
        NotificationFanoutOccurrence occurrence = _fanout.CreatedOccurrences[0];
        await Assert.That(occurrence.EventId).IsEqualTo(eventId);
        await Assert.That(occurrence.SessionId).IsEqualTo(sessionId);
        await Assert.That(occurrence.AggregateVersion).IsEqualTo(concurrencyStamp);
        await Assert.That(occurrence.AudienceCutoffAt).IsEqualTo(Now.UtcDateTime);
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory().Parse(occurrence);
        await Assert.That(template.ChangeSet.Fields).IsEquivalentTo([
            NotificationFanoutChangeField.StartTime,
            NotificationFanoutChangeField.EndTime]);
        await Assert.That(template.Before.StartsAt).IsEqualTo(previousStart);
        await Assert.That(template.Before.EndsAt).IsEqualTo(previousEnd);
        await Assert.That(template.After.StartsAt).IsEqualTo(newStart);
        await Assert.That(template.After.EndsAt).IsEqualTo(newEnd);
        await _refundCampaignRepository.Received(1).CreateAsync(
            Arg.Is<RefundCampaign>(campaign => campaign.EventId == eventId &&
                campaign.Kind == RefundCampaignKind.MaterialChange),
            Arg.Is<OutboxMessage>(message => message.EventType == RefundOutboxMessageFactory.CampaignProcessRequested),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDraftScheduleChanges_CreatesNoAttendeeOccurrence()
    {
        Guid eventId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid concurrencyStamp = Guid.NewGuid();
        DateTimeOffset previousStart = Now.AddDays(1);
        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        var session = new EventSession(EventSessionStatusEnum.Draft)
        {
            Id = sessionId,
            EventId = eventId,
            TenantId = tenantId,
            Title = "Draft session",
            StartTime = previousStart,
            EndTime = previousStart.AddHours(1),
            ConcurrencyStamp = concurrencyStamp,
            Event = null!,
            Tenant = null!
        };
        _eventRepository.Exists(eventId).Returns(true);
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventSessionRepository.GetById(sessionId).Returns(session);

        var result = await _handler.Handle(
            CreateScheduleCommand(
                sessionId,
                eventId,
                concurrencyStamp,
                previousStart.AddHours(1),
                previousStart.AddHours(2),
                session.Title),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(_fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(_fanout.OutboxPointers).IsEmpty();
    }

    [Test]
    public async Task Handle_WhenPublishedSessionMovesToAnotherEvent_RejectsBeforeMutation()
    {
        Guid sourceEventId = Guid.NewGuid();
        Guid targetEventId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid concurrencyStamp = Guid.NewGuid();
        var targetEvent = DataBuilder.Event.Generate();
        targetEvent.Id = targetEventId;
        targetEvent.TenantId = tenantId;
        var session = new EventSession(EventSessionStatusEnum.Published)
        {
            Id = sessionId,
            EventId = sourceEventId,
            TenantId = tenantId,
            Title = "Published session",
            ConcurrencyStamp = concurrencyStamp,
            Event = null!,
            Tenant = null!
        };
        _eventRepository.Exists(targetEventId).Returns(true);
        _eventRepository.GetById(targetEventId).Returns(targetEvent);
        _eventSessionRepository.GetById(sessionId).Returns(session);
        var command = new UpdateEventSessionCommand
        {
            EventSessionId = sessionId,
            ExpectedConcurrencyStamp = concurrencyStamp,
            EventSessionDto = new UpdateEventSessionDto
            {
                Event = new UpdateEventSessionEventDto { EventId = targetEventId }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_update_invalid_status");
        await Assert.That(result.Errors).Contains(
            "Published event sessions cannot be moved to another event until attendee transfer notifications are supported.");
        await Assert.That(session.EventId).IsEqualTo(sourceEventId);
        await _eventSessionRepository.DidNotReceive().MoveToEventAsync(
            Arg.Any<EventSession>(),
            Arg.Any<Guid>(),
            Arg.Any<EventLocation>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await _eventSessionRepository.DidNotReceive().UpdateWithRoomOverlapGuardAsync(
            Arg.Any<EventSession>(),
            Arg.Any<CancellationToken>());
        await Assert.That(_fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(_fanout.OutboxPointers).IsEmpty();
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenSerializableAttemptRetries_ReloadsAuthoritativeSession()
    {
        Guid eventId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid concurrencyStamp = Guid.NewGuid();
        DateTimeOffset staleStart = Now.AddDays(2);
        DateTimeOffset authoritativeStart = Now.AddDays(1);
        DateTimeOffset newStart = Now.AddDays(3);
        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = tenantId;
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        EventSession firstAttemptSession = CreatePublishedSession(staleStart);
        EventSession retrySession = CreatePublishedSession(authoritativeStart);
        _eventRepository.Exists(eventId).Returns(true);
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventSessionRepository.GetById(sessionId).Returns(firstAttemptSession, retrySession);
        int updateAttempts = 0;
        _eventSessionRepository
            .When(repository => repository.UpdateWithRoomOverlapGuardAsync(
                Arg.Any<EventSession>(),
                Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (++updateAttempts == 1)
                {
                    throw new TimeoutException("Simulated transient database failure.");
                }
            });
        _unitOfWork
            .ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                Func<CancellationToken, Task<bool>> operation = call.Arg<Func<CancellationToken, Task<bool>>>();
                try
                {
                    return await operation(call.Arg<CancellationToken>());
                }
                catch (TimeoutException)
                {
                    return await operation(call.Arg<CancellationToken>());
                }
            });

        var result = await _handler.Handle(
            CreateScheduleCommand(
                sessionId,
                eventId,
                concurrencyStamp,
                newStart,
                newStart.AddHours(1),
                "Published session"),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _eventSessionRepository.Received(2).GetById(sessionId);
        await Assert.That(updateAttempts).IsEqualTo(2);
        await Assert.That(_fanout.CreatedOccurrences).Count().IsEqualTo(1);
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory()
            .Parse(_fanout.CreatedOccurrences[0]);
        await Assert.That(template.Before.StartsAt).IsEqualTo(authoritativeStart);
        await Assert.That(template.After.StartsAt).IsEqualTo(newStart);

        EventSession CreatePublishedSession(DateTimeOffset start) => new(EventSessionStatusEnum.Published)
        {
            Id = sessionId,
            EventId = eventId,
            TenantId = tenantId,
            Title = "Published session",
            StartTime = start,
            EndTime = start.AddHours(1),
            ConcurrencyStamp = concurrencyStamp,
            Event = null!,
            Tenant = null!
        };
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

    private sealed class FanoutFixture
    {
        public FanoutFixture()
        {
            OccurrenceRepository.GetPendingForEventCoordinationAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(Array.Empty<NotificationFanoutOccurrence>());
            OccurrenceRepository.SessionBelongsToEventForCoordinationAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);
            OccurrenceRepository.Create(Arg.Any<NotificationFanoutOccurrence>())
                .Returns(call =>
                {
                    NotificationFanoutOccurrence occurrence = call.Arg<NotificationFanoutOccurrence>();
                    CreatedOccurrences.Add(occurrence);
                    return occurrence;
                });
            OutboxRepository.Create(Arg.Any<OutboxMessage>())
                .Returns(call =>
                {
                    OutboxMessage message = call.Arg<OutboxMessage>();
                    OutboxPointers.Add(message);
                    return message;
                });
            Coordinator = new NotificationFanoutOccurrenceCoordinator(
                OccurrenceRepository,
                Substitute.For<INotificationFanoutEmailSuppressionRepository>(),
                OutboxRepository,
                new NotificationFanoutRecipientTemplateFactory());
        }

        public INotificationFanoutOccurrenceRepository OccurrenceRepository { get; } =
            Substitute.For<INotificationFanoutOccurrenceRepository>();
        public IOutboxRepository OutboxRepository { get; } = Substitute.For<IOutboxRepository>();
        public NotificationFanoutOccurrenceCoordinator Coordinator { get; }
        public List<NotificationFanoutOccurrence> CreatedOccurrences { get; } = [];
        public List<OutboxMessage> OutboxPointers { get; } = [];
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
