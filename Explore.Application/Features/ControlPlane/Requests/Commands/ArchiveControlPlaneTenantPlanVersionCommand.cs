// ABOUTME: Secured command for archiving a tenant plan version.
// ABOUTME: Removes archived versions from future provisioning without mutating assigned tenants.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class ArchiveControlPlaneTenantPlanVersionCommand(Guid versionId)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plans";

    public Guid VersionId { get; } = versionId;

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["versionId"] = VersionId
    };
}
