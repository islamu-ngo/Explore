// ABOUTME: Control-plane command for applying an assigned tenant plan version to tenant settings.
// ABOUTME: Keeps plan application explicit, audited by caller identity, and separate from assignment switching.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed record ApplyControlPlaneTenantPlanAssignmentCommand(
    Guid TenantId,
    Guid AssignmentId,
    Guid AppliedByUserId) : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plan-assignments";

    public string ResourceId => SettingKey;

    public IDictionary<string, object> ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["tenantId"] = TenantId,
        ["assignmentId"] = AssignmentId
    };
}
