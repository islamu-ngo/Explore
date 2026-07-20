// ABOUTME: Routes durable outbox messages to local application side-effect dispatchers.
// ABOUTME: Handles notification fanout and provider synchronization after durable transaction commits.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Management.Handlers.Commands;
using Explore.Application.Features.Management.Requests.Commands;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Infrastructure.Services.Moderation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Messaging;

public sealed class CompositeOutboxMessageDispatcher(
    NotificationFanoutOccurrenceHandoffService notificationFanoutOccurrenceHandoffService,
    IEventPublishedNotificationFanoutService notificationFanoutService,
    IEventModerationNotificationFanoutService moderationNotificationFanoutService,
    IReportProviderSyncDispatcher reportProviderSyncDispatcher,
    LocationPrivacyCorrectionDispatcher locationPrivacyCorrectionDispatcher,
    IOutboxRepository outboxRepository,
    IMediator mediator,
    ILogger<CompositeOutboxMessageDispatcher> logger) : IOutboxMessageDispatcher
{
    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct = default)
    {
        switch (message.EventType)
        {
            case NotificationFanoutOccurrenceOutboxMessageFactory.EventType:
                await notificationFanoutOccurrenceHandoffService.HandoffAsync(message, ct);
                return;

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

            case LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType:
            case LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType:
            case LocationPrivacyCorrectionDispatcher.GovernanceCorrectionEventType:
                await locationPrivacyCorrectionDispatcher.DispatchAsync(message, ct);
                return;

            case ManagedTenantProvisioningOutboxEvents.ProcessRequested:
                await mediator.Send(
                    new ProcessManagedTenantProvisioningOperationCommand(message.AggregateId, message.Id),
                    ct);
                return;

            default:
                throw new InvalidOperationException(
                    $"Unknown outbox EventType '{message.EventType}' for message {message.Id}. " +
                    $"Add a route in {nameof(CompositeOutboxMessageDispatcher)}.");
        }
    }

    public async Task ReconcileDeadLetterAsync(
        OutboxMessage message,
        CancellationToken ct = default)
    {
        switch (message.EventType)
        {
            case ManagedTenantProvisioningOutboxEvents.ProcessRequested:
                await mediator.Send(
                    new ReconcileManagedTenantProvisioningDeadLetterCommand(message.AggregateId, message.Id),
                    ct);
                return;

            case LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType:
            case LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType:
            case LocationPrivacyCorrectionDispatcher.GovernanceCorrectionEventType:
                await locationPrivacyCorrectionDispatcher.DispatchAsync(message, ct);
                return;

            default:
                throw new InvalidOperationException(
                    $"Outbox EventType '{message.EventType}' for message {message.Id} " +
                    "does not support dead-letter reconciliation.");
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

        EventHeavyRedactedNotificationFanoutPayloadParseResult parsed;
        try
        {
            parsed = EventHeavyRedactedNotificationFanoutPayloadParser.Parse(message.Payload);
        }
        catch (JsonException)
        {
            await outboxRepository.TryReplaceProcessingPayloadAsync(
                message.Id,
                message.Payload,
                EventHeavyRedactedNotificationFanoutPayloadParser.SafeInvalidPayload,
                cancellationToken);
            throw new JsonException("The heavy moderation fanout payload is invalid.");
        }

        if (parsed.WasLegacy)
        {
            bool scrubbed = await outboxRepository.TryReplaceProcessingPayloadAsync(
                message.Id,
                message.Payload,
                parsed.CanonicalPayload,
                cancellationToken);
            if (!scrubbed)
            {
                throw new InvalidOperationException("The retained heavy moderation fanout payload could not be safely replaced.");
            }

            message.Payload = parsed.CanonicalPayload;
        }

        logger.LogInformation(
            "Dispatching internal heavy redaction notification fanout for moderation record {ModerationRecordId} from outbox message {MessageId}",
            parsed.Request.ModerationRecordId,
            message.Id);

        await moderationNotificationFanoutService.FanoutHeavyRedactionAsync(parsed.Request, cancellationToken);
    }
}
