// ABOUTME: Captures explicit consent facts required when a Private Home owner changes.
// ABOUTME: Keeps consent identity, timestamp, and version separate from descriptive location kind.

namespace Explore.Domain;

public sealed record LocationOwnershipConsent(
    Guid NewOwnerUserId,
    Guid ConsentedByUserId,
    DateTime ConsentedAtUtc,
    string ConsentVersion);
