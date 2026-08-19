// ABOUTME: Secured query for previewing tenant plan setting changes before assignment.
// ABOUTME: Produces a side-effect-free diff between effective settings and a proposed plan draft.

using Explore.Application.Authorization;
using Explore.Application.Features.ControlPlane.Plans;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class PreviewControlPlaneTenantPlanDiffQuery(
    TenantPlanEffectiveConfiguration current,
    TenantPlanDraft draft)
    : IRequest<TenantPlanDiffResult>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plan-assignments";

    public TenantPlanEffectiveConfiguration Current { get; } = current;
    public TenantPlanDraft Draft { get; } = draft;

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
