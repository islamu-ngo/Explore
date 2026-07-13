// ABOUTME: Tenant-scoped webhook consumer that owns endpoint subscriptions and provider mapping state.
// ABOUTME: Represents a tenant, organization, group, user, or system integration receiving outgoing webhooks.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class WebhookConsumer : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid? OwnerActorId { get; set; }
    public Actor? OwnerActor { get; set; }
    public Guid? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    public WebhookConsumerKind ConsumerKind { get; set; }
    public required string Name { get; set; }
    public WebhookConsumerStatus Status { get; set; }
    public WebhookProviderMode ProviderMode { get; set; }
    public string? ExternalProviderAppId { get; set; }

    public ICollection<WebhookConsumerProviderBinding> ProviderBindings { get; private set; } = [];

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public WebhookConsumerProviderBinding? GetVerifiedProviderBinding(WebhookProviderKind providerKind)
    {
        var matchingBindings = ProviderBindings
            .Where(binding =>
                binding.ProviderKind == providerKind &&
                binding.IsVerifiedFor(TenantId, Id))
            .Take(2)
            .ToArray();

        return matchingBindings.Length == 1 ? matchingBindings[0] : null;
    }
}

public enum WebhookConsumerKind
{
    Tenant = 1,
    Organization = 2,
    Group = 3,
    User = 4,
    SystemIntegration = 5
}

public enum WebhookConsumerStatus
{
    Active = 1,
    Disabled = 2,
    Archived = 3
}

public enum WebhookProviderMode
{
    Disabled = 1,
    Local = 2,
    Svix = 3,
    Composite = 4,
    DryRun = 5
}
