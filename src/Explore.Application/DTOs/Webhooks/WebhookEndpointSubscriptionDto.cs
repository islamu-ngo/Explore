// ABOUTME: API DTO for webhook endpoint event type subscription metadata.
// ABOUTME: Exposes event catalog ids/names without leaking endpoint secret material.

namespace Explore.Application.DTOs.Webhooks;

public sealed record WebhookEndpointSubscriptionDto
{
    public Guid Id { get; init; }

    public Guid EventTypeId { get; init; }

    public required string EventTypeName { get; init; }

    public required string EventTypeGroupName { get; init; }

    public bool IsEnabled { get; init; }

    public DateTime CreatedAt { get; init; }
}
