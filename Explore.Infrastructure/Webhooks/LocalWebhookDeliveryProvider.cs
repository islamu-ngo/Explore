// ABOUTME: Local outgoing webhook provider that fans canonical messages out to local endpoint attempts.
// ABOUTME: Creates durable scheduled delivery attempt rows while HTTP sending remains worker-owned.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Domain;

namespace Explore.Infrastructure.Webhooks;

public sealed class LocalWebhookDeliveryProvider : IWebhookDeliveryProvider
{
    private readonly IWebhookEndpointRepository _endpointRepository;
    private readonly IWebhookDeliveryAttemptRepository _attemptRepository;
    private readonly WebhookRetryScheduler _retryScheduler;

    public LocalWebhookDeliveryProvider(
        IWebhookEndpointRepository endpointRepository,
        IWebhookDeliveryAttemptRepository attemptRepository,
        WebhookRetryScheduler retryScheduler)
    {
        _endpointRepository = endpointRepository;
        _attemptRepository = attemptRepository;
        _retryScheduler = retryScheduler;
    }

    public string ProviderName => "Local";

    public async Task<WebhookProviderPublishResult> PublishAsync(
        WebhookProviderMessage message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingAttempts = await _attemptRepository.GetByMessageAsync(
            message.TenantId,
            message.MessageId,
            cancellationToken);

        if (existingAttempts.Count > 0)
        {
            return WebhookProviderPublishResult.Success(message.MessageId.ToString("N"));
        }

        var endpoints = await _endpointRepository.GetActiveSubscribedEndpointsAsync(
            message.TenantId,
            message.EventType,
            WebhookProviderMode.Local,
            cancellationToken);

        if (message.ConsumerId is { } consumerId)
        {
            endpoints = endpoints.Where(endpoint => endpoint.ConsumerId == consumerId).ToList();
        }

        if (endpoints.Count == 0)
        {
            return WebhookProviderPublishResult.Success(message.MessageId.ToString("N"));
        }

        var now = DateTime.UtcNow;
        var scheduledAt = _retryScheduler.GetScheduledAtUtc(1, now);
        var attempts = endpoints.Select(endpoint => new WebhookDeliveryAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = message.TenantId,
            MessageId = message.MessageId,
            EndpointId = endpoint.Id,
            AttemptNumber = 1,
            Status = WebhookDeliveryAttemptStatus.Scheduled,
            ScheduledAt = scheduledAt,
            CreatedAt = now
        }).ToList();

        await _attemptRepository.CreateManyAsync(attempts, cancellationToken);

        return WebhookProviderPublishResult.Success(message.MessageId.ToString("N"));
    }
}
