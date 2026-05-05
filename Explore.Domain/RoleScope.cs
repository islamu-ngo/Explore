// ABOUTME: Lookup-table entity for RBAC role and permission scope levels.
// ABOUTME: IDs mirror RoleScopeEnum values and are referenced by Role and Permission.

namespace Explore.Domain;

public class RoleScope
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
