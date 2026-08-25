// ABOUTME: Sub-resource DTO for instance-level event submission policies.
// ABOUTME: Controls who can submit events and UI behavior for event cards.

namespace Explore.Application.DTOs.Instance;

public sealed record EventPolicyDto
{
    public bool AllowUserSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSubmittedEvents { get; set; } = true;
    public bool AllowGroupSubmittedEvents { get; set; } = true;
    public bool EventCardClickOpensDetailPage { get; set; }
    public bool LockTenantEventCardClickBehavior { get; set; }
}
