// ABOUTME: Summary DTO for admin theme catalog lists.
// ABOUTME: Surfaces ownership, active/default state, and concurrency token without the full palette payload.

namespace Explore.Application.DTOs.Appearance;

public class UiThemeListItemDto
{
    public Guid Id { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public bool IsPlatformTheme { get; set; }
    public int SortOrder { get; set; }
    public uint RowVersion { get; set; }
}
