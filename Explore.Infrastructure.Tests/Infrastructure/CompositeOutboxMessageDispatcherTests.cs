// ABOUTME: Unit tests for composite outbox dispatch routing between MQContract and internal fanout.
// ABOUTME: Verifies internal notification fanout does not go through external broker dispatch.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Models.IntegrationEvents;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class CompositeOutboxMessageDispatcherTests
{
    [Test]
    public async Task DispatchAsync_WithInternalFanoutEvent_RoutesToFanoutServiceOnly()
    {
        var messagingProvider = Substitute.For<IMessagingProvider>();
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(messagingProvider, fanoutService, moderationFanoutService);
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
        await messagingProvider.DidNotReceiveWithAnyArgs().PublishAsync<EventPublishedIntegrationEvent>(default!, default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithLightModerationFanoutEvent_RoutesToModerationFanoutServiceOnly()
    {
        var messagingProvider = Substitute.For<IMessagingProvider>();
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(messagingProvider, fanoutService, moderationFanoutService);
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
        await messagingProvider.DidNotReceiveWithAnyArgs().PublishAsync<EventPublishedIntegrationEvent>(default!, default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithHeavyRedactionFanoutEvent_RoutesToModerationFanoutServiceOnly()
    {
        var messagingProvider = Substitute.For<IMessagingProvider>();
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(messagingProvider, fanoutService, moderationFanoutService);
        var request = new EventHeavyRedactedNotificationFanoutRequested
        {
            TenantId = Guid.NewGuid(),
            ModerationRecordId = Guid.NewGuid(),
            SourceActorId = Guid.NewGuid(),
            RedactedAt = DateTimeOffset.UtcNow
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
                && payload.SourceActorId == request.SourceActorId),
            Arg.Any<CancellationToken>());
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutLightModerationAsync(default!, default);
        await fanoutService.DidNotReceiveWithAnyArgs().FanoutAsync(default!, default);
        await messagingProvider.DidNotReceiveWithAnyArgs().PublishAsync<EventPublishedIntegrationEvent>(default!, default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithEventPublishedEvent_RoutesToMqDispatcherOnly()
    {
        var messagingProvider = Substitute.For<IMessagingProvider>();
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var moderationFanoutService = Substitute.For<IEventModerationNotificationFanoutService>();
        var dispatcher = CreateDispatcher(messagingProvider, fanoutService, moderationFanoutService);
        var integrationEvent = new EventPublishedIntegrationEvent
        {
            TenantId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Title = "Community Iftar",
            StartDate = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        await dispatcher.DispatchAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "Event",
            AggregateId = integrationEvent.EventId,
            EventType = PublishEventCommandHandler.EventPublishedEventType,
            Payload = JsonSerializer.Serialize(integrationEvent)
        });

        await messagingProvider.Received(1).PublishAsync(
            Arg.Any<EventPublishedIntegrationEvent>(),
            "events.published",
            Arg.Any<CancellationToken>());
        await fanoutService.DidNotReceiveWithAnyArgs().FanoutAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutLightModerationAsync(default!, default);
        await moderationFanoutService.DidNotReceiveWithAnyArgs().FanoutHeavyRedactionAsync(default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithUnknownEventType_Throws()
    {
        var dispatcher = CreateDispatcher(
            Substitute.For<IMessagingProvider>(),
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

    private static CompositeOutboxMessageDispatcher CreateDispatcher(
        IMessagingProvider messagingProvider,
        IEventPublishedNotificationFanoutService fanoutService,
        IEventModerationNotificationFanoutService moderationFanoutService)
    {
        var mqDispatcher = new MqContractOutboxMessageDispatcher(
            messagingProvider,
            NullLogger<MqContractOutboxMessageDispatcher>.Instance);

        return new CompositeOutboxMessageDispatcher(
            mqDispatcher,
            fanoutService,
            moderationFanoutService,
            NullLogger<CompositeOutboxMessageDispatcher>.Instance);
    }
}
