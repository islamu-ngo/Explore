// ABOUTME: Secured command for switching one tenant to a selected tenant plan version.
// ABOUTME: Supersedes the previous active assignment and creates one new active assignment.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class SwitchControlPlaneTenantPlanAssignmentCommand(
    Guid tenantId,
    Guid tenantPlanVersionId,
    Guid assignedByUserId)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plan-assignments";

    public Guid TenantId { get; } = tenantId;
    public Guid TenantPlanVersionId { get; } = tenantPlanVersionId;
    public Guid AssignedByUserId { get; } = assignedByUserId;

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["tenantId"] = TenantId,
        ["tenantPlanVersionId"] = TenantPlanVersionId
    };
}
