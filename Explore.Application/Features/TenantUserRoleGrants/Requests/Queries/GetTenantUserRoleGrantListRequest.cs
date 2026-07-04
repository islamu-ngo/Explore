// ABOUTME: CQRS query request for listing tenant user role grants.
// ABOUTME: Returns List<TenantUserRoleGrantListDto> with user, tenant, role, and grant audit info.

using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantUserRoleGrant;
using MediatR;

namespace Explore.Application.Features.TenantUserRoleGrants.Requests.Queries;

[AuthorizeResource(ResourceKinds.TenantUserRoleGrant, AuthorizationActions.TenantUserRoleGrants.View)]
public class GetTenantUserRoleGrantListRequest : IRequest<List<TenantUserRoleGrantListDto>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D")
    };
}
