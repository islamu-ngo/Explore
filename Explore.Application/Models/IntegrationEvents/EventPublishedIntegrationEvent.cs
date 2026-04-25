// ABOUTME: Integration event published when a new event is created or updated.
// ABOUTME: Consumed by external systems for event aggregation, notifications, or federated search.

namespace Explore.Application.Models.IntegrationEvents;

using MQContract.Attributes;

[Message(channel: "events.published", typeName: "EventPublished", typeVersion: "1.0.0")]
public sealed record EventPublishedIntegrationEvent : IntegrationEventBase
{
    public required Guid EventId { get; init; }
    public required string Title { get; init; }
    public required DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string? Location { get; init; }
    public bool IsDeleted { get; init; }
}
