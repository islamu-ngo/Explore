// ABOUTME: DTO representing a permission assigned to a role.
// ABOUTME: Used by GetRolePermissions query to show role's granted permissions.

namespace Explore.Application.DTOs.Permission;

public class RolePermissionDto
{
    public int RoleId { get; set; }
    public required string RoleName { get; set; }
    public int PermissionId { get; set; }
    public required string PermissionMasterCode { get; set; }
    public required string PermissionFullName { get; set; }
    public required string ResourceKind { get; set; }
    public required string Action { get; set; }
}
