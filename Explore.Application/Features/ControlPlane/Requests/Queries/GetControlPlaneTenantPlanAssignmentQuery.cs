// ABOUTME: Secured query for reading a tenant's active control-plane plan assignment.
// ABOUTME: Keeps tenant plan assignment visibility under instance-setting read authority.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class GetControlPlaneTenantPlanAssignmentQuery(Guid tenantId) : IRequest<ControlPlaneTenantPlanAssignmentDto?>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plan-assignments";

    public Guid TenantId { get; } = tenantId;

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["tenantId"] = TenantId.ToString()
    };
}
