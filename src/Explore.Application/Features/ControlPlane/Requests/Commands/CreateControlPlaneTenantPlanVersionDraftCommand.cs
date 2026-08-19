// ABOUTME: Secured command for drafting a new version of an existing tenant plan tier.
// ABOUTME: Does not move assigned tenants; publishing decides whether existing tenants update.

using Explore.Application.Authorization;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class CreateControlPlaneTenantPlanVersionDraftCommand(string planKey, TenantPlanDraft draft)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plans";

    public string PlanKey { get; } = planKey;
    public TenantPlanDraft Draft { get; } = draft;

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
