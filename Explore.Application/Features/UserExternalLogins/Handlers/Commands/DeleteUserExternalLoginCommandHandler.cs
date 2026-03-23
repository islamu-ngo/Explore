// ABOUTME: Handler for removing a user's external login link.
// ABOUTME: Fetches login record by ID and delegates deletion.
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.UserExternalLogins.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Handlers.Commands;

public class DeleteUserExternalLoginCommandHandler : IRequestHandler<DeleteUserExternalLoginCommand, bool>
{
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;

    public DeleteUserExternalLoginCommandHandler(IUserExternalLoginRepository userExternalLoginRepository)
    {
        _userExternalLoginRepository = userExternalLoginRepository;
    }

    public async Task<bool> Handle(DeleteUserExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var login = await _userExternalLoginRepository.GetById(request.Id);
        if (login == null)
        {
            return false;
        }

        var userLogins = await _userExternalLoginRepository.GetByUser(login.UserId);
        if (userLogins.Count <= 1)
        {
            throw new BadRequestException("Cannot unlink the last remaining authentication provider.");
        }

        await _userExternalLoginRepository.Delete(login);
        return true;
    }
}
