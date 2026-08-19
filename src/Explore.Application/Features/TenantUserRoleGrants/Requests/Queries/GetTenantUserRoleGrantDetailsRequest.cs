// ABOUTME: CQRS query request for getting a tenant user role grant by ID.
// ABOUTME: Returns TenantUserRoleGrantDto with tenant-local user, role, and audit info.

using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantUserRoleGrant;
using MediatR;

namespace Explore.Application.Features.TenantUserRoleGrants.Requests.Queries;

[AuthorizeResource(ResourceKinds.TenantUserRoleGrant, AuthorizationActions.TenantUserRoleGrants.View)]
public class GetTenantUserRoleGrantDetailsRequest : IRequest<TenantUserRoleGrantDto?>, ISecureRequest
{
    public Guid Id { get; set; }

    public Guid TenantId { get; init; }

    string? ISecureRequest.ResourceId => Id == Guid.Empty ? null : Id.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new TenantScopedAuthorizationFacts(TenantId);
}
