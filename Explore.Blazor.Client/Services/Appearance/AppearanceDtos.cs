// ABOUTME: Client-side DTOs for the appearance subsystem.
// ABOUTME: These mirror the server DTOs but live in the Blazor Client project to avoid referencing Explore.Application.

namespace Explore.Blazor.Client.Services.Appearance;

public sealed class ResolvedAppearanceDto
{
    public Guid? ActiveProfileId { get; set; }
    public Guid? SourcePresetId { get; set; }
    public string? SourcePresetKey { get; set; }
    public string ResolutionSource { get; set; } = default!;
    public string ThemeMode { get; set; } = "system";
    public bool? ServerEffectiveDarkMode { get; set; }
    public string Direction { get; set; } = "auto";
    public string Language { get; set; } = "en";
    public ResolvedThemeDto Theme { get; set; } = default!;
    public AppearanceCapabilitiesDto Capabilities { get; set; } = default!;
}

public sealed class ResolvedThemeDto
{
    public string DisplayName { get; set; } = default!;
    public ClientPaletteDto LightPalette { get; set; } = default!;
    public ClientPaletteDto DarkPalette { get; set; } = default!;
    public bool IsSnapshot { get; set; }
    public bool IsUserEditable { get; set; }
    public string? Origin { get; set; }
}

public sealed class AppearanceCapabilitiesDto
{
    public bool CanEditProfile { get; set; }
    public bool CanCreateCustomProfile { get; set; }
    public bool CanClonePreset { get; set; }
    public bool CanDeleteProfile { get; set; }
}

public sealed class AvailablePresetDto
{
    public Guid Id { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEditable { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public required ClientPaletteDto LightPalette { get; set; }
    public required ClientPaletteDto DarkPalette { get; set; }
    public DateTimeOffset? DeprecatedAt { get; set; }
}

public sealed class UserAppearanceProfileDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ThemeMode { get; set; } = "system";
    public required ClientPaletteDto LightPaletteSnapshot { get; set; }
    public required ClientPaletteDto DarkPaletteSnapshot { get; set; }
    public string? SourcePresetKey { get; set; }
    public Guid? SourcePresetId { get; set; }
    public int? SourcePresetSeedVersion { get; set; }
    public bool IsUserEditable { get; set; }
    public bool IsDefault { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset? ClonedAt { get; set; }
}

public sealed class ClientPaletteDto
{
    public string Primary { get; set; } = string.Empty;
    public string PrimaryContrastText { get; set; } = "#FFFFFF";
    public string Secondary { get; set; } = string.Empty;
    public string SecondaryContrastText { get; set; } = "#FFFFFF";
    public string Background { get; set; } = string.Empty;
    public string Surface { get; set; } = string.Empty;
    public string AppbarBackground { get; set; } = string.Empty;
    public string AppbarText { get; set; } = string.Empty;
    public string DrawerBackground { get; set; } = string.Empty;
    public string DrawerText { get; set; } = string.Empty;
    public string DrawerIcon { get; set; } = string.Empty;
    public string TextPrimary { get; set; } = string.Empty;
    public string TextSecondary { get; set; } = string.Empty;
    public string Info { get; set; } = string.Empty;
    public string Success { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string LinesDefault { get; set; } = string.Empty;
    public string Divider { get; set; } = string.Empty;
}

public sealed class ClonePresetRequestDto
{
    public string? Name { get; set; }
}

public sealed class CreateCustomProfileRequestDto
{
    public required string Name { get; set; }
    public string ThemeMode { get; set; } = "system";
    public required string NaturalColor { get; set; }
    public required string BrandColor { get; set; }
}

public sealed class UpdateAppearanceProfileRequestDto
{
    public string? Name { get; set; }
    public ClientPaletteDto? LightPaletteSnapshot { get; set; }
    public ClientPaletteDto? DarkPaletteSnapshot { get; set; }
    public string? ThemeMode { get; set; }
}

public sealed class SetActiveProfileRequestDto
{
    public Guid ProfileId { get; set; }
    public string? ThemeMode { get; set; }
    public string? Direction { get; set; }
    public string? Language { get; set; }
}

public sealed class SetThemeModeRequestDto
{
    public string ThemeMode { get; set; } = "system";
}

public sealed class ArchiveProfileRequestDto
{
}

public sealed class DuplicateProfileRequestDto
{
    public string? Name { get; set; }
}
