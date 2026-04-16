// ABOUTME: Lookup of organizer-selectable registration scope policies for an event.
// ABOUTME: Controls which of Event / Day / SessionSelection intents are accepted; consumed by registration handlers and Blazor policy-aware UX.

namespace Explore.Domain;

public class EventRegistrationPolicy
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
