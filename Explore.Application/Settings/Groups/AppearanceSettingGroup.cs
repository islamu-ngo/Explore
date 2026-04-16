// ABOUTME: Strongly-typed appearance setting group resolving theme, direction, and language from hierarchical settings.
// ABOUTME: Keeps appearance behaviour inside the existing settings engine; language persisted here for v1 per plan D3.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Common.Localization;
using Explore.Domain.Constants;

public class AppearanceSettingGroup : ISettingGroup
{
    public Guid? DefaultThemeId { get; private set; }
    public string ThemeMode { get; private set; } = "system";
    public string Direction { get; private set; } = "auto";
    public string Language { get; private set; } = "en";

    public static IEnumerable<string> SettingKeys =>
    [
        GovernanceSettingKeys.Appearance.DefaultThemeId,
        GovernanceSettingKeys.Appearance.ThemeMode,
        GovernanceSettingKeys.Appearance.Direction,
        GovernanceSettingKeys.Appearance.Language
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue(GovernanceSettingKeys.Appearance.DefaultThemeId, out var themeIdSetting))
        {
            var rawThemeId = SettingValueSerializer.DeserializeString(themeIdSetting.Value);
            DefaultThemeId = Guid.TryParse(rawThemeId, out var parsedThemeId)
                ? parsedThemeId
                : null;
        }

        if (settings.TryGetValue(GovernanceSettingKeys.Appearance.ThemeMode, out var themeModeSetting))
        {
            var mode = SettingValueSerializer.DeserializeString(themeModeSetting.Value, "system");
            ThemeMode = mode is "light" or "dark" ? mode : "system";
        }

        if (settings.TryGetValue(GovernanceSettingKeys.Appearance.Direction, out var directionSetting))
        {
            var dir = SettingValueSerializer.DeserializeString(directionSetting.Value, "auto");
            Direction = dir is "ltr" or "rtl" ? dir : "auto";
        }

        if (settings.TryGetValue(GovernanceSettingKeys.Appearance.Language, out var languageSetting))
        {
            var raw = SettingValueSerializer.DeserializeString(languageSetting.Value, "en");
            Language = CultureRegistry.TryGetEntry(raw, out var entry) ? entry.Code : "en";
        }
    }
}
