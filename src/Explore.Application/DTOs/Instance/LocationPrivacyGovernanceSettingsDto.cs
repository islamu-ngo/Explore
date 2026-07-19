// ABOUTME: Carries instance-level ceilings for EventLocation creation and disclosure.
// ABOUTME: Uses conservative defaults when older clients omit the location-privacy section.

namespace Explore.Application.DTOs.Instance;

public sealed class LocationPrivacyGovernanceSettingsDto
{
    public bool AllowHomeLocations { get; set; }
    public bool AllowPublicExactAddress { get; set; }
    public bool AllowPublicCoordinates { get; set; }
    public string MinimumHomeAudience { get; set; } = "NEVER";
    public string DefaultRevealOffset { get; set; } = "P30D";
}
