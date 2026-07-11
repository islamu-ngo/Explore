// ABOUTME: Wrapper DTO for PATCH-based Organization profile updates using nullable per-property groups.
// ABOUTME: Route ID owns identity; nullable field groups use OptionalUpdate for explicit clear semantics.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Organization;

public class UpdateOrganizationDto
{
    public UpdateOrganizationFullNameDto? FullName { get; set; }
    public UpdateOrganizationWebsiteUrlDto? WebsiteUrl { get; set; }
    public UpdateOrganizationEmailDto? Email { get; set; }
    public UpdateOrganizationCountryDto? Country { get; set; }
    public UpdateOrganizationCityDto? City { get; set; }
    public UpdateOrganizationPostcodeDto? Postcode { get; set; }
    public UpdateOrganizationAddressDto? Address { get; set; }
}

public class UpdateOrganizationFullNameDto
{
    public required string Value { get; set; }
}

public class UpdateOrganizationWebsiteUrlDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateOrganizationEmailDto
{
    public required string Value { get; set; }
}

public class UpdateOrganizationCountryDto
{
    public required string Value { get; set; }
}

public class UpdateOrganizationCityDto
{
    public required string Value { get; set; }
}

public class UpdateOrganizationPostcodeDto
{
    public int Value { get; set; }
}

public class UpdateOrganizationAddressDto
{
    public required string Value { get; set; }
}
