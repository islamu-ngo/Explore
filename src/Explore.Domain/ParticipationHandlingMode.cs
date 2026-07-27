// ABOUTME: Normalized lookup describing which system owns an event's participation handling.
// ABOUTME: Separates information-only, walk-in, external-managed, and platform-managed participation.

namespace Explore.Domain;

public sealed class ParticipationHandlingMode
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
