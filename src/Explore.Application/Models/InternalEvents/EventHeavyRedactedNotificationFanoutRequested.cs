// ABOUTME: Internal outbox payload requesting generic attendee notifications after irreversible event redaction.
// ABOUTME: Omits event identity and content so heavy moderation fanout payloads stay safe if inspected.

using System.Text.Json.Serialization;

namespace Explore.Application.Models.InternalEvents;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EventHeavyRedactedNotificationFanoutRequested
{
    public const int CurrentVersion = 1;

    public required Guid TenantId { get; init; }
    public required Guid ModerationRecordId { get; init; }
    public int Version { get; init; } = CurrentVersion;
}
