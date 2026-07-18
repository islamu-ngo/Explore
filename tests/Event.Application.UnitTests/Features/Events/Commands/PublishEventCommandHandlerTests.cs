// ABOUTME: Unit tests for publish-event command handling.
// ABOUTME: Verifies lifecycle readiness, concurrency, outbox, and cache side effects.

using System.Text.Json;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services.Lifecycle;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Commands;

public class PublishEventCommandHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventLifecyclePolicyProvider _policyProvider;
    private readonly HybridCache _cache;
    private readonly PublishEventCommandHandler _handler;

    public PublishEventCommandHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        _cache = Substitute.For<HybridCache>();

        _policyProvider
            .GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(CreateEventPublishPolicy());

        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>();
                return operation(CancellationToken.None);
            });

        _outboxRepository.Create(Arg.Any<OutboxMessage>())
            .Returns(callInfo => callInfo.Arg<OutboxMessage>());

        _handler = new PublishEventCommandHandler(
            _eventRepository,
            _outboxRepository,
            _unitOfWork,
            _cache,
            _policyProvider,
            new EventLifecycleReadinessEvaluator());
    }

    [Test]
    public async Task Handle_WhenDraftEventIsReady_PublishesAndCreatesNotificationFanoutOutboxMessage()
    {
        var concurrencyStamp = Guid.NewGuid();
        var @event = CreateReadyEvent(concurrencyStamp);
        var createdMessages = new List<OutboxMessage>();
        _eventRepository.GetById(@event.Id).Returns(@event);
        _outboxRepository.Create(Arg.Any<OutboxMessage>())
            .Returns(callInfo =>
            {
                var message = callInfo.Arg<OutboxMessage>();
                createdMessages.Add(message);
                return message;
            });

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await _eventRepository.Received(1).Update(@event);
        await _outboxRepository.Received(1).Create(Arg.Is<OutboxMessage>(message =>
            message.AggregateType == "Event"
            && message.AggregateId == @event.Id
            && message.EventType == "EventPublishedNotificationFanoutRequested"
            && message.Status == OutboxMessageStatus.Pending
            && message.Payload != null));

        var fanoutMessage = createdMessages.Single();
        await Assert.That(fanoutMessage.EventType).IsEqualTo("EventPublishedNotificationFanoutRequested");
        var fanoutPayload = JsonSerializer.Deserialize<EventPublishedNotificationFanoutRequested>(fanoutMessage.Payload!);
        await Assert.That(fanoutPayload).IsNotNull();
        await Assert.That(fanoutPayload!.TenantId).IsEqualTo(@event.TenantId);
        await Assert.That(fanoutPayload.EventId).IsEqualTo(@event.Id);
        await Assert.That(fanoutPayload.EventTitle).IsEqualTo(@event.Title);
        await Assert.That(fanoutPayload.SourceActorId).IsEqualTo(@event.ActorId);
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenConcurrencyStampDoesNotMatch_ReturnsConflictFailure()
    {
        var @event = CreateReadyEvent(Guid.NewGuid());
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = Guid.NewGuid() }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_publish_concurrency_conflict");
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Handle_WhenEventIsMissingSchedule_ReturnsReadinessFailure()
    {
        var concurrencyStamp = Guid.NewGuid();
        var @event = CreateReadyEvent(concurrencyStamp);
        @event.FirstSessionStartUtc = null;
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_publish_readiness_failed");
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors![0]).Contains("scheduled session");
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WhenCommunityProfileAndScheduleIsMissing_PublishesUsingInternalSafetyFields()
    {
        var concurrencyStamp = Guid.NewGuid();
        var @event = CreateReadyEvent(concurrencyStamp);
        @event.FirstSessionStartUtc = null;
        @event.LastSessionStartUtc = null;
        _eventRepository.GetById(@event.Id).Returns(@event);
        _policyProvider
            .GetEffectivePolicyAsync(@event.TenantId, ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(CreateCommunityPublishPolicy());

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await _eventRepository.Received(1).Update(@event);
    }

    [Test]
    public async Task Handle_WhenCommunityProfileEventIsModerated_ReturnsReadinessFailureWithoutMutation()
    {
        var concurrencyStamp = Guid.NewGuid();
        var @event = CreateReadyEvent(concurrencyStamp);
        @event.EventStatusId = (int)EventStatusEnum.Moderated;
        _eventRepository.GetById(@event.Id).Returns(@event);
        _policyProvider
            .GetEffectivePolicyAsync(@event.TenantId, ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(CreateCommunityPublishPolicy());

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_publish_readiness_failed");
        await Assert.That(result.Errors).Contains(error => error.Contains("moderated", StringComparison.OrdinalIgnoreCase));
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    private static Explore.Domain.Event CreateReadyEvent(Guid concurrencyStamp) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Draft Event",
        ActorId = Guid.NewGuid(),
        Actor = new Actor
        {
            ActorType = new ActorType { Id = 1, FullName = "User", MasterCode = "user" },
            Tenant = CreateTenant(),
            Pii = new ActorPii { DisplayName = "Publisher" }
        },
        TenantId = Guid.NewGuid(),
        Tenant = CreateTenant(),
        VisibilityTypeId = 1,
        VisibilityType = new VisibilityType { Id = 1, FullName = "Public", MasterCode = "public" },
        EventStatusId = (int)EventStatusEnum.Draft,
        EventStatus = new EventStatus { Id = (int)EventStatusEnum.Draft, FullName = "Draft", MasterCode = "draft" },
        EventFormatId = 1,
        EventFormat = new EventFormat { Id = 1, FullName = "In person", MasterCode = "in_person" },
        FirstSessionStartUtc = DateTimeOffset.UtcNow.AddDays(1),
        LastSessionStartUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
        ConcurrencyStamp = concurrencyStamp
    };

    private static Tenant CreateTenant() => new()
    {
        FullName = "Test Tenant",
        Slug = "test",
        TenantStatus = null!
    };

    private static EventLifecyclePolicy CreateEventPublishPolicy() => new()
    {
        Profile = ValidationProfile.EventPublish,
        RequiredEventFields = new HashSet<Enum>
        {
            EventFieldKey.Title,
            EventFieldKey.Tenant,
            EventFieldKey.Owner,
            EventFieldKey.Status,
            EventFieldKey.Visibility,
            EventFieldKey.Format,
            EventFieldKey.ScheduleSessions,
            EventFieldKey.ScheduleFirstStart
        },
        RequiredSessionFields = new HashSet<Enum>()
    };

    private static EventLifecyclePolicy CreateCommunityPublishPolicy() => new()
    {
        Profile = ValidationProfile.EventPublishCommunityLexicon,
        RequiredEventFields = new HashSet<Enum>
        {
            EventFieldKey.Title,
            EventFieldKey.Tenant,
            EventFieldKey.Owner,
            EventFieldKey.Status
        },
        RequiredSessionFields = new HashSet<Enum>()
    };
}
