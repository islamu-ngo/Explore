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
    public async Task ArchiveEvent_WhenConcurrencyMatches_ArchivesEventAndInvalidatesCaches()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var eventEntity = CreateEvent(EventStatusEnum.Published);
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
                federationOutbox));

        var result = await handler.Handle(new ArchiveEventCommand
        {
            Id = eventEntity.Id,
            Request = new ArchiveEventRequestDto { ExpectedConcurrencyStamp = eventEntity.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
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
            AtprotoPublicationPlannerTestFactory.Disabled());

        var result = await handler.Handle(new ArchiveEventCommand
        {
            Id = eventEntity.Id,
            Request = new ArchiveEventRequestDto { ExpectedConcurrencyStamp = Guid.CreateVersion7() }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
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
        bool cacheObservedCommit = false;
        var unitOfWork = CreateUnitOfWork(() => transactionCompleted = true);
        cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cacheObservedCommit = transactionCompleted;
                return ValueTask.CompletedTask;
            });
        var fanout = new FanoutFixture(() => occurrenceCreatedBeforeCommit = !transactionCompleted);
        var handler = new CancelEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            fanout.Coordinator,
            Substitute.For<IEventLifecycleScheduler>(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CancelEventCommand
        {
            Id = eventEntity.Id,
            Request = new CancelEventRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
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
        await Assert.That(occurrenceCreatedBeforeCommit).IsTrue();
        await Assert.That(cacheObservedCommit).IsTrue();
        await cache.Received(1).RemoveAsync($"event:detail:{eventEntity.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(eventEntity.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelEvent_WhenAlreadyCancelled_ReturnsFailureWithoutUpdating()
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
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CancelEventCommand
        {
            Id = eventEntity.Id,
            Request = new CancelEventRequestDto { ExpectedConcurrencyStamp = eventEntity.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_cancel_already_cancelled");
        await eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await Assert.That(fanout.CreatedOccurrences).IsEmpty();
        await Assert.That(fanout.OutboxPointers).IsEmpty();
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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

    private static Explore.Domain.Event CreateEvent(EventStatusEnum status) => new()
    {
        Id = Guid.CreateVersion7(),
        Title = "Lifecycle event",
        ActorId = Guid.CreateVersion7(),
        Actor = null!,
        TenantId = Guid.CreateVersion7(),
        Tenant = null!,
        VisibilityTypeId = (int)VisibilityTypeEnum.Public,
        VisibilityType = null!,
        EventStatusId = (int)status,
        EventStatus = null!,
        EventFormatId = (int)EventFormatEnum.Local,
        EventFormat = null!,
        ConcurrencyStamp = Guid.CreateVersion7()
    };
}
