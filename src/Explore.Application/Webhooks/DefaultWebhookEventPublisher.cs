// ABOUTME: Default application service that persists canonical webhook messages and dispatches providers.
// ABOUTME: Bridges payload building, idempotent message creation, provider publish, and safe business metrics.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Telemetry;
using Explore.Domain;

namespace Explore.Application.Webhooks;

public sealed class DefaultWebhookEventPublisher(
    IWebhookPayloadBuilder payloadBuilder,
    IWebhookMessageRepository messageRepository,
    IWebhookDeliveryProvider deliveryProvider,
    BusinessMetrics metrics) : IWebhookEventPublisher
{
    public async Task<WebhookEventPublishResult> PublishAsync(
        WebhookEventBuildContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var providerName = deliveryProvider.ProviderName;
        if (IsDisabledProvider(providerName))
        {
            return WebhookEventPublishResult.SkippedResult(
                "webhooks_disabled",
                "Outgoing product webhooks are disabled.");
        }

        var message = await messageRepository.GetByTenantAndIdAsync(
            context.TenantId,
            context.MessageId,
            cancellationToken);

        if (message is not null)
        {
            var duplicatePayload = await payloadBuilder.BuildAsync(context, cancellationToken);
            if (!duplicatePayload.Succeeded)
            {
                return WebhookEventPublishResult.Failure(
                    message.Id,
                    duplicatePayload.FailureCategory ?? "webhook_payload_build_failed",
                    isRetryable: false,
                    duplicatePayload.SafeDetail);
            }

            if (!string.Equals(message.PayloadHash, duplicatePayload.PayloadHash, StringComparison.Ordinal))
            {
                return WebhookEventPublishResult.Failure(
                    message.Id,
                    "webhook_payload_conflict",
                    isRetryable: false,
                    "The message identity already exists with different payload bytes.");
            }

            return WebhookEventPublishResult.Success(message.Id);
        }

        var creation = await CreateMessageAsync(context, providerName, cancellationToken);
        if (creation.Failure is not null)
        {
            return creation.Failure;
        }

        message = creation.Message!;

        var payloadBytes = message.GetPayloadBytes();
        if (payloadBytes is null)
        {
            return WebhookEventPublishResult.Failure(
                message.Id,
                message.PayloadClearedAt is null ? "payload_unavailable" : "message_payload_cleared",
                isRetryable: false);
        }

        var providerResult = await deliveryProvider.PublishAsync(
            CreateProviderMessage(message, payloadBytes),
            cancellationToken);

        if (providerResult.Succeeded)
        {
            return WebhookEventPublishResult.Success(message.Id, providerResult.ProviderMessageId);
        }

        var failureCategory = providerResult.FailureCategory ?? "webhook_provider_failed";
        metrics.RecordWebhookProviderPublishFailure(
            message.TenantId.ToString("D"),
            message.EventType,
            providerName,
            failureCategory);

        return WebhookEventPublishResult.Failure(
            message.Id,
            failureCategory,
            providerResult.IsRetryable,
            providerResult.SafeDetail);
    }

    private async Task<MessageCreation> CreateMessageAsync(
        WebhookEventBuildContext context,
        string providerName,
        CancellationToken cancellationToken)
    {
        var payload = await payloadBuilder.BuildAsync(context, cancellationToken);
        if (!payload.Succeeded)
        {
            return new MessageCreation(null, WebhookEventPublishResult.Failure(
                context.MessageId,
                payload.FailureCategory ?? "webhook_payload_build_failed",
                isRetryable: false,
                payload.SafeDetail));
        }

        var message = WebhookMessage.Create(
            context.MessageId,
            context.TenantId,
            context.EventType,
            context.EventId,
            context.AggregateKind,
            context.AggregateId,
            context.ConsumerId,
            payload.PayloadBytes!,
            payload.PayloadRetentionUntil!.Value.UtcDateTime,
            DateTime.UtcNow);

        if (!string.Equals(message.PayloadHash, payload.PayloadHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Payload builder hash does not match the exact bytes it returned.");
        }

        var created = await messageRepository.CreateAsync(message, cancellationToken);
        metrics.RecordWebhookMessageCreated(
            context.TenantId.ToString("D"),
            context.EventType,
            providerName,
            "created");

        return new MessageCreation(created, null);
    }

    private static WebhookProviderMessage CreateProviderMessage(WebhookMessage message, byte[] payloadBytes) =>
        new(
            message.Id,
            message.TenantId,
            message.ConsumerId,
            message.EventType,
            message.EventId,
            message.AggregateKind,
            message.AggregateId,
            payloadBytes,
            message.PayloadHash,
            message.PayloadRetentionUntil);

    private static bool IsDisabledProvider(string providerName) =>
        string.Equals(providerName, "Disabled", StringComparison.OrdinalIgnoreCase);

    private sealed record MessageCreation(WebhookMessage? Message, WebhookEventPublishResult? Failure);
}
