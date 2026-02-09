using System;

namespace Explore.Application.DTOs.Location;

public class LocationDto
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Address { get; set; }
    public required string Postcode { get; set; }
    public required string Country { get; set; }
    public required string City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
    public Guid TenantId { get; set; }
}
