// ABOUTME: CQRS command for granting a tenant-scoped role to an existing tenant user.
// ABOUTME: Requires tenant_user_role_grant Create permission via AuthorizeResource.

using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantUserRoleGrant;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantUserRoleGrants.Requests.Commands;

[AuthorizeResource(ResourceKinds.TenantUserRoleGrant, AuthorizationActions.Create)]
public class CreateTenantUserRoleGrantCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateTenantUserRoleGrantDto TenantUserRoleGrantDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
