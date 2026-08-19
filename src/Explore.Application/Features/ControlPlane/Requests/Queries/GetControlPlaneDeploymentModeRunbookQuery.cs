// ABOUTME: Secured Control Plane query for the deployment-mode migration runbook.
// ABOUTME: Exposes operator-visible transition preconditions without allowing casual settings toggles.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class GetControlPlaneDeploymentModeRunbookQuery : IRequest<ControlPlaneDeploymentModeRunbookDto>, ISecureRequest
{
    public const string SettingKey = "control-plane.deployment-mode.runbook";

    string ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
