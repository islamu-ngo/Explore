// ABOUTME: CQRS command for removing one user's participation from one tenant.
// ABOUTME: Uses user-update authorization because membership removal leaves the global account intact.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.TenantUsers.Requests.Commands;

[AuthorizeResource(ResourceKinds.User, AuthorizationActions.Update)]
public sealed record RemoveTenantMembershipCommand(Guid TenantId, Guid UserId) : IRequest<bool>, ISecureRequest
{
    string? ISecureRequest.ResourceId => UserId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["userId"] = UserId.ToString("D")
    };
}
