// ABOUTME: MediatR command for adding a member to a group.
// ABOUTME: Carries the target group ID and user/actor ID.
using Explore.Application.DTOs.GroupMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.GroupMembers.Requests.Commands;

public class AddGroupMemberCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required AddGroupMemberDto AddGroupMemberDto { get; set; }
    public string? RequesterUserId { get; set; }
}
