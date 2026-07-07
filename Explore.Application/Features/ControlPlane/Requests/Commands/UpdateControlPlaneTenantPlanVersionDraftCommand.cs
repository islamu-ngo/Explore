// ABOUTME: Secured command for replacing a draft tenant plan version's template content.
// ABOUTME: Keeps plan updates versioned while validating pricing, settings, and quotas first.

using Explore.Application.Authorization;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class UpdateControlPlaneTenantPlanVersionDraftCommand(Guid versionId, TenantPlanDraft draft)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plans";

    public Guid VersionId { get; } = versionId;
    public TenantPlanDraft Draft { get; } = draft;

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["versionId"] = VersionId,
        ["planKey"] = Draft.Key
    };
}
