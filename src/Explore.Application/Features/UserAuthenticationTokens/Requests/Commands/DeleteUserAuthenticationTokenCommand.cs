// ABOUTME: MediatR command for deleting a user authentication token by ID.
// ABOUTME: Carries the target token ID.
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;

public class DeleteUserAuthenticationTokenCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
