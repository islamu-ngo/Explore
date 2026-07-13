// ABOUTME: Svix outgoing webhook provider that publishes canonical ISLAMU messages to Svix.
// ABOUTME: Ensures app mapping, idempotent message creation, provider-link audit state, and safe failures.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Domain;
using Svix;

namespace Explore.Infrastructure.Webhooks;

public sealed class SvixWebhookDeliveryProvider(
    ISvixWebhookClient svixClient,
    IWebhookConsumerRepository consumerRepository,
    IWebhookProviderLinkRepository providerLinkRepository) : IWebhookDeliveryProvider
{
    private const string Provider = "Svix";

    public string ProviderName => Provider;

    public async Task<WebhookProviderPublishResult> PublishAsync(
        WebhookProviderMessage message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingLink = await providerLinkRepository.GetByTenantMessageAndProviderAsync(
            message.TenantId,
            WebhookExternalProvider.Svix,
            message.MessageId,
            cancellationToken);

        if (existingLink?.SyncState == WebhookProviderLinkSyncState.Synced
            && !string.IsNullOrWhiteSpace(existingLink.ExternalMessageId))
        {
            return WebhookProviderPublishResult.Success(existingLink.ExternalMessageId);
        }

        try
        {
            var app = await ResolveApplicationAsync(message, cancellationToken);
            var createdMessage = await svixClient.CreateMessageAsync(
                CreateMessageRequest(message, app.AppUid),
                cancellationToken);

            await providerLinkRepository.CreateAsync(
                new WebhookProviderLink
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = message.TenantId,
                    ConsumerId = message.ConsumerId,
                    MessageId = message.MessageId,
                    Provider = WebhookExternalProvider.Svix,
                    ExternalMessageId = createdMessage.MessageId,
                    SyncState = WebhookProviderLinkSyncState.Synced,
                    LastSyncedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);

            return WebhookProviderPublishResult.Success(createdMessage.MessageId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SvixWebhookConfigurationException ex)
        {
            return WebhookProviderPublishResult.Failure(ex.FailureCategory, isRetryable: false, ex.FailureCategory);
        }
        catch (ApiException ex)
        {
            var failure = SvixWebhookFailureClassifier.Classify(ex);
            return WebhookProviderPublishResult.Failure(failure.Category, failure.IsRetryable, failure.SafeDetail);
        }
        catch (Exception ex)
        {
            return WebhookProviderPublishResult.Failure(
                "svix_provider_failed",
                isRetryable: true,
                ex.GetType().Name);
        }
    }

    private async Task<SvixApplicationSyncResult> ResolveApplicationAsync(
        WebhookProviderMessage message,
        CancellationToken cancellationToken)
    {
        WebhookConsumer? consumer = null;
        if (message.ConsumerId is { } consumerId)
        {
            consumer = await consumerRepository.GetByTenantAndIdAsync(
                message.TenantId,
                consumerId,
                cancellationToken);
        }

        return await svixClient.GetOrCreateApplicationAsync(
            SvixWebhookApplicationMapper.CreateSyncRequest(message.TenantId, message.ConsumerId, consumer),
            cancellationToken);
    }

    private static SvixMessageCreateRequest CreateMessageRequest(WebhookProviderMessage message, string appUid) =>
        new(
            message.TenantId,
            appUid,
            message.EventType,
            message.MessageId.ToString("D"),
            message.PayloadBytes.ToArray(),
            CalculateRetentionDays(message.PayloadRetentionUntil),
            message.MessageId.ToString("D"));

    private static int CalculateRetentionDays(DateTimeOffset payloadRetentionUntil)
    {
        var remaining = payloadRetentionUntil.ToUniversalTime() - DateTimeOffset.UtcNow;
        return Math.Clamp((int)Math.Ceiling(remaining.TotalDays), 1, 365);
    }

}
