// ABOUTME: Stable action kinds for event moderation history records.
// ABOUTME: Distinguishes reversible light moderation from irreversible heavy redaction.

namespace Explore.Domain.Enums;

public enum EventModerationActionKind
{
    LightModerated = 1,
    HeavyRedacted = 2,
    Unmoderated = 3
}
