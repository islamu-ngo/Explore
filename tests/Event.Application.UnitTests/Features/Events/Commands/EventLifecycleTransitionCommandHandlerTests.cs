// ABOUTME: Unit tests for explicit event lifecycle transition command handlers.
// ABOUTME: Verifies archive/cancel transitions preserve concurrency gates, cache invalidation, and status side effects.

using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Commands;

public sealed class EventLifecycleTransitionCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 18, 30, 0, TimeSpan.Zero);

    [Test]
    public async Task ArchiveEvent_WhenCancelledAndConcurrencyMatches_ArchivesEventAndInvalidatesCaches()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var eventEntity = CreateEvent(EventStatusEnum.Cancelled);
        eventRepository.GetById(eventEntity.Id).Returns(eventEntity);
        var userContext = Substitute.For<IUserContext>();
        Guid ownerUserId = Guid.CreateVersion7();
        userContext.GetRequiredUserId().Returns(ownerUserId);
        var federationOutbox = Substitute.For<IPdsSyncOutboxRepository>();
        var handler = new ArchiveEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.ExistingEventDelete(
                eventEntity.TenantId,
                eventEntity.Id,
                ownerUserId,
                federationOutbox),
            TimeProvider.System);

        var result = await handler.Handle(new ArchiveEventCommand
        {
            Id = eventEntity.Id,
            Request = new ArchiveEventRequestDto { ExpectedConcurrencyStamp = eventEntity.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(eventEntity.EventStatusId).IsEqualTo((int)EventStatusEnum.Archived);
        await eventRepository.Received(1).Update(eventEntity);
        await federationOutbox.Received(1).AddAsync(
            Arg.Is<PdsSyncOutbox>(outbox =>
                outbox.Operation == PdsSyncOperation.Delete
                && outbox.RecordKey == "stable-lifecycle-key"),
            Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveAsync($"event:detail:{eventEntity.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(eventEntity.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ArchiveEvent_WhenPublished_ReturnsStableFailureWithoutMutation()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var eventEntity = CreateEvent(EventStatusEnum.Published);
        eventRepository.GetById(eventEntity.Id).Returns(eventEntity);
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());
        var handler = new ArchiveEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            TimeProvider.System);

        var result = await handler.Handle(new ArchiveEventCommand
        {
            Id = eventEntity.Id,
            Request = new ArchiveEventRequestDto { ExpectedConcurrencyStamp = eventEntity.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_archive_transition_not_allowed");
        await Assert.That(eventEntity.EventStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ArchiveEvent_WhenAlreadyArchived_ReturnsSuccessWithoutMutationSideEffects()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var eventEntity = CreateEvent(EventStatusEnum.Archived);
        eventRepository.GetById(eventEntity.Id).Returns(eventEntity);
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());
        var handler = new ArchiveEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            TimeProvider.System);

        var result = await handler.Handle(new ArchiveEventCommand
        {
            Id = eventEntity.Id,
            Request = new ArchiveEventRequestDto { ExpectedConcurrencyStamp = Guid.CreateVersion7() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(eventEntity.EventStatusId).IsEqualTo((int)EventStatusEnum.Archived);
        await unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        userContext.DidNotReceive().GetRequiredUserId();
    }

    [Test]
    public async Task ArchiveEvent_WhenConcurrencyDiffers_ReturnsConflictWithoutUpdating()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var eventEntity = CreateEvent(EventStatusEnum.Published);
        eventRepository.GetById(eventEntity.Id).Returns(eventEntity);
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());
        var handler = new ArchiveEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            TimeProvider.System);

        var result = await handler.Handle(new ArchiveEventCommand
        {
            Id = eventEntity.Id,
            Request = new ArchiveEventRequestDto { ExpectedConcurrencyStamp = Guid.CreateVersion7() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_archive_concurrency_conflict");
        await Assert.That(eventEntity.EventStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task CancelEvent_WhenConcurrencyMatches_CancelsEventAndInvalidatesCaches()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var eventEntity = CreateEvent(EventStatusEnum.Published);
        Guid expectedConcurrencyStamp = eventEntity.ConcurrencyStamp;
        eventRepository.GetById(eventEntity.Id).Returns(eventEntity);
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());
        bool transactionCompleted = false;
        bool occurrenceCreatedBeforeCommit = false;
        bool campaignCreatedBeforeCommit = false;
        bool cacheObservedCommit = false;
        var unitOfWork = CreateUnitOfWork(() => transactionCompleted = true);
        cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cacheObservedCommit = transactionCompleted;
                return ValueTask.CompletedTask;
            });
        var fanout = new FanoutFixture(() => occurrenceCreatedBeforeCommit = !transactionCompleted);
        var campaigns = Substitute.For<IRefundCampaignRepository>();
        var admissionOutbox = Substitute.For<IOutboxRepository>();
        campaigns.CreateAsync(Arg.Any<RefundCampaign>(), Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                campaignCreatedBeforeCommit = !transactionCompleted;
                return call.Arg<RefundCampaign>();
            });
        var handler = new CancelEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            fanout.Coordinator,
            Substitute.For<IEventLifecycleScheduler>(),
            campaigns,
            admissionOutbox,
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CancelEventCommand
        {
            Id = eventEntity.Id,
            Request = new CancelEventRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await admissionOutbox.Received(1).Create(
            Arg.Is<OutboxMessage>(message =>
                message.EventType == AdmissionRevocationOutboxMessageFactory.EventCancellationRequested &&
                message.AggregateId == eventEntity.Id));
        await Assert.That(eventEntity.EventStatusId).IsEqualTo((int)EventStatusEnum.Cancelled);
        await eventRepository.Received(1).Update(eventEntity);
        await Assert.That(fanout.CreatedOccurrences).Count().IsEqualTo(1);
        await Assert.That(fanout.OutboxPointers).Count().IsEqualTo(1);
        NotificationFanoutOccurrence occurrence = fanout.CreatedOccurrences[0];
        await Assert.That(occurrence.TenantId).IsEqualTo(eventEntity.TenantId);
        await Assert.That(occurrence.EventId).IsEqualTo(eventEntity.Id);
        await Assert.That(occurrence.SessionId).IsNull();
        await Assert.That(occurrence.TemplateKey).IsEqualTo(NotificationFanoutRecipientTemplateFactory.EventCancelledTemplateKey);
        await Assert.That(occurrence.AggregateVersion).IsEqualTo(expectedConcurrencyStamp);
        await Assert.That(occurrence.AudienceCutoffAt).IsEqualTo(Now.UtcDateTime);
        await Assert.That(campaignCreatedBeforeCommit).IsTrue();
        await campaigns.Received(1).CreateAsync(
            Arg.Is<RefundCampaign>(campaign =>
                campaign.TenantId == eventEntity.TenantId && campaign.EventId == eventEntity.Id),
            Arg.Is<OutboxMessage>(message => message.EventType == RefundOutboxMessageFactory.CampaignProcessRequested),
            Arg.Any<CancellationToken>());
        await Assert.That(occurrenceCreatedBeforeCommit).IsTrue();
        await Assert.That(cacheObservedCommit).IsTrue();
        await cache.Received(1).RemoveAsync($"event:detail:{eventEntity.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(eventEntity.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelEvent_WhenAlreadyCancelled_ReturnsSuccessWithoutMutationSideEffects()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var eventEntity = CreateEvent(EventStatusEnum.Cancelled);
        eventRepository.GetById(eventEntity.Id).Returns(eventEntity);
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());
        var fanout = new FanoutFixture();
        var handler = new CancelEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            fanout.Coordinator,
            Substitute.For<IEventLifecycleScheduler>(),
            Substitute.For<IRefundCampaignRepository>(),
            Substitute.For<IOutboxRepository>(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CancelEventCommand
        {
            Id = eventEntity.Id,
            Request = new CancelEventRequestDto { ExpectedConcurrencyStamp = Guid.CreateVersion7() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.FailureCode).IsNull();
        await unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        userContext.DidNotReceive().GetRequiredUserId();
    }

    [Test]
    public async Task ArchiveEvent_WhenRolledBackAttemptIsRetried_ReloadsAndStagesStableFederationIdentity()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid stamp = Guid.CreateVersion7();
        var outerEvent = CreateEvent(EventStatusEnum.Cancelled, eventId, tenantId, stamp);
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Cancelled, eventId, tenantId, stamp);
        var retryEvent = CreateEvent(EventStatusEnum.Cancelled, eventId, tenantId, stamp);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(outerEvent, firstAttemptEvent, retryEvent);
        var unitOfWork = CreateTwoAttemptUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var userContext = Substitute.For<IUserContext>();
        Guid userId = Guid.CreateVersion7();
        userContext.GetRequiredUserId().Returns(userId);
        var federationOutbox = Substitute.For<IPdsSyncOutboxRepository>();
        var stagedRows = new List<PdsSyncOutbox>();
        federationOutbox.AddAsync(Arg.Any<PdsSyncOutbox>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            stagedRows.Add(call.Arg<PdsSyncOutbox>());
            return Task.CompletedTask;
        });
        var handler = new ArchiveEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.ExistingEventDelete(tenantId, eventId, userId, federationOutbox),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new ArchiveEventCommand
        {
            Id = eventId,
            Request = new ArchiveEventRequestDto { ExpectedConcurrencyStamp = stamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventRepository.Received(1).Update(firstAttemptEvent);
        await eventRepository.Received(1).Update(retryEvent);
        await Assert.That(stagedRows).Count().IsEqualTo(2);
        await Assert.That(stagedRows.Select(row => row.Id).Distinct()).Count().IsEqualTo(1);
        await cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ArchiveEvent_WhenCommittedAttemptIsRetried_DoesNotStageDuplicateFederationWork()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid stamp = Guid.CreateVersion7();
        var outerEvent = CreateEvent(EventStatusEnum.Cancelled, eventId, tenantId, stamp);
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Cancelled, eventId, tenantId, stamp);
        var committedEvent = CreateEvent(EventStatusEnum.Archived, eventId, tenantId, Guid.CreateVersion7());
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(outerEvent, firstAttemptEvent, committedEvent);
        var unitOfWork = CreateTwoAttemptUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var userContext = Substitute.For<IUserContext>();
        Guid userId = Guid.CreateVersion7();
        userContext.GetRequiredUserId().Returns(userId);
        var federationOutbox = Substitute.For<IPdsSyncOutboxRepository>();
        var handler = new ArchiveEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.ExistingEventDelete(tenantId, eventId, userId, federationOutbox),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new ArchiveEventCommand
        {
            Id = eventId,
            Request = new ArchiveEventRequestDto { ExpectedConcurrencyStamp = stamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventRepository.Received(1).Update(firstAttemptEvent);
        await eventRepository.DidNotReceive().Update(committedEvent);
        await federationOutbox.Received(1).AddAsync(Arg.Any<PdsSyncOutbox>(), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelEvent_WhenRolledBackAttemptIsRetried_ReloadsAndRestagesDurableWork()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid stamp = Guid.CreateVersion7();
        var outerEvent = CreateEvent(EventStatusEnum.Published, eventId, tenantId, stamp);
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Published, eventId, tenantId, stamp);
        var retryEvent = CreateEvent(EventStatusEnum.Published, eventId, tenantId, stamp);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(outerEvent, firstAttemptEvent, retryEvent);
        var cache = Substitute.For<HybridCache>();
        var fanout = new FanoutFixture();
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());
        var handler = new CancelEventCommandHandler(
            eventRepository,
            CreateTwoAttemptUnitOfWork(),
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            fanout.Coordinator,
            Substitute.For<IEventLifecycleScheduler>(),
            Substitute.For<IRefundCampaignRepository>(),
            Substitute.For<IOutboxRepository>(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CancelEventCommand
        {
            Id = eventId,
            Request = new CancelEventRequestDto { ExpectedConcurrencyStamp = stamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventRepository.Received(1).Update(firstAttemptEvent);
        await eventRepository.Received(1).Update(retryEvent);
        await Assert.That(fanout.CreatedOccurrences).Count().IsEqualTo(2);
        await Assert.That(fanout.CreatedOccurrences.Select(row => row.Id).Distinct()).Count().IsEqualTo(1);
        await cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelEvent_WhenCommittedAttemptIsRetried_DoesNotStageDuplicateFanout()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid stamp = Guid.CreateVersion7();
        var outerEvent = CreateEvent(EventStatusEnum.Published, eventId, tenantId, stamp);
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Published, eventId, tenantId, stamp);
        var committedEvent = CreateEvent(EventStatusEnum.Cancelled, eventId, tenantId, Guid.CreateVersion7());
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(outerEvent, firstAttemptEvent, committedEvent);
        var cache = Substitute.For<HybridCache>();
        var fanout = new FanoutFixture();
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());
        var handler = new CancelEventCommandHandler(
            eventRepository,
            CreateTwoAttemptUnitOfWork(),
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            fanout.Coordinator,
            Substitute.For<IEventLifecycleScheduler>(),
            Substitute.For<IRefundCampaignRepository>(),
            Substitute.For<IOutboxRepository>(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CancelEventCommand
        {
            Id = eventId,
            Request = new CancelEventRequestDto { ExpectedConcurrencyStamp = stamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await eventRepository.Received(1).Update(firstAttemptEvent);
        await eventRepository.DidNotReceive().Update(committedEvent);
        await Assert.That(fanout.CreatedOccurrences).Count().IsEqualTo(1);
        await Assert.That(fanout.OutboxPointers).Count().IsEqualTo(1);
        await cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
    }

    private static IUnitOfWork CreateUnitOfWork(Action? onCompleted = null)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                BaseCommandResponse<Guid> response = await callInfo
                    .Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()(CancellationToken.None);
                onCompleted?.Invoke();
                return response;
            });
        return unitOfWork;
    }

    private static IUnitOfWork CreateTwoAttemptUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                await operation(CancellationToken.None);
                return await operation(CancellationToken.None);
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

    private static Explore.Domain.Event CreateEvent(
        EventStatusEnum status,
        Guid? id = null,
        Guid? tenantId = null,
        Guid? concurrencyStamp = null) => new(status)
        {
            Id = id ?? Guid.CreateVersion7(),
            Title = "Lifecycle event",
            ActorId = Guid.CreateVersion7(),
            Actor = null!,
            TenantId = tenantId ?? Guid.CreateVersion7(),
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            ConcurrencyStamp = concurrencyStamp ?? Guid.CreateVersion7()
        };
}
