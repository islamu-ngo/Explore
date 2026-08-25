// ABOUTME: Lightweight role DTO for list views. Replaces OrganizationRoleListDto and UserRoleListDto.
// ABOUTME: Includes scope for filtering and display grouping in UI dropdowns.

namespace Explore.Application.DTOs.Role;

public sealed record RoleListDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public int RoleScopeId { get; init; }
    public required string RoleScopeCode { get; init; }
    public required string RoleScopeName { get; init; }
    public bool IsSystem { get; init; }
}
