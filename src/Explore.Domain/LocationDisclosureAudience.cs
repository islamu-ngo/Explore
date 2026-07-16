// ABOUTME: Normalized lookup entity for event-location disclosure audiences.
// ABOUTME: Stable backend codes remain separate from localized user-interface labels.

namespace Explore.Domain;

public sealed class LocationDisclosureAudience
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
