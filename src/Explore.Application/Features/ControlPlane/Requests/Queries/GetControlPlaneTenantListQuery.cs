// ABOUTME: Secured query for the multi-tenant control-plane tenant list.
// ABOUTME: Uses instance-setting authorization so only instance operators can inspect tenant fleet state.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed record GetControlPlaneTenantListQuery : IRequest<IReadOnlyList<ControlPlaneTenantListItemDto>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenants";

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
