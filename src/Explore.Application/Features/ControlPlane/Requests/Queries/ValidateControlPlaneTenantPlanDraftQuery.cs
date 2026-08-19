// ABOUTME: Secured query for validating a tenant plan draft without persistence side effects.
// ABOUTME: Reuses the same SaaS-tier validator used by tenant plan commands.

using Explore.Application.Authorization;
using Explore.Application.Features.ControlPlane.Plans;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class ValidateControlPlaneTenantPlanDraftQuery(TenantPlanDraft draft)
    : IRequest<TenantPlanValidationResult>, ISecureRequest
{
    public const string SettingKey = "control-plane.tenant-plans";

    public TenantPlanDraft Draft { get; } = draft;

    string? ISecureRequest.ResourceId => SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
