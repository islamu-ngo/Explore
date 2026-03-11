// ABOUTME: Handles UpdateAnalyticsGovernanceSettingsCommand — persists analytics governance settings.
// ABOUTME: Uses IHierarchicalSettingsResolver.SetValueAsync to write at Instance scope.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public sealed class UpdateAnalyticsGovernanceSettingsCommandHandler(
    IHierarchicalSettingsResolver settingsResolver)
    : IRequestHandler<UpdateAnalyticsGovernanceSettingsCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateAnalyticsGovernanceSettingsCommand request, CancellationToken cancellationToken)
    {
        var s = request.Settings;
        var userId = request.UserId;

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Analytics.CookieConsentEnabled,
            s.CookieConsentEnabled.ToString().ToLowerInvariant(),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Analytics.DeclineBehavior,
            s.DeclineBehavior.ToString().ToLowerInvariant(),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Analytics.ConsentCookieLifetimeDays,
            s.ConsentCookieLifetimeDays.ToString(),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Analytics.GlobalDisableClientTracking,
            s.GlobalDisableClientTracking.ToString().ToLowerInvariant(),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Analytics.PosthogCookielessMode,
            ToSnakeCase(s.PosthogCookielessMode.ToString()),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Analytics.PosthogPersonProfiles,
            ToSnakeCase(s.PosthogPersonProfiles.ToString()),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Analytics.PosthogSessionReplay,
            s.PosthogSessionReplay.ToString().ToLowerInvariant(),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Analytics.PosthogAutocapture,
            s.PosthogAutocapture.ToString().ToLowerInvariant(),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Analytics.PosthogHeatmaps,
            s.PosthogHeatmaps.ToString().ToLowerInvariant(),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        await settingsResolver.SetValueAsync(
            GovernanceSettingKeys.Analytics.PosthogToolbar,
            s.PosthogToolbar.ToString().ToLowerInvariant(),
            SettingScope.Instance, Guid.Empty, userId, cancellationToken);

        return new BaseCommandResponse<Guid> { Success = true };
    }

    private static string ToSnakeCase(string value)
    {
        // Converts PascalCase enum names (e.g., "OnReject") to snake_case ("on_reject")
        return string.Concat(value.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
    }
}
