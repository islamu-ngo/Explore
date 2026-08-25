// ABOUTME: Unit tests for import-event command handling and lifecycle readiness behavior.
// ABOUTME: Verifies provenance requirements, tolerant draft defaults, cache invalidation, and no publish outbox path.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services.Lifecycle;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Commands;

public sealed class ImportEventCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    [Test]
    public async Task Handle_WhenImportOmitsPublicationFields_CreatesDraftWithStructuralDefaults()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = CreateUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        policyProvider
            .GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventImportCreate, Arg.Any<CancellationToken>())
            .Returns(CreateImportPolicy());
        eventRepository.Create(Arg.Any<Explore.Domain.Event>())
            .Returns(callInfo => callInfo.Arg<Explore.Domain.Event>());
        var handler = new ImportEventCommandHandler(
            eventRepository,
            Substitute.For<IStorageObjectRepository>(),
            unitOfWork,
            cache,
            policyProvider,
            new EventLifecycleReadinessEvaluator(),
            TimeProvider.System);

        var request = CreateValidRequest();
        request = request with { VisibilityTypeId = null };
        request = request with { EventFormatId = null };

        var result = await handler.Handle(new ImportEventCommand { Request = request, TenantId = TenantId }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event imported successfully.");
        await eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.EventStatusId == (int)EventStatusEnum.Draft
            && entity.VisibilityTypeId == (int)VisibilityTypeEnum.Private
            && entity.EventFormatId == (int)EventFormatEnum.Local
            && entity.EventProvenanceTypeId == (int)EventProvenanceTypeEnum.Imported
            && entity.ProvenanceSource == request.ProvenanceSource
            && entity.ProvenanceExternalId == request.ProvenanceExternalId
            && entity.ParticipationConfiguration != null
            && entity.ParticipationConfiguration.ParticipationHandlingModeId == (int)ParticipationHandlingModeEnum.InformationOnly
            && entity.ParticipationConfiguration.AdvanceRegistrationObligationId == (int)AdvanceRegistrationObligationEnum.NotApplicable));
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenTransactionFails_DoesNotInvalidateCache()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        policyProvider
            .GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventImportCreate, Arg.Any<CancellationToken>())
            .Returns(CreateImportPolicy());
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns<Task<BaseCommandResponse<Guid>>>(_ => throw new InvalidOperationException("commit failed"));
        var handler = new ImportEventCommandHandler(
            eventRepository,
            Substitute.For<IStorageObjectRepository>(),
            unitOfWork,
            cache,
            policyProvider,
            new EventLifecycleReadinessEvaluator(),
            TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new ImportEventCommand { Request = CreateValidRequest(), TenantId = TenantId }, CancellationToken.None));

        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenTransactionCommits_InvalidatesCacheAfterCommit()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        bool transactionCompleted = false;
        bool cacheObservedCommit = false;
        var unitOfWork = CreateUnitOfWork(() => transactionCompleted = true);
        var cache = Substitute.For<HybridCache>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        policyProvider
            .GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventImportCreate, Arg.Any<CancellationToken>())
            .Returns(CreateImportPolicy());
        eventRepository.Create(Arg.Any<Explore.Domain.Event>())
            .Returns(callInfo => callInfo.Arg<Explore.Domain.Event>());
        cache.RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cacheObservedCommit = transactionCompleted;
                return ValueTask.CompletedTask;
            });
        var handler = new ImportEventCommandHandler(
            eventRepository,
            Substitute.For<IStorageObjectRepository>(),
            unitOfWork,
            cache,
            policyProvider,
            new EventLifecycleReadinessEvaluator(),
            TimeProvider.System);
        var request = CreateValidRequest() with { VisibilityTypeId = (int)VisibilityTypeEnum.Public };

        var result = await handler.Handle(new ImportEventCommand { Request = request, TenantId = TenantId }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(cacheObservedCommit).IsTrue();
        await eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.VisibilityTypeId == (int)VisibilityTypeEnum.Public));
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenTransactionDelegateRunsTwice_ReusesIdentityAndTimestamps()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        var createdEntities = new List<Explore.Domain.Event>();
        policyProvider
            .GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventImportCreate, Arg.Any<CancellationToken>())
            .Returns(CreateImportPolicy());
        eventRepository.Create(Arg.Any<Explore.Domain.Event>())
            .Returns(callInfo =>
            {
                var entity = callInfo.Arg<Explore.Domain.Event>();
                createdEntities.Add(entity);
                return entity;
            });
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                await operation(CancellationToken.None);
                return await operation(CancellationToken.None);
            });
        var handler = new ImportEventCommandHandler(
            eventRepository,
            Substitute.For<IStorageObjectRepository>(),
            unitOfWork,
            cache,
            policyProvider,
            new EventLifecycleReadinessEvaluator(),
            new FixedTimeProvider(Now));
        var request = CreateValidRequest();

        var result = await handler.Handle(new ImportEventCommand { Request = request, TenantId = TenantId }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(createdEntities).HasCount().EqualTo(2);
        await Assert.That(createdEntities[0].Id).IsEqualTo(createdEntities[1].Id);
        await Assert.That(createdEntities[0].Id.Version).IsEqualTo(7);
        await Assert.That(createdEntities[0].CreatedAt).IsEqualTo(Now.UtcDateTime);
        await Assert.That(createdEntities[0].CreatedAt).IsEqualTo(createdEntities[1].CreatedAt);
        await Assert.That(createdEntities[0].UpdatedAt).IsEqualTo(Now.UtcDateTime);
        await Assert.That(createdEntities[0].UpdatedAt).IsEqualTo(createdEntities[1].UpdatedAt);
        await Assert.That(createdEntities[0].ParticipationConfiguration!.CreatedAt).IsEqualTo(Now.UtcDateTime);
        await Assert.That(createdEntities[0].ParticipationConfiguration!.CreatedAt)
            .IsEqualTo(createdEntities[1].ParticipationConfiguration!.CreatedAt);
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCommittedFirstAttemptIsRetried_ReturnsExistingImportWithoutDuplicateCreate()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        var request = CreateValidRequest();
        Explore.Domain.Event? committedEvent = null;
        Guid capturedEventId = Guid.Empty;
        int lookupCount = 0;
        policyProvider
            .GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventImportCreate, Arg.Any<CancellationToken>())
            .Returns(CreateImportPolicy());
        eventRepository.GetById(Arg.Any<Guid>())
            .Returns(callInfo =>
            {
                capturedEventId = callInfo.Arg<Guid>();
                return lookupCount++ == 0 ? null : committedEvent;
            });
        eventRepository.Create(Arg.Any<Explore.Domain.Event>())
            .Returns(callInfo =>
            {
                var created = callInfo.Arg<Explore.Domain.Event>();
                committedEvent = new Explore.Domain.Event(EventStatusEnum.Draft)
                {
                    Id = created.Id,
                    Title = created.Title,
                    TenantId = created.TenantId,
                    Actor = null!,
                    Tenant = null!,
                    EventStatus = null!,
                    VisibilityType = null!,
                    EventFormat = null!,
                    EventProvenanceTypeId = (int)EventProvenanceTypeEnum.Imported,
                    ProvenanceSource = created.ProvenanceSource,
                    ProvenanceExternalId = created.ProvenanceExternalId,
                    ParticipationConfiguration = created.ParticipationConfiguration
                };
                return created;
            });
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                await operation(CancellationToken.None);
                return await operation(CancellationToken.None);
            });
        var handler = new ImportEventCommandHandler(
            eventRepository,
            Substitute.For<IStorageObjectRepository>(),
            unitOfWork,
            cache,
            policyProvider,
            new EventLifecycleReadinessEvaluator(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new ImportEventCommand { Request = request, TenantId = TenantId }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event imported successfully.");
        await Assert.That(result.Id).IsEqualTo(capturedEventId);
        await Assert.That(committedEvent!.Id).IsEqualTo(capturedEventId);
        await Assert.That(committedEvent.TenantId).IsEqualTo(TenantId);
        await eventRepository.Received(2).GetById(capturedEventId);
        await eventRepository.Received(1).Create(Arg.Any<Explore.Domain.Event>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(ExistingImportMismatch.Tenant)]
    [Arguments(ExistingImportMismatch.ProvenanceType)]
    [Arguments(ExistingImportMismatch.ProvenanceSource)]
    [Arguments(ExistingImportMismatch.ProvenanceExternalId)]
    public async Task Handle_WhenRetryFindsMismatchedImport_FailsClosedWithoutDuplicateCreate(
        ExistingImportMismatch mismatch)
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        var request = CreateValidRequest();
        Explore.Domain.Event? mismatchedEvent = null;
        Guid capturedEventId = Guid.Empty;
        int lookupCount = 0;
        policyProvider
            .GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventImportCreate, Arg.Any<CancellationToken>())
            .Returns(CreateImportPolicy());
        eventRepository.GetById(Arg.Any<Guid>())
            .Returns(callInfo =>
            {
                capturedEventId = callInfo.Arg<Guid>();
                return lookupCount++ == 0 ? null : mismatchedEvent;
            });
        eventRepository.Create(Arg.Any<Explore.Domain.Event>())
            .Returns(callInfo =>
            {
                var created = callInfo.Arg<Explore.Domain.Event>();
                mismatchedEvent = new Explore.Domain.Event(EventStatusEnum.Draft)
                {
                    Id = created.Id,
                    Title = created.Title,
                    TenantId = mismatch == ExistingImportMismatch.Tenant ? Guid.NewGuid() : created.TenantId,
                    Actor = null!,
                    Tenant = null!,
                    EventStatus = null!,
                    VisibilityType = null!,
                    EventFormat = null!,
                    EventProvenanceTypeId = mismatch == ExistingImportMismatch.ProvenanceType
                        ? (int)EventProvenanceTypeEnum.OrganizerCreated
                        : (int)EventProvenanceTypeEnum.Imported,
                    ProvenanceSource = mismatch == ExistingImportMismatch.ProvenanceSource
                        ? "different-source"
                        : created.ProvenanceSource,
                    ProvenanceExternalId = mismatch == ExistingImportMismatch.ProvenanceExternalId
                        ? "different-external-id"
                        : created.ProvenanceExternalId,
                    ParticipationConfiguration = created.ParticipationConfiguration
                };
                return created;
            });
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                await operation(CancellationToken.None);
                return await operation(CancellationToken.None);
            });
        var handler = new ImportEventCommandHandler(
            eventRepository,
            Substitute.For<IStorageObjectRepository>(),
            unitOfWork,
            cache,
            policyProvider,
            new EventLifecycleReadinessEvaluator(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new ImportEventCommand { Request = request, TenantId = TenantId }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_import_validation_failed");
        await Assert.That(mismatchedEvent!.Id).IsEqualTo(capturedEventId);
        await eventRepository.Received(2).GetById(capturedEventId);
        await eventRepository.Received(1).Create(Arg.Any<Explore.Domain.Event>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenProvenanceIsMissing_ReturnsValidationFailure()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var handler = new ImportEventCommandHandler(
            eventRepository,
            Substitute.For<IStorageObjectRepository>(),
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            Substitute.For<IEventLifecyclePolicyProvider>(),
            new EventLifecycleReadinessEvaluator(),
            TimeProvider.System);

        var request = CreateValidRequest();
        request = request with { ProvenanceSource = "" };

        var result = await handler.Handle(new ImportEventCommand { Request = request, TenantId = TenantId }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_import_validation_failed");
        await eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WhenReadinessPolicyRejectsImport_DoesNotPersist()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var policyProvider = Substitute.For<IEventLifecyclePolicyProvider>();
        policyProvider
            .GetEffectivePolicyAsync(Arg.Any<Guid?>(), ValidationProfile.EventImportCreate, Arg.Any<CancellationToken>())
            .Returns(CreateImpossibleImportPolicy());
        var handler = new ImportEventCommandHandler(
            eventRepository,
            Substitute.For<IStorageObjectRepository>(),
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            policyProvider,
            new EventLifecycleReadinessEvaluator(),
            TimeProvider.System);

        var result = await handler.Handle(new ImportEventCommand { Request = CreateValidRequest(), TenantId = TenantId }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_import_readiness_failed");
        await eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
    }

    private static IUnitOfWork CreateUnitOfWork(Action? onCompleted = null)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var response = await callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()(CancellationToken.None);
                onCompleted?.Invoke();
                return response;
            });
        return unitOfWork;
    }

    private static ImportEventRequestDto CreateValidRequest() => new()
    {
        Title = "Imported event",
        OwnerActorId = Guid.NewGuid(),
        ProvenanceSource = "legacy-system",
        ProvenanceExternalId = "legacy-123",
        ParticipationConfiguration = new ConfigureEventParticipationDto
        {
            ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.InformationOnly,
            AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.NotApplicable
        }
    };

    private static EventLifecyclePolicy CreateImportPolicy() => new()
    {
        Profile = ValidationProfile.EventImportCreate,
        RequiredEventFields = new HashSet<Enum>
        {
            EventFieldKey.Title,
            EventFieldKey.Tenant,
            EventFieldKey.Owner,
            EventFieldKey.Status,
            EventFieldKey.ProvenanceSource,
            EventFieldKey.ProvenanceExternalId
        },
        RequiredSessionFields = new HashSet<Enum>()
    };

    private static EventLifecyclePolicy CreateImpossibleImportPolicy() => new()
    {
        Profile = ValidationProfile.EventImportCreate,
        RequiredEventFields = new HashSet<Enum>
        {
            EventFieldKey.CoverImage
        },
        RequiredSessionFields = new HashSet<Enum>()
    };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    public enum ExistingImportMismatch
    {
        Tenant,
        ProvenanceType,
        ProvenanceSource,
        ProvenanceExternalId
    }
}
