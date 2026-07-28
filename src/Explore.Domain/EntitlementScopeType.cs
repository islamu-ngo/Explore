// ABOUTME: Stable lookup row for an event, event-day, or event-session ticket entitlement target.
// ABOUTME: Lets ticket catalog validation keep entitlement targets within their owning event.

namespace Explore.Domain;

public sealed class EntitlementScopeType
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
