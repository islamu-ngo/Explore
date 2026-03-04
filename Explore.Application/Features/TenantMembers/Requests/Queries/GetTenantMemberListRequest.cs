// ABOUTME: CQRS query request for listing all tenant members.
// ABOUTME: Returns List<TenantMemberListDto> with user, tenant, and role info.

using Explore.Application.DTOs.TenantMember;
using MediatR;

namespace Explore.Application.Features.TenantMembers.Requests.Queries;

public class GetTenantMemberListRequest : IRequest<List<TenantMemberListDto>>
{
}
