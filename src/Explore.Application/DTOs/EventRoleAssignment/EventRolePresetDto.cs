// ABOUTME: Event role preset exposed to assigners after applying the same-event authority ceiling.
// ABOUTME: Keeps UI/API role choices role-agnostic and prevents blind exposure of all event-scope roles.

namespace Explore.Application.DTOs.EventRoleAssignment;

public sealed record EventRolePresetDto
{
    public int RoleId { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyCollection<string> PermissionCodes { get; init; }
}
