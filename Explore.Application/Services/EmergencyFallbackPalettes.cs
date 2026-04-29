// ABOUTME: Hardcoded emergency fallback palettes used when no system preset is in the database.
// ABOUTME: Matches the Enterprise Blue theme seeded during migration. Includes high-contrast variants.

namespace Explore.Application.Services;

using Explore.Domain.ValueObjects;

public static class EmergencyFallbackPalettes
{
    public static UiThemePalette FallbackLight => new()
    {
        Primary = "#0F62FE",
        PrimaryContrastText = "#FFFFFF",
        Secondary = "#475569",
        SecondaryContrastText = "#FFFFFF",
        Background = "#F1F5F9",
        Surface = "#FFFFFF",
        AppbarBackground = "#FFFFFF",
        AppbarText = "#1E293B",
        DrawerBackground = "#FFFFFF",
        DrawerText = "#1E293B",
        DrawerIcon = "#475569",
        TextPrimary = "#0F172A",
        TextSecondary = "#475569",
        Info = "#2563EB",
        Success = "#16A34A",
        Warning = "#D97706",
        Error = "#DC2626",
        LinesDefault = "#CBD5E1",
        Divider = "#CBD5E1"
    };

    public static UiThemePalette FallbackDark => new()
    {
        Primary = "#3B82F6",
        PrimaryContrastText = "#FFFFFF",
        Secondary = "#F1F5F9",
        SecondaryContrastText = "#0F172A",
        Background = "#0B0F19",
        Surface = "#1E293B",
        AppbarBackground = "rgba(11,15,25,0.85)",
        AppbarText = "#F1F5F9",
        DrawerBackground = "#0B0F19",
        DrawerText = "#F1F5F9",
        DrawerIcon = "#CBD5E1",
        TextPrimary = "#F8FAFC",
        TextSecondary = "#94A3B8",
        Info = "#60A5FA",
        Success = "#10B981",
        Warning = "#F59E0B",
        Error = "#EF4444",
        LinesDefault = "#334155",
        Divider = "#1E293B"
    };

    public static UiThemePalette FallbackLightHighContrast => new()
    {
        Primary = "#0050D8",
        PrimaryContrastText = "#FFFFFF",
        Secondary = "#1E293B",
        SecondaryContrastText = "#FFFFFF",
        Background = "#FFFFFF",
        Surface = "#FFFFFF",
        AppbarBackground = "#FFFFFF",
        AppbarText = "#000000",
        DrawerBackground = "#FFFFFF",
        DrawerText = "#000000",
        DrawerIcon = "#000000",
        TextPrimary = "#000000",
        TextSecondary = "#1E293B",
        Info = "#0050D8",
        Success = "#006600",
        Warning = "#B45309",
        Error = "#B91C1C",
        LinesDefault = "#000000",
        Divider = "#000000"
    };

    public static UiThemePalette FallbackDarkHighContrast => new()
    {
        Primary = "#93C5FD",
        PrimaryContrastText = "#000000",
        Secondary = "#F8FAFC",
        SecondaryContrastText = "#000000",
        Background = "#000000",
        Surface = "#0A0A0A",
        AppbarBackground = "#000000",
        AppbarText = "#FFFFFF",
        DrawerBackground = "#000000",
        DrawerText = "#FFFFFF",
        DrawerIcon = "#FFFFFF",
        TextPrimary = "#FFFFFF",
        TextSecondary = "#E2E8F0",
        Info = "#93C5FD",
        Success = "#6EE7B7",
        Warning = "#FCD34D",
        Error = "#FCA5A5",
        LinesDefault = "#FFFFFF",
        Divider = "#FFFFFF"
    };
}