// ABOUTME: Secured command for audited control-plane tenant lifecycle status transitions.
// ABOUTME: Uses instance-setting update authority so tenant lifecycle controls stay instance-admin-only.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class TransitionControlPlaneTenantLifecycleCommand(
    Guid tenantId,
    TenantStatusEnum targetStatus,
    string? reason,
    string? confirmationText = null)
    : IRequest<BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenants.lifecycle";

    public Guid TenantId { get; } = tenantId;
    public TenantStatusEnum TargetStatus { get; } = targetStatus;
    public string? Reason { get; } = reason;
    public string? ConfirmationText { get; } = confirmationText;

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["targetTenantId"] = TenantId.ToString(),
        ["targetStatus"] = TargetStatus.ToString()
    };
}
