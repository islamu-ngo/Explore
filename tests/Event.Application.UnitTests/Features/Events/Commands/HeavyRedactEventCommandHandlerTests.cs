// ABOUTME: Unit tests for irreversible heavy moderation command handling.
// ABOUTME: Verifies redaction orchestration, safe audit rows, image delete state, idempotency, and cache invalidation.

using System.Diagnostics.Metrics;
using System.Text.Json;
using Event.Application.UnitTests.Common;
using Explore.Application.Authorization;
using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Moderation;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Models.Storage;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Commands;

public sealed class HeavyRedactEventCommandHandlerTests
{
    private readonly IEventHeavyRedactionRepository _redactionRepository = Substitute.For<IEventHeavyRedactionRepository>();
    private readonly IEventModerationRecordRepository _moderationRecordRepository = Substitute.For<IEventModerationRecordRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IStorageObjectDeletionService _storageObjectDeletionService = Substitute.For<IStorageObjectDeletionService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly HeavyRedactEventCommandHandler _handler;

    public HeavyRedactEventCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                return operation(CancellationToken.None);
            });

        _moderationRecordRepository.Create(Arg.Any<EventModerationRecord>())
            .Returns(call => call.Arg<EventModerationRecord>());
        _outboxRepository.Create(Arg.Any<OutboxMessage>())
            .Returns(call => call.Arg<OutboxMessage>());

        _handler = new HeavyRedactEventCommandHandler(
            _redactionRepository,
            _moderationRecordRepository,
            _outboxRepository,
            _storageObjectDeletionService,
            _unitOfWork,
            _currentUserService,
            _cache,
            CreateMetrics(),
            NullLogger<HeavyRedactEventCommandHandler>.Instance,
            AtprotoPublicationPlannerTestFactory.Disabled());

        _storageObjectDeletionService
            .DeleteRequestedForResourceAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new StorageObjectDeletionResult(1, 1, 0, 0));
    }

    [Test]
    public async Task Handle_WhenEventNeedsHeavyRedaction_RedactsGraphWritesAuditAndInvalidatesCaches()
    {
        var moderatorUserId = Guid.NewGuid();
        var image = CreateStorageObject();
        var @event = CreateEvent(EventStatusEnum.Published, image.Id);
        var sourceReportId = Guid.NewGuid();
        var sourceReportDecisionId = Guid.NewGuid();
        const string reasonCode = "illegal_image";
        const string correlationId = "case-heavy-123";
        var graph = new EventHeavyRedactionGraph(
            @event,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [image]);
        var createdRecords = new List<EventModerationRecord>();
        var createdMessages = new List<OutboxMessage>();

        _currentUserService.UserId.Returns(moderatorUserId);
        _redactionRepository.GetForUpdateAsync(@event.Id, Arg.Any<CancellationToken>())
            .Returns(graph);
        _moderationRecordRepository.GetLatestByEventAsync(@event.TenantId, @event.Id, Arg.Any<CancellationToken>())
            .Returns((EventModerationRecord?)null);
        _moderationRecordRepository.Create(Arg.Do<EventModerationRecord>(record => createdRecords.Add(record)))
            .Returns(call => call.Arg<EventModerationRecord>());
        _outboxRepository.Create(Arg.Do<OutboxMessage>(message => createdMessages.Add(message)))
            .Returns(call => call.Arg<OutboxMessage>());

        var result = await _handler.Handle(new HeavyRedactEventCommand
        {
            Id = @event.Id,
            ReasonCode = reasonCode,
            CorrelationId = correlationId,
            SourceReportId = sourceReportId,
            SourceReportDecisionId = sourceReportDecisionId
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await Assert.That(@event.Title).IsEqualTo(EventRedactionSentinelPolicy.DisplayText);
        await Assert.That(@event.FeaturedImageId).IsNull();
        await Assert.That(image.LifecycleState).IsEqualTo(StorageObjectLifecycleStates.DeleteRequested);
        await Assert.That(image.OwningResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(image.OwningResourceId).IsEqualTo(@event.Id);

        await Assert.That(createdRecords).Count().IsEqualTo(1);
        var record = createdRecords.Single();
        await Assert.That(record.ActionKind).IsEqualTo(EventModerationActionKind.HeavyRedacted);
        await Assert.That(record.PreviousStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await Assert.That(record.ResultingStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await Assert.That(record.IsIrreversible).IsTrue();
        await Assert.That(record.ModeratorUserId).IsEqualTo(moderatorUserId);
        await Assert.That(record.ReasonCode).IsEqualTo(reasonCode);
        await Assert.That(record.CorrelationId).IsEqualTo(correlationId);
        await Assert.That(record.SourceReportId).IsEqualTo(sourceReportId);
        await Assert.That(record.SourceReportDecisionId).IsEqualTo(sourceReportDecisionId);

        await Assert.That(createdMessages).Count().IsEqualTo(1);
        var message = createdMessages.Single();
        await Assert.That(message.EventType).IsEqualTo(EventModerationOutboxMessageFactory.EventHeavyRedactedNotificationFanoutRequestedEventType);
        await Assert.That(message.AggregateId).IsEqualTo(@event.Id);
        await Assert.That(message.Status).IsEqualTo(OutboxMessageStatus.Pending);

        var payload = JsonSerializer.Deserialize<EventHeavyRedactedNotificationFanoutRequested>(message.Payload!);
        await Assert.That(payload).IsNotNull();
        await Assert.That(payload!.TenantId).IsEqualTo(@event.TenantId);
        await Assert.That(payload.ModerationRecordId).IsEqualTo(record.Id);
        await Assert.That(payload.SourceActorId).IsEqualTo(@event.ActorId);
        await Assert.That(message.Payload).DoesNotContain(@event.Id.ToString());
        await Assert.That(message.Payload).DoesNotContain(@event.Id.ToString("N"));
        await Assert.That(message.Payload).DoesNotContain("Illegal Event");
        await Assert.That(message.Payload).DoesNotContain("illegal.png");
        await Assert.That(message.Payload).DoesNotContain("/images/illegal.png");

        await _redactionRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _storageObjectDeletionService.Received(1).DeleteRequestedForResourceAsync(
            @event.TenantId,
            ResourceKinds.Event,
            @event.Id,
            moderatorUserId,
            100,
            Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"event:detail:{@event.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenLatestRecordIsAlreadyHeavyRedacted_ReturnsSuccessWithoutDuplicateAudit()
    {
        var @event = CreateEvent(EventStatusEnum.Moderated, imageId: null);
        var graph = new EventHeavyRedactionGraph(@event, [], [], [], [], [], [], [], [], [], []);
        var latestRecord = EventModerationRecord.CreateHeavyRedaction(
            @event.TenantId,
            @event.Id,
            Guid.NewGuid(),
            "illegal_content",
            (int)EventStatusEnum.Published,
            null,
            DateTimeOffset.UtcNow.AddMinutes(-5));

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _redactionRepository.GetForUpdateAsync(@event.Id, Arg.Any<CancellationToken>())
            .Returns(graph);
        _moderationRecordRepository.GetLatestByEventAsync(@event.TenantId, @event.Id, Arg.Any<CancellationToken>())
            .Returns(latestRecord);

        var result = await _handler.Handle(new HeavyRedactEventCommand { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _redactionRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _storageObjectDeletionService.Received(1).DeleteRequestedForResourceAsync(
            @event.TenantId,
            ResourceKinds.Event,
            @event.Id,
            Arg.Any<Guid?>(),
            100,
            Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithSourceReportDecisionAndNoCurrentUser_WritesProviderAuditRecord()
    {
        var image = CreateStorageObject();
        var @event = CreateEvent(EventStatusEnum.Published, image.Id);
        var sourceReportId = Guid.NewGuid();
        var sourceReportDecisionId = Guid.NewGuid();
        var graph = new EventHeavyRedactionGraph(@event, [], [], [], [], [], [], [], [], [], [image]);
        var createdRecords = new List<EventModerationRecord>();

        _currentUserService.UserId.Returns((Guid?)null);
        _redactionRepository.GetForUpdateAsync(@event.Id, Arg.Any<CancellationToken>())
            .Returns(graph);
        _moderationRecordRepository.GetLatestByEventAsync(@event.TenantId, @event.Id, Arg.Any<CancellationToken>())
            .Returns((EventModerationRecord?)null);
        _moderationRecordRepository.Create(Arg.Do<EventModerationRecord>(record => createdRecords.Add(record)))
            .Returns(call => call.Arg<EventModerationRecord>());

        var result = await _handler.Handle(new HeavyRedactEventCommand
        {
            Id = @event.Id,
            ReasonCode = "coop_decision",
            CorrelationId = "coop-correlation",
            SourceReportId = sourceReportId,
            SourceReportDecisionId = sourceReportDecisionId
        }, CancellationToken.None);

        var record = createdRecords.Single();
        await Assert.That(result.Success).IsTrue();
        await Assert.That(record.ModeratorUserId).IsNull();
        await Assert.That(record.SourceReportId).IsEqualTo(sourceReportId);
        await Assert.That(record.SourceReportDecisionId).IsEqualTo(sourceReportDecisionId);
        await Assert.That(record.ActionKind).IsEqualTo(EventModerationActionKind.HeavyRedacted);
        await Assert.That(image.LifecycleState).IsEqualTo(StorageObjectLifecycleStates.DeleteRequested);
        await _storageObjectDeletionService.Received(1).DeleteRequestedForResourceAsync(
            @event.TenantId,
            ResourceKinds.Event,
            @event.Id,
            null,
            100,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenImageDeletionFails_ReturnsPendingRetryFailureAfterRedactionCommit()
    {
        var moderatorUserId = Guid.NewGuid();
        var image = CreateStorageObject();
        var @event = CreateEvent(EventStatusEnum.Published, image.Id);
        var graph = new EventHeavyRedactionGraph(@event, [], [], [], [], [], [], [], [], [], [image]);

        _currentUserService.UserId.Returns(moderatorUserId);
        _redactionRepository.GetForUpdateAsync(@event.Id, Arg.Any<CancellationToken>())
            .Returns(graph);
        _moderationRecordRepository.GetLatestByEventAsync(@event.TenantId, @event.Id, Arg.Any<CancellationToken>())
            .Returns((EventModerationRecord?)null);
        _storageObjectDeletionService
            .DeleteRequestedForResourceAsync(
                @event.TenantId,
                ResourceKinds.Event,
                @event.Id,
                moderatorUserId,
                100,
                Arg.Any<CancellationToken>())
            .Returns(new StorageObjectDeletionResult(1, 0, 0, 1));

        var result = await _handler.Handle(new HeavyRedactEventCommand { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_heavy_redaction_storage_deletion_pending");
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await Assert.That(@event.Title).IsEqualTo(EventRedactionSentinelPolicy.DisplayText);
        await Assert.That(image.LifecycleState).IsEqualTo(StorageObjectLifecycleStates.DeleteRequested);
        await _redactionRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _moderationRecordRepository.Received(1).Create(Arg.Any<EventModerationRecord>());
    }

    [Test]
    public async Task Handle_WhenModeratorUserCannotBeResolved_ReturnsFailureBeforeTransaction()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await _handler.Handle(new HeavyRedactEventCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_heavy_redaction_user_unresolved");
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
        await _storageObjectDeletionService.DidNotReceiveWithAnyArgs()
            .DeleteRequestedForResourceAsync(default, default!, default, default, default, default);
    }

    private static Explore.Domain.Event CreateEvent(EventStatusEnum status, Guid? imageId) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Tenant = null!,
        ActorId = Guid.CreateVersion7(),
        Actor = null!,
        Title = "Illegal Event",
        FeaturedImageId = imageId,
        BackgroundImageId = imageId,
        EventStatusId = (int)status,
        EventStatus = null!,
        VisibilityTypeId = (int)VisibilityTypeEnum.Public,
        VisibilityType = null!,
        EventFormatId = (int)EventFormatEnum.Local,
        EventFormat = null!,
        ConcurrencyStamp = Guid.CreateVersion7()
    };

    private static StorageObject CreateStorageObject() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Tenant = null!,
        FileTypeId = (int)FileTypeEnum.Image,
        FileType = null!,
        Provider = StorageProviders.Local,
        ObjectKey = "tenants/test/illegal.png",
        Uri = "/images/illegal.png",
        FullName = "illegal.png",
        SafeDisplayName = "illegal.png",
        Extension = ".png",
        Size = 100,
        Visibility = StorageObjectVisibilities.PublicImage,
        Purpose = StorageObjectPurposes.EventImage,
        LifecycleState = StorageObjectLifecycleStates.Active
    };

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }
}
