// ABOUTME: Stable relational metadata for individually addressable webhook provider capabilities.
// ABOUTME: Uses the capability flag value as the normalized lookup identifier for lossless snapshots.

namespace Explore.Domain;

public sealed class WebhookProviderCapabilityLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
