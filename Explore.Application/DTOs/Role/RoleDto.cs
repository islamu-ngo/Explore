// ABOUTME: Unified role DTO with scope. Replaces OrganizationRoleDto and UserRoleDto.
// ABOUTME: Used for role detail views and role assignment dropdowns across all scopes.

namespace Explore.Application.DTOs.Role;

public class RoleDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public int RoleScopeId { get; set; }
    public required string RoleScopeCode { get; set; }
    public required string RoleScopeName { get; set; }
    public bool IsSystem { get; set; }
}
