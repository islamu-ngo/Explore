// ABOUTME: Appearance setting definitions for theme selection behavior resolved through the hierarchical settings engine.
// ABOUTME: Stores only references and mode flags; theme catalog rows live in first-class UiTheme entities.

namespace Explore.Domain.Settings.Definitions;

public static class AppearanceSettingDefinitions
{
    public static readonly SettingDefinition DefaultThemeId = new(
        Key: "appearance.default_theme_id",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "Appearance",
        Description: "Effective selected theme reference inherited through the settings hierarchy",
        MaxScope: SettingScope.User);

    public static readonly SettingDefinition ThemeMode = new(
        Key: "appearance.theme_mode",
        ValueType: SettingValueType.String,
        DefaultValue: "\"system\"",
        Category: "Appearance",
        Description: "Effective theme mode selection. Allowed values: system, light, dark",
        MaxScope: SettingScope.User,
        AllowedValues: ["system", "light", "dark"]);

    public static readonly SettingDefinition Direction = new(
        Key: "appearance.direction",
        ValueType: SettingValueType.String,
        DefaultValue: "\"auto\"",
        Category: "Appearance",
        Description: "Text direction preference. auto = language-based, ltr = force left-to-right, rtl = force right-to-left",
        MaxScope: SettingScope.User,
        AllowedValues: ["auto", "ltr", "rtl"]);

    // Note: language is semantically not "appearance"; stored here for v1 delivery speed (plan D3).
    // The tech-debt ticket to split into UserPreferences is tracked in the Phase 10 backlog.
    public static readonly SettingDefinition Language = new(
        Key: "appearance.language",
        ValueType: SettingValueType.String,
        DefaultValue: "\"en\"",
        Category: "Appearance",
        Description: "User language preference (ISO 639-1). Must be in the compile-time CultureRegistry.",
        MaxScope: SettingScope.User,
        AllowedValues: ["en", "fr", "ar"]);

    public static IReadOnlyList<SettingDefinition> All =>
        [DefaultThemeId, ThemeMode, Direction, Language];
}
