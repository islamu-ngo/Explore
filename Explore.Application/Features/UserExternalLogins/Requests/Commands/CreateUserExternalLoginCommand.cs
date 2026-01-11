using MediatR;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.Responses;

namespace Explore.Application.Features.UserExternalLogins.Requests.Commands
{
    public class CreateUserExternalLoginCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateUserExternalLoginDto UserExternalLoginDto { get; set; }
    }
}
