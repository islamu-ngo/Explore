using System;

namespace Explore.Application.DTOs.Location;

public class LocationListDto
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public string? Timezone { get; set; }
}
