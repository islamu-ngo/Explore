// ABOUTME: Maps LocalProvider webhook delivery attempts into safe operations DTOs.
// ABOUTME: Preserves bounded response previews while keeping endpoint secrets and payload bodies out of reads.

using Explore.Application.DTOs.Webhooks;
using Explore.Domain;

namespace Explore.Application.Features.Webhooks.Handlers;

internal static class WebhookDeliveryAttemptDtoMapper
{
    public static WebhookDeliveryAttemptDto Map(WebhookDeliveryAttempt attempt) =>
        new()
        {
            Id = attempt.Id,
            TenantId = attempt.TenantId,
            MessageId = attempt.MessageId,
            MessageEventType = attempt.Message?.EventType,
            EndpointId = attempt.EndpointId,
            EndpointUrl = attempt.Endpoint?.Url,
            EndpointStatusName = (attempt.Endpoint?.Status ?? WebhookEndpointStatus.Archived).ToString(),
            AttemptNumber = attempt.AttemptNumber,
            StatusId = (int)attempt.Status,
            StatusName = attempt.Status.ToString(),
            ScheduledAt = attempt.ScheduledAt,
            SentAt = attempt.SentAt,
            CompletedAt = attempt.CompletedAt,
            HttpStatusCode = attempt.HttpStatusCode,
            FailureCategory = attempt.FailureCategory,
            ResponseBodyPreview = attempt.ResponseBodyPreview,
            DurationMs = attempt.DurationMs,
            NextRetryAt = attempt.NextRetryAt,
            CreatedAt = attempt.CreatedAt,
            UpdatedAt = attempt.UpdatedAt
        };
}
