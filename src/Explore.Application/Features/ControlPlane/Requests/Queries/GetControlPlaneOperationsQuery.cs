// ABOUTME: Secured query for the Control Plane operations snapshot.
// ABOUTME: Authorizes operational status through instance-setting metadata before handlers run.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class GetControlPlaneOperationsQuery : IRequest<ControlPlaneOperationsDto>, ISecureRequest
{
    public const string SettingKey = "control-plane.operations";

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
