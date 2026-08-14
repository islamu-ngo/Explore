// ABOUTME: Command request for revising a tenant paid-event policy.
// ABOUTME: Uses tenant setting authorization with the canonical paid-event policy id.

using Explore.Application.Authorization;
using Explore.Application.DTOs.PaidEventPolicies;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.PaidEventPolicies.Requests.Commands;

[AuthorizeResource(ResourceKinds.TenantSetting, AuthorizationActions.TenantSettings.Update)]
public sealed record ReviseTenantPaidEventPolicyCommand(Guid TenantId, RevisePaidEventPolicyDto Policy)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : $"{TenantId}:paid-event-policy";

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["settingKey"] = "paid-event-policy"
    };
}
