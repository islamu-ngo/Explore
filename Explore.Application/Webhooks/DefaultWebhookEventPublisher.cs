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

        if (message is null)
        {
            var creation = await CreateMessageAsync(context, providerName, cancellationToken);
            if (creation.Failure is not null)
            {
                return creation.Failure;
            }

            message = creation.Message!;
        }
        else if (IsProviderSettled(message.Status))
        {
            return WebhookEventPublishResult.Success(message.Id, message.ProviderMessageId);
        }

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
            await messageRepository.MarkProviderQueuedAsync(
                message.TenantId,
                message.Id,
                providerResult.ProviderMessageId,
                DateTime.UtcNow,
                cancellationToken);

            return WebhookEventPublishResult.Success(message.Id, providerResult.ProviderMessageId);
        }

        await messageRepository.MarkProviderFailedAsync(
            message.TenantId,
            message.Id,
            DateTime.UtcNow,
            cancellationToken);

        return WebhookEventPublishResult.Failure(
            message.Id,
            providerResult.FailureCategory ?? "webhook_provider_failed",
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
            ProviderMode = ResolveProviderMode(providerName),
            Status = WebhookMessageStatus.Pending,
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

    private static bool IsProviderSettled(WebhookMessageStatus status) =>
        status is WebhookMessageStatus.Queued
            or WebhookMessageStatus.Delivered
            or WebhookMessageStatus.PartiallyFailed
            or WebhookMessageStatus.Cancelled;

    private static bool IsDisabledProvider(string providerName) =>
        string.Equals(providerName, "Disabled", StringComparison.OrdinalIgnoreCase);

    private static WebhookProviderMode ResolveProviderMode(string providerName)
    {
        if (string.Equals(providerName, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return WebhookProviderMode.Disabled;
        }

        if (string.Equals(providerName, "Svix", StringComparison.OrdinalIgnoreCase))
        {
            return WebhookProviderMode.Svix;
        }

        if (string.Equals(providerName, "Composite", StringComparison.OrdinalIgnoreCase))
        {
            return WebhookProviderMode.Composite;
        }

        if (string.Equals(providerName, "DryRun", StringComparison.OrdinalIgnoreCase))
        {
            return WebhookProviderMode.DryRun;
        }

        return WebhookProviderMode.Local;
    }

    private sealed record MessageCreation(WebhookMessage? Message, WebhookEventPublishResult? Failure);
}
