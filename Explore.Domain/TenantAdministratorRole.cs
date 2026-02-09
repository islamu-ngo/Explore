// ABOUTME: Tenant-scoped administrator role lookup table for stable, enum-backed role assignment.
// ABOUTME: Replaces ambiguous generic user role semantics with explicit tenant admin roles.

namespace Explore.Domain;

public class TenantAdministratorRole
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string MasterCode { get; set; }
    public string? Description { get; set; }
}
