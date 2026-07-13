// ABOUTME: Stable relational lookup rows for webhook consumer ownership kinds.
// ABOUTME: Mirrors WebhookConsumerKind identifiers used by consumer aggregates and public contracts.

namespace Explore.Domain;

public sealed class WebhookConsumerKindLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

public enum WebhookConsumerKind
{
    Tenant = 1,
    Organization = 2,
    Group = 3,
    User = 4,
    SystemIntegration = 5
}
