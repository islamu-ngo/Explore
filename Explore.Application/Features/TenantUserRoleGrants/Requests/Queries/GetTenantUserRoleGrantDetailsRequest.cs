// ABOUTME: CQRS query request for getting a tenant user role grant by ID.
// ABOUTME: Returns TenantUserRoleGrantDto with tenant-local user, role, and audit info.

using Explore.Application.DTOs.TenantUserRoleGrant;
using MediatR;

namespace Explore.Application.Features.TenantUserRoleGrants.Requests.Queries;

public class GetTenantUserRoleGrantDetailsRequest : IRequest<TenantUserRoleGrantDto?>
{
    public Guid Id { get; set; }
}
