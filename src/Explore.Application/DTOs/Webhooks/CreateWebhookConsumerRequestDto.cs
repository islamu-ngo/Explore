// ABOUTME: API request DTO for creating a tenant-scoped outgoing webhook consumer.
// ABOUTME: Uses integer enum ids so handlers validate domain values explicitly.

namespace Explore.Application.DTOs.Webhooks;

public sealed class CreateWebhookConsumerRequestDto
{
    public Guid? OwnerActorId { get; set; }

    public Guid? OwnerUserId { get; set; }

    public int ConsumerKindId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int ProviderModeId { get; set; }
}
