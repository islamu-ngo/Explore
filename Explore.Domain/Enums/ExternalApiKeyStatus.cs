// ABOUTME: Stable integer identifiers for external API key lifecycle statuses.
// ABOUTME: Mapped to ExternalApiKeyStatus lookup-table rows; IsUsable determines authentication eligibility.

namespace Explore.Domain.Enums;

public enum ExternalApiKeyStatusEnum
{
    Active = 1,
    Revoked = 2,
    Expired = 3,
    Suspended = 4,
    PendingRotation = 5
}
