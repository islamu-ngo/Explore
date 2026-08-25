// ABOUTME: Secured query for a single control-plane tenant lifecycle detail resource.
// ABOUTME: Returns bounded tenant metadata plus lifecycle audit history for instance operators.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed record GetControlPlaneTenantDetailsQuery
    : IRequest<ControlPlaneTenantDetailDto?>, ISecureRequest
{
    public GetControlPlaneTenantDetailsQuery(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public const string SettingKey = "control-plane.tenants";

    public Guid TenantId { get; }

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
