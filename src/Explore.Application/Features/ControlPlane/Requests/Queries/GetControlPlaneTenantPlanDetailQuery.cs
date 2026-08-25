// ABOUTME: Secured query for reading a control-plane tenant plan and version history.
// ABOUTME: Returns normalized SaaS tier settings and quotas without exposing tenant data.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed record GetControlPlaneTenantPlanDetailQuery
    : IRequest<ControlPlaneTenantPlanDetailDto?>, ISecureRequest
{
    public GetControlPlaneTenantPlanDetailQuery(string key)
    {
        Key = key;
    }

    public const string SettingKey = "control-plane.tenant-plans";

    public string Key { get; }

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
