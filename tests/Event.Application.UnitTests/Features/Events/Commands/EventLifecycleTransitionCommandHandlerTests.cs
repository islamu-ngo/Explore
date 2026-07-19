// ABOUTME: Unit tests for explicit event lifecycle transition command handlers.
// ABOUTME: Verifies archive/cancel transitions preserve concurrency gates, cache invalidation, and status side effects.

using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
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
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var eventEntity = CreateEvent(EventStatusEnum.Published);
        eventRepository.GetById(eventEntity.Id).Returns(eventEntity);
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());
        var handler = new CancelEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled());

        var result = await handler.Handle(new CancelEventCommand
        {
            Id = eventEntity.Id,
            Request = new CancelEventRequestDto { ExpectedConcurrencyStamp = eventEntity.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(eventEntity.EventStatusId).IsEqualTo((int)EventStatusEnum.Cancelled);
        await eventRepository.Received(1).Update(eventEntity);
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
        var handler = new CancelEventCommandHandler(
            eventRepository,
            unitOfWork,
            cache,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled());

        var result = await handler.Handle(new CancelEventCommand
        {
            Id = eventEntity.Id,
            Request = new CancelEventRequestDto { ExpectedConcurrencyStamp = eventEntity.ConcurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_cancel_already_cancelled");
        await eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    private static IUnitOfWork CreateUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()(CancellationToken.None));
        return unitOfWork;
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
