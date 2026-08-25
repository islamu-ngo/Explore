using System;

namespace Explore.Application.DTOs.Location;

public sealed record CreateLocationDto
{
    public required string FullName { get; init; }
    public required string Address { get; init; }
    public required string Postcode { get; init; }
    public required string Country { get; init; }
    public required string City { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? Timezone { get; init; }
}
