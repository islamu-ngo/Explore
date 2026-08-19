// ABOUTME: Secured query for a single control-plane tenant lifecycle detail resource.
// ABOUTME: Returns bounded tenant metadata plus lifecycle audit history for instance operators.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class GetControlPlaneTenantDetailsQuery(Guid tenantId) : IRequest<ControlPlaneTenantDetailDto?>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenants";

    public Guid TenantId { get; } = tenantId;

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
