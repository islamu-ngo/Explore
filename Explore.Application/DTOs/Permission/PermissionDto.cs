// ABOUTME: Full permission detail DTO including all fields for admin views.
// ABOUTME: Used by GetPermissionList and GetAssignablePermissions queries.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Permission;

public class PermissionDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public required string ResourceKind { get; set; }
    public required string Action { get; set; }
    public string? FieldScope { get; set; }
    public required string GroupName { get; set; }
    public RoleScopeEnum Scope { get; set; }
    public bool IsSystem { get; set; }
    public bool IsFiltered { get; set; }
    public bool IsActive { get; set; }
}
