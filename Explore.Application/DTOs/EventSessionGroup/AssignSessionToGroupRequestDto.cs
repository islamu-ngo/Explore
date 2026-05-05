// ABOUTME: Request contract for assigning an EventSession program item to an EventSessionGroup.
// ABOUTME: Includes EventId for explicit same-event validation; TenantId remains server-owned.

namespace Explore.Application.DTOs.EventSessionGroup;

public class AssignSessionToGroupRequestDto
{
    public Guid EventId { get; set; }
    public Guid EventSessionGroupId { get; set; }
    public Guid EventSessionId { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}
