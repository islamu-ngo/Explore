// ABOUTME: Wrapper DTO for PATCH-based Location updates using nullable per-property groups.
// ABOUTME: Body IDs and tenant IDs are absent because route/context authority owns identity and tenancy.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Location;

public sealed record UpdateLocationDto
{
    public UpdateLocationFullNameDto? FullName { get; init; }
    public UpdateLocationAddressDto? Address { get; init; }
    public UpdateLocationPostcodeDto? Postcode { get; init; }
    public UpdateLocationCountryDto? Country { get; init; }
    public UpdateLocationCityDto? City { get; init; }
    public UpdateLocationLatitudeDto? Latitude { get; init; }
    public UpdateLocationLongitudeDto? Longitude { get; init; }
    public UpdateLocationTimezoneDto? Timezone { get; init; }
}

public sealed record UpdateLocationFullNameDto
{
    public required string Value { get; init; }
}

public sealed record UpdateLocationAddressDto
{
    public required string Value { get; init; }
}

public sealed record UpdateLocationPostcodeDto
{
    public required string Value { get; init; }
}

public sealed record UpdateLocationCountryDto
{
    public required string Value { get; init; }
}

public sealed record UpdateLocationCityDto
{
    public required string Value { get; init; }
}

public sealed record UpdateLocationLatitudeDto
{
    public OptionalUpdate<double?> Value { get; init; } = OptionalUpdate<double?>.Unspecified();
}

public sealed record UpdateLocationLongitudeDto
{
    public OptionalUpdate<double?> Value { get; init; } = OptionalUpdate<double?>.Unspecified();
}

public sealed record UpdateLocationTimezoneDto
{
    public OptionalUpdate<string?> Value { get; init; } = OptionalUpdate<string?>.Unspecified();
}
