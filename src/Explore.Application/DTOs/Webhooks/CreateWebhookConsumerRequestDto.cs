// ABOUTME: API request DTO for creating an outgoing webhook consumer under one typed owner.
// ABOUTME: Uses the normalized owner-kind lookup id plus an optional owner id resolved by Application.

namespace Explore.Application.DTOs.Webhooks;

public sealed record CreateWebhookConsumerRequestDto
{
    public Guid? OwnerId { get; init; }

    public int ConsumerKindId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int ProviderModeId { get; init; }
}
