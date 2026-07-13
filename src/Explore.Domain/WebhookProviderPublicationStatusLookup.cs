// ABOUTME: Stable relational lookup rows for provider publication lifecycle states.
// ABOUTME: Mirrors WebhookProviderPublicationStatus identifiers owned exclusively by provider publications.

namespace Explore.Domain;

public sealed class WebhookProviderPublicationStatusLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

public enum WebhookProviderPublicationStatus
{
    Prepared = 1,
    Publishing = 2,
    ProviderQueued = 3,
    RetryDue = 4,
    PublicationUnknown = 5,
    DeadLettered = 6,
    ManualReconciliation = 7,
    Abandoned = 8
}
