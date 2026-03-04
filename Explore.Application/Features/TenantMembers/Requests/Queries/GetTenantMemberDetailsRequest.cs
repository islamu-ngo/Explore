// ABOUTME: CQRS query request for getting tenant member details by ID.
// ABOUTME: Returns TenantMemberDto with full user, tenant, and role info.

using Explore.Application.DTOs.TenantMember;
using MediatR;

namespace Explore.Application.Features.TenantMembers.Requests.Queries;

public class GetTenantMemberDetailsRequest : IRequest<TenantMemberDto>
{
    public Guid Id { get; set; }
}
