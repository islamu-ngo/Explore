// ABOUTME: DTO for event team member listing with user, role, and assignment lifecycle details.
// ABOUTME: Used by GetEventTeamListRequest for team management UI and API responses.

using Explore.Domain.Enums;
using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.EventRoleAssignment;

public sealed class EventTeamMemberDto
{
    [JsonIgnore]
    public Guid TenantId { get; set; }

    [JsonIgnore]
    public Guid EventId { get; set; }

    public Guid AssignmentId { get; set; }
    public Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public required string UserFullName { get; set; }
    public int RoleId { get; set; }
    public required string RoleName { get; set; }
    public required string RoleMasterCode { get; set; }
    public EventRoleAssignmentStatus Status { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsEffective { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}
