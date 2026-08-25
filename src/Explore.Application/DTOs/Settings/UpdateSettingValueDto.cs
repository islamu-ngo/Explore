// ABOUTME: Simple request body DTO for single setting value updates via API.
// ABOUTME: Contains only the new value — key and scope are determined by the route.

namespace Explore.Application.DTOs.Settings;

public sealed record UpdateSettingValueDto
{
    public required string Value { get; init; }
}
