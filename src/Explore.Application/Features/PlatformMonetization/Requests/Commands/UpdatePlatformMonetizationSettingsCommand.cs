// ABOUTME: Replaces the singleton instance-admin platform monetization settings document.
// ABOUTME: Carries expected immutable revision versions and instance-setting update authorization metadata.

using Explore.Application.Authorization;
using Explore.Application.DTOs.PlatformMonetization;
using Explore.Application.Features.PlatformMonetization.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.PlatformMonetization.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class UpdatePlatformMonetizationSettingsCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string SettingKey = GetPlatformMonetizationSettingsQuery.SettingKey;

    public UpdatePlatformMonetizationSettingsDto Settings { get; init; } = new();

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey
    };
}
