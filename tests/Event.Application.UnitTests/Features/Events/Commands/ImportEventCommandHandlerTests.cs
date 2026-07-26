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
            unitOfWork,
            cache,
            policyProvider,
            new EventLifecycleReadinessEvaluator());

        var request = CreateValidRequest();
        request.VisibilityTypeId = null;
        request.EventFormatId = null;

        var result = await handler.Handle(new ImportEventCommand { Request = request }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await eventRepository.Received(1).Create(Arg.Is<Explore.Domain.Event>(entity =>
            entity.EventStatusId == (int)EventStatusEnum.Draft
            && entity.VisibilityTypeId == (int)VisibilityTypeEnum.Private
            && entity.EventFormatId == (int)EventFormatEnum.Local
            && entity.EventProvenanceTypeId == (int)EventProvenanceTypeEnum.Imported
            && entity.ProvenanceSource == request.ProvenanceSource
            && entity.ProvenanceExternalId == request.ProvenanceExternalId));
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(request.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenProvenanceIsMissing_ReturnsValidationFailure()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var handler = new ImportEventCommandHandler(
            eventRepository,
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            Substitute.For<IEventLifecyclePolicyProvider>(),
            new EventLifecycleReadinessEvaluator());

        var request = CreateValidRequest();
        request.ProvenanceSource = "";

        var result = await handler.Handle(new ImportEventCommand { Request = request }, CancellationToken.None);

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
            CreateUnitOfWork(),
            Substitute.For<HybridCache>(),
            policyProvider,
            new EventLifecycleReadinessEvaluator());

        var result = await handler.Handle(new ImportEventCommand { Request = CreateValidRequest() }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_import_readiness_failed");
        await eventRepository.DidNotReceive().Create(Arg.Any<Explore.Domain.Event>());
    }

    private static IUnitOfWork CreateUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()(CancellationToken.None));
        return unitOfWork;
    }

    private static ImportEventRequestDto CreateValidRequest() => new()
    {
        Title = "Imported event",
        TenantId = Guid.NewGuid(),
        OwnerActorId = Guid.NewGuid(),
        ProvenanceSource = "legacy-system",
        ProvenanceExternalId = "legacy-123"
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
}
