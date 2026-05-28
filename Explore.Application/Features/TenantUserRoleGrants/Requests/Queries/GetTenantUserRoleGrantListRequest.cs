// ABOUTME: CQRS query request for listing tenant user role grants.
// ABOUTME: Returns List<TenantUserRoleGrantListDto> with user, tenant, role, and grant audit info.

using Explore.Application.DTOs.TenantUserRoleGrant;
using MediatR;

namespace Explore.Application.Features.TenantUserRoleGrants.Requests.Queries;

public class GetTenantUserRoleGrantListRequest : IRequest<List<TenantUserRoleGrantListDto>>
{
}
