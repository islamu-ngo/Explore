// ABOUTME: Routes durable outbox messages to local application side-effect dispatchers.
// ABOUTME: Handles notification fanout and provider synchronization after durable transaction commits.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Infrastructure.Services.Moderation;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Messaging;

public sealed class CompositeOutboxMessageDispatcher(
    IEventPublishedNotificationFanoutService notificationFanoutService,
    IEventModerationNotificationFanoutService moderationNotificationFanoutService,
    IReportProviderSyncDispatcher reportProviderSyncDispatcher,
    ILogger<CompositeOutboxMessageDispatcher> logger) : IOutboxMessageDispatcher
{
    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct = default)
    {
        switch (message.EventType)
        {
            case PublishEventCommandHandler.EventPublishedNotificationFanoutRequestedEventType:
                await DispatchNotificationFanoutAsync(message, ct);
                return;

            case EventModerationOutboxMessageFactory.EventLightModeratedNotificationFanoutRequestedEventType:
                await DispatchLightModerationNotificationFanoutAsync(message, ct);
                return;

            case EventModerationOutboxMessageFactory.EventHeavyRedactedNotificationFanoutRequestedEventType:
                await DispatchHeavyRedactionNotificationFanoutAsync(message, ct);
                return;

            case EventReportOutboxMessageFactory.EventReportProviderSyncRequestedEventType:
                await reportProviderSyncDispatcher.DispatchAsync(message, ct);
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

    private async Task DispatchLightModerationNotificationFanoutAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Payload))
        {
            throw new InvalidOperationException($"Outbox message {message.Id} has no payload for light moderation notification fanout.");
        }

        var request = JsonSerializer.Deserialize<EventLightModeratedNotificationFanoutRequested>(message.Payload)
            ?? throw new JsonException($"Failed to deserialize light moderation notification fanout payload for message {message.Id}.");

        logger.LogInformation(
            "Dispatching internal light moderation notification fanout for event {EventId} from outbox message {MessageId}",
            request.EventId,
            message.Id);

        await moderationNotificationFanoutService.FanoutLightModerationAsync(request, cancellationToken);
    }

    private async Task DispatchHeavyRedactionNotificationFanoutAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Payload))
        {
            throw new InvalidOperationException($"Outbox message {message.Id} has no payload for heavy redaction notification fanout.");
        }

        var request = JsonSerializer.Deserialize<EventHeavyRedactedNotificationFanoutRequested>(message.Payload)
            ?? throw new JsonException($"Failed to deserialize heavy redaction notification fanout payload for message {message.Id}.");

        logger.LogInformation(
            "Dispatching internal heavy redaction notification fanout for moderation record {ModerationRecordId} from outbox message {MessageId}",
            request.ModerationRecordId,
            message.Id);

        await moderationNotificationFanoutService.FanoutHeavyRedactionAsync(request, cancellationToken);
    }
}
