// ABOUTME: Lightweight role DTO for list views. Replaces OrganizationRoleListDto and UserRoleListDto.
// ABOUTME: Includes scope for filtering and display grouping in UI dropdowns.

namespace Explore.Application.DTOs.Role;

public class RoleListDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public int RoleScopeId { get; set; }
    public required string RoleScopeCode { get; set; }
    public required string RoleScopeName { get; set; }
    public bool IsSystem { get; set; }
}
