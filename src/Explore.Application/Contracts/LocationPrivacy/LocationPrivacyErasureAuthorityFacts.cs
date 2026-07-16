// ABOUTME: Defines PII-free facts exchanged with the retained location-erasure authority.
// ABOUTME: Carries only opaque identifiers, a typed reason, authority sequence, and server UTC metadata.

using Explore.Domain.Enums;

namespace Explore.Application.Contracts.LocationPrivacy;

public sealed record LocationPrivacyErasureIntent(
    Guid IntentId,
    Guid OwnerUserId,
    IReadOnlyList<Guid> LocationIds,
    LocationPrivacyErasureReasonEnum Reason);
