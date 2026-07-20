// ABOUTME: Unit tests for composite outbox dispatch routing to internal side-effect handlers.
// ABOUTME: Verifies retired broker events fail closed while local fanout and provider sync still route.

using System.Text.Json;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Features.Management.Handlers.Commands;
using Explore.Application.Features.Management.Requests.Commands;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Infrastructure.Messaging;
using Explore.Infrastructure.Services.Moderation;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class CompositeOutboxMessageDispatcherTests
{
    [Test]
    public async Task DispatchAsync_WithFanoutOccurrencePointer_RoutesToDurableHandoff()
    {
        var occurrence = CreateOccurrence();
        var occurrenceRepository = Substitute.For<INotificationFanoutOccurrenceRepository>();
        var runRepository = Substitute.For<INotificationFanoutRunRepository>();
        occurrenceRepository.GetByPointerAsync(
                Arg.Any<NotificationFanoutOccurrenceRequested>(),
                false,
                Arg.Any<CancellationToken>())
            .Returns(occurrence);
        runRepository.EnsurePendingOccurrenceRunAsync(
                occurrence.TenantId,
                occurrence.Id,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateRun(occurrence));
        var dispatcher = CreateDispatcher(
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            Substitute.For<IEventModerationNotificationFanoutService>(),
            occurrenceRepository: occurrenceRepository,
            runRepository: runRepository);

        await dispatcher.DispatchAsync(NotificationFanoutOccurrenceOutboxMessageFactory.Create(occurrence));

        await runRepository.Received(1).EnsurePendingOccurrenceRunAsync(
            occurrence.TenantId,
            occurrence.Id,
            Arg.Is<Guid>(runId => runId != Guid.Empty && runId != occurrence.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_WithInternalFanoutEvent_RoutesToFanoutServiceOnly()
    {
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(fanoutService, moderationFanoutService);
        var request = new EventPublishedNotificationFanoutRequested
        {
            TenantId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            EventTitle = "Community Iftar",
            SourceActorId = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
            PublishedAt = DateTimeOffset.UtcNow
        };

        await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = request.EventId,
            EventType = PublishEventCommandHandler.EventPublishedNotificationFanoutRequestedEventType,
            Payload = JsonSerializer.Serialize(request)
        });

        await fanoutService.Received(1).FanoutAsync(
            Arg.Is<EventPublishedNotificationFanoutRequested>(payload =>
                payload.TenantId == request.TenantId
                && payload.EventId == request.EventId
                && payload.SourceActorId == request.SourceActorId),
            Arg.Any<CancellationToken>());
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutLightModerationAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutHeavyRedactionAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithEventReportProviderSyncEvent_RoutesToReportProviderSyncDispatcherOnly()
    {
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var reportProviderSyncDispatcher = Substitute.For<IReportProviderSyncDispatcher>();
        var dispatcher = CreateDispatcher(
            fanoutService,
            moderationFanoutService,
            reportProviderSyncDispatcher);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "EventReport",
            AggregateId = Guid.NewGuid(),
            EventType = EventReportOutboxMessageFactory.EventReportProviderSyncRequestedEventType,
            Payload = "{}"
        };

        await dispatcher.DispatchAsync(message);

        await reportProviderSyncDispatcher.Received(1).DispatchAsync(
            Arg.Is<OutboxMessage>(payload => payload.Id == message.Id),
            Arg.Any<CancellationToken>());
        await fanoutService.DidNotReceiveWithAnyArgs().FanoutAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutLightModerationAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutHeavyRedactionAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithLightModerationFanoutEvent_RoutesToModerationFanoutServiceOnly()
    {
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(fanoutService, moderationFanoutService);
        var request = new EventLightModeratedNotificationFanoutRequested
        {
            TenantId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            ModerationRecordId = Guid.NewGuid(),
            EventTitle = "Community Iftar",
            SourceActorId = Guid.NewGuid(),
            ModeratedAt = DateTimeOffset.UtcNow
        };

        await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = request.EventId,
            EventType = EventModerationOutboxMessageFactory.EventLightModeratedNotificationFanoutRequestedEventType,
            Payload = JsonSerializer.Serialize(request)
        });

        await moderationFanoutService.Received(1).FanoutLightModerationAsync(
            Arg.Is<EventLightModeratedNotificationFanoutRequested>(payload =>
                payload.TenantId == request.TenantId
                && payload.EventId == request.EventId
                && payload.ModerationRecordId == request.ModerationRecordId),
            Arg.Any<CancellationToken>());
        await fanoutService.DidNotReceiveWithAnyArgs().FanoutAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutHeavyRedactionAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithHeavyRedactionFanoutEvent_RoutesToModerationFanoutServiceOnly()
    {
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(fanoutService, moderationFanoutService);
        var request = new EventHeavyRedactedNotificationFanoutRequested
        {
            TenantId = Guid.NewGuid(),
            ModerationRecordId = Guid.NewGuid(),
            Version = EventHeavyRedactedNotificationFanoutRequested.CurrentVersion
        };

        await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = Guid.NewGuid(),
            EventType = EventModerationOutboxMessageFactory.EventHeavyRedactedNotificationFanoutRequestedEventType,
            Payload = JsonSerializer.Serialize(request)
        });

        await moderationFanoutService.Received(1).FanoutHeavyRedactionAsync(
            Arg.Is<EventHeavyRedactedNotificationFanoutRequested>(payload =>
                payload.TenantId == request.TenantId
                && payload.ModerationRecordId == request.ModerationRecordId
                && payload.Version == request.Version),
            Arg.Any<CancellationToken>());
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutLightModerationAsync(default!, default);
        await fanoutService.DidNotReceiveWithAnyArgs().FanoutAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithHistoricalHeavyPayload_ScrubsAtRestBeforeCanonicalDispatch()
    {
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        outboxRepository.TryReplaceProcessingPayloadAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var dispatcher = CreateDispatcher(
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            moderationFanoutService,
            outboxRepository: outboxRepository);
        Guid tenantId = Guid.CreateVersion7();
        Guid moderationRecordId = Guid.CreateVersion7();
        string historicalPayload = JsonSerializer.Serialize(new
        {
            TenantId = tenantId,
            ModerationRecordId = moderationRecordId,
            SourceActorId = Guid.CreateVersion7(),
            RedactedAt = DateTimeOffset.UtcNow
        });
        var message = new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateType = "Event",
            EventType = EventModerationOutboxMessageFactory.EventHeavyRedactedNotificationFanoutRequestedEventType,
            Payload = historicalPayload
        };

        await dispatcher.DispatchAsync(message);

        await outboxRepository.Received(1).TryReplaceProcessingPayloadAsync(
            message.Id,
            historicalPayload,
            Arg.Is<string>(payload => payload.Contains("Version", StringComparison.Ordinal)
                && !payload.Contains("SourceActorId", StringComparison.Ordinal)
                && !payload.Contains("RedactedAt", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
        await moderationFanoutService.Received(1).FanoutHeavyRedactionAsync(
            Arg.Is<EventHeavyRedactedNotificationFanoutRequested>(payload => payload.TenantId == tenantId
                && payload.ModerationRecordId == moderationRecordId
                && payload.Version == EventHeavyRedactedNotificationFanoutRequested.CurrentVersion),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_WithUnknownHeavyPayloadMember_ScrubsAndRejectsWithoutFanout()
    {
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        outboxRepository.TryReplaceProcessingPayloadAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var dispatcher = CreateDispatcher(
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            moderationFanoutService,
            outboxRepository: outboxRepository);
        string unsafePayload = JsonSerializer.Serialize(new
        {
            TenantId = Guid.CreateVersion7(),
            ModerationRecordId = Guid.CreateVersion7(),
            Version = EventHeavyRedactedNotificationFanoutRequested.CurrentVersion,
            EventTitle = "private-title-canary",
            EventSlug = "private-slug-canary",
            EventUrl = "https://private.example/events/canary",
            Description = "private-description-canary",
            ImageUrl = "private-image-canary",
            OrganizerId = "private-organizer-canary",
            Evidence = "private-evidence-canary",
            DecisionNote = "private-decision-note-canary",
            ReasonCode = "private-reason-canary",
            ModeratorUserId = "private-moderator-canary",
            Provider = "private-provider-canary",
            StoragePath = "private-storage-path-canary",
            StorageKey = "private-storage-key-canary",
            RawError = "private-raw-error-canary",
            RecipientEmail = "attendee-pii-canary@example.test"
        });
        var message = new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateType = "Event",
            EventType = EventModerationOutboxMessageFactory.EventHeavyRedactedNotificationFanoutRequestedEventType,
            Payload = unsafePayload
        };

        await Assert.That(async () => await dispatcher.DispatchAsync(message)).Throws<JsonException>();

        await outboxRepository.Received(1).TryReplaceProcessingPayloadAsync(
            message.Id,
            unsafePayload,
            EventHeavyRedactedNotificationFanoutPayloadParser.SafeInvalidPayload,
            Arg.Any<CancellationToken>());
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutHeavyRedactionAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithRetiredEventPublishedEvent_Throws()
    {
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(fanoutService, moderationFanoutService);

        await Assert.That(async () => await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = Guid.NewGuid(),
            EventType = "EventPublished",
            Payload = "{}"
        })).Throws<InvalidOperationException>();
        await fanoutService.DidNotReceiveWithAnyArgs().FanoutAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutLightModerationAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutHeavyRedactionAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithUnknownEventType_Throws()
    {
        var dispatcher = CreateDispatcher(
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            Substitute.For<IEventModerationNotificationFanoutService>());

        await Assert.That(async () => await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = Guid.NewGuid(),
            EventType = "UnknownEvent",
            Payload = "{}"
        })).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ReconcileDeadLetterAsync_WithManagedTenantPointer_RoutesTerminalCommand()
    {
        var mediator = Substitute.For<IMediator>();
        var dispatcher = CreateDispatcher(
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            Substitute.For<IEventModerationNotificationFanoutService>(),
            mediator: mediator);
        Guid operationId = Guid.CreateVersion7();

        await dispatcher.ReconcileDeadLetterAsync(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateType = nameof(ManagedTenantProvisioningOperation),
            AggregateId = operationId,
            EventType = ManagedTenantProvisioningOutboxEvents.ProcessRequested
        });

        await mediator.Received(1).Send(
            Arg.Is<ReconcileManagedTenantProvisioningDeadLetterCommand>(command =>
                command.OperationId == operationId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType)]
    [Arguments(LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType)]
    [Arguments(LocationPrivacyCorrectionDispatcher.GovernanceCorrectionEventType)]
    public async Task DispatchAsync_WithLocationPrivacyEvent_RoutesToCorrectionDispatcher(string eventType)
    {
        var cache = new RecordingHybridCache();
        CompositeOutboxMessageDispatcher dispatcher = CreateDispatcher(
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            Substitute.For<IEventModerationNotificationFanoutService>(),
            cache: cache);

        await dispatcher.DispatchAsync(CreateLocationPrivacyMessage(eventType));

        await Assert.That(cache.RemovedTags).Contains(CacheTags.EventLocations);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task ReconcileDeadLetterAsync_WithLocationPrivacyEvent_ReplaysCorrectionDispatcher()
    {
        var cache = new RecordingHybridCache();
        CompositeOutboxMessageDispatcher dispatcher = CreateDispatcher(
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            Substitute.For<IEventModerationNotificationFanoutService>(),
            cache: cache);

        await dispatcher.ReconcileDeadLetterAsync(CreateLocationPrivacyMessage(
            LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType));

        await Assert.That(cache.RemovedTags).Contains(CacheTags.EventLocations);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task ReconcileDeadLetterAsync_WithUnsupportedEventType_Throws()
    {
        CompositeOutboxMessageDispatcher dispatcher = CreateDispatcher(
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            Substitute.For<IEventModerationNotificationFanoutService>());

        await Assert.That(async () => await dispatcher.ReconcileDeadLetterAsync(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateType = "Unknown",
            AggregateId = Guid.CreateVersion7(),
            EventType = "UnknownEvent"
        })).Throws<InvalidOperationException>();
    }

    private static CompositeOutboxMessageDispatcher CreateDispatcher(
        IEventPublishedNotificationFanoutService fanoutService,
        IEventModerationNotificationFanoutService moderationFanoutService,
        IReportProviderSyncDispatcher? reportProviderSyncDispatcher = null,
        IMediator? mediator = null,
        INotificationFanoutOccurrenceRepository? occurrenceRepository = null,
        INotificationFanoutRunRepository? runRepository = null,
        IOutboxRepository? outboxRepository = null,
        HybridCache? cache = null)
    {
        var correctionPlanner = Substitute.For<IAtprotoLocationPrivacyCorrectionPlanner>();
        correctionPlanner.PlanLocationPrivacyCorrectionAsync(
                Arg.Any<AtprotoLocationPrivacyCorrectionInput>(),
                Arg.Any<CancellationToken>())
            .Returns(AtprotoPublicationPlanningResult.Skipped("correction_already_planned"));
        return new CompositeOutboxMessageDispatcher(
            new NotificationFanoutOccurrenceHandoffService(
                occurrenceRepository ?? Substitute.For<INotificationFanoutOccurrenceRepository>(),
                runRepository ?? Substitute.For<INotificationFanoutRunRepository>()),
            fanoutService,
            moderationFanoutService,
            reportProviderSyncDispatcher ?? Substitute.For<IReportProviderSyncDispatcher>(),
            new LocationPrivacyCorrectionDispatcher(
                cache ?? new RecordingHybridCache(),
                correctionPlanner),
            outboxRepository ?? Substitute.For<IOutboxRepository>(),
            mediator ?? Substitute.For<IMediator>(),
            NullLogger<CompositeOutboxMessageDispatcher>.Instance);
    }

    private static NotificationFanoutOccurrence CreateOccurrence()
    {
        DateTime occurredAt = DateTime.UtcNow;
        Guid eventId = Guid.CreateVersion7();
        return NotificationFanoutOccurrence.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            eventId,
            sessionId: null,
            occurredAt,
            audienceCutoffAt: occurredAt,
            Guid.CreateVersion7(),
            "{\"fields\":[\"startTime\"]}",
            "{\"startTime\":\"2026-08-01T08:00:00Z\"}",
            "{\"startTime\":\"2026-08-01T09:00:00Z\"}",
            "event.updated",
            templateVersion: 1,
            (int)Explore.Domain.Enums.NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional,
            policyVersion: 1,
            priority: 30,
            notBefore: occurredAt,
            sourceType: "event",
            sourceId: eventId,
            coalescingKey: $"event:{eventId:N}:schedule",
            coalescingWindowEndsAt: occurredAt);
    }

    private static NotificationFanoutRun CreateRun(NotificationFanoutOccurrence occurrence) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = occurrence.TenantId,
            Tenant = null!,
            FanoutOccurrenceId = occurrence.Id,
            FanoutKind = "recipient_occurrence",
            NotificationEntityTypeId = (int)Explore.Domain.Enums.NotificationEntityTypeEnum.Event,
            NotificationEntityType = null!,
            EntityId = occurrence.EventId,
            SourceActorId = Guid.CreateVersion7(),
            SourceActor = null!,
            Status = "pending",
            ConcurrencyStamp = Guid.CreateVersion7()
        };

    private static OutboxMessage CreateLocationPrivacyMessage(string eventType)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid eventLocationId = Guid.CreateVersion7();
        Guid locationId = Guid.CreateVersion7();
        object payload = eventType switch
        {
            LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType => new
            {
                SchemaVersion = 1,
                IntentId = Guid.CreateVersion7(),
                AuthoritySequence = 1,
                LocationId = locationId,
                LocationVersion = Guid.CreateVersion7()
            },
            LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType => new
            {
                SchemaVersion = 1,
                IntentId = Guid.CreateVersion7(),
                AuthoritySequence = 1,
                TenantId = tenantId,
                EventId = eventId,
                EventLocationId = eventLocationId,
                LocationId = (Guid?)null,
                PolicyVersion = 1
            },
            LocationPrivacyCorrectionDispatcher.GovernanceCorrectionEventType => new
            {
                SchemaVersion = 1,
                TenantId = tenantId,
                EventId = eventId,
                EventLocationId = eventLocationId,
                PolicyVersion = 2
            },
            _ => throw new ArgumentOutOfRangeException(nameof(eventType))
        };

        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateType = eventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
                ? nameof(Location)
                : nameof(EventLocation),
            AggregateId = eventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
                ? locationId
                : eventLocationId,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAt = new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    private sealed class RecordingHybridCache : HybridCache
    {
        public List<string> RemovedTags { get; } = [];

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) => factory(state, cancellationToken);

        public override ValueTask RemoveAsync(
            string key,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public override ValueTask RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RemovedTags.Add(tag);
            return ValueTask.CompletedTask;
        }

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
