// ABOUTME: Lookup-table entity for secret validation lifecycle statuses.
// ABOUTME: IDs mirror SecretValidationResult values and are referenced by SecretBinding.

namespace Explore.Domain;

public class SecretValidationStatus
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
