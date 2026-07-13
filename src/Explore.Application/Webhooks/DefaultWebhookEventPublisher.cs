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
            return WebhookEventPublishResult.Success(message.Id);
        }

        var creation = await CreateMessageAsync(context, providerName, cancellationToken);
        if (creation.Failure is not null)
        {
            return creation.Failure;
        }

        message = creation.Message!;

        if (string.IsNullOrWhiteSpace(message.PayloadJson))
        {
            return WebhookEventPublishResult.Failure(
                message.Id,
                message.PayloadClearedAt is null ? "payload_unavailable" : "message_payload_cleared",
                isRetryable: false);
        }

        var providerResult = await deliveryProvider.PublishAsync(
            CreateProviderMessage(message),
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

        var message = new WebhookMessage
        {
            Id = context.MessageId,
            TenantId = context.TenantId,
            EventType = context.EventType,
            EventId = context.EventId,
            AggregateKind = context.AggregateKind,
            AggregateId = context.AggregateId,
            ConsumerId = context.ConsumerId,
            PayloadJson = payload.RawPayloadJson,
            PayloadHash = payload.PayloadHash!,
            PayloadRetentionUntil = payload.PayloadRetentionUntil!.Value.UtcDateTime,
            CreatedAt = DateTime.UtcNow
        };

        var created = await messageRepository.CreateAsync(message, cancellationToken);
        metrics.RecordWebhookMessageCreated(
            context.TenantId.ToString("D"),
            context.EventType,
            providerName,
            "created");

        return new MessageCreation(created, null);
    }

    private static WebhookProviderMessage CreateProviderMessage(WebhookMessage message) =>
        new(
            message.Id,
            message.TenantId,
            message.ConsumerId,
            message.EventType,
            message.EventId,
            message.AggregateKind,
            message.AggregateId,
            message.PayloadJson!,
            message.PayloadHash,
            message.PayloadRetentionUntil);

    private static bool IsDisabledProvider(string providerName) =>
        string.Equals(providerName, "Disabled", StringComparison.OrdinalIgnoreCase);

    private sealed record MessageCreation(WebhookMessage? Message, WebhookEventPublishResult? Failure);
}
