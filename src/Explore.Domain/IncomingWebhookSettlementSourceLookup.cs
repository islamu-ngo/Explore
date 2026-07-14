// ABOUTME: Stable relational lookup rows for authoritative incoming webhook settlement sources.
// ABOUTME: Distinguishes a newly committed effect receipt from settlement proven by an existing receipt.

namespace Explore.Domain;

public sealed class IncomingWebhookSettlementSourceLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

public enum IncomingWebhookSettlementSource
{
    None = 0,
    EffectCommitted = 1,
    ExistingReceipt = 2
}
