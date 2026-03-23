// ABOUTME: Lightweight DTO representing a selectable theme returned to runtime and future settings UIs.
// ABOUTME: Exposes ownership/default metadata without leaking the full palette payload at this query stage.

namespace Explore.Application.DTOs.Appearance;

public class AvailableThemeDto
{
    public Guid Id { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsPlatformTheme { get; set; }
    public int SortOrder { get; set; }
}
