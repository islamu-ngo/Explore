// ABOUTME: MediatR command for updating a user's profile fields.
// ABOUTME: Carries the route user ID and grouped UpdateUserDto payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.User;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Commands;

[AuthorizeResource(ResourceKinds.User, AuthorizationActions.Update)]
public class UpdateUserCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid UserId { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    public required UpdateUserDto UpdateUserDto { get; set; }

    string? ISecureRequest.ResourceId => UserId.ToString();
}
