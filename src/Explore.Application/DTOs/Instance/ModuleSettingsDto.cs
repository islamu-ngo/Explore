// ABOUTME: Sub-resource DTO for module enablement settings.
// ABOUTME: Controls which optional modules (Islamic, Tech) are active on the instance.

namespace Explore.Application.DTOs.Instance;

public sealed record ModuleSettingsDto
{
    public bool EnableIslamicModule { get; set; } = true;
    public bool EnableTechModule { get; set; } = true;
}
