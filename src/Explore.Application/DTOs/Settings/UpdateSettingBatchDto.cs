// ABOUTME: Request body DTO for batch setting updates via API.
// ABOUTME: Contains key-value pairs and optional batch update mode selection.

namespace Explore.Application.DTOs.Settings;

public sealed record UpdateSettingBatchDto
{
    public required Dictionary<string, string> Values { get; init; }

    /// <summary>
    /// Optional batch update mode. If not specified, the controller selects an appropriate default:
    /// BestEffort for user scope (skip locked, apply rest) or Strict for tenant scope (reject all if any locked).
    /// </summary>
    public BatchUpdateMode? Mode { get; init; }
}
