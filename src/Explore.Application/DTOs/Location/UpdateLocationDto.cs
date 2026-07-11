// ABOUTME: Wrapper DTO for PATCH-based Location updates using nullable per-property groups.
// ABOUTME: Body IDs and tenant IDs are absent because route/context authority owns identity and tenancy.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Location;

public class UpdateLocationDto
{
    public UpdateLocationFullNameDto? FullName { get; set; }
    public UpdateLocationAddressDto? Address { get; set; }
    public UpdateLocationPostcodeDto? Postcode { get; set; }
    public UpdateLocationCountryDto? Country { get; set; }
    public UpdateLocationCityDto? City { get; set; }
    public UpdateLocationLatitudeDto? Latitude { get; set; }
    public UpdateLocationLongitudeDto? Longitude { get; set; }
    public UpdateLocationTimezoneDto? Timezone { get; set; }
}

public class UpdateLocationFullNameDto
{
    public required string Value { get; set; }
}

public class UpdateLocationAddressDto
{
    public required string Value { get; set; }
}

public class UpdateLocationPostcodeDto
{
    public required string Value { get; set; }
}

public class UpdateLocationCountryDto
{
    public required string Value { get; set; }
}

public class UpdateLocationCityDto
{
    public required string Value { get; set; }
}

public class UpdateLocationLatitudeDto
{
    public OptionalUpdate<double?> Value { get; set; } = OptionalUpdate<double?>.Unspecified();
}

public class UpdateLocationLongitudeDto
{
    public OptionalUpdate<double?> Value { get; set; } = OptionalUpdate<double?>.Unspecified();
}

public class UpdateLocationTimezoneDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}
