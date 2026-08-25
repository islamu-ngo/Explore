// ABOUTME: Secured query for reading a tenant's active control-plane plan assignment.
// ABOUTME: Keeps tenant plan assignment visibility under instance-setting read authority.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed record GetControlPlaneTenantPlanAssignmentQuery
    : IRequest<ControlPlaneTenantPlanAssignmentDto?>, ISecureRequest
{
    public GetControlPlaneTenantPlanAssignmentQuery(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public const string SettingKey = "control-plane.tenant-plan-assignments";

    public Guid TenantId { get; }

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
