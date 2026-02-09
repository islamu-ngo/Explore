using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserExternalLogin;
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

        await _userExternalLoginRepository.Delete(login);
        return true;
    }
}
