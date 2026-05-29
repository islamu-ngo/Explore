// ABOUTME: Routes durable outbox messages to external MQ or internal application dispatchers.
// ABOUTME: Preserves external MQContract publishing while handling local notification fanout events.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Models.InternalEvents;
using Explore.Domain;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Messaging;

public sealed class CompositeOutboxMessageDispatcher(
    MqContractOutboxMessageDispatcher mqContractDispatcher,
    IEventPublishedNotificationFanoutService notificationFanoutService,
    ILogger<CompositeOutboxMessageDispatcher> logger) : IOutboxMessageDispatcher
{
    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct = default)
    {
        switch (message.EventType)
        {
            case PublishEventCommandHandler.EventPublishedEventType:
                await mqContractDispatcher.DispatchAsync(message, ct);
                return;

            case PublishEventCommandHandler.EventPublishedNotificationFanoutRequestedEventType:
                await DispatchNotificationFanoutAsync(message, ct);
                return;

            default:
                throw new InvalidOperationException(
                    $"Unknown outbox EventType '{message.EventType}' for message {message.Id}. " +
                    $"Add a route in {nameof(CompositeOutboxMessageDispatcher)}.");
        }
    }

    private async Task DispatchNotificationFanoutAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Payload))
        {
            throw new InvalidOperationException($"Outbox message {message.Id} has no payload for notification fanout.");
        }

        var request = JsonSerializer.Deserialize<EventPublishedNotificationFanoutRequested>(message.Payload)
            ?? throw new JsonException($"Failed to deserialize notification fanout payload for message {message.Id}.");

        logger.LogInformation(
            "Dispatching internal notification fanout for event {EventId} from outbox message {MessageId}",
            request.EventId,
            message.Id);

        await notificationFanoutService.FanoutAsync(request, cancellationToken);
    }
}
