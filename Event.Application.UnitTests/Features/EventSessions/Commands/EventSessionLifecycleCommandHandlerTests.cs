// ABOUTME: Unit tests for event-session draft, schedule, and publish lifecycle command handlers.
// ABOUTME: Verifies policy readiness, concurrency gates, schedule projection, guarded writes, and cache invalidation.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Handlers.Commands;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
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
    [Test]
    public async Task CreateDraft_WhenParentEventExists_CreatesUnscheduledDraftSession()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var parentEvent = CreateEvent(EventStatusEnum.Published);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventSessionRepository.Create(Arg.Any<EventSession>())
            .Returns(callInfo => callInfo.Arg<EventSession>());
        var handler = new CreateDraftEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreatePolicyProvider(CreateSessionDraftPolicy()),
            new EventLifecycleReadinessEvaluator(),
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
        var handler = new ScheduleEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            eventDayRepository,
            new EventScheduleProjectionCalculator(),
            CreatePolicyProvider(CreateSessionSchedulePolicy()),
            new EventLifecycleReadinessEvaluator(),
            cache);
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
            cache);
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
            Substitute.For<HybridCache>());

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
        var session = CreateSession(parentEvent, EventSessionStatusEnum.Published);
        parentEvent.Sessions.Add(session);
        eventSessionRepository.GetById(session.Id).Returns(session);
        eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        eventRepository.GetScheduleGraphForUpdateAsync(parentEvent.Id, Arg.Any<CancellationToken>()).Returns(parentEvent);
        var handler = new CancelEventSessionCommandHandler(
            eventSessionRepository,
            eventRepository,
            CreateUnitOfWork(),
            cache);

        var result = await handler.Handle(new CancelEventSessionCommand
        {
            Id = session.Id,
            Request = new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = session.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Cancelled);
        await eventSessionRepository.Received(1).Update(session);
        await eventRepository.Received(1).Update(parentEvent);
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), Arg.Any<CancellationToken>());
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

    private static IUnitOfWork CreateUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                return operation(CancellationToken.None);
            });

        return unitOfWork;
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
