// ABOUTME: Sub-DTO for creating event location venues within the scheduling graph.
// ABOUTME: Uses a temp key for cross-referencing sessions and rooms before persistence.

namespace Explore.Application.DTOs.Event;

public sealed record CreateEventLocationDto
{
    public required string TempKey { get; init; }
    public required string FullName { get; init; }
    public required string Address { get; init; }
    public required string Postcode { get; init; }
    public required string Country { get; init; }
    public required string City { get; init; }
    public string? Timezone { get; init; }
}
