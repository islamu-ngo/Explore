// ABOUTME: MediatR command for deleting a user account.
// ABOUTME: Carries the target user ID.
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Commands;

[AuthorizeResource(ResourceKinds.User, AuthorizationActions.Delete)]
public class DeleteUserCommand : IRequest<Unit>, ISecureRequest
{
    public Guid UserId { get; set; }

    string? ISecureRequest.ResourceId => UserId.ToString();
}
