// ABOUTME: Normalized lookup row describing the governed reuse scope of a Location address.
// ABOUTME: Carries stable machine metadata while aggregate and database checks enforce scope semantics.

namespace Explore.Domain;

public sealed class LocationAddressVisibility
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
