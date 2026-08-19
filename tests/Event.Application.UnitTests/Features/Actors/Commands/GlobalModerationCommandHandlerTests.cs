// ABOUTME: Focused tests for instance-admin global Actor and AT Protocol identity moderation handlers.
// ABOUTME: Covers secure request context, authorization-before-lookup, transitions, deleted targets, and idempotent retries.

using Event.Application.UnitTests.Common;
using Explore.Application.Authorization;
using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Actors.Handlers.Commands;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Actors.Commands;

public sealed class GlobalModerationCommandHandlerTests
{
    private static readonly DateTime ModeratedAt = new(2026, 7, 28, 16, 0, 0, DateTimeKind.Utc);
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly IAtprotoIdentityRepository _identityRepository = Substitute.For<IAtprotoIdentityRepository>();
    private readonly IAtprotoRecordRepository _recordRepository = Substitute.For<IAtprotoRecordRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IPdsSyncOutboxRepository _pdsSyncOutboxRepository = Substitute.For<IPdsSyncOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly IAtprotoDiscoveryCacheInvalidator _discoveryCacheInvalidator =
        Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
    private readonly TimeProvider _timeProvider = new FixedTimeProvider(ModeratedAt);
    private readonly ModerateActorCommandHandler _actorHandler;
    private readonly ModerateAtprotoIdentityCommandHandler _identityHandler;
    private readonly Guid _operatorUserId = Guid.CreateVersion7();

