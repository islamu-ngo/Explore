// ABOUTME: Normalized lookup describing whether a public event action may be exposed.
// ABOUTME: Separates link health and moderation from the action's semantic kind.

namespace Explore.Domain;

public sealed class EventPublicActionHealthState
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
