// ABOUTME: Secured command for cloning a tenant plan version into a new draft SaaS tier.
// ABOUTME: Copies pricing, setting, and quota template rows without assigning tenants.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class CloneControlPlaneTenantPlanCommand(Guid sourceVersionId, string key, string name)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plans";

    public Guid SourceVersionId { get; } = sourceVersionId;
    public string Key { get; } = key;
    public string Name { get; } = name;

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["sourceVersionId"] = SourceVersionId,
        ["planKey"] = Key
    };
}
