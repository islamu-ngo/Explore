// ABOUTME: Sub-DTO for creating event location venues within the scheduling graph.
// ABOUTME: Uses a temp key for cross-referencing sessions and rooms before persistence.

using System;

namespace Explore.Application.DTOs.Event;

public class CreateEventLocationDto
{
    public required string TempKey { get; set; }
    public required string FullName { get; set; }
    public required string Address { get; set; }
    public required string Postcode { get; set; }
    public required string Country { get; set; }
    public required string City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
}
