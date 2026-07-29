// ABOUTME: Requests the singleton instance-admin platform monetization settings document.
// ABOUTME: Carries instance-setting view authorization metadata for the platform-monetization key.

using Explore.Application.Authorization;
using Explore.Application.DTOs.PlatformMonetization;
using MediatR;

namespace Explore.Application.Features.PlatformMonetization.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class GetPlatformMonetizationSettingsQuery : IRequest<PlatformMonetizationSettingsDto>, ISecureRequest
{
    public const string SettingKey = "platform-monetization";

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey
    };
}
