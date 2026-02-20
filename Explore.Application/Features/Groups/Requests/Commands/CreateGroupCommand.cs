// ABOUTME: MediatR command request for creating a new Group entity.
// ABOUTME: Carries CreateGroupDto and returns BaseCommandResponse<Guid> with the new Group ID.

using Explore.Application.DTOs.Group;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Groups.Requests.Commands;

public class CreateGroupCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateGroupDto GroupDto { get; set; }
}
