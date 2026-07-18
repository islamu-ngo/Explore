// ABOUTME: Strongly typed ATProto event settings resolved from the hierarchical governance engine.
// ABOUTME: Fails closed to platform validation whenever capability or stored profile state is invalid.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;

public sealed class AtprotoFederationSettingGroup : ISettingGroup
{
    public const string PlatformProfile = "platform";
    public const string CommunityLexiconProfile = "community_lexicon";

    public bool EventsEnabled { get; private set; }
    public string EventValidationProfile { get; private set; } = PlatformProfile;
    public bool PublishMyEvents { get; private set; }

    public static IEnumerable<string> SettingKeys =>
    [
        GovernanceSettingKeys.Federation.AtprotoEventsEnabled,
        GovernanceSettingKeys.Federation.AtprotoEventValidationProfile,
        GovernanceSettingKeys.Federation.AtprotoPublishMyEvents
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue(GovernanceSettingKeys.Federation.AtprotoEventsEnabled, out var enabled))
        {
            EventsEnabled = SettingValueSerializer.DeserializeBool(enabled.Value, false);
        }

        if (settings.TryGetValue(GovernanceSettingKeys.Federation.AtprotoPublishMyEvents, out var consent))
        {
            PublishMyEvents = SettingValueSerializer.DeserializeBool(consent.Value, false);
        }

        if (!EventsEnabled
            || !settings.TryGetValue(GovernanceSettingKeys.Federation.AtprotoEventValidationProfile, out var profile))
        {
            return;
        }

        var value = SettingValueSerializer.DeserializeString(profile.Value, PlatformProfile);
        EventValidationProfile = string.Equals(value, CommunityLexiconProfile, StringComparison.OrdinalIgnoreCase)
            ? CommunityLexiconProfile
            : PlatformProfile;
    }
}
