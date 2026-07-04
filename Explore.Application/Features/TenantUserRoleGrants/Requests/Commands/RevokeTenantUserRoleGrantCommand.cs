// ABOUTME: CQRS command for revoking a tenant-scoped user role grant.
// ABOUTME: Requires tenant_user_role_grant Delete permission via AuthorizeResource.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.TenantUserRoleGrants.Requests.Commands;

[AuthorizeResource(ResourceKinds.TenantUserRoleGrant, AuthorizationActions.Delete)]
public class RevokeTenantUserRoleGrantCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    public Guid TenantId { get; init; }

    string? ISecureRequest.ResourceId => Id == Guid.Empty ? null : Id.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D")
    };
}
