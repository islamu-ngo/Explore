// ABOUTME: Canonical integer identifiers for event provenance lookup rows.
// ABOUTME: Values must remain aligned with persistence seeding and API lookup metadata.

namespace Explore.Domain.Enums;

public enum EventProvenanceTypeEnum
{
    OrganizerCreated = 1,
    CommunityReported = 2,
    TenantCurated = 3,
    Imported = 4,
    Federated = 5
}