    public GlobalModerationCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()(CancellationToken.None));
        _unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, bool Changed)>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call
                .Arg<Func<CancellationToken, Task<(BaseCommandResponse<Guid> Response, bool Changed)>>>()(CancellationToken.None));

        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(_operatorUserId);
        _adminContext.IsInstanceAdminAsync(_operatorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _recordRepository.GetLiveGroundedEventOwnershipsForActorAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        _recordRepository.GetLiveGroundedEventOwnershipsForActorAndDidAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        _pdsSyncOutboxRepository.GetUnsettledEventMutationsForActorAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        _pdsSyncOutboxRepository.GetUnsettledEventMutationsForActorAndDidAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        _actorHandler = new ModerateActorCommandHandler(
            _adminContext,
            _actorRepository,
            _recordRepository,
            _pdsSyncOutboxRepository,
            _eventRepository,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            _unitOfWork,
            _cache,
            [_discoveryCacheInvalidator],
            _timeProvider);
        _identityHandler = new ModerateAtprotoIdentityCommandHandler(
            _adminContext,
            _identityRepository,
            _recordRepository,
            _pdsSyncOutboxRepository,
            _eventRepository,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            _unitOfWork,
            _cache,
            [_discoveryCacheInvalidator],
            _timeProvider);
    }

    [Test]
    public async Task Commands_UseGlobalActorModerationInstanceSettingContext()
    {
        var actorCommand = ActorCommand(Guid.CreateVersion7(), GlobalModerationAction.Suspend);
        var identityCommand = IdentityCommand(Guid.CreateVersion7(), GlobalModerationAction.Reinstate);

        var actorRequest = (ISecureRequest)actorCommand;
        var identityRequest = (ISecureRequest)identityCommand;

        await Assert.That(actorRequest.ResourceId).IsEqualTo("global-actor-moderation");
        await Assert.That(identityRequest.ResourceId).IsEqualTo("global-actor-moderation");
        // Global moderation is an instance-governance setting: the setting key names the row, and only
        // instance administrators reach it, so no tenant or actor fact is published.
        await Assert.That(actorRequest.AuthorizationFacts)
            .IsEqualTo(InstanceScopedAuthorizationFacts.Instance);
        await Assert.That(identityRequest.AuthorizationFacts)
            .IsEqualTo(InstanceScopedAuthorizationFacts.Instance);
    }

    [Test]
    public async Task ActorSuspend_WhenInstanceAdmin_TransitionsAndPersistsOneAggregate()
    {
        var actor = CreateActor();
        _actorRepository.GetById(actor.Id).Returns(actor);

        var result = await _actorHandler.Handle(
            ActorCommand(actor.Id, GlobalModerationAction.Suspend, "policy-violation"),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.IsSuspended).IsTrue();
        await Assert.That(actor.ModerationRecords.Single().Action).IsEqualTo(GlobalModerationAction.Suspend);
        await Assert.That(actor.ModerationRecords.Single().CreatedBy).IsEqualTo(_operatorUserId);
        await Assert.That(actor.ModerationRecords.Single().CreatedAt).IsEqualTo(ModeratedAt);
        await _actorRepository.Received(1).Update(actor);
        await _cache.Received(1).RemoveAsync($"actor:detail:{actor.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.Events, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventLists, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventDetails, Arg.Any<CancellationToken>());
        await _discoveryCacheInvalidator.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ActorSuspend_WhenAlreadySuspended_ReturnsSuccessWithoutPersistingAnotherRecord()
    {
        var actor = CreateActor();
        actor.Suspend("policy-violation", ModeratedAt, _operatorUserId);
        _actorRepository.GetById(actor.Id).Returns(actor);

        var result = await _actorHandler.Handle(
            ActorCommand(actor.Id, GlobalModerationAction.Suspend, "retry"),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.ModerationRecords.Count).IsEqualTo(1);
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
        await _cache.Received(1).RemoveAsync($"actor:detail:{actor.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.Events, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventLists, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventDetails, Arg.Any<CancellationToken>());
        await _discoveryCacheInvalidator.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ActorSuspend_ReconcilesGroundedEventsAcrossTenantsWithoutAppendingAnotherModerationRecord()
    {
        var actor = CreateActor();
        actor.Suspend("policy-violation", ModeratedAt, _operatorUserId);
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        var eventEntity = new Explore.Domain.Event
        {
            Id = eventId,
            TenantId = tenantId,
            ActorId = actor.Id,
            Title = "Moderated event",
            Actor = actor,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        var ownership = new AtprotoOutboundRecordOwnership
        {
            AtprotoRecordId = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = ownerUserId,
            SourceEntityType = AtprotoEventPublicationPlanner.EventSourceType,
            SourceEntityId = eventId,
            SourceVersion = Guid.CreateVersion7()
        };
        _actorRepository.GetById(actor.Id).Returns(actor);
        _recordRepository.GetLiveGroundedEventOwnershipsForActorAsync(actor.Id, Arg.Any<CancellationToken>())
            .Returns([ownership]);
        _eventRepository.GetAtprotoLifecycleStateAsync(tenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(eventEntity);

        var handler = CreateActorHandler(
            AtprotoPublicationPlannerTestFactory.ExistingEventDelete(
                tenantId,
                eventId,
                ownerUserId,
                _pdsSyncOutboxRepository,
                _eventRepository,
                _recordRepository));

        var result = await handler.Handle(
            ActorCommand(actor.Id, GlobalModerationAction.Suspend, "retry"),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(actor.ModerationRecords.Count).IsEqualTo(1);
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
        await _pdsSyncOutboxRepository.Received(1).AddAsync(
            Arg.Is<PdsSyncOutbox>(outbox =>
                outbox.Operation == PdsSyncOperation.Delete
                && outbox.TenantId == tenantId
                && outbox.UserId == ownerUserId
                && outbox.SourceEntityId == eventId
                && outbox.SourceVersion == eventEntity.ConcurrencyStamp),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ActorSuspend_ReconcilesUnsettledCreateWithoutSettledOwnership()
    {
        var actor = CreateActor();
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        var eventEntity = new Explore.Domain.Event
        {
            Id = eventId,
            TenantId = tenantId,
            ActorId = actor.Id,
            Title = "Racing event",
            Actor = actor,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        var pendingCreate = new PdsSyncOutbox
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = ownerUserId,
            Did = "did:plc:lifecycle-owner",
            Collection = AtprotoEventPublicationPlanner.EventCollection,
            RecordKey = "pending-create",
            Operation = PdsSyncOperation.Create,
            PayloadHash = "hash",
            IdempotencyKey = Guid.CreateVersion7().ToString("N"),
            PdsHost = "https://pds.example/",
            SourceEntityType = AtprotoEventPublicationPlanner.EventSourceType,
            SourceEntityId = eventId,
            SourceVersion = eventEntity.ConcurrencyStamp,
            Status = PdsSyncStatus.Processing,
            CreatedAt = ModeratedAt,
            MaxRetries = 10
        };
        _actorRepository.GetById(actor.Id).Returns(actor);
        _pdsSyncOutboxRepository.GetUnsettledEventMutationsForActorAsync(
                actor.Id,
                AtprotoEventPublicationPlanner.EventSourceType,
                AtprotoEventPublicationPlanner.EventCollection,
                Arg.Any<CancellationToken>())
            .Returns([pendingCreate]);
        _eventRepository.GetAtprotoLifecycleStateAsync(tenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(eventEntity);

        var handler = CreateActorHandler(
            AtprotoPublicationPlannerTestFactory.ExistingEventDelete(
                tenantId,
                eventId,
                ownerUserId,
                _pdsSyncOutboxRepository,
                _eventRepository,
                _recordRepository));

        var result = await handler.Handle(
            ActorCommand(actor.Id, GlobalModerationAction.Suspend),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _pdsSyncOutboxRepository.Received(1).AddAsync(
            Arg.Is<PdsSyncOutbox>(outbox =>
                outbox.Operation == PdsSyncOperation.Delete
                && outbox.TenantId == tenantId
                && outbox.SourceEntityId == eventId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ActorReinstate_DoesNotSelectGroundedEventOwnerships()
    {
        var actor = CreateActor();
        actor.Suspend("policy-violation", ModeratedAt, _operatorUserId);
        _actorRepository.GetById(actor.Id).Returns(actor);

        var result = await _actorHandler.Handle(
            ActorCommand(actor.Id, GlobalModerationAction.Reinstate),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _recordRepository.DidNotReceive().GetLiveGroundedEventOwnershipsForActorAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ActorSuspend_WhenOperatorIsNotAnInstanceAdmin_DoesNotLookUpTarget()
    {
        _adminContext.IsInstanceAdminAsync(_operatorUserId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _actorHandler.Handle(
            ActorCommand(Guid.CreateVersion7(), GlobalModerationAction.Suspend),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _actorRepository.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    [Test]
    public async Task ActorSuspend_WhenAuditUserCannotBeResolved_DoesNotLookUpTarget()
    {
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await _actorHandler.Handle(
            ActorCommand(Guid.CreateVersion7(), GlobalModerationAction.Suspend),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _actorRepository.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    [Test]
    public async Task ActorSuspend_WhenReasonIsBlank_RejectsBeforeTargetLookup()
    {
        var result = await _actorHandler.Handle(
            ActorCommand(Guid.CreateVersion7(), GlobalModerationAction.Suspend, " "),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _actorRepository.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    [Test]
    public async Task ActorSuspend_WhenTargetIsDeleted_ThrowsNotFoundWithoutPersisting()
    {
        var actor = CreateActor();
        actor.IsDeleted = true;
        _actorRepository.GetById(actor.Id).Returns(actor);

        await Assert.That(async () => await _actorHandler.Handle(
                ActorCommand(actor.Id, GlobalModerationAction.Suspend),
                CancellationToken.None))
            .Throws<NotFoundException>();
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
    }

    [Test]
    public async Task ActorSuspend_WhenTargetIsMissing_ThrowsNotFoundWithoutPersisting()
    {
        var actorId = Guid.CreateVersion7();
        _actorRepository.GetById(actorId).Returns((Actor?)null);

        await Assert.That(async () => await _actorHandler.Handle(
                ActorCommand(actorId, GlobalModerationAction.Suspend),
                CancellationToken.None))
            .Throws<NotFoundException>();
        await _actorRepository.DidNotReceive().Update(Arg.Any<Actor>());
    }

    [Test]
    public async Task IdentityReinstate_WhenInstanceAdmin_PreservesIsActiveAndAppendsRecord()
    {
        var identity = CreateIdentity(isActive: false, isSuspended: true);
        _identityRepository.GetById(identity.Id).Returns(identity);

        var result = await _identityHandler.Handle(
            IdentityCommand(identity.Id, GlobalModerationAction.Reinstate, "key-rotated"),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(identity.IsActive).IsFalse();
        await Assert.That(identity.IsSuspended).IsFalse();
        await Assert.That(identity.ModerationRecords.Single().Action).IsEqualTo(GlobalModerationAction.Reinstate);
        await Assert.That(identity.ModerationRecords.Single().CreatedAt).IsEqualTo(ModeratedAt);
        await _identityRepository.Received(1).Update(identity);
        await _cache.Received(1).RemoveByTagAsync(CacheTags.Events, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventLists, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventDetails, Arg.Any<CancellationToken>());
        await _discoveryCacheInvalidator.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IdentitySuspend_ReconcilesOnlyGroundedEventsForItsExactDid()
    {
        var identity = CreateIdentity(isActive: true, isSuspended: false);
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        var eventEntity = new Explore.Domain.Event
        {
            Id = eventId,
            TenantId = tenantId,
            ActorId = identity.ActorId,
            Title = "Moderated event",
            Actor = identity.Actor,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        var ownership = new AtprotoOutboundRecordOwnership
        {
            AtprotoRecordId = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = ownerUserId,
            SourceEntityType = AtprotoEventPublicationPlanner.EventSourceType,
            SourceEntityId = eventId,
            SourceVersion = Guid.CreateVersion7()
        };
        _identityRepository.GetById(identity.Id).Returns(identity);
        _recordRepository.GetLiveGroundedEventOwnershipsForActorAndDidAsync(
                identity.ActorId,
                identity.Did,
                Arg.Any<CancellationToken>())
            .Returns([ownership]);
        _eventRepository.GetAtprotoLifecycleStateAsync(tenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(eventEntity);

        var handler = CreateIdentityHandler(
            AtprotoPublicationPlannerTestFactory.ExistingEventDelete(
                tenantId,
                eventId,
                ownerUserId,
                _pdsSyncOutboxRepository,
                _eventRepository,
                _recordRepository,
                identity.Did));

        var result = await handler.Handle(
            IdentityCommand(identity.Id, GlobalModerationAction.Suspend),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _pdsSyncOutboxRepository.Received(1).AddAsync(
            Arg.Is<PdsSyncOutbox>(outbox =>
                outbox.Operation == PdsSyncOperation.Delete
                && outbox.Did == identity.Did
                && outbox.TenantId == tenantId
                && outbox.UserId == ownerUserId
                && outbox.SourceEntityId == eventId
                && outbox.SourceVersion == eventEntity.ConcurrencyStamp),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IdentityReinstate_WhenAlreadyActive_ReturnsSuccessWithoutPersistingAnotherRecord()
    {
        var identity = CreateIdentity(isActive: true, isSuspended: false);
        _identityRepository.GetById(identity.Id).Returns(identity);

        var result = await _identityHandler.Handle(
            IdentityCommand(identity.Id, GlobalModerationAction.Reinstate),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(identity.ModerationRecords.Count).IsZero();
        await _identityRepository.DidNotReceive().Update(Arg.Any<AtprotoIdentity>());
        await _recordRepository.DidNotReceive().GetLiveGroundedEventOwnershipsForActorAndDidAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.Events, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventLists, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventDetails, Arg.Any<CancellationToken>());
        await _discoveryCacheInvalidator.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IdentityReinstate_WhenTargetIsDeleted_ThrowsNotFoundWithoutPersisting()
    {
        var identity = CreateIdentity(isActive: true, isSuspended: true);
        identity.IsDeleted = true;
        _identityRepository.GetById(identity.Id).Returns(identity);

        await Assert.That(async () => await _identityHandler.Handle(
                IdentityCommand(identity.Id, GlobalModerationAction.Reinstate),
                CancellationToken.None))
            .Throws<NotFoundException>();
        await _identityRepository.DidNotReceive().Update(Arg.Any<AtprotoIdentity>());
    }

    [Test]
    public async Task IdentityReinstate_WhenTargetIsMissing_ThrowsNotFoundWithoutPersisting()
    {
        var identityId = Guid.CreateVersion7();
        _identityRepository.GetById(identityId).Returns((AtprotoIdentity?)null);

        await Assert.That(async () => await _identityHandler.Handle(
                IdentityCommand(identityId, GlobalModerationAction.Reinstate),
                CancellationToken.None))
            .Throws<NotFoundException>();
        await _identityRepository.DidNotReceive().Update(Arg.Any<AtprotoIdentity>());
    }

    [Test]
    public async Task IdentityModeration_WhenActionIsUndefined_RejectsBeforeTargetLookup()
    {
        var result = await _identityHandler.Handle(
            IdentityCommand(Guid.CreateVersion7(), (GlobalModerationAction)999),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _identityRepository.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    private static ModerateActorCommand ActorCommand(
        Guid actorId,
        GlobalModerationAction action,
        string reasonCode = "policy-violation") => new()
        {
            ActorId = actorId,
            Moderation = new GlobalModerationRequest
            {
                Action = action,
                ReasonCode = reasonCode
            }
        };

    private static ModerateAtprotoIdentityCommand IdentityCommand(
        Guid identityId,
        GlobalModerationAction action,
        string reasonCode = "policy-violation") => new()
        {
            AtprotoIdentityId = identityId,
            Moderation = new GlobalModerationRequest
            {
                Action = action,
                ReasonCode = reasonCode
            }
        };

    private ModerateActorCommandHandler CreateActorHandler(AtprotoEventPublicationPlanner planner) => new(
        _adminContext,
        _actorRepository,
        _recordRepository,
        _pdsSyncOutboxRepository,
        _eventRepository,
        planner,
        _unitOfWork,
        _cache,
        [_discoveryCacheInvalidator],
        _timeProvider);

    private ModerateAtprotoIdentityCommandHandler CreateIdentityHandler(AtprotoEventPublicationPlanner planner) => new(
        _adminContext,
        _identityRepository,
        _recordRepository,
        _pdsSyncOutboxRepository,
        _eventRepository,
        planner,
        _unitOfWork,
        _cache,
        [_discoveryCacheInvalidator],
        _timeProvider);

    private static Actor CreateActor()
    {
        var actorId = Guid.CreateVersion7();
        return new Actor
        {
            Id = actorId,
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = new ActorType
            {
                Id = (int)ActorTypeEnum.User,
                FullName = "User",
                MasterCode = "USER"
            },
            Pii = new ActorPii
            {
                ActorId = actorId,
                DisplayName = "Moderated actor"
            },
            ConcurrencyStamp = Guid.CreateVersion7()
        };
    }

    private static AtprotoIdentity CreateIdentity(bool isActive, bool isSuspended)
    {
        var actor = CreateActor();
        return new AtprotoIdentity
        {
            Id = Guid.CreateVersion7(),
            Did = "did:plc:moderated-identity",
            ActorId = actor.Id,
            Actor = actor,
            PdsHost = "https://pds.example.test",
            IsActive = isActive,
            IsSuspended = isSuspended,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
    }

    private sealed class FixedTimeProvider(DateTime value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(value);
    }
}
