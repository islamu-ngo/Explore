// ABOUTME: DTO for a user-owned appearance profile — a stable snapshot independent of source preset.
// ABOUTME: Returned by the profiles endpoint so the UI can list, activate, and manage user themes.

namespace Explore.Application.DTOs.Appearance;

public sealed class UserAppearanceProfileDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ThemeMode { get; set; } = "system";
    public required UiThemePaletteDto LightPaletteSnapshot { get; set; }
    public required UiThemePaletteDto DarkPaletteSnapshot { get; set; }
    public string? SourcePresetKey { get; set; }
    public Guid? SourcePresetId { get; set; }
    public int? SourcePresetSeedVersion { get; set; }
    public bool IsUserEditable { get; set; }
    public bool IsDefault { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset? ClonedAt { get; set; }
}