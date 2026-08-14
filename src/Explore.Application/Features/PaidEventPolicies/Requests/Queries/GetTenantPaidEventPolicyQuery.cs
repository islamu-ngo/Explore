// ABOUTME: Query request for reading the active tenant paid-event policy.
// ABOUTME: Uses tenant setting authorization with the canonical paid-event policy id.

using Explore.Application.Authorization;
using Explore.Application.DTOs.PaidEventPolicies;
using MediatR;

namespace Explore.Application.Features.PaidEventPolicies.Requests.Queries;

[AuthorizeResource(ResourceKinds.TenantSetting, AuthorizationActions.TenantSettings.View)]
public sealed record GetTenantPaidEventPolicyQuery(Guid TenantId) : IRequest<PaidEventPolicyDto?>, ISecureRequest
{
    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : $"{TenantId}:paid-event-policy";

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["settingKey"] = "paid-event-policy"
    };
}
