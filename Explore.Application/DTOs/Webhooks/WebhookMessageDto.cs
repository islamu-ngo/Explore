// ABOUTME: API DTO for canonical outgoing webhook message audit rows.
// ABOUTME: Exposes provider state and retention metadata while omitting raw payload JSON.

namespace Explore.Application.DTOs.Webhooks;

public sealed class WebhookMessageDto
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }

    public required string EventType { get; init; }

    public required string EventId { get; init; }

    public required string AggregateKind { get; init; }

    public Guid AggregateId { get; init; }

    public Guid? ConsumerId { get; init; }

    public string? ConsumerName { get; init; }

    public required string PayloadHash { get; init; }

    public DateTime PayloadRetentionUntil { get; init; }

    public DateTime? PayloadClearedAt { get; init; }

    public int ProviderModeId { get; init; }

    public required string ProviderModeName { get; init; }

    public string? ProviderMessageId { get; init; }

    public int StatusId { get; init; }

    public required string StatusName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? PublishedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
