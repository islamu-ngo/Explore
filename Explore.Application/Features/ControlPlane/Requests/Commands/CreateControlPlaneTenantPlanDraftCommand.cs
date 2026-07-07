// ABOUTME: Secured command for creating a draft control-plane tenant plan SaaS tier.
// ABOUTME: Reuses tenant plan draft validation before any persistence or provisioning side effects.

using Explore.Application.Authorization;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class CreateControlPlaneTenantPlanDraftCommand(TenantPlanDraft draft)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plans";

    public TenantPlanDraft Draft { get; } = draft;

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["planKey"] = Draft.Key
    };
}
