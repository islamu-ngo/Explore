// ABOUTME: Stable relational lookup rows for incoming webhook operator-redrive results.
// ABOUTME: Mirrors append-only redrive evidence outcomes with normalized integer identifiers.

namespace Explore.Domain;

public sealed class IncomingWebhookRedriveResultLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

public enum IncomingWebhookRedriveResult
{
    Scheduled = 1
}
