// ABOUTME: Unified role DTO with scope. Replaces OrganizationRoleDto and UserRoleDto.
// ABOUTME: Used for role detail views and role assignment dropdowns across all scopes.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Role;

public class RoleDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public RoleScopeEnum Scope { get; set; }
    public bool IsSystem { get; set; }
}
