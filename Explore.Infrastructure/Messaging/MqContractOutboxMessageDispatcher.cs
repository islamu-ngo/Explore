// ABOUTME: Outbox dispatcher that publishes integration events to message broker via MQContract.
// ABOUTME: Routes OutboxMessage.EventType to matching integration event channel using IMessagingProvider.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models.IntegrationEvents;
using Explore.Domain;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Messaging;

/// <summary>
/// Dispatches <see cref="OutboxMessage"/> entries to the message broker via <see cref="IMessagingProvider"/>.
/// Deserializes <see cref="OutboxMessage.Payload"/> to the appropriate integration event type based on
/// <see cref="OutboxMessage.EventType"/>, then publishes to the configured channel.
/// </summary>
public sealed class MqContractOutboxMessageDispatcher(
    IMessagingProvider messagingProvider,
    ILogger<MqContractOutboxMessageDispatcher> logger) : IOutboxMessageDispatcher
{
    private const string DefaultChannel = "integration.events";

    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message.Payload))
        {
            logger.LogWarning(
                "Outbox message {Id} has no payload for EventType={EventType}. Skipping dispatch.",
                message.Id, message.EventType);
            return;
        }

        try
        {
            var integrationEvent = DeserializePayload(message);

            var channel = DetermineChannel(message.EventType);

            logger.LogInformation(
                "Publishing integration event {EventType} for {AggregateType}/{AggregateId} to channel {Channel}",
                message.EventType, message.AggregateType, message.AggregateId, channel);

            await messagingProvider.PublishAsync(integrationEvent, channel, ct);

            logger.LogDebug(
                "Successfully published message {Id} to channel {Channel}",
                message.Id, channel);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex,
                "Failed to deserialize payload for message {Id}, EventType={EventType}",
                message.Id, message.EventType);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to dispatch message {Id} to message broker",
                message.Id);
            throw;
        }
    }

    private object DeserializePayload(OutboxMessage message)
    {
        // Route by EventType to matching integration event record
        return message.EventType switch
        {
            "EventPublished" => JsonSerializer.Deserialize<EventPublishedIntegrationEvent>(message.Payload!)
                ?? throw new JsonException($"Failed to deserialize EventPublishedIntegrationEvent for message {message.Id}"),

            // Future: Add more event types here
            // "RegistrationConfirmed" => JsonSerializer.Deserialize<RegistrationConfirmedIntegrationEvent>(message.Payload!) ?? throw new JsonException(...),
            // "PaymentProcessed" => JsonSerializer.Deserialize<PaymentProcessedIntegrationEvent>(message.Payload!) ?? throw new JsonException(...),

            _ => throw new InvalidOperationException(
                $"Unknown EventType '{message.EventType}' for message {message.Id}. " +
                $"Add deserialization case in {nameof(MqContractOutboxMessageDispatcher)}.{nameof(DeserializePayload)}.")
        };
    }

    /// <summary>
    /// Maps EventType to message broker channel.
    /// Override this mapping per deployment if needed (e.g., via configuration).
    /// </summary>
    private static string DetermineChannel(string eventType)
    {
        // For now, all integration events go to a single channel
        // Future: route by event type prefix, tenant, or environment
        return eventType switch
        {
            "EventPublished" => "events.published",
            _ => DefaultChannel
        };
    }
}
