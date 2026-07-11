// ABOUTME: Local delivery audit row linking a notification intent to ISLAMU-owned email dispatch state.
// ABOUTME: Captures safe provider-facing status metadata without storing raw transport errors or payload bodies.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class NotificationDelivery : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid NotificationIntentId { get; set; }
    public NotificationIntent? NotificationIntent { get; set; }

    public Guid? EmailDispatchOutboxId { get; set; }
    public EmailDispatchOutbox? EmailDispatchOutbox { get; set; }

    public int StatusId { get; set; }
    public NotificationDeliveryStatus? Status { get; set; }

    public string? ProviderMessageId { get; set; }
    public string? ProviderStatus { get; set; }
    public string? FailureCategory { get; set; }
    public DateTime? QueuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
