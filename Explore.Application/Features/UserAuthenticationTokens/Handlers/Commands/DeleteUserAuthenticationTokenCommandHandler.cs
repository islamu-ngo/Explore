// ABOUTME: Handler for revoking/deleting a user authentication token.
// ABOUTME: Fetches token by ID and delegates deletion.
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Handlers.Commands;

public class DeleteUserAuthenticationTokenCommandHandler : IRequestHandler<DeleteUserAuthenticationTokenCommand, bool>
{
    private readonly IUserAuthenticationTokenRepository _userAuthenticationTokenRepository;

    public DeleteUserAuthenticationTokenCommandHandler(IUserAuthenticationTokenRepository userAuthenticationTokenRepository)
    {
        _userAuthenticationTokenRepository = userAuthenticationTokenRepository;
    }

    public async Task<bool> Handle(DeleteUserAuthenticationTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _userAuthenticationTokenRepository.GetById(request.Id);
        if (token == null)
        {
            return false;
        }

        await _userAuthenticationTokenRepository.Delete(token);
        return true;
    }
}
