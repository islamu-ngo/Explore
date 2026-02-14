// ABOUTME: Lightweight permission DTO for list views and dropdowns.
// ABOUTME: Excludes verbose fields like Description for compact display.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Permission;

public class PermissionListDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public required string ResourceKind { get; set; }
    public required string Action { get; set; }
    public required string GroupName { get; set; }
    public RoleScopeEnum Scope { get; set; }
}
