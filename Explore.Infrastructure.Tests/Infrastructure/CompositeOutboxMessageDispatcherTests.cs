// ABOUTME: Unit tests for composite outbox dispatch routing between MQContract and internal fanout.
// ABOUTME: Verifies internal notification fanout does not go through external broker dispatch.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Models.IntegrationEvents;
using Explore.Application.Models.InternalEvents;
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
        var dispatcher = CreateDispatcher(messagingProvider, fanoutService);
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
        await messagingProvider.DidNotReceiveWithAnyArgs().PublishAsync<EventPublishedIntegrationEvent>(default!, default!, default);
    }

    [Test]
    public async Task DispatchAsync_WithEventPublishedEvent_RoutesToMqDispatcherOnly()
    {
        var messagingProvider = Substitute.For<IMessagingProvider>();
        var fanoutService = Substitute.For<IEventPublishedNotificationFanoutService>();
        var dispatcher = CreateDispatcher(messagingProvider, fanoutService);
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
    }

    [Test]
    public async Task DispatchAsync_WithUnknownEventType_Throws()
    {
        var dispatcher = CreateDispatcher(
            Substitute.For<IMessagingProvider>(),
            Substitute.For<IEventPublishedNotificationFanoutService>());

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
        IEventPublishedNotificationFanoutService fanoutService)
    {
        var mqDispatcher = new MqContractOutboxMessageDispatcher(
            messagingProvider,
            NullLogger<MqContractOutboxMessageDispatcher>.Instance);

        return new CompositeOutboxMessageDispatcher(
            mqDispatcher,
            fanoutService,
            NullLogger<CompositeOutboxMessageDispatcher>.Instance);
    }
}
