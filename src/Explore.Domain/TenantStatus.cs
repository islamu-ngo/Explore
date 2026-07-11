// ABOUTME: Lookup-table entity for tenant lifecycle statuses used by onboarding and lifecycle transitions.
// ABOUTME: Stores metadata for each status including whether it represents an active tenant state.

namespace Explore.Domain;

public class TenantStatus
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public bool IsActiveState { get; set; }
}
