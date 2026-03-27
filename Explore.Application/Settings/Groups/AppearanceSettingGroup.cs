// ABOUTME: Strongly-typed appearance setting group resolving theme reference and effective mode from hierarchical settings.
// ABOUTME: Keeps appearance behavior inside the existing settings engine without embedding runtime theme logic in layouts.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;

public class AppearanceSettingGroup : ISettingGroup
{
    public Guid? DefaultThemeId { get; private set; }
    public string ThemeMode { get; private set; } = "system";
    public string Direction { get; private set; } = "auto";

    public static IEnumerable<string> SettingKeys =>
    [
        GovernanceSettingKeys.Appearance.DefaultThemeId,
        GovernanceSettingKeys.Appearance.ThemeMode,
        GovernanceSettingKeys.Appearance.Direction
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
    }
}
