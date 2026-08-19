// ABOUTME: Locks a tenant setting override so the tenant cannot unlock or change it.
// ABOUTME: Instance-admin scoped Control Plane command using explicit tenant identity.
using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

/// <summary>
/// Locks a tenant-scoped setting override for an explicit tenant, preventing the
/// tenant from editing it. Used by instance administrators from the Control Plane.
/// </summary>
[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class LockControlPlaneTenantSettingCommand(Guid tenantId, string key)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; } = tenantId;
    public string Key { get; } = key;

    public const string SettingKey = "control-plane.tenant-effective-configuration";

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
