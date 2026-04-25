// ABOUTME: Base record for all integration events published to message queues.
// ABOUTME: Provides common fields (TenantId, OccurredAt) and MQContract Message attribute pattern.

namespace Explore.Application.Models.IntegrationEvents;

/// <summary>
/// Base record for integration events.
/// All derived records must include MQContract [Message] attribute with channel, typeName, and typeVersion.
/// </summary>
public abstract record IntegrationEventBase
{
    public required Guid TenantId { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
