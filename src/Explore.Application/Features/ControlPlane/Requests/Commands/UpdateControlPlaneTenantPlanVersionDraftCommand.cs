// ABOUTME: Secured command for replacing a draft tenant plan version's template content.
// ABOUTME: Keeps plan updates versioned while validating pricing, settings, and quotas first.

using Explore.Application.Authorization;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class UpdateControlPlaneTenantPlanVersionDraftCommand(
    Guid versionId,
    PatchControlPlaneTenantPlanVersionDraftDto update)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plans";

    public Guid VersionId { get; } = versionId;
    public PatchControlPlaneTenantPlanVersionDraftDto Update { get; } = update;

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}

public sealed class PatchControlPlaneTenantPlanVersionDraftDto
{
    public PatchTenantPlanPricingDto? Pricing { get; set; }
    public PatchTenantPlanProvisioningDto? IsActiveForProvisioning { get; set; }
    public PatchTenantPlanSettingOverridesDto? SettingOverrides { get; set; }
    public PatchTenantPlanQuotaLimitsDto? QuotaLimits { get; set; }
}

public sealed class PatchTenantPlanPricingDto
{
    public decimal? Amount { get; set; }
    public string? CurrencyCode { get; set; }
    public string? BillingPeriod { get; set; }
}

public sealed class PatchTenantPlanProvisioningDto
{
    public bool? Value { get; set; }
}

public sealed class PatchTenantPlanSettingOverridesDto
{
    public IReadOnlyList<TenantPlanSettingOverride>? Values { get; set; }
}

public sealed class PatchTenantPlanQuotaLimitsDto
{
    public IReadOnlyList<TenantPlanQuotaLimit>? Values { get; set; }
}
