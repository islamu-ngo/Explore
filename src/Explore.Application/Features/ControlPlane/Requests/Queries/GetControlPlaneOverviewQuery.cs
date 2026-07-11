// ABOUTME: Secured query for the multi-tenant control-plane overview snapshot.
// ABOUTME: Uses instance-setting authorization metadata so the API remains the authority.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class GetControlPlaneOverviewQuery : IRequest<ControlPlaneOverviewDto>, ISecureRequest
{
    public const string SettingKey = "control-plane";

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey
    };
}
