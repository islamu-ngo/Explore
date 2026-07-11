// ABOUTME: List read-model DTO for Location collection responses.
// ABOUTME: Includes concurrency metadata so list-driven editors can issue PATCH If-Match updates.

namespace Explore.Application.DTOs.Location;

public class LocationListDto
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public string? Timezone { get; set; }
    public Guid ConcurrencyStamp { get; set; }
}
