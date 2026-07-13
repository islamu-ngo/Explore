// ABOUTME: Stable relational lookup rows for Local webhook delivery lifecycle states.
// ABOUTME: Keeps mutable Local target state independent from append-only HTTP attempt outcomes.

namespace Explore.Domain;

public sealed class WebhookLocalDeliveryStatusLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

public enum WebhookLocalDeliveryStatus
{
    Pending = 1,
    Delivering = 2,
    RetryDue = 3,
    Succeeded = 4,
    DeadLettered = 5,
    Abandoned = 6
}
