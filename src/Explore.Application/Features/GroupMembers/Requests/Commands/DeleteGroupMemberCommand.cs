// ABOUTME: MediatR command for removing a member from a group.
// ABOUTME: Carries the group member ID.
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.GroupMembers.Requests.Commands;

public sealed record DeleteGroupMemberCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid MemberId { get; init; }
    public string? RequesterUserId { get; init; }
}
