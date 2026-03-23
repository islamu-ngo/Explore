// ABOUTME: MediatR command for updating a user's profile fields.
// ABOUTME: Carries the UpdateUserDto payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.User;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Commands;

[AuthorizeResource("user", PermissionAction.Update)]
public class UpdateUserCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateUserDto UpdateUserDto { get; set; }

    string? ISecureRequest.ResourceId => UpdateUserDto.Id.ToString();
}
