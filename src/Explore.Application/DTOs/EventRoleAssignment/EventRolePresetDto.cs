// ABOUTME: Event role preset exposed to assigners after applying the same-event authority ceiling.
// ABOUTME: Keeps UI/API role choices role-agnostic and prevents blind exposure of all event-scope roles.

namespace Explore.Application.DTOs.EventRoleAssignment;

public sealed class EventRolePresetDto
{
    public int RoleId { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public required IReadOnlyCollection<string> PermissionCodes { get; set; }
}
