// ABOUTME: Handler for revoking/deleting a user authentication token.
// ABOUTME: Fetches token by ID and delegates deletion.
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Handlers.Commands;

public class DeleteUserAuthenticationTokenCommandHandler : IRequestHandler<DeleteUserAuthenticationTokenCommand, bool>
{
    private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteUserAuthenticationTokenCommandHandler(
        IUserAuthenticationTokenRepository userAuthenticationTokenRepository,
        ICurrentUserService currentUserService)
    {
        _userAuthenticationTokenRepository = userAuthenticationTokenRepository;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteUserAuthenticationTokenCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new AuthorizationException(ResourceKinds.User, AuthorizationActions.Users.Update);

        var token = await _userAuthenticationTokenRepository.GetByIdForUser(
            request.Id,
            currentUserId,
            cancellationToken);
        if (token == null)
        {
            return false;
        }

        await _userAuthenticationTokenRepository.Delete(token);
        return true;
    }
}
