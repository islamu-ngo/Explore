// ABOUTME: Tenant-scoped outgoing webhook endpoint with provider ids, secret refs, and delivery controls.
// ABOUTME: LocalProvider treats this row as authoritative while SvixProvider can mirror provider endpoint ids.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class WebhookEndpoint : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid ConsumerId { get; set; }
    public WebhookConsumer? Consumer { get; set; }

    public required string Url { get; set; }
    public string? Description { get; set; }
    public WebhookEndpointStatus Status { get; set; }
    public required string SecretRef { get; set; }
    public int SecretVersion { get; set; }
    public string? PreviousSecretRef { get; set; }
    public DateTime? PreviousSecretValidUntil { get; set; }
    public string? ProviderEndpointId { get; set; }
    public int MaxAttempts { get; set; }
    public int TimeoutSeconds { get; set; }
    public int? RateLimitPerMinute { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public int ConsecutiveFailureCount { get; set; }
    public DateTime? CircuitOpenedAt { get; set; }
    public DateTime? AutoPausedAt { get; set; }
    public string? AutoPauseReason { get; set; }
    public DateTime? LastResumedAt { get; set; }
    public Guid? LastResumedBy { get; set; }
    public long DeliveryStateVersion { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public List<WebhookEndpointSubscription> Subscriptions { get; } = [];

    public bool RecordFailure(DateTime failedAt, string failureCategory, int autoPauseThreshold)
    {
        if (autoPauseThreshold < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(autoPauseThreshold));
        }

        LastFailureAt = failedAt;
        UpdatedAt = failedAt;

        if (Status != WebhookEndpointStatus.Active)
        {
            return false;
        }

        ConsecutiveFailureCount++;
        DeliveryStateVersion++;
        if (ConsecutiveFailureCount < autoPauseThreshold)
        {
            return false;
        }

        Status = WebhookEndpointStatus.AutoPaused;
        CircuitOpenedAt = failedAt;
        AutoPausedAt = failedAt;
        AutoPauseReason = failureCategory;
        return true;
    }

    public void RecordSuccess(DateTime succeededAt)
    {
        LastSuccessAt = succeededAt;
        UpdatedAt = succeededAt;
        if (Status == WebhookEndpointStatus.Active)
        {
            ConsecutiveFailureCount = 0;
            DeliveryStateVersion++;
        }
    }

    public bool Resume(DateTime resumedAt, Guid actorUserId)
    {
        if (Status != WebhookEndpointStatus.AutoPaused)
        {
            return false;
        }

        Status = WebhookEndpointStatus.Active;
        ConsecutiveFailureCount = 0;
        CircuitOpenedAt = null;
        AutoPausedAt = null;
        AutoPauseReason = null;
        LastResumedAt = resumedAt;
        LastResumedBy = actorUserId;
        UpdatedAt = resumedAt;
        UpdatedBy = actorUserId;
        DeliveryStateVersion++;
        return true;
    }
}

public enum WebhookEndpointStatus
{
    Active = 1,
    Disabled = 2,
    AutoPaused = 3,
    Archived = 4
}
