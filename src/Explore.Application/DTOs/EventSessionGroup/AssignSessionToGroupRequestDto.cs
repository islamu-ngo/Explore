// ABOUTME: Request contract for assigning an EventSession program item to an EventSessionGroup.
// ABOUTME: Includes EventId for explicit same-event validation; TenantId remains server-owned.

namespace Explore.Application.DTOs.EventSessionGroup;

public sealed record AssignSessionToGroupRequestDto
{
    public Guid EventId { get; init; }
    public Guid EventSessionGroupId { get; init; }
    public Guid EventSessionId { get; init; }
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
}
