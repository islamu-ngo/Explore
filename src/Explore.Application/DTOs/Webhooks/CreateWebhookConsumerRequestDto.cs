// ABOUTME: API request DTO for creating an outgoing webhook consumer under one typed owner.
// ABOUTME: Uses the normalized owner-kind lookup id plus an optional owner id resolved by Application.

namespace Explore.Application.DTOs.Webhooks;

public sealed class CreateWebhookConsumerRequestDto
{
    public Guid? OwnerId { get; set; }

    public int ConsumerKindId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int ProviderModeId { get; set; }
}
