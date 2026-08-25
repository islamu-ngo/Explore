// ABOUTME: Response DTO for batch setting updates with per-key result tracking.
// ABOUTME: Supports BestEffort (partial success) and Strict (all-or-nothing) semantics.

namespace Explore.Application.DTOs.Settings;

/// <summary>
/// Result of a batch setting update. In BestEffort mode, some keys may be skipped
/// (locked or invalid) while others succeed. In Strict mode, all fail if any is blocked.
/// </summary>
public sealed record BatchUpdateResponseDto
{
    public bool Success { get; init; }
    public required IReadOnlyList<SettingUpdateResultDto> Results { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Per-key result within a batch update.
/// </summary>
public sealed record SettingUpdateResultDto
{
    public required string Key { get; init; }
    public bool Applied { get; init; }
    public string? SkipReason { get; init; }
}
