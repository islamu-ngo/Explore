// ABOUTME: Normalized lookup describing an organizer claim lifecycle state.
// ABOUTME: Keeps persisted state IDs stable while domain methods own transitions.

namespace Explore.Domain;

public sealed class EventOrganizerClaimStatus
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
