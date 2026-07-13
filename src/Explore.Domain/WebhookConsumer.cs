// ABOUTME: Tenant-scoped webhook consumer that owns endpoint subscriptions and provider mapping state.
// ABOUTME: Represents a tenant, organization, group, user, or system integration receiving outgoing webhooks.

using System.ComponentModel.DataAnnotations.Schema;
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

    public int ConsumerKindId { get; set; }
    public WebhookConsumerKindLookup ConsumerKindLookup { get; set; } = null!;
    [NotMapped]
    public WebhookConsumerKind ConsumerKind
    {
        get => (WebhookConsumerKind)ConsumerKindId;
        set => ConsumerKindId = (int)value;
    }
    public required string Name { get; set; }
    public int StatusId { get; set; }
    public WebhookConsumerStatusLookup StatusLookup { get; set; } = null!;
    [NotMapped]
    public WebhookConsumerStatus Status
    {
        get => (WebhookConsumerStatus)StatusId;
        set => StatusId = (int)value;
    }
    public int ProviderModeId { get; set; }
    public WebhookProviderModeLookup ProviderModeLookup { get; set; } = null!;
    [NotMapped]
    public WebhookProviderMode ProviderMode
    {
        get => (WebhookProviderMode)ProviderModeId;
        set => ProviderModeId = (int)value;
    }
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
