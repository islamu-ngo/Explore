// ABOUTME: Lookup-table entity for tenant plan assignment lifecycle statuses.
// ABOUTME: Supports normalized constraints around one active SaaS tier assignment per tenant.

namespace Explore.Domain;

public class TenantPlanAssignmentStatus
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public bool IsActiveAssignment { get; set; }
}
