// ABOUTME: Secured command for publishing a draft tenant plan version.
// ABOUTME: Encodes the instance-admin choice to pin or move existing tenant assignments.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

public enum TenantPlanExistingAssignmentPolicy
{
    LeaveExistingTenantsPinned = 0,
    MoveExistingTenantsToPublishedVersion = 1
}

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed record PublishControlPlaneTenantPlanVersionCommand
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public PublishControlPlaneTenantPlanVersionCommand(
        Guid versionId,
        TenantPlanExistingAssignmentPolicy existingTenantPolicy)
    {
        VersionId = versionId;
        ExistingTenantPolicy = existingTenantPolicy;
    }

    public const string SettingKey = "control-plane.tenant-plans";

    public Guid VersionId { get; }
    public TenantPlanExistingAssignmentPolicy ExistingTenantPolicy { get; }

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
