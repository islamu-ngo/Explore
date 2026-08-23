// ABOUTME: Detail read-model DTO for Location responses.
// ABOUTME: Includes concurrency metadata required by PATCH If-Match updates.

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

    /// <summary>
    /// Descriptive venue kind. It never grants disclosure — it only tells management surfaces whether the
    /// consent-backed private home workflow applies to this venue.
    /// </summary>
    public int LocationKindId { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
