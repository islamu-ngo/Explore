// ABOUTME: Lookup-table entity for secret data-plane source types.
// ABOUTME: IDs mirror SecretSourceType values and are referenced by SecretBinding.

namespace Explore.Domain;

public class SecretSourceTypeLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
