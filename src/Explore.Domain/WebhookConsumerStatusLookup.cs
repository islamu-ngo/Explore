// ABOUTME: Stable relational lookup rows for webhook consumer lifecycle states.
// ABOUTME: Mirrors WebhookConsumerStatus identifiers used by consumer aggregates and public contracts.

namespace Explore.Domain;

public sealed class WebhookConsumerStatusLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

public enum WebhookConsumerStatus
{
    Active = 1,
    Disabled = 2,
    Archived = 3
}
