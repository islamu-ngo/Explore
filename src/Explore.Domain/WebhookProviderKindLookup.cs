// ABOUTME: Stable relational lookup rows for supported webhook delivery providers.
// ABOUTME: Mirrors WebhookProviderKind identifiers used by bindings and provider publications.

namespace Explore.Domain;

public sealed class WebhookProviderKindLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

public enum WebhookProviderKind
{
    Local = 1,
    Svix = 2
}
