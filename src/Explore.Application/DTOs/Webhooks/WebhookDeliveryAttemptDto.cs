// ABOUTME: API DTO for LocalProvider webhook delivery attempt audit rows.
// ABOUTME: Surfaces safe HTTP outcome metadata for operations without exposing secrets or full bodies.

namespace Explore.Application.DTOs.Webhooks;

public sealed class WebhookDeliveryAttemptDto
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }

    public Guid MessageId { get; init; }

    public string? MessageEventType { get; init; }

    public Guid EndpointId { get; init; }

    public string? EndpointUrl { get; init; }

    public required string EndpointStatusName { get; init; }

    public int AttemptNumber { get; init; }

    public int StatusId { get; init; }

    public required string StatusName { get; init; }

    public DateTime ScheduledAt { get; init; }

    public DateTime? SentAt { get; init; }

    public DateTime? CompletedAt { get; init; }

    public int? HttpStatusCode { get; init; }

    public string? FailureCategory { get; init; }

    public string? ResponseBodyPreview { get; init; }

    public int? DurationMs { get; init; }

    public DateTime? NextRetryAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
