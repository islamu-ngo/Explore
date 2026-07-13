// ABOUTME: Canonical tenant-scoped webhook message envelope emitted after domain/application events.
// ABOUTME: Stores immutable provider-neutral payload metadata and retention evidence without delivery state.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class WebhookMessage : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public required string EventType { get; set; }
    public required string EventId { get; set; }
    public required string AggregateKind { get; set; }
    public Guid AggregateId { get; set; }
    public Guid? ConsumerId { get; set; }
    public WebhookConsumer? Consumer { get; set; }
    public string? PayloadJson { get; set; }
    public required string PayloadHash { get; set; }
    public DateTime PayloadRetentionUntil { get; set; }
    public DateTime? PayloadClearedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
