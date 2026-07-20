// ABOUTME: Unit tests for event-session draft, schedule, and publish lifecycle command handlers.
// ABOUTME: Verifies lifecycle policy, atomic attendee fanout, retry-safe scheduling, and cache sequencing.

using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Handlers.Commands;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Services.Lifecycle;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventSessions.Commands;

public sealed class EventSessionLifecycleCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 19, 15, 0, TimeSpan.Zero);

    [Test]
    public async Task CreateDraft_WhenParentEventExists_CreatesUnscheduledDraftSession()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<EventSession>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<EventSession>>>()(
                call.Arg<CancellationToken>()));
        var eventLocationAttachmentService = EventLocationAttachmentServiceTestFixture.ForExistingEvent(
            eventRepository,
            Guid.NewGuid());
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventSessionRepository.Create(Arg.Any<EventSession>())
            .Returns(callInfo => callInfo.Arg<EventSession>());
        var handler = new CreateDraftEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreatePolicyProvider(CreateSessionDraftPolicy()),
            new EventLifecycleReadinessEvaluator(),
            unitOfWork,
            eventLocationAttachmentService,
            cache);

        var result = await handler.Handle(new CreateDraftEventSessionCommand
        {
            TenantId = parentEvent.TenantId,
            Request = new CreateDraftEventSessionRequestDto
            {
                EventId = parentEvent.Id,
                Title = "Draft session"
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await eventSessionRepository.Received(1).Create(Arg.Is<EventSession>(session =>
            session.EventId == parentEvent.Id
            && session.TenantId == parentEvent.TenantId
            && session.EventSessionStatusId == (int)EventSessionStatusEnum.Draft
            && session.StartTime == null
            && session.EndTime == null));
        await cache.Received(1).RemoveAsync($"event:detail:{parentEvent.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Schedule_WhenConcurrencyMatches_ProjectsScheduleAndUsesOverlapGuard()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var eventDayRepository = Substitute.For<IEventDayRepository>();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Draft);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var fanout = new FanoutFixture();
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            eventDayRepository,
            new EventScheduleProjectionCalculator(),
            CreatePolicyProvider(CreateSessionSchedulePolicy()),
            new EventLifecycleReadinessEvaluator(),
            CreateUnitOfWork(),
            cache,
            fanout.Coordinator,
            new FixedTimeProvider(Now));
        var start = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(2);

        var result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = session.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp,
                StartTime = start,
                EndTime = end
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(session.StartTime).IsEqualTo(start);
        await Assert.That(session.EndTime).IsEqualTo(end);
        await Assert.That(session.LocalStartDate).IsNotNull();
        await eventSessionRepository.Received(1).UpdateWithRoomOverlapGuardAsync(session, Arg.Any<CancellationToken>());
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Schedule_WhenSessionRemainsDraft_DoesNotMoveParentPublicScheduleSummary()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var eventDayRepository = Substitute.For<IEventDayRepository>();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Draft);
        parentEvent.Sessions.Add(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            eventDayRepository,
            new EventScheduleProjectionCalculator(),
            CreatePolicyProvider(CreateSessionSchedulePolicy()),
            new EventLifecycleReadinessEvaluator(),
            CreateUnitOfWork(),
            cache,
            new FanoutFixture().Coordinator,
            new FixedTimeProvider(Now));
        var start = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(2);

        var result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = session.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp,
                StartTime = start,
                EndTime = end
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(parentEvent.SessionCount).IsEqualTo(0);
        await Assert.That(parentEvent.FirstSessionStartUtc).IsNull();
        await eventRepository.Received(1).Update(parentEvent);
    }

    [Test]
    public async Task Schedule_WhenConcurrencyDiffers_ReturnsConflictWithoutUpdating()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Draft);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            Substitute.For<IEventRepository>(),
            Substitute.For<IEventDayRepository>(),
            new EventScheduleProjectionCalculator(),
            CreatePolicyProvider(CreateSessionSchedulePolicy()),
            new EventLifecycleReadinessEvaluator(),
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            new FanoutFixture().Coordinator,
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = session.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = Guid.NewGuid(),
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1)
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_schedule_concurrency_conflict");
        await eventSessionRepository.DidNotReceive().UpdateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Schedule_WhenPublishedSessionIsRescheduled_FreezesOneOccurrenceBeforeCommit()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var eventDayRepository = Substitute.For<IEventDayRepository>();
        var cache = Substitute.For<HybridCache>();
        Explore.Domain.Event parentEvent = CreateEvent(EventStatusEnum.Published);
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        EventSession session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        DateTimeOffset previousStart = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset previousEnd = previousStart.AddHours(1);
        DateTimeOffset newStart = previousStart.AddHours(1);
        DateTimeOffset newEnd = previousEnd.AddHours(1);
        session.StartTime = previousStart;
        session.EndTime = previousEnd;
        Guid expectedConcurrencyStamp = session.ConcurrencyStamp;
        parentEvent.Sessions.Add(session);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        bool transactionCompleted = false;
        bool occurrenceCreatedBeforeCommit = false;
        bool cacheObservedCommit = false;
        cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cacheObservedCommit = transactionCompleted;
                return ValueTask.CompletedTask;
            });
        var fanout = new FanoutFixture(() => occurrenceCreatedBeforeCommit = !transactionCompleted);
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            eventDayRepository,
            new EventScheduleProjectionCalculator(),
            CreatePolicyProvider(CreateSessionSchedulePolicy()),
            new EventLifecycleReadinessEvaluator(),
            CreateUnitOfWork(() => transactionCompleted = true),
            cache,
            fanout.Coordinator,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = session.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = expectedConcurrencyStamp,
                StartTime = newStart,
                EndTime = newEnd
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(fanout.CreatedOccurrences).Count().IsEqualTo(1);
        await Assert.That(fanout.OutboxPointers).Count().IsEqualTo(1);
        NotificationFanoutOccurrence occurrence = fanout.CreatedOccurrences[0];
        session.Reschedule(newStart.AddHours(1), newEnd.AddHours(1), "Europe/Brussels", new EventScheduleProjectionCalculator());
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory().Parse(occurrence);
        await Assert.That(occurrence.EventId).IsEqualTo(parentEvent.Id);
        await Assert.That(occurrence.SessionId).IsEqualTo(session.Id);
        await Assert.That(occurrence.AggregateVersion).IsEqualTo(expectedConcurrencyStamp);
        await Assert.That(occurrence.AudienceCutoffAt).IsEqualTo(Now.UtcDateTime);
        await Assert.That(template.ChangeSet.Fields).IsEquivalentTo([
            NotificationFanoutChangeField.StartTime,
            NotificationFanoutChangeField.EndTime]);
        await Assert.That(template.Before.StartsAt).IsEqualTo(previousStart);
        await Assert.That(template.Before.EndsAt).IsEqualTo(previousEnd);
        await Assert.That(template.After.StartsAt).IsEqualTo(newStart);
        await Assert.That(template.After.EndsAt).IsEqualTo(newEnd);
        await Assert.That(occurrenceCreatedBeforeCommit).IsTrue();
        await Assert.That(cacheObservedCommit).IsTrue();
    }

    [Test]
    public async Task Schedule_WhenPublishedScheduleIsUnchanged_CreatesNoOccurrence()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        Explore.Domain.Event parentEvent = CreateEvent(EventStatusEnum.Published);
        EventSession session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        DateTimeOffset start = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        session.StartTime = start;
        session.EndTime = start.AddHours(1);
        parentEvent.Sessions.Add(session);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        var fanout = new FanoutFixture();
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            Substitute.For<IEventDayRepository>(),
            new EventScheduleProjectionCalculator(),
            CreatePolicyProvider(CreateSessionSchedulePolicy()),
            new EventLifecycleReadinessEvaluator(),
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            fanout.Coordinator,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = session.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp,
                StartTime = session.StartTime.Value,
                EndTime = session.EndTime.Value
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
    }

    [Test]
    public async Task Schedule_WhenSerializableAttemptRetries_ReloadsAuthoritativePublishedSession()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        Explore.Domain.Event parentEvent = CreateEvent(EventStatusEnum.Published);
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        DateTimeOffset staleStart = new(2026, 8, 2, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset authoritativeStart = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset newStart = new(2026, 8, 3, 10, 0, 0, TimeSpan.Zero);
        EventSession firstAttemptSession = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        EventSession retrySession = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        retrySession.Id = firstAttemptSession.Id;
        retrySession.ConcurrencyStamp = firstAttemptSession.ConcurrencyStamp;
        firstAttemptSession.StartTime = staleStart;
        firstAttemptSession.EndTime = staleStart.AddHours(1);
        retrySession.StartTime = authoritativeStart;
        retrySession.EndTime = authoritativeStart.AddHours(1);
        eventSessionRepository.GetById(firstAttemptSession.Id).Returns(firstAttemptSession, retrySession);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        int updateAttempts = 0;
        eventSessionRepository
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
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                Func<CancellationToken, Task<BaseCommandResponse<Guid>>> operation =
                    call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                try
                {
                    return await operation(call.Arg<CancellationToken>());
                }
                catch (TimeoutException)
                {
                    return await operation(call.Arg<CancellationToken>());
                }
            });
        var fanout = new FanoutFixture();
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            Substitute.For<IEventDayRepository>(),
            new EventScheduleProjectionCalculator(),
            CreatePolicyProvider(CreateSessionSchedulePolicy()),
            new EventLifecycleReadinessEvaluator(),
            unitOfWork,
            Substitute.For<HybridCache>(),
            fanout.Coordinator,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = firstAttemptSession.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = firstAttemptSession.ConcurrencyStamp,
                StartTime = newStart,
                EndTime = newStart.AddHours(1)
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await eventSessionRepository.Received(2).GetById(firstAttemptSession.Id);
        await Assert.That(updateAttempts).IsEqualTo(2);
        await Assert.That(fanout.CreatedOccurrences).Count().IsEqualTo(1);
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory()
            .Parse(fanout.CreatedOccurrences[0]);
        await Assert.That(template.Before.StartsAt).IsEqualTo(authoritativeStart);
        await Assert.That(template.After.StartsAt).IsEqualTo(newStart);
    }

    [Test]
    public async Task Publish_WhenSessionIsReadyAndParentPublished_TransitionsToPublished()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Approved);
        var start = DateTimeOffset.UtcNow.AddDays(1);
        session.Reschedule(start, start.AddHours(1), "UTC", new EventScheduleProjectionCalculator());
        parentEvent.Sessions.Add(session);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        var handler = new PublishEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreatePolicyProvider(CreateSessionPublishPolicy()),
            new EventLifecycleReadinessEvaluator(),
            cache);

        var result = await handler.Handle(new PublishEventSessionCommand
        {
            Id = session.Id,
            Request = new PublishEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Published);
        await Assert.That(parentEvent.SessionCount).IsEqualTo(1);
        await Assert.That(parentEvent.FirstSessionStartUtc).IsEqualTo(session.StartTime);
        await eventSessionRepository.Received(1).Update(session);
        await eventRepository.Received(1).Update(parentEvent);
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WhenParentEventIsDraft_ReturnsReadinessFailureWithoutUpdating()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var parentEvent = CreateEvent(EventStatusEnum.Draft);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Approved);
        session.StartTime = DateTimeOffset.UtcNow.AddDays(1);
        session.EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        var handler = new PublishEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreatePolicyProvider(CreateSessionPublishPolicy()),
            new EventLifecycleReadinessEvaluator(),
            Substitute.For<HybridCache>());

        var result = await handler.Handle(new PublishEventSessionCommand
        {
            Id = session.Id,
            Request = new PublishEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_publish_readiness_failed");
        await Assert.That(result.Errors!.Any(error => error.Contains("Parent event must be published"))).IsTrue();
        await eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
    }

    [Test]
    public async Task Cancel_WhenSessionIsPublished_TransitionsToCancelledAndRefreshesParentSummary()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        parentEvent.EventTimeZoneId = "Europe/Brussels";
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        session.StartTime = Now.AddDays(1);
        session.EndTime = Now.AddDays(1).AddHours(1);
        Guid expectedConcurrencyStamp = session.ConcurrencyStamp;
        parentEvent.Sessions.Add(session);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        bool transactionCompleted = false;
        bool occurrenceCreatedBeforeCommit = false;
        bool cacheObservedCommit = false;
        cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cacheObservedCommit = transactionCompleted;
                return ValueTask.CompletedTask;
            });
        var fanout = new FanoutFixture(() => occurrenceCreatedBeforeCommit = !transactionCompleted);
        var handler = new CancelEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(() => transactionCompleted = true),
            cache,
            fanout.Coordinator,
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CancelEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Cancelled);
        await eventSessionRepository.Received(1).Update(session);
        await eventRepository.Received(1).Update(parentEvent);
        await Assert.That(fanout.CreatedOccurrences).Count().IsEqualTo(1);
        await Assert.That(fanout.OutboxPointers).Count().IsEqualTo(1);
        NotificationFanoutOccurrence occurrence = fanout.CreatedOccurrences[0];
        await Assert.That(occurrence.EventId).IsEqualTo(parentEvent.Id);
        await Assert.That(occurrence.SessionId).IsEqualTo(session.Id);
        await Assert.That(occurrence.AggregateVersion).IsEqualTo(expectedConcurrencyStamp);
        await Assert.That(occurrence.AudienceCutoffAt).IsEqualTo(Now.UtcDateTime);
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory().Parse(occurrence);
        await Assert.That(template.IsCancellation).IsTrue();
        await Assert.That(template.IsSessionScoped).IsTrue();
        await Assert.That(template.Before.EventTitle).IsEqualTo(parentEvent.Title);
        await Assert.That(template.Before.SessionTitle).IsEqualTo(session.Title);
        await Assert.That(template.Before.StartsAt).IsEqualTo(session.StartTime);
        await Assert.That(template.Before.EndsAt).IsEqualTo(session.EndTime);
        await Assert.That(template.Before.Timezone).IsEqualTo("Europe/Brussels");
        await Assert.That(occurrenceCreatedBeforeCommit).IsTrue();
        await Assert.That(cacheObservedCommit).IsTrue();
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Cancel_WhenSessionWasNotPublished_DoesNotCreateAttendeeOccurrence()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Approved);
        parentEvent.Sessions.Add(session);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        var fanout = new FanoutFixture();
        var handler = new CancelEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            fanout.Coordinator,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CancelEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Cancelled);
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
    }

    [Test]
    public async Task Cancel_WhenSessionAlreadyCancelled_CreatesNoWork()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Cancelled);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        var fanout = new FanoutFixture();
        var handler = new CancelEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(),
            cache,
            fanout.Coordinator,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CancelEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_cancel_already_cancelled");
        await eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Complete_WhenSessionIsPublishedAndParentPublished_TransitionsToCompleted()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        var handler = new CompleteEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(),
            Substitute.For<HybridCache>());

        var result = await handler.Handle(new CompleteEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Completed);
        await eventSessionRepository.Received(1).Update(session);
    }

    [Test]
    public async Task Complete_WhenParentEventIsModerated_ReturnsInvalidStatusWithoutUpdating()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var parentEvent = CreateEvent(EventStatusEnum.Moderated);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        var handler = new CompleteEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(),
            Substitute.For<HybridCache>());

        var result = await handler.Handle(new CompleteEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_complete_invalid_status");
        await eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
    }

    [Test]
    public async Task Archive_WhenSessionIsCancelled_TransitionsToArchived()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Cancelled);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        var handler = new ArchiveEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(),
            Substitute.For<HybridCache>());

        var result = await handler.Handle(new ArchiveEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Archived);
        await eventSessionRepository.Received(1).Update(session);
    }

    private static IEventLifecyclePolicyProvider CreatePolicyProvider(EventLifecyclePolicy policy)
    {
        var provider = Substitute.For<IEventLifecyclePolicyProvider>();
        provider
            .GetEffectivePolicyAsync(Arg.Any<Guid?>(), policy.Profile, Arg.Any<CancellationToken>())
            .Returns(policy);
        return provider;
    }

    private static IUnitOfWork CreateUnitOfWork(Action? onCompleted = null)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                BaseCommandResponse<Guid> response = await operation(call.Arg<CancellationToken>());
                onCompleted?.Invoke();
                return response;
            });
        unitOfWork
            .ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                BaseCommandResponse<Guid> response = await operation(call.Arg<CancellationToken>());
                onCompleted?.Invoke();
                return response;
            });

        return unitOfWork;
    }

    private sealed class FanoutFixture
    {
        public FanoutFixture(Action? onOccurrenceCreated = null)
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
                    onOccurrenceCreated?.Invoke();
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

    private static EventLifecyclePolicy CreateSessionDraftPolicy() => new()
    {
        Profile = ValidationProfile.SessionDraftCreate,
        RequiredEventFields = new HashSet<Enum>(),
        RequiredSessionFields = new HashSet<Enum>
        {
            EventSessionFieldKey.ParentEvent,
            EventSessionFieldKey.Tenant,
            EventSessionFieldKey.Status,
            EventSessionFieldKey.Title
        }
    };

    private static EventLifecyclePolicy CreateSessionSchedulePolicy() => new()
    {
        Profile = ValidationProfile.SessionSchedule,
        RequiredEventFields = new HashSet<Enum>(),
        RequiredSessionFields = new HashSet<Enum>
        {
            EventSessionFieldKey.ParentEvent,
            EventSessionFieldKey.Tenant,
            EventSessionFieldKey.Status,
            EventSessionFieldKey.ScheduleStart,
            EventSessionFieldKey.ScheduleEnd
        }
    };

    private static EventLifecyclePolicy CreateSessionPublishPolicy() => new()
    {
        Profile = ValidationProfile.SessionPublish,
        RequiredEventFields = new HashSet<Enum>(),
        RequiredSessionFields = new HashSet<Enum>
        {
            EventSessionFieldKey.ParentEvent,
            EventSessionFieldKey.Tenant,
            EventSessionFieldKey.Status,
            EventSessionFieldKey.Title,
            EventSessionFieldKey.ScheduleStart,
            EventSessionFieldKey.ScheduleEnd,
            EventSessionFieldKey.ParentEventCompatibility
        }
    };

    private static Explore.Domain.Event CreateEvent(EventStatusEnum status) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Parent event",
        ActorId = Guid.NewGuid(),
        Actor = null!,
        TenantId = Guid.NewGuid(),
        Tenant = null!,
        VisibilityTypeId = (int)VisibilityTypeEnum.Public,
        VisibilityType = null!,
        EventStatusId = (int)status,
        EventStatus = null!,
        EventFormatId = (int)EventFormatEnum.Local,
        EventFormat = null!
    };

    private static EventSession CreateSession(Explore.Domain.Event parentEvent, EventSessionStatusEnum status) => new()
    {
        Id = Guid.NewGuid(),
        EventId = parentEvent.Id,
        Event = null!,
        TenantId = parentEvent.TenantId,
        Tenant = null!,
        Title = "Lifecycle session",
        EventSessionStatusId = (int)status,
        ConcurrencyStamp = Guid.NewGuid()
    };
}
