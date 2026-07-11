// ABOUTME: Secured Control Plane command for deliberate deployment-mode transitions.
// ABOUTME: Prevents casual settings toggles by requiring target-mode confirmation and update permission.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class TransitionControlPlaneDeploymentModeCommand(
    DeploymentMode targetMode,
    string? reason,
    string? confirmationText) : IRequest<BaseCommandResponse<ControlPlaneDeploymentModeTransitionDto>>, ISecureRequest
{
    public const string SettingKey = "control-plane.deployment-mode.runbook";

    public DeploymentMode TargetMode { get; } = targetMode;

    public string? Reason { get; } = reason;

    public string? ConfirmationText { get; } = confirmationText;

    string ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object> ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["targetMode"] = targetMode.ToString()
    };
}
