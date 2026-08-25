// ABOUTME: Unit tests for event-session draft, schedule, and publish lifecycle command handlers.
// ABOUTME: Verifies lifecycle policy, atomic attendee fanout, retry-safe scheduling, and cache sequencing.

using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSessions.Handlers.Commands;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Services.Lifecycle;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Explore.Domain.ValueObjects;
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

        await Assert.That(result.IsSuccess).IsTrue();
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
            Substitute.For<IEventLifecycleScheduler>(),
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

        await Assert.That(result.IsSuccess).IsTrue();
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
            Substitute.For<IEventLifecycleScheduler>(),
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

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(parentEvent.SessionCount).IsEqualTo(0);
        await Assert.That(parentEvent.FirstSessionStartUtc).IsNull();
        await eventRepository.Received(1).Update(parentEvent);
    }

    [Test]
    public async Task Schedule_WhenConcurrencyDiffersAndTimesDiffer_ReturnsConflictWithoutWork()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Draft);
        session.StartTime = Now.AddDays(1);
        session.EndTime = Now.AddDays(1).AddHours(1);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            Substitute.For<IEventDayRepository>(),
            new EventScheduleProjectionCalculator(),
            CreatePolicyProvider(CreateSessionSchedulePolicy()),
            new EventLifecycleReadinessEvaluator(),
            unitOfWork,
            Substitute.For<HybridCache>(),
            new FanoutFixture().Coordinator,
            Substitute.For<IEventLifecycleScheduler>(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = session.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = Guid.NewGuid(),
                StartTime = session.StartTime.Value.AddHours(2),
                EndTime = session.EndTime.Value.AddHours(2)
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_schedule_concurrency_conflict");
        await eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await unitOfWork.DidNotReceive().ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
            Arg.Any<CancellationToken>());
        await eventSessionRepository.DidNotReceive().UpdateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Schedule_WhenEligibleTimesAreEqualAndStampIsStale_ReturnsSuccessWithoutWork()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        var readinessEvaluator = Substitute.For<IEventLifecycleReadinessEvaluator>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Draft);
        session.StartTime = Now.AddDays(1);
        session.EndTime = Now.AddDays(1).AddHours(1);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            Substitute.For<IEventDayRepository>(),
            new EventScheduleProjectionCalculator(),
            policyProvider,
            readinessEvaluator,
            unitOfWork,
            cache,
            new FanoutFixture().Coordinator,
            Substitute.For<IEventLifecycleScheduler>(),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = session.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = Guid.NewGuid(),
                StartTime = session.StartTime.Value,
                EndTime = session.EndTime.Value
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event session schedule is unchanged.");
        await eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await unitOfWork.DidNotReceive().ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
            Arg.Any<CancellationToken>());
        await policyProvider.DidNotReceive().GetEffectivePolicyAsync(
            Arg.Any<Guid?>(), Arg.Any<ValidationProfile>(), Arg.Any<CancellationToken>());
        readinessEvaluator.DidNotReceive().Evaluate(
            Arg.Any<EventSession>(), Arg.Any<Explore.Domain.Event?>(), Arg.Any<ValidationProfile>(), Arg.Any<EventLifecyclePolicy>());
        await eventSessionRepository.DidNotReceive().UpdateWithRoomOverlapGuardAsync(
            Arg.Any<EventSession>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Schedule_WhenSaveReportsConcurrencyConflict_ReturnsStableConflictWithoutCacheOrHooks()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var eventDayRepository = Substitute.For<IEventDayRepository>();
        var cache = Substitute.For<HybridCache>();
        var scheduler = Substitute.For<IEventLifecycleScheduler>();
        var fanout = new FanoutFixture();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventSessionRepository.UpdateWithRoomOverlapGuardAsync(session, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The event session was modified while saving.")));
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
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
            scheduler,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = session.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp,
                StartTime = Now.AddDays(1),
                EndTime = Now.AddDays(1).AddHours(1)
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event session was modified by another request.");
        await Assert.That(result.Errors).IsEquivalentTo(["Refresh the event session and try scheduling again."]);
        await Assert.That(result.FailureCode).IsEqualTo("event_session_schedule_concurrency_conflict");
        await eventSessionRepository.Received(2).GetById(session.Id);
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await scheduler.DidNotReceive().ReprojectEventRemindersInCurrentTransactionAsync(
            Arg.Any<EventReminderReprojectionInput>(),
            Arg.Any<CancellationToken>());
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
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
        eventSessionRepository.GetByIdForEventAsync(
            session.Id,
            parentEvent.Id,
            parentEvent.TenantId,
            Arg.Any<CancellationToken>()).Returns(session);
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
            Substitute.For<IEventLifecycleScheduler>(),
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

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(fanout.CreatedOccurrences).Count().IsEqualTo(1);
        await Assert.That(fanout.OutboxPointers).Count().IsEqualTo(1);
        NotificationFanoutOccurrence occurrence = fanout.CreatedOccurrences[0];
        session.Reschedule(UtcInstantRange.Create(newStart.AddHours(1), newEnd.AddHours(1)), "Europe/Brussels", new EventScheduleProjectionCalculator());
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
        var cache = Substitute.For<HybridCache>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        var readinessEvaluator = Substitute.For<IEventLifecycleReadinessEvaluator>();
        var scheduler = Substitute.For<IEventLifecycleScheduler>();
        var fanout = new FanoutFixture();
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            Substitute.For<IEventDayRepository>(),
            new EventScheduleProjectionCalculator(),
            policyProvider,
            readinessEvaluator,
            CreateUnitOfWork(),
            cache,
            fanout.Coordinator,
            scheduler,
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

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
        await policyProvider.DidNotReceive().GetEffectivePolicyAsync(
            Arg.Any<Guid?>(),
            Arg.Any<ValidationProfile>(),
            Arg.Any<CancellationToken>());
        readinessEvaluator.DidNotReceive().Evaluate(
            Arg.Any<EventSession>(),
            Arg.Any<Explore.Domain.Event?>(),
            Arg.Any<ValidationProfile>(),
            Arg.Any<EventLifecyclePolicy>());
        await eventSessionRepository.DidNotReceive().UpdateWithRoomOverlapGuardAsync(Arg.Any<EventSession>(), Arg.Any<CancellationToken>());
        await eventRepository.DidNotReceive().GetScheduleGraphForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await scheduler.DidNotReceive().ReprojectEventRemindersInCurrentTransactionAsync(
            Arg.Any<EventReminderReprojectionInput>(),
            Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WhenSessionAlreadyPublishedAndStampIsStale_ReturnsSuccessBeforeReadinessAndSideEffects()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Draft);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        var readinessEvaluator = Substitute.For<IEventLifecycleReadinessEvaluator>();
        var handler = new PublishEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            policyProvider,
            readinessEvaluator,
            CreateUnitOfWork(),
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new PublishEventSessionCommand
        {
            Id = session.Id,
            Request = new PublishEventSessionRequestDto { ExpectedConcurrencyStamp = Guid.NewGuid() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event session is already published.");
        await eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await policyProvider.DidNotReceive().GetEffectivePolicyAsync(
            Arg.Any<Guid?>(), Arg.Any<ValidationProfile>(), Arg.Any<CancellationToken>());
        readinessEvaluator.DidNotReceive().Evaluate(
            Arg.Any<EventSession>(), Arg.Any<Explore.Domain.Event?>(), Arg.Any<ValidationProfile>(), Arg.Any<EventLifecyclePolicy>());
        await eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
        await eventRepository.DidNotReceive().GetScheduleGraphForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WhenSessionIsNotPublishedAndStampIsStale_ReturnsConcurrencyConflictWithoutWork()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        var readinessEvaluator = Substitute.For<IEventLifecycleReadinessEvaluator>();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Approved);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var handler = new PublishEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            policyProvider,
            readinessEvaluator,
            CreateUnitOfWork(),
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new PublishEventSessionCommand
        {
            Id = session.Id,
            Request = new PublishEventSessionRequestDto { ExpectedConcurrencyStamp = Guid.NewGuid() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_publish_concurrency_conflict");
        await eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await policyProvider.DidNotReceive().GetEffectivePolicyAsync(
            Arg.Any<Guid?>(), Arg.Any<ValidationProfile>(), Arg.Any<CancellationToken>());
        readinessEvaluator.DidNotReceive().Evaluate(
            Arg.Any<EventSession>(), Arg.Any<Explore.Domain.Event?>(), Arg.Any<ValidationProfile>(), Arg.Any<EventLifecyclePolicy>());
        await eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Schedule_WhenSerializableAttemptRetries_ReloadsAuthoritativePublishedSession()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        Explore.Domain.Event firstAttemptParent = CreateEvent(EventStatusEnum.Published);
        firstAttemptParent.EventTimeZoneId = "Europe/Brussels";
        firstAttemptParent.Title = "Stale parent title";
        Explore.Domain.Event retryParent = CreateEvent(EventStatusEnum.Published);
        retryParent.Id = firstAttemptParent.Id;
        retryParent.TenantId = firstAttemptParent.TenantId;
        retryParent.EventTimeZoneId = "Europe/Brussels";
        retryParent.Title = "Authoritative parent title";
        DateTimeOffset staleStart = new(2026, 8, 2, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset authoritativeStart = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset newStart = new(2026, 8, 3, 10, 0, 0, TimeSpan.Zero);
        EventSession firstAttemptSession = CreateSession(firstAttemptParent, EventSessionStatusEnum.Published);
        EventSession retrySession = CreateSession(retryParent, EventSessionStatusEnum.Published);
        retrySession.Id = firstAttemptSession.Id;
        retrySession.ConcurrencyStamp = firstAttemptSession.ConcurrencyStamp;
        firstAttemptSession.StartTime = staleStart;
        firstAttemptSession.EndTime = staleStart.AddHours(1);
        retrySession.StartTime = authoritativeStart;
        retrySession.EndTime = authoritativeStart.AddHours(1);
        eventSessionRepository.GetById(firstAttemptSession.Id).Returns(firstAttemptSession, firstAttemptSession, retrySession);
        eventRepository.GetById(firstAttemptParent.Id).Returns(firstAttemptParent, retryParent);
        eventRepository.GetScheduleGraphForUpdateAsync(firstAttemptParent.Id, Arg.Any<CancellationToken>()).Returns(retryParent);
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
                Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>> operation =
                    call.Arg<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>();
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
            Substitute.For<IEventLifecycleScheduler>(),
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

        await Assert.That(result.IsSuccess).IsTrue();
        await eventSessionRepository.Received(3).GetById(firstAttemptSession.Id);
        await Assert.That(updateAttempts).IsEqualTo(2);
        await Assert.That(fanout.CreatedOccurrences).Count().IsEqualTo(1);
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory()
            .Parse(fanout.CreatedOccurrences[0]);
        await Assert.That(template.Before.EventTitle).IsEqualTo("Authoritative parent title");
        await Assert.That(template.Before.StartsAt).IsEqualTo(authoritativeStart);
        await Assert.That(template.After.StartsAt).IsEqualTo(newStart);
    }

    [Test]
    public async Task Schedule_WhenCommitAmbiguityRetryObservesRequestedSchedule_InvalidatesCacheAndRunsDurableHooksOnce()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var scheduler = Substitute.For<IEventLifecycleScheduler>();
        Explore.Domain.Event firstAttemptParent = CreateEvent(EventStatusEnum.Published);
        firstAttemptParent.EventTimeZoneId = "Europe/Brussels";
        Explore.Domain.Event retryParent = CreateEvent(EventStatusEnum.Published);
        retryParent.Id = firstAttemptParent.Id;
        retryParent.TenantId = firstAttemptParent.TenantId;
        retryParent.EventTimeZoneId = "Europe/Brussels";
        DateTimeOffset previousStart = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset requestedStart = previousStart.AddHours(2);
        EventSession firstAttemptSession = CreateSession(firstAttemptParent, EventSessionStatusEnum.Published);
        firstAttemptSession.StartTime = previousStart;
        firstAttemptSession.EndTime = previousStart.AddHours(1);
        EventSession retrySession = CreateSession(retryParent, EventSessionStatusEnum.Published);
        retrySession.Id = firstAttemptSession.Id;
        retrySession.ConcurrencyStamp = Guid.NewGuid();
        retrySession.StartTime = requestedStart;
        retrySession.EndTime = requestedStart.AddHours(1);
        firstAttemptParent.Sessions.Add(firstAttemptSession);
        eventSessionRepository.GetById(firstAttemptSession.Id).Returns(firstAttemptSession, firstAttemptSession, retrySession);
        eventRepository.GetById(firstAttemptParent.Id).Returns(firstAttemptParent, retryParent);
        eventRepository.GetScheduleGraphForUpdateAsync(firstAttemptParent.Id, Arg.Any<CancellationToken>()).Returns(firstAttemptParent);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>();
                await operation(call.Arg<CancellationToken>());
                return await operation(call.Arg<CancellationToken>());
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
            cache,
            fanout.Coordinator,
            scheduler,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = firstAttemptSession.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = firstAttemptSession.ConcurrencyStamp,
                StartTime = requestedStart,
                EndTime = requestedStart.AddHours(1)
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventSessionRepository.Received(3).GetById(firstAttemptSession.Id);
        await eventRepository.Received(2).GetById(firstAttemptParent.Id);
        await eventSessionRepository.Received(1).UpdateWithRoomOverlapGuardAsync(firstAttemptSession, Arg.Any<CancellationToken>());
        await eventRepository.Received(1).GetScheduleGraphForUpdateAsync(firstAttemptParent.Id, Arg.Any<CancellationToken>());
        await Assert.That(fanout.CreatedOccurrences).Count().IsEqualTo(1);
        await Assert.That(fanout.OutboxPointers).Count().IsEqualTo(1);
        await scheduler.Received(1).ReprojectEventRemindersInCurrentTransactionAsync(
            Arg.Any<EventReminderReprojectionInput>(),
            Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveAsync($"event:detail:{firstAttemptParent.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(firstAttemptParent.TenantId), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
            Arg.Any<CancellationToken>());
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
        session.Reschedule(UtcInstantRange.Create(start, start.AddHours(1)), "UTC", new EventScheduleProjectionCalculator());
        parentEvent.Sessions.Add(session);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventSessionRepository.GetByIdForEventAsync(
            session.Id,
            parentEvent.Id,
            parentEvent.TenantId,
            Arg.Any<CancellationToken>()).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        var handler = new PublishEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreatePolicyProvider(CreateSessionPublishPolicy()),
            new EventLifecycleReadinessEvaluator(),
            CreateUnitOfWork(),
            cache,
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new PublishEventSessionCommand
        {
            Id = session.Id,
            Request = new PublishEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Published);
        await Assert.That(session.UpdatedAt).IsEqualTo(Now.UtcDateTime);
        await Assert.That(parentEvent.SessionCount).IsEqualTo(1);
        await Assert.That(parentEvent.FirstSessionStartUtc).IsEqualTo(session.StartTime);
        await eventSessionRepository.Received(1).Update(session);
        await eventRepository.Received(1).Update(parentEvent);
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WhenParentSummaryUpdateFails_RollsBackSessionAndParentAndDoesNotInvalidateCache()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        Explore.Domain.Event outerParent = CreateEvent(EventStatusEnum.Published);
        Explore.Domain.Event transactionParent = CreateEvent(EventStatusEnum.Published);
        transactionParent.Id = outerParent.Id;
        transactionParent.TenantId = outerParent.TenantId;
        EventSession outerSession = CreateSession(outerParent, EventSessionStatusEnum.Approved);
        outerSession.StartTime = Now.AddDays(1);
        outerSession.EndTime = Now.AddDays(1).AddHours(1);
        EventSession transactionSession = CreateSession(transactionParent, EventSessionStatusEnum.Approved);
        transactionSession.Id = outerSession.Id;
        transactionSession.ConcurrencyStamp = outerSession.ConcurrencyStamp;
        transactionSession.StartTime = Now.AddDays(1);
        transactionSession.EndTime = Now.AddDays(1).AddHours(1);
        transactionParent.Sessions.Add(transactionSession);
        eventSessionRepository.GetById(outerSession.Id).Returns(outerSession);
        eventSessionRepository.GetByIdForEventAsync(
            outerSession.Id,
            outerParent.Id,
            outerParent.TenantId,
            Arg.Any<CancellationToken>()).Returns(transactionSession);
        eventRepository.GetById(outerParent.Id).Returns(outerParent, transactionParent);
        eventRepository.GetScheduleGraphForUpdateAsync(outerParent.Id, Arg.Any<CancellationToken>()).Returns(transactionParent);
        int persistedSessionStatus = (int)EventSessionStatusEnum.Approved;
        int? persistedSessionCount = 0;
        eventSessionRepository.Update(transactionSession).Returns(_ =>
        {
            persistedSessionStatus = transactionSession.EventSessionStatusId;
            return Task.CompletedTask;
        });
        eventRepository.Update(transactionParent).Returns(_ =>
        {
            persistedSessionCount = transactionParent.SessionCount;
            throw new InvalidOperationException("Simulated parent rollup failure.");
        });
        var unitOfWork = new RollbackUnitOfWork(() =>
        {
            persistedSessionStatus = (int)EventSessionStatusEnum.Approved;
            persistedSessionCount = 0;
        });
        var handler = new PublishEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreatePolicyProvider(CreateSessionPublishPolicy()),
            new EventLifecycleReadinessEvaluator(),
            unitOfWork,
            cache,
            new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new PublishEventSessionCommand
        {
            Id = outerSession.Id,
            Request = new PublishEventSessionRequestDto { ExpectedConcurrencyStamp = outerSession.ConcurrencyStamp }
        }, CancellationToken.None));

        await Assert.That(persistedSessionStatus).IsEqualTo((int)EventSessionStatusEnum.Approved);
        await Assert.That(persistedSessionCount).IsEqualTo(0);
        await Assert.That(unitOfWork.RolledBack).IsTrue();
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WhenCommitAmbiguityRetryObservesPublishedSession_InvalidatesFinalIdentityOnceWithoutDuplicateWrites()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        Explore.Domain.Event outerParent = CreateEvent(EventStatusEnum.Published);
        Explore.Domain.Event firstAttemptParent = CreateEvent(EventStatusEnum.Published);
        firstAttemptParent.Id = outerParent.Id;
        firstAttemptParent.TenantId = outerParent.TenantId;
        Explore.Domain.Event retryParent = CreateEvent(EventStatusEnum.Published);
        retryParent.Id = outerParent.Id;
        retryParent.TenantId = outerParent.TenantId;
        EventSession outerSession = CreateSession(outerParent, EventSessionStatusEnum.Approved);
        outerSession.StartTime = Now.AddDays(1);
        outerSession.EndTime = Now.AddDays(1).AddHours(1);
        EventSession firstAttemptSession = CreateSession(firstAttemptParent, EventSessionStatusEnum.Approved);
        firstAttemptSession.Id = outerSession.Id;
        firstAttemptSession.ConcurrencyStamp = outerSession.ConcurrencyStamp;
        firstAttemptSession.StartTime = Now.AddDays(1);
        firstAttemptSession.EndTime = Now.AddDays(1).AddHours(1);
        firstAttemptParent.Sessions.Add(firstAttemptSession);
        EventSession retrySession = CreateSession(retryParent, EventSessionStatusEnum.Published);
        retrySession.Id = outerSession.Id;
        retrySession.ConcurrencyStamp = Guid.NewGuid();
        eventSessionRepository.GetById(outerSession.Id).Returns(outerSession);
        eventSessionRepository.GetByIdForEventAsync(
            outerSession.Id,
            outerParent.Id,
            outerParent.TenantId,
            Arg.Any<CancellationToken>()).Returns(firstAttemptSession, retrySession);
        eventRepository.GetById(outerParent.Id).Returns(outerParent, firstAttemptParent, retryParent);
        eventRepository.GetScheduleGraphForUpdateAsync(outerParent.Id, Arg.Any<CancellationToken>()).Returns(firstAttemptParent);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>();
                await operation(call.Arg<CancellationToken>());
                return await operation(call.Arg<CancellationToken>());
            });
        var handler = new PublishEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreatePolicyProvider(CreateSessionPublishPolicy()),
            new EventLifecycleReadinessEvaluator(),
            unitOfWork,
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new PublishEventSessionCommand
        {
            Id = outerSession.Id,
            Request = new PublishEventSessionRequestDto { ExpectedConcurrencyStamp = outerSession.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventSessionRepository.Received(1).Update(firstAttemptSession);
        await eventSessionRepository.DidNotReceive().Update(retrySession);
        await eventRepository.Received(1).Update(firstAttemptParent);
        await cache.Received(1).RemoveAsync($"event:detail:{outerParent.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(outerParent.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WhenRetryReloadHasDifferentStamp_ReturnsStableConflictWithoutSecondMutationOrCacheInvalidation()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        Explore.Domain.Event outerParent = CreateEvent(EventStatusEnum.Published);
        Explore.Domain.Event firstAttemptParent = CreateEvent(EventStatusEnum.Published);
        firstAttemptParent.Id = outerParent.Id;
        firstAttemptParent.TenantId = outerParent.TenantId;
        EventSession outerSession = CreateSession(outerParent, EventSessionStatusEnum.Approved);
        outerSession.StartTime = Now.AddDays(1);
        outerSession.EndTime = Now.AddDays(1).AddHours(1);
        EventSession firstAttemptSession = CreateSession(firstAttemptParent, EventSessionStatusEnum.Approved);
        firstAttemptSession.Id = outerSession.Id;
        firstAttemptSession.ConcurrencyStamp = outerSession.ConcurrencyStamp;
        firstAttemptSession.StartTime = Now.AddDays(1);
        firstAttemptSession.EndTime = Now.AddDays(1).AddHours(1);
        EventSession retrySession = CreateSession(firstAttemptParent, EventSessionStatusEnum.Approved);
        retrySession.Id = outerSession.Id;
        retrySession.ConcurrencyStamp = Guid.NewGuid();
        eventSessionRepository.GetById(outerSession.Id).Returns(outerSession);
        eventSessionRepository.GetByIdForEventAsync(
            outerSession.Id,
            outerParent.Id,
            outerParent.TenantId,
            Arg.Any<CancellationToken>()).Returns(firstAttemptSession, retrySession);
        eventRepository.GetById(outerParent.Id).Returns(outerParent, firstAttemptParent);
        eventSessionRepository.When(repository => repository.Update(firstAttemptSession))
            .Do(_ => throw new TimeoutException("Simulated transient database failure."));
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>();
                try
                {
                    return await operation(call.Arg<CancellationToken>());
                }
                catch (TimeoutException)
                {
                    return await operation(call.Arg<CancellationToken>());
                }
            });
        var handler = new PublishEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreatePolicyProvider(CreateSessionPublishPolicy()),
            new EventLifecycleReadinessEvaluator(),
            unitOfWork,
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new PublishEventSessionCommand
        {
            Id = outerSession.Id,
            Request = new PublishEventSessionRequestDto { ExpectedConcurrencyStamp = outerSession.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_publish_concurrency_conflict");
        await eventSessionRepository.Received(1).Update(firstAttemptSession);
        await eventSessionRepository.DidNotReceive().Update(retrySession);
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WhenParentStopsBeingPublishedBetweenAttempts_ReevaluatesReadinessBeforeSecondMutation()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        Explore.Domain.Event outerParent = CreateEvent(EventStatusEnum.Published);
        Explore.Domain.Event firstAttemptParent = CreateEvent(EventStatusEnum.Published);
        firstAttemptParent.Id = outerParent.Id;
        firstAttemptParent.TenantId = outerParent.TenantId;
        Explore.Domain.Event retryParent = CreateEvent(EventStatusEnum.Draft);
        retryParent.Id = outerParent.Id;
        retryParent.TenantId = outerParent.TenantId;
        EventSession outerSession = CreateSession(outerParent, EventSessionStatusEnum.Approved);
        outerSession.StartTime = Now.AddDays(1);
        outerSession.EndTime = Now.AddDays(1).AddHours(1);
        EventSession firstAttemptSession = CreateSession(firstAttemptParent, EventSessionStatusEnum.Approved);
        firstAttemptSession.Id = outerSession.Id;
        firstAttemptSession.ConcurrencyStamp = outerSession.ConcurrencyStamp;
        firstAttemptSession.StartTime = outerSession.StartTime;
        firstAttemptSession.EndTime = outerSession.EndTime;
        EventSession retrySession = CreateSession(retryParent, EventSessionStatusEnum.Approved);
        retrySession.Id = outerSession.Id;
        retrySession.ConcurrencyStamp = outerSession.ConcurrencyStamp;
        retrySession.StartTime = outerSession.StartTime;
        retrySession.EndTime = outerSession.EndTime;
        eventSessionRepository.GetById(outerSession.Id).Returns(outerSession);
        eventSessionRepository.GetByIdForEventAsync(
            outerSession.Id,
            outerParent.Id,
            outerParent.TenantId,
            Arg.Any<CancellationToken>()).Returns(firstAttemptSession, retrySession);
        eventRepository.GetById(outerParent.Id).Returns(outerParent, firstAttemptParent, retryParent);
        eventSessionRepository.When(repository => repository.Update(firstAttemptSession))
            .Do(_ => throw new TimeoutException("Simulated transient database failure."));
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>();
                try
                {
                    return await operation(call.Arg<CancellationToken>());
                }
                catch (TimeoutException)
                {
                    return await operation(call.Arg<CancellationToken>());
                }
            });
        var handler = new PublishEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreatePolicyProvider(CreateSessionPublishPolicy()),
            new EventLifecycleReadinessEvaluator(),
            unitOfWork,
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new PublishEventSessionCommand
        {
            Id = outerSession.Id,
            Request = new PublishEventSessionRequestDto { ExpectedConcurrencyStamp = outerSession.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_publish_readiness_failed");
        await eventSessionRepository.Received(1).Update(firstAttemptSession);
        await eventSessionRepository.DidNotReceive().Update(retrySession);
        await Assert.That(retrySession.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Approved);
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WhenPersistenceReportsConcurrencyConflict_ReturnsStableConflictResponse()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Approved);
        session.StartTime = Now.AddDays(1);
        session.EndTime = Now.AddDays(1).AddHours(1);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>(
                new ConcurrencyConflictException(
                    ConcurrencyConflictException.ConcurrentUpdate,
                    "The event session was modified by another request.")));
        var cache = Substitute.For<HybridCache>();
        var handler = new PublishEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreatePolicyProvider(CreateSessionPublishPolicy()),
            new EventLifecycleReadinessEvaluator(),
            unitOfWork,
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new PublishEventSessionCommand
        {
            Id = session.Id,
            Request = new PublishEventSessionRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_publish_concurrency_conflict");
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new PublishEventSessionCommand
        {
            Id = session.Id,
            Request = new PublishEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_publish_readiness_failed");
        await Assert.That(result.Errors!.Any(error => error.Contains("Parent event must be published"))).IsTrue();
        await eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
    }

    [Test]
    [Arguments("cancel", EventSessionStatusEnum.Published, "event_session_cancel_concurrency_conflict")]
    [Arguments("complete", EventSessionStatusEnum.Published, "event_session_complete_concurrency_conflict")]
    [Arguments("archive", EventSessionStatusEnum.Cancelled, "event_session_archive_concurrency_conflict")]
    public async Task LifecycleTransition_WhenSaveReportsConcurrencyConflict_ReturnsHandlerConflictWithoutCacheOrHooks(
        string transition,
        EventSessionStatusEnum initialStatus,
        string expectedFailureCode)
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var scheduler = Substitute.For<IEventLifecycleScheduler>();
        var fanout = new FanoutFixture();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, initialStatus);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventSessionRepository.GetByIdForEventAsync(
            session.Id,
            parentEvent.Id,
            parentEvent.TenantId,
            Arg.Any<CancellationToken>()).Returns(session);
        eventSessionRepository.Update(session)
            .Returns(Task.FromException(new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The event session was modified while saving.")));
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        IUnitOfWork unitOfWork = CreateUnitOfWork();

        BaseCommandResponse<Guid> result = transition switch
        {
            "cancel" => await new CancelEventSessionCommandHandler(
                eventSessionRepository,
                eventRepository,
                unitOfWork,
                cache,
                fanout.Coordinator,
                scheduler,
                new FixedTimeProvider(Now)).Handle(new CancelEventSessionCommand
                {
                    Id = session.Id,
                    Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
                }, CancellationToken.None),
            "complete" => await new CompleteEventSessionCommandHandler(
                eventSessionRepository,
                eventRepository,
                unitOfWork,
                cache,
                new FixedTimeProvider(Now)).Handle(new CompleteEventSessionCommand
                {
                    Id = session.Id,
                    Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
                }, CancellationToken.None),
            "archive" => await new ArchiveEventSessionCommandHandler(
                eventSessionRepository,
                eventRepository,
                unitOfWork,
                cache,
                new FixedTimeProvider(Now)).Handle(new ArchiveEventSessionCommand
                {
                    Id = session.Id,
                    Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
                }, CancellationToken.None),
            _ => throw new InvalidOperationException($"Unsupported lifecycle transition '{transition}'.")
        };

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event session was modified by another request.");
        await Assert.That(result.Errors).IsEquivalentTo(["Refresh the event session and try again."]);
        await Assert.That(result.FailureCode).IsEqualTo(expectedFailureCode);
        await eventSessionRepository.Received(1).GetByIdForEventAsync(
            session.Id,
            parentEvent.Id,
            parentEvent.TenantId,
            Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await scheduler.DidNotReceive().ReprojectEventRemindersInCurrentTransactionAsync(
            Arg.Any<EventReminderReprojectionInput>(),
            Arg.Any<CancellationToken>());
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
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
        eventSessionRepository.GetByIdForEventAsync(
            session.Id,
            parentEvent.Id,
            parentEvent.TenantId,
            Arg.Any<CancellationToken>()).Returns(session);
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
            Substitute.For<IEventLifecycleScheduler>(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CancelEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
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
        eventSessionRepository.GetByIdForEventAsync(
            session.Id,
            parentEvent.Id,
            parentEvent.TenantId,
            Arg.Any<CancellationToken>()).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        var fanout = new FanoutFixture();
        var handler = new CancelEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            fanout.Coordinator,
            Substitute.For<IEventLifecycleScheduler>(),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CancelEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Cancelled);
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
    }

    [Test]
    public async Task Cancel_WhenSessionAlreadyCancelledAndStampIsStale_CreatesNoWork()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Cancelled);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var fanout = new FanoutFixture();
        var scheduler = Substitute.For<IEventLifecycleScheduler>();
        var unitOfWork = CreateUnitOfWork();
        var handler = new CancelEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            unitOfWork,
            cache,
            fanout.Coordinator,
            scheduler,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CancelEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = Guid.NewGuid() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event session is already cancelled.");
        await eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
        await unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await scheduler.DidNotReceive().ReprojectEventRemindersInCurrentTransactionAsync(
            Arg.Any<EventReminderReprojectionInput>(),
            Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Complete_WhenRetryConcurrencyChanges_RevalidatesBeforeSecondMutation()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        Explore.Domain.Event outerParent = CreateEvent(EventStatusEnum.Published);
        Explore.Domain.Event firstAttemptParent = CreateEvent(EventStatusEnum.Published);
        firstAttemptParent.Id = outerParent.Id;
        firstAttemptParent.TenantId = outerParent.TenantId;
        EventSession outerSession = CreateSession(outerParent, EventSessionStatusEnum.Published);
        EventSession firstAttemptSession = CreateSession(firstAttemptParent, EventSessionStatusEnum.Published);
        firstAttemptSession.Id = outerSession.Id;
        firstAttemptSession.ConcurrencyStamp = outerSession.ConcurrencyStamp;
        EventSession retrySession = CreateSession(firstAttemptParent, EventSessionStatusEnum.Published);
        retrySession.Id = outerSession.Id;
        retrySession.ConcurrencyStamp = Guid.NewGuid();
        eventSessionRepository.GetById(outerSession.Id).Returns(outerSession);
        eventSessionRepository.GetByIdForEventAsync(
            outerSession.Id,
            outerParent.Id,
            outerParent.TenantId,
            Arg.Any<CancellationToken>()).Returns(firstAttemptSession, retrySession);
        eventRepository.GetById(outerParent.Id).Returns(outerParent, firstAttemptParent);
        eventSessionRepository
            .When(repository => repository.Update(firstAttemptSession))
            .Do(_ => throw new TimeoutException("Simulated transient database failure."));
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                try
                {
                    return await operation(call.Arg<CancellationToken>());
                }
                catch (TimeoutException)
                {
                    return await operation(call.Arg<CancellationToken>());
                }
            });
        var handler = new CompleteEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            unitOfWork,
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CompleteEventSessionCommand
        {
            Id = outerSession.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = outerSession.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_complete_concurrency_conflict");
        await eventSessionRepository.Received(1).GetById(outerSession.Id);
        await eventSessionRepository.Received(2).GetByIdForEventAsync(
            outerSession.Id,
            outerParent.Id,
            outerParent.TenantId,
            Arg.Any<CancellationToken>());
        await eventRepository.Received(2).GetById(outerParent.Id);
        await eventSessionRepository.Received(1).Update(firstAttemptSession);
        await eventSessionRepository.DidNotReceive().Update(retrySession);
        await eventRepository.DidNotReceive().GetScheduleGraphForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Complete_WhenParentChangesBetweenAttempts_RevalidatesRetryParentBeforeMutation()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        Explore.Domain.Event outerParent = CreateEvent(EventStatusEnum.Published);
        Explore.Domain.Event firstAttemptParent = CreateEvent(EventStatusEnum.Published);
        firstAttemptParent.Id = outerParent.Id;
        firstAttemptParent.TenantId = outerParent.TenantId;
        Explore.Domain.Event retryParent = CreateEvent(EventStatusEnum.Moderated);
        retryParent.Id = outerParent.Id;
        retryParent.TenantId = outerParent.TenantId;
        EventSession outerSession = CreateSession(outerParent, EventSessionStatusEnum.Published);
        EventSession firstAttemptSession = CreateSession(firstAttemptParent, EventSessionStatusEnum.Published);
        firstAttemptSession.Id = outerSession.Id;
        firstAttemptSession.ConcurrencyStamp = outerSession.ConcurrencyStamp;
        EventSession retrySession = CreateSession(retryParent, EventSessionStatusEnum.Published);
        retrySession.Id = outerSession.Id;
        retrySession.ConcurrencyStamp = outerSession.ConcurrencyStamp;
        eventSessionRepository.GetById(outerSession.Id).Returns(outerSession);
        eventSessionRepository.GetByIdForEventAsync(
            outerSession.Id,
            outerParent.Id,
            outerParent.TenantId,
            Arg.Any<CancellationToken>()).Returns(firstAttemptSession, retrySession);
        eventRepository.GetById(outerParent.Id).Returns(outerParent, firstAttemptParent, retryParent);
        eventSessionRepository
            .When(repository => repository.Update(firstAttemptSession))
            .Do(_ => throw new TimeoutException("Simulated transient database failure."));
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                try
                {
                    return await operation(call.Arg<CancellationToken>());
                }
                catch (TimeoutException)
                {
                    return await operation(call.Arg<CancellationToken>());
                }
            });
        var handler = new CompleteEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            unitOfWork,
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CompleteEventSessionCommand
        {
            Id = outerSession.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = outerSession.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_complete_invalid_status");
        await eventSessionRepository.Received(1).GetById(outerSession.Id);
        await eventSessionRepository.Received(2).GetByIdForEventAsync(
            outerSession.Id,
            outerParent.Id,
            outerParent.TenantId,
            Arg.Any<CancellationToken>());
        await eventRepository.Received(3).GetById(outerParent.Id);
        await eventSessionRepository.Received(1).Update(firstAttemptSession);
        await eventSessionRepository.DidNotReceive().Update(retrySession);
        await Assert.That(retrySession.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Published);
        await eventRepository.DidNotReceive().GetScheduleGraphForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Cancel_WhenCommitAmbiguityRetryObservesTargetState_InvalidatesCacheAndRunsDurableHooksOnce()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var scheduler = Substitute.For<IEventLifecycleScheduler>();
        Explore.Domain.Event outerParent = CreateEvent(EventStatusEnum.Published);
        Explore.Domain.Event firstAttemptParent = CreateEvent(EventStatusEnum.Published);
        firstAttemptParent.Id = outerParent.Id;
        firstAttemptParent.TenantId = outerParent.TenantId;
        Explore.Domain.Event retryParent = CreateEvent(EventStatusEnum.Published);
        retryParent.Id = outerParent.Id;
        retryParent.TenantId = outerParent.TenantId;
        EventSession outerSession = CreateSession(outerParent, EventSessionStatusEnum.Published);
        EventSession firstAttemptSession = CreateSession(firstAttemptParent, EventSessionStatusEnum.Published);
        firstAttemptSession.Id = outerSession.Id;
        firstAttemptSession.ConcurrencyStamp = outerSession.ConcurrencyStamp;
        firstAttemptSession.StartTime = Now.AddDays(1);
        firstAttemptSession.EndTime = Now.AddDays(1).AddHours(1);
        firstAttemptParent.Sessions.Add(firstAttemptSession);
        EventSession retrySession = CreateSession(retryParent, EventSessionStatusEnum.Cancelled);
        retrySession.Id = outerSession.Id;
        retrySession.ConcurrencyStamp = Guid.NewGuid();
        eventSessionRepository.GetById(outerSession.Id).Returns(outerSession);
        eventSessionRepository.GetByIdForEventAsync(
            outerSession.Id,
            outerParent.Id,
            outerParent.TenantId,
            Arg.Any<CancellationToken>()).Returns(firstAttemptSession, retrySession);
        eventRepository.GetById(outerParent.Id).Returns(outerParent, firstAttemptParent, retryParent);
        eventRepository.GetScheduleGraphForUpdateAsync(firstAttemptParent.Id, Arg.Any<CancellationToken>()).Returns(firstAttemptParent);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                await operation(call.Arg<CancellationToken>());
                return await operation(call.Arg<CancellationToken>());
            });
        var fanout = new FanoutFixture();
        var handler = new CancelEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            unitOfWork,
            cache,
            fanout.Coordinator,
            scheduler,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CancelEventSessionCommand
        {
            Id = outerSession.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = outerSession.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventSessionRepository.Received(1).GetById(outerSession.Id);
        await eventSessionRepository.Received(2).GetByIdForEventAsync(
            outerSession.Id,
            outerParent.Id,
            outerParent.TenantId,
            Arg.Any<CancellationToken>());
        await eventRepository.Received(3).GetById(outerParent.Id);
        await eventSessionRepository.Received(1).Update(firstAttemptSession);
        await eventSessionRepository.DidNotReceive().Update(retrySession);
        await eventRepository.Received(1).GetScheduleGraphForUpdateAsync(firstAttemptParent.Id, Arg.Any<CancellationToken>());
        await eventRepository.Received(1).Update(firstAttemptParent);
        await Assert.That(fanout.CreatedOccurrences).Count().IsEqualTo(1);
        await Assert.That(fanout.OutboxPointers).Count().IsEqualTo(1);
        await scheduler.Received(1).ReprojectEventRemindersInCurrentTransactionAsync(
            Arg.Any<EventReminderReprojectionInput>(),
            Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveAsync($"event:detail:{outerParent.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(outerParent.TenantId), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Complete_WhenSessionIsPublishedAndParentPublished_TransitionsToCompleted()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventSessionRepository.GetByIdForEventAsync(
            session.Id,
            parentEvent.Id,
            parentEvent.TenantId,
            Arg.Any<CancellationToken>()).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        var handler = new CompleteEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CompleteEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Completed);
        await Assert.That(session.UpdatedAt).IsEqualTo(Now.UtcDateTime);
        await eventSessionRepository.Received(1).Update(session);
    }

    [Test]
    public async Task Complete_WhenSessionIsNotCompletedAndStampIsStale_ReturnsConcurrencyConflictWithoutWork()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var handler = new CompleteEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            unitOfWork,
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CompleteEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = Guid.NewGuid() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_complete_concurrency_conflict");
        await eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Complete_WhenParentEventIsModerated_ReturnsInvalidStatusWithoutUpdating()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var parentEvent = CreateEvent(EventStatusEnum.Moderated);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventSessionRepository.GetByIdForEventAsync(
            session.Id,
            parentEvent.Id,
            parentEvent.TenantId,
            Arg.Any<CancellationToken>()).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        var handler = new CompleteEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CompleteEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
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
        eventSessionRepository.GetByIdForEventAsync(
            session.Id,
            parentEvent.Id,
            parentEvent.TenantId,
            Arg.Any<CancellationToken>()).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        var handler = new ArchiveEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new ArchiveEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Archived);
        await Assert.That(session.UpdatedAt).IsEqualTo(Now.UtcDateTime);
        await eventSessionRepository.Received(1).Update(session);
    }

    [Test]
    public async Task Complete_WhenSessionAlreadyCompletedAndStampIsStale_CreatesNoWork()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Completed);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var handler = new CompleteEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            unitOfWork,
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CompleteEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = Guid.NewGuid() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event session is already completed.");
        await eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
        await eventRepository.DidNotReceive().GetScheduleGraphForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Archive_WhenSessionAlreadyArchivedAndStampIsStale_CreatesNoWork()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Archived);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var handler = new ArchiveEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            unitOfWork,
            cache,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new ArchiveEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = Guid.NewGuid() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event session is already archived.");
        await eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
        await eventRepository.DidNotReceive().GetScheduleGraphForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(EventSessionStatusEnum.Cancelled)]
    [Arguments(EventSessionStatusEnum.Moderated)]
    public async Task Schedule_WhenLifecycleDisallowsReschedule_ReturnsSanitizedFailureWithoutSideEffects(
        EventSessionStatusEnum status)
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var eventDayRepository = Substitute.For<IEventDayRepository>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        var readinessEvaluator = Substitute.For<IEventLifecycleReadinessEvaluator>();
        var cache = Substitute.For<HybridCache>();
        var scheduler = Substitute.For<IEventLifecycleScheduler>();
        var fanout = new FanoutFixture();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        var session = CreateSession(parentEvent, status);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            eventDayRepository,
            new EventScheduleProjectionCalculator(),
            policyProvider,
            readinessEvaluator,
            CreateUnitOfWork(),
            cache,
            fanout.Coordinator,
            scheduler,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = session.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp,
                StartTime = Now.AddDays(1),
                EndTime = Now.AddDays(1).AddHours(1)
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_schedule_invalid_status");
        await Assert.That(result.Message).IsEqualTo("Event session cannot be scheduled from its current lifecycle state.");
        await Assert.That(result.Errors).IsEquivalentTo(["Event session cannot be scheduled from its current lifecycle state."]);
        await policyProvider.DidNotReceive().GetEffectivePolicyAsync(
            Arg.Any<Guid?>(),
            Arg.Any<ValidationProfile>(),
            Arg.Any<CancellationToken>());
        readinessEvaluator.DidNotReceive().Evaluate(
            Arg.Any<EventSession>(),
            Arg.Any<Explore.Domain.Event?>(),
            Arg.Any<ValidationProfile>(),
            Arg.Any<EventLifecyclePolicy>());
        await eventDayRepository.DidNotReceive().FindByEventAndLocalDateAsync(
            Arg.Any<Guid>(),
            Arg.Any<DateOnly>(),
            Arg.Any<CancellationToken>());
        await eventSessionRepository.DidNotReceive().UpdateWithRoomOverlapGuardAsync(
            Arg.Any<EventSession>(),
            Arg.Any<CancellationToken>());
        await eventRepository.DidNotReceive().GetScheduleGraphForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await scheduler.DidNotReceive().ReprojectEventRemindersInCurrentTransactionAsync(
            Arg.Any<EventReminderReprojectionInput>(),
            Arg.Any<CancellationToken>());
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(EventSessionStatusEnum.Rejected)]
    [Arguments(EventSessionStatusEnum.Cancelled)]
    [Arguments(EventSessionStatusEnum.Archived)]
    [Arguments(EventSessionStatusEnum.Completed)]
    [Arguments(EventSessionStatusEnum.Moderated)]
    public async Task Schedule_WhenLifecycleDisallowsRescheduleAndTimesAreEqual_ReturnsInvalidStatus(
        EventSessionStatusEnum status)
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        var readinessEvaluator = Substitute.For<IEventLifecycleReadinessEvaluator>();
        var scheduler = Substitute.For<IEventLifecycleScheduler>();
        var fanout = new FanoutFixture();
        Explore.Domain.Event parentEvent = CreateEvent(EventStatusEnum.Published);
        EventSession session = CreateSession(parentEvent, status);
        session.StartTime = Now.AddDays(1);
        session.EndTime = Now.AddDays(1).AddHours(1);
        eventSessionRepository.GetById(session.Id).Returns(session);
        var unitOfWork = CreateUnitOfWork();
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            Substitute.For<IEventDayRepository>(),
            new EventScheduleProjectionCalculator(),
            policyProvider,
            readinessEvaluator,
            unitOfWork,
            cache,
            fanout.Coordinator,
            scheduler,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new ScheduleEventSessionCommand
        {
            Id = session.Id,
            Request = new ScheduleEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = Guid.NewGuid(),
                StartTime = session.StartTime.Value,
                EndTime = session.EndTime.Value
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_schedule_invalid_status");
        await eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await unitOfWork.DidNotReceive().ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
            Arg.Any<CancellationToken>());
        await policyProvider.DidNotReceive().GetEffectivePolicyAsync(
            Arg.Any<Guid?>(),
            Arg.Any<ValidationProfile>(),
            Arg.Any<CancellationToken>());
        readinessEvaluator.DidNotReceive().Evaluate(
            Arg.Any<EventSession>(),
            Arg.Any<Explore.Domain.Event?>(),
            Arg.Any<ValidationProfile>(),
            Arg.Any<EventLifecyclePolicy>());
        await eventSessionRepository.DidNotReceive().UpdateWithRoomOverlapGuardAsync(
            Arg.Any<EventSession>(),
            Arg.Any<CancellationToken>());
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
        await scheduler.DidNotReceive().ReprojectEventRemindersInCurrentTransactionAsync(
            Arg.Any<EventReminderReprojectionInput>(),
            Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>();
                (BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId) response =
                    await operation(call.Arg<CancellationToken>());
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
        unitOfWork
            .ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId)>>>();
                (BaseCommandResponse<Guid> Response, Guid? ParentEventId, Guid? TenantId) response = await operation(call.Arg<CancellationToken>());
                onCompleted?.Invoke();
                return response;
            });

        return unitOfWork;
    }

    private sealed class RollbackUnitOfWork(Action rollback) : IUnitOfWork
    {
        public bool RolledBack { get; private set; }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
            ExecuteInTransactionAsync<object?>(async token =>
            {
                await operation(token);
                return null;
            }, ct);

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            try
            {
                return await operation(ct);
            }
            catch
            {
                rollback();
                RolledBack = true;
                throw;
            }
        }

        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            ExecuteInTransactionAsync(operation, ct);
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

    private static Explore.Domain.Event CreateEvent(EventStatusEnum status) => new(status)
    {
        Id = Guid.NewGuid(),
        Title = "Parent event",
        ActorId = Guid.NewGuid(),
        Actor = null!,
        TenantId = Guid.NewGuid(),
        Tenant = null!,
        VisibilityTypeId = (int)VisibilityTypeEnum.Public,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormatId = (int)EventFormatEnum.Local,
        EventFormat = null!
    };

    private static EventSession CreateSession(Explore.Domain.Event parentEvent, EventSessionStatusEnum status) => new(status)
    {
        Id = Guid.NewGuid(),
        EventId = parentEvent.Id,
        Event = null!,
        TenantId = parentEvent.TenantId,
        Tenant = null!,
        Title = "Lifecycle session",
        ConcurrencyStamp = Guid.NewGuid()
    };
}
