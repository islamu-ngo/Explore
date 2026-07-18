// ABOUTME: MediatR command for idempotently revoking a user authentication session by ID.
// ABOUTME: Carries only the opaque target ID; ownership is derived from current server context.
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;

public class DeleteUserAuthenticationTokenCommand : IRequest
{
    public Guid Id { get; set; }
}
