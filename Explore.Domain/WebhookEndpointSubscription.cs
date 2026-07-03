// ABOUTME: Tenant-scoped join row linking a webhook endpoint to an enabled canonical event type.
// ABOUTME: Supports LocalProvider endpoint filtering and mirrors Svix event type subscriptions where needed.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class WebhookEndpointSubscription : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid EndpointId { get; set; }
    public WebhookEndpoint? Endpoint { get; set; }
    public Guid EventTypeId { get; set; }
    public WebhookEventType? EventType { get; set; }
    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
