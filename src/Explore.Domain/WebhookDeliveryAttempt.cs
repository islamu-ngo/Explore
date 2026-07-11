// ABOUTME: LocalProvider delivery attempt ledger for one webhook message and endpoint pair.
// ABOUTME: Captures safe HTTP outcome metadata, retry scheduling, and worker claim state.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class WebhookDeliveryAttempt : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid MessageId { get; set; }
    public WebhookMessage? Message { get; set; }
    public Guid EndpointId { get; set; }
    public WebhookEndpoint? Endpoint { get; set; }

    public int AttemptNumber { get; set; }
    public WebhookDeliveryAttemptStatus Status { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? ProcessingLeaseToken { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? FailureCategory { get; set; }
    public string? ResponseBodyPreview { get; set; }
    public int? DurationMs { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public enum WebhookDeliveryAttemptStatus
{
    Scheduled = 1,
    Sending = 2,
    Succeeded = 3,
    Failed = 4,
    Abandoned = 5
}
