// ABOUTME: Normalized lookup row for provider-neutral payment-attempt statuses.
// ABOUTME: Keeps persisted status IDs separate from the Domain enum convenience mirror.

namespace Explore.Domain;

public sealed class PaymentAttemptStatus
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
