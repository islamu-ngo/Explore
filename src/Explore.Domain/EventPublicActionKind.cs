// ABOUTME: Normalized lookup describing the semantic purpose of a public event action.
// ABOUTME: Keeps labels and authorization independent from arbitrary organizer-provided URLs.

namespace Explore.Domain;

public sealed class EventPublicActionKind
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
