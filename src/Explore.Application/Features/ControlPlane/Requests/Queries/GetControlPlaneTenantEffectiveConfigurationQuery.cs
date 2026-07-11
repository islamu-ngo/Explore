// ABOUTME: Secured query for a tenant's effective control-plane configuration read model.
// ABOUTME: Reuses instance-setting read authority for plan assignment, resolved settings, and quota usage.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class GetControlPlaneTenantEffectiveConfigurationQuery(Guid tenantId)
    : IRequest<ControlPlaneTenantEffectiveConfigurationDto>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-effective-configuration";

    public Guid TenantId { get; } = tenantId;

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
        ["tenantId"] = TenantId.ToString()
    };
}
