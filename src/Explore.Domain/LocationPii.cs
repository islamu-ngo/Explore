// ABOUTME: Stores precise location-identifying fields in a dedicated extension table.
// Uses a 1:1 shared primary-key relationship with Location for hard-deleteable PII.

namespace Explore.Domain;

public class LocationPii
{
    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    public required string Address { get; set; }
    public required string Postcode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
