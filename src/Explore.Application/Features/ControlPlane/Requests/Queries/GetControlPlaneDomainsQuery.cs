// ABOUTME: Secured query for the multi-tenant control-plane domain and DNS checklist.
// ABOUTME: Authorizes domain guidance through instance-setting metadata before the handler runs.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class GetControlPlaneDomainsQuery : IRequest<ControlPlaneDomainOverviewDto>, ISecureRequest
{
    public const string SettingKey = "control-plane.domains";

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey
    };
}
