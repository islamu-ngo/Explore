// ABOUTME: Tenant-scoped mapping from canonical webhook rows to external provider objects.
// ABOUTME: Lets SvixProvider and future providers synchronize apps, endpoints, messages, and retry state safely.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class WebhookProviderLink : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid? ConsumerId { get; set; }
    public WebhookConsumer? Consumer { get; set; }
    public Guid? EndpointId { get; set; }
    public WebhookEndpoint? Endpoint { get; set; }
    public Guid? MessageId { get; set; }
    public WebhookMessage? Message { get; set; }

    public WebhookExternalProvider Provider { get; set; }
    public string? ExternalAppId { get; set; }
    public string? ExternalEndpointId { get; set; }
    public string? ExternalMessageId { get; set; }
    public WebhookProviderLinkSyncState SyncState { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? LastErrorCategory { get; set; }
    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public enum WebhookExternalProvider
{
    Svix = 1
}

public enum WebhookProviderLinkSyncState
{
    Pending = 1,
    Synced = 2,
    Failed = 3,
    Disabled = 4
}
