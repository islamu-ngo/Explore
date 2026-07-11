// ABOUTME: Secured command for rolling a tenant back to a previous plan assignment.
// ABOUTME: Reactivates the chosen assignment and marks the current active row as rolled back.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class RollbackControlPlaneTenantPlanAssignmentCommand(
    Guid tenantId,
    Guid assignmentId,
    Guid operatorId)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plan-assignments";

    public Guid TenantId { get; } = tenantId;
    public Guid AssignmentId { get; } = assignmentId;
    public Guid OperatorId { get; } = operatorId;

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["tenantId"] = TenantId,
        ["assignmentId"] = AssignmentId
    };
}
