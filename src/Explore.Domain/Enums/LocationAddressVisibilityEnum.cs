// ABOUTME: Stable lookup identifiers governing who may reuse a Location's current address.
// ABOUTME: Defaults to quarantine and keeps creator, organization, and tenant approval scopes explicit.

namespace Explore.Domain.Enums;

public enum LocationAddressVisibilityEnum
{
    Quarantined = 1,
    CreatorPrivate = 2,
    OrganizationScoped = 3,
    TenantApproved = 4
}
