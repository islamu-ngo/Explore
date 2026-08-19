// ABOUTME: Writes or updates a tenant-scoped setting override with an explicit tenant identity.
// ABOUTME: Instance-admin scoped Control Plane command, fails closed when target setting is system-locked.
using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

/// <summary>
/// Writes or updates a tenant-scoped setting override for an explicit tenant from
/// the Control Plane. Bypasses the current-tenant context used by the regular
/// tenant settings endpoints, so instance administrators can govern any tenant.
/// </summary>
[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class SetControlPlaneTenantSettingCommand(Guid tenantId, string key, string value)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; } = tenantId;
    public string Key { get; } = key;
    public string Value { get; } = value;

    public const string SettingKey = "control-plane.tenant-effective-configuration";

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
