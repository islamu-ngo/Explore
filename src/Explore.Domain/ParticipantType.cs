// ABOUTME: Normalized lookup row for participant operational types.
// ABOUTME: Keeps stable participant-type IDs separate from their enum convenience mirror.

namespace Explore.Domain;

public sealed class ParticipantType
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
