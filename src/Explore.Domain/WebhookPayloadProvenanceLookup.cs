// ABOUTME: Stable relational lookup rows describing webhook payload byte provenance.
// ABOUTME: Distinguishes exact captured bytes from canonicalized legacy JSON that cannot recover original formatting.

namespace Explore.Domain;

public sealed class WebhookPayloadProvenanceLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

public enum WebhookPayloadProvenance
{
    ExactBytes = 1,
    LegacyJsonCanonicalized = 2,
    NormalizedProviderEnvelope = 3
}
