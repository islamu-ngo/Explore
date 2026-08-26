// ABOUTME: Normalized lookup row describing the provenance of a Location's current address.
// ABOUTME: Carries stable machine metadata without granting address reuse or disclosure authority.

namespace Explore.Domain;

public sealed class LocationAddressSource
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
