// ABOUTME: Internal outbox payload requesting generic attendee notifications after irreversible event redaction.
// ABOUTME: Omits event identity and content so heavy moderation fanout payloads stay safe if inspected.

namespace Explore.Application.Models.InternalEvents;

public sealed record EventHeavyRedactedNotificationFanoutRequested
{
    public required Guid TenantId { get; init; }
    public required Guid ModerationRecordId { get; init; }
    public required Guid SourceActorId { get; init; }
    public required DateTimeOffset RedactedAt { get; init; }
}
