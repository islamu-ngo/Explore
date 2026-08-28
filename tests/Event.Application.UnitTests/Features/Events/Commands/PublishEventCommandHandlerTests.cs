// ABOUTME: Unit tests for publish-event command handling.
// ABOUTME: Verifies lifecycle readiness, concurrency, outbox, and cache side effects.

using System.Text.Json;
using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services.Federation;
using Explore.Application.Services.Lifecycle;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Commands;

public class PublishEventCommandHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventLocationRepository _eventLocationRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventLifecyclePolicyProvider _policyProvider;
    private readonly HybridCache _cache;
    private readonly PublishEventCommandHandler _handler;
    private readonly IUserContext _userContext;

    public PublishEventCommandHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _eventLocationRepository = Substitute.For<IEventLocationRepository>();
        _eventLocationRepository
            .GetByEventIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<EventLocation>().AsReadOnly());
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        _cache = Substitute.For<HybridCache>();
        _userContext = Substitute.For<IUserContext>();
        _userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());

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
            CreateExecutor(AtprotoPublicationPlannerTestFactory.Disabled()));
    }

    [Test]
    public async Task Handle_WhenDraftEventIsReady_PublishesAndCreatesNotificationFanoutOutboxMessage()
    {
        var concurrencyStamp = Guid.CreateVersion7();
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

        await Assert.That(result.IsSuccess).IsTrue();
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
    [Category("TCM110ApprovalPolicyVertical")]
    public async Task Handle_WhenEffectivePolicyRequiresApproval_RejectsBeforePublicationSideEffects()
    {
        var concurrencyStamp = Guid.CreateVersion7();
        var @event = CreateReadyEvent(concurrencyStamp);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _policyProvider
            .GetEffectivePolicyAsync(@event.TenantId, ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(CreateEventPublishPolicy() with { RequiresApproval = true });
        var atprotoSettings = Substitute.For<IHierarchicalSettingsResolver>();
        var handler = CreateHandler(CreateObservableAtprotoPlanner(atprotoSettings));

        var result = await handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        var actual = new ApprovalRejectionObservables(
            result.IsSuccess,
            result.FailureCode,
            (EventStatusEnum)@event.EventStatusId,
            @event.ConcurrencyStamp);
        var expected = new ApprovalRejectionObservables(
            Success: false,
            FailureCode: "event_publish_approval_required",
            EventStatusEnum.Draft,
            concurrencyStamp);

        await Assert.That(actual).IsEqualTo(expected);
        await _unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await atprotoSettings.DidNotReceive().ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("TCM110ApprovalPolicyVertical")]
    public async Task Handle_WhenRetryObservesApprovalRequirement_RejectsFreshAttemptWithoutSecondPublication()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid concurrencyStamp = Guid.CreateVersion7();
        var outerEvent = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        var rolledBackAttemptEvent = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        var retryEvent = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        _eventRepository.GetById(eventId).Returns(outerEvent, rolledBackAttemptEvent, retryEvent);
        _policyProvider
            .GetEffectivePolicyAsync(tenantId, ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(
                CreateEventPublishPolicy() with { RequiresApproval = false },
                CreateEventPublishPolicy() with { RequiresApproval = true });
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>();
                await operation(CancellationToken.None);
                return await operation(CancellationToken.None);
            });

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = eventId,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        var actual = new ApprovalRejectionObservables(
            result.IsSuccess,
            result.FailureCode,
            (EventStatusEnum)retryEvent.EventStatusId,
            retryEvent.ConcurrencyStamp);
        var expected = new ApprovalRejectionObservables(
            Success: false,
            FailureCode: "event_publish_approval_required",
            EventStatusEnum.Draft,
            concurrencyStamp);

        await Assert.That(actual).IsEqualTo(expected);
        await _eventRepository.Received(1).Update(rolledBackAttemptEvent);
        await _eventRepository.DidNotReceive().Update(retryEvent);
        await _outboxRepository.Received(1).Create(Arg.Any<OutboxMessage>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEnabledAtproto_StagesEventOutboxAfterLocalSaveInsideTransactionWithoutPdsCall()
    {
        var userId = Guid.CreateVersion7();
        var concurrencyStamp = Guid.CreateVersion7();
        var @event = CreateReadyEvent(concurrencyStamp);
        @event.Actor.AtprotoIdentities.Add(new AtprotoIdentity
        {
            Id = Guid.CreateVersion7(),
            Did = "did:plc:publisher",
            ActorId = @event.ActorId,
            Actor = @event.Actor,
            PdsHost = "https://pds.example.test/",
            IsActive = true,
            LastResolvedAt = DateTime.UtcNow
        });
        _userContext.GetRequiredUserId().Returns(userId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _eventRepository.IsPubliclyEligibleAsync(
                @event.TenantId,
                @event.Id,
                Arg.Any<CancellationToken>())
            .Returns(true);
        _eventRepository.GetAtprotoPublicationGraphAsync(
                @event.TenantId,
                @event.Id,
                Arg.Any<CancellationToken>())
            .Returns(new AtprotoPublicationGraphFactory(@event).Build());

        var settings = Substitute.For<IHierarchicalSettingsResolver>();
        settings.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IEnumerable<string>>().Select(key => new ResolvedSetting
            {
                Key = key,
                Value = key == GovernanceSettingKeys.Federation.AtprotoEventValidationProfile
                    ? "\"platform\""
                    : "true",
                Source = SettingSource.UserPreference
            }).ToArray());
        var sessions = Substitute.For<IUserAuthenticationTokenRepository>();
        sessions.GetAtprotoSessionsForReadAsync(
                @event.TenantId,
                userId,
                RepositoryBackedAtprotoSession.Provider,
                Arg.Any<CancellationToken>())
            .Returns([
                new UserAuthenticationToken
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = @event.TenantId,
                    Tenant = null!,
                    UserId = userId,
                    User = null!,
                    Provider = RepositoryBackedAtprotoSession.Provider,
                    SubjectDid = "did:plc:publisher",
                    SessionCiphertext = [1],
                    EncryptionKeyId = "enc",
                    OAuthClientKeyId = "oauth",
                    PdsHost = "https://pds.example/"
                }
            ]);
        var logins = Substitute.For<IUserExternalLoginRepository>();
        logins.GetByProviderAndKey(RepositoryBackedAtprotoSession.Provider, "did:plc:publisher")
            .Returns(new UserExternalLogin
            {
                Id = Guid.CreateVersion7(),
                TenantId = @event.TenantId,
                Tenant = null!,
                UserId = userId,
                User = null!,
                Provider = RepositoryBackedAtprotoSession.Provider,
                ProviderKey = "did:plc:publisher"
            });
        var payloads = Substitute.For<IAtprotoPublicationPayloadBuilder>();
        payloads.BuildEventAsync(
                Arg.Any<AtprotoEventPublicationEntityGraph>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(AtprotoPublicationPayloadBuildResult.Valid(new("{}", "hash")));
        var federationOutbox = Substitute.For<IPdsSyncOutboxRepository>();
        var gateway = Substitute.For<IAtprotoPdsDeliveryGateway>();
        var insideTransaction = false;
        var localSaved = false;
        var addedAfterLocalSaveInsideTransaction = false;
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                insideTransaction = true;
                try
                {
                    return await call.ArgAt<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(0)(
                        call.ArgAt<CancellationToken>(1));
                }
                finally
                {
                    insideTransaction = false;
                }
            });
        _eventRepository.Update(@event).Returns(_ =>
        {
            localSaved = true;
            return Task.CompletedTask;
        });
        federationOutbox.AddAsync(Arg.Any<PdsSyncOutbox>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                addedAfterLocalSaveInsideTransaction = localSaved && insideTransaction;
                return Task.CompletedTask;
            });
        var planner = new AtprotoEventPublicationPlanner(
            new AtprotoEventGovernanceResolver(settings),
            _eventRepository,
            Substitute.For<IAtprotoRecordRepository>(),
            sessions,
            logins,
            payloads,
            federationOutbox,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AtprotoEventPublicationPlanner>.Instance);
        var handler = new PublishEventCommandHandler(CreateExecutor(planner));

        var result = await handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(addedAfterLocalSaveInsideTransaction).IsTrue();
        await federationOutbox.Received(1).AddAsync(
            Arg.Is<PdsSyncOutbox>(row =>
                row.SourceEntityId == @event.Id
                && row.Operation == PdsSyncOperation.Create),
            Arg.Any<CancellationToken>());
        await gateway.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
    }

    [Test]
    public async Task Handle_WhenConcurrencyStampDoesNotMatch_ReturnsConflictFailure()
    {
        var @event = CreateReadyEvent(Guid.CreateVersion7());
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = Guid.CreateVersion7() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_publish_concurrency_conflict");
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Handle_WhenAlreadyPublished_ReturnsSuccessWithoutMutationSideEffects()
    {
        var concurrencyStamp = Guid.CreateVersion7();
        var @event = CreateReadyEvent(concurrencyStamp, EventStatusEnum.Published);
        @event.FirstSessionStartUtc = null;
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = Guid.CreateVersion7() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await _policyProvider.DidNotReceive().GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventPublish, Arg.Any<CancellationToken>());
        await _eventLocationRepository.DidNotReceive().GetByEventIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _userContext.DidNotReceive().GetRequiredUserId();
    }

    [Test]
    public async Task Handle_WhenRolledBackAttemptIsRetried_ReloadsAndStagesStablePublicationIdentity()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid concurrencyStamp = Guid.CreateVersion7();
        var outerEvent = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        var firstAttemptEvent = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        var retryEvent = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        _eventRepository.GetById(eventId).Returns(outerEvent, firstAttemptEvent, retryEvent);
        var messages = new List<OutboxMessage>();
        _outboxRepository.Create(Arg.Any<OutboxMessage>()).Returns(call =>
        {
            var message = call.Arg<OutboxMessage>();
            messages.Add(message);
            return message;
        });
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>();
                await operation(CancellationToken.None);
                return await operation(CancellationToken.None);
            });

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = eventId,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _eventRepository.Received(3).GetById(eventId);
        await _eventRepository.Received(1).Update(firstAttemptEvent);
        await _eventRepository.Received(1).Update(retryEvent);
        await Assert.That(messages).Count().IsEqualTo(2);
        await Assert.That(messages.Select(message => message.Id).Distinct()).Count().IsEqualTo(1);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCommittedAttemptIsRetried_StagesPublicationOnceAndInvalidatesCacheOnce()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid concurrencyStamp = Guid.CreateVersion7();
        var outerEvent = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        var firstAttemptEvent = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        var committedEvent = CreateReadyEvent(Guid.CreateVersion7(), EventStatusEnum.Published, eventId, tenantId);
        _eventRepository.GetById(eventId).Returns(outerEvent, firstAttemptEvent, committedEvent);
        var messages = new List<OutboxMessage>();
        _outboxRepository.Create(Arg.Any<OutboxMessage>()).Returns(call =>
        {
            var message = call.Arg<OutboxMessage>();
            messages.Add(message);
            return message;
        });
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>();
                await operation(CancellationToken.None);
                return await operation(CancellationToken.None);
            });

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = eventId,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _eventRepository.Received(3).GetById(eventId);
        await _eventRepository.Received(1).Update(firstAttemptEvent);
        await _eventRepository.DidNotReceive().Update(committedEvent);
        await Assert.That(messages).Count().IsEqualTo(1);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEventIsMissingSchedule_ReturnsReadinessFailure()
    {
        var concurrencyStamp = Guid.CreateVersion7();
        var @event = CreateReadyEvent(concurrencyStamp);
        @event.FirstSessionStartUtc = null;
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_publish_readiness_failed");
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors![0]).Contains("scheduled session");
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WhenCommunityProfileAndScheduleIsMissing_PublishesUsingInternalSafetyFields()
    {
        var concurrencyStamp = Guid.CreateVersion7();
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

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await _eventRepository.Received(1).Update(@event);
    }

    [Test]
    public async Task Handle_WhenCommunityProfileEventIsModerated_ReturnsReadinessFailureWithoutMutation()
    {
        var concurrencyStamp = Guid.CreateVersion7();
        var @event = CreateReadyEvent(concurrencyStamp, EventStatusEnum.Moderated);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _policyProvider
            .GetEffectivePolicyAsync(@event.TenantId, ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(CreateCommunityPublishPolicy());

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_publish_readiness_failed");
        await Assert.That(result.Errors).Contains(error => error.Contains("moderated", StringComparison.OrdinalIgnoreCase));
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task ApprovalModeRejectsAStaleConcurrencyStampBeforeTransaction()
    {
        var @event = CreateReadyEvent(Guid.CreateVersion7());
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await CreateApprovalHandler().Handle(new ApprovePublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = Guid.CreateVersion7() }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventPublicationExecutor.ConcurrencyConflictCode);
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task ApprovalModeRetainsReadinessAndLocationPrivacyChecks()
    {
        Guid concurrencyStamp = Guid.CreateVersion7();
        var @event = CreateReadyEvent(concurrencyStamp);
        @event.FirstSessionStartUtc = null;
        _eventRepository.GetById(@event.Id).Returns(@event);
        _policyProvider
            .GetEffectivePolicyAsync(@event.TenantId, ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(CreateEventPublishPolicy() with { RequiresApproval = true });

        var result = await CreateApprovalHandler().Handle(new ApprovePublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventPublicationExecutor.ReadinessFailedCode);
        await _eventLocationRepository.Received(1).GetByEventIdAsync(
            @event.Id,
            Arg.Any<CancellationToken>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApprovalModeRetryReevaluatesPolicyAndUsesStableOutboxIdentity()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid concurrencyStamp = Guid.CreateVersion7();
        var outerEvent = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        var rolledBackAttempt = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        var retryAttempt = CreateReadyEvent(concurrencyStamp, id: eventId, tenantId: tenantId);
        _eventRepository.GetById(eventId).Returns(outerEvent, rolledBackAttempt, retryAttempt);
        _policyProvider
            .GetEffectivePolicyAsync(tenantId, ValidationProfile.EventPublish, Arg.Any<CancellationToken>())
            .Returns(
                CreateEventPublishPolicy() with { RequiresApproval = true },
                CreateEventPublishPolicy() with { RequiresApproval = true });
        var messages = new List<OutboxMessage>();
        _outboxRepository.Create(Arg.Any<OutboxMessage>()).Returns(call =>
        {
            OutboxMessage message = call.Arg<OutboxMessage>();
            messages.Add(message);
            return message;
        });
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>();
                await operation(CancellationToken.None);
                return await operation(CancellationToken.None);
            });

        var result = await CreateApprovalHandler().Handle(new ApprovePublishEventCommand
        {
            Id = eventId,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _policyProvider.Received(2).GetEffectivePolicyAsync(
            tenantId,
            ValidationProfile.EventPublish,
            Arg.Any<CancellationToken>());
        await _eventRepository.Received(1).Update(rolledBackAttempt);
        await _eventRepository.Received(1).Update(retryAttempt);
        await Assert.That(messages).Count().IsEqualTo(2);
        await Assert.That(messages.Select(message => message.Id).Distinct()).Count().IsEqualTo(1);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
    }

    private static Explore.Domain.Event CreateReadyEvent(
        Guid concurrencyStamp,
        EventStatusEnum status = EventStatusEnum.Draft,
        Guid? id = null,
        Guid? tenantId = null) => new(status)
        {
            Id = id ?? Guid.CreateVersion7(),
            Title = "Draft Event",
            ActorId = Guid.CreateVersion7(),
            Actor = new Actor
            {
                ActorType = new ActorType { Id = 1, FullName = "User", MasterCode = "user" },
                Pii = new ActorPii { DisplayName = "Publisher" }
            },
            TenantId = tenantId ?? Guid.CreateVersion7(),
            Tenant = CreateTenant(),
            VisibilityTypeId = 1,
            VisibilityType = new VisibilityType { Id = 1, FullName = "Public", MasterCode = "public" },
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

    private PublishEventCommandHandler CreateHandler(AtprotoEventPublicationPlanner planner) =>
        new(CreateExecutor(planner));

    private ApprovePublishEventCommandHandler CreateApprovalHandler() =>
        new(CreateExecutor(AtprotoPublicationPlannerTestFactory.Disabled()));

    private EventPublicationExecutor CreateExecutor(AtprotoEventPublicationPlanner planner) => new(
        _eventRepository,
        _eventLocationRepository,
        _outboxRepository,
        _unitOfWork,
        _cache,
        _policyProvider,
        new EventLifecycleReadinessEvaluator(),
        _userContext,
        planner,
        TimeProvider.System);

    private AtprotoEventPublicationPlanner CreateObservableAtprotoPlanner(IHierarchicalSettingsResolver settings) => new(
        new AtprotoEventGovernanceResolver(settings),
        _eventRepository,
        Substitute.For<IAtprotoRecordRepository>(),
        Substitute.For<IUserAuthenticationTokenRepository>(),
        Substitute.For<IUserExternalLoginRepository>(),
        Substitute.For<IAtprotoPublicationPayloadBuilder>(),
        Substitute.For<IPdsSyncOutboxRepository>(),
        Microsoft.Extensions.Logging.Abstractions.NullLogger<AtprotoEventPublicationPlanner>.Instance);

    private sealed record ApprovalRejectionObservables(
        bool Success,
        string? FailureCode,
        EventStatusEnum Status,
        Guid ConcurrencyStamp);

    private sealed class AtprotoPublicationGraphFactory(Explore.Domain.Event eventEntity)
    {
        public AtprotoEventPublicationEntityGraph Build() =>
            new(eventEntity, [], [], [], [], [], [], [], [], [], [], [], [], [], [], []);
    }

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
        RequiredSessionFields = new HashSet<Enum>(),
        RequiresApproval = false
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
