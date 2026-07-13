// ABOUTME: Stable relational lookup rows for webhook endpoint lifecycle states.
// ABOUTME: Mirrors WebhookEndpointStatus identifiers used by endpoint aggregates and public contracts.

namespace Explore.Domain;

public sealed class WebhookEndpointStatusLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

public enum WebhookEndpointStatus
{
    Active = 1,
    Disabled = 2,
    AutoPaused = 3,
    Archived = 4
}
