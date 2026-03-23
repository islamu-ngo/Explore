// ABOUTME: MediatR command for removing a member from a group.
// ABOUTME: Carries the group member ID.
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.GroupMembers.Requests.Commands;

public class DeleteGroupMemberCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid MemberId { get; set; }
    public string? RequesterUserId { get; set; }
}
