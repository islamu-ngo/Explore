using Explore.Application.DTOs.GroupMember;
using MediatR;

namespace Explore.Application.Features.GroupMembers.Requests.Queries;

public class GetGroupMembersRequest : IRequest<List<GroupMemberDto>>
{
    public Guid GroupId { get; set; }
}
