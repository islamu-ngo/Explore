// ABOUTME: MediatR command request for updating an existing Group entity.
// ABOUTME: Carries the Group ID, UserId for authorization, and UpdateGroupDto with new values.

using Explore.Application.DTOs.Group;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Groups.Requests.Commands;

public class UpdateGroupCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required UpdateGroupDto GroupDto { get; set; }
}
