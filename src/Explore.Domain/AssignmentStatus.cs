// ABOUTME: Normalized lookup row for participant assignment states.
// ABOUTME: Keeps stable assignment-status IDs separate from their enum convenience mirror.

namespace Explore.Domain;

public sealed class AssignmentStatus
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
