// ABOUTME: CQRS command for granting a tenant-scoped role to an existing tenant user.
// ABOUTME: Requires tenant_user_role_grant Create permission via AuthorizeResource.

using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantUserRoleGrant;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantUserRoleGrants.Requests.Commands;

[AuthorizeResource(ResourceKinds.TenantUserRoleGrant, AuthorizationActions.Create)]
public sealed record CreateTenantUserRoleGrantCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateTenantUserRoleGrantDto TenantUserRoleGrantDto { get; init; }

    public Guid TenantId { get; init; }

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new TenantScopedAuthorizationFacts(TenantId);
}
