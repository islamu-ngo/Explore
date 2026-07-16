// ABOUTME: Normalized lookup entity classifying the physical kind of a location.
// ABOUTME: The row carries descriptive metadata only and grants no disclosure authority.

namespace Explore.Domain;

public sealed class LocationKind
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
