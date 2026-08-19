// ABOUTME: Unlocks a previously locked tenant setting override so the tenant can edit it again.
// ABOUTME: Instance-admin scoped Control Plane command using explicit tenant identity.
using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

/// <summary>
/// Unlocks a tenant-scoped setting override for an explicit tenant, allowing the
/// tenant to edit it again. Used by instance administrators from the Control Plane.
/// </summary>
[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class UnlockControlPlaneTenantSettingCommand(Guid tenantId, string key)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; } = tenantId;
    public string Key { get; } = key;

    public const string SettingKey = "control-plane.tenant-effective-configuration";

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
